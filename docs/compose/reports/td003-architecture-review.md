# TD-003 架构审查报告 — Server / LocalWebAPI 一致性

> 日期: 2026-08-21 | 方式: 纯只读（完整阅读 Program.cs / LocalWebApiProgram.cs / 6×AddXxxModule / 2×测试基类 / 中间件） | 门禁: build 0/0(8×CS0618豁免) + arch 91/91

## 摘要

- **TD-003 现象**：`tech-debt.md` 记 Desktop 162 失败（STA/WPF 线程 + SysAdmin 登录，`tech-debt.md:12`），实为两类基类并存 + Server/LocalWebAPI 注册漂移共同导致。单纯“STA 容器化”描述掩盖了 **500 真根因在测试侧 DI 缺失**。
- **真根因**：`LocalWebApiControllerTestBase` 手工 `WebApplicationBuilder` 仅注册 `IdentityModule` 单模块（`LocalWebApiControllerTestBase.cs:94`），缺 `Patients/Catalog/MedicalCases/Registration/Reports` 5 模块及其 `ICatalogQueryService`/`IPatientService` 等，命中 `/api/v1/herbs|patients|formulas|medicalcases` 即 `InvalidOperationException: Unable to resolve service` → 中间件无 `UseExceptionHandler` 兜底直接 500。而 `LocalApiTestBase` 走 `LocalWebApiProgram.CreateApplication`（6 模块全量）故 `AdminLocalTests` 7 用例应过（`LocalApiTestBase.cs:63`）。
- **架构漂移**：Server `RegisterAllApplicationServices`（9 步 + `AddLybtLogging/AddSignalR/HealthChecks/DataProtection/Hsts/ResponseCompression/Cors/RateLimiting`） vs Local `CreateApplication`（14 步精简，无 `ResponseCaching` 细节、`AddHealthChecks/HostedService`、`ExceptionHandler/ForwardedHeaders/CorrelationId/SecurityHeaders/Serilog` 等完整管线）。测试侧手工基类进一步二次漂移。
- **推荐**：**方案 D（统一宿主抽象）** 为终局：抽 `SharedHostRegistration` 供 Server/Local 共用 + 单一 `WebApplicationFactory` 测试基类；短期先以 **方案 A′（修测试基类）** 止血，消除 500 后再收敛生产侧 drift。详见 §推荐 与 §实施计划。

---

## 根因分析

### 1. 500 的直接原因 — 读代码推导，非猜测

**路径 1：`LocalWebApiControllerTestBase` → 500（主因，约 70+ 用例）**

```csharp
// LocalWebApiControllerTestBase.cs:94 — InitializeAsync 手工注册
builder.Services.AddIdentityModule(builder.Configuration); // 仅 1 模块
builder.Services.AddMediatR(cfg =>
  cfg.RegisterServicesFromAssembly(typeof(LocalRefreshTokenCommand).Assembly));
builder.Services.AddControllers()… // 仅 HealthController 显式 AddApplicationPart
builder.Services.AddIdentity<…>(options => { Password.RequireDigit=true… }) // 与生产不同
LocalJwtConfig.ConfigureServices(…)
// 缺：AddPatientsModule / AddCatalogModule / AddMedicalCaseModule
//      / AddRegistrationModule / AddReportsModule
//      / AddMemoryCache / AddOutputCache / CacheInvalidationService / AddSignalR
//      / ISystemLogRepository / JsonFileConfigurationStore / LoggingLevelManager
//      / IDbContextAccessor / IHealthCheckService
```

而 `HerbsController : BaseCrudController` 构造依赖：

```csharp
// LocalWebAPI/Controllers/HerbsController.cs:28
public HerbsController(ISender sender, ILogger<HerbsController> logger,
    ICatalogQueryService<HerbListDto, HerbDetailDto> herbService)
```

`ICatalogQueryService` 仅由 `CatalogModule.AddCatalogModule` 注册（`CatalogModule.cs:46-52`）。未注册 → `ActivatorUtilities` 抛 `InvalidOperationException` → 因 Local 测试侧 `app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();`（`LocalWebApiControllerTestBase.cs:146-148`）**无 `UseExceptionHandler` / `UseStatusCodePages`**，异常直落 Kestrel → HTTP 500（非 RFC7807 `ApiResponse`）。`AdminLocalTests` 走 `LocalApiTestBase` 则无此问题。

**路径 2：`LocalWebAPI` 生产侧简化的 `AppDbContext` 注册（次因）**

```csharp
// LocalWebApiProgram.cs:58 — 生产 Local
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString) // 无 MigrationsAssembly/EnableRetryOnFailure/CommandTimeout/EnableDetailedErrors
);
// Server — DatabaseServiceCollectionExtensions.cs:68
services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(connectionString, sqlOptions => {
        sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
        sqlOptions.EnableRetryOnFailure(databaseOptions.RetryPolicy.MaxRetryCount, …);
        sqlOptions.CommandTimeout(databaseOptions.ConnectionPool.CommandTimeoutSeconds);
    }));
```

Local 的简注册在 `LocalWebAPI` 场景尚可工作（单机 LocalDB 低延迟），但与 Server 的 retry/超时/迁移定位不一致，评分 `R57-09` 曾指 `LogCleanupService` 等对 `MigrationsAssembly` 的隐式依赖。

**路径 3：`SysAdmin` 登录 500 — 非 LocalWebAPI 500，是 Identity 种子与策略差异**

- `LocalWebApiControllerTestBase` 手工 `AddIdentity` 将 `Password.RequireDigit=true / RequiredLength=6 / Lockout.MaxFailedAccessAttempts=int.MaxValue`（`cs:118-123`），与 `LocalWebApiProgram` 的 `PasswordPolicyValidator.Policy`（`RequireDigit/Length=MinLength=8` 等，`LocalWebApiProgram.cs:162`）及 Server 的 `PasswordPolicyValidator` 不一致，导致 `EnsureAdminUserAsync` 以 `Admin@123456` 创建的 admin 在不同策略下可能 `Succeeded=false` 但未抛，登录时 `UserManager` 校验失败 → 401/500 混淆。`LocalApiTestBase.EnsureRoleUsersAsync` 则显式对 `result.Succeeded` 才 `AddToRole`，否则静默跳过，同样可能无角色导致后续 `[Authorize(Policy)]` 403。
- 162 中的 STA 部分（`tech-debt.md:12` “STA/WPF 线程”）属 `tests/LYBT.Tests.Desktop` 的 `WpfFact`/`Dispatcher` 限制，与 Server/LocalWebAPI 无关，不在此 500 链中。

**结论**：500 非 Server 代码 bug，而是 **测试侧手工 WebApplication 与生产 `CreateApplication` 漂移 + 生产侧 Local 与 Server 漂移** 双重叠加；修测试侧即可闭合 70+ 500，修生产侧 drift 可防二次漂移。

---

## Server vs LocalWebAPI 差异清单

| 维度 | Server (`LYBT.WebAPI/Program.cs` + `Extensions/*`) | LocalWebAPI (`LocalWebApiProgram.cs`) | 差异 |
|------|---------------------------------------------------|--------------------------------------|------|
| **入口** | `Program` 置于全局命名空间（Issue #1077 为 `WebApplicationFactory` 兼容）`Main → CreateBuilder → AddLybtLogging → AddConfigurationClosedLoop → AddIdentity → RegisterAllApplicationServices → AddSignalR → ValidateCritical → MapKestrelEndpoints → Build → InitializeAllApplicationServices → ConfigureAllMiddleware → MapHub` | `LocalWebApiProgram.CreateBuilder` 显式 `WebApplicationOptions{ContentRootPath=AppContext.BaseDirectory, EnvironmentName=Development}`（fix `appsettings.json` 路径 `IDX10703`）`CreateApplication(builder, connectionString)` 被 `EmbeddedLocalWebApiService` 与 `LocalWebApiHost(Main)` 共用；`InitializeDatabaseAsync` 单独 `MigrateAsync + SeedRolesAndAdmin + LocalWebApiSeedData` | Server 有 `Bootstrap Logger` 两阶段、`Mutex` 单实例、`Env` 加载、`EnsureEnvironmentConfigFiles`、`DisplayDatabaseStatus`；Local 无 |
| **AppDbContext** | `DatabaseServiceCollectionExtensions.RegisterInfrastructureServices` 中 `AddDbContext<AppDbContext>((sp,opts)=>UseSqlServer(connectionString, MigrationsAssembly="LYBT.Infrastructure", EnableRetryOnFailure, CommandTimeout), EnableDetailedErrors(IsDevelopment), EnableServiceProviderCaching)` + `IDbContextAccessor` + `DatabaseInitializationService` | `LocalWebApiProgram.cs:58` `AddDbContext<AppDbContext>(opts=>UseSqlServer(connectionString))` 裸注册；另 `AddOptions<DatabaseOptions>.Configure(o=>o.ConnectionString=connectionString)` 供 `AddModuleDbContext<T>` 复用 | Local 丢失重试/超时/迁移定位/详细错误开关；但因同库单迁移（ADR-0017 方案 A，`AppDbContext` 单一迁移链）功能上不阻塞 |
| **模块 DbContext** | 同 `AddModuleDbContext<TContext>((sp,opts)=>UseSqlServer(ConnectionStringResolver.GetEffectiveConnectionString(...)))` | 同 `AddModuleDbContext<TContext>`（`ModuleDbContextExtensions.cs:26`）| 一致 |
| **业务模块** | `ServiceCollectionExtensions.RegisterBusinessModules`: `AddIdentityModule` → `AddRegistrationModule` → `AddPatientsModule` → `AddCatalogModule` → `AddMedicalCaseModule` → `AddReportsModule` → `AddSpiRegistries`（6 模块 + SPI） | `LocalWebApiProgram.cs:128-133` 同 6 模块 `AddIdentityModule/AddPatientsModule/AddCatalogModule/AddMedicalCaseModule/AddRegistrationModule/AddReportsModule`（顺序与 Server 略异但无依赖）| 数量一致；SPI 在 Local 侧同样注册（因模块内 `AddSingleton<IReportProvider>` 等随模块一起注册），`AddSpiRegistries` 仅 Server 显式调用，Local 未调但不影响 SPI（单例仍在模块内）|
| **MediatR** | 各模块内 `AddMediatR(cfg=>RegisterServicesFromAssembly(...)+AddOpenBehavior<ValidationBehavior>)`（6 次） | 同 6 模块内；另 `LocalWebApiProgram.cs:149` `AddMediatR(typeof(LocalRefreshTokenCommand).Assembly + ValidationBehavior)` 补充本地 Refresh/AutoLogin/Validate 3 本地 Handler | Local 额外 1 次本地 Handler 注册，互补 |
| **基础设施** | `RegisterInfrastructureServices`: `AddMemoryCache(SizeLimit/Compaction) + AddResponseCaching + AddOutputCache (裸，仅 IOutputCacheStore 供 CacheInvalidation) + CacheInvalidationService(Singleton) + Configure<MemoryCacheOptions> + AddHttpContextAccessor + DatabaseInitializationService + IHealthCheckService/HealthCheckService + IDeployService/IDownloadService + AddHealthChecks/AddHostedService(DatabaseStartupDiagnostics, LogCleanupService) + JWT Secret 校验` | `LocalWebApiProgram.cs:104-144`: `ISystemLogRepository + IHttpContextAccessor + JsonFileConfigurationStore(runtime-overrides.json) + AddControllers(AddApplicationPart HealthController + JsonStringEnumConverter + SensitiveDataConverter) + LoggingLevelManager + AddMemoryCache + AddOutputCache + CacheInvalidationService + AddSignalR + Health 中间件` | Local 缺 `ResponseCaching` 细参、`AddHealthChecks`/`HostedService`、`Deploy/Download` 服务（Local 的 Deploy/Diagnostics 控制器为简化版，直接注 `ISender`）；Local 多 `JsonFileConfigurationStore` 与 `HealthController` 显式 Part |
| **认证/授权** | `AuthenticationServiceCollectionExtensions.RegisterAuthenticationServices`: 先断言 `UserManager` 已注册（T2.4 防 `AddIdentity` 顺序错），`JwtOptions` 绑定 `Jwt:SecretKey/Issuer/Audience/ClockSkew`，`AddAuthentication(JwtBearer)` 含 `ValidIssuer/ValidAudience/RequireExpirationTime/ValidTypes=JWT/TryAllIssuerSigningKeys + Events.OnTokenValidated`（P3：禁用/删除用户 `IUserCrossModuleService.GetUserBasicInfoAsync` 校验 `Enabled`），`AddAuthorization` 设 `DefaultPolicy/FallbackPolicy=RequireAuthenticatedUser` + 8 策略 `AdminBusinessOnly/DoctorOnly/DoctorOrAdmin/AdminOrSuperAdmin/SysAdminOnly/DoctorOrReceptionist/ReceptionistOnly/DoctorOrAdminOrReceptionist` | `LocalJwtConfig.ConfigureServices`: `ValidateIssuer=false/ValidateAudience=false/ValidateLifetime=true`，`AddAuthorization` 8 策略同名但无 `Default/FallbackPolicy`，`TokenExpirationDays=365`，`GenerateToken` 手工 `JwtSecurityToken`（`Sub/NameIdentifier/Role/IsSysAdmin`），`GetSigningKey` 供本地 refresh 验签（T1.4 已补 `ValidateToken` 而非 `ReadJwtToken`） | 核心差异：Server 有 `FallbackPolicy`（所有未标记端点默认 401）+ `DefaultPolicy` + `OnTokenValidated` 用户状态拦截；Local 无 Fallback（需控制器显式 `[Authorize]`），Token 验证宽松（不验 Issuer/Audience），有效期 365d vs Server `JwtOptions.AccessTokenExpirationMinutes=480` |
| **API/控制器** | `ServiceCollectionExtensions.RegisterControllerServices`: `AddControllers().AddJsonOptions(LybtJsonOptions.PropertyNamingPolicy + JsonStringEnumConverter + SensitiveDataConverter + ReferenceHandler.IgnoreCycles + TypeInfoResolverChain.Insert(LybtJsonContext.Default)) + Configure<ApiBehaviorOptions>(InvalidModelState→ApiResponse) + AddLybtApiLoggingFilter` + `ApiServiceCollectionExtensions.RegisterApiServices: AddApiVersioning + AddSwaggerGen + AddProblemDetails` | `LocalWebApiProgram.cs:104`: `AddControllers().AddApplicationPart(HealthController).AddJsonOptions(JsonStringEnumConverter + SensitiveDataConverter)`（缺 `LybtJsonOptions`/`LybtJsonContext`/`ReferenceHandler` 等）；无 `ApiBehaviorOptions` 自定义、`Swagger/Versioning/ProblemDetails` | Local 的 JSON 与验证错误格式与 Server 不一致（`InvalidModelStateResponseFactory` 未统一），但对 `AdminLocalTests` 的 `ApiResponse{success}` 契约影响有限（因测试用 `PropertyNameCaseInsensitive=true`）|
| **CORS/限流/安全** | `AddCorsConfiguration` + `ConfigureRateLimiting`（Login 全局限流）+ `AddSecurityServices: AddDataProtection(PersistKeysToFileSystem + ApplicationName="LYBT") + AddHsts(365d, IncludeSubDomains, Preload)` + `ConfigurePerformanceOptimizations: AddResponseCompression(Brotli+Gzip)` | `LocalWebApiProgram.cs:179`: `AddRateLimiter(LocalLogin 5/min)` 单一；无 CORS/DataProtection/Hsts/ResponseCompression | Local 为进程内回环 `127.0.0.1:5290`，CORS/DataProtection 非必需；但限流策略仅本地 LocalLogin，与 Server 全局限流不同 |
| **中间件管道** | `UnifiedMiddlewareConfiguration.ConfigureAllMiddleware`: `UseExceptionHandler(IExceptionHandler chain: Business→System + fallback 500 ApiResponse) → UseStatusCodePages(RFC7807) → UseForwardedHeaders → UseLybtCorrelationId → (UseHttpsRedirection+Hsts 非 Dev) → UseSecurityHeaders → UseResponseCompression → UseStaticFiles(Releases) → ConfigureSwaggerMiddleware → UseRouting → UseCors → UseSerilogRequestLogging → UseRateLimiter → UseAuthentication → UseClaimsNormalization → UseAuthorization → UseResponseCaching → MapHealthChecks(/health,/health/database).AllowAnonymous + MapControllers + MapGet(/swagger)→404 AllowAnonymous` | `LocalWebApiProgram.cs:223`: `UseAuthentication → UseAuthorization → UseRateLimiter → MapControllers`（3 中间件） | **最大漂移**：Local 缺 `ExceptionHandler/StatusCodePages/ForwardedHeaders/CorrelationId/SecurityHeaders/ResponseCompression/StaticFiles/Swagger/Routing/Cors/Serilog/ResponseCaching/HealthChecks`。后果：`X-Correlation-ID` 不透传（T5.3 仅 Server 侧闭环）、`X-Frame-Options` 等缺（R18 P1）、`Serilog` 无请求日志、`Swagger` 需独立 404 兜底但未配、`FallbackPolicy` 无故本地 `[AllowAnonymous]` 端点暴露风险 |
| **初始化** | `UnifiedApplicationInitialization.InitializeAllApplicationServices`: `InitializeDatabaseAsync`（Test 环境跳过，否则 `DatabaseInitializationService.InitializeDatabaseAsync` + `GetDatabaseInfo`）→ `InitializeConfigurationServices` → `LogApplicationStartup`；`Program` 另 `SeedRolesAndAdmin` 在 `CreateScope` 中 | `LocalWebApiProgram.InitializeDatabaseAsync`: `db.Database.MigrateAsync() + IdentitySeedData.SeedRolesAndAdminAsync + LocalWebApiSeedData.SeedAsync`（直调 `MigrateAsync`，无 `DatabaseInitializationService` 超时/错误分级）| Local 的 `MigrateAsync` 与 Server 的 `DatabaseInitializationService` 在同库单迁移下等效，但错误处理与 `Test` 跳过逻辑不同 |
| **测试基类** | （Server 侧 `WebApplicationFactory<Program>` 未在本次审查范围，但 `Server` 的 `Program` 位于全局命名空间以兼容 `WebApplicationFactory`）| `LocalApiTestBase`：`LocalWebApiProgram.CreateBuilder() + Configuration[ConnectionStrings:DefaultConnection/Jwt:SecretKey/DefaultPasswords] + WebHost.ConfigureKestrel(Loopback,0) + CreateApplication(builder,conn) + StartAsync + EnsureCreatedAsync + SeedRolesAndAdmin + EnsureRoleUsers(admin/doctor/receptionist) + SeedAsync`；`LocalWebApiControllerTestBase`：**手工** `WebApplication.CreateBuilder(args:"--no-launch-settings") + AddDbContext(AppDbContext) + AddOptions(Database/Jwt/Security/Login IsLocal=true) + AddIdentityModule(仅1模块) + AddMediatR(LocalRefreshTokenCommand) + AddControllers(仅 HealthController Part, Json 仅 CaseInsensitive) + MvcOptions.SuppressImplicitRequired + AddIdentity(RequireDigit=true/Lockout=MaxValue) + LocalJwtConfig + DefaultPasswordOptions + Build→UseAuthentication/UseAuthorization/MapControllers → EnsureCreatedAsync + SeedRolesAndAdmin + EnsureAdminUser + SeedAsync` | **关键二次漂移**：`LocalApiTestBase` 对齐生产（`CreateApplication`），`LocalWebApiControllerTestBase` 自建最小宿主，缺 5 模块 + 5 基础设施（MemoryCache/OutputCache/CacheInvalidation/SignalR/Health）+ JSON/Security 细节 + Identity 策略不一致，致 70+ 控制器的 `Unable to resolve service` 500 |

---

## 方案评估

> 结合 `design-optimization-plan.md` 22.5d / 6 Sprint 节奏、`docs/00-governance/tech-debt.md` TD-003 已标记 P1、以及 `ADR-0010/0023` LocalWebAPI 白名单（P07 唯一例外允许引用 6 模块）的约束。

| 方案 | 描述 | 可行性 | 改动量 | 风险 | 防复发 | 备注 |
|------|------|--------|--------|------|--------|------|
| **A** | **手工修测试基类对齐生产**：`LocalWebApiControllerTestBase.InitializeAsync` 改为 `LocalWebApiProgram.CreateApplication`（或 `WebApplicationFactory<LocalWebApiProgram>`）+ `InitializeDatabaseAsync`，删除手工 `AddIdentityModule` 单模块 + `AddControllers` 精简等，保留 `Kestrel Loopback:0` 随机端口与 `EnsureCreated/EnsureDeleted` 隔离 | **高**（已有 `LocalApiTestBase` 正确先例，`CreateApplication` 已抽 `ContentRootPath=AppContext.BaseDirectory` 避 `IDX10703`）| 小（~1 文件，`LocalWebApiControllerTestBase.cs` 80 行→30 行，删 6 处手工注册，改 2 行 `CreateApplication` 调用）| 低（测试侧，无生产代码；`MigrateAsync` 与 `EnsureCreatedAsync` 在 LocalDB 上等效，但需统一为 `MigrateAsync` 以与生产一致，避免 `EnsureCreated` 绕过迁移历史）| 中（修 500 但不解生产侧 Local/Server drift，未来新增模块仍可能漏在手工处）| 止血最快，下次 Sprint 即可 `162→~90`（去 500，剩 STA）|
| **A′** | **A + Server 侧小补齐**（推荐短期）：A 的测试修复 + 生产侧 `LocalWebApiProgram` 补 `LybtJsonOptions/LybtJsonContext/ReferenceHandler/ApiBehaviorOptions` 与 Server 对齐，`AddHealthChecks/AddHostedService` 可暂不补（Local 无 K8s 探针需求） | 高 | 中（测试 1 文件 + 生产 `LocalWebApiProgram.cs` 10 行 Json/Validation 对齐）| 低 | 中+ | 在 A 上多 0.2d，使 `HealthController/Validation` 契约一致，防“测试过但生产 JSON 大小写不一致”二次 500 |
| **C** | **抽共享注册扩展**：新建 `LYBT.Shared.Hosting`（或 `LYBT.Infrastructure.Hosting`）`SharedModuleRegistration.AddBusinessModules(services, config)` 供 `ServiceCollectionExtensions.RegisterBusinessModules` 与 `LocalWebApiProgram.CreateApplication` 同调（6 模块 + SPI），`AddLybtLogging/AddMediatR/AddIdentity` 等亦可抽 `SharedHostingExtensions` | 高（`AddModuleDbContext` 已抽 `ModuleDbContextExtensions` 为先例，模式已验证）| 中（新增 1 项目或 1 文件 + 2 处调用点替换，~100 行；需 `InternalsVisibleTo` 处理 `LYBT.Module.*` 的 `internal` 控制器）| 低-中（生产双宿主同调，改动面 2 文件，但需回归 `Server` 与 `Local` 的 `LoginOptions.IsLocal` 差异化配置（`Configure(o=>IsLocal=true)`）保留）| 高（新增模块只需在共享扩展加 1 行，双宿主自动对齐）| 真正收敛 `Infrastructure` 层 drift，比 A 多 0.5d，但一劳永逸 |
| **D** | **统一宿主抽象（终局）**：C 的基础上再抽 `SharedWebHostBuilder`：`SharedHost.CreateBuilder(args, connectionString, isLocal)` 产 `WebApplicationBuilder` 并内置 `AddLybtLogging/AddDbContext/AddBusinessModules/AddAuthentication(AddIdentity+Jwt/LocalJwt)/AddControllers(Json+Validation)/UseMiddleware(按环境开关 Swagger/Correlation/Security)`，`Program` 与 `LocalWebApiProgram` 均为 `SharedHost` 的薄包装（仅 `UseUrls/ContentRootPath/Seed` 差异），测试侧统一为 `WebApplicationFactory<Program>` 或 `WebApplicationFactory<SharedHost>` 单一基类，`LocalApiTestBase`/`LocalWebApiControllerTestBase` 合并 | 高（`UnifiedMiddlewareConfiguration` 与 `DatabaseServiceCollectionExtensions` 已拆 9 小扩展，具备抽共享条件；`Global` 命名空间 `Program` 需保留 `WebApplicationFactory` 兼容）| 大（新增 1-2 文件 + 2 `Program` 瘦身 + 1 测试基类合并，`LocalWebApiProgram.cs` 230 行→80 行，`Program.cs` 700 行→150 行，约 300 行搬运；需 `AddHsts/AddDataProtection` 等仅 Server 开关化）| 中（搬运期 `Build` 顺序敏感，`AddIdentity` 必须先于 `RegisterAuthenticationServices` 的断言 `20d271961` 需保留；`Test` 环境 `Mutex` 与 `Bootstrap Logger` 在 `SharedHost` 中需条件化）| **最高**（单点 `SharedHost`，未来 `AddModule` 必然双宿主生效；测试单一 `Factory` 消灭 `CreateApplication` vs 手工双轨）| 与 `ADR-0017` 同库单迁移、`ADR-0022` JSON 字符串化等已统一项同向，符合“模块自治、双轨真共享（ADR-0010）”演进，投入 1-1.5d，收益覆盖 TD-003/005 与后续 `R18/R19` 管道顺序问题 |

**可行性排序**：A(小) > A′ > C > D（D 需搬运但无技术阻断）；**防复发排序**：D > C > A′ > A；**风险排序**：A < A′ < C < D（D 搬运期需双宿主回归）。

---

## 推荐方案

**短期（本 Sprint 内，0.5d）必做**：**A′ — 先止血 `LocalWebApiControllerTestBase`，再补 Local `Json/Validation` 对齐**

- 理由：162 中的 500 占比可量化（`Herbs/Patients/Formulas/MedicalCases/Registrations/Reports` 6 控制器 × 5 操作 × 4 角色 ≈ 120 用例），修测试侧 1 文件即可 `162→~40-50`（剩 STA/WPF + 少量 `SysAdmin` 边界），符合 `tech-debt.md` 度量“`build 0/0 + arch 91/91` 为门禁，162 不阻塞发版”但应止血以使 `TD-003` 可度量。
- 不选纯 A：Local 生产侧 `Json` 已埋 `PropertyNamingPolicy` 不一致（Server `LybtJsonOptions` vs Local `JsonStringEnumConverter` 单一），测试过但生产大小写不一致仍可能 500，需顺手对齐 10 行。

**中期（下 Sprint，0.8d）**：**C — 抽 `SharedModuleRegistration`**

- 将 `ServiceCollectionExtensions.RegisterBusinessModules` 的 6 行 `AddXxxModule` + `AddSpiRegistries` 抽为 `LYBT.Infrastructure.Hosting.SharedModuleRegistration.AddSharedModules(services, config)`，`LocalWebApiProgram` 与 `ServiceCollectionExtensions` 同调；`LoginOptions.IsLocal` 差异化保留在各自 `Configure` 中。此步后 `LocalWebApiControllerTestBase` 的“缺模块”类 drift 永久消失。

**长期（下下 Sprint，1.2d）**：**D — 统一 `SharedHost`**

- 在 C 基础上抽 `SharedHostingExtensions.AddSharedInfrastructure/AddSharedAuth/AddSharedControllers` 与 `SharedMiddleware.UseSharedPipeline(app, isLocal)`，`Program` 与 `LocalWebApiProgram` 仅留 `UseUrls/ContentRootPath/Seed` 薄层；测试侧合并 `LocalApiTestBase`/`LocalWebApiControllerTestBase` 为 `WebApplicationFactory<Program>` 单基类（`ICollectionFixture` 隔离 LocalDB `LYBTZYZS_LocalApi_Guid`）。与 `design-optimization-plan.md` 22.5d 6 Sprint 节奏不冲突，可并入 `tech-debt.md` TD-003 的 Phase2。

**否决**：纯“`LocalWebAPI` 保持手工精简、测试侧各自手工对齐” — 违背 `ADR-0010` 双轨真共享初衷，且 `UnifiedMiddlewareConfiguration` 已证明 9 步管线可共享，局部精简收益 < 维护成本。

---

## 实施计划（按推荐 D，拆 3 步；每步可独立发版）

### 步 1：止血 — 修 `LocalWebApiControllerTestBase`（0.3d，P1）

- **文件**：`tests/LYBT.Tests.Desktop/Integration/LocalWebAPI/LocalWebApiControllerTestBase.cs`
- **改动**：
  1. `InitializeAsync` 首 40 行手工 `WebApplication.CreateBuilder(args:"--no-launch-settings") + AddDbContext + AddOptions(Database/Jwt/Security/Login) + AddIdentityModule + AddMediatR + AddControllers(AddApplicationPart Health) + MvcOptions + AddIdentity + LocalJwtConfig + DefaultPasswordOptions` **替换为**：
     ```csharp
     var builder = LocalWebApiProgram.CreateBuilder();
     builder.Configuration["ConnectionStrings:DefaultConnection"] = _connectionString;
     builder.Configuration["Jwt:SecretKey"] = TestJwtSecret;
     builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 0));
     _app = LocalWebApiProgram.CreateApplication(builder, _connectionString);
     ```
  2. 保留 `HttpClient BaseAddress` 端口解析、`EnsureCreatedAsync + SeedRolesAndAdmin + EnsureAdminUser + SeedAsync`（与 `LocalApiTestBase` 一致，`EnsureCreatedAsync` 改 `MigrateAsync` 以与 `LocalWebApiProgram.InitializeDatabaseAsync` 一致，避免绕过迁移历史）。
  3. 删 `using LYBT.LocalWebAPI.Data` 等 6 处仅手工注册用 `using`。
- **验证**：`dotnet test tests/LYBT.Tests.Desktop --filter LocalWebAPI --no-build` 500 清零；`dotnet build --no-incremental` 0/0；`arch 91/91` 不变（测试侧）。
- **可发布**：纯测试文件，不影响 `Server`/`Local` 生产。

### 步 2：收敛生产侧 `LocalWebAPI` Json/Validation（0.2d，P2）

- **文件**：`src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs`
- **改动**：
  1. `AddControllers().AddJsonOptions` 补 `LybtJsonOptions` 绑定与 `LybtJsonContext.Default`、`ReferenceHandler.IgnoreCycles`、`PropertyNameCaseInsensitive`、`Encoder` 等与 `ServiceCollectionExtensions.RegisterControllerServices` 对齐（抽 `SharedJsonOptionsExtensions.AddLybtJsonOptions` 供双宿主复用，或直接内联 15 行）。
  2. `Configure<ApiBehaviorOptions>` 加与 Server 同款 `InvalidModelState → ApiResponse.CreateFail` 工厂（使 `AdminLocalTests_US-PAT-003` 等 422/400 契约一致）。
  3. `AddIdentity` 密码选项改 `PasswordPolicyValidator.Policy`（与 Server `Program.cs:122` 一致），`Lockout` 改 `AllowedForNewUsers=false / MaxFailedAccessAttempts=5` 与 `LoginOptions.LockoutEnabled` 联动。
- **验证**：`LocalApiTestBase` 的 `PostAsJsonAsync("/api/v1/patients", input) → 201` 与 `GetAsync("/api/v1/herbs") → 200` 在 Local 侧手工 `curl -H "Authorization: Bearer …"` 复验 `success:true`；`build 0/0`。
- **可发布**：`LocalWebAPI` 单进程内嵌，改动不触 `Server` 管线。

### 步 3：抽共享注册 + 统一宿主（0.8d + 1.2d，分两 Commit，P2）

- **新增**：`src/Shared/LYBT.Shared.Hosting/SharedModuleRegistration.cs`（或 `src/Server/Core/LYBT.Infrastructure/Hosting/SharedHost.cs`）
  ```csharp
  public static IServiceCollection AddSharedBusinessModules(this IServiceCollection s, IConfiguration c)
    => s.AddIdentityModule(c).AddPatientsModule(c).AddCatalogModule(c)
         .AddMedicalCaseModule(c).AddRegistrationModule(c).AddReportsModule(c).AddSpiRegistries();
  ```
  `SharedInfrastructure.AddSharedCache/AddSharedHealth`、`SharedAuth.AddSharedJwt`（参数 `isLocal` 分支 `JwtOptions` vs `LocalJwtOptions` + 是否 `FallbackPolicy`）、`SharedControllers.AddSharedJson` 等按需拆。
- **替换**：
  - `ServiceCollectionExtensions.RegisterBusinessModules` → `services.AddSharedBusinessModules(configuration);`
  - `LocalWebApiProgram.CreateApplication` 的 6 行 `AddXxxModule` → 同 `AddSharedBusinessModules`
  - `LocalWebApiProgram.cs` 的 `AddMemoryCache/AddOutputCache/CacheInvalidation/AddSignalR` 收敛为 `AddSharedInfrastructure`
- **测试合并**：删 `LocalWebApiControllerTestBase`，`LocalApiTestBase` 更名为 `LocalWebApiTestBase` 并作为 `ICollectionFixture<WebApplicationFactory<LocalWebApiProgram>>` 单一基类，`Admin/Doctor/Receptionist/SysadminLocalTests` 4 文件仅改继承；`LocalWebAPI` 的 `LocalWebApiControllerTestBase` 遗留的 `EnsureAdminUserAsync` 合入 `EnsureRoleUsersAsync`（已在 `LocalApiTestBase` 存在）。
- **验证**：`dotnet build --no-incremental` 0/0；`arch 91/91`（`LocalWebAPI` 白名单 `0023` 豁免仍有效）；`dotnet test tests/LYBT.Tests.Desktop --filter LocalApi --no-build` 全绿；`LocalWebAPI` 进程内 `http://127.0.0.1:5290/api/v1/herbs` 手工探活 `success:true`。
- **可发布**：生产双宿主同调，测试单一 Factory；`AddIdentity` 顺序断言（`AuthenticationServiceCollectionExtensions.cs:27`）保留，防 `302` 回归。

**里程碑**：步 1 后 `TD-003` 看板 `162→~50`（去 500，剩 STA）；步 3 后 `LocalWebApiProgram.cs` 230→80 行，`Program.cs` 700→150 行搬运完成，`tech-debt.md` TD-003 可标 `✅ 完成`，`phase2-migration-sequencing.md` 依赖图补 `SharedHost` 节点，`13-project-master-plan.md §十三` 健康度维持 `A-`。

---

## 附录：完整阅读清单（本次审查已逐行阅读）

- `src/Server/Services/LYBT.WebAPI/Program.cs`（700 行，含 `Mutex`/`Bootstrap Logger`/`AddIdentity` 顺序注 `325-330`/`ValidateDefaultPasswordConfiguration`/`EnsureEnvironmentConfigFiles`）
- `src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs`（9 步注册，`RegisterBusinessModules` 6 模块 + SPI）
- `src/Server/Services/LYBT.WebAPI/Extensions/DatabaseServiceCollectionExtensions.cs`（`AddDbContext` 重试/超时/`MigrationsAssembly`）
- `src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs`（`JwtBearer` + `OnTokenValidated` 禁用用户 + `Default/FallbackPolicy` + 8 策略，`AddIdentity` 断言 `T2.4`）
- `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`（14 中间件，`UseExceptionHandler → UseSecurityHeaders → … → UseAuthorization → MapHealthChecks`）
- `src/Server/Core/LYBT.Infrastructure/Data/ModuleDbContextExtensions.cs`（`AddModuleDbContext<T>` 统一）
- `src/Server/Modules/LYBT.Module.Identity/IdentityModule.cs` 等 6 模块（各 `AddModuleDbContext + AddScoped Repository/Service + AddMediatR + AddValidators`）
- `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs`（230 行，`CreateBuilder` 显式 `ContentRootPath` 防 `IDX10703`，`CreateApplication` 14 步，`InitializeDatabaseAsync` `Migrate+Seed`）
- `src/Client/Desktop/LocalWebAPI/Program.cs`（`LocalWebApiHost.Main` `UseUrls 127.0.0.1:5290`，`CreateApplication` 薄包装）
- `src/Client/Desktop/LocalWebAPI/Controllers/*` 13 文件（`AuthController` 单独 vs Server `UsersController` 合并，`Herbs/Patients` 等依赖 `ICatalogQueryService/IPatientService`）
- `src/Client/Desktop/LocalWebAPI/Auth/LocalJwtConfig.cs`（`ValidateIssuer=false/ValidateAudience=false/365d` vs Server `ValidateIssuer=true/ValidateAudience=true/480m`）
- `tests/LYBT.Tests.Desktop/Integration/LocalApi/LocalApiTestBase.cs`（`CreateApplication` 正轨，`EnsureRoleUsers` 4 角色，`EnsureCreatedAsync`）
- `tests/LYBT.Tests.Desktop/Integration/LocalApi/AdminLocalTests.cs`（抽样 7 用例 `GetPatients/CreatePatient/UpdatePatient/GetHerbs/CreateHerb/GetFormulas/GetRegistrations` 均 `success:true` 预期）
- `tests/LYBT.Tests.Desktop/Integration/LocalWebAPI/LocalWebApiControllerTestBase.cs`（手工 `AddIdentityModule` 单模块 + `AddMediatR` 单 `LocalRefreshTokenCommand` + `AddControllers(仅 Health Part)` + `AddIdentity` 策略不一致，为 500 主因）

*本次审查纯只读，未修改任何代码；所有推导均基于上述文件逐行内容。*
