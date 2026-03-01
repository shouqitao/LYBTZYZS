# LYBT.Infrastructure 代码知识

## 模块概述

Server 端基础设施层 -- 提供 EF Core DbContext、Repository 基类、跨模块服务、缓存失效、BaseService 权限验证、BaseApiController 等核心基础能力。所有 Server 端模块依赖此项目。

### 目录结构

```
LYBT.Infrastructure/
├── Data/
│   ├── AppDbContext.cs                     # 统一数据库上下文（审计字段自动化）
│   ├── AppDbContextFactory.cs              # 设计时 DbContext 工厂
│   ├── DatabaseInitializationService.cs    # 数据库初始化
│   ├── Configurations/                     # EF Core 实体配置
│   │   ├── Base/
│   │   │   └── BaseEntityConfiguration.cs  # 基类配置（主键/审计/并发/软删除）
│   │   ├── MedicalCaseConfiguration.cs
│   │   ├── ConsultationConfiguration.cs
│   │   ├── PrescriptionConfiguration.cs
│   │   ├── PatientConfiguration.cs
│   │   ├── UserConfiguration.cs
│   │   ├── HerbConfiguration.cs
│   │   ├── FormulaConfiguration.cs
│   │   └── ...
│   └── Migrations/                         # EF Core 迁移（数十个）
├── Repositories/
│   └── BaseRepository.cs                   # 泛型仓储基类（CRUD + 分页 + 软删除）
├── Interfaces/
│   └── IRepository.cs                      # 仓储接口（11个标准方法）
├── Services/
│   ├── BaseService.cs                      # Service 基类（权限验证 + 统一错误处理）
│   └── CrossModule/                        # 跨模块服务（ISP 拆分）
│       ├── IPatientCrossModuleService.cs
│       ├── IUserCrossModuleService.cs
│       ├── IHerbCrossModuleService.cs
│       ├── ICrossModuleAuthService.cs
│       └── ReferenceCheckResult.cs
├── Caching/
│   ├── ICacheInvalidationService.cs        # 缓存失效接口
│   └── CacheInvalidationService.cs         # OutputCache Tag + MemoryCache 前缀清理
├── Web/
│   ├── BaseApiController.cs                # API 控制器基类
│   └── ApiErrorCodes.cs                    # API 错误码
├── Logging/
│   ├── LogCleanupService.cs                # 日志清理后台服务
│   └── HttpContextCorrelationIdProvider.cs # 请求关联ID
└── Configuration/
    ├── Services/DefaultPasswordService.cs  # 默认密码服务
    └── Validation/ProductionConfigurationValidator.cs
```

## 架构决策

| 决策 | 原因 | 日期 | 关联 OpenSpec |
|------|------|------|--------------|
| 单一 AppDbContext 统一所有实体 | 简化架构，单数据库 LYBTDB | 初始设计 | - |
| BaseEntityConfiguration 统一审计/并发/软删除配置 | DRY 原则，所有实体继承统一配置 | - | - |
| BaseRepository 模板方法模式 | 子类通过覆盖 ApplyKeywordFilter/ApplyDefaultOrdering 自定义逻辑 | Issue #2103 | - |
| SaveChangesAsync 全局 RowVersion 同步 | 防止同一请求内多次操作导致不必要的并发异常 | Issue #2250 | - |
| CrossModuleService 实现 4 个 ISP 接口 | 替代原 ICrossModuleService 单一大接口，接口隔离 | D5-1 | - |
| CacheInvalidationService 聚合双缓存失效 | 同时清理 OutputCache Tag 和 MemoryCache 前缀 | - | - |
| BaseService 移除 IMapper 依赖 | 各 Service 注入具体 Mapper（Mapperly） | - | adopt-mapperly-unified-mapping |
| 迁移文件从 Migrations/ 迁移到 Data/Migrations/ | 目录结构规范化 | 2025-12-29 | - |

## 核心组件详解

### AppDbContext

- **审计自动化**: 重写 SaveChangesAsync/SaveChanges，自动填充 CreatedAt/UpdatedAt/CreatedBy/UpdatedBy
- **用户上下文**: 通过 IHttpContextAccessor 获取当前用户 ID (ClaimTypes.NameIdentifier)
- **实体配置**: 使用 ApplyConfigurationsFromAssembly 自动发现所有 IEntityTypeConfiguration
- **查询优化**: modelBuilder.ApplyOptimizations() 配置索引和全局过滤器

### BaseEntityConfiguration

```csharp
// 统一配置所有继承 BaseEntity 的实体:
- 主键: builder.HasKey(e => e.Id)
- 审计: CreatedAt (GETUTCDATE()), UpdatedAt, CreatedBy, UpdatedBy
- 并发: RowVersion (IsRowVersion + IsConcurrencyToken)
- 软删除: IsDeleted + HasQueryFilter(e => !e.IsDeleted)
```

### BaseRepository

- **11 个标准 CRUD 方法**: GetByIdAsync, GetAllAsync, FindAsync, GetPagedAsync, AddAsync, UpdateAsync, DeleteAsync (软删除), AddRangeAsync, UpdateRangeAsync, DeleteRangeAsync, RestoreAsync
- **高级查询**: GetQueryable, GetNoTrackingQueryable, FromSqlRawAsync, SelectAsync (投影)
- **分页助手**: GetPagedResultAsync (protected，供子类复用)
- **SaveChangesAsync**: 全局 RowVersion 同步后调用 _context.SaveChangesAsync()

### CrossModuleService

单一实现类同时实现 4 个 ISP 接口:
- **IPatientCrossModuleService**: 患者基本信息、存在性检查、引用计数
- **IUserCrossModuleService**: 用户基本信息、凭证查询、密码更新、登录状态
- **IHerbCrossModuleService**: 药材信息、引用检查、批量价格查询
- **ICrossModuleAuthService**: Token 撤销

### CacheInvalidationService

```csharp
// Tag 命名约定: "herbs", "formulas", "patients", "medicalcases"
// 双缓存清理:
//   1. OutputCache: EvictByTagAsync(tag)
//   2. MemoryCache: RemoveByPrefix(tag) -- 约定 key 以 tag 为前缀
```

### BaseService

- **非泛型版本**: 权限验证核心 (ValidateEditPermission/ValidateDeletePermission)
  - 管理员始终有权限
  - 非管理员: 必须本人创建 + 当天创建
- **泛型版本 BaseService<T>**: 统一错误处理 (ExecuteAsync) + FluentValidation 集成

## 代码文件结构

### Data/ -- 数据层核心

#### Data/AppDbContext.cs
- **类**: `AppDbContext` : `DbContext`
- **用途**: 统一应用数据库上下文，集成审计字段自动化
- **构造函数**: 2 个重载 (无 IHttpContextAccessor / 有 IHttpContextAccessor)
- **DbSet 属性**: Users, AuthSessions, RefreshTokens, AutoLoginTokens, SecurityAuditLogs, Patients, MedicalCases, MedicalCaseAuditLogs, Consultations, Prescriptions, PrescriptionItems, MedicalCasePrintLogs, Herbs, Formulas, SystemLogs
- **重写方法**:
  - `OnModelCreating()` -- ApplyOptimizations() + ApplyConfigurationsFromAssembly() 自动发现配置
  - `SaveChangesAsync()` / `SaveChanges()` -- 调用 SetAuditFields() 自动填充审计字段
- **私有方法**:
  - `SetAuditFields()` -- 遍历 ChangeTracker，Added 设 CreatedAt/UpdatedAt/CreatedBy，Modified 设 UpdatedAt/UpdatedBy
  - `GetCurrentUserId()` -- 从 IHttpContextAccessor 读取 ClaimTypes.NameIdentifier
- **被引用**: 全部 Repository、CrossModuleService、DatabaseInitializationService、LogCleanupService

#### Data/AppDbContextFactory.cs
- **类**: `AppDbContextFactory` : `IDesignTimeDbContextFactory<AppDbContext>`
- **用途**: EF Core 设计时工厂 (migrations 命令使用)
- **公共方法**: `CreateDbContext(string[] args)` -- 从 appsettings.json 读取 DefaultConnection，配置 SqlServer + MigrationsAssembly
- **仅在设计时使用**: 运行 `dotnet ef migrations` 时自动调用

#### Data/DatabaseInitializationService.cs
- **类**: `DatabaseInitializationService`
- **用途**: 数据库初始化 (迁移 + 系统管理员自动创建)
- **依赖**: AppDbContext, SystemAdminOptions, DefaultPasswordOptions
- **公共方法**:
  - `InitializeDatabaseAsync()` -- 检查 pending migrations，重试3次应用迁移；InMemory 数据库用 EnsureCreatedAsync；可选创建 SuperAdmin
  - `GetDatabaseInfoAsync()` -- 检查数据库连接状态
- **私有方法**: `EnsureSystemAdminExistsAsync()` -- 使用 IgnoreQueryFilters 检查是否已存在 SuperAdmin，不存在则创建

#### Data/Configuration/EntityOptimizationExtensions.cs
- **类**: `EntityOptimizationExtensions` (static)
- **用途**: ModelBuilder 扩展，配置全局查询过滤器和索引
- **公共方法**: `ApplyOptimizations(this ModelBuilder)` -- 调用 4 个私有优化方法
- **私有方法**:
  - `ApplyGlobalQueryFilters()` -- 反射遍历所有 BaseEntity 子类添加 !IsDeleted 过滤器；FormulaHerbItem 基于关联 Formula.IsDeleted
  - `OptimizePatientEntity()` -- 手机号索引 IX_Patient_Phone
  - `OptimizeUserEntity()` -- Email 唯一索引 IX_User_Email, 手机号索引 IX_User_Phone
- **已移除 (2026-03-01)**: `OptimizeMedicalCaseEntity()` / `OptimizePrescriptionEntity()` -- MVP阶段空实现，索引已在各 Configuration 类中定义
- **仅被 AppDbContext.OnModelCreating() 调用**: 无外部直接引用

### Data/Configurations/ -- EF Core 实体配置

#### Data/Configurations/Base/BaseEntityConfiguration.cs
- **类**: `BaseEntityConfiguration<T>` : `IEntityTypeConfiguration<T>` where T : BaseEntity (abstract)
- **用途**: 所有继承 BaseEntity 的实体的统一配置基类
- **配置内容**: 主键 (Id), 审计字段 (CreatedAt GETUTCDATE(), UpdatedAt, CreatedBy, UpdatedBy), 并发 (RowVersion), 软删除 (IsDeleted + QueryFilter)
- **被继承**: PatientConfiguration, UserConfiguration, HerbConfiguration, FormulaConfiguration, ConsultationConfiguration, PrescriptionConfiguration, MedicalCaseConfiguration, MedicalCasePrintLogConfiguration

#### Data/Configurations/MedicalCaseConfiguration.cs
- **类**: `MedicalCaseConfiguration` : `BaseEntityConfiguration<MedicalCase>`
- **配置内容**: NeedsPrescription nullable, CreatedBy 必填, 过滤唯一索引 UX_MedicalCases_Patient_ActiveOnly (CaseStatus=1 AND IsDeleted=0), UserId 索引, 打印字段默认值, PrintLogs 一对多关系 (Cascade)

#### Data/Configurations/ConsultationConfiguration.cs
- **类**: `ConsultationConfiguration` : `BaseEntityConfiguration<Consultation>`
- **配置内容**: CreatedBy 必填, 与 MedicalCase 1:1 共享主键关系 (HasForeignKey<Consultation>(c => c.Id), Cascade)

#### Data/Configurations/PrescriptionConfiguration.cs
- **类**: `PrescriptionConfiguration` : `BaseEntityConfiguration<Prescription>`
- **配置内容**: Discount 精度 (3,2), MedicalCaseId 唯一索引 UX_Prescriptions_MedicalCaseId, CreatedBy 必填, 与 MedicalCase 1:1 外键关系 (Cascade)

#### Data/Configurations/PatientConfiguration.cs
- **类**: `PatientConfiguration` : `BaseEntityConfiguration<Patient>`
- **配置内容**: Status 枚举转 int

#### Data/Configurations/UserConfiguration.cs
- **类**: `UserConfiguration` : `BaseEntityConfiguration<User>`
- **配置内容**: UserName 唯一索引, Status/Role 枚举转 int

#### Data/Configurations/HerbConfiguration.cs
- **类**: `HerbConfiguration` : `BaseEntityConfiguration<Herb>`
- **配置内容**: Price/CostPrice 精度 (18,2), Status 枚举转 int

#### Data/Configurations/FormulaConfiguration.cs
- **类**: `FormulaConfiguration` : `BaseEntityConfiguration<Formula>`
- **配置内容**: Status 枚举转 int, IsShared 默认 false, Herbs 一对多关系 (FormulaHerbItem, Cascade)

#### Data/Configurations/FormulaHerbItemConfiguration.cs
- **类**: `FormulaHerbItemConfiguration` : `IEntityTypeConfiguration<FormulaHerbItem>`
- **注意**: 不继承 BaseEntityConfiguration (FormulaHerbItem 不继承 BaseEntity)
- **配置内容**: HerbName 必填, Dosage 默认 1, Unit 默认 "g", 与 Herb 多对一关系 (Restrict)

#### Data/Configurations/PrescriptionItemConfiguration.cs
- **类**: `PrescriptionItemConfiguration` : `IEntityTypeConfiguration<PrescriptionItem>`
- **注意**: 不继承 BaseEntityConfiguration (PrescriptionItem 不继承 BaseEntity)
- **配置内容**: UnitPrice 精度 (18,2), 与 Prescription 多对一关系 (Cascade)

#### Data/Configurations/MedicalCasePrintLogConfiguration.cs
- **类**: `MedicalCasePrintLogConfiguration` : `BaseEntityConfiguration<MedicalCasePrintLog>`
- **配置内容**: PrintType 枚举转 int; FK 关系在 MedicalCaseConfiguration 中定义

#### Data/Configurations/MedicalCaseAuditLogConfiguration.cs
- **类**: `MedicalCaseAuditLogConfiguration` : `IEntityTypeConfiguration<MedicalCaseAuditLog>`
- **注意**: 不继承 BaseEntityConfiguration (MedicalCaseAuditLog 非 BaseEntity)
- **配置内容**: 必填字段, JSON 字段, 与 MedicalCase 关系 (Restrict), QueryFilter 匹配 MedicalCase.IsDeleted, 复合索引 (MedicalCaseId+CreatedAt, OperatorId+CreatedAt)

#### Data/Configurations/RefreshTokenConfiguration.cs
- **类**: `RefreshTokenConfiguration` : `IEntityTypeConfiguration<RefreshToken>`
- **配置内容**: Token 唯一索引, IsRevoked+Token 覆盖索引 (Include UserId/UserType/ExpiresAt), FamilyId 索引 (重放攻击检测), 与 User FK (Cascade)

#### Data/Configurations/AuthSessionConfiguration.cs
- **类**: `AuthSessionConfiguration` : `IEntityTypeConfiguration<AuthSession>`
- **配置内容**: Status 枚举转 int; MVP 阶段移除多余索引

#### Data/Configurations/AutoLoginTokenConfiguration.cs
- **类**: `AutoLoginTokenConfiguration` : `IEntityTypeConfiguration<AutoLoginToken>`
- **配置内容**: Token 唯一索引, FamilyId 索引, UserId+UserName 复合索引, 与 User FK (Cascade)

#### Data/Configurations/SecurityAuditLogConfiguration.cs
- **类**: `SecurityAuditLogConfiguration` : `IEntityTypeConfiguration<SecurityAuditLog>`
- **配置内容**: EventType+CreatedAt 复合索引, UserId+CreatedAt 复合索引

#### Data/Configurations/SystemLogConfiguration.cs
- **类**: `SystemLogConfiguration` : `IEntityTypeConfiguration<SystemLog>`
- **配置内容**: 4 个索引 (Timestamp, Level, CorrelationId, UserId)

### Interfaces/ -- 仓储接口

#### Interfaces/IRepository.cs
- **接口**: `IRepository<T>` where T : class
- **查询方法 (5个)**: GetByIdAsync(Guid), GetAllAsync(), GetPagedAsync(int, int, string?), FindAsync(Expression), GetSingleAsync(Expression)
- **写入方法 (4个)**: AddAsync(T), UpdateAsync(T), DeleteAsync(Guid), AddRangeAsync(IEnumerable)
- **辅助方法 (2个)**: CountAsync(), SaveChangesAsync()

### Repositories/ -- 仓储基类

#### Repositories/BaseRepository.cs
- **类**: `BaseRepository<TEntity>` : `IRepository<TEntity>` where TEntity : BaseEntity (abstract)
- **用途**: 泛型仓储基类，提供标准 CRUD + 分页 + 软删除 + 高级查询
- **模板方法 (protected virtual)**:
  - `ApplyKeywordFilter(query, keyword)` -- 子类覆盖提供关键字搜索逻辑
  - `ApplyDefaultOrdering(query)` -- 子类覆盖提供排序逻辑 (默认 CreatedAt DESC)
- **查询方法**:
  - `GetByIdAsync(Guid)` -- 按 ID 查询 (带 !IsDeleted 过滤)
  - `GetAllAsync()` -- 获取全部 (CreatedAt DESC)
  - `FindAsync(Expression)` -- 条件查询 (2个重载: 简单版/高级版带 Include+分页+排序)
  - `SelectAsync<TResult>(predicate, selector)` -- 投影查询
  - `GetPagedAsync(int, int, string?)` -- 模板方法分页 (AsNoTracking, 使用 ApplyKeywordFilter + ApplyDefaultOrdering)
  - `GetPagedAsync(int, int, Expression?, Expression?, bool)` -- 高级分页 (动态过滤+排序)
  - `ExistsAsync(Expression)` / `ExistsAsync(Guid)` -- 存在性检查
  - `CountAsync()` / `CountAsync(Expression?)` -- 计数
- **写入方法**:
  - `AddAsync(T)` / `AddRangeAsync(IEnumerable)` -- 新增 (自动生成 Guid)
  - `UpdateAsync(T)` / `UpdateRangeAsync(IEnumerable)` -- 更新
  - `DeleteAsync(Guid)` -- 软删除 (IsDeleted=true)
  - `DeleteRangeAsync(Expression)` -- 批量软删除
  - `RestoreAsync(Guid)` -- 恢复软删除 (IgnoreQueryFilters)
  - `HardDeleteAsync(Guid)` -- 物理删除
- **高级查询**:
  - `GetQueryable()` / `GetNoTrackingQueryable()` -- IQueryable 访问
  - `FromSqlRawAsync(sql, params)` -- 原生 SQL
  - `GetPagedResultAsync(query, page, size)` (protected) -- 分页助手
- **SaveChangesAsync()**: 全局 RowVersion 同步 (Modified/Unchanged 实体同步 OriginalValue = CurrentValue)
- **被继承**: MedicalCaseRepository, PatientRepository, UserRepository, HerbRepository, FormulaRepository

### Services/ -- 服务层

#### Services/BaseService.cs
- **类**: `BaseService` (abstract, 非泛型)
- **用途**: 统一权限验证基类
- **权限方法**:
  - `ValidateEditPermission(entityId, currentUserId, createdUserId, createdDate, isAdmin, entityType)` -- 管理员始终通过；非管理员需本人创建+当天创建
  - `ValidateDeletePermission(...)` -- 同上 + hasRelatedData 检查
  - `ExtractUserInfoAsync(HttpContext?)` -- 从 Claims 提取 UserId/IsAdmin/Role
- **辅助方法**: `IsToday(DateTime)`, `GetRoleDisplayName(string)`, `LogPermissionValidation(...)`

- **类**: `BaseService<T>` : `BaseService` where T : class (abstract, 泛型)
- **用途**: 统一错误处理 + FluentValidation 集成
- **方法**:
  - `ExecuteAsync<TResult>(Func, operationName)` -- 执行操作，AppException 转 Result.Failure (带错误码)
  - `ExecuteAsync(Func, operationName)` -- 无返回值版本
  - `ValidateAsync<TDto>(dto, validator)` -- FluentValidation 异步验证
  - `Validate<TDto>(dto, validator)` -- FluentValidation 同步验证

#### Services/CrossModuleQueryService.cs (文件名与类名不一致)
- **类**: `CrossModuleService` (实现 4 个 ISP 接口)
- **文件名注意**: 文件名为 CrossModuleQueryService.cs 但类名为 CrossModuleService
- **依赖**: AppDbContext (直接使用 DbContext 进行跨模块数据访问)
- **IPatientCrossModuleService 实现**:
  - `GetPatientBasicInfoAsync(Guid)` -- 返回 PatientBasicDto
  - `GetPatientsBasicInfoAsync(IEnumerable<Guid>)` -- 批量返回 Dictionary
  - `PatientExistsAsync(Guid)` -- 存在性检查
  - `CheckPatientReferenceAsync(Guid)` -- 检查 MedicalCases 引用数
- **IHerbCrossModuleService 实现**:
  - `GetHerbBasicInfoAsync(Guid)` -- 返回 HerbBasicDto
  - `GetHerbByNameOrPinyinAsync(string)` -- 按名称/拼音查找
  - `CheckHerbReferenceAsync(Guid)` -- 检查 PrescriptionItems 引用数
  - `GetHerbPricesAsync(IEnumerable<Guid>)` -- 批量获取价格
- **IUserCrossModuleService 实现**:
  - `GetUserBasicInfoAsync(Guid)` -- 返回 UserBasicDto
  - `GetUserByUsernameAsync(string)` -- 返回 UserCredentialDto (含 PasswordHash)
  - `UpdateUserPasswordHashAsync(Guid, string)` -- 更新密码 (不用 FindAsync)
  - `UserExistsAsync(Guid)` -- 存在性检查
  - `UpdateLoginFailureAsync(Guid, int, DateTime?)` -- 更新锁定状态
  - `ResetLoginStateAsync(Guid)` -- 重置登录状态
- **ICrossModuleAuthService 实现**:
  - `RevokeUserTokensAsync(Guid, string)` -- 批量撤销 RefreshToken，失败记 Warning 不阻塞

#### Services/CrossModule/IPatientCrossModuleService.cs
- **接口**: `IPatientCrossModuleService` (4 个方法)
- **消费者**: MedicalCase 模块, Sync 模块

#### Services/CrossModule/IUserCrossModuleService.cs
- **接口**: `IUserCrossModuleService` (6 个方法)
- **消费者**: MedicalCase 模块, Auth 模块

#### Services/CrossModule/IHerbCrossModuleService.cs
- **接口**: `IHerbCrossModuleService` (4 个方法)
- **消费者**: Sync 模块, Formula 模块

#### Services/CrossModule/ICrossModuleAuthService.cs
- **接口**: `ICrossModuleAuthService` (1 个方法: RevokeUserTokensAsync)
- **消费者**: Users 模块 (角色变更/禁用时触发)

#### Services/CrossModule/ReferenceCheckResult.cs
- **记录**: `ReferenceCheckResult` (record)
- **属性**: HasReferences (bool), ReferenceCount (int), Message (string?)
- **消费者**: Sync 模块 (CheckPatientReferenceAsync, CheckHerbReferenceAsync)

### Caching/ -- 缓存失效

#### Caching/ICacheInvalidationService.cs
- **接口**: `ICacheInvalidationService`
- **方法**: `InvalidateAsync(string tag)`, `InvalidateAsync(IEnumerable<string> tags)`
- **消费者**: HerbService, FormulaService, PatientService, MedicalCaseCommandService, MedicalCaseStateService, FormulaImportExportService

#### Caching/CacheInvalidationService.cs
- **类**: `CacheInvalidationService` : `ICacheInvalidationService` (sealed)
- **依赖**: IOutputCacheStore, IMemoryCache
- **实现**: EvictByTagAsync (OutputCache) + RemoveByPrefix (MemoryCache 扩展方法)

### Web/ -- API 基础设施

#### Web/BaseApiController.cs
- **类**: `BaseApiController` : `ControllerBase` (abstract)
- **用途**: 所有 API 控制器的基类
- **核心方法**:
  - `GetOperator()` -- 从 Claims 提取 (OperatorId, OperatorName, OperatorRole)，兼容多种 JWT Claim 标准
  - `LogOperation(operation, data?, targetId?)` -- 带脱敏的统一日志
  - `GetModelErrors()` / `GetValidationErrorMessage()` -- 模型验证错误提取
  - `IsValidGuid(Guid)` / `GetRequestId()` -- 参数验证/链路追踪
- **API 响应方法**: `Success()`, `Success<T>()`, `SuccessPaged<T>()`, `Error()`, `NotFound()`, `BusinessFail()`, `ValidationFail()`
- **Result 处理**: `HandleResult<T>()`, `HandlePagedResult<T>()`, `HandleBoolResult()`, `HandleAuthResult<T>()` (根据 ModuleErrorCode 映射 HTTP 状态码)
- **验证方法**: `ValidateGuid()`, `IsAdminOrOwner()`, `ValidateOwnership()`, `GetEntityWithOwnershipCheckAsync<TDto>()` (2个重载), `ValidateModel()`
- **被继承**: AuthController, UsersController, PatientsController, MedicalCaseController, HerbsController, FormulasController, HealthController, SyncController, DiagnosticsController

#### Web/ApiErrorCodes.cs
- **类**: `ApiErrorCodes` (static)
- **常量**: `DATA_SAVE_FAILED`, `DATASAVEFAILED` (兼容别名)
- **消费者**: 仅 HerbsController

### Logging/ -- 日志服务

#### Logging/LogCleanupService.cs
- **类**: `LogCleanupService` : `BackgroundService`
- **用途**: 定期清理过期数据库日志 (Error/Fatal 永久保留)
- **配置**: LogCleanupOptions (Enabled, RetentionDays=90, CleanupIntervalHours=24, InitialDelayMinutes=5, BatchSize=1000)
- **实现**: 分批 DELETE TOP(@batchSize) 避免长时间锁表

- **类**: `LogCleanupOptions`
- **用途**: 日志清理配置 POCO
- **配置节**: `Lybt:Logging:Cleanup`

#### Logging/HttpContextCorrelationIdProvider.cs
- **类**: `HttpContextCorrelationIdProvider` : `ICorrelationIdProvider`
- **用途**: Server 端从 HttpContext.Items 获取/设置 CorrelationId
- **方法**: `GetCorrelationId()`, `SetCorrelationId(string)`
- **消费者**: CorrelationIdEnricher (Serilog enricher)

### Configuration/ -- 配置服务

#### Configuration/Services/DefaultPasswordService.cs
- **类**: `DefaultPasswordService`
- **用途**: 环境感知的默认密码治理 (生产环境强制禁用)
- **方法**:
  - `GetSystemAdminPassword()` -- 获取管理员默认密码 (生产环境返回 null)
  - `GetNewUserPassword()` -- 获取新用户默认密码
  - `IsDefaultPasswordAllowed()` -- 环境检查 (Production 禁用, Development 可配置)
  - `IsDefaultPasswordAvailable(bool isDatabaseEmpty)` -- 增加数据库空检查
  - `GetConfigurationSummary()` -- 配置摘要 (返回 DefaultPasswordSummary)
- **状态**: [SUSPECT] DI 注册但无外部调用 (详见死代码分析)

- **类**: `DefaultPasswordSummary`
- **用途**: 默认密码配置摘要 POCO
- **状态**: [SUSPECT] 仅 DefaultPasswordService.GetConfigurationSummary() 返回，无外部消费

#### Configuration/Validation/ProductionConfigurationValidator.cs
- **类**: `ProductionConfigurationValidator`
- **用途**: Production 环境启动时验证必需配置项
- **方法**: `ValidateOrThrow()` -- 验证 7 个必需配置项 (ConnectionString, JWT SecretKey, 默认密码, 管理员信息等)
- **验证规则**: 值存在性, 最小长度, 正则格式
- **消费者**: Program.cs (启动时调用)

- **辅助类型**: `ConfigurationItem`, `ConfigurationError`, `Severity` (Critical/Important/Optional), `ErrorType` (Missing/Placeholder/InvalidFormat), `ProductionConfigurationException`

### DependencyInjection/ -- 依赖注入扩展

#### DependencyInjection/RepositoryServiceCollectionExtensions.cs
- **类**: `RepositoryServiceCollectionExtensions` (static)
- **方法**:
  - `AddRepository<TRepository, TImplementation>(this IServiceCollection, ServiceLifetime)` -- 泛型注册; 被 UsersModule 使用
  - `AddServerRepositories(this IServiceCollection)` -- 注册核心 Repository (当前为空操作，具体注册在各模块); 被 DatabaseServiceCollectionExtensions 调用
- **已移除 (2026-03-01)**: `AddRepositories(params Assembly[])` -- 反射扫描注册，无任何调用

### 根目录

#### GlobalSuppressions.cs
- **用途**: 程序集级别代码分析抑制
- **抑制项**: CS0618 (过时成员), SA0001 (XML文档), CS8601 (可空引用)

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| `DefaultPasswordService` 全部方法 | [SUSPECT] DI 注册但无外部注入/调用 | DatabaseInitializationService 直接读取 DefaultPasswordOptions | 确认是否有运行时注入场景，无则移除 |
| `DefaultPasswordSummary` | [SUSPECT] 仅 GetConfigurationSummary 返回 | 若 DefaultPasswordService 移除则一并清理 | 跟随 DefaultPasswordService |
| `AddRepositories(params Assembly[])` | [已清理] 2026-03-01 | 各模块使用 AddRepository<T,U> 或模块注册方法 | 已移除 |
| `AddServerRepositories()` | [SUSPECT] 被调用但方法体为空操作 | 各模块独立注册 Repository | 评估是否需要保留扩展点 |
| `EntityOptimizationExtensions` 中 OptimizeMedicalCaseEntity/OptimizePrescriptionEntity | [已清理] 2026-03-01 | 索引已在各 Configuration 类中定义 | 已移除 |
| `ApiErrorCodes.DATASAVEFAILED` 兼容别名 | [SUSPECT] 仅 HerbsController 使用 DATA_SAVE_FAILED | 统一使用 DATA_SAVE_FAILED | 确认无其他引用后移除兼容别名 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| FindAsync 应用全局查询过滤器 (IsDeleted) | EF Core 8 的 FindAsync 在实体不在 ChangeTracker 中时会应用过滤器 | 需要查询软删除记录时使用 IgnoreQueryFilters() |
| BaseEntityConfiguration 中 GETUTCDATE() 是 SQL Server 特定语法 | 切换到 SQLite 时需要适配 | 双模式架构中 SQLite Provider 另行处理 |
| SaveChangesAsync 跳过 RowVersion 并发检查 | 设计决策: 同一请求内多次操作同步 OriginalValue = CurrentValue | 仅对 Modified/Unchanged 状态的实体处理 |
| PrescriptionItem 没有 RowVersion | PrescriptionItem 不继承 BaseEntity | Issue #2250: 使用 Metadata 检查属性是否存在，避免异常 |
| CrossModuleService 中更新密码不用 FindAsync | FindAsync 在实体不在 ChangeTracker 中时应用全局过滤器 | 使用 FirstOrDefaultAsync + 显式 !IsDeleted 条件 |
| BaseApiController.GetOperator() 兼容多种 Claims | JWT 标准不统一 (sub/NameIdentifier/unique_name 等) | 按优先级依次尝试多种 Claim 类型 |

## OpenSpec 追踪

| OpenSpec ID | 内容 | 状态 |
|-------------|------|------|
| enhance-dataflow-logging | LOG-015 Repository 操作日志 | 已完成 |
| adopt-mapperly-unified-mapping | 移除 IMapper 依赖，各 Service 注入 Mapper | 已完成 |
| refactor-medicalcase-management | MedicalCaseAuditLog 配置 (LIFECYCLE-008) | 已完成 |
| refactor-server-ddd-aggregates | Consultation/Prescription 移除反向导航配置 | 已完成 |
| fix-doctorid-to-userid | MedicalCase 列名 DoctorId -> UserId | 已完成 |
| refactor-login-authentication | AutoLoginToken 配置 (CVT-001) | 已完成 |
| consolidate-medicalcase-queries | 医案查询方法从 CrossModuleService 删除 | 已完成 |
| optimize-module-list-ui | BaseApiController 所有权检查 | 已完成 |
| refactor-diagnosis-fields | 迁移: 重构诊断字段 | 已完成 |

## EF Core 实体关系配置要点

### MedicalCase - Consultation (1:1 共享主键)

```csharp
// ConsultationConfiguration.cs
builder.HasOne<MedicalCase>()          // 无反向导航
    .WithOne(m => m.Consultation)
    .HasForeignKey<Consultation>(c => c.Id)  // 共享主键
    .IsRequired()
    .OnDelete(DeleteBehavior.Cascade);
```

### MedicalCase - Prescription (1:0..1 外键)

```csharp
// PrescriptionConfiguration.cs
builder.HasOne<MedicalCase>()          // 无反向导航
    .WithOne(m => m.Prescription)
    .HasForeignKey<Prescription>(p => p.MedicalCaseId)
    .IsRequired()
    .OnDelete(DeleteBehavior.Cascade);

// 唯一索引保证一医案至多一处方
builder.HasIndex(p => p.MedicalCaseId).IsUnique();
```

### MedicalCase 关键索引

```csharp
// 单患者仅一条未完成医案 (过滤唯一索引)
builder.HasIndex(m => m.PatientId)
    .IsUnique()
    .HasFilter("[CaseStatus] = 1 AND [IsDeleted] = 0");

// 按医生查询性能索引
builder.HasIndex(m => m.UserId);
```

## 设计分析

### 文件名与类名不一致

| 文件 | 文件名 | 类名 | 建议 |
|------|--------|------|------|
| Services/CrossModuleQueryService.cs | CrossModuleQueryService | CrossModuleService | 重命名文件为 CrossModuleService.cs 以保持一致 |

### Configuration 继承体系分类

项目中 EF Core 配置分为两种模式:

1. **继承 BaseEntityConfiguration<T>**: 用于 BaseEntity 子实体 (自动获得审计/并发/软删除配置)
   - MedicalCaseConfiguration, ConsultationConfiguration, PrescriptionConfiguration, PatientConfiguration, UserConfiguration, HerbConfiguration, FormulaConfiguration, MedicalCasePrintLogConfiguration

2. **直接实现 IEntityTypeConfiguration<T>**: 用于非 BaseEntity 实体 (无 RowVersion/IsDeleted)
   - FormulaHerbItemConfiguration (FormulaHerbItem), PrescriptionItemConfiguration (PrescriptionItem), MedicalCaseAuditLogConfiguration, RefreshTokenConfiguration, AuthSessionConfiguration, AutoLoginTokenConfiguration, SecurityAuditLogConfiguration, SystemLogConfiguration

### 全局查询过滤器双重配置

BaseEntity 子实体的 `HasQueryFilter(e => !e.IsDeleted)` 存在双重配置:
- `BaseEntityConfiguration<T>.Configure()` 中设置
- `EntityOptimizationExtensions.ApplyGlobalQueryFilters()` 中通过反射再次设置

EF Core 不会叠加过滤器 (后者覆盖前者)，不会产生功能问题，但存在冗余。

### CrossModuleService 责任集中

`CrossModuleService` 一个类实现 4 个接口 (318 行)，涵盖 Patient/User/Herb/Auth 四个域的跨模块访问。当前规模可接受，但若持续增长应考虑拆分为独立实现类。

## 模块演进记录

- **初始设计**: 单一 AppDbContext + BaseRepository CRUD
- **Issue #2103**: BaseRepository 简化，保留核心 11 个方法
- **Issue #2250**: SaveChangesAsync 增加 RowVersion 同步，修复 PrescriptionItem 兼容
- **D5-1**: CrossModuleService ISP 拆分为 4 个接口
- **Phase 2 (Epic #1725)**: Repository 层简化，GetPagedResultAsync 助手
- **迁移**: 40+ 个 EF Core 迁移文件，从 InitialCreateV2 到最新
