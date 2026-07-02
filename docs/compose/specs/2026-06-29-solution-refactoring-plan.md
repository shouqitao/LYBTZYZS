# LYBTZYZS Solution Refactoring Plan — Official Architecture Reference

> Generated 2026-06-29  
> References: Microsoft eShopOnContainers/eShopOnWeb, ABP.IO Modular Monolith, Milan Jovanovic MMA Course, Ardalis Clean Architecture

---

## [S1] Official Architecture Sources

| Source | Pattern | Key Principles | Reference |
|--------|---------|----------------|-----------|
| Microsoft eShopOnWeb | Clean Architecture | Domain ← Application ← Infrastructure; Dependency Rule | github.com/dotnet-architecture/eShopOnWeb |
| Microsoft eShopOnContainers | Microservices + DDD | Service autonomy; own data store per service; event-driven communication | github.com/dotnet/eShop |
| ABP.IO | Modular Monolith | Module isolation; shared database with schema separation; integration events | abp.io/architecture/modular-monolith |
| Milan Jovanovic MMA | Modular Monolith | Module boundaries; data isolation; domain events; CQRS; outbox pattern | milanjovanovic.tech/modular-monolith-architecture |
| Ardalis Clean Architecture | Clean Architecture | Use cases at center; entities → use cases → interface adapters → frameworks | github.com/ardalis/CleanArchitecture |
| Jason Taylor Clean Architecture | Clean Architecture + CQRS | MediatR; FluentValidation; Mapster; Domain events | github.com/jasontaylordev/CleanArchitecture |

---

## [S2] Current vs Target Architecture

### Current Structure (40 projects)
```
LYBTZYZS.sln
├── src/Server/
│   ├── LYBT.Entities          ← Flat entities, no domain logic
│   ├── LYBT.Infrastructure    ← Shared DbContext, repositories, auth
│   ├── LYBT.Module.* (8)      ← Services + repositories per module
│   └── LYBT.WebAPI            ← Controllers
├── src/Client/Desktop/
│   ├── Desktop.Core (9)       ← Contracts, Foundation, Infrastructure
│   ├── Desktop.Modules (8)    ← MVVM per domain
│   ├── Desktop.Roles (4)      ← Role workspaces
│   ├── Shell                  ← Prism entry
│   └── LocalWebAPI            ← Embedded ASP.NET Core
├── src/Shared (8)             ← Primitives, Models, Config, etc.
└── tests (3)
```

### Target Structure (44 projects)
```
LYBTZYZS.sln
├── src/Server/
│   ├── LYBT.Domain/           ← NEW: Entities + Value Objects + Domain Events + Domain Services
│   ├── LYBT.Application/      ← NEW: Use Cases (MediatR Commands/Queries) + Validators
│   ├── LYBT.Infrastructure/   ← REFACTORED: Only persistence + external services
│   ├── LYBT.SharedKernel/     ← NEW: Event contracts + shared primitives
│   ├── LYBT.Module.* (8)      ← REFACTORED: Each with Domain/Application/Infrastructure folders
│   └── LYBT.WebAPI            ← Same controllers,薄 wrapper
├── src/Client/Desktop/        ← Same structure, enhanced
├── src/Shared (8)             ← Same
└── tests (4)                  ← +LYBT.Tests.Sync
```

---

## [S3] Per-Module Refactoring Plan

### Module 1: Auth

**Current**: `IJwtService` + `IAuthService` in `LYBT.Module.Auth`

**Target (per ABP.IO + eShopOnWeb)**:
```
LYBT.Module.Auth/
├── Domain/
│   └── AuthSession.cs              ← Aggregate root (currently anemic)
├── Application/
│   ├── LoginCommand.cs             ← MediatR command
│   ├── LoginCommandHandler.cs
│   ├── LogoutCommand.cs
│   ├── LogoutCommandHandler.cs
│   ├── RefreshTokenCommand.cs
│   ├── RefreshTokenCommandHandler.cs
│   ├── ValidateTokenQuery.cs
│   └── ValidateTokenQueryHandler.cs
├── Infrastructure/
│   ├── JwtTokenService.cs          ← renamed from JwtService
│   ├── AuthDbContext.cs            ← NEW: module-scoped DbContext
│   └── AuthSessionRepository.cs
└── AuthModule.cs                   ← DI registration
```

**Key changes**:
- `AuthSession` becomes aggregate root with domain methods: `Create()`, `Revoke()`, `Extend()`
- Login/Logout become MediatR commands instead of direct service calls
- Module gets own `AuthDbContext` (schema-isolated, shared connection)

### Module 2: Users

**Current**: `IUserService` + `IUserManagerService` in `LYBT.Module.Users`

**Target (per Milan Jovanovic Module Boundaries)**:
```
LYBT.Module.Users/
├── Domain/
│   ├── ApplicationUser.cs          ← moved from LYBT.Entities
│   ├── ApplicationRole.cs          ← moved from LYBT.Entities
│   ├── UserRole.cs                 ← moved from LYBT.Entities
│   ├── Events/
│   │   ├── UserCreatedEvent.cs     ← NEW
│   │   ├── UserDeletedEvent.cs     ← NEW
│   │   └── UserStatusChangedEvent.cs ← NEW
│   └── Services/
│       └── PasswordPolicy.cs       ← domain service
├── Application/
│   ├── CreateUserCommand.cs
│   ├── UpdateUserCommand.cs
│   ├── DeleteUserCommand.cs
│   ├── GetUserQuery.cs
│   ├── GetUsersQuery.cs
│   ├── ChangePasswordCommand.cs
│   ├── ResetPasswordCommand.cs
│   └── Validators/
│       ├── CreateUserValidator.cs
│       └── UpdateUserValidator.cs
├── Infrastructure/
│   ├── UsersDbContext.cs           ← NEW
│   ├── UserRepository.cs
│   └── UserCrossModuleService.cs   ← implements IUserCrossModuleService
└── UsersModule.cs
```

**Key changes**:
- Domain events on user lifecycle: `UserCreatedEvent`, `UserDeletedEvent`, `UserStatusChangedEvent`
- Cross-module queries via `IUserCrossModuleService` (kept, but now documented as integration point)
- Module-scoped `UsersDbContext`

### Module 3: Patients

**Current**: `IPatientService` + `IPatientRepository` in `LYBT.Module.Patients`

**Target (per Microsoft eShopOnWeb Clean Architecture)**:
```
LYBT.Module.Patients/
├── Domain/
│   ├── Patient.cs                  ← moved from LYBT.Entities, enriched with domain methods
│   ├── Events/
│   │   ├── PatientCreatedEvent.cs
│   │   ├── PatientUpdatedEvent.cs
│   │   └── PatientDeletedEvent.cs
│   └── ValueObjects/
│       └── PatientSearchCriteria.cs
├── Application/
│   ├── CreatePatientCommand.cs
│   ├── UpdatePatientCommand.cs
│   ├── DeletePatientCommand.cs
│   ├── GetPatientQuery.cs
│   ├── GetPatientsQuery.cs
│   ├── SearchPatientsQuery.cs
│   ├── BatchImportCommand.cs
│   └── Validators/
├── Infrastructure/
│   ├── PatientsDbContext.cs
│   ├── PatientRepository.cs
│   └── PatientReferenceChecker.cs
└── PatientsModule.cs
```

**Key changes**:
- Patient gets domain methods: `UpdateProfile()`, `ChangeStatus()`, `SoftDelete()`
- Batch import becomes MediatR command with validation pipeline
- Reference checking via domain service

### Module 4: Herbs

**Current**: `IHerbService` + `IHerbRepository` + `IHerbReferenceRepository`

**Target (per ABP.IO Module Development Best Practices)**:
```
LYBT.Module.Herbs/
├── Domain/
│   ├── Herb.cs                     ← moved, enriched
│   ├── Events/
│   │   ├── HerbCreatedEvent.cs
│   │   ├── HerbPriceChangedEvent.cs
│   │   └── HerbDeletedEvent.cs
│   └── Services/
│       └── HerbReferenceChecker.cs ← domain service
├── Application/
│   ├── CRUD commands/queries
│   ├── BatchImportCommand.cs
│   ├── BatchDeleteCommand.cs
│   └── Validators/
├── Infrastructure/
│   ├── HerbsDbContext.cs
│   ├── HerbRepository.cs
│   └── HerbReferenceRepository.cs
└── HerbsModule.cs
```

**Key changes**:
- `HerbPriceChangedEvent` triggers downstream reactions (formula price recalculation)
- Reference checking as domain service (not application service)

### Module 5: Formula

**Current**: `IFormulaService` + `IFormulaImportExportService`

**Target (per Milan Jovanovic Event-Driven Architecture)**:
```
LYBT.Module.Formula/
├── Domain/
│   ├── Formula.cs                   ← moved, enriched with AddHerb/RemoveHerb/Validate
│   ├── FormulaHerbItem.cs           ← moved
│   ├── Events/
│   │   ├── FormulaCreatedEvent.cs
│   │   ├── FormulaValidatedEvent.cs
│   │   └── FormulaDeletedEvent.cs
│   └── Services/
│       └── FormulaValidator.cs      ← domain service
├── Application/
│   ├── CRUD commands/queries
│   ├── BatchImportCommand.cs
│   ├── ValidateFormulaHerbCommand.cs
│   └── Validators/
├── Infrastructure/
│   ├── FormulasDbContext.cs
│   ├── FormulaRepository.cs
│   └── FormulaImportExportService.cs
└── FormulaModule.cs
```

**Key changes**:
- Formula gets domain methods: `AddHerb()`, `RemoveHerb()`, `Validate()`, `MarkShared()`
- Herb validation becomes domain service with interaction checks

### Module 6: MedicalCase (DDD Aggregate Root)

**Current**: `IMedicalCaseFacade` + Command/Query/State services

**Target (per Microsoft eShopOnContainers DDD + Milan Jovanovic CQRS)**:
```
LYBT.Module.MedicalCase/
├── Domain/
│   ├── MedicalCase.cs              ← ALREADY aggregate root, keep as-is
│   ├── Consultation.cs             ← internal entity
│   ├── Prescription.cs             ← internal entity
│   ├── PrescriptionItem.cs         ← value object
│   ├── Events/
│   │   ├── MedicalCaseCreatedEvent.cs
│   │   ├── MedicalCaseCompletedEvent.cs
│   │   ├── MedicalCaseCancelledEvent.cs
│   │   └── PrescriptionCreatedEvent.cs
│   ├── ValueObjects/
│   │   ├── CaseNumber.cs
│   │   └── PrescriptionNumber.cs
│   └── Services/
│       ├── ICaseNumberGenerator.cs
│       └── IPriceCalculator.cs
├── Application/
│   ├── Commands/
│   │   ├── CreateMedicalCaseCommand.cs
│   │   ├── UpdateMedicalCaseCommand.cs
│   │   ├── DeleteMedicalCaseCommand.cs
│   │   ├── CompleteMedicalCaseCommand.cs
│   │   ├── SuspendMedicalCaseCommand.cs
│   │   └── CancelMedicalCaseCommand.cs
│   ├── Queries/
│   │   ├── GetMedicalCaseQuery.cs
│   │   ├── GetMedicalCasesQuery.cs
│   │   ├── QueryMedicalCasesQuery.cs
│   │   └── SearchMedicalCasesQuery.cs
│   ├── Handlers/                   ← CommandHandlers + QueryHandlers
│   └── Validators/
├── Infrastructure/
│   ├── MedicalCasesDbContext.cs
│   ├── MedicalCaseRepository.cs
│   ├── MedicalCaseReferenceRepository.cs
│   ├── CaseNumberGenerator.cs
│   └── PriceCalculator.cs
└── MedicalCaseModule.cs
```

**Key changes**:
- Full CQRS with MediatR (currently partial)
- Domain events on every state transition
- Value objects for CaseNumber, PrescriptionNumber
- Domain services for number generation and price calculation

### Module 7: Registration

**Current**: `IRegistrationService` + `IRegistrationRepository`

**Target**:
```
LYBT.Module.Registration/
├── Domain/
│   ├── Registration.cs             ← moved, enriched
│   ├── Events/
│   │   ├── RegistrationCreatedEvent.cs
│   │   ├── RegistrationCancelledEvent.cs
│   │   └── RegistrationCompletedEvent.cs
│   └── Services/
│       └── QueueManager.cs         ← domain service for queue logic
├── Application/
│   ├── CRUD commands/queries
│   ├── QuickVisitCommand.cs
│   ├── StartVisitCommand.cs
│   └── Validators/
├── Infrastructure/
│   ├── RegistrationsDbContext.cs
│   └── RegistrationRepository.cs
└── RegistrationModule.cs
```

### Module 8: Reports

**Current**: `IReportService` + `IReportRepository`

**Target (read-only module, no domain events needed)**:
```
LYBT.Module.Reports/
├── Application/
│   ├── GetDailyIncomeQuery.cs
│   ├── GetDailyConsultationsQuery.cs
│   └── GetDailyHerbUsageQuery.cs
├── Infrastructure/
│   ├── ReportsDbContext.cs
│   └── ReportRepository.cs
└── ReportsModule.cs
```

**Key change**: Reports is read-only — no domain layer needed, just Application + Infrastructure.

---

## [S4] SharedKernel (NEW — per Milan Jovanovic)

```
LYBT.SharedKernel/
├── Events/
│   ├── IDomainEvent.cs             ← : INotification
│   └── IDomainEventDispatcher.cs
├── Contracts/
│   ├── IUserCrossModuleService.cs  ← moved from Infrastructure
│   ├── ICrossModuleAuthService.cs  ← moved from Infrastructure
│   └── IPatientQueryService.cs     ← NEW: cross-module query contract
├── Primitives/
│   ├── ValueObject.cs              ← base record
│   ├── Entity.cs                   ← base class (replaces BaseEntity)
│   └── AggregateRoot.cs            ← marker interface
└── Outbox/
    ├── OutboxMessage.cs            ← NEW: outbox pattern
    └── IOutboxService.cs           ← NEW
```

**Key principle** (per ABP.IO): SharedKernel contains ONLY contracts and primitives. No implementations.

---

## [S5] Cross-Module Communication (per Milan Jovanovic)

### Before (current)
```
MedicalCaseService → IUserCrossModuleService → UserCrossModuleService
Direct interface call, tight coupling
```

### After (target)
```
MedicalCaseModule                          UsersModule
      │                                        │
      ├─ Publish MedicalCaseCompletedEvent ────┤
      │   (via IDomainEventDispatcher)         │
      │                                        │
      │   ┌────────────────────────────────────┘
      │   │
      │   └─ UserStatusChangedHandler reacts
      │      (updates last visit time)
      │
      └─ For synchronous reads:
         IUserCrossModuleService (kept as query contract)
         Implementation in UsersModule
```

**Three communication patterns** (per Milan Jovanovic):

| Pattern | Use Case | Implementation |
|---------|----------|---------------|
| **Query Contract** | MedicalCase needs user info for display | `IUserCrossModuleService` — synchronous, in-process |
| **Command Contract** | Registration needs to create MedicalCase | `IMedicalCaseFacade.SaveAsync()` — synchronous, in-process |
| **Domain Event** | MedicalCase completed → update registration status | `MedicalCaseCompletedEvent` → `INotificationHandler` — async, in-process |

---

## [S6] Module Data Isolation (per ABP.IO)

### Approach: Shared Database, Separate Schemas

```
LYBTDB_Dev (SQL Server)
├── Auth.*        → Auth module tables
├── Users.*       → Users module tables
├── Patients.*    → Patients module tables
├── Herbs.*       → Herbs module tables
├── Formula.*     → Formula module tables
├── MedicalCase.* → MedicalCase module tables
├── Registration.* → Registration module tables
└── Reports.*     → Reports module tables (read-only views)
```

Each module gets its own `DbContext` but shares the same connection string. EF Core schema mapping handles isolation.

### Per-Module DbContext Pattern

```csharp
// Example: PatientsDbContext
public class PatientsDbContext : DbContext
{
    public DbSet<Patient> Patients { get; set; }
    // ONLY Patient-related tables

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Patients");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);
    }
}
```

---

## [S7] Outbox Pattern (per Kamil Grzybek + Milan Jovanovic)

### Implementation

```csharp
// LYBT.SharedKernel/Outbox/OutboxMessage.cs
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}

// Saves domain events to Outbox table within same transaction
// Background worker processes Outbox → publishes to event bus
// Guarantees at-least-once delivery
```

### Flow
1. Command handler saves entity + outbox messages in same transaction
2. Background worker polls Outbox table
3. Worker publishes events via IDomainEventDispatcher
4. Event handlers in other modules react
5. Worker marks outbox message as processed

---

## [S8] Implementation Phases

### Phase 1: SharedKernel + Domain Events (2 weeks)
- Create `LYBT.SharedKernel` project
- Define `IDomainEvent`, `IDomainEventDispatcher`, `OutboxMessage`
- Move cross-module interfaces to SharedKernel
- Add domain events to MedicalCase module (pilot)
- Architecture tests to enforce module boundaries

### Phase 2: Value Objects + Domain Methods (1 week)
- Create `ValueObject` base record
- Add `CaseNumber`, `PrescriptionNumber` value objects
- Enrich Patient, Herb, Formula with domain methods
- Add domain events to all modules

### Phase 3: Module-Scoped DbContext (3 weeks)
- Create per-module DbContext classes
- Move entity configurations to respective modules
- Create query contract interfaces in SharedKernel
- Update DI registration
- Integration tests for each module

### Phase 4: MediatR + CQRS (2 weeks)
- Add MediatR to all modules
- Create Commands/Queries for MedicalCase (pilot)
- Add validation pipeline (FluentValidation + MediatR)
- Add logging pipeline
- Extend to all modules

### Phase 5: Offline-First Sync Engine (4 weeks)
- Create `SyncOperation` entity
- Implement `ISyncQueue` for pending operations
- Implement `ISyncEngine` with push/pull logic
- Implement `IConflictResolver` (last-write-wins + user notification)
- Add sync status UI

### Phase 6: Documentation (1 week)
- XML docs on all public APIs
- ADR for each major decision
- Update AGENTS.md with new architecture

### Phase 7: Testing (1 week)
- Unit tests for domain entities and value objects
- Integration tests for module DbContext
- Architecture tests for module isolation
- Sync engine tests

**Total: 14 weeks**

---

## [S9] Migration Safety

| Risk | Mitigation |
|------|-----------|
| Breaking module isolation | Architecture tests catch violations immediately |
| Sync engine data loss | Local-first writes always succeed; sync is best-effort |
| MediatR overhead | In-process mediator; negligible latency |
| DbContext split | Shared connection string; test with integration tests |
| Existing functionality | Additive only in Phase 1-2; no breaking changes |

### Backward Compatibility
- Phase 1-2: Additive only — new projects, new interfaces
- Phase 3: Move entities to Domain, keep backward-compatible facades
- Phase 4: Wrap existing services with MediatR handlers
- Phase 5: Sync engine is new subsystem
- Phase 6-7: Documentation and cleanup

---

## [S10] Verification Checklist

After each phase, verify:

- [ ] `dotnet build LYBTZYZS.sln` passes
- [ ] `dotnet test tests/LYBT.Tests.Server/` passes
- [ ] `dotnet test tests/LYBT.Tests.Desktop/` passes
- [ ] `dotnet test tests/LYBT.Tests.Architecture/` passes (module isolation)
- [ ] No cross-module direct table queries (architecture test)
- [ ] All public APIs have XML documentation
- [ ] Domain events published for all state transitions
- [ ] Outbox pattern implemented for cross-module events
