# LYBT.Module.Patients - Server Patients Module

**Purpose**: Server-side patient management with Entity-direct-return optimization.

## Structure

```
LYBT.Module.Patients/
├── Interfaces/          # IPatientService, IPatientServiceOptimized, IPatientRepository
├── Services/            # PatientService (987 lines)
├── Repositories/        # PatientRepository
├── Mapping/             # PatientMapper (Mapperly)
└── PatientsModule.cs    # Module registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| CRUD + import/export | `Services/PatientService.cs` | 987 lines, largest in module |
| Entity-direct mode | `Interfaces/IPatientServiceOptimized.cs` | Performance optimization |
| Reference checking | `Services/PatientService.cs` | CheckReferenceAsync (MedicalCases) |
| Batch import | `Services/PatientService.cs` | EPPlus, HashSet dedup |

## CONVENTIONS

- **Entity-direct return** — IPatientServiceOptimized avoids DTO mapping overhead
- **Age is computed** — Patient.Age based on BirthDate, Mapperly ignores it, manual copy needed
- **Cross-module** — IPatientCrossModuleService exposes PatientBasicDto to MedicalCase
- **Pinyin auto-gen** — CreateAsync/UpdateAsync auto-generate PinYinCode

## ANTI-PATTERNS

- **Service too large** — 987 lines, consider splitting ImportExport/Reference services
- **Direct AppDbContext** — CheckReferenceAsync queries MedicalCases table directly
- **FindAsync with soft-delete** — Use IgnoreQueryFilters() for Restore operations
