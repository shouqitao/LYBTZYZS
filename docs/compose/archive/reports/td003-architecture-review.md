# TD-003 架构审查报告 — Server / LocalWebAPI 一致性（已归档）

> **归档说明**: 本报告已于 2026-09-17 归档。原始文件路径: `docs/compose/reports/td003-architecture-review.md`
> **归档原因**: TD-003 已修复（`SharedHost` 统一宿主 + 基类合并），结论已落地为 ADR `adr-shared-host-abstraction.md`
> **原始日期**: 2026-08-21 | **方式**: 纯只读

## 归档内容摘要

### 摘要

- **TD-003 现象**：`tech-debt.md` 记 Desktop 162 失败（STA/WPF 线程 + SysAdmin 登录，`tech-debt.md:12`），实为两类基类并存 + Server/LocalWebAPI 注册漂移共同导致。单纯"STA 容器化"描述掩盖了 **500 真根因在测试侧 DI 缺失**。
- **真根因**：`LocalWebApiControllerTestBase` 手工 `WebApplicationBuilder` 仅注册 `IdentityModule` 单模块（`LocalWebApiControllerTestBase.cs:94`），缺 `Patients/Catalog/MedicalCases/Registration/Reports` 5 模块及其 `ICatalogQueryService`/`IPatientService` 等，命中 `/api/v1/herbs|patients|formulas|medicalcases` 即 `InvalidOperationException: Unable to resolve service` → 中间件无 `UseExceptionHandler` 兜底直接 500。而 `LocalApiTestBase` 走 `LocalWebApiProgram.CreateApplication`（6 模块全量）故 `AdminLocalTests` 7 用例应过（`LocalApiTestBase.cs:63`）。
- **架构漂移**：Server `RegisterAllApplicationServices`（9 步 + `AddLybtLogging/AddSignalR/HealthChecks/DataProtection/Hsts/ResponseCompression/Cors/RateLimiting`） vs Local `CreateApplication`（14 步精简，无 `ResponseCaching` 细节、`AddHealthChecks/HostedService`、`ExceptionHandler/ForwardedHeaders/CorrelationId/SecurityHeaders/Serilog` 等完整管线）。测试侧手工基类进一步二次漂移。
- **推荐**：**方案 D（统一宿主抽象）** 为终局：抽 `SharedHostRegistration` 供 Server/Local 共用 + 单一 `WebApplicationFactory` 测试基类；短期先以 **方案 A′（修测试基类）** 止血，消除 500 后再收敛生产侧 drift。详见 §推荐 与 §实施计划。

### 根因分析

#### 1. 500 的直接原因 — 读代码推导，非猜测

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
```

## 后续替代文档

- **统一宿主抽象 ADR**: [`docs/03-architecture/decisions/adr-shared-host-abstraction.md`](../../03-architecture/decisions/adr-shared-host-abstraction.md)
- **测试指南**: [`docs/05-development/04-testing.md`](../../05-development/04-testing.md)
- **技术债看板**: [`docs/00-governance/tech-debt.md`](../../00-governance/tech-debt.md)（TD-003 已标记完成）
