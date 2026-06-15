<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-06-14 -->

# LYBT.LocalWebAPI

## Purpose

Embedded ASP.NET Core WebAPI that runs inside the Desktop client process for local/offline mode. Uses the **same Service/Repository layer** as Remote WebAPI (unified architecture since 2026-06-14). Controllers delegate to `I*Service` interfaces — zero parallel implementation.

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
