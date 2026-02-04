# Research Findings: implement-data-sync

## Overview
数据同步功能的调研发现和技术设计。

---

## 设计决策汇总

| 决策点 | 选择 | 原因 |
|--------|------|------|
| 数据量级 | 小数据量（<1000条） | 全量 Checksum 比对，简单可靠 |
| 同步触发 | 纯手动 | 完全可控，用户明确知道何时同步 |
| 删除策略 | 软删除 + 引用检查 | 有引用数据只能禁用，无引用可删除 |
| Checksum 范围 | 业务字段 + Status + IsDeleted | 排除审计字段避免"假差异" |
| 冲突处理 | 批量选择 + 预览对比 | 用户决定，直观高效 |
| 同步粒度 | 实体级 | 简单实现，字段级太复杂 |

---

## 引用检查设计

### 引用关系

```
Patient ←───── MedicalCase ←───── Prescription ←───── PrescriptionItem ─────→ Herb
                                        │
                                        └─ ReferencedFormulas (文本描述，非外键)
                                                    │
                                              Formula (模板)
```

### 检查策略

| 实体 | 引用检查 | 删除策略 |
|------|---------|----------|
| **Herb** | 查询 `PrescriptionItem.HerbId` | 有引用→禁用；无引用→软删除 |
| **Patient** | 查询 `MedicalCase.PatientId` | 有医案→禁用；无医案→软删除 |
| **Formula** | 不需要（文本描述引用） | 可直接软删除 |

### 实现任务

| 任务 | 当前状态 | 本次需完成 |
|------|---------|-----------|
| Herb 引用检查 | 框架存在，逻辑 TODO | 实现查询 `PrescriptionItem` |
| Patient 引用检查 | 不存在 | 新增 `CheckReferenceAsync` 方法 |

---

## SyncLog 表设计

### 本地表 (SQLite)

```sql
CREATE TABLE SyncLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    -- 实体标识
    EntityType TEXT NOT NULL,           -- Herb/Patient/Formula
    EntityId TEXT NOT NULL,             -- GUID

    -- 变更信息
    Operation TEXT NOT NULL,            -- Create/Update/Delete
    ChangedFields TEXT,                 -- 变更字段列表(JSON)，用于冲突展示
    LocalChecksum TEXT NOT NULL,        -- 本地数据Checksum (SHA256)

    -- 时间戳
    LocalChangedAt DATETIME NOT NULL,   -- 本地变更时间

    -- 同步状态
    SyncStatus TEXT NOT NULL DEFAULT 'Pending',
        -- Pending: 待同步
        -- Synced: 已同步
        -- Conflict: 冲突待处理
        -- Skipped: 用户跳过

    SyncedAt DATETIME,                  -- 同步完成时间
    ErrorMessage TEXT                   -- 错误信息
);

CREATE INDEX IX_SyncLog_Status ON SyncLog(SyncStatus);
CREATE INDEX IX_SyncLog_Entity ON SyncLog(EntityType, EntityId);
```

### 服务器表 (SQL Server)

```sql
CREATE TABLE SyncMetadata (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EntityType NVARCHAR(50) NOT NULL,
    EntityId UNIQUEIDENTIFIER NOT NULL,
    LastModifiedAt DATETIME2 NOT NULL,
    ModifiedBy UNIQUEIDENTIFIER,
    Checksum NVARCHAR(64) NOT NULL,     -- SHA256
    IsDeleted BIT DEFAULT 0,

    INDEX IX_SyncMetadata_Entity (EntityType, EntityId),
    INDEX IX_SyncMetadata_Modified (LastModifiedAt)
);
```

---

## Checksum 计算

### 算法

```csharp
public static class ChecksumHelper
{
    /// <summary>
    /// 计算实体 Checksum（排除审计字段）
    /// </summary>
    public static string ComputeChecksum<T>(T entity) where T : class
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // 序列化为 JSON（排除审计字段）
        var json = JsonSerializer.Serialize(entity, options);

        // 计算 SHA256
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
```

### Checksum 字段配置

| 实体 | 纳入字段 | 排除字段 |
|------|---------|---------|
| **Herb** | Name, PinYinCode, Category, Origin, Spec, Unit, Price, CostPrice, Effect, Usage, Remark, Status | CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, RowVersion, IsDeleted* |
| **Patient** | Name, PinYinCode, Gender, BirthDate, IdNumber, PhoneNumber, Address, AllergyHistory, MedicalHistory, Status, DisableReason | 同上 |
| **Formula** | Name, PinYinCode, Category, Effect, Usage, Remark, Status, Items[] | 同上 |

*注：IsDeleted 单独处理，删除操作有专门的同步逻辑

---

## 差异检测算法

### 流程

```
1. 本地获取所有实体的 (EntityId, LocalChecksum, LocalChangedAt)
2. 调用服务器 API 获取 (EntityId, ServerChecksum, ServerChangedAt)
3. 比对生成差异列表：

   LocalOnly:   本地有，服务器无 → 待上传
   ServerOnly:  服务器有，本地无 → 待下载
   Modified:    双方都有但 Checksum 不同 → 冲突
   Identical:   Checksum 相同 → 无需同步
```

### 数据结构

```csharp
public class SyncDiff
{
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public DiffType Type { get; set; }

    // 用于冲突展示
    public string? EntityName { get; set; }        // 实体名称（如药材名）
    public string? LocalChecksum { get; set; }
    public string? ServerChecksum { get; set; }
    public DateTime? LocalChangedAt { get; set; }
    public DateTime? ServerChangedAt { get; set; }
    public List<string>? ChangedFields { get; set; } // 变更字段列表
}

public enum DiffType
{
    LocalOnly,      // 仅本地有（待上传）
    ServerOnly,     // 仅服务器有（待下载）
    Modified,       // 双方都有但不同（冲突）
    Identical       // 完全相同（无需同步）
}
```

---

## 同步 API 设计

### 基础数据同步

```
GET  /api/v1/sync/metadata
     Query: entityType=Herb
     Response: [{ entityId, checksum, lastModifiedAt, isDeleted }]

POST /api/v1/sync/compare
     Body: { entityType, localEntities: [{ entityId, checksum }] }
     Response: { diffs: [{ entityId, diffType, serverChecksum, serverChangedAt }] }

POST /api/v1/sync/upload
     Body: { entityType, entities: [...] }
     Response: { success: [...], conflicts: [...], errors: [...] }

POST /api/v1/sync/download
     Body: { entityType, entityIds: [...] }
     Response: { entities: [...] }
```

### 删除同步

```
POST /api/v1/sync/delete
     Body: { entityType, entityIds: [...] }
     Response: {
       success: [...],
       rejected: [{ entityId, reason: "有引用数据，请先禁用" }]
     }
```

---

## UI 流程设计

### 同步主界面

```
┌─────────────────────────────────────────────────────────────┐
│ 数据同步                                    [检查同步]       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  上次同步：2026-02-03 10:30:00                              │
│  本地待同步：5 条                                            │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 药材 (Herb)           本地: 3条   服务器: 2条        │   │
│  │ 患者 (Patient)        本地: 1条   服务器: 0条        │   │
│  │ 经验方 (Formula)      本地: 1条   服务器: 1条        │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│                              [开始同步]                      │
└─────────────────────────────────────────────────────────────┘
```

### 差异预览界面

```
┌─────────────────────────────────────────────────────────────┐
│ 同步预览                                                    │
├─────────────────────────────────────────────────────────────┤
│ 待上传 (3)                                                  │
│ ┌─────────────────────────────────────────────────────┐    │
│ │ ☑ 黄芪 (Herb) - 新增                                │    │
│ │ ☑ 当归 (Herb) - 修改：价格 45→50                    │    │
│ │ ☑ 张三 (Patient) - 新增                             │    │
│ └─────────────────────────────────────────────────────┘    │
│                                                             │
│ 待下载 (2)                                                  │
│ ┌─────────────────────────────────────────────────────┐    │
│ │ ☑ 人参 (Herb) - 新增                                │    │
│ │ ☑ 逍遥散 (Formula) - 修改：用法说明                  │    │
│ └─────────────────────────────────────────────────────┘    │
│                                                             │
│            [全选上传] [全选下载] [执行同步] [取消]           │
└─────────────────────────────────────────────────────────────┘
```

### 冲突处理界面

```
┌─────────────────────────────────────────────────────────────┐
│ 检测到 2 条数据冲突                                          │
├─────────────────────────────────────────────────────────────┤
│ 黄芪 (Herb)                                                 │
│ ┌───────────────────────┬───────────────────────┐          │
│ │ 本地版本              │ 服务器版本             │          │
│ │ 价格: 50元/克         │ 价格: 55元/克          │          │
│ │ 修改: 02-03 10:30     │ 修改: 02-03 09:15      │          │
│ └───────────────────────┴───────────────────────┘          │
│                    [使用本地] [使用服务器] [跳过]            │
├─────────────────────────────────────────────────────────────┤
│ 张三 (Patient)                                              │
│ ┌───────────────────────┬───────────────────────┐          │
│ │ 本地版本              │ 服务器版本             │          │
│ │ 地址: 北京市朝阳区    │ 地址: 北京市海淀区      │          │
│ │ 修改: 02-03 11:00     │ 修改: 02-03 10:45      │          │
│ └───────────────────────┴───────────────────────┘          │
│                    [使用本地] [使用服务器] [跳过]            │
├─────────────────────────────────────────────────────────────┤
│        [全部使用本地] [全部使用服务器] [完成]                │
└─────────────────────────────────────────────────────────────┘
```

---

## 架构设计

### 服务层架构

```
┌─────────────────────────────────────────────────────────────┐
│                     SyncViewModel                            │
│              (UI 绑定、用户交互、进度展示)                    │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                      ISyncService                            │
│   CheckDiffAsync / UploadAsync / DownloadAsync / Resolve    │
└─────────────────────────┬───────────────────────────────────┘
                          │
         ┌────────────────┼────────────────┐
         │                │                │
┌────────▼────────┐ ┌─────▼─────┐ ┌────────▼────────┐
│ ISyncApiClient  │ │ ISyncLog  │ │ IDataSource     │
│ (远程 API 调用) │ │ Repository│ │ (本地数据访问)  │
└─────────────────┘ └───────────┘ └─────────────────┘
```

### 新增文件清单

**Desktop 端**:
```
src/Client/Desktop/Core/LYBT.Desktop.LocalData/
├── Entities/
│   └── SyncLog.cs
├── Repositories/
│   └── SyncLogRepository.cs

src/Client/Desktop/Core/LYBT.Desktop.Contracts/
├── Services/
│   ├── ISyncService.cs
│   └── ISyncApiClient.cs
├── Models/
│   ├── SyncDiff.cs
│   └── SyncResult.cs

src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/
├── Services/
│   ├── SyncService.cs
│   ├── SyncApiClient.cs
│   └── ChecksumHelper.cs

src/Client/Desktop/Modules/LYBT.Desktop.Sync/  (新模块)
├── ViewModels/
│   ├── SyncViewModel.cs
│   └── ConflictResolutionViewModel.cs
├── Views/
│   ├── SyncView.xaml
│   └── ConflictResolutionDialog.xaml
└── SyncModule.cs
```

**Server 端**:
```
src/Server/Modules/LYBT.Module.Sync/  (新模块)
├── Controllers/
│   └── SyncController.cs
├── Services/
│   ├── ISyncService.cs
│   ├── SyncService.cs
│   └── ChecksumHelper.cs
├── Entities/
│   └── SyncMetadata.cs
├── Repositories/
│   └── SyncMetadataRepository.cs
└── SyncModule.cs
```

---

## 同步流程时序图

```
┌──────┐          ┌──────────┐          ┌──────────┐          ┌────────┐
│ User │          │ Desktop  │          │  Server  │          │   DB   │
└──┬───┘          └────┬─────┘          └────┬─────┘          └───┬────┘
   │                   │                     │                    │
   │ 点击[检查同步]    │                     │                    │
   │──────────────────>│                     │                    │
   │                   │                     │                    │
   │                   │ 获取本地实体Checksum │                    │
   │                   │────────────────────────────────────────>│
   │                   │<────────────────────────────────────────│
   │                   │                     │                    │
   │                   │ POST /sync/compare  │                    │
   │                   │────────────────────>│                    │
   │                   │                     │ 获取服务器Checksum  │
   │                   │                     │───────────────────>│
   │                   │                     │<───────────────────│
   │                   │   返回差异列表       │                    │
   │                   │<────────────────────│                    │
   │                   │                     │                    │
   │   展示差异预览    │                     │                    │
   │<──────────────────│                     │                    │
   │                   │                     │                    │
   │ 确认同步/处理冲突 │                     │                    │
   │──────────────────>│                     │                    │
   │                   │                     │                    │
   │                   │ POST /sync/upload   │                    │
   │                   │────────────────────>│                    │
   │                   │                     │───────────────────>│
   │                   │<────────────────────│                    │
   │                   │                     │                    │
   │                   │ POST /sync/download │                    │
   │                   │────────────────────>│                    │
   │                   │<────────────────────│                    │
   │                   │                     │                    │
   │                   │ 更新本地数据         │                    │
   │                   │────────────────────────────────────────>│
   │                   │                     │                    │
   │   同步完成        │                     │                    │
   │<──────────────────│                     │                    │
   │                   │                     │                    │
```

---

## 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| 网络中断 | 同步操作支持重试，每条记录独立处理 |
| 数据丢失 | 软删除机制，同步前本地备份 |
| 冲突过多 | 清晰的冲突展示UI，批量处理选项 |
| 引用检查遗漏 | 服务器端再次验证，拒绝非法删除 |

---

## Open Questions (已解决)

1. ~~同步粒度：实体级 vs 字段级？~~ → 实体级
2. ~~同步触发：手动 vs 自动？~~ → 手动
3. ~~删除策略？~~ → 软删除 + 引用检查
4. ~~Checksum 字段范围？~~ → 业务字段 + Status，排除审计字段
5. ~~冲突处理交互？~~ → 批量选择 + 预览对比

---

## References

- [implement-local-mode 归档](openspec/changes/archive/2026-02-03-implement-local-mode/)
- [Offline-First Architecture Best Practices](https://developer.android.com/topic/architecture/data-layer/offline-first)
- [Conflict Resolution Strategies](https://mobterest.medium.com/conflict-resolution-strategies-in-data-synchronization-2a10be5b82bc)

---
*Created: 2026-02-03*
*Last Updated: 2026-02-04*
