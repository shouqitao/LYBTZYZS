# LYBT.Desktop.Clinical - Desktop Clinical Workspace

**Purpose**: Doctor (and Receptionist) role workspace module. Hosts the clinical workflow (patient selection → medical case workspace) plus the thin-wrapper management views and the receptionist front desk home.

## Structure

```
LYBT.Desktop.Clinical/
├── Views/                  # ClinicalHomeView, ClinicalWorkspaceView, PatientSelectionView,
│                           #   MedicalCaseWorkspaceView + 4 thin wrappers (Herb/Formula/Patient/MedicalCase Manag.)
├── ViewModels/             # ClinicalHomeViewModel, ClinicalWorkspaceViewModel, MedicalCaseWorkspaceViewModel,
│                           #   PatientSelectionViewModel + Workspace/ (PendingQueue, CardReader, StateManager, NavHandler)
├── Receptionist/           # ReceptionistHomeView + ReceptionistHomeViewModel (原独立模块，已并入)
└── ClinicalModule.cs       # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `ClinicalModule.cs` | `[ModuleDependency]` on Patients/MedicalCase/Registration/CardReader (runtime order) |
| Doctor home | `RoleRegistry` → `ViewNames.ClinicalWorkspace` | `ClinicalWorkspaceView` is the Doctor home; `ClinicalHomeView` 仍注册但非首页 |
| Receptionist home | `Receptionist/Views/ReceptionistHomeView.xaml` | `ReceptionistRoleDefinition.HomeViewName` |
| Unified workspace | `Views/ClinicalWorkspaceView.xaml` | Left: `PatientSelectionControl` (Patients module); Right: consultation area |
| Case workspace | `ViewModels/MedicalCaseWorkspaceViewModel.cs` | Implements `IMedicalCaseWorkspaceContext` / `IWorkspaceHost` / `IMedicalCaseDataProvider` |
| Pending queue | `ViewModels/Workspace/PendingQueueViewModel.cs` | Uses `IRegistrationService.GetQueueAsync(doctorId)`; auto-suspend on patient switch |
| Card reader | `ViewModels/Workspace/CardReaderViewModel.cs` | Manual + auto read; `MaskIdNumber` |

## CONVENTIONS

- **Control reuse** — thin-wrapper views embed business-module `Controls/` (View 在角色台，Control 在业务模块); Clinical compiles against Patients/Catalog/MedicalCase/Registrations
- **Role-based loading** — `ClinicalModule` is loaded `WhenAvailable` in `App.ConfigureModuleCatalog`
- **Workflow-driven** — layout follows patient → consultation → prescription
- **Child VMs** — `PendingQueueViewModel` / `CardReaderViewModel` derive from `ChildViewModelBase` and talk to the host via `IWorkspaceHost`

## ANTI-PATTERNS

- **Business logic in workspace** — logic stays in business modules; the workspace is layout + orchestration only
- **Direct module coupling from business modules** — Clinical may reference business modules (it hosts their Controls), never the reverse
- **Assuming ClinicalHomeView is the Doctor home** — check `RoleRegistry`/`DoctorRoleDefinition.HomeViewName` instead

<!-- MANUAL: -->
