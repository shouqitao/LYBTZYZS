<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Module.Registration

## Purpose

Server-side registration (appointment/queue) module. Manages patient registration/queuing for clinic visits, supporting receptionist-created walk-in registrations and doctor-initiated registrations. Implements the three-layer pattern (Controller -> Service -> Repository) with cross-module access to Patient data via `IPatientCrossModuleService`.

## Key Files

| File | Description |
|------|-------------|
| `RegistrationModule.cs` | Static extension method `AddRegistrationModule()` for DI service registration |
| `LYBT.Module.Registration.csproj` | Project file; net8.0, Riok.Mapperly, EF Core; InternalsVisibleTo LYBT.Tests.Server |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `Interfaces/` | `IRegistrationRepository`, `IRegistrationService` -- contract definitions |
| `Services/` | `RegistrationService` -- business logic for registration CRUD and status transitions |
| `Repositories/` | `RegistrationRepository` -- data access via BaseRepository pattern |
| `Mapping/` | `RegistrationMapper` -- Riok.Mapperly compile-time entity-to-DTO mapping |

## For AI Agents

### Working In This Directory

- Follows standard three-layer architecture: `Controller (in WebAPI) -> Service -> Repository -> DbContext`.
- The controller lives in `LYBT.WebAPI/Controllers/RegistrationsController.cs`, not in this module.
- Uses `IPatientCrossModuleService` to access Patient data -- do NOT directly reference LYBT.Module.Patients.
- Mapper uses Riok.Mapperly (compile-time source generation), not AutoMapper runtime mapping.
- Registration supports multiple sources: Receptionist (walk-in), Doctor, Online (future).

### Testing Requirements

- Server integration tests: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Registration"`
- Unit tests cover service logic in `tests/LYBT.Tests.Server.Unit/` (if applicable).
- Tests use real SQL Server + Respawn, zero mock (Testing Trophy pattern).

### Common Patterns

- **Cross-module service** -- Uses `IPatientCrossModuleService` for patient data access (interface segregation).
- **BaseService<T>** -- Service extends `BaseService<RegistrationEntity>` for common logging/error handling.
- **Riok.Mapperly** -- Compile-time mapping via `RegistrationMapper` partial class.
- **Scoped DI** -- Repository and Service registered as Scoped lifetime.

## Dependencies

### Internal

- `LYBT.Infrastructure` -- BaseRepository, DbContext, BaseService
- `LYBT.Entities` -- Registration entity
- Cross-module: `IPatientCrossModuleService` (from Infrastructure.Services.CrossModule)

### External

- `Riok.Mapperly` -- Compile-time object mapping
- `Microsoft.EntityFrameworkCore` -- EF Core ORM

<!-- MANUAL: -->
