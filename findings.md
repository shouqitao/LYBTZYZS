# LYBTZYZS WebAPI 架构评估报告

**评估日期**: 2026-03-30  
**技术栈**: .NET 8 + EF Core 8 + ASP.NET Core  
**评估范围**: src/Server/ + src/Shared/（排除 src/Client/）

---

## 1. 项目结构与依赖方向

### ✅ 整体架构清晰
项目采用模块化分层架构：
- `LYBT.WebAPI` → 各 Module → `LYBT.Infrastructure` → `LYBT.Entities`
- `LYBT.Shared.*` 作为横切共享库，被各层正确引用

### 🟢 Info: 模块间存在少量直接引用
- `MedicalCase` 引用 `Registration` 项目（`LYBT.Module.MedicalCase.csproj`）
- `Patients` 引用 `MedicalCase` 项目
- `Users` 引用 `Registration` 项目

**文件**: `src/Server/Modules/LYBT.Module.MedicalCase/LYBT.Module.MedicalCase.csproj`

**评价**: 这形成了 `Patients → MedicalCase → Registration` 的引用链。虽然不是循环依赖，但 Patients 依赖 MedicalCase 在领域模型上不太自然（患者应该是更基础的实体）。建议未来通过 `ICrossModuleService` 接口完全解耦。

### 🟢 Info: 无循环依赖 ✅
经检查所有 csproj，未发现循环引用。依赖方向总体正确：Controller → Module(Service) → Infrastructure(Repository) → Entities。

### 🟢 Info: Shared 项目职责划分合理
- `LYBT.Shared.Models` - DTO/枚举/通用模型
- `LYBT.Shared.Configuration` - 配置选项类
- `LYBT.Shared.Utilities` - 工具类（密码、文本）
- `LYBT.Shared.ExceptionHandling` - 异常体系
- `LYBT.Shared.Logging` - Serilog 配置与脱敏
- `LYBT.Shared.Validators` - 验证器
- `LYBT.Shared.Primitives` - 错误码等基础类型

---

## 2. Controller 层

### 🟢 Info: 控制器设计规范
- 所有控制器继承 `BaseApiController`，统一响应格式
- 使用 `ApiResponse<T>` 包装所有响应
- 控制器按职责拆分：`MedicalCasesController`(CRUD)、`MedicalCaseProcessingController`(状态流转)、`MedicalCasePrintController`(打印)、`MedicalCaseAuditController`(审计)
- 无明显业务逻辑泄漏

### 🟢 Info: 权限标注完整
- 类级别 `[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]`
- 方法级别精确控制，如 `[Authorize(Roles = RoleConstants.Doctor)]` 用于创建
- 实现了 FallbackPolicy，默认要求认证，AllowAnonymous 需显式标注

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs` 行 27-30

### 🟢 Info: API 路由 RESTful 且版本化
- `api/v{version:apiVersion}/medicalcases` 遵循 REST 规范
- 使用 URL 段版本控制 `ApiVersion("1")`
- HTTP 方法语义正确：POST创建、PUT更新、DELETE删除

### 🟡 Warning: 控制器中直接使用 Mapper 实例
- `MedicalCasesController` 中直接 `new MedicalCaseMapper()` 并使用手写的 `MapToMedicalCaseDetailDto` 方法
- 应统一使用 Mapperly 生成的 `ToDetailDto` 方法，保持映射逻辑一致

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs` 行 35-36, 69

### 🟡 Warning: 部分控制器返回 `Ok()` 而非使用 BaseApi 方法
- `MedicalCasesController.CreateMedicalCase` 直接构造 `ApiResponse<T>` 返回 `Ok()`，未使用 `Success<T>()` 方法
- 这绕过了 `RequestId` 自动填充

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs` 行 69-71

### 🟢 Info: 错误处理统一
- 全局 `BusinessExceptionHandler` + `SystemExceptionHandler`（IExceptionHandler 模式）
- 控制器内已移除 try-catch，由全局处理器接管
- 业务错误返回 422，系统错误返回 500

**文件**: `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Server/BusinessExceptionHandler.cs`

---

## 3. Service 层

### 🟢 Info: MedicalCase 多 Service 拆分合理（CQRS 思想）
拆分为 6 个专职 Service：
- `MedicalCaseCommandService` - 写操作
- `MedicalCaseQueryService` - 读操作
- `MedicalCaseStateService` - 状态流转
- `MedicalCasePermissionService` - 权限检查
- `MedicalCaseAuditService` - 审计日志
- `MedicalCasePrintService` - 打印

通过 `MedicalCaseFacade` 门面模式聚合，减少 Controller 依赖数。**这是优秀的设计**。

**文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/`

### 🟢 Info: BaseService 提供统一权限验证基类
- 实现"当天可改"规则（医生只能修改当天创建的记录）
- 管理员拥有全权限
- 权限逻辑集中在 BaseService，避免各 Service 重复

**文件**: `src/Server/Core/LYBT.Infrastructure/Services/BaseService.cs`

### 🟡 Warning: UserService 依赖过多（7个构造函数参数）
```
IUserRepository, ILogger, IConfiguration, IHttpContextAccessor, 
IValidator<UserInputDto>, ICrossModuleAuthService, IUserBatchOperationService, 
IRegistrationRepository
```
`IConfiguration` 和 `IHttpContextAccessor` 的注入暗示有配置/上下文逻辑未封装。

**文件**: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs` 行 34-43

### 🟡 Warning: Service 层直接引用 `IConfiguration`
- `UserService` 和 `AuthService` 都注入了 `IConfiguration`
- 应通过强类型 Options 模式（`IOptions<T>`）访问配置

**文件**: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs` 行 37  
**文件**: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs` 行 17

### 🟡 Warning: CrossModule 接口定义在 Infrastructure 层
跨模块接口（`ICrossModuleAuthService`, `IUserCrossModuleService` 等）定义在 `LYBT.Infrastructure/Services/CrossModule/`，但接口实现在各 Module 内部。这虽然可行，但将接口移到 Shared 层会更清晰。

**文件**: `src/Server/Core/LYBT.Infrastructure/Services/CrossModule/`

---

## 4. Repository & 数据层

### 🟢 Info: BaseRepository 实现质量高
- 泛型基类提供完整 CRUD + 分页 + 软删除
- 模板方法模式（`ApplyKeywordFilter`, `ApplyDefaultOrdering`）支持子类定制
- 所有查询自动过滤 `IsDeleted`
- `AsNoTracking` 用于分页查询
- 提供 `GetQueryable()` / `GetNoTrackingQueryable()` 灵活查询

**文件**: `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`

### 🟢 Info: RowVersion 并发处理
- `SaveChangesAsync` 中同步 RowVersion 的 OriginalValue/CurrentValue
- 捕获 `DbUpdateConcurrencyException` 并抛出友好错误

**文件**: `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs` (SaveChangesAsync 方法)

### 🟢 Info: 审计字段自动填充
- `AppDbContext.SaveChangesAsync` 调用 `SetAuditFields()` 自动设置 `UpdatedAt`、`UpdatedBy` 等字段
- 通过 `IHttpContextAccessor` 获取当前用户

**文件**: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`

### 🟡 Warning: BaseRepository 每个 Add/Update 操作都立即 SaveChanges
```csharp
public virtual async Task<TEntity> AddAsync(TEntity entity, ...)
{
    await _dbSet.AddAsync(entity, cancellationToken);
    await SaveChangesAsync(cancellationToken); // 每次操作都提交
    return entity;
}
```
这导致无法在 Service 层实现工作单元模式（多个操作在同一个事务中提交）。对于需要事务的场景，Service 需要直接使用 `Database.BeginTransactionAsync()`。

**文件**: `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs` AddAsync/UpdateAsync/DeleteAsync 方法

### 🟡 Warning: `FromSqlRawAsync` 存在 SQL 注入风险
```csharp
public virtual async Task<List<TEntity>> FromSqlRawAsync(string sql, params object[] parameters)
```
虽然方法签名支持参数化查询，但 `sql` 参数是裸字符串，调用者可能拼接 SQL。

**文件**: `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`

### 🟢 Info: Mapperly 已全面采用
- 所有 Module 使用 Mapperly 替代 AutoMapper
- Patient、MedicalCase、Herb、Formula 等均有 Mapperly 映射器
- 编译时生成映射代码，性能优异

**文件**: `src/Server/Modules/LYBT.Module.Patients/Mapping/PatientMapper.cs`  
**文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Mapping/MedicalCaseMapper.cs`

### 🟢 Info: EF Core 配置使用 IEntityTypeConfiguration
- `AppDbContext.OnModelCreating` 使用 `ApplyConfigurationsFromAssembly`
- 符合 EF Core 最佳实践

**文件**: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` OnModelCreating 方法

---

## 5. 安全

### 🟢 Info: 密码使用 BCrypt 存储 ✅
- `PasswordHelper` 使用 BCrypt，工作因子 11（合理范围 10-15）
- 支持弱密码检测
- 支持密码强度评估

**文件**: `src/Shared/LYBT.Shared.Utilities/Security/PasswordHelper.cs`

### 🟢 Info: JWT 实现质量高
- 密钥长度验证（≥32字符）
- 生产环境禁止使用默认密钥
- Token 验证参数齐全（Issuer、Audience、Lifetime、SigningKey）
- 时钟偏移可配置
- 支持 Token 撤销服务

**文件**: `src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs`  
**文件**: `src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs`

### 🟢 Info: 账户锁定机制完善
- 5次失败登录后锁定15分钟
- 记录失败次数和锁定结束时间

**文件**: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs` 行 55-58

### 🟢 Info: 安全头中间件完善
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: strict-origin-when-cross-origin
- Permissions-Policy: 禁用 camera/microphone/geolocation
- 生产环境 CSP + HSTS
- 移除 X-Powered-By 和 Server 头

**文件**: `src/Server/Services/LYBT.WebAPI/Middleware/SecurityHeadersMiddleware.cs`

### 🟢 Info: 敏感数据脱敏
- `SensitiveDataMasker` 用于日志脱敏
- `BaseApiController.LogOperation` 自动脱敏

**文件**: `src/Shared/LYBT.Shared.Logging/Masking/SensitiveDataMasker.cs`

### 🟡 Warning: 开发环境使用硬编码默认 JWT 密钥
```csharp
jwtSecret = "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction";
```
虽然仅限非生产环境，但如果开发/测试环境使用真实数据，仍有风险。

**文件**: `src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs`

### 🟢 Info: 输入验证
- 使用 FluentValidation 进行 DTO 验证
- Controller 层有 `ValidateModel()` / `ValidateGuid()` 便捷方法
- `[ApiController]` 自动触发模型验证

### 🟢 Info: 生产配置验证
- `ProductionConfigurationValidator` 在启动时检查关键配置项
- 生产环境缺失关键配置会阻止启动

**文件**: `src/Server/Services/LYBT.WebAPI/Program.cs` (configValidator 相关代码)

### 🟢 Info: 限流已启用
- `app.UseRateLimiter()` 已在中间件管道中注册

**文件**: `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs` 行 54

---

## 6. 横切关注点

### 🟢 Info: 日志系统完善
- Serilog 两阶段初始化（Bootstrap Logger + Final Logger）
- 支持运行时动态调整日志级别（`LoggingLevelManager`）
- CorrelationId 端到端追踪（支持 W3C traceparent）
- 敏感数据自动脱敏（`WithSensitiveDataMasking()`）
- 审计字段自动填充

**文件**: `src/Server/Services/LYBT.WebAPI/Program.cs`  
**文件**: `src/Server/Services/LYBT.WebAPI/Middleware/CorrelationIdMiddleware.cs`

### 🟢 Info: 异常处理分层
- `BusinessExceptionHandler`：处理 `AppException`，返回 4xx
- `SystemExceptionHandler`：处理未预期异常，返回 500
- 使用 ASP.NET Core 8 的 `IExceptionHandler` 接口（优于传统的 ExceptionHandler 中间件）
- 配合 `UseStatusCodePagesWithProblemDetails()` 实现 RFC 7807

**文件**: `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Server/`

### 🟢 Info: DI 注册模块化
- `ApiServiceCollectionExtensions` - API 相关服务
- `AuthenticationServiceCollectionExtensions` - 认证授权
- `DatabaseServiceCollectionExtensions` - 数据库
- `ServiceCollectionExtensions` - 总注册入口
- `UnifiedApplicationInitialization` - 应用初始化

**文件**: `src/Server/Services/LYBT.WebAPI/Extensions/`

### 🟢 Info: 配置管理规范
- 统一使用 `LYBT.Shared.Configuration` 中的强类型 Options
- 支持 `.env` 文件 + `appsettings.json` 双配置模式
- 环境感知（`ConfigureEnvironmentAwareHosting`）

### 🟡 Warning: Program.Main 方法过长
- `Program.cs` 的 `Main` 方法包含大量启动逻辑
- 虽然通过 Extension 方法拆分了部分逻辑，但 Main 本身仍有约 100+ 行
- 建议进一步提取为独立的 `Startup` 或 `WebApplicationBuilder` 扩展

**文件**: `src/Server/Services/LYBT.WebAPI/Program.cs`

---

## 7. 代码质量

### 🟢 Info: 命名规范良好
- 类名、方法名遵循 C# 命名规范
- 中英文注释混合使用，XML 文档注释完整
- 接口以 `I` 前缀，Service/Repository 后缀统一

### 🟡 Warning: 源文件中存在大量编码为 GBK/GB2312 的中文注释
在读取过程中发现很多 `.cs` 文件中的中文注释显示为乱码（如 `/// <summary>` 后的中文）。这可能是源文件编码问题。虽然不影响编译，但影响代码可读性和团队协作。

**文件**: 多个 Infrastructure/Repositories/Auth 服务文件

### 🟢 Info: 魔法字符串已收敛
- 使用常量类 `PolicyConstants`、`RoleConstants`、`HttpHeaderConstants`
- 错误码使用 `ErrorCode` 枚举
- 业务常量使用 `const`（如 `MaxFailedLoginCount = 5`, `LockoutMinutes = 15`）

### 🟢 Info: 注释质量高
- 方法级别 XML 文档注释完整
- 关键设计决策有 Issue 编号引用（如 `Issue #2103`, `OpenSpec: ...`）
- 代码中的 `//` 注释解释了"为什么"而非"做什么"

### 🟡 Warning: `MedicalCaseMapper` 中混合了 Mapperly 和手写映射
```csharp
// Mapperly 生成的 partial 方法
public partial MedicalCaseDetailDto ToDetailDto(MedicalCase entity);

// 手写方法
public MedicalCaseDetailDto MapToMedicalCaseDetailDto(MedicalCase entity) { ... }
```
Controller 中使用手写的 `MapToMedicalCaseDetailDto`，而 Mapperly 的 `ToDetailDto` 未被充分使用。应统一到 Mapperly。

**文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Mapping/MedicalCaseMapper.cs`

---

## 总结评分

### 评分: **B+**

**优点**:
1. **架构层次清晰** - 模块化设计，职责分离良好
2. **安全基础扎实** - BCrypt、JWT、安全头、限流、账户锁定一应俱全
3. **横切关注点完善** - 日志、异常处理、配置管理、审计追踪体系完整
4. **MedicalCase 模块拆分优秀** - Facade + CQRS 思想，SRP 贯彻到位
5. **技术选型现代** - Mapperly、FluentValidation、Serilog、API Versioning
6. **代码注释质量高** - 有 Issue 追溯、设计决策记录

**不足**:
1. 部分 Service 依赖注入过多（UserService 8个参数）
2. Repository 即时提交模式限制事务控制
3. IConfiguration 直接注入（应使用 Options 模式）
4. MedicalCaseMapper 手写映射与 Mapperly 并存
5. Patients → MedicalCase 模块引用方向可优化

---

## Top 5 改进建议

### 1. 🟡 统一使用 Mapperly，移除手写映射（优先级: 高）
- 删除 `MedicalCaseMapper.MapToMedicalCaseDetailDto()` 等手写方法
- Controller 统一使用 Mapperly 生成的 `ToDetailDto()`
- 确保 Mapperly 配置覆盖所有映射场景

### 2. 🟡 引入工作单元模式（优先级: 高）
- 将 `BaseRepository.AddAsync/UpdateAsync` 拆分为"操作"和"提交"两步
- 或提供 `AddWithoutSave` / `UpdateWithoutSave` 变体
- 让 Service 层控制事务边界

### 3. 🟡 消除 IConfiguration 直接注入（优先级: 中）
- `UserService`、`AuthService` 中的 `IConfiguration` 替换为 `IOptions<T>`
- 将相关配置项提取到强类型 Options 类

### 4. 🟡 解耦 Patients → MedicalCase 模块引用（优先级: 中）
- Patients 模块不应直接引用 MedicalCase 模块
- 通过 `ICrossModuleService` 接口解耦
- 保持依赖方向：基础模块 → 上层模块

### 5. 🟢 统一文件编码为 UTF-8（优先级: 低）
- 确保所有 `.cs` 文件使用 UTF-8 编码
- 在 `.editorconfig` 中强制指定 `charset = utf-8`
- 提升跨平台协作可读性

---

## 发现统计

| 严重程度 | 数量 |
|---------|------|
| 🔴 Critical | 0 |
| 🟡 Warning | 8 |
| 🟢 Info | 22 |
| **总计** | **30** |
