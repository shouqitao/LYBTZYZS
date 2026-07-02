<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-06-29 -->

# Server

## Purpose
ASP.NET Core WebAPI backend for the LYBTZYZS TCM clinic management system. Implements a **modular monolith** architecture with MediatR CQRS pattern. Each business module (Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration, Reports, Sync) is self-contained with its own Domain, Application, and Infrastructure layers. Cross-module communication uses domain events via SharedKernel. MedicalCase is the sole DDD aggregate root.

## Key Files
| File | Description |
|------|-------------|
| GlobalUsings.cs | Global using directives for the server project tree |
| README.md | Server project overview |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| [Core/](Core/AGENTS.md) | SharedKernel (domain events, entities, outbox), Infrastructure (AppDbContext, BaseRepository), Entities |
| [Modules/](Modules/AGENTS.md) | Business modules — each with Domain/Application/Infrastructure layers |
| [Services/](Services/AGENTS.md) | API entry point — ASP.NET Core WebAPI host |

## For AI Agents

### Working In This Directory
- Dependency direction: `Services(WebAPI) -> Modules -> Core(SharedKernel, Infrastructure, Entities)`
- **Modules MUST NOT reference each other**; cross-module communication via domain events (`IDomainEvent`) or `ICrossModuleService` interfaces in SharedKernel.
- Each module has its **own DbContext** (per-module data isolation) — never use the shared `AppDbContext` for module data.
- Service layer MUST NOT directly inject `AppDbContext` — must use Repository interface (enforced by architecture test).
- All DTOs live in `Shared.Models`; entities live in `LYBT.Entities` or module `Domain/` folders.
- MedicalCase is the sole DDD aggregate root — Consultation and Prescription are internal entities with no independent repositories.

### MediatR CQRS Pattern
- **Commands**: Write operations (Create, Update, Delete) → CommandHandler
- **Queries**: Read operations (Get, Search, List) → QueryHandler
- Each handler implements `IRequestHandler<TCommand, TResponse>` (MediatR)
- Domain events implement `IDomainEvent : INotification` (MediatR) for cross-module reaction
- Outbox pattern (`IOutboxService`) ensures reliable event delivery after transaction commit

### Testing Requirements
- `dotnet test tests/LYBT.Tests.Server/` — ~1185 tests, real SQL Server + Respawn, zero mock
- `dotnet test tests/LYBT.Tests.Architecture/` — ~76 architecture guard tests

### Common Patterns
- **Modular Monolith**: Each module is self-contained (Domain + Application + Infrastructure)
- **CQRS**: CommandHandler / QueryHandler via MediatR
- **Repository**: Module-specific repositories (e.g., `PatientRepository`, `HerbRepository`) + `BaseRepository<T>`
- **Domain Events**: `IDomainEvent` for state changes; `IDomainEventDispatcher` for dispatching
- **Outbox**: `IOutboxService` for reliable event delivery (same-transaction write + async processing)
- **Cross-module**: `ICrossModuleService` interfaces in SharedKernel for synchronous cross-module queries

## Dependencies

### Internal
- [Shared/](../Shared/AGENTS.md) — `LYBT.Shared.Models`, `LYBT.Shared.Configuration`, `LYBT.Shared.ExceptionHandling`

### External
- ASP.NET Core 8
- Entity Framework Core 8
- MediatR (CQRS + domain events)
- SQL Server (via EF Core)

<!-- MANUAL: -->
