# LYBT.Desktop.MedicalCase - Desktop MedicalCase Module

**Purpose**: Desktop UI module for medical case management (DDD aggregate root: Consultation + Prescription). Also hosts the `Reports/` statistics sub-module (independent Prism module, currently a stub).

## Structure

```
LYBT.Desktop.MedicalCase/
├── Controls/            # MasterDetail / Edit / View controls + WorkflowStepIndicator
├── Dialogs/             # FormulaImportDialog, HistoryCopyDialog, UnsavedChangesDialog (+ their VMs)
├── Extensions/          # PrescriptionImportExtensions
├── Interfaces/          # IMedicalCaseService, IMedicalCaseDataProvider, IMedicalCaseWorkspaceContext, IEditModeStateMachine, IDataProvider, IValidatable
├── Mappers/             # Mapperly mappers + PrescriptionItemMapper (shared DTO↔Model)
├── Models/              # Detail model, navigation parameters, workspace states, Items/ (EditContext, ConsultationItem, PrescriptionItemModel)
├── Repositories/        # MedicalCaseRepository (ApiClientRepositoryBase + IApiClientMedicalCases)
├── Reports/             # ReportsModule (STUB) + Views/ViewModels/Repositories/Services for 报表
├── Services/            # MedicalCaseService aggregate proxy + Query/Command/Lifecycle + AuditLogService
├── ViewModels/          # MasterDetail + AuditLog + Items/PrescriptionItemViewModel + Components/ (state machine, print handler) + Workspace/ (3 child VMs)
├── Views/               # MedicalCaseMasterDetailView, AuditLogView
└── MedicalCaseModule.cs # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `MedicalCaseModule.cs` | `[ModuleDependency("PatientsModule")]` + `("CatalogModule")` (runtime order only; no cross-module ProjectReference) |
| ViewModels | `ViewModels/` | `MasterDetailViewModelBase` derivative + `ChildViewModelBase` workspace VMs |
| Aggregate orchestration | `Services/MedicalCaseService.cs` | Delegates to Query/Command/Lifecycle services; owns `AggregateSaveAsync` |
| Edit session | `Models/Items/MedicalCaseEditContext.cs` | Singleton in module DI; BeginEdit/Commit/Cancel/IsDirty baseline |
| Edit state machine | `ViewModels/Components/EditModeStateMachine.cs` | 6 states × 10 events, transition-table driven |
| Printing | `ViewModels/Components/PrescriptionPrintHandler.cs` | `IPrintService<PrescriptionPrintModel>`, draft watermark |
| Prescription import | `Extensions/PrescriptionImportExtensions.cs` + `Dialogs/FormulaImportDialog*` | Import logic (Validated + Enabled formulas only) |
| Reports (stub) | `Reports/` | `ReportsModule` registers `ReportsHomeView` navigation only |
| Workspace data contract | `Interfaces/IMedicalCaseDataProvider.cs` | Implemented by `Clinical.MedicalCaseWorkspaceViewModel`; consumed by `MedicalCaseCommandsViewModel` |

## CONVENTIONS

- **ViewModel base** — `MasterDetailViewModelBase<TListDto, TDetailModel>`; workspace children use `ChildViewModelBase`
- **Edit sub-VMs** — data flows through `IMedicalCaseDataProvider` (not `Func<>` delegates anymore); CanExecute across child-VM boundaries needs a manual `Commands.RefreshCanExecute()`
- **Object mapping** — Riok.Mapperly (compile-time; AutoMapper is forbidden). `PrescriptionItemMapper` holds the shared 14-field DTO↔Model mapping
- **Navigation** — Prism Region-based via `MedicalCaseNavigationParameters` (in `LYBT.Desktop.Contracts`)
- **Data access** — `MedicalCaseRepository` routed through `Contracts.ApiClient.IApiClientMedicalCases`

## ANTI-PATTERNS

- **Direct repository access in ViewModel** — go through `IMedicalCaseService`; bypassing `AggregateSaveAsync` leaves the DTO snapshot / edit baseline inconsistent
- **Cross-module references** — MUST NOT reference other Desktop modules; use `LYBT.Desktop.Contracts` interfaces
- **Creating a medical case from the management view** — `CreateNewDetail` throws `NotSupportedException`
- **`[ObservableProperty]` on Mapperly-mapped Items** — Mapperly cannot see source-generated members; keep `[MapperIgnoreSource/Target]` + manual mapping (`PrescriptionItemViewModel` stays `BindableBase`)

<!-- MANUAL: -->
