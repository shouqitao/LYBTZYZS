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

## 开发笔记

# LYBT.Module.Sync 代码知识

数据同步模块 - 处理 Herb/Patient/Formula 三种实体在 Server 与 Desktop 之间的双向同步，基于 Checksum 比对机制。

## 代码文件结构

```
LYBT.Module.Sync/
├── SyncModule.cs                             # 模块DI注册
├── Interfaces/
│   └── ISyncService.cs                       # 同步服务接口
└── Services/
    ├── SyncService.cs                        # 同步服务实现
    └── ChecksumHelper.cs                     # SHA256校验和计算辅助类
```

### SyncModule.cs
**SyncModule** (static) | 模块DI注册入口

| 方法 | 说明 |
|------|------|
| AddSyncModule(IServiceCollection, IConfiguration) | 注册 ISyncService -> SyncService (Scoped) |
| UseSyncModule(IApplicationBuilder) | 中间件配置 (当前空实现) |

### Interfaces/ISyncService.cs
**ISyncService** | 同步服务接口

| 方法 | 说明 |
|------|------|
| GetMetadataAsync(string entityType) | 获取指定实体类型的全部元数据 (ID, Checksum, LastModifiedAt) |
| CompareAsync(SyncCompareInputDto input) | 比对本地与服务器的差异 (LocalOnly/ServerOnly/Modified) |
| UploadAsync(SyncUploadInputDto input) | 上传本地数据到服务器 (支持冲突覆盖) |
| DownloadAsync(SyncDownloadInputDto input) | 从服务器下载指定实体数据 |
| DeleteAsync(SyncDeleteInputDto input) | 同步删除操作 (带引用检查) |
| GetSupportedEntityTypes() | 返回支持的实体类型列表: Herb, Patient, Formula |

### Services/ChecksumHelper.cs
**ChecksumHelper** (static) | SHA256 校验和计算

| 方法 | 说明 |
|------|------|
| ComputeHerbChecksum(Herb) | 计算Herb实体校验和 (排除审计字段) |
| ComputePatientChecksum(Patient) | 计算Patient实体校验和 (排除审计字段) |
| ComputeFormulaChecksum(Formula) | 计算Formula实体校验和 (含Herbs子集合，按HerbId排序) |
| ComputeChecksum(object entity, string entityType) | 按实体类型分发计算校验和 |
| ComputeHash(object data) | (private) JSON序列化后计算SHA256哈希 |

校验和排除字段: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, RowVersion
校验和包含字段: 所有业务字段 + Id + Status + IsDeleted

### Services/SyncService.cs
**SyncService** : ISyncService | 同步服务实现

依赖注入: AppDbContext, IHerbCrossModuleService, IPatientCrossModuleService, ILogger

支持实体类型: `Herb`, `Patient`, `Formula`

| 公开方法 | 说明 |
|----------|------|
| GetSupportedEntityTypes() | 返回 ["Herb", "Patient", "Formula"] |
| GetMetadataAsync(string entityType) | 使用 IgnoreQueryFilters 查询全部记录 (含软删除) 计算Checksum |
| CompareAsync(SyncCompareInputDto) | Checksum 比对: 构建ServerDict/LocalDict，生成LocalOnly/ServerOnly/Modified差异 |
| UploadAsync(SyncUploadInputDto) | 逐项上传: 反序列化JSON -> 存在则覆盖或报冲突 -> 不存在则新增 |
| DownloadAsync(SyncDownloadInputDto) | 按ID列表下载: 序列化实体为JsonElement返回 |
| DeleteAsync(SyncDeleteInputDto) | 带引用检查的软删除: Herb检查处方引用, Patient检查医案引用, Formula无引用检查 |

| 私有方法 (元数据) | 说明 |
|-------------------|------|
| GetHerbMetadataAsync() | IgnoreQueryFilters + AsNoTracking 查询全部Herb |
| GetPatientMetadataAsync() | IgnoreQueryFilters + AsNoTracking 查询全部Patient |
| GetFormulaMetadataAsync() | IgnoreQueryFilters + Include(Herbs) + AsNoTracking 查询全部Formula |

| 私有方法 (上传) | 说明 |
|-----------------|------|
| UploadHerbAsync(JsonElement, bool overwrite) | 反序列化Herb, FindAsync查重, 覆盖或新增 |
| UploadPatientAsync(JsonElement, bool overwrite) | 反序列化Patient, FindAsync查重, 覆盖或新增 |
| UploadFormulaAsync(JsonElement, bool overwrite) | 反序列化Formula, 包含Herbs子集合处理 (先删旧再加新) |

| 私有方法 (下载) | 说明 |
|-----------------|------|
| GetHerbJsonAsync(Guid id) | 序列化Herb为JsonElement |
| GetPatientJsonAsync(Guid id) | 序列化Patient为JsonElement |
| GetFormulaJsonAsync(Guid id) | 序列化Formula(含Herbs)为JsonElement |

| 私有方法 (删除检查) | 说明 |
|---------------------|------|
| CanDeleteHerbAsync(Guid) | 通过IHerbCrossModuleService检查处方引用 |
| CanDeletePatientAsync(Guid) | 通过IPatientCrossModuleService检查医案引用 |
| SoftDeleteEntityAsync(string entityType, Guid) | 执行软删除 (设置IsDeleted=true) |
| ValidateEntityType(string, out string?) | 验证实体类型是否在支持列表中 |

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| SyncModule.UseSyncModule(IApplicationBuilder) | [DEAD] 仅定义，无调用方 | 无 (空实现) | 可安全移除 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| SyncService | 直接操作AppDbContext而非通过Repository | 同步模块需要跨多个实体类型操作，使用DbContext更直接 | 可接受，同步属于基础设施层操作 |
| SyncService.GetMetadataAsync | 全量加载所有记录到内存计算Checksum | 数据量大时可能有内存压力 | 考虑分批处理或数据库端计算 |
| SyncService.UploadAsync | 使用FindAsync查重 | FindAsync受全局查询过滤器影响，已软删除的实体不会被找到 | 上传时如目标已软删除，会创建新记录而非恢复 |
| ChecksumHelper | Server端和Desktop端各有独立实现 | Desktop端: LYBT.Desktop.LocalData/Helpers/ChecksumHelper.cs | 两端实现需保持一致，否则同步比对会误判 |
| SyncService.UploadFormulaAsync | Formula上传时先删旧Herbs再加新 | 使用RemoveRange + Add方式全量替换子集合 | 与FormulaService.UpdateAsync的Herbs处理模式一致 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 元数据查询使用 IgnoreQueryFilters | 同步需要比对包括已软删除的记录 | 这是正确设计，确保删除操作也能同步 |
| FindAsync 在上传时可能遗漏已软删除记录 | EF Core 8 的 FindAsync 受全局查询过滤器影响 | 当前设计下已软删除记录会作为新记录插入，可能导致主键冲突 |
| ChecksumHelper 双端一致性 | Server 和 Desktop 各自实现 ChecksumHelper，字段选择和序列化配置必须完全一致 | 修改任一端的 Checksum 计算逻辑时，必须同步修改另一端 |
| Formula 的 Checksum 包含 Herbs 子集合 | FormulaChecksum 对 Herbs 按 HerbId + HerbName 排序后计算 | 确保两端排序逻辑一致，否则 Checksum 不匹配 |
| UploadAsync 统一 SaveChanges | 所有上传项处理完后统一调用 SaveChangesAsync | 部分失败时已成功的项也会被保存 |
| DeleteAsync 中 Formula 无引用检查 | 设计决策: Formula 不被其他实体直接引用 | 如果未来添加引用关系，需更新删除检查逻辑 |
