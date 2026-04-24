# LYBT.Module.MedicalCase - Server MedicalCase Module

**Purpose**: Server-side medical case module using CQRS pattern (not traditional 3-layer).

## Structure

```
LYBT.Module.MedicalCase/
├── Interfaces/          # 11 interface definitions
├── Handlers/            # CommandHandler implementations
├── Services/            # MedicalCaseDataManager
└── MedicalCaseModule.cs # Module registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| CQRS handlers | `Handlers/` | Command handlers for medical case operations |
| Data manager | `Services/MedicalCaseDataManager.cs` | Aggregate root operations |
| Interfaces | `Interfaces/` | 11 service interfaces |

## CONVENTIONS

- **CQRS pattern** — CommandHandler pattern, not traditional Controller→Service→Repository
- **Aggregate root** — MedicalCase is sole DDD aggregate; Consultation + Prescription are internal
- **No independent repos** — Consultation/Prescription accessed only through MedicalCaseDataManager
- **Domain methods** — MedicalCaseModel has `Complete()`, `SaveAsDraft()`, `SoftDelete()`, `UpdateConsultation()`

## ANTI-PATTERNS

- **Direct Consultation/Prescription repos** — All operations go through MedicalCaseDataManager
- **Service injecting DbContext** — Must use Repository interface
- **Cross-module references** — MUST NOT reference other server modules
