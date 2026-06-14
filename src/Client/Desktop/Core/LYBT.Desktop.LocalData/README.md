# LYBT.Desktop.LocalData

> 本地 SQL Server LocalDB 数据层，离线模式核心基础设施

## 项目定位

- **层级**: Desktop Core (基础设施层)
- **职责**: 提供基于 SQL Server LocalDB + EF Core 的本地数据存储，支持离线模式下的完整数据操作，包括本地认证、数据同步、种子数据初始化
- **状态**: Active

## 目录结构

```
LYBT.Desktop.LocalData/
├── Context/               # LocalDbContext (LocalDB DbContext)
├── Helpers/               # 工具类 (ChecksumHelper)
├── Initialization/        # 数据库初始化与种子数据
├── Mappers/               # Mapperly 实体-DTO 映射器
└── Services/              # 本地认证与同步服务
```

## 核心组件

| 名称 | 说明 |
|------|------|
| LocalDbContext | SQL Server LocalDB DbContext，管理 10 个 DbSet，处理软删除过滤 |
| LocalAuthService | 本地 BCrypt 密码认证，支持登录失败锁定 (5次/15分钟) |
| SyncService | 本地-服务器数据同步协调，基于 Checksum 的增量同步 |
| DatabaseInitializer | LocalDB 数据库初始化，确保数据库创建与 Schema 同步 |
| SeedData | 种子数据填充，提供初始基础数据 |
| ChecksumHelper | 数据校验和计算，用于同步时检测数据变更 |
| Local*Mapper (x5) | 基于 Mapperly 的编译时映射器，Entity 与 DTO 之间转换 |

## 设计依据

本项目是双模式架构 (SYNC-D02) 的本地模式实现。远程模式通过 API 访问 SQL Server，本地模式通过 SQL Server LocalDB 实现离线数据操作。两种模式共享 Service/Repository 层，数据访问统一通过 Repository 模式 + SwitchingApiClient 双模式 API 路由。

## 依赖关系

### 依赖
- Microsoft.EntityFrameworkCore.SqlServer - SQL Server LocalDB 数据库引擎
- BCrypt.Net-Next - 密码哈希验证
- Riok.Mapperly - 编译时对象映射
- LYBT.Entities - 领域实体定义
- LYBT.Shared.Models - 共享 DTO 模型
- LYBT.Shared.Validators - 共享验证规则
- LYBT.Shared.Configuration - 同步选项等配置
- LYBT.Desktop.Contracts - 服务接口契约 (ISyncService, ILocalAuthService)

### 被依赖
- LYBT.Desktop.Shell - 主程序组合根
- LYBT.Tests.Desktop.Unit - 单元测试
- LYBT.Tests.Desktop.Integration - 集成测试

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始 README 创建 |

## 开发笔记

# LYBT.Desktop.LocalData 代码知识

本地数据层 - SQL Server LocalDB EF Core 实现，提供离线模式的数据存取、同步和本地认证服务。

## 代码文件结构

```
LYBT.Desktop.LocalData/
├── Context/
│   └── LocalDbContext.cs           # SQL Server LocalDB 数据库上下文
├── Helpers/
│   └── ChecksumHelper.cs          # SHA256 校验和计算 (同步用)
├── Initialization/
│   ├── DatabaseInitializer.cs      # LocalDB 数据库初始化
│   └── SeedData.cs                 # 默认管理员种子数据
├── Mappers/
│   ├── LocalFormulaMapper.cs       # 验方 Entity <-> DTO 映射
│   ├── LocalHerbMapper.cs          # 药材 Entity <-> DTO 映射
│   ├── LocalMedicalCaseMapper.cs   # 医案 Entity <-> DTO 映射
│   ├── LocalPatientMapper.cs       # 患者 Entity <-> DTO 映射
│   └── LocalUserMapper.cs          # 用户 Entity <-> DTO 映射
└── Services/
    ├── LocalAuthService.cs         # 本地认证 (BCrypt 密码验证)
    ├── LocalDbBackupService.cs     # 数据库备份恢复服务
    └── SyncService.cs              # 本地-服务器数据同步服务
```

### Context/LocalDbContext.cs
**LocalDbContext** : DbContext | SQL Server LocalDB 数据库上下文，10 个 DbSet，自动审计字段

| 方法 | 说明 |
|------|------|
| OnModelCreating(ModelBuilder) | 配置软删除过滤器、实体关系 |
| ApplySoftDeleteFilter(ModelBuilder) | 遍历 ISoftDeletable 实体应用全局查询过滤器 |
| ConfigureRelationships(ModelBuilder) | MedicalCase 聚合根关系: 1:1 Consultation(共享主键), 1:0..1 Prescription, 1:N PrintLog |
| SaveChangesAsync(CancellationToken) | 重写保存，自动设置 CreatedAt/UpdatedAt/CreatedBy/UpdatedBy |
| SaveChanges() | 同步版自动审计 |

### Data Access 层说明

原 `DataSources/` 目录下的 `LocalUserDataSource`、`LocalPatientDataSource`、`LocalHerbDataSource`、`LocalFormulaDataSource`、`LocalMedicalCaseDataSource` 类已移除。数据访问统一由 **Repository 模式** 处理：

- **Repository 接口**: 定义在 `LYBT.Desktop.Contracts/Repositories/` (IUserRepository, IPatientRepository, IHerbRepository, IFormulaRepository, IMedicalCaseRepository, IRegistrationRepository)
- **Repository 实现**: `LYBT.Desktop.Infrastructure/Repositories/RepositoryBase.cs` 泛型基类，通过 Refit API 客户端访问数据
- **双模式路由**: `SwitchingApiClient` 根据连接 URL 自动路由到远程服务器 API 或本地嵌入 LocalWebAPI
- **本地模式数据存取**: 本地嵌入的 LocalWebAPI 直接操作 `LocalDbContext`，对 Repository 层完全透明

### Helpers/ChecksumHelper.cs
**ChecksumHelper** (static) | SHA256 校验和计算，用于同步差异比对

| 方法 | 说明 |
|------|------|
| ComputeHerbChecksum(Herb) | 计算药材 Checksum (排除审计字段) |
| ComputePatientChecksum(Patient) | 计算患者 Checksum |
| ComputeFormulaChecksum(Formula) | 计算验方 Checksum (含排序后的药材子项) |
| ComputeChecksum(object, string) | 按实体类型分发计算 |
| ComputeHash(object) | 序列化为 JSON 后计算 SHA256 |

### Initialization/DatabaseInitializer.cs
**DatabaseInitializer** | SQL Server LocalDB 数据库初始化器

| 方法 | 说明 |
|------|------|
| InitializeAsync(CancellationToken) | 创建目录、EnsureCreated、种子数据 |
| DatabaseExists() | 检查数据库文件是否存在 (static) |
| GetConnectionString() | 获取连接字符串 (static) |
| DatabasePath | AppData/LYBTZYZS/lybtzyzs.db (static) |

### Initialization/SeedData.cs
**SeedData** (static) | 种子数据，创建默认管理员

| 方法 | 说明 |
|------|------|
| SeedAsync(LocalDbContext, ILogger, CancellationToken) | 初始化种子数据 |
| SeedAdminUserAsync(LocalDbContext, ILogger, CancellationToken) | 创建 admin/Admin@123 SuperAdmin 账户 |

### Mappers/LocalFormulaMapper.cs
**LocalFormulaMapper** (internal, partial) : Riok.Mapperly [Mapper] | 验方 Entity <-> DTO

| 方法 | 说明 |
|------|------|
| ToDetailDto(Formula) | Formula -> FormulaDetailDto (含计算属性 HerbCount) |
| ToEntity(FormulaInputDto) | FormulaInputDto -> Formula |
| ToDto(FormulaHerbItem) | FormulaHerbItem -> FormulaHerbItemDto |
| ToEntity(FormulaHerbItemInputDto) | FormulaHerbItemInputDto -> FormulaHerbItem |

### Mappers/LocalHerbMapper.cs
**LocalHerbMapper** (internal, partial) : Riok.Mapperly [Mapper] | 药材 Entity <-> DTO

| 方法 | 说明 |
|------|------|
| ToDetailDto(Herb) | Herb -> HerbDetailDto |
| ToEntity(HerbInputDto) | HerbInputDto -> Herb |

### Mappers/LocalPatientMapper.cs
**LocalPatientMapper** (internal, partial) : Riok.Mapperly [Mapper] | 患者 Entity <-> DTO

| 方法 | 说明 |
|------|------|
| ToDetailDto(Patient) | Patient -> PatientDetailDto |
| ToEntity(PatientInputDto) | PatientInputDto -> Patient |

### Mappers/LocalUserMapper.cs
**LocalUserMapper** (internal, partial) : Riok.Mapperly [Mapper] | 用户 Entity <-> DTO

| 方法 | 说明 |
|------|------|
| ToDetailDto(User) | User -> UserDetailDto (排除 PasswordHash) |
| ToEntity(UserInputDto) | UserInputDto -> User (排除 Password/ConfirmPassword) |

### Mappers/LocalMedicalCaseMapper.cs
**LocalMedicalCaseMapper** (internal, partial) : Riok.Mapperly [Mapper] | 医案聚合根映射

| 方法 | 说明 |
|------|------|
| ToDetailDto(MedicalCase) | MedicalCase -> MedicalCaseDetailDto (含 Consultation/Prescription 嵌套) |
| ToConsultationDetailDto(Consultation, MedicalCase) | Consultation -> ConsultationDetailDto (需要父实体补充 PatientId 等) |
| ToPrescriptionDetailDto(Prescription) | Prescription -> PrescriptionDetailDto |
| ToPrescriptionItemDto(PrescriptionItem) | PrescriptionItem -> PrescriptionItemDto |
| ToEntity(MedicalCaseInputDto) | MedicalCaseInputDto -> MedicalCase |

### Services/LocalAuthService.cs
**LocalAuthService** : ILocalAuthService | 本地认证服务

| 方法 | 说明 |
|------|------|
| ValidateAsync(string, string, CancellationToken) | 验证用户名密码，含账户锁定机制 (5次失败锁定15分钟) |
| ChangePasswordAsync(Guid, string, string, CancellationToken) | 修改密码，验证旧密码 |

### Services/SyncService.cs
**SyncService** : ISyncService | 数据同步服务，协调本地与服务器之间的同步

| 方法 | 说明 |
|------|------|
| GetSupportedEntityTypesAsync(CancellationToken) | 获取支持的实体类型 (Herb/Patient/Formula) |
| CheckDifferencesAsync(string, CancellationToken) | Checksum 比对，生成 LocalOnly/ServerOnly/Conflicts 三类差异 |
| UploadAsync(string, List\<Guid\>, CancellationToken) | 上传本地实体到服务器 |
| DownloadAsync(string, List\<Guid\>, CancellationToken) | 从服务器下载实体到本地 |
| DeleteAsync(string, List\<Guid\>, CancellationToken) | 请求服务器删除实体 |
| ExecuteSyncAsync(string, SyncResolution, CancellationToken) | 执行完整同步流程 (上传+下载+冲突解决) |

## 死代码与废弃标记

- Mappers (5个) 均为 `internal` 访问级别，仅在 LocalData 项目内部被对应的 Service 使用，无外部引用 -- 属于正常设计 (internal 封装)
- `SeedData` 仅被 `DatabaseInitializer` 调用，测试项目通过 `DatabaseInitializer` 间接使用 -- 非死代码
- Repository 注册在 `ServiceCollectionExtensions.cs` 中完成 -- 非死代码

## 设计分析

1. **Repository + 双模式 API 模式**: 数据访问统一通过 Repository 接口 (Contracts/Repositories/)，由 `SwitchingApiClient` 根据连接 URL 路由到远程服务器 API 或本地嵌入 LocalWebAPI，Repository 层对模式切换完全透明
2. **Mapperly 编译期映射**: 使用 Riok.Mapperly 源生成器替代运行时反射映射，5 个 Mapper 均为 `internal partial class`，编译器自动生成映射实现
3. **聚合根模式**: MedicalCase 管理 Consultation + Prescription 三实体的完整生命周期，通过 IMedicalCaseRepository 聚合保存
4. **Checksum 同步**: ChecksumHelper 计算实体业务字段的 SHA256 哈希 (排除审计字段)，必须与服务器端 `LYBT.Module.Sync.Services.ChecksumHelper` 保持完全一致
5. **角色保护逻辑**: 用户管理实现 SuperAdmin 不可删除/不可禁用、最后管理员保护等业务规则

## 已知陷阱

- ChecksumHelper 的 JSON 序列化选项 (camelCase, WhenWritingNull) 必须与服务器端完全一致，否则相同数据会产生不同的 Checksum 导致同步误判
- SeedData 中硬编码了默认管理员密码 "Admin@123"，仅用于首次初始化，不应在生产环境保留

---
最后更新: 2026-03-01
