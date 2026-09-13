<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.Registrations

## Purpose

Patient registration (挂号) module for the TCM clinic desktop client. Manages the registration queue workflow: receptionists create registrations for patients, doctors view their personal queue and start consultations (接诊), and receptionists can cancel waiting registrations. The queue polls every 30 seconds and refreshes in real time via SignalR (US-REG-008, 15s fallback polling). Starting a visit creates a MedicalCase and navigates to the MedicalCaseWorkspace in Clinical editing mode. PRD: registration.md US-REG-001 through US-REG-006.

## Key Files

| File | Description |
|------|-------------|
| `RegistrationModule.cs` | Prism IModule entry point. `[ModuleDependency]` on AuthenticationModule + PatientsModule + UsersModule (runtime load order only). Registers IRegistrationService, RegistrationEditContext, ISignalRClient (singleton), RegistrationListViewModel, RegistrationListView, and RegistrationCreateDialog. |
| `ViewModels/RegistrationListViewModel.cs` | Queue display ViewModel. Role-aware (Receptionist/Admin/SuperAdmin see all, Doctor sees own). Commands: Refresh, CreateRegistration (dialog), StartVisit (creates MedicalCase + navigates), CancelRegistration. Auto-refreshes via PeriodicTimer (30s) and subscribes to `RegistrationRefreshedEvent`. |
| `Dialogs/RegistrationCreateDialogViewModel.cs` | Dialog ViewModel for creating a new registration. Patient search with autocomplete, doctor dropdown selection. Uses IPatientService and IUserService from LYBT.Desktop.Contracts.Services. |
| `Dialogs/RegistrationCreateDialog.xaml` | Registration creation dialog UI. |
| `Models/RegistrationDetailModel.cs` | Detail model (`ValidatableModelBase`). |
| `Models/Items/RegistrationEditContext.cs` | New-registration edit context (`ValidatableModelBase`, `CreateNew()` / `ToInputDto()`). |
| `Events/RegistrationRefreshedEvent.cs` | `PubSubEvent` published by SignalRClient; consumed by RegistrationListViewModel. |
| `Repositories/RegistrationRepository.cs` | `EntityApiClientRepositoryBase` + `IRegistrationRepository`, routed through `IApiClientRegistrations`. |
| `Services/RegistrationService.cs` | IRegistrationService implementation wrapping IRegistrationRepository with `CommandResult` + logging. |
| `Services/SignalRClient.cs` | `ISignalRClient`: connects `hubs/registration`, publishes `RegistrationRefreshedEvent`; falls back to 15s polling when disconnected. |
| `Views/RegistrationListView.xaml` | Registration queue list UI (navigation target). |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `ViewModels/` | RegistrationListViewModel -- queue display and operations. |
| `Views/` | RegistrationListView -- the navigation target for the queue. |
| `Dialogs/` | RegistrationCreateDialog and its ViewModel -- modal registration creation. |
| `Models/` + `Models/Items/` | RegistrationDetailModel, RegistrationEditContext. |
| `Events/` | RegistrationRefreshedEvent --待诊列表刷新通知. |
| `Repositories/` | RegistrationRepository -- API-client routed data access. |
| `Services/` | RegistrationService + SignalRClient. |

## For AI Agents

### Working In This Directory

- **Cross-module dependencies**: Registration 通过 `LYBT.Desktop.Contracts.Services`（IPatientService/IUserService 已下沉 Contracts）使用患者搜索与医生列表，不再直接引用 Patients/Users/MedicalCase 模块；导航参数契约（WorkspaceMode/EditState/MedicalCaseNavigationParameters）已下沉 `LYBT.Desktop.Contracts`（A-18 批次2）。`[ModuleDependency]` 仅声明运行时加载顺序。
- **Role-based behavior**: Receptionist/Admin/SuperAdmin see all queue items and can cancel. Doctor sees only their own queue and can start visits. Role is checked via `SessionManager.CurrentUser?.Role`.
- **StartVisit flow**: Calls `_registrationService.StartVisitAsync()` which returns a MedicalCaseId, then fetches full PatientDetailDto via `IPatientApi.GetPatientByIdAsync()`, then navigates to MedicalCaseWorkspace with Clinical mode + Editing state.
- **Cancel guard**: Only Receptionist can cancel, and only Waiting-status registrations with Source=Receptionist.
- **API-routed repository**: `RegistrationRepository : EntityApiClientRepositoryBase<...>` delegates to `Contracts.ApiClient.IApiClientRegistrations`; there is no Local/Remote branch anymore.
- **Real-time refresh**: `ISignalRClient.StartAsync(doctorId)` on doctor navigation; pushes and the 15s fallback poll both publish `RegistrationRefreshedEvent`. Keep the `SubscriptionToken` and unsubscribe in `Dispose`.

### Testing Requirements

- Test via `LYBT.Tests.Desktop` project.
- Verify queue loading with role-based filtering (DoctorId parameter).
- Verify StartVisit creates MedicalCase and triggers navigation with correct parameters.
- Verify CancelRegistration guard conditions (role + status + source).
- Test RegistrationCreateDialogViewModel: patient search, doctor list loading, confirm with validation.

### Common Patterns

- **CommandResult pattern**: All service methods return `CommandResult<T>` or `CommandResult` -- check `.Success` before accessing `.Data`.
- **Auto-refresh**: Uses `PeriodicTimer` with `QueueRefreshIntervalSeconds` (30s), started on `OnNavigatedToCore`, stopped on `OnNavigatedFromCore` (which also stops SignalR).
- **CommunityToolkit MVVM**: Uses `[ObservableProperty]`, `[RelayCommand]` attributes (mixed with Prism base class).
- **Dialog registration**: `containerRegistry.RegisterDialog<TView, TViewModel>()` for modal dialogs shown via IDialogService (shown by name `"RegistrationCreateDialog"`).
- **Navigation parameters**: Uses `Dictionary<string, object>` with constants from `MedicalCaseNavigationParameters` and `ViewNames`.

## Dependencies

### Internal

| Dependency | Purpose |
|------------|---------|
| `LYBT.Desktop.Contracts` | Services.IRegistrationService/IPatientService/IUserService/INavigationCoordinator, Repositories.IRegistrationRepository, ApiClient.IApiClientRegistrations, Enums/Models (WorkspaceMode, EditState, MedicalCaseNavigationParameters) |
| `LYBT.Desktop.Infrastructure` | Constants.ViewNames, Extensions, `NavigableViewModelBase` / `DialogViewModelBase` base classes |
| `LYBT.Desktop.Foundation` | Repositories.EntityApiClientRepositoryBase, Application, Security, ExceptionHandling (transitive/API-client layer) |
| `LYBT.Shared.Models` | RegistrationListDto, RegistrationDetailDto, RegistrationInputDto, PatientListDto, UserListDto, CommandResult, Enums |

### External

| Package | Purpose |
|---------|---------|
| `Prism.Core` / `Prism.DryIoc` / `Prism.Wpf` | MVVM framework, DI, navigation, dialog service |
| `CommunityToolkit.Mvvm` | [ObservableProperty], [RelayCommand] source generators |
| `Microsoft.AspNetCore.SignalR.Client` | US-REG-008 real-time queue notifications |

<!-- MANUAL: -->
