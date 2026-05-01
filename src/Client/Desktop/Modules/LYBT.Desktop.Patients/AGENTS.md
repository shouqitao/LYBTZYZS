# LYBT.Desktop.Patients - Desktop Patients Module

**Purpose**: Desktop UI module for patient management with workflow entry point and card reader integration.

## Structure

```
LYBT.Desktop.Patients/
├── CommandHandlers/     # IPatientCommandHandler (dead code, not registered)
├── Controls/            # PatientMasterDetailControl, PatientEditControl, PatientSelectionControl, PatientViewControl
├── Interfaces/          # IPatientRepository, IPatientSearchCache, IPatientService
├── Mappers/             # PatientMapper (Mapperly)
├── Models/              # PatientDetailModel, PatientItem, PatientViewState, Display models
├── Repositories/        # PatientRepository (DataSource abstraction)
├── Services/            # PatientService, PatientSearchCache, PatientImportExecutor, PendingQueueManager, etc.
├── ViewModels/          # PatientMasterDetailViewModel, Components/ (Validator, Coordinator)
└── PatientsModule.cs    # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `PatientsModule.cs` | Registers all services + components |
| ViewModel logic | `ViewModels/PatientMasterDetailViewModel.cs` | MasterDetailViewModelBase derivative |
| Pending queue | `Services/PendingQueueManager.cs` | Waiting patient queue |
| Card reader | `Services/PatientCardReaderIntegration.cs` | ID card lookup/create |
| Medical case start | `ViewModels/Components/MedicalCaseStartCoordinator.cs` | Multi-doctor detection |
| Search cache | `Services/PatientSearchCache.cs` | LRU, 10 items, 5min expiry |

## CONVENTIONS

- **ViewModel base** — `MasterDetailViewModelBase<ListDto, DetailModel>` (V2 composition pattern)
- **Component architecture** — ViewModel split into Components (Validator, Coordinator) and Services
- **PatientItem.Age** — Computed from BirthDate (not stored), Mapper must ignore
- **DataSource abstraction** — Repository delegates to IPatientDataSource (Local/Remote)

## ANTI-PATTERNS

- **IPatientCommandHandler dead code** — Not registered in DI, actual business via PatientService
- **PatientViewState dead code** — Defined but no runtime consumers
- **Cross-module references** — MUST NOT reference other Desktop modules directly
