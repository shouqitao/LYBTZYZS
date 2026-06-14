# LYBT.Module.Patients - Server Patients Module

**Purpose**: Server-side patient management with Entity-direct-return optimization.

## Structure

```
LYBT.Module.Patients/
├── Interfaces/          # IPatientService, IPatientRepository, IPatientImportExportService
├── Services/            # PatientService (600 lines), PatientImportExportService (414 lines)
├── Repositories/        # PatientRepository
├── Mapping/             # PatientMapper (Mapperly)
└── PatientsModule.cs    # Module registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| CRUD + search | `Services/PatientService.cs` | 600 lines |
| Import/export | `Services/PatientImportExportService.cs` | 414 lines, Excel import/export |
| Reference checking | `Services/PatientService.cs` | CheckReferenceAsync (MedicalCases) |
| Batch import | `Services/PatientService.cs` | EPPlus, HashSet dedup |

## CONVENTIONS

- **ImportExportService split** — PatientImportExportService handles Excel/JSON import/export separately
- **Age is computed** — Patient.Age based on BirthDate, Mapperly ignores it, manual copy needed
- **Cross-module** — IPatientCrossModuleService exposes PatientBasicDto to MedicalCase
- **Pinyin auto-gen** — CreateAsync/UpdateAsync auto-generate PinYinCode

## ANTI-PATTERNS

- **Service split** — PatientService (600 lines) + PatientImportExportService (414 lines)
- **Direct AppDbContext** — CheckReferenceAsync queries MedicalCases table directly
- **FindAsync with soft-delete** — Use IgnoreQueryFilters() for Restore operations
