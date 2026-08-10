<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-06-21 -->

# LYBT.LocalWebAPI

## Purpose

Embedded ASP.NET Core WebAPI that runs inside the Desktop client process for local/offline mode. Uses the **same Service/Repository layer** as Remote WebAPI (unified architecture since 2026-06-14). Controllers delegate to `I*Service` interfaces — zero parallel implementation.

## Architecture Decision

**[ADR-0010](../../../docs/03-architecture/decisions/0010-localwebapi-unified-service-layer.md)**: LocalWebAPI 统一服务层 — 文档化跨层引用例外

LocalWebAPI 是 **Client → Server 唯一的跨层引用路径**。这是有意设计，不是遗漏。

### 依赖图

```
Desktop Shell
  → LocalWebAPI (embedded Kestrel, port 5290)
    → Server/Core/LYBT.Entities        (domain entities)
    → Server/Core/LYBT.Infrastructure  (AppDbContext, BaseRepository)
    → Server/Modules/LYBT.Module.Identity    (IAuthService/IUserService — Auth+Users 合并)
    → Server/Modules/LYBT.Module.Catalog     (IHerbService/IFormulaService — Herbs+Formulas 合并)
    → Server/Modules/LYBT.Module.Patients    (IPatientService)
    → Server/Modules/LYBT.Module.MedicalCases (IMedicalCaseFacade)
    → Server/Modules/LYBT.Module.Registrations (IRegistrationService)
    → Server/Modules/LYBT.Module.Reports     (IReportsService)
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
| `LYBT.LocalWebAPI.csproj` | ASP.NET Core SDK; references Server Core + 6 Server Modules |
| `Auth/LocalJwtConfig.cs` | Simplified JWT (1-year token, no refresh) |
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
| `Auth/` | LocalJwtConfig (simplified JWT generation) |
| `Commands/` | MediatR CQRS Commands（本地登录/刷新） |
| `Controllers/` | 11 controllers — 与 Remote WebAPI 同构 |
| `Data/` | SeedData only (LocalWebApiDbContext deleted — uses AppDbContext) |

## Controllers

| Controller | Service Injected | Notes |
|------------|-----------------|-------|
| AuthController | IAuthService + IAutoLoginService | Hybrid: Service verification + local JWT |
| UsersController | IUserService | Full CRUD + batch + password reset |
| PatientsController | IPatientService + IPatientImportExportService | Full CRUD + import/export |
| CatalogController | ICatalogQueryService | 药材+验方合并（2026-08 模块合并），CRUD + 批量 + 引用检查 + 克隆 |
| MedicalCasesController | IMedicalCaseCommandService/QueryService/StateService | 12 endpoints via Facade |
| RegistrationsController | IRegistrationService | CRUD + queue + quick-visit |
| ConfigurationController | (none — in-memory store) | Key/value config, no business logic |
| HealthController | IHealthCheckService | CanConnectAsync only |
| DiagnosticsController | (none — LoggingLevelManager) | Log level management |
| ReportsController | IReportsService | 只读报表查询（B-04）|
| DeployController | (none) | restart 确认（A-13）|

## For AI Agents

- Controllers use Service layer — same as Remote WebAPI. Do NOT inject DbContext directly (except AuthController for local JWT, HealthController for connectivity, DiagnosticsController for log queries).
- `LocalWebApiDbContext` is DELETED — uses `AppDbContext` from `LYBT.Infrastructure`.
- All 6 Server Module DI registrations are in `LocalWebApiProgram.CreateApplication()`.
- Architecture test `P21` (LocalWebAPI ↛ Server modules) is SKIPPED — intentionally unified.

## Dependencies

### Internal
- `LYBT.Infrastructure` — AppDbContext, BaseRepository, BaseApiController
- `LYBT.Module.*` — All 6 server modules (Identity, Catalog, Patients, MedicalCases, Registrations, Reports)
- `LYBT.Entities` — Domain entities
- `LYBT.Shared.Models` — DTOs and contracts
- `LYBT.Desktop.Contracts` — Desktop interface definitions

### External
- `Microsoft.EntityFrameworkCore.SqlServer` — SQL Server provider (LocalDB)
- `Microsoft.AspNetCore.Authentication.JwtBearer` — JWT auth
