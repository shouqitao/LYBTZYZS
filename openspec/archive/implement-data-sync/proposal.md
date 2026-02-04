# implement-data-sync

## Why

### 业务背景

LYBTZYZS Desktop 客户端已实现本地模式（implement-local-mode），支持离线数据存储。但目前本地数据与服务器数据是隔离的，用户无法：

1. 将本地新增的药材/患者/经验方同步到服务器
2. 从服务器下载最新数据到本地
3. 在多设备间共享基础数据

### 发现的问题

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| Desktop LocalData | 功能缺失 | 本地数据孤立 | 支持双向同步 |
| HerbService.CheckReferenceAsync | TODO未实现 | 框架存在，逻辑空 | 完整引用检查 |
| PatientService | 功能缺失 | 无引用检查 | 删除前验证医案引用 |
| Server | 模块缺失 | 无同步API | 提供同步端点 |

### 影响分析

- **变更类型**: Feature（新增功能）
- **变更范围**: 跨层变更（Desktop + Server + Shared）
- **影响模块**: Herbs, Patients, Formula, 新增 Sync 模块
- **风险等级**: Medium（影响边缘功能，核心业务不受影响）

## What Changes

### Phase 1: 引用检查实现

完善删除前的引用检查逻辑，确保数据完整性。

**变更内容**:
- 实现 `HerbService.CheckReferenceAsync` - 查询 PrescriptionItem.HerbId
- 新增 `PatientService.CheckReferenceAsync` - 查询 MedicalCase.PatientId
- 更新删除逻辑：有引用时拒绝删除，提示用户禁用

**注意**: Formula 不需要引用检查，`Prescription.ReferencedFormulas` 是文本描述非外键。

### Phase 2: 服务器端同步模块

创建 `LYBT.Module.Sync` 模块，提供同步 API。

**变更内容**:
- SyncMetadata 实体（记录实体 Checksum 和修改时间）
- ChecksumHelper（SHA256 计算，排除审计字段）
- SyncController API 端点：
  - `GET /api/v1/sync/metadata` - 获取实体元数据
  - `POST /api/v1/sync/compare` - 比对差异
  - `POST /api/v1/sync/upload` - 上传本地变更
  - `POST /api/v1/sync/download` - 下载服务器变更
  - `POST /api/v1/sync/delete` - 同步删除（含引用检查）

### Phase 3: 客户端同步模块

扩展 LocalData 和 Infrastructure，实现同步逻辑。

**变更内容**:
- SyncLog 实体（SQLite）
- ISyncService / ISyncApiClient 接口
- SyncService 实现（差异检测、冲突处理）
- ChecksumHelper（客户端版）

### Phase 4: 同步 UI

创建 `LYBT.Desktop.Sync` 模块，提供用户界面。

**变更内容**:
- SyncViewModel（状态管理、用户交互）
- SyncView.xaml（同步主界面）
- ConflictResolutionDialog.xaml（冲突处理弹窗）

### Phase 5: 测试与验证

**变更内容**:
- 单元测试（Checksum、差异检测）
- 集成测试（API 端点）
- 端到端测试

## Architecture

### 数据流架构

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
│ ISyncApiClient  │ │ SyncLog   │ │ IDataSource     │
│ (远程 API 调用) │ │ Repository│ │ (本地数据访问)  │
└────────┬────────┘ └───────────┘ └─────────────────┘
         │
         │ HTTP
         ▼
┌─────────────────────────────────────────────────────────────┐
│                   LYBT.Module.Sync                           │
│              SyncController / SyncService                    │
└─────────────────────────────────────────────────────────────┘
```

### 引用关系

```
Patient ←───── MedicalCase ←───── Prescription ←───── PrescriptionItem ─────→ Herb
                                        │
                                        └─ ReferencedFormulas (文本描述，非外键)
                                                    │
                                              Formula (模板)
```

### 变更影响范围

```
src/Server/
├── Modules/
│   ├── LYBT.Module.Sync/           (新增)
│   ├── LYBT.Module.Herbs/          (修改: CheckReferenceAsync)
│   └── LYBT.Module.Patients/       (修改: 新增 CheckReferenceAsync)

src/Client/Desktop/
├── Core/
│   ├── LYBT.Desktop.LocalData/     (修改: 新增 SyncLog)
│   ├── LYBT.Desktop.Contracts/     (修改: 新增接口)
│   └── LYBT.Desktop.Infrastructure/(修改: 新增服务)
└── Modules/
    └── LYBT.Desktop.Sync/          (新增)

src/Shared/
└── LYBT.Shared.Models/             (修改: 新增 Sync DTOs)
```

## Design Decisions

| 决策点 | 选择 | 原因 |
|--------|------|------|
| 数据量级 | 小数据量（<1000条） | 全量 Checksum 比对，简单可靠 |
| 同步触发 | 纯手动 | 完全可控，用户明确知道何时同步 |
| 删除策略 | 软删除 + 引用检查 | 有引用数据只能禁用，无引用可删除 |
| Checksum 范围 | 业务字段 + Status | 排除审计字段避免"假差异" |
| 冲突处理 | 批量选择 + 预览对比 | 用户决定，直观高效 |
| 同步粒度 | 实体级 | 简单实现，字段级太复杂 |

## Impact

- **新增文件**: ~20个
- **修改文件**: ~10个
- **风险等级**: Medium
- **测试要求**: 单元测试 + 集成测试 + E2E测试

## Risks

| 风险 | 缓解措施 |
|------|----------|
| 网络中断 | 同步操作支持重试，每条记录独立处理 |
| 数据丢失 | 软删除机制，同步前本地备份 |
| 冲突过多 | 清晰的冲突展示UI，批量处理选项 |
| 引用检查遗漏 | 服务器端再次验证，拒绝非法删除 |

## Dependencies

- implement-local-mode (已完成)
- LocalData 项目和 DataSource 架构
- LocalDbContext 可扩展

## References

- 详细设计文档: [findings.md](../../../findings.md)
- 任务计划: [task_plan.md](../../../task_plan.md)
- Offline-First Architecture: https://developer.android.com/topic/architecture/data-layer/offline-first
- Conflict Resolution Strategies: https://mobterest.medium.com/conflict-resolution-strategies-in-data-synchronization

---
*Created: 2026-02-04*
*Status: Draft*
