<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-06-29 -->

# Modules (Server)

## Purpose
Business modules for the ASP.NET Core backend. Each module is a **self-contained vertical slice** with Domain, Application, and Infrastructure layers. Modules use **MediatR CQRS** (Commands/Queries/Handlers) for request processing and **domain events** for cross-module state propagation. Each module has its own DbContext for data isolation. Cross-module communication uses `ICrossModuleService` interfaces (synchronous queries) or domain events (async state changes).

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
- Each module follows **Domain / Application / Infrastructure** structure (see module template below).
- **Modules MUST NOT reference each other.** Use `ICrossModuleService` (SharedKernel) for synchronous cross-module queries, or domain events for async state changes.
- Each module registers its own **DbContext** (per-module data isolation) — never share `AppDbContext`.
- All DTOs are defined in `LYBT.Shared.Models`, not within modules.
- MediatR is registered per-module: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`.

### Module Structure Template
```
LYBT.Module.<Name>/
├── Domain/                    # Domain entities, value objects, domain events
│   ├── <Entity>.cs            # Rich domain model (IAggregateRoot)
│   └── Events/                # Domain events (IDomainEvent)
│       └── <Entity>CreatedEvent.cs
├── Application/               # Business logic layer (MediatR handlers)
│   ├── Commands/              # Write operations
│   │   ├── Create<Entity>Command.cs
│   │   └── Create<Entity>CommandHandler.cs
│   ├── Queries/               # Read operations
│   │   ├── Get<Entity>Query.cs
│   │   └── Get<Entity>QueryHandler.cs
│   ├── Validators/            # FluentValidation validators
│   └── Mappers/               # Riok.Mapperly mappers
├── Infrastructure/            # Data access layer
│   ├── <Module>DbContext.cs   # Module-scoped DbContext
│   └── <Entity>Repository.cs  # Repository implementation
├── Interfaces/                # Service interfaces (ICrossModuleService, etc.)
├── Controllers/               # Minimal API controllers (delegate to MediatR)
└── <Name>Module.cs            # Static extension method for DI registration
```

### MediatR Registration Pattern
Each module registers MediatR handlers via its `<Name>Module.cs`:
```csharp
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Create<Entity>Command).Assembly));
```

### Repository Pattern
- Module-specific repositories extend `BaseRepository<T>` from Infrastructure
- Repositories are registered in `<Name>Module.cs` via `services.AddScoped<I<Entity>Repository, <Entity>Repository>()`
- Service layer MUST NOT inject `AppDbContext` directly — use repository interfaces

### Domain Events for State Changes
- Domain events implement `IDomainEvent : INotification` (MediatR)
- Events are raised in domain entity methods or command handlers
- Cross-module handlers subscribe via `INotificationHandler<TEvent>`
- Outbox pattern ensures reliable delivery (same-transaction write + async processing)

### Testing Requirements
- `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase"` — test a specific module
- All server tests use real SQL Server + Respawn for database reset, zero mocks.

### Common Patterns
- **Controller**: Minimal, dispatches to MediatR (ICommand/IQuery → Handler)
- **Command/Query Handler**: Business logic + validation + orchestrates Repository calls
- **Repository**: Extends `BaseRepository<T>`, adds domain-specific queries
- **Cross-module (sync)**: `ICrossModuleService` interface in SharedKernel, implemented in providing module
- **Cross-module (async)**: Domain events via `IDomainEvent` → `INotificationHandler`
- **Validators**: FluentValidation, registered per-module assembly scan

## Dependencies

### Internal
- [Core/](../Core/AGENTS.md) — `LYBT.SharedKernel`, `LYBT.Infrastructure`, `LYBT.Entities`
- [Shared/](../../Shared/AGENTS.md) — `LYBT.Shared.Models`, `LYBT.Shared.Validators`

### External
- ASP.NET Core 8 (controllers, DI)
- Entity Framework Core 8 (data access)
- MediatR (CQRS + domain events)
- FluentValidation (input validation)

<!-- MANUAL: -->
