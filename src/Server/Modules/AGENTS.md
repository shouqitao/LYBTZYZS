<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Modules (Server)

## Purpose
Business modules for the ASP.NET Core backend. Each module encapsulates a domain area with its own Controller, Service, and Repository layers. Modules are strictly isolated from each other — cross-module communication requires `ICrossModuleService` interfaces. MedicalCase uses a CQRS pattern with CommandHandler for complex operations.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Module.Auth/ | Authentication — JWT token generation, login/logout, password hashing |
| LYBT.Module.Users/ | User account management — CRUD, role assignment, profile |
| LYBT.Module.Patients/ | Patient management — CRUD, search, medical history |
| LYBT.Module.Herbs/ | Herb (TCM medicine) catalog — CRUD, categories, properties |
| LYBT.Module.Formula/ | Formula (empirical recipe) management — CRUD, herb composition |
| LYBT.Module.MedicalCase/ | Medical case (DDD aggregate) — consultations, prescriptions, CQRS commands |
| LYBT.Module.Registration/ | Patient registration and appointment scheduling |
| LYBT.Module.Sync/ | Data synchronization — local/remote mode data exchange |

## For AI Agents

### Working In This Directory
- Each module follows three-layer: Controller -> Service -> Repository.
- Modules MUST NOT reference each other. Use `ICrossModuleService` for cross-module needs.
- Service layer MUST NOT inject `AppDbContext` directly — use `IRepository<T>` (enforced by architecture test `P10_Services_Should_Not_Directly_Inject_AppDbContext`).
- MedicalCase module is special: uses CQRS with `CommandHandler` pattern instead of simple service layer.
- All DTOs are defined in `LYBT.Shared.Models`, not within modules.

### Testing Requirements
- `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase"` — test a specific module
- All server tests use real SQL Server + Respawn for database reset, zero mocks.

### Common Patterns
- **Controller**: Minimal, delegates to Service
- **Service**: Business logic, validation, orchestrates Repository calls
- **Repository**: Extends `BaseRepository<T>`, adds domain-specific queries
- **Cross-module**: `ICrossModuleService` interface in the consuming module, implemented in the providing module
- **CQRS (MedicalCase)**: `ICommandHandler<TCommand, TResult>` for complex write operations

## Dependencies

### Internal
- [Core/](../Core/AGENTS.md) — `LYBT.Entities`, `LYBT.Infrastructure`
- [Shared/](../../Shared/AGENTS.md) — `LYBT.Shared.Models`, `LYBT.Shared.Validators`

### External
- ASP.NET Core 8 (controllers, DI)
- Entity Framework Core 8 (data access)
- FluentValidation (input validation)

<!-- MANUAL: -->
