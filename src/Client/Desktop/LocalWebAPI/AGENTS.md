<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-06-21 -->

# LYBT.LocalWebAPI

## Purpose

Embedded ASP.NET Core WebAPI that runs inside the Desktop client process for local/offline mode. Uses the **same Service/Repository layer** as Remote WebAPI (unified architecture since 2026-06-14). Controllers delegate to `I*Service` interfaces — zero parallel implementation.

## Architecture Decision

**[ADR-0010](../../../../docs/03-architecture/decisions/0010-localwebapi-unified-service-layer.md)**: LocalWebAPI 统一服务层 — 文档化跨层引用例外

LocalWebAPI 是 **Client → Server 唯一的跨层引用路径**。这是有意设计，不是遗漏。

### 依赖图

```
Desktop Shell
  → LocalWebAPI (embedded Kestrel, port 5300)
    → Shared/LYBT.Entities                          (domain entities — moved out of Server/Core)
    → Server/Core/LYBT.Infrastructure               (AppDbContext, BaseRepository)
    → Server/Modules/LYBT.Module.Identity           (IAuthService/IUserService — Auth+Users 合并)
    → Server/Modules/LYBT.Module.Catalog            (IHerbService/IFormulaService — Herbs+Formulas 合并)
    → Server/Modules/LYBT.Module.Patients           (IPatientService)
    → Server/Modules/LYBT.Module.MedicalCases       (IMedicalCaseCommandService/QueryService/StateService)
    → Server/Modules/LYBT.Module.Registrations      (IRegistrationService)
    → Server/Modules/LYBT.Module.Reports            (IReportsService)
```

### 变更协议

如需新增/删除 Server Module 引用：
1. 更新 ADR-0010 的引用列表
2. 更新本文件的依赖图
3. 确保 P21 审计测试通过（`P21_LocalWebAPI_ServerModule_References_Match_ADR0010`）

## Key Files

| File | Description |
|------|-------------|
| `LocalWebApiProgram.cs` | Entry point: AppDbContext + IHttpContextAccessor + 6 AddXxxModule() registrations + LocalJwtConfig |
| `LYBT.LocalWebAPI.csproj` | ASP.NET Core SDK; references Server Core + 6 Server Modules + Shared/LYBT.Entities |
| `Auth/LocalJwtConfig.cs` | Simplified JWT — 12-hour access token；支持 RefreshToken（`AuthController.RefreshToken` + `LocalRefreshTokenCommandHandler`） |
| `Data/LocalWebApiSeedData.cs` | Seed data initialization (accepts AppDbContext) |

## Architecture (Unified Service Layer)

```
Desktop VM → Repository → SwitchingApiClient → HttpClientApiClient
  → HTTP → LocalWebAPI Controller → I*Service → Repository → AppDbContext → LocalDB
```

Controllers inherit `BaseApiController` (from `LYBT.Infrastructure.Web`) and use:
- `HandleResult<T>()` — converts `Result<T>` to `ApiResponse<T>` HTTP response
- `Success<T>()` / `SuccessPaged<T>()` — success responses
- `GetCurrentUserId()` / `IsAdmin()` — JWT claims extraction for operator params

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `Auth/` | LocalJwtConfig (simplified JWT generation + refresh) |
| `Commands/` / `Handlers/` | MediatR CQRS Commands（本地登录/刷新） |
| `Controllers/` | 14 controllers — 与 Remote WebAPI 同构（见下表） |
| `Data/` | SeedData only (LocalWebApiDbContext deleted — uses AppDbContext) |

## Controllers

| Controller | Service Injected | Notes |
|------------|-----------------|-------|
| AuthController | IAuthService + IAutoLoginService | Hybrid: Service verification + local JWT；含 `RefreshToken` |
| UsersController | IUserService | Full CRUD + batch + password reset |
| PatientsController | IPatientService + IPatientImportExportService | Full CRUD + import/export |
| HerbsController | ICatalogQueryService&lt;HerbListDto, HerbDetailDto&gt; | 药材 CRUD/批量/引用检查（Catalog 模块，路由 `/api/v1/herbs`） |
| FormulasController | ICatalogQueryService&lt;FormulaListDto, FormulaDetailDto&gt; | 验方 CRUD/批量/引用检查/克隆（Catalog 模块，路由 `/api/v1/formulas`） |
| MedicalCasesController | IMedicalCaseCommandService/QueryService/StateService | CQRS via BaseMedicalCasesController |
| RegistrationsController | IRegistrationService | CRUD + queue + start-visit + cancel |
| ConfigurationController | (none — in-memory store) | Key/value config, no business logic |
| HealthController | IHealthCheckService | CanConnectAsync only |
| DiagnosticsController | (none — LoggingLevelManager) | Log level management |
| ReportsController | IReportsService | 只读报表查询（B-04）|
| DeployController | (none) | restart 确认（A-13）|
| DownloadController | (none) | 下载辅助端点 |
| SecurityAuditController | (none) | 安全审计日志查询 |

## For AI Agents

- Controllers use Service layer — same as Remote WebAPI. Do NOT inject DbContext directly (except AuthController for local JWT, HealthController for connectivity, DiagnosticsController for log queries).
- `LocalWebApiDbContext` is DELETED — uses `AppDbContext` from `LYBT.Infrastructure`.
- All 6 Server Module DI registrations are in `LocalWebApiProgram.CreateApplication()`.
- Architecture test `P21` (LocalWebAPI ↛ Server modules) is SKIPPED — intentionally unified.

## Dependencies

### Internal
- `LYBT.Infrastructure` — AppDbContext, BaseRepository, BaseApiController
- `LYBT.Module.*` — All 6 server modules (Identity, Catalog, Patients, MedicalCases, Registrations, Reports)
- `LYBT.Entities` — Domain entities（路径 `src/Shared/LYBT.Entities`，非 Server/Core）
- `LYBT.Shared.Models` — DTOs and contracts
- `LYBT.Desktop.Contracts` — Desktop interface definitions

### External
- `Microsoft.EntityFrameworkCore.SqlServer` — SQL Server provider (LocalDB)
- `Microsoft.AspNetCore.Authentication.JwtBearer` — JWT auth
