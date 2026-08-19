<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.Registrations

## Purpose

Patient registration (挂号) module for the TCM clinic desktop client. Manages the registration queue workflow: receptionists create registrations for patients, doctors view their personal queue and start consultations (接诊), and receptionists can cancel waiting registrations. The queue auto-refreshes every 30 seconds. Starting a visit creates a MedicalCase and navigates to the MedicalCaseWorkspace in Clinical editing mode. PRD: registration.md US-REG-001 through US-REG-006.

## Key Files

| File | Description |
|------|-------------|
| `RegistrationModule.cs` | Prism IModule entry point. Depends on AuthenticationModule. Registers IRegistrationService, RegistrationListViewModel, RegistrationListView, and RegistrationCreateDialog. |
| `ViewModels/RegistrationListViewModel.cs` | Queue display ViewModel. Role-aware (Receptionist sees all, Doctor sees own). Commands: Refresh, CreateRegistration (dialog), StartVisit (creates MedicalCase + navigates), CancelRegistration. Auto-refreshes via PeriodicTimer every 30s. |
| `Dialogs/RegistrationCreateDialogViewModel.cs` | Dialog ViewModel for creating a new registration. Patient search with autocomplete, doctor dropdown selection. Uses IPatientService and IUserService from LYBT.Desktop.Contracts.Services. |
| `Dialogs/RegistrationCreateDialog.xaml` | Registration creation dialog UI. |
| `Repositories/RegistrationRepository.cs` | Dual-mode repository (Remote via IRegistrationApi / Local via ILocalRegistrationApi). Uses IApiRouter to determine offline mode. |
| `Services/RegistrationService.cs` | IRegistrationService implementation wrapping IRegistrationRepository with error handling and logging. |
| `Views/RegistrationListView.xaml` | Registration queue list UI. |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `ViewModels/` | RegistrationListViewModel -- queue display and operations. |
| `Views/` | RegistrationListView -- the navigation target for the queue. |
| `Dialogs/` | RegistrationCreateDialog and its ViewModel -- modal registration creation. |
| `Repositories/` | RegistrationRepository -- dual-mode (remote/local) data access. |
| `Services/` | RegistrationService -- service layer wrapping repository with CommandResult pattern. |

## For AI Agents

### Working In This Directory

- **Cross-module dependencies**: Registration 通过 `LYBT.Desktop.Contracts.Services`（IPatientService/IUserService 已下沉 Contracts）使用患者搜索与医生列表，不再直接引用 Patients/Users/MedicalCase 模块；导航参数契约（WorkspaceMode/EditState/MedicalCaseNavigationParameters）已下沉 `LYBT.Desktop.Contracts`（A-18 批次2）。
- **Role-based behavior**: Receptionist/Admin/SuperAdmin see all queue items and can cancel. Doctor sees only their own queue and can start visits. Role is checked via `SessionManager.CurrentUser?.Role`.
- **StartVisit flow**: Calls `_registrationService.StartVisitAsync()` which returns a MedicalCaseId, then fetches full PatientDetailDto via `IPatientApi.GetPatientByIdAsync()`, then navigates to MedicalCaseWorkspace with Clinical mode + Editing state.
- **Cancel guard**: Only Receptionist can cancel, and only Waiting-status registrations with Source=Receptionist.
- **Dual-mode repository**: RegistrationRepository routes to IRegistrationApi (remote) or ILocalRegistrationApi (local) based on `IApiRouter.IsOffline`.

### Testing Requirements

- Test via `LYBT.Tests.Desktop` project.
- Verify queue loading with role-based filtering (DoctorId parameter).
- Verify StartVisit creates MedicalCase and triggers navigation with correct parameters.
- Verify CancelRegistration guard conditions (role + status + source).
- Test RegistrationCreateDialogViewModel: patient search, doctor list loading, confirm with validation.

### Common Patterns

- **CommandResult pattern**: All service methods return `CommandResult<T>` or `CommandResult` -- check `.Success` before accessing `.Data`.
- **Auto-refresh**: Uses `PeriodicTimer` with 30-second interval, started on `OnNavigatedTo`, stopped on `OnNavigatedFrom`.
- **CommunityToolkit MVVM**: Uses `[ObservableProperty]`, `[RelayCommand]` attributes (mixed with Prism base class).
- **Dialog registration**: `containerRegistry.RegisterDialog<TView, TViewModel>()` for modal dialogs shown via IDialogService.
- **Navigation parameters**: Uses `Dictionary<string, object>` with constants from `MedicalCaseNavigationParameters` and `ViewNames`.

## Dependencies

### Internal

| Dependency | Purpose |
|------------|---------|
| `LYBT.Desktop.Contracts` | IRegistrationService, IRegistrationRepository, IRegistrationApi, ILocalRegistrationApi, IPatientService, IUserService, IApiRouter, INavigationCoordinator, ISessionManager |
| `LYBT.Desktop.Contracts` | WorkspaceMode/EditState enums, MedicalCaseNavigationParameters (导航参数契约，A-18 批次2 下沉) |
| `LYBT.Desktop.Infrastructure` | ViewNames constants, Extensions |
| `LYBT.Desktop.Infrastructure` | NavigableViewModelBase, DialogViewModelBase base classes |
| `LYBT.Shared.Models` | RegistrationListDto, RegistrationDetailDto, RegistrationInputDto, PatientListDto, UserListDto, CommandResult |
| `LYBT.Shared.Primitives` | Shared constants and primitives |

### External

| Package | Purpose |
|---------|---------|
| `Prism.Core` / `Prism.DryIoc` / `Prism.Wpf` | MVVM framework, DI, navigation, dialog service |
| `CommunityToolkit.Mvvm` | [ObservableProperty], [RelayCommand] source generators |

<!-- MANUAL: -->
