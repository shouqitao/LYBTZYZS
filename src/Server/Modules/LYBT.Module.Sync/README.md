# LYBT.Module.Sync

> Server 端基础数据双向同步 | Checksum 比对 | Herb/Patient/Formula

## 项目定位

- **层级**: Server Modules
- **职责**: 处理 Desktop 客户端与 Server 之间的基础数据同步，通过 SHA256 Checksum 比对差异，支持上传、下载、删除三种同步操作
- **状态**: Active

## 目录结构

```
LYBT.Module.Sync/
├── SyncModule.cs
├── Interfaces/
│   └── ISyncService.cs
└── Services/
    ├── SyncService.cs
    └── ChecksumHelper.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| ISyncService | 6 | 元数据获取/差异比对/上传/下载/删除/支持类型查询 |

### ISyncService 方法列表

| 方法 | 说明 |
|------|------|
| GetSupportedEntityTypes | 返回支持的实体类型 (Herb/Patient/Formula) |
| GetMetadataAsync | 获取指定实体的元数据列表，用于客户端 Checksum 比对 |
| CompareAsync | 将客户端本地元数据与服务器比对，返回差异清单 |
| UploadAsync | 客户端上传数据到服务器，支持冲突覆盖模式 |
| DownloadAsync | 按 ID 列表从服务器下载实体数据 |
| DeleteAsync | 同步删除操作，Herb/Patient 有引用检查，Formula 无引用检查 |

## 设计依据

### Checksum 机制

- 使用 SHA256 对实体业务字段计算哈希，排除审计字段 (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, RowVersion)
- Formula 的 Herbs 子集合按 HerbId 排序后再计算，保证顺序一致性
- 元数据查询使用 `IgnoreQueryFilters()` 确保软删除记录也参与比对

### 差异类型

| DiffType | 说明 |
|----------|------|
| LocalOnly | 仅客户端有，需上传 |
| ServerOnly | 仅服务器有，需下载 |
| Modified | 双方都有但 Checksum 不同 |

### 删除引用检查

- Herb: 检查是否被处方引用 (通过 IHerbCrossModuleService)
- Patient: 检查是否有医案记录 (通过 IPatientCrossModuleService)
- Formula: 无引用检查，直接软删除

## 依赖关系

### 依赖

- LYBT.Infrastructure (AppDbContext, IHerbCrossModuleService, IPatientCrossModuleService)
- LYBT.Entities (Herb, Patient, Formula)
- LYBT.Shared.Models (SyncMetadataDto, SyncCompareInputDto 等 Contracts)

### 被依赖

- LYBT.WebAPI (SyncController)

## API 端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/v1/sync/entity-types | GET | 获取支持的实体类型列表 |
| /api/v1/sync/metadata | GET | 获取指定实体类型的元数据 |
| /api/v1/sync/compare | POST | 比对本地与服务器的差异 |
| /api/v1/sync/upload | POST | 上传本地数据到服务器 |
| /api/v1/sync/download | POST | 从服务器下载数据 |
| /api/v1/sync/delete | POST | 同步删除（带引用检查） |

授权策略: DoctorOrAdmin

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 按 README 规范创建文档 |
