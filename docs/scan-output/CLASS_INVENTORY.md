# CLASS INVENTORY - 113 files, 10 projects
Generated 2026-06-29

## 1. LYBT.Desktop.Admin (12 files)

AdminModule : IModule -- OnInitialized(), RegisterTypes()
AdminHomeViewModel : NavigableViewModelBase -- CurrentUserName, IsSysAdmin, IsNotSysAdmin; 7 NavigateToXxx Commands; LoadCurrentUserAsync()
SystemSettingsViewModel : NavigableViewModelBase -- SystemName, HospitalName, ContactPhone, AutoBackupEnabled, BackupPath, ClinicName, ClinicAddress, ClinicPhone, ClinicDepartment, LicenseNumber, ClinicEmail; SaveCommand, ResetCommand, BrowseBackupPathCommand
ISystemSettingsService -- SystemName, HospitalName, ContactPhone, AutoBackupEnabled, BackupPath; Save(), ResetToDefaults()
SystemSettingsService : ISystemSettingsService -- JSON file persistence; nested SystemSettings class
UserManagementView : UserControl, INavigationAware -- DefaultRoleFilter support
SystemSettingsView, PatientManagementView, MedicalCaseManagementView, HerbManagementView, FormulaManagementView, AdminHomeView : UserControl -- thin wrappers

## 2. LYBT.Desktop.Clinical (17 files)

ClinicalModule : IModule -- [ModuleDependency(Patients,MedicalCase)]
ClinicalHomeViewModel : NavigableViewModelBase -- CurrentUserName, TodayConsultationCount, PendingCaseCount; 8 commands incl StartMedicalCase, EditProfile, ChangePassword
PatientSelectionViewModel : NavigableViewModelBase, IWorkspaceHost -- Patient list, card reader, pending queue sub-VMs; StartMedicalCase with suspended-case handling
MedicalCaseWorkspaceViewModel : NavigableViewModelBase, IMedicalCaseWorkspaceContext, IWorkspaceHost -- Composite shell with ConsultationEditor, PrescriptionEditor, Commands child VMs; edit-mode FSM; 5-step workflow
ClinicalWorkspaceViewModel : NavigableViewModelBase -- Integrated workspace: patient list + consultation area; patient history cache
HistoryItem -- Date, Diagnosis, Summary
PatientSelectionWorkspaceContext : IMedicalCaseWorkspaceContext -- Read-only adapter for patient selection phase
PendingQueueViewModel : ChildViewModelBase -- Pending queue switching with auto-suspend
CardReaderViewModel : ChildViewModelBase -- Card reader init, manual/auto read, patient create/find
9 View code-behinds (thin wrappers + PatientDoubleClicked handlers)

## 3. LYBT.Desktop.Receptionist (3 files)

ReceptionistModule : IModule -- [ModuleDependency(Patients,Registration,CardReader)]
ReceptionistHomeViewModel : NavigableViewModelBase -- Stats, search, card reader, registration queue; 6 commands
RegistrationQueueItem -- Id, PatientName, DoctorName, CreatedAt, Status, WaitingTime
ReceptionistHomeView : UserControl

## 4. LYBT.Desktop.Sysadmin (6 files)

SysadminModule : IModule
SysadminHomeViewModel : NavigableViewModelBase -- Dashboard polling (30s interval); DbStatus, SystemInfo cards
LogLevelControlViewModel : NavigableViewModelBase -- CurrentLevel, StatusMessage; SetLevel, EnableDebug, DisableDebug commands
StatusCard : ObservableObject -- Title, Value, Status, IsHealthy
DashboardStatus : ObservableObject -- DbStatus, SystemInfo, IsLoading
SysadminHomeView, LogLevelControlView : UserControl

## 5. LYBT.Desktop.Shell (49 files)

App : PrismApplication -- Single-instance Mutex, module catalog (14 modules), role-based loading, Serilog config
MainWindowViewModel : CoreViewModelBase -- Login state, sidebar, theme, status bar; 18 delegated commands, 3 relay commands (Logout, RetryHealthCheck, ToggleSidebar)
AccountSettingsViewModel : CoreViewModelBase, INavigationAware -- Profile editing + password change; SaveProfile, ChangePassword, GoBack
NativeMethods -- P/Invoke: FindWindow, SetForegroundWindow, ShowWindow, IsIconic
StatusBarManager : ObservableObject, IDisposable -- API health, connection URL, mode display, time
NavigationManager : ObservableObject -- Role-based nav item builder using IRoleRegistry
MenuManager -- 13 command props (keyboard shortcuts), visibility by role
ApplicationInitializationService : IApplicationInitializationService -- Error handling, warmup, module coordinator
LoginCoordinator : ILoginCoordinator -- Full auth FSM; Login, Logout, session, token lifecycle
EmbeddedLocalWebApiService : IEmbeddedLocalWebApiService -- Kestrel in-process at localhost:5300
SnackbarService : ISnackbarService -- MDIX message queue wrapper
DialogHostService : IDialogHostService -- MaterialDesign DialogHost bridge
AppStartupOrchestrator -- Resolves IStartupPipeline, registers 6 steps, runs async
ThemeService : ObservableObject, IThemeService -- Dark/light mode toggle
StartupPipeline : IStartupPipeline -- Ordered steps with parallel groups, perf monitoring
6 StartupSteps: ErrorHandling(10), ModuleCoordinator(20/Parallel), CoreServices(30/Parallel), LocalWebApi(250), ApiHealthCheck(40), Warmup(50)
SessionLifecycleManager : ISessionLifecycleManager -- Session state FSM, token refresh, activity tracking
SessionBasedCurrentUserProvider : ICurrentUserProvider
ISessionLifecycleManager + SessionState enum + SessionStateChangedEventArgs + SessionDiagnostics record
ApplicationState enum -- 6 states
IHealthCheckCoordinator + HealthStatusChangedEventArgs
HealthCheckCoordinator : IHealthCheckCoordinator -- Tick-based health checks
ApiHealthMonitor : IApiHealthMonitor -- Circuit breaker pattern (Closed/Open/HalfOpen)
IApplicationBootstrapper + ApplicationBootstrapper -- Role-driven module loading via IRoleRegistry
StringResources (auto-generated) -- 40+ localized string props
6 Extension classes: UnifiedApiClient, ServiceCollection, PrismConfiguration, Logging, HttpService, DataSource
MessageDialogViewModel, InputDialogViewModel, ConfirmationDialogViewModel : DialogViewModelBase
MainWindow : Window -- Alt+F4 interception
AccountSettingsControl : UserControl -- PasswordBox bridging

## 6. LYBT.LocalWebAPI (22 files)

Program (top-level) -- Entry point
LocalWebApiProgram (static) -- CreateBuilder, CreateApplication (8 module registrations), InitializeDatabaseAsync, RunAsync
LocalJwtConfig (static) -- HMAC-SHA256, 365-day tokens, policy constants
LocalApiMapper (static) -- Entity-to-DTO extensions for Patient, User, Registration, MedicalCase
LocalWebApiSeedData (static) -- Seeds Herb, Formula, Patient
11 Controllers: Health, Diagnostics, Auth (login/logout/refresh/auto-login/validate), Users (delegates to BaseUsersController), Patients (7 endpoints), Herbs (8 endpoints), Formulas (11 endpoints incl clone/validate), MedicalCases (16 endpoints incl state transitions), Registrations (7 endpoints), Reports (3 endpoints), Configuration (in-memory k/v store)
6 Http Repositories: HttpUserRepository, HttpPatientRepository, HttpHerbRepository, HttpFormulaRepository, HttpMedicalCaseRepository, HttpRegistrationRepository -- all delegate to IApiClient

## 7-10. Tools (4 files)

ApiTester/Program -- Login + password reset for shouqitao/jjr
LoginTester/Program -- Verify password reset login
PasswordHashGenerator/Program -- BCrypt hash generation + verification + SQL output
UserInfoVerifier/Program -- Login + fetch user details; nested UserInfo class

TOTAL: 113 .cs files | ~85 classes/interfaces/enums/records | 10 projects
