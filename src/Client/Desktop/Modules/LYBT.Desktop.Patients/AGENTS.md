# LYBT.Desktop.Patients - Desktop Patients Module

**Purpose**: Desktop UI module for patient management with workflow entry point and card reader integration. Exposes embeddable `Controls/` only — it registers no navigation views and no dialogs.

## Structure

```
LYBT.Desktop.Patients/
├── Controls/            # PatientMasterDetailControl, PatientEditControl, PatientSelectionControl, PatientViewControl
├── Mappers/             # PatientMapper (Mapperly, DTO↔Model↔InputDto)
├── Models/              # PatientDetailModel, Items/PatientEditContext (导入进度模型在 Desktop.Infrastructure/Models)
├── Repositories/        # PatientRepository (EntityApiClientRepositoryBase + IApiClientPatients)
├── Services/            # PatientService, PatientCardReaderIntegration, PatientExcelService (B-12)
├── ViewModels/          # PatientMasterDetailViewModel, PatientEditorViewModel, PatientCardReaderViewModel, Handlers/
└── PatientsModule.cs    # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `PatientsModule.cs` | `[ModuleDependency("AuthenticationModule")]` + `("UsersModule")`; registers services, validator, mapper, MasterDetail, 3 VMs |
| ViewModel logic | `ViewModels/PatientMasterDetailViewModel.cs` | MasterDetailViewModelBase derivative |
| Card reader | `Services/PatientCardReaderIntegration.cs` | ID card dedupe chain (PRD-15) |
| Card reader VM | `ViewModels/PatientCardReaderViewModel.cs` | ReadCardCommand / FindPatientByIdNumberAsync / MaskIdNumber |
| Excel import/export | `Services/PatientExcelService.cs` | B-12: template/parse/export `.xlsx`; backend keeps JSON contract |
| Status handler | `ViewModels/Handlers/PatientStatusHandler.cs` | Restore only (no ToggleStatus) |

## CONVENTIONS

- **ViewModel base** — `MasterDetailViewModelBase<ListDto, DetailModel>`; edit sub-VM uses `EditorViewModelBase<PatientEditContext>`
- **Component architecture** — status handling split into `ViewModels/Handlers/`; read-card concerns split into `PatientCardReaderViewModel`
- **Data access** — `PatientRepository : EntityApiClientRepositoryBase<...>` routed through `Contracts.ApiClient.IApiClientPatients` (no Local/Remote branch)
- **Embedded UI** — role workspaces embed `PatientMasterDetailControl` / `PatientSelectionControl`; VM resolved via `ViewModelLocationProvider.Register`

## ANTI-PATTERNS

- **Cross-module references** — MUST NOT reference other Desktop modules directly; use `LYBT.Desktop.Contracts` interfaces
- **Assuming a Local fallback exists** — batch import/export/template go straight to the API and return null on failure

<!-- MANUAL: -->
