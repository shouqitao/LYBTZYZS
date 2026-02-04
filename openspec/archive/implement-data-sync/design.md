# implement-data-sync 设计文档

## 概述

基于 [proposal.md](./proposal.md) 的详细技术设计，实现基础数据（Herb、Patient、Formula）的双向同步功能。

## 架构决策

### ADR-1: 全量 Checksum 比对

**状态**: 已采纳

**背景**: 需要检测本地与服务器数据的差异

**决策**: 采用全量 Checksum 比对，而非增量时间戳比对

**后果**:
- 正面: 简单可靠，不依赖时钟同步
- 负面: 每次同步需传输所有实体的 Checksum（小数据量可接受）

### ADR-2: 实体级同步粒度

**状态**: 已采纳

**背景**: 冲突时需要决定保留哪个版本

**决策**: 以实体为单位进行同步，不支持字段级合并

**后果**:
- 正面: 实现简单，用户理解容易
- 负面: 无法自动合并不同字段的修改

### ADR-3: 引用检查拦截删除

**状态**: 已采纳

**背景**: 防止删除被引用的数据导致数据不一致

**决策**: 删除前检查引用，有引用时拒绝删除并提示禁用

**后果**:
- 正面: 数据完整性有保障
- 负面: 用户需要额外操作（先禁用再考虑删除）

## 实现策略

### Phase 1: 引用检查实现

#### 1.1 Herb 引用检查完善

**现有代码位置**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:500-525`

**当前状态**: 框架存在，TODO 未实现

**实现方案**:
```csharp
// HerbService.CheckReferenceAsync 实现
var referenceCount = await _dbContext.PrescriptionItems
    .CountAsync(pi => pi.HerbId == herbId);

var recentReferences = await _dbContext.PrescriptionItems
    .Where(pi => pi.HerbId == herbId)
    .OrderByDescending(pi => pi.Prescription.CreatedAt)
    .Take(5)
    .Select(pi => new PrescriptionReferenceDto { ... })
    .ToListAsync();
```

**需要注入**: `ApplicationDbContext` 或通过 Repository 暴露查询方法

#### 1.2 Patient 引用检查新增

**参考 DTO**: `HerbReferenceCheckDto` 已存在，创建类似的 `PatientReferenceCheckDto`

**实现方案**:
```csharp
public class PatientReferenceCheckDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }
    public bool HasReferences { get; set; }
    public int MedicalCaseCount { get; set; }
    public bool CanDelete { get; set; }
    public List<MedicalCaseReferenceDto>? RecentMedicalCases { get; set; }
}
```

### Phase 2: 服务器端同步模块

#### 2.1 模块结构

```
src/Server/Modules/LYBT.Module.Sync/
├── SyncModule.cs                    # 模块注册
├── Controllers/
│   └── SyncController.cs            # API 控制器
├── Services/
│   ├── ISyncService.cs              # 服务接口
│   ├── SyncService.cs               # 服务实现
│   └── ChecksumHelper.cs            # Checksum 计算
├── Entities/
│   └── SyncMetadata.cs              # 同步元数据实体
└── Repositories/
    ├── ISyncMetadataRepository.cs   # 仓储接口
    └── SyncMetadataRepository.cs    # 仓储实现
```

#### 2.2 SyncMetadata 实体

```csharp
[Table("SyncMetadata")]
public class SyncMetadata
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(50)]
    public string EntityType { get; set; }  // Herb/Patient/Formula

    [Required]
    public Guid EntityId { get; set; }

    [Required]
    public DateTime LastModifiedAt { get; set; }

    public Guid? ModifiedBy { get; set; }

    [Required]
    [StringLength(64)]
    public string Checksum { get; set; }  // SHA256

    public bool IsDeleted { get; set; }
}
```

#### 2.3 ChecksumHelper

```csharp
public static class ChecksumHelper
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Compute<T>(T entity) where T : class
    {
        // 排除审计字段的序列化
        var json = JsonSerializer.Serialize(entity, _options);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
```

#### 2.4 API 端点设计

| 端点 | 方法 | 描述 |
|------|------|------|
| `/api/v1/sync/metadata` | GET | 获取指定类型的所有实体元数据 |
| `/api/v1/sync/compare` | POST | 比对本地与服务器差异 |
| `/api/v1/sync/upload` | POST | 上传本地变更到服务器 |
| `/api/v1/sync/download` | POST | 下载服务器数据到本地 |
| `/api/v1/sync/delete` | POST | 同步删除（含引用检查） |

### Phase 3: 共享层 DTO

#### 3.1 文件位置

```
src/Shared/LYBT.Shared.Models/Contracts/Sync/
├── SyncMetadataDto.cs
├── SyncCompareRequestDto.cs
├── SyncCompareResponseDto.cs
├── SyncUploadRequestDto.cs
├── SyncUploadResponseDto.cs
├── SyncDownloadRequestDto.cs
├── SyncDownloadResponseDto.cs
├── SyncDeleteRequestDto.cs
├── SyncDeleteResponseDto.cs
└── SyncDiffDto.cs
```

#### 3.2 核心 DTO

```csharp
public class SyncDiffDto
{
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public DiffType Type { get; set; }
    public string? EntityName { get; set; }
    public string? LocalChecksum { get; set; }
    public string? ServerChecksum { get; set; }
    public DateTime? LocalChangedAt { get; set; }
    public DateTime? ServerChangedAt { get; set; }
}

public enum DiffType
{
    LocalOnly,    // 仅本地有
    ServerOnly,   // 仅服务器有
    Modified,     // 双方都有但不同
    Identical     // 相同
}
```

### Phase 4: 客户端同步模块

#### 4.1 LocalData 扩展

**文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Context/LocalDbContext.cs`

**变更**: 添加 `SyncLogs` DbSet

```csharp
public DbSet<SyncLog> SyncLogs { get; set; }
```

#### 4.2 SyncLog 实体

```csharp
public class SyncLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string EntityType { get; set; }

    [Required]
    public string EntityId { get; set; }  // GUID as string

    [Required]
    public string Operation { get; set; }  // Create/Update/Delete

    public string? ChangedFields { get; set; }  // JSON

    [Required]
    public string LocalChecksum { get; set; }

    [Required]
    public DateTime LocalChangedAt { get; set; }

    [Required]
    public string SyncStatus { get; set; } = "Pending";

    public DateTime? SyncedAt { get; set; }

    public string? ErrorMessage { get; set; }
}
```

#### 4.3 服务层

**Contracts 层**:
```
src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/
├── ISyncService.cs
└── ISyncApiClient.cs
```

**Infrastructure 层**:
```
src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/
├── SyncService.cs
├── SyncApiClient.cs
└── ChecksumHelper.cs
```

### Phase 5: 同步 UI

#### 5.1 模块结构

```
src/Client/Desktop/Modules/LYBT.Desktop.Sync/
├── LYBT.Desktop.Sync.csproj
├── SyncModule.cs
├── ViewModels/
│   ├── SyncViewModel.cs
│   └── ConflictResolutionViewModel.cs
└── Views/
    ├── SyncView.xaml
    └── ConflictResolutionDialog.xaml
```

#### 5.2 导航入口

在 Shell 或设置模块添加"数据同步"菜单项

## 变更清单

### 新增文件

| 文件路径 | 说明 |
|----------|------|
| `src/Server/Modules/LYBT.Module.Sync/` | 服务器同步模块（整个目录） |
| `src/Shared/LYBT.Shared.Models/Contracts/Sync/` | 同步 DTO（整个目录） |
| `src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientReferenceCheckDto.cs` | 患者引用检查 DTO |
| `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Entities/SyncLog.cs` | 同步日志实体 |
| `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISyncService.cs` | 同步服务接口 |
| `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISyncApiClient.cs` | API 客户端接口 |
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SyncService.cs` | 同步服务实现 |
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SyncApiClient.cs` | API 客户端实现 |
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Helpers/ChecksumHelper.cs` | Checksum 工具 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Sync/` | 同步 UI 模块（整个目录） |

### 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | 完善 CheckReferenceAsync 实现 |
| `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` | 新增 CheckReferenceAsync |
| `src/Server/Modules/LYBT.Module.Patients/Interfaces/IPatientService.cs` | 新增接口方法 |
| `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Context/LocalDbContext.cs` | 添加 SyncLogs DbSet |
| `src/Server/Core/LYBT.Infrastructure/Data/ApplicationDbContext.cs` | 添加 SyncMetadata DbSet |
| `LYBT.Server.sln` | 添加 LYBT.Module.Sync 项目引用 |
| `LYBT.Desktop.sln` | 添加 LYBT.Desktop.Sync 项目引用 |

## 依赖关系

### 模块依赖

```
Phase 1 (引用检查) ──────────────────┐
                                     │
Phase 3 (共享 DTO) ──────────────────┼──> Phase 2 (Server) ──> Phase 4 (Client) ──> Phase 5 (UI)
                                     │
                                     └──> Phase 6 (测试)
```

### 变更顺序

1. **Phase 1** 可独立完成，不依赖其他 Phase
2. **Phase 3** (共享 DTO) 必须先于 Phase 2 和 Phase 4
3. **Phase 2** (Server) 必须先于 Phase 4 (Client)
4. **Phase 4** (Client) 必须先于 Phase 5 (UI)

## 测试策略

### 单元测试

- `ChecksumHelper.Compute` - 验证相同数据产生相同 Checksum
- `SyncService.CheckDiffAsync` - 验证差异检测逻辑
- `HerbService.CheckReferenceAsync` - 验证引用检查正确性

### 集成测试

- `SyncController` API 端点测试
- 完整同步流程测试（上传/下载/冲突）

### E2E 测试

- 模拟离线修改后在线同步
- 模拟冲突场景及解决

## 风险缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| Checksum 算法不一致 | 低 | 高 | 共享 ChecksumHelper 实现 |
| 网络中断 | 中 | 中 | 单条记录独立处理，支持重试 |
| 并发冲突 | 低 | 中 | 乐观并发 + 用户决定 |

## 回滚计划

如果变更失败:
1. 回滚数据库迁移（Server: SyncMetadata, Client: SyncLog）
2. 移除新增的模块项目
3. 恢复修改的服务文件

---

**设计者**: Claude Code
**日期**: 2026-02-04
**状态**: 已完成 (2026-02-04)
