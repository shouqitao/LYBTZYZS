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
  → LocalWebAPI (embedded Kestrel, port 5300)
    → Server/Core/LYBT.Entities        (domain entities)
    → Server/Core/LYBT.Infrastructure  (AppDbContext, BaseRepository)
    → Server/Modules/LYBT.Module.Auth     (IAuthService)
    → Server/Modules/LYBT.Module.Users    (IUserService)
    → Server/Modules/LYBT.Module.Patients (IPatientService)
    → Server/Modules/LYBT.Module.Herbs    (IHerbService)
    → Server/Modules/LYBT.Module.Formulas (IFormulaService)
    → Server/Modules/LYBT.Module.MedicalCases (IMedicalCaseFacade)
    → Server/Modules/LYBT.Module.Registration (IRegistrationService)
    → Server/Modules/LYBT.Module.Reports   (IReportsService)
```

### 变更协议

如需新增/删除 Server Module 引用：
1. 更新 ADR-0010 的引用列表
2. 更新本文件的依赖图
3. 确保 P21 审计测试通过（`P21_LocalWebAPI_ServerModule_References_Match_ADR0010`）

## Key Files

| File | Description |
|------|-------------|
| `LocalWebApiProgram.cs` | Entry point: AppDbContext + IHttpContextAccessor + 8 AddXxxModule() registrations + LocalJwtConfig |
| `LYBT.LocalWebAPI.csproj` | ASP.NET Core SDK; references Server Core + 8 Server Modules |
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
| `Controllers/` | 10 controllers — 7 use Service layer, 3 use DbContext directly |
| `Data/` | SeedData only (LocalWebApiDbContext deleted — uses AppDbContext) |

## Controllers

| Controller | Service Injected | Notes |
|------------|-----------------|-------|
| AuthController | IAuthService + IAutoLoginService | Hybrid: Service verification + local JWT |
| UsersController | IUserService | Full CRUD + batch + password reset |
| PatientsController | IPatientService + IPatientImportExportService | Full CRUD + import/export |
| HerbsController | IHerbService | Full CRUD + batch + reference check |
| FormulasController | IFormulaService + IFormulaImportExportService | Full CRUD + clone + validation |
| MedicalCasesController | IMedicalCaseFacade + IPermissionService | 22 endpoints via Facade |
| RegistrationsController | IRegistrationService | CRUD + queue + quick-visit |
| ConfigurationController | (none — in-memory store) | Key/value config, no business logic |
| HealthController | (none — DB connectivity) | CanConnectAsync only |
| DiagnosticsController | (none — LoggingLevelManager) | Log level management |

## For AI Agents

- Controllers use Service layer — same as Remote WebAPI. Do NOT inject DbContext directly (except AuthController for local JWT, HealthController for connectivity, DiagnosticsController for log queries).
- `LocalWebApiDbContext` is DELETED — uses `AppDbContext` from `LYBT.Infrastructure`.
- All 8 Server Module DI registrations are in `LocalWebApiProgram.CreateApplication()`.
- Architecture test `P21` (LocalWebAPI ↛ Server modules) is SKIPPED — intentionally unified.

## Dependencies

### Internal
- `LYBT.Infrastructure` — AppDbContext, BaseRepository, BaseApiController
- `LYBT.Module.*` — All 8 server modules (Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration, Sync)
- `LYBT.Entities` — Domain entities
- `LYBT.Shared.Models` — DTOs and contracts
- `LYBT.Desktop.Contracts` — Desktop interface definitions

### External
- `Microsoft.EntityFrameworkCore.SqlServer` — SQL Server provider (LocalDB)
- `Microsoft.AspNetCore.Authentication.JwtBearer` — JWT auth
