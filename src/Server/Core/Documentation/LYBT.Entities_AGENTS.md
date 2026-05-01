# LYBT.Entities - Server Domain Entities

**Purpose**: Core domain entities organized by business aggregate (DDD-style folders).

## Structure

```
LYBT.Entities/
├── Attributes/          # Custom attributes
├── Auth/                # AdminSecret, AuthSession
├── Common/              # BaseEntity, enums (CommonStatus, UserRole, etc.)
├── Consultations/       # ConsultationModel
├── Formulas/            # FormulaModel, FormulaHerbItem
├── Herbs/               # HerbModel
├── MedicalCases/        # MedicalCaseModel (aggregate root)
├── Patients/            # PatientModel
├── Prescriptions/       # PrescriptionModel, PrescriptionItem
├── Registrations/       # RegistrationModel
└── Users/               # UserModel
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Base entity | `Common/BaseEntity.cs` | Id, CreatedAt, UpdatedAt, IsDeleted |
| Aggregate root | `MedicalCases/MedicalCaseModel.cs` | Has Consultation + Prescription as internals |
| Enums | `Common/` | UserRole, CommonStatus, CaseStatus, etc. |
| Domain methods | `MedicalCases/MedicalCaseModel.cs` | Complete(), SaveAsDraft(), SoftDelete() |

## CONVENTIONS

- **Aggregate root** — MedicalCase is sole DDD aggregate; Consultation + Prescription are internal
- **Base entity** — All entities inherit from BaseEntity (Id, audit fields, soft-delete)
- **Anemic model** — Entities are mostly data containers; domain logic in MedicalCaseModel
- **No cross-entity references** — Entities reference by Guid, navigation properties in DbContext

## ANTI-PATTERNS

- **Navigation property abuse** — Prefer Guid references; let DbContext handle joins
- **Domain logic in entities** — Only MedicalCaseModel has domain methods; keep others anemic
- **Missing soft-delete** — All entities should inherit IsDeleted from BaseEntity
