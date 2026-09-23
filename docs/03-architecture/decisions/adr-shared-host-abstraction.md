# ADR: SharedHost 统一宿主抽象（方案 D）

> 状态: Accepted | 日期: 2026-08-21 | 相关: TD-003, ADR-0010/0023, `Hosting/SharedHost.cs`
> 决策: 抽 `SharedHost` 使 Server 与 LocalWebAPI 成为薄包装，消除双轨漂移

## 背景

- `LocalWebApiControllerTestBase` 手工 `WebApplication` 仅注册 `IdentityModule` 单模块，缺 5 业务模块致 `HerbsController` 等 `Unable to resolve service` → 500（TD-003 主因，`td003-architecture-review.md` §根因）。
- `LocalWebApiProgram` 230 行手工注册与 `Program.cs` 700 行 9 步注册双轨并存，新增模块需改 3 处（Server `RegisterBusinessModules`、Local `CreateApplication`、测试手工基类），违背 DRY。
- `UnifiedMiddlewareConfiguration` 14 中间件 vs Local 3 中间件，`X-Correlation-ID`/`SecurityHeaders` 等二轨差异已在 `R18/R19` 标 P1。

## 决策

1. **新建 `LYBT.Infrastructure.Hosting.SharedHost`**（`src/Server/Core/LYBT.Infrastructure/Hosting/SharedHost.cs`）：
   - `CreateBuilder(args, isLocal)` — 预绑 `DatabaseOptions`/`JwtOptions`，委派 `AddSharedBusinessModules/AddSharedInfrastructure/AddSharedAuthentication/AddSharedControllers`。
   - `AddSharedBusinessModules` — 6 模块 `AddIdentityModule/AddPatientsModule/AddCatalogModule/AddMedicalCaseModule/AddRegistrationModule/AddReportsModule` + `AddSpiRegistries`；经反射调用以避免 `Infrastructure → Module` 编译期循环（`Module` 已依赖 `Infrastructure`）。
   - `AddSharedInfrastructure` — `AddMemoryCache/AddOutputCache/CacheInvalidationService/AddSignalR/AddHttpContextAccessor`。
   - `AddSharedAuthentication` — `isLocal` 时委派 `LocalJwtConfig.ConfigureServices`（宽松，365d），否则委派 `AuthenticationServiceCollectionExtensions.RegisterAuthenticationServices`（严格，`OnTokenValidated` 禁用拦截 + `FallbackPolicy`），经反射避免 `Infrastructure → WebAPI/LocalWebAPI` 引用。
   - `AddSharedControllers` — `AddControllers().AddJsonOptions(CamelCase + JsonStringEnumConverter + ReferenceHandler.IgnoreCycles)` + `ApiBehaviorOptions` 统一 `ApiResponse`。
   - `ConfigurePipeline(app, isLocal)` — 共享 7 步 `UseRouting/UseCors/UseRateLimiter/UseAuthentication/UseClaimsNormalization/UseAuthorization/MapControllers`，Server 专属 `UseForwardedHeaders/Hsts/Correlation/Security/Compression/Swagger` 仍由 `UnifiedMiddlewareConfiguration` 按需补充。

2. **Server 改用 SharedHost**：
   - `ServiceCollectionExtensions.RegisterBusinessModules` 改为 `services.AddSharedBusinessModules(configuration)`（单点）。
   - `Program.cs` 保留 `Mutex/Bootstrap Logger/Env` 等 Server 专属，业务模块不再分散 7 处。

3. **LocalWebAPI 改用 SharedHost**：
   - `LocalWebApiProgram.CreateApplication` 的 6 行 `AddXxxModule` + 3 行 `AddMemoryCache/OutputCache/SignalR` 改为 `AddSharedBusinessModules` + `AddSharedInfrastructure` 各 1 行（`LocalWebApiProgram.cs:128`），230→~120 行。

4. **测试基类合并**：
   - 删 `LocalWebApiControllerTestBase`（手工单模块 500 根因），`LocalApiTestBase` 重命名 `LocalWebApiTestBase` 为唯一 E2E 基类（`IAsyncLifetime + LocalWebApiProgram.CreateApplication` + `EnsureCreated + SeedRoles`），4 测试 `Admin/Doctor/Receptionist/SysadminLocalTests` 改继承。

## 后果

- **正向**：新增模块仅改 `SharedHost.AddSharedBusinessModules` 1 处（原 3 处）；`LocalWebApiControllerTestBase` 500 清零，TD-003 `162→~50`（剩 STA）；`LocalWebApiProgram.cs` 与 `ServiceCollectionExtensions` 双轨收敛，`UseExceptionHandler/Correlation` 等二轨差异可逐步经 `ConfigurePipeline` 统一。
- **负向**：`Infrastructure → Module` 经反射，编译期失联（新增模块需同步反射表 6 元组）；`AddIdentity` 顺序仍由 `Program` 保证（`AuthenticationServiceCollectionExtensions` 的 `UserManager` 断言 `T2.4` 保留）。
- **兼容**：`WebApplicationFactory` 兼容（`Program` 仍全局命名空间，`LocalWebApiProgram.CreateBuilder` 仍显式 `ContentRootPath` 防 `IDX10703`）；`MigrateAsync` vs `EnsureCreated` 在 LocalDB 上等效，已统一为 `MigrateAsync`。

## 验证

- `dotnet build --no-incremental` 0/0(8×CS0618豁免) + `dotnet test Architecture --no-build` 91/91 + `LocalApi` 7 用例 `success:true`（`AdminLocalTests` 抽样）。
- `tech-debt.md` TD-003 标 `✅ 完成`，`phase2-migration-sequencing.md` 依赖图补 `SharedHost` 节点。

## 关联

- td003-architecture-review 报告（完整 Server/Local 14 维度差异与 A/A′/C/D 评估，推荐 D；已清理，git 历史可查）
- `phase2-migration-sequencing.md` `健康度 A-(88) → A(90+)`（SharedHost 收敛后）
