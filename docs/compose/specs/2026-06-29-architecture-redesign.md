# LYBTZYZS Architecture Redesign — Best-Practice-Driven Spec

> Generated 2026-06-29 via Tavily research + codegraph deep analysis  
> Research sources: Milan Jovanovic (Modular Monolith), ABP.IO, Microsoft Learn, Kamil Grzybek, Prism Library docs, Flutter/Android offline-first guides, MedicalMet TCM specs

---

## [S1] Problem Statement

> **Serena验证状态** (2026-06-29): 所有实体/接口/方法签名已通过Serena符号查询交叉验证
> - BaseEntity: 7属性 ✓ | MedicalCase: 13属性+4方法 ✓ | Patient: 8属性 ✓
> - Herb: 13属性 ✓ | Formula: 13属性 ✓ | AppDbContext: 10 DbSet ✓
> - IRepository\<T\>: 15方法 ✓ | 跨模块模式: IUserCrossModuleService/ICrossModuleAuthService ✓
> - 所有Service接口方法签名已验证 ✓

The current LYBTZYZS architecture has **14 identified gaps** compared to industry best practices for modular monolith clinic management systems:

| # | Gap | Severity | Source |
|---|-----|----------|--------|
| G1 | No domain events — modules use direct `ICrossModuleService` calls (verified: `IUserCrossModuleService`, `ICrossModuleAuthService` in `CrossModuleService`) | High | Milan Jovanovic, ABP.IO |
| G2 | Shared DbContext across all modules — no data isolation (verified: single `AppDbContext` with 10 DbSets) | High | ABP.IO "Modular Monolith Data Isolation" |
| G3 | Generic `IRepository<T>` over EF Core (verified: 15 methods in IRepository) | Medium | Microsoft Learn, Stack Overflow consensus |
| G4 | Only MedicalCase is aggregate root; Patient/Herb/Formula are anemic (verified: no domain methods on Patient/Herb/Formula) | Medium | DDD best practices |
| G5 | No value objects (Money, PhoneNumber, etc.) | Medium | DDD blue book |
| G6 | No Outbox pattern for guaranteed cross-module delivery | Medium | Kamil Grzybek |
| G7 | No proper offline-first sync engine for dual-mode | High | Flutter/Android offline-first guides |
| G8 | No XML documentation on public APIs | Low | .NET convention |
| G9 | Inconsistent error handling (Result<T> vs exceptions) | Medium | Clean Architecture |
| G10 | No domain services — business logic in application services | Medium | DDD |
| G11 | Missing HIPAA-style audit trail and data encryption | Medium | Healthcare compliance |
| G12 | No Vertical Slice option for complex workflows | Low | Milan Jovanovic |
| G13 | MedicalCase CQRS incomplete — no MediatR integration | Low | CQRS best practices |
| G14 | No ADR documentation beyond ADR-0010 | Low | Architecture governance |

---

## [S2] Research Findings — Best Practices

### Modular Monolith (Milan Jovanovic, ABP.IO)
- **Data Isolation**: Each module owns its own DbContext/schema; no cross-module table queries
- **Communication**: Synchronous via query/command contracts; Asynchronous via domain events
- **Module Structure**: Domain → Application → Infrastructure per module
- **Shared Kernel**: Minimal shared code (events, contracts, primitives)

### Clean Architecture (Microsoft Learn)
- **Dependency Rule**: Domain ← Application ← Infrastructure; never reverse
- **Repository Pattern**: Controversial with EF Core — use `IQueryable` directly or thin wrapper
- **CQRS**: Separate read/write models only when workload demands it

### DDD (Blue Book, EventStorming)
- **Aggregate Roots**: Transaction consistency boundary; only root accessed directly
- **Value Objects**: Immutable, identity-less (Money, Address, PhoneNumber)
- **Domain Events**: Capture side effects; publish after transaction commits
- **Domain Services**: Logic that doesn't belong to a single entity

### Offline-First (Flutter docs, Android docs, Ditto)
- **Local Write First**: All writes go to local DB immediately
- **Sync Engine**: Background task syncs when connectivity available
- **Conflict Resolution**: Last-write-wins, CRDTs, or operational transforms
- **Pending Operations Queue**: Track unsynced operations with retry

### TCM-Specific (MedicalMet, SmartZhongYi)
- **Herb Inventory**: Gram/tael units, batch-level tracking, expiry control
- **Prescription Builder**: Drag-and-drop formula construction, herb interaction checks
- **Diagnosis Templates**: Structured pulse/tongue/pattern diagnosis
- **Bilingual Support**: Chinese/English herb names

---

## [S3] Redesigned Solution Structure

### Project Layout (40 → 44 projects)

```
LYBTZYZS.sln
├── src/
│   ├── Server/
│   │   ├── Core/
│   │   │   ├── LYBT.Domain/                    ← NEW: Domain layer (entities, value objects, domain events, domain services)
│   │   │   ├── LYBT.Application/               ← NEW: Application layer (use cases, commands, queries, validators)
│   │   │   ├── LYBT.Infrastructure/            ← REFACTORED: Only persistence, external services
│   │   │   └── LYBT.SharedKernel/              ← NEW: Shared kernel (events, contracts, primitives)
│   │   ├── Modules/
│   │   │   ├── LYBT.Module.Auth/
│   │   │   ├── LYBT.Module.Users/
│   │   │   ├── LYBT.Module.Patients/
│   │   │   ├── LYBT.Module.Herbs/
│   │   │   ├── LYBT.Module.Formula/
│   │   │   ├── LYBT.Module.MedicalCase/
│   │   │   ├── LYBT.Module.Registration/
│   │   │   └── LYBT.Module.Reports/
│   │   └── Services/
│   │       └── LYBT.WebAPI/
│   ├── Client/
│   │   └── Desktop/ (same structure, enhanced)
│   ├── Shared/ (same structure)
│   └── Tools/ (same structure)
└── tests/
```

### Key Structural Changes

| Change | Before | After | Rationale |
|--------|--------|-------|-----------|
| Entities | `LYBT.Entities` (flat) | `LYBT.Domain` (DDD: entities, value objects, events, services) | DDD best practice |
| Business Logic | In `LYBT.Module.*Services` | `LYBT.Application` (use cases, MediatR handlers) | Clean Architecture |
| Persistence | `LYBT.Infrastructure` (monolith) | Per-module `Infrastructure` folder | Data isolation |
| Cross-module | `ICrossModuleService` direct calls | Domain Events via `LYBT.SharedKernel.Events` | Loose coupling |
| DbContext | Single `AppDbContext` | Module-scoped `DbContext` (shared connection) | ABP.IO data isolation |

---

## [S4] Domain Layer — `LYBT.Domain`

### Value Objects (NEW)

```csharp
// LYBT.Domain/Common/ValueObjects/Money.cs
public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }  // default "CNY"

    public Money(decimal amount, string currency = "CNY")
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative");
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero => new(0);
    public Money Add(Money other) => new(Amount + other.Amount, Currency);
    public Money Multiply(int quantity) => new(Amount * quantity, Currency);
}
```

```csharp
// LYBT.Domain/Common/ValueObjects/PhoneNumber.cs
public record PhoneNumber
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        if (!Regex.IsMatch(value, @"^1[3-9]\d{9}$"))
            throw new ArgumentException("Invalid Chinese phone number");
        Value = value;
    }

    public string Masked => Value.Length >= 7
        ? Value[..3] + "****" + Value[^4..]
        : Value;
}
```

```csharp
// LYBT.Domain/Common/ValueObjects/IdNumber.cs
public record IdNumber
{
    public string Value { get; }
    public Gender Gender  // derived from 17th digit
    public int Age        // derived from birth digits

    public IdNumber(string value) { /* validation + derivation */ }
}
```

```csharp
// LYBT.Domain/Common/ValueObjects/CaseNumber.cs
public record CaseNumber
{
    public string Value { get; }  // e.g. "MC20260629001"

    public static CaseNumber Generate(DateTime date, int sequence)
        => new($"MC{date:yyyyMMdd}{sequence:D3}");
}
```

```csharp
// LYBT.Domain/Common/ValueObjects/PrescriptionNumber.cs
public record PrescriptionNumber
{
    public string Value { get; }  // e.g. "RX20260629001"

    public static PrescriptionNumber Generate(DateTime date, int sequence)
        => new($"RX{date:yyyyMMdd}{sequence:D3}");
}
```

### Domain Events (NEW)

```csharp
// LYBT.SharedKernel/Events/IDomainEvent.cs
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
```

```csharp
// LYBT.Module.MedicalCase/Domain/Events/
public record MedicalCaseCreatedEvent(Guid MedicalCaseId, Guid PatientId, string PatientName, Guid DoctorId, string DoctorName) : IDomainEvent;
public record MedicalCaseCompletedEvent(Guid MedicalCaseId, Guid PatientId, Guid DoctorId, string DoctorName, DateTime CompletedAt) : IDomainEvent;
public record MedicalCaseCancelledEvent(Guid MedicalCaseId, Guid PatientId, string Reason) : IDomainEvent;
public record PrescriptionCreatedEvent(Guid PrescriptionId, Guid MedicalCaseId, decimal TotalPrice) : IDomainEvent;
```

```csharp
// LYBT.Module.Patients/Domain/Events/
public record PatientCreatedEvent(Guid PatientId, string Name) : IDomainEvent;
public record PatientDeletedEvent(Guid PatientId, string Name) : IDomainEvent;
```

```csharp
// LYBT.Module.Herbs/Domain/Events/
public record HerbDeletedEvent(Guid HerbId, string HerbName, int ReferenceCount) : IDomainEvent;
public record HerbPriceChangedEvent(Guid HerbId, string HerbName, Money OldPrice, Money NewPrice) : IDomainEvent;
```

```csharp
// LYBT.Module.Registration/Domain/Events/
public record RegistrationCreatedEvent(Guid RegistrationId, Guid PatientId, Guid DoctorId) : IDomainEvent;
public record RegistrationCancelledEvent(Guid RegistrationId, Guid PatientId, string Reason) : IDomainEvent;
```

### Domain Services (NEW)

```csharp
// LYBT.Module.MedicalCase/Domain/Services/ICaseNumberGenerator.cs
public interface ICaseNumberGenerator
{
    Task<CaseNumber> GenerateAsync(CancellationToken ct);
}

// LYBT.Module.Prescription/Domain/Services/IPriceCalculator.cs
public interface IPriceCalculator
{
    Money CalculateItemTotal(Money unitPrice, int dosage);
    Money CalculatePrescriptionTotal(IEnumerable<PrescriptionItem> items, decimal discount);
}
```

### Enhanced Entities (DDD-compliant)

**Patient** — Add domain methods:
```csharp
// LYBT.Domain/Patients/Patient.cs (enhanced)
public class Patient : BaseEntity, IAggregateRoot
{
    // ... existing properties ...

    // NEW: Domain methods
    public void UpdateProfile(string name, Gender gender, DateTime? birthDate, PhoneNumber? phone, IdNumber? idNumber)
    {
        Name = name;
        Gender = gender;
        BirthDate = birthDate;
        PhoneNumber = phone?.Value;
        IdNumber = idNumber?.Value;
        PinYinCode = PinYinHelper.GetInitials(name);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(CommonStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Herb** — Add domain methods:
```csharp
// LYBT.Domain/Herbs/Herb.cs (enhanced)
public class Herb : BaseEntity, IAggregateRoot
{
    // ... existing properties ...

    public void UpdatePrice(Money newPrice)
    {
        var oldPrice = new Money(Price);
        Price = newPrice.Amount;
        UpdatedAt = DateTime.UtcNow;
        // Domain event: HerbPriceChangedEvent
    }

    public void UpdateStock(string unit, decimal price, decimal? costPrice)
    {
        Unit = unit;
        Price = price;
        CostPrice = costPrice;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Formula** — Add domain methods:
```csharp
// LYBT.Domain/Formulas/Formula.cs (enhanced)
public class Formula : BaseEntity, IAggregateRoot
{
    // ... existing properties ...

    public void AddHerb(FormulaHerbItem herbItem) { /* validation + add */ }
    public void RemoveHerb(Guid herbItemId) { /* remove + reindex */ }
    public void Validate() { ValidationStatus = FormulaValidationStatus.Validated; }
    public void MarkShared() { IsShared = true; }
}
```

**MedicalCase** — Keep existing domain methods (already DDD-compliant):
```csharp
// Already has: Complete(), Suspend(), SoftDelete(), UpdateConsultation()
// ADD: Domain events on state transitions
public void Complete()
{
    CaseStatus = MedicalCaseStatus.Completed;
    CompletedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
    // NEW: AddDomainEvent(new MedicalCaseCompletedEvent(...))
}
```

---

## [S5] Application Layer — `LYBT.Application`

### MediatR Integration (NEW)

```csharp
// LYBT.Application/Common/Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Any()) throw new ValidationException(failures);
        return await next();
    }
}
```

```csharp
// LYBT.Application/Common/Behaviors/LoggingBehavior.cs
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    // Logs request name, elapsed time, correlation ID
}
```

### Command/Query Separation (per module)

**MedicalCase Module (full CQRS)**:
```csharp
// LYBT.Module.MedicalCase/Application/Commands/CreateMedicalCaseCommand.cs
public record CreateMedicalCaseCommand(
    Guid PatientId, string PatientName,
    Guid DoctorId, string DoctorName,
    string? CaseNumber,
    MedicalCaseStatus CaseStatus,
    bool? NeedsPrescription,
    ConsultationInputDto? Consultation,
    PrescriptionInputDto? Prescription
) : IRequest<Result<MedicalCaseDetailDto>>;

public class CreateMedicalCaseCommandHandler : IRequestHandler<CreateMedicalCaseCommand, Result<MedicalCaseDetailDto>>
{
    private readonly IMedicalCaseRepository _repo;
    private readonly ICaseNumberGenerator _numberGen;
    private readonly IPriceCalculator _priceCalc;

    public async Task<Result<MedicalCaseDetailDto>> Handle(CreateMedicalCaseCommand cmd, CancellationToken ct)
    {
        var caseNumber = cmd.CaseNumber ?? (await _numberGen.GenerateAsync(ct)).Value;
        var medicalCase = new MedicalCase(cmd.PatientId, cmd.PatientName, cmd.DoctorId, cmd.DoctorName, caseNumber, cmd.CaseStatus);

        if (cmd.Consultation != null)
            medicalCase.UpdateConsultation(cmd.Consultation.PresentIllness, cmd.Consultation.TongueDiagnosis, cmd.Consultation.PulseDiagnosis, cmd.Consultation.TcmDiagnosis);

        if (cmd.Prescription != null)
            medicalCase.AttachPrescription(cmd.Prescription, _priceCalc);

        await _repo.AddAsync(medicalCase, ct);
        // Domain events published via SaveChanges interceptor
        return Result<MedicalCaseDetailDto>.Success(medicalCase.ToDetailDto());
    }
}
```

```csharp
// LYBT.Module.MedicalCase/Application/Queries/GetMedicalCaseQuery.cs
public record GetMedicalCaseQuery(Guid Id) : IRequest<Result<MedicalCaseDetailDto>>;

public class GetMedicalCaseQueryHandler : IRequestHandler<GetMedicalCaseQuery, Result<MedicalCaseDetailDto>>
{
    private readonly IMedicalCaseRepository _repo;
    // Read-only, uses AsNoTracking projection
}
```

**Other Modules (simplified CQRS — commands only, queries via repository)**:
```csharp
// Patient, Herb, Formula, Registration, Users — keep service pattern but use MediatR for complex operations
// Simple CRUD stays in service layer; complex workflows become commands
```

---

## [S6] Module Data Isolation (ABP.IO pattern)

### Per-Module DbContext

```csharp
// LYBT.Module.Patients/Infrastructure/PatientsDbContext.cs
public class PatientsDbContext : DbContext
{
    public DbSet<Patient> Patients { get; set; }
    // ONLY Patient tables — no cross-module access

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);
    }
}
```

```csharp
// LYBT.Module.MedicalCase/Infrastructure/MedicalCasesDbContext.cs
public class MedicalCasesDbContext : DbContext
{
    public DbSet<MedicalCase> MedicalCases { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
    // MedicalCase aggregate only
}
```

### Cross-Module Queries via Contracts (NOT direct table access)

```csharp
// LYBT.SharedKernel/Contracts/IPatientQueryService.cs
public interface IPatientQueryService
{
    Task<PatientBasicDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<PatientBasicDto>> GetByIdsAsync(List<Guid> ids, CancellationToken ct);
}

// Implemented in LYBT.Module.Patients/Infrastructure/PatientQueryService.cs
// Called by MedicalCase module via DI — NOT by direct DbContext access
```

### Shared Connection, Separate Schemas

```csharp
// LYBT.Infrastructure/Data/SharedDbContextFactory.cs
// All module DbContexts share the same connection string
// but use different schema mappings
// Database still single SQL Server, but each module's tables are schema-isolated
```

---

## [S7] Offline-First Sync Engine (NEW)

### Architecture

```
┌─────────────────────────────────────┐
│  Desktop Client (WPF)               │
│  ┌───────────┐  ┌────────────────┐  │
│  │ Local DB   │  │ Sync Engine    │  │
│  │ (LocalDB)  │←→│ - Queue        │  │
│  │            │  │ - Conflict Res │  │
│  └───────────┘  │ - Retry        │  │
│                  └───────┬────────┘  │
└──────────────────────────┼───────────┘
                           │ HTTPS
                    ┌──────▼──────┐
                    │ WebAPI       │
                    │ (Remote DB)  │
                    └─────────────┘
```

### Sync Queue Entity

```csharp
// LYBT.Desktop.LocalData/Sync/SyncOperation.cs
public class SyncOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; }       // "Patient", "MedicalCase", etc.
    public Guid EntityId { get; set; }
    public string Operation { get; set; }         // "Create", "Update", "Delete"
    public string Payload { get; set; }           // JSON serialized entity
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SyncedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
}

public enum SyncStatus { Pending, InProgress, Completed, Failed, Conflict }
```

### Sync Engine

```csharp
// LYBT.Desktop.LocalData/Sync/ISyncEngine.cs
public interface ISyncEngine
{
    Task<SyncResult> SyncAllAsync(CancellationToken ct);
    Task<SyncResult> PushPendingAsync(CancellationToken ct);
    Task<SyncResult> PullUpdatesAsync(DateTime? lastSyncTime, CancellationToken ct);
    Task<SyncConflict> ResolveConflictAsync(SyncConflict conflict, ConflictResolution resolution, CancellationToken ct);
    event EventHandler<SyncProgressEventArgs>? ProgressChanged;
}

// LYBT.Desktop.LocalData/Sync/SyncEngine.cs
public class SyncEngine : ISyncEngine
{
    private readonly ISyncQueue _queue;
    private readonly IApiClient _remoteApi;
    private readonly ILocalDbContext _localDb;
    private readonly IConflictResolver _conflictResolver;

    // Implements:
    // 1. Push pending operations to remote
    // 2. Pull updates since last sync
    // 3. Detect conflicts (last-write-wins with user notification)
    // 4. Retry failed operations with exponential backoff
}
```

### Conflict Resolution Strategy

```csharp
// LYBT.Desktop.LocalData/Sync/IConflictResolver.cs
public interface IConflictResolver
{
    Task<ConflictResolution> ResolveAsync(LocalVersion local, RemoteVersion remote, CancellationToken ct);
}

public enum ConflictResolution
{
    KeepLocal,      // User chose local version
    KeepRemote,     // User chose remote version
    Merge,          // Automatic merge (simple fields)
    Skip            // Skip this operation
}
```

---

## [S8] Cross-Module Event Bus (replaces ICrossModuleService)

### In-Process Event Dispatcher

```csharp
// LYBT.SharedKernel/Events/IDomainEventDispatcher.cs
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct);
}

// LYBT.Infrastructure/Events/DomainEventDispatcher.cs
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var domainEvent in events)
        {
            await _mediator.Publish(domainEvent, ct);
        }
    }
}
```

### Outbox Pattern (Kamil Grzybek)

```csharp
// LYBT.Infrastructure/Outbox/OutboxMessage.cs
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
```

### Event Handlers (cross-module reactions)

```csharp
// LYBT.Module.Registration/Domain/Events/MedicalCaseCompletedHandler.cs
public class MedicalCaseCompletedHandler : INotificationHandler<MedicalCaseCompletedEvent>
{
    private readonly IRegistrationRepository _repo;

    public async Task Handle(MedicalCaseCompletedEvent notification, CancellationToken ct)
    {
        // Update registration status when medical case completes
        var registration = await _repo.GetByMedicalCaseIdAsync(notification.MedicalCaseId, ct);
        if (registration != null)
        {
            registration.Complete();
            await _repo.UpdateAsync(registration, ct);
        }
    }
}
```

---

## [S9] Error Handling Standardization (NEW)

### Unified Result Pattern

```csharp
// LYBT.SharedKernel/Common/Result.cs
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorCode ErrorCode { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    public static Result<T> Success(T value) => new(true, value, default, default, null);
    public static Result<T> Failure(ErrorCode code, string error) => new(false, default, error, code, null);
    public static Result<T> ValidationFailure(Dictionary<string, string[]> errors)
        => new(false, default, "Validation failed", ErrorCode.ValidationFailed, errors);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ErrorCode ErrorCode { get; }

    public static Result Success() => new(true, null, default);
    public static Result Failure(ErrorCode code, string error) => new(false, error, code);
}
```

### Global Exception → Result Mapping

```csharp
// All application services return Result<T>
// Controllers map Result → ApiResponse<T>
// No exceptions for business logic — only infrastructure failures throw
```

---

## [S10] XML Documentation Standard (NEW)

Every public class and method MUST have XML documentation:

```csharp
/// <summary>
/// 创建新的医案（病历记录）。
/// 当患者首次就诊或复诊时，由医生创建医案。
/// </summary>
/// <param name="patientId">患者ID</param>
/// <param name="patientName">患者姓名（冗余存储，避免JOIN查询）</param>
/// <param name="doctorId">医生用户ID</param>
/// <param name="doctorName">医生姓名（冗余存储）</param>
/// <param name="caseNumber">医案编号（可选，自动生成格式：MCyyyyMMddNNN）</param>
/// <param name="consultation">诊断信息（可选，可后续补充）</param>
/// <param name="prescription">处方信息（可选，可后续补充）</param>
/// <returns>创建的医案详情</returns>
/// <exception cref="AppException">当患者不存在或医生无权限时抛出</exception>
/// <remarks>
/// 业务规则：
/// - 同一患者同一时间只能有一个进行中的医案
/// - 创建后自动触发 MedicalCaseCreatedEvent
/// - 诊号格式：MC + 年月日 + 3位序号
/// </remarks>
public async Task<Result<MedicalCaseDetailDto>> CreateMedicalCaseAsync(
    Guid patientId, string patientName,
    Guid doctorId, string doctorName,
    string? caseNumber,
    ConsultationInputDto? consultation,
    PrescriptionInputDto? prescription,
    CancellationToken ct = default)
{
    // Implementation
}
```

---

## [S11] ADR Documentation Template (NEW)

Each major architectural decision gets an ADR:

```markdown
# ADR-0011: Module Data Isolation

## Status
Accepted

## Context
Current architecture uses single AppDbContext shared by all modules.
This violates ABP.IO modular monolith data isolation principle.
Modules can (and do) query each other's tables directly.

## Decision
Each module gets its own DbContext with only its tables.
Cross-module reads use query contract interfaces (IPatientQueryService).
Cross-module writes use domain events.

## Consequences
+ Modules can evolve independently
+ Database schema changes are isolated
+ Easier to test modules in isolation
- Slightly more complex DI registration
- Cross-module joins require application-level composition
```

---

## [S12] Implementation Priority

| Phase | Scope | Effort | Risk |
|-------|-------|--------|------|
| **Phase 1** | Domain events + Outbox pattern | 2 weeks | Low |
| **Phase 2** | Value objects + domain methods | 1 week | Low |
| **Phase 3** | Per-module DbContext (data isolation) | 3 weeks | Medium |
| **Phase 4** | MediatR + CQRS for MedicalCase | 2 weeks | Low |
| **Phase 5** | Offline-first sync engine | 4 weeks | High |
| **Phase 6** | XML docs + ADR documentation | 1 week | Low |
| **Phase 7** | Error handling standardization | 1 week | Low |

**Total estimated**: 14 weeks

### Phase 1 Details: Domain Events + Outbox

1. Create `LYBT.SharedKernel` project with `IDomainEvent`, `IDomainEventDispatcher`
2. Create `OutboxMessage` entity and `OutboxService`
3. Add `SaveChangesInterceptor` to publish domain events to Outbox
4. Add background worker to process Outbox
5. Create event handlers for MedicalCase→Registration cross-module reactions
6. Remove `ICrossModuleService` interfaces (migrate to events)

### Phase 3 Details: Data Isolation

1. Create per-module DbContext classes
2. Move entity configurations to respective modules
3. Create query contract interfaces in SharedKernel
4. Implement query services in each module
5. Update DI registration
6. Run architecture tests to verify isolation

### Phase 5 Details: Sync Engine

1. Create `SyncOperation` entity in LocalData
2. Implement `ISyncQueue` for pending operations
3. Implement `ISyncEngine` with push/pull logic
4. Implement `IConflictResolver` (last-write-wins + user notification)
5. Add sync status UI in Desktop
6. Add background sync timer

---

## [S13] Migration Path (Backward Compatibility)

The redesign is **incremental** — existing code continues to work during migration:

1. **Phase 1-2**: Additive only — new projects, new interfaces, existing code untouched
2. **Phase 3**: Move entities to Domain, but keep backward-compatible facades
3. **Phase 4**: Wrap existing services with MediatR handlers
4. **Phase 5**: Sync engine is new subsystem — no existing code changes
5. **Phase 6-7**: Documentation and cleanup — no functional changes

### Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Breaking module isolation | Architecture tests catch violations immediately |
| Sync engine data loss | Local-first writes always succeed; sync is best-effort with retry |
| MediatR overhead | In-process mediator; negligible latency |
| DbContext split complexity | Use shared connection string; test with integration tests |

---

## [S14] Testing Strategy Updates

| Test Type | Scope | Framework |
|-----------|-------|-----------|
| Unit Tests | Domain entities, value objects, domain services | xUnit + Moq |
| Integration Tests | Module DbContext, repository, service | xUnit + WebApplicationFactory + Respawn |
| Architecture Tests | Module isolation, dependency rules | NetArchTest |
| Sync Engine Tests | Queue, conflict resolution, retry | xUnit + InMemory provider |
| E2E Tests | Full workflow (login → register → consult → prescribe) | Playwright (Desktop) |

---

## [S15] Summary of Changes

| Area | Before | After |
|------|--------|-------|
| **Domain Model** | Anemic entities | Rich domain with methods + value objects |
| **Cross-Module** | Direct interface calls | Domain events + Outbox |
| **Data Access** | Shared DbContext | Per-module DbContext (shared connection) |
| **CQRS** | Partial (MedicalCase only) | Full CQRS via MediatR (all modules) |
| **Offline Sync** | SwitchingApiClient only | Full sync engine with conflict resolution |
| **Error Handling** | Mixed Result<T>/exceptions | Unified Result<T> everywhere |
| **Documentation** | None on methods | XML docs + ADR on all public APIs |
| **Value Objects** | Primitive types | Money, PhoneNumber, IdNumber, CaseNumber |
| **Domain Services** | None | ICaseNumberGenerator, IPriceCalculator |
| **Event Bus** | None | In-process MediatR + Outbox pattern |
