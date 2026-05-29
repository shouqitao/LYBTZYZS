<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Server

## Purpose
ASP.NET Core WebAPI backend for the LYBTZYZS TCM clinic management system. Implements a three-layer architecture (Controller -> Service -> Repository -> DbContext) with domain entities, EF Core infrastructure, business modules (Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration, Sync), and the API entry point. MedicalCase uses a CQRS pattern with CommandHandler.

## Key Files
| File | Description |
|------|-------------|
| GlobalUsings.cs | Global using directives for the server project tree |
| README.md | Server project overview |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| [Core/](Core/AGENTS.md) | Core libraries — domain entities and EF Core infrastructure |
| [Modules/](Modules/AGENTS.md) | Business modules — Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration, Sync |
| [Services/](Services/AGENTS.md) | API entry point — ASP.NET Core WebAPI host |

## For AI Agents

### Working In This Directory
- Dependency direction: `Services(WebAPI) -> Modules -> Core(Infrastructure -> Entities)`
- Modules MUST NOT reference each other; cross-module communication via `ICrossModuleService` interfaces.
- Service layer MUST NOT directly inject `AppDbContext` — must use Repository interface (enforced by architecture test).
- All DTOs live in `Shared.Models`; entities live in `LYBT.Entities`.
- MedicalCase is the sole DDD aggregate root — Consultation and Prescription are internal entities with no independent repositories.

### Testing Requirements
- `dotnet test tests/LYBT.Tests.Server/` — ~1185 tests, real SQL Server + Respawn, zero mock
- `dotnet test tests/LYBT.Tests.Architecture/` — ~76 architecture guard tests

### Common Patterns
- **Three-layer**: Controller -> Service -> Repository -> DbContext
- **CQRS (MedicalCase)**: CommandHandler pattern for complex operations
- **Repository**: `BaseRepository<T>` (21 methods), `IRepository<T>` interface
- **Cross-module**: `ICrossModuleService` interfaces for inter-module communication

## Dependencies

### Internal
- [Shared/](../Shared/AGENTS.md) — `LYBT.Shared.Models`, `LYBT.Shared.Configuration`, `LYBT.Shared.ExceptionHandling`

### External
- ASP.NET Core 8
- Entity Framework Core 8
- SQL Server (via EF Core)

<!-- MANUAL: -->
