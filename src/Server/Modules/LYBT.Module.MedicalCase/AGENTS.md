# LYBT.Module.MedicalCase - Server MedicalCase Module

**Purpose**: Server-side medical case module using CQRS pattern (not traditional 3-layer).

## Structure

```
LYBT.Module.MedicalCase/
├── Interfaces/          # 11 interface definitions
├── Services/            # 10 files: MedicalCaseCommandService, MedicalCaseQueryService, MedicalCaseStateService, MedicalCaseFacade, MedicalCaseAuditService, MedicalCasePermissionService, MedicalCasePrintService, MedicalCaseReferenceService, MedicalCaseRules, MedicalCaseServiceHelper
├── Repositories/        # 3 repository files
├── Mapping/             # MedicalCaseMapper (Riok.Mapperly)
└── MedicalCaseModule.cs # Module registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Write operations | `Services/MedicalCaseCommandService.cs` | CQRS command side |
| Read operations | `Services/MedicalCaseQueryService.cs` | CQRS query side |
| State transitions | `Services/MedicalCaseStateService.cs` | Complete/Cancel/Suspend + Registration linkage |
| Facade | `Services/MedicalCaseFacade.cs` | Aggregate root operations |
| Audit | `Services/MedicalCaseAuditService.cs` | 20-field diff tracking |
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
