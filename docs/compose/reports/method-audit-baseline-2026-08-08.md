# A-30-S0 方法级统计基线 — 方法总账

> 生成：Mimo Code（S0 只读审查）｜任务书：`task-a30-s0-method-baseline-2026-08-08.md`
> 基线 commit：`7edf020f5`（HEAD `4a956a851` 仅为任务书文档提交，代码树无差异）｜生成时间：2026-08-09 02:10
> 性质：**只读统计**，未修改任何 src/tests 代码；脚本内嵌见文末附录
> 统计口径：源码级正则（PowerShell）+ Roslyn 采样校准，误差率见 §0 口径说明

## 0. 统计口径与误差率说明

| 项 | 说明 |
|----|------|
| 范围 | `src/`（35 项目：Shared 5 + Server 10 + Desktop 16 + Tools 1）+ `tests/`（3 项目）全部 `.cs`；排除 `bin/` `obj/` `Migrations/`；`GlobalUsings.cs`×3（层级 using 文件）计入扫描但无类型/方法 |
| 扫描文件数 | 1208（src 1028 + tests 180） |
| 类型口径 | 源码级正则匹配 `class/interface/record/struct/enum` 声明（含嵌套）；Roslyn 采样 5 文件类型 **0 误差** |
| 方法口径 | 源码级正则匹配类型体内方法声明（含构造函数/接口方法），跨行签名括号平衡收集；**不含**属性访问器（get/set）|
| Roslyn 采样校准 | 5 文件（Server Handler/Controller/Desktop VM/Shared DTO/Repository 各 1）：34 方法中正则命中 33，**miss=1**（`CheckOwnershipAsync` 元组返回类型 `Task<(X,Y)>` 正则漏报），方法漏报率 **2.9%**；可见性 0 误差 |
| 已知误差源 | ① 元组返回类型方法（全仓 8 文件含 `Task<(`，如 MedicalCase 三 Service）可能漏报；② 局部函数/表达式体方法偶发误判；③ `var`/`if` 等关键字行已用排除词表过滤 |
| 判定红线 | 本报告**不判定**任何方法应删/应合并/应移动（S1-S4 工作）；仅提供统计基线 |

## 1. 全仓汇总（35 项目）

| 层 | 项目 | 类型数 | 方法数 | 公开 | 私有/内部 | 平均方法/类 |
|----|------|-------|--------|------|----------|------------|
| Desktop | LYBT.Desktop.Admin | 19 | 61 | 29 | 32 | 3.21 |
| Desktop | LYBT.Desktop.Auth | 10 | 57 | 15 | 42 | 5.7 |
| Desktop | LYBT.Desktop.Clinical | 23 | 140 | 56 | 84 | 6.09 |
| Desktop | LYBT.Desktop.Contracts | 110 | 487 | 27 | 460 | 4.43 |
| Desktop | LYBT.Desktop.Controls | 46 | 149 | 77 | 72 | 3.24 |
| Desktop | LYBT.Desktop.Formula | 14 | 77 | 45 | 32 | 5.5 |
| Desktop | LYBT.Desktop.Foundation | 108 | 511 | 377 | 134 | 4.73 |
| Desktop | LYBT.Desktop.Herbs | 13 | 59 | 38 | 21 | 4.54 |
| Desktop | LYBT.Desktop.Infrastructure | 127 | 596 | 286 | 310 | 4.69 |
| Desktop | LYBT.Desktop.MedicalCase | 55 | 240 | 141 | 99 | 4.36 |
| Desktop | LYBT.Desktop.Patients | 19 | 73 | 47 | 26 | 3.84 |
| Desktop | LYBT.Desktop.Printing | 17 | 61 | 40 | 21 | 3.59 |
| Desktop | LYBT.Desktop.Registrations | 10 | 58 | 23 | 35 | 5.8 |
| Desktop | LYBT.Desktop.Shell | 55 | 232 | 105 | 127 | 4.22 |
| Desktop | LYBT.Desktop.Users | 15 | 77 | 51 | 26 | 5.13 |
| Shared | LYBT.Entities | 18 | 39 | 39 | 0 | 2.17 |
| Server | LYBT.Infrastructure | 71 | 210 | 98 | 112 | 2.96 |
| Desktop | LYBT.LocalWebAPI | 24 | 109 | 106 | 3 | 4.54 |
| Server | LYBT.Module.Auth | 25 | 52 | 34 | 18 | 2.08 |
| Server | LYBT.Module.Formula | 36 | 54 | 41 | 13 | 1.5 |
| Server | LYBT.Module.Herbs | 38 | 72 | 54 | 18 | 1.89 |
| Server | LYBT.Module.MedicalCase | 22 | 173 | 98 | 75 | 7.86 |
| Server | LYBT.Module.Patients | 29 | 53 | 37 | 16 | 1.83 |
| Server | LYBT.Module.Registration | 27 | 69 | 54 | 15 | 2.56 |
| Server | LYBT.Module.Reports | 11 | 46 | 23 | 23 | 4.18 |
| Server | LYBT.Module.Users | 37 | 91 | 63 | 28 | 2.46 |
| Shared | LYBT.Shared.Configuration | 33 | 9 | 8 | 1 | 0.27 |
| Shared | LYBT.Shared.ExceptionHandling | 7 | 61 | 60 | 1 | 8.71 |
| Shared | LYBT.Shared.Logging | 10 | 38 | 28 | 10 | 3.8 |
| Shared | LYBT.Shared.Models | 142 | 65 | 59 | 6 | 0.46 |
| Tests | LYBT.Tests.Architecture | 8 | 87 | 81 | 6 | 10.88 |
| Tests | LYBT.Tests.Desktop | 126 | 1177 | 1024 | 153 | 9.34 |
| Tests | LYBT.Tests.Server | 84 | 681 | 603 | 78 | 8.11 |
| Tools | LYBT.Tools.PasswordHashGenerator | 1 | 4 | 0 | 4 | 4 |
| Server | LYBT.WebAPI | 34 | 150 | 124 | 26 | 4.41 |
| **合计** | **35** | **1424** | **6118** | **3991** | **2127** | — |

> 注：`private` 列=非 public（含 private/protected/internal/接口默认）；公开率 65.2%

## 2. 方法全量清单（按项目分组）

格式：`行号 | 可见性 | 返回类型 方法名(参数类型列表) | 类`

### LYBT.Desktop.Admin

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 13 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | AdminModule |
| 18 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | AdminModule |
| 38 | public (interface 默认) | ` void Save();` | ISystemSettingsService |
| 43 | public (interface 默认) | ` void ResetToDefaults();` | ISystemSettingsService |
| 18 | public | ` public SystemSettingsService(ILogger<SystemSettingsService> logger)` | SystemSettingsService |
| 105 | public | ` public void Save()` | SystemSettingsService |
| 123 | public | ` public void ResetToDefaults()` | SystemSettingsService |
| 137 | private | ` private SystemSettings LoadSettings()` | SystemSettingsService |
| 164 | private | ` private static SystemSettings CreateDefaultSettings()` | SystemSettingsService |
| 14 | public | ` public AuthHealthService(IApiClientAuth authApi)` | AuthHealthService |
| 19 | public | ` public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync(CancellationToken ct = default)` | AuthHealthService |
| 13 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | SysadminModule |
| 18 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | SysadminModule |
| 27 | public | ` public DeploymentViewModel(IViewModelServices services, IApiClient apiClient, INavigationCoordinator navigationCoordinator)` | DeploymentViewModel |
| 36 | private | ` private void SelectFile()` | DeploymentViewModel |
| 47 | private | ` private async Task UploadAsync()` | DeploymentViewModel |
| 86 | private | ` private async Task RestartAsync()` | DeploymentViewModel |
| 109 | private | ` private void GoBack() => _navigationCoordinator.NavigateBack();` | DeploymentViewModel |
| 25 | public | ` public LogLevelControlViewModel(IViewModelServices services, IApiClient apiClient)` | LogLevelControlViewModel |
| 32 | public | ` public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)` | LogLevelControlViewModel |
| 38 | private | ` private async Task LoadStatusAsync()` | LogLevelControlViewModel |
| 59 | private | ` private async Task SetLevelAsync(string level)` | LogLevelControlViewModel |
| 78 | private | ` private async Task EnableDebugAsync()` | LogLevelControlViewModel |
| 97 | private | ` private async Task DisableDebugAsync()` | LogLevelControlViewModel |
| 24 | public | ` public SysadminHomeViewModel( IViewModelServices services, IAuthHealthService authHealthService, IClinicSettingsService clinicSettings)` | SysadminHomeViewModel |
| 35 | public | ` public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)` | SysadminHomeViewModel |
| 41 | public | ` public override void OnNavigatedFrom(Prism.Regions.NavigationContext navigationContext)` | SysadminHomeViewModel |
| 47 | private | ` private void StartPolling()` | SysadminHomeViewModel |
| 55 | private | ` private void StopPolling() => _pollCts?.Cancel();` | SysadminHomeViewModel |
| 57 | private | ` private async Task PollDashboardAsync(CancellationToken ct)` | SysadminHomeViewModel |
| 7 | public | ` public DeploymentView() => InitializeComponent();` | DeploymentView |
| 10 | public | ` public LogLevelControlView()` | LogLevelControlView |
| 10 | public | ` public SysadminHomeView()` | SysadminHomeView |
| 46 | public | ` public AdminHomeViewModel( IViewModelServices services, IAuthenticationService authService, IDialogService dialogService, INavigationCoordinator navi…` | AdminHomeViewModel |
| 69 | private | ` private void NavigateToUserManagement() => NavigateTo(ViewNames.UserManagement);` | AdminHomeViewModel |
| 75 | private | ` private void NavigateToHerbManagement() => NavigateTo(ViewNames.HerbManagement);` | AdminHomeViewModel |
| 81 | private | ` private void NavigateToPatientManagement() => NavigateTo(ViewNames.PatientManagement);` | AdminHomeViewModel |
| 87 | private | ` private void NavigateToFormulaManagement() => NavigateTo(ViewNames.FormulaManagement);` | AdminHomeViewModel |
| 93 | private | ` private void NavigateToMedicalCaseManagement() => NavigateTo(ViewNames.MedicalCaseManagement);` | AdminHomeViewModel |
| 99 | private | ` private void NavigateToSystemSettings() => NavigateTo(ViewNames.SystemSettings);` | AdminHomeViewModel |
| 105 | private | ` private void NavigateToReports() => NavigateTo(ViewNames.ReportsHome);` | AdminHomeViewModel |
| 115 | private | ` private void NavigateTo(string viewName)` | AdminHomeViewModel |
| 133 | private | ` private async Task LoadCurrentUserAsync()` | AdminHomeViewModel |
| 161 | public | ` public override void OnNavigatedTo(NavigationContext navigationContext)` | AdminHomeViewModel |
| 167 | public | ` public override bool IsNavigationTarget(NavigationContext navigationContext)` | AdminHomeViewModel |
| 172 | public | ` public override void OnNavigatedFrom(NavigationContext navigationContext)` | AdminHomeViewModel |
| 141 | public | ` public SystemSettingsViewModel( IViewModelServices services, ISystemSettingsService settingsService, IClinicSettingsService clinicSettingsService, IA…` | SystemSettingsViewModel |
| 159 | protected | ` protected override async Task InitializeAsync(NavigationContext context)` | SystemSettingsViewModel |
| 187 | private | ` private void LoadClinicSettings()` | SystemSettingsViewModel |
| 203 | private | ` private async Task SaveAsync()` | SystemSettingsViewModel |
| 250 | private | ` private async Task ResetAsync()` | SystemSettingsViewModel |
| 288 | private | ` private async Task BrowseBackupPathAsync()` | SystemSettingsViewModel |
| 314 | private | ` private async Task LoadServerConfigAsync()` | SystemSettingsViewModel |
| 353 | private | ` private async Task SaveServerConfigAsync()` | SystemSettingsViewModel |
| 386 | private | ` private async Task ValidateConfigAsync()` | SystemSettingsViewModel |
| 11 | public | ` public AdminHomeView()` | AdminHomeView |
| 10 | public | ` public SystemSettingsView()` | SystemSettingsView |
| 15 | public | ` public UserManagementView()` | UserManagementView |
| 20 | public | ` public void OnNavigatedTo(NavigationContext navigationContext)` | UserManagementView |
| 29 | public | ` public bool IsNavigationTarget(NavigationContext navigationContext) => true;` | UserManagementView |
| 31 | public | ` public void OnNavigatedFrom(NavigationContext navigationContext) { }` | UserManagementView |
### LYBT.Desktop.Auth

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 20 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | AuthenticationModule |
| 28 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | AuthenticationModule |
| 37 | public | ` public ConnectionStatusViewModel( IViewModelServices services, IApplicationStateService applicationStateService, IConnectionModeService? connectionMo…` | ConnectionStatusViewModel |
| 60 | public | ` public async Task LoadApiStatusAsync()` | ConnectionStatusViewModel |
| 92 | public | ` public async Task DetectConnectionModeAsync()` | ConnectionStatusViewModel |
| 125 | private | ` private void SwitchToLocal()` | ConnectionStatusViewModel |
| 152 | private | ` private void SwitchToRemote()` | ConnectionStatusViewModel |
| 179 | private | ` private async Task RetryApiCheckAsync()` | ConnectionStatusViewModel |
| 213 | private | ` private void OnApiStatusChanged(object? sender, ApiStatusChangedEventArgs e)` | ConnectionStatusViewModel |
| 237 | private | ` private void OnConnectionModeChanged(object? sender, ConnectionMode e)` | ConnectionStatusViewModel |
| 255 | protected | ` protected override void OnDisposing()` | ConnectionStatusViewModel |
| 40 | public | ` public FirstRunSetupViewModel( IViewModelServices services, IConnectionModeService connectionModeService, IConnectionSettingsService connectionSettin…` | FirstRunSetupViewModel |
| 51 | protected | ` protected override void OnDialogOpenedCore(IDialogParameters? parameters)` | FirstRunSetupViewModel |
| 60 | default(private) | ` partial void OnTestStatusChanged(ConnectionTestStatus value)` | FirstRunSetupViewModel |
| 67 | default(private) | ` partial void OnIsRemoteAvailableChanged(bool value)` | FirstRunSetupViewModel |
| 76 | private | ` private async Task TestConnectionAsync()` | FirstRunSetupViewModel |
| 114 | private | ` private bool CanTestConnection() => TestStatus != ConnectionTestStatus.Testing;` | FirstRunSetupViewModel |
| 119 | protected | ` protected override bool CanConfirm() =>` | FirstRunSetupViewModel |
| 122 | protected | ` protected override void Confirm()` | FirstRunSetupViewModel |
| 142 | private | ` private void UseLocalMode()` | FirstRunSetupViewModel |
| 157 | private | ` private async Task SaveRemoteAsync()` | FirstRunSetupViewModel |
| 36 | public | ` public LoginCredentialsViewModel( IViewModelServices services, IUsernameStorageService? usernameStorage, ICredentialVault? credentialVault)` | LoginCredentialsViewModel |
| 49 | public | ` public async Task LoadSavedCredentialsAsync()` | LoginCredentialsViewModel |
| 95 | public | ` public async Task SaveCredentialsAsync(bool rememberPassword)` | LoginCredentialsViewModel |
| 128 | default(private) | ` partial void OnRememberUsernameChanged(bool value)` | LoginCredentialsViewModel |
| 139 | default(private) | ` partial void OnRememberPasswordChanged(bool value)` | LoginCredentialsViewModel |
| 154 | default(private) | ` partial void OnUsernameChanged(string value)` | LoginCredentialsViewModel |
| 165 | private | ` private async Task ClearSavedUsernameAsync()` | LoginCredentialsViewModel |
| 178 | private | ` private async Task ClearSavedPasswordAsync()` | LoginCredentialsViewModel |
| 37 | private | ` private static void MarkFirstRunCompleted()` | LoginViewModel |
| 146 | public | ` public LoginViewModel( IViewModelServices services, ILoginCoordinator loginCoordinator, IApplicationStateService applicationStateService, IUsernameSt…` | LoginViewModel |
| 187 | private | ` private async Task BackgroundInitAsync()` | LoginViewModel |
| 203 | private | ` private async Task MaybeShowFirstRunSetupAsync()` | LoginViewModel |
| 235 | public | ` public override void OnNavigatedTo(NavigationContext navigationContext)` | LoginViewModel |
| 240 | private | ` private async Task ExecuteLoginAsync()` | LoginViewModel |
| 270 | private | ` private void ExecuteOpenSettings()` | LoginViewModel |
| 284 | private | ` private async Task ExecuteCloseApplicationAsync()` | LoginViewModel |
| 296 | private | ` private void OnCredentialsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)` | LoginViewModel |
| 301 | private | ` private void OnConnectionStatusPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)` | LoginViewModel |
| 308 | protected | ` protected override void OnDisposing()` | LoginViewModel |
| 34 | public | ` public ServerConfigViewModel( IViewModelServices services, IConnectionModeService connectionModeService, IConnectionSettingsService connectionSetting…` | ServerConfigViewModel |
| 45 | protected | ` protected override void OnDialogOpenedCore(IDialogParameters? parameters)` | ServerConfigViewModel |
| 59 | default(private) | ` partial void OnTestStatusChanged(ConnectionTestStatus value)` | ServerConfigViewModel |
| 69 | protected | ` protected override void OnIsLoadingChangedCore(bool value)` | ServerConfigViewModel |
| 78 | protected | ` protected override void OnIsBusyChangedCore(bool value)` | ServerConfigViewModel |
| 88 | private | ` private async Task TestConnectionAsync()` | ServerConfigViewModel |
| 122 | private | ` private bool CanTestConnection() => TestStatus != ConnectionTestStatus.Testing;` | ServerConfigViewModel |
| 127 | protected | ` protected override bool CanConfirm() =>` | ServerConfigViewModel |
| 133 | protected | ` protected override void Confirm()` | ServerConfigViewModel |
| 143 | private | ` private async Task SaveAndEnableAsync()` | ServerConfigViewModel |
| 171 | private | ` private bool CanSaveOnly() =>` | ServerConfigViewModel |
| 178 | private | ` private async Task SaveOnlyAsync()` | ServerConfigViewModel |
| 10 | public | ` public FirstRunSetupView()` | FirstRunSetupView |
| 11 | public | ` public LoginView()` | LoginView |
| 18 | private | ` private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)` | LoginView |
| 28 | private | ` private void OnPasswordChanged(object sender, RoutedEventArgs e)` | LoginView |
| 10 | public | ` public ServerConfigView()` | ServerConfigView |
### LYBT.Desktop.Clinical

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 17 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | ClinicalModule |
| 22 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | ClinicalModule |
| 59 | public | ` public ReceptionistHomeViewModel( IViewModelServices services, INavigationCoordinator navigationCoordinator, IRegistrationService registrationService…` | ReceptionistHomeViewModel |
| 79 | private | ` private void LoadCurrentUser()` | ReceptionistHomeViewModel |
| 88 | private | ` private void NavigateToPatientManagement()` | ReceptionistHomeViewModel |
| 92 | private | ` private void NavigateToRegistrationQueue()` | ReceptionistHomeViewModel |
| 96 | private | ` private async Task NavigateToCardReaderAsync()` | ReceptionistHomeViewModel |
| 151 | private | ` private void CreateNewPatient()` | ReceptionistHomeViewModel |
| 157 | private | ` private void CreateNewRegistration()` | ReceptionistHomeViewModel |
| 163 | private | ` private async Task SearchPatientAsync()` | ReceptionistHomeViewModel |
| 224 | private | ` private async Task ProcessCardReaderResultAsync(CardReadResult cardResult)` | ReceptionistHomeViewModel |
| 293 | private | ` private static string MaskIdNumber(string? idNumber)` | ReceptionistHomeViewModel |
| 300 | protected | ` protected override async Task InitializeAsync(NavigationContext context)` | ReceptionistHomeViewModel |
| 306 | private | ` private async Task LoadStatisticsAsync()` | ReceptionistHomeViewModel |
| 327 | private | ` private async Task LoadRegistrationQueueAsync()` | ReceptionistHomeViewModel |
| 362 | private | ` private void NavigateToView(string viewName, IDictionary<string, object>? parameters = null)` | ReceptionistHomeViewModel |
| 10 | public | ` public ReceptionistHomeView()` | ReceptionistHomeView |
| 59 | public | ` public ClinicalHomeViewModel( IViewModelServices services, IAuthenticationService authService, IDialogService dialogService, INavigationCoordinator n…` | ClinicalHomeViewModel |
| 87 | private | ` private void StartMedicalCase()` | ClinicalHomeViewModel |
| 97 | private | ` private void NavigateToPatientManagement()` | ClinicalHomeViewModel |
| 107 | private | ` private void NavigateToMedicalCaseQuery()` | ClinicalHomeViewModel |
| 117 | private | ` private void NavigateToHerbLibrary()` | ClinicalHomeViewModel |
| 127 | private | ` private void NavigateToFormulaLibrary()` | ClinicalHomeViewModel |
| 138 | private | ` private void NavigateToRegistrationQueue()` | ClinicalHomeViewModel |
| 148 | private | ` private void EditProfile()` | ClinicalHomeViewModel |
| 158 | private | ` private void ChangePassword()` | ClinicalHomeViewModel |
| 172 | private | ` private async Task LoadCurrentUserAsync()` | ClinicalHomeViewModel |
| 196 | private | ` private void LoadTodayStatistics()` | ClinicalHomeViewModel |
| 207 | public | ` public override void OnNavigatedTo(NavigationContext navigationContext)` | ClinicalHomeViewModel |
| 214 | public | ` public override bool IsNavigationTarget(NavigationContext navigationContext)` | ClinicalHomeViewModel |
| 219 | public | ` public override void OnNavigatedFrom(NavigationContext navigationContext)` | ClinicalHomeViewModel |
| 92 | public | ` public ClinicalWorkspaceViewModel( IViewModelServices services, IPatientService patientService, INavigationCoordinator navigationCoordinator, IMedica…` | ClinicalWorkspaceViewModel |
| 114 | default(private) | ` partial void OnSelectedPatientChanged(PatientListDto? value)` | ClinicalWorkspaceViewModel |
| 127 | private | ` private void StartConsultation()` | ClinicalWorkspaceViewModel |
| 143 | private | ` private bool CanStartConsultation() => SelectedPatient != null;` | ClinicalWorkspaceViewModel |
| 147 | private | ` private void NewPatient()` | ClinicalWorkspaceViewModel |
| 163 | private | ` private async Task RefreshAsync() => await LoadPatientsAsync();` | ClinicalWorkspaceViewModel |
| 167 | private | ` private async Task SearchAsync() => await LoadPatientsAsync();` | ClinicalWorkspaceViewModel |
| 174 | private | ` private async Task LoadPatientsAsync()` | ClinicalWorkspaceViewModel |
| 222 | private | ` private async Task LoadPatientDetailAsync()` | ClinicalWorkspaceViewModel |
| 248 | private | ` private async Task LoadPatientHistoryAsync()` | ClinicalWorkspaceViewModel |
| 299 | private | ` private static string BuildSummary(MedicalCaseListDto c)` | ClinicalWorkspaceViewModel |
| 312 | private | ` private void OnCacheInvalidated(CacheInvalidatedPayload payload)` | ClinicalWorkspaceViewModel |
| 327 | public | ` public override void OnNavigatedTo(NavigationContext navigationContext)` | ClinicalWorkspaceViewModel |
| 333 | public | ` public override bool IsNavigationTarget(NavigationContext navigationContext) => true;` | ClinicalWorkspaceViewModel |
| 335 | public | ` public override void OnNavigatedFrom(NavigationContext navigationContext)` | ClinicalWorkspaceViewModel |
| 254 | public | ` public MedicalCaseWorkspaceViewModel( IViewModelServices services, IMedicalCaseService medicalCaseService, INavigationCoordinator navigationCoordinat…` | MedicalCaseWorkspaceViewModel |
| 317 | public | ` public override void OnNavigatedTo(NavigationContext navigationContext)` | MedicalCaseWorkspaceViewModel |
| 323 | private | ` private async Task OnNavigatedToAsync(NavigationContext navigationContext)` | MedicalCaseWorkspaceViewModel |
| 345 | public | ` public override bool IsNavigationTarget(NavigationContext navigationContext) => false;` | MedicalCaseWorkspaceViewModel |
| 347 | public | ` public override void OnNavigatedFrom(NavigationContext navigationContext)` | MedicalCaseWorkspaceViewModel |
| 357 | private | ` private void UpdateState()` | MedicalCaseWorkspaceViewModel |
| 367 | private | ` private void DetermineEditMode(WorkspaceMode workspaceMode, EditState initialEditState, bool isHistoricalEdit)` | MedicalCaseWorkspaceViewModel |
| 378 | private | ` private void OnEditStateChangedFsm(object? sender, EditStateChangedEventArgs e)` | MedicalCaseWorkspaceViewModel |
| 388 | private | ` private async Task InitializePatientInfoAsync()` | MedicalCaseWorkspaceViewModel |
| 409 | private | ` private async Task LoadMedicalCaseDataAsync()` | MedicalCaseWorkspaceViewModel |
| 428 | private | ` private async Task ResumeSuspendedIfNeededAsync()` | MedicalCaseWorkspaceViewModel |
| 442 | private | ` private void InitializeChildViewModels()` | MedicalCaseWorkspaceViewModel |
| 474 | private | ` private async Task ExecuteBackAsync()` | MedicalCaseWorkspaceViewModel |
| 485 | public | ` public async Task<LeaveConsultationResult> HandleLeaveRequestAsync()` | MedicalCaseWorkspaceViewModel |
| 494 | private | ` private void ExecuteSaveChanges()` | MedicalCaseWorkspaceViewModel |
| 504 | private | ` private void OnConsultationCompleted(CaseConsultationCompletedPayload payload)` | MedicalCaseWorkspaceViewModel |
| 507 | private | ` private void OnPrescriptionCompleted(CasePrescriptionCompletedPayload payload)` | MedicalCaseWorkspaceViewModel |
| 510 | private | ` private void OnChildPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)` | MedicalCaseWorkspaceViewModel |
| 524 | private | ` private void ExecuteViewPatientHistory()` | MedicalCaseWorkspaceViewModel |
| 531 | private | ` private void ExecuteViewAuditLogs()` | MedicalCaseWorkspaceViewModel |
| 577 | protected | ` protected override void Dispose(bool disposing)` | MedicalCaseWorkspaceViewModel |
| 105 | public | ` public PatientSelectionViewModel( IViewModelServices services, IPatientService patientService, IMedicalCaseQueryService medicalCaseQueryService, IMed…` | PatientSelectionViewModel |
| 145 | default(private) | ` partial void OnSelectedPatientChanged(PatientListDto? value)` | PatientSelectionViewModel |
| 156 | private | ` private void BackToHome()` | PatientSelectionViewModel |
| 171 | private | ` private void NewPatient()` | PatientSelectionViewModel |
| 180 | private | ` private async Task RefreshAsync() => await LoadPatientsAsync();` | PatientSelectionViewModel |
| 184 | private | ` private async Task SearchAsync() => await LoadPatientsAsync();` | PatientSelectionViewModel |
| 188 | private | ` private async Task StartMedicalCaseAsync()` | PatientSelectionViewModel |
| 241 | private | ` private bool CanStartMedicalCase() => SelectedPatient != null;` | PatientSelectionViewModel |
| 250 | private | ` private async Task LoadPatientsAsync()` | PatientSelectionViewModel |
| 290 | private | ` private async Task LoadPatientDetailAsync()` | PatientSelectionViewModel |
| 315 | private | ` private async Task HandleSuspendedCaseAsync(PendingMedicalCaseDto suspendedCase)` | PatientSelectionViewModel |
| 363 | private | ` private async Task CreateAndNavigateToNewMedicalCaseAsync()` | PatientSelectionViewModel |
| 391 | private | ` private void NavigateToMedicalCase(Guid medicalCaseId)` | PatientSelectionViewModel |
| 408 | private | ` private void SetBusyWithMessage(bool isBusy, string? message)` | PatientSelectionViewModel |
| 420 | private | ` private async Task ShowErrorDialogAsync(string message)` | PatientSelectionViewModel |
| 450 | public | ` public override void OnNavigatedTo(NavigationContext navigationContext)` | PatientSelectionViewModel |
| 460 | public | ` public override bool IsNavigationTarget(NavigationContext navigationContext)` | PatientSelectionViewModel |
| 465 | public | ` public override void OnNavigatedFrom(NavigationContext navigationContext)` | PatientSelectionViewModel |
| 471 | protected | ` protected override void Dispose(bool disposing)` | PatientSelectionViewModel |
| 71 | public | ` public CardReaderViewModel( ICardReaderService cardReaderService, IPatientCardReaderIntegration patientIntegration, IMedicalCaseService medicalCaseSe…` | CardReaderViewModel |
| 99 | public | ` public override async Task InitializeAsync()` | CardReaderViewModel |
| 132 | public | ` public async Task ManualReadCardAsync()` | CardReaderViewModel |
| 169 | public | ` public void ToggleAutoRead()` | CardReaderViewModel |
| 187 | public | ` public void StartAutoRead()` | CardReaderViewModel |
| 197 | public | ` public void StopAutoRead()` | CardReaderViewModel |
| 205 | public | ` public async Task DisconnectAsync()` | CardReaderViewModel |
| 224 | private | ` private async Task HandleCardReadResultAsync(CardReadResult result)` | CardReaderViewModel |
| 240 | private | ` private async Task ProcessPatientFromCardAsync(CardReadResult cardResult)` | CardReaderViewModel |
| 280 | private | ` private async Task HandleNewPatientFromCardAsync(CardReadResult cardResult)` | CardReaderViewModel |
| 330 | private | ` private async Task NavigateToMedicalCaseForPatientAsync(PatientFromCardResult patient)` | CardReaderViewModel |
| 381 | private | ` private void NavigateToWorkspace(Guid medicalCaseId, PatientDetailDto patientDetail)` | CardReaderViewModel |
| 393 | private | ` private void UpdateStatus(string message)` | CardReaderViewModel |
| 401 | public | ` public static string MaskIdNumber(string? idNumber)` | CardReaderViewModel |
| 408 | private | ` private void OnConnectionStateChanged(object? sender, CardReaderConnectionEventArgs e)` | CardReaderViewModel |
| 415 | private | ` private void OnCardReadCompleted(object? sender, CardReadResult e)` | CardReaderViewModel |
| 421 | private | ` private void OnCardReadError(object? sender, CardReadErrorEventArgs e)` | CardReaderViewModel |
| 431 | public | ` public override void Dispose()` | CardReaderViewModel |
| 50 | public | ` public PendingQueueViewModel( IMedicalCaseWorkspaceContext context, IWorkspaceHost host, ILoggerFactory loggerFactory, IMedicalCaseService medicalCas…` | PendingQueueViewModel |
| 66 | private | ` private async Task RefreshAsync() => await RefreshQueueAsync();` | PendingQueueViewModel |
| 69 | private | ` private async Task SelectAsync(PendingMedicalCaseDto c) => await SelectPendingCaseAsync(c);` | PendingQueueViewModel |
| 74 | public | ` public async Task RefreshQueueAsync()` | PendingQueueViewModel |
| 117 | private | ` private static MedicalCaseStatus MapRegistrationStatus(RegistrationStatus status) => status switch` | PendingQueueViewModel |
| 128 | public | ` public async Task SelectPendingCaseAsync(PendingMedicalCaseDto? pendingCase)` | PendingQueueViewModel |
| 217 | private | ` private async Task HandleSuspendedCaseAsync(PendingMedicalCaseDto pendingCase)` | PendingQueueViewModel |
| 264 | private | ` private async Task NavigateToNewMedicalCaseAsync(PendingMedicalCaseDto pendingCase)` | PendingQueueViewModel |
| 312 | private | ` private async Task NavigateToExistingMedicalCaseAsync(PendingMedicalCaseDto pendingCase)` | PendingQueueViewModel |
| 358 | private | ` private PatientDetailDto? GetPatientDetail(Guid patientId)` | PendingQueueViewModel |
| 36 | public | ` public WorkspaceNavigationHandler( IMedicalCaseService medicalCaseService, INavigationCoordinator navigationCoordinator, IActiveConsultationService a…` | WorkspaceNavigationHandler |
| 65 | public | ` public async Task ExecuteBackAsync(WorkspaceState state, Guid medicalCaseId, Func<ConsultationInputDto?> getConsultationData, Func<PrescriptionInputD…` | WorkspaceNavigationHandler |
| 96 | public | ` public async Task<LeaveConsultationResult> HandleLeaveRequestAsync( Guid medicalCaseId, Func<ConsultationInputDto?> getConsultationData, Func<Prescri…` | WorkspaceNavigationHandler |
| 139 | public | ` public async Task<bool> HandleManagementLeaveRequestAsync( Guid medicalCaseId, Func<ConsultationInputDto?> getConsultationData, Func<PrescriptionInpu…` | WorkspaceNavigationHandler |
| 180 | public | ` public async Task SuspendOnlyAsync( Guid medicalCaseId, Func<ConsultationInputDto?> getConsultationData, Func<PrescriptionInputDto?> getPrescriptionD…` | WorkspaceNavigationHandler |
| 197 | public | ` public async Task CancelCaseOnlyAsync( Guid medicalCaseId, Func<ConsultationInputDto?> getConsultationData, Func<PrescriptionInputDto?> getPrescripti…` | WorkspaceNavigationHandler |
| 214 | public | ` public async Task ExecuteSaveChangesAsync( Guid medicalCaseId, Func<ConsultationInputDto?> getConsultationData, Func<PrescriptionInputDto?> getPrescr…` | WorkspaceNavigationHandler |
| 23 | public | ` public WorkspaceStateManager( IEditModeStateMachine editStateMachine, Func<ConsultationEditorViewModel> getConsultationEditor, Func<PrescriptionEdito…` | WorkspaceStateManager |
| 38 | public | ` public WorkspaceState UpdateState(WorkspaceState current)` | WorkspaceStateManager |
| 59 | public | ` public int CalculateCurrentStep()` | WorkspaceStateManager |
| 88 | public | ` public CompletenessCheck CalculateCompleteness(WorkspaceState current)` | WorkspaceStateManager |
| 108 | public | ` public bool CalculateCanComplete()` | WorkspaceStateManager |
| 122 | default(private) | ` public (WorkspaceState state, bool canEdit, bool startEditing) DetermineEditMode( WorkspaceState currentState, WorkspaceMode workspaceMode, MedicalCa…` | WorkspaceStateManager |
| 152 | public | ` public void InitializeEditStateMachine(bool canEdit, bool startEditing)` | WorkspaceStateManager |
| 169 | public | ` public WorkspaceState OnEditStateChanged(WorkspaceState currentState, EditStateChangedEventArgs e)` | WorkspaceStateManager |
| 11 | public | ` public ClinicalHomeView()` | ClinicalHomeView |
| 14 | public | ` public ClinicalWorkspaceView()` | ClinicalWorkspaceView |
| 22 | private | ` private void PatientSelectionControl_PatientDoubleClicked(object? sender, PatientListDto e)` | ClinicalWorkspaceView |
| 17 | public | ` public FormulaManagementView()` | FormulaManagementView |
| 17 | public | ` public HerbManagementView()` | HerbManagementView |
| 17 | public | ` public MedicalCaseManagementView()` | MedicalCaseManagementView |
| 13 | public | ` public MedicalCaseWorkspaceView()` | MedicalCaseWorkspaceView |
| 17 | public | ` public PatientManagementView()` | PatientManagementView |
| 12 | public | ` public PatientSelectionView()` | PatientSelectionView |
| 21 | private | ` private void PatientSelectionControl_PatientDoubleClicked(object? sender, PatientListDto e)` | PatientSelectionView |
| 11 | public | ` public PendingQueueView()` | PendingQueueView |
### LYBT.Desktop.Contracts

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 32 | public (interface 默认) | ` Task<ApiResponse<LoginResponse>> LoginAsync([Refit.Body] LoginRequest loginRequest);` | IAuthApi |
| 45 | public (interface 默认) | ` Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync([Refit.Body] AutoLoginRequest request);` | IAuthApi |
| 59 | public (interface 默认) | ` Task<ApiResponse> LogoutAsync([Refit.Body] LogoutRequest logoutRequest);` | IAuthApi |
| 72 | public (interface 默认) | ` Task<ApiResponse<LoginResponse>> RefreshTokenAsync([Refit.Body] RefreshTokenRequest request);` | IAuthApi |
| 88 | public (interface 默认) | ` Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync();` | IAuthApi |
| 100 | public (interface 默认) | ` Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync();` | IAuthApi |
| 20 | public (interface 默认) | ` Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync();` | IConfigurationApi |
| 27 | public (interface 默认) | ` Task<ApiResponse<string>> GetValueAsync(string key);` | IConfigurationApi |
| 35 | public (interface 默认) | ` Task<ApiResponse> SetValueAsync(string key, [Refit.Body] string value);` | IConfigurationApi |
| 42 | public (interface 默认) | ` Task<ApiResponse> UpdateConfigurationAsync([Refit.Body] Dictionary<string, string> settings);` | IConfigurationApi |
| 48 | public (interface 默认) | ` Task<ApiResponse> ValidateProductionAsync();` | IConfigurationApi |
| 18 | public (interface 默认) | ` Task<ApiResponse<object>> UploadAsync([Refit.Body] MultipartFormDataContent content);` | IDeployApi |
| 24 | public (interface 默认) | ` Task<ApiResponse<object>> RestartAsync();` | IDeployApi |
| 21 | public (interface 默认) | ` Task<ApiResponse<object>> GetLoggingStatusAsync();` | IDiagnosticsApi |
| 28 | public (interface 默认) | ` Task<ApiResponse<object>> EnableDebugModeAsync([Refit.Body] EnableDebugModeRequest request);` | IDiagnosticsApi |
| 34 | public (interface 默认) | ` Task<ApiResponse<object>> DisableDebugModeAsync();` | IDiagnosticsApi |
| 41 | public (interface 默认) | ` Task<ApiResponse<object>> SetLoggingLevelAsync([Refit.Body] SetLoggingLevelRequest request);` | IDiagnosticsApi |
| 15 | public (interface 默认) | ` Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync( [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20, [Refit.Query] string? k…` | IFormulaApi |
| 25 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id);` | IFormulaApi |
| 31 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync([Refit.Body] FormulaInputDto request);` | IFormulaApi |
| 37 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, [Refit.Body] FormulaInputDto request);` | IFormulaApi |
| 43 | public (interface 默认) | ` Task<ApiResponse> DeleteFormulaAsync(Guid id);` | IFormulaApi |
| 49 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id);` | IFormulaApi |
| 55 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id);` | IFormulaApi |
| 60 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);` | IFormulaApi |
| 65 | public (interface 默认) | ` Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync([Refit.Body] FormulaBatchImportInputDto request);` | IFormulaApi |
| 71 | public (interface 默认) | ` Task<HttpResponseMessage> ExportFormulasAsync([Refit.Query] string? category = null);` | IFormulaApi |
| 77 | public (interface 默认) | ` Task<HttpResponseMessage> ExportTemplateAsync();` | IFormulaApi |
| 83 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id);` | IFormulaApi |
| 89 | public (interface 默认) | ` Task<ApiResponse<List<FormulaListDto>>> GetPendingValidationAsync();` | IFormulaApi |
| 95 | public (interface 默认) | ` Task<ApiResponse<FormulaHerbItemDto>> ValidateHerbAsync( Guid formulaId, Guid herbItemId, [Refit.Body] ValidateFormulaHerbInputDto request);` | IFormulaApi |
| 104 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);` | IFormulaApi |
| 110 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);` | IFormulaApi |
| 15 | public (interface 默认) | ` Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync( [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20, [Refit.Query] string? keyword…` | IHerbApi |
| 25 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id);` | IHerbApi |
| 31 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> CreateHerbAsync([Refit.Body] HerbInputDto request);` | IHerbApi |
| 37 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, [Refit.Body] HerbInputDto request);` | IHerbApi |
| 43 | public (interface 默认) | ` Task<ApiResponse> DeleteHerbAsync(Guid id);` | IHerbApi |
| 51 | public (interface 默认) | ` Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync( [Refit.Body] HerbBatchImportInputDto request);` | IHerbApi |
| 58 | public (interface 默认) | ` Task<HttpResponseMessage> ExportTemplateAsync();` | IHerbApi |
| 64 | public (interface 默认) | ` Task<HttpResponseMessage> ExportHerbsAsync([Refit.Query] string? keyword = null);` | IHerbApi |
| 69 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id);` | IHerbApi |
| 74 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);` | IHerbApi |
| 80 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id);` | IHerbApi |
| 86 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);` | IHerbApi |
| 92 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);` | IHerbApi |
| 17 | public (interface 默认) | ` Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync( [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20, [Refit.Query] s…` | IMedicalCaseApi |
| 36 | public (interface 默认) | ` Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync( [Refit.Query] MedicalCaseQueryType queryType = MedicalCaseQueryType.All, […` | IMedicalCaseApi |
| 50 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id);` | IMedicalCaseApi |
| 61 | public (interface 默认) | ` Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync([Refit.Query] Guid? patientId = null);` | IMedicalCaseApi |
| 70 | public (interface 默认) | ` Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync( [Refit.Query] string? patientName = null, [Refit.Query] string? diagnos…` | IMedicalCaseApi |
| 82 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync([Refit.Body] MedicalCaseInputDto request);` | IMedicalCaseApi |
| 92 | public (interface 默认) | ` Task<ApiResponse> DeleteMedicalCaseAsync(Guid id);` | IMedicalCaseApi |
| 111 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync( Guid medicalCaseId, [Refit.Body] SetPrescriptionFlagRequest request);` | IMedicalCaseApi |
| 124 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id);` | IMedicalCaseApi |
| 131 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync( Guid id, [Refit.Body] ConsultationInputDto? request = null);` | IMedicalCaseApi |
| 139 | public (interface 默认) | ` Task<Refit.IApiResponse> CancelMedicalCaseAsync( Guid id, [Refit.Body] CancelMedicalCaseRequest? request = null);` | IMedicalCaseApi |
| 148 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync( Guid id, [Refit.Body] MedicalCaseStatusInputDto request);` | IMedicalCaseApi |
| 160 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync( Guid id, [Refit.Body] MedicalCaseInputDto request);` | IMedicalCaseApi |
| 167 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);` | IMedicalCaseApi |
| 177 | public (interface 默认) | ` Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync( Guid id, [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20);` | IMedicalCaseApi |
| 186 | public (interface 默认) | ` Task<ApiResponse<MedicalCasePermissionsDto>> GetPermissionsAsync(Guid id);` | IMedicalCaseApi |
| 192 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintAsync( Guid id, [Refit.Body] RecordPrintRequest request);` | IMedicalCaseApi |
| 16 | public (interface 默认) | ` Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync( [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20, [Refit.Query] string? k…` | IPatientApi |
| 25 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id);` | IPatientApi |
| 31 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> CreatePatientAsync([Refit.Body] PatientInputDto request);` | IPatientApi |
| 37 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, [Refit.Body] PatientInputDto request);` | IPatientApi |
| 43 | public (interface 默认) | ` Task<ApiResponse> DeletePatientAsync(Guid id);` | IPatientApi |
| 53 | public (interface 默认) | ` Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync([Refit.Body] PatientBatchImportInputDto request);` | IPatientApi |
| 60 | public (interface 默认) | ` Task<HttpResponseMessage> ExportTemplateAsync();` | IPatientApi |
| 68 | public (interface 默认) | ` Task<HttpResponseMessage> ExportPatientsAsync([Refit.Query] string? keyword = null);` | IPatientApi |
| 73 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);` | IPatientApi |
| 79 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id);` | IPatientApi |
| 85 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id);` | IPatientApi |
| 17 | public (interface 默认) | ` Task<ApiResponse<RegistrationDetailDto>> CreateAsync([Refit.Body] RegistrationInputDto request);` | IRegistrationApi |
| 23 | public (interface 默认) | ` Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id);` | IRegistrationApi |
| 30 | public (interface 默认) | ` Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync( [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20, [Refit.Query] string? …` | IRegistrationApi |
| 44 | public (interface 默认) | ` Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync( [Refit.Query] Guid? doctorId = null);` | IRegistrationApi |
| 52 | public (interface 默认) | ` Task<ApiResponse<Guid>> StartVisitAsync(Guid id);` | IRegistrationApi |
| 59 | public (interface 默认) | ` Task<ApiResponse> CancelAsync(Guid id);` | IRegistrationApi |
| 9 | public (interface 默认) | ` Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync( [Refit.Query] DateTime? startDate = null, [Refit.Query] DateTime? endDate = null);` | IReportsApi |
| 14 | public (interface 默认) | ` Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync( [Refit.Query] DateTime? startDate = null, [Refit.Query] DateTime? endDate = null)…` | IReportsApi |
| 19 | public (interface 默认) | ` Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync( [Refit.Query] DateTime? startDate = null, [Refit.Query] DateTime? endDate = null);` | IReportsApi |
| 17 | public (interface 默认) | ` Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync( [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20, [Refit.Query] string? keyword…` | IUserApi |
| 26 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id);` | IUserApi |
| 32 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> CreateUserAsync([Refit.Body] UserInputDto request);` | IUserApi |
| 38 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, [Refit.Body] UserInputDto request);` | IUserApi |
| 44 | public (interface 默认) | ` Task<ApiResponse> DeleteUserAsync(Guid id);` | IUserApi |
| 50 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, [Refit.Body] ChangeProfileDto request);` | IUserApi |
| 56 | public (interface 默认) | ` Task<ApiResponse> ChangePasswordAsync(Guid id, [Refit.Body] ChangePasswordRequest request);` | IUserApi |
| 62 | public (interface 默认) | ` Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, [Refit.Body] ResetPasswordRequest request);` | IUserApi |
| 68 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id);` | IUserApi |
| 74 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);` | IUserApi |
| 80 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id);` | IUserApi |
| 86 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);` | IUserApi |
| 92 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);` | IUserApi |
| 27 | public (interface 默认) | ` Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest);` | IApiClientAuth |
| 34 | public (interface 默认) | ` Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request);` | IApiClientAuth |
| 40 | public (interface 默认) | ` Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest);` | IApiClientAuth |
| 47 | public (interface 默认) | ` Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);` | IApiClientAuth |
| 54 | public (interface 默认) | ` Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync();` | IApiClientAuth |
| 60 | public (interface 默认) | ` Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync();` | IApiClientAuth |
| 12 | public (interface 默认) | ` Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync();` | IApiClientConfiguration |
| 15 | public (interface 默认) | ` Task<ApiResponse<string>> GetValueAsync(string key);` | IApiClientConfiguration |
| 18 | public (interface 默认) | ` Task<ApiResponse> SetValueAsync(string key, string value);` | IApiClientConfiguration |
| 21 | public (interface 默认) | ` Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings);` | IApiClientConfiguration |
| 24 | public (interface 默认) | ` Task<ApiResponse> ValidateProductionAsync();` | IApiClientConfiguration |
| 9 | public (interface 默认) | ` Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content);` | IApiClientDeploy |
| 10 | public (interface 默认) | ` Task<ApiResponse<object>> RestartAsync();` | IApiClientDeploy |
| 9 | public (interface 默认) | ` Task<ApiResponse<object>> GetLoggingStatusAsync();` | IApiClientDiagnostics |
| 10 | public (interface 默认) | ` Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request);` | IApiClientDiagnostics |
| 11 | public (interface 默认) | ` Task<ApiResponse<object>> DisableDebugModeAsync();` | IApiClientDiagnostics |
| 12 | public (interface 默认) | ` Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request);` | IApiClientDiagnostics |
| 29 | public (interface 默认) | ` Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync( int page = 1, int pageSize = 20, string? keyword = null, string? category = null);` | IApiClientFormulas |
| 39 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id);` | IApiClientFormulas |
| 45 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request);` | IApiClientFormulas |
| 52 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request);` | IApiClientFormulas |
| 58 | public (interface 默认) | ` Task<ApiResponse> DeleteFormulaAsync(Guid id);` | IApiClientFormulas |
| 64 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id);` | IApiClientFormulas |
| 70 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id);` | IApiClientFormulas |
| 76 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);` | IApiClientFormulas |
| 82 | public (interface 默认) | ` Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request);` | IApiClientFormulas |
| 89 | public (interface 默认) | ` Task<HttpResponseMessage> ExportFormulasAsync(string? category = null);` | IApiClientFormulas |
| 95 | public (interface 默认) | ` Task<HttpResponseMessage> ExportTemplateAsync();` | IApiClientFormulas |
| 101 | public (interface 默认) | ` Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id);` | IApiClientFormulas |
| 107 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request);` | IApiClientFormulas |
| 113 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request);` | IApiClientFormulas |
| 118 | public (interface 默认) | ` Task<ApiResponse<List<FormulaListDto>>> GetPendingValidationAsync();` | IApiClientFormulas |
| 126 | public (interface 默认) | ` Task<ApiResponse<FormulaHerbItemDto>> ValidateHerbAsync( Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto request);` | IApiClientFormulas |
| 136 | public (interface 默认) | ` Task<List<string>> GetCategoriesAsync();` | IApiClientFormulas |
| 29 | public (interface 默认) | ` Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync( int page = 1, int pageSize = 20, string? keyword = null, string? category = null);` | IApiClientHerbs |
| 39 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id);` | IApiClientHerbs |
| 45 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request);` | IApiClientHerbs |
| 52 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request);` | IApiClientHerbs |
| 58 | public (interface 默认) | ` Task<ApiResponse> DeleteHerbAsync(Guid id);` | IApiClientHerbs |
| 64 | public (interface 默认) | ` Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request);` | IApiClientHerbs |
| 70 | public (interface 默认) | ` Task<HttpResponseMessage> ExportTemplateAsync();` | IApiClientHerbs |
| 77 | public (interface 默认) | ` Task<HttpResponseMessage> ExportHerbsAsync(string? keyword = null);` | IApiClientHerbs |
| 83 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id);` | IApiClientHerbs |
| 89 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);` | IApiClientHerbs |
| 95 | public (interface 默认) | ` Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id);` | IApiClientHerbs |
| 101 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request);` | IApiClientHerbs |
| 107 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request);` | IApiClientHerbs |
| 114 | public (interface 默认) | ` Task<List<string>> GetCategoriesAsync();` | IApiClientHerbs |
| 32 | public (interface 默认) | ` Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync( int page = 1, int pageSize = 20, string? keyword = null, bool includeAllDoct…` | IApiClientMedicalCases |
| 49 | public (interface 默认) | ` Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync( MedicalCaseQueryType queryType = MedicalCaseQueryType.All, Guid? patientId…` | IApiClientMedicalCases |
| 63 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id);` | IApiClientMedicalCases |
| 69 | public (interface 默认) | ` Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(Guid? patientId = null);` | IApiClientMedicalCases |
| 80 | public (interface 默认) | ` Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync( string? patientName = null, string? diagnosisKeyword = null, DateTime? …` | IApiClientMedicalCases |
| 93 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseInputDto request);` | IApiClientMedicalCases |
| 99 | public (interface 默认) | ` Task<ApiResponse> DeleteMedicalCaseAsync(Guid id);` | IApiClientMedicalCases |
| 107 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync( Guid medicalCaseId, SetPrescriptionFlagRequest request);` | IApiClientMedicalCases |
| 116 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id);` | IApiClientMedicalCases |
| 123 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync( Guid id, ConsultationInputDto? request = null);` | IApiClientMedicalCases |
| 132 | public (interface 默认) | ` Task<ApiResponse> CancelMedicalCaseAsync( Guid id, CancelMedicalCaseRequest? request = null);` | IApiClientMedicalCases |
| 142 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync( Guid id, MedicalCaseStatusInputDto request);` | IApiClientMedicalCases |
| 151 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync( Guid id, MedicalCaseInputDto request);` | IApiClientMedicalCases |
| 159 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);` | IApiClientMedicalCases |
| 165 | public (interface 默认) | ` Task<ApiResponse<MedicalCasePermissionsDto>> GetPermissionsAsync(Guid id);` | IApiClientMedicalCases |
| 172 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintAsync( Guid id, RecordPrintRequest request);` | IApiClientMedicalCases |
| 182 | public (interface 默认) | ` Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync( Guid id, int page = 1, int pageSize = 20);` | IApiClientMedicalCases |
| 28 | public (interface 默认) | ` Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync( int page = 1, int pageSize = 20, string? keyword = null);` | IApiClientPatients |
| 37 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id);` | IApiClientPatients |
| 43 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request);` | IApiClientPatients |
| 50 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request);` | IApiClientPatients |
| 56 | public (interface 默认) | ` Task<ApiResponse> DeletePatientAsync(Guid id);` | IApiClientPatients |
| 63 | public (interface 默认) | ` Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request);` | IApiClientPatients |
| 70 | public (interface 默认) | ` Task<HttpResponseMessage> ExportTemplateAsync();` | IApiClientPatients |
| 78 | public (interface 默认) | ` Task<HttpResponseMessage> ExportPatientsAsync(string? keyword = null);` | IApiClientPatients |
| 84 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);` | IApiClientPatients |
| 90 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id);` | IApiClientPatients |
| 96 | public (interface 默认) | ` Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id);` | IApiClientPatients |
| 27 | public (interface 默认) | ` Task<ApiResponse<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request);` | IApiClientRegistrations |
| 33 | public (interface 默认) | ` Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id);` | IApiClientRegistrations |
| 46 | public (interface 默认) | ` Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync( int page = 1, int pageSize = 20, string? keyword = null, DateTime? startDate = null…` | IApiClientRegistrations |
| 60 | public (interface 默认) | ` Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync(Guid? doctorId = null);` | IApiClientRegistrations |
| 67 | public (interface 默认) | ` Task<ApiResponse<Guid>> StartVisitAsync(Guid id);` | IApiClientRegistrations |
| 74 | public (interface 默认) | ` Task<ApiResponse> CancelAsync(Guid id);` | IApiClientRegistrations |
| 82 | public (interface 默认) | ` Task<List<RegistrationListDto>> GetRegistrationsAsync(DateTime? date = null);` | IApiClientRegistrations |
| 88 | public (interface 默认) | ` Task<QuickVisitResultDto> QuickVisitAsync(QuickVisitInputDto request);` | IApiClientRegistrations |
| 94 | public (interface 默认) | ` Task DeleteRegistrationAsync(Guid id);` | IApiClientRegistrations |
| 8 | public (interface 默认) | ` Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null);` | IApiClientReports |
| 10 | public (interface 默认) | ` Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null);` | IApiClientReports |
| 12 | public (interface 默认) | ` Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null);` | IApiClientReports |
| 29 | public (interface 默认) | ` Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync( int page = 1, int pageSize = 20, string? keyword = null);` | IApiClientUsers |
| 38 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id);` | IApiClientUsers |
| 44 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request);` | IApiClientUsers |
| 51 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request);` | IApiClientUsers |
| 57 | public (interface 默认) | ` Task<ApiResponse> DeleteUserAsync(Guid id);` | IApiClientUsers |
| 65 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request);` | IApiClientUsers |
| 73 | public (interface 默认) | ` Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request);` | IApiClientUsers |
| 81 | public (interface 默认) | ` Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequest request);` | IApiClientUsers |
| 87 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id);` | IApiClientUsers |
| 93 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);` | IApiClientUsers |
| 101 | public (interface 默认) | ` Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id);` | IApiClientUsers |
| 107 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request);` | IApiClientUsers |
| 113 | public (interface 默认) | ` Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request);` | IApiClientUsers |
| 120 | public (interface 默认) | ` Task<UserDetailDto> GetCurrentUserAsync();` | IApiClientUsers |
| 23 | public (interface 默认) | ` Task<ApiResponse<PagedResult<TListDto>>> GetPagedAsync( int page = 1, int pageSize = 20, string? keyword = null, string? category = null);` | IEntityApiSegment |
| 33 | public (interface 默认) | ` Task<ApiResponse<TDetailDto>> GetByIdAsync(Guid id);` | IEntityApiSegment |
| 39 | public (interface 默认) | ` Task<ApiResponse<TDetailDto>> CreateAsync(TInputDto request);` | IEntityApiSegment |
| 46 | public (interface 默认) | ` Task<ApiResponse<TDetailDto>> UpdateAsync(Guid id, TInputDto request);` | IEntityApiSegment |
| 52 | public (interface 默认) | ` Task<ApiResponse> DeleteAsync(Guid id);` | IEntityApiSegment |
| 182 | public | ` public AuthStateChangedEventArgs( AuthState previousState, AuthState currentState, AuthEvent trigger, string? statusMessage = null)` | AuthStateChangedEventArgs |
| 38 | public | ` public static MedicalCaseNavigationParameters ForClinical(Guid patientId, Guid? medicalCaseId = null)` | MedicalCaseNavigationParameters |
| 61 | public | ` public static MedicalCaseNavigationParameters ForManagementView(Guid medicalCaseId, Guid patientId)` | MedicalCaseNavigationParameters |
| 78 | public | ` public static MedicalCaseNavigationParameters ForManagementEdit(Guid medicalCaseId, Guid patientId)` | MedicalCaseNavigationParameters |
| 116 | public | ` public static string GetLevelDescription(PerformanceLevel level) => level switch` | PerformanceThresholds |
| 53 | public | ` public string GetFormattedReport()` | PerformanceReport |
| 101 | public | ` public string GetJsonReport()` | PerformanceReport |
| 129 | private | ` private static string GetLevelIndicator(PerformanceLevel level) => level switch` | PerformanceReport |
| 138 | private | ` private static string FormatBytes(long bytes)` | PerformanceReport |
| 16 | public (interface 默认) | ` void StartTiming(string operationName);` | IPerformanceMonitor |
| 23 | public (interface 默认) | ` long StopTiming(string operationName);` | IPerformanceMonitor |
| 30 | public (interface 默认) | ` long RecordMemoryBaseline(string label);` | IPerformanceMonitor |
| 35 | public (interface 默认) | ` IReadOnlyDictionary<string, long> GetMemorySnapshots();` | IPerformanceMonitor |
| 42 | public (interface 默认) | ` PerformanceMetric? GetMetric(string operationName);` | IPerformanceMonitor |
| 47 | public (interface 默认) | ` IReadOnlyCollection<PerformanceMetric> GetAllMetrics();` | IPerformanceMonitor |
| 53 | public (interface 默认) | ` PerformanceReport GenerateReport();` | IPerformanceMonitor |
| 58 | public (interface 默认) | ` void Clear();` | IPerformanceMonitor |
| 79 | public | ` public PerformanceMetricRecordedEventArgs(PerformanceMetric metric)` | PerformanceMetricRecordedEventArgs |
| 15 | public (interface 默认) | ` Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null, CancellationToken c…` | IFormulaRepository |
| 20 | public (interface 默认) | ` Task<FormulaDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);` | IFormulaRepository |
| 25 | public (interface 默认) | ` Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto, CancellationToken ct = default);` | IFormulaRepository |
| 30 | public (interface 默认) | ` Task<FormulaDetailDto> UpdateAsync(FormulaInputDto dto, CancellationToken ct = default);` | IFormulaRepository |
| 35 | public (interface 默认) | ` Task DeleteAsync(Guid id, CancellationToken ct = default);` | IFormulaRepository |
| 40 | public (interface 默认) | ` Task<List<FormulaListDto>> SearchAsync(string keyword, CancellationToken ct = default);` | IFormulaRepository |
| 45 | public (interface 默认) | ` Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId, CancellationToken ct = default);` | IFormulaRepository |
| 52 | public (interface 默认) | ` Task<FormulaDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default);` | IFormulaRepository |
| 57 | public (interface 默认) | ` Task<FormulaDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default);` | IFormulaRepository |
| 62 | public (interface 默认) | ` Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);` | IFormulaRepository |
| 71 | public (interface 默认) | ` Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default);` | IFormulaRepository |
| 76 | public (interface 默认) | ` Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default);` | IFormulaRepository |
| 81 | public (interface 默认) | ` Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default);` | IFormulaRepository |
| 15 | public (interface 默认) | ` Task<PagedResult<HerbListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null, CancellationToken ct =…` | IHerbRepository |
| 20 | public (interface 默认) | ` Task<HerbDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);` | IHerbRepository |
| 25 | public (interface 默认) | ` Task<HerbDetailDto> CreateAsync(HerbInputDto dto, CancellationToken ct = default);` | IHerbRepository |
| 30 | public (interface 默认) | ` Task<HerbDetailDto> UpdateAsync(HerbInputDto dto, CancellationToken ct = default);` | IHerbRepository |
| 35 | public (interface 默认) | ` Task DeleteAsync(Guid id, CancellationToken ct = default);` | IHerbRepository |
| 40 | public (interface 默认) | ` Task<List<HerbListDto>> SearchAsync(string keyword, CancellationToken ct = default);` | IHerbRepository |
| 47 | public (interface 默认) | ` Task<HerbBatchImportResultDto?> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default);` | IHerbRepository |
| 52 | public (interface 默认) | ` Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default);` | IHerbRepository |
| 57 | public (interface 默认) | ` Task<byte[]?> ExportHerbsAsync(string? keyword = null, CancellationToken ct = default);` | IHerbRepository |
| 66 | public (interface 默认) | ` Task<HerbDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default);` | IHerbRepository |
| 71 | public (interface 默认) | ` Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);` | IHerbRepository |
| 16 | public (interface 默认) | ` Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default);` | IMedicalCaseRepository |
| 21 | public (interface 默认) | ` Task<PagedResult<MedicalCaseDetailDto>> SearchAsync( string? patientName = null, string? diagnosisKeyword = null, DateTime? startDate = null, DateTim…` | IMedicalCaseRepository |
| 33 | public (interface 默认) | ` Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);` | IMedicalCaseRepository |
| 38 | public (interface 默认) | ` Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query, CancellationToken ct = default);` | IMedicalCaseRepository |
| 43 | public (interface 默认) | ` Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid? patientId = null, CancellationToken ct = default);` | IMedicalCaseRepository |
| 48 | public (interface 默认) | ` Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto, CancellationToken ct = default);` | IMedicalCaseRepository |
| 53 | public (interface 默认) | ` Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto, CancellationToken ct = default);` | IMedicalCaseRepository |
| 58 | public (interface 默认) | ` Task DeleteAsync(Guid id, CancellationToken ct = default);` | IMedicalCaseRepository |
| 64 | public (interface 默认) | ` Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default);` | IMedicalCaseRepository |
| 69 | public (interface 默认) | ` Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto, CancellationToken ct = default);` | IMedicalCaseRepository |
| 74 | public (interface 默认) | ` Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request, CancellationToken ct = default);` | IMedicalCaseRepository |
| 79 | public (interface 默认) | ` Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request, CancellationToken ct = default);` | IMedicalCaseRepository |
| 84 | public (interface 默认) | ` Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequest? request, CancellationToken ct = default);` | IMedicalCaseRepository |
| 89 | public (interface 默认) | ` Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request, CancellationToken ct = default);` | IMedicalCaseRepository |
| 96 | public (interface 默认) | ` Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);` | IMedicalCaseRepository |
| 16 | public (interface 默认) | ` Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default);` | IPatientRepository |
| 21 | public (interface 默认) | ` Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);` | IPatientRepository |
| 26 | public (interface 默认) | ` Task<PatientDetailDto> CreateAsync(PatientInputDto patient, CancellationToken ct = default);` | IPatientRepository |
| 31 | public (interface 默认) | ` Task<PatientDetailDto> UpdateAsync(PatientInputDto patient, CancellationToken ct = default);` | IPatientRepository |
| 36 | public (interface 默认) | ` Task DeleteAsync(Guid id, CancellationToken ct = default);` | IPatientRepository |
| 41 | public (interface 默认) | ` Task<List<PatientListDto>> SearchAsync(string keyword, CancellationToken ct = default);` | IPatientRepository |
| 46 | public (interface 默认) | ` Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default);` | IPatientRepository |
| 53 | public (interface 默认) | ` Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default);` | IPatientRepository |
| 58 | public (interface 默认) | ` Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default);` | IPatientRepository |
| 63 | public (interface 默认) | ` Task<byte[]?> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default);` | IPatientRepository |
| 72 | public (interface 默认) | ` Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);` | IPatientRepository |
| 16 | public (interface 默认) | ` Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input, CancellationToken ct = default);` | IRegistrationRepository |
| 21 | public (interface 默认) | ` Task<RegistrationDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);` | IRegistrationRepository |
| 26 | public (interface 默认) | ` Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null, CancellationToken ct = default);` | IRegistrationRepository |
| 32 | public (interface 默认) | ` Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null, CancellationToken ct = default);` | IRegistrationRepository |
| 38 | public (interface 默认) | ` Task<Guid?> StartVisitAsync(Guid id, CancellationToken ct = default);` | IRegistrationRepository |
| 44 | public (interface 默认) | ` Task CancelAsync(Guid id, CancellationToken ct = default);` | IRegistrationRepository |
| 17 | public (interface 默认) | ` Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default);` | IUserRepository |
| 22 | public (interface 默认) | ` Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);` | IUserRepository |
| 27 | public (interface 默认) | ` Task<UserDetailDto> CreateAsync(UserInputDto user, CancellationToken ct = default);` | IUserRepository |
| 32 | public (interface 默认) | ` Task<UserDetailDto> UpdateAsync(UserInputDto user, CancellationToken ct = default);` | IUserRepository |
| 37 | public (interface 默认) | ` Task DeleteAsync(Guid id, CancellationToken ct = default);` | IUserRepository |
| 42 | public (interface 默认) | ` Task<UserDetailDto> GetByUsernameAsync(string username, CancellationToken ct = default);` | IUserRepository |
| 47 | public (interface 默认) | ` Task<List<UserListDto>> SearchAsync(string keyword, CancellationToken ct = default);` | IUserRepository |
| 52 | public (interface 默认) | ` Task<List<UserListDto>> GetDoctorsAsync(CancellationToken ct = default);` | IUserRepository |
| 57 | public (interface 默认) | ` Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto, CancellationToken ct = default);` | IUserRepository |
| 62 | public (interface 默认) | ` Task<CommandResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);` | IUserRepository |
| 67 | public (interface 默认) | ` Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken ct = default);` | IUserRepository |
| 72 | public (interface 默认) | ` Task<UserDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default);` | IUserRepository |
| 77 | public (interface 默认) | ` Task<UserDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default);` | IUserRepository |
| 82 | public (interface 默认) | ` Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);` | IUserRepository |
| 16 | public | ` public static CommandResult<T> Succeeded(T data) => new(true, data, null);` | CommandResult |
| 21 | public | ` public static CommandResult<T> Failed(string error) => new(false, default, error);` | CommandResult |
| 26 | public | ` public static CommandResult<T> NotFound(string? message = null)` | CommandResult |
| 32 | public | ` public static implicit operator bool(CommandResult<T> result) => result.Success;` | CommandResult |
| 44 | public | ` public static CommandResult Succeeded() => new(true, null);` | CommandResult |
| 49 | public | ` public static CommandResult Failed(string error) => new(false, error);` | CommandResult |
| 54 | public | ` public static implicit operator bool(CommandResult result) => result.Success;` | CommandResult |
| 50 | public (interface 默认) | ` IEnumerable<string> GetAllModules();` | IRoleDefinition |
| 15 | public (interface 默认) | ` void Register(IRoleDefinition roleDefinition);` | IRoleRegistry |
| 22 | public (interface 默认) | ` IRoleDefinition? GetDefinition(UserRole role);` | IRoleRegistry |
| 27 | public (interface 默认) | ` IReadOnlyCollection<IRoleDefinition> GetAllDefinitions();` | IRoleRegistry |
| 33 | public (interface 默认) | ` bool IsRegistered(UserRole role);` | IRoleRegistry |
| 40 | public (interface 默认) | ` string GetHomeViewName(UserRole role);` | IRoleRegistry |
| 47 | public (interface 默认) | ` IEnumerable<string> GetModulesForRole(UserRole role);` | IRoleRegistry |
| 37 | public (interface 默认) | ` bool Fire(AuthEvent evt, string? statusMessage = null);` | IAuthenticationStateMachine |
| 46 | public (interface 默认) | ` Task<bool> FireAsync(AuthEvent evt, string? statusMessage = null);` | IAuthenticationStateMachine |
| 53 | public (interface 默认) | ` bool CanFire(AuthEvent evt);` | IAuthenticationStateMachine |
| 58 | public (interface 默认) | ` void Reset();` | IAuthenticationStateMachine |
| 63 | public (interface 默认) | ` IEnumerable<AuthEvent> GetPermittedEvents();` | IAuthenticationStateMachine |
| 13 | public (interface 默认) | ` Task<PagedResult<FormulaListDto>> GetFormulasPagedAsync(int page, int pageSize);` | IFormulaSearchProvider |
| 16 | public (interface 默认) | ` Task<FormulaDetailDto?> GetFormulaByIdAsync(Guid id);` | IFormulaSearchProvider |
| 12 | public (interface 默认) | ` Task<IReadOnlyList<HerbListDto>> SearchHerbsAsync(string keyword);` | IHerbSearchProvider |
| 15 | public (interface 默认) | ` Task<IReadOnlyList<HerbListDto>> GetAllHerbsAsync();` | IHerbSearchProvider |
| 25 | public (interface 默认) | ` void Register(Guid medicalCaseId, Func<Task<LeaveConsultationResult>> leaveHandler);` | IActiveConsultationService |
| 31 | public (interface 默认) | ` void Unregister();` | IActiveConsultationService |
| 39 | public (interface 默认) | ` Task<LeaveConsultationResult> RequestLeaveAsync();` | IActiveConsultationService |
| 60 | public | ` public static LeaveConsultationResult AllowLeave(LeaveConsultationChoice choice = LeaveConsultationChoice.None)` | LeaveConsultationResult |
| 66 | public | ` public static LeaveConsultationResult CancelLeave()` | LeaveConsultationResult |
| 57 | public (interface 默认) | ` Task StartMonitoringAsync(CancellationToken ct = default);` | IApiHealthMonitor |
| 60 | public (interface 默认) | ` Task StopMonitoringAsync();` | IApiHealthMonitor |
| 63 | public (interface 默认) | ` Task<ApiMonitorHealthStatus> ForceCheckAsync();` | IApiHealthMonitor |
| 66 | public (interface 默认) | ` void ResetCircuitBreaker();` | IApiHealthMonitor |
| 28 | public (interface 默认) | ` void Start();` | IApplicationTickService |
| 33 | public (interface 默认) | ` void Stop();` | IApplicationTickService |
| 13 | public (interface 默认) | ` Task InitializeAsync();` | IAsyncInitializable |
| 15 | public (interface 默认) | ` Task<CommandResult<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid medicalCaseId, int page, int pageSize, CancellationToken ct = default);` | IAuditLogService |
| 14 | public (interface 默认) | ` Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync(CancellationToken ct = default);` | IAuthHealthService |
| 30 | public (interface 默认) | ` Task ShowInfoAsync(string message, string? title = null);` | ICommonDialogService |
| 37 | public (interface 默认) | ` Task ShowWarningAsync(string message, string? title = null);` | ICommonDialogService |
| 44 | public (interface 默认) | ` Task ShowErrorAsync(string message, string? title = null);` | ICommonDialogService |
| 52 | public (interface 默认) | ` Task<bool> ShowConfirmAsync(string message, string? title = null);` | ICommonDialogService |
| 61 | public (interface 默认) | ` Task<TripleChoiceResult> ShowTripleChoiceAsync(string message, string? title = null);` | ICommonDialogService |
| 70 | public (interface 默认) | ` Task<string?> ShowInputAsync(string message, string? title = null, string? defaultValue = null);` | ICommonDialogService |
| 78 | public (interface 默认) | ` Task<string?> ShowOpenFileDialogAsync(string? filter = null, string? title = null);` | ICommonDialogService |
| 87 | public (interface 默认) | ` Task<string?> ShowSaveFileDialogAsync(string? filter = null, string? title = null, string? defaultFileName = null);` | ICommonDialogService |
| 64 | public (interface 默认) | ` Task<bool> CheckRemoteAvailableAsync();` | IConnectionModeService |
| 72 | public (interface 默认) | ` Task<bool> TestRemoteConnectionAsync(string url);` | IConnectionModeService |
| 78 | public (interface 默认) | ` Task<bool> TestLocalConnectionAsync();` | IConnectionModeService |
| 89 | public (interface 默认) | ` void SetMode(ConnectionMode mode);` | IConnectionModeService |
| 39 | public (interface 默认) | ` Task SetUrlAsync(string url);` | IConnectionSettingsService |
| 42 | public (interface 默认) | ` Task SaveRemoteUrlAsync(string url);` | IConnectionSettingsService |
| 45 | public (interface 默认) | ` Task SavePreferredModeAsync(string mode);` | IConnectionSettingsService |
| 55 | public (interface 默认) | ` bool IsValidUrl(string url);` | IConnectionSettingsService |
| 11 | public (interface 默认) | ` void InvalidatePatientCaches();` | IDesktopCacheManager |
| 16 | public (interface 默认) | ` void InvalidateMedicalCaseCaches();` | IDesktopCacheManager |
| 21 | public (interface 默认) | ` void InvalidateHerbCaches();` | IDesktopCacheManager |
| 26 | public (interface 默认) | ` void InvalidateFormulaCaches();` | IDesktopCacheManager |
| 31 | public (interface 默认) | ` void InvalidateUserCaches();` | IDesktopCacheManager |
| 36 | public (interface 默认) | ` void InvalidateAll();` | IDesktopCacheManager |
| 22 | public (interface 默认) | ` void MarkAsChanged();` | IEditable |
| 27 | public (interface 默认) | ` void MarkAsSaved();` | IEditable |
| 32 | public (interface 默认) | ` void BeginEdit();` | IEditable |
| 37 | public (interface 默认) | ` void CancelEdit();` | IEditable |
| 42 | public (interface 默认) | ` void EndEdit();` | IEditable |
| 15 | public (interface 默认) | ` Task StartAsync(CancellationToken cancellationToken = default);` | IEmbeddedLocalWebApiService |
| 18 | public (interface 默认) | ` Task StopAsync(CancellationToken cancellationToken = default);` | IEmbeddedLocalWebApiService |
| 20 | public (interface 默认) | ` Task<CommandResult<FormulaDetailDto>> GetByIdAsync(Guid formulaId, CancellationToken ct = default);` | IFormulaService |
| 25 | public (interface 默认) | ` Task<CommandResult<PagedResult<FormulaListDto>>> GetPagedAsync( int page, int pageSize, string? keyword = null, CancellationToken ct = default);` | IFormulaService |
| 35 | public (interface 默认) | ` Task<CommandResult<FormulaDetailDto>> CreateFormulaAsync( string formulaName, string effect, string usage, string property, string category, string r…` | IFormulaService |
| 49 | public (interface 默认) | ` Task<CommandResult<FormulaDetailDto>> UpdateFormulaAsync( Guid formulaId, string formulaName, string effect, string usage, string property, string ca…` | IFormulaService |
| 64 | public (interface 默认) | ` Task<CommandResult<FormulaDetailDto>> CopyFormulaAsync(FormulaDetailDto sourceFormula, CancellationToken ct = default);` | IFormulaService |
| 73 | public (interface 默认) | ` Task<CommandResult<bool>> DeleteFormulaAsync(Guid formulaId, CancellationToken ct = default);` | IFormulaService |
| 82 | public (interface 默认) | ` Task<CommandResult<FormulaDetailDto>> ToggleStatusAsync(Guid formulaId, CancellationToken ct = default);` | IFormulaService |
| 91 | public (interface 默认) | ` Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> formulaIds, CancellationToken ct = default);` | IFormulaService |
| 100 | public (interface 默认) | ` Task<CommandResult<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default);` | IFormulaService |
| 105 | public (interface 默认) | ` Task<CommandResult<byte[]>> ExportFormulasAsync(string? category = null, CancellationToken ct = default);` | IFormulaService |
| 110 | public (interface 默认) | ` Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default);` | IFormulaService |
| 18 | public (interface 默认) | ` Task<CommandResult<HerbDetailDto>> CreateHerbAsync(HerbInputDto createDto, CancellationToken ct = default);` | IHerbService |
| 23 | public (interface 默认) | ` Task<CommandResult<HerbDetailDto>> UpdateHerbAsync(HerbInputDto updateDto, CancellationToken ct = default);` | IHerbService |
| 28 | public (interface 默认) | ` Task<CommandResult<bool>> DeleteHerbAsync(Guid herbId, CancellationToken ct = default);` | IHerbService |
| 33 | public (interface 默认) | ` Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> herbIds, CancellationToken ct = default);` | IHerbService |
| 42 | public (interface 默认) | ` Task<CommandResult<HerbDetailDto>> GetByIdAsync(Guid herbId, CancellationToken ct = default);` | IHerbService |
| 47 | public (interface 默认) | ` Task<CommandResult<PagedResult<HerbListDto>>> GetPagedAsync( int page, int pageSize, string? searchText = null, string? category = null, Cancellation…` | IHerbService |
| 53 | public (interface 默认) | ` Task<CommandResult<List<HerbListDto>>> GetAllAsync(CancellationToken ct = default);` | IHerbService |
| 58 | public (interface 默认) | ` Task<CommandResult<List<HerbListDto>>> SearchAsync(string keyword, CancellationToken ct = default);` | IHerbService |
| 67 | public (interface 默认) | ` Task<CommandResult<HerbDetailDto>> ToggleStatusAsync(Guid herbId, CancellationToken ct = default);` | IHerbService |
| 76 | public (interface 默认) | ` Task<CommandResult<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default);` | IHerbService |
| 81 | public (interface 默认) | ` Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default);` | IHerbService |
| 86 | public (interface 默认) | ` Task<CommandResult<byte[]>> ExportHerbsAsync(string? keyword, CancellationToken ct = default);` | IHerbService |
| 51 | public (interface 默认) | ` Task<CommandResult<UserDetailDto>> LoginAsync(string username, string password);` | ILoginCoordinator |
| 59 | public (interface 默认) | ` Task HandleLoginSuccessAsync(UserDetailDto user, DateTime tokenExpiresAt);` | ILoginCoordinator |
| 65 | public (interface 默认) | ` Task LogoutAsync();` | ILoginCoordinator |
| 70 | public (interface 默认) | ` LoginFlowDiagnostics GetDiagnostics();` | ILoginCoordinator |
| 81 | public | ` public LoginSuccessEventArgs(UserDetailDto user, DateTime tokenExpiresAt)` | LoginSuccessEventArgs |
| 26 | public (interface 默认) | ` Task<bool> SaveAsync(CancellationToken ct = default);` | IMedicalCaseCommandService |
| 32 | public (interface 默认) | ` Task<bool> DeleteAsync(CancellationToken ct = default);` | IMedicalCaseCommandService |
| 34 | public (interface 默认) | ` Task InitializeAsync(Guid entityId, CancellationToken ct = default);` | IMedicalCaseLifecycleService |
| 39 | public (interface 默认) | ` Task ReloadAsync(CancellationToken ct = default);` | IMedicalCaseLifecycleService |
| 75 | public (interface 默认) | ` Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default);` | IMedicalCaseLifecycleService |
| 20 | public (interface 默认) | ` Task<PagedResult<MedicalCaseListDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null, CancellationToken ct = default);` | IMedicalCaseQueryService |
| 27 | public (interface 默认) | ` Task<PagedResult<MedicalCaseListDto>?> QueryAsync(MedicalCaseQueryDto query, CancellationToken ct = default);` | IMedicalCaseQueryService |
| 36 | public (interface 默认) | ` Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync( Guid patientId, Guid doctorId, bool checkAllDoctors = false, CancellationToken ct = de…` | IMedicalCaseQueryService |
| 47 | public (interface 默认) | ` Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid? patientId = null, CancellationToken ct = default);` | IMedicalCaseQueryService |
| 20 | public (interface 默认) | ` Task NavigateTo(string viewName, IDictionary<string, object>? parameters = null);` | INavigationCoordinator |
| 25 | public (interface 默认) | ` Task NavigateTo<TParams>(string viewName, TParams parameters) where TParams : class;` | INavigationCoordinator |
| 30 | public (interface 默认) | ` Task NavigateToHome();` | INavigationCoordinator |
| 36 | public (interface 默认) | ` Task NavigateToHome(UserRole role);` | INavigationCoordinator |
| 41 | public (interface 默认) | ` void NavigateBack();` | INavigationCoordinator |
| 60 | public (interface 默认) | ` void NavigateForward();` | INavigationCoordinator |
| 88 | public (interface 默认) | ` void ClearHistory();` | INavigationCoordinator |
| 102 | public (interface 默认) | ` void ShowLoginDialog();` | INavigationCoordinator |
| 107 | public (interface 默认) | ` void ClearLoginRegion();` | INavigationCoordinator |
| 112 | public (interface 默认) | ` void ClearContentRegion();` | INavigationCoordinator |
| 121 | public (interface 默认) | ` void SubscribeToRegionCollection();` | INavigationCoordinator |
| 126 | public (interface 默认) | ` void UnsubscribeFromRegionCollection();` | INavigationCoordinator |
| 145 | public | ` public NavigationChangedEventArgs(string? fromView, string toView, IDictionary<string, object>? parameters = null)` | NavigationChangedEventArgs |
| 17 | public (interface 默认) | ` Task<CommandResult<PatientDetailDto>> CreatePatientAsync(PatientInputDto inputDto, CancellationToken ct = default);` | IPatientService |
| 22 | public (interface 默认) | ` Task<CommandResult<PatientDetailDto>> UpdatePatientAsync(PatientInputDto inputDto, CancellationToken ct = default);` | IPatientService |
| 27 | public (interface 默认) | ` Task<CommandResult<bool>> DeletePatientAsync(Guid patientId, CancellationToken ct = default);` | IPatientService |
| 36 | public (interface 默认) | ` Task<CommandResult<BatchOperationResultDto>> BatchDeletePatientsAsync(IEnumerable<Guid> patientIds, CancellationToken ct = default);` | IPatientService |
| 45 | public (interface 默认) | ` Task<CommandResult<IEnumerable<PatientListDto>>> SearchPatientsAsync(string keyword, CancellationToken ct = default);` | IPatientService |
| 50 | public (interface 默认) | ` Task<CommandResult<PagedResult<PatientListDto>>> GetPatientsPagedAsync(int page, int pageSize, string? keyword = null, CancellationToken ct = default…` | IPatientService |
| 55 | public (interface 默认) | ` Task<CommandResult<PatientDetailDto>> GetByIdAsync(Guid patientId, CancellationToken ct = default);` | IPatientService |
| 64 | public (interface 默认) | ` Task<CommandResult<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default);` | IPatientService |
| 69 | public (interface 默认) | ` Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default);` | IPatientService |
| 74 | public (interface 默认) | ` Task<CommandResult<byte[]>> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default);` | IPatientService |
| 18 | public (interface 默认) | ` Task<CommandResult<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request, CancellationToken ct = default);` | IRegistrationService |
| 23 | public (interface 默认) | ` Task<CommandResult<RegistrationDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);` | IRegistrationService |
| 28 | public (interface 默认) | ` Task<CommandResult<PagedResult<RegistrationListDto>>> GetPagedAsync( int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = …` | IRegistrationService |
| 38 | public (interface 默认) | ` Task<CommandResult<List<RegistrationListDto>>> GetQueueAsync( Guid? doctorId = null, CancellationToken ct = default);` | IRegistrationService |
| 46 | public (interface 默认) | ` Task<CommandResult<Guid>> StartVisitAsync(Guid id, CancellationToken ct = default);` | IRegistrationService |
| 52 | public (interface 默认) | ` Task<CommandResult> CancelAsync(Guid id, CancellationToken ct = default);` | IRegistrationService |
| 14 | public (interface 默认) | ` Task<CommandResult<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);` | IReportService |
| 17 | public (interface 默认) | ` Task<CommandResult<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = def…` | IReportService |
| 20 | public (interface 默认) | ` Task<CommandResult<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);` | IReportService |
| 48 | public (interface 默认) | ` void SetSession(UserDetailDto user, string accessToken, string? refreshToken = null);` | ISessionManager |
| 53 | public (interface 默认) | ` void ClearSession();` | ISessionManager |
| 60 | public (interface 默认) | ` bool HasPermission(UserRole requiredRole);` | ISessionManager |
| 65 | public (interface 默认) | ` bool HasPermission(string permission);` | ISessionManager |
| 70 | public (interface 默认) | ` bool HasRole(string role);` | ISessionManager |
| 75 | public (interface 默认) | ` bool IsAdmin();` | ISessionManager |
| 80 | public (interface 默认) | ` string GetCurrentUserRoleDisplay();` | ISessionManager |
| 103 | public | ` public SessionChangedEventArgs(bool isLoggedIn, UserDetailDto? user = null)` | SessionChangedEventArgs |
| 30 | public (interface 默认) | ` Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);` | IStartupStep |
| 55 | public (interface 默认) | ` void RegisterStep(IStartupStep step);` | IStartupPipeline |
| 63 | public (interface 默认) | ` Task<StartupPipelineResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);` | IStartupPipeline |
| 68 | public (interface 默认) | ` StartupPipelineDiagnostics GetDiagnostics();` | IStartupPipeline |
| 74 | public (interface 默认) | ` void Reset();` | IStartupPipeline |
| 114 | public | ` public static StartupStepResult Succeeded(TimeSpan duration) => new()` | StartupStepResult |
| 120 | public | ` public static StartupStepResult Failed(string errorMessage, Exception? exception = null, TimeSpan duration = default) => new()` | StartupStepResult |
| 128 | public | ` public static StartupStepResult SkippedResult() => new()` | StartupStepResult |
| 155 | public | ` public static StartupPipelineResult Succeeded(TimeSpan totalDuration, IReadOnlyDictionary<string, StartupStepResult> stepResults) => new()` | StartupPipelineResult |
| 162 | public | ` public static StartupPipelineResult Failed(string failedStepName, string errorMessage, TimeSpan totalDuration, IReadOnlyDictionary<string, StartupSte…` | StartupPipelineResult |
| 181 | public | ` public StartupPipelineStateChangedEventArgs( StartupPipelineState previousState, StartupPipelineState currentState, string? currentStepName = null)` | StartupPipelineStateChangedEventArgs |
| 203 | public | ` public StartupStepCompletedEventArgs( string stepName, int stepOrder, StartupStepResult result, int completedCount, int totalCount)` | StartupStepCompletedEventArgs |
| 12 | public (interface 默认) | ` void ShowInfo(string message);` | IToastService |
| 13 | public (interface 默认) | ` void ShowSuccess(string message);` | IToastService |
| 14 | public (interface 默认) | ` void ShowWarning(string message);` | IToastService |
| 15 | public (interface 默认) | ` void ShowError(string message);` | IToastService |
| 16 | public (interface 默认) | ` void Show(string message, ToastType type, int durationMilliseconds = 3000);` | IToastService |
| 15 | public (interface 默认) | ` void Invoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal);` | IUiThreadDispatcher |
| 20 | public (interface 默认) | ` T Invoke<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal);` | IUiThreadDispatcher |
| 25 | public (interface 默认) | ` Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal);` | IUiThreadDispatcher |
| 30 | public (interface 默认) | ` Task<T> InvokeAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal);` | IUiThreadDispatcher |
| 36 | public (interface 默认) | ` void BeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal);` | IUiThreadDispatcher |
| 41 | public (interface 默认) | ` bool CheckAccess();` | IUiThreadDispatcher |
| 17 | public (interface 默认) | ` void ResetActivity();` | IUserActivityState |
| 37 | public (interface 默认) | ` void StartTracking();` | IUserActivityTracker |
| 42 | public (interface 默认) | ` void StopTracking();` | IUserActivityTracker |
| 47 | public (interface 默认) | ` void ResetActivity();` | IUserActivityTracker |
| 14 | public (interface 默认) | ` Task HandleExceptionAsync(Exception exception, string? context = null);` | IUserNotificationService |
| 19 | public (interface 默认) | ` Task ShowErrorAsync(string message, string? title = null);` | IUserNotificationService |
| 24 | public (interface 默认) | ` Task ShowSuccessAsync(string message, string? title = null);` | IUserNotificationService |
| 29 | public (interface 默认) | ` Task ShowWarningAsync(string message, string? title = null);` | IUserNotificationService |
| 34 | public (interface 默认) | ` Task ShowInfoAsync(string message, string? title = null);` | IUserNotificationService |
| 39 | public (interface 默认) | ` Task<bool> ShowConfirmAsync(string message, string? title = null);` | IUserNotificationService |
| 18 | public (interface 默认) | ` Task<CommandResult<UserDetailDto>> CreateUserAsync(UserInputDto createDto, CancellationToken ct = default);` | IUserService |
| 23 | public (interface 默认) | ` Task<CommandResult<UserDetailDto>> UpdateUserAsync(UserInputDto updateDto, CancellationToken ct = default);` | IUserService |
| 28 | public (interface 默认) | ` Task<CommandResult<bool>> DeleteUserAsync(Guid userId, CancellationToken ct = default);` | IUserService |
| 33 | public (interface 默认) | ` Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> userIds, CancellationToken ct = default);` | IUserService |
| 42 | public (interface 默认) | ` Task<CommandResult<UserDetailDto>> GetByIdAsync(Guid userId, CancellationToken ct = default);` | IUserService |
| 47 | public (interface 默认) | ` Task<CommandResult<PagedResult<UserListDto>>> GetPagedAsync( int page, int pageSize, string? searchText = null, CancellationToken ct = default);` | IUserService |
| 53 | public (interface 默认) | ` Task<CommandResult<List<UserDetailDto>>> GetAllAsync(CancellationToken ct = default);` | IUserService |
| 58 | public (interface 默认) | ` Task<CommandResult<UserDetailDto>> GetByUsernameAsync(string username, CancellationToken ct = default);` | IUserService |
| 63 | public (interface 默认) | ` Task<CommandResult<List<UserListDto>>> SearchAsync(string keyword, CancellationToken ct = default);` | IUserService |
| 68 | public (interface 默认) | ` Task<CommandResult<List<UserListDto>>> GetDoctorsAsync(CancellationToken ct = default);` | IUserService |
| 77 | public (interface 默认) | ` Task<CommandResult<UserDetailDto>> ChangeProfileAsync( Guid userId, ChangeProfileDto dto, CancellationToken ct = default);` | IUserService |
| 87 | public (interface 默认) | ` Task<CommandResult<bool>> ChangePasswordAsync( Guid userId, string oldPassword, string newPassword, CancellationToken ct = default);` | IUserService |
| 96 | public (interface 默认) | ` Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync( Guid userId, string newPassword, CancellationToken ct = default);` | IUserService |
| 107 | public (interface 默认) | ` Task<CommandResult<UserDetailDto>> ToggleStatusAsync(Guid userId, CancellationToken ct = default);` | IUserService |
| 10 | public (interface 默认) | ` void SetBusy(bool isBusy, string? message = null);` | IWorkspaceHost |
| 11 | public (interface 默认) | ` Task ShowErrorAsync(string message);` | IWorkspaceHost |
| 12 | public (interface 默认) | ` Task ShowSuccessAsync(string message);` | IWorkspaceHost |
| 13 | public (interface 默认) | ` Task<bool> ShowConfirmAsync(string message, string title = "确认");` | IWorkspaceHost |
| 20 | public (interface 默认) | ` void NotifyStateChanged();` | IWorkspaceHost |
| 26 | public (interface 默认) | ` void RequestEnterEditMode();` | IWorkspaceHost |
### LYBT.Desktop.Controls

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 14 | public | ` public BaseDetailContainer() => InitializeComponent();` | BaseDetailContainer |
| 115 | private | ` private static void OnGoBackCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | BaseDetailContainer |
| 124 | private | ` private void UpdateGoBackCommandWithDirtyCheck(ICommand originalCommand)` | BaseDetailContainer |
| 16 | public | ` public BreadcrumbBar()` | BreadcrumbBar |
| 37 | private | ` private static void OnNavigationPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | BreadcrumbBar |
| 73 | private | ` private void UpdateBreadcrumbs(string path)` | BreadcrumbBar |
| 17 | public | ` public DataGridToolbar() => InitializeComponent();` | DataGridToolbar |
| 16 | public | ` public DetailToolbar() => InitializeComponent();` | DetailToolbar |
| 17 | public | ` public EmptyState() => InitializeComponent();` | EmptyState |
| 10 | public | ` public FormulaViewControl()` | FormulaViewControl |
| 25 | public | ` public HerbItemChangedEventArgs(HerbItemChangeType changeType, PrescriptionItemDto item, int index = -1)` | HerbItemChangedEventArgs |
| 74 | private | ` private static void OnAllHerbsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | HerbItemControl |
| 102 | public | ` public HerbItemControl()` | HerbItemControl |
| 114 | private | ` private void InitializeViewModel()` | HerbItemControl |
| 124 | private | ` private void OnViewModelItemChanged(object? sender, HerbItemChangedEventArgs e)` | HerbItemControl |
| 137 | public | ` public void LoadFromDto(PrescriptionItemDto dto)` | HerbItemControl |
| 145 | public | ` public PrescriptionItemDto ToDto()` | HerbItemControl |
| 153 | public | ` public void Clear()` | HerbItemControl |
| 161 | public | ` public void FocusHerbName()` | HerbItemControl |
| 172 | public | ` public bool Validate()` | HerbItemControl |
| 196 | private | ` private void OnControlPreviewMouseDown(object sender, MouseButtonEventArgs e)` | HerbItemControl |
| 214 | private | ` private bool IsDescendantOf(DependencyObject child, DependencyObject parent)` | HerbItemControl |
| 233 | private | ` private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)` | HerbItemControl |
| 296 | private | ` private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)` | HerbItemControl |
| 311 | private | ` private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)` | HerbItemControl |
| 321 | private | ` private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)` | HerbItemControl |
| 330 | private | ` private void TryAutoMatchHerb()` | HerbItemControl |
| 355 | private | ` private void OnListBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)` | HerbItemControl |
| 372 | private | ` private static T? FindParent<T>(DependencyObject child) where T : DependencyObject` | HerbItemControl |
| 384 | private | ` private void UpdateSelectedHerb(HerbListDto herb)` | HerbItemControl |
| 395 | private | ` private void HandleEnterWhenPopupClosed(KeyEventArgs e)` | HerbItemControl |
| 429 | private | ` private void OnDosageKeyDown(object sender, KeyEventArgs e)` | HerbItemControl |
| 445 | private | ` private void OnDosageGotFocus(object sender, RoutedEventArgs e)` | HerbItemControl |
| 454 | private | ` private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)` | HerbItemControl |
| 462 | private | ` private void OnDeleteMenuItemClick(object sender, RoutedEventArgs e)` | HerbItemControl |
| 125 | default(private) | ` partial void OnHerbNameChanged(string value)` | HerbItemControlViewModel |
| 133 | default(private) | ` partial void OnSelectedHerbChanged(HerbListDto? value)` | HerbItemControlViewModel |
| 154 | default(private) | ` partial void OnAllHerbsChanged(ObservableCollection<HerbListDto>? value)` | HerbItemControlViewModel |
| 166 | public | ` public void LoadFromDto(PrescriptionItemDto dto)` | HerbItemControlViewModel |
| 183 | public | ` public PrescriptionItemDto ToDto()` | HerbItemControlViewModel |
| 200 | public | ` public void Clear()` | HerbItemControlViewModel |
| 222 | public | ` public bool Validate()` | HerbItemControlViewModel |
| 235 | private | ` private void FilterHerbs()` | HerbItemControlViewModel |
| 263 | private | ` private void ValidateDosage()` | HerbItemControlViewModel |
| 293 | private | ` private void OnItemChanged(HerbItemChangeType changeType)` | HerbItemControlViewModel |
| 30 | public | ` public HerbListChangedEventArgs( HerbListChangeType changeType, int itemCount, PrescriptionItemDto? affectedItem = null, int affectedIndex = -1)` | HerbListChangedEventArgs |
| 77 | private | ` private static void OnAllHerbsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | HerbListControl |
| 101 | private | ` private static void OnDuplicateStrategyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | HerbListControl |
| 128 | private | ` private static void OnHerbItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | HerbListControl |
| 139 | private | ` private void OnHerbItemsChanged(IList<PrescriptionItemDto>? items)` | HerbListControl |
| 162 | public | ` public HerbListControl()` | HerbListControl |
| 168 | private | ` private void InitializeViewModel()` | HerbListControl |
| 178 | private | ` private void OnViewModelListChanged(object? sender, HerbListChangedEventArgs e)` | HerbListControl |
| 187 | private | ` private void SyncToHerbItemsProperty()` | HerbListControl |
| 237 | public | ` public void LoadFromDto(IEnumerable<PrescriptionItemDto> items)` | HerbListControl |
| 245 | public | ` public async Task AddHerbsAsync( IEnumerable<PrescriptionItemDto> herbs, Func<PrescriptionItemDto, PrescriptionItemDto, Task<bool>>? onDuplicateFound…` | HerbListControl |
| 258 | public | ` public void AddHerbs(IEnumerable<PrescriptionItemDto> herbs)` | HerbListControl |
| 266 | public | ` public void Clear()` | HerbListControl |
| 274 | public | ` public bool Validate()` | HerbListControl |
| 282 | public | ` public bool CanAddHerb(Guid herbId)` | HerbListControl |
| 291 | private | ` private void OnHerbItemChanged(object? sender, HerbItemChangedEventArgs e)` | HerbListControl |
| 296 | private | ` private void OnHerbItemDeleteRequested(object? sender, EventArgs e)` | HerbListControl |
| 313 | private | ` private void OnHerbItemNextRequested(object? sender, EventArgs e)` | HerbListControl |
| 348 | private | ` private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject` | HerbListControl |
| 64 | default(private) | ` partial void OnAllHerbsChanged(ObservableCollection<HerbListDto>? value)` | HerbListControlViewModel |
| 76 | public | ` public HerbListControlViewModel()` | HerbListControlViewModel |
| 90 | private | ` private void ClearAll()` | HerbListControlViewModel |
| 95 | private | ` private bool CanClearAll() => ValidItemCount > 0;` | HerbListControlViewModel |
| 101 | private | ` private void SortByRole()` | HerbListControlViewModel |
| 119 | private | ` private bool CanSortByRole() => ValidItemCount > 1;` | HerbListControlViewModel |
| 128 | public | ` public void LoadFromDto(IEnumerable<PrescriptionItemDto> items)` | HerbListControlViewModel |
| 145 | public | ` public IReadOnlyList<PrescriptionItemDto> ToDto()` | HerbListControlViewModel |
| 159 | public | ` public async Task AddHerbsAsync( IEnumerable<PrescriptionItemDto> herbs, Func<PrescriptionItemDto, PrescriptionItemDto, Task<bool>>? onDuplicateFound…` | HerbListControlViewModel |
| 196 | public | ` public void AddHerbs(IEnumerable<PrescriptionItemDto> herbs)` | HerbListControlViewModel |
| 222 | public | ` public void Clear()` | HerbListControlViewModel |
| 232 | public | ` public void DeleteAt(int index)` | HerbListControlViewModel |
| 249 | public | ` public void MoveItem(int oldIndex, int newIndex)` | HerbListControlViewModel |
| 266 | public | ` public bool Validate()` | HerbListControlViewModel |
| 280 | public | ` public bool CanAddHerb(Guid herbId)` | HerbListControlViewModel |
| 288 | public | ` public void RequestNewSlot()` | HerbListControlViewModel |
| 296 | public | ` public int GetNextEmptySlotIndex(int afterIndex)` | HerbListControlViewModel |
| 313 | private | ` private HerbItemControlViewModel CreateItemViewModel()` | HerbListControlViewModel |
| 328 | private | ` private void AddItem(PrescriptionItemDto dto)` | HerbListControlViewModel |
| 366 | private | ` private void EnsureSingleEmptySlot()` | HerbListControlViewModel |
| 391 | private | ` private void Compact()` | HerbListControlViewModel |
| 410 | private | ` private int FindHerbIndex(Guid herbId)` | HerbListControlViewModel |
| 423 | private | ` private bool CheckForDuplicates()` | HerbListControlViewModel |
| 436 | private | ` private void UnsubscribeItem(HerbItemControlViewModel item)` | HerbListControlViewModel |
| 444 | private | ` private void ClearItemsWithUnsubscribe()` | HerbListControlViewModel |
| 454 | private | ` private void OnItemChanged(object? sender, HerbItemChangedEventArgs e)` | HerbListControlViewModel |
| 473 | private | ` private void OnListChanged(HerbListChangeType changeType, PrescriptionItemDto? item = null, int index = -1)` | HerbListControlViewModel |
| 11 | public | ` public InfoCard() => InitializeComponent();` | InfoCard |
| 21 | public | ` public LoadingOverlay()` | LoadingOverlay |
| 40 | private | ` private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | LoadingOverlay |
| 45 | private | ` private void OnIsLoadingChanged(bool isLoading)` | LoadingOverlay |
| 58 | private | ` private void OnDelayTimerTick(object? sender, EventArgs e)` | LoadingOverlay |
| 20 | protected | ` protected void InitializeAsyncSupport()` | MasterDetailControlBase |
| 19 | public | ` public MasterDetailLayout()` | MasterDetailLayout |
| 26 | private | ` private void OnLoaded(object sender, RoutedEventArgs e)` | MasterDetailLayout |
| 36 | private | ` private void OnUnloaded(object sender, RoutedEventArgs e)` | MasterDetailLayout |
| 44 | private | ` private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)` | MasterDetailLayout |
| 52 | private | ` private void ApplyResponsiveLayout()` | MasterDetailLayout |
| 12 | public | ` public PatientInfoCardControl() => InitializeComponent();` | PatientInfoCardControl |
| 18 | public | ` public SearchBox()` | SearchBox |
| 70 | private | ` private void ExecuteClear()` | SearchBox |
| 105 | public | ` public StatusBadge()` | StatusBadge |
| 113 | private | ` private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | StatusBadge |
| 121 | private | ` private static void OnBadgeTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | StatusBadge |
| 133 | private | ` private void UpdateDisplay()` | StatusBadge |
| 157 | private | ` private void UpdateColors()` | StatusBadge |
| 164 | private | ` private static string MapStatusToDisplayText(string status)` | StatusBadge |
| 184 | private | ` private static BadgeType MapStatusToBadgeType(string status)` | StatusBadge |
| 195 | default(private) | ` private static (Brush background, Brush foreground) GetBadgeColors(BadgeType type)` | StatusBadge |
| 18 | public | ` public ToastControl()` | ToastControl |
| 41 | public | ` public void Show(string message, ToastType type, int durationMilliseconds = 3000)` | ToastControl |
| 66 | public | ` public void Hide()` | ToastControl |
| 88 | private | ` private void SetIcon(ToastType type)` | ToastControl |
| 10 | public | ` public UnifiedPaginationBar() => InitializeComponent();` | UnifiedPaginationBar |
| 12 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | BooleanToVisibilityConverter |
| 21 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | BooleanToVisibilityConverter |
| 16 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | BoolToBrushConverter |
| 27 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | BoolToBrushConverter |
| 17 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | BoolToColorConverter |
| 32 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | BoolToColorConverter |
| 11 | public | ` public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)` | BoolToIntConverter |
| 14 | public | ` public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)` | BoolToIntConverter |
| 14 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | DecocteMethodToVisibilityConverter |
| 29 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | DecocteMethodToVisibilityConverter |
| 16 | public | ` public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)` | EnumDescriptionConverter |
| 46 | public | ` public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)` | EnumDescriptionConverter |
| 11 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | FirstCharacterConverter |
| 21 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | FirstCharacterConverter |
| 13 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | InverseBooleanConverter |
| 24 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | InverseBooleanConverter |
| 13 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | InverseBooleanToVisibilityConverter |
| 24 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | InverseBooleanToVisibilityConverter |
| 14 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | InverseNullToVisibilityConverter |
| 24 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | InverseNullToVisibilityConverter |
| 12 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | NullToVisibilityConverter |
| 22 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | NullToVisibilityConverter |
| 15 | public | ` public object Convert(object value, Type targetType, object parameter, CultureInfo culture)` | StringToVisibilityConverter |
| 25 | public | ` public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)` | StringToVisibilityConverter |
| 15 | protected | ` protected override Freezable CreateInstanceCore() => new BindingProxy();` | BindingProxy |
| 29 | public | ` public static ScreenSizeCategory GetScreenCategory(double width)` | ResponsiveLayoutHelper |
| 43 | public | ` public static int GetOptimalColumnCount(double width)` | ResponsiveLayoutHelper |
| 57 | public | ` public static double GetRecommendedMasterWidth(ScreenSizeCategory category)` | ResponsiveLayoutHelper |
| 71 | public | ` public static double GetRecommendedDetailWidth(ScreenSizeCategory category)` | ResponsiveLayoutHelper |
| 46 | public | ` public static int CalculateMergedDosage(this DuplicateDosageStrategy strategy, int existingDosage, int newDosage)` | DuplicateDosageStrategyExtensions |
| 62 | public | ` public static string GetDisplayName(this DuplicateDosageStrategy strategy)` | DuplicateDosageStrategyExtensions |
### LYBT.Desktop.Formula

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 16 | public | ` public FormulaEditControl()` | FormulaEditControl |
| 12 | public | ` public FormulaMasterDetailControl()` | FormulaMasterDetailControl |
| 21 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | FormulaModule |
| 26 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | FormulaModule |
| 49 | private | ` private partial FormulaDetailModel ToItemCore(FormulaDetailDto dto);` | FormulaDetailModelMapper |
| 56 | public | ` public FormulaDetailModel ToItem(FormulaDetailDto dto)` | FormulaDetailModelMapper |
| 98 | private | ` private partial FormulaDetailDto ToDtoCore(FormulaDetailModel model);` | FormulaDetailModelMapper |
| 105 | public | ` public FormulaDetailDto ToDto(FormulaDetailModel model)` | FormulaDetailModelMapper |
| 138 | private | ` private partial FormulaInputDto ToInputDtoCore(FormulaDetailModel model);` | FormulaDetailModelMapper |
| 145 | public | ` public FormulaInputDto ToInputDto(FormulaDetailModel model)` | FormulaDetailModelMapper |
| 144 | public | ` public static FormulaDetailModel CreateNew()` | FormulaDetailModel |
| 157 | public | ` public FormulaDetailModel Clone()` | FormulaDetailModel |
| 103 | public | ` public static FormulaEditContext CreateNew()` | FormulaEditContext |
| 17 | public | ` public FormulaRepository( IApiClient apiClient, ILogger<FormulaRepository> logger)` | FormulaRepository |
| 29 | public | ` public async Task<List<FormulaListDto>> SearchAsync(string keyword, CancellationToken ct = default)` | FormulaRepository |
| 47 | public | ` public async Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId, CancellationToken ct = default)` | FormulaRepository |
| 68 | public | ` public async Task<FormulaDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default)` | FormulaRepository |
| 84 | public | ` public async Task<FormulaDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)` | FormulaRepository |
| 100 | public | ` public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)` | FormulaRepository |
| 113 | public | ` public async Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)` | FormulaRepository |
| 138 | public | ` public async Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default)` | FormulaRepository |
| 163 | public | ` public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)` | FormulaRepository |
| 16 | public | ` public FormulaSearchProvider(IFormulaRepository formulaRepository)` | FormulaSearchProvider |
| 22 | public | ` public async Task<PagedResult<FormulaListDto>> GetFormulasPagedAsync(int page, int pageSize)` | FormulaSearchProvider |
| 28 | public | ` public async Task<FormulaDetailDto?> GetFormulaByIdAsync(Guid id)` | FormulaSearchProvider |
| 21 | public | ` public FormulaService(IFormulaRepository repository, ILogger<FormulaService> logger)` | FormulaService |
| 29 | public | ` public async Task<CommandResult<FormulaDetailDto>> GetByIdAsync(Guid formulaId, CancellationToken ct = default)` | FormulaService |
| 48 | public | ` public async Task<CommandResult<PagedResult<FormulaListDto>>> GetPagedAsync( int page, int pageSize, string? keyword = null, CancellationToken ct = d…` | FormulaService |
| 70 | public | ` public async Task<CommandResult<FormulaDetailDto>> CreateFormulaAsync( string formulaName, string effect, string usage, string property, string categ…` | FormulaService |
| 114 | public | ` public async Task<CommandResult<FormulaDetailDto>> UpdateFormulaAsync( Guid formulaId, string formulaName, string effect, string usage, string proper…` | FormulaService |
| 164 | public | ` public async Task<CommandResult<FormulaDetailDto>> CopyFormulaAsync(FormulaDetailDto sourceFormula, CancellationToken ct = default)` | FormulaService |
| 206 | public | ` public async Task<CommandResult<bool>> DeleteFormulaAsync(Guid formulaId, CancellationToken ct = default)` | FormulaService |
| 227 | public | ` public async Task<CommandResult<FormulaDetailDto>> ToggleStatusAsync(Guid formulaId, CancellationToken ct = default)` | FormulaService |
| 252 | public | ` public async Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> formulaIds, CancellationToken ct = default)` | FormulaService |
| 277 | public | ` public async Task<CommandResult<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)` | FormulaService |
| 298 | public | ` public async Task<CommandResult<byte[]>> ExportFormulasAsync(string? category = null, CancellationToken ct = default)` | FormulaService |
| 318 | public | ` public async Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default)` | FormulaService |
| 40 | public | ` public void InitializeFromDto(FormulaDetailDto dto)` | FormulaEditorViewModel |
| 83 | public | ` public void InitializeForNewCase()` | FormulaEditorViewModel |
| 96 | public | ` public void SetAllHerbs(IEnumerable<HerbListDto> allHerbs)` | FormulaEditorViewModel |
| 108 | public | ` public List<FormulaHerbItemInputDto> GetHerbInputDtos()` | FormulaEditorViewModel |
| 125 | public | ` public bool Validate()` | FormulaEditorViewModel |
| 131 | public | ` public void AddHerb(IEnumerable<HerbListDto> allHerbs)` | FormulaEditorViewModel |
| 139 | public | ` public void DeleteHerb(FormulaHerbItemViewModel herb)` | FormulaEditorViewModel |
| 146 | public | ` public void Reset()` | FormulaEditorViewModel |
| 156 | private | ` private void OnFormulaPropertyChanged(object? sender, PropertyChangedEventArgs e)` | FormulaEditorViewModel |
| 41 | public | ` public LYBT.Shared.Models.Contracts.Formula.FormulaHerbItemInputDto ToDto()` | FormulaHerbItemViewModel |
| 47 | protected | ` protected override string? GetDetailDisplayName() => CurrentDetail?.Name;` | FormulaMasterDetailViewModel |
| 57 | public | ` public FormulaMasterDetailViewModel( IViewModelServices viewModelServices, IMasterDetailServices<FormulaListDto, FormulaDetailModel> masterDetailServ…` | FormulaMasterDetailViewModel |
| 84 | protected | ` protected override async Task LoadListAsync()` | FormulaMasterDetailViewModel |
| 118 | protected | ` protected override async Task LoadDetailAsync(FormulaListDto item)` | FormulaMasterDetailViewModel |
| 141 | protected | ` protected override FormulaDetailModel CreateNewDetail()` | FormulaMasterDetailViewModel |
| 149 | protected | ` protected override async Task<bool> SaveDetailAsync(FormulaDetailModel detail)` | FormulaMasterDetailViewModel |
| 208 | protected | ` protected override async Task<bool> DeleteItemAsync(FormulaListDto item)` | FormulaMasterDetailViewModel |
| 228 | private | ` private async Task ToggleStatusAsync()` | FormulaMasterDetailViewModel |
| 238 | private | ` private bool CanToggleStatus() => HasSelection && !IsBusy;` | FormulaMasterDetailViewModel |
| 242 | private | ` private async Task CopyFormulaAsync()` | FormulaMasterDetailViewModel |
| 278 | private | ` private bool CanCopyFormula() => HasSelection && !IsBusy;` | FormulaMasterDetailViewModel |
| 281 | protected | ` protected override async Task InvalidateCachesAsync()` | FormulaMasterDetailViewModel |
| 288 | protected | ` protected override async Task RestoreItemAsync(FormulaListDto item)` | FormulaMasterDetailViewModel |
| 295 | private | ` private void AddHerb()` | FormulaMasterDetailViewModel |
| 300 | private | ` private bool CanAddHerb() => IsEditMode;` | FormulaMasterDetailViewModel |
| 304 | private | ` private void DeleteHerb(FormulaHerbItemViewModel? herb)` | FormulaMasterDetailViewModel |
| 310 | private | ` private bool CanDeleteHerb(FormulaHerbItemViewModel? herb) => herb != null && IsEditMode;` | FormulaMasterDetailViewModel |
| 314 | private | ` private async Task SearchByCategoryAsync(string? category)` | FormulaMasterDetailViewModel |
| 327 | protected | ` protected override async Task OnNavigatedToAsync(NavigationContext navigationContext)` | FormulaMasterDetailViewModel |
| 334 | private | ` private async Task LoadAllHerbsAsync()` | FormulaMasterDetailViewModel |
| 354 | private | ` private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)` | FormulaMasterDetailViewModel |
| 359 | protected | ` protected override void Dispose(bool disposing)` | FormulaMasterDetailViewModel |
| 18 | public | ` public FormulaStatusHandler( IFormulaRepository formulaRepository, IMasterDetailServices<FormulaListDto, FormulaDetailModel> masterDetailServices, IL…` | FormulaStatusHandler |
| 28 | protected | ` protected override Guid GetEntityId(FormulaListDto e) => e.Id;` | FormulaStatusHandler |
| 29 | protected | ` protected override string GetEntityDisplayName(FormulaListDto e) => e.Name;` | FormulaStatusHandler |
| 30 | protected | ` protected override CommonStatus GetEntityStatus(FormulaListDto e) => e.Status;` | FormulaStatusHandler |
| 32 | protected | ` protected override async Task<object?> ExecuteRestoreAsync(Guid id)` | FormulaStatusHandler |
| 35 | protected | ` protected override async Task<CommonStatus?> ExecuteToggleStatusAsync(Guid id)` | FormulaStatusHandler |
| 15 | public (interface 默认) | ` Task<bool> ToggleStatusAsync(FormulaListDto formula);` | IFormulaStatusHandler |
| 22 | public (interface 默认) | ` Task<bool> RestoreAsync(FormulaListDto formula);` | IFormulaStatusHandler |
### LYBT.Desktop.Foundation

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 20 | public | ` public ApiStatusChangedEventArgs(bool isHealthy, string connectionStatus, string? lastError = null)` | ApiStatusChangedEventArgs |
| 57 | public | ` public ApplicationStateService( IApiHealthCheckService? apiHealthCheckService, IOptions<ApiClientOptions> apiOptions, ILogger<ApplicationStateService…` | ApplicationStateService |
| 75 | public | ` public async Task<bool> CheckApiHealthAsync(int timeoutSeconds = 10)` | ApplicationStateService |
| 127 | private | ` private void UpdateState(bool isHealthy, string connectionStatus, string? lastError)` | ApplicationStateService |
| 46 | public (interface 默认) | ` Task<bool> CheckApiHealthAsync(int timeoutSeconds = 10);` | IApplicationStateService |
| 24 | public | ` public DesktopCacheManager( IMemoryCache memoryCache, IEventAggregator eventAggregator, ILogger<DesktopCacheManager> logger)` | DesktopCacheManager |
| 34 | public | ` public void InvalidatePatientCaches()` | DesktopCacheManager |
| 46 | public | ` public void InvalidateMedicalCaseCaches()` | DesktopCacheManager |
| 58 | public | ` public void InvalidateHerbCaches()` | DesktopCacheManager |
| 70 | public | ` public void InvalidateFormulaCaches()` | DesktopCacheManager |
| 82 | public | ` public void InvalidateUserCaches()` | DesktopCacheManager |
| 94 | public | ` public void InvalidateAll()` | DesktopCacheManager |
| 44 | public | ` public static string GetUserMessageFromStatusCode(HttpStatusCode statusCode)` | ClientErrorMessageMapper |
| 54 | public | ` public static string GetUserMessageFromStatusCode(int statusCode)` | ClientErrorMessageMapper |
| 84 | public | ` public static string GetUserMessageFromErrorCode(string? errorCode)` | ClientErrorMessageMapper |
| 106 | public | ` public static string GetUserMessageFromErrorCode(int errorCode)` | ClientErrorMessageMapper |
| 120 | private | ` private static string GetErrorCodePrefix(string errorCode)` | ClientErrorMessageMapper |
| 132 | public | ` public static string GetUserFriendlyMessage(Exception exception)` | ClientErrorMessageMapper |
| 158 | private | ` private static string GetAppExceptionMessage(AppException exception)` | ClientErrorMessageMapper |
| 177 | private | ` private static string GetRefitApiExceptionMessage(Exception exception)` | ClientErrorMessageMapper |
| 220 | private | ` private static string? ExtractMessageFromApiResponse(string content)` | ClientErrorMessageMapper |
| 273 | private | ` private static string GetInvalidOperationExceptionMessage(InvalidOperationException exception)` | ClientErrorMessageMapper |
| 288 | private | ` private static string GetArgumentExceptionMessage(ArgumentException exception)` | ClientErrorMessageMapper |
| 309 | private | ` private static string GetHttpExceptionMessage(HttpRequestException exception)` | ClientErrorMessageMapper |
| 333 | public | ` public static string GetSafeOperationFailureMessage(string operationName, Exception exception)` | ClientErrorMessageMapper |
| 348 | public | ` public static string GetSafeOperationFailureMessage(string operationName)` | ClientErrorMessageMapper |
| 365 | public | ` public static string GetSafeMessageWithTrackingCode(string operationName, Exception exception, bool includeTrackingCode = true)` | ClientErrorMessageMapper |
| 381 | public | ` public static string GetMessageWithTrackingCode(string message, bool includeTrackingCode = true)` | ClientErrorMessageMapper |
| 395 | public | ` public static string GetShortTrackingCode()` | ClientErrorMessageMapper |
| 404 | public | ` public static string GetFullTrackingCode()` | ClientErrorMessageMapper |
| 17 | public | ` public ApiHealthCheckService(HttpClient httpClient, IOptions<ApiClientOptions> apiOptions)` | ApiHealthCheckService |
| 26 | public | ` public async Task<ApiHealthStatus> CheckHealthAsync(int timeout = 5000)` | ApiHealthCheckService |
| 13 | public (interface 默认) | ` Task<ApiHealthStatus> CheckHealthAsync(int timeout = 5000);` | IApiHealthCheckService |
| 15 | public | ` public AuthorizationMessageHandler( ITokenStorageService tokenStorage, ILogger<AuthorizationMessageHandler> logger)` | AuthorizationMessageHandler |
| 23 | protected | ` protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken)` | AuthorizationMessageHandler |
| 57 | private | ` private static bool IsAnonymousEndpoint(string path)` | AuthorizationMessageHandler |
| 26 | public | ` public AuthApiClient(IAuthApi api)` | AuthApiClient |
| 32 | public | ` public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest)` | AuthApiClient |
| 36 | public | ` public Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request)` | AuthApiClient |
| 40 | public | ` public Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest)` | AuthApiClient |
| 44 | public | ` public Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)` | AuthApiClient |
| 48 | public | ` public Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync()` | AuthApiClient |
| 52 | public | ` public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync()` | AuthApiClient |
| 17 | public | ` public AuthHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | AuthHttpApiClient |
| 19 | public | ` public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest)` | AuthHttpApiClient |
| 22 | public | ` public Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request)` | AuthHttpApiClient |
| 25 | public | ` public async Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest)` | AuthHttpApiClient |
| 31 | public | ` public Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)` | AuthHttpApiClient |
| 34 | public | ` public Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync()` | AuthHttpApiClient |
| 37 | public | ` public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync()` | AuthHttpApiClient |
| 14 | public | ` public ConfigurationApiClient(IConfigurationApi api)` | ConfigurationApiClient |
| 19 | public | ` public Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync()` | ConfigurationApiClient |
| 22 | public | ` public Task<ApiResponse<string>> GetValueAsync(string key)` | ConfigurationApiClient |
| 25 | public | ` public Task<ApiResponse> SetValueAsync(string key, string value)` | ConfigurationApiClient |
| 28 | public | ` public Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings)` | ConfigurationApiClient |
| 31 | public | ` public Task<ApiResponse> ValidateProductionAsync()` | ConfigurationApiClient |
| 17 | public | ` public ConfigurationHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | ConfigurationHttpApiClient |
| 19 | public | ` public Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync()` | ConfigurationHttpApiClient |
| 22 | public | ` public Task<ApiResponse<string>> GetValueAsync(string key)` | ConfigurationHttpApiClient |
| 25 | public | ` public Task<ApiResponse> SetValueAsync(string key, string value)` | ConfigurationHttpApiClient |
| 28 | public | ` public Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings)` | ConfigurationHttpApiClient |
| 31 | public | ` public Task<ApiResponse> ValidateProductionAsync()` | ConfigurationHttpApiClient |
| 15 | public | ` public DeployApiClient(IDeployApi api)` | DeployApiClient |
| 20 | public | ` public Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content)` | DeployApiClient |
| 23 | public | ` public Task<ApiResponse<object>> RestartAsync()` | DeployApiClient |
| 17 | public | ` public DeployHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | DeployHttpApiClient |
| 19 | public | ` public Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content)` | DeployHttpApiClient |
| 22 | public | ` public Task<ApiResponse<object>> RestartAsync()` | DeployHttpApiClient |
| 15 | public | ` public DiagnosticsApiClient(IDiagnosticsApi api)` | DiagnosticsApiClient |
| 20 | public | ` public Task<ApiResponse<object>> GetLoggingStatusAsync()` | DiagnosticsApiClient |
| 23 | public | ` public Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request)` | DiagnosticsApiClient |
| 26 | public | ` public Task<ApiResponse<object>> DisableDebugModeAsync()` | DiagnosticsApiClient |
| 29 | public | ` public Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request)` | DiagnosticsApiClient |
| 17 | public | ` public DiagnosticsHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | DiagnosticsHttpApiClient |
| 19 | public | ` public Task<ApiResponse<object>> GetLoggingStatusAsync()` | DiagnosticsHttpApiClient |
| 22 | public | ` public Task<ApiResponse<object>> EnableDebugModeAsync(EnableDebugModeRequest request)` | DiagnosticsHttpApiClient |
| 25 | public | ` public Task<ApiResponse<object>> DisableDebugModeAsync()` | DiagnosticsHttpApiClient |
| 28 | public | ` public Task<ApiResponse<object>> SetLoggingLevelAsync(SetLoggingLevelRequest request)` | DiagnosticsHttpApiClient |
| 27 | public | ` public FormulaApiClient(IFormulaApi api)` | FormulaApiClient |
| 33 | public | ` public Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync( int page = 1, int pageSize = 20, string? keyword = null, string? category = n…` | FormulaApiClient |
| 38 | public | ` public Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id)` | FormulaApiClient |
| 42 | public | ` public Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request)` | FormulaApiClient |
| 46 | public | ` public Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request)` | FormulaApiClient |
| 50 | public | ` public Task<ApiResponse> DeleteFormulaAsync(Guid id)` | FormulaApiClient |
| 54 | public | ` public Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id)` | FormulaApiClient |
| 58 | public | ` public Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id)` | FormulaApiClient |
| 62 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | FormulaApiClient |
| 66 | public | ` public Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request)` | FormulaApiClient |
| 70 | public | ` public Task<HttpResponseMessage> ExportFormulasAsync(string? category = null)` | FormulaApiClient |
| 74 | public | ` public Task<HttpResponseMessage> ExportTemplateAsync()` | FormulaApiClient |
| 78 | public | ` public Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id)` | FormulaApiClient |
| 82 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)` | FormulaApiClient |
| 86 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)` | FormulaApiClient |
| 90 | public | ` public Task<ApiResponse<List<FormulaListDto>>> GetPendingValidationAsync()` | FormulaApiClient |
| 94 | public | ` public Task<ApiResponse<FormulaHerbItemDto>> ValidateHerbAsync( Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto request)` | FormulaApiClient |
| 100 | public | ` public Task<List<string>> GetCategoriesAsync()` | FormulaApiClient |
| 18 | public | ` public FormulasHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | FormulasHttpApiClient |
| 20 | public | ` public async Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync( int page, int pageSize, string? keyword, string? category)` | FormulasHttpApiClient |
| 27 | public | ` public Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id)` | FormulasHttpApiClient |
| 30 | public | ` public Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request)` | FormulasHttpApiClient |
| 33 | public | ` public Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request)` | FormulasHttpApiClient |
| 36 | public | ` public Task<ApiResponse> DeleteFormulaAsync(Guid id)` | FormulasHttpApiClient |
| 39 | public | ` public Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id)` | FormulasHttpApiClient |
| 42 | public | ` public Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id)` | FormulasHttpApiClient |
| 45 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | FormulasHttpApiClient |
| 48 | public | ` public Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request)` | FormulasHttpApiClient |
| 51 | public | ` public async Task<HttpResponseMessage> ExportFormulasAsync(string? category)` | FormulasHttpApiClient |
| 59 | public | ` public Task<HttpResponseMessage> ExportTemplateAsync()` | FormulasHttpApiClient |
| 62 | public | ` public Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id)` | FormulasHttpApiClient |
| 65 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)` | FormulasHttpApiClient |
| 68 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)` | FormulasHttpApiClient |
| 71 | public | ` public Task<ApiResponse<List<FormulaListDto>>> GetPendingValidationAsync()` | FormulasHttpApiClient |
| 74 | public | ` public Task<ApiResponse<FormulaHerbItemDto>> ValidateHerbAsync( Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto request)` | FormulasHttpApiClient |
| 78 | public | ` public Task<List<string>> GetCategoriesAsync()` | FormulasHttpApiClient |
| 27 | public | ` public HerbApiClient(IHerbApi api)` | HerbApiClient |
| 33 | public | ` public Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync( int page = 1, int pageSize = 20, string? keyword = null, string? category = null)` | HerbApiClient |
| 38 | public | ` public Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id)` | HerbApiClient |
| 42 | public | ` public Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request)` | HerbApiClient |
| 46 | public | ` public Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request)` | HerbApiClient |
| 50 | public | ` public Task<ApiResponse> DeleteHerbAsync(Guid id)` | HerbApiClient |
| 54 | public | ` public Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request)` | HerbApiClient |
| 58 | public | ` public Task<HttpResponseMessage> ExportTemplateAsync()` | HerbApiClient |
| 62 | public | ` public Task<HttpResponseMessage> ExportHerbsAsync(string? keyword = null)` | HerbApiClient |
| 66 | public | ` public Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id)` | HerbApiClient |
| 70 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | HerbApiClient |
| 74 | public | ` public Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id)` | HerbApiClient |
| 78 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)` | HerbApiClient |
| 82 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)` | HerbApiClient |
| 87 | public | ` public Task<List<string>> GetCategoriesAsync()` | HerbApiClient |
| 18 | public | ` public HerbsHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | HerbsHttpApiClient |
| 20 | public | ` public async Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync( int page, int pageSize, string? keyword, string? category)` | HerbsHttpApiClient |
| 27 | public | ` public Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id)` | HerbsHttpApiClient |
| 30 | public | ` public Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request)` | HerbsHttpApiClient |
| 33 | public | ` public Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request)` | HerbsHttpApiClient |
| 36 | public | ` public Task<ApiResponse> DeleteHerbAsync(Guid id)` | HerbsHttpApiClient |
| 39 | public | ` public Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request)` | HerbsHttpApiClient |
| 42 | public | ` public Task<HttpResponseMessage> ExportTemplateAsync()` | HerbsHttpApiClient |
| 45 | public | ` public async Task<HttpResponseMessage> ExportHerbsAsync(string? keyword)` | HerbsHttpApiClient |
| 53 | public | ` public Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id)` | HerbsHttpApiClient |
| 56 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | HerbsHttpApiClient |
| 59 | public | ` public Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id)` | HerbsHttpApiClient |
| 62 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)` | HerbsHttpApiClient |
| 65 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)` | HerbsHttpApiClient |
| 68 | public | ` public Task<List<string>> GetCategoriesAsync()` | HerbsHttpApiClient |
| 28 | public | ` public MedicalCaseApiClient(IMedicalCaseApi api)` | MedicalCaseApiClient |
| 34 | public | ` public Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync( int page = 1, int pageSize = 20, string? keyword = null, bool include…` | MedicalCaseApiClient |
| 39 | public | ` public Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync( MedicalCaseQueryType queryType = MedicalCaseQueryType.All, Guid? pa…` | MedicalCaseApiClient |
| 51 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id)` | MedicalCaseApiClient |
| 55 | public | ` public Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(Guid? patientId = null)` | MedicalCaseApiClient |
| 59 | public | ` public Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync( string? patientName = null, string? diagnosisKeyword = null, Dat…` | MedicalCaseApiClient |
| 69 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseInputDto request)` | MedicalCaseApiClient |
| 73 | public | ` public Task<ApiResponse> DeleteMedicalCaseAsync(Guid id)` | MedicalCaseApiClient |
| 77 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync( Guid medicalCaseId, SetPrescriptionFlagRequest request)` | MedicalCaseApiClient |
| 82 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id)` | MedicalCaseApiClient |
| 86 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync(Guid id, ConsultationInputDto? request = null)` | MedicalCaseApiClient |
| 94 | public | ` public async Task<ApiResponse> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequest? request = null)` | MedicalCaseApiClient |
| 104 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)` | MedicalCaseApiClient |
| 108 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(Guid id, MedicalCaseInputDto request)` | MedicalCaseApiClient |
| 112 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | MedicalCaseApiClient |
| 116 | public | ` public Task<ApiResponse<MedicalCasePermissionsDto>> GetPermissionsAsync(Guid id)` | MedicalCaseApiClient |
| 120 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintAsync(Guid id, RecordPrintRequest request)` | MedicalCaseApiClient |
| 124 | public | ` public Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid id, int page = 1, int pageSize = 20)` | MedicalCaseApiClient |
| 19 | public | ` public MedicalCasesHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | MedicalCasesHttpApiClient |
| 21 | public | ` public async Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync( int page, int pageSize, string? keyword, bool includeAllDoctors…` | MedicalCasesHttpApiClient |
| 30 | public | ` public async Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync( MedicalCaseQueryType queryType, Guid? patientId, Guid? doctor…` | MedicalCasesHttpApiClient |
| 42 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id)` | MedicalCasesHttpApiClient |
| 45 | public | ` public async Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(Guid? patientId)` | MedicalCasesHttpApiClient |
| 52 | public | ` public async Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync( string? patientName, string? diagnosisKeyword, DateTime? s…` | MedicalCasesHttpApiClient |
| 63 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseInputDto request)` | MedicalCasesHttpApiClient |
| 66 | public | ` public Task<ApiResponse> DeleteMedicalCaseAsync(Guid id)` | MedicalCasesHttpApiClient |
| 69 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request)` | MedicalCasesHttpApiClient |
| 72 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id)` | MedicalCasesHttpApiClient |
| 75 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync(Guid id, ConsultationInputDto? request)` | MedicalCasesHttpApiClient |
| 78 | public | ` public async Task<ApiResponse> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequest? request)` | MedicalCasesHttpApiClient |
| 84 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)` | MedicalCasesHttpApiClient |
| 87 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(Guid id, MedicalCaseInputDto request)` | MedicalCasesHttpApiClient |
| 90 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | MedicalCasesHttpApiClient |
| 93 | public | ` public Task<ApiResponse<MedicalCasePermissionsDto>> GetPermissionsAsync(Guid id)` | MedicalCasesHttpApiClient |
| 96 | public | ` public Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintAsync(Guid id, RecordPrintRequest request)` | MedicalCasesHttpApiClient |
| 99 | public | ` public Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid id, int page, int pageSize)` | MedicalCasesHttpApiClient |
| 27 | public | ` public PatientApiClient(IPatientApi api)` | PatientApiClient |
| 33 | public | ` public Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync( int page = 1, int pageSize = 20, string? keyword = null)` | PatientApiClient |
| 38 | public | ` public Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id)` | PatientApiClient |
| 42 | public | ` public Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request)` | PatientApiClient |
| 46 | public | ` public Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request)` | PatientApiClient |
| 50 | public | ` public Task<ApiResponse> DeletePatientAsync(Guid id)` | PatientApiClient |
| 54 | public | ` public Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request)` | PatientApiClient |
| 58 | public | ` public Task<HttpResponseMessage> ExportTemplateAsync()` | PatientApiClient |
| 62 | public | ` public Task<HttpResponseMessage> ExportPatientsAsync(string? keyword = null)` | PatientApiClient |
| 66 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | PatientApiClient |
| 70 | public | ` public Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id)` | PatientApiClient |
| 74 | public | ` public Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id)` | PatientApiClient |
| 18 | public | ` public PatientsHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | PatientsHttpApiClient |
| 20 | public | ` public async Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync( int page, int pageSize, string? keyword)` | PatientsHttpApiClient |
| 27 | public | ` public Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id)` | PatientsHttpApiClient |
| 30 | public | ` public Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request)` | PatientsHttpApiClient |
| 33 | public | ` public Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request)` | PatientsHttpApiClient |
| 36 | public | ` public Task<ApiResponse> DeletePatientAsync(Guid id)` | PatientsHttpApiClient |
| 39 | public | ` public Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request)` | PatientsHttpApiClient |
| 42 | public | ` public Task<HttpResponseMessage> ExportTemplateAsync()` | PatientsHttpApiClient |
| 45 | public | ` public async Task<HttpResponseMessage> ExportPatientsAsync(string? keyword)` | PatientsHttpApiClient |
| 53 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | PatientsHttpApiClient |
| 56 | public | ` public Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id)` | PatientsHttpApiClient |
| 59 | public | ` public Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id)` | PatientsHttpApiClient |
| 26 | public | ` public RegistrationApiClient(IRegistrationApi api)` | RegistrationApiClient |
| 32 | public | ` public Task<ApiResponse<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request)` | RegistrationApiClient |
| 36 | public | ` public Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id)` | RegistrationApiClient |
| 40 | public | ` public Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync( int page = 1, int pageSize = 20, string? keyword = null, DateTime? startDate…` | RegistrationApiClient |
| 51 | public | ` public Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync(Guid? doctorId = null)` | RegistrationApiClient |
| 55 | public | ` public Task<ApiResponse<Guid>> StartVisitAsync(Guid id)` | RegistrationApiClient |
| 59 | public | ` public Task<ApiResponse> CancelAsync(Guid id)` | RegistrationApiClient |
| 64 | public | ` public Task<List<RegistrationListDto>> GetRegistrationsAsync(DateTime? date = null)` | RegistrationApiClient |
| 69 | public | ` public Task<QuickVisitResultDto> QuickVisitAsync(QuickVisitInputDto request)` | RegistrationApiClient |
| 74 | public | ` public Task DeleteRegistrationAsync(Guid id)` | RegistrationApiClient |
| 18 | public | ` public RegistrationsHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | RegistrationsHttpApiClient |
| 20 | public | ` public Task<ApiResponse<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request)` | RegistrationsHttpApiClient |
| 23 | public | ` public Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id)` | RegistrationsHttpApiClient |
| 26 | public | ` public async Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync( int page, int pageSize, string? keyword, DateTime? startDate, DateTime…` | RegistrationsHttpApiClient |
| 39 | public | ` public async Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync(Guid? doctorId)` | RegistrationsHttpApiClient |
| 46 | public | ` public async Task<ApiResponse<Guid>> StartVisitAsync(Guid id)` | RegistrationsHttpApiClient |
| 49 | public | ` public async Task<ApiResponse> CancelAsync(Guid id)` | RegistrationsHttpApiClient |
| 55 | public | ` public async Task<List<RegistrationListDto>> GetRegistrationsAsync(DateTime? date)` | RegistrationsHttpApiClient |
| 62 | public | ` public Task<QuickVisitResultDto> QuickVisitAsync(QuickVisitInputDto request)` | RegistrationsHttpApiClient |
| 65 | public | ` public async Task DeleteRegistrationAsync(Guid id)` | RegistrationsHttpApiClient |
| 12 | public | ` public ReportsApiClient(IReportsApi api)` | ReportsApiClient |
| 17 | public | ` public Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null)` | ReportsApiClient |
| 20 | public | ` public Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null)` | ReportsApiClient |
| 23 | public | ` public Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null)` | ReportsApiClient |
| 17 | public | ` public ReportsHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | ReportsHttpApiClient |
| 19 | public | ` public Task<ApiResponse<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate, DateTime? endDate)` | ReportsHttpApiClient |
| 22 | public | ` public Task<ApiResponse<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate, DateTime? endDate)` | ReportsHttpApiClient |
| 25 | public | ` public Task<ApiResponse<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate, DateTime? endDate)` | ReportsHttpApiClient |
| 27 | public | ` public UserApiClient(IUserApi api)` | UserApiClient |
| 33 | public | ` public Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync( int page = 1, int pageSize = 20, string? keyword = null)` | UserApiClient |
| 38 | public | ` public Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id)` | UserApiClient |
| 42 | public | ` public Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request)` | UserApiClient |
| 46 | public | ` public Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request)` | UserApiClient |
| 50 | public | ` public Task<ApiResponse> DeleteUserAsync(Guid id)` | UserApiClient |
| 54 | public | ` public Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request)` | UserApiClient |
| 58 | public | ` public Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request)` | UserApiClient |
| 62 | public | ` public Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequest request)` | UserApiClient |
| 65 | public | ` public Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id)` | UserApiClient |
| 69 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | UserApiClient |
| 73 | public | ` public Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id)` | UserApiClient |
| 77 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)` | UserApiClient |
| 81 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)` | UserApiClient |
| 86 | public | ` public Task<UserDetailDto> GetCurrentUserAsync()` | UserApiClient |
| 18 | public | ` public UsersHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }` | UsersHttpApiClient |
| 20 | public | ` public async Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync( int page, int pageSize, string? keyword)` | UsersHttpApiClient |
| 27 | public | ` public Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id)` | UsersHttpApiClient |
| 30 | public | ` public Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request)` | UsersHttpApiClient |
| 33 | public | ` public Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request)` | UsersHttpApiClient |
| 36 | public | ` public Task<ApiResponse> DeleteUserAsync(Guid id)` | UsersHttpApiClient |
| 39 | public | ` public Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request)` | UsersHttpApiClient |
| 42 | public | ` public Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request)` | UsersHttpApiClient |
| 45 | public | ` public Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequest request)` | UsersHttpApiClient |
| 48 | public | ` public Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id)` | UsersHttpApiClient |
| 51 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)` | UsersHttpApiClient |
| 54 | public | ` public Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id)` | UsersHttpApiClient |
| 57 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)` | UsersHttpApiClient |
| 60 | public | ` public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)` | UsersHttpApiClient |
| 63 | public | ` public Task<UserDetailDto> GetCurrentUserAsync()` | UsersHttpApiClient |
| 40 | protected | ` protected HttpApiClientBase(IHttpClientFactory httpClientFactory)` | HttpApiClientBase |
| 45 | protected | ` protected HttpClient CreateClient() => _httpClientFactory.CreateClient();` | HttpApiClientBase |
| 47 | protected | ` protected static StringContent ToJsonContent<T>(T value)` | HttpApiClientBase |
| 53 | protected | ` protected static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct = default)` | HttpApiClientBase |
| 63 | protected | ` protected static async Task<ApiResponse<T>> DeserializeEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken ct = default)` | HttpApiClientBase |
| 82 | protected | ` protected static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)` | HttpApiClientBase |
| 95 | protected | ` protected static ApiResponse<T> WrapSuccess<T>(T data, string message = "操作成功")` | HttpApiClientBase |
| 98 | protected | ` protected static ApiResponse WrapSuccess(string message = "操作成功")` | HttpApiClientBase |
| 102 | protected | ` protected static string BuildPagedUrl(string baseUrl, int page, int pageSize, params (string Key, string? Value)[] filters)` | HttpApiClientBase |
| 117 | protected | ` protected static string BuildQueryString(string baseUrl, params (string Key, string? Value)[] parameters)` | HttpApiClientBase |
| 134 | protected | ` protected async Task<HttpResponseMessage> SendAsync(string url, HttpMethod method, object? body = null, CancellationToken ct = default)` | HttpApiClientBase |
| 165 | protected | ` protected async Task<ApiResponse<T>> GetAndWrapAsync<T>(string url, CancellationToken ct = default)` | HttpApiClientBase |
| 172 | protected | ` protected async Task<T> GetRawAsync<T>(string url, CancellationToken ct = default)` | HttpApiClientBase |
| 180 | protected | ` protected Task<ApiResponse<T>> PostAndWrapAsync<T>(string url, object? body = null, CancellationToken ct = default)` | HttpApiClientBase |
| 184 | protected | ` protected async Task<ApiResponse> SendVoidAsync(string url, HttpMethod method, object? body = null, CancellationToken ct = default)` | HttpApiClientBase |
| 191 | protected | ` protected Task<ApiResponse> PostVoidAsync(string url, object? body = null, CancellationToken ct = default)` | HttpApiClientBase |
| 195 | protected | ` protected async Task<T> PostRawAsync<T>(string url, object? body = null, CancellationToken ct = default)` | HttpApiClientBase |
| 203 | protected | ` protected Task<ApiResponse<T>> PutAndWrapAsync<T>(string url, object? body = null, CancellationToken ct = default)` | HttpApiClientBase |
| 207 | protected | ` protected Task<ApiResponse> PutVoidAsync(string url, object? body = null, CancellationToken ct = default)` | HttpApiClientBase |
| 211 | protected | ` protected Task<ApiResponse> DeleteVoidAsync(string url, CancellationToken ct = default)` | HttpApiClientBase |
| 215 | protected | ` protected async Task<ApiResponse<T>> SendAndWrapAsync<T>(string url, HttpMethod method, object? body = null, CancellationToken ct = default)` | HttpApiClientBase |
| 222 | protected | ` protected Task<ApiResponse<PagedResult<T>>> GetPagedAndWrapAsync<T>(string url, CancellationToken ct = default)` | HttpApiClientBase |
| 226 | protected | ` protected async Task<HttpResponseMessage> GetResponseAsync(string url, CancellationToken ct = default)` | HttpApiClientBase |
| 44 | public | ` public HttpClientApiClient(IHttpClientFactory httpClientFactory)` | HttpClientApiClient |
| 43 | public | ` public static void AddHttpClientApiClient( this IContainerRegistry containerRegistry, int port)` | HttpClientApiClientExtensions |
| 86 | public | ` public static void AddHttpClientApiClient( this IContainerRegistry containerRegistry)` | HttpClientApiClientExtensions |
| 120 | public | ` public LocalWebApiHttpClientFactory(Uri baseAddress)` | LocalWebApiHttpClientFactory |
| 130 | public | ` public HttpClient CreateClient(string name) => _httpClient;` | LocalWebApiHttpClientFactory |
| 133 | public | ` public void Dispose()` | LocalWebApiHttpClientFactory |
| 17 | public | ` public LoggingHttpHandler(ILogger<LoggingHttpHandler> logger)` | LoggingHttpHandler |
| 22 | protected | ` protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken)` | LoggingHttpHandler |
| 61 | public | ` public RefitApiClient(HttpClient httpClient, RefitSettings refitSettings)` | RefitApiClient |
| 34 | public | ` public static void AddRefitApiClient( this IContainerRegistry containerRegistry, HttpClient httpClient, RefitSettings refitSettings)` | RefitApiClientExtensions |
| 16 | public | ` public static IAsyncPolicy<HttpResponseMessage> CreateHttpRetryPolicy( ILogger? logger = null, int retryCount = 3, TimeSpan? baseDelay = null)` | RetryPolicyExtensions |
| 43 | public | ` public static IAsyncPolicy<HttpResponseMessage> CreateTimeoutPolicy( TimeSpan timeout, ILogger? logger = null)` | RetryPolicyExtensions |
| 53 | public | ` public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy( ILogger? logger = null, int failureThreshold = 5, TimeSpan durationOfBrea…` | RetryPolicyExtensions |
| 86 | public | ` public static IAsyncPolicy<HttpResponseMessage> CreateCompositePolicy( ILogger? logger = null, int retryCount = 3, TimeSpan? baseDelay = null, TimeSp…` | RetryPolicyExtensions |
| 107 | private | ` private static bool ShouldRetry(HttpStatusCode statusCode)` | RetryPolicyExtensions |
| 36 | public | ` public SwitchingApiClient( IConnectionSettingsService connectionSettings, Func<string, HttpClient> remoteHttpClientFactory, Func<string, IHttpClientF…` | SwitchingApiClient |
| 118 | public | ` public void Dispose()` | SwitchingApiClient |
| 46 | public | ` public TokenRefreshHandler( ITokenStorageService tokenStorage, ICredentialVault credentialVault, IOptions<ApiClientOptions> apiOptions, ILogger<Token…` | TokenRefreshHandler |
| 78 | protected | ` protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken)` | TokenRefreshHandler |
| 148 | public | ` public async Task<TokenRefreshResult> RefreshTokenAsync()` | TokenRefreshHandler |
| 170 | private | ` private async Task<TokenRefreshResult> RefreshTokenWithRetryAsync(string refreshToken)` | TokenRefreshHandler |
| 231 | private | ` private async Task<TokenRefreshResult> ExecuteRefreshAsync(string refreshToken)` | TokenRefreshHandler |
| 291 | private | ` private async Task<TokenRefreshResult> HandleRefreshErrorResponseAsync(HttpResponseMessage response)` | TokenRefreshHandler |
| 311 | private | ` private TokenRefreshResult CategorizeUnauthorizedError(string errorContent)` | TokenRefreshHandler |
| 337 | private | ` private TokenRefreshResult CategorizeApiError(string errorMessage)` | TokenRefreshHandler |
| 362 | private | ` private static TokenRefreshFailedEventArgs CreateFailedEventArgs( TokenRefreshFailureReason reason, string detailedMessage)` | TokenRefreshHandler |
| 380 | private | ` private async Task PublishTokenRefreshSucceededEventAsync()` | TokenRefreshHandler |
| 407 | private | ` private void PublishTokenRefreshFailedEvent(TokenRefreshFailedEventArgs eventArgs)` | TokenRefreshHandler |
| 436 | private | ` private static bool IsAutoLoginEligible(TokenRefreshFailureReason? reason)` | TokenRefreshHandler |
| 447 | private | ` private async Task<TokenRefreshResult> TryAutoLoginFallbackAsync()` | TokenRefreshHandler |
| 525 | protected | ` protected override void Dispose(bool disposing)` | TokenRefreshHandler |
| 12 | public (interface 默认) | ` Task LoadModuleAsync(string moduleName);` | IModuleLoadingService |
| 17 | public (interface 默认) | ` Task LoadAllModulesAsync();` | IModuleLoadingService |
| 22 | public (interface 默认) | ` IEnumerable<string> GetLoadedModules();` | IModuleLoadingService |
| 28 | public (interface 默认) | ` bool IsModuleLoaded(string moduleName);` | IModuleLoadingService |
| 39 | public (interface 默认) | ` Task LoadModulesAsync(IEnumerable<string>? moduleNames = null);` | IModuleLoadingService |
| 19 | public | ` public ModuleLoadingService( IModuleManager moduleManager, IModuleCatalog moduleCatalog, ILogger<ModuleLoadingService> logger)` | ModuleLoadingService |
| 29 | public | ` public async Task LoadModuleAsync(string moduleName)` | ModuleLoadingService |
| 65 | public | ` public async Task LoadAllModulesAsync()` | ModuleLoadingService |
| 83 | public | ` public IEnumerable<string> GetLoadedModules()` | ModuleLoadingService |
| 91 | public | ` public bool IsModuleLoaded(string moduleName)` | ModuleLoadingService |
| 99 | public | ` public async Task LoadModulesAsync(IEnumerable<string>? moduleNames = null)` | ModuleLoadingService |
| 15 | protected | ` protected ApiClientRepositoryBase(ILogger logger)` | ApiClientRepositoryBase |
| 28 | protected | ` protected void HandleException(Exception ex, string operation, params object?[] args)` | ApiClientRepositoryBase |
| 44 | protected | ` protected async Task ExecuteAsync( Func<Task> action, string operation, LogLevel logLevel = LogLevel.Debug)` | ApiClientRepositoryBase |
| 67 | protected | ` protected async Task<TResult> ExecuteAsync<TResult>( Func<Task<TResult>> func, string operation, LogLevel logLevel = LogLevel.Debug)` | ApiClientRepositoryBase |
| 93 | protected | ` protected async Task<TResult> ExecuteAsync<TResult>( Func<Task<TResult>> func, string operation, string logMessage, object?[] logArgs, LogLevel logLe…` | ApiClientRepositoryBase |
| 119 | protected | ` protected async Task<BatchOperationResultDto?> ExecuteBatchDeleteAsync( Func<Task<ApiResponse<BatchOperationResultDto>>> func, string operation, stri…` | ApiClientRepositoryBase |
| 31 | protected | ` protected EntityApiClientRepositoryBase( ILogger logger, IEntityApiSegment<TListDto, TDetailDto, TInputDto> api)` | EntityApiClientRepositoryBase |
| 42 | public | ` public virtual async Task<PagedResult<TListDto>> GetPagedAsync( int page = 1, int pageSize = 20, string? keyword = null, string? category = null, Can…` | EntityApiClientRepositoryBase |
| 69 | public | ` public virtual Task<TDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)` | EntityApiClientRepositoryBase |
| 81 | public | ` public virtual Task<TDetailDto> CreateAsync(TInputDto dto, CancellationToken ct = default)` | EntityApiClientRepositoryBase |
| 97 | public | ` public virtual Task<TDetailDto> UpdateAsync(TInputDto dto, CancellationToken ct = default)` | EntityApiClientRepositoryBase |
| 117 | public | ` public virtual Task DeleteAsync(Guid id, CancellationToken ct = default)` | EntityApiClientRepositoryBase |
| 28 | public | ` public AuthenticationService( IApiClientAuth authApi, ITokenStorageService tokenStorage, ITokenValidator tokenValidator, ICredentialVault credentialV…` | AuthenticationService |
| 47 | public | ` public async Task<bool> IsLoggedInAsync()` | AuthenticationService |
| 56 | public | ` public async Task<CommandResult<LoginResponse>> LoginAsync(LoginRequest request)` | AuthenticationService |
| 84 | public | ` public async Task<CommandResult> LogoutAsync()` | AuthenticationService |
| 137 | public | ` public async Task<UserDetailDto?> GetCurrentUserAsync()` | AuthenticationService |
| 147 | public | ` public UserDetailDto? GetCurrentUser()` | AuthenticationService |
| 157 | public | ` public string? GetToken()` | AuthenticationService |
| 166 | public | ` public async Task<CommandResult<ValidateTokenResponse>> ValidateTokenAsync(string token)` | AuthenticationService |
| 213 | public | ` public void ClearAuthInfo()` | AuthenticationService |
| 221 | public | ` public async Task<bool> CheckConnectionAsync()` | AuthenticationService |
| 249 | public | ` public async Task<CommandResult<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request)` | AuthenticationService |
| 282 | private | ` private void PublishLoginStartedEvent(string? userName, bool isAutoLogin)` | AuthenticationService |
| 305 | private | ` private DateTime? ExtractTokenExpiration(string token)` | AuthenticationService |
| 120 | public | ` public AuthenticationStateMachine( ILogger<AuthenticationStateMachine> logger, IEventAggregator? eventAggregator = null)` | AuthenticationStateMachine |
| 131 | internal | ` internal AuthenticationStateMachine( ILogger<AuthenticationStateMachine> logger, AuthState initialState)` | AuthenticationStateMachine |
| 140 | public | ` public bool CanFire(AuthEvent evt)` | AuthenticationStateMachine |
| 149 | public | ` public bool Fire(AuthEvent evt, string? statusMessage = null)` | AuthenticationStateMachine |
| 180 | public | ` public Task<bool> FireAsync(AuthEvent evt, string? statusMessage = null)` | AuthenticationStateMachine |
| 186 | public | ` public void Reset()` | AuthenticationStateMachine |
| 192 | public | ` public IEnumerable<AuthEvent> GetPermittedEvents()` | AuthenticationStateMachine |
| 206 | internal | ` internal void ForceState(AuthState state, string? statusMessage = null)` | AuthenticationStateMachine |
| 221 | private | ` private static string? GetDefaultStatusMessage(AuthState state)` | AuthenticationStateMachine |
| 243 | private | ` private void RaiseStateChanged(AuthStateChangedEventArgs args)` | AuthenticationStateMachine |
| 321 | public | ` public TokenLifecycleStateChangedEventArgs( TokenLifecycleState previousState, TokenLifecycleState currentState, TimeSpan? remainingTime = null)` | TokenLifecycleStateChangedEventArgs |
| 30 | public | ` public CredentialStorage(ILogger logger)` | CredentialStorage |
| 51 | public | ` public Task<VaultStorage?> LoadVaultAsync()` | CredentialStorage |
| 79 | public | ` public Task SaveVaultAsync(VaultStorage vault)` | CredentialStorage |
| 99 | public | ` public bool DeleteVaultFile()` | CredentialStorage |
| 115 | public | ` public bool VaultFileExists() => File.Exists(_vaultFilePath);` | CredentialStorage |
| 120 | public | ` public bool OldCredentialsFileExists() => File.Exists(_oldCredentialsPath);` | CredentialStorage |
| 125 | public | ` public async Task<OldCredentialFormat?> ReadOldCredentialsAsync()` | CredentialStorage |
| 23 | public | ` public CredentialVault(ILogger<CredentialVault> logger)` | CredentialVault |
| 35 | public | ` public async Task<bool> SavePasswordAsync(string username, string password)` | CredentialVault |
| 95 | public | ` public async Task<string?> GetPasswordAsync(string username)` | CredentialVault |
| 147 | public | ` public async Task<bool> HasSavedPasswordAsync(string username)` | CredentialVault |
| 198 | public | ` public async Task<bool> ClearPasswordAsync(string username)` | CredentialVault |
| 241 | public | ` public async Task<bool> SaveAutoLoginTokenAsync(string username, string autoLoginToken)` | CredentialVault |
| 308 | public | ` public async Task<string?> GetAutoLoginTokenAsync(string username)` | CredentialVault |
| 364 | public | ` public async Task<bool> ClearCredentialsAsync(string? username = null)` | CredentialVault |
| 407 | public | ` public async Task<bool> VerifyIntegrityAsync(string username)` | CredentialVault |
| 446 | public | ` public async Task MigrateOldFormatAsync()` | CredentialVault |
| 491 | public | ` public async Task<bool> HasValidTokenAsync(string username)` | CredentialVault |
| 17 | public | ` public DpapiPhotoStorageService(ILogger<DpapiPhotoStorageService> logger)` | DpapiPhotoStorageService |
| 33 | public | ` public Task<string> SavePhotoAsync(byte[] photoData, string identifier)` | DpapiPhotoStorageService |
| 71 | public | ` public Task<byte[]?> LoadPhotoAsync(string encryptedFilePath)` | DpapiPhotoStorageService |
| 111 | public | ` public Task<bool> DeletePhotoAsync(string encryptedFilePath)` | DpapiPhotoStorageService |
| 138 | public | ` public bool PhotoExists(string encryptedFilePath)` | DpapiPhotoStorageService |
| 146 | private | ` private static string ComputeFileHash(string identifier)` | DpapiPhotoStorageService |
| 18 | public | ` public DpapiProtector()` | DpapiProtector |
| 28 | public | ` public string Encrypt(string plainText)` | DpapiProtector |
| 41 | public | ` public string Decrypt(string encryptedBase64)` | DpapiProtector |
| 56 | public | ` public string ComputeHmac(string username, string encryptedData)` | DpapiProtector |
| 18 | public (interface 默认) | ` Task<bool> IsLoggedInAsync();` | IAuthenticationService |
| 23 | public (interface 默认) | ` Task<CommandResult<LoginResponse>> LoginAsync(LoginRequest request);` | IAuthenticationService |
| 28 | public (interface 默认) | ` Task<CommandResult> LogoutAsync();` | IAuthenticationService |
| 33 | public (interface 默认) | ` Task<UserDetailDto?> GetCurrentUserAsync();` | IAuthenticationService |
| 39 | public (interface 默认) | ` UserDetailDto? GetCurrentUser();` | IAuthenticationService |
| 44 | public (interface 默认) | ` string? GetToken();` | IAuthenticationService |
| 49 | public (interface 默认) | ` Task<CommandResult<ValidateTokenResponse>> ValidateTokenAsync(string token);` | IAuthenticationService |
| 54 | public (interface 默认) | ` void ClearAuthInfo();` | IAuthenticationService |
| 59 | public (interface 默认) | ` Task<bool> CheckConnectionAsync();` | IAuthenticationService |
| 70 | public (interface 默认) | ` Task<CommandResult<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request);` | IAuthenticationService |
| 24 | public (interface 默认) | ` Task<bool> SavePasswordAsync(string username, string password);` | ICredentialVault |
| 31 | public (interface 默认) | ` Task<string?> GetPasswordAsync(string username);` | ICredentialVault |
| 38 | public (interface 默认) | ` Task<bool> HasSavedPasswordAsync(string username);` | ICredentialVault |
| 45 | public (interface 默认) | ` Task<bool> ClearPasswordAsync(string username);` | ICredentialVault |
| 57 | public (interface 默认) | ` Task<bool> SaveAutoLoginTokenAsync(string username, string autoLoginToken);` | ICredentialVault |
| 64 | public (interface 默认) | ` Task<string?> GetAutoLoginTokenAsync(string username);` | ICredentialVault |
| 71 | public (interface 默认) | ` Task<bool> ClearCredentialsAsync(string? username = null);` | ICredentialVault |
| 78 | public (interface 默认) | ` Task<bool> VerifyIntegrityAsync(string username);` | ICredentialVault |
| 84 | public (interface 默认) | ` Task MigrateOldFormatAsync();` | ICredentialVault |
| 91 | public (interface 默认) | ` Task<bool> HasValidTokenAsync(string username);` | ICredentialVault |
| 22 | public (interface 默认) | ` Task<LogoutResult> LogoutAsync();` | ILogoutService |
| 30 | public (interface 默认) | ` Task ExecuteLocalLogoutAsync();` | ILogoutService |
| 37 | public (interface 默认) | ` Task<int> ProcessPendingServerLogoutsAsync();` | ILogoutService |
| 63 | public | ` public static LogoutResult FullSuccess(string? message = null) =>` | LogoutResult |
| 69 | public | ` public static LogoutResult LocalSuccessServerQueued(string? message = null) =>` | LogoutResult |
| 75 | public | ` public static LogoutResult LocalSuccessOnly(string? message = null) =>` | LogoutResult |
| 15 | public (interface 默认) | ` Task<string> SavePhotoAsync(byte[] photoData, string identifier);` | IPhotoStorageService |
| 22 | public (interface 默认) | ` Task<byte[]?> LoadPhotoAsync(string encryptedFilePath);` | IPhotoStorageService |
| 29 | public (interface 默认) | ` Task<bool> DeletePhotoAsync(string encryptedFilePath);` | IPhotoStorageService |
| 34 | public (interface 默认) | ` bool PhotoExists(string encryptedFilePath);` | IPhotoStorageService |
| 33 | public (interface 默认) | ` void StartMonitoring(DateTime tokenExpiresAt);` | ITokenLifecycleService |
| 39 | public (interface 默认) | ` Task StartMonitoringFromStorageAsync();` | ITokenLifecycleService |
| 44 | public (interface 默认) | ` void StopMonitoring();` | ITokenLifecycleService |
| 50 | public (interface 默认) | ` void UpdateExpiration(DateTime newExpiresAt);` | ITokenLifecycleService |
| 56 | public (interface 默认) | ` Task<bool> TryRefreshTokenAsync();` | ITokenLifecycleService |
| 61 | public (interface 默认) | ` void Reset();` | ITokenLifecycleService |
| 39 | public (interface 默认) | ` void SetTokens(string accessToken, string refreshToken, DateTime expiry);` | ITokenManager |
| 44 | public (interface 默认) | ` void ClearTokens();` | ITokenManager |
| 50 | public (interface 默认) | ` bool IsTokenValid();` | ITokenManager |
| 57 | public (interface 默认) | ` bool IsTokenExpiringSoon(TimeSpan threshold);` | ITokenManager |
| 11 | public (interface 默认) | ` Task<TokenRefreshResult> RefreshTokenAsync();` | ITokenRefreshHandler |
| 18 | public (interface 默认) | ` Task SaveAuthenticationAsync(LoginResponse loginResponse, bool rememberMe);` | ITokenStorageService |
| 23 | public (interface 默认) | ` Task<string?> GetTokenAsync();` | ITokenStorageService |
| 28 | public (interface 默认) | ` Task<string?> GetRefreshTokenAsync();` | ITokenStorageService |
| 33 | public (interface 默认) | ` Task<LoginResponse?> GetLoginResponseAsync();` | ITokenStorageService |
| 38 | public (interface 默认) | ` Task ClearAuthenticationAsync();` | ITokenStorageService |
| 43 | public (interface 默认) | ` Task<bool> IsTokenExpiredAsync();` | ITokenStorageService |
| 59 | public (interface 默认) | ` string? GetToken();` | ITokenStorageService |
| 64 | public (interface 默认) | ` LoginResponse? GetLoginResponse();` | ITokenStorageService |
| 69 | public (interface 默认) | ` void ClearAuthentication();` | ITokenStorageService |
| 21 | public (interface 默认) | ` Task<TokenValidationResult> ValidateTokenAsync(string token);` | ITokenValidator |
| 28 | public (interface 默认) | ` Task<TokenUserInfo?> ValidateAndGetUserInfoAsync(string token);` | ITokenValidator |
| 14 | public (interface 默认) | ` Task SaveUsernameAsync(string username, bool rememberMe);` | IUsernameStorageService |
| 20 | public (interface 默认) | ` Task<string?> GetSavedUsernameAsync();` | IUsernameStorageService |
| 26 | public (interface 默认) | ` Task<bool> IsRememberMeEnabledAsync();` | IUsernameStorageService |
| 31 | public (interface 默认) | ` Task ClearUsernameAsync();` | IUsernameStorageService |
| 29 | public | ` public LocalTokenValidator( IOptions<JwtOptions> jwtOptions, ILogger<LocalTokenValidator> logger)` | LocalTokenValidator |
| 44 | public | ` public Task<TokenValidationResult> ValidateTokenAsync(string token)` | LocalTokenValidator |
| 177 | public | ` public async Task<TokenUserInfo?> ValidateAndGetUserInfoAsync(string token)` | LocalTokenValidator |
| 186 | private | ` private TokenUserInfo? ExtractUserInfo(ClaimsPrincipal principal)` | LocalTokenValidator |
| 37 | public | ` public LogoutService( ILogger<LogoutService> logger, ITokenStorageService tokenStorage, IApiClientAuth authApi, IAuthenticationStateMachine stateMach…` | LogoutService |
| 55 | public | ` public async Task<LogoutResult> LogoutAsync()` | LogoutService |
| 101 | private | ` private void PublishLogoutCompletedEvent(string? username, ServerLogoutAttemptResult serverResult)` | LogoutService |
| 124 | public | ` public async Task ExecuteLocalLogoutAsync()` | LogoutService |
| 141 | public | ` public async Task<int> ProcessPendingServerLogoutsAsync()` | LogoutService |
| 213 | private | ` private async Task<ServerLogoutAttemptResult> TryServerLogoutAsync(string? username, string? refreshToken)` | LogoutService |
| 252 | private | ` private void PublishPendingLogoutsClearedEvent(int processedCount)` | LogoutService |
| 274 | private | ` private void PublishServerLogoutFailedEvent( string? username, ServerLogoutFailureReason reason, string? errorMessage, bool queuedForRetry, int retry…` | LogoutService |
| 305 | private | ` private void PublishLogoutStartedEvent(string? username)` | LogoutService |
| 323 | private | ` private async Task<ServerLogoutAttemptResult> ExecuteServerLogoutWithRetryAsync( string? username, string? refreshToken, int retryCount)` | LogoutService |
| 427 | public | ` public static ServerLogoutAttemptResult CreateSuccess() =>` | ServerLogoutAttemptResult |
| 45 | public | ` public TokenLifecycleService( IApiClientAuth authApi, ITokenStorageService tokenStorage, IEventAggregator eventAggregator, ILogger<TokenLifecycleServ…` | TokenLifecycleService |
| 92 | public | ` public void StartMonitoring(DateTime tokenExpiresAt)` | TokenLifecycleService |
| 110 | public | ` public async Task StartMonitoringFromStorageAsync()` | TokenLifecycleService |
| 134 | public | ` public void StopMonitoring()` | TokenLifecycleService |
| 147 | public | ` public void UpdateExpiration(DateTime newExpiresAt)` | TokenLifecycleService |
| 166 | public | ` public async Task<bool> TryRefreshTokenAsync()` | TokenLifecycleService |
| 205 | public | ` public void Reset()` | TokenLifecycleService |
| 219 | private | ` private void OnMonitorTick(object? state)` | TokenLifecycleService |
| 234 | private | ` private void CheckTokenState()` | TokenLifecycleService |
| 283 | private | ` private void TransitionTo(TokenLifecycleState newState)` | TokenLifecycleService |
| 301 | public | ` public void Dispose()` | TokenLifecycleService |
| 23 | public | ` public TokenManager(ILogger<TokenManager> logger)` | TokenManager |
| 66 | public | ` public void SetTokens(string accessToken, string refreshToken, DateTime expiry)` | TokenManager |
| 89 | public | ` public void ClearTokens()` | TokenManager |
| 102 | public | ` public bool IsTokenValid()` | TokenManager |
| 122 | public | ` public bool IsTokenExpiringSoon(TimeSpan threshold)` | TokenManager |
| 14 | public | ` public TokenRefreshFailedEventArgs( TokenRefreshFailureReason reason, string userMessage, string detailedMessage, bool canRetry, bool requiresReLogin…` | TokenRefreshFailedEventArgs |
| 28 | public | ` public static TokenRefreshFailedEventArgs NetworkError(string detailedMessage) =>` | TokenRefreshFailedEventArgs |
| 35 | public | ` public static TokenRefreshFailedEventArgs RefreshTokenExpired(string detailedMessage) =>` | TokenRefreshFailedEventArgs |
| 42 | public | ` public static TokenRefreshFailedEventArgs RefreshTokenRevoked(string detailedMessage) =>` | TokenRefreshFailedEventArgs |
| 49 | public | ` public static TokenRefreshFailedEventArgs RefreshTokenInvalid(string detailedMessage) =>` | TokenRefreshFailedEventArgs |
| 56 | public | ` public static TokenRefreshFailedEventArgs ServerError(string detailedMessage) =>` | TokenRefreshFailedEventArgs |
| 63 | public | ` public static TokenRefreshFailedEventArgs UserDisabled(string detailedMessage) =>` | TokenRefreshFailedEventArgs |
| 80 | private | ` private TokenRefreshResult(bool success, TokenRefreshFailureReason? failureReason, string? errorMessage)` | TokenRefreshResult |
| 87 | public | ` public static TokenRefreshResult Succeeded() => new(true, null, null);` | TokenRefreshResult |
| 89 | public | ` public static TokenRefreshResult Failed(TokenRefreshFailureReason reason, string errorMessage) =>` | TokenRefreshResult |
| 27 | public | ` public TokenStorageService(ILogger<TokenStorageService> logger)` | TokenStorageService |
| 45 | public | ` public async Task SaveAuthenticationAsync(LoginResponse loginResponse, bool rememberMe)` | TokenStorageService |
| 71 | public | ` public async Task<string?> GetTokenAsync()` | TokenStorageService |
| 80 | public | ` public async Task<string?> GetRefreshTokenAsync()` | TokenStorageService |
| 93 | public | ` public async Task<LoginResponse?> GetLoginResponseAsync()` | TokenStorageService |
| 118 | public | ` public async Task ClearAuthenticationAsync()` | TokenStorageService |
| 128 | public | ` public async Task<bool> IsTokenExpiredAsync()` | TokenStorageService |
| 157 | public | ` public string? GetToken()` | TokenStorageService |
| 165 | public | ` public LoginResponse? GetLoginResponse()` | TokenStorageService |
| 177 | public | ` public void ClearAuthentication()` | TokenStorageService |
| 18 | public | ` public UsernameStorageService(ILogger<UsernameStorageService> logger)` | UsernameStorageService |
| 38 | public | ` public async Task SaveUsernameAsync(string username, bool rememberMe)` | UsernameStorageService |
| 77 | public | ` public async Task<string?> GetSavedUsernameAsync()` | UsernameStorageService |
| 112 | public | ` public async Task<bool> IsRememberMeEnabledAsync()` | UsernameStorageService |
| 142 | public | ` public async Task ClearUsernameAsync()` | UsernameStorageService |
| 45 | public | ` public ConnectionModeService( IConnectionSettingsService connectionSettings, IApplicationStateService applicationState, ILogger<ConnectionModeService…` | ConnectionModeService |
| 84 | public | ` public async Task<bool> CheckRemoteAvailableAsync()` | ConnectionModeService |
| 97 | public | ` public async Task<bool> TestRemoteConnectionAsync(string url)` | ConnectionModeService |
| 123 | public | ` public async Task<bool> TestLocalConnectionAsync()` | ConnectionModeService |
| 140 | public | ` public void SetMode(ConnectionMode mode)` | ConnectionModeService |
| 170 | private | ` private void ApplyMode(ConnectionMode mode)` | ConnectionModeService |
| 188 | private | ` private void OnUrlChanged(object? sender, string newUrl)` | ConnectionModeService |
| 200 | public | ` public void Dispose()` | ConnectionModeService |
### LYBT.Desktop.Herbs

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 16 | public | ` public HerbEditControl()` | HerbEditControl |
| 12 | public | ` public HerbMasterDetailControl()` | HerbMasterDetailControl |
| 11 | public | ` public HerbViewControl()` | HerbViewControl |
| 22 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | HerbsModule |
| 27 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | HerbsModule |
| 172 | public | ` public static HerbDetailModel CreateNew()` | HerbDetailModel |
| 186 | public | ` public HerbDetailModel Clone()` | HerbDetailModel |
| 155 | public | ` public static HerbEditContext CreateNew()` | HerbEditContext |
| 17 | public | ` public HerbRepository( IApiClient apiClient, ILogger<HerbRepository> logger)` | HerbRepository |
| 29 | public | ` public async Task<List<HerbListDto>> SearchAsync(string keyword, CancellationToken ct = default)` | HerbRepository |
| 47 | public | ` public async Task<HerbBatchImportResultDto?> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default)` | HerbRepository |
| 74 | public | ` public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)` | HerbRepository |
| 99 | public | ` public async Task<byte[]?> ExportHerbsAsync(string? keyword = null, CancellationToken ct = default)` | HerbRepository |
| 128 | public | ` public async Task<HerbDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default)` | HerbRepository |
| 144 | public | ` public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)` | HerbRepository |
| 15 | public | ` public HerbSearchProvider(IHerbService herbService)` | HerbSearchProvider |
| 21 | public | ` public async Task<IReadOnlyList<HerbListDto>> SearchHerbsAsync(string keyword)` | HerbSearchProvider |
| 28 | public | ` public async Task<IReadOnlyList<HerbListDto>> GetAllHerbsAsync()` | HerbSearchProvider |
| 21 | public | ` public RemoteHerbService( IHerbRepository herbRepository, ILogger<RemoteHerbService> logger)` | RemoteHerbService |
| 34 | public | ` public async Task<CommandResult<HerbDetailDto>> CreateHerbAsync(HerbInputDto createDto, CancellationToken ct = default)` | RemoteHerbService |
| 54 | public | ` public async Task<CommandResult<HerbDetailDto>> UpdateHerbAsync(HerbInputDto updateDto, CancellationToken ct = default)` | RemoteHerbService |
| 74 | public | ` public async Task<CommandResult<bool>> DeleteHerbAsync(Guid herbId, CancellationToken ct = default)` | RemoteHerbService |
| 94 | public | ` public async Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> herbIds, CancellationToken ct = default)` | RemoteHerbService |
| 122 | public | ` public async Task<CommandResult<HerbDetailDto>> GetByIdAsync(Guid herbId, CancellationToken ct = default)` | RemoteHerbService |
| 144 | public | ` public async Task<CommandResult<PagedResult<HerbListDto>>> GetPagedAsync( int page, int pageSize, string? searchText = null, string? category = null,…` | RemoteHerbService |
| 165 | public | ` public async Task<CommandResult<List<HerbListDto>>> GetAllAsync(CancellationToken ct = default)` | RemoteHerbService |
| 188 | public | ` public async Task<CommandResult<List<HerbListDto>>> SearchAsync(string keyword, CancellationToken ct = default)` | RemoteHerbService |
| 212 | public | ` public async Task<CommandResult<HerbDetailDto>> ToggleStatusAsync(Guid herbId, CancellationToken ct = default)` | RemoteHerbService |
| 240 | public | ` public async Task<CommandResult<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default)` | RemoteHerbService |
| 264 | public | ` public async Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default)` | RemoteHerbService |
| 287 | public | ` public async Task<CommandResult<byte[]>> ExportHerbsAsync(string? keyword, CancellationToken ct = default)` | RemoteHerbService |
| 19 | public | ` public HerbStatusHandler( IHerbService herbService, IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServices, ILogger<HerbStatusHandl…` | HerbStatusHandler |
| 29 | protected | ` protected override Guid GetEntityId(HerbListDto e) => e.Id;` | HerbStatusHandler |
| 30 | protected | ` protected override string GetEntityDisplayName(HerbListDto e) => e.Name;` | HerbStatusHandler |
| 31 | protected | ` protected override CommonStatus GetEntityStatus(HerbListDto e) => e.Status;` | HerbStatusHandler |
| 33 | protected | ` protected override Task<object?> ExecuteRestoreAsync(Guid id)` | HerbStatusHandler |
| 36 | protected | ` protected override async Task<CommonStatus?> ExecuteToggleStatusAsync(Guid id)` | HerbStatusHandler |
| 15 | public (interface 默认) | ` Task<bool> ToggleStatusAsync(HerbListDto herb);` | IHerbStatusHandler |
| 22 | public (interface 默认) | ` Task<bool> RestoreAsync(HerbListDto herb);` | IHerbStatusHandler |
| 31 | public | ` public void InitializeFromDto(HerbDetailDto dto)` | HerbEditorViewModel |
| 59 | public | ` public void InitializeForNewCase()` | HerbEditorViewModel |
| 69 | public | ` public HerbInputDto GetHerbData()` | HerbEditorViewModel |
| 90 | public | ` public bool Validate()` | HerbEditorViewModel |
| 96 | public | ` public void Reset()` | HerbEditorViewModel |
| 103 | private | ` private void OnHerbPropertyChanged(object? sender, PropertyChangedEventArgs e)` | HerbEditorViewModel |
| 37 | protected | ` protected override string? GetDetailDisplayName() => CurrentDetail?.Name;` | HerbMasterDetailViewModel |
| 50 | public | ` public HerbMasterDetailViewModel( IViewModelServices viewModelServices, IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServices, IHe…` | HerbMasterDetailViewModel |
| 71 | protected | ` protected override async Task LoadListAsync()` | HerbMasterDetailViewModel |
| 104 | protected | ` protected override async Task LoadDetailAsync(HerbListDto item)` | HerbMasterDetailViewModel |
| 153 | protected | ` protected override HerbDetailModel CreateNewDetail()` | HerbMasterDetailViewModel |
| 162 | protected | ` protected override async Task<bool> SaveDetailAsync(HerbDetailModel detail)` | HerbMasterDetailViewModel |
| 202 | protected | ` protected override async Task<bool> DeleteItemAsync(HerbListDto item)` | HerbMasterDetailViewModel |
| 221 | private | ` private async Task ToggleStatusAsync()` | HerbMasterDetailViewModel |
| 231 | private | ` private bool CanToggleStatus() => HasSelection && !IsBusy;` | HerbMasterDetailViewModel |
| 235 | private | ` private void CopyHerb()` | HerbMasterDetailViewModel |
| 250 | private | ` private bool CanCopyHerb() => HasSelection && !IsBusy && IsAdmin;` | HerbMasterDetailViewModel |
| 253 | protected | ` protected override async Task InvalidateCachesAsync()` | HerbMasterDetailViewModel |
| 260 | protected | ` protected override async Task RestoreItemAsync(HerbListDto item)` | HerbMasterDetailViewModel |
| 267 | private | ` private async Task SearchByCategoryAsync(string? category)` | HerbMasterDetailViewModel |
### LYBT.Desktop.Infrastructure

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 32 | public | ` public static bool GetShowCheckBoxColumn(DependencyObject obj) =>` | DataGridSelectionBehavior |
| 35 | public | ` public static void SetShowCheckBoxColumn(DependencyObject obj, bool value) =>` | DataGridSelectionBehavior |
| 49 | public | ` public static IList? GetSelectedItems(DependencyObject obj) =>` | DataGridSelectionBehavior |
| 52 | public | ` public static void SetSelectedItems(DependencyObject obj, IList? value) =>` | DataGridSelectionBehavior |
| 72 | private | ` private static void OnShowCheckBoxColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | DataGridSelectionBehavior |
| 93 | private | ` private static void DataGrid_Loaded(object sender, RoutedEventArgs e)` | DataGridSelectionBehavior |
| 99 | private | ` private static void AddCheckBoxColumn(DataGrid dataGrid)` | DataGridSelectionBehavior |
| 199 | private | ` private static void RemoveCheckBoxColumn(DataGrid dataGrid)` | DataGridSelectionBehavior |
| 210 | private | ` private static void OnSelectAllCheckBoxClick(DataGrid dataGrid, CheckBox selectAllCheckBox)` | DataGridSelectionBehavior |
| 245 | private | ` private static void OnRowCheckBoxClick(DataGrid dataGrid, object sender, MouseButtonEventArgs e)` | DataGridSelectionBehavior |
| 281 | private | ` private static void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)` | DataGridSelectionBehavior |
| 289 | private | ` private static void SyncSelectedItems(DataGrid dataGrid)` | DataGridSelectionBehavior |
| 309 | private | ` private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | DataGridSelectionBehavior |
| 323 | private | ` private static void UpdateSelectAllCheckBoxState(DataGrid dataGrid)` | DataGridSelectionBehavior |
| 347 | private | ` private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject` | DataGridSelectionBehavior |
| 18 | public | ` public static string GetBoundPassword(DependencyObject obj) =>` | PasswordBoxHelper |
| 21 | public | ` public static void SetBoundPassword(DependencyObject obj, string value) =>` | PasswordBoxHelper |
| 24 | private | ` private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | PasswordBoxHelper |
| 38 | private | ` private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)` | PasswordBoxHelper |
| 36 | public (interface 默认) | ` Task<bool> ConnectAsync(string? connectionString = null);` | ICardReader |
| 41 | public (interface 默认) | ` Task DisconnectAsync();` | ICardReader |
| 50 | public (interface 默认) | ` Task<CardReadResult> ReadCardAsync( bool savePhoto = false, string? photoPath = null, CancellationToken cancellationToken = default);` | ICardReader |
| 59 | public (interface 默认) | ` Task<bool> DetectCardAsync();` | ICardReader |
| 14 | public (interface 默认) | ` IReadOnlyList<CardReaderInfo> GetSupportedReaders();` | ICardReaderFactory |
| 22 | public (interface 默认) | ` ICardReader CreateReader(CardReaderType readerType, CardReaderOptions? options = null);` | ICardReaderFactory |
| 29 | public (interface 默认) | ` Task<ICardReader?> AutoDetectReaderAsync(CardReaderOptions? options = null);` | ICardReaderFactory |
| 46 | public | ` public HuaDaHD100CardReader(CardReaderOptions? options = null, ILogger<HuaDaHD100CardReader>? logger = null)` | HuaDaHD100CardReader |
| 55 | public | ` public Task<bool> ConnectAsync(string? connectionString = null)` | HuaDaHD100CardReader |
| 106 | public | ` public Task DisconnectAsync()` | HuaDaHD100CardReader |
| 132 | public | ` public Task<CardReadResult> ReadCardAsync( bool savePhoto = false, string? photoPath = null, CancellationToken cancellationToken = default)` | HuaDaHD100CardReader |
| 234 | public | ` public Task<bool> DetectCardAsync()` | HuaDaHD100CardReader |
| 263 | public | ` public void Dispose()` | HuaDaHD100CardReader |
| 274 | private | ` private void OnConnectionStateChanged(bool isConnected, string? errorMessage)` | HuaDaHD100CardReader |
| 286 | private | ` private static string MaskIdNumber(string idNumber)` | HuaDaHD100CardReader |
| 38 | public | ` public MockCardReader(CardReaderOptions? options = null)` | MockCardReader |
| 46 | public | ` public Task<bool> ConnectAsync(string? connectionString = null)` | MockCardReader |
| 61 | public | ` public Task DisconnectAsync()` | MockCardReader |
| 71 | public | ` public Task<CardReadResult> ReadCardAsync( bool savePhoto = false, string? photoPath = null, CancellationToken cancellationToken = default)` | MockCardReader |
| 108 | public | ` public Task<bool> DetectCardAsync()` | MockCardReader |
| 121 | public | ` public void Dispose()` | MockCardReader |
| 18 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | CardReaderModule |
| 26 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | CardReaderModule |
| 45 | public | ` public static IServiceCollection AddCardReaderServices(this IServiceCollection services)` | CardReaderServiceCollectionExtensions |
| 17 | public (interface 默认) | ` Task<PatientFromCardResult?> FindPatientByIdNumberAsync(string idNumber);` | IPatientCardReaderIntegration |
| 24 | public (interface 默认) | ` Task<Guid> QuickCreatePatientAsync(CardReadResult cardResult);` | IPatientCardReaderIntegration |
| 32 | public (interface 默认) | ` Task<PatientFromCardResult> FindOrCreatePatientAsync(CardReadResult cardResult);` | IPatientCardReaderIntegration |
| 39 | public (interface 默认) | ` Task<PatientDetailDto?> GetPatientDetailByIdAsync(Guid patientId);` | IPatientCardReaderIntegration |
| 47 | public (interface 默认) | ` Task<PatientMatchResult> MatchPatientAsync(CardReadResult cardResult);` | IPatientCardReaderIntegration |
| 78 | public | ` public static CardReadResult Success( string name, string idNumber, string sex, string nation, string birth, string address, string department, strin…` | CardReadResult |
| 108 | public | ` public static CardReadResult Failure(int errorCode, string? errorMessage = null)` | CardReadResult |
| 122 | private | ` private static Gender ParseGender(string sex)` | CardReadResult |
| 135 | private | ` private static DateTime? ParseDate(string dateStr)` | CardReadResult |
| 154 | private | ` private static DateTime? ParseExpireDate(string expireStr)` | CardReadResult |
| 166 | private | ` private static string GetErrorMessage(int errorCode)` | CardReadResult |
| 22 | public | ` public static extern int HD_InitComm(int port);` | HuaDaNativeMethods |
| 29 | public | ` public static extern int HD_CloseComm();` | HuaDaNativeMethods |
| 37 | public | ` public static extern int HD_Authenticate(int type);` | HuaDaNativeMethods |
| 48 | public | ` public static extern long HD_ReadCard();` | HuaDaNativeMethods |
| 65 | public | ` public static extern int HD_Read_BaseMsg( StringBuilder bmpData, StringBuilder name, StringBuilder sex, StringBuilder nation, StringBuilder birth, St…` | HuaDaNativeMethods |
| 83 | public | ` public static extern IntPtr GetName();` | HuaDaNativeMethods |
| 87 | public | ` public static extern IntPtr GetCertNo();` | HuaDaNativeMethods |
| 91 | public | ` public static extern IntPtr GetSex();` | HuaDaNativeMethods |
| 95 | public | ` public static extern IntPtr GetNation();` | HuaDaNativeMethods |
| 99 | public | ` public static extern IntPtr GetBirth();` | HuaDaNativeMethods |
| 103 | public | ` public static extern IntPtr GetAddress();` | HuaDaNativeMethods |
| 107 | public | ` public static extern IntPtr GetDepartemt();` | HuaDaNativeMethods |
| 111 | public | ` public static extern IntPtr GetEffectDate();` | HuaDaNativeMethods |
| 115 | public | ` public static extern IntPtr GetExpireDate();` | HuaDaNativeMethods |
| 119 | public | ` public static extern int GetCardType();` | HuaDaNativeMethods |
| 129 | public | ` public static extern IntPtr GetBmpFileData();` | HuaDaNativeMethods |
| 137 | public | ` public static extern int GetBmpFile(string bmpFilePath);` | HuaDaNativeMethods |
| 146 | public | ` public static string PtrToString(IntPtr ptr)` | HuaDaNativeMethods |
| 157 | public | ` public static bool IsDllAvailable()` | HuaDaNativeMethods |
| 47 | public | ` public CardReaderFactory(ILoggerFactory? loggerFactory = null)` | CardReaderFactory |
| 55 | public | ` public IReadOnlyList<CardReaderInfo> GetSupportedReaders()` | CardReaderFactory |
| 65 | public | ` public ICardReader CreateReader(CardReaderType readerType, CardReaderOptions? options = null)` | CardReaderFactory |
| 86 | public | ` public async Task<ICardReader?> AutoDetectReaderAsync(CardReaderOptions? options = null)` | CardReaderFactory |
| 132 | private | ` private static bool CheckDllAvailability(IReadOnlyList<string> requiredDlls)` | CardReaderFactory |
| 50 | public | ` public CardReaderService(ICardReaderFactory factory, CardReaderOptions? options = null, ILogger<CardReaderService>? logger = null)` | CardReaderService |
| 62 | public | ` public async Task<bool> InitializeAsync(CardReaderType readerType = CardReaderType.Auto)` | CardReaderService |
| 126 | public | ` public async Task DisconnectAsync()` | CardReaderService |
| 165 | public | ` public async Task<CardReadResult> ReadCardAsync(bool savePhoto = false, CancellationToken cancellationToken = default)` | CardReaderService |
| 212 | public | ` public void StartAutoRead(int intervalMs = 500)` | CardReaderService |
| 248 | public | ` public void StopAutoRead()` | CardReaderService |
| 269 | private | ` private async void AutoReadCallback(object? state)` | CardReaderService |
| 315 | private | ` private void OnReaderConnectionStateChanged(object? sender, CardReaderConnectionEventArgs e)` | CardReaderService |
| 328 | private | ` private void OnConnectionStateChanged(bool isConnected, string? errorMessage)` | CardReaderService |
| 340 | private | ` private void OnCardReadError(int errorCode, string message, Exception? exception = null)` | CardReaderService |
| 353 | public | ` public void Dispose()` | CardReaderService |
| 32 | public (interface 默认) | ` Task<bool> InitializeAsync(CardReaderType readerType = CardReaderType.Auto);` | ICardReaderService |
| 37 | public (interface 默认) | ` Task DisconnectAsync();` | ICardReaderService |
| 45 | public (interface 默认) | ` Task<CardReadResult> ReadCardAsync(bool savePhoto = false, CancellationToken cancellationToken = default);` | ICardReaderService |
| 51 | public (interface 默认) | ` void StartAutoRead(int intervalMs = 500);` | ICardReaderService |
| 56 | public (interface 默认) | ` void StopAutoRead();` | ICardReaderService |
| 36 | public | ` public ApplicationCommands()` | ApplicationCommands |
| 23 | public | ` public static IContainerRegistry AddViewModelServices(this IContainerRegistry containerRegistry)` | ViewModelServicesExtensions |
| 63 | public | ` public static IContainerRegistry AddListViewServices<T>(this IContainerRegistry containerRegistry) where T : class` | ViewModelServicesExtensions |
| 77 | public | ` public static IContainerRegistry AddMasterDetailServices<TListItem, TDetail>(this IContainerRegistry containerRegistry)` | ViewModelServicesExtensions |
| 27 | public | ` public EventSubscriptionManager(IEventAggregator eventAggregator)` | EventSubscriptionManager |
| 38 | public | ` public void Subscribe<TEvent, TPayload>(Action<TPayload> handler)` | EventSubscriptionManager |
| 58 | public | ` public void Subscribe<TEvent, TPayload>( Action<TPayload> handler, ThreadOption threadOption, bool keepSubscriberReferenceAlive = false, Predicate<TP…` | EventSubscriptionManager |
| 80 | public | ` public void Subscribe<TEvent, TPayload>( Action<TPayload> handler, Predicate<TPayload> filter)` | EventSubscriptionManager |
| 98 | public | ` public void Subscribe<TEvent>(Action handler)` | EventSubscriptionManager |
| 115 | public | ` public void Publish<TEvent, TPayload>(TPayload payload)` | EventSubscriptionManager |
| 126 | public | ` public void Publish<TEvent>()` | EventSubscriptionManager |
| 141 | public | ` public void ClearSubscriptions()` | EventSubscriptionManager |
| 150 | private | ` private void ThrowIfDisposed()` | EventSubscriptionManager |
| 158 | public | ` public void Dispose()` | EventSubscriptionManager |
| 17 | public | ` public DesktopExceptionHandler(ILogger<DesktopExceptionHandler> logger)` | DesktopExceptionHandler |
| 23 | public | ` public void HandleException(Exception exception, string? context = null)` | DesktopExceptionHandler |
| 29 | public | ` public Task HandleExceptionAsync(Exception exception, string? context = null)` | DesktopExceptionHandler |
| 36 | public | ` public void LogException(Exception exception, ExceptionSeverity severity = ExceptionSeverity.Error)` | DesktopExceptionHandler |
| 51 | public | ` public string GetUserFriendlyMessage(Exception exception)` | DesktopExceptionHandler |
| 57 | public | ` public bool CanRetry(Exception exception)` | DesktopExceptionHandler |
| 74 | public | ` public void RegisterGlobalExceptionHandlers()` | DesktopExceptionHandler |
| 95 | public | ` public void UnregisterGlobalExceptionHandlers()` | DesktopExceptionHandler |
| 113 | private | ` private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)` | DesktopExceptionHandler |
| 134 | private | ` private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)` | DesktopExceptionHandler |
| 155 | private | ` private void LogExceptionInternal(Exception exception, string methodName, string? context)` | DesktopExceptionHandler |
| 174 | private | ` private static LogLevel DetermineLogLevel(Exception exception)` | DesktopExceptionHandler |
| 192 | public | ` public Result<T> HandleException<T>(Exception exception, string methodName, string? context = null)` | DesktopExceptionHandler |
| 204 | public | ` public Result HandleExceptionWithResult(Exception exception, string methodName, string? context = null)` | DesktopExceptionHandler |
| 216 | public | ` public async Task<Result<T>> SafeExecuteAsync<T>(Func<Task<Result<T>>> operation, string methodName, string? context = null)` | DesktopExceptionHandler |
| 229 | public | ` public async Task<Result> SafeExecuteAsync(Func<Task<Result>> operation, string methodName, string? context = null)` | DesktopExceptionHandler |
| 14 | public (interface 默认) | ` void HandleException(Exception exception, string? context = null);` | IDesktopExceptionHandler |
| 19 | public (interface 默认) | ` Task HandleExceptionAsync(Exception exception, string? context = null);` | IDesktopExceptionHandler |
| 24 | public (interface 默认) | ` void LogException(Exception exception, ExceptionSeverity severity = ExceptionSeverity.Error);` | IDesktopExceptionHandler |
| 29 | public (interface 默认) | ` string GetUserFriendlyMessage(Exception exception);` | IDesktopExceptionHandler |
| 34 | public (interface 默认) | ` bool CanRetry(Exception exception);` | IDesktopExceptionHandler |
| 42 | public (interface 默认) | ` void RegisterGlobalExceptionHandlers();` | IDesktopExceptionHandler |
| 47 | public (interface 默认) | ` void UnregisterGlobalExceptionHandlers();` | IDesktopExceptionHandler |
| 56 | public (interface 默认) | ` Result<T> HandleException<T>(Exception exception, string methodName, string? context = null);` | IDesktopExceptionHandler |
| 61 | public (interface 默认) | ` Result HandleExceptionWithResult(Exception exception, string methodName, string? context = null);` | IDesktopExceptionHandler |
| 66 | public (interface 默认) | ` Task<Result<T>> SafeExecuteAsync<T>(Func<Task<Result<T>>> operation, string methodName, string? context = null);` | IDesktopExceptionHandler |
| 71 | public (interface 默认) | ` Task<Result> SafeExecuteAsync(Func<Task<Result>> operation, string methodName, string? context = null);` | IDesktopExceptionHandler |
| 18 | public | ` public static void SafeFireAndForget( this Task task, Action<Exception>? onException = null, bool continueOnCapturedContext = false)` | TaskExtensions |
| 33 | public | ` public static void SafeFireAndForget<T>( this Task<T> task, Action<Exception>? onException = null, bool continueOnCapturedContext = false)` | TaskExtensions |
| 41 | private | ` private static async Task SafeFireAndForgetInternal( Task task, Action<Exception>? onException, bool continueOnCapturedContext)` | TaskExtensions |
| 12 | public | ` public static string MaskIdNumber(string? idNumber)` | PrivacyHelper |
| 14 | public (interface 默认) | ` ClinicSettingsOptions GetSettings();` | IClinicSettingsService |
| 51 | public (interface 默认) | ` Task<bool> SaveSettingsAsync(ClinicSettingsOptions settings);` | IClinicSettingsService |
| 46 | public | ` public static LoggerConfiguration CreateLoggerConfiguration(LogEventLevel minimumLevel = LogEventLevel.Information)` | DesktopSerilogConfiguration |
| 76 | public | ` public static void Initialize(LogEventLevel minimumLevel = LogEventLevel.Information)` | DesktopSerilogConfiguration |
| 85 | public | ` public static void CloseAndFlush()` | DesktopSerilogConfiguration |
| 93 | private | ` private static void EnsureLogDirectoryExists()` | DesktopSerilogConfiguration |
| 11 | public (interface 默认) | ` Task EnsureModuleLoadedAsync(string viewName);` | IModuleLazyLoader |
| 16 | public (interface 默认) | ` Task PreloadModulesAsync(UserRole role);` | IModuleLazyLoader |
| 17 | public (interface 默认) | ` void RecordNavigation(string? fromView, string toView);` | INavigationHistoryService |
| 20 | public (interface 默认) | ` void ClearHistory();` | INavigationHistoryService |
| 9 | public (interface 默认) | ` void StartMonitoring();` | IRegionMonitor |
| 12 | public (interface 默认) | ` void StopMonitoring();` | IRegionMonitor |
| 38 | public | ` public ModuleLazyLoader( IModuleLoadingService? moduleLoadingService, ILogger<ModuleLazyLoader> logger)` | ModuleLazyLoader |
| 49 | public | ` public async Task EnsureModuleLoadedAsync(string viewName)` | ModuleLazyLoader |
| 66 | public | ` public async Task PreloadModulesAsync(UserRole role)` | ModuleLazyLoader |
| 27 | public | ` public NavigationCoordinator( INavigationServices services, ILogger<NavigationCoordinator> logger)` | NavigationCoordinator |
| 90 | public | ` public async Task NavigateTo(string viewName, IDictionary<string, object>? parameters = null)` | NavigationCoordinator |
| 158 | public | ` public async Task NavigateTo<TParams>(string viewName, TParams parameters) where TParams : class` | NavigationCoordinator |
| 172 | public | ` public async Task NavigateToHome()` | NavigationCoordinator |
| 184 | public | ` public async Task NavigateToHome(UserRole role)` | NavigationCoordinator |
| 192 | public | ` public void NavigateBack()` | NavigationCoordinator |
| 215 | public | ` public void NavigateForward()` | NavigationCoordinator |
| 238 | public | ` public void ClearHistory() => _services.HistoryService.ClearHistory();` | NavigationCoordinator |
| 245 | public | ` public void ShowLoginDialog()` | NavigationCoordinator |
| 255 | public | ` public void ClearLoginRegion()` | NavigationCoordinator |
| 265 | public | ` public void ClearContentRegion()` | NavigationCoordinator |
| 278 | private | ` private static NavigationParameters? ConvertToNavigationParameters(IDictionary<string, object>? parameters)` | NavigationCoordinator |
| 294 | public | ` public void SubscribeToRegionCollection() => _services.RegionMonitor.StartMonitoring();` | NavigationCoordinator |
| 297 | public | ` public void UnsubscribeFromRegionCollection() => _services.RegionMonitor.StopMonitoring();` | NavigationCoordinator |
| 20 | public | ` public NavigationHistoryService(ILogger<NavigationHistoryService> logger)` | NavigationHistoryService |
| 25 | public | ` public void RecordNavigation(string? fromView, string toView)` | NavigationHistoryService |
| 34 | public | ` public void ClearHistory()` | NavigationHistoryService |
| 41 | private | ` private void UpdateBreadcrumbs(string? fromView, string toView)` | NavigationHistoryService |
| 8 | public | ` public NavigationServices( Prism.Regions.IRegionManager regionManager, ISessionManager sessionManager, IRoleRegistry roleRegistry, INavigationHistory…` | NavigationServices |
| 15 | public | ` public RegionMonitor(IRegionManager regionManager, ILogger<RegionMonitor> logger)` | RegionMonitor |
| 21 | public | ` public void StartMonitoring()` | RegionMonitor |
| 29 | public | ` public void StopMonitoring()` | RegionMonitor |
| 42 | private | ` private void OnRegionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)` | RegionMonitor |
| 51 | private | ` private void SubscribeToRegionNavigationEvents(IRegion region)` | RegionMonitor |
| 61 | public | ` public void Dispose()` | RegionMonitor |
| 26 | public | ` public PerformanceMonitor(ILogger<PerformanceMonitor>? logger = null)` | PerformanceMonitor |
| 32 | public | ` public void StartTiming(string operationName)` | PerformanceMonitor |
| 59 | public | ` public long StopTiming(string operationName)` | PerformanceMonitor |
| 108 | public | ` public long RecordMemoryBaseline(string label)` | PerformanceMonitor |
| 127 | public | ` public IReadOnlyDictionary<string, long> GetMemorySnapshots()` | PerformanceMonitor |
| 136 | public | ` public PerformanceMetric? GetMetric(string operationName)` | PerformanceMonitor |
| 145 | public | ` public IReadOnlyCollection<PerformanceMetric> GetAllMetrics()` | PerformanceMonitor |
| 154 | public | ` public PerformanceReport GenerateReport()` | PerformanceMonitor |
| 167 | public | ` public void Clear()` | PerformanceMonitor |
| 182 | private | ` private void LogPerformanceMetric(PerformanceMetric metric)` | PerformanceMonitor |
| 40 | public | ` public IEnumerable<string> GetAllModules()` | RoleDefinitionBase |
| 23 | public | ` public RoleRegistry(ILogger<RoleRegistry> logger)` | RoleRegistry |
| 29 | public | ` public void Register(IRoleDefinition roleDefinition)` | RoleRegistry |
| 50 | public | ` public IRoleDefinition? GetDefinition(UserRole role)` | RoleRegistry |
| 57 | public | ` public IReadOnlyCollection<IRoleDefinition> GetAllDefinitions()` | RoleRegistry |
| 63 | public | ` public bool IsRegistered(UserRole role)` | RoleRegistry |
| 69 | public | ` public string GetHomeViewName(UserRole role)` | RoleRegistry |
| 81 | public | ` public IEnumerable<string> GetModulesForRole(UserRole role)` | RoleRegistry |
| 18 | public | ` public ActiveConsultationService(ILogger<ActiveConsultationService> logger)` | ActiveConsultationService |
| 48 | public | ` public void Register(Guid medicalCaseId, Func<Task<LeaveConsultationResult>> leaveHandler)` | ActiveConsultationService |
| 60 | public | ` public void Unregister()` | ActiveConsultationService |
| 77 | public | ` public async Task<LeaveConsultationResult> RequestLeaveAsync()` | ActiveConsultationService |
| 48 | public | ` public ApplicationTickService(ILogger<ApplicationTickService> logger)` | ApplicationTickService |
| 62 | public | ` public void Start()` | ApplicationTickService |
| 85 | public | ` public void Stop()` | ApplicationTickService |
| 100 | private | ` private void OnTimerTick(object? sender, EventArgs e)` | ApplicationTickService |
| 126 | public | ` public void Dispose()` | ApplicationTickService |
| 15 | public | ` public AsyncExecutor(IUiThreadDispatcher dispatcher, ILogger<AsyncExecutor>? logger = null)` | AsyncExecutor |
| 22 | public | ` public async Task<bool> ExecuteSafelyAsync(Func<Task> action, Action<Exception>? onError = null)` | AsyncExecutor |
| 38 | public | ` public async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action, T? defaultValue = default, Action<Exception>? onError = null)` | AsyncExecutor |
| 53 | public | ` public async Task<bool> ExecuteWithRetryAsync( Func<Task> action, int maxRetries = 3, int retryDelay = 1000, Func<Exception, bool>? shouldRetry = nul…` | AsyncExecutor |
| 84 | public | ` public async Task<T?> ExecuteWithRetryAsync<T>( Func<Task<T>> action, int maxRetries = 3, int retryDelay = 1000, Func<Exception, bool>? shouldRetry =…` | AsyncExecutor |
| 114 | public | ` public void ExecuteOnUIThread(Action action)` | AsyncExecutor |
| 127 | public | ` public Task ExecuteOnUIThreadAsync(Action action)` | AsyncExecutor |
| 139 | public | ` public async Task<bool> ExecuteWithTimeoutAsync( Func<CancellationToken, Task> action, TimeSpan timeout, CancellationToken cancellationToken = defaul…` | AsyncExecutor |
| 33 | public | ` public ClinicSettingsService( IOptions<ClinicSettingsOptions> clinicOptions, ILogger<ClinicSettingsService> logger)` | ClinicSettingsService |
| 44 | public | ` public ClinicSettingsOptions GetSettings()` | ClinicSettingsService |
| 52 | public | ` public async Task<bool> SaveSettingsAsync(ClinicSettingsOptions settings)` | ClinicSettingsService |
| 15 | public | ` public CommonDialogService(IDialogService dialogService)` | CommonDialogService |
| 23 | public | ` public Task ShowInfoAsync(string message, string? title = null)` | CommonDialogService |
| 32 | public | ` public Task ShowWarningAsync(string message, string? title = null)` | CommonDialogService |
| 41 | public | ` public Task ShowErrorAsync(string message, string? title = null)` | CommonDialogService |
| 50 | public | ` public Task<bool> ShowConfirmAsync(string message, string? title = null)` | CommonDialogService |
| 60 | public | ` public Task<TripleChoiceResult> ShowTripleChoiceAsync(string message, string? title = null)` | CommonDialogService |
| 78 | public | ` public Task<string?> ShowInputAsync(string message, string? title = null, string? defaultValue = null)` | CommonDialogService |
| 87 | public | ` public Task<string?> ShowOpenFileDialogAsync(string? filter = null, string? title = null)` | CommonDialogService |
| 102 | public | ` public Task<string?> ShowSaveFileDialogAsync(string? filter = null, string? title = null, string? defaultFileName = null)` | CommonDialogService |
| 31 | public | ` public ConnectionSettingsService( IOptions<ApiClientOptions> apiOptions, ILogger<ConnectionSettingsService> logger)` | ConnectionSettingsService |
| 77 | public | ` public async Task SetUrlAsync(string url)` | ConnectionSettingsService |
| 112 | public | ` public async Task SaveRemoteUrlAsync(string url)` | ConnectionSettingsService |
| 140 | public | ` public async Task SavePreferredModeAsync(string mode)` | ConnectionSettingsService |
| 164 | public | ` public bool IsValidUrl(string url)` | ConnectionSettingsService |
| 173 | private | ` private async Task PersistRemoteUrlAsync(string url)` | ConnectionSettingsService |
| 178 | private | ` private async Task PersistPreferredModeAsync(string mode)` | ConnectionSettingsService |
| 187 | private | ` private async Task PersistSettingAsync(string section, string key, string value)` | ConnectionSettingsService |
| 216 | private | ` private static bool IsLocalUrl(string url)` | ConnectionSettingsService |
| 39 | public | ` public void EnterEditMode()` | DetailEditorService |
| 48 | public | ` public void CancelEdit()` | DetailEditorService |
| 62 | public | ` public void ConfirmSaved()` | DetailEditorService |
| 78 | public | ` public void CreateNew(Func<TDetail> factory)` | DetailEditorService |
| 90 | public | ` public void LoadDetail(TDetail detail, Func<TDetail, TDetail>? clone = null)` | DetailEditorService |
| 102 | public | ` public void MarkAsChanged()` | DetailEditorService |
| 108 | public | ` public void Clear()` | DetailEditorService |
| 19 | public | ` public DialogManager(IDialogService dialogService)` | DialogManager |
| 25 | public | ` public Task ShowSuccessAsync(string message, string? title = null)` | DialogManager |
| 31 | public | ` public Task ShowErrorAsync(string message, string? title = null)` | DialogManager |
| 37 | public | ` public Task ShowWarningAsync(string message, string? title = null)` | DialogManager |
| 43 | public | ` public Task ShowInfoAsync(string message, string? title = null)` | DialogManager |
| 54 | private | ` private Task ShowMessageDialogAsync(string type, string message, string title)` | DialogManager |
| 74 | public | ` public Task<bool> ShowConfirmAsync(string message, string? title = null)` | DialogManager |
| 93 | public | ` public Task<string?> ShowInputAsync(string message, string? title = null, string? defaultValue = null)` | DialogManager |
| 120 | public | ` public Task<TResult?> ShowDialogAsync<TResult>(string dialogName, IDictionary<string, object>? parameters = null)` | DialogManager |
| 22 | public | ` public ErrorHandler(ILogger<ErrorHandler>? logger = null)` | ErrorHandler |
| 43 | public | ` public IEnumerable GetErrors(string? propertyName)` | ErrorHandler |
| 56 | public | ` public void HandleException(Exception exception, string? context = null)` | ErrorHandler |
| 69 | public | ` public void SetError(string propertyName, string error)` | ErrorHandler |
| 75 | public | ` public void SetErrors(string propertyName, IEnumerable<string> errors)` | ErrorHandler |
| 92 | public | ` public void ClearError(string propertyName)` | ErrorHandler |
| 103 | public | ` public void ClearAllErrors()` | ErrorHandler |
| 119 | public | ` public bool ValidateProperty(object? value, string propertyName)` | ErrorHandler |
| 139 | public | ` public bool ValidateAll(object target)` | ErrorHandler |
| 16 | public (interface 默认) | ` Task<bool> ExecuteSafelyAsync(Func<Task> action, Action<Exception>? onError = null);` | IAsyncExecutor |
| 26 | public (interface 默认) | ` Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action, T? defaultValue = default, Action<Exception>? onError = null);` | IAsyncExecutor |
| 36 | public (interface 默认) | ` Task<bool> ExecuteWithRetryAsync( Func<Task> action, int maxRetries = 3, int retryDelay = 1000, Func<Exception, bool>? shouldRetry = null);` | IAsyncExecutor |
| 51 | public (interface 默认) | ` Task<T?> ExecuteWithRetryAsync<T>( Func<Task<T>> action, int maxRetries = 3, int retryDelay = 1000, Func<Exception, bool>? shouldRetry = null);` | IAsyncExecutor |
| 61 | public (interface 默认) | ` void ExecuteOnUIThread(Action action);` | IAsyncExecutor |
| 67 | public (interface 默认) | ` Task ExecuteOnUIThreadAsync(Action action);` | IAsyncExecutor |
| 76 | public (interface 默认) | ` Task<bool> ExecuteWithTimeoutAsync( Func<CancellationToken, Task> action, TimeSpan timeout, CancellationToken cancellationToken = default);` | IAsyncExecutor |
| 39 | public (interface 默认) | ` void EnterEditMode();` | IDetailEditorService |
| 44 | public (interface 默认) | ` void CancelEdit();` | IDetailEditorService |
| 49 | public (interface 默认) | ` void ConfirmSaved();` | IDetailEditorService |
| 55 | public (interface 默认) | ` void CreateNew(Func<TDetail> factory);` | IDetailEditorService |
| 62 | public (interface 默认) | ` void LoadDetail(TDetail detail, Func<TDetail, TDetail>? clone = null);` | IDetailEditorService |
| 67 | public (interface 默认) | ` void MarkAsChanged();` | IDetailEditorService |
| 72 | public (interface 默认) | ` void Clear();` | IDetailEditorService |
| 86 | public | ` public EditModeChangedEventArgs(bool isEditMode, bool isNew)` | EditModeChangedEventArgs |
| 15 | public (interface 默认) | ` Task ShowSuccessAsync(string message, string? title = null);` | IDialogManager |
| 22 | public (interface 默认) | ` Task ShowErrorAsync(string message, string? title = null);` | IDialogManager |
| 29 | public (interface 默认) | ` Task ShowWarningAsync(string message, string? title = null);` | IDialogManager |
| 36 | public (interface 默认) | ` Task ShowInfoAsync(string message, string? title = null);` | IDialogManager |
| 44 | public (interface 默认) | ` Task<bool> ShowConfirmAsync(string message, string? title = null);` | IDialogManager |
| 53 | public (interface 默认) | ` Task<string?> ShowInputAsync(string message, string? title = null, string? defaultValue = null);` | IDialogManager |
| 62 | public (interface 默认) | ` Task<TResult?> ShowDialogAsync<TResult>(string dialogName, IDictionary<string, object>? parameters = null);` | IDialogManager |
| 31 | public (interface 默认) | ` void HandleException(Exception exception, string? context = null);` | IErrorHandler |
| 38 | public (interface 默认) | ` void SetError(string propertyName, string error);` | IErrorHandler |
| 45 | public (interface 默认) | ` void SetErrors(string propertyName, IEnumerable<string> errors);` | IErrorHandler |
| 51 | public (interface 默认) | ` void ClearError(string propertyName);` | IErrorHandler |
| 56 | public (interface 默认) | ` void ClearAllErrors();` | IErrorHandler |
| 64 | public (interface 默认) | ` bool ValidateProperty(object? value, string propertyName);` | IErrorHandler |
| 71 | public (interface 默认) | ` bool ValidateAll(object target);` | IErrorHandler |
| 85 | public | ` public ErrorChangedEventArgs(string? propertyName, IReadOnlyList<string> errors)` | ErrorChangedEventArgs |
| 32 | public (interface 默认) | ` void Dispose();` | IListViewServices |
| 37 | public (interface 默认) | ` void ResetAll();` | IListViewServices |
| 30 | public (interface 默认) | ` Task ExecuteWithLoadingAsync(Func<Task> action, string? message = null, bool isBusy = false);` | ILoadingStateManager |
| 40 | public (interface 默认) | ` Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action, string? message = null, bool isBusy = false);` | ILoadingStateManager |
| 44 | public (interface 默认) | ` void BeginLoading(string? message = null);` | ILoadingStateManager |
| 47 | public (interface 默认) | ` void EndLoading();` | ILoadingStateManager |
| 50 | public (interface 默认) | ` void Reset();` | ILoadingStateManager |
| 51 | public (interface 默认) | ` void Dispose();` | IMasterDetailServices |
| 56 | public (interface 默认) | ` void ResetAll();` | IMasterDetailServices |
| 45 | public (interface 默认) | ` void GoToFirstPage();` | IPaginationService |
| 48 | public (interface 默认) | ` void GoToPreviousPage();` | IPaginationService |
| 51 | public (interface 默认) | ` void GoToNextPage();` | IPaginationService |
| 54 | public (interface 默认) | ` void GoToLastPage();` | IPaginationService |
| 58 | public (interface 默认) | ` void GoToPage(int page);` | IPaginationService |
| 61 | public (interface 默认) | ` void Reset();` | IPaginationService |
| 78 | public | ` public PageChangedEventArgs(int oldPage, int newPage, int pageSize)` | PageChangedEventArgs |
| 30 | public (interface 默认) | ` Task ExecuteSearchAsync(Func<string, Task> searchAction);` | ISearchService |
| 36 | public (interface 默认) | ` Task ExecuteSearchImmediateAsync(Func<string, Task> searchAction);` | ISearchService |
| 39 | public (interface 默认) | ` void ClearSearch();` | ISearchService |
| 42 | public (interface 默认) | ` void CancelSearch();` | ISearchService |
| 53 | public | ` public SearchRequestedEventArgs(string searchText)` | SearchRequestedEventArgs |
| 38 | public (interface 默认) | ` void Select(T? item);` | ISelectionService |
| 44 | public (interface 默认) | ` void SelectMultiple(IEnumerable<T> items);` | ISelectionService |
| 50 | public (interface 默认) | ` void ToggleSelection(T item);` | ISelectionService |
| 53 | public (interface 默认) | ` void ClearSelection();` | ISelectionService |
| 71 | public | ` public SelectionChangedEventArgs(T? newSelection, T? oldSelection, IReadOnlyList<T> allSelectedItems)` | SelectionChangedEventArgs |
| 30 | public | ` public ListViewServices( ILoadingStateManager loading, IPaginationService pagination, ISearchService search, ISelectionService<T> selection, IErrorHa…` | ListViewServices |
| 47 | public | ` public void ResetAll()` | ListViewServices |
| 57 | public | ` public void Dispose()` | ListViewServices |
| 63 | protected | ` protected virtual void Dispose(bool disposing)` | ListViewServices |
| 35 | public | ` public async Task ExecuteWithLoadingAsync(Func<Task> action, string? message = null, bool isBusy = false)` | LoadingStateManager |
| 52 | public | ` public async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action, string? message = null, bool isBusy = false)` | LoadingStateManager |
| 69 | public | ` public void BeginLoading(string? message = null)` | LoadingStateManager |
| 83 | public | ` public void EndLoading()` | LoadingStateManager |
| 101 | public | ` public void Reset()` | LoadingStateManager |
| 48 | public | ` public MasterDetailServices( IListViewServices<TListItem> list, IDetailEditorService<TDetail> detailEditor, IDialogManager dialog, INavigationCoordin…` | MasterDetailServices |
| 61 | public | ` public void ResetAll()` | MasterDetailServices |
| 68 | public | ` public void Dispose()` | MasterDetailServices |
| 74 | protected | ` protected virtual void Dispose(bool disposing)` | MasterDetailServices |
| 12 | public (interface 默认) | ` void ShowInfo(string message, string? title = null);` | INotificationService |
| 17 | public (interface 默认) | ` void ShowSuccess(string message, string? title = null);` | INotificationService |
| 22 | public (interface 默认) | ` void ShowWarning(string message, string? title = null);` | INotificationService |
| 27 | public (interface 默认) | ` void ShowError(string message, string? title = null);` | INotificationService |
| 32 | public (interface 默认) | ` Task ShowErrorAsync(string message, string? title = null);` | INotificationService |
| 37 | public (interface 默认) | ` Task ShowInfoAsync(string message, string? title = null);` | INotificationService |
| 42 | public (interface 默认) | ` Task ShowSuccessAsync(string message, string? title = null);` | INotificationService |
| 47 | public (interface 默认) | ` Task ShowWarningAsync(string message, string? title = null);` | INotificationService |
| 52 | public (interface 默认) | ` Task<bool> ShowConfirmAsync(string message, string title = "确认");` | INotificationService |
| 57 | public (interface 默认) | ` void ShowLoading(string message = "正在加载...");` | INotificationService |
| 62 | public (interface 默认) | ` void HideLoading();` | INotificationService |
| 16 | public | ` public NotificationService(ILogger<NotificationService> logger, IUiThreadDispatcher dispatcher)` | NotificationService |
| 35 | public | ` public void ShowInfo(string message, string? title = null)` | NotificationService |
| 43 | public | ` public void ShowSuccess(string message, string? title = null)` | NotificationService |
| 51 | public | ` public void ShowWarning(string message, string? title = null)` | NotificationService |
| 59 | public | ` public void ShowError(string message, string? title = null)` | NotificationService |
| 67 | public | ` public async Task ShowErrorAsync(string message, string? title = null)` | NotificationService |
| 75 | public | ` public async Task ShowInfoAsync(string message, string? title = null)` | NotificationService |
| 83 | public | ` public async Task ShowSuccessAsync(string message, string? title = null)` | NotificationService |
| 91 | public | ` public async Task ShowWarningAsync(string message, string? title = null)` | NotificationService |
| 99 | public | ` public async Task<bool> ShowConfirmAsync(string message, string title = "确认")` | NotificationService |
| 122 | public | ` public void ShowLoading(string message = "正在加载...")` | NotificationService |
| 143 | public | ` public void HideLoading()` | NotificationService |
| 164 | private | ` private void ShowNotification(string message, NotificationType type, string title)` | NotificationService |
| 200 | private | ` private static MessageBoxImage GetMessageBoxImage(NotificationType type)` | NotificationService |
| 52 | public | ` public void GoToFirstPage()` | PaginationService |
| 61 | public | ` public void GoToPreviousPage()` | PaginationService |
| 70 | public | ` public void GoToNextPage()` | PaginationService |
| 79 | public | ` public void GoToLastPage()` | PaginationService |
| 88 | public | ` public void GoToPage(int page)` | PaginationService |
| 102 | public | ` public void Reset()` | PaginationService |
| 108 | default(private) | ` partial void OnPageSizeChanged(int oldValue, int newValue)` | PaginationService |
| 27 | public | ` public async Task ExecuteSearchAsync(Func<string, Task> searchAction)` | SearchService |
| 46 | public | ` public async Task ExecuteSearchImmediateAsync(Func<string, Task> searchAction)` | SearchService |
| 66 | public | ` public void ClearSearch()` | SearchService |
| 73 | public | ` public void CancelSearch()` | SearchService |
| 83 | public | ` public void Dispose()` | SearchService |
| 93 | protected | ` protected virtual void Dispose(bool disposing)` | SearchService |
| 32 | public | ` public SelectionService()` | SelectionService |
| 42 | public | ` public void Select(T? item)` | SelectionService |
| 60 | public | ` public void SelectMultiple(IEnumerable<T> items)` | SelectionService |
| 75 | public | ` public void ToggleSelection(T item)` | SelectionService |
| 100 | public | ` public void ClearSelection()` | SelectionService |
| 108 | private | ` private void RaiseSelectionChanged(T? newSelection, T? oldSelection)` | SelectionService |
| 116 | default(private) | ` partial void OnSelectedItemChanged(T? oldValue, T? newValue)` | SelectionService |
| 21 | public | ` public SessionManager(IAuthenticationService authService) => _authService = authService ?? throw new ArgumentNullException(nameof(authService));` | SessionManager |
| 43 | public | ` public void SetSession(UserDetailDto user, string accessToken, string? refreshToken = null)` | SessionManager |
| 53 | public | ` public void ClearSession()` | SessionManager |
| 71 | public | ` public bool HasPermission(UserRole requiredRole) => CurrentUser != null && CurrentUser.Role >= requiredRole;` | SessionManager |
| 72 | public | ` public bool HasPermission(string permission) => IsAuthenticated && CurrentUser != null;` | SessionManager |
| 73 | public | ` public bool HasRole(string role) => CurrentUser != null && CurrentUser.Role.ToString().Equals(role, StringComparison.OrdinalIgnoreCase);` | SessionManager |
| 74 | public | ` public bool IsAdmin() => CurrentUser?.Role is UserRole.Admin or UserRole.SuperAdmin;` | SessionManager |
| 75 | public | ` public string GetCurrentUserRoleDisplay() => CurrentUser == null ? "未登录" : CurrentUser.Role switch { UserRole.Admin => "管理员", UserRole.Doctor => "医生"…` | SessionManager |
| 20 | public | ` public ToastService()` | ToastService |
| 28 | public | ` public void ShowInfo(string message)` | ToastService |
| 36 | public | ` public void ShowSuccess(string message)` | ToastService |
| 44 | public | ` public void ShowWarning(string message)` | ToastService |
| 52 | public | ` public void ShowError(string message)` | ToastService |
| 60 | public | ` public void Show(string message, ToastType type, int durationMilliseconds = 3000)` | ToastService |
| 107 | private | ` private void ShowMessageBoxFallback(string message, ToastType type)` | ToastService |
| 136 | public | ` public static Panel GetOrCreate(Window window)` | AdornerLayer |
| 91 | public | ` public UserActivityTracker( ILogger<UserActivityTracker> logger, IApplicationTickService tickService, int inactivityTimeoutMinutes = 15, int warningB…` | UserActivityTracker |
| 115 | public | ` public void StartTracking()` | UserActivityTracker |
| 157 | public | ` public void StopTracking()` | UserActivityTracker |
| 182 | public | ` public void ResetActivity()` | UserActivityTracker |
| 192 | private | ` private void OnPreProcessInput(object sender, PreProcessInputEventArgs e)` | UserActivityTracker |
| 213 | private | ` private void OnTick(object? sender, ApplicationTickEventArgs e)` | UserActivityTracker |
| 226 | private | ` private void CheckInactivity()` | UserActivityTracker |
| 261 | private | ` private void OnSessionExpired()` | UserActivityTracker |
| 273 | public | ` public void Dispose()` | UserActivityTracker |
| 17 | public | ` public Task HandleExceptionAsync(Exception exception, string? context = null)` | UserNotificationService |
| 29 | public | ` public Task ShowErrorAsync(string message, string? title = null)` | UserNotificationService |
| 42 | public | ` public Task ShowSuccessAsync(string message, string? title = null)` | UserNotificationService |
| 55 | public | ` public Task ShowWarningAsync(string message, string? title = null)` | UserNotificationService |
| 68 | public | ` public Task ShowInfoAsync(string message, string? title = null)` | UserNotificationService |
| 81 | public | ` public Task<bool> ShowConfirmAsync(string message, string? title = null)` | UserNotificationService |
| 25 | public | ` public ViewModelServices( ILoggerFactory loggerFactory, IEventAggregator eventAggregator, IRegionManager regionManager, ISessionManager sessionManage…` | ViewModelServices |
| 11 | public | ` public WpfUiThreadDispatcher()` | WpfUiThreadDispatcher |
| 17 | internal | ` internal WpfUiThreadDispatcher(Dispatcher dispatcher)` | WpfUiThreadDispatcher |
| 22 | public | ` public void Invoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)` | WpfUiThreadDispatcher |
| 30 | public | ` public T Invoke<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)` | WpfUiThreadDispatcher |
| 38 | public | ` public Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)` | WpfUiThreadDispatcher |
| 49 | public | ` public Task<T> InvokeAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)` | WpfUiThreadDispatcher |
| 57 | public | ` public void BeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)` | WpfUiThreadDispatcher |
| 62 | public | ` public bool CheckAccess() => _dispatcher.CheckAccess();` | WpfUiThreadDispatcher |
| 47 | public | ` public virtual bool CanCloseDialog() => true;` | DialogViewModelBase |
| 52 | public | ` public virtual void OnDialogClosed()` | DialogViewModelBase |
| 61 | public | ` public virtual void OnDialogOpened(IDialogParameters parameters)` | DialogViewModelBase |
| 76 | protected | ` protected DialogViewModelBase(IViewModelServices services)` | DialogViewModelBase |
| 88 | protected | ` protected virtual void OnDialogOpenedCore(IDialogParameters? parameters) { }` | DialogViewModelBase |
| 93 | protected | ` protected virtual void OnDialogClosedCore() { }` | DialogViewModelBase |
| 103 | protected | ` protected void CloseDialog(ButtonResult result = ButtonResult.None)` | DialogViewModelBase |
| 113 | protected | ` protected void CloseDialog(IDialogParameters parameters, ButtonResult result = ButtonResult.OK)` | DialogViewModelBase |
| 125 | protected | ` protected void CloseDialogWithResult<T>(string key, T value, ButtonResult result = ButtonResult.OK)` | DialogViewModelBase |
| 143 | protected | ` protected T GetDialogParameter<T>(IDialogParameters parameters, string key)` | DialogViewModelBase |
| 161 | protected | ` protected T GetDialogParameter<T>(IDialogParameters parameters, string key, T defaultValue)` | DialogViewModelBase |
| 174 | protected | ` protected bool TryGetDialogParameter<T>(IDialogParameters parameters, string key, out T? value)` | DialogViewModelBase |
| 187 | protected | ` protected virtual void Cancel()` | DialogViewModelBase |
| 197 | protected | ` protected virtual void Confirm()` | DialogViewModelBase |
| 206 | protected | ` protected virtual bool CanConfirm() => !IsBusy && !IsLoading;` | DialogViewModelBase |
| 215 | default(private) | ` partial void OnIsLoadingChanged(bool value)` | DialogViewModelBase |
| 224 | protected | ` protected virtual void OnIsLoadingChangedCore(bool value) { }` | DialogViewModelBase |
| 229 | protected | ` protected override void OnIsBusyChangedCore(bool value)` | DialogViewModelBase |
| 80 | default(private) | ` partial void OnHerbNameChanged(string value)` | HerbItemViewModelBase |
| 88 | default(private) | ` partial void OnSelectedHerbChanged(HerbListDto? value)` | HerbItemViewModelBase |
| 107 | protected | ` protected virtual void OnHerbSelected(HerbListDto herb)` | HerbItemViewModelBase |
| 116 | protected | ` protected virtual void OnDosageChanged(int newDosage)` | HerbItemViewModelBase |
| 128 | protected | ` protected void FilterHerbs()` | HerbItemViewModelBase |
| 175 | private | ` private int GetMatchScore(HerbListDto herb, string searchText)` | HerbItemViewModelBase |
| 238 | private | ` private bool IsPinyinFuzzyMatch(string pinyinCode, string searchText)` | HerbItemViewModelBase |
| 22 | protected | ` protected async Task ExecuteWithErrorHandlingAsync( Func<Task> action, string operationName, bool showBusy = true, bool showErrorToUser = true)` | NavigableViewModelBase |
| 62 | protected | ` protected async Task<T?> ExecuteWithErrorHandlingAsync<T>( Func<Task<T>> action, string operationName, T? defaultValue = default, bool showBusy = tru…` | NavigableViewModelBase |
| 102 | protected | ` protected void RunOnUIThread(Action action)` | NavigableViewModelBase |
| 110 | protected | ` protected Task RunOnUIThreadAsync(Func<Task> action)` | NavigableViewModelBase |
| 205 | protected | ` protected NavigableViewModelBase(IViewModelServices services)` | NavigableViewModelBase |
| 227 | default(private) | ` partial void OnIsBusyChanged(bool value)` | NavigableViewModelBase |
| 235 | protected | ` protected virtual void OnIsBusyChangedCore(bool value) { }` | NavigableViewModelBase |
| 246 | protected | ` protected void SetBusy(bool isBusy, string? message = null)` | NavigableViewModelBase |
| 262 | protected | ` protected void ClearError()` | NavigableViewModelBase |
| 271 | protected | ` protected void SetError(string message)` | NavigableViewModelBase |
| 284 | protected | ` protected void AddDisposable(IDisposable disposable)` | NavigableViewModelBase |
| 23 | protected | ` protected virtual Task ShowSuccessMessageAsync(string message)` | NavigableViewModelBase |
| 33 | protected | ` protected virtual Task ShowErrorMessageAsync(string message)` | NavigableViewModelBase |
| 43 | protected | ` protected virtual Task ShowWarningMessageAsync(string message)` | NavigableViewModelBase |
| 52 | protected | ` protected virtual async Task<bool> ShowConfirmMessageAsync(string message, string title = "确认")` | NavigableViewModelBase |
| 64 | protected | ` protected void MarkAsChanged()` | NavigableViewModelBase |
| 72 | protected | ` protected void MarkAsSaved()` | NavigableViewModelBase |
| 91 | public | ` public virtual void BeginEdit()` | NavigableViewModelBase |
| 101 | public | ` public virtual void CancelEdit()` | NavigableViewModelBase |
| 112 | public | ` public virtual void EndEdit()` | NavigableViewModelBase |
| 123 | protected | ` protected virtual void OnBeginEdit() { }` | NavigableViewModelBase |
| 128 | protected | ` protected virtual void OnCancelEdit() { }` | NavigableViewModelBase |
| 133 | protected | ` protected virtual void OnEndEdit() { }` | NavigableViewModelBase |
| 139 | public | ` public void Dispose()` | NavigableViewModelBase |
| 145 | protected | ` protected virtual void Dispose(bool disposing)` | NavigableViewModelBase |
| 162 | protected | ` protected virtual void OnDisposing() { }` | NavigableViewModelBase |
| 23 | public | ` public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;` | NavigableViewModelBase |
| 26 | public | ` public virtual void OnNavigatedTo(NavigationContext navigationContext)` | NavigableViewModelBase |
| 63 | public | ` public virtual void OnNavigatedFrom(NavigationContext navigationContext)` | NavigableViewModelBase |
| 75 | public | ` public virtual void ConfirmNavigationRequest( NavigationContext navigationContext, Action<bool> continuationCallback)` | NavigableViewModelBase |
| 92 | private | ` private async Task ConfirmNavigationWithUnsavedChangesAsync(Action<bool> continuationCallback)` | NavigableViewModelBase |
| 110 | protected | ` protected virtual async Task<bool> ShowUnsavedChangesDialogAsync()` | NavigableViewModelBase |
| 120 | protected | ` protected virtual bool CanNavigateAway() => true;` | NavigableViewModelBase |
| 129 | protected | ` protected virtual void OnNavigatedToCore(NavigationContext context) { }` | NavigableViewModelBase |
| 134 | protected | ` protected virtual void OnNavigatedFromCore(NavigationContext context) { }` | NavigableViewModelBase |
| 139 | protected | ` protected virtual Task InitializeAsync(NavigationContext context) => Task.CompletedTask;` | NavigableViewModelBase |
| 149 | protected | ` protected virtual void NavigateToHome()` | NavigableViewModelBase |
| 171 | protected | ` protected virtual void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)` | NavigableViewModelBase |
| 188 | protected | ` protected virtual string GetHomeViewName()` | NavigableViewModelBase |
| 25 | public | ` public IEnumerable GetErrors(string? propertyName) =>` | ValidatableModelBase |
| 39 | protected | ` protected ValidatableModelBase()` | ValidatableModelBase |
| 54 | protected | ` protected bool SetPropertyAndValidate<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)` | ValidatableModelBase |
| 69 | protected | ` protected virtual void ValidateProperty([CallerMemberName] string? propertyName = null)` | ValidatableModelBase |
| 98 | public | ` public virtual bool ValidateAll()` | ValidatableModelBase |
| 122 | protected | ` protected void AddValidationError(string propertyName, string errorMessage)` | ValidatableModelBase |
| 139 | protected | ` protected void ClearValidationErrors(string? propertyName = null)` | ValidatableModelBase |
| 159 | protected | ` protected virtual void OnErrorsChanged(string propertyName)` | ValidatableModelBase |
| 16 | protected | ` protected ChildViewModelBase(IWorkspaceHost host, ILoggerFactory loggerFactory)` | ChildViewModelBase |
| 27 | public | ` public virtual Task InitializeAsync() => Task.CompletedTask;` | ChildViewModelBase |
| 29 | public | ` public virtual void Dispose() { }` | ChildViewModelBase |
| 17 | public | ` public MasterDetailCommandGroup( IMasterDetailServices<TListItem, TDetail> services, ICommandHost<TListItem, TDetail> host)` | MasterDetailCommandGroup |
| 30 | private | ` private async Task RefreshAsync()` | MasterDetailCommandGroup |
| 37 | private | ` private async Task SearchAsync()` | MasterDetailCommandGroup |
| 47 | private | ` private async Task ClearSearchAsync()` | MasterDetailCommandGroup |
| 55 | private | ` private async Task GoToFirstPageAsync()` | MasterDetailCommandGroup |
| 62 | private | ` private async Task GoToPreviousPageAsync()` | MasterDetailCommandGroup |
| 69 | private | ` private async Task GoToNextPageAsync()` | MasterDetailCommandGroup |
| 76 | private | ` private async Task GoToLastPageAsync()` | MasterDetailCommandGroup |
| 82 | private | ` private bool CanGoToFirstPage() => _services.Pagination.CanGoToFirstPage;` | MasterDetailCommandGroup |
| 83 | private | ` private bool CanGoToPreviousPage() => _services.Pagination.CanGoToPreviousPage;` | MasterDetailCommandGroup |
| 84 | private | ` private bool CanGoToNextPage() => _services.Pagination.CanGoToNextPage;` | MasterDetailCommandGroup |
| 85 | private | ` private bool CanGoToLastPage() => _services.Pagination.CanGoToLastPage;` | MasterDetailCommandGroup |
| 92 | private | ` private async Task CreateNewAsync()` | MasterDetailCommandGroup |
| 102 | private | ` private void Edit()` | MasterDetailCommandGroup |
| 108 | private | ` private async Task SaveAsync()` | MasterDetailCommandGroup |
| 123 | private | ` private async Task CancelAsync()` | MasterDetailCommandGroup |
| 135 | private | ` private async Task DeleteAsync()` | MasterDetailCommandGroup |
| 168 | private | ` private async Task BatchEnableAsync()` | MasterDetailCommandGroup |
| 182 | private | ` private async Task BatchDisableAsync()` | MasterDetailCommandGroup |
| 196 | private | ` private async Task RestoreAsync()` | MasterDetailCommandGroup |
| 214 | private | ` private bool CanCreateNew() => !_services.DetailEditor.IsEditMode && !_services.Loading.IsBusy;` | MasterDetailCommandGroup |
| 215 | private | ` private bool CanEdit() => _services.Selection.HasSelection && !_services.DetailEditor.IsEditMode && !_services.Loading.IsBusy;` | MasterDetailCommandGroup |
| 216 | private | ` private bool CanSave() => _services.DetailEditor.IsEditMode && _services.DetailEditor.CurrentDetail != null && !_services.Loading.IsBusy;` | MasterDetailCommandGroup |
| 217 | private | ` private bool CanCancel() => _services.DetailEditor.IsEditMode;` | MasterDetailCommandGroup |
| 218 | private | ` private bool CanDelete() => _services.Selection.HasSelection && !_services.DetailEditor.IsEditMode && !_services.Loading.IsBusy;` | MasterDetailCommandGroup |
| 219 | private | ` private bool CanRestore() => _services.Selection.HasSelection && !_services.Loading.IsBusy && _host.IsAdmin;` | MasterDetailCommandGroup |
| 226 | public | ` public void NotifyCrudCommandsChanged()` | MasterDetailCommandGroup |
| 236 | public | ` public void NotifyPaginationCommandsChanged()` | MasterDetailCommandGroup |
| 244 | public | ` public void NotifyAllCommandsChanged()` | MasterDetailCommandGroup |
| 261 | public (interface 默认) | ` Task LoadListAsync();` | ICommandHost |
| 262 | public (interface 默认) | ` TDetail CreateNewDetail();` | ICommandHost |
| 263 | public (interface 默认) | ` Task<bool> SaveDetailAsync(TDetail detail);` | ICommandHost |
| 264 | public (interface 默认) | ` Task<bool> DeleteItemAsync(TListItem item);` | ICommandHost |
| 265 | public (interface 默认) | ` Task DeleteBatchAsync(List<TListItem> items);` | ICommandHost |
| 266 | public (interface 默认) | ` Task EnableBatchAsync(List<TListItem> items);` | ICommandHost |
| 267 | public (interface 默认) | ` Task DisableBatchAsync(List<TListItem> items);` | ICommandHost |
| 268 | public (interface 默认) | ` Task RestoreItemAsync(TListItem item);` | ICommandHost |
| 269 | public (interface 默认) | ` Task InvalidateCachesAsync();` | ICommandHost |
| 270 | public (interface 默认) | ` Task OnDetailCreatedAsync(TDetail detail);` | ICommandHost |
| 271 | public (interface 默认) | ` Task OnDetailSavedAsync(TDetail detail);` | ICommandHost |
| 272 | public (interface 默认) | ` Task OnItemDeletedAsync(TListItem item);` | ICommandHost |
| 273 | public (interface 默认) | ` List<TListItem> GetSelectedItemsForDelete();` | ICommandHost |
| 19 | public | ` public ServiceEventBridge( IMasterDetailServices<TListItem, TDetail> services, IServiceEventCallback<TListItem, TDetail> callback)` | ServiceEventBridge |
| 28 | private | ` private void Subscribe()` | ServiceEventBridge |
| 40 | private | ` private void ForwardPropertyChanged(object? sender, PropertyChangedEventArgs e)` | ServiceEventBridge |
| 43 | private | ` private void OnLoadingPropertyChanged(object? sender, PropertyChangedEventArgs e)` | ServiceEventBridge |
| 59 | private | ` private void OnPaginationPropertyChanged(object? sender, PropertyChangedEventArgs e)` | ServiceEventBridge |
| 74 | private | ` private void OnPaginationPageChanged(object? sender, EventArgs e)` | ServiceEventBridge |
| 80 | private | ` private void OnSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)` | ServiceEventBridge |
| 89 | private | ` private void OnSelectionSelectionChanged(object? sender, SelectionChangedEventArgs<TListItem> e)` | ServiceEventBridge |
| 95 | private | ` private void OnDetailEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)` | ServiceEventBridge |
| 116 | private | ` private void OnErrorHandlerPropertyChanged(object? sender, PropertyChangedEventArgs e)` | ServiceEventBridge |
| 128 | public | ` public void Dispose()` | ServiceEventBridge |
| 148 | public (interface 默认) | ` void OnPropertyChanged(string? propertyName);` | IServiceEventCallback |
| 149 | public (interface 默认) | ` void OnIsLoadingChanged(bool isLoading);` | IServiceEventCallback |
| 150 | public (interface 默认) | ` void OnIsBusyChanged(bool isBusy);` | IServiceEventCallback |
| 151 | public (interface 默认) | ` void OnPaginationCommandsChanged();` | IServiceEventCallback |
| 152 | public (interface 默认) | ` Task OnPageChanged();` | IServiceEventCallback |
| 153 | public (interface 默认) | ` void OnSelectionChanged();` | IServiceEventCallback |
| 154 | public (interface 默认) | ` Task OnSelectionItemChanged(SelectionChangedEventArgs<TListItem> e);` | IServiceEventCallback |
| 155 | public (interface 默认) | ` void OnHasUnsavedChangesChanged(bool hasUnsavedChanges);` | IServiceEventCallback |
| 156 | public (interface 默认) | ` void OnCrudCommandsChanged();` | IServiceEventCallback |
| 157 | public (interface 默认) | ` void OnDetailStateChanged();` | IServiceEventCallback |
| 158 | public (interface 默认) | ` void OnErrorMessageChanged(string message);` | IServiceEventCallback |
| 16 | protected | ` protected BaseStatusHandler(IDialogManager dialog, ILogger logger)` | BaseStatusHandler |
| 26 | protected | ` protected abstract Guid GetEntityId(TListDto entity);` | BaseStatusHandler |
| 29 | protected | ` protected abstract string GetEntityDisplayName(TListDto entity);` | BaseStatusHandler |
| 32 | protected | ` protected abstract Task<object?> ExecuteRestoreAsync(Guid id);` | BaseStatusHandler |
| 35 | protected | ` protected virtual CommonStatus GetEntityStatus(TListDto entity)` | BaseStatusHandler |
| 39 | protected | ` protected virtual Task<CommonStatus?> ExecuteToggleStatusAsync(Guid id)` | BaseStatusHandler |
| 46 | public | ` public async Task<bool> RestoreAsync(TListDto entity)` | BaseStatusHandler |
| 78 | public | ` public virtual async Task<bool> ToggleStatusAsync(TListDto entity)` | BaseStatusHandler |
| 96 | protected | ` protected virtual string? GetDetailDisplayName() => null;` | MasterDetailViewModelBase |
| 118 | protected | ` protected MasterDetailViewModelBase( IViewModelServices services, IMasterDetailServices<TListItem, TDetail> masterDetailServices)` | MasterDetailViewModelBase |
| 131 | default(private) | ` [RelayCommand] protected Task RefreshAsync() => _commands.RefreshCommand.ExecuteAsync(null);` | MasterDetailViewModelBase |
| 132 | default(private) | ` [RelayCommand] protected Task SearchAsync() => _commands.SearchCommand.ExecuteAsync(null);` | MasterDetailViewModelBase |
| 133 | default(private) | ` [RelayCommand] protected Task ClearSearchAsync() => _commands.ClearSearchCommand.ExecuteAsync(null);` | MasterDetailViewModelBase |
| 147 | private | ` private bool CanGoToFirstPage() => _masterDetailServices.Pagination.CanGoToFirstPage;` | MasterDetailViewModelBase |
| 148 | private | ` private bool CanGoToPreviousPage() => _masterDetailServices.Pagination.CanGoToPreviousPage;` | MasterDetailViewModelBase |
| 149 | private | ` private bool CanGoToNextPage() => _masterDetailServices.Pagination.CanGoToNextPage;` | MasterDetailViewModelBase |
| 150 | private | ` private bool CanGoToLastPage() => _masterDetailServices.Pagination.CanGoToLastPage;` | MasterDetailViewModelBase |
| 151 | private | ` private bool CanCreateNew() => !IsEditMode && !IsBusy;` | MasterDetailViewModelBase |
| 152 | private | ` private bool CanEdit() => HasSelection && !IsEditMode && !IsBusy;` | MasterDetailViewModelBase |
| 153 | private | ` private bool CanSave() => IsEditMode && CurrentDetail != null && !IsBusy;` | MasterDetailViewModelBase |
| 154 | private | ` private bool CanCancel() => IsEditMode;` | MasterDetailViewModelBase |
| 155 | private | ` private bool CanDelete() => HasSelection && !IsEditMode && !IsBusy;` | MasterDetailViewModelBase |
| 156 | private | ` private bool CanRestore() => HasSelection && !IsBusy && IsAdmin;` | MasterDetailViewModelBase |
| 238 | protected | ` protected abstract Task LoadListAsync();` | MasterDetailViewModelBase |
| 239 | protected | ` protected abstract Task LoadDetailAsync(TListItem item);` | MasterDetailViewModelBase |
| 240 | protected | ` protected abstract TDetail CreateNewDetail();` | MasterDetailViewModelBase |
| 241 | protected | ` protected abstract Task<bool> SaveDetailAsync(TDetail detail);` | MasterDetailViewModelBase |
| 242 | protected | ` protected abstract Task<bool> DeleteItemAsync(TListItem item);` | MasterDetailViewModelBase |
| 248 | protected | ` protected virtual Task OnDetailCreatedAsync(TDetail detail) => Task.CompletedTask;` | MasterDetailViewModelBase |
| 249 | protected | ` protected virtual Task OnDetailSavedAsync(TDetail detail) => Task.CompletedTask;` | MasterDetailViewModelBase |
| 250 | protected | ` protected virtual Task OnItemDeletedAsync(TListItem item) => Task.CompletedTask;` | MasterDetailViewModelBase |
| 251 | protected | ` protected virtual async Task DeleteBatchAsync(List<TListItem> items)` | MasterDetailViewModelBase |
| 259 | protected | ` protected virtual async Task EnableBatchAsync(List<TListItem> items)` | MasterDetailViewModelBase |
| 263 | protected | ` protected virtual async Task DisableBatchAsync(List<TListItem> items)` | MasterDetailViewModelBase |
| 267 | protected | ` protected virtual Task RestoreItemAsync(TListItem item) => Task.CompletedTask;` | MasterDetailViewModelBase |
| 268 | protected | ` protected virtual Task InvalidateCachesAsync() => Task.CompletedTask;` | MasterDetailViewModelBase |
| 269 | protected | ` protected virtual Task SetItemEnabledAsync(TListItem item, bool enabled) => Task.CompletedTask;` | MasterDetailViewModelBase |
| 275 | public | ` public virtual async Task InitializeAsync()` | MasterDetailViewModelBase |
| 281 | protected | ` protected override void OnNavigatedToCore(NavigationContext navigationContext)` | MasterDetailViewModelBase |
| 287 | protected | ` protected virtual async Task OnNavigatedToAsync(NavigationContext navigationContext)` | MasterDetailViewModelBase |
| 297 | protected | ` protected override void OnDisposing()` | MasterDetailViewModelBase |
| 13 | public | ` public ValidationErrorsAccessor(Dictionary<string, List<string>> errors) => _errors = errors;` | ValidationErrorsAccessor |
| 32 | public | ` public ValidationHasErrorsAccessor(Dictionary<string, List<string>> errors) => _errors = errors;` | ValidationHasErrorsAccessor |
### LYBT.Desktop.MedicalCase

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 18 | public | ` public MedicalCaseEditControl()` | MedicalCaseEditControl |
| 12 | public | ` public MedicalCaseMasterDetailControl()` | MedicalCaseMasterDetailControl |
| 19 | public | ` public MedicalCaseViewControl()` | MedicalCaseViewControl |
| 14 | public | ` public WorkflowStepIndicator()` | WorkflowStepIndicator |
| 33 | private | ` private static void OnCurrentStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)` | WorkflowStepIndicator |
| 66 | private | ` private void UpdateSteps()` | WorkflowStepIndicator |
| 11 | public | ` public FormulaImportDialog()` | FormulaImportDialog |
| 94 | default(private) | ` partial void OnSearchTextChanged(string value)` | FormulaImportDialogViewModel |
| 102 | default(private) | ` partial void OnSelectedCategoryChanged(string value)` | FormulaImportDialogViewModel |
| 110 | default(private) | ` partial void OnSelectedFormulaChanged(FormulaListDto? value)` | FormulaImportDialogViewModel |
| 122 | public | ` public FormulaImportDialogViewModel( IViewModelServices services, IFormulaSearchProvider formulaSearchProvider)` | FormulaImportDialogViewModel |
| 143 | protected | ` protected override void OnDialogOpenedCore(IDialogParameters? parameters)` | FormulaImportDialogViewModel |
| 151 | protected | ` protected override bool CanConfirm() => SelectedFormula != null && SelectedFormulaHerbs.Any();` | FormulaImportDialogViewModel |
| 156 | protected | ` protected override void Confirm()` | FormulaImportDialogViewModel |
| 173 | private | ` private void InitializeCategories()` | FormulaImportDialogViewModel |
| 189 | private | ` private void LoadFormulasAsync() => LoadFormulasInternalAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载验方列表失败"));` | FormulaImportDialogViewModel |
| 190 | private | ` private async Task LoadFormulasInternalAsync()` | FormulaImportDialogViewModel |
| 242 | private | ` private void FilterFormulas()` | FormulaImportDialogViewModel |
| 286 | private | ` private void LoadFormulaPreviewAsync() => LoadFormulaPreviewInternalAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载验方预览失败"));` | FormulaImportDialogViewModel |
| 287 | private | ` private async Task LoadFormulaPreviewInternalAsync()` | FormulaImportDialogViewModel |
| 11 | public | ` public HistoryCopyDialog()` | HistoryCopyDialog |
| 149 | default(private) | ` partial void OnSearchTextChanged(string value)` | HistoryCopyDialogViewModel |
| 157 | default(private) | ` partial void OnStartDateChanged(DateTime? value)` | HistoryCopyDialogViewModel |
| 165 | default(private) | ` partial void OnEndDateChanged(DateTime? value)` | HistoryCopyDialogViewModel |
| 173 | default(private) | ` partial void OnSelectedCaseChanged(MedicalCaseDetailDto? value)` | HistoryCopyDialogViewModel |
| 181 | default(private) | ` partial void OnIsShowingAllPatientsChanged(bool value)` | HistoryCopyDialogViewModel |
| 198 | default(private) | ` partial void OnIsShowingAllCurrentPatientChanged(bool value)` | HistoryCopyDialogViewModel |
| 210 | public | ` public HistoryCopyDialogViewModel( IViewModelServices services, IMedicalCaseRepository medicalCaseRepository)` | HistoryCopyDialogViewModel |
| 228 | protected | ` protected override void OnDialogOpenedCore(IDialogParameters? parameters)` | HistoryCopyDialogViewModel |
| 248 | protected | ` protected override bool CanConfirm() => SelectedCase != null && SelectedPrescriptionItems.Any();` | HistoryCopyDialogViewModel |
| 253 | protected | ` protected override void Confirm()` | HistoryCopyDialogViewModel |
| 272 | private | ` private void ShowMoreCurrentPatient()` | HistoryCopyDialogViewModel |
| 281 | private | ` private void ToggleAllPatients()` | HistoryCopyDialogViewModel |
| 294 | private | ` private void LoadCasesAsync() => LoadCasesInternalAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载患者历史医案失败"));` | HistoryCopyDialogViewModel |
| 295 | private | ` private async Task LoadCasesInternalAsync()` | HistoryCopyDialogViewModel |
| 360 | private | ` private void LoadAllPatientsAsync() => LoadAllPatientsInternalAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载全部患者历史医案失败"));` | HistoryCopyDialogViewModel |
| 361 | private | ` private async Task LoadAllPatientsInternalAsync()` | HistoryCopyDialogViewModel |
| 428 | private | ` private void ApplyCurrentPatientFilter()` | HistoryCopyDialogViewModel |
| 462 | private | ` private void FilterCases()` | HistoryCopyDialogViewModel |
| 504 | private | ` private void LoadCaseDetailAsync() => LoadCaseDetailInternalAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载医案详情失败"));` | HistoryCopyDialogViewModel |
| 505 | private | ` private async Task LoadCaseDetailInternalAsync()` | HistoryCopyDialogViewModel |
| 10 | public | ` public UnsavedChangesDialog()` | UnsavedChangesDialog |
| 17 | public | ` public UnsavedChangesDialogViewModel(IViewModelServices services)` | UnsavedChangesDialogViewModel |
| 28 | private | ` private void Save()` | UnsavedChangesDialogViewModel |
| 38 | private | ` private void Discard()` | UnsavedChangesDialogViewModel |
| 16 | public | ` public static IReadOnlyList<PrescriptionItemDto> ToPrescriptionItemDtos( this FormulaDetailDto formula, List<FormulaHerbItemDto> herbs, IReadOnlyDict…` | PrescriptionImportExtensions |
| 43 | public | ` public static IReadOnlyList<PrescriptionItemDto> ToPrescriptionItemDtos( this List<PrescriptionItemDto> items, IReadOnlyDictionary<Guid, decimal>? he…` | PrescriptionImportExtensions |
| 21 | public (interface 默认) | ` ConsultationInputDto? GetConsultationData();` | IDataProvider |
| 27 | public (interface 默认) | ` PrescriptionInputDto? GetPrescriptionData();` | IDataProvider |
| 23 | public (interface 默认) | ` void Initialize(WorkspaceEditState initialState, Func<WorkspaceEditEvent, bool>? guardPredicate = null);` | IEditModeStateMachine |
| 26 | public (interface 默认) | ` bool CanFire(WorkspaceEditEvent evt);` | IEditModeStateMachine |
| 32 | public (interface 默认) | ` bool Fire(WorkspaceEditEvent evt, string? context = null);` | IEditModeStateMachine |
| 35 | public (interface 默认) | ` IEnumerable<WorkspaceEditEvent> GetPermittedEvents();` | IEditModeStateMachine |
| 51 | public | ` public EditStateChangedEventArgs( WorkspaceEditState previousState, WorkspaceEditState newState, WorkspaceEditEvent triggerEvent, string? context = n…` | EditStateChangedEventArgs |
| 16 | public (interface 默认) | ` ConsultationInputDto? GetConsultationData();` | IMedicalCaseDataProvider |
| 17 | public (interface 默认) | ` PrescriptionInputDto? GetPrescriptionData();` | IMedicalCaseDataProvider |
| 18 | public (interface 默认) | ` IValidatable? GetConsultationValidator();` | IMedicalCaseDataProvider |
| 19 | public (interface 默认) | ` IValidatable? GetPrescriptionValidator();` | IMedicalCaseDataProvider |
| 20 | public (interface 默认) | ` IDataProvider? GetPrescriptionProvider();` | IMedicalCaseDataProvider |
| 21 | public (interface 默认) | ` ConsultationItem? GetConsultationItem();` | IMedicalCaseDataProvider |
| 22 | public (interface 默认) | ` PrescriptionItemViewModel? GetPrescriptionItem();` | IMedicalCaseDataProvider |
| 23 | public (interface 默认) | ` IEnumerable<HerbListDto>? GetAllHerbs();` | IMedicalCaseDataProvider |
| 24 | public (interface 默认) | ` string GetRemark();` | IMedicalCaseDataProvider |
| 25 | public (interface 默认) | ` string GetEditReason();` | IMedicalCaseDataProvider |
| 26 | public (interface 默认) | ` bool GetIsPrescriptionEnabled();` | IMedicalCaseDataProvider |
| 42 | public (interface 默认) | ` void ClearCache();` | IMedicalCaseService |
| 12 | public (interface 默认) | ` bool Validate();` | IValidatable |
| 43 | private | ` private partial ConsultationItem ToItemCore(ConsultationDetailDto dto);` | ConsultationMapper |
| 50 | public | ` public ConsultationItem ToItem(ConsultationDetailDto dto)` | ConsultationMapper |
| 85 | private | ` private partial ConsultationDetailDto ToDtoCore(ConsultationItem item);` | ConsultationMapper |
| 92 | public | ` public ConsultationDetailDto ToDto(ConsultationItem item)` | ConsultationMapper |
| 124 | public | ` public partial ConsultationInputDto ToInputDto(ConsultationItem item);` | ConsultationMapper |
| 28 | public | ` public partial MedicalCaseDetailDto Clone(MedicalCaseDetailDto source);` | MedicalCaseCloneMapper |
| 35 | public | ` public partial ConsultationDetailDto Clone(ConsultationDetailDto source);` | MedicalCaseCloneMapper |
| 42 | public | ` public partial PrescriptionDetailDto Clone(PrescriptionDetailDto source);` | MedicalCaseCloneMapper |
| 74 | private | ` private partial MedicalCaseDetailModel ToItemCore(MedicalCaseDetailDto dto);` | MedicalCaseDetailModelMapper |
| 81 | public | ` public MedicalCaseDetailModel ToItem(MedicalCaseDetailDto dto)` | MedicalCaseDetailModelMapper |
| 150 | private | ` private partial MedicalCaseInputDto ToInputDtoCore(MedicalCaseDetailModel model);` | MedicalCaseDetailModelMapper |
| 157 | public | ` public MedicalCaseInputDto ToInputDto(MedicalCaseDetailModel model)` | MedicalCaseDetailModelMapper |
| 49 | private | ` private partial PrescriptionItemViewModel ToItemCore(PrescriptionDetailDto dto);` | PrescriptionMapper |
| 56 | public | ` public PrescriptionItemViewModel ToItem(PrescriptionDetailDto dto)` | PrescriptionMapper |
| 96 | private | ` private partial PrescriptionDetailDto ToDtoCore(PrescriptionItemViewModel item);` | PrescriptionMapper |
| 103 | public | ` public PrescriptionDetailDto ToDto(PrescriptionItemViewModel item)` | PrescriptionMapper |
| 147 | private | ` private partial PrescriptionInputDto ToInputDtoCore(PrescriptionItemViewModel item);` | PrescriptionMapper |
| 154 | public | ` public PrescriptionInputDto ToInputDto(PrescriptionItemViewModel item)` | PrescriptionMapper |
| 30 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | MedicalCaseModule |
| 35 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | MedicalCaseModule |
| 226 | public | ` public void Reset()` | ConsultationItem |
| 239 | public | ` public ConsultationInputDto? GetConsultationData() => s_mapper.ToInputDto(this);` | ConsultationItem |
| 242 | public | ` public PrescriptionInputDto? GetPrescriptionData() => null;` | ConsultationItem |
| 264 | public | ` public bool Validate()` | ConsultationItem |
| 318 | public | ` public void Clear()` | PrescriptionItemViewModel |
| 344 | public | ` public void Reset()` | PrescriptionItemViewModel |
| 358 | public | ` public void NotifyItemsChanged()` | PrescriptionItemViewModel |
| 373 | public | ` public ConsultationInputDto? GetConsultationData() => null;` | PrescriptionItemViewModel |
| 376 | public | ` public PrescriptionInputDto? GetPrescriptionData()` | PrescriptionItemViewModel |
| 417 | public | ` public bool Validate()` | PrescriptionItemViewModel |
| 222 | public | ` public MedicalCaseDetailModel Clone()` | MedicalCaseDetailModel |
| 85 | public | ` public WorkspaceState EnterEditMode()` | WorkspaceState |
| 88 | public | ` public WorkspaceState EnterReadOnlyMode()` | WorkspaceState |
| 91 | public | ` public WorkspaceState DetermineFromContext( WorkspaceMode workspaceMode, bool isCompleted, bool isOwner, bool isAdmin, bool preferEditing)` | WorkspaceState |
| 16 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | ReportsModule |
| 21 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | ReportsModule |
| 19 | public | ` public ReportService(IApiClient apiClient, ILogger<ReportService> logger)` | ReportService |
| 26 | public | ` public async Task<CommandResult<DailyIncomeDto>> GetDailyIncomeAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = def…` | ReportService |
| 43 | public | ` public async Task<CommandResult<DailyConsultationDto>> GetDailyConsultationsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationT…` | ReportService |
| 60 | public | ` public async Task<CommandResult<DailyHerbUsageDto>> GetDailyHerbUsageAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct…` | ReportService |
| 33 | public | ` public ReportsHomeViewModel( IViewModelServices services, IReportService reportService)` | ReportsHomeViewModel |
| 41 | default(private) | ` partial void OnSelectedDateChanged(DateTime value)` | ReportsHomeViewModel |
| 46 | public | ` public override async void OnNavigatedTo(NavigationContext navigationContext)` | ReportsHomeViewModel |
| 52 | public | ` public override bool IsNavigationTarget(NavigationContext navigationContext) => true;` | ReportsHomeViewModel |
| 54 | public | ` public override void OnNavigatedFrom(NavigationContext navigationContext)` | ReportsHomeViewModel |
| 60 | private | ` private void GoToToday() => SelectedDate = DateTime.Today;` | ReportsHomeViewModel |
| 63 | private | ` private async Task LoadDataAsync()` | ReportsHomeViewModel |
| 7 | public | ` public ReportsHomeView()` | ReportsHomeView |
| 18 | public | ` public MedicalCaseRepository( IApiClient apiClient, ILogger<MedicalCaseRepository> logger)` | MedicalCaseRepository |
| 30 | public | ` public async Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = defa…` | MedicalCaseRepository |
| 52 | public | ` public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)` | MedicalCaseRepository |
| 63 | public | ` public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto, CancellationToken ct = default)` | MedicalCaseRepository |
| 81 | public | ` public async Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto, CancellationToken ct = default)` | MedicalCaseRepository |
| 101 | public | ` public async Task DeleteAsync(Guid id, CancellationToken ct = default)` | MedicalCaseRepository |
| 120 | public | ` public async Task<PagedResult<MedicalCaseDetailDto>> SearchAsync( string? patientName = null, string? diagnosisKeyword = null, DateTime? startDate = …` | MedicalCaseRepository |
| 147 | public | ` public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query, CancellationToken ct = default)` | MedicalCaseRepository |
| 168 | public | ` public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid? patientId = null, CancellationToken ct = default)` | MedicalCaseRepository |
| 185 | public | ` public async Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default)` | MedicalCaseRepository |
| 213 | public | ` public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequest? request, CancellationToken ct = default)` | MedicalCaseRepository |
| 240 | public | ` public async Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request, CancellationToken ct = default)` | MedicalCaseRepository |
| 268 | public | ` public async Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request, CancellationToken ct = default)` | MedicalCaseRepository |
| 302 | public | ` public async Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto, CancellationToken ct = default)` | MedicalCaseRepository |
| 326 | public | ` public async Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request, CancellationToken ct = default)` | MedicalCaseRepository |
| 360 | public | ` public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)` | MedicalCaseRepository |
| 20 | public | ` public AuditLogService(IApiClient apiClient, ILogger<AuditLogService> logger)` | AuditLogService |
| 27 | public | ` public async Task<CommandResult<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid medicalCaseId, int page, int pageSize, CancellationToken ct = defau…` | AuditLogService |
| 21 | public | ` public MedicalCaseCommandService( IMedicalCaseRepository repository, MedicalCaseEditContext context, ILogger<MedicalCaseCommandService> logger, ISess…` | MedicalCaseCommandService |
| 38 | public | ` public virtual async Task<bool> SaveAsync(CancellationToken ct = default)` | MedicalCaseCommandService |
| 61 | public | ` public virtual async Task<bool> DeleteAsync(CancellationToken ct = default)` | MedicalCaseCommandService |
| 118 | private | ` private bool IsMedicalCaseChanged() => _context.CurrentDetail != null && _context.OriginalDetail != null &&` | MedicalCaseCommandService |
| 124 | private | ` private bool IsConsultationChanged()` | MedicalCaseCommandService |
| 133 | private | ` private bool IsPrescriptionChanged()` | MedicalCaseCommandService |
| 141 | private | ` private static void UpdateMedicalCaseFields(MedicalCaseDetailDto target, MedicalCaseDetailDto source)` | MedicalCaseCommandService |
| 24 | public | ` public void SetCurrent(MedicalCaseDetailDto detail)` | MedicalCaseEditContext |
| 30 | public | ` public void UpdateOriginal()` | MedicalCaseEditContext |
| 36 | public | ` public void Clear()` | MedicalCaseEditContext |
| 42 | public | ` public void ClearCache()` | MedicalCaseEditContext |
| 23 | public | ` public MedicalCaseLifecycleService( IMedicalCaseRepository repository, MedicalCaseEditContext context, ILogger<MedicalCaseLifecycleService> logger)` | MedicalCaseLifecycleService |
| 37 | public | ` public async Task InitializeAsync(Guid entityId, CancellationToken ct = default)` | MedicalCaseLifecycleService |
| 50 | public | ` public async Task ReloadAsync(CancellationToken ct = default)` | MedicalCaseLifecycleService |
| 166 | public | ` public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default)` | MedicalCaseLifecycleService |
| 189 | private | ` private async Task<ApiResponse<MedicalCaseDetailDto>> SuspendViaApiAsync(Guid medicalCaseId)` | MedicalCaseLifecycleService |
| 18 | public | ` public MedicalCaseQueryService( IMedicalCaseRepository repository, ILogger<MedicalCaseQueryService> logger)` | MedicalCaseQueryService |
| 26 | public | ` public virtual async Task<PagedResult<MedicalCaseListDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null, CancellationToken ct = d…` | MedicalCaseQueryService |
| 38 | public | ` public virtual async Task<PagedResult<MedicalCaseListDto>?> QueryAsync(MedicalCaseQueryDto query, CancellationToken ct = default)` | MedicalCaseQueryService |
| 50 | public | ` public virtual async Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false, Cance…` | MedicalCaseQueryService |
| 79 | public | ` public virtual async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid? patientId = null, CancellationToken ct = default)` | MedicalCaseQueryService |
| 29 | public | ` public MedicalCaseService( IMedicalCaseRepository repository, IMedicalCaseQueryService queryService, IMedicalCaseCommandService commandService, IMedi…` | MedicalCaseService |
| 49 | public | ` public virtual async Task<PagedResult<MedicalCaseListDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null, CancellationToken ct = d…` | MedicalCaseService |
| 52 | public | ` public virtual async Task<PagedResult<MedicalCaseListDto>?> QueryAsync(MedicalCaseQueryDto query, CancellationToken ct = default)` | MedicalCaseService |
| 55 | public | ` public virtual async Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false, Cance…` | MedicalCaseService |
| 58 | public | ` public virtual async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid? patientId = null, CancellationToken ct = default)` | MedicalCaseService |
| 68 | public | ` public virtual async Task<bool> SaveAsync(CancellationToken ct = default)` | MedicalCaseService |
| 71 | public | ` public virtual async Task<bool> DeleteAsync(CancellationToken ct = default)` | MedicalCaseService |
| 85 | public | ` public async Task InitializeAsync(Guid entityId, CancellationToken ct = default)` | MedicalCaseService |
| 88 | public | ` public virtual async Task ReloadAsync(CancellationToken ct = default)` | MedicalCaseService |
| 103 | public | ` public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default)` | MedicalCaseService |
| 141 | public | ` public void ClearCache()` | MedicalCaseService |
| 240 | public | ` public virtual async Task<MedicalCaseDetailDto?> GetByIdSimpleAsync(Guid id)` | MedicalCaseService |
| 253 | public | ` public virtual async Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request)` | MedicalCaseService |
| 275 | public | ` public virtual async Task<ApiResponse> DeleteMedicalCaseAsync(Guid medicalCaseId)` | MedicalCaseService |
| 288 | public | ` public virtual async Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid medicalCaseId, MedicalCaseStatusInputDto request)` | MedicalCaseService |
| 309 | public | ` public virtual async Task<ApiResponse<MedicalCaseDetailDto>> SuspendViaApiAsync(Guid medicalCaseId, ConsultationInputDto? consultationData = null)` | MedicalCaseService |
| 330 | public | ` public virtual async Task<ApiResponse<MedicalCaseDetailDto>> CancelMedicalCaseViaApiAsync(Guid medicalCaseId, string? reason = null)` | MedicalCaseService |
| 30 | public | ` public AuditLogViewModel(IViewModelServices services, IAuditLogService auditLogService, INavigationCoordinator navigationCoordinator)` | AuditLogViewModel |
| 38 | public | ` public override void OnNavigatedTo(NavigationContext navigationContext)` | AuditLogViewModel |
| 49 | private | ` private async Task LoadLogsAsync()` | AuditLogViewModel |
| 76 | private | ` private async Task PreviousPageAsync()` | AuditLogViewModel |
| 85 | private | ` private async Task NextPageAsync()` | AuditLogViewModel |
| 94 | private | ` private void GoBack() => _navigationCoordinator.NavigateBack();` | AuditLogViewModel |
| 67 | public | ` public EditModeStateMachine(ILogger<EditModeStateMachine> logger)` | EditModeStateMachine |
| 73 | internal | ` internal EditModeStateMachine(ILogger<EditModeStateMachine> logger, WorkspaceEditState initialState)` | EditModeStateMachine |
| 80 | public | ` public void Initialize(WorkspaceEditState initialState, Func<WorkspaceEditEvent, bool>? guardPredicate = null)` | EditModeStateMachine |
| 93 | public | ` public bool CanFire(WorkspaceEditEvent evt)` | EditModeStateMachine |
| 103 | public | ` public bool Fire(WorkspaceEditEvent evt, string? context = null)` | EditModeStateMachine |
| 170 | public | ` public IEnumerable<WorkspaceEditEvent> GetPermittedEvents()` | EditModeStateMachine |
| 35 | public | ` public PrescriptionPrintHandler( IMedicalCaseService medicalCaseService, IMedicalCaseRepository repository, ISessionManager sessionManager, IClinicSe…` | PrescriptionPrintHandler |
| 63 | public | ` public async Task<PrintResult> PrintPreviewAsync( Guid medicalCaseId, IDataProvider? prescriptionProvider, PatientDetailDto? currentPatient, Consulta…` | PrescriptionPrintHandler |
| 111 | public | ` public async Task<PrintResult> ExportPdfAsync( Guid medicalCaseId, IDataProvider? prescriptionProvider, PatientDetailDto? currentPatient, Consultatio…` | PrescriptionPrintHandler |
| 165 | private | ` private PrescriptionPrintModel BuildPrintModel( PrescriptionDetailDto prescription, PatientDetailDto? patient, ConsultationInputDto? consultation)` | PrescriptionPrintHandler |
| 246 | private | ` private static int CalculateAge(DateTime? birthDate)` | PrescriptionPrintHandler |
| 261 | public | ` public PrescriptionDetailDto? BuildPrescriptionDetailDto( Guid medicalCaseId, IDataProvider? prescriptionProvider)` | PrescriptionPrintHandler |
| 321 | private | ` private PrintResult() { }` | PrintResult |
| 323 | public | ` public static PrintResult Success() => new() { IsSuccess = true };` | PrintResult |
| 324 | public | ` public static PrintResult Failed(string errorMessage) => new() { IsSuccess = false, ErrorMessage = errorMessage };` | PrintResult |
| 47 | protected | ` protected override string? GetDetailDisplayName() => CurrentDetail?.PatientName;` | MedicalCaseMasterDetailViewModel |
| 58 | public | ` public MedicalCaseMasterDetailViewModel( IViewModelServices viewModelServices, IMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel> mast…` | MedicalCaseMasterDetailViewModel |
| 86 | protected | ` protected override async Task LoadListAsync()` | MedicalCaseMasterDetailViewModel |
| 113 | protected | ` protected override async Task LoadDetailAsync(MedicalCaseListDto item)` | MedicalCaseMasterDetailViewModel |
| 143 | protected | ` protected override MedicalCaseDetailModel CreateNewDetail()` | MedicalCaseMasterDetailViewModel |
| 150 | protected | ` protected override async Task<bool> SaveDetailAsync(MedicalCaseDetailModel detail)` | MedicalCaseMasterDetailViewModel |
| 183 | protected | ` protected override async Task<bool> DeleteItemAsync(MedicalCaseListDto item)` | MedicalCaseMasterDetailViewModel |
| 206 | private | ` private async Task LoadHerbsAsync()` | MedicalCaseMasterDetailViewModel |
| 228 | protected | ` protected override async Task OnNavigatedToAsync(NavigationContext navigationContext)` | MedicalCaseMasterDetailViewModel |
| 249 | public | ` public MasterDetailWorkspaceHost(MedicalCaseMasterDetailViewModel parent)` | MasterDetailWorkspaceHost |
| 256 | public | ` public void NotifyStateChanged()` | MasterDetailWorkspaceHost |
| 261 | public | ` public void SetBusy(bool isBusy, string? message = null)` | MasterDetailWorkspaceHost |
| 267 | public | ` public Task ShowErrorAsync(string message)` | MasterDetailWorkspaceHost |
| 273 | public | ` public Task ShowSuccessAsync(string message)` | MasterDetailWorkspaceHost |
| 280 | public | ` public async Task<bool> ShowConfirmAsync(string message, string title = "确认")` | MasterDetailWorkspaceHost |
| 289 | public | ` public void RequestEnterEditMode()` | MasterDetailWorkspaceHost |
| 302 | public | ` public MasterDetailWorkspaceContext(MedicalCaseMasterDetailViewModel parent)` | MasterDetailWorkspaceContext |
| 28 | public | ` public ConsultationEditorViewModel( IMedicalCaseWorkspaceContext context, IWorkspaceHost host, ILoggerFactory loggerFactory)` | ConsultationEditorViewModel |
| 38 | public | ` public void InitializeFromDto(ConsultationDetailDto dto)` | ConsultationEditorViewModel |
| 46 | public | ` public void InitializeForNewCase(string patientName, Guid patientId, Guid userId)` | ConsultationEditorViewModel |
| 55 | public | ` public ConsultationInputDto? GetConsultationData() => Consultation.GetConsultationData();` | ConsultationEditorViewModel |
| 56 | public | ` public bool Validate() => Consultation.Validate();` | ConsultationEditorViewModel |
| 59 | public | ` public void Reset() => Consultation.Reset();` | ConsultationEditorViewModel |
| 60 | public | ` public MedicalCaseCommandsViewModel( IMedicalCaseWorkspaceContext context, IWorkspaceHost host, ILoggerFactory loggerFactory, IMedicalCaseService med…` | MedicalCaseCommandsViewModel |
| 93 | public | ` public void RefreshCanExecute()` | MedicalCaseCommandsViewModel |
| 115 | private | ` private async Task ExecuteSaveAsync()` | MedicalCaseCommandsViewModel |
| 149 | private | ` private async Task ExecuteSuspendAsync()` | MedicalCaseCommandsViewModel |
| 183 | private | ` private async Task ExecuteCompleteAsync()` | MedicalCaseCommandsViewModel |
| 220 | private | ` private async Task ExecutePrintAsync()` | MedicalCaseCommandsViewModel |
| 254 | private | ` private async Task ExecuteExportPdfAsync()` | MedicalCaseCommandsViewModel |
| 293 | private | ` private void ExecuteEnterEditMode()` | MedicalCaseCommandsViewModel |
| 302 | private | ` private void ExecuteImportFormula()` | MedicalCaseCommandsViewModel |
| 317 | private | ` private void ExecuteCopyHistory()` | MedicalCaseCommandsViewModel |
| 339 | private | ` private async Task ExecuteClearHerbsAsync()` | MedicalCaseCommandsViewModel |
| 363 | private | ` private Task HandleFormulaImportResultAsync(IDialogParameters parameters)` | MedicalCaseCommandsViewModel |
| 420 | private | ` private Task HandleHistoryCopyResultAsync(IDialogParameters parameters)` | MedicalCaseCommandsViewModel |
| 485 | private | ` private IReadOnlyDictionary<Guid, decimal>? BuildHerbPriceLookup()` | MedicalCaseCommandsViewModel |
| 495 | private | ` private IReadOnlyList<PrescriptionItemDto> FilterDisabledHerbs( IReadOnlyList<PrescriptionItemDto> items, string source)` | MedicalCaseCommandsViewModel |
| 44 | public | ` public PrescriptionEditorViewModel( IMedicalCaseWorkspaceContext context, IWorkspaceHost host, ILoggerFactory loggerFactory)` | PrescriptionEditorViewModel |
| 55 | public | ` public void InitializeFromDto(PrescriptionDetailDto dto)` | PrescriptionEditorViewModel |
| 63 | public | ` public void InitializeForNewCase()` | PrescriptionEditorViewModel |
| 69 | public | ` public PrescriptionInputDto? GetPrescriptionData() => Prescription.GetPrescriptionData();` | PrescriptionEditorViewModel |
| 70 | public | ` public bool Validate() => Prescription.Validate();` | PrescriptionEditorViewModel |
| 73 | public | ` public void Reset()` | PrescriptionEditorViewModel |
| 79 | public | ` public override void Dispose()` | PrescriptionEditorViewModel |
| 85 | private | ` private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)` | PrescriptionEditorViewModel |
| 7 | public | ` public AuditLogView() => InitializeComponent();` | AuditLogView |
| 12 | public | ` public MedicalCaseMasterDetailView()` | MedicalCaseMasterDetailView |
### LYBT.Desktop.Patients

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 18 | public | ` public PatientEditControl()` | PatientEditControl |
| 12 | public | ` public PatientMasterDetailControl()` | PatientMasterDetailControl |
| 28 | public | ` public PatientSelectionControl()` | PatientSelectionControl |
| 43 | private | ` private void PatientDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)` | PatientSelectionControl |
| 63 | private | ` private T? GetPropertyValue<T>(string propertyName)` | PatientSelectionControl |
| 11 | public | ` public PatientViewControl()` | PatientViewControl |
| 121 | public | ` public static PatientEditContext CreateNew()` | PatientEditContext |
| 163 | public | ` public void UpdateFromDto(PatientDetailDto dto)` | PatientItem |
| 135 | public | ` public static PatientDetailModel CreateNew()` | PatientDetailModel |
| 147 | public | ` public PatientDetailModel Clone()` | PatientDetailModel |
| 28 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | PatientsModule |
| 33 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | PatientsModule |
| 22 | public | ` public PatientRepository( IApiClient apiClient, ILogger<PatientRepository> logger)` | PatientRepository |
| 33 | public | ` public Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)` | PatientRepository |
| 38 | public | ` public async Task<List<PatientListDto>> SearchAsync(string keyword, CancellationToken ct = default)` | PatientRepository |
| 53 | public | ` public async Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)` | PatientRepository |
| 84 | public | ` public async Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)` | PatientRepository |
| 99 | public | ` public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)` | PatientRepository |
| 113 | public | ` public async Task<byte[]?> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default)` | PatientRepository |
| 131 | public | ` public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)` | PatientRepository |
| 21 | public | ` public PatientCardReaderIntegration( IPatientRepository patientRepository, ILogger<PatientCardReaderIntegration> logger, IPhotoStorageService? photoS…` | PatientCardReaderIntegration |
| 34 | public | ` public async Task<PatientFromCardResult?> FindPatientByIdNumberAsync(string idNumber)` | PatientCardReaderIntegration |
| 72 | public | ` public async Task<Guid> QuickCreatePatientAsync(CardReadResult cardResult)` | PatientCardReaderIntegration |
| 102 | public | ` public async Task<PatientFromCardResult> FindOrCreatePatientAsync(CardReadResult cardResult)` | PatientCardReaderIntegration |
| 148 | public | ` public async Task<PatientMatchResult> MatchPatientAsync(CardReadResult cardResult)` | PatientCardReaderIntegration |
| 232 | private | ` private async Task SaveEncryptedPhotoAsync(CardReadResult cardResult)` | PatientCardReaderIntegration |
| 257 | private | ` private static PatientInputDto MapCardResultToPatientInput(CardReadResult cardResult)` | PatientCardReaderIntegration |
| 273 | public | ` public async Task<PatientDetailDto?> GetPatientDetailByIdAsync(Guid patientId)` | PatientCardReaderIntegration |
| 21 | public | ` public PatientService( IPatientRepository patientRepository, ILogger<PatientService> logger)` | PatientService |
| 34 | public | ` public async Task<CommandResult<PatientDetailDto>> CreatePatientAsync(PatientInputDto inputDto, CancellationToken ct = default)` | PatientService |
| 54 | public | ` public async Task<CommandResult<PatientDetailDto>> UpdatePatientAsync(PatientInputDto inputDto, CancellationToken ct = default)` | PatientService |
| 74 | public | ` public async Task<CommandResult<bool>> DeletePatientAsync(Guid patientId, CancellationToken ct = default)` | PatientService |
| 98 | public | ` public async Task<CommandResult<BatchOperationResultDto>> BatchDeletePatientsAsync(IEnumerable<Guid> patientIds, CancellationToken ct = default)` | PatientService |
| 142 | public | ` public async Task<CommandResult<IEnumerable<PatientListDto>>> SearchPatientsAsync(string keyword, CancellationToken ct = default)` | PatientService |
| 162 | public | ` public async Task<CommandResult<PagedResult<PatientListDto>>> GetPatientsPagedAsync(int page, int pageSize, string? keyword = null, CancellationToken…` | PatientService |
| 182 | public | ` public async Task<CommandResult<PatientDetailDto>> GetByIdAsync(Guid patientId, CancellationToken ct = default)` | PatientService |
| 213 | public | ` public async Task<CommandResult<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)` | PatientService |
| 237 | public | ` public async Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default)` | PatientService |
| 260 | public | ` public async Task<CommandResult<byte[]>> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default)` | PatientService |
| 15 | public (interface 默认) | ` Task<bool> RestoreAsync(PatientListDto patient);` | IPatientStatusHandler |
| 17 | public | ` public PatientStatusHandler( IPatientRepository patientRepository, IMasterDetailServices<PatientListDto, PatientDetailModel> masterDetailServices, IL…` | PatientStatusHandler |
| 27 | protected | ` protected override Guid GetEntityId(PatientListDto e) => e.Id;` | PatientStatusHandler |
| 28 | protected | ` protected override string GetEntityDisplayName(PatientListDto e) => e.Name;` | PatientStatusHandler |
| 30 | protected | ` protected override Task<object?> ExecuteRestoreAsync(Guid id)` | PatientStatusHandler |
| 23 | public | ` public PatientCardReaderViewModel( IViewModelServices viewModelServices, ICardReaderService cardReaderService, IPatientCardReaderIntegration patientC…` | PatientCardReaderViewModel |
| 48 | public | ` public async Task<CardReadResult?> ReadCardAsync()` | PatientCardReaderViewModel |
| 86 | private | ` private bool CanReadCard() => !IsReadingCard;` | PatientCardReaderViewModel |
| 89 | public | ` public async Task<PatientFromCardResult?> FindPatientByIdNumberAsync(string idNumber)` | PatientCardReaderViewModel |
| 95 | public | ` public async Task<PatientFromCardResult> FindOrCreatePatientAsync(CardReadResult cardResult)` | PatientCardReaderViewModel |
| 101 | public | ` public static string MaskIdNumber(string? idNumber)` | PatientCardReaderViewModel |
| 36 | public | ` public void InitializeFromDto(PatientDetailDto dto)` | PatientEditorViewModel |
| 58 | public | ` public void InitializeForNewCase()` | PatientEditorViewModel |
| 68 | public | ` public PatientInputDto GetPatientData()` | PatientEditorViewModel |
| 83 | public | ` public bool Validate()` | PatientEditorViewModel |
| 89 | public | ` public void Reset()` | PatientEditorViewModel |
| 96 | private | ` private void OnPatientPropertyChanged(object? sender, PropertyChangedEventArgs e)` | PatientEditorViewModel |
| 45 | protected | ` protected override string? GetDetailDisplayName() => CurrentDetail?.Name;` | PatientMasterDetailViewModel |
| 75 | public | ` public PatientMasterDetailViewModel( IViewModelServices viewModelServices, IMasterDetailServices<PatientListDto, PatientDetailModel> masterDetailServ…` | PatientMasterDetailViewModel |
| 100 | protected | ` protected override async Task LoadListAsync()` | PatientMasterDetailViewModel |
| 130 | protected | ` protected override async Task LoadDetailAsync(PatientListDto item)` | PatientMasterDetailViewModel |
| 151 | protected | ` protected override PatientDetailModel CreateNewDetail()` | PatientMasterDetailViewModel |
| 158 | protected | ` protected override async Task<bool> SaveDetailAsync(PatientDetailModel detail)` | PatientMasterDetailViewModel |
| 208 | protected | ` protected override async Task<bool> DeleteItemAsync(PatientListDto item)` | PatientMasterDetailViewModel |
| 228 | protected | ` protected override async Task InvalidateCachesAsync()` | PatientMasterDetailViewModel |
| 235 | protected | ` protected override async Task RestoreItemAsync(PatientListDto item)` | PatientMasterDetailViewModel |
| 242 | private | ` private void ViewMedicalRecords()` | PatientMasterDetailViewModel |
| 250 | private | ` private bool CanViewMedicalRecords() => HasSelection;` | PatientMasterDetailViewModel |
| 254 | private | ` private void NewConsultation()` | PatientMasterDetailViewModel |
| 262 | private | ` private bool CanNewConsultation() => HasSelection;` | PatientMasterDetailViewModel |
| 270 | private | ` private async Task ReadCardAsync()` | PatientMasterDetailViewModel |
| 301 | private | ` private bool CanReadCard() => !_cardReaderViewModel.IsReadingCard;` | PatientMasterDetailViewModel |
| 304 | private | ` private async Task HandleNewPatientFromCardAsync(CardReadResult cardResult)` | PatientMasterDetailViewModel |
| 327 | private | ` private async Task SearchAndSelectPatientAsync(Guid patientId)` | PatientMasterDetailViewModel |
### LYBT.Desktop.Printing

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 16 | public (interface 默认) | ` Task<bool> PrintAsync(TModel model, PrintOptions? options = null);` | IPrintService |
| 23 | public (interface 默认) | ` Task PreviewAsync(TModel model, PrintOptions? options = null);` | IPrintService |
| 32 | public (interface 默认) | ` Task<bool> ExportAsync(TModel model, string filePath, ExportFormat format = ExportFormat.Xps);` | IPrintService |
| 40 | public (interface 默认) | ` Task<int> BatchPrintAsync(TModel[] models, PrintOptions? options = null);` | IPrintService |
| 46 | public (interface 默认) | ` string[] GetAvailablePrinters();` | IPrintService |
| 52 | public (interface 默认) | ` void SetDefaultPrinter(string printerName);` | IPrintService |
| 57 | public (interface 默认) | ` string? GetDefaultPrinter();` | IPrintService |
| 114 | public | ` public PrescriptionPrintModel CloneWithItems(List<PrescriptionItemPrintModel> items)` | PrescriptionPrintModel |
| 169 | private | ` private static string GetDecocteMethodDescription(DecocteMethod method)` | PrescriptionItemPrintModel |
| 16 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | PrintingModule |
| 21 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | PrintingModule |
| 29 | public | ` public PrescriptionDocumentBuilder(ILogger<PrescriptionDocumentBuilder> logger)` | PrescriptionDocumentBuilder |
| 37 | public | ` public static Size GetPageSize(PaperSize paperSize)` | PrescriptionDocumentBuilder |
| 50 | public | ` public static bool IsA4(Size pageSize) => pageSize.Width >= A4PageSize.Width;` | PrescriptionDocumentBuilder |
| 55 | public | ` public static int GetFirstPageHerbLimit(Size pageSize) =>` | PrescriptionDocumentBuilder |
| 56 | default(private) | ` IsA4(pageSize) ? A4FirstPageHerbLimit : A5FirstPageHerbLimit;` | PrescriptionDocumentBuilder |
| 62 | public | ` public FixedDocument BuildFixedDocument(PrescriptionPrintModel model, Size pageSize)` | PrescriptionDocumentBuilder |
| 93 | public | ` public void BuildMultiPageDocument(FixedDocument document, PrescriptionPrintModel model, Size pageSize)` | PrescriptionDocumentBuilder |
| 133 | public | ` public static PrescriptionPrintModel CloneModelWithItems( PrescriptionPrintModel source, List<PrescriptionItemPrintModel> items)` | PrescriptionDocumentBuilder |
| 143 | public | ` public FixedPage CreateFixedPage(PrescriptionPrintModel model, Size pageSize)` | PrescriptionDocumentBuilder |
| 156 | public | ` public FixedPage CreateContinuationFixedPage(PrescriptionPrintModel model, Size pageSize, bool isLastPage)` | PrescriptionDocumentBuilder |
| 188 | public | ` public static FixedPage CreatePageFromTemplate(UserControl template, Size pageSize)` | PrescriptionDocumentBuilder |
| 23 | public | ` public static void Export(PrescriptionPrintModel model, string filePath)` | PrescriptionPdfExporter |
| 87 | private | ` private static void ComposeClinicHeader(ColumnDescriptor col, PrescriptionPrintModel model)` | PrescriptionPdfExporter |
| 102 | private | ` private static void ComposePatientInfoRow1(ColumnDescriptor col, PrescriptionPrintModel model)` | PrescriptionPdfExporter |
| 114 | private | ` private static void ComposePatientInfoRow2(ColumnDescriptor col, PrescriptionPrintModel model)` | PrescriptionPdfExporter |
| 125 | private | ` private static void ComposeFieldRow(ColumnDescriptor col, string label, string value)` | PrescriptionPdfExporter |
| 136 | private | ` private static void ComposeFourDiagnosis(ColumnDescriptor col, PrescriptionPrintModel model)` | PrescriptionPdfExporter |
| 151 | private | ` private static void ComposePrescription(ColumnDescriptor col, PrescriptionPrintModel model)` | PrescriptionPdfExporter |
| 182 | private | ` private static void ComposeSignatureRow(ColumnDescriptor col, PrescriptionPrintModel model)` | PrescriptionPdfExporter |
| 193 | private | ` private static void ComposeFeeRow(ColumnDescriptor col, PrescriptionPrintModel model)` | PrescriptionPdfExporter |
| 207 | private | ` private static void LabelValue(RowDescriptor row, string label, string value, int relativeSize)` | PrescriptionPdfExporter |
| 21 | public | ` public PrescriptionPreviewWindowBuilder( ILogger<PrescriptionPreviewWindowBuilder> logger, PrescriptionDocumentBuilder documentBuilder, PrescriptionP…` | PrescriptionPreviewWindowBuilder |
| 34 | public | ` public void ShowPreviewWindow(FixedDocument document, PrescriptionPrintModel model, PrintOptions options)` | PrescriptionPreviewWindowBuilder |
| 71 | private | ` private Border CreateSettingsPanel( FixedDocument document, PrescriptionPrintModel model, PrintOptions options, Window parentWindow, DocumentViewer d…` | PrescriptionPreviewWindowBuilder |
| 201 | private | ` private void PopulatePrinterList(ComboBox printerComboBox)` | PrescriptionPreviewWindowBuilder |
| 20 | public | ` public PrescriptionPrintExecutor(ILogger<PrescriptionPrintExecutor> logger)` | PrescriptionPrintExecutor |
| 28 | public | ` public void Dispose()` | PrescriptionPrintExecutor |
| 34 | protected | ` protected virtual void Dispose(bool disposing)` | PrescriptionPrintExecutor |
| 50 | public | ` public PrintQueueCollection GetPrintQueues()` | PrescriptionPrintExecutor |
| 58 | public | ` public string[] GetAvailablePrinters()` | PrescriptionPrintExecutor |
| 79 | public | ` public void SetDefaultPrinter(string printerName)` | PrescriptionPrintExecutor |
| 91 | public | ` public string? GetDefaultPrinter()` | PrescriptionPrintExecutor |
| 99 | public | ` public bool ExecutePrintWithDialog(FixedDocument document, PrintOptions options)` | PrescriptionPrintExecutor |
| 118 | public | ` public bool ExecutePrintDirect(FixedDocument document, PrintOptions options)` | PrescriptionPrintExecutor |
| 149 | public | ` public void SetupPrinter(PrintDialog printDialog, PrintOptions options)` | PrescriptionPrintExecutor |
| 172 | public | ` public PrintQueue? GetPrintQueue(string? printerName)` | PrescriptionPrintExecutor |
| 22 | public | ` public PrescriptionPrintService( ILogger<PrescriptionPrintService> logger, PrescriptionDocumentBuilder documentBuilder, PrescriptionPrintExecutor exe…` | PrescriptionPrintService |
| 37 | public | ` public async Task<bool> PrintAsync(PrescriptionPrintModel model, PrintOptions? options = null)` | PrescriptionPrintService |
| 85 | public | ` public async Task PreviewAsync(PrescriptionPrintModel model, PrintOptions? options = null)` | PrescriptionPrintService |
| 117 | public | ` public async Task<bool> ExportAsync(PrescriptionPrintModel model, string filePath, ExportFormat format = ExportFormat.Xps)` | PrescriptionPrintService |
| 173 | public | ` public async Task<int> BatchPrintAsync(PrescriptionPrintModel[] models, PrintOptions? options = null)` | PrescriptionPrintService |
| 207 | public | ` public string[] GetAvailablePrinters()` | PrescriptionPrintService |
| 215 | public | ` public void SetDefaultPrinter(string printerName)` | PrescriptionPrintService |
| 223 | public | ` public string? GetDefaultPrinter()` | PrescriptionPrintService |
| 12 | public | ` public PrescriptionContinuationA4Template()` | PrescriptionContinuationA4Template |
| 20 | public | ` public void SetAsLastPage()` | PrescriptionContinuationA4Template |
| 12 | public | ` public PrescriptionContinuationTemplate()` | PrescriptionContinuationTemplate |
| 20 | public | ` public void SetAsLastPage()` | PrescriptionContinuationTemplate |
| 11 | public | ` public PrescriptionPrintA4Template()` | PrescriptionPrintA4Template |
| 10 | public | ` public PrescriptionPrintTemplate()` | PrescriptionPrintTemplate |
### LYBT.Desktop.Registrations

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 10 | public | ` public RegistrationCreateDialog()` | RegistrationCreateDialog |
| 57 | public | ` public RegistrationCreateDialogViewModel( IViewModelServices services, IPatientService patientService, IUserService userService, IRegistrationService…` | RegistrationCreateDialogViewModel |
| 70 | protected | ` protected override void OnDialogOpenedCore(IDialogParameters? parameters)` | RegistrationCreateDialogViewModel |
| 76 | protected | ` protected override bool CanConfirm() =>` | RegistrationCreateDialogViewModel |
| 82 | protected | ` protected override void Confirm()` | RegistrationCreateDialogViewModel |
| 90 | private | ` private async Task ConfirmAsync()` | RegistrationCreateDialogViewModel |
| 134 | private | ` private async Task SearchPatientsAsync()` | RegistrationCreateDialogViewModel |
| 178 | private | ` private void SelectPatient(PatientListDto patient)` | RegistrationCreateDialogViewModel |
| 189 | private | ` private void ClearPatientSelection()` | RegistrationCreateDialogViewModel |
| 198 | default(private) | ` partial void OnPatientSearchTextChanged(string value)` | RegistrationCreateDialogViewModel |
| 207 | default(private) | ` partial void OnSelectedPatientChanged(PatientListDto? value)` | RegistrationCreateDialogViewModel |
| 212 | default(private) | ` partial void OnSelectedDoctorChanged(UserListDto? value)` | RegistrationCreateDialogViewModel |
| 219 | private | ` private async Task LoadDoctorsAsync()` | RegistrationCreateDialogViewModel |
| 22 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | RegistrationModule |
| 28 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | RegistrationModule |
| 17 | public | ` public RegistrationRepository( IApiClient apiClient, ILogger<RegistrationRepository> logger)` | RegistrationRepository |
| 28 | public | ` public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input, CancellationToken ct = default)` | RegistrationRepository |
| 47 | public | ` public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)` | RegistrationRepository |
| 59 | public | ` public async Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null, CancellationToken ct = default)` | RegistrationRepository |
| 76 | public | ` public async Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null, CancellationToken ct = default)` | RegistrationRepository |
| 94 | public | ` public async Task<Guid?> StartVisitAsync(Guid id, CancellationToken ct = default)` | RegistrationRepository |
| 116 | public | ` public async Task CancelAsync(Guid id, CancellationToken ct = default)` | RegistrationRepository |
| 21 | public | ` public RemoteRegistrationService( IRegistrationRepository registrationRepository, ILogger<RemoteRegistrationService> logger)` | RemoteRegistrationService |
| 33 | public | ` public async Task<CommandResult<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request, CancellationToken ct = default)` | RemoteRegistrationService |
| 53 | public | ` public async Task<CommandResult<RegistrationDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)` | RemoteRegistrationService |
| 75 | public | ` public async Task<CommandResult<PagedResult<RegistrationListDto>>> GetPagedAsync( int page = 1, int pageSize = 20, string? keyword = null, Cancellati…` | RemoteRegistrationService |
| 100 | public | ` public async Task<CommandResult<List<RegistrationListDto>>> GetQueueAsync( Guid? doctorId = null, CancellationToken ct = default)` | RemoteRegistrationService |
| 122 | public | ` public async Task<CommandResult<Guid>> StartVisitAsync(Guid id, CancellationToken ct = default)` | RemoteRegistrationService |
| 151 | public | ` public async Task<CommandResult> CancelAsync(Guid id, CancellationToken ct = default)` | RemoteRegistrationService |
| 17 | public (interface 默认) | ` Task StartAsync(Guid doctorId, CancellationToken cancellationToken = default);` | ISignalRClient |
| 20 | public (interface 默认) | ` Task StopAsync();` | ISignalRClient |
| 38 | public | ` public SignalRClient( ITokenManager tokenManager, IApplicationStateService applicationState, IEventAggregator eventAggregator, ILogger<SignalRClient>…` | SignalRClient |
| 51 | public | ` public async Task StartAsync(Guid doctorId, CancellationToken cancellationToken = default)` | SignalRClient |
| 83 | public | ` public async Task StopAsync()` | SignalRClient |
| 104 | private | ` private Uri BuildHubUrl(Guid doctorId)` | SignalRClient |
| 110 | private | ` private void OnNotificationReceived()` | SignalRClient |
| 116 | private | ` private Task OnReconnectedAsync(string? _)` | SignalRClient |
| 123 | private | ` private Task OnClosedAsync(Exception? exception)` | SignalRClient |
| 133 | private | ` private void StartPolling()` | SignalRClient |
| 142 | private | ` private void StopPolling()` | SignalRClient |
| 149 | private | ` private async Task PollLoopAsync(CancellationToken cancellationToken)` | SignalRClient |
| 77 | public | ` public RegistrationListViewModel( IViewModelServices services, IRegistrationService registrationService, INavigationCoordinator navigationCoordinator…` | RegistrationListViewModel |
| 105 | protected | ` protected override async Task InitializeAsync(NavigationContext context)` | RegistrationListViewModel |
| 118 | protected | ` protected override void OnNavigatedToCore(NavigationContext context)` | RegistrationListViewModel |
| 128 | protected | ` protected override void OnNavigatedFromCore(NavigationContext context)` | RegistrationListViewModel |
| 134 | private | ` private void StartAutoRefresh()` | RegistrationListViewModel |
| 142 | private | ` private void StopAutoRefresh()` | RegistrationListViewModel |
| 149 | private | ` private async Task RunAutoRefreshLoopAsync(CancellationToken ct)` | RegistrationListViewModel |
| 160 | private | ` private void OnRegistrationRefreshed()` | RegistrationListViewModel |
| 172 | private | ` private async Task RefreshAsync()` | RegistrationListViewModel |
| 179 | private | ` private void CreateRegistration()` | RegistrationListViewModel |
| 202 | private | ` private async Task StartVisitAsync()` | RegistrationListViewModel |
| 255 | private | ` private bool CanStartVisit() =>` | RegistrationListViewModel |
| 263 | private | ` private async Task CancelRegistrationAsync()` | RegistrationListViewModel |
| 298 | private | ` private bool CanCancelRegistration() =>` | RegistrationListViewModel |
| 307 | private | ` private async Task LoadQueueAsync()` | RegistrationListViewModel |
| 345 | default(private) | ` partial void OnSelectedRegistrationChanged(RegistrationListDto? value)` | RegistrationListViewModel |
| 11 | public | ` public RegistrationListView()` | RegistrationListView |
### LYBT.Desktop.Shell

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 38 | protected | ` protected override void OnStartup(StartupEventArgs e)` | App |
| 56 | private | ` private static bool TryAcquireSingleInstance()` | App |
| 69 | protected | ` protected override void OnExit(ExitEventArgs e)` | App |
| 80 | protected | ` protected override Window CreateShell() => Container.Resolve<MainWindow>();` | App |
| 83 | protected | ` protected override void InitializeShell(Window shell)` | App |
| 90 | protected | ` protected override void RegisterTypes(IContainerRegistry containerRegistry)` | App |
| 116 | protected | ` protected override void ConfigureViewModelLocator()` | App |
| 124 | protected | ` protected override void OnInitialized()` | App |
| 132 | protected | ` protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)` | App |
| 164 | private | ` private static void SetConsoleEncoding()` | App |
| 183 | private | ` private static bool HasConsole()` | App |
| 14 | public | ` public AccountSettingsControl()` | AccountSettingsControl |
| 23 | private | ` private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)` | AccountSettingsControl |
| 35 | private | ` private void OnOldPasswordChanged(object sender, RoutedEventArgs e)` | AccountSettingsControl |
| 44 | private | ` private void OnNewPasswordChanged(object sender, RoutedEventArgs e)` | AccountSettingsControl |
| 53 | private | ` private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)` | AccountSettingsControl |
| 86 | public | ` public ConfirmationDialogViewModel(IViewModelServices services)` | ConfirmationDialogViewModel |
| 99 | protected | ` protected override void OnDialogOpenedCore(IDialogParameters? parameters)` | ConfirmationDialogViewModel |
| 118 | protected | ` protected override void OnDialogClosedCore()` | ConfirmationDialogViewModel |
| 130 | protected | ` protected override void Confirm()` | ConfirmationDialogViewModel |
| 147 | protected | ` protected override void Cancel()` | ConfirmationDialogViewModel |
| 62 | public | ` public InputDialogViewModel(IViewModelServices services)` | InputDialogViewModel |
| 75 | protected | ` protected override void OnDialogOpenedCore(IDialogParameters? parameters)` | InputDialogViewModel |
| 92 | protected | ` protected override void OnDialogClosedCore()` | InputDialogViewModel |
| 104 | protected | ` protected override bool CanConfirm()` | InputDialogViewModel |
| 120 | protected | ` protected override void Confirm()` | InputDialogViewModel |
| 138 | default(private) | ` partial void OnInputValueChanged(string value)` | InputDialogViewModel |
| 90 | public | ` public MessageDialogViewModel(IViewModelServices services)` | MessageDialogViewModel |
| 103 | protected | ` protected override void OnDialogOpenedCore(IDialogParameters? parameters)` | MessageDialogViewModel |
| 131 | protected | ` protected override void OnDialogClosedCore()` | MessageDialogViewModel |
| 143 | private | ` private static MessageType ParseMessageType(string type)` | MessageDialogViewModel |
| 158 | private | ` private string GetDefaultTitle()` | MessageDialogViewModel |
| 177 | protected | ` protected override void Confirm()` | MessageDialogViewModel |
| 11 | public | ` public ConfirmationDialog()` | ConfirmationDialog |
| 10 | public | ` public InputDialog()` | InputDialog |
| 10 | public | ` public MessageDialog()` | MessageDialog |
| 28 | public | ` public static void RegisterRepositories( this IContainerRegistry containerRegistry, IConfiguration? configuration = null)` | DataSourceRegistrationExtensions |
| 43 | private | ` private static void RegisterRemoteRepositories(IContainerRegistry containerRegistry)` | DataSourceRegistrationExtensions |
| 11 | public | ` public static void RegisterLogging(this IContainerRegistry containerRegistry)` | LoggingRegistrationExtensions |
| 21 | public | ` public static void AddLybtClientConfiguration( this IContainerRegistry containerRegistry, IConfiguration configuration)` | PrismConfigurationExtensions |
| 53 | private | ` private static void RegisterOptions<TOptions>( IContainerRegistry containerRegistry, IConfiguration configuration, string sectionName) where TOptions…` | PrismConfigurationExtensions |
| 46 | public | ` public static void RegisterAllServices(this IContainerRegistry containerRegistry)` | ServiceCollectionExtensions |
| 64 | private | ` private static IConfiguration RegisterConfiguration(IContainerRegistry containerRegistry)` | ServiceCollectionExtensions |
| 80 | private | ` private static void RegisterCacheServices(IContainerRegistry containerRegistry)` | ServiceCollectionExtensions |
| 90 | private | ` private static void RegisterFoundationServices(IContainerRegistry containerRegistry)` | ServiceCollectionExtensions |
| 108 | private | ` private static void RegisterPresentationServices(IContainerRegistry containerRegistry)` | ServiceCollectionExtensions |
| 129 | private | ` private static void RegisterInfrastructureServices(IContainerRegistry containerRegistry)` | ServiceCollectionExtensions |
| 171 | private | ` private static void RegisterCommandServices(IContainerRegistry containerRegistry)` | ServiceCollectionExtensions |
| 178 | private | ` private static void RegisterApplicationServices(IContainerRegistry containerRegistry)` | ServiceCollectionExtensions |
| 36 | public | ` public static void AddUnifiedApiClient( this IContainerRegistry containerRegistry, IConfiguration configuration)` | UnifiedApiClientExtensions |
| 122 | public | ` public LocalWebApiHttpClientFactory(Uri baseAddress)` | LocalWebApiHttpClientFactory |
| 131 | public | ` public HttpClient CreateClient(string name) => _httpClient;` | LocalWebApiHttpClientFactory |
| 133 | public | ` public void Dispose()` | LocalWebApiHttpClientFactory |
| 29 | public | ` public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);` | NativeMethods |
| 38 | public | ` public static extern bool SetForegroundWindow(IntPtr hWnd);` | NativeMethods |
| 48 | public | ` public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);` | NativeMethods |
| 57 | public | ` public static extern bool IsIconic(IntPtr hWnd);` | NativeMethods |
| 64 | public | ` public static bool ActivateExistingWindow(string windowTitle)` | NativeMethods |
| 83 | public | ` public static extern IntPtr GetConsoleWindow();` | NativeMethods |
| 89 | public | ` public static extern bool SetConsoleOutputCP(uint wCodePageID);` | NativeMethods |
| 95 | public | ` public static extern bool SetConsoleCP(uint wCodePageID);` | NativeMethods |
| 32 | internal | ` internal StringResources() {` | StringResources |
| 19 | public | ` public AppStartupOrchestrator(IContainerProvider container, ILogger<AppStartupOrchestrator> logger)` | AppStartupOrchestrator |
| 25 | public | ` public async Task RunStartupAsync()` | AppStartupOrchestrator |
| 63 | private | ` private void RegisterSteps(IStartupPipeline pipeline)` | AppStartupOrchestrator |
| 23 | public | ` public ApplicationBootstrapper( IModuleManager moduleManager, IRoleRegistry roleRegistry, IPerformanceMonitor performanceMonitor, ILogger<Application…` | ApplicationBootstrapper |
| 36 | public | ` public Task LoadModulesForRoleAsync(UserRole userRole)` | ApplicationBootstrapper |
| 16 | public (interface 默认) | ` Task LoadModulesForRoleAsync(UserRole userRole);` | IApplicationBootstrapper |
| 27 | public | ` public EmbeddedLocalWebApiService( ILogger<EmbeddedLocalWebApiService> logger, IOptions<OfflineModeOptions> offlineModeOptions, IOptions<DefaultPassw…` | EmbeddedLocalWebApiService |
| 40 | public | ` public async Task StartAsync(CancellationToken cancellationToken = default)` | EmbeddedLocalWebApiService |
| 78 | public | ` public async Task StopAsync(CancellationToken cancellationToken = default)` | EmbeddedLocalWebApiService |
| 102 | public | ` public void Dispose()` | EmbeddedLocalWebApiService |
| 33 | public | ` public ApiHealthMonitor( IApiHealthCheckService healthCheckService, ILogger<ApiHealthMonitor> logger)` | ApiHealthMonitor |
| 57 | public | ` public Task StartMonitoringAsync(CancellationToken ct = default)` | ApiHealthMonitor |
| 75 | public | ` public Task StopMonitoringAsync()` | ApiHealthMonitor |
| 95 | public | ` public async Task<ApiMonitorHealthStatus> ForceCheckAsync()` | ApiHealthMonitor |
| 103 | public | ` public void ResetCircuitBreaker()` | ApiHealthMonitor |
| 111 | private | ` private async Task<ApiMonitorHealthStatus> PerformCheckAsync()` | ApiHealthMonitor |
| 199 | private | ` private void OnSuccess()` | ApiHealthMonitor |
| 214 | private | ` private void OnFailure(string errorMessage)` | ApiHealthMonitor |
| 238 | private | ` private void UpdateState(ApiMonitorHealthStatus newStatus, ApiConnectionState newConnectionState, string? error)` | ApiHealthMonitor |
| 264 | public | ` public void Dispose()` | ApiHealthMonitor |
| 14 | public (interface 默认) | ` ObservableCollection<NavigationItem> BuildNavigationItems(UserRole role);` | INavigationManager |
| 20 | public (interface 默认) | ` void UpdateTime();` | IStatusBarManager |
| 21 | public (interface 默认) | ` Task ForceCheckAsync();` | IStatusBarManager |
| 22 | public (interface 默认) | ` void Dispose();` | IStatusBarManager |
| 6 | public (interface 默认) | ` void ToggleTheme();` | IThemeService |
| 7 | public (interface 默认) | ` void ApplyTheme(bool isDark);` | IThemeService |
| 46 | public (interface 默认) | ` void ApplyLoginSuccess(UserDetailDto user);` | ILoginStateManager |
| 49 | public (interface 默认) | ` void ApplyPasswordChanged();` | ILoginStateManager |
| 52 | public (interface 默认) | ` void ApplyProfileUpdate(UserDetailDto updatedUser);` | ILoginStateManager |
| 55 | public (interface 默认) | ` Task PerformLogoutAsync();` | ILoginStateManager |
| 58 | public (interface 默认) | ` Task HandleTokenExpiredAsync();` | ILoginStateManager |
| 61 | public (interface 默认) | ` Task HandleSessionExpiredAsync();` | ILoginStateManager |
| 40 | public | ` public LoginCoordinator( ILogger<LoginCoordinator> logger, IAuthenticationService authenticationService, ITokenStorageService tokenStorageService, IS…` | LoginCoordinator |
| 90 | public | ` public async Task<CommandResult<UserDetailDto>> LoginAsync(string username, string password)` | LoginCoordinator |
| 142 | private | ` private async Task<CommandResult<UserDetailDto>> CompleteLoginFlowAsync(UserDetailDto user, DateTime tokenExpiresAt)` | LoginCoordinator |
| 164 | private | ` private void RaiseLoginSucceeded(UserDetailDto user, DateTime tokenExpiresAt)` | LoginCoordinator |
| 183 | public | ` public async Task HandleLoginSuccessAsync(UserDetailDto user, DateTime tokenExpiresAt)` | LoginCoordinator |
| 226 | public | ` public async Task LogoutAsync()` | LoginCoordinator |
| 271 | public | ` public LoginFlowDiagnostics GetDiagnostics()` | LoginCoordinator |
| 287 | private | ` private void OnStateMachineStateChanged(object? sender, AuthStateChangedEventArgs e)` | LoginCoordinator |
| 297 | private | ` private async Task StartSessionAsync(UserDetailDto user, DateTime tokenExpiresAt)` | LoginCoordinator |
| 311 | private | ` private async Task LoadModulesForUserAsync(UserDetailDto user)` | LoginCoordinator |
| 318 | private | ` private Task NavigateToRoleHomeAsync(UserDetailDto user)` | LoginCoordinator |
| 352 | public | ` public void Dispose()` | LoginCoordinator |
| 57 | public | ` public LoginStateManager( IUserActivityTracker userActivityTracker, ITokenLifecycleService tokenLifecycleService, ILoginCoordinator loginCoordinator,…` | LoginStateManager |
| 73 | public | ` public void ApplyLoginSuccess(UserDetailDto user)` | LoginStateManager |
| 86 | public | ` public void ApplyPasswordChanged()` | LoginStateManager |
| 95 | public | ` public void ApplyProfileUpdate(UserDetailDto updatedUser)` | LoginStateManager |
| 102 | public | ` public async Task PerformLogoutAsync()` | LoginStateManager |
| 131 | public | ` public async Task HandleTokenExpiredAsync()` | LoginStateManager |
| 142 | public | ` public async Task HandleSessionExpiredAsync()` | LoginStateManager |
| 152 | public | ` public void Dispose()` | LoginStateManager |
| 29 | public | ` public MenuManager( INavigationCoordinator navigationCoordinator, ISessionManager sessionManager, IRoleRegistry roleRegistry, ILogger<MenuManager> lo…` | MenuManager |
| 102 | private | ` private void InitializeCommands()` | MenuManager |
| 118 | private | ` private void ExecuteAccountSettings()` | MenuManager |
| 123 | private | ` private void ExecuteNavigateToHome()` | MenuManager |
| 129 | private | ` private void ExecuteNavigateToSystemSettings()` | MenuManager |
| 136 | private | ` private void ExecuteNavigateBack()` | MenuManager |
| 144 | private | ` private void ExecuteNavigateForward()` | MenuManager |
| 151 | private | ` private void RaiseNavigationCanExecuteChanged()` | MenuManager |
| 158 | private | ` private async Task ExecuteQuickAddPatientAsync()` | MenuManager |
| 169 | private | ` private async Task ExecuteQuickStartMedicalCaseAsync()` | MenuManager |
| 180 | private | ` private void ExecuteShowHelp()` | MenuManager |
| 187 | private | ` private void ExecuteShowSettings() => _ = _userNotificationService.ShowSuccessAsync("用户设置功能将在未来版本中实现");` | MenuManager |
| 190 | private | ` private async Task ExecuteToggleThemeAsync()` | MenuManager |
| 32 | public | ` public NavigationManager( INavigationCoordinator navigationCoordinator, ILogger<NavigationManager> logger, IRoleRegistry roleRegistry)` | NavigationManager |
| 42 | default(private) | ` partial void OnSelectedNavItemChanged(NavigationItem? value)` | NavigationManager |
| 50 | public | ` public ObservableCollection<NavigationItem> BuildNavigationItems(UserRole role)` | NavigationManager |
| 117 | private | ` private NavigationItem CreateNavItem(string title, string viewName, string iconKind, string group = "业务") =>` | NavigationManager |
| 78 | public (interface 默认) | ` Task StartSessionAsync(string userName, string userRole, DateTime tokenExpiresAt);` | ISessionLifecycleManager |
| 83 | public (interface 默认) | ` Task EndSessionAsync();` | ISessionLifecycleManager |
| 89 | public (interface 默认) | ` Task<bool> RefreshTokenAsync();` | ISessionLifecycleManager |
| 95 | public (interface 默认) | ` void UpdateTokenExpiration(DateTime newExpiresAt);` | ISessionLifecycleManager |
| 100 | public (interface 默认) | ` void RecordUserActivity();` | ISessionLifecycleManager |
| 105 | public (interface 默认) | ` SessionDiagnostics GetDiagnostics();` | ISessionLifecycleManager |
| 128 | public | ` public SessionStateChangedEventArgs(SessionState previousState, SessionState currentState)` | SessionStateChangedEventArgs |
| 9 | public | ` public SessionBasedCurrentUserProvider(ISessionManager sessionManager)` | SessionBasedCurrentUserProvider |
| 32 | public | ` public SessionLifecycleManager( ILogger<SessionLifecycleManager> logger, ITokenLifecycleService tokenLifecycleService, IUserActivityTracker userActiv…` | SessionLifecycleManager |
| 111 | public | ` public Task StartSessionAsync(string userName, string userRole, DateTime tokenExpiresAt)` | SessionLifecycleManager |
| 144 | public | ` public Task EndSessionAsync()` | SessionLifecycleManager |
| 173 | public | ` public async Task<bool> RefreshTokenAsync()` | SessionLifecycleManager |
| 214 | public | ` public void UpdateTokenExpiration(DateTime newExpiresAt)` | SessionLifecycleManager |
| 226 | public | ` public void RecordUserActivity()` | SessionLifecycleManager |
| 237 | public | ` public SessionDiagnostics GetDiagnostics()` | SessionLifecycleManager |
| 258 | private | ` private void TransitionTo(SessionState newState)` | SessionLifecycleManager |
| 280 | private | ` private void OnTokenLifecycleStateChanged(TokenLifecycleStateChangedEventArgs args)` | SessionLifecycleManager |
| 312 | private | ` private void OnUserActivitySessionExpired(object? sender, EventArgs e)` | SessionLifecycleManager |
| 323 | public | ` public void Dispose()` | SessionLifecycleManager |
| 15 | public | ` public ShellDialogHelper( ICommonDialogService? dialogService, IToastService? toastService, ILogger<ShellDialogHelper> logger)` | ShellDialogHelper |
| 25 | public | ` public async Task ShowSuccessMessageAsync(string message)` | ShellDialogHelper |
| 33 | public | ` public async Task ShowErrorMessageAsync(string message)` | ShellDialogHelper |
| 41 | public | ` public async Task ShowWarningMessageAsync(string message)` | ShellDialogHelper |
| 49 | public | ` public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")` | ShellDialogHelper |
| 31 | public | ` public ShellEventCoordinator( IShellEventServices services, IEventAggregator eventAggregator, ILogger<ShellEventCoordinator> logger)` | ShellEventCoordinator |
| 44 | private | ` private void SubscribeToEvents()` | ShellEventCoordinator |
| 56 | private | ` private void OnLoginSucceeded(object? sender, LoginSuccessEventArgs args)` | ShellEventCoordinator |
| 102 | private | ` private void OnSessionExpired(object? sender, EventArgs e)` | ShellEventCoordinator |
| 115 | private | ` private void OnPasswordChanged(PasswordChangedPayload payload)` | ShellEventCoordinator |
| 143 | private | ` private void OnProfileUpdated(ProfileUpdatedPayload payload)` | ShellEventCoordinator |
| 168 | private | ` private void RaiseHandled(EventHandler? handled)` | ShellEventCoordinator |
| 186 | private | ` private async Task OnTokenLifecycleStateChanged(TokenLifecycleStateChangedEventArgs args)` | ShellEventCoordinator |
| 206 | private | ` private void OnLogoutRequested(object? sender, EventArgs e)` | ShellEventCoordinator |
| 214 | public | ` public void Dispose()` | ShellEventCoordinator |
| 11 | public | ` public ShellEventServices( ILoginStateManager loginState, IUserActivityTracker activityTracker, ILoginCoordinator loginCoordinator, ITokenLifecycleSe…` | ShellEventServices |
| 9 | public | ` public ShellServices( IMenuManager menu, INavigationManager navigation, IStatusBarManager statusBar, ILoginStateManager loginState, ShellEventCoordin…` | ShellServices |
| 27 | public | ` public StartupPipeline( ILogger<StartupPipeline> logger, IPerformanceMonitor performanceMonitor)` | StartupPipeline |
| 57 | public | ` public void RegisterStep(IStartupStep step)` | StartupPipeline |
| 81 | public | ` public async Task<StartupPipelineResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)` | StartupPipeline |
| 219 | public | ` public StartupPipelineDiagnostics GetDiagnostics()` | StartupPipeline |
| 251 | private | ` private async Task<StartupStepResult> ExecuteStepAsync( IStartupStep step, IProgress<string>? progress, CancellationToken cancellationToken)` | StartupPipeline |
| 307 | private | ` private async Task<List<StartupStepResult>> ExecuteUnitAsync( ExecutionUnit unit, IProgress<string>? progress, CancellationToken cancellationToken)` | StartupPipeline |
| 328 | private | ` private static List<ExecutionUnit> BuildExecutionUnits(IReadOnlyList<IStartupStep> sortedSteps)` | StartupPipeline |
| 367 | public | ` public ExecutionUnit(IStartupStep single) => Steps = new List<IStartupStep> { single };` | ExecutionUnit |
| 369 | public | ` public ExecutionUnit(IReadOnlyList<IStartupStep> parallel) => Steps = parallel.ToList();` | ExecutionUnit |
| 18 | public | ` public ApiHealthCheckStartupStep( IApplicationStateService applicationStateService, ILogger<ApiHealthCheckStartupStep> logger, int timeoutSeconds = 1…` | ApiHealthCheckStartupStep |
| 38 | public | ` public Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)` | ApiHealthCheckStartupStep |
| 18 | public | ` public ErrorHandlingStartupStep( IDesktopExceptionHandler exceptionHandler, ILogger<ErrorHandlingStartupStep> logger)` | ErrorHandlingStartupStep |
| 36 | public | ` public Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)` | ErrorHandlingStartupStep |
| 16 | public | ` public LocalWebApiStartupStep( IEmbeddedLocalWebApiService localWebApi, ILogger<LocalWebApiStartupStep> logger)` | LocalWebApiStartupStep |
| 29 | public | ` public async Task<StartupStepResult> ExecuteAsync( IProgress<string>? progress, CancellationToken cancellationToken = default)` | LocalWebApiStartupStep |
| 18 | public | ` public ModuleCoordinatorStartupStep( IModuleManager moduleManager, ILogger<ModuleCoordinatorStartupStep> logger)` | ModuleCoordinatorStartupStep |
| 39 | public | ` public Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)` | ModuleCoordinatorStartupStep |
| 60 | private | ` private void SubscribeToModuleEvents()` | ModuleCoordinatorStartupStep |
| 55 | public | ` public StatusBarManager( IApiHealthMonitor apiHealthMonitor, IConnectionSettingsService connectionSettings, IConnectionModeService connectionModeServ…` | StatusBarManager |
| 76 | private | ` private void Initialize()` | StatusBarManager |
| 84 | private | ` private void OnHealthStatusChanged(object? sender, ApiHealthMonitorChangedEventArgs e)` | StatusBarManager |
| 95 | private | ` private void OnConnectionUrlChanged(object? sender, string newUrl)` | StatusBarManager |
| 105 | private | ` private void OnConnectionModeChanged(object? sender, ConnectionMode e)` | StatusBarManager |
| 115 | public | ` public void UpdateTime()` | StatusBarManager |
| 123 | public | ` public async Task ForceCheckAsync()` | StatusBarManager |
| 129 | public | ` public void Dispose()` | StatusBarManager |
| 17 | public | ` public ThemeService(IConfiguration? configuration = null)` | ThemeService |
| 24 | public | ` public void ToggleTheme()` | ThemeService |
| 29 | public | ` public void ApplyTheme(bool isDark)` | ThemeService |
| 38 | public | ` public void InitializeThemeSync()` | ThemeService |
| 53 | private | ` private void LoadThemePreference()` | ThemeService |
| 75 | private | ` private void SaveThemePreference(bool isDarkMode)` | ThemeService |
| 90 | public | ` public void Dispose()` | ThemeService |
| 67 | public | ` public AccountSettingsViewModel( IViewModelServices services, IAuthenticationService authService, IUserService userService, INavigationCoordinator na…` | AccountSettingsViewModel |
| 82 | private | ` private async Task SaveProfileAsync()` | AccountSettingsViewModel |
| 135 | private | ` private bool CanSaveProfile() => !IsBusy && !string.IsNullOrWhiteSpace(EditRealName);` | AccountSettingsViewModel |
| 142 | private | ` private async Task ChangePasswordAsync()` | AccountSettingsViewModel |
| 207 | private | ` private bool CanChangePassword() =>` | AccountSettingsViewModel |
| 218 | private | ` private void GoBack() => _navigationCoordinator.NavigateBack();` | AccountSettingsViewModel |
| 224 | private | ` private async Task LoadUserProfileAsync()` | AccountSettingsViewModel |
| 243 | private | ` private void ClearPasswordFields()` | AccountSettingsViewModel |
| 254 | public | ` public override async void OnNavigatedTo(NavigationContext navigationContext)` | AccountSettingsViewModel |
| 272 | public | ` public override bool IsNavigationTarget(NavigationContext navigationContext) => true;` | AccountSettingsViewModel |
| 274 | public | ` public override void OnNavigatedFrom(NavigationContext navigationContext)` | AccountSettingsViewModel |
| 73 | default(private) | ` partial void OnIsDarkModeChanged(bool value) => _shell.Theme.ApplyTheme(value);` | MainWindowViewModel |
| 100 | public | ` public MainWindowViewModel( IViewModelServices services, IShellServices shell, INavigationCoordinator navigationCoordinator, INavigationManager navig…` | MainWindowViewModel |
| 144 | private | ` private async Task LogoutAsync()` | MainWindowViewModel |
| 175 | private | ` private async Task RetryHealthCheckAsync()` | MainWindowViewModel |
| 180 | default(private) | ` partial void OnIsSidebarExpandedChanged(bool value)` | MainWindowViewModel |
| 186 | private | ` private void ToggleSidebar()` | MainWindowViewModel |
| 195 | private | ` private void OnTick(object? sender, ApplicationTickEventArgs e)` | MainWindowViewModel |
| 200 | private | ` private void OnLoginStateChanged(object? sender, EventArgs e)` | MainWindowViewModel |
| 211 | private | ` private void OnLoginSuccessHandled(object? sender, EventArgs e)` | MainWindowViewModel |
| 220 | public | ` public async Task OnWindowLoadedAsync()` | MainWindowViewModel |
| 233 | public | ` public async Task<bool> RequestCloseApplicationAsync()` | MainWindowViewModel |
| 245 | protected | ` protected override async Task ShowSuccessMessageAsync(string message) =>` | MainWindowViewModel |
| 248 | protected | ` protected override async Task ShowErrorMessageAsync(string message) =>` | MainWindowViewModel |
| 251 | protected | ` protected override async Task ShowWarningMessageAsync(string message) =>` | MainWindowViewModel |
| 254 | protected | ` protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认") =>` | MainWindowViewModel |
| 261 | protected | ` protected override void OnDisposing()` | MainWindowViewModel |
| 10 | public | ` public AccountSettingsView()` | AccountSettingsView |
| 18 | public | ` public MainWindow(ISnackbarMessageQueue snackbarMessageQueue)` | MainWindow |
| 31 | private | ` private async void OnWindowLoaded(object sender, RoutedEventArgs e)` | MainWindow |
| 51 | protected | ` protected override async void OnPreviewKeyDown(KeyEventArgs e)` | MainWindow |
| 82 | private | ` private bool IsOnLoginScreen()` | MainWindow |
### LYBT.Desktop.Users

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 17 | public | ` public UserEditControl()` | UserEditControl |
| 13 | public | ` public UserMasterDetailControl()` | UserMasterDetailControl |
| 22 | public | ` public void SetDefaultRoleFilter(UserRole role)` | UserMasterDetailControl |
| 11 | public | ` public UserViewControl()` | UserViewControl |
| 146 | public | ` public static UserEditContext CreateNew()` | UserEditContext |
| 159 | public | ` public UserEditContext Clone()` | UserEditContext |
| 240 | public | ` public void UpdateFromDto(UserDetailDto dto)` | UserItem |
| 145 | public | ` public static UserDetailModel CreateNew()` | UserDetailModel |
| 158 | public | ` public UserDetailModel Clone()` | UserDetailModel |
| 21 | public | ` public UserRepository( IApiClient apiClient, ILogger<UserRepository> logger)` | UserRepository |
| 34 | public | ` public Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)` | UserRepository |
| 39 | public | ` public async Task<List<UserListDto>> SearchAsync(string keyword, CancellationToken ct = default)` | UserRepository |
| 57 | public | ` public async Task<UserDetailDto> GetByUsernameAsync(string username, CancellationToken ct = default)` | UserRepository |
| 81 | public | ` public async Task<List<UserListDto>> GetDoctorsAsync(CancellationToken ct = default)` | UserRepository |
| 110 | public | ` public async Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto, CancellationToken ct = default)` | UserRepository |
| 130 | public | ` public async Task<CommandResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)` | UserRepository |
| 155 | public | ` public async Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync( Guid userId, ResetPasswordRequest request, CancellationToken ct = defa…` | UserRepository |
| 189 | public | ` public async Task<UserDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default)` | UserRepository |
| 206 | public | ` public async Task<UserDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)` | UserRepository |
| 222 | public | ` public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)` | UserRepository |
| 22 | public | ` public RemoteUserService( IUserRepository userRepository, ILogger<RemoteUserService> logger)` | RemoteUserService |
| 35 | public | ` public async Task<CommandResult<UserDetailDto>> CreateUserAsync(UserInputDto createDto, CancellationToken ct = default)` | RemoteUserService |
| 55 | public | ` public async Task<CommandResult<UserDetailDto>> UpdateUserAsync(UserInputDto updateDto, CancellationToken ct = default)` | RemoteUserService |
| 75 | public | ` public async Task<CommandResult<bool>> DeleteUserAsync(Guid userId, CancellationToken ct = default)` | RemoteUserService |
| 95 | public | ` public async Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> userIds, CancellationToken ct = default)` | RemoteUserService |
| 123 | public | ` public async Task<CommandResult<UserDetailDto>> GetByIdAsync(Guid userId, CancellationToken ct = default)` | RemoteUserService |
| 145 | public | ` public async Task<CommandResult<PagedResult<UserListDto>>> GetPagedAsync( int page, int pageSize, string? searchText = null, CancellationToken ct = d…` | RemoteUserService |
| 166 | public | ` public async Task<CommandResult<List<UserDetailDto>>> GetAllAsync(CancellationToken ct = default)` | RemoteUserService |
| 195 | public | ` public async Task<CommandResult<UserDetailDto>> GetByUsernameAsync(string username, CancellationToken ct = default)` | RemoteUserService |
| 217 | public | ` public async Task<CommandResult<List<UserListDto>>> SearchAsync(string keyword, CancellationToken ct = default)` | RemoteUserService |
| 237 | public | ` public async Task<CommandResult<List<UserListDto>>> GetDoctorsAsync(CancellationToken ct = default)` | RemoteUserService |
| 261 | public | ` public async Task<CommandResult<UserDetailDto>> ChangeProfileAsync( Guid userId, ChangeProfileDto dto, CancellationToken ct = default)` | RemoteUserService |
| 286 | public | ` public async Task<CommandResult<bool>> ChangePasswordAsync( Guid userId, string oldPassword, string newPassword, CancellationToken ct = default)` | RemoteUserService |
| 321 | public | ` public async Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync( Guid userId, string newPassword, CancellationToken ct = default)` | RemoteUserService |
| 359 | public | ` public async Task<CommandResult<UserDetailDto>> ToggleStatusAsync(Guid userId, CancellationToken ct = default)` | RemoteUserService |
| 22 | public | ` public void OnInitialized(IContainerProvider containerProvider)` | UsersModule |
| 28 | public | ` public void RegisterTypes(IContainerRegistry containerRegistry)` | UsersModule |
| 14 | public (interface 默认) | ` Task ResetPasswordAsync(UserListDto user);` | IUserPasswordHandler |
| 21 | public (interface 默认) | ` bool CanResetPassword(UserListDto? user, bool isBusy);` | IUserPasswordHandler |
| 15 | public (interface 默认) | ` Task<bool> ToggleUserStatusAsync(UserListDto user);` | IUserStatusHandler |
| 22 | public (interface 默认) | ` Task<bool> RestoreAsync(UserListDto user);` | IUserStatusHandler |
| 29 | public (interface 默认) | ` bool CanToggleUserStatus(UserListDto? user, bool isBusy);` | IUserStatusHandler |
| 37 | public (interface 默认) | ` bool CanRestore(UserListDto? user, bool isBusy, bool isAdmin);` | IUserStatusHandler |
| 19 | public | ` public UserPasswordHandler( IUserService userService, IMasterDetailServices<UserListDto, UserDetailModel> masterDetailServices, ILogger<UserPasswordH…` | UserPasswordHandler |
| 30 | public | ` public async Task ResetPasswordAsync(UserListDto user)` | UserPasswordHandler |
| 59 | public | ` public bool CanResetPassword(UserListDto? user, bool isBusy)` | UserPasswordHandler |
| 21 | public | ` public UserStatusHandler( IUserService userService, IUserRepository userRepository, IMasterDetailServices<UserListDto, UserDetailModel> masterDetailS…` | UserStatusHandler |
| 33 | protected | ` protected override Guid GetEntityId(UserListDto e) => e.Id;` | UserStatusHandler |
| 34 | protected | ` protected override string GetEntityDisplayName(UserListDto e) => e.RealName ?? e.UserName;` | UserStatusHandler |
| 36 | protected | ` protected override async Task<object?> ExecuteRestoreAsync(Guid id)` | UserStatusHandler |
| 40 | public | ` public async Task<bool> ToggleUserStatusAsync(UserListDto user)` | UserStatusHandler |
| 64 | public | ` public bool CanToggleUserStatus(UserListDto? user, bool isBusy) => user != null && !isBusy;` | UserStatusHandler |
| 67 | public | ` public bool CanRestore(UserListDto? user, bool isBusy, bool isAdmin) => user != null && !isBusy && isAdmin;` | UserStatusHandler |
| 27 | public | ` public UserEditorViewModel(IDesktopCacheManager cacheManager)` | UserEditorViewModel |
| 35 | public | ` public void InitializeFromDto(UserDetailDto dto)` | UserEditorViewModel |
| 59 | public | ` public void InitializeForNewCase()` | UserEditorViewModel |
| 68 | public | ` public UserInputDto GetUserInput()` | UserEditorViewModel |
| 87 | public | ` public bool Validate()` | UserEditorViewModel |
| 95 | public | ` public void Reset()` | UserEditorViewModel |
| 94 | protected | ` protected override string? GetDetailDisplayName() => CurrentDetail?.RealName;` | UserMasterDetailViewModel |
| 110 | public | ` public UserMasterDetailViewModel( IViewModelServices viewModelServices, IMasterDetailServices<UserListDto, UserDetailModel> masterDetailServices, IUs…` | UserMasterDetailViewModel |
| 135 | protected | ` protected override async Task LoadListAsync()` | UserMasterDetailViewModel |
| 170 | private | ` private IEnumerable<UserListDto> ApplyFilters(IEnumerable<UserListDto> items)` | UserMasterDetailViewModel |
| 183 | protected | ` protected override async Task LoadDetailAsync(UserListDto item)` | UserMasterDetailViewModel |
| 239 | protected | ` protected override UserDetailModel CreateNewDetail()` | UserMasterDetailViewModel |
| 248 | protected | ` protected override async Task<bool> SaveDetailAsync(UserDetailModel detail)` | UserMasterDetailViewModel |
| 304 | protected | ` protected override async Task<bool> DeleteItemAsync(UserListDto item)` | UserMasterDetailViewModel |
| 333 | private | ` private void ClearFilters()` | UserMasterDetailViewModel |
| 341 | private | ` private bool CanClearFilters() => HasActiveFilters;` | UserMasterDetailViewModel |
| 349 | private | ` private async Task ResetPasswordAsync()` | UserMasterDetailViewModel |
| 355 | private | ` private bool CanResetPassword() => _passwordHandler.CanResetPassword(SelectedItem, IsBusy);` | UserMasterDetailViewModel |
| 359 | private | ` private async Task ToggleUserStatusAsync()` | UserMasterDetailViewModel |
| 369 | private | ` private bool CanToggleUserStatus() => _statusHandler.CanToggleUserStatus(SelectedItem, IsBusy);` | UserMasterDetailViewModel |
| 372 | protected | ` protected override async Task InvalidateCachesAsync()` | UserMasterDetailViewModel |
| 379 | protected | ` protected override async Task RestoreItemAsync(UserListDto item)` | UserMasterDetailViewModel |
| 388 | private | ` private void OnDetailPropertyChanged(object? sender, PropertyChangedEventArgs e)` | UserMasterDetailViewModel |
| 396 | protected | ` protected override void Dispose(bool disposing)` | UserMasterDetailViewModel |
### LYBT.Entities

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 72 | public | ` public static AuthSession Create( Guid userId, string tokenHash, DateTime expiryTime, string ipAddress, string? userAgent = null)` | AuthSession |
| 102 | public | ` public void Logout()` | AuthSession |
| 114 | public | ` public void Revoke()` | AuthSession |
| 124 | public | ` public void Revoke(string reason)` | AuthSession |
| 133 | public | ` public bool IsValid()` | AuthSession |
| 144 | public | ` public bool IsExpired()` | AuthSession |
| 103 | public | ` public static FormulaHerbItem Create( Guid formulaId, string herbName, int dosage = 1, string unit = "g", Guid? herbId = null, string? originalHerbNa…` | FormulaHerbItem |
| 135 | public | ` public void BindHerb(Guid herbId, string herbName)` | FormulaHerbItem |
| 87 | public | ` public static Formula Create( string name, string? effect = null, string? indication = null, string? usage = null, string? remark = null, string? pro…` | Formula |
| 123 | public | ` public void UpdateProfile( string name, string? effect, string? indication, string? usage, string? remark, string? property, string? category, bool i…` | Formula |
| 149 | public | ` public void AddHerb(FormulaHerbItem herb)` | Formula |
| 158 | public | ` public void Validate()` | Formula |
| 164 | public | ` public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)` | Formula |
| 171 | public | ` public void SoftDelete(Guid deletedBy)` | Formula |
| 178 | public | ` public void Restore(Guid restoredBy)` | Formula |
| 91 | public | ` public static Herb Create( string name, string unit, decimal price, string? pinYinCode = null, string? category = null, string? properties = null, st…` | Herb |
| 139 | public | ` public void UpdateProfile( string name, string unit, decimal price, string? pinYinCode, string? category, string? properties, string? origin, string?…` | Herb |
| 180 | public | ` public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)` | Herb |
| 190 | public | ` public void SoftDelete(Guid deletedBy)` | Herb |
| 200 | public | ` public void Restore(Guid restoredBy)` | Herb |
| 127 | public | ` public void Complete()` | MedicalCase |
| 137 | public | ` public void Suspend()` | MedicalCase |
| 146 | public | ` public void SoftDelete()` | MedicalCase |
| 155 | public | ` public void UpdateConsultation(string? presentIllness, string? tongueDiagnosis, string? pulseDiagnosis, string? tcmDiagnosis)` | MedicalCase |
| 92 | public | ` public static Patient Create( string name, Gender gender, DateTime? birthDate = null, string? phoneNumber = null, string? idNumber = null, string? pi…` | Patient |
| 122 | public | ` public void UpdateProfile( string name, Gender gender, DateTime? birthDate, string? phoneNumber, string? idNumber, string? pinYinCode, Guid updatedBy…` | Patient |
| 147 | public | ` public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)` | Patient |
| 157 | public | ` public void SoftDelete(Guid deletedBy)` | Patient |
| 167 | public | ` public void Restore(Guid restoredBy)` | Patient |
| 84 | public | ` public void StartVisit()` | Registration |
| 96 | public | ` public void Complete()` | Registration |
| 105 | public | ` public void Cancel()` | Registration |
| 114 | public | ` public void AssignMedicalCase(Guid medicalCaseId)` | Registration |
| 123 | public | ` public void RevertToWaiting()` | Registration |
| 133 | public | ` public void SoftDelete(Guid deletedBy)` | Registration |
| 93 | public | ` public static ApplicationUser Create( string userName, string realName, UserRole role, string? phoneNumber = null, string? email = null, string? rema…` | ApplicationUser |
| 129 | public | ` public void UpdateProfile(string realName, string? phoneNumber, string? email, string? remark, Guid updatedBy, decimal? registrationFee = null)` | ApplicationUser |
| 147 | public | ` public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)` | ApplicationUser |
| 160 | public | ` public void SoftDelete(Guid deletedBy)` | ApplicationUser |
### LYBT.Infrastructure

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 16 | protected | ` protected abstract Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct);` | BatchOperationHandlerBase |
| 19 | protected | ` protected abstract Task UpdateAsync(TEntity entity, CancellationToken ct);` | BatchOperationHandlerBase |
| 22 | protected | ` protected abstract Task ApplyOperationAsync(TEntity entity, Guid operatorId, CancellationToken ct);` | BatchOperationHandlerBase |
| 33 | protected | ` protected virtual string? GetEntityName(TEntity entity) => null;` | BatchOperationHandlerBase |
| 36 | protected | ` protected virtual Task<string?> ValidateAsync(TEntity entity, Guid id, Guid operatorId, CancellationToken ct)` | BatchOperationHandlerBase |
| 49 | protected | ` protected virtual Task OnBatchCompletedAsync(BatchOperationResultDto result, CancellationToken ct)` | BatchOperationHandlerBase |
| 53 | protected | ` protected virtual void FinalizeResult(BatchOperationResultDto result) { }` | BatchOperationHandlerBase |
| 56 | protected | ` protected virtual string BuildMessage(int successCount, int failureCount)` | BatchOperationHandlerBase |
| 62 | protected | ` protected async Task<Result<BatchOperationResultDto>> ExecuteBatchAsync( List<Guid> ids, Guid operatorId, CancellationToken ct)` | BatchOperationHandlerBase |
| 104 | private | ` private void RecordFailure(BatchOperationResultDto result, Guid id, string? name, string reason)` | BatchOperationHandlerBase |
| 17 | public | ` public CacheInvalidationService( IOutputCacheStore outputCacheStore, IMemoryCache memoryCache, ILogger<CacheInvalidationService> logger)` | CacheInvalidationService |
| 27 | public | ` public async Task InvalidateAsync(string tag, CancellationToken cancellationToken = default)` | CacheInvalidationService |
| 38 | public | ` public async Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)` | CacheInvalidationService |
| 13 | public (interface 默认) | ` Task InvalidateAsync(string tag, CancellationToken cancellationToken = default);` | ICacheInvalidationService |
| 18 | public (interface 默认) | ` Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);` | ICacheInvalidationService |
| 46 | public | ` public static bool IsAllowed(string key)` | ConfigurationWritePolicy |
| 15 | public (interface 默认) | ` Task<Result<Dictionary<string, string>>> GetConfigurationAsync(CancellationToken cancellationToken = default);` | ISystemConfigurationService |
| 20 | public (interface 默认) | ` Task<Result<string?>> GetValueAsync(string key, CancellationToken cancellationToken = default);` | ISystemConfigurationService |
| 25 | public (interface 默认) | ` Task<Result> ValidateProductionConfigAsync(CancellationToken cancellationToken = default);` | ISystemConfigurationService |
| 30 | public (interface 默认) | ` Task<Result> SetValueAsync(string key, string value, CancellationToken cancellationToken = default);` | ISystemConfigurationService |
| 35 | public (interface 默认) | ` Task<Result> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default);` | ISystemConfigurationService |
| 21 | public | ` public SystemConfigurationService( IConfiguration configuration, IConfigurationStore store, ProductionConfigurationValidator validator, ILogger<Syste…` | SystemConfigurationService |
| 33 | public | ` public async Task<Result<Dictionary<string, string>>> GetConfigurationAsync(CancellationToken cancellationToken = default)` | SystemConfigurationService |
| 46 | public | ` public async Task<Result<string?>> GetValueAsync(string key, CancellationToken cancellationToken = default)` | SystemConfigurationService |
| 56 | public | ` public async Task<Result> ValidateProductionConfigAsync(CancellationToken cancellationToken = default)` | SystemConfigurationService |
| 71 | public | ` public async Task<Result> SetValueAsync(string key, string value, CancellationToken cancellationToken = default)` | SystemConfigurationService |
| 93 | public | ` public async Task<Result> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default)` | SystemConfigurationService |
| 123 | private | ` private void ReloadConfiguration()` | SystemConfigurationService |
| 12 | public (interface 默认) | ` Task<IReadOnlyDictionary<string, string>> LoadAllAsync(CancellationToken cancellationToken = default);` | IConfigurationStore |
| 17 | public (interface 默认) | ` Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default);` | IConfigurationStore |
| 22 | public (interface 默认) | ` Task RemoveAsync(string key, CancellationToken cancellationToken = default);` | IConfigurationStore |
| 20 | public | ` public JsonFileConfigurationStore(string? filePath = null, IReadOnlyDictionary<string, string?>? baseline = null)` | JsonFileConfigurationStore |
| 27 | public | ` public async Task<IReadOnlyDictionary<string, string>> LoadAllAsync(CancellationToken cancellationToken = default)` | JsonFileConfigurationStore |
| 40 | public | ` public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)` | JsonFileConfigurationStore |
| 66 | public | ` public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)` | JsonFileConfigurationStore |
| 80 | public | ` public void Dispose()` | JsonFileConfigurationStore |
| 86 | private | ` private async Task PersistAsync(CancellationToken cancellationToken)` | JsonFileConfigurationStore |
| 111 | private | ` private void LoadFromFile()` | JsonFileConfigurationStore |
| 119 | public | ` public ProductionConfigurationValidator(IConfiguration configuration)` | ProductionConfigurationValidator |
| 127 | public | ` public void ValidateOrThrow()` | ProductionConfigurationValidator |
| 142 | public | ` public List<string> ValidateCriticalItems()` | ProductionConfigurationValidator |
| 164 | public | ` public List<string> ValidateImportantItems()` | ProductionConfigurationValidator |
| 183 | private | ` private void ValidateAllItems()` | ProductionConfigurationValidator |
| 193 | private | ` private void ValidateItem(ConfigurationItem item)` | ProductionConfigurationValidator |
| 242 | private | ` private void ValidateCrossFieldRules()` | ProductionConfigurationValidator |
| 291 | private | ` private string GetDetailedErrorMessage()` | ProductionConfigurationValidator |
| 360 | private | ` private void AppendErrorDetail(StringBuilder sb, ConfigurationError error)` | ProductionConfigurationValidator |
| 430 | public | ` public ProductionConfigurationException(string message) : base(message)` | ProductionConfigurationException |
| 29 | public | ` public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)` | AppDbContext |
| 33 | public | ` public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)` | AppDbContext |
| 98 | protected | ` protected override void OnModelCreating(ModelBuilder modelBuilder)` | AppDbContext |
| 115 | public | ` public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)` | AppDbContext |
| 124 | public | ` public override int SaveChanges()` | AppDbContext |
| 133 | private | ` private void SetAuditFields()` | AppDbContext |
| 177 | private | ` private Guid? GetCurrentUserId()` | AppDbContext |
| 16 | public | ` public AppDbContext CreateDbContext(string[] args)` | AppDbContextFactory |
| 18 | public | ` public static void ApplyOptimizations(this ModelBuilder modelBuilder)` | EntityOptimizationExtensions |
| 33 | private | ` private static void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)` | EntityOptimizationExtensions |
| 58 | private | ` private static void ConfigureGlobalQueryFilter<TEntity>(ModelBuilder modelBuilder)` | EntityOptimizationExtensions |
| 68 | private | ` private static void OptimizePatientEntity(ModelBuilder modelBuilder)` | EntityOptimizationExtensions |
| 82 | private | ` private static void OptimizeUserEntity(ModelBuilder modelBuilder)` | EntityOptimizationExtensions |
| 12 | public | ` public void Configure(EntityTypeBuilder<AuthSession> entity)` | AuthSessionConfiguration |
| 15 | public | ` public virtual void Configure(EntityTypeBuilder<T> builder)` | BaseEntityConfiguration |
| 15 | public | ` public override void Configure(EntityTypeBuilder<Consultation> builder)` | ConsultationConfiguration |
| 14 | public | ` public override void Configure(EntityTypeBuilder<Formula> builder)` | FormulaConfiguration |
| 13 | public | ` public void Configure(EntityTypeBuilder<FormulaHerbItem> entity)` | FormulaHerbItemConfiguration |
| 14 | public | ` public override void Configure(EntityTypeBuilder<Herb> builder)` | HerbConfiguration |
| 12 | public | ` public void Configure(EntityTypeBuilder<MedicalCaseAuditLog> builder)` | MedicalCaseAuditLogConfiguration |
| 16 | public | ` public override void Configure(EntityTypeBuilder<MedicalCase> builder)` | MedicalCaseConfiguration |
| 14 | public | ` public override void Configure(EntityTypeBuilder<Patient> builder)` | PatientConfiguration |
| 15 | public | ` public override void Configure(EntityTypeBuilder<Prescription> builder)` | PrescriptionConfiguration |
| 12 | public | ` public void Configure(EntityTypeBuilder<PrescriptionItem> entity)` | PrescriptionItemConfiguration |
| 14 | public | ` public override void Configure(EntityTypeBuilder<Registration> builder)` | RegistrationConfiguration |
| 12 | public | ` public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)` | SecurityAuditLogConfiguration |
| 12 | public | ` public void Configure(EntityTypeBuilder<SystemLog> entity)` | SystemLogConfiguration |
| 13 | public | ` public void Configure(EntityTypeBuilder<ApplicationUser> builder)` | UserConfiguration |
| 24 | public | ` public DatabaseInitializationService( IDbContextAccessor dbAccessor, ILogger<DatabaseInitializationService> logger, IOptions<SystemAdminOptions> syst…` | DatabaseInitializationService |
| 42 | public | ` public async Task InitializeDatabaseAsync()` | DatabaseInitializationService |
| 117 | public | ` public async Task<string> GetDatabaseInfoAsync()` | DatabaseInitializationService |
| 135 | private | ` private async Task EnsureSystemAdminExistsAsync()` | DatabaseInitializationService |
| 205 | private | ` private static bool IsDevelopment()` | DatabaseInitializationService |
| 215 | private | ` private static bool ValidateSetupToken(string? configuredToken)` | DatabaseInitializationService |
| 13 | public | ` public DbContextAccessor(AppDbContext context)` | DbContextAccessor |
| 18 | public | ` public BusinessExceptionHandler(ILogger<BusinessExceptionHandler> logger)` | BusinessExceptionHandler |
| 24 | public | ` public async ValueTask<bool> TryHandleAsync( HttpContext httpContext, Exception exception, CancellationToken cancellationToken)` | BusinessExceptionHandler |
| 70 | private | ` private static string GetCorrelationId(HttpContext httpContext)` | BusinessExceptionHandler |
| 32 | public | ` public static string GetByStatusCode(int statusCode)` | ProblemTypeUris |
| 18 | public | ` public SystemExceptionHandler( ILogger<SystemExceptionHandler> logger, IHostEnvironment environment)` | SystemExceptionHandler |
| 27 | public | ` public async ValueTask<bool> TryHandleAsync( HttpContext httpContext, Exception exception, CancellationToken cancellationToken)` | SystemExceptionHandler |
| 86 | default(private) | ` private (int StatusCode, string Title, string Detail) GetExceptionInfo(Exception exception)` | SystemExceptionHandler |
| 182 | private | ` private static string GetCorrelationId(HttpContext httpContext)` | SystemExceptionHandler |
| 15 | public | ` public static async Task<PagedResult<T>> GetPagedResultAsync<T>( this IQueryable<T> query, int pageNumber, int pageSize, CancellationToken cancellati…` | QueryablePagingExtensions |
| 15 | public (interface 默认) | ` Task<DatabaseHealthCheckResult> CheckDatabaseAsync();` | IHealthCheckService |
| 21 | public (interface 默认) | ` Task<HealthStatus> GetOverallStatusAsync();` | IHealthCheckService |
| 32 | public (interface 默认) | ` Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);` | IRepository |
| 40 | public (interface 默认) | ` Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);` | IRepository |
| 48 | public (interface 默认) | ` Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);` | IRepository |
| 56 | public (interface 默认) | ` Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);` | IRepository |
| 13 | public (interface 默认) | ` Task<List<SystemLog>> GetRecentLogsAsync(int count, CancellationToken cancellationToken = default);` | ISystemLogRepository |
| 21 | public | ` public LogCleanupService( IServiceProvider serviceProvider, ILogger<LogCleanupService> logger, IOptions<LoggingOptions> options)` | LogCleanupService |
| 31 | protected | ` protected override async Task ExecuteAsync(CancellationToken stoppingToken)` | LogCleanupService |
| 63 | private | ` private async Task CleanupOldLogsAsync(CancellationToken cancellationToken)` | LogCleanupService |
| 22 | protected | ` protected BaseRepository(TDbContext context, ILogger logger)` | BaseRepository |
| 34 | public | ` public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)` | BaseRepository |
| 53 | public | ` public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)` | BaseRepository |
| 74 | public | ` public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)` | BaseRepository |
| 93 | public | ` public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)` | BaseRepository |
| 118 | public | ` public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)` | BaseRepository |
| 15 | public | ` public SystemLogRepository(AppDbContext context)` | SystemLogRepository |
| 20 | public | ` public async Task<List<SystemLog>> GetRecentLogsAsync(int count, CancellationToken cancellationToken = default)` | SystemLogRepository |
| 25 | public | ` public override bool CanConvert(Type typeToConvert)` | SensitiveDataJsonConverterFactory |
| 48 | private | ` private static bool HasSensitiveProperties(Type type)` | SensitiveDataJsonConverterFactory |
| 68 | public | ` public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)` | SensitiveDataJsonConverterFactory |
| 84 | public | ` public SensitiveDataJsonConverter(JsonSerializerOptions options)` | SensitiveDataJsonConverter |
| 99 | public | ` public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)` | SensitiveDataJsonConverter |
| 107 | public | ` public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)` | SensitiveDataJsonConverter |
| 175 | private | ` private static string GetPropertyName(string name, JsonSerializerOptions options)` | SensitiveDataJsonConverter |
| 13 | protected | ` protected BaseService(ILogger logger)` | BaseService |
| 25 | protected | ` protected BaseService(ILogger logger) : base(logger)` | BaseService |
| 16 | public | ` public CrossModuleService( IPatientCrossModuleService patient, IHerbCrossModuleService herb, IUserCrossModuleService user)` | CrossModuleService |
| 28 | public | ` public Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken cancellationToken = default)` | CrossModuleService |
| 33 | public | ` public Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken cancellationToken = default)` | CrossModuleService |
| 36 | public | ` public Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)` | CrossModuleService |
| 39 | public | ` public Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)` | CrossModuleService |
| 42 | public | ` public Task<List<HerbBasicDto>> GetAllActiveHerbsAsync(CancellationToken cancellationToken = default)` | CrossModuleService |
| 47 | public | ` public Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default)` | CrossModuleService |
| 50 | public | ` public Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)` | CrossModuleService |
| 53 | public | ` public Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default)` | CrossModuleService |
| 56 | public | ` public Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default)` | CrossModuleService |
| 59 | public | ` public Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default)` | CrossModuleService |
| 13 | public | ` public static IServiceCollection AddCrossModuleService(this IServiceCollection services)` | CrossModuleServiceExtensions |
| 11 | public (interface 默认) | ` Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default);` | IAuthCrossModuleService |
| 14 | public (interface 默认) | ` Task RecordSecurityAuditAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default);` | IAuthCrossModuleService |
| 15 | public (interface 默认) | ` Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken cancellationToken = default);` | ICrossModuleService |
| 20 | public (interface 默认) | ` Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken cancellationToken = default);` | ICrossModuleService |
| 23 | public (interface 默认) | ` Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default);` | ICrossModuleService |
| 26 | public (interface 默认) | ` Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default);` | ICrossModuleService |
| 29 | public (interface 默认) | ` Task<List<HerbBasicDto>> GetAllActiveHerbsAsync(CancellationToken cancellationToken = default);` | ICrossModuleService |
| 34 | public (interface 默认) | ` Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default);` | ICrossModuleService |
| 37 | public (interface 默认) | ` Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);` | ICrossModuleService |
| 40 | public (interface 默认) | ` Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default);` | ICrossModuleService |
| 43 | public (interface 默认) | ` Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default);` | ICrossModuleService |
| 46 | public (interface 默认) | ` Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default);` | ICrossModuleService |
| 12 | public (interface 默认) | ` Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken cancellationToken = default);` | IHerbCrossModuleService |
| 15 | public (interface 默认) | ` Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin, CancellationToken cancellationToken = default);` | IHerbCrossModuleService |
| 18 | public (interface 默认) | ` Task<ReferenceCheckResult> CheckHerbReferenceAsync(Guid herbId, CancellationToken cancellationToken = default);` | IHerbCrossModuleService |
| 21 | public (interface 默认) | ` Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default);` | IHerbCrossModuleService |
| 24 | public (interface 默认) | ` Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default);` | IHerbCrossModuleService |
| 27 | public (interface 默认) | ` Task<List<HerbBasicDto>> GetAllActiveHerbsAsync(CancellationToken cancellationToken = default);` | IHerbCrossModuleService |
| 13 | public (interface 默认) | ` Task<int> CountUnfinishedMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default);` | IMedicalCaseCrossModuleService |
| 16 | public (interface 默认) | ` Task<int> CountMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default);` | IMedicalCaseCrossModuleService |
| 19 | public (interface 默认) | ` Task<List<MedicalCaseReferenceDto>> GetRecentMedicalCasesAsync(Guid patientId, int count, CancellationToken cancellationToken = default);` | IMedicalCaseCrossModuleService |
| 22 | public (interface 默认) | ` Task<Guid?> CreateMedicalCaseForRegistrationAsync(Guid patientId, Guid registrationId, Guid doctorId, CancellationToken cancellationToken = default);` | IMedicalCaseCrossModuleService |
| 12 | public (interface 默认) | ` Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken cancellationToken = default);` | IPatientCrossModuleService |
| 12 | public (interface 默认) | ` Task CompleteByMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default);` | IRegistrationCrossModuleService |
| 17 | public (interface 默认) | ` Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId, CancellationToken ct = default);` | IRegistrationCrossModuleService |
| 22 | public (interface 默认) | ` Task LinkRegistrationToMedicalCaseAsync(Guid registrationId, Guid medicalCaseId, CancellationToken ct = default);` | IRegistrationCrossModuleService |
| 12 | public (interface 默认) | ` Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default);` | IUserCrossModuleService |
| 15 | public (interface 默认) | ` Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);` | IUserCrossModuleService |
| 18 | public (interface 默认) | ` Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken cancellationToken = default);` | IUserCrossModuleService |
| 21 | public (interface 默认) | ` Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);` | IUserCrossModuleService |
| 26 | public (interface 默认) | ` Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default);` | IUserCrossModuleService |
| 31 | public (interface 默认) | ` Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default);` | IUserCrossModuleService |
| 36 | public (interface 默认) | ` Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default);` | IUserCrossModuleService |
| 18 | public | ` public HealthCheckService(IDbContextAccessor dbAccessor, ILogger<HealthCheckService> logger)` | HealthCheckService |
| 25 | public | ` public async Task<DatabaseHealthCheckResult> CheckDatabaseAsync()` | HealthCheckService |
| 91 | public | ` public async Task<HealthStatus> GetOverallStatusAsync()` | HealthCheckService |
| 15 | public | ` public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)` | ValidationBehavior |
| 20 | public | ` public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)` | ValidationBehavior |
| 19 | protected | ` protected BaseApiController(ILogger logger)` | BaseApiController |
| 29 | default(private) | ` protected (Guid OperatorId, string OperatorName, UserRole OperatorRole) GetOperator()` | BaseApiController |
| 38 | protected | ` protected void LogOperation(string operation, object? data = null, Guid? targetId = null)` | BaseApiController |
| 58 | protected | ` protected string GetRequestId() => HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();` | BaseApiController |
| 60 | protected | ` protected IActionResult Success(string message = "操作成功")` | BaseApiController |
| 63 | protected | ` protected IActionResult Success<T>(T data, string message = "操作成功")` | BaseApiController |
| 66 | protected | ` protected IActionResult SuccessPaged<T>(PagedResult<T> pagedResult, string message = "查询成功")` | BaseApiController |
| 69 | protected | ` protected IActionResult Error(string message)` | BaseApiController |
| 75 | protected | ` protected IActionResult NotFound(string message = "资源未找到")` | BaseApiController |
| 78 | protected | ` protected IActionResult BusinessFail(string message, string? errorCode = null)` | BaseApiController |
| 81 | protected | ` protected IActionResult ValidationFail(string message = "参数验证失败")` | BaseApiController |
| 84 | protected | ` protected IActionResult Forbid(string message)` | BaseApiController |
| 91 | protected | ` protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "操作成功", bool useAuthMapping = false)` | BaseApiController |
| 94 | protected | ` protected IActionResult HandleResult(Result result, string successMessage = "操作成功")` | BaseApiController |
| 105 | protected | ` protected IActionResult? ValidateGuid(Guid id, string paramName = "ID")` | BaseApiController |
| 119 | protected | ` protected bool IsAdminOrOwner(Guid? createdBy)` | BaseApiController |
| 144 | protected | ` protected IActionResult? ValidateOwnership(Guid? createdBy, string resourceName = "资源")` | BaseApiController |
| 158 | protected | ` protected IActionResult? ValidateModel()` | BaseApiController |
| 171 | protected | ` protected IActionResult? ValidatePagination(int page, int pageSize)` | BaseApiController |
| 14 | public | ` public static Guid GetCurrentUserId(ClaimsPrincipal user)` | BaseClaimsHelper |
| 17 | protected | ` protected BaseCrudController(ISender sender, ILogger logger)` | BaseCrudController |
| 29 | public | ` public virtual Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cancellation…` | BaseCrudController |
| 40 | public | ` public virtual Task<IActionResult> GetById(Guid id, CancellationToken ct)` | BaseCrudController |
| 47 | public | ` public virtual Task<IActionResult> Delete(Guid id, CancellationToken ct)` | BaseCrudController |
| 54 | public | ` public virtual Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)` | BaseCrudController |
| 61 | public | ` public virtual Task<IActionResult> Restore(Guid id, CancellationToken ct)` | BaseCrudController |
| 68 | public | ` public virtual Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | BaseCrudController |
| 76 | protected | ` protected async Task<IActionResult> ExecuteBatchDeleteAsync( BatchDeleteInputDto dto, Func<List<Guid>, Guid, IRequest<Result<BatchOperationResultDto>…` | BaseCrudController |
| 98 | protected | ` protected async Task<IActionResult> ExecuteBatchCheckReferenceAsync<T>( List<Guid> ids, Func<List<Guid>, IRequest<Result<List<T>>>> createQuery, stri…` | BaseCrudController |
| 12 | private | ` private static string GetRequestId(ControllerBase controller)` | ControllerBaseExtensions |
| 17 | public | ` public static IActionResult Success(this ControllerBase controller, string message = "操作成功")` | ControllerBaseExtensions |
| 24 | public | ` public static IActionResult Success<T>(this ControllerBase controller, T data, string message = "操作成功")` | ControllerBaseExtensions |
| 31 | public | ` public static IActionResult SuccessPaged<T>(this ControllerBase controller, PagedResult<T> pagedResult, string message = "查询成功")` | ControllerBaseExtensions |
| 42 | public | ` public static IActionResult Error(this ControllerBase controller, string message)` | ControllerBaseExtensions |
| 49 | public | ` public static IActionResult NotFoundResponse(this ControllerBase controller, string message = "资源未找到")` | ControllerBaseExtensions |
| 56 | public | ` public static IActionResult BusinessFail(this ControllerBase controller, string message, string? errorCode = null)` | ControllerBaseExtensions |
| 65 | public | ` public static IActionResult ValidationFail(this ControllerBase controller, string message = "参数验证失败")` | ControllerBaseExtensions |
| 76 | public | ` public static IActionResult ForbidResponse(this ControllerBase controller, string message)` | ControllerBaseExtensions |
| 85 | public | ` public static IActionResult HandleResult<T>(this ControllerBase controller, Result<T> result, string successMessage = "操作成功", bool useAuthMapping = f…` | ControllerBaseExtensions |
| 121 | public | ` public static IActionResult HandleResult(this ControllerBase controller, Result result, string successMessage = "操作成功")` | ControllerBaseExtensions |
| 141 | private | ` private static ApiResponse<T> CreateModuleErrorResponse<T>(ControllerBase controller, string message, ErrorCode errorCode)` | ControllerBaseExtensions |
### LYBT.LocalWebAPI

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 35 | public | ` public static void Initialize(LocalJwtOptions options)` | LocalJwtConfig |
| 43 | public | ` public static void ConfigureServices(IServiceCollection services, LocalJwtOptions jwtOptions)` | LocalJwtConfig |
| 97 | public | ` public static string GenerateToken(ApplicationUser user, IList<string> roles)` | LocalJwtConfig |
| 126 | private | ` private static string ParseRoleClaim(IList<string> roles)` | LocalJwtConfig |
| 19 | public | ` public AuthController(ISender sender, ILogger<AuthController> logger) : base(logger)` | AuthController |
| 24 | private | ` private static Guid GetCurrentUserId(ClaimsPrincipal principal)` | AuthController |
| 30 | public | ` public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)` | AuthController |
| 40 | public | ` public IActionResult Logout([FromBody] LogoutRequest request)` | AuthController |
| 48 | public | ` public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)` | AuthController |
| 58 | public | ` public async Task<IActionResult> AutoLogin([FromBody] AutoLoginRequest request, CancellationToken ct)` | AuthController |
| 67 | public | ` public async Task<IActionResult> ValidateToken(CancellationToken ct)` | AuthController |
| 23 | public | ` public ConfigurationController(IConfigurationStore store, ILogger<ConfigurationController> logger)` | ConfigurationController |
| 31 | public | ` public async Task<IActionResult> GetAll(CancellationToken ct)` | ConfigurationController |
| 79 | public | ` public async Task<IActionResult> Validate(CancellationToken ct)` | ConfigurationController |
| 14 | public | ` public DeployController(ILogger<DeployController> logger) : base(logger) { }` | DeployController |
| 17 | public | ` public IActionResult Upload(IFormFile file)` | DeployController |
| 23 | public | ` public IActionResult Restart()` | DeployController |
| 26 | public | ` public DiagnosticsController(ISystemLogRepository systemLogRepository, IHealthCheckService healthCheckService, LoggingLevelManager loggingLevelManage…` | DiagnosticsController |
| 35 | public | ` public async Task<IActionResult> GetDbInfo()` | DiagnosticsController |
| 57 | public | ` public IActionResult GetVersion()` | DiagnosticsController |
| 77 | public | ` public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 50, CancellationToken ct = default)` | DiagnosticsController |
| 99 | public | ` public IActionResult GetLoggingStatus()` | DiagnosticsController |
| 116 | public | ` public IActionResult EnableDebugMode([FromBody] EnableDebugModeRequest? request)` | DiagnosticsController |
| 145 | public | ` public IActionResult DisableDebugMode()` | DiagnosticsController |
| 160 | public | ` public IActionResult SetLoggingLevel([FromBody] SetLoggingLevelRequest request)` | DiagnosticsController |
| 180 | private | ` private bool IsAdminOrHigher()` | DiagnosticsController |
| 25 | public | ` public FormulasController( ISender sender, ILogger<FormulasController> logger, IFormulaService formulaService) : base(sender, logger)` | FormulasController |
| 37 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | FormulasController |
| 56 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | FormulasController |
| 73 | public | ` public async Task<IActionResult> Create([FromBody] FormulaInputDto input, CancellationToken ct)` | FormulasController |
| 92 | public | ` public async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto input, CancellationToken ct)` | FormulasController |
| 115 | public | ` public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)` | FormulasController |
| 140 | public | ` public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)` | FormulasController |
| 163 | public | ` public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | FormulasController |
| 175 | public | ` public async Task<IActionResult> Clone(Guid id, CancellationToken ct)` | FormulasController |
| 213 | public | ` public async Task<IActionResult> BatchImport([FromBody] List<FormulaImportItemDto> formulas, CancellationToken ct)` | FormulasController |
| 227 | public | ` public async Task<IActionResult> GetPendingValidation(CancellationToken ct)` | FormulasController |
| 239 | public | ` public async Task<IActionResult> ValidateHerb(Guid formulaId, Guid herbItemId, [FromBody] ValidateFormulaHerbInputDto request, CancellationToken ct)` | FormulasController |
| 252 | public | ` public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)` | FormulasController |
| 273 | public | ` public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | FormulasController |
| 290 | public | ` public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | FormulasController |
| 22 | public | ` public HealthController( IHealthCheckService healthCheckService, UserManager<ApplicationUser> userManager, ILogger<HealthController> logger)` | HealthController |
| 33 | public | ` public async Task<IActionResult> GetHealth()` | HealthController |
| 49 | public | ` public IActionResult Ping()` | HealthController |
| 61 | public | ` public async Task<IActionResult> GetDetails()` | HealthController |
| 25 | public | ` public HerbsController(ISender sender, ILogger<HerbsController> logger, IHerbService herbService)` | HerbsController |
| 35 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | HerbsController |
| 52 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | HerbsController |
| 67 | public | ` public async Task<IActionResult> Create([FromBody] HerbInputDto input, CancellationToken ct)` | HerbsController |
| 87 | public | ` public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto input, CancellationToken ct)` | HerbsController |
| 112 | public | ` public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)` | HerbsController |
| 137 | public | ` public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)` | HerbsController |
| 163 | public | ` public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)` | HerbsController |
| 180 | public | ` public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | HerbsController |
| 192 | public | ` public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken ct)` | HerbsController |
| 207 | public | ` public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)` | HerbsController |
| 219 | public | ` public async Task<IActionResult> BatchCheckReference([FromBody] HerbBatchCheckReferenceInputDto dto, CancellationToken ct)` | HerbsController |
| 233 | public | ` public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | HerbsController |
| 250 | public | ` public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | HerbsController |
| 23 | public | ` public MedicalCasesController( ISender sender, ILogger<MedicalCasesController> logger, IMedicalCaseCommandService medicalCaseCommandService, IMedical…` | MedicalCasesController |
| 37 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | MedicalCasesController |
| 63 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | MedicalCasesController |
| 76 | public | ` public override async Task<IActionResult> Query([FromQuery] MedicalCaseQueryDto query, CancellationToken ct = default)` | MedicalCasesController |
| 90 | public | ` public async Task<IActionResult> GetByStatus(MedicalCaseStatus status, CancellationToken ct = default)` | MedicalCasesController |
| 103 | public | ` public async Task<IActionResult> GetPending([FromQuery] Guid? patientId = null, CancellationToken ct = default)` | MedicalCasesController |
| 123 | public | ` public async Task<IActionResult> Update( Guid id, [FromBody] MedicalCaseInputDto input, CancellationToken ct)` | MedicalCasesController |
| 149 | public | ` public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)` | MedicalCasesController |
| 165 | public | ` public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | MedicalCasesController |
| 190 | public | ` public async Task<IActionResult> Create([FromBody] MedicalCaseInputDto input, CancellationToken ct)` | MedicalCasesController |
| 205 | public | ` public async Task<IActionResult> CloseCase(Guid id, CancellationToken ct)` | MedicalCasesController |
| 222 | public | ` public async Task<IActionResult> SuspendCase(Guid id, [FromBody] ConsultationInputDto? request = null, CancellationToken ct = default)` | MedicalCasesController |
| 239 | public | ` public async Task<IActionResult> CancelCase(Guid id, [FromBody] CancelMedicalCaseRequest? request = null, CancellationToken ct = default)` | MedicalCasesController |
| 256 | public | ` public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MedicalCaseStatusInputDto request, CancellationToken ct = default)` | MedicalCasesController |
| 25 | public | ` public PatientsController( ISender sender, ILogger<PatientsController> logger, IPatientService patientService) : base(sender, logger)` | PatientsController |
| 37 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | PatientsController |
| 58 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | PatientsController |
| 72 | public | ` public async Task<IActionResult> Create([FromBody] PatientInputDto input, CancellationToken ct)` | PatientsController |
| 91 | public | ` public async Task<IActionResult> Update(Guid id, [FromBody] PatientInputDto input, CancellationToken ct)` | PatientsController |
| 118 | public | ` public async Task<IActionResult> GetByIdNumber(string idNumber, CancellationToken ct)` | PatientsController |
| 130 | public | ` public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)` | PatientsController |
| 143 | public | ` public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)` | PatientsController |
| 164 | public | ` public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)` | PatientsController |
| 185 | public | ` public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)` | PatientsController |
| 201 | public | ` public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto dto, CancellationToken ct)` | PatientsController |
| 214 | public | ` public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | PatientsController |
| 227 | public | ` public async Task<IActionResult> BatchImport([FromBody] PatientBatchImportInputDto request, CancellationToken ct)` | PatientsController |
| 20 | public | ` public RegistrationsController(ISender sender, ILogger<RegistrationsController> logger)` | RegistrationsController |
| 27 | public | ` public override async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto, CancellationToken ct)` | RegistrationsController |
| 32 | public | ` public override async Task<IActionResult> StartVisit(Guid id, CancellationToken ct)` | RegistrationsController |
| 37 | public | ` public async Task<IActionResult> Create([FromBody] RegistrationInputDto input, CancellationToken ct)` | RegistrationsController |
| 48 | public | ` public override async Task<IActionResult> Cancel(Guid id, CancellationToken ct)` | RegistrationsController |
| 18 | public | ` public ReportsController(IReportRepository reportRepository, ILogger<ReportsController> logger) : base(logger)` | ReportsController |
| 24 | public | ` public async Task<IActionResult> GetDailyIncome( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken canc…` | ReportsController |
| 46 | public | ` public async Task<IActionResult> GetDailyConsultations( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationTok…` | ReportsController |
| 67 | public | ` public async Task<IActionResult> GetDailyHerbs( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cance…` | ReportsController |
| 13 | public | ` public UsersController( ISender sender, ILogger<UsersController> logger, IUserService userService)` | UsersController |
| 15 | public | ` public static async Task SeedAsync(AppDbContext context, IServiceProvider? serviceProvider = null)` | LocalWebApiSeedData |
| 8 | public | ` public static UserRole ParseUserRole(IList<string> roles)` | LocalAuthHelpers |
| 26 | public | ` public LocalAutoLoginCommandHandler( UserManager<ApplicationUser> userManager, IOptions<LocalJwtOptions> jwtOptions, ILogger<LocalAutoLoginCommandHan…` | LocalAutoLoginCommandHandler |
| 36 | public | ` public async Task<ApiResponse<LoginResponse>> Handle(LocalAutoLoginCommand command, CancellationToken cancellationToken)` | LocalAutoLoginCommandHandler |
| 21 | public | ` public LocalLoginCommandHandler( UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<LocalLoginCommandHan…` | LocalLoginCommandHandler |
| 31 | public | ` public async Task<ApiResponse<LoginResponse>> Handle(LocalLoginCommand command, CancellationToken cancellationToken)` | LocalLoginCommandHandler |
| 21 | public | ` public LocalRefreshTokenCommandHandler( UserManager<ApplicationUser> userManager, ILogger<LocalRefreshTokenCommandHandler> logger)` | LocalRefreshTokenCommandHandler |
| 29 | public | ` public async Task<ApiResponse<LoginResponse>> Handle(LocalRefreshTokenCommand command, CancellationToken cancellationToken)` | LocalRefreshTokenCommandHandler |
| 15 | public | ` public LocalValidateTokenQueryHandler(UserManager<ApplicationUser> userManager)` | LocalValidateTokenQueryHandler |
| 20 | public | ` public async Task<ApiResponse<ValidateTokenResponse>> Handle(LocalValidateTokenQuery query, CancellationToken cancellationToken)` | LocalValidateTokenQueryHandler |
| 37 | public | ` public static WebApplicationBuilder CreateBuilder(string[]? args = null)` | LocalWebApiProgram |
| 45 | public | ` public static WebApplication CreateApplication(WebApplicationBuilder builder, string connectionString)` | LocalWebApiProgram |
| 140 | public | ` public static async Task InitializeDatabaseAsync(WebApplication app)` | LocalWebApiProgram |
### LYBT.Module.Auth

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 14 | public | ` public AutoLoginCommandHandler( IJwtService jwtService, ILogger<AutoLoginCommandHandler> logger)` | AutoLoginCommandHandler |
| 22 | public | ` public Task<Result<LoginResponse>> Handle( AutoLoginCommand request, CancellationToken cancellationToken)` | AutoLoginCommandHandler |
| 32 | public | ` public LoginCommandHandler( IJwtService jwtService, ICrossModuleService crossModuleService, IAuthSessionRepository authSessionRepository, ISecurityAu…` | LoginCommandHandler |
| 54 | public | ` public async Task<Result<LoginResponse>> Handle( LoginCommand request, CancellationToken cancellationToken)` | LoginCommandHandler |
| 196 | private | ` private static string ComputeTokenHash(string token)` | LoginCommandHandler |
| 17 | public | ` public LogoutCommandHandler( IAuthSessionRepository authSessionRepository, ISecurityAuditService securityAuditService, ILogger<LogoutCommandHandler> …` | LogoutCommandHandler |
| 27 | public | ` public async Task<Result<bool>> Handle( LogoutCommand request, CancellationToken cancellationToken)` | LogoutCommandHandler |
| 56 | private | ` private static string ComputeTokenHash(string token)` | LogoutCommandHandler |
| 20 | public | ` public RefreshTokenCommandHandler( IJwtService jwtService, IAuthSessionRepository authSessionRepository, ISecurityAuditService securityAuditService, …` | RefreshTokenCommandHandler |
| 32 | public | ` public async Task<Result<LoginResponse>> Handle( RefreshTokenCommand request, CancellationToken cancellationToken)` | RefreshTokenCommandHandler |
| 120 | private | ` private static string ComputeTokenHash(string token)` | RefreshTokenCommandHandler |
| 18 | public | ` public RevokeAllUserTokensCommandHandler( IAuthSessionRepository authSessionRepository, ISecurityAuditService securityAuditService, ILogger<RevokeAll…` | RevokeAllUserTokensCommandHandler |
| 28 | public | ` public async Task<Result<bool>> Handle( RevokeAllUserTokensCommand request, CancellationToken cancellationToken)` | RevokeAllUserTokensCommandHandler |
| 16 | public | ` public partial UserDetailDto ToUserDetailDto(UserCredentialDto user);` | AuthUserMapper |
| 18 | public | ` public ValidateTokenQueryHandler( IJwtService jwtService, IAuthSessionRepository authSessionRepository, ILogger<ValidateTokenQueryHandler> logger)` | ValidateTokenQueryHandler |
| 28 | public | ` public async Task<Result<ValidateTokenResult>> Handle( ValidateTokenQuery request, CancellationToken cancellationToken)` | ValidateTokenQueryHandler |
| 67 | private | ` private static string ComputeTokenHash(string token)` | ValidateTokenQueryHandler |
| 24 | public | ` public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)` | AuthModule |
| 24 | public | ` public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)` | AuthDbContext |
| 28 | protected | ` protected override void OnModelCreating(ModelBuilder modelBuilder)` | AuthDbContext |
| 14 | public | ` public AuthSessionRepository(AuthDbContext context)` | AuthSessionRepository |
| 20 | public | ` public async Task<AuthSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)` | AuthSessionRepository |
| 27 | public | ` public async Task AddAsync(AuthSession session, CancellationToken cancellationToken = default)` | AuthSessionRepository |
| 34 | public | ` public async Task UpdateAsync(AuthSession session, CancellationToken cancellationToken = default)` | AuthSessionRepository |
| 41 | public | ` public async Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default)` | AuthSessionRepository |
| 10 | public | ` public SecurityAuditRepository(AuthDbContext context) => _context = context;` | SecurityAuditRepository |
| 12 | public | ` public async Task AddAsync(SecurityAuditLog log, CancellationToken ct = default)` | SecurityAuditRepository |
| 17 | public | ` public async Task<int> SaveChangesAsync(CancellationToken ct = default)` | SecurityAuditRepository |
| 13 | public (interface 默认) | ` Task<AuthSession?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);` | IAuthSessionRepository |
| 18 | public (interface 默认) | ` Task AddAsync(AuthSession session, CancellationToken ct);` | IAuthSessionRepository |
| 23 | public (interface 默认) | ` Task UpdateAsync(AuthSession session, CancellationToken ct);` | IAuthSessionRepository |
| 28 | public (interface 默认) | ` Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken ct);` | IAuthSessionRepository |
| 21 | public (interface 默认) | ` string GenerateToken(string userId, string userName, UserRole role, string userType = "user");` | IJwtService |
| 31 | public (interface 默认) | ` string GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims, string userType = "user");` | IJwtService |
| 38 | public (interface 默认) | ` ClaimsPrincipal? ValidateToken(string token);` | IJwtService |
| 45 | public (interface 默认) | ` Result<LoginResponse> RefreshToken(string expiredToken);` | IJwtService |
| 52 | public (interface 默认) | ` Result<LoginResponse> ValidateAutoLoginToken(string autoLoginToken);` | IJwtService |
| 7 | public (interface 默认) | ` Task AddAsync(SecurityAuditLog log, CancellationToken ct = default);` | ISecurityAuditRepository |
| 8 | public (interface 默认) | ` Task<int> SaveChangesAsync(CancellationToken ct = default);` | ISecurityAuditRepository |
| 7 | public (interface 默认) | ` Task RecordEventAsync(SecurityAuditEvent auditEvent, CancellationToken ct = default);` | ISecurityAuditService |
| 15 | public | ` public AuthCrossModuleService( IAuthSessionRepository authSessionRepository, ISecurityAuditService securityAuditService)` | AuthCrossModuleService |
| 23 | public | ` public async Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default)` | AuthCrossModuleService |
| 26 | public | ` public async Task RecordSecurityAuditAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)` | AuthCrossModuleService |
| 28 | public | ` public JwtService(IOptionsMonitor<JwtOptions> jwtOptionsMonitor, IWebHostEnvironment environment)` | JwtService |
| 44 | private | ` private void ValidateSecretKeyStrength()` | JwtService |
| 79 | public | ` public string GenerateToken(string userId, string userName, UserRole role, string userType = "user")` | JwtService |
| 130 | public | ` public string GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims, string userType = "user")` | JwtService |
| 190 | public | ` public ClaimsPrincipal? ValidateToken(string token)` | JwtService |
| 225 | public | ` public Result<LoginResponse> RefreshToken(string expiredToken)` | JwtService |
| 303 | public | ` public Result<LoginResponse> ValidateAutoLoginToken(string autoLoginToken)` | JwtService |
| 13 | public | ` public SecurityAuditService(ISecurityAuditRepository repository, ILogger<SecurityAuditService> logger)` | SecurityAuditService |
| 19 | public | ` public async Task RecordEventAsync(SecurityAuditEvent auditEvent, CancellationToken ct = default)` | SecurityAuditService |
### LYBT.Module.Formula

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 15 | public | ` public BatchDeleteFormulasCommandHandler( IFormulaRepository formulaRepository)` | BatchDeleteFormulasCommandHandler |
| 21 | public | ` public async Task<Result<BatchOperationResultDto>> Handle( BatchDeleteFormulasCommand request, CancellationToken cancellationToken)` | BatchDeleteFormulasCommandHandler |
| 27 | protected | ` protected override Task<Formula?> GetByIdAsync(Guid id, CancellationToken ct)` | BatchDeleteFormulasCommandHandler |
| 30 | protected | ` protected override Task UpdateAsync(Formula formula, CancellationToken ct)` | BatchDeleteFormulasCommandHandler |
| 33 | protected | ` protected override Task ApplyOperationAsync(Formula formula, Guid operatorId, CancellationToken ct)` | BatchDeleteFormulasCommandHandler |
| 43 | protected | ` protected override void FinalizeResult(BatchOperationResultDto result)` | BatchDeleteFormulasCommandHandler |
| 46 | protected | ` protected override string BuildMessage(int successCount, int failureCount)` | BatchDeleteFormulasCommandHandler |
| 15 | public | ` public BatchDisableFormulasCommandHandler(IFormulaRepository formulaRepository)` | BatchDisableFormulasCommandHandler |
| 20 | public | ` public async Task<Result<BatchOperationResultDto>> Handle( BatchDisableFormulasCommand request, CancellationToken cancellationToken)` | BatchDisableFormulasCommandHandler |
| 15 | public | ` public BatchEnableFormulasCommandHandler(IFormulaRepository formulaRepository)` | BatchEnableFormulasCommandHandler |
| 20 | public | ` public async Task<Result<BatchOperationResultDto>> Handle( BatchEnableFormulasCommand request, CancellationToken cancellationToken)` | BatchEnableFormulasCommandHandler |
| 17 | public | ` public async Task<Result<FormulaBatchImportResultDto>> Handle( BatchImportFormulasCommand request, CancellationToken cancellationToken)` | BatchImportFormulasCommandHandler |
| 20 | public | ` public CreateFormulaCommandHandler( IFormulaRepository formulaRepository)` | CreateFormulaCommandHandler |
| 27 | public | ` public async Task<Result<FormulaDetailDto>> Handle( CreateFormulaCommand request, CancellationToken cancellationToken)` | CreateFormulaCommandHandler |
| 18 | public | ` public DeleteFormulaCommandHandler( IFormulaRepository formulaRepository)` | DeleteFormulaCommandHandler |
| 25 | public | ` public async Task<Result> Handle( DeleteFormulaCommand request, CancellationToken cancellationToken)` | DeleteFormulaCommandHandler |
| 17 | public | ` public RestoreFormulaCommandHandler(IFormulaRepository formulaRepository)` | RestoreFormulaCommandHandler |
| 22 | public | ` public async Task<Result<FormulaDetailDto>> Handle( RestoreFormulaCommand request, CancellationToken cancellationToken)` | RestoreFormulaCommandHandler |
| 18 | public | ` public ToggleFormulaStatusCommandHandler(IFormulaRepository formulaRepository)` | ToggleFormulaStatusCommandHandler |
| 23 | public | ` public async Task<Result<FormulaDetailDto>> Handle( ToggleFormulaStatusCommand request, CancellationToken cancellationToken)` | ToggleFormulaStatusCommandHandler |
| 17 | public | ` public UpdateFormulaCommandHandler(IFormulaRepository formulaRepository)` | UpdateFormulaCommandHandler |
| 22 | public | ` public async Task<Result<FormulaDetailDto>> Handle( UpdateFormulaCommand request, CancellationToken cancellationToken)` | UpdateFormulaCommandHandler |
| 19 | public | ` public async Task<Result> Handle( ValidateFormulaHerbCommand request, CancellationToken cancellationToken)` | ValidateFormulaHerbCommandHandler |
| 19 | public | ` public static Formula ToEntity(FormulaInputDto dto, Guid? createdBy = null) => Formula.Create( dto.Name, dto.Effect, dto.Indications, dto.Usage, dto.…` | FormulaDtoMapper |
| 38 | public | ` public static partial FormulaListDto ToListDto(Formula entity);` | FormulaDtoMapper |
| 45 | public | ` public static FormulaDetailDto ToDetailDto(Formula entity) => new()` | FormulaDtoMapper |
| 71 | public | ` public static FormulaHerbItemDto ToHerbItemDto(FormulaHerbItem entity) => new()` | FormulaDtoMapper |
| 18 | public | ` public async Task<Result<List<FormulaDetailDto>>> Handle( GetPendingValidationQuery request, CancellationToken cancellationToken)` | GetPendingValidationQueryHandler |
| 11 | public | ` public BatchDisableFormulasValidator()` | BatchDisableFormulasValidator |
| 11 | public | ` public BatchEnableFormulasValidator()` | BatchEnableFormulasValidator |
| 14 | public | ` public CreateFormulaValidator()` | CreateFormulaValidator |
| 8 | public | ` public FormulaBatchImportCommandValidator()` | FormulaBatchImportCommandValidator |
| 11 | public | ` public RestoreFormulaValidator()` | RestoreFormulaValidator |
| 11 | public | ` public ToggleFormulaStatusValidator()` | ToggleFormulaStatusValidator |
| 11 | public | ` public UpdateFormulaValidator()` | UpdateFormulaValidator |
| 24 | public | ` public static IServiceCollection AddFormulaModule(this IServiceCollection services, IConfiguration configuration)` | FormulaModule |
| 36 | private | ` private static IServiceCollection AddFormulaModuleDDD(this IServiceCollection services, IConfiguration configuration)` | FormulaModule |
| 19 | public | ` public FormulaDbContext(DbContextOptions<FormulaDbContext> options) : base(options)` | FormulaDbContext |
| 24 | protected | ` protected override void OnModelCreating(ModelBuilder modelBuilder)` | FormulaDbContext |
| 18 | public | ` public FormulaRepository(FormulaDbContext context, ILogger<FormulaRepository> logger)` | FormulaRepository |
| 24 | public | ` public override async Task<Formula?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)` | FormulaRepository |
| 32 | public | ` public async Task<Formula?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)` | FormulaRepository |
| 41 | public | ` public async Task<PagedResult<Formula>> GetPagedAsync( int page, int pageSize, string? keyword, string? category, CancellationToken cancellationToken…` | FormulaRepository |
| 81 | public | ` public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)` | FormulaRepository |
| 93 | public | ` public async Task<List<Formula>> FindWithHerbsAsync( System.Linq.Expressions.Expression<Func<Formula, bool>> predicate, CancellationToken cancellatio…` | FormulaRepository |
| 15 | public (interface 默认) | ` Task<Formula?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);` | IFormulaRepository |
| 20 | public (interface 默认) | ` Task<PagedResult<Formula>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, CancellationToken ct);` | IFormulaRepository |
| 25 | public (interface 默认) | ` Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);` | IFormulaRepository |
| 30 | public (interface 默认) | ` Task<List<Formula>> FindWithHerbsAsync( System.Linq.Expressions.Expression<Func<Formula, bool>> predicate, CancellationToken ct = default);` | IFormulaRepository |
| 11 | public (interface 默认) | ` Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);` | IFormulaService |
| 12 | public (interface 默认) | ` Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);` | IFormulaService |
| 16 | public | ` public FormulaService(IFormulaRepository formulaRepository)` | FormulaService |
| 21 | public | ` public async Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)` | FormulaService |
| 35 | public | ` public async Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)` | FormulaService |
### LYBT.Module.Herbs

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 20 | public | ` public BatchDeleteHerbsCommandHandler( IHerbRepository herbRepository, ICacheInvalidationService cacheInvalidation)` | BatchDeleteHerbsCommandHandler |
| 28 | public | ` public Task<Result<BatchOperationResultDto>> Handle( BatchDeleteHerbsCommand request, CancellationToken cancellationToken)` | BatchDeleteHerbsCommandHandler |
| 32 | protected | ` protected override Task<Herb?> GetByIdAsync(Guid id, CancellationToken ct)` | BatchDeleteHerbsCommandHandler |
| 35 | protected | ` protected override Task UpdateAsync(Herb herb, CancellationToken ct)` | BatchDeleteHerbsCommandHandler |
| 38 | protected | ` protected override Task ApplyOperationAsync(Herb herb, Guid operatorId, CancellationToken ct)` | BatchDeleteHerbsCommandHandler |
| 48 | protected | ` protected override async Task OnBatchCompletedAsync(BatchOperationResultDto result, CancellationToken ct)` | BatchDeleteHerbsCommandHandler |
| 54 | protected | ` protected override void FinalizeResult(BatchOperationResultDto result)` | BatchDeleteHerbsCommandHandler |
| 57 | protected | ` protected override string BuildMessage(int successCount, int failureCount)` | BatchDeleteHerbsCommandHandler |
| 15 | public | ` public BatchDisableHerbsCommandHandler(IHerbRepository herbRepository)` | BatchDisableHerbsCommandHandler |
| 20 | public | ` public async Task<Result<BatchOperationResultDto>> Handle( BatchDisableHerbsCommand request, CancellationToken cancellationToken)` | BatchDisableHerbsCommandHandler |
| 15 | public | ` public BatchEnableHerbsCommandHandler(IHerbRepository herbRepository)` | BatchEnableHerbsCommandHandler |
| 20 | public | ` public async Task<Result<BatchOperationResultDto>> Handle( BatchEnableHerbsCommand request, CancellationToken cancellationToken)` | BatchEnableHerbsCommandHandler |
| 19 | public | ` public BatchImportHerbsCommandHandler(IHerbRepository herbRepository)` | BatchImportHerbsCommandHandler |
| 24 | public | ` public async Task<Result<HerbBatchImportResultDto>> Handle( BatchImportHerbsCommand request, CancellationToken cancellationToken)` | BatchImportHerbsCommandHandler |
| 17 | public | ` public CreateHerbCommandHandler( IHerbRepository herbRepository)` | CreateHerbCommandHandler |
| 23 | public | ` public async Task<Result<HerbDetailDto>> Handle( CreateHerbCommand request, CancellationToken cancellationToken)` | CreateHerbCommandHandler |
| 15 | public | ` public DeleteHerbCommandHandler( IHerbRepository herbRepository)` | DeleteHerbCommandHandler |
| 21 | public | ` public async Task<Result> Handle( DeleteHerbCommand request, CancellationToken cancellationToken)` | DeleteHerbCommandHandler |
| 17 | public | ` public RestoreHerbCommandHandler(IHerbRepository herbRepository)` | RestoreHerbCommandHandler |
| 22 | public | ` public async Task<Result<HerbDetailDto>> Handle( RestoreHerbCommand request, CancellationToken cancellationToken)` | RestoreHerbCommandHandler |
| 18 | public | ` public ToggleHerbStatusCommandHandler(IHerbRepository herbRepository)` | ToggleHerbStatusCommandHandler |
| 23 | public | ` public async Task<Result<HerbDetailDto>> Handle( ToggleHerbStatusCommand request, CancellationToken cancellationToken)` | ToggleHerbStatusCommandHandler |
| 17 | public | ` public UpdateHerbCommandHandler(IHerbRepository herbRepository)` | UpdateHerbCommandHandler |
| 22 | public | ` public async Task<Result<HerbDetailDto>> Handle( UpdateHerbCommand request, CancellationToken cancellationToken)` | UpdateHerbCommandHandler |
| 19 | public | ` public static Herb ToEntity(HerbInputDto dto, Guid? createdBy = null) => Herb.Create( dto.Name, dto.Unit, dto.Price, dto.PinYinCode, dto.Category, dt…` | HerbDtoMapper |
| 37 | public | ` public static partial HerbListDto ToListDto(Herb entity);` | HerbDtoMapper |
| 42 | public | ` public static partial HerbDetailDto ToDetailDto(Herb entity);` | HerbDtoMapper |
| 16 | public | ` public CheckHerbReferenceQueryHandler( IHerbRepository herbRepository, IHerbReferenceRepository referenceRepository)` | CheckHerbReferenceQueryHandler |
| 24 | public | ` public async Task<Result<HerbReferenceCheckDto>> Handle( CheckHerbReferenceQuery request, CancellationToken cancellationToken)` | CheckHerbReferenceQueryHandler |
| 57 | public | ` public async Task<Result<List<HerbReferenceCheckDto>>> Handle( BatchCheckHerbReferenceQuery request, CancellationToken cancellationToken)` | CheckHerbReferenceQueryHandler |
| 11 | public | ` public BatchDisableHerbsValidator()` | BatchDisableHerbsValidator |
| 11 | public | ` public BatchEnableHerbsValidator()` | BatchEnableHerbsValidator |
| 11 | public | ` public CreateHerbValidator()` | CreateHerbValidator |
| 8 | public | ` public HerbBatchImportCommandValidator()` | HerbBatchImportCommandValidator |
| 11 | public | ` public RestoreHerbValidator()` | RestoreHerbValidator |
| 11 | public | ` public ToggleHerbStatusValidator()` | ToggleHerbStatusValidator |
| 11 | public | ` public UpdateHerbValidator()` | UpdateHerbValidator |
| 27 | public | ` public static IServiceCollection AddHerbsModule(this IServiceCollection services, IConfiguration configuration)` | HerbsModule |
| 17 | public | ` public HerbReferenceRepository(HerbsDbContext dbContext, ILogger<HerbReferenceRepository> logger)` | HerbReferenceRepository |
| 22 | public | ` public async Task<int> GetPrescriptionReferenceCountAsync(Guid herbId, CancellationToken ct = default)` | HerbReferenceRepository |
| 28 | public | ` public async Task<int> GetFormulaReferenceCountAsync(Guid herbId, CancellationToken ct = default)` | HerbReferenceRepository |
| 34 | public | ` public async Task<List<PrescriptionReferenceDto>> GetRecentPrescriptionReferencesAsync(Guid herbId, int take, CancellationToken ct = default)` | HerbReferenceRepository |
| 55 | public | ` public async Task<Dictionary<Guid, int>> GetBatchPrescriptionReferenceCountsAsync(List<Guid> herbIds, CancellationToken ct = default)` | HerbReferenceRepository |
| 69 | public | ` public async Task<Dictionary<Guid, int>> GetBatchFormulaReferenceCountsAsync(List<Guid> herbIds, CancellationToken ct = default)` | HerbReferenceRepository |
| 15 | public | ` public HerbRepository(HerbsDbContext context, ILogger<HerbRepository> logger)` | HerbRepository |
| 21 | public | ` public async Task<Herb?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)` | HerbRepository |
| 29 | public | ` public async Task<PagedResult<Herb>> GetPagedAsync( int page, int pageSize, string? keyword, string? category, CancellationToken cancellationToken = …` | HerbRepository |
| 68 | public | ` public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)` | HerbRepository |
| 80 | public | ` public async Task<Herb?> GetByNameAsync(string name, CancellationToken cancellationToken = default)` | HerbRepository |
| 37 | public | ` public HerbsDbContext(DbContextOptions<HerbsDbContext> options) : base(options)` | HerbsDbContext |
| 41 | protected | ` protected override void OnModelCreating(ModelBuilder modelBuilder)` | HerbsDbContext |
| 12 | public (interface 默认) | ` Task<int> GetPrescriptionReferenceCountAsync(Guid herbId, CancellationToken ct = default);` | IHerbReferenceRepository |
| 15 | public (interface 默认) | ` Task<int> GetFormulaReferenceCountAsync(Guid herbId, CancellationToken ct = default);` | IHerbReferenceRepository |
| 18 | public (interface 默认) | ` Task<List<PrescriptionReferenceDto>> GetRecentPrescriptionReferencesAsync(Guid herbId, int take, CancellationToken ct = default);` | IHerbReferenceRepository |
| 21 | public (interface 默认) | ` Task<Dictionary<Guid, int>> GetBatchPrescriptionReferenceCountsAsync(List<Guid> herbIds, CancellationToken ct = default);` | IHerbReferenceRepository |
| 24 | public (interface 默认) | ` Task<Dictionary<Guid, int>> GetBatchFormulaReferenceCountsAsync(List<Guid> herbIds, CancellationToken ct = default);` | IHerbReferenceRepository |
| 15 | public (interface 默认) | ` Task<Herb?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);` | IHerbRepository |
| 20 | public (interface 默认) | ` Task<PagedResult<Herb>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, CancellationToken ct);` | IHerbRepository |
| 25 | public (interface 默认) | ` Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);` | IHerbRepository |
| 30 | public (interface 默认) | ` Task<Herb?> GetByNameAsync(string name, CancellationToken ct = default);` | IHerbRepository |
| 11 | public (interface 默认) | ` Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);` | IHerbService |
| 12 | public (interface 默认) | ` Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);` | IHerbService |
| 19 | public | ` public HerbCrossModuleService(HerbsDbContext context, ILogger<HerbCrossModuleService> logger)` | HerbCrossModuleService |
| 25 | public | ` public async Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken cancellationToken = default)` | HerbCrossModuleService |
| 40 | public | ` public async Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin, CancellationToken cancellationToken = default)` | HerbCrossModuleService |
| 59 | public | ` public async Task<ReferenceCheckResult> CheckHerbReferenceAsync(Guid herbId, CancellationToken cancellationToken = default)` | HerbCrossModuleService |
| 71 | public | ` public async Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)` | HerbCrossModuleService |
| 94 | public | ` public async Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)` | HerbCrossModuleService |
| 118 | public | ` public async Task<List<HerbBasicDto>> GetAllActiveHerbsAsync(CancellationToken cancellationToken = default)` | HerbCrossModuleService |
| 16 | public | ` public HerbService(IHerbRepository herbRepository)` | HerbService |
| 21 | public | ` public async Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)` | HerbService |
| 35 | public | ` public async Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)` | HerbService |
### LYBT.Module.MedicalCase

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 24 | protected | ` protected BaseMedicalCasesController( ISender sender, ILogger logger, IMedicalCaseCommandService medicalCaseCommandService, IMedicalCaseQueryService …` | BaseMedicalCasesController |
| 43 | public | ` public virtual async Task<IActionResult> Search( [FromQuery] string? patientName = null, [FromQuery] string? diagnosisKeyword = null, [FromQuery] Dat…` | BaseMedicalCasesController |
| 64 | public | ` public virtual async Task<IActionResult> Query([FromQuery] MedicalCaseQueryDto query, CancellationToken ct = default)` | BaseMedicalCasesController |
| 89 | public | ` public virtual async Task<IActionResult> GetPatientConsultations( Guid patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, Cancellati…` | BaseMedicalCasesController |
| 104 | public | ` public virtual async Task<IActionResult> GetPatientPrescriptions( Guid patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, Cancellati…` | BaseMedicalCasesController |
| 119 | public | ` public virtual async Task<IActionResult> GetConsultations( Guid medicalCaseId, CancellationToken ct)` | BaseMedicalCasesController |
| 131 | public | ` public virtual async Task<IActionResult> GetPrescriptions( Guid medicalCaseId, CancellationToken ct)` | BaseMedicalCasesController |
| 143 | public | ` public virtual async Task<IActionResult> GetBatchDetails([FromBody] List<Guid> ids, CancellationToken ct)` | BaseMedicalCasesController |
| 161 | public | ` public virtual async Task<IActionResult> GetPermissions(Guid id, CancellationToken ct)` | BaseMedicalCasesController |
| 175 | public | ` public virtual async Task<IActionResult> GetAuditLogs( Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = defau…` | BaseMedicalCasesController |
| 192 | public | ` public virtual async Task<IActionResult> SetPrescriptionFlag( Guid id, [FromBody] SetPrescriptionFlagRequest request, CancellationToken ct)` | BaseMedicalCasesController |
| 213 | public | ` public virtual async Task<IActionResult> RecordPrint( Guid id, [FromBody] RecordPrintRequest request, CancellationToken ct)` | BaseMedicalCasesController |
| 234 | public | ` public virtual async Task<IActionResult> AddPrintLog( Guid id, [FromBody] PrintLogRequest request, CancellationToken ct)` | BaseMedicalCasesController |
| 34 | public | ` public MedicalCaseDbContext(DbContextOptions<MedicalCaseDbContext> options) : base(options)` | MedicalCaseDbContext |
| 38 | protected | ` protected override void OnModelCreating(ModelBuilder modelBuilder)` | MedicalCaseDbContext |
| 23 | public (interface 默认) | ` Task<MedicalCase?> SetPrescriptionFlagAsync( Guid medicalCaseId, bool needsPrescription, Guid currentUserId, bool isAdmin = false, CancellationToken …` | IMedicalCaseCommandService |
| 38 | public (interface 默认) | ` Task<bool> DeleteAsync(Guid id, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default);` | IMedicalCaseCommandService |
| 50 | public (interface 默认) | ` Task<MedicalCase?> SaveAsync( MedicalCaseInputDto request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);` | IMedicalCaseCommandService |
| 62 | public (interface 默认) | ` Task<LYBT.Shared.Models.Contracts.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid o…` | IMedicalCaseCommandService |
| 67 | public (interface 默认) | ` Task<Result<MedicalCaseDetailDto>> SaveWithDetailAsync( MedicalCaseInputDto request, Guid currentUserId, bool isAdmin = false, CancellationToken canc…` | IMedicalCaseCommandService |
| 76 | public (interface 默认) | ` Task<Result<MedicalCaseDetailDto>> SetPrescriptionFlagWithDetailAsync( Guid medicalCaseId, bool needsPrescription, Guid currentUserId, bool isAdmin =…` | IMedicalCaseCommandService |
| 86 | public (interface 默认) | ` Task<Result<bool>> AddPrintLogAsync( Guid medicalCaseId, int printType, bool isSuccess, string? printerName, Guid operatorId, string operatorName, Ca…` | IMedicalCaseCommandService |
| 98 | public (interface 默认) | ` Task<Result<bool>> RecordPrintAsync( Guid medicalCaseId, int printType, string? printerName, Guid operatorId, string operatorName, CancellationToken …` | IMedicalCaseCommandService |
| 29 | public (interface 默认) | ` Task<PagedResult<MedicalCase>> GetListAsync( MedicalCaseStatus? status, Guid? patientId, int page, int pageSize, Guid? currentDoctorId = null, bool i…` | IMedicalCaseQueryService |
| 50 | public (interface 默认) | ` Task<PagedResult<MedicalCaseListDto>> GetListDtoAsync( MedicalCaseStatus? status, Guid? patientId, int page, int pageSize, Guid? currentDoctorId = nu…` | IMedicalCaseQueryService |
| 66 | public (interface 默认) | ` Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 74 | public (interface 默认) | ` Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 83 | public (interface 默认) | ` Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 92 | public (interface 默认) | ` Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 99 | public (interface 默认) | ` Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync(CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 112 | public (interface 默认) | ` Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync( string? patientName = null, string? diagnosisKeyword = null, DateTime? startDate = n…` | IMedicalCaseQueryService |
| 129 | public (interface 默认) | ` Task<List<MedicalCaseDetailDto>> GetPatientRecentMedicalCasesAsync(Guid patientId, int count = 5, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 138 | public (interface 默认) | ` Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 143 | public (interface 默认) | ` Task<Result<MedicalCaseDetailDto>> GetDetailDtoAsync(Guid id, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 148 | public (interface 默认) | ` Task<Result<List<MedicalCaseDetailDto>>> GetBatchDetailDtosAsync(List<Guid> ids, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 153 | public (interface 默认) | ` Task<PagedResult<ConsultationDetailDto>> GetPatientConsultationsAsync(Guid patientId, int page, int pageSize, CancellationToken cancellationToken = d…` | IMedicalCaseQueryService |
| 158 | public (interface 默认) | ` Task<PagedResult<PrescriptionDetailDto>> GetPatientPrescriptionsAsync(Guid patientId, int page, int pageSize, CancellationToken cancellationToken = d…` | IMedicalCaseQueryService |
| 163 | public (interface 默认) | ` Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid caseId, int page, int pageSize, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 168 | public (interface 默认) | ` Task<Result<MedicalCasePermissionsDto>> GetPermissionsAsync(Guid caseId, Guid userId, int userRole, CancellationToken cancellationToken = default);` | IMedicalCaseQueryService |
| 7 | public (interface 默认) | ` Task<int> CountUnfinishedAsync(Guid patientId, CancellationToken ct = default);` | IMedicalCaseReferenceRepository |
| 8 | public (interface 默认) | ` Task<int> CountAllAsync(Guid patientId, CancellationToken ct = default);` | IMedicalCaseReferenceRepository |
| 9 | public (interface 默认) | ` Task<List<MedicalCaseReferenceDto>> GetRecentAsync(Guid patientId, int count, CancellationToken ct = default);` | IMedicalCaseReferenceRepository |
| 17 | public (interface 默认) | ` Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 22 | public (interface 默认) | ` Task<PagedResult<MedicalCase>> GetByPatientIdPagedAsync(Guid patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 27 | public (interface 默认) | ` Task<PagedResult<MedicalCase>> GetPatientConsultationsPagedAsync(Guid patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = …` | IMedicalCaseRepository |
| 32 | public (interface 默认) | ` Task<PagedResult<MedicalCase>> GetPatientPrescriptionsPagedAsync(Guid patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = …` | IMedicalCaseRepository |
| 37 | public (interface 默认) | ` Task<MedicalCase> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 44 | public (interface 默认) | ` Task<MedicalCase?> GetByIdWithDetailsFreshAsync(Guid id, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 50 | public (interface 默认) | ` Task<PagedResult<MedicalCase>> GetPagedWithDetailsAsync( int pageNumber, int pageSize, MedicalCaseStatus? status, Guid? patientId, Guid? doctorId, bo…` | IMedicalCaseRepository |
| 63 | public (interface 默认) | ` Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 70 | public (interface 默认) | ` Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync(CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 75 | public (interface 默认) | ` Task<PagedResult<MedicalCase>> QueryPagedAsync( string? patientName, DateTime? startDate, DateTime? endDate, string? diagnosisKeyword, int pageNumber…` | IMedicalCaseRepository |
| 93 | public (interface 默认) | ` Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 101 | public (interface 默认) | ` Task<List<MedicalCase>> GetBatchWithDetailsAsync(List<Guid> ids, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 108 | public (interface 默认) | ` Task<int> CountByPrefixAsync(string prefix, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 115 | public (interface 默认) | ` Task<int> CountPrescriptionsByPrefixAsync(string prefix, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 122 | public (interface 默认) | ` Task AddPrintLogAsync(MedicalCasePrintLog printLog, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 127 | public (interface 默认) | ` Task<List<MedicalCaseAuditLog>> GetAuditLogsAsync(Guid medicalCaseId, int page, int pageSize, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 132 | public (interface 默认) | ` Task<int> CountAuditLogsAsync(Guid medicalCaseId, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 138 | public (interface 默认) | ` Task AddAuditLogAsync(MedicalCaseAuditLog log, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 147 | public (interface 默认) | ` Task<bool> HardDeleteAsync(MedicalCase entity, CancellationToken cancellationToken = default);` | IMedicalCaseRepository |
| 22 | public (interface 默认) | ` Task<MedicalCase?> UpdateStatusAsync( Guid medicalCaseId, MedicalCaseStatus status, CancellationToken cancellationToken = default);` | IMedicalCaseStateService |
| 37 | public (interface 默认) | ` Task<MedicalCase?> CompleteAsync( Guid medicalCaseId, Guid operatorId, bool isAdmin = false, bool skipWorkflowValidation = false, CancellationToken c…` | IMedicalCaseStateService |
| 54 | public (interface 默认) | ` Task<MedicalCase?> SuspendAsync( Guid id, ConsultationInputDto? request, Guid operatorId, bool isAdmin = false, CancellationToken cancellationToken =…` | IMedicalCaseStateService |
| 71 | public (interface 默认) | ` Task<MedicalCase?> CancelAsync( Guid id, Guid operatorId, bool isAdmin = false, string? reason = null, CancellationToken cancellationToken = default)…` | IMedicalCaseStateService |
| 35 | public | ` public partial MedicalCaseListDto ToListDto(MedicalCase entity);` | MedicalCaseMapper |
| 40 | public | ` public partial List<MedicalCaseListDto> ToListDtos(List<MedicalCase> entities);` | MedicalCaseMapper |
| 58 | public | ` public partial MedicalCaseDetailDto ToDetailDto(MedicalCase entity);` | MedicalCaseMapper |
| 63 | public | ` public partial List<MedicalCaseDetailDto> ToDetailDtos(List<MedicalCase> entities);` | MedicalCaseMapper |
| 75 | public | ` public partial ConsultationDetailDto ToConsultationDetailDto(Consultation entity);` | MedicalCaseMapper |
| 89 | public | ` public partial PrescriptionDetailDto ToPrescriptionDetailDto(Prescription entity);` | MedicalCaseMapper |
| 105 | public | ` public partial Prescription ToPrescriptionEntity(PrescriptionInputDto dto);` | MedicalCaseMapper |
| 120 | public | ` public partial void UpdatePrescriptionEntity(PrescriptionInputDto dto, Prescription entity);` | MedicalCaseMapper |
| 132 | public | ` public partial PrescriptionItemDto ToPrescriptionItemDto(PrescriptionItem entity);` | MedicalCaseMapper |
| 142 | public | ` public MedicalCaseDetailDto MapToMedicalCaseDetailDto(MedicalCase entity)` | MedicalCaseMapper |
| 170 | private | ` private ConsultationDetailDto EnrichConsultationDetailDto(MedicalCase entity)` | MedicalCaseMapper |
| 184 | private | ` private PrescriptionDetailDto EnrichPrescriptionDetailDto(MedicalCase entity)` | MedicalCaseMapper |
| 29 | public | ` public static IServiceCollection AddMedicalCaseModule(this IServiceCollection services, IConfiguration configuration)` | MedicalCaseModule |
| 14 | public | ` public MedicalCaseReferenceRepository(MedicalCaseDbContext dbContext, ILogger<MedicalCaseReferenceRepository> logger)` | MedicalCaseReferenceRepository |
| 19 | public | ` public async Task<int> CountUnfinishedAsync(Guid patientId, CancellationToken ct = default)` | MedicalCaseReferenceRepository |
| 27 | public | ` public async Task<int> CountAllAsync(Guid patientId, CancellationToken ct = default)` | MedicalCaseReferenceRepository |
| 34 | public | ` public async Task<List<MedicalCaseReferenceDto>> GetRecentAsync(Guid patientId, int count, CancellationToken ct = default)` | MedicalCaseReferenceRepository |
| 16 | public | ` public async Task<List<MedicalCaseAuditLog>> GetAuditLogsAsync(Guid medicalCaseId, int page, int pageSize, CancellationToken cancellationToken = defa…` | MedicalCaseRepository |
| 29 | public | ` public async Task<int> CountAuditLogsAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 39 | public | ` public async Task AddAuditLogAsync(MedicalCaseAuditLog log, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 49 | public | ` public async Task<bool> HardDeleteAsync(MedicalCase entity, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 63 | public | ` public async Task AddPrintLogAsync(MedicalCasePrintLog printLog, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 21 | public | ` public MedicalCaseRepository(MedicalCaseDbContext context, ILogger<MedicalCaseRepository> logger)` | MedicalCaseRepository |
| 29 | private | ` private IQueryable<MedicalCase> GetBaseQuery()` | MedicalCaseRepository |
| 38 | private | ` private IQueryable<MedicalCase> GetDetailQuery()` | MedicalCaseRepository |
| 50 | public | ` public async Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 61 | public | ` public async Task<PagedResult<MedicalCase>> GetByPatientIdPagedAsync(Guid patientId, int pageNumber, int pageSize, CancellationToken cancellationToke…` | MedicalCaseRepository |
| 73 | public | ` public async Task<PagedResult<MedicalCase>> GetPatientConsultationsPagedAsync( Guid patientId, int pageNumber, int pageSize, CancellationToken cancel…` | MedicalCaseRepository |
| 87 | public | ` public async Task<PagedResult<MedicalCase>> GetPatientPrescriptionsPagedAsync( Guid patientId, int pageNumber, int pageSize, CancellationToken cancel…` | MedicalCaseRepository |
| 101 | public | ` public async Task<MedicalCase> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 112 | public | ` public async Task<PagedResult<MedicalCase>> GetPagedWithDetailsAsync( int pageNumber, int pageSize, MedicalCaseStatus? status, Guid? patientId, Guid?…` | MedicalCaseRepository |
| 156 | public | ` public async Task<PagedResult<MedicalCase>> QueryPagedAsync( string? patientName, DateTime? startDate, DateTime? endDate, string? diagnosisKeyword, i…` | MedicalCaseRepository |
| 200 | public | ` public async Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 249 | public | ` public async Task<int> CountByPrefixAsync(string prefix, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 260 | public | ` public async Task<int> CountPrescriptionsByPrefixAsync(string prefix, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 273 | public | ` public async Task<List<MedicalCase>> GetBatchWithDetailsAsync(List<Guid> ids, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 20 | public | ` public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null, CancellationToken cancellationToken = defa…` | MedicalCaseRepository |
| 77 | public | ` public async Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync(CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 124 | private | ` private static string MaskPhoneNumber(string phoneNumber)` | MedicalCaseRepository |
| 21 | public | ` public async Task<MedicalCase?> GetByIdWithDetailsFreshAsync(Guid id, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 74 | public | ` public override async Task<MedicalCase> UpdateAsync(MedicalCase entity, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 107 | private | ` private async Task FixPrescriptionEntityStatesAsync(MedicalCase entity, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 135 | private | ` private void FixNewPrescriptionItemsState(Prescription prescription)` | MedicalCaseRepository |
| 154 | private | ` private async Task FixExistingPrescriptionItemsStateAsync(Prescription prescription, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 178 | private | ` private async Task<MedicalCase> GetOrLoadExistingEntityAsync(MedicalCase entity, CancellationToken cancellationToken = default)` | MedicalCaseRepository |
| 205 | private | ` private void LogTrackedEntitiesState()` | MedicalCaseRepository |
| 34 | public | ` public MedicalCaseCommandService( IMedicalCaseRepository repository, IRegistrationCrossModuleService registrationCrossModule, ICrossModuleService cro…` | MedicalCaseCommandService |
| 62 | private | ` private async Task<MedicalCase?> CreateFromInputDtoAsync( MedicalCaseInputDto request, Guid currentUserId, bool isAdmin = false, CancellationToken ca…` | MedicalCaseCommandService |
| 143 | public | ` public Task<MedicalCase?> SetPrescriptionFlagAsync( Guid medicalCaseId, bool needsPrescription, Guid currentUserId, bool isAdmin = false, Cancellatio…` | MedicalCaseCommandService |
| 157 | public | ` public async Task<MedicalCase?> SaveAsync( MedicalCaseInputDto request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken…` | MedicalCaseCommandService |
| 178 | private | ` private async Task<MedicalCase?> ExecuteSaveAttemptAsync( MedicalCaseInputDto request, Guid medicalCaseId, Guid currentUserId, bool isAdmin, Cancella…` | MedicalCaseCommandService |
| 222 | private | ` private void ValidateEditPermission(MedicalCase medicalCase, Guid currentUserId, bool isAdmin)` | MedicalCaseCommandService |
| 228 | private | ` private static void UpdateMedicalCaseBasicFields(MedicalCase medicalCase, MedicalCaseInputDto request)` | MedicalCaseCommandService |
| 236 | private | ` private static void UpdateConsultationFields(Consultation consultation, ConsultationInputDto dto)` | MedicalCaseCommandService |
| 248 | public | ` public async Task<LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>> SaveWithDetailAsync( MedicalCaseInputDto request, Guid currentUse…` | MedicalCaseCommandService |
| 265 | public | ` public async Task<LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>> SetPrescriptionFlagWithDetailAsync( Guid medicalCaseId, bool need…` | MedicalCaseCommandService |
| 283 | public | ` public async Task<LYBT.Shared.Models.Contracts.Common.Result<bool>> AddPrintLogAsync( Guid medicalCaseId, int printType, bool isSuccess, string? prin…` | MedicalCaseCommandService |
| 330 | public | ` public async Task<LYBT.Shared.Models.Contracts.Common.Result<bool>> RecordPrintAsync( Guid medicalCaseId, int printType, string? printerName, Guid op…` | MedicalCaseCommandService |
| 375 | private | ` private async Task<string> GenerateCaseNumberAsync(CancellationToken cancellationToken = default)` | MedicalCaseCommandService |
| 23 | public | ` public async Task<bool> DeleteAsync(Guid id, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default)` | MedicalCaseCommandService |
| 58 | public | ` public async Task<LYBT.Shared.Models.Contracts.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>> BatchDeleteAsync(List<Guid…` | MedicalCaseCommandService |
| 18 | public | ` public MedicalCaseCrossModuleService( IMedicalCaseReferenceRepository referenceRepository, IMedicalCaseCommandService commandService)` | MedicalCaseCrossModuleService |
| 27 | public | ` public async Task<int> CountUnfinishedMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default)` | MedicalCaseCrossModuleService |
| 33 | public | ` public async Task<int> CountMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default)` | MedicalCaseCrossModuleService |
| 39 | public | ` public async Task<List<MedicalCaseReferenceDto>> GetRecentMedicalCasesAsync(Guid patientId, int count, CancellationToken cancellationToken = default)` | MedicalCaseCrossModuleService |
| 45 | public | ` public async Task<Guid?> CreateMedicalCaseForRegistrationAsync(Guid patientId, Guid registrationId, Guid doctorId, CancellationToken cancellationToke…` | MedicalCaseCrossModuleService |
| 19 | public | ` public MedicalCasePrescriptionService( IMedicalCaseRepository repository, ICacheInvalidationService cacheInvalidation, ILogger<MedicalCasePrescriptio…` | MedicalCasePrescriptionService |
| 34 | public | ` public async Task<MedicalCase?> SetPrescriptionFlagAsync( Guid medicalCaseId, bool needsPrescription, Guid currentUserId, bool isAdmin = false, Cance…` | MedicalCasePrescriptionService |
| 25 | public | ` public MedicalCaseQueryService( IMedicalCaseRepository repository, MedicalCaseMapper mapper, ILogger<MedicalCaseQueryService> logger)` | MedicalCaseQueryService |
| 40 | public | ` public async Task<PagedResult<MedicalCase>> GetListAsync( MedicalCaseStatus? status, Guid? patientId, int page, int pageSize, Guid? currentDoctorId =…` | MedicalCaseQueryService |
| 59 | public | ` public async Task<PagedResult<MedicalCaseListDto>> GetListDtoAsync( MedicalCaseStatus? status, Guid? patientId, int page, int pageSize, Guid? current…` | MedicalCaseQueryService |
| 94 | public | ` public async Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 112 | public | ` public async Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 131 | public | ` public async Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 158 | public | ` public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null, CancellationToken cancellationToken = defa…` | MedicalCaseQueryService |
| 177 | public | ` public async Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync(CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 192 | public | ` public async Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync( string? patientName = null, string? diagnosisKeyword = null, DateTime? …` | MedicalCaseQueryService |
| 223 | public | ` public async Task<List<MedicalCaseDetailDto>> GetPatientRecentMedicalCasesAsync(Guid patientId, int count = 5, CancellationToken cancellationToken = …` | MedicalCaseQueryService |
| 256 | public | ` public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 271 | private | ` private async Task<PagedResult<MedicalCaseListDto>> QueryByPatientAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default) {` | MedicalCaseQueryService |
| 286 | private | ` private async Task<PagedResult<MedicalCaseListDto>> QueryPendingAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 313 | private | ` private async Task<PagedResult<MedicalCaseListDto>> QueryUnfinishedAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 333 | private | ` private async Task<PagedResult<MedicalCaseListDto>> QueryRecentAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 369 | public | ` public async Task<Result<MedicalCaseDetailDto>> GetDetailDtoAsync(Guid id, CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 382 | public | ` public async Task<Result<List<MedicalCaseDetailDto>>> GetBatchDetailDtosAsync(List<Guid> ids, CancellationToken cancellationToken = default)` | MedicalCaseQueryService |
| 392 | public | ` public async Task<PagedResult<ConsultationDetailDto>> GetPatientConsultationsAsync( Guid patientId, int page, int pageSize, CancellationToken cancell…` | MedicalCaseQueryService |
| 420 | public | ` public async Task<PagedResult<PrescriptionDetailDto>> GetPatientPrescriptionsAsync( Guid patientId, int page, int pageSize, CancellationToken cancell…` | MedicalCaseQueryService |
| 449 | public | ` public async Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsAsync( Guid caseId, int page, int pageSize, CancellationToken cancellationToken = def…` | MedicalCaseQueryService |
| 482 | public | ` public async Task<Result<MedicalCasePermissionsDto>> GetPermissionsAsync( Guid caseId, Guid userId, int userRole, CancellationToken cancellationToken…` | MedicalCaseQueryService |
| 113 | public | ` public static async Task<T> ExecuteWithConcurrencyRetryAsync<T>( Func<Task<T>> action, string operationName, ILogger logger, int maxRetries = 3)` | MedicalCaseServiceHelper |
| 146 | public | ` public static void EnsureCanEdit( MedicalCase medicalCase, Guid userId, bool isAdmin, string operation, ILogger logger)` | MedicalCaseServiceHelper |
| 168 | public | ` public static void ResetPrintMarker(MedicalCase medicalCase)` | MedicalCaseServiceHelper |
| 179 | public | ` public static void EnsureCanDelete( MedicalCase medicalCase, Guid userId, bool isAdmin, string operation, ILogger logger)` | MedicalCaseServiceHelper |
| 32 | public | ` public MedicalCaseStateService( IMedicalCaseRepository repository, ICrossModuleService crossModule, ILogger<MedicalCaseStateService> logger, ICacheIn…` | MedicalCaseStateService |
| 50 | public | ` public async Task<MedicalCase?> UpdateStatusAsync( Guid medicalCaseId, MedicalCaseStatus status, CancellationToken cancellationToken = default)` | MedicalCaseStateService |
| 95 | public | ` public async Task<MedicalCase?> CompleteAsync( Guid medicalCaseId, Guid operatorId, bool isAdmin = false, bool skipWorkflowValidation = false, Cancel…` | MedicalCaseStateService |
| 169 | public | ` public async Task<MedicalCase?> SuspendAsync( Guid id, ConsultationInputDto? request, Guid operatorId, bool isAdmin = false, CancellationToken cancel…` | MedicalCaseStateService |
| 226 | public | ` public async Task<MedicalCase?> CancelAsync( Guid id, Guid operatorId, bool isAdmin = false, string? reason = null, CancellationToken cancellationTok…` | MedicalCaseStateService |
| 293 | private | ` private async Task TryWriteCancelAuditAsync( MedicalCase medicalCase, Guid operatorId, bool isAdmin, string? reason, CancellationToken cancellationTo…` | MedicalCaseStateService |
| 329 | private | ` private async Task RollbackRegistrationAsync(Guid medicalCaseId, string caseNumber, CancellationToken cancellationToken = default)` | MedicalCaseStateService |
| 339 | private | ` private async Task CompleteRegistrationAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)` | MedicalCaseStateService |
| 20 | public | ` public PrescriptionItemService( IMedicalCaseRepository repository, ICrossModuleService crossModule, ILogger<PrescriptionItemService> logger)` | PrescriptionItemService |
| 34 | public | ` public async Task HandlePrescriptionUpdateAsync( MedicalCase medicalCase, PrescriptionInputDto prescriptionDto, CancellationToken cancellationToken =…` | PrescriptionItemService |
| 60 | public | ` public Task SoftDeletePrescriptionIfExists(MedicalCase medicalCase)` | PrescriptionItemService |
| 76 | public | ` public async Task CreateNewPrescriptionAsync( MedicalCase medicalCase, PrescriptionInputDto prescriptionDto, CancellationToken cancellationToken = de…` | PrescriptionItemService |
| 106 | public | ` public async Task UpdateExistingPrescriptionAsync( Prescription prescription, PrescriptionInputDto prescriptionDto, CancellationToken cancellationTok…` | PrescriptionItemService |
| 136 | public | ` public async Task<List<LYBT.Entities.Prescriptions.PrescriptionItem>> CreatePrescriptionItemsAsync( Guid prescriptionId, PrescriptionInputDto prescri…` | PrescriptionItemService |
| 209 | public | ` public async Task<string> GeneratePrescriptionNumberAsync(CancellationToken cancellationToken = default)` | PrescriptionItemService |
### LYBT.Module.Patients

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 20 | public | ` public BatchDeletePatientsCommandHandler( IPatientRepository patientRepository, IMedicalCaseCrossModuleService medicalCaseCrossModuleService)` | BatchDeletePatientsCommandHandler |
| 28 | public | ` public Task<Result<BatchOperationResultDto>> Handle( BatchDeletePatientsCommand request, CancellationToken cancellationToken)` | BatchDeletePatientsCommandHandler |
| 32 | protected | ` protected override Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct)` | BatchDeletePatientsCommandHandler |
| 35 | protected | ` protected override Task UpdateAsync(Patient patient, CancellationToken ct)` | BatchDeletePatientsCommandHandler |
| 38 | protected | ` protected override Task ApplyOperationAsync(Patient patient, Guid operatorId, CancellationToken ct)` | BatchDeletePatientsCommandHandler |
| 48 | protected | ` protected override async Task<string?> ValidateAsync( Patient patient, Guid id, Guid operatorId, CancellationToken ct)` | BatchDeletePatientsCommandHandler |
| 55 | protected | ` protected override void FinalizeResult(BatchOperationResultDto result)` | BatchDeletePatientsCommandHandler |
| 58 | protected | ` protected override string BuildMessage(int successCount, int failureCount)` | BatchDeletePatientsCommandHandler |
| 18 | public | ` public BatchImportPatientsCommandHandler(IPatientRepository patientRepository)` | BatchImportPatientsCommandHandler |
| 23 | public | ` public async Task<Result<PatientBatchImportResultDto>> Handle( BatchImportPatientsCommand request, CancellationToken cancellationToken)` | BatchImportPatientsCommandHandler |
| 17 | public | ` public CreatePatientCommandHandler( IPatientRepository patientRepository)` | CreatePatientCommandHandler |
| 23 | public | ` public async Task<Result<PatientDetailDto>> Handle( CreatePatientCommand request, CancellationToken cancellationToken)` | CreatePatientCommandHandler |
| 17 | public | ` public DeletePatientCommandHandler( IPatientRepository patientRepository, IMedicalCaseCrossModuleService medicalCaseCrossModuleService)` | DeletePatientCommandHandler |
| 25 | public | ` public async Task<Result> Handle( DeletePatientCommand request, CancellationToken cancellationToken)` | DeletePatientCommandHandler |
| 17 | public | ` public RestorePatientCommandHandler(IPatientRepository patientRepository)` | RestorePatientCommandHandler |
| 22 | public | ` public async Task<Result<PatientDetailDto>> Handle( RestorePatientCommand request, CancellationToken cancellationToken)` | RestorePatientCommandHandler |
| 20 | public | ` public TogglePatientStatusCommandHandler( IPatientRepository patientRepository, IMedicalCaseCrossModuleService medicalCaseCrossModuleService)` | TogglePatientStatusCommandHandler |
| 28 | public | ` public async Task<Result<PatientDetailDto>> Handle( TogglePatientStatusCommand request, CancellationToken cancellationToken)` | TogglePatientStatusCommandHandler |
| 17 | public | ` public UpdatePatientCommandHandler(IPatientRepository patientRepository)` | UpdatePatientCommandHandler |
| 22 | public | ` public async Task<Result<PatientDetailDto>> Handle( UpdatePatientCommand request, CancellationToken cancellationToken)` | UpdatePatientCommandHandler |
| 19 | public | ` public static Patient ToEntity(PatientInputDto dto, Guid? createdBy = null) => Patient.Create( dto.Name, dto.Gender, dto.BirthDate, dto.PhoneNumber, …` | PatientMapper |
| 31 | public | ` public static partial PatientListDto ToListDto(Patient entity);` | PatientMapper |
| 36 | public | ` public static partial PatientDetailDto ToDetailDto(Patient entity);` | PatientMapper |
| 19 | public | ` public async Task<Result<List<PatientReferenceCheckDto>>> Handle( BatchCheckPatientReferenceQuery request, CancellationToken cancellationToken)` | BatchCheckPatientReferenceQueryHandler |
| 20 | public | ` public async Task<Result<PatientReferenceCheckDto>> Handle( CheckPatientReferenceQuery request, CancellationToken cancellationToken)` | CheckPatientReferenceQueryHandler |
| 11 | public | ` public CreatePatientValidator()` | CreatePatientValidator |
| 11 | public | ` public RestorePatientValidator()` | RestorePatientValidator |
| 11 | public | ` public UpdatePatientValidator()` | UpdatePatientValidator |
| 17 | public | ` public PatientRepository(PatientsDbContext context, ILogger<PatientRepository> logger)` | PatientRepository |
| 23 | public | ` public async Task<PagedResult<Patient>> GetPagedAsync( int page, int pageSize, string? keyword, CancellationToken cancellationToken = default)` | PatientRepository |
| 31 | public | ` public async Task<PagedResult<Patient>> GetPagedAsync( int page, int pageSize, string? keyword, CommonStatus? status, CancellationToken cancellationT…` | PatientRepository |
| 69 | public | ` public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)` | PatientRepository |
| 81 | public | ` public async Task<Patient?> GetExactByNameAsync(string name, CancellationToken cancellationToken = default)` | PatientRepository |
| 88 | public | ` public async Task<Patient?> GetByIdNumberAsync(string idNumber, CancellationToken cancellationToken = default)` | PatientRepository |
| 95 | public | ` public async Task<Patient?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)` | PatientRepository |
| 16 | public | ` public PatientsDbContext(DbContextOptions<PatientsDbContext> options) : base(options)` | PatientsDbContext |
| 20 | protected | ` protected override void OnModelCreating(ModelBuilder modelBuilder)` | PatientsDbContext |
| 16 | public (interface 默认) | ` Task<PagedResult<Patient>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);` | IPatientRepository |
| 21 | public (interface 默认) | ` Task<PagedResult<Patient>> GetPagedAsync(int page, int pageSize, string? keyword, CommonStatus? status, CancellationToken ct);` | IPatientRepository |
| 26 | public (interface 默认) | ` Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);` | IPatientRepository |
| 31 | public (interface 默认) | ` Task<Patient?> GetExactByNameAsync(string name, CancellationToken ct = default);` | IPatientRepository |
| 36 | public (interface 默认) | ` Task<Patient?> GetByIdNumberAsync(string idNumber, CancellationToken ct);` | IPatientRepository |
| 41 | public (interface 默认) | ` Task<Patient?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);` | IPatientRepository |
| 11 | public (interface 默认) | ` Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, bool filterDisabled, CancellationToken ct);` | IPatientService |
| 12 | public (interface 默认) | ` Task<Result<PatientDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);` | IPatientService |
| 13 | public (interface 默认) | ` Task<Result<PatientDetailDto>> GetByIdNumberAsync(string idNumber, CancellationToken ct);` | IPatientService |
| 26 | public | ` public static IServiceCollection AddPatientsModule(this IServiceCollection services, IConfiguration configuration)` | PatientsModule |
| 18 | public | ` public PatientCrossModuleService(PatientsDbContext context, ILogger<PatientCrossModuleService> logger)` | PatientCrossModuleService |
| 24 | public | ` public async Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken cancellationToken = default)` | PatientCrossModuleService |
| 17 | public | ` public PatientService(IPatientRepository patientRepository)` | PatientService |
| 22 | public | ` public async Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, bool filterDisabled, CancellationToken …` | PatientService |
| 37 | public | ` public async Task<Result<PatientDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)` | PatientService |
| 45 | public | ` public async Task<Result<PatientDetailDto>> GetByIdNumberAsync(string idNumber, CancellationToken ct)` | PatientService |
### LYBT.Module.Registration

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 17 | public | ` public CancelRegistrationCommandHandler( IRegistrationRepository repository, INotificationService notificationService)` | CancelRegistrationCommandHandler |
| 25 | public | ` public async Task<Result> Handle( CancelRegistrationCommand request, CancellationToken cancellationToken)` | CancelRegistrationCommandHandler |
| 21 | public | ` public CreateRegistrationCommandHandler( IRegistrationRepository repository, RegistrationMapper mapper, INotificationService notificationService)` | CreateRegistrationCommandHandler |
| 31 | public | ` public async Task<Result<RegistrationDetailDto>> Handle( CreateRegistrationCommand request, CancellationToken cancellationToken)` | CreateRegistrationCommandHandler |
| 19 | public | ` public async Task<Result<QuickVisitResultDto>> Handle( QuickVisitCommand request, CancellationToken cancellationToken)` | QuickVisitCommandHandler |
| 22 | public | ` public StartVisitCommandHandler( IRegistrationRepository repository, IMedicalCaseCrossModuleService medicalCaseCrossModule, INotificationService noti…` | StartVisitCommandHandler |
| 32 | public | ` public async Task<Result<Guid>> Handle( StartVisitCommand request, CancellationToken cancellationToken)` | StartVisitCommandHandler |
| 17 | public | ` public GetRegistrationQueryHandler(IRegistrationRepository repository, RegistrationMapper mapper)` | GetRegistrationQueryHandler |
| 23 | public | ` public async Task<RegistrationDetailDto?> Handle( GetRegistrationQuery request, CancellationToken cancellationToken)` | GetRegistrationQueryHandler |
| 18 | public | ` public GetRegistrationsQueryHandler(IRegistrationRepository repository, RegistrationMapper mapper)` | GetRegistrationsQueryHandler |
| 24 | public | ` public async Task<PagedResult<RegistrationListDto>> Handle( GetRegistrationsQuery request, CancellationToken cancellationToken)` | GetRegistrationsQueryHandler |
| 17 | public | ` public GetWaitingQueueQueryHandler(IRegistrationRepository repository, RegistrationMapper mapper)` | GetWaitingQueueQueryHandler |
| 23 | public | ` public async Task<List<RegistrationListDto>> Handle( GetWaitingQueueQuery request, CancellationToken cancellationToken)` | GetWaitingQueueQueryHandler |
| 11 | public | ` public CreateRegistrationValidator()` | CreateRegistrationValidator |
| 8 | public | ` public QuickVisitCommandValidator()` | QuickVisitCommandValidator |
| 18 | protected | ` protected BaseRegistrationsController(ISender sender, ILogger logger)` | BaseRegistrationsController |
| 29 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | BaseRegistrationsController |
| 51 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | BaseRegistrationsController |
| 69 | public | ` public Task<IActionResult> Update(Guid id, [FromBody] RegistrationInputDto dto, CancellationToken ct)` | BaseRegistrationsController |
| 73 | public | ` public override Task<IActionResult> Delete(Guid id, CancellationToken ct)` | BaseRegistrationsController |
| 77 | public | ` public override Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | BaseRegistrationsController |
| 88 | public | ` public virtual async Task<IActionResult> GetQueue([FromQuery] Guid? doctorId = null, CancellationToken ct = default)` | BaseRegistrationsController |
| 98 | public | ` public virtual async Task<IActionResult> StartVisit(Guid id, CancellationToken ct)` | BaseRegistrationsController |
| 114 | public | ` public virtual async Task<IActionResult> Cancel(Guid id, CancellationToken ct)` | BaseRegistrationsController |
| 130 | public | ` public virtual async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto, CancellationToken ct)` | BaseRegistrationsController |
| 14 | public | ` public void Add(string connectionId, Guid doctorId)` | RegistrationConnectionManager |
| 20 | public | ` public bool TryRemove(string connectionId, out Guid doctorId)` | RegistrationConnectionManager |
| 24 | public | ` public bool TryGetDoctorId(string connectionId, out Guid doctorId)` | RegistrationConnectionManager |
| 28 | public | ` public int CountConnections(Guid doctorId)` | RegistrationConnectionManager |
| 21 | public | ` public RegistrationHub(RegistrationConnectionManager connections, ILogger<RegistrationHub> logger)` | RegistrationHub |
| 28 | public | ` public override async Task OnConnectedAsync()` | RegistrationHub |
| 50 | public | ` public override async Task OnDisconnectedAsync(Exception? exception)` | RegistrationHub |
| 61 | public | ` public static string GetDoctorGroup(Guid doctorId) => $"doctor-{doctorId}";` | RegistrationHub |
| 63 | private | ` private bool TryGetDoctorId(out Guid doctorId)` | RegistrationHub |
| 16 | public | ` public RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : base(options)` | RegistrationDbContext |
| 20 | protected | ` protected override void OnModelCreating(ModelBuilder modelBuilder)` | RegistrationDbContext |
| 18 | public | ` public RegistrationRepository(RegistrationDbContext context)` | RegistrationRepository |
| 24 | public | ` public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)` | RegistrationRepository |
| 30 | public | ` public async Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)` | RegistrationRepository |
| 37 | public | ` public async Task<PagedResult<Registration>> GetPagedAsync( int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate, Guid? pa…` | RegistrationRepository |
| 85 | public | ` public async Task<List<Registration>> GetWaitingQueueAsync( Guid? doctorId = null, CancellationToken cancellationToken = default)` | RegistrationRepository |
| 101 | public | ` public async Task<int> GetTodayMaxQueueNumberAsync(CancellationToken cancellationToken = default)` | RegistrationRepository |
| 111 | public | ` public async Task<Registration?> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)` | RegistrationRepository |
| 120 | public | ` public async Task AddAsync(Registration registration, CancellationToken cancellationToken = default)` | RegistrationRepository |
| 126 | public | ` public Task UpdateAsync(Registration registration, CancellationToken cancellationToken = default)` | RegistrationRepository |
| 133 | public | ` public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)` | RegistrationRepository |
| 12 | public (interface 默认) | ` Task NotifyNewRegistrationAsync(Guid doctorId, RegistrationDetailDto registration, CancellationToken cancellationToken = default);` | INotificationService |
| 15 | public (interface 默认) | ` Task NotifyRegistrationStatusChangedAsync(Guid doctorId, Guid registrationId, string newStatus, CancellationToken cancellationToken = default);` | INotificationService |
| 15 | public (interface 默认) | ` Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);` | IRegistrationRepository |
| 20 | public (interface 默认) | ` Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);` | IRegistrationRepository |
| 26 | public (interface 默认) | ` Task<PagedResult<Registration>> GetPagedAsync( int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate, Guid? patientId, Guid…` | IRegistrationRepository |
| 37 | public (interface 默认) | ` Task<List<Registration>> GetWaitingQueueAsync( Guid? doctorId = null, CancellationToken cancellationToken = default);` | IRegistrationRepository |
| 44 | public (interface 默认) | ` Task<int> GetTodayMaxQueueNumberAsync(CancellationToken cancellationToken = default);` | IRegistrationRepository |
| 51 | public (interface 默认) | ` Task<Registration?> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default);` | IRegistrationRepository |
| 56 | public (interface 默认) | ` Task AddAsync(Registration registration, CancellationToken cancellationToken = default);` | IRegistrationRepository |
| 61 | public (interface 默认) | ` Task UpdateAsync(Registration registration, CancellationToken cancellationToken = default);` | IRegistrationRepository |
| 66 | public (interface 默认) | ` Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);` | IRegistrationRepository |
| 16 | public | ` public partial RegistrationListDto ToListDto(Registration entity);` | RegistrationMapper |
| 21 | public | ` public partial List<RegistrationListDto> ToListDtos(List<Registration> entities);` | RegistrationMapper |
| 26 | public | ` public partial RegistrationDetailDto ToDetailDto(Registration entity);` | RegistrationMapper |
| 27 | public | ` public static IServiceCollection AddRegistrationModule(this IServiceCollection services, IConfiguration configuration)` | RegistrationModule |
| 18 | public | ` public NotificationService( IHubContext<RegistrationHub> hubContext, ILogger<NotificationService> logger)` | NotificationService |
| 27 | public | ` public async Task NotifyNewRegistrationAsync( Guid doctorId, RegistrationDetailDto registration, CancellationToken cancellationToken = default)` | NotificationService |
| 37 | public | ` public async Task NotifyRegistrationStatusChangedAsync( Guid doctorId, Guid registrationId, string newStatus, CancellationToken cancellationToken = d…` | NotificationService |
| 46 | private | ` private async Task SendToDoctorAsync( Guid doctorId, string method, object?[] args, CancellationToken cancellationToken)` | NotificationService |
| 15 | public | ` public RegistrationCrossModuleService( IRegistrationRepository registrationRepository)` | RegistrationCrossModuleService |
| 21 | public | ` public async Task CompleteByMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default)` | RegistrationCrossModuleService |
| 31 | public | ` public async Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId, CancellationToken ct = default)` | RegistrationCrossModuleService |
| 51 | public | ` public async Task LinkRegistrationToMedicalCaseAsync(Guid registrationId, Guid medicalCaseId, CancellationToken ct = default)` | RegistrationCrossModuleService |
### LYBT.Module.Reports

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 16 | public | ` public ReportRepository(AppDbContext context)` | ReportRepository |
| 22 | public | ` public async Task<decimal> GetRegistrationFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)` | ReportRepository |
| 30 | public | ` public async Task<decimal> GetMedicineFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)` | ReportRepository |
| 47 | public | ` public async Task<int> GetConsultationCountAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)` | ReportRepository |
| 54 | public | ` public async Task<List<DoctorCountDto>> GetConsultationsByDoctorAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = def…` | ReportRepository |
| 69 | public | ` public async Task<List<HerbUsageItemDto>> GetHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)` | ReportRepository |
| 77 | public | ` public async Task<List<ReportDayValueDto>> GetRegistrationFeeByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = d…` | ReportRepository |
| 87 | public | ` public async Task<List<ReportDayValueDto>> GetMedicineFeeByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = defau…` | ReportRepository |
| 101 | public | ` public async Task<List<ReportDayCountDto>> GetConsultationCountByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken =…` | ReportRepository |
| 111 | public | ` public async Task<List<DoctorPerformancePointDto>> GetDoctorPerformanceAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToke…` | ReportRepository |
| 158 | public | ` public async Task<List<HerbUsageItemDto>> GetHerbRankingAsync(DateTime startDate, DateTime endDate, int top, CancellationToken cancellationToken = de…` | ReportRepository |
| 167 | public | ` public async Task<List<PatientFlowPointDto>> GetPatientFlowByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = def…` | ReportRepository |
| 198 | private | ` private IQueryable<HerbUsageItemDto> HerbUsageQuery(DateTime startDate, DateTime endDate)` | ReportRepository |
| 8 | public (interface 默认) | ` Task<decimal> GetRegistrationFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 9 | public (interface 默认) | ` Task<decimal> GetMedicineFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 10 | public (interface 默认) | ` Task<int> GetConsultationCountAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 11 | public (interface 默认) | ` Task<List<DoctorCountDto>> GetConsultationsByDoctorAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 12 | public (interface 默认) | ` Task<List<HerbUsageItemDto>> GetHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 17 | public (interface 默认) | ` Task<List<ReportDayValueDto>> GetRegistrationFeeByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 22 | public (interface 默认) | ` Task<List<ReportDayValueDto>> GetMedicineFeeByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 27 | public (interface 默认) | ` Task<List<ReportDayCountDto>> GetConsultationCountByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 32 | public (interface 默认) | ` Task<List<DoctorPerformancePointDto>> GetDoctorPerformanceAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 37 | public (interface 默认) | ` Task<List<HerbUsageItemDto>> GetHerbRankingAsync(DateTime startDate, DateTime endDate, int top, CancellationToken cancellationToken = default);` | IReportRepository |
| 42 | public (interface 默认) | ` Task<List<PatientFlowPointDto>> GetPatientFlowByDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportRepository |
| 11 | public (interface 默认) | ` Task<DailyIncomeDto> GetDailyIncomeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportService |
| 12 | public (interface 默认) | ` Task<DailyConsultationDto> GetDailyConsultationsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportService |
| 13 | public (interface 默认) | ` Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportService |
| 18 | public (interface 默认) | ` Task<IncomeTrendDto> GetIncomeTrendAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, CancellationToken cancellationToken = d…` | IReportService |
| 23 | public (interface 默认) | ` Task<ConsultationTrendDto> GetConsultationTrendAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, CancellationToken cancellat…` | IReportService |
| 28 | public (interface 默认) | ` Task<List<DoctorPerformanceDto>> GetDoctorPerformanceAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);` | IReportService |
| 33 | public (interface 默认) | ` Task<List<HerbUsageItemDto>> GetHerbRankingAsync(DateTime startDate, DateTime endDate, int top = 10, CancellationToken cancellationToken = default);` | IReportService |
| 38 | public (interface 默认) | ` Task<PatientFlowDto> GetPatientFlowAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, CancellationToken cancellationToken = d…` | IReportService |
| 15 | public | ` public static IServiceCollection AddReportsModule(this IServiceCollection services, IConfiguration configuration)` | ReportsModule |
| 15 | public | ` public ReportService(IReportRepository reportRepository)` | ReportService |
| 20 | public | ` public async Task<DailyIncomeDto> GetDailyIncomeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)` | ReportService |
| 33 | public | ` public async Task<DailyConsultationDto> GetDailyConsultationsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = defaul…` | ReportService |
| 45 | public | ` public async Task<DailyHerbUsageDto> GetDailyHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)` | ReportService |
| 52 | public | ` public async Task<IncomeTrendDto> GetIncomeTrendAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, CancellationToken cancella…` | ReportService |
| 70 | public | ` public async Task<ConsultationTrendDto> GetConsultationTrendAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, CancellationTo…` | ReportService |
| 82 | public | ` public async Task<List<DoctorPerformanceDto>> GetDoctorPerformanceAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = d…` | ReportService |
| 98 | public | ` public async Task<List<HerbUsageItemDto>> GetHerbRankingAsync(DateTime startDate, DateTime endDate, int top, CancellationToken cancellationToken = de…` | ReportService |
| 103 | public | ` public async Task<PatientFlowDto> GetPatientFlowAsync(DateTime startDate, DateTime endDate, ReportGranularity granularity, CancellationToken cancella…` | ReportService |
| 119 | private | ` private static List<decimal> Rollup(List<ReportDayValueDto> days, List<ReportBucket> buckets)` | ReportService |
| 17 | public | ` public static List<ReportBucket> Build(DateTime startDate, DateTime endDate, ReportGranularity granularity)` | ReportTimeBuckets |
| 43 | private | ` private static DateTime Next(DateTime current, ReportGranularity granularity) => granularity switch` | ReportTimeBuckets |
| 50 | private | ` private static DateTime StartOfWeek(DateTime date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));` | ReportTimeBuckets |
### LYBT.Module.Users

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 16 | public | ` public BatchDeleteUsersCommandHandler(IUserRepository userRepository)` | BatchDeleteUsersCommandHandler |
| 21 | public | ` public async Task<Result<BatchOperationResultDto>> Handle( BatchDeleteUsersCommand request, CancellationToken cancellationToken)` | BatchDeleteUsersCommandHandler |
| 28 | protected | ` protected override Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct)` | BatchDeleteUsersCommandHandler |
| 31 | protected | ` protected override Task UpdateAsync(ApplicationUser user, CancellationToken ct)` | BatchDeleteUsersCommandHandler |
| 34 | protected | ` protected override Task ApplyOperationAsync(ApplicationUser user, Guid operatorId, CancellationToken ct)` | BatchDeleteUsersCommandHandler |
| 44 | protected | ` protected override string? GetEntityName(ApplicationUser user) => user.UserName;` | BatchDeleteUsersCommandHandler |
| 46 | protected | ` protected override Task<string?> ValidateAsync( ApplicationUser user, Guid id, Guid operatorId, CancellationToken ct)` | BatchDeleteUsersCommandHandler |
| 16 | public | ` public BatchDisableUsersCommandHandler(IUserRepository userRepository)` | BatchDisableUsersCommandHandler |
| 21 | public | ` public Task<Result<BatchOperationResultDto>> Handle( BatchDisableUsersCommand request, CancellationToken cancellationToken)` | BatchDisableUsersCommandHandler |
| 25 | protected | ` protected override Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct)` | BatchDisableUsersCommandHandler |
| 28 | protected | ` protected override Task UpdateAsync(ApplicationUser user, CancellationToken ct)` | BatchDisableUsersCommandHandler |
| 31 | protected | ` protected override Task ApplyOperationAsync(ApplicationUser user, Guid operatorId, CancellationToken ct)` | BatchDisableUsersCommandHandler |
| 42 | protected | ` protected override string? GetEntityName(ApplicationUser user) => user.UserName;` | BatchDisableUsersCommandHandler |
| 44 | protected | ` protected override Task<string?> ValidateAsync( ApplicationUser user, Guid id, Guid operatorId, CancellationToken ct)` | BatchDisableUsersCommandHandler |
| 16 | public | ` public BatchEnableUsersCommandHandler(IUserRepository userRepository)` | BatchEnableUsersCommandHandler |
| 21 | public | ` public Task<Result<BatchOperationResultDto>> Handle( BatchEnableUsersCommand request, CancellationToken cancellationToken)` | BatchEnableUsersCommandHandler |
| 25 | protected | ` protected override Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct)` | BatchEnableUsersCommandHandler |
| 28 | protected | ` protected override Task UpdateAsync(ApplicationUser user, CancellationToken ct)` | BatchEnableUsersCommandHandler |
| 31 | protected | ` protected override Task ApplyOperationAsync(ApplicationUser user, Guid operatorId, CancellationToken ct)` | BatchEnableUsersCommandHandler |
| 43 | protected | ` protected override string? GetEntityName(ApplicationUser user) => user.UserName;` | BatchEnableUsersCommandHandler |
| 45 | protected | ` protected override Task<string?> ValidateAsync( ApplicationUser user, Guid id, Guid operatorId, CancellationToken ct)` | BatchEnableUsersCommandHandler |
| 16 | public | ` public ChangePasswordCommandHandler( UserManager<ApplicationUser> userManager, IAuthCrossModuleService authCrossModule)` | ChangePasswordCommandHandler |
| 24 | public | ` public async Task<Result> Handle( ChangePasswordCommand request, CancellationToken cancellationToken)` | ChangePasswordCommandHandler |
| 17 | public | ` public ChangeProfileCommandHandler(IUserRepository userRepository)` | ChangeProfileCommandHandler |
| 22 | public | ` public async Task<Result<UserDetailDto>> Handle( ChangeProfileCommand request, CancellationToken cancellationToken)` | ChangeProfileCommandHandler |
| 18 | public | ` public CreateUserCommandHandler( UserManager<ApplicationUser> userManager)` | CreateUserCommandHandler |
| 24 | public | ` public async Task<Result<UserDetailDto>> Handle( CreateUserCommand request, CancellationToken cancellationToken)` | CreateUserCommandHandler |
| 18 | public | ` public DeleteUserCommandHandler( IUserRepository userRepository, IAuthCrossModuleService authCrossModule)` | DeleteUserCommandHandler |
| 26 | public | ` public async Task<Result> Handle( DeleteUserCommand request, CancellationToken cancellationToken)` | DeleteUserCommandHandler |
| 17 | public | ` public ResetPasswordCommandHandler( UserManager<ApplicationUser> userManager, IAuthCrossModuleService authCrossModule)` | ResetPasswordCommandHandler |
| 25 | public | ` public async Task<Result<ResetPasswordResult>> Handle( ResetPasswordCommand request, CancellationToken cancellationToken)` | ResetPasswordCommandHandler |
| 15 | public | ` public RestoreUserCommandHandler(IUserRepository userRepository)` | RestoreUserCommandHandler |
| 20 | public | ` public async Task<Result<UserDetailDto>> Handle( RestoreUserCommand request, CancellationToken cancellationToken)` | RestoreUserCommandHandler |
| 18 | public | ` public ToggleUserStatusCommandHandler( IUserRepository userRepository, IAuthCrossModuleService authCrossModule)` | ToggleUserStatusCommandHandler |
| 26 | public | ` public async Task<Result<UserDetailDto>> Handle( ToggleUserStatusCommand request, CancellationToken cancellationToken)` | ToggleUserStatusCommandHandler |
| 17 | public | ` public UpdateUserCommandHandler(IUserRepository userRepository)` | UpdateUserCommandHandler |
| 22 | public | ` public async Task<Result<UserDetailDto>> Handle( UpdateUserCommand request, CancellationToken cancellationToken)` | UpdateUserCommandHandler |
| 18 | public | ` public static partial UserBasicDto ToBasicDto(ApplicationUser user);` | UserCrossModuleMapper |
| 25 | public | ` public static partial UserCredentialDto ToCredentialDto(ApplicationUser user);` | UserCrossModuleMapper |
| 30 | private | ` private static string ToNonNullString(string? value) => value ?? string.Empty;` | UserCrossModuleMapper |
| 35 | private | ` private static DateTime? ToLockoutEnd(DateTimeOffset? value) => value?.UtcDateTime;` | UserCrossModuleMapper |
| 18 | public | ` public static UserListDto ToListDto(ApplicationUser entity) => new()` | UserMapper |
| 35 | public | ` public static UserDetailDto ToDetailDto(ApplicationUser entity) => new()` | UserMapper |
| 11 | public | ` public ChangeProfileValidator()` | ChangeProfileValidator |
| 11 | public | ` public CreateUserValidator()` | CreateUserValidator |
| 11 | public | ` public UpdateUserValidator()` | UpdateUserValidator |
| 27 | protected | ` protected BaseUsersController(ISender sender, ILogger logger, IUserService userService)` | BaseUsersController |
| 37 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | BaseUsersController |
| 53 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | BaseUsersController |
| 65 | public | ` public async Task<IActionResult> Create([FromBody] UserInputDto input, CancellationToken ct)` | BaseUsersController |
| 78 | public | ` public async Task<IActionResult> Update(Guid id, [FromBody] UserInputDto input, CancellationToken ct)` | BaseUsersController |
| 96 | public | ` public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)` | BaseUsersController |
| 115 | public | ` public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)` | BaseUsersController |
| 131 | public | ` public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)` | BaseUsersController |
| 152 | public | ` public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | BaseUsersController |
| 177 | public | ` public virtual async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)` | BaseUsersController |
| 192 | public | ` public virtual async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct = default)` | BaseUsersController |
| 219 | public | ` public virtual async Task<IActionResult> ChangeProfile(Guid id, [FromBody] ChangeProfileDto dto, CancellationToken ct = default)` | BaseUsersController |
| 243 | public | ` public virtual async Task<IActionResult> ChangePassword(Guid id, [FromBody] LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request, Cancella…` | BaseUsersController |
| 268 | public | ` public virtual async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)` | BaseUsersController |
| 289 | public | ` public virtual async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)` | BaseUsersController |
| 16 | public | ` public UserRepository(UsersDbContext context)` | UserRepository |
| 22 | public | ` public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)` | UserRepository |
| 29 | public | ` public async Task<ApplicationUser?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)` | UserRepository |
| 37 | public | ` public async Task<PagedResult<ApplicationUser>> GetPagedAsync( int page, int pageSize, string? keyword, UserRole? role, CommonStatus? status, Cancell…` | UserRepository |
| 80 | public | ` public async Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)` | UserRepository |
| 13 | public | ` public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)` | UsersDbContext |
| 17 | protected | ` protected override void OnModelCreating(ModelBuilder modelBuilder)` | UsersDbContext |
| 9 | public (interface 默认) | ` Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct);` | IUserRepository |
| 10 | public (interface 默认) | ` Task<ApplicationUser?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);` | IUserRepository |
| 11 | public (interface 默认) | ` Task<PagedResult<ApplicationUser>> GetPagedAsync(int page, int pageSize, string? keyword, UserRole? role, CommonStatus? status, CancellationToken ct)…` | IUserRepository |
| 12 | public (interface 默认) | ` Task UpdateAsync(ApplicationUser user, CancellationToken ct);` | IUserRepository |
| 11 | public (interface 默认) | ` Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);` | IUserService |
| 12 | public (interface 默认) | ` Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);` | IUserService |
| 13 | public (interface 默认) | ` Task<Result<UserDetailDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct);` | IUserService |
| 14 | public | ` public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)` | IdentitySeedData |
| 40 | private | ` private static string ResolveSysAdminPassword(IHostEnvironment environment, string configuredPassword)` | IdentitySeedData |
| 61 | private | ` private static async Task EnsureUserAsync( UserManager<ApplicationUser> userManager, string userName, string realName, string email, string defaultPa…` | IdentitySeedData |
| 22 | public | ` public UserCrossModuleService(UsersDbContext context, UserManager<ApplicationUser> userManager, ILogger<UserCrossModuleService> logger)` | UserCrossModuleService |
| 29 | public | ` public async Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default)` | UserCrossModuleService |
| 41 | public | ` public async Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)` | UserCrossModuleService |
| 53 | public | ` public async Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken cancellationToken = default)` | UserCrossModuleService |
| 64 | public | ` public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)` | UserCrossModuleService |
| 71 | public | ` public async Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default)` | UserCrossModuleService |
| 85 | public | ` public async Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default)` | UserCrossModuleService |
| 98 | public | ` public async Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default)` | UserCrossModuleService |
| 16 | public | ` public UserService(IUserRepository userRepository)` | UserService |
| 21 | public | ` public async Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)` | UserService |
| 35 | public | ` public async Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)` | UserService |
| 43 | public | ` public async Task<Result<UserDetailDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct)` | UserService |
| 27 | public | ` public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)` | UsersModule |
### LYBT.Shared.Configuration

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 13 | public | ` public static string GetEffectiveConnectionString(DatabaseOptions? databaseOptions, IConfiguration configuration)` | ConnectionStringResolver |
| 23 | private | ` private static string? FirstNonEmpty(params string?[] candidates)` | ConnectionStringResolver |
| 18 | public | ` public static IServiceCollection AddLybtClientConfiguration( this IServiceCollection services, IConfiguration configuration)` | ClientConfigurationExtensions |
| 18 | public | ` public static IServiceCollection AddLybtServerConfiguration( this IServiceCollection services, IConfiguration configuration)` | ServerConfigurationExtensions |
| 64 | public | ` public LoginRateLimitOptions()` | LoginRateLimitOptions |
| 11 | public | ` public ValidateOptionsResult Validate(string? name, DatabaseOptions options)` | DatabaseOptionsValidator |
| 11 | public | ` public ValidateOptionsResult Validate(string? name, JwtOptions options)` | JwtOptionsValidator |
| 11 | public | ` public ValidateOptionsResult Validate(string? name, LocalJwtOptions options)` | LocalJwtOptionsValidator |
| 11 | public | ` public ValidateOptionsResult Validate(string? name, SecurityOptions options)` | SecurityOptionsValidator |
### LYBT.Shared.ExceptionHandling

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 35 | public | ` public virtual int GetHttpStatusCode() =>` | AppException |
| 44 | public | ` public AppException() : base("应用程序异常")` | AppException |
| 48 | public | ` public AppException(string message) : base(message)` | AppException |
| 52 | public | ` public AppException(string message, Exception innerException) : base(message, innerException)` | AppException |
| 56 | public | ` public AppException(string message, string? errorCode = null, string? userMessage = null, bool showDetailToUser = false)` | AppException |
| 64 | public | ` public AppException(string message, Exception innerException, string? errorCode = null, string? userMessage = null, bool showDetailToUser = false)` | AppException |
| 75 | public | ` public AppException(EC typedErrorCode, string message, string? userMessage = null, bool showDetailToUser = false)` | AppException |
| 87 | public | ` public AppException(EC typedErrorCode, string message, Exception innerException, string? userMessage = null, bool showDetailToUser = false)` | AppException |
| 17 | public | ` public override int GetHttpStatusCode() => 400;` | BusinessException |
| 21 | public | ` public BusinessException() : base("业务规则违反")` | BusinessException |
| 26 | public | ` public BusinessException(string message) : base(message)` | BusinessException |
| 31 | public | ` public BusinessException(string message, Exception innerException) : base(message, innerException)` | BusinessException |
| 36 | public | ` public BusinessException(string message, string businessRule)` | BusinessException |
| 43 | public | ` public BusinessException(EC errorCode, string message, string? businessRule = null)` | BusinessException |
| 50 | public | ` public override int GetHttpStatusCode() => 409;` | ConflictException |
| 54 | public | ` public ConflictException() : base("资源冲突")` | ConflictException |
| 60 | public | ` public ConflictException(string message) : base(message)` | ConflictException |
| 67 | public | ` public ConflictException(string message, Exception innerException) : base(message, innerException)` | ConflictException |
| 74 | public | ` public ConflictException(EC errorCode, string message, string? userMessage = null)` | ConflictException |
| 80 | public | ` public static ConflictException MedicalCaseVersion(Guid caseId, int expectedVersion, int currentVersion)` | ConflictException |
| 94 | public | ` public static ConflictException MedicalCaseLocked(Guid caseId, string? lockedBy = null)` | ConflictException |
| 106 | public | ` public static ConflictException Duplicate(string resourceType, string fieldName, string value)` | ConflictException |
| 22 | public | ` public override int GetHttpStatusCode() => 404;` | NotFoundException |
| 26 | public | ` public NotFoundException() : base("请求的资源不存在")` | NotFoundException |
| 32 | public | ` public NotFoundException(string message) : base(message)` | NotFoundException |
| 39 | public | ` public NotFoundException(string resourceType, string resourceId)` | NotFoundException |
| 49 | public | ` public NotFoundException(EC errorCode, string message, string? resourceType = null, string? resourceId = null)` | NotFoundException |
| 57 | public | ` public static NotFoundException User(Guid userId) =>` | NotFoundException |
| 60 | public | ` public static NotFoundException Patient(Guid patientId) =>` | NotFoundException |
| 63 | public | ` public static NotFoundException Herb(Guid herbId) =>` | NotFoundException |
| 66 | public | ` public static NotFoundException MedicalCase(Guid caseId) =>` | NotFoundException |
| 69 | public | ` public static NotFoundException Formula(Guid formulaId) =>` | NotFoundException |
| 27 | public | ` public override int GetHttpStatusCode() => 400;` | ValidationException |
| 31 | public | ` public ValidationException() : base("验证失败")` | ValidationException |
| 37 | public | ` public ValidationException(string message) : base(message)` | ValidationException |
| 44 | public | ` public ValidationException(string message, Exception innerException) : base(message, innerException)` | ValidationException |
| 51 | public | ` public ValidationException(string fieldName, string errorMessage)` | ValidationException |
| 61 | public | ` public ValidationException(Dictionary<string, string[]> errors)` | ValidationException |
| 76 | public | ` public ValidationException AddError(string fieldName, string errorMessage)` | ValidationException |
| 33 | public | ` public override int GetHttpStatusCode() => (int)StatusCode;` | ApiException |
| 37 | public | ` public ApiException() : base("API调用异常")` | ApiException |
| 42 | public | ` public ApiException(string message) : base(message)` | ApiException |
| 47 | public | ` public ApiException(string message, Exception innerException) : base(message, innerException)` | ApiException |
| 52 | public | ` public ApiException(HttpStatusCode statusCode, string message, string? responseContent = null)` | ApiException |
| 60 | public | ` public ApiException(HttpStatusCode statusCode, string message, string requestUrl, string httpMethod, string? responseContent = null)` | ApiException |
| 70 | private | ` private static string GetDefaultUserMessage(HttpStatusCode statusCode) => statusCode switch` | ApiException |
| 84 | public | ` public static ApiException Unauthorized(string? message = null) =>` | ApiException |
| 87 | public | ` public static ApiException Forbidden(string? message = null) =>` | ApiException |
| 90 | public | ` public static ApiException ServiceUnavailable(string? message = null) =>` | ApiException |
| 93 | public | ` public static ApiException Timeout(string? message = null) =>` | ApiException |
| 22 | public | ` public override int GetHttpStatusCode() => 401;` | UnauthorizedException |
| 26 | public | ` public UnauthorizedException() : base("未授权访问")` | UnauthorizedException |
| 32 | public | ` public UnauthorizedException(string message) : base(message)` | UnauthorizedException |
| 39 | public | ` public UnauthorizedException(string message, Exception innerException) : base(message, innerException)` | UnauthorizedException |
| 46 | public | ` public UnauthorizedException(EC errorCode, string message, string? failureReason = null)` | UnauthorizedException |
| 53 | public | ` public static UnauthorizedException InvalidPassword() =>` | UnauthorizedException |
| 56 | public | ` public static UnauthorizedException InvalidRefreshToken() =>` | UnauthorizedException |
| 59 | public | ` public static UnauthorizedException UserDisabled() =>` | UnauthorizedException |
| 62 | public | ` public static UnauthorizedException UserLocked() =>` | UnauthorizedException |
| 65 | public | ` public static UnauthorizedException PasswordChangeRequired() =>` | UnauthorizedException |
| 72 | public | ` public static UnauthorizedException TokenExpired() =>` | UnauthorizedException |
### LYBT.Shared.Logging

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 18 | public | ` public string? GetCorrelationId()` | ActivityCorrelationIdProvider |
| 24 | public | ` public void SetCorrelationId(string correlationId)` | ActivityCorrelationIdProvider |
| 33 | public | ` public string GetCorrelationIdOrNew()` | ActivityCorrelationIdProvider |
| 13 | public (interface 默认) | ` string? GetCorrelationId();` | ICorrelationIdProvider |
| 19 | public (interface 默认) | ` void SetCorrelationId(string correlationId);` | ICorrelationIdProvider |
| 35 | public | ` public CorrelationIdEnricher(ICorrelationIdProvider correlationIdProvider)` | CorrelationIdEnricher |
| 45 | public | ` public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)` | CorrelationIdEnricher |
| 73 | public | ` public static Serilog.LoggerConfiguration WithCorrelationId( this Serilog.Configuration.LoggerEnrichmentConfiguration enrichmentConfiguration, ICorre…` | CorrelationIdEnricherExtensions |
| 33 | public | ` public static LoggerConfiguration UseSharedLogging( this LoggerConfiguration loggerConfiguration, ICorrelationIdProvider correlationIdProvider)` | LoggerConfigurationExtensions |
| 53 | public | ` public static LoggerConfiguration WithSensitiveDataMasking( this LoggerConfiguration loggerConfiguration)` | LoggerConfigurationExtensions |
| 69 | public | ` public static LoggerConfiguration WriteToConsoleWithTemplate( this LoggerConfiguration loggerConfiguration, LogEventLevel minimumLevel = LogEventLeve…` | LoggerConfigurationExtensions |
| 92 | public | ` public static LoggerConfiguration WriteToFileWithTemplate( this LoggerConfiguration loggerConfiguration, string logFilePath, LogEventLevel minimumLev…` | LoggerConfigurationExtensions |
| 42 | public | ` public LoggingLevelManager(LogEventLevel defaultLevel = LogEventLevel.Information)` | LoggingLevelManager |
| 54 | public | ` public DebugModeInfo EnableDebugMode(LogEventLevel level = LogEventLevel.Debug, int? durationMinutes = 30)` | LoggingLevelManager |
| 99 | public | ` public DebugModeInfo DisableDebugMode()` | LoggingLevelManager |
| 127 | public | ` public DebugModeInfo GetStatus()` | LoggingLevelManager |
| 150 | public | ` public void SetLevel(LogEventLevel level)` | LoggingLevelManager |
| 161 | public | ` public void Dispose()` | LoggingLevelManager |
| 170 | protected | ` protected virtual void Dispose(bool disposing)` | LoggingLevelManager |
| 18 | public | ` public bool TryDestructure( object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)` | SensitiveDataDestructuringPolicy |
| 24 | private | ` private static partial Regex PasswordPattern();` | SensitiveDataMasker |
| 31 | private | ` private static partial Regex ConnectionStringPattern();` | SensitiveDataMasker |
| 37 | private | ` private static partial Regex BearerTokenPattern();` | SensitiveDataMasker |
| 63 | public | ` public static string Mask(string? value, MaskingMode mode, SensitiveDataType dataType = SensitiveDataType.PersonalInfo)` | SensitiveDataMasker |
| 81 | private | ` private static string MaskPartial(string value, SensitiveDataType dataType)` | SensitiveDataMasker |
| 106 | private | ` private static string MaskHash(string value)` | SensitiveDataMasker |
| 117 | private | ` private static string MaskDefault(string value)` | SensitiveDataMasker |
| 132 | public | ` public static SensitiveDataAttribute? GetSensitiveDataAttribute(PropertyInfo property)` | SensitiveDataMasker |
| 142 | public | ` public static Dictionary<string, object?> MaskObject(object obj)` | SensitiveDataMasker |
| 173 | private | ` private static partial Regex UriSensitiveParamPattern();` | SensitiveDataMasker |
| 181 | public | ` public static string MaskUri(string? uri)` | SensitiveDataMasker |
| 200 | public | ` public static string SanitizeText(string? input)` | SensitiveDataMasker |
| 232 | public | ` public static bool IsSensitiveFieldName(string? fieldName)` | SensitiveDataMasker |
| 246 | public | ` public static string? SerializeWithSanitization(object? obj)` | SensitiveDataMasker |
| 275 | public | ` public static string SanitizeException(Exception? exception, int maxStackTraceLines = 5)` | SensitiveDataMasker |
| 306 | public | ` public override bool CanConvert(Type typeToConvert) => true;` | SanitizingJsonConverter |
| 308 | public | ` public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)` | SanitizingJsonConverter |
| 313 | public | ` public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)` | SanitizingJsonConverter |
### LYBT.Shared.Models

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 24 | public | ` public SensitiveDataAttribute(SensitiveDataType dataType = SensitiveDataType.PersonalInfo)` | SensitiveDataAttribute |
| 51 | public | ` public static ApiResponse<T> CreateSuccess(T? data = default, string message = "操作成功")` | ApiResponse |
| 64 | public | ` public static ApiResponse<T> CreateFail(string message = "操作失败", object? errors = null)` | ApiResponse |
| 84 | public | ` public static new ApiResponse CreateSuccess(object? data = null, string message = "操作成功")` | ApiResponse |
| 17 | public | ` public PagedResult()` | PagedResult |
| 25 | public | ` public PagedResult(List<T> items, int totalCount, int currentPage, int pageSize)` | PagedResult |
| 42 | private | ` private Result(bool isSuccess, T? value, string? error, IReadOnlyList<string>? errors, ErrorCode errorCode, Exception? exception = null, ErrorCode? m…` | Result |
| 54 | public | ` public static Result<T> Success(T value) => new(true, value, null, null, default);` | Result |
| 57 | public | ` public static Result<T> Success(T value, string message) => new(true, value, message, null, default);` | Result |
| 60 | public | ` public static Result<T> Failure(ErrorCode code, string error) => new(false, default, error, null, code);` | Result |
| 63 | public | ` public static Result<T> Failure(string error) => new(false, default, error, null, ErrorCode.InternalError);` | Result |
| 66 | public | ` public static Result<T> Failure(string error, Exception exception) => new(false, default, error, null, ErrorCode.InternalError, exception);` | Result |
| 69 | public | ` public static Result<T> Failure(ErrorCode code, List<string> errors) => new(false, default, string.Join("; ", errors), errors, code);` | Result |
| 72 | public | ` public static Result<T> Failure(List<string> errors) => new(false, default, string.Join("; ", errors), errors, ErrorCode.InternalError);` | Result |
| 75 | public | ` public static Result<T> ValidationFailure(string error) => new(false, default, error, null, ErrorCode.ValidationFailed);` | Result |
| 78 | public | ` public static Result<T> FromException(Exception ex, string? operationName = null)` | Result |
| 87 | public | ` public static implicit operator Result<T>(T value) => Success(value);` | Result |
| 119 | private | ` private Result(bool isSuccess, string? error, IReadOnlyList<string>? errors, ErrorCode errorCode, Exception? exception = null, ErrorCode? moduleError…` | Result |
| 130 | public | ` public static Result Success() => new(true, null, null, default);` | Result |
| 133 | public | ` public static Result Success(string message) => new(true, message, null, default);` | Result |
| 136 | public | ` public static Result Failure(ErrorCode code, string error) => new(false, error, null, code);` | Result |
| 139 | public | ` public static Result Failure(string error) => new(false, error, null, ErrorCode.InternalError);` | Result |
| 142 | public | ` public static Result Failure(string error, Exception exception) => new(false, error, null, ErrorCode.InternalError, exception);` | Result |
| 145 | public | ` public static Result Failure(ErrorCode code, List<string> errors) => new(false, string.Join("; ", errors), errors, code);` | Result |
| 148 | public | ` public static Result Failure(List<string> errors) => new(false, string.Join("; ", errors), errors, ErrorCode.InternalError);` | Result |
| 151 | public | ` public static Result FromException(Exception ex, string? operationName = null)` | Result |
| 8 | public | ` public DatabaseHealthCheckResult()` | DatabaseHealthCheckResult |
| 15 | public | ` public DatabaseHealthCheckResult(string name, string description)` | DatabaseHealthCheckResult |
| 20 | public | ` public static MedicalCaseInputDto ToInputDto(this MedicalCaseDetailDto dto)` | DtoConversionExtensions |
| 40 | public | ` public static ConsultationInputDto ToInputDto(this ConsultationDetailDto dto)` | DtoConversionExtensions |
| 59 | public | ` public static PrescriptionInputDto ToPrescriptionInputDto(this PrescriptionDetailDto dto)` | DtoConversionExtensions |
| 12 | public | ` public static int ToHttpStatusCode(this ErrorCode errorCode)` | ErrorCodeExtensions |
| 144 | public | ` public static ErrorCategory ToCategory(this ErrorCode errorCode)` | ErrorCodeExtensions |
| 286 | public | ` public static string GetModuleName(this ErrorCode errorCode)` | ErrorCodeExtensions |
| 307 | public | ` public static string ToFormattedString(this ErrorCode errorCode)` | ErrorCodeExtensions |
| 152 | public | ` public static string GetUserMessage(ErrorCode code) => Get(code, useEnglish: false);` | ErrorMessages |
| 15 | public | ` public static void RemoveByPrefix(this IMemoryCache cache, string prefix)` | CacheExtensions |
| 33 | public | ` public static void Clear(this IMemoryCache cache)` | CacheExtensions |
| 44 | private | ` private static IEnumerable<string> GetAllCacheKeys(IMemoryCache cache)` | CacheExtensions |
| 45 | public | ` public static string GenerateTemporaryPassword()` | PasswordHelper |
| 76 | public | ` public static string GenerateSalt(int length = RandomByteLength)` | PasswordHelper |
| 98 | public | ` public static PasswordValidationResult ValidatePassword( string password, int minLength = 8, bool requireUppercase = true, bool requireLowercase = tr…` | PasswordHelper |
| 159 | public | ` public static PasswordStrength CheckPasswordStrength(string password)` | PasswordHelper |
| 198 | public | ` public static bool IsCommonPassword(string password)` | PasswordHelper |
| 208 | public | ` public static string GenerateSecurePassword()` | PasswordHelper |
| 217 | public | ` public static string GenerateSecurePassword(int length = 20)` | PasswordHelper |
| 220 | private | ` private static int GetRandomInt(int maxValue) => RandomNumberGenerator.GetInt32(maxValue);` | PasswordHelper |
| 222 | private | ` private static void Shuffle(Span<char> array)` | PasswordHelper |
| 240 | public | ` public static string GenerateSecurePassword( int length, bool includeUppercase = true, bool includeLowercase = true, bool includeDigits = true, bool …` | PasswordHelper |
| 305 | public | ` public static string GenerateSecurePassword( bool includeUppercase, bool includeLowercase, bool includeDigits, bool includeSpecialChars)` | PasswordHelper |
| 318 | public | ` public static bool SecureEquals(string? password1, string? password2)` | PasswordHelper |
| 25 | public | ` public static string GetPinYinCode(string? text)` | PinYinHelper |
| 61 | private | ` private static string GetPinYinCodeFallback(string text)` | PinYinHelper |
| 11 | public | ` public LoginRequestValidator()` | LoginRequestValidator |
| 12 | public | ` public static bool CanCreateNewCase(IEnumerable<MedicalCaseStatus> existingStatuses)` | MedicalCaseBusinessRules |
| 16 | public | ` public static bool IsValidStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to)` | MedicalCaseBusinessRules |
| 24 | public | ` public static bool HasActiveCase(IEnumerable<MedicalCaseStatus> statuses)` | MedicalCaseBusinessRules |
| 27 | public | ` public static bool HasSuspendedCase(IEnumerable<MedicalCaseStatus> statuses)` | MedicalCaseBusinessRules |
| 12 | public | ` public FormulaInputDtoValidator()` | FormulaInputDtoValidator |
| 54 | public | ` public FormulaHerbItemInputDtoValidator()` | FormulaHerbItemInputDtoValidator |
| 13 | public | ` public HerbInputDtoValidator()` | HerbInputDtoValidator |
| 26 | public | ` public MedicalCaseInputDtoValidator()` | MedicalCaseInputDtoValidator |
| 13 | public | ` public PatientInputDtoValidator()` | PatientInputDtoValidator |
| 13 | public | ` public PrescriptionInputDtoValidator()` | PrescriptionInputDtoValidator |
| 53 | public | ` public PrescriptionItemInputDtoValidator()` | PrescriptionItemInputDtoValidator |
### LYBT.Tests.Architecture

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 21 | public | ` public void AR001_MedicalCase_Should_Be_Aggregate_Root()` | AggregateRootArchTests |
| 76 | public | ` public void AR003_All_Entities_Should_Support_Soft_Delete()` | AggregateRootArchTests |
| 17 | public | ` public void AM01_ServerTests_No_NSubstitute_Reference()` | AntiMockRuleTests |
| 28 | public | ` public void AM02_ServerTests_No_NSubstitute_Dependencies()` | AntiMockRuleTests |
| 40 | public | ` public void AM03_IntegrationTests_No_EFCore_InMemory()` | AntiMockRuleTests |
| 18 | public | ` public void P01_UI_Should_Not_Depend_On_Infrastructure()` | ArchTests |
| 37 | public | ` public void P01b_UI_Should_Not_Depend_On_Entities()` | ArchTests |
| 75 | public | ` public void P01c_Desktop_Should_Not_Depend_On_WebAPI()` | ArchTests |
| 95 | public | ` public void P02_Controller_Should_Be_In_WebAPI_Project()` | ArchTests |
| 119 | public | ` public void P03_No_Workflow_Framework_References()` | ArchTests |
| 150 | public | ` public void P03b_No_Rules_Engine_References()` | ArchTests |
| 177 | public | ` public void P04_UserName_Convention()` | ArchTests |
| 229 | public | ` public void B01_Cache_Should_Use_ICacheService_Only()` | ArchTests |
| 243 | private | ` private static bool IsLegitimateMemoryCacheUsage(Type type)` | ArchTests |
| 266 | public | ` public void B01b_Cache_No_Duplicate_Registration()` | ArchTests |
| 293 | public | ` public void B02_Use_GlobalExceptionHandler_Only()` | ArchTests |
| 321 | public | ` public void B02b_Controllers_Should_Use_BaseApiController()` | ArchTests |
| 368 | public | ` public void B03_Configuration_Use_ConfigurationHelper()` | ArchTests |
| 399 | public | ` public void B04_Frontend_Should_Use_Desktop_Namespace()` | ArchTests |
| 428 | public | ` public void B05_No_Reintroduce_Deleted_Components()` | ArchTests |
| 469 | public | ` public void P05_Entities_Should_Not_Depend_On_Shared()` | ArchTests |
| 491 | public | ` public void P05b_SharedUtilities_Should_Not_Depend_On_AspNetCore()` | ArchTests |
| 509 | public | ` public void P05c_SharedUtilities_Should_Not_Depend_On_Swashbuckle()` | ArchTests |
| 530 | public | ` public void P05d_Shared_Should_Not_Depend_On_Server_Modules()` | ArchTests |
| 557 | public | ` public void P05e_Shared_Should_Not_Depend_On_Desktop()` | ArchTests |
| 596 | public | ` public void P06_NoReverseOrCircularDependencies(string sourceAssembly, string[] forbiddenDependencies, string rule)` | ArchTests |
| 619 | public | ` public void P07_ServerModules_Should_Not_Reference_Other_ServerModules()` | ArchTests |
| 22 | private | ` private static IEnumerable<Type> GetCustomControlTypes()` | CustomControlArchTests |
| 41 | public | ` public void CC01_Custom_Controls_Must_Exist()` | CustomControlArchTests |
| 71 | private | ` private static bool SetsDataContextInConstructor(Type type)` | CustomControlArchTests |
| 118 | public | ` public void CC02_MasterDetailLayout_Must_Have_Content_Properties()` | CustomControlArchTests |
| 141 | public | ` public void CC03_DataGridToolbar_Must_Have_Content_Properties()` | CustomControlArchTests |
| 160 | public | ` public void CC04_Controls_Must_Inherit_From_Control()` | CustomControlArchTests |
| 19 | public | ` public void DP01_Desktop_Should_Not_Depend_On_Server()` | DesktopLayerArchTests |
| 36 | public | ` public void DP02_Desktop_Should_Not_Contain_DTO_Classes()` | DesktopLayerArchTests |
| 68 | public | ` public void DP03_UI_Models_Must_Have_Correct_Suffix()` | DesktopLayerArchTests |
| 95 | public | ` public void DP04_ViewModels_Must_Inherit_Base_Classes()` | DesktopLayerArchTests |
| 141 | public | ` public void DP05_Events_No_Duplicate_Definitions()` | DesktopLayerArchTests |
| 174 | public | ` public void DP06_Desktop_Should_Not_Use_Entity_Classes()` | DesktopLayerArchTests |
| 204 | public | ` public void DP07_Services_Must_Follow_Naming_Convention()` | DesktopLayerArchTests |
| 234 | public | ` public void DP08_ViewModels_No_Direct_Api_Interfaces()` | DesktopLayerArchTests |
| 256 | public | ` public void DP09_Must_Use_Unified_Navigation_Service()` | DesktopLayerArchTests |
| 294 | public | ` public void DM01b_Modules_No_Forbidden_Directories()` | DesktopLayerArchTests |
| 336 | public | ` public void DM02_ViewModels_Use_Standard_Base_Classes()` | DesktopLayerArchTests |
| 391 | public | ` public void DM01_AllRepositories_Must_Have_Remote_Implementation()` | DesktopLayerArchTests |
| 437 | public | ` public void DM03_CrudViewModels_Must_Inherit_MasterDetailViewModelBase()` | DesktopLayerArchTests |
| 482 | public | ` public void DM07_LocalData_Must_Not_Depend_On_SQLite()` | DesktopLayerArchTests |
| 502 | public | ` public void DM08_Production_Should_Not_Contain_LocalDbContext()` | DesktopLayerArchTests |
| 516 | public | ` public void DM04_ViewModels_No_New_DelegateCommand()` | DesktopLayerArchTests |
| 572 | public | ` public void DM05_Repository_Interfaces_Must_Be_In_Contracts()` | DesktopLayerArchTests |
| 610 | public | ` public void DM06_Business_Modules_No_Cross_References()` | DesktopLayerArchTests |
| 651 | public | ` public void DP07_DesktopModules_Should_Not_Reference_Other_DesktopModules()` | DesktopLayerArchTests |
| 689 | public | ` public void DP10_ViewModels_Must_Not_Inject_IApiClient_SubInterfaces()` | DesktopLayerArchTests |
| 19 | public | ` public void P20_LocalWebAPI_Controllers_Only_Inject_Allowed_Types()` | LocalWebApiPatternTests |
| 58 | public | ` public void P21_LocalWebAPI_References_Match_ADR0010()` | LocalWebApiPatternTests |
| 91 | public | ` public void P22_LocalWebAPI_Controllers_Must_Have_ApiController()` | LocalWebApiPatternTests |
| 24 | public | ` public void P09b_Controllers_Should_Use_V1_Routes()` | ServerArchTests |
| 80 | public | ` public void P09c_Controller_Must_Be_In_Controllers_Namespace()` | ServerArchTests |
| 111 | public | ` public void P10b_Service_Must_Have_Service_Suffix()` | ServerArchTests |
| 157 | public | ` public void P11_No_Redis_Usage()` | ServerArchTests |
| 172 | public | ` public void P11b_Only_Use_EntityFramework()` | ServerArchTests |
| 187 | public | ` public void P12_Entities_Should_Not_Depend_On_Business_Layers()` | ServerArchTests |
| 204 | public | ` public void P12b_Infrastructure_Should_Not_Depend_On_WebAPI()` | ServerArchTests |
| 221 | public | ` public void P13_Dto_Must_Have_Dto_Suffix()` | ServerArchTests |
| 244 | public | ` public void P14_Service_IO_Methods_Must_Be_Async()` | ServerArchTests |
| 299 | public | ` public void P15_Configuration_Must_Be_In_Correct_Location()` | ServerArchTests |
| 319 | public | ` public void P16_Modules_No_Circular_Dependencies()` | ServerArchTests |
| 357 | public | ` public void P17_Infrastructure_Hardening_Rules()` | ServerArchTests |
| 393 | public | ` public void P02b_AllRepositories_Must_Inherit_BaseRepository()` | ServerArchTests |
| 428 | public | ` public void P09_Controller_Must_Have_ClassLevel_Authorize()` | ServerArchTests |
| 509 | public | ` public void P08_CrossModule_References_Must_Use_Interfaces()` | ServerArchTests |
| 544 | public | ` public void P10_Services_Should_Not_Directly_Inject_AppDbContext()` | ServerArchTests |
| 595 | public | ` public void P18_Module_Repositories_Must_Inject_Own_DbContext()` | ServerArchTests |
| 637 | public | ` public void MC01_MedicalCase_StateTransition_Rules()` | ServerArchTests |
| 655 | public | ` public void MC02_MedicalCase_Validators_Must_Exist()` | ServerArchTests |
| 672 | public | ` public void A01_Controllers_Must_Inherit_BaseApiController()` | ServerArchTests |
| 699 | public | ` public void A02_Desktop_Repositories_Must_Inherit_ApiClientRepositoryBase()` | ServerArchTests |
| 720 | public | ` public void A03_Modules_Must_Have_DI_Registration()` | ServerArchTests |
| 748 | public | ` public void A04_Options_Must_Define_SectionName()` | ServerArchTests |
| 782 | public | ` public void A05_Controller_Methods_Must_Return_IActionResult()` | ServerArchTests |
| 826 | public | ` public void A06_Validators_Must_Inherit_AbstractValidator()` | ServerArchTests |
| 851 | public | ` public void A07_Mapperly_Mappers_Must_Have_Mapper_Attribute()` | ServerArchTests |
| 876 | private | ` private static bool InheritsApiClientRepositoryBase(Type type)` | ServerArchTests |
| 899 | public | ` public void P19_Cqrs_Services_Must_Not_Expose_Write_Methods()` | ServerArchTests |
| 930 | public | ` public void P19b_Cqrs_Write_Endpoints_Must_Not_Call_Service_Write_Methods()` | ServerArchTests |
| 972 | private | ` private static IEnumerable<string> DecodeServiceWriteCalls(byte[] il, System.Reflection.Module module)` | ServerArchTests |
| 1040 | private | ` private static int IlOperandSize(byte opcode)` | ServerArchTests |
### LYBT.Tests.Desktop

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 17 | public | ` public static AndConstraint<StringAssertions> HaveClaim( this StringAssertions assertions, string claimType, string? expectedValue = null, string bec…` | JwtAssertions |
| 47 | public | ` public static AndConstraint<StringAssertions> HaveRole( this StringAssertions assertions, string expectedRole, string because = "", params object[] b…` | JwtAssertions |
| 71 | public | ` public static AndConstraint<StringAssertions> HaveUsername( this StringAssertions assertions, string expectedUsername, string because = "", params ob…` | JwtAssertions |
| 102 | public | ` public static AndConstraint<StringAssertions> NotBeExpired( this StringAssertions assertions, string because = "", params object[] becauseArgs)` | JwtAssertions |
| 122 | public | ` public static AndConstraint<StringAssertions> HaveIssuer( this StringAssertions assertions, string expectedIssuer, string because = "", params object…` | JwtAssertions |
| 142 | public | ` public static AndConstraint<StringAssertions> HaveAudience( this StringAssertions assertions, string expectedAudience, string because = "", params ob…` | JwtAssertions |
| 163 | private | ` private static JwtSecurityToken ParseToken(string? tokenString)` | JwtAssertions |
| 187 | public | ` public static AndConstraint<ObjectAssertions> HaveClaim( this ObjectAssertions assertions, string claimType, string? expectedValue = null, string bec…` | JwtSecurityTokenAssertions |
| 26 | public | ` public static FormulaBuilder Create() => new();` | FormulaBuilder |
| 28 | public | ` public FormulaBuilder WithId(Guid id)` | FormulaBuilder |
| 34 | public | ` public FormulaBuilder WithName(string name)` | FormulaBuilder |
| 40 | public | ` public FormulaBuilder WithEffect(string effect)` | FormulaBuilder |
| 46 | public | ` public FormulaBuilder WithDescription(string? description)` | FormulaBuilder |
| 52 | public | ` public FormulaBuilder WithUsage(string usage)` | FormulaBuilder |
| 58 | public | ` public FormulaBuilder WithProperty(string? property)` | FormulaBuilder |
| 64 | public | ` public FormulaBuilder WithCategory(string? category)` | FormulaBuilder |
| 70 | public | ` public FormulaBuilder WithIsShared(bool isShared)` | FormulaBuilder |
| 76 | public | ` public FormulaBuilder WithInstructions(string? instructions)` | FormulaBuilder |
| 82 | public | ` public FormulaBuilder WithIndications(string? indications)` | FormulaBuilder |
| 88 | public | ` public FormulaBuilder WithContraindications(string? contraindications)` | FormulaBuilder |
| 94 | public | ` public FormulaBuilder WithPreparation(string? preparation)` | FormulaBuilder |
| 100 | public | ` public FormulaBuilder WithRemark(string? remark)` | FormulaBuilder |
| 106 | public | ` public FormulaBuilder WithHerbs(List<FormulaHerbItemInputDto> herbs)` | FormulaBuilder |
| 112 | public | ` public FormulaBuilder AddHerb(FormulaHerbItemInputDto herb)` | FormulaBuilder |
| 118 | public | ` public FormulaBuilder AddHerb(Guid herbId, string herbName, int dosage, string unit)` | FormulaBuilder |
| 133 | public | ` public FormulaInputDto BuildInputDto() => new()` | FormulaBuilder |
| 154 | public | ` public static FormulaBuilder Simple() => Create()` | FormulaBuilder |
| 163 | public | ` public static FormulaBuilder ColdRemedy() => Create()` | FormulaBuilder |
| 176 | public | ` public static FormulaBuilder Shared() => Create()` | FormulaBuilder |
| 24 | public | ` public static HerbBuilder Create() => new();` | HerbBuilder |
| 26 | public | ` public HerbBuilder WithId(Guid id)` | HerbBuilder |
| 32 | public | ` public HerbBuilder WithName(string name)` | HerbBuilder |
| 38 | public | ` public HerbBuilder WithPinYinCode(string? pinYinCode)` | HerbBuilder |
| 44 | public | ` public HerbBuilder WithCategory(string? category)` | HerbBuilder |
| 50 | public | ` public HerbBuilder WithOrigin(string? origin)` | HerbBuilder |
| 56 | public | ` public HerbBuilder WithSpec(string? spec)` | HerbBuilder |
| 62 | public | ` public HerbBuilder WithUnit(string unit)` | HerbBuilder |
| 68 | public | ` public HerbBuilder WithPrice(decimal price)` | HerbBuilder |
| 74 | public | ` public HerbBuilder WithCostPrice(decimal? costPrice)` | HerbBuilder |
| 80 | public | ` public HerbBuilder WithEffect(string? effect)` | HerbBuilder |
| 86 | public | ` public HerbBuilder WithUsage(string? usage)` | HerbBuilder |
| 92 | public | ` public HerbBuilder WithRemark(string? remark)` | HerbBuilder |
| 101 | public | ` public HerbInputDto BuildInputDto() => new()` | HerbBuilder |
| 120 | public | ` public static HerbBuilder GanCao() => Create()` | HerbBuilder |
| 134 | public | ` public static HerbBuilder RenShen() => Create()` | HerbBuilder |
| 148 | public | ` public static HerbBuilder DangGui() => Create()` | HerbBuilder |
| 162 | public | ` public static HerbBuilder SheXiang() => Create()` | HerbBuilder |
| 176 | public | ` public static HerbBuilder Simple() => Create()` | HerbBuilder |
| 21 | public | ` public static MedicalCaseBuilder Create() => new();` | MedicalCaseBuilder |
| 23 | public | ` public MedicalCaseBuilder WithId(Guid id)` | MedicalCaseBuilder |
| 29 | public | ` public MedicalCaseBuilder WithPatientId(Guid patientId)` | MedicalCaseBuilder |
| 35 | public | ` public MedicalCaseBuilder WithUserId(Guid userId)` | MedicalCaseBuilder |
| 41 | public | ` public MedicalCaseBuilder WithRegistrationId(Guid? registrationId)` | MedicalCaseBuilder |
| 47 | public | ` public MedicalCaseBuilder WithConsultation(ConsultationInputDto? consultation)` | MedicalCaseBuilder |
| 53 | public | ` public MedicalCaseBuilder WithPrescription(PrescriptionInputDto? prescription)` | MedicalCaseBuilder |
| 59 | public | ` public MedicalCaseBuilder WithNeedsPrescription(bool? needsPrescription)` | MedicalCaseBuilder |
| 68 | public | ` public MedicalCaseInputDto BuildInputDto() => new()` | MedicalCaseBuilder |
| 82 | public | ` public static MedicalCaseBuilder Simple(Guid patientId, Guid userId) => Create()` | MedicalCaseBuilder |
| 89 | public | ` public static MedicalCaseBuilder Complete(Guid patientId, Guid userId) => Create()` | MedicalCaseBuilder |
| 108 | public | ` public static MedicalCaseBuilder WithoutPrescription(Guid patientId, Guid userId) => Create()` | MedicalCaseBuilder |
| 19 | public | ` public static PatientBuilder Create() => new();` | PatientBuilder |
| 21 | public | ` public PatientBuilder WithId(Guid id)` | PatientBuilder |
| 27 | public | ` public PatientBuilder WithName(string name)` | PatientBuilder |
| 33 | public | ` public PatientBuilder WithIdNumber(string idNumber)` | PatientBuilder |
| 39 | public | ` public PatientBuilder WithPhoneNumber(string? phoneNumber)` | PatientBuilder |
| 45 | public | ` public PatientBuilder WithBirthDate(DateTime? birthDate)` | PatientBuilder |
| 51 | public | ` public PatientBuilder WithGender(Gender gender)` | PatientBuilder |
| 57 | public | ` public PatientInputDto BuildInputDto() => new()` | PatientBuilder |
| 68 | public | ` public PatientDetailDto BuildDetailDto() => new()` | PatientBuilder |
| 83 | public | ` public PatientListDto BuildListDto() => new()` | PatientBuilder |
| 96 | public | ` public static PatientBuilder AdultMale() => Create()` | PatientBuilder |
| 105 | public | ` public static PatientBuilder AdultFemale() => Create()` | PatientBuilder |
| 114 | public | ` public static PatientBuilder Child() => Create()` | PatientBuilder |
| 123 | public | ` public static PatientBuilder WithAllergicHistory() => Create()` | PatientBuilder |
| 24 | public | ` public static UserBuilder Create() => new();` | UserBuilder |
| 26 | public | ` public UserBuilder WithId(Guid id)` | UserBuilder |
| 32 | public | ` public UserBuilder WithUserName(string userName)` | UserBuilder |
| 38 | public | ` public UserBuilder WithRealName(string realName)` | UserBuilder |
| 44 | public | ` public UserBuilder WithEmail(string? email)` | UserBuilder |
| 50 | public | ` public UserBuilder WithPhoneNumber(string? phoneNumber)` | UserBuilder |
| 56 | public | ` public UserBuilder WithStatus(CommonStatus status)` | UserBuilder |
| 62 | public | ` public UserBuilder WithRole(UserRole role)` | UserBuilder |
| 68 | public | ` public UserBuilder WithLastLoginTime(DateTime lastLoginTime)` | UserBuilder |
| 74 | public | ` public UserBuilder WithRemark(string? remark)` | UserBuilder |
| 83 | public | ` public UserDetailDto Build() => new()` | UserBuilder |
| 100 | public | ` public LoginRequest BuildLoginRequest(string password = "Test123!") => new()` | UserBuilder |
| 109 | public | ` public static UserBuilder Admin() => Create()` | UserBuilder |
| 117 | public | ` public static UserBuilder Doctor() => Create()` | UserBuilder |
| 125 | public | ` public static UserBuilder Disabled() => Create()` | UserBuilder |
| 24 | public | ` public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)` | LocalDbContext |
| 28 | public | ` public LocalDbContext( DbContextOptions<LocalDbContext> options, ICurrentUserProvider currentUserProvider) : base(options)` | LocalDbContext |
| 69 | protected | ` protected override void OnModelCreating(ModelBuilder modelBuilder)` | LocalDbContext |
| 86 | private | ` private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)` | LocalDbContext |
| 102 | private | ` private static void ConfigureRelationships(ModelBuilder modelBuilder)` | LocalDbContext |
| 137 | private | ` private static void ConfigureIndexes(ModelBuilder modelBuilder)` | LocalDbContext |
| 165 | public | ` public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)` | LocalDbContext |
| 171 | public | ` public override int SaveChanges()` | LocalDbContext |
| 177 | private | ` private void SetAuditFields()` | LocalDbContext |
| 23 | public | ` public static Patient CreatePatient( string? name = null, Gender? gender = null, string? phoneNumber = null, DateTime? birthDate = null, string? idNu…` | TestDataFactory |
| 52 | public | ` public static ApplicationUser CreateUser( string? userName = null, string? realName = null, UserRole? role = null, string? passwordHash = null)` | TestDataFactory |
| 78 | public | ` public static MedicalCase CreateMedicalCase( Guid? patientId = null, string? patientName = null, Guid? userId = null, string? doctorName = null, Medi…` | TestDataFactory |
| 105 | public | ` public static Consultation CreateConsultation( Guid? medicalCaseId = null, string? presentIllness = null, string? tongueDiagnosis = null, string? pul…` | TestDataFactory |
| 127 | public | ` public static Prescription CreatePrescription( Guid? medicalCaseId = null, int? dosageCount = null, string? usage = null, string? advice = null)` | TestDataFactory |
| 150 | public | ` public static PrescriptionItem CreatePrescriptionItem( Guid? prescriptionId = null, Guid? herbId = null, string? herbName = null, int? dosage = null,…` | TestDataFactory |
| 173 | public | ` public static async Task<Patient> SavePatientAsync(LocalDbContext context, Patient? patient = null)` | TestDataFactory |
| 184 | public | ` public static async Task<ApplicationUser> SaveUserAsync(LocalDbContext context, ApplicationUser? user = null)` | TestDataFactory |
| 195 | public | ` public static async Task<MedicalCase> SaveMedicalCaseAsync( LocalDbContext context, MedicalCase? medicalCase = null, Consultation? consultation = nul…` | TestDataFactory |
| 242 | public | ` public static void ResetCounters()` | TestDataFactory |
| 250 | private | ` private static Patient CreatePatient(Guid id, string name)` | TestDataFactory |
| 265 | private | ` private static ApplicationUser CreateUser(Guid id, string realName)` | TestDataFactory |
| 12 | public | ` public void Invoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)` | TestUiThreadDispatcher |
| 18 | public | ` public T Invoke<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)` | TestUiThreadDispatcher |
| 24 | public | ` public Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)` | TestUiThreadDispatcher |
| 31 | public | ` public Task<T> InvokeAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)` | TestUiThreadDispatcher |
| 37 | public | ` public void BeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)` | TestUiThreadDispatcher |
| 43 | public | ` public bool CheckAccess() => true;` | TestUiThreadDispatcher |
| 23 | default(private) | ` _serviceProvider ?? throw new InvalidOperationException("Fixture not initialized");` | UserJourneyFixture |
| 39 | public | ` public async Task InitializeAsync()` | UserJourneyFixture |
| 66 | public | ` public async Task DisposeAsync()` | UserJourneyFixture |
| 85 | public | ` public void Dispose()` | UserJourneyFixture |
| 94 | public | ` public IServiceScope CreateScope() =>` | UserJourneyFixture |
| 101 | public | ` public async Task ResetDatabaseAsync()` | UserJourneyFixture |
| 126 | protected | ` protected virtual void ConfigureServices(IServiceCollection services)` | UserJourneyFixture |
| 27 | protected | ` protected UserJourneyTestBase(UserJourneyFixture fixture)` | UserJourneyTestBase |
| 52 | protected | ` protected TViewModel CreateViewModel<TViewModel>(Action<IServiceCollection>? additionalConfiguration = null)` | UserJourneyTestBase |
| 76 | protected | ` protected TViewModel CreateViewModel<TViewModel>(Func<IServiceProvider, TViewModel> factory)` | UserJourneyTestBase |
| 89 | protected | ` protected IViewModelServices CreateViewModelServicesMock()` | UserJourneyTestBase |
| 117 | protected | ` protected IMasterDetailServices<TList, TDetail> CreateMasterDetailServicesMock<TList, TDetail>()` | UserJourneyTestBase |
| 164 | protected | ` protected virtual void ConfigureBaseServices(IServiceCollection services)` | UserJourneyTestBase |
| 191 | protected | ` protected async Task SaveChangesAsync()` | UserJourneyTestBase |
| 199 | protected | ` protected async Task ResetDatabaseAsync()` | UserJourneyTestBase |
| 207 | public | ` public void Dispose()` | UserJourneyTestBase |
| 15 | public | ` public static void InitializeWpf()` | WpfTestHelper |
| 45 | public | ` public async Task EndToEnd_Login_Store_ValidateInSameSession()` | AuthenticationIntegrationTests |
| 118 | public | ` public async Task MemoryStorage_NewServiceProvider_TokenNotShared()` | AuthenticationIntegrationTests |
| 159 | public | ` public async Task TokenRefresh_ExpiredToken_AutoRefresh()` | AuthenticationIntegrationTests |
| 252 | public | ` public async Task TokenClear_ExpiredToken_RequireRelogin()` | AuthenticationIntegrationTests |
| 312 | private | ` private IServiceProvider CreateServiceProvider()` | AuthenticationIntegrationTests |
| 342 | private | ` private string GenerateValidToken(Guid userId, string userName, string role)` | AuthenticationIntegrationTests |
| 368 | private | ` private string GenerateTokenWithCustomExpiry( Guid userId, string userName, string role, DateTime notBefore, DateTime expires)` | AuthenticationIntegrationTests |
| 15 | public | ` public AuthNegativeTests(ITestOutputHelper output)` | AuthNegativeTests |
| 23 | public | ` public async Task Login_EmptyUsername_ShouldFail()` | AuthNegativeTests |
| 41 | public | ` public async Task Login_EmptyPassword_ShouldFail()` | AuthNegativeTests |
| 59 | public | ` public async Task Login_WrongPassword_ShouldFail()` | AuthNegativeTests |
| 77 | public | ` public async Task Login_NonexistentUser_ShouldFail()` | AuthNegativeTests |
| 99 | public | ` public async Task RefreshToken_InvalidToken_ShouldFail()` | AuthNegativeTests |
| 25 | public | ` public AuthTests(ITestOutputHelper output)` | AuthTests |
| 37 | public | ` public async Task Login_WithValidCredentials_ShouldReturnToken()` | AuthTests |
| 77 | public | ` public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()` | AuthTests |
| 102 | public | ` public async Task ValidateToken_AfterLogin_ShouldReturnUserInfo()` | AuthTests |
| 126 | public | ` public async Task ValidateToken_FromHeader_ShouldReturnUserInfo()` | AuthTests |
| 140 | public | ` public async Task Login_WithAutoToken_ShouldReturnAutoLoginToken()` | AuthTests |
| 160 | public | ` public async Task RefreshToken_WithValidToken_ShouldReturnNewToken()` | AuthTests |
| 191 | public | ` public async Task Logout_AfterLogin_ShouldSucceed()` | AuthTests |
| 27 | public | ` public DiagnosticsTests(ITestOutputHelper output)` | DiagnosticsTests |
| 43 | public | ` public async Task GetLoggingStatus_ShouldReturnCurrentStatus()` | DiagnosticsTests |
| 72 | public | ` public async Task EnableDebugMode_ShouldEnableDebugLogging()` | DiagnosticsTests |
| 107 | public | ` public async Task SetLoggingLevel_ShouldUpdateLogLevel()` | DiagnosticsTests |
| 141 | public | ` public async Task DisableDebugMode_ShouldDisableDebugLogging()` | DiagnosticsTests |
| 20 | public | ` public HealthCheckTests(ITestOutputHelper output)` | HealthCheckTests |
| 32 | public | ` public async Task HealthEndpoint_ShouldReturnHealthy()` | HealthCheckTests |
| 55 | public | ` public async Task PingEndpoint_ShouldReturnPong()` | HealthCheckTests |
| 82 | public | ` public async Task HealthDetailedEndpoint_ShouldReturnDatabaseStatus()` | HealthCheckTests |
| 20 | public | ` public RetryPolicyIntegrationTests()` | RetryPolicyIntegrationTests |
| 28 | public | ` public async Task RetryPolicy_WhenTransientFailure_ShouldRetryAndSucceed()` | RetryPolicyIntegrationTests |
| 51 | public | ` public async Task RetryPolicy_WhenAllRetriesFail_ShouldReturnLastFailure()` | RetryPolicyIntegrationTests |
| 70 | public | ` public async Task RetryPolicy_WhenHttpRequestException_ShouldRetry()` | RetryPolicyIntegrationTests |
| 93 | public | ` public async Task RetryPolicy_WhenNonRetryableStatusCode_ShouldNotRetry()` | RetryPolicyIntegrationTests |
| 112 | public | ` public async Task RetryPolicy_WhenInternalServerError_ShouldNotRetry()` | RetryPolicyIntegrationTests |
| 135 | public | ` public async Task CircuitBreaker_WhenThresholdExceeded_ShouldBreak()` | RetryPolicyIntegrationTests |
| 162 | public | ` public async Task CircuitBreaker_WhenBelowThreshold_ShouldNotBreak()` | RetryPolicyIntegrationTests |
| 189 | public | ` public async Task TimeoutPolicy_WhenWithinTimeout_ShouldSucceed()` | RetryPolicyIntegrationTests |
| 208 | public | ` public async Task TimeoutPolicy_WhenExceedsTimeout_ShouldThrow()` | RetryPolicyIntegrationTests |
| 231 | public | ` public async Task CompositePolicy_WhenTransientFailure_ShouldRetryAndSucceed()` | RetryPolicyIntegrationTests |
| 259 | public | ` public async Task CompositePolicy_WhenSuccessOnFirstAttempt_ShouldNotRetry()` | RetryPolicyIntegrationTests |
| 42 | public | ` public TokenRefreshHandlerIntegrationTests()` | TokenRefreshHandlerIntegrationTests |
| 54 | public | ` public void Dispose()` | TokenRefreshHandlerIntegrationTests |
| 65 | public | ` public async Task SendAsync_WhenUserInactive_ShouldNotRefreshToken()` | TokenRefreshHandlerIntegrationTests |
| 116 | public | ` public async Task SendAsync_WhenUserActive_AndTokenExpiring_ShouldAttemptRefresh()` | TokenRefreshHandlerIntegrationTests |
| 182 | public | ` public async Task SendAsync_WhenTokenNotExpiring_ShouldNotRefresh()` | TokenRefreshHandlerIntegrationTests |
| 229 | public | ` public async Task SendAsync_WhenNotLoggedIn_ShouldPassThrough()` | TokenRefreshHandlerIntegrationTests |
| 266 | public | ` public async Task SendAsync_WithoutUserActivityState_ShouldRefreshNormally()` | TokenRefreshHandlerIntegrationTests |
| 317 | public | ` public MockHttpMessageHandler( HttpStatusCode statusCode, string content, ApiResponse<LoginResponse>? refreshApiResponse = null)` | MockHttpMessageHandler |
| 327 | protected | ` protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken)` | MockHttpMessageHandler |
| 15 | public | ` public FrameworkVerificationTests(UserJourneyFixture fixture) : base(fixture)` | FrameworkVerificationTests |
| 20 | public | ` public async Task Database_ShouldBeInitialized()` | FrameworkVerificationTests |
| 30 | public | ` public async Task TestDataFactory_CreatePatient_ShouldSaveToDatabase()` | FrameworkVerificationTests |
| 46 | public | ` public async Task TestDataFactory_SavePatientAsync_ShouldCreatePatient()` | FrameworkVerificationTests |
| 58 | public | ` public async Task TestDataFactory_CreateUser_ShouldSaveToDatabase()` | FrameworkVerificationTests |
| 75 | public | ` public async Task TestDataFactory_SaveMedicalCaseAsync_ShouldCreateCompleteCase()` | FrameworkVerificationTests |
| 99 | public | ` public void CreateViewModelServicesMock_ShouldReturnConfiguredMock()` | FrameworkVerificationTests |
| 113 | public | ` public void CreateMasterDetailServicesMock_ShouldReturnConfiguredMock()` | FrameworkVerificationTests |
| 133 | public | ` public async Task ResetDatabase_ShouldClearAllData()` | FrameworkVerificationTests |
| 158 | public | ` public async Task ServiceProvider_ShouldResolveLocalDbContext()` | FrameworkVerificationTests |
| 170 | public | ` public void WpfTestHelper_ShouldInitializeWithoutError()` | FrameworkVerificationTests |
| 20 | protected | ` protected new async Task<LoginResponse> LoginAsAdminAsync()` | AdminTestBase |
| 31 | protected | ` protected async Task<LoginResponse> LoginAsSuperAdminAsync()` | AdminTestBase |
| 39 | protected | ` protected async Task<bool> VerifyAdminUserManagementAccess()` | AdminTestBase |
| 48 | protected | ` protected async Task<bool> VerifySuperAdminDiagnosticsAccess()` | AdminTestBase |
| 21 | public | ` public AuthenticationDelegatingHandler(TokenHolder tokenHolder)` | AuthenticationDelegatingHandler |
| 26 | protected | ` protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken)` | AuthenticationDelegatingHandler |
| 16 | protected | ` protected new async Task<LoginResponse> LoginAsDoctorAsync()` | DoctorTestBase |
| 24 | protected | ` protected async Task<bool> VerifyDoctorMedicalCaseAccess()` | DoctorTestBase |
| 8 | public | ` public static T AssertSuccess<T>(ApiResponse<T> response)` | E2EAssertionHelpers |
| 17 | public | ` public static void AssertError<T>(ApiResponse<T> response, string? expectedMessagePart = null)` | E2EAssertionHelpers |
| 28 | public | ` public static PagedResult<TItem> AssertPaged<TItem>( ApiResponse<PagedResult<TItem>> response, int? expectedMinCount = null)` | E2EAssertionHelpers |
| 47 | public | ` public static async Task AssertUnauthorized(Func<Task> action)` | E2EAssertionHelpers |
| 53 | public | ` public static async Task AssertForbidden(Func<Task> action)` | E2EAssertionHelpers |
| 59 | public | ` public static async Task<Refit.ApiException> AssertApiException( Func<Task> action, System.Net.HttpStatusCode expectedStatus)` | E2EAssertionHelpers |
| 7 | protected | ` protected new async Task<LoginResponse> LoginAsReceptionistAsync()` | ReceptionistTestBase |
| 15 | protected | ` protected async Task<bool> VerifyReceptionistRegistrationAccess()` | ReceptionistTestBase |
| 23 | public | ` public TestDataTracker(IServiceProvider serviceProvider, ILogger logger)` | TestDataTracker |
| 29 | public | ` public void Track(EntityType type, Guid id)` | TestDataTracker |
| 34 | public | ` public Guid Track(EntityType type, ApiResponse<object> response)` | TestDataTracker |
| 44 | public | ` public async ValueTask DisposeAsync()` | TestDataTracker |
| 67 | private | ` private async Task DeleteEntityAsync(EntityType type, Guid id)` | TestDataTracker |
| 96 | private | ` private static Guid ExtractId(object data)` | TestDataTracker |
| 42 | protected | ` protected WebApiE2ETestBase()` | WebApiE2ETestBase |
| 77 | protected | ` protected async Task<LoginResponse> LoginAsAsync(string username, string password)` | WebApiE2ETestBase |
| 111 | protected | ` protected async Task<LoginResponse> LoginAsSysadminAsync()` | WebApiE2ETestBase |
| 153 | protected | ` protected async Task<LoginResponse> LoginAsAdminAsync()` | WebApiE2ETestBase |
| 165 | protected | ` protected async Task<LoginResponse> LoginAsDoctorAsync()` | WebApiE2ETestBase |
| 177 | protected | ` protected async Task<LoginResponse> LoginAsReceptionistAsync()` | WebApiE2ETestBase |
| 189 | private | ` private async Task<LoginResponse> LoginOrCreateUserAsync(string username, string password, string realName, UserRole role)` | WebApiE2ETestBase |
| 240 | protected | ` protected HttpClient CreateAuthenticatedClient()` | WebApiE2ETestBase |
| 256 | internal | ` internal IAuthApi CreateAuthenticatedAuthApi()` | WebApiE2ETestBase |
| 274 | protected | ` protected string GetBaseUrl()` | WebApiE2ETestBase |
| 279 | private | ` private IConfiguration BuildConfiguration()` | WebApiE2ETestBase |
| 290 | private | ` private void ConfigureRefitClients(IServiceCollection services)` | WebApiE2ETestBase |
| 309 | private | ` private static RefitSettings CreateRefitSettings()` | WebApiE2ETestBase |
| 322 | private | ` private static HttpClientHandler CreateHttpMessageHandler(bool skipSslValidation)` | WebApiE2ETestBase |
| 332 | private | ` private static void ConfigureClient<T>( IServiceCollection services, string baseUrl, int timeoutSeconds, bool skipSslValidation, RefitSettings refitS…` | WebApiE2ETestBase |
| 362 | public | ` public virtual void Dispose()` | WebApiE2ETestBase |
| 370 | public | ` public virtual async ValueTask DisposeAsync()` | WebApiE2ETestBase |
| 19 | public | ` public async Task Sysadmin_Can_Login_With_Default_Password()` | AuthControllerTests |
| 33 | public | ` public async Task Sysadmin_Token_Contains_IsSysAdmin_Claim()` | AuthControllerTests |
| 58 | public | ` public async Task Sysadmin_Can_Access_Validated_With_Token()` | AuthControllerTests |
| 73 | public | ` public async Task Validate_Without_Token_Returns_IsValid_False()` | AuthControllerTests |
| 87 | public | ` public async Task Admin_Can_Login_With_Default_Password()` | AuthControllerTests |
| 103 | public | ` public async Task Login_With_Wrong_Password_Returns_Unauthorized()` | AuthControllerTests |
| 110 | public | ` public async Task Login_With_Nonexistent_User_Returns_Unauthorized()` | AuthControllerTests |
| 121 | public | ` public async Task Logout_Returns_Ok()` | AuthControllerTests |
| 132 | private | ` private async Task<HttpResponseMessage> LoginAsync(string username, string password)` | AuthControllerTests |
| 138 | private | ` private async Task<string> GetSysadminTokenAsync()` | AuthControllerTests |
| 17 | private | ` private async Task AuthenticateAsync()` | FormulasControllerTests |
| 23 | private | ` private async Task<JsonElement> CreateTestFormulaAsync(string? name = null)` | FormulasControllerTests |
| 42 | public | ` public async Task GetFormulas_Returns_Ok()` | FormulasControllerTests |
| 56 | public | ` public async Task CreateFormula_And_GetById_Works()` | FormulasControllerTests |
| 72 | public | ` public async Task DeleteFormula_Soft_Deletes()` | FormulasControllerTests |
| 89 | public | ` public async Task CloneFormula_Creates_Copy()` | FormulasControllerTests |
| 109 | public | ` public async Task ToggleStatus_Toggles_Formula()` | FormulasControllerTests |
| 132 | public | ` public async Task RestoreFormula_Works_After_Soft_Delete()` | FormulasControllerTests |
| 16 | public | ` public async Task Ping_Returns_Ok()` | HealthControllerTests |
| 27 | public | ` public async Task GetHealth_Returns_Ok()` | HealthControllerTests |
| 39 | public | ` public async Task GetDetails_Returns_User_Count()` | HealthControllerTests |
| 17 | private | ` private async Task AuthenticateAsync()` | HerbsControllerTests |
| 23 | private | ` private async Task<JsonElement> CreateTestHerbAsync(string? name = null, string? category = null)` | HerbsControllerTests |
| 41 | public | ` public async Task GetHerbs_Returns_Ok()` | HerbsControllerTests |
| 55 | public | ` public async Task CreateHerb_And_GetById_Works()` | HerbsControllerTests |
| 71 | public | ` public async Task DeleteHerb_Soft_Deletes()` | HerbsControllerTests |
| 88 | public | ` public async Task RestoreHerb_Works_After_Soft_Delete()` | HerbsControllerTests |
| 108 | public | ` public async Task ToggleStatus_Toggles_Herb()` | HerbsControllerTests |
| 131 | public | ` public async Task GetCategories_Returns_Distinct()` | HerbsControllerTests |
| 43 | public | ` public async Task InitializeAsync()` | LocalWebApiControllerTestBase |
| 131 | public | ` public async Task DisposeAsync()` | LocalWebApiControllerTestBase |
| 156 | protected | ` protected async Task<string> GetAdminTokenAsync()` | LocalWebApiControllerTestBase |
| 175 | protected | ` protected void SetAuthHeader(string token)` | LocalWebApiControllerTestBase |
| 26 | private | ` private AppDbContext CreateContext()` | AppDbContextTests |
| 36 | public | ` public void Dispose()` | AppDbContextTests |
| 46 | public | ` public async Task Can_Create_And_Query_Patients()` | AppDbContextTests |
| 60 | public | ` public async Task Soft_Delete_Filters_Work()` | AppDbContextTests |
| 82 | public | ` public async Task SeedData_Creates_Admin_User()` | AppDbContextTests |
| 100 | public | ` public void GenerateToken_Produces_Valid_Jwt()` | LocalJwtConfigTests |
| 117 | public | ` public void GenerateToken_Contains_Sub_Claim()` | LocalJwtConfigTests |
| 17 | private | ` private async Task AuthenticateAsync()` | MedicalCasesControllerTests |
| 64 | public | ` public async Task GetMedicalCases_Returns_Ok()` | MedicalCasesControllerTests |
| 77 | public | ` public async Task CreateMedicalCase_Works()` | MedicalCasesControllerTests |
| 88 | public | ` public async Task GetMedicalCase_Returns_NotFound_For_Invalid_Id()` | MedicalCasesControllerTests |
| 98 | public | ` public async Task Search_Returns_Empty_When_No_Match()` | MedicalCasesControllerTests |
| 111 | public | ` public async Task GetByStatus_Returns_Filtered()` | MedicalCasesControllerTests |
| 133 | public | ` public async Task GetPendingCases_Returns_Ok()` | MedicalCasesControllerTests |
| 17 | private | ` private async Task AuthenticateAsync()` | PatientsControllerTests |
| 23 | private | ` private async Task<JsonElement> CreateTestPatientAsync(string? name = null, string? idNumber = null)` | PatientsControllerTests |
| 41 | public | ` public async Task GetPatients_Returns_Ok()` | PatientsControllerTests |
| 55 | public | ` public async Task CreatePatient_And_GetById_Works()` | PatientsControllerTests |
| 71 | public | ` public async Task DeletePatient_Soft_Deletes()` | PatientsControllerTests |
| 88 | public | ` public async Task RestorePatient_Works_After_Soft_Delete()` | PatientsControllerTests |
| 108 | public | ` public async Task GetByIdNumber_Returns_Patient()` | PatientsControllerTests |
| 124 | public | ` public async Task TogglePatientStatus_Toggles()` | PatientsControllerTests |
| 17 | private | ` private async Task AuthenticateAsync()` | RegistrationsControllerTests |
| 23 | private | ` private async Task<JsonElement> CreateTestRegistrationAsync()` | RegistrationsControllerTests |
| 61 | public | ` public async Task GetRegistrations_Returns_Ok()` | RegistrationsControllerTests |
| 74 | public | ` public async Task CreateRegistration_Works()` | RegistrationsControllerTests |
| 85 | public | ` public async Task GetQueue_Returns_Ok()` | RegistrationsControllerTests |
| 102 | public | ` public async Task StartVisit_Returns_NotFound_For_Invalid_Id()` | RegistrationsControllerTests |
| 112 | public | ` public async Task Cancel_Returns_NotFound_For_Invalid_Id()` | RegistrationsControllerTests |
| 122 | public | ` public async Task DeleteRegistration_Soft_Deletes()` | RegistrationsControllerTests |
| 19 | private | ` private async Task AuthenticateAsync()` | UsersControllerTests |
| 26 | public | ` public async Task GetAll_Returns_Admin_User()` | UsersControllerTests |
| 41 | public | ` public async Task GetById_Returns_Admin()` | UsersControllerTests |
| 61 | public | ` public async Task GetById_Returns_NotFound_For_Invalid_Id()` | UsersControllerTests |
| 71 | public | ` public async Task Create_User_Succeeds()` | UsersControllerTests |
| 93 | public | ` public async Task Create_Duplicate_User_Returns_Conflict()` | UsersControllerTests |
| 111 | public | ` public async Task ToggleStatus_Toggles_User_Status()` | UsersControllerTests |
| 17 | public | ` public FormulaNegativeTests(ITestOutputHelper output)` | FormulaNegativeTests |
| 22 | private | ` private async Task<Guid> CreateTestHerbAsync()` | FormulaNegativeTests |
| 38 | public | ` public async Task CreateFormula_EmptyHerbsList_ShouldFail()` | FormulaNegativeTests |
| 61 | public | ` public async Task CreateFormula_EmptyName_ShouldFail()` | FormulaNegativeTests |
| 88 | public | ` public async Task GetFormula_NonexistentId_ShouldFail()` | FormulaNegativeTests |
| 107 | public | ` public async Task CloneFormula_NonexistentId_ShouldFail()` | FormulaNegativeTests |
| 16 | public | ` public FormulaTests(ITestOutputHelper output)` | FormulaTests |
| 21 | private | ` private static HerbInputDto CreateTestHerbInput(string suffix = "") => new()` | FormulaTests |
| 29 | private | ` private async Task<Guid> CreateTestHerbAsync()` | FormulaTests |
| 37 | private | ` private FormulaInputDto CreateTestFormulaInput(string suffix = "", Guid? herbId = null) => new()` | FormulaTests |
| 64 | public | ` public async Task CreateFormula_ValidInput_ReturnsCreatedFormula()` | FormulaTests |
| 82 | public | ` public async Task GetFormulaById_ExistingFormula_ReturnsDetail()` | FormulaTests |
| 102 | public | ` public async Task UpdateFormula_ValidInput_ReturnsUpdatedFormula()` | FormulaTests |
| 123 | public | ` public async Task GetFormulas_WithPagination_ReturnsPagedResult()` | FormulaTests |
| 146 | public | ` public async Task CloneFormula_ExistingFormula_ReturnsClonedFormula()` | FormulaTests |
| 167 | public | ` public async Task ToggleStatus_EnabledFormula_TogglesSuccessfully()` | FormulaTests |
| 190 | private | ` private async Task DeleteAndRestore_Formula_CompletesSuccessfully()` | FormulaTests |
| 214 | private | ` private async Task<FormulaDetailDto> CreateTestFormulaAsync(string suffix = "")` | FormulaTests |
| 227 | public | ` public async Task BatchDelete_MultipleFormulas_ReturnsOperationResult()` | FormulaTests |
| 250 | public | ` public async Task BatchImport_ValidData_ReturnsImportResult()` | FormulaTests |
| 289 | public | ` public async Task ExportTemplate_ReturnsFileResponse()` | FormulaTests |
| 304 | public | ` public async Task ExportFormulas_WithCategory_ReturnsFileResponse()` | FormulaTests |
| 324 | public | ` public async Task GetFormulas_WithKeyword_FiltersResults()` | FormulaTests |
| 350 | public | ` public async Task FormulaFullLifecycle_CreateCloneToggleDeleteRestore_AllSucceed()` | FormulaTests |
| 16 | public | ` public HerbNegativeTests(ITestOutputHelper output)` | HerbNegativeTests |
| 24 | public | ` public async Task CreateHerb_EmptyName_ShouldFail()` | HerbNegativeTests |
| 43 | public | ` public async Task CreateHerb_NegativePrice_ShouldFail()` | HerbNegativeTests |
| 67 | public | ` public async Task CreateHerb_PriceExceedsMax_ShouldFail()` | HerbNegativeTests |
| 91 | public | ` public async Task CreateHerb_EmptyUnit_ShouldFail()` | HerbNegativeTests |
| 115 | public | ` public async Task GetHerb_NonexistentId_ShouldFail()` | HerbNegativeTests |
| 14 | public | ` public HerbTests(ITestOutputHelper output)` | HerbTests |
| 19 | private | ` private static HerbInputDto CreateTestHerbInput(string suffix = "") => new()` | HerbTests |
| 40 | public | ` public async Task CreateHerb_ValidInput_ReturnsCreatedHerb()` | HerbTests |
| 57 | public | ` public async Task GetHerbById_ExistingHerb_ReturnsDetail()` | HerbTests |
| 76 | public | ` public async Task UpdateHerb_ValidInput_ReturnsUpdatedHerb()` | HerbTests |
| 96 | public | ` public async Task GetHerbs_WithPagination_ReturnsPagedResult()` | HerbTests |
| 118 | public | ` public async Task ToggleStatus_EnabledHerb_TogglesSuccessfully()` | HerbTests |
| 140 | private | ` private async Task DeleteAndRestore_Herb_CompletesSuccessfully()` | HerbTests |
| 163 | private | ` private async Task<HerbDetailDto> CreateTestHerbAsync(string suffix = "")` | HerbTests |
| 176 | public | ` public async Task BatchDelete_MultipleHerbs_ReturnsOperationResult()` | HerbTests |
| 205 | public | ` public async Task GetHerbs_WithKeyword_FiltersResults()` | HerbTests |
| 226 | public | ` public async Task GetHerbs_WithCategory_FiltersResults()` | HerbTests |
| 248 | public | ` public async Task ExportTemplate_ReturnsFileResponse()` | HerbTests |
| 262 | public | ` public async Task ExportHerbs_WithKeyword_ReturnsFileResponse()` | HerbTests |
| 280 | private | ` private async Task HerbFullLifecycle_CreateUpdateToggleDeleteRestore_AllSucceed()` | HerbTests |
| 22 | public | ` public MedicalCaseNegativeTests(ITestOutputHelper output)` | MedicalCaseNegativeTests |
| 27 | private | ` private static string GenerateValidIdNumber()` | MedicalCaseNegativeTests |
| 40 | private | ` private async Task<Guid> CreateTestPatientAsync()` | MedicalCaseNegativeTests |
| 57 | public | ` public async Task CreateCase_NonexistentPatientId_ShouldFail()` | MedicalCaseNegativeTests |
| 85 | public | ` public async Task CreateCase_NonexistentDoctorId_ShouldFail()` | MedicalCaseNegativeTests |
| 114 | public | ` public async Task GetCase_NonexistentId_ShouldFail()` | MedicalCaseNegativeTests |
| 133 | public | ` public async Task CloseCase_AlreadyClosed_ShouldFail()` | MedicalCaseNegativeTests |
| 166 | public | ` public async Task DeleteCase_NonexistentId_ShouldFail()` | MedicalCaseNegativeTests |
| 19 | public | ` public MedicalCaseTests(ITestOutputHelper output)` | MedicalCaseTests |
| 28 | private | ` private static string GenerateIdNumber()` | MedicalCaseTests |
| 47 | private | ` private static string GeneratePhoneNumber()` | MedicalCaseTests |
| 61 | private | ` private async Task<Guid> CreateTestPatientAsync()` | MedicalCaseTests |
| 89 | private | ` private MedicalCaseInputDto CreateTestCaseInput(Guid patientId, Guid userId) => new()` | MedicalCaseTests |
| 108 | public | ` public async Task CreateMedicalCase_WithConsultation_ReturnsCreatedCase()` | MedicalCaseTests |
| 125 | public | ` public async Task GetMedicalCaseById_ExistingCase_ReturnsDetail()` | MedicalCaseTests |
| 146 | public | ` public async Task GetMedicalCases_WithPagination_ReturnsPagedResult()` | MedicalCaseTests |
| 169 | public | ` public async Task SaveMedicalCase_UpdateConsultation_Succeeds()` | MedicalCaseTests |
| 196 | public | ` public async Task QueryMedicalCases_ByPatient_ReturnsMatchingCases()` | MedicalCaseTests |
| 217 | public | ` public async Task SearchMedicalCases_ByDiagnosisKeyword_ReturnsMatchingCases()` | MedicalCaseTests |
| 241 | public | ` public async Task SetPrescriptionFlag_ToggleFlag_Succeeds()` | MedicalCaseTests |
| 262 | public | ` public async Task SaveMedicalCase_WithPrescription_Succeeds()` | MedicalCaseTests |
| 309 | public | ` public async Task CloseCase_ActiveCase_ClosesSuccessfully()` | MedicalCaseTests |
| 328 | public | ` public async Task SuspendCase_ActiveCase_SuspendsSuccessfully()` | MedicalCaseTests |
| 347 | public | ` public async Task CancelMedicalCase_ActiveCase_CancelsSuccessfully()` | MedicalCaseTests |
| 368 | public | ` public async Task UpdateStatus_ActiveToSuspended_UpdatesSuccessfully()` | MedicalCaseTests |
| 393 | public | ` public async Task GetPendingCases_ReturnsListSuccessfully()` | MedicalCaseTests |
| 407 | public | ` public async Task GetPermissions_ExistingCase_ReturnsPermissions()` | MedicalCaseTests |
| 423 | public | ` public async Task GetAuditLogs_ExistingCase_ReturnsAuditLogs()` | MedicalCaseTests |
| 443 | public | ` public async Task DeleteMedicalCase_ExistingCase_Succeeds()` | MedicalCaseTests |
| 462 | public | ` public async Task BatchDelete_MultipleCases_ReturnsOperationResult()` | MedicalCaseTests |
| 494 | public | ` public async Task MedicalCaseFullLifecycle_CreateSavePrescriptionClose_AllSucceed()` | MedicalCaseTests |
| 18 | public | ` public PatientNegativeTests(ITestOutputHelper output)` | PatientNegativeTests |
| 25 | private | ` private static string GenerateValidIdNumber()` | PatientNegativeTests |
| 41 | public | ` public async Task CreatePatient_EmptyName_ShouldFail()` | PatientNegativeTests |
| 60 | public | ` public async Task CreatePatient_InvalidIdNumber_ShouldFail()` | PatientNegativeTests |
| 85 | public | ` public async Task CreatePatient_InvalidPhoneNumber_ShouldFail()` | PatientNegativeTests |
| 110 | public | ` public async Task GetPatient_NonexistentId_ShouldFail()` | PatientNegativeTests |
| 129 | public | ` public async Task DeletePatient_NonexistentId_ShouldFail()` | PatientNegativeTests |
| 16 | public | ` public PatientTests(ITestOutputHelper output)` | PatientTests |
| 24 | private | ` private static string GenerateIdNumber()` | PatientTests |
| 51 | private | ` private static string GeneratePhoneNumber()` | PatientTests |
| 16 | public | ` public RegistrationNegativeTests(ITestOutputHelper output)` | RegistrationNegativeTests |
| 24 | public | ` public async Task CreateRegistration_NonexistentPatient_ShouldFail()` | RegistrationNegativeTests |
| 49 | public | ` public async Task GetRegistration_NonexistentId_ShouldFail()` | RegistrationNegativeTests |
| 68 | public | ` public async Task StartVisit_NonexistentRegistration_ShouldFail()` | RegistrationNegativeTests |
| 87 | public | ` public async Task CancelRegistration_NonexistentId_ShouldFail()` | RegistrationNegativeTests |
| 18 | public | ` public RegistrationTests(ITestOutputHelper output)` | RegistrationTests |
| 26 | private | ` private static string GenerateIdNumber()` | RegistrationTests |
| 45 | private | ` private static string GeneratePhoneNumber()` | RegistrationTests |
| 59 | private | ` private async Task<Guid> CreateTestPatientAsync()` | RegistrationTests |
| 96 | private | ` private async Task<RegistrationDetailDto> CreateTestRegistrationAsync(Guid patientId, string patientName, Guid doctorId, string doctorName)` | RegistrationTests |
| 118 | public | ` public async Task CreateRegistration_ValidInput_ReturnsCreatedRegistration()` | RegistrationTests |
| 152 | public | ` public async Task GetRegistrationById_ExistingRegistration_ReturnsDetail()` | RegistrationTests |
| 175 | public | ` public async Task GetRegistrations_WithPagination_ReturnsPagedResult()` | RegistrationTests |
| 197 | public | ` public async Task GetQueue_WithDoctorFilter_ReturnsWaitingList()` | RegistrationTests |
| 219 | public | ` public async Task GetQueue_WithoutDoctorFilter_ReturnsAllWaitingList()` | RegistrationTests |
| 240 | public | ` public async Task StartVisit_WaitingRegistration_ChangesToInProgress()` | RegistrationTests |
| 266 | public | ` public async Task CancelRegistration_WaitingRegistration_CancelsSuccessfully()` | RegistrationTests |
| 288 | public | ` public async Task GetRegistrations_WithKeyword_FiltersResults()` | RegistrationTests |
| 309 | public | ` public async Task RegistrationFullLifecycle_CreateStartVisitCancel_AllSucceed()` | RegistrationTests |
| 361 | public | ` public async Task RegistrationFullLifecycle_ReceptionistFlow_Succeeds()` | RegistrationTests |
| 20 | public | ` public UserNegativeTests(ITestOutputHelper output)` | UserNegativeTests |
| 25 | private | ` private static UserInputDto CreateValidUserInput(string suffix = "") => new()` | UserNegativeTests |
| 39 | public | ` public async Task CreateUser_DuplicateUsername_ShouldFail()` | UserNegativeTests |
| 64 | public | ` public async Task CreateUser_ShortPassword_ShouldFail()` | UserNegativeTests |
| 85 | public | ` public async Task CreateUser_PasswordMismatch_ShouldFail()` | UserNegativeTests |
| 105 | public | ` public async Task CreateUser_InvalidEmail_ShouldFail()` | UserNegativeTests |
| 125 | public | ` public async Task ChangePassword_WrongOldPassword_ShouldFail()` | UserNegativeTests |
| 149 | public | ` public async Task GetUser_NonexistentId_ShouldFail()` | UserNegativeTests |
| 17 | public | ` public UserTests(ITestOutputHelper output)` | UserTests |
| 25 | private | ` private static string GeneratePhoneNumber()` | UserTests |
| 39 | private | ` private static UserInputDto CreateTestUserInput(string suffix = "") => new()` | UserTests |
| 58 | public | ` public async Task CreateUser_ValidInput_ReturnsCreatedUser()` | UserTests |
| 75 | public | ` public async Task GetUserById_ExistingUser_ReturnsUserDetail()` | UserTests |
| 94 | public | ` public async Task UpdateUser_ValidInput_ReturnsUpdatedUser()` | UserTests |
| 114 | public | ` public async Task GetUsers_WithPagination_ReturnsPagedResult()` | UserTests |
| 137 | public | ` public async Task ToggleStatus_EnabledUser_DisablesUser()` | UserTests |
| 159 | private | ` private async Task DeleteAndRestore_User_CompletesSuccessfully()` | UserTests |
| 190 | public | ` public async Task BatchDelete_MultipleUsers_ReturnsOperationResult()` | UserTests |
| 219 | public | ` public async Task ChangePassword_ExistingUser_Succeeds()` | UserTests |
| 242 | public | ` public async Task ResetPassword_ExistingUser_ReturnsNewPassword()` | UserTests |
| 266 | public | ` public async Task ChangeProfile_ExistingUser_ReturnsUpdatedProfile()` | UserTests |
| 296 | public | ` public async Task GetUsers_WithKeyword_FiltersResults()` | UserTests |
| 321 | private | ` private async Task UserFullLifecycle_CreateUpdateToggleDeleteRestore_AllSucceed()` | UserTests |
| 13 | public | ` public PermissionBoundaryTests(ITestOutputHelper output)` | PermissionBoundaryTests |
| 22 | public | ` public async Task SuperAdmin_CanAccessUserManagement()` | PermissionBoundaryTests |
| 37 | public | ` public async Task SuperAdmin_CanAccessSystemDiagnostics()` | PermissionBoundaryTests |
| 52 | public | ` public async Task SuperAdmin_CanAccessAllMedicalCases()` | PermissionBoundaryTests |
| 66 | public | ` public async Task SuperAdmin_CanAccessHealthDetails()` | PermissionBoundaryTests |
| 81 | public | ` public async Task AnonymousUser_CanAccessBasicHealthCheck()` | PermissionBoundaryTests |
| 98 | public | ` public async Task AnonymousUser_CannotAccessUserManagement()` | PermissionBoundaryTests |
| 115 | public | ` public async Task AnonymousUser_CannotAccessMedicalCases()` | PermissionBoundaryTests |
| 132 | public | ` public async Task AnonymousUser_CannotAccessDiagnostics()` | PermissionBoundaryTests |
| 21 | public | ` public RolePermissionBoundaryTests(E2ECollectionFixture fixture, ITestOutputHelper output)` | RolePermissionBoundaryTests |
| 27 | private | ` private async Task LoginAsRoleAsync(string role)` | RolePermissionBoundaryTests |
| 44 | public | ` public async Task Receptionist_CannotAccessUserManagement()` | RolePermissionBoundaryTests |
| 56 | public | ` public async Task Receptionist_CannotCreateHerb()` | RolePermissionBoundaryTests |
| 73 | public | ` public async Task Receptionist_CannotCreateMedicalCase()` | RolePermissionBoundaryTests |
| 96 | public | ` public async Task Receptionist_CanAccessPatients()` | RolePermissionBoundaryTests |
| 110 | public | ` public async Task Doctor_CannotManageUsers()` | RolePermissionBoundaryTests |
| 122 | public | ` public async Task Doctor_CanCreateHerb()` | RolePermissionBoundaryTests |
| 142 | public | ` public async Task Doctor_CanCreateMedicalCase()` | RolePermissionBoundaryTests |
| 168 | public | ` public async Task Admin_CanManageUsers()` | RolePermissionBoundaryTests |
| 182 | public | ` public async Task Admin_CannotPerformSuperAdminOnlyActions()` | RolePermissionBoundaryTests |
| 198 | public | ` public async Task Admin_CanManageHerbs()` | RolePermissionBoundaryTests |
| 218 | public | ` public async Task Doctor_CannotDeleteUsers()` | RolePermissionBoundaryTests |
| 231 | public | ` public async Task Receptionist_CanCreateRegistration()` | RolePermissionBoundaryTests |
| 271 | private | ` private static string GenerateIdNumber()` | RolePermissionBoundaryTests |
| 17 | public | ` public WorkflowIntegrationTests(ITestOutputHelper output)` | WorkflowIntegrationTests |
| 24 | private | ` private static string GenerateIdNumber()` | WorkflowIntegrationTests |
| 37 | private | ` private static string GeneratePhoneNumber()` | WorkflowIntegrationTests |
| 47 | public | ` public async Task ReceptionistToDoctor_CreateRegistrationAndStartVisit()` | WorkflowIntegrationTests |
| 111 | public | ` public async Task ReceptionistToDoctor_CancelRegistration()` | WorkflowIntegrationTests |
| 166 | public | ` public async Task FullLifecycle_PatientRegistrationToMedicalCase()` | WorkflowIntegrationTests |
| 22 | public | ` public DataIntegrityTests(ITestOutputHelper output)` | DataIntegrityTests |
| 31 | public | ` public async Task DeleteHerbUsedInFormula_ShouldHandleGracefully()` | DataIntegrityTests |
| 85 | public | ` public async Task DeletePatientWithMedicalCases_ShouldHandleGracefully()` | DataIntegrityTests |
| 120 | public | ` public async Task ClosedCase_RejectModification_DataPreserved()` | DataIntegrityTests |
| 195 | public | ` public async Task MultipleRegistrations_SamePatient_IndependentCases()` | DataIntegrityTests |
| 263 | private | ` private static string GenerateIdNumber()` | DataIntegrityTests |
| 21 | public | ` public HerbFormulaWorkflowTests(ITestOutputHelper output)` | HerbFormulaWorkflowTests |
| 30 | public | ` public async Task HerbToFormulaToPrescription_FullChain_DataConsistent()` | HerbFormulaWorkflowTests |
| 152 | public | ` public async Task ModifyFormulaHerbs_ExistingPrescriptionUnaffected()` | HerbFormulaWorkflowTests |
| 237 | public | ` public async Task CreateMultipleFormulas_SameHerbs_DifferentDosages()` | HerbFormulaWorkflowTests |
| 310 | private | ` private static string GenerateIdNumber()` | HerbFormulaWorkflowTests |
| 22 | public | ` public PatientVisitWorkflowTests(ITestOutputHelper output)` | PatientVisitWorkflowTests |
| 29 | public | ` public async Task FullClinicalVisit_PatientToClosedCase_AllStatesVerified()` | PatientVisitWorkflowTests |
| 128 | public | ` public async Task SuspendAndResume_CaseStateTransitions()` | PatientVisitWorkflowTests |
| 162 | public | ` public async Task CancelVisit_RegistrationAndCaseCleanup()` | PatientVisitWorkflowTests |
| 192 | private | ` private async Task<Guid> CreateTestPatientAsync()` | PatientVisitWorkflowTests |
| 208 | private | ` private static string GenerateIdNumber()` | PatientVisitWorkflowTests |
| 221 | private | ` private static string GeneratePhoneNumber()` | PatientVisitWorkflowTests |
| 39 | public | ` public LoginViewModelTests()` | LoginViewModelTests |
| 65 | private | ` private LoginViewModel CreateSut()` | LoginViewModelTests |
| 83 | public | ` public void Constructor_InitializesDefaultState()` | LoginViewModelTests |
| 100 | public | ` public void Username_SetValue_RaisesPropertyChanged()` | LoginViewModelTests |
| 120 | public | ` public void Password_SetValue_RaisesPropertyChanged()` | LoginViewModelTests |
| 140 | public | ` public void RememberUsername_SetValue_RaisesPropertyChanged()` | LoginViewModelTests |
| 160 | public | ` public void RememberPassword_WhenSetTrue_AlsoSetsRememberUsername()` | LoginViewModelTests |
| 179 | public | ` public void LoginCommand_CanExecute_WhenUsernameAndPasswordNotEmpty()` | LoginViewModelTests |
| 203 | public | ` public async Task LoginAsync_ValidCredentials_CallsCoordinator()` | LoginViewModelTests |
| 223 | public | ` public async Task LoginAsync_InvalidCredentials_ShowsError()` | LoginViewModelTests |
| 243 | public | ` public async Task LoginAsync_RememberUsernameTrue_SavesUsername()` | LoginViewModelTests |
| 262 | public | ` public async Task LoginAsync_RememberUsernameFalse_ClearsUsername()` | LoginViewModelTests |
| 281 | public | ` public async Task LoginAsync_RememberPasswordTrue_SavesPassword()` | LoginViewModelTests |
| 303 | public | ` public async Task LoginAsync_RememberPasswordFalse_ClearsPassword()` | LoginViewModelTests |
| 335 | public | ` public void ApiStatus_Default_IsChecking()` | LoginViewModelTests |
| 345 | public | ` public void IsApiUnhealthy_DefaultStatus_ReturnsFalse()` | LoginViewModelTests |
| 355 | public | ` public void IsApiUnhealthy_WhenUnhealthy_ReturnsTrue()` | LoginViewModelTests |
| 368 | public | ` public void IsApiUnhealthy_WhenHealthy_ReturnsFalse()` | LoginViewModelTests |
| 385 | public | ` public async Task Dispose_CancelsBackgroundInitializationTask()` | LoginViewModelTests |
| 397 | public | ` public void Dispose_MultipleCallsAreSafe()` | LoginViewModelTests |
| 407 | public | ` public void Dispose_AfterDispose_CommandsDoNotCrash()` | LoginViewModelTests |
| 428 | public | ` public async Task LoginAsync_SuccessfulLogin_TransitionsToLoggedInState()` | LoginViewModelTests |
| 454 | public | ` public async Task LoginAsync_FailedLogin_ShowsErrorAndClearsPassword()` | LoginViewModelTests |
| 26 | public | ` public CardReaderDataFillTests()` | CardReaderDataFillTests |
| 35 | private | ` private static CardReadResult CreateSuccessCardResult( string name = "李四", string idNumber = "320102199505151234", Gender gender = Gender.Male, DateT…` | CardReaderDataFillTests |
| 54 | private | ` private static PatientDetailDto CreateExistingPatient( Guid? id = null, string name = "李四", string idNumber = "320102199505151234", DateTime? lastVis…` | CardReaderDataFillTests |
| 76 | public | ` public async Task FindPatientByIdNumber_existing_patient_returns_info_with_LastVisitTime()` | CardReaderDataFillTests |
| 98 | public | ` public async Task FindPatientByIdNumber_not_found_returns_null()` | CardReaderDataFillTests |
| 115 | public | ` public async Task FindPatientByIdNumber_empty_idNumber_returns_null(string? idNumber)` | CardReaderDataFillTests |
| 130 | public | ` public async Task FindOrCreatePatient_new_patient_creates_with_IsNewlyCreated_true()` | CardReaderDataFillTests |
| 166 | public | ` public async Task FindOrCreatePatient_existing_patient_returns_with_IsNewlyCreated_false()` | CardReaderDataFillTests |
| 188 | public | ` public async Task QuickCreatePatient_maps_card_fields_correctly()` | CardReaderDataFillTests |
| 218 | public | ` public async Task QuickCreatePatient_throws_when_card_read_failed()` | CardReaderDataFillTests |
| 234 | public | ` public async Task FindOrCreatePatient_throws_when_card_read_failed()` | CardReaderDataFillTests |
| 250 | public | ` public async Task FindPatientByIdNumber_repository_exception_returns_null()` | CardReaderDataFillTests |
| 13 | public | ` public void SectionName_is_CardReader()` | CardReaderOptionsConfigurationTests |
| 19 | public | ` public void Binds_from_configuration_section()` | CardReaderOptionsConfigurationTests |
| 46 | public | ` public void Uses_defaults_when_section_missing()` | CardReaderOptionsConfigurationTests |
| 62 | public | ` public void Partial_config_uses_defaults_for_missing_properties()` | CardReaderOptionsConfigurationTests |
| 21 | public | ` public CardReaderPureTests()` | CardReaderPureTests |
| 33 | private | ` private CardReaderViewModel CreateSut() => new( _cardReaderService, _patientIntegration, _medicalCaseService, _navigationCoordinator, _context, _host…` | CardReaderPureTests |
| 42 | public | ` public void MaskIdNumber_masks_middle_digits(string input, string expected)` | CardReaderPureTests |
| 51 | public | ` public void MaskIdNumber_returns_input_when_short_or_null(string? input)` | CardReaderPureTests |
| 61 | public | ` public void Default_StatusMessage_is_not_connected()` | CardReaderPureTests |
| 70 | public | ` public void IsConnected_delegates_to_service()` | CardReaderPureTests |
| 79 | public | ` public void IsAutoReadEnabled_delegates_to_service()` | CardReaderPureTests |
| 92 | public | ` public void ToggleAutoRead_does_nothing_when_not_connected()` | CardReaderPureTests |
| 104 | public | ` public void ToggleAutoRead_starts_when_connected_and_not_auto()` | CardReaderPureTests |
| 116 | public | ` public void ToggleAutoRead_stops_when_connected_and_auto()` | CardReaderPureTests |
| 132 | public | ` public void Dispose_unsubscribes_and_stops_auto_read_if_enabled()` | CardReaderPureTests |
| 143 | public | ` public void Dispose_does_not_stop_auto_read_if_not_enabled()` | CardReaderPureTests |
| 154 | public | ` public void Dispose_is_idempotent()` | CardReaderPureTests |
| 16 | private | ` private EditModeStateMachine Create(WorkspaceEditState initial = WorkspaceEditState.ReadOnly)` | EditModeStateMachineTests |
| 22 | public | ` public void Constructor_defaults_to_ReadOnly()` | EditModeStateMachineTests |
| 29 | public | ` public void Initialize_sets_state_and_clears_guard()` | EditModeStateMachineTests |
| 37 | public | ` public void IsDirty_true_only_in_DirtyEditing()` | EditModeStateMachineTests |
| 44 | public | ` public void IsDirty_false_in_other_states()` | EditModeStateMachineTests |
| 56 | public | ` public void ReadOnly_EnterEdit_transitions_to_Editing()` | EditModeStateMachineTests |
| 72 | public | ` public void ReadOnly_invalid_events_return_false(WorkspaceEditEvent evt)` | EditModeStateMachineTests |
| 82 | public | ` public void Editing_ExitEdit_transitions_to_ReadOnly()` | EditModeStateMachineTests |
| 90 | public | ` public void Editing_MakeChange_transitions_to_DirtyEditing()` | EditModeStateMachineTests |
| 98 | public | ` public void Editing_RequestLeave_transitions_to_ReadOnly_no_dialog_needed()` | EditModeStateMachineTests |
| 106 | public | ` public void Editing_Save_transitions_to_Saving()` | EditModeStateMachineTests |
| 119 | public | ` public void Editing_invalid_events_return_false(WorkspaceEditEvent evt)` | EditModeStateMachineTests |
| 129 | public | ` public void DirtyEditing_ExitEdit_shows_confirm_LeavingConfirming()` | EditModeStateMachineTests |
| 137 | public | ` public void DirtyEditing_Save_transitions_to_Saving()` | EditModeStateMachineTests |
| 145 | public | ` public void DirtyEditing_RequestLeave_transitions_to_LeavingConfirming()` | EditModeStateMachineTests |
| 158 | public | ` public void DirtyEditing_invalid_events_return_false(WorkspaceEditEvent evt)` | EditModeStateMachineTests |
| 168 | public | ` public void Saving_SaveCompleted_transitions_to_ReadOnly()` | EditModeStateMachineTests |
| 176 | public | ` public void Saving_SaveFailed_transitions_to_TransitionBlocked()` | EditModeStateMachineTests |
| 191 | public | ` public void Saving_invalid_events_return_false(WorkspaceEditEvent evt)` | EditModeStateMachineTests |
| 201 | public | ` public void TransitionBlocked_MakeChange_returns_to_DirtyEditing()` | EditModeStateMachineTests |
| 209 | public | ` public void TransitionBlocked_RequestLeave_shows_confirm()` | EditModeStateMachineTests |
| 224 | public | ` public void TransitionBlocked_invalid_events_return_false(WorkspaceEditEvent evt)` | EditModeStateMachineTests |
| 234 | public | ` public void LeavingConfirming_LeaveConfirmed_transitions_to_ReadOnly()` | EditModeStateMachineTests |
| 242 | public | ` public void LeavingConfirming_LeaveCancelled_returns_to_DirtyEditing()` | EditModeStateMachineTests |
| 250 | public | ` public void LeavingConfirming_Save_transitions_to_Saving()` | EditModeStateMachineTests |
| 264 | public | ` public void LeavingConfirming_invalid_events_return_false(WorkspaceEditEvent evt)` | EditModeStateMachineTests |
| 274 | public | ` public void Guard_blocks_EnterEdit_when_CanEdit_false()` | EditModeStateMachineTests |
| 285 | public | ` public void Guard_allows_EnterEdit_when_CanEdit_true()` | EditModeStateMachineTests |
| 295 | public | ` public void Guard_null_allows_all_valid_transitions()` | EditModeStateMachineTests |
| 306 | public | ` public void StateChanged_raised_on_valid_transition()` | EditModeStateMachineTests |
| 321 | public | ` public void StateChanged_not_raised_on_invalid_transition()` | EditModeStateMachineTests |
| 333 | public | ` public void StateChanged_context_string_propagated()` | EditModeStateMachineTests |
| 347 | public | ` public void GetPermittedEvents_ReadOnly_returns_EnterEdit()` | EditModeStateMachineTests |
| 354 | public | ` public void GetPermittedEvents_Editing_contains_ExitEdit_MakeChange_Save_RequestLeave()` | EditModeStateMachineTests |
| 365 | public | ` public void GetPermittedEvents_Saving_contains_SaveCompleted_SaveFailed()` | EditModeStateMachineTests |
| 376 | public | ` public void Workflow_ReadOnly_edit_dirty_save_complete()` | EditModeStateMachineTests |
| 396 | public | ` public void Workflow_save_fail_then_retry()` | EditModeStateMachineTests |
| 417 | public | ` public void Workflow_dirty_leave_cancel_stay()` | EditModeStateMachineTests |
| 430 | public | ` public void Workflow_dirty_leave_confirmed()` | EditModeStateMachineTests |
| 440 | public | ` public void Workflow_clean_edit_leave_no_dialog()` | EditModeStateMachineTests |
| 449 | public | ` public void Workflow_leave_during_confirm_save_then_leave()` | EditModeStateMachineTests |
| 467 | public | ` public void CanFire_returns_true_for_valid_transition()` | EditModeStateMachineTests |
| 474 | public | ` public void CanFire_returns_false_for_invalid_transition()` | EditModeStateMachineTests |
| 481 | public | ` public void CanFire_respects_guard_predicate()` | EditModeStateMachineTests |
| 22 | public | ` public PatientMatchFallbackChainTests()` | PatientMatchFallbackChainTests |
| 29 | private | ` private static CardReadResult CreateSuccessCardResult( string name = "张三", string idNumber = "110101199001011234", DateTime? birthDate = null)` | PatientMatchFallbackChainTests |
| 46 | private | ` private static PatientDetailDto CreatePatientDto( Guid? id = null, string name = "张三", string? idNumber = "110101199001011234", DateTime? birthDate =…` | PatientMatchFallbackChainTests |
| 65 | public | ` public async Task MatchPatientAsync_returns_ExactMatch_when_IdNumber_found()` | PatientMatchFallbackChainTests |
| 89 | public | ` public async Task MatchPatientAsync_returns_FuzzyMatch_when_single_NameBirthDate_match()` | PatientMatchFallbackChainTests |
| 124 | public | ` public async Task MatchPatientAsync_returns_MultipleCandidates_when_multiple_NameBirthDate_matches()` | PatientMatchFallbackChainTests |
| 160 | public | ` public async Task MatchPatientAsync_returns_NoMatch_when_nothing_found()` | PatientMatchFallbackChainTests |
| 183 | public | ` public async Task MatchPatientAsync_throws_when_card_result_failed()` | PatientMatchFallbackChainTests |
| 192 | public | ` public async Task MatchPatientAsync_throws_when_card_result_null()` | PatientMatchFallbackChainTests |
| 199 | public | ` public async Task MatchPatientAsync_filters_by_BirthDate_from_search_results()` | PatientMatchFallbackChainTests |
| 230 | public | ` public async Task MatchPatientAsync_skips_IdNumber_lookup_when_IdNumber_empty()` | PatientMatchFallbackChainTests |
| 38 | public | ` public PatientSelectionViewModelTests()` | PatientSelectionViewModelTests |
| 63 | private | ` private PatientSelectionViewModel CreateSut() => new( _viewModelServices, _patientService, _medicalCaseQueryService, _medicalCaseService, _registrati…` | PatientSelectionViewModelTests |
| 73 | private | ` private static object? GetProperty(object obj, string name)` | PatientSelectionViewModelTests |
| 77 | public | ` public void Constructor_CreatesCardReaderChildVm_NotNull()` | PatientSelectionViewModelTests |
| 86 | public | ` public void Constructor_CreatesPendingQueueChildVm_NotNull()` | PatientSelectionViewModelTests |
| 95 | public | ` public void CardReader_IsCardReaderViewModel_CorrectType()` | PatientSelectionViewModelTests |
| 104 | public | ` public void PendingQueue_IsPendingQueueViewModel_CorrectType()` | PatientSelectionViewModelTests |
| 113 | public | ` public void PatientSelectionViewModel_ImplementsIWorkspaceHost()` | PatientSelectionViewModelTests |
| 122 | public | ` public void IWorkspaceHost_SetBusy_DoesNotThrow()` | PatientSelectionViewModelTests |
| 133 | public | ` public async Task IWorkspaceHost_ShowErrorAsync_DoesNotThrow()` | PatientSelectionViewModelTests |
| 144 | public | ` public async Task IWorkspaceHost_ShowConfirmAsync_ReturnsBoolean()` | PatientSelectionViewModelTests |
| 158 | public | ` public void PendingQueue_HasNoPendingCases_TrueWhenEmpty()` | PatientSelectionViewModelTests |
| 168 | public | ` public void ExistingPatients_CollectionIsEmpty_OnConstruction()` | PatientSelectionViewModelTests |
| 176 | public | ` public void ExistingHasSelection_IsFalse_WhenNoPatientSelected()` | PatientSelectionViewModelTests |
| 16 | private | ` private static PatientSelectionWorkspaceContext CreateSut() => new();` | PatientSelectionWorkspaceContextTests |
| 19 | public | ` public void MedicalCaseId_Always_ReturnsGuidEmpty()` | PatientSelectionWorkspaceContextTests |
| 32 | public | ` public void CurrentPatient_Always_ReturnsNull()` | PatientSelectionWorkspaceContextTests |
| 45 | public | ` public void State_Always_IsReadOnly()` | PatientSelectionWorkspaceContextTests |
| 60 | public | ` public void SessionManager_Always_ReturnsNull()` | PatientSelectionWorkspaceContextTests |
| 35 | public | ` public PendingQueueViewModelTests()` | PendingQueueViewModelTests |
| 55 | private | ` private PendingQueueViewModel CreateSut() => new( _context, _host, _loggerFactory, _medicalCaseService, _registrationService, _navigationCoordinator)…` | PendingQueueViewModelTests |
| 64 | public | ` public void Queue_IsObservableCollection_BackedByPendingQueueManager()` | PendingQueueViewModelTests |
| 73 | public | ` public void HasNoPendingCases_ReturnsTrue_WhenQueueIsEmpty()` | PendingQueueViewModelTests |
| 81 | public | ` public async Task HasNoPendingCases_ReturnsFalse_WhenQueueHasItems()` | PendingQueueViewModelTests |
| 97 | public | ` public async Task SelectPendingCaseAsync_WithNoActiveMedicalCaseId_SkipsSuspend_NavigatesDirectly()` | PendingQueueViewModelTests |
| 128 | public | ` public async Task RefreshQueueAsync_WithEmptyQueue_DoesNotThrow()` | PendingQueueViewModelTests |
| 13 | public | ` public DesktopExceptionHandlerTests()` | DesktopExceptionHandlerTests |
| 22 | public | ` public void US_ERR_003_CanRetry_TimeoutException_ReturnsTrue()` | DesktopExceptionHandlerTests |
| 28 | public | ` public void US_ERR_003_CanRetry_HttpRequestException_ReturnsTrue()` | DesktopExceptionHandlerTests |
| 34 | public | ` public void US_ERR_003_CanRetry_TaskCanceledException_ReturnsTrue()` | DesktopExceptionHandlerTests |
| 40 | public | ` public void US_ERR_003_CanRetry_SocketException_ReturnsTrue()` | DesktopExceptionHandlerTests |
| 46 | public | ` public void US_ERR_003_CanRetry_ArgumentException_ReturnsFalse()` | DesktopExceptionHandlerTests |
| 52 | public | ` public void US_ERR_003_CanRetry_InvalidOperationException_ReturnsFalse()` | DesktopExceptionHandlerTests |
| 58 | public | ` public void US_ERR_003_HandleException_Generic_ReturnsFailureResult()` | DesktopExceptionHandlerTests |
| 66 | public | ` public void US_ERR_003_HandleException_DoesNotThrow()` | DesktopExceptionHandlerTests |
| 9 | public | ` public void Dispose()` | ErrorTraceCodeTests |
| 17 | public | ` public void US_ERR_007_GetShortTrackingCode_WithProvider_Returns8UppercaseChars()` | ErrorTraceCodeTests |
| 28 | public | ` public void US_ERR_007_GetShortTrackingCode_WithProvider_IsUppercase()` | ErrorTraceCodeTests |
| 38 | public | ` public void US_ERR_007_GetFullTrackingCode_WithProvider_ReturnsFullId()` | ErrorTraceCodeTests |
| 49 | public | ` public void US_ERR_007_GetShortTrackingCode_WithoutProvider_Returns8Chars()` | ErrorTraceCodeTests |
| 59 | public | ` public void US_ERR_007_GetShortTrackingCode_WithoutProvider_IsUppercase()` | ErrorTraceCodeTests |
| 12 | public | ` public void US_ERR_008_ExceptionSeverityMapper_MapsToToastForInfo()` | NotificationTypeMappingTests |
| 17 | public | ` public void US_ERR_008_ExceptionSeverityMapper_MapsToDialogForError()` | NotificationTypeMappingTests |
| 22 | public | ` public void US_ERR_008_ExceptionSeverity_EnumValues_AreCorrect()` | NotificationTypeMappingTests |
| 31 | public | ` public void US_ERR_008_ExceptionSeverity_HasExpectedMemberCount()` | NotificationTypeMappingTests |
| 46 | public | ` public FormulaMasterDetailViewModelTests(UserJourneyFixture fixture) : base(fixture)` | FormulaMasterDetailViewModelTests |
| 121 | private | ` private FormulaMasterDetailViewModel CreateSut()` | FormulaMasterDetailViewModelTests |
| 133 | public | ` public async Task InitializeAsync_LoadsFormulaList()` | FormulaMasterDetailViewModelTests |
| 156 | public | ` public async Task SaveCommand_CreatesNewFormulaAndInvalidatesCache()` | FormulaMasterDetailViewModelTests |
| 212 | public | ` public async Task SaveCommand_UpdatesExistingFormula()` | FormulaMasterDetailViewModelTests |
| 269 | public | ` public void EditCommand_EntersEditModeForSelectedFormula()` | FormulaMasterDetailViewModelTests |
| 280 | public | ` public async Task DeleteCommand_DeletesSelectedFormula()` | FormulaMasterDetailViewModelTests |
| 301 | public | ` public void AddHerbCommand_AddsEditableHerbRow()` | FormulaMasterDetailViewModelTests |
| 313 | public | ` public void DeleteHerbCommand_RemovesEditableHerbRow()` | FormulaMasterDetailViewModelTests |
| 328 | public | ` public async Task SearchCommand_FiltersByKeyword()` | FormulaMasterDetailViewModelTests |
| 341 | public | ` public async Task SearchByCategoryCommand_FiltersByCategory()` | FormulaMasterDetailViewModelTests |
| 21 | public | ` public AuthenticationStateMachineTests()` | AuthenticationStateMachineTests |
| 26 | private | ` private AuthenticationStateMachine CreateStateMachine(AuthState initialState = AuthState.Idle)` | AuthenticationStateMachineTests |
| 35 | public | ` public void Constructor_ShouldStartInIdleState()` | AuthenticationStateMachineTests |
| 51 | public | ` public void Idle_StartLogin_ShouldTransitionToAuthenticating()` | AuthenticationStateMachineTests |
| 66 | public | ` public void Idle_StartAutoLogin_ShouldTransitionToValidatingToken()` | AuthenticationStateMachineTests |
| 81 | public | ` public void Idle_InvalidEvent_ShouldReturnFalse()` | AuthenticationStateMachineTests |
| 99 | public | ` public void Authenticating_CredentialsValidated_ShouldTransitionToLoadingProfile()` | AuthenticationStateMachineTests |
| 114 | public | ` public void Authenticating_LoginFailure_ShouldTransitionToFailed()` | AuthenticationStateMachineTests |
| 128 | public | ` public void Authenticating_Reset_ShouldTransitionToIdle()` | AuthenticationStateMachineTests |
| 146 | public | ` public void ValidatingToken_TokenValidated_ShouldTransitionToLoadingProfile()` | AuthenticationStateMachineTests |
| 160 | public | ` public void ValidatingToken_LoginFailure_ShouldTransitionToIdle()` | AuthenticationStateMachineTests |
| 178 | public | ` public void LoadingProfile_ProfileLoaded_ShouldTransitionToLoadingModules()` | AuthenticationStateMachineTests |
| 196 | public | ` public void LoadingModules_ModulesLoaded_ShouldTransitionToNavigating()` | AuthenticationStateMachineTests |
| 214 | public | ` public void Navigating_NavigationCompleted_ShouldTransitionToAuthenticated()` | AuthenticationStateMachineTests |
| 234 | public | ` public void Authenticated_StartLogout_ShouldTransitionToLoggingOut()` | AuthenticationStateMachineTests |
| 249 | public | ` public void Authenticated_SessionExpire_ShouldTransitionToSessionExpired()` | AuthenticationStateMachineTests |
| 263 | public | ` public void Authenticated_StartTokenRefresh_ShouldTransitionToRefreshingToken()` | AuthenticationStateMachineTests |
| 282 | public | ` public void Failed_StartLogin_ShouldTransitionToAuthenticating()` | AuthenticationStateMachineTests |
| 296 | public | ` public void Failed_Reset_ShouldTransitionToIdle()` | AuthenticationStateMachineTests |
| 313 | public | ` public void LoggingOut_LogoutSuccess_ShouldTransitionToIdle()` | AuthenticationStateMachineTests |
| 327 | public | ` public void LoggingOut_LogoutFailure_ShouldTransitionToIdle()` | AuthenticationStateMachineTests |
| 345 | public | ` public void RefreshingToken_TokenRefreshSuccess_ShouldTransitionToAuthenticated()` | AuthenticationStateMachineTests |
| 359 | public | ` public void RefreshingToken_TokenRefreshFailure_ShouldTransitionToSessionExpired()` | AuthenticationStateMachineTests |
| 377 | public | ` public void SessionExpired_StartLogin_ShouldTransitionToAuthenticating()` | AuthenticationStateMachineTests |
| 395 | public | ` public void CanFire_ValidTransition_ShouldReturnTrue()` | AuthenticationStateMachineTests |
| 406 | public | ` public void CanFire_InvalidTransition_ShouldReturnFalse()` | AuthenticationStateMachineTests |
| 425 | public | ` public async Task Fire_ShouldPublishAuthStateChangedEvent()` | AuthenticationStateMachineTests |
| 456 | public | ` public void Fire_WithoutEventAggregator_ShouldNotThrow()` | AuthenticationStateMachineTests |
| 474 | public | ` public void CompleteLoginFlow_ShouldTransitionCorrectly()` | AuthenticationStateMachineTests |
| 505 | public | ` public void TokenRefreshFlow_ShouldTransitionCorrectly()` | AuthenticationStateMachineTests |
| 519 | public | ` public void TokenRefreshFailureFlow_ShouldTransitionToSessionExpired()` | AuthenticationStateMachineTests |
| 537 | public | ` public void AutoLoginFlow_Success_ShouldTransitionToAuthenticated()` | AuthenticationStateMachineTests |
| 560 | public | ` public void AutoLoginFlow_Failure_ShouldTransitionToIdle()` | AuthenticationStateMachineTests |
| 578 | public | ` public void GetPermittedEvents_Idle_ShouldReturnCorrectEvents()` | AuthenticationStateMachineTests |
| 593 | public | ` public void GetPermittedEvents_Authenticated_ShouldReturnCorrectEvents()` | AuthenticationStateMachineTests |
| 26 | public | ` public async Task LoginAsync_ShouldPublish_LoginStartedEvent()` | AuthEventPublishingTests |
| 54 | public | ` public async Task LoginWithAutoTokenAsync_ShouldPublish_LoginStartedEvent_WithAutoLoginFlag()` | AuthEventPublishingTests |
| 86 | public | ` public async Task LoginAsync_WhenApiFails_ShouldStillPublish_LoginStartedEvent()` | AuthEventPublishingTests |
| 111 | public | ` public async Task LogoutAsync_ShouldPublish_LogoutStartedEvent()` | AuthEventPublishingTests |
| 141 | private | ` private AuthenticationService CreateAuthenticationService(IApiClientAuth? authApi = null)` | AuthEventPublishingTests |
| 152 | private | ` private LogoutService CreateLogoutService(ITokenStorageService? tokenStorage = null)` | AuthEventPublishingTests |
| 22 | public | ` public ConnectionSettingsServiceTests()` | ConnectionSettingsServiceTests |
| 28 | public | ` public void Dispose()` | ConnectionSettingsServiceTests |
| 33 | private | ` private static IOptions<ApiClientOptions> CreateApiOptions(string? baseUrl = null, string? remoteUrl = null, string? preferredMode = null)` | ConnectionSettingsServiceTests |
| 47 | public | ` public void CurrentUrl_WithSavedUrl_ShouldReturnSavedValue()` | ConnectionSettingsServiceTests |
| 56 | public | ` public void CurrentUrl_WithNullConfig_ShouldDefaultToLocalhost()` | ConnectionSettingsServiceTests |
| 65 | public | ` public void CurrentUrl_WithEmptyConfig_ShouldDefaultToLocalhost()` | ConnectionSettingsServiceTests |
| 84 | public | ` public void IsLocal_ShouldDetectLocalhostCorrectly(string url, bool expected)` | ConnectionSettingsServiceTests |
| 105 | public | ` public void IsValidUrl_ShouldValidateCorrectly(string url, bool expected)` | ConnectionSettingsServiceTests |
| 118 | public | ` public async Task SetUrlAsync_WithValidUrl_ShouldUpdateCurrentUrl()` | ConnectionSettingsServiceTests |
| 130 | public | ` public async Task SetUrlAsync_WithSameUrl_ShouldNotFireEvent()` | ConnectionSettingsServiceTests |
| 143 | public | ` public async Task SetUrlAsync_WithDifferentUrl_ShouldFireEvent()` | ConnectionSettingsServiceTests |
| 156 | public | ` public async Task SetUrlAsync_WithInvalidUrl_ShouldThrow()` | ConnectionSettingsServiceTests |
| 166 | public | ` public async Task SetUrlAsync_WithEmptyUrl_ShouldThrow()` | ConnectionSettingsServiceTests |
| 180 | public | ` public async Task SetUrlAsync_ShouldPersistToFile()` | ConnectionSettingsServiceTests |
| 19 | public | ` public CredentialVaultTests()` | CredentialVaultTests |
| 29 | public | ` public void Dispose()` | CredentialVaultTests |
| 41 | public | ` public async Task SaveAutoLoginTokenAsync_WithValidData_ShouldReturnTrue()` | CredentialVaultTests |
| 55 | public | ` public async Task SaveAutoLoginTokenAsync_WithEmptyUsername_ShouldThrowArgumentException()` | CredentialVaultTests |
| 69 | public | ` public async Task SaveAutoLoginTokenAsync_WithEmptyToken_ShouldThrowArgumentException()` | CredentialVaultTests |
| 83 | public | ` public async Task SaveAutoLoginTokenAsync_WithNullUsername_ShouldThrowArgumentException()` | CredentialVaultTests |
| 96 | public | ` public async Task SaveAutoLoginTokenAsync_UpdateExistingUser_ShouldOverwrite()` | CredentialVaultTests |
| 117 | public | ` public async Task GetAutoLoginTokenAsync_AfterSave_ShouldReturnSameToken()` | CredentialVaultTests |
| 132 | public | ` public async Task GetAutoLoginTokenAsync_WithNonExistentUser_ShouldReturnNull()` | CredentialVaultTests |
| 145 | public | ` public async Task GetAutoLoginTokenAsync_WithEmptyUsername_ShouldReturnNull()` | CredentialVaultTests |
| 155 | public | ` public async Task GetAutoLoginTokenAsync_WithNullUsername_ShouldReturnNull()` | CredentialVaultTests |
| 165 | public | ` public async Task GetAutoLoginTokenAsync_CaseInsensitiveUsername_ShouldMatch()` | CredentialVaultTests |
| 184 | public | ` public async Task ClearCredentialsAsync_WithSpecificUser_ShouldRemoveOnlyThatUser()` | CredentialVaultTests |
| 200 | public | ` public async Task ClearCredentialsAsync_WithNullUsername_ShouldClearAll()` | CredentialVaultTests |
| 216 | public | ` public async Task ClearCredentialsAsync_WhenNoData_ShouldReturnTrue()` | CredentialVaultTests |
| 230 | public | ` public async Task VerifyIntegrityAsync_AfterSave_ShouldReturnTrue()` | CredentialVaultTests |
| 244 | public | ` public async Task VerifyIntegrityAsync_WithNonExistentUser_ShouldReturnFalse()` | CredentialVaultTests |
| 254 | public | ` public async Task VerifyIntegrityAsync_WithEmptyUsername_ShouldReturnFalse()` | CredentialVaultTests |
| 268 | public | ` public async Task HasValidTokenAsync_AfterSave_ShouldReturnTrue()` | CredentialVaultTests |
| 282 | public | ` public async Task HasValidTokenAsync_WithNonExistentUser_ShouldReturnFalse()` | CredentialVaultTests |
| 292 | public | ` public async Task HasValidTokenAsync_AfterClear_ShouldReturnFalse()` | CredentialVaultTests |
| 311 | public | ` public async Task MigrateOldFormatAsync_WhenNoOldFile_ShouldNotThrow()` | CredentialVaultTests |
| 325 | public | ` public async Task MultipleUsers_ShouldStoreIndependently()` | CredentialVaultTests |
| 354 | public | ` public async Task Token_ShouldPersistAcrossNewInstance()` | CredentialVaultTests |
| 24 | public | ` public FakeHandler(string json) => _json = json;` | FakeHandler |
| 25 | protected | ` protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)` | FakeHandler |
| 27 | public | ` public LocalTokenValidatorTests()` | LocalTokenValidatorTests |
| 47 | public | ` public async Task ValidateToken_ValidToken_ReturnsSuccess()` | LocalTokenValidatorTests |
| 70 | public | ` public async Task ValidateToken_ExpiredToken_ReturnsFailed()` | LocalTokenValidatorTests |
| 88 | public | ` public async Task ValidateToken_InvalidSignature_ReturnsFailed()` | LocalTokenValidatorTests |
| 106 | public | ` public async Task ValidateToken_MissingClaims_ReturnsFailed()` | LocalTokenValidatorTests |
| 124 | public | ` public async Task ValidateToken_ClockSkew_StillValid()` | LocalTokenValidatorTests |
| 142 | public | ` public async Task ValidateToken_EmptyToken_ReturnsFailed()` | LocalTokenValidatorTests |
| 160 | public | ` public async Task ValidateAndGetUserInfoAsync_ValidToken_ReturnsUserInfo()` | LocalTokenValidatorTests |
| 178 | public | ` public async Task ValidateAndGetUserInfoAsync_InvalidToken_ReturnsNull()` | LocalTokenValidatorTests |
| 195 | private | ` private string GenerateValidToken()` | LocalTokenValidatorTests |
| 225 | private | ` private string GenerateExpiredToken()` | LocalTokenValidatorTests |
| 257 | private | ` private string GenerateTokenWithInvalidSignature()` | LocalTokenValidatorTests |
| 287 | private | ` private string GenerateTokenWithMissingClaims()` | LocalTokenValidatorTests |
| 316 | private | ` private string GenerateTokenExpiringWithinClockSkew()` | LocalTokenValidatorTests |
| 34 | public | ` public LogoutServiceTests()` | LogoutServiceTests |
| 47 | public | ` public void Dispose()` | LogoutServiceTests |
| 56 | public | ` public void Constructor_WithNullLogger_ShouldThrow()` | LogoutServiceTests |
| 64 | public | ` public void Constructor_WithNullTokenStorage_ShouldThrow()` | LogoutServiceTests |
| 72 | public | ` public void Constructor_WithNullAuthApi_ShouldThrow()` | LogoutServiceTests |
| 80 | public | ` public void Constructor_WithNullStateMachine_ShouldThrow()` | LogoutServiceTests |
| 92 | public | ` public async Task LogoutAsync_Success_ShouldReturnFullSuccess()` | LogoutServiceTests |
| 108 | public | ` public async Task LogoutAsync_ShouldTriggerStateMachineStartLogout()` | LogoutServiceTests |
| 121 | public | ` public async Task LogoutAsync_ShouldTriggerStateMachineLogoutSuccess()` | LogoutServiceTests |
| 134 | public | ` public async Task LogoutAsync_ShouldClearLocalAuthentication()` | LogoutServiceTests |
| 147 | public | ` public async Task LogoutAsync_WithNoUser_ShouldStillSucceed()` | LogoutServiceTests |
| 162 | public | ` public async Task LogoutAsync_ServerFailure_ShouldQueueForRetry()` | LogoutServiceTests |
| 182 | public | ` public async Task LogoutAsync_ServerFailure_ShouldPublishServerLogoutFailedEvent()` | LogoutServiceTests |
| 208 | public | ` public async Task LogoutAsync_TokenInvalid_ShouldNotQueueForRetry()` | LogoutServiceTests |
| 231 | public | ` public async Task ExecuteLocalLogoutAsync_ShouldClearAuthentication()` | LogoutServiceTests |
| 241 | public | ` public async Task ExecuteLocalLogoutAsync_WithException_ShouldNotThrow()` | LogoutServiceTests |
| 257 | public | ` public async Task ProcessPendingServerLogoutsAsync_WithEmptyQueue_ShouldReturnZero()` | LogoutServiceTests |
| 267 | public | ` public async Task ProcessPendingServerLogoutsAsync_WithPendingItem_ShouldProcess()` | LogoutServiceTests |
| 290 | public | ` public async Task ProcessPendingServerLogoutsAsync_AllCleared_ShouldPublishPendingLogoutsClearedEvent()` | LogoutServiceTests |
| 323 | public | ` public void PendingServerLogoutCount_Initial_ShouldBeZero()` | LogoutServiceTests |
| 330 | public | ` public async Task PendingServerLogoutCount_AfterNetworkFailure_ShouldBeOne()` | LogoutServiceTests |
| 350 | public | ` public async Task LogoutAsync_Timeout_ShouldQueueForRetry()` | LogoutServiceTests |
| 372 | private | ` private void SetupSuccessfulLogout()` | LogoutServiceTests |
| 380 | private | ` private static LoginResponse CreateLoginResponse()` | LogoutServiceTests |
| 24 | public | ` public SwitchingApiClientTests()` | SwitchingApiClientTests |
| 31 | private | ` private SwitchingApiClient CreateClient(IConnectionSettingsService cs)` | SwitchingApiClientTests |
| 62 | public | ` public void LocalUrl_ShouldUseHttpClientApiClient()` | SwitchingApiClientTests |
| 77 | public | ` public void RemoteUrl_ShouldUseRefitApiClient()` | SwitchingApiClientTests |
| 91 | public | ` public void LocalhostUrl_ShouldTriggerLocalClient()` | SwitchingApiClientTests |
| 109 | public | ` public void UrlChange_ShouldRecreateClient()` | SwitchingApiClientTests |
| 128 | public | ` public void SameUrl_ShouldNotRecreateClient()` | SwitchingApiClientTests |
| 148 | public | ` public void AllProperties_ShouldDelegateCorrectly()` | SwitchingApiClientTests |
| 182 | public | ` public FakeHttpMessageHandler(string label) => _label = label;` | FakeHttpMessageHandler |
| 184 | protected | ` protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken ct)` | FakeHttpMessageHandler |
| 33 | public | ` public TestableHerbMasterDetailViewModel( IViewModelServices viewModelServices, IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServi…` | TestableHerbMasterDetailViewModel |
| 44 | public | ` public Task<bool> SaveDetailPublicAsync(HerbDetailModel detail) => base.SaveDetailAsync(detail);` | TestableHerbMasterDetailViewModel |
| 46 | public | ` public Task<bool> DeleteItemPublicAsync(HerbListDto item) => base.DeleteItemAsync(item);` | TestableHerbMasterDetailViewModel |
| 15 | public | ` public BreadcrumbBarTests(UserJourneyFixture fixture) : base(fixture)` | BreadcrumbBarTests |
| 21 | private | ` private BreadcrumbBar CreateSut() => new();` | BreadcrumbBarTests |
| 24 | public | ` public void Constructor_InitializesWithDefaults()` | BreadcrumbBarTests |
| 33 | public | ` public void Constructor_InitializesBreadcrumbsCollection()` | BreadcrumbBarTests |
| 41 | public | ` public void NavigationPath_WhenEmpty_DoesNotCreateBreadcrumbs()` | BreadcrumbBarTests |
| 50 | public | ` public void NavigationPath_WhenWhitespace_DoesNotCreateBreadcrumbs()` | BreadcrumbBarTests |
| 59 | public | ` public void NavigationPath_SingleItem_CreatesOneBreadcrumb()` | BreadcrumbBarTests |
| 72 | public | ` public void NavigationPath_TwoItems_SeparatedByGreaterThan()` | BreadcrumbBarTests |
| 91 | public | ` public void NavigationPath_ThreeItems_CreatesThreeBreadcrumbs()` | BreadcrumbBarTests |
| 112 | public | ` public void NavigationPath_TrimsWhitespaceFromParts()` | BreadcrumbBarTests |
| 124 | public | ` public void NavigationPath_HandlesMultipleSeparators()` | BreadcrumbBarTests |
| 133 | public | ` public void NavigateCommand_CanBeSet()` | BreadcrumbBarTests |
| 144 | public | ` public void NavigationPath_WhenChanged_UpdatesBreadcrumbs()` | BreadcrumbBarTests |
| 157 | public | ` public void BreadcrumbItems_HaveCorrectLevels()` | BreadcrumbBarTests |
| 169 | public | ` public void BreadcrumbItems_NavigateCommand_IsSetFromParent()` | BreadcrumbBarTests |
| 183 | public | ` public void BreadcrumbItem_Model_HasAllProperties()` | BreadcrumbBarTests |
| 202 | public | ` public void NavigationPath_EmptyString_ClearsBreadcrumbs()` | BreadcrumbBarTests |
| 17 | public | ` public ToastServiceTests(UserJourneyFixture fixture) : base(fixture)` | ToastServiceTests |
| 23 | private | ` private ToastService CreateSut() => new();` | ToastServiceTests |
| 26 | public | ` public void Constructor_InitializesService()` | ToastServiceTests |
| 34 | public | ` public void ShowInfo_CreatesInfoToast()` | ToastServiceTests |
| 48 | public | ` public void ShowSuccess_CreatesSuccessToast()` | ToastServiceTests |
| 58 | public | ` public void ShowWarning_CreatesWarningToast()` | ToastServiceTests |
| 68 | public | ` public void ShowError_CreatesErrorToast()` | ToastServiceTests |
| 78 | public | ` public void Show_WithCustomDuration_CreatesToast()` | ToastServiceTests |
| 88 | public | ` public void ShowInfo_WithNullMessage_DoesNotThrow()` | ToastServiceTests |
| 99 | public | ` public void ShowSuccess_WithEmptyMessage_DoesNotThrow()` | ToastServiceTests |
| 109 | public | ` public void ShowWarning_WithWhitespace_DoesNotThrow()` | ToastServiceTests |
| 119 | public | ` public void ShowError_WithLongMessage_DoesNotThrow()` | ToastServiceTests |
| 130 | public | ` public void Show_WithAllToastTypes_DoesNotThrow()` | ToastServiceTests |
| 146 | public | ` public void Show_WithDifferentDurations_DoesNotThrow()` | ToastServiceTests |
| 162 | public | ` public void MultipleShowCalls_DoesNotThrow()` | ToastServiceTests |
| 16 | public | ` public void Dispose() => _sut.Dispose();` | LoggingLevelManagerTests |
| 19 | public | ` public void US_LOG_004_DefaultLevel_IsInformation()` | LoggingLevelManagerTests |
| 26 | public | ` public void US_LOG_004_LevelSwitch_InitiallyAtDefaultLevel()` | LoggingLevelManagerTests |
| 33 | public | ` public void US_LOG_004_IsDebugModeActive_DefaultFalse()` | LoggingLevelManagerTests |
| 40 | public | ` public void US_LOG_004_EnableDebugMode_SetsIsDebugModeActiveTrue()` | LoggingLevelManagerTests |
| 50 | public | ` public void US_LOG_004_EnableDebugMode_LowersMinimumLevel()` | LoggingLevelManagerTests |
| 60 | public | ` public void US_LOG_004_EnableDebugMode_ReturnsDebugModeInfoWithIsActiveTrue()` | LoggingLevelManagerTests |
| 71 | public | ` public void US_LOG_004_DisableDebugMode_RestoresDefaultLevel()` | LoggingLevelManagerTests |
| 85 | public | ` public void US_LOG_004_DisableDebugMode_ReturnsDebugModeInfoWithIsActiveFalse()` | LoggingLevelManagerTests |
| 99 | public | ` public void US_LOG_004_GetStatus_ReturnsNonNull()` | LoggingLevelManagerTests |
| 109 | public | ` public void US_LOG_004_GetStatus_WhenNotActive_ReturnsIsActiveFalse()` | LoggingLevelManagerTests |
| 119 | public | ` public void US_LOG_004_GetStatus_WhenActive_ReturnsIsActiveTrue()` | LoggingLevelManagerTests |
| 132 | public | ` public void US_LOG_004_SetLevel_ChangesMinimumLevel()` | LoggingLevelManagerTests |
| 142 | public | ` public void US_LOG_004_SetLevel_ToVerbose_IsDebugModeActive()` | LoggingLevelManagerTests |
| 15 | public | ` public void US_LOG_003_Mask_FullMode_ReturnsHiddenText()` | SensitiveDataMaskerTests |
| 25 | public | ` public void US_LOG_003_Mask_FullMode_EmptyInput_ReturnsEmpty()` | SensitiveDataMaskerTests |
| 35 | public | ` public void US_LOG_003_Mask_HashMode_StartsWithRedactedPrefix()` | SensitiveDataMaskerTests |
| 46 | public | ` public void US_LOG_003_Mask_HashMode_Contains8HexCharacters()` | SensitiveDataMaskerTests |
| 58 | public | ` public void US_LOG_003_Mask_HashMode_SameInputProducesSameHash()` | SensitiveDataMaskerTests |
| 69 | public | ` public void US_LOG_003_IsSensitiveFieldName_Password_ReturnsTrue()` | SensitiveDataMaskerTests |
| 76 | public | ` public void US_LOG_003_IsSensitiveFieldName_Token_ReturnsTrue()` | SensitiveDataMaskerTests |
| 83 | public | ` public void US_LOG_003_IsSensitiveFieldName_AccessToken_ReturnsTrue()` | SensitiveDataMaskerTests |
| 90 | public | ` public void US_LOG_003_IsSensitiveFieldName_UserName_ReturnsFalse()` | SensitiveDataMaskerTests |
| 97 | public | ` public void US_LOG_003_IsSensitiveFieldName_NullOrEmpty_ReturnsFalse()` | SensitiveDataMaskerTests |
| 105 | public | ` public void US_LOG_003_SanitizeText_MasksBearerToken()` | SensitiveDataMaskerTests |
| 119 | public | ` public void US_LOG_003_SanitizeText_MasksPasswordField()` | SensitiveDataMaskerTests |
| 133 | public | ` public void US_LOG_003_MaskUri_MasksPasswordQueryParameter()` | SensitiveDataMaskerTests |
| 148 | public | ` public void US_LOG_003_MaskUri_MasksTokenQueryParameter()` | SensitiveDataMaskerTests |
| 162 | public | ` public void US_LOG_003_MaskUri_NullInput_ReturnsEmpty()` | SensitiveDataMaskerTests |
| 16 | public | ` public ConsultationEditorPureTests()` | ConsultationEditorPureTests |
| 25 | private | ` private ConsultationEditorViewModel CreateSut() => new(_context, _host, _loggerFactory);` | ConsultationEditorPureTests |
| 28 | public | ` public void InitializeForNewCase_sets_patient_fields_and_resets_diagnosis()` | ConsultationEditorPureTests |
| 45 | public | ` public void InitializeFromDto_maps_all_fields()` | ConsultationEditorPureTests |
| 69 | public | ` public void Validate_fails_when_TcmDiagnosis_empty()` | ConsultationEditorPureTests |
| 79 | public | ` public void Validate_succeeds_when_TcmDiagnosis_filled()` | ConsultationEditorPureTests |
| 89 | public | ` public void Reset_clears_diagnosis_fields()` | ConsultationEditorPureTests |
| 106 | public | ` public void GetConsultationData_returns_dto_from_mapper()` | ConsultationEditorPureTests |
| 22 | public | ` public ConsultationEditorViewModelTests(UserJourneyFixture fixture) : base(fixture)` | ConsultationEditorViewModelTests |
| 32 | private | ` private ConsultationEditorViewModel CreateSut() => new(_context, _host, _loggerFactory);` | ConsultationEditorViewModelTests |
| 35 | public | ` public void Constructor_InitializesEmptyConsultation()` | ConsultationEditorViewModelTests |
| 48 | public | ` public void InitializeFromDto_MapsAllFields()` | ConsultationEditorViewModelTests |
| 76 | public | ` public void InitializeForNewCase_SetsPatientInfo_AndMedicalCaseId()` | ConsultationEditorViewModelTests |
| 93 | public | ` public void Reset_ClearsAllFields()` | ConsultationEditorViewModelTests |
| 111 | public | ` public void Validate_ReturnsFalse_WhenTcmDiagnosisIsEmpty()` | ConsultationEditorViewModelTests |
| 121 | public | ` public void Validate_ReturnsTrue_WhenTcmDiagnosisIsProvided()` | ConsultationEditorViewModelTests |
| 132 | public | ` public void GetConsultationData_MapsAllFields()` | ConsultationEditorViewModelTests |
| 151 | public | ` public void Consultation_SetProperty_RaisesPropertyChanged()` | ConsultationEditorViewModelTests |
| 15 | public | ` public ConsultationItemTests(UserJourneyFixture fixture) : base(fixture)` | ConsultationItemTests |
| 19 | private | ` private ConsultationItem CreateSut() => new();` | ConsultationItemTests |
| 22 | public | ` public void Constructor_InitializesWithDefaults()` | ConsultationItemTests |
| 39 | public | ` public void IsDiagnosisComplete_ReturnsFalse_WhenTcmDiagnosisIsNull()` | ConsultationItemTests |
| 48 | public | ` public void IsDiagnosisComplete_ReturnsFalse_WhenTcmDiagnosisIsEmpty()` | ConsultationItemTests |
| 57 | public | ` public void IsDiagnosisComplete_ReturnsFalse_WhenTcmDiagnosisIsWhitespace()` | ConsultationItemTests |
| 66 | public | ` public void IsDiagnosisComplete_ReturnsTrue_WhenTcmDiagnosisHasValue()` | ConsultationItemTests |
| 75 | public | ` public void TcmDiagnosis_SetProperty_RaisesIsDiagnosisCompleteChanged()` | ConsultationItemTests |
| 88 | public | ` public void IsPresentIllnessValid_ReturnsFalse_WhenPresentIllnessIsNull()` | ConsultationItemTests |
| 97 | public | ` public void IsPresentIllnessValid_ReturnsFalse_WhenPresentIllnessIsEmpty()` | ConsultationItemTests |
| 106 | public | ` public void IsPresentIllnessValid_ReturnsFalse_WhenPresentIllnessIsWhitespace()` | ConsultationItemTests |
| 115 | public | ` public void IsPresentIllnessValid_ReturnsFalse_WhenPresentIllnessIsLessThan5Characters()` | ConsultationItemTests |
| 124 | public | ` public void IsPresentIllnessValid_ReturnsTrue_WhenPresentIllnessIsExactly5Characters()` | ConsultationItemTests |
| 133 | public | ` public void IsPresentIllnessValid_ReturnsTrue_WhenPresentIllnessIsMoreThan5Characters()` | ConsultationItemTests |
| 142 | public | ` public void PresentIllness_SetProperty_RaisesPropertyChanged()` | ConsultationItemTests |
| 154 | public | ` public void DisplayText_ReturnsPatientNameAndDiagnosis()` | ConsultationItemTests |
| 164 | public | ` public void DisplayText_ReturnsPatientNameAndUndiagnosed_WhenDiagnosisIsEmpty()` | ConsultationItemTests |
| 174 | public | ` public void Validate_ReturnsFalse_WhenTcmDiagnosisIsEmpty()` | ConsultationItemTests |
| 186 | public | ` public void Validate_ReturnsTrue_WhenTcmDiagnosisHasValue()` | ConsultationItemTests |
| 198 | public | ` public void Validate_SetsValidationMessage_AndRaisesErrorsChanged()` | ConsultationItemTests |
| 212 | public | ` public void Reset_ClearsAllFields_ExceptIds()` | ConsultationItemTests |
| 237 | public | ` public void GetConsultationData_ReturnsValidDto()` | ConsultationItemTests |
| 260 | public | ` public void GetPrescriptionData_ReturnsNull()` | ConsultationItemTests |
| 50 | public | ` public MedicalCaseMasterDetailViewModelTests()` | MedicalCaseMasterDetailViewModelTests |
| 108 | private | ` private MedicalCaseMasterDetailViewModel CreateSut()` | MedicalCaseMasterDetailViewModelTests |
| 123 | public | ` public void Constructor_InitializesPageTitle()` | MedicalCaseMasterDetailViewModelTests |
| 133 | public | ` public void Constructor_ThrowsArgumentNullException_WhenMedicalCaseServiceIsNull()` | MedicalCaseMasterDetailViewModelTests |
| 149 | public | ` public void Constructor_ThrowsArgumentNullException_WhenHerbSearchProviderIsNull()` | MedicalCaseMasterDetailViewModelTests |
| 165 | public | ` public void Constructor_ThrowsArgumentNullException_WhenCacheManagerIsNull()` | MedicalCaseMasterDetailViewModelTests |
| 181 | public | ` public void EntityDisplayName_ReturnsCorrectValue()` | MedicalCaseMasterDetailViewModelTests |
| 195 | public | ` public async Task LoadListAsync_LoadsPagedDataAndPopulatesItems()` | MedicalCaseMasterDetailViewModelTests |
| 225 | public | ` public async Task LoadListAsync_HandlesExceptionAndLogsError()` | MedicalCaseMasterDetailViewModelTests |
| 242 | public | ` public async Task LoadListAsync_PassesSearchTextToRepository()` | MedicalCaseMasterDetailViewModelTests |
| 270 | public | ` public async Task LoadDetailAsync_LoadsDetailViaServiceAndInitializesChildVMs()` | MedicalCaseMasterDetailViewModelTests |
| 292 | public | ` public async Task LoadDetailAsync_HandlesNullResultFromService()` | MedicalCaseMasterDetailViewModelTests |
| 309 | public | ` public async Task LoadDetailAsync_HandlesExceptionAndLogsError()` | MedicalCaseMasterDetailViewModelTests |
| 331 | public | ` public async Task SaveDetailAsync_BuildsAggregateDtoAndCallsSave()` | MedicalCaseMasterDetailViewModelTests |
| 363 | public | ` public async Task SaveDetailAsync_ReturnsFalse_WhenSaveFails()` | MedicalCaseMasterDetailViewModelTests |
| 385 | public | ` public async Task SaveDetailAsync_IncludesPrescriptionItems_WhenPresent()` | MedicalCaseMasterDetailViewModelTests |
| 427 | public | ` public async Task DeleteItemAsync_CallsRepositoryDeleteAndInvalidatesCache()` | MedicalCaseMasterDetailViewModelTests |
| 445 | public | ` public async Task DeleteItemAsync_ReturnsFalse_WhenDeleteFails()` | MedicalCaseMasterDetailViewModelTests |
| 467 | public | ` public async Task LoadHerbsAsync_LoadsHerbsViaProviderAndPopulatesAllHerbs()` | MedicalCaseMasterDetailViewModelTests |
| 491 | public | ` public async Task LoadHerbsAsync_HandlesEmptyResult()` | MedicalCaseMasterDetailViewModelTests |
| 511 | public | ` public async Task LoadHerbsAsync_HandlesExceptionAndLogsError()` | MedicalCaseMasterDetailViewModelTests |
| 532 | public | ` public async Task OnNavigatedTo_CallsLoadHerbsAsync_WhenAllHerbsIsEmpty()` | MedicalCaseMasterDetailViewModelTests |
| 556 | public | ` public void OnNavigatedTo_SkipsLoadingHerbs_WhenAllHerbsAlreadyLoaded()` | MedicalCaseMasterDetailViewModelTests |
| 584 | public | ` public void CreateNewDetail_ThrowsNotSupportedException()` | MedicalCaseMasterDetailViewModelTests |
| 603 | public | ` public void SelectedPatientName_ReturnsEmpty_WhenNoSelection()` | MedicalCaseMasterDetailViewModelTests |
| 614 | public | ` public void SelectedPatientName_ReturnsPatientName_WhenItemSelected()` | MedicalCaseMasterDetailViewModelTests |
| 629 | private | ` private static MedicalCaseListDto CreateMedicalCaseListDto(Guid? id = null, string patientName = "测试患者")` | MedicalCaseMasterDetailViewModelTests |
| 642 | private | ` private static MedicalCaseDetailDto CreateMedicalCaseDetailDto()` | MedicalCaseMasterDetailViewModelTests |
| 655 | private | ` private static MedicalCaseDetailModel CreateMedicalCaseDetailModel()` | MedicalCaseMasterDetailViewModelTests |
| 678 | public | ` public static void TestCreateNewDetail(this MedicalCaseMasterDetailViewModel vm)` | MedicalCaseMasterDetailViewModelTestExtensions |
| 714 | public | ` public static async Task<bool> SaveDetailAsync(this MedicalCaseMasterDetailViewModel vm, MedicalCaseDetailModel detail)` | MedicalCaseMasterDetailViewModelTestExtensions |
| 730 | public | ` public static async Task<bool> DeleteItemAsync(this MedicalCaseMasterDetailViewModel vm, MedicalCaseListDto item)` | MedicalCaseMasterDetailViewModelTestExtensions |
| 746 | public | ` public static async Task InvokeLoadDetailAsync(this MedicalCaseMasterDetailViewModel vm, MedicalCaseListDto item)` | MedicalCaseMasterDetailViewModelTestExtensions |
| 781 | public | ` public static async Task InvokeLoadHerbsAsync(this MedicalCaseMasterDetailViewModel vm)` | MedicalCaseMasterDetailViewModelTestExtensions |
| 796 | public | ` public static void InvokeOnNavigatedTo(this MedicalCaseMasterDetailViewModel vm, NavigationContext navigationContext)` | MedicalCaseMasterDetailViewModelTestExtensions |
| 45 | public | ` public MedicalCaseWorkspaceViewModelTests()` | MedicalCaseWorkspaceViewModelTests |
| 91 | private | ` private MedicalCaseWorkspaceViewModel CreateSut()` | MedicalCaseWorkspaceViewModelTests |
| 106 | public | ` public void Constructor_InitializesChildViewModels()` | MedicalCaseWorkspaceViewModelTests |
| 118 | public | ` public void Constructor_InitializesMedicalCaseIdToEmptyGuid()` | MedicalCaseWorkspaceViewModelTests |
| 128 | public | ` public void Constructor_CurrentPatient_IsNullByDefault()` | MedicalCaseWorkspaceViewModelTests |
| 142 | public | ` public void MedicalCaseId_SetValue_UpdatesProperty()` | MedicalCaseWorkspaceViewModelTests |
| 156 | public | ` public void MedicalCaseId_SetValue_TriggersPropertyChanged()` | MedicalCaseWorkspaceViewModelTests |
| 179 | public | ` public void CurrentPatient_SetValue_UpdatesProperty()` | MedicalCaseWorkspaceViewModelTests |
| 193 | public | ` public void CurrentPatient_SetValue_TriggersPropertyChanged()` | MedicalCaseWorkspaceViewModelTests |
| 217 | public | ` public void CanCreateMedicalCaseDetailDto_WithValidData()` | MedicalCaseWorkspaceViewModelTests |
| 230 | public | ` public void MedicalCaseDetailDto_CaseStatus_DefaultsToSuspended()` | MedicalCaseWorkspaceViewModelTests |
| 240 | public | ` public void MedicalCaseDetailDto_HasPrescription_ReturnsCorrectValue()` | MedicalCaseWorkspaceViewModelTests |
| 256 | public | ` public void IsNavigationTarget_ReturnsTrue()` | MedicalCaseWorkspaceViewModelTests |
| 270 | private | ` private static MedicalCaseDetailDto CreateMedicalCaseDetailDto(bool hasPrescription = false)` | MedicalCaseWorkspaceViewModelTests |
| 286 | private | ` private static PatientDetailDto CreatePatientDetailDto()` | MedicalCaseWorkspaceViewModelTests |
| 16 | public | ` public PrescriptionEditorPureTests()` | PrescriptionEditorPureTests |
| 25 | private | ` private PrescriptionEditorViewModel CreateSut() => new(_context, _host, _loggerFactory);` | PrescriptionEditorPureTests |
| 28 | public | ` public void InitializeForNewCase_clears_prescription_and_sets_medicalCaseId()` | PrescriptionEditorPureTests |
| 40 | public | ` public void HasItems_reflects_collection_state()` | PrescriptionEditorPureTests |
| 52 | public | ` public void Adding_item_notifies_host_state_changed()` | PrescriptionEditorPureTests |
| 62 | public | ` public void Removing_item_notifies_host_state_changed()` | PrescriptionEditorPureTests |
| 75 | public | ` public void Validate_fails_when_no_items()` | PrescriptionEditorPureTests |
| 84 | public | ` public void Validate_succeeds_when_has_items()` | PrescriptionEditorPureTests |
| 94 | public | ` public void Reset_clears_items_and_resets_defaults()` | PrescriptionEditorPureTests |
| 110 | public | ` public void GetPrescriptionData_returns_null_when_no_items()` | PrescriptionEditorPureTests |
| 118 | public | ` public void Dispose_unsubscribes_from_collection_changes()` | PrescriptionEditorPureTests |
| 131 | public | ` public void InitializeFromDto_copies_prescription_fields()` | PrescriptionEditorPureTests |
| 23 | public | ` public PrescriptionEditorViewModelTests(UserJourneyFixture fixture) : base(fixture)` | PrescriptionEditorViewModelTests |
| 33 | private | ` private PrescriptionEditorViewModel CreateSut() => new(_context, _host, _loggerFactory);` | PrescriptionEditorViewModelTests |
| 36 | public | ` public void Constructor_InitializesEmptyPrescriptionAndHooksCollection()` | PrescriptionEditorViewModelTests |
| 46 | public | ` public void InitializeForNewCase_SetsMedicalCaseId_AndClearsItems()` | PrescriptionEditorViewModelTests |
| 59 | public | ` public void InitializeFromDto_CopiesFieldsAndItems()` | PrescriptionEditorViewModelTests |
| 101 | public | ` public void AddItem_RaisesPropertyChanged_AndNotifiesHost()` | PrescriptionEditorViewModelTests |
| 115 | public | ` public void RemoveItem_RaisesPropertyChanged_AndNotifiesHost()` | PrescriptionEditorViewModelTests |
| 129 | public | ` public void Reset_ClearsItems_AndRaisesHasItemsChange()` | PrescriptionEditorViewModelTests |
| 153 | public | ` public void Validate_ReturnsFalse_WhenNoItems()` | PrescriptionEditorViewModelTests |
| 162 | public | ` public void Validate_ReturnsTrue_WhenHasItems()` | PrescriptionEditorViewModelTests |
| 172 | public | ` public void GetPrescriptionData_ReturnsNull_WhenNoItems()` | PrescriptionEditorViewModelTests |
| 180 | public | ` public void GetPrescriptionData_ReturnsDto_WhenValidAndHasItems()` | PrescriptionEditorViewModelTests |
| 192 | public | ` public void Dispose_UnsubscribesFromCollectionChanged()` | PrescriptionEditorViewModelTests |
| 204 | public | ` public void PrescriptionCollection_ChangingItems_RaisesHasItemsPropertyChanged()` | PrescriptionEditorViewModelTests |
| 16 | private | ` private static Dictionary<Guid, decimal> CreatePriceLookup() => new()` | PrescriptionImportExtensionsTests |
| 25 | public | ` public void ToPrescriptionItemDtos_FormulaImport_WithPrices_ShouldFillUnitPrice()` | PrescriptionImportExtensionsTests |
| 46 | public | ` public void ToPrescriptionItemDtos_FormulaImport_WithoutPrices_ShouldLeaveZero()` | PrescriptionImportExtensionsTests |
| 64 | public | ` public void ToPrescriptionItemDtos_FormulaImport_HerbNotInPriceLookup_ShouldLeaveZero()` | PrescriptionImportExtensionsTests |
| 88 | public | ` public void ToPrescriptionItemDtos_HistoryCopy_WithPrices_ShouldRefreshToCurrentPrice()` | PrescriptionImportExtensionsTests |
| 108 | public | ` public void ToPrescriptionItemDtos_HistoryCopy_WithoutPrices_ShouldKeepOriginalPrice()` | PrescriptionImportExtensionsTests |
| 16 | public | ` public PrescriptionItemTests(UserJourneyFixture fixture) : base(fixture)` | PrescriptionItemTests |
| 20 | private | ` private PrescriptionItemViewModel CreateSut() => new();` | PrescriptionItemTests |
| 23 | public | ` public void Constructor_InitializesWithDefaults()` | PrescriptionItemTests |
| 40 | public | ` public void ItemCount_ReturnsZero_WhenItemsIsEmpty()` | PrescriptionItemTests |
| 48 | public | ` public void ItemCount_ReturnsCorrectCount_WhenItemsHasElements()` | PrescriptionItemTests |
| 59 | public | ` public void HasItems_ReturnsFalse_WhenItemsIsEmpty()` | PrescriptionItemTests |
| 67 | public | ` public void HasItems_ReturnsFalse_WhenItemsHasZeroElements()` | PrescriptionItemTests |
| 76 | public | ` public void HasItems_ReturnsTrue_WhenItemsHasElements()` | PrescriptionItemTests |
| 85 | public | ` public void Items_SetProperty_RaisesHasItemsPropertyChanged()` | PrescriptionItemTests |
| 99 | public | ` public void IsValid_ReturnsFalse_WhenHasItemsIsFalse()` | PrescriptionItemTests |
| 107 | public | ` public void IsValid_ReturnsTrue_WhenHasItemsIsTrue()` | PrescriptionItemTests |
| 116 | public | ` public void TotalPrice_ReturnsZero_WhenNoItems()` | PrescriptionItemTests |
| 125 | public | ` public void TotalPrice_CalculatesCorrectly_WithItems()` | PrescriptionItemTests |
| 156 | public | ` public void SingleDosePrice_CalculatesCorrectly()` | PrescriptionItemTests |
| 173 | public | ` public void DisplayText_ReturnsNumberAndItemCount()` | PrescriptionItemTests |
| 184 | public | ` public void DisplayText_ReturnsNewText_WhenPrescriptionNumberIsNull()` | PrescriptionItemTests |
| 193 | public | ` public void Validate_ReturnsFalse_WhenNoItems()` | PrescriptionItemTests |
| 204 | public | ` public void Validate_ReturnsTrue_WhenHasItems()` | PrescriptionItemTests |
| 216 | public | ` public void Validate_ReturnsTrue_WhenValidationDisabled()` | PrescriptionItemTests |
| 227 | public | ` public void ValidationEnabled_CanBeToggled()` | PrescriptionItemTests |
| 241 | public | ` public void Clear_ResetsAllFieldsIncludingId()` | PrescriptionItemTests |
| 262 | public | ` public void Reset_ResetsEditableFields_KeepingId()` | PrescriptionItemTests |
| 283 | public | ` public void NotifyItemsChanged_RaisesAllRelatedProperties()` | PrescriptionItemTests |
| 300 | public | ` public void GetConsultationData_ReturnsNull()` | PrescriptionItemTests |
| 310 | public | ` public void GetPrescriptionData_ReturnsNull_WhenNoItems()` | PrescriptionItemTests |
| 320 | public | ` public void GetPrescriptionData_ReturnsDto_WhenHasItems()` | PrescriptionItemTests |
| 31 | public | ` public PrescriptionPrintHandlerTests()` | PrescriptionPrintHandlerTests |
| 47 | private | ` private PrescriptionPrintHandler CreateSut() => new( _medicalCaseService, _repository, _sessionManager, _clinicSettingsService, _loggerFactory, _prin…` | PrescriptionPrintHandlerTests |
| 51 | public | ` public async Task PrintPreviewAsync_NoPrescription_ShouldReturnFailed()` | PrescriptionPrintHandlerTests |
| 66 | public | ` public async Task PrintPreviewAsync_EmptyItems_ShouldReturnFailed()` | PrescriptionPrintHandlerTests |
| 87 | public | ` public async Task PrintPreviewAsync_NullItems_ShouldReturnFailed()` | PrescriptionPrintHandlerTests |
| 108 | public | ` public async Task PrintPreviewAsync_NotCompleted_ShouldReturnFailed()` | PrescriptionPrintHandlerTests |
| 130 | public | ` public async Task ExportPdfAsync_NotCompleted_ShouldReturnFailed()` | PrescriptionPrintHandlerTests |
| 14 | public | ` public WorkflowStepIndicatorTests(UserJourneyFixture fixture) : base(fixture)` | WorkflowStepIndicatorTests |
| 20 | private | ` private WorkflowStepIndicator CreateSut() => new();` | WorkflowStepIndicatorTests |
| 23 | public | ` public void Constructor_InitializesWithDefaults()` | WorkflowStepIndicatorTests |
| 33 | public | ` public void Constructor_InitializesStepsWithCorrectLabels()` | WorkflowStepIndicatorTests |
| 45 | public | ` public void Constructor_InitializesStep1Active()` | WorkflowStepIndicatorTests |
| 56 | public | ` public void Constructor_InitializesOtherStepsInactive()` | WorkflowStepIndicatorTests |
| 68 | public | ` public void Constructor_MarksLastStep()` | WorkflowStepIndicatorTests |
| 76 | public | ` public void CurrentStep_WhenSetTo2_UpdatesStepStates()` | WorkflowStepIndicatorTests |
| 89 | public | ` public void CurrentStep_WhenSetTo3_UpdatesStepStates()` | WorkflowStepIndicatorTests |
| 101 | public | ` public void CurrentStep_WhenSetTo5_MarksAllPreviousCompleted()` | WorkflowStepIndicatorTests |
| 117 | public | ` public void CurrentStep_CanBeSetBackwards()` | WorkflowStepIndicatorTests |
| 132 | public | ` public void StepWidth_CanBeCustomized()` | WorkflowStepIndicatorTests |
| 141 | public | ` public void Steps_CollectionIsObservable()` | WorkflowStepIndicatorTests |
| 150 | public | ` public void CurrentStep_WhenChanged_RebuildsStepsCollection()` | WorkflowStepIndicatorTests |
| 161 | public | ` public void WorkflowStep_Model_HasAllProperties()` | WorkflowStepIndicatorTests |
| 11 | public | ` public void Default_state_is_editing_create_mode()` | WorkspaceStateTests |
| 22 | public | ` public void EnterReadOnlyMode_returns_new_instance_with_readonly()` | WorkspaceStateTests |
| 33 | public | ` public void EnterEditMode_when_CanEdit_returns_editing_state()` | WorkspaceStateTests |
| 41 | public | ` public void EnterEditMode_when_CannotEdit_returns_same_state()` | WorkspaceStateTests |
| 49 | public | ` public void DetermineFromContext_completed_case_owner_clinical()` | WorkspaceStateTests |
| 62 | public | ` public void DetermineFromContext_suspended_case_owner_clinical()` | WorkspaceStateTests |
| 75 | public | ` public void DetermineFromContext_admin_can_always_edit()` | WorkspaceStateTests |
| 92 | public | ` public void HeaderTitle_matches_mode_and_editing(WorkspaceMode mode, bool isEditing, string expected)` | WorkspaceStateTests |
| 100 | public | ` public void ShowSuspendButton_only_when_editing_clinical()` | WorkspaceStateTests |
| 112 | public | ` public void With_expression_creates_new_instance()` | WorkspaceStateTests |
| 122 | public | ` public void DetermineFromContext_non_owner_non_admin_gets_readonly()` | WorkspaceStateTests |
| 134 | public | ` public void DetermineFromContext_admin_completed_preferEditing_gets_editing()` | WorkspaceStateTests |
| 149 | public | ` public void BackButtonText_matches_mode(WorkspaceMode mode, string expected)` | WorkspaceStateTests |
| 9 | public | ` public void New_Instance_DefaultGroup_IsBusiness()` | NavigationItemTests |
| 19 | public | ` public void Group_CanBeSet_ToKnownValues(string group)` | NavigationItemTests |
| 14 | public | ` public void AgeDisplay_ReturnsFormattedAge()` | PatientDetailDisplayModelTests |
| 24 | public | ` public void AgeDisplay_ReturnsUnknownWhenNull()` | PatientDetailDisplayModelTests |
| 34 | public | ` public void GenderDisplay_ReturnsChineseText()` | PatientDetailDisplayModelTests |
| 46 | public | ` public void Summary_CombinesBasicInfo()` | PatientDetailDisplayModelTests |
| 47 | public | ` public PatientMasterDetailViewModelTests()` | PatientMasterDetailViewModelTests |
| 111 | private | ` private PatientMasterDetailViewModel CreateSut()` | PatientMasterDetailViewModelTests |
| 126 | public | ` public void Constructor_InitializesPageTitle()` | PatientMasterDetailViewModelTests |
| 136 | public | ` public void Constructor_ThrowsArgumentNullException_WhenPatientServiceIsNull()` | PatientMasterDetailViewModelTests |
| 152 | public | ` public void Constructor_ThrowsArgumentNullException_WhenStatusHandlerIsNull()` | PatientMasterDetailViewModelTests |
| 168 | public | ` public void Constructor_ThrowsArgumentNullException_WhenCacheManagerIsNull()` | PatientMasterDetailViewModelTests |
| 184 | public | ` public void Constructor_ThrowsArgumentNullException_WhenCardReaderViewModelIsNull()` | PatientMasterDetailViewModelTests |
| 200 | public | ` public void Constructor_ThrowsArgumentNullException_WhenPatientEditorIsNull()` | PatientMasterDetailViewModelTests |
| 216 | public | ` public void EntityDisplayName_ReturnsCorrectValue()` | PatientMasterDetailViewModelTests |
| 226 | public | ` public void ChildViewModels_AreExposedViaProperties()` | PatientMasterDetailViewModelTests |
| 237 | public | ` public void GenderOptions_ContainsAllGenderValues()` | PatientMasterDetailViewModelTests |
| 250 | public | ` public void StatusOptions_ContainsEnabledAndDisabled()` | PatientMasterDetailViewModelTests |
| 265 | public | ` public async Task LoadListAsync_LoadsPagedDataAndPopulatesItems()` | PatientMasterDetailViewModelTests |
| 297 | public | ` public async Task LoadListAsync_HandlesExceptionAndLogsError()` | PatientMasterDetailViewModelTests |
| 314 | public | ` public async Task LoadListAsync_PassesSearchTextToService()` | PatientMasterDetailViewModelTests |
| 344 | public | ` public async Task LoadDetailAsync_LoadsDetailViaServiceAndInitializesEditor()` | PatientMasterDetailViewModelTests |
| 363 | public | ` public async Task LoadDetailAsync_HandlesNullResultFromService()` | PatientMasterDetailViewModelTests |
| 380 | public | ` public async Task LoadDetailAsync_HandlesExceptionAndLogsError()` | PatientMasterDetailViewModelTests |
| 402 | public | ` public void CreateNewDetail_InitializesEditorForNewCase()` | PatientMasterDetailViewModelTests |
| 422 | public | ` public void SaveDetailAsync_ReturnsFalse_WhenValidationFails()` | PatientMasterDetailViewModelTests |
| 442 | public | ` public async Task SaveDetailAsync_CreatesNewPatient_WhenIsNew()` | PatientMasterDetailViewModelTests |
| 472 | public | ` public async Task SaveDetailAsync_UpdatesExistingPatient_WhenNotIsNew()` | PatientMasterDetailViewModelTests |
| 498 | public | ` public async Task SaveDetailAsync_ReturnsFalse_WhenCreateFails()` | PatientMasterDetailViewModelTests |
| 527 | public | ` public async Task DeleteItemAsync_CallsServiceDeleteAndInvalidatesCache()` | PatientMasterDetailViewModelTests |
| 546 | public | ` public async Task DeleteItemAsync_ReturnsFalse_WhenServiceFails()` | PatientMasterDetailViewModelTests |
| 567 | public | ` public async Task RestoreAsync_CallsStatusHandlerAndRefreshes()` | PatientMasterDetailViewModelTests |
| 585 | public | ` public async Task RestoreAsync_DoesNothing_WhenNoSelection()` | PatientMasterDetailViewModelTests |
| 599 | public | ` public async Task RestoreAsync_DoesNotRefresh_WhenRestoreFails()` | PatientMasterDetailViewModelTests |
| 617 | public | ` public void CanRestore_ReturnsFalse_WhenNoSelection()` | PatientMasterDetailViewModelTests |
| 632 | public | ` public async Task ReadCardAsync_DelegatesToCardReaderViewModel()` | PatientMasterDetailViewModelTests |
| 656 | public | ` public async Task ReadCardAsync_CallsFindPatientByIdNumber_WhenCardReadSucceeds()` | PatientMasterDetailViewModelTests |
| 680 | public | ` public async Task ReadCardAsync_ReturnsEarly_WhenReadCardFails()` | PatientMasterDetailViewModelTests |
| 698 | private | ` private static PatientListDto CreatePatientListDto(Guid? id = null, string name = "测试患者")` | PatientMasterDetailViewModelTests |
| 710 | private | ` private static PatientDetailDto CreatePatientDetailDto(Guid? id = null, string name = "测试患者", string? pinYinCode = "CSHZ")` | PatientMasterDetailViewModelTests |
| 736 | public | ` public static PatientDetailModel TestCreateNewDetail(this PatientMasterDetailViewModel vm)` | PatientMasterDetailViewModelTestExtensions |
| 770 | public | ` public static async Task<bool> SaveDetailAsync(this PatientMasterDetailViewModel vm, PatientDetailModel detail)` | PatientMasterDetailViewModelTestExtensions |
| 786 | public | ` public static async Task<bool> DeleteItemAsync(this PatientMasterDetailViewModel vm, PatientListDto item)` | PatientMasterDetailViewModelTestExtensions |
| 802 | public | ` public static async Task InvokeLoadDetailAsync(this PatientMasterDetailViewModel vm, PatientListDto item)` | PatientMasterDetailViewModelTestExtensions |
| 835 | public | ` public static async Task RestoreAsync(this PatientMasterDetailViewModel vm)` | PatientMasterDetailViewModelTestExtensions |
| 850 | public | ` public static async Task ImportAsync(this PatientMasterDetailViewModel vm)` | PatientMasterDetailViewModelTestExtensions |
| 865 | public | ` public static async Task ExportAsync(this PatientMasterDetailViewModel vm)` | PatientMasterDetailViewModelTestExtensions |
| 880 | public | ` public static async Task DownloadTemplateAsync(this PatientMasterDetailViewModel vm)` | PatientMasterDetailViewModelTestExtensions |
| 895 | public | ` public static async Task ReadCardAsync(this PatientMasterDetailViewModel vm)` | PatientMasterDetailViewModelTestExtensions |
| 21 | public | ` public void IPatientApi_ShouldBeInterface()` | RefitClientContractTests |
| 29 | public | ` public void IPatientApi_ShouldHaveRequiredMethods()` | RefitClientContractTests |
| 42 | public | ` public void IPatientApi_CreatePatientAsync_ShouldAcceptPatientInputDto()` | RefitClientContractTests |
| 54 | public | ` public void IPatientApi_Methods_ShouldReturnApiResponse()` | RefitClientContractTests |
| 73 | public | ` public void IAuthApi_ShouldBeInterface()` | RefitClientContractTests |
| 81 | public | ` public void IAuthApi_ShouldHaveRequiredMethods()` | RefitClientContractTests |
| 92 | public | ` public void IAuthApi_LoginAsync_ShouldAcceptLoginRequest()` | RefitClientContractTests |
| 108 | public | ` public void PatientInputDto_ShouldHaveRequiredProperties()` | RefitClientContractTests |
| 120 | public | ` public void PatientDetailDto_ShouldHaveRequiredProperties()` | RefitClientContractTests |
| 132 | public | ` public void LoginRequest_ShouldHaveRequiredProperties()` | RefitClientContractTests |
| 142 | public | ` public void LoginResponse_ShouldHaveRequiredProperties()` | RefitClientContractTests |
| 157 | public | ` public void Gender_ShouldBeEnum()` | RefitClientContractTests |
| 53 | public | ` public Task InitializePublicAsync() => base.InitializeAsync(CreateTestNavigationContext());` | TestableRegistrationListViewModel |
| 12 | public | ` public void CanCancelRegistration_requires_receptionist_role(UserRole role, bool expected)` | RegistrationQueueLogicTests |
| 24 | public | ` public void CanStartVisit_requires_Waiting_status(RegistrationStatus status, bool expected)` | RegistrationQueueLogicTests |
| 36 | public | ` public void CanCancelRegistration_requires_Waiting_and_Receptionist_source( RegistrationStatus status, RegistrationSource source, bool expected)` | RegistrationQueueLogicTests |
| 46 | public | ` public void Doctor_queue_filter_by_role()` | RegistrationQueueLogicTests |
| 57 | public | ` public void Doctor_queue_filter_returns_only_own_registrations()` | RegistrationQueueLogicTests |
| 76 | public | ` public void Queue_status_filter_excludes_non_waiting()` | RegistrationQueueLogicTests |
| 93 | public | ` public void Queue_ordered_by_registration_time()` | RegistrationQueueLogicTests |
| 8 | public | ` public void RegistrationSource_has_two_values()` | RegistrationSourceTests |
| 16 | public | ` public void RegistrationSource_has_correct_ordinal(RegistrationSource source, int expected)` | RegistrationSourceTests |
| 22 | public | ` public void Receptionist_is_default_source()` | RegistrationSourceTests |
| 30 | public | ` public void Source_description_matches(RegistrationSource source, string expected)` | RegistrationSourceTests |
| 45 | public | ` public void Receptionist_goes_through_Waiting_state(RegistrationSource source, bool expected)` | RegistrationSourceTests |
| 56 | public | ` public void Initial_status_matches_source_type(RegistrationSource source, RegistrationStatus status, bool expected)` | RegistrationSourceTests |
| 15 | public | ` public void RegistrationStatus_has_four_values()` | RegistrationStatusTransitionTests |
| 25 | public | ` public void RegistrationStatus_has_correct_ordinal(RegistrationStatus status, int expected)` | RegistrationStatusTransitionTests |
| 31 | public | ` public void RegistrationStatus_default_is_Waiting()` | RegistrationStatusTransitionTests |
| 45 | public | ` public void Valid_transitions_are_allowed(RegistrationStatus from, RegistrationStatus to, bool expected)` | RegistrationStatusTransitionTests |
| 59 | public | ` public void Invalid_transitions_are_rejected(RegistrationStatus from, RegistrationStatus to, bool expected)` | RegistrationStatusTransitionTests |
| 70 | public | ` public void Terminal_states_cannot_transition(RegistrationStatus status)` | RegistrationStatusTransitionTests |
| 79 | public | ` public void Waiting_is_initial_state()` | RegistrationStatusTransitionTests |
| 95 | public | ` public void Status_description_matches(RegistrationStatus status, string expected)` | RegistrationStatusTransitionTests |
| 116 | private | ` private static bool IsTransitionValid(RegistrationStatus from, RegistrationStatus to)` | RegistrationStatusTransitionTests |
| 17 | public | ` public DpapiPhotoStorageServiceTests()` | DpapiPhotoStorageServiceTests |
| 23 | public | ` public void Dispose()` | DpapiPhotoStorageServiceTests |
| 33 | public | ` public async Task SavePhotoAsync_WithValidData_ReturnsEncryptedFilePath()` | DpapiPhotoStorageServiceTests |
| 54 | public | ` public async Task LoadPhotoAsync_AfterSave_ReturnsOriginalData()` | DpapiPhotoStorageServiceTests |
| 73 | public | ` public async Task LoadPhotoAsync_WithNonExistentFile_ReturnsNull()` | DpapiPhotoStorageServiceTests |
| 83 | public | ` public async Task LoadPhotoAsync_WithEmptyPath_ReturnsNull()` | DpapiPhotoStorageServiceTests |
| 93 | public | ` public async Task DeletePhotoAsync_ExistingFile_DeletesAndReturnsTrue()` | DpapiPhotoStorageServiceTests |
| 109 | public | ` public async Task DeletePhotoAsync_NonExistentFile_ReturnsTrue()` | DpapiPhotoStorageServiceTests |
| 119 | public | ` public async Task PhotoExists_AfterSave_ReturnsTrue()` | DpapiPhotoStorageServiceTests |
| 132 | public | ` public void PhotoExists_WithEmptyPath_ReturnsFalse()` | DpapiPhotoStorageServiceTests |
| 139 | public | ` public async Task SavePhotoAsync_WithEmptyData_ThrowsArgumentException()` | DpapiPhotoStorageServiceTests |
| 146 | public | ` public async Task SavePhotoAsync_WithNullData_ThrowsArgumentNullException()` | DpapiPhotoStorageServiceTests |
| 153 | public | ` public async Task SavePhotoAsync_SameIdentifier_OverwritesFile()` | DpapiPhotoStorageServiceTests |
| 20 | public | ` public StartupPipelineTests()` | StartupPipelineTests |
| 30 | public | ` public void Constructor_ShouldInitialize_WithNotStartedState()` | StartupPipelineTests |
| 42 | public | ` public void RegisterStep_ShouldAddStep()` | StartupPipelineTests |
| 56 | public | ` public void RegisterStep_ShouldThrow_WhenStepIsNull()` | StartupPipelineTests |
| 66 | public | ` public void RegisterStep_ShouldThrow_WhenStepNameAlreadyExists()` | StartupPipelineTests |
| 82 | public | ` public void RegisterStep_ShouldThrow_WhenPipelineAlreadyStarted()` | StartupPipelineTests |
| 101 | public | ` public async Task ExecuteAsync_WithNoSteps_ShouldComplete()` | StartupPipelineTests |
| 112 | public | ` public async Task ExecuteAsync_ShouldExecuteSteps_InOrderByOrder()` | StartupPipelineTests |
| 133 | public | ` public async Task ExecuteAsync_WhenRequiredStepFails_ShouldStopAndReturnFailed()` | StartupPipelineTests |
| 158 | public | ` public async Task ExecuteAsync_WhenOptionalStepFails_ShouldContinue()` | StartupPipelineTests |
| 181 | public | ` public async Task ExecuteAsync_ShouldThrow_WhenExecutedTwice()` | StartupPipelineTests |
| 195 | public | ` public async Task ExecuteAsync_WhenCancelled_ShouldReturnCancelledResult()` | StartupPipelineTests |
| 218 | public | ` public async Task ExecuteAsync_ShouldRaiseStateChangedEvent()` | StartupPipelineTests |
| 233 | public | ` public async Task ExecuteAsync_ShouldRaiseStepCompletedEvent()` | StartupPipelineTests |
| 256 | public | ` public void GetDiagnostics_ShouldReturnCorrectInfo()` | StartupPipelineTests |
| 275 | public | ` public async Task GetDiagnostics_AfterExecution_ShouldShowCompletedSteps()` | StartupPipelineTests |
| 296 | private | ` private static IStartupStep CreateSubstituteStep(string name, int order, bool isRequired, Action? onExecute = null)` | StartupPipelineTests |
| 311 | private | ` private static IStartupStep CreateFailingStep(string name, int order, bool isRequired, string errorMessage)` | StartupPipelineTests |
| 28 | public | ` public ErrorHandlingStartupStepTests()` | ErrorHandlingStartupStepTests |
| 38 | public | ` public void Properties_ShouldHaveCorrectValues()` | ErrorHandlingStartupStepTests |
| 46 | public | ` public async Task ExecuteAsync_ShouldCallRegisterGlobalExceptionHandlers()` | ErrorHandlingStartupStepTests |
| 57 | public | ` public async Task ExecuteAsync_WhenServiceThrows_ShouldReturnFailed()` | ErrorHandlingStartupStepTests |
| 74 | public | ` public async Task ExecuteAsync_ShouldReportProgress()` | ErrorHandlingStartupStepTests |
| 97 | public | ` public ModuleCoordinatorStartupStepTests()` | ModuleCoordinatorStartupStepTests |
| 107 | public | ` public void Properties_ShouldHaveCorrectValues()` | ModuleCoordinatorStartupStepTests |
| 115 | public | ` public async Task ExecuteAsync_ShouldSucceed()` | ModuleCoordinatorStartupStepTests |
| 125 | public | ` public async Task ExecuteAsync_IsNotRequired_ShouldNotBlockStartup()` | ModuleCoordinatorStartupStepTests |
| 142 | public | ` public async Task ExecuteAsync_ShouldReportProgress()` | ModuleCoordinatorStartupStepTests |
| 165 | public | ` public ApiHealthCheckStartupStepTests()` | ApiHealthCheckStartupStepTests |
| 175 | public | ` public void Properties_ShouldHaveCorrectValues()` | ApiHealthCheckStartupStepTests |
| 184 | public | ` public async Task ExecuteAsync_WhenApiHealthy_ShouldReturnSuccess()` | ApiHealthCheckStartupStepTests |
| 199 | public | ` public async Task ExecuteAsync_WhenApiUnhealthy_ShouldStillReturnSuccess_DoesNotBlockStartup()` | ApiHealthCheckStartupStepTests |
| 215 | public | ` public async Task ExecuteAsync_WhenServiceThrows_ShouldStillReturnSuccess_ExceptionHandledInBackground()` | ApiHealthCheckStartupStepTests |
| 231 | public | ` public async Task ExecuteAsync_ShouldTriggerBackgroundHealthCheck()` | ApiHealthCheckStartupStepTests |
| 249 | public | ` public async Task ExecuteAsync_ShouldReportProgress()` | ApiHealthCheckStartupStepTests |
| 42 | public | ` public UserMasterDetailViewModelTests()` | UserMasterDetailViewModelTests |
| 97 | private | ` private UserMasterDetailViewModel CreateSut()` | UserMasterDetailViewModelTests |
| 112 | public | ` public void Constructor_InitializesPageTitle()` | UserMasterDetailViewModelTests |
| 122 | public | ` public void Constructor_ThrowsArgumentNullException_WhenCommandHandlerIsNull()` | UserMasterDetailViewModelTests |
| 138 | public | ` public void Constructor_ThrowsArgumentNullException_WhenPasswordHandlerIsNull()` | UserMasterDetailViewModelTests |
| 154 | public | ` public void Constructor_ThrowsArgumentNullException_WhenStatusHandlerIsNull()` | UserMasterDetailViewModelTests |
| 170 | public | ` public void Constructor_ThrowsArgumentNullException_WhenCacheManagerIsNull()` | UserMasterDetailViewModelTests |
| 186 | public | ` public void Constructor_ThrowsArgumentNullException_WhenUserEditorIsNull()` | UserMasterDetailViewModelTests |
| 202 | public | ` public void EntityDisplayName_ReturnsCorrectValue()` | UserMasterDetailViewModelTests |
| 212 | public | ` public void RoleOptions_ContainsAllRoleValues()` | UserMasterDetailViewModelTests |
| 226 | public | ` public void StatusOptions_ContainsEnabledAndDisabled()` | UserMasterDetailViewModelTests |
| 237 | public | ` public void SelectedRoleFilter_DefaultValueIsNull()` | UserMasterDetailViewModelTests |
| 247 | public | ` public void SelectedStatusFilter_DefaultValueIsNull()` | UserMasterDetailViewModelTests |
| 257 | public | ` public void ShowInactiveUsers_DefaultValueIsFalse()` | UserMasterDetailViewModelTests |
| 271 | public | ` public void SelectedRoleFilter_SetValue_TriggersPropertyChanged()` | UserMasterDetailViewModelTests |
| 291 | public | ` public void SelectedStatusFilter_SetValue_TriggersPropertyChanged()` | UserMasterDetailViewModelTests |
| 311 | public | ` public void ShowInactiveUsers_SetValue_TriggersPropertyChanged()` | UserMasterDetailViewModelTests |
| 334 | private | ` private static UserListDto CreateUserListDto(Guid? id = null, string userName = "testuser")` | UserMasterDetailViewModelTests |
| 346 | private | ` private static UserDetailDto CreateUserDetailDto(Guid? id = null, string userName = "testuser")` | UserMasterDetailViewModelTests |
| 20 | public | ` public NavigableViewModelBaseTests(UserJourneyFixture fixture) : base(fixture)` | NavigableViewModelBaseTests |
| 29 | public | ` public TestNavigableViewModel(IViewModelServices services) : base(services)` | TestNavigableViewModel |
| 34 | public | ` public async Task CallShowSuccessMessageAsync(string message)` | TestNavigableViewModel |
| 37 | public | ` public async Task CallShowErrorMessageAsync(string message)` | TestNavigableViewModel |
| 40 | public | ` public async Task CallShowWarningMessageAsync(string message)` | TestNavigableViewModel |
### LYBT.Tests.Server

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 27 | public | ` public static async Task<T> ShouldBeSuccessWithDataAsync<T>( this HttpResponseMessage response, string? because = null)` | BusinessAssertions |
| 44 | public | ` public static async Task<T> ShouldBeCreatedWithDataAsync<T>( this HttpResponseMessage response, string? because = null)` | BusinessAssertions |
| 61 | public | ` public static async Task<PagedResult<T>> ShouldBePagedResultAsync<T>( this HttpResponseMessage response, int? expectedMinCount = null, string? becaus…` | BusinessAssertions |
| 86 | public | ` public static async Task ShouldBeBusinessErrorAsync( this HttpResponseMessage response, HttpStatusCode expectedStatus, string? messageContains = null…` | BusinessAssertions |
| 107 | public | ` public static void ShouldBeUnauthorized(this HttpResponseMessage response)` | BusinessAssertions |
| 115 | public | ` public static void ShouldBeForbidden(this HttpResponseMessage response)` | BusinessAssertions |
| 123 | public | ` public static async Task ShouldBeNotFoundAsync( this HttpResponseMessage response, string? messageContains = null)` | BusinessAssertions |
| 134 | public | ` public static async Task ShouldBeSuccessAsync( this HttpResponseMessage response, string? because = null)` | BusinessAssertions |
| 149 | public | ` public static async Task ShouldBeValidationErrorAsync( this HttpResponseMessage response, string? messageContains = null)` | BusinessAssertions |
| 160 | public | ` public static async Task ShouldBeConflictAsync( this HttpResponseMessage response, string? messageContains = null)` | BusinessAssertions |
| 170 | public | ` public static void ShouldBeNoContent(this HttpResponseMessage response)` | BusinessAssertions |
| 37 | protected | ` protected IntegrationTestBase(TFixture fixture)` | IntegrationTestBase |
| 42 | public | ` public async Task InitializeAsync()` | IntegrationTestBase |
| 47 | public | ` public Task DisposeAsync() => Task.CompletedTask;` | IntegrationTestBase |
| 49 | protected | ` protected Task<HttpClient> LoginAsAdminAsync() => Fixture.LoginAsAdminAsync();` | IntegrationTestBase |
| 50 | protected | ` protected Task<HttpClient> LoginAsDoctorAsync() => Fixture.LoginAsDoctorAsync();` | IntegrationTestBase |
| 51 | protected | ` protected Task<HttpClient> LoginAsReceptionistAsync() => Fixture.LoginAsReceptionistAsync();` | IntegrationTestBase |
| 52 | protected | ` protected Task<HttpClient> LoginAsSysAdminAsync() => Fixture.LoginAsSysAdminAsync();` | IntegrationTestBase |
| 57 | protected | ` protected async Task<Guid> GetAdminUserIdAsync(HttpClient adminClient)` | IntegrationTestBase |
| 67 | protected | ` protected async Task<Guid> GetDoctorUserIdAsync(HttpClient adminClient)` | IntegrationTestBase |
| 87 | protected | ` protected static string UniqueName(string baseName)` | IntegrationTestBase |
| 95 | protected | ` protected static string UniquePhone()` | IntegrationTestBase |
| 106 | protected | ` protected static string UniqueIdNumber()` | IntegrationTestBase |
| 120 | protected | ` protected static string UniqueEmail(string baseName)` | IntegrationTestBase |
| 128 | protected | ` protected static string UniqueUsername(string baseName)` | IntegrationTestBase |
| 17 | public | ` public LocalSqlServerProvider()` | LocalSqlServerProvider |
| 26 | private | ` private string GetBaseConnectionString()` | LocalSqlServerProvider |
| 42 | private | ` private string GetFullConnectionString()` | LocalSqlServerProvider |
| 65 | public | ` public async Task InitializeAsync()` | LocalSqlServerProvider |
| 78 | public | ` public async Task DisposeAsync()` | LocalSqlServerProvider |
| 71 | public | ` public async Task InitializeAsync()` | ServerFixture |
| 129 | public | ` public void Dispose()` | ServerFixture |
| 137 | public | ` public async Task DisposeAsync()` | ServerFixture |
| 157 | public | ` public async Task ResetAsync()` | ServerFixture |
| 175 | public | ` public Task<HttpClient> LoginAsSysAdminAsync()` | ServerFixture |
| 181 | public | ` public Task<HttpClient> LoginAsAdminAsync()` | ServerFixture |
| 187 | public | ` public Task<HttpClient> LoginAsDoctorAsync()` | ServerFixture |
| 193 | public | ` public Task<HttpClient> LoginAsReceptionistAsync()` | ServerFixture |
| 200 | public | ` public async Task<HttpClient> LoginAsAsync(string username, string password)` | ServerFixture |
| 233 | public | ` public async Task<T> WithDbContextAsync<T>(Func<AppDbContext, Task<T>> action)` | ServerFixture |
| 243 | public | ` public async Task WithDbContextAsync(Func<AppDbContext, Task> action)` | ServerFixture |
| 253 | public | ` public static StringContent CreateJsonContent<T>(T obj)` | ServerFixture |
| 262 | public | ` public static T? ParseResponse<T>(string content)` | ServerFixture |
| 269 | private | ` private async Task MigrateAsync()` | ServerFixture |
| 280 | private | ` private async Task SeedBaseDataAsync()` | ServerFixture |
| 305 | private | ` private static async Task CreateIdentityUserAsync( UserManager<ApplicationUser> userManager, string userName, string realName, string role, string pa…` | ServerFixture |
| 328 | private | ` private static void RemoveHostedServices(IServiceCollection services)` | ServerFixture |
| 50 | private | ` private static void RemoveHostedServices(IServiceCollection services)` | SharedTestContext |
| 14 | public | ` public static FormulaBuilder Default() => new();` | FormulaBuilder |
| 16 | public | ` public FormulaBuilder WithName(string name) { _name = name; return this; }` | FormulaBuilder |
| 17 | public | ` public FormulaBuilder WithEffect(string effect) { _effect = effect; return this; }` | FormulaBuilder |
| 18 | public | ` public FormulaBuilder WithDescription(string desc) { _description = desc; return this; }` | FormulaBuilder |
| 19 | public | ` public FormulaBuilder WithUsage(string usage) { _usage = usage; return this; }` | FormulaBuilder |
| 21 | public | ` public FormulaBuilder AddHerb(Guid? herbId, string herbName, int dosage, string unit = "克")` | FormulaBuilder |
| 34 | public | ` public object Build() => new` | FormulaBuilder |
| 16 | public | ` public static HerbBuilder Default() => new();` | HerbBuilder |
| 18 | public | ` public HerbBuilder WithName(string name) { _name = name; return this; }` | HerbBuilder |
| 19 | public | ` public HerbBuilder WithPinYinCode(string code) { _pinYinCode = code; return this; }` | HerbBuilder |
| 20 | public | ` public HerbBuilder WithCategory(string cat) { _category = cat; return this; }` | HerbBuilder |
| 21 | public | ` public HerbBuilder WithUnit(string unit) { _unit = unit; return this; }` | HerbBuilder |
| 22 | public | ` public HerbBuilder WithPrice(decimal price) { _price = price; return this; }` | HerbBuilder |
| 23 | public | ` public HerbBuilder WithCostPrice(decimal cost) { _costPrice = cost; return this; }` | HerbBuilder |
| 24 | public | ` public HerbBuilder WithEffect(string effect) { _effect = effect; return this; }` | HerbBuilder |
| 26 | public | ` public object Build() => new` | HerbBuilder |
| 13 | public | ` public static MedicalCaseBuilder Default() => new();` | MedicalCaseBuilder |
| 15 | public | ` public MedicalCaseBuilder WithRegistration(Guid registrationId)` | MedicalCaseBuilder |
| 20 | public | ` public MedicalCaseBuilder ForPatient(Guid patientId)` | MedicalCaseBuilder |
| 25 | public | ` public MedicalCaseBuilder WithDoctor(Guid userId)` | MedicalCaseBuilder |
| 30 | public | ` public MedicalCaseBuilder WithRemark(string remark)` | MedicalCaseBuilder |
| 35 | public | ` public object BuildCreate() => new` | MedicalCaseBuilder |
| 44 | public | ` public static object BuildUpdate( Guid caseId, Guid patientId = default, Guid userId = default, object? consultation = null, object? prescription = n…` | MedicalCaseBuilder |
| 62 | public | ` public static object BuildConsultation( string? tcmDiagnosis = "风寒感冒", string? presentIllness = "患者近日受凉", string? tongueDiagnosis = "舌淡红苔薄白", string?…` | MedicalCaseBuilder |
| 74 | public | ` public static object BuildPrescription( List<object>? items = null, int dosageCount = 7, string? usage = "日一剂，水煎服", string? advice = null, Guid medic…` | MedicalCaseBuilder |
| 89 | public | ` public static object BuildPrescriptionItem( Guid herbId, string herbName, int dosage, string unit = "克", decimal unitPrice = 10.0m) => new` | MedicalCaseBuilder |
| 25 | public | ` public static PatientBuilder Default() => new();` | PatientBuilder |
| 27 | public | ` public PatientBuilder WithName(string name) { _name = name; return this; }` | PatientBuilder |
| 28 | public | ` public PatientBuilder WithGender(Gender gender) { _gender = gender; return this; }` | PatientBuilder |
| 29 | public | ` public PatientBuilder WithBirthDate(DateTime? date) { _birthDate = date; return this; }` | PatientBuilder |
| 30 | public | ` public PatientBuilder WithPhone(string phone) { _phoneNumber = phone; return this; }` | PatientBuilder |
| 31 | public | ` public PatientBuilder WithIdNumber(string idNumber) { _idNumber = idNumber; return this; }` | PatientBuilder |
| 33 | public | ` public PatientInputDto Build() => new()` | PatientBuilder |
| 47 | private | ` private static string GenerateIdNumber()` | PatientBuilder |
| 14 | public | ` public static RegistrationBuilder Default() => new();` | RegistrationBuilder |
| 16 | public | ` public RegistrationBuilder ForPatient(Guid patientId, string patientName)` | RegistrationBuilder |
| 23 | public | ` public RegistrationBuilder WithDoctor(Guid doctorId, string doctorName)` | RegistrationBuilder |
| 30 | public | ` public RegistrationBuilder WithRemark(string remark)` | RegistrationBuilder |
| 35 | public | ` public object Build() => new` | RegistrationBuilder |
| 17 | public | ` public static UserBuilder Default() => new();` | UserBuilder |
| 19 | public | ` public UserBuilder WithUserName(string name) { _userName = name; return this; }` | UserBuilder |
| 20 | public | ` public UserBuilder WithRealName(string name) { _realName = name; return this; }` | UserBuilder |
| 21 | public | ` public UserBuilder WithEmail(string email) { _email = email; return this; }` | UserBuilder |
| 22 | public | ` public UserBuilder WithPhone(string phone) { _phoneNumber = phone; return this; }` | UserBuilder |
| 23 | public | ` public UserBuilder WithRole(UserRole role) { _role = role; return this; }` | UserBuilder |
| 24 | public | ` public UserBuilder WithPassword(string pwd) { _password = pwd; return this; }` | UserBuilder |
| 26 | public | ` public object Build() => new` | UserBuilder |
| 42 | protected | ` protected TransactionalIntegrationTestBase()` | TransactionalIntegrationTestBase |
| 47 | public | ` public async Task InitializeAsync()` | TransactionalIntegrationTestBase |
| 68 | public | ` public async Task DisposeAsync()` | TransactionalIntegrationTestBase |
| 85 | protected | ` protected async Task<HttpClient> LoginAsAsync(string username, string password)` | TransactionalIntegrationTestBase |
| 114 | protected | ` protected Task<HttpClient> LoginAsAdminAsync() => LoginAsAsync("admin", "TestAdmin2025@");` | TransactionalIntegrationTestBase |
| 115 | protected | ` protected Task<HttpClient> LoginAsDoctorAsync() => LoginAsAsync("doctor", "TestDoctor2025@");` | TransactionalIntegrationTestBase |
| 116 | protected | ` protected Task<HttpClient> LoginAsSysAdminAsync() => LoginAsAsync("sysadmin", "TestAdmin2025@");` | TransactionalIntegrationTestBase |
| 120 | protected | ` protected async Task<Guid> GetAdminUserIdAsync(HttpClient adminClient)` | TransactionalIntegrationTestBase |
| 130 | protected | ` protected async Task<Guid> GetDoctorUserIdAsync(HttpClient adminClient)` | TransactionalIntegrationTestBase |
| 150 | protected | ` protected static string UniqueName(string baseName)` | TransactionalIntegrationTestBase |
| 158 | protected | ` protected static string UniquePhone()` | TransactionalIntegrationTestBase |
| 169 | protected | ` protected static string UniqueIdNumber()` | TransactionalIntegrationTestBase |
| 183 | protected | ` protected static string UniqueEmail(string baseName)` | TransactionalIntegrationTestBase |
| 191 | protected | ` protected static string UniqueUsername(string baseName)` | TransactionalIntegrationTestBase |
| 200 | private | ` private async Task SeedBaseDataAsync()` | TransactionalIntegrationTestBase |
| 224 | private | ` private static async Task CreateIdentityUserAsync( UserManager<ApplicationUser> userManager, string userName, string realName, string role, string pa…` | TransactionalIntegrationTestBase |
| 17 | public | ` public AuthSessionRepositoryTests()` | AuthSessionRepositoryTests |
| 26 | public | ` public void Dispose() => _context.Dispose();` | AuthSessionRepositoryTests |
| 28 | private | ` private static AuthSession CreateSession(Guid userId, DateTime expiry)` | AuthSessionRepositoryTests |
| 32 | public | ` public async Task RevokeAllUserSessionsAsync_WithActiveSessions_RevokesOnlyTargetUsers()` | AuthSessionRepositoryTests |
| 51 | public | ` public async Task RevokeAllUserSessionsAsync_WithNoActiveSessions_LeavesThemUntouched()` | AuthSessionRepositoryTests |
| 13 | private | ` private static AuthSession CreateSession(Guid userId)` | AuthSessionTests |
| 17 | public | ` public void Revoke_WithReason_SetsRevokedStateAndReason()` | AuthSessionTests |
| 31 | public | ` public void Revoke_WithoutReason_LeavesRevokedReasonNull()` | AuthSessionTests |
| 42 | public | ` public void Logout_DoesNotSetRevokedReason()` | AuthSessionTests |
| 24 | public | ` public JwtServiceTests()` | JwtServiceTests |
| 41 | private | ` private static IOptionsMonitor<T> CreateOptionsMonitor<T>(T value) where T : class, new()` | JwtServiceTests |
| 47 | public | ` public void Dispose()` | JwtServiceTests |
| 55 | public | ` public void GenerateToken_WithValidParameters_ShouldReturnNonEmptyToken()` | JwtServiceTests |
| 70 | public | ` public void GenerateToken_WithValidParameters_ShouldContainCorrectClaims()` | JwtServiceTests |
| 92 | public | ` public void GenerateToken_WithCustomUserType_ShouldContainCustomUserType()` | JwtServiceTests |
| 111 | public | ` public void GenerateToken_WithAdditionalClaims_ShouldContainAdditionalClaims()` | JwtServiceTests |
| 139 | public | ` public void GenerateToken_WithEmptyUserId_ShouldThrowArgumentException(string userId)` | JwtServiceTests |
| 154 | public | ` public void GenerateToken_WithEmptyUserName_ShouldThrowArgumentException(string userName)` | JwtServiceTests |
| 165 | public | ` public void GenerateToken_ShouldHaveCorrectExpiration()` | JwtServiceTests |
| 185 | public | ` public void GenerateToken_ShouldHaveCorrectIssuerAndAudience()` | JwtServiceTests |
| 208 | public | ` public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()` | JwtServiceTests |
| 227 | public | ` public void ValidateToken_WithExpiredToken_ShouldReturnNull()` | JwtServiceTests |
| 253 | public | ` public void ValidateToken_WithInvalidSignature_ShouldReturnNull()` | JwtServiceTests |
| 276 | public | ` public void ValidateToken_WithTamperedToken_ShouldReturnNull()` | JwtServiceTests |
| 294 | public | ` public void ValidateToken_WithEmptyToken_ShouldReturnNull(string token)` | JwtServiceTests |
| 304 | public | ` public void ValidateToken_WithWrongIssuer_ShouldReturnNull()` | JwtServiceTests |
| 327 | public | ` public void ValidateToken_WithWrongAudience_ShouldReturnNull()` | JwtServiceTests |
| 354 | public | ` public void Constructor_WithShortSecretKey_ShouldThrowArgumentException()` | JwtServiceTests |
| 371 | public | ` public void Constructor_WithEmptySecretKey_ShouldThrowInvalidOperationException()` | JwtServiceTests |
| 388 | public | ` public void Constructor_InProductionWithDefaultKey_ShouldThrowInvalidOperationException()` | JwtServiceTests |
| 411 | public | ` public void GenerateAndValidate_RoundTrip_ShouldPreserveAllClaims()` | JwtServiceTests |
| 439 | public | ` public void GenerateAndValidate_MultipleTokens_ShouldBeIndependent()` | JwtServiceTests |
| 482 | public | ` public TestOptionsMonitor(IOptions<T> options)` | TestOptionsMonitor |
| 491 | public | ` public IDisposable OnChange(Action<T, string> listener) => new NoOpDisposable();` | TestOptionsMonitor |
| 495 | public | ` public void Dispose() { }` | NoOpDisposable |
| 22 | public | ` public RevokeAllUserTokensCommandHandlerTests()` | RevokeAllUserTokensCommandHandlerTests |
| 42 | public | ` public void Dispose()` | RevokeAllUserTokensCommandHandlerTests |
| 49 | public | ` public async Task Handle_RevokesAllSessionsAndRecordsAudit()` | RevokeAllUserTokensCommandHandlerTests |
| 21 | public | ` public SecurityAuditServiceTests()` | SecurityAuditServiceTests |
| 32 | public | ` public void Dispose() => _context.Dispose();` | SecurityAuditServiceTests |
| 35 | public | ` public async Task RecordEventAsync_PersistsAuditLogWithAllFields()` | SecurityAuditServiceTests |
| 63 | public | ` public async Task RecordEventAsync_WithFailure_RecordsFailureReason()` | SecurityAuditServiceTests |
| 14 | public | ` public JsonFileConfigurationStoreTests()` | JsonFileConfigurationStoreTests |
| 20 | public | ` public void Dispose()` | JsonFileConfigurationStoreTests |
| 26 | private | ` private string CreateStorePath() => Path.Combine(_tempDir, "runtime-overrides.json");` | JsonFileConfigurationStoreTests |
| 29 | public | ` public async Task SetValue_ThenLoadAll_ReturnsSameValue()` | JsonFileConfigurationStoreTests |
| 44 | public | ` public async Task SetValue_EqualToBaseline_DoesNotPersistOverride()` | JsonFileConfigurationStoreTests |
| 61 | public | ` public async Task SetValue_DifferentFromBaseline_PersistsOverride()` | JsonFileConfigurationStoreTests |
| 80 | public | ` public async Task Remove_ThenLoadAll_Empty()` | JsonFileConfigurationStoreTests |
| 10 | public | ` public void AllowAutoCreateInProduction_DefaultValue_ShouldBeFalse()` | SystemAdminOptionsTests |
| 17 | public | ` public void InitialSetupToken_DefaultValue_ShouldBeNull()` | SystemAdminOptionsTests |
| 26 | public | ` public void BindFromConfiguration_ShouldMapNewProperties( bool allowAutoCreate, string? setupToken)` | SystemAdminOptionsTests |
| 46 | public | ` public void SectionName_ShouldBeSystemAdmin()` | SystemAdminOptionsTests |
| 22 | public | ` public SystemConfigurationServiceTests()` | SystemConfigurationServiceTests |
| 28 | public | ` public void Dispose()` | SystemConfigurationServiceTests |
| 34 | private | ` private static Dictionary<string, string?> BuildBaseline() => new()` | SystemConfigurationServiceTests |
| 46 | default(private) | ` private (SystemConfigurationService Service, string OverridePath) CreateService()` | SystemConfigurationServiceTests |
| 68 | public | ` public async Task SetValue_ThenGetValue_ReturnsUpdatedValue()` | SystemConfigurationServiceTests |
| 84 | public | ` public async Task SetValue_SensitiveKey_JwtSecretKey_IsRejected()` | SystemConfigurationServiceTests |
| 98 | public | ` public async Task SetValue_SensitiveKey_ConnectionString_IsRejected()` | SystemConfigurationServiceTests |
| 111 | public | ` public async Task SetValue_SensitiveSection_DefaultPasswords_IsRejected()` | SystemConfigurationServiceTests |
| 124 | public | ` public async Task SetValue_UnknownSection_IsRejected()` | SystemConfigurationServiceTests |
| 137 | public | ` public async Task SetValue_EmptyKey_IsRejected()` | SystemConfigurationServiceTests |
| 150 | public | ` public async Task UpdateConfiguration_BatchValidKeys_Succeeds()` | SystemConfigurationServiceTests |
| 170 | public | ` public async Task SetValue_TriggersOptionsMonitorHotReload()` | SystemConfigurationServiceTests |
| 203 | public | ` public async Task UpdateConfiguration_ContainsSensitiveKey_RejectsWholeBatch()` | SystemConfigurationServiceTests |
| 15 | public | ` public void Herbs_ShouldBeInitializedAsEmptyList()` | FormulaModelTests |
| 24 | public | ` public void Herbs_ListOperations_ShouldWork()` | FormulaModelTests |
| 19 | public | ` public void IsLocked_ShouldReturnFalse_WhenActive()` | MedicalCaseModelTests |
| 26 | public | ` public void IsLocked_ShouldReturnFalse_WhenSuspended()` | MedicalCaseModelTests |
| 33 | public | ` public void IsLocked_ShouldReturnFalse_WhenCompletedToday()` | MedicalCaseModelTests |
| 44 | public | ` public void IsLocked_ShouldReturnTrue_WhenCompletedBeforeToday()` | MedicalCaseModelTests |
| 62 | public | ` public void IsActive_ShouldReturnCorrectValue(MedicalCaseStatus status, bool expected)` | MedicalCaseModelTests |
| 72 | public | ` public void IsCompleted_ShouldReturnCorrectValue(MedicalCaseStatus status, bool expected)` | MedicalCaseModelTests |
| 83 | public | ` public void Consultation_ShouldUseSharedPrimaryKey()` | MedicalCaseModelTests |
| 93 | public | ` public void Prescription_ShouldBeOptionalWithForeignKey()` | MedicalCaseModelTests |
| 107 | public | ` public void NeedsPrescription_ShouldSupportThreeStates()` | MedicalCaseModelTests |
| 14 | public | ` public void Age_WhenBirthDateIsNull_ShouldReturnNull()` | PatientModelTests |
| 21 | public | ` public void Age_WhenBirthDateIsSet_ShouldCalculateCorrectAge()` | PatientModelTests |
| 28 | public | ` public void Age_WhenBirthDateIsThisYear_ShouldReturn0()` | PatientModelTests |
| 35 | public | ` public void Age_WhenBirthdayNotYetReached_ShouldSubtractOne()` | PatientModelTests |
| 14 | public | ` public void Constructor_ShouldInitializeBusinessDefaults()` | PrescriptionModelTests |
| 25 | public | ` public void Items_ShouldSupportAddingWithForeignKey()` | PrescriptionModelTests |
| 19 | public | ` public void GetExceptionInfo_UnauthorizedAccessException_ShouldMapTo403()` | SystemExceptionHandlerTests |
| 58 | public | ` public IFileInfo GetFileInfo(string subpath) => new NotFoundFileInfo(subpath);` | EmptyFileProvider |
| 60 | public | ` public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;` | EmptyFileProvider |
| 62 | public | ` public IChangeToken Watch(string filter) => NullChangeToken.Singleton;` | EmptyFileProvider |
| 43 | public | ` public Task InitializeAsync()` | DatabaseInitializationServiceTests |
| 59 | public | ` public void Dispose()` | DatabaseInitializationServiceTests |
| 65 | public | ` public async Task DisposeAsync()` | DatabaseInitializationServiceTests |
| 70 | private | ` private DatabaseInitializationService CreateService( SystemAdminOptions? adminOptions = null, DefaultPasswordOptions? passwordOptions = null, ILogger…` | DatabaseInitializationServiceTests |
| 86 | private | ` private static string HashPassword(string password) =>` | DatabaseInitializationServiceTests |
| 92 | public | ` public async Task InitializeDatabase_WhenNoSuperAdminExists_InitializationSucceeds()` | DatabaseInitializationServiceTests |
| 105 | public | ` public async Task InitializeDatabase_WhenNoSuperAdminExists_DoesNotCreateUser()` | DatabaseInitializationServiceTests |
| 127 | public | ` public async Task InitializeDatabase_WhenSuperAdminExists_SkipsCreation()` | DatabaseInitializationServiceTests |
| 160 | public | ` public async Task InitializeDatabase_WhenSoftDeletedSuperAdminExists_SkipsCreation()` | DatabaseInitializationServiceTests |
| 195 | public | ` public async Task InitializeDatabase_WhenEmailOccupied_SkipsCreation()` | DatabaseInitializationServiceTests |
| 227 | public | ` public async Task InitializeDatabase_WhenAutoCreateFalse_SkipsCreation()` | DatabaseInitializationServiceTests |
| 256 | public | ` public async Task InitializeDatabase_CalledTwice_DoesNotCreateUser()` | DatabaseInitializationServiceTests |
| 279 | public | ` public async Task EnsureSystemAdminExists_WhenCreated_SetsMustChangeOnNextLogin_WhenForceChangeEnabled()` | DatabaseInitializationServiceTests |
| 302 | public | ` public async Task EnsureSystemAdminExists_WhenCreated_DoesNotSetMustChangeOnNextLogin_WhenForceChangeDisabled()` | DatabaseInitializationServiceTests |
| 326 | public | ` public async Task EnsureSystemAdminExists_Production_AutoCreateDisabled_DoesNotCreateAdmin()` | DatabaseInitializationServiceTests |
| 362 | public | ` public async Task EnsureSystemAdminExists_Production_AutoCreateEnabled_ValidToken_DoesNotCreateAdmin()` | DatabaseInitializationServiceTests |
| 406 | public | ` public async Task EnsureSystemAdminExists_Production_AutoCreateEnabled_InvalidToken_DoesNotCreateAdmin()` | DatabaseInitializationServiceTests |
| 446 | public | ` public async Task EnsureSystemAdminExists_Development_DoesNotCreateAdmin()` | DatabaseInitializationServiceTests |
| 479 | public | ` public async Task EnsureSystemAdminExists_LogsCreationEvent_WithStructuredData()` | DatabaseInitializationServiceTests |
| 499 | public | ` public async Task EnsureSystemAdminExists_NewAdmin_DoesNotCreateUser()` | DatabaseInitializationServiceTests |
| 522 | public | ` public async Task EnsureSystemAdminExists_ExistingAdmin_DoesNotResetMustChangeFlag()` | DatabaseInitializationServiceTests |
| 555 | public | ` public async Task EnsureSystemAdminExists_ExistingAdmin_DoesNotChangePassword()` | DatabaseInitializationServiceTests |
| 592 | public | ` public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;` | CapturingLogger |
| 594 | public | ` public bool IsEnabled(LogLevel logLevel) => true;` | CapturingLogger |
| 596 | public | ` public void Log<TState>( LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)` | CapturingLogger |
| 623 | public | ` public void Dispose()` | NullScope |
| 652 | public | ` public TestDbContextAccessor(AppDbContext context) => Context = context;` | TestDbContextAccessor |
| 10 | private | ` private static Dictionary<string, string?> BuildFullConfig()` | ProductionConfigurationValidatorTests |
| 34 | private | ` private static ProductionConfigurationValidator CreateValidator(Dictionary<string, string?> configData)` | ProductionConfigurationValidatorTests |
| 45 | public | ` public void ValidateCriticalItems_WhenAllCriticalPresent_ReturnsEmpty()` | ProductionConfigurationValidatorTests |
| 58 | public | ` public void ValidateCriticalItems_WhenCriticalAbsent_ReturnsMissingItems()` | ProductionConfigurationValidatorTests |
| 76 | public | ` public void ValidateCriticalItems_WhenJwtSecretKeyTooShort_ReportsMinLengthViolation()` | ProductionConfigurationValidatorTests |
| 97 | public | ` public void ValidateImportantItems_WhenAllImportantPresent_ReturnsEmpty()` | ProductionConfigurationValidatorTests |
| 110 | public | ` public void ValidateImportantItems_WhenImportantAbsent_ReturnsMissingItems()` | ProductionConfigurationValidatorTests |
| 128 | public | ` public void ValidateImportantItems_WhenEmailFormatInvalid_ReportsFormatViolation()` | ProductionConfigurationValidatorTests |
| 149 | public | ` public void ValidateOrThrow_WhenAllConfigValid_DoesNotThrow()` | ProductionConfigurationValidatorTests |
| 160 | public | ` public void ValidateOrThrow_WhenCriticalMissing_ThrowsProductionConfigurationException()` | ProductionConfigurationValidatorTests |
| 178 | public | ` public void ValidateOrThrow_WhenAutoCreateFalse_NoErrors()` | ProductionConfigurationValidatorTests |
| 192 | public | ` public void ValidateOrThrow_WhenAutoCreateTrueAndValidToken_NoErrors()` | ProductionConfigurationValidatorTests |
| 206 | public | ` public void ValidateOrThrow_WhenAutoCreateTrueAndMissingToken_ThrowsWithCrossFieldError()` | ProductionConfigurationValidatorTests |
| 221 | public | ` public void ValidateOrThrow_WhenAutoCreateTrueAndPlaceholderToken_ThrowsWithCrossFieldError()` | ProductionConfigurationValidatorTests |
| 236 | public | ` public void ValidateOrThrow_WhenAutoCreateTrueAndShortToken_ThrowsWithCrossFieldError()` | ProductionConfigurationValidatorTests |
| 251 | public | ` public void ValidateOrThrow_WhenAutoCreateNotSet_NoErrors()` | ProductionConfigurationValidatorTests |
| 17 | public | ` public SensitiveDataJsonConverterTests()` | SensitiveDataJsonConverterTests |
| 24 | public | ` public void Serialize_WithSensitiveProperties_ShouldMaskValues()` | SensitiveDataJsonConverterTests |
| 63 | public | ` public void Serialize_WithoutSensitiveProperties_ShouldNotUseMasking()` | SensitiveDataJsonConverterTests |
| 85 | public | ` public void Deserialize_ShouldNotMaskValues()` | SensitiveDataJsonConverterTests |
| 101 | public | ` public void Serialize_NullValue_ShouldSerializeAsNull()` | SensitiveDataJsonConverterTests |
| 26 | public | ` public void IsValidStatusTransition_OnlyAllowsSuspendedActiveBidirectional( MedicalCaseStatus from, MedicalCaseStatus to, bool expected)` | MedicalCaseBusinessRulesTests |
| 33 | public | ` public void CanCreateNewCase_RejectsWhenActiveOrSuspendedExists()` | MedicalCaseBusinessRulesTests |
| 41 | public | ` public void HasActiveCase_HasSuspendedCase_DetectSingleActiveConstraint()` | MedicalCaseBusinessRulesTests |
| 26 | public | ` public void ToListDto_WithValidEntity_ShouldMapAllProperties()` | MedicalCaseMapperTests |
| 47 | public | ` public void ToListDto_ShouldIgnoreComputedFields()` | MedicalCaseMapperTests |
| 70 | public | ` public void ToListDtos_WithMultipleEntities_ShouldMapAll()` | MedicalCaseMapperTests |
| 91 | public | ` public void ToListDtos_WithEmptyList_ShouldReturnEmpty()` | MedicalCaseMapperTests |
| 108 | public | ` public void ToDetailDto_WithValidEntity_ShouldMapAllProperties()` | MedicalCaseMapperTests |
| 129 | public | ` public void ToDetailDto_ShouldIgnoreNestedAndComputedFields()` | MedicalCaseMapperTests |
| 154 | public | ` public void ToDetailDtos_WithMultipleEntities_ShouldMapAll()` | MedicalCaseMapperTests |
| 177 | public | ` public void ToConsultationDetailDto_WithValidEntity_ShouldMapProperties()` | MedicalCaseMapperTests |
| 198 | public | ` public void ToConsultationDetailDto_ShouldMapIdToMedicalCaseId()` | MedicalCaseMapperTests |
| 211 | public | ` public void ToConsultationDetailDto_ShouldIgnoreContextFields()` | MedicalCaseMapperTests |
| 231 | public | ` public void ToPrescriptionDetailDto_WithValidEntity_ShouldMapProperties()` | MedicalCaseMapperTests |
| 255 | public | ` public void ToPrescriptionDetailDto_ShouldIgnoreComputedFields()` | MedicalCaseMapperTests |
| 278 | public | ` public void ToPrescriptionEntity_WithValidDto_ShouldMapProperties()` | MedicalCaseMapperTests |
| 297 | public | ` public void ToPrescriptionEntity_ShouldIgnoreAuditAndSystemFields()` | MedicalCaseMapperTests |
| 323 | public | ` public void UpdatePrescriptionEntity_ShouldUpdateMappedFields()` | MedicalCaseMapperTests |
| 348 | public | ` public void UpdatePrescriptionEntity_ShouldNotModifyIgnoredFields()` | MedicalCaseMapperTests |
| 371 | public | ` public void ToPrescriptionItemDto_WithValidEntity_ShouldMapProperties()` | MedicalCaseMapperTests |
| 393 | public | ` public void ToPrescriptionItemDto_ShouldIgnoreComputedFields()` | MedicalCaseMapperTests |
| 413 | public | ` public void MapToMedicalCaseDetailDto_WithFullNavigationProperties_ShouldMapAll()` | MedicalCaseMapperTests |
| 438 | public | ` public void MapToMedicalCaseDetailDto_WithConsultation_ShouldEnrichConsultationDto()` | MedicalCaseMapperTests |
| 458 | public | ` public void MapToMedicalCaseDetailDto_WithPrescription_ShouldEnrichPrescriptionDto()` | MedicalCaseMapperTests |
| 475 | public | ` public void MapToMedicalCaseDetailDto_WithoutConsultation_ShouldBeNull()` | MedicalCaseMapperTests |
| 491 | public | ` public void MapToMedicalCaseDetailDto_WithoutPrescription_ShouldBeNull()` | MedicalCaseMapperTests |
| 506 | public | ` public void MapToMedicalCaseDetailDto_WithDeletedPrescription_ShouldBeNull()` | MedicalCaseMapperTests |
| 521 | public | ` public void MapToMedicalCaseDetailDto_PrescriptionCalculation_ShouldBeCorrect()` | MedicalCaseMapperTests |
| 543 | private | ` private static MedicalCaseEntity CreateTestMedicalCase()` | MedicalCaseMapperTests |
| 559 | private | ` private static MedicalCaseEntity CreateTestMedicalCaseWithNavigations()` | MedicalCaseMapperTests |
| 629 | private | ` private static Consultation CreateTestConsultation()` | MedicalCaseMapperTests |
| 644 | private | ` private static Prescription CreateTestPrescription()` | MedicalCaseMapperTests |
| 662 | private | ` private static PrescriptionInputDto CreateTestPrescriptionInputDto()` | MedicalCaseMapperTests |
| 674 | private | ` private static PrescriptionItem CreateTestPrescriptionItem()` | MedicalCaseMapperTests |
| 22 | public | ` public async Task ExecuteWithConcurrencyRetryAsync_WithSuccessfulAction_ShouldReturnResult()` | MedicalCaseServiceHelperTests |
| 39 | public | ` public async Task ExecuteWithConcurrencyRetryAsync_WithDbUpdateConcurrencyException_ShouldRetry()` | MedicalCaseServiceHelperTests |
| 63 | public | ` public async Task ExecuteWithConcurrencyRetryAsync_WithMaxRetriesExceeded_ShouldThrow()` | MedicalCaseServiceHelperTests |
| 89 | public | ` public void EnsureCanEdit_OwnerWithActiveCase_ShouldNotThrow()` | MedicalCaseServiceHelperTests |
| 105 | public | ` public void EnsureCanEdit_NonOwner_ShouldThrowUnauthorizedAccessException()` | MedicalCaseServiceHelperTests |
| 122 | public | ` public void EnsureCanEdit_Admin_ShouldNotThrow()` | MedicalCaseServiceHelperTests |
| 142 | public | ` public void EnsureCanDelete_OwnerWithActiveCase_ShouldNotThrow()` | MedicalCaseServiceHelperTests |
| 158 | public | ` public void EnsureCanDelete_NonOwner_ShouldThrowUnauthorizedAccessException()` | MedicalCaseServiceHelperTests |
| 175 | public | ` public void EnsureCanDelete_Admin_ShouldNotThrow()` | MedicalCaseServiceHelperTests |
| 194 | private | ` private static Entities.MedicalCases.MedicalCase CreateTestMedicalCase()` | MedicalCaseServiceHelperTests |
| 18 | public | ` public NotificationServiceTests()` | NotificationServiceTests |
| 24 | public | ` public async Task NotifyNewRegistrationAsync_SendsRegistrationToDoctorGroup()` | NotificationServiceTests |
| 47 | public | ` public async Task NotifyRegistrationStatusChangedAsync_SendsStatusToDoctorGroup()` | NotificationServiceTests |
| 62 | public | ` public async Task NotifyNewRegistrationAsync_EmptyDoctorId_DoesNotSend()` | NotificationServiceTests |
| 70 | public | ` public async Task NotifyRegistrationStatusChangedAsync_EmptyDoctorId_DoesNotSend()` | NotificationServiceTests |
| 78 | public | ` public async Task NotifyDifferentDoctors_ArePushedToSeparateGroups_NoCrossDelivery()` | NotificationServiceTests |
| 96 | public | ` public FakeHubContext()` | FakeHubContext |
| 112 | public | ` public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();` | FakeClients |
| 113 | public | ` public IClientProxy Client(string connectionId) => throw new NotSupportedException();` | FakeClients |
| 114 | public | ` public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();` | FakeClients |
| 115 | public | ` public IClientProxy Group(string groupName) => new FakeProxy(sent, groupName);` | FakeClients |
| 116 | public | ` public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();` | FakeClients |
| 117 | public | ` public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();` | FakeClients |
| 118 | public | ` public IClientProxy User(string userId) => throw new NotSupportedException();` | FakeClients |
| 119 | public | ` public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();` | FakeClients |
| 124 | public | ` public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)` | FakeProxy |
| 11 | public | ` public void Add_Then_TryGetDoctorId_ReturnsMappedDoctor()` | RegistrationConnectionManagerTests |
| 24 | public | ` public void TryRemove_RemovesMappingAndReturnsDoctorId()` | RegistrationConnectionManagerTests |
| 38 | public | ` public void CountConnections_CountsOnlyTargetDoctorConnections()` | RegistrationConnectionManagerTests |
| 53 | public | ` public void TryGetDoctorId_UnknownConnection_ReturnsFalse()` | RegistrationConnectionManagerTests |
| 25 | public | ` public ReportRepositoryTests()` | ReportRepositoryTests |
| 34 | public | ` public void Dispose() => _context.Dispose();` | ReportRepositoryTests |
| 36 | private | ` private static MedicalCase CreateCase(string doctorName, MedicalCaseStatus status = MedicalCaseStatus.Completed, bool isDeleted = false)` | ReportRepositoryTests |
| 50 | private | ` private static Prescription CreatePrescription(Guid medicalCaseId, params PrescriptionItem[] items)` | ReportRepositoryTests |
| 59 | private | ` private static PrescriptionItem CreateItem(string herbName, int dosage, decimal unitPrice)` | ReportRepositoryTests |
| 71 | private | ` private static Registration CreateRegistration(Guid medicalCaseId, decimal fee, string doctorName = "张医生", bool isDeleted = false)` | ReportRepositoryTests |
| 89 | private | ` private async Task AddCaseAsync(MedicalCase medicalCase, DateTime createdAt)` | ReportRepositoryTests |
| 97 | private | ` private async Task AddRegistrationAsync(Registration registration, DateTime createdAt)` | ReportRepositoryTests |
| 105 | private | ` private async Task AddAsync(params object[] entities)` | ReportRepositoryTests |
| 112 | public | ` public async Task GetRegistrationFeeByDayAsync_GroupsFeesByDayAndFiltersRange()` | ReportRepositoryTests |
| 131 | public | ` public async Task GetMedicineFeeByDayAsync_SumsOnlyCompletedCaseItemsByDay()` | ReportRepositoryTests |
| 153 | public | ` public async Task GetConsultationCountByDayAsync_CountsCompletedCasesByDay()` | ReportRepositoryTests |
| 169 | public | ` public async Task GetDoctorPerformanceAsync_CombinesCountsFeesAndPrescriptions()` | ReportRepositoryTests |
| 212 | public | ` public async Task GetHerbRankingAsync_ReturnsTopNByUsageCount()` | ReportRepositoryTests |
| 237 | public | ` public async Task GetPatientFlowByDayAsync_ClassifiesNewAndReturning()` | ReportRepositoryTests |
| 26 | public | ` public ReportServiceTests()` | ReportServiceTests |
| 35 | public | ` public void Dispose() => _context.Dispose();` | ReportServiceTests |
| 37 | private | ` private async Task AddCaseAsync(MedicalCase medicalCase, DateTime createdAt)` | ReportServiceTests |
| 45 | private | ` private async Task AddRegistrationAsync(Registration registration, DateTime createdAt)` | ReportServiceTests |
| 53 | private | ` private async Task AddAsync(params object[] entities)` | ReportServiceTests |
| 59 | private | ` private static MedicalCase CreateCase(string doctorName, MedicalCaseStatus status = MedicalCaseStatus.Completed)` | ReportServiceTests |
| 72 | private | ` private static Prescription CreatePrescription(Guid medicalCaseId, params PrescriptionItem[] items)` | ReportServiceTests |
| 81 | private | ` private static PrescriptionItem CreateItem(string herbName, int dosage, decimal unitPrice)` | ReportServiceTests |
| 93 | private | ` private static Registration CreateRegistration(Guid medicalCaseId, decimal fee)` | ReportServiceTests |
| 110 | public | ` public async Task GetIncomeTrendAsync_DayGranularity_AssemblesLabelsAndTotals()` | ReportServiceTests |
| 132 | public | ` public async Task GetIncomeTrendAsync_WeekGranularity_RollsUpWholeWeek()` | ReportServiceTests |
| 153 | public | ` public async Task GetIncomeTrendAsync_MonthGranularity_UsesYearMonthLabel()` | ReportServiceTests |
| 168 | public | ` public async Task GetConsultationTrendAsync_ZeroFillsEmptyDays()` | ReportServiceTests |
| 181 | public | ` public async Task GetDoctorPerformanceAsync_ComputesAveragePrescriptionPrice()` | ReportServiceTests |
| 203 | public | ` public async Task GetDoctorPerformanceAsync_NoPrescription_ReturnsZeroAverage()` | ReportServiceTests |
| 216 | public | ` public async Task GetPatientFlowAsync_WeekGranularity_RollsUpByWeek()` | ReportServiceTests |
| 244 | public | ` public async Task GetHerbRankingAsync_AppliesTopLimit()` | ReportServiceTests |
| 20 | public | ` public HerbRepositoryTests()` | HerbRepositoryTests |
| 30 | public | ` public void Dispose()` | HerbRepositoryTests |
| 38 | private | ` private Herb CreateTestHerb(string name, string pinYinCode, string origin = "测试产地", Guid? createdBy = null)` | HerbRepositoryTests |
| 53 | public | ` public async Task GetByNameAsync_WithExactName_ReturnsHerb()` | HerbRepositoryTests |
| 70 | public | ` public async Task GetByNameAsync_WithNonExistentName_ReturnsNull()` | HerbRepositoryTests |
| 82 | public | ` public async Task GetByNameAsync_WithDeletedHerb_ReturnsNull()` | HerbRepositoryTests |
| 102 | public | ` public async Task GetPagedAsync_WithDefaultParameters_ReturnsPagedResult()` | HerbRepositoryTests |
| 129 | public | ` public async Task GetPagedAsync_WithKeywordMatchingName_ReturnsFilteredResults()` | HerbRepositoryTests |
| 153 | public | ` public async Task GetPagedAsync_WithKeywordMatchingPinyin_ReturnsFilteredResults()` | HerbRepositoryTests |
| 177 | public | ` public async Task GetPagedAsync_WithKeywordMatchingMultiple_ReturnsAllMatches()` | HerbRepositoryTests |
| 202 | public | ` public async Task GetPagedAsync_WithPagination_ReturnsCorrectPage()` | HerbRepositoryTests |
| 227 | public | ` public async Task GetPagedAsync_WithLargeDataset_Supports300PlusHerbs()` | HerbRepositoryTests |
| 263 | public | ` public async Task GetPagedAsync_WithDeletedHerbs_ExcludesDeleted()` | HerbRepositoryTests |
| 285 | public | ` public async Task GetPagedAsync_ResultsSortedByName_Ascending()` | HerbRepositoryTests |
| 311 | public | ` public async Task GetPagedAsync_WithEmptyDatabase_ReturnsEmptyResult()` | HerbRepositoryTests |
| 14 | public | ` public void SectionName_ShouldBeApiClient()` | ApiClientOptionsTests |
| 21 | public | ` public void DefaultValues_ShouldBeSetCorrectly()` | ApiClientOptionsTests |
| 35 | public | ` public void Validation_BaseUrl_Required(string? baseUrl)` | ApiClientOptionsTests |
| 51 | public | ` public void Validation_BaseUrl_MustBeValidUrl()` | ApiClientOptionsTests |
| 70 | public | ` public void Validation_BaseUrl_ValidUrls(string baseUrl)` | ApiClientOptionsTests |
| 87 | public | ` public void Validation_TimeoutSeconds_OutOfRange(int seconds)` | ApiClientOptionsTests |
| 110 | public | ` public void Validation_TimeoutSeconds_ValidRange(int seconds)` | ApiClientOptionsTests |
| 20 | public | ` public void ValidateOnStart_InvalidJwtSecretKey_ThrowsOptionsValidationException()` | ValidateOnStartTests |
| 47 | public | ` public void ValidateOnStart_ShortJwtSecretKey_ThrowsOptionsValidationException()` | ValidateOnStartTests |
| 74 | public | ` public void ValidateOnStart_AccessTokenLongerThanRefreshToken_ThrowsOptionsValidationException()` | ValidateOnStartTests |
| 105 | public | ` public void ValidateOnStart_ConnectionPoolMinGreaterThanMax_ThrowsOptionsValidationException()` | ValidateOnStartTests |
| 134 | public | ` public void ValidateOnStart_RetryPolicyBaseDelayGreaterThanMax_ThrowsOptionsValidationException()` | ValidateOnStartTests |
| 167 | public | ` public void ValidateOnStart_LoginLimitInternalLessThanPermit_ThrowsOptionsValidationException()` | ValidateOnStartTests |
| 197 | public | ` public void ValidateOnStart_ApiLimitAdminLessThanPermit_ThrowsOptionsValidationException()` | ValidateOnStartTests |
| 231 | public | ` public void ValidateOnStart_ValidConfiguration_NoExceptionThrown()` | ValidateOnStartTests |
| 272 | public | ` public void ValidateOnStart_RateLimitingDisabled_SkipsRateLimitingValidation()` | ValidateOnStartTests |
| 14 | public | ` public void JwtOptions_ShouldHaveSecureDefaults()` | JwtOptionsValidationTests |
| 31 | public | ` public void JwtOptions_ShouldRejectWeakSecrets(string? secretKey)` | JwtOptionsValidationTests |
| 44 | public | ` public void JwtOptions_ShouldAcceptStrongSecret()` | JwtOptionsValidationTests |
| 63 | public | ` public void AccessTokenExpiration_ShouldBeReasonable(int minutes, bool shouldBeValid)` | JwtOptionsValidationTests |
| 84 | public | ` public void RefreshTokenExpiration_ShouldBeReasonable(int days, bool shouldBeValid)` | JwtOptionsValidationTests |
| 100 | public | ` public void JwtOptions_ShouldRequireIssuerAndAudience()` | JwtOptionsValidationTests |
| 115 | public | ` public void Issuer_ShouldUseSecureProtocol(string issuer, bool shouldBeValid)` | JwtOptionsValidationTests |
| 129 | private | ` private bool IsSecretValid(string? secretKey)` | JwtOptionsValidationTests |
| 134 | private | ` private bool IsAccessTokenExpirationValid(int minutes)` | JwtOptionsValidationTests |
| 139 | private | ` private bool IsRefreshTokenExpirationValid(int days)` | JwtOptionsValidationTests |
| 144 | private | ` private bool IsIssuerSecure(string? issuer)` | JwtOptionsValidationTests |
| 22 | public | ` public void ServerConfiguration_LoadFromJson_AllOptionsRegistered()` | ConfigurationLoadingTests |
| 48 | public | ` public void ServerConfiguration_LoadFromJson_ValuesBindCorrectly()` | ConfigurationLoadingTests |
| 79 | public | ` public void ClientConfiguration_LoadFromJson_AllOptionsRegistered()` | ConfigurationLoadingTests |
| 98 | public | ` public void ClientConfiguration_LoadFromJson_ValuesBindCorrectly()` | ConfigurationLoadingTests |
| 124 | public | ` public void Configuration_EnvironmentVariables_OverrideJsonValues()` | ConfigurationLoadingTests |
| 173 | public | ` public void Configuration_IOptionsMonitor_SupportsReload()` | ConfigurationLoadingTests |
| 190 | public | ` public void Configuration_IOptionsSnapshot_RegisteredCorrectly()` | ConfigurationLoadingTests |
| 212 | public | ` public void ServerConfiguration_RegistersIValidateOptions_ForJwtOptions()` | ConfigurationLoadingTests |
| 228 | public | ` public void ServerConfiguration_RegistersIValidateOptions_ForDatabaseOptions()` | ConfigurationLoadingTests |
| 244 | public | ` public void ServerConfiguration_RegistersIValidateOptions_ForSecurityOptions()` | ConfigurationLoadingTests |
| 260 | public | ` public void ClientConfiguration_RegistersIValidateOptions_ForJwtOptions()` | ConfigurationLoadingTests |
| 280 | public | ` public void ServerConfiguration_RegistersIValidateOptions_ForLocalJwtOptions()` | ConfigurationLoadingTests |
| 296 | public | ` public void LocalJwtOptionsValidator_InvalidBase64Key_FailsValidation()` | ConfigurationLoadingTests |
| 311 | public | ` public void LocalJwtOptionsValidator_ShortKey_FailsValidation()` | ConfigurationLoadingTests |
| 326 | public | ` public void LocalJwtOptionsValidator_ValidKey_PassesValidation()` | ConfigurationLoadingTests |
| 347 | public | ` public void LocalJwtOptions_DefaultValues_AreCorrect()` | ConfigurationLoadingTests |
| 356 | public | ` public void CorsOptions_DefaultValues_AreCorrect()` | ConfigurationLoadingTests |
| 365 | public | ` public void DesktopUpdateOptions_DefaultValues_AreCorrect()` | ConfigurationLoadingTests |
| 374 | public | ` public void OfflineModeOptions_DefaultValues_AreCorrect()` | ConfigurationLoadingTests |
| 385 | public | ` public void AppInfoOptions_DefaultValues_AreCorrect()` | ConfigurationLoadingTests |
| 394 | public | ` public void AllNewOptions_SectionNameConstants_AreCorrect()` | ConfigurationLoadingTests |
| 407 | private | ` private static IConfiguration CreateServerConfiguration()` | ConfigurationLoadingTests |
| 472 | private | ` private static IConfiguration CreateClientConfiguration()` | ConfigurationLoadingTests |
| 14 | public | ` public void SectionName_ShouldBeJwt()` | JwtOptionsTests |
| 21 | public | ` public void DefaultValues_ShouldBeSetCorrectly()` | JwtOptionsTests |
| 38 | public | ` public void Validation_SecretKey_Required(string? secretKey)` | JwtOptionsTests |
| 56 | public | ` public void Validation_SecretKey_MinLength32(string secretKey)` | JwtOptionsTests |
| 72 | public | ` public void Validation_SecretKey_ValidWhen32OrMore()` | JwtOptionsTests |
| 93 | public | ` public void Validation_AccessTokenExpirationMinutes_OutOfRange(int minutes)` | JwtOptionsTests |
| 116 | public | ` public void Validation_AccessTokenExpirationMinutes_ValidRange(int minutes)` | JwtOptionsTests |
| 137 | public | ` public void Validation_RefreshTokenExpirationDays_OutOfRange(int days)` | JwtOptionsTests |
| 159 | public | ` public void Validation_RefreshTokenExpirationDays_ValidRange(int days)` | JwtOptionsTests |
| 17 | public | ` public void Validate_ValidBase64SecretKey_ReturnsSuccess()` | JwtOptionsValidatorTests |
| 35 | public | ` public void Validate_InvalidBase64SecretKey_ReturnsFailure()` | JwtOptionsValidatorTests |
| 54 | public | ` public void Validate_ShortBase64SecretKey_ReturnsFailure()` | JwtOptionsValidatorTests |
| 73 | public | ` public void Validate_EmptySecretKey_ReturnsSuccess()` | JwtOptionsValidatorTests |
| 91 | public | ` public void Validate_AccessTokenLongerThanRefreshToken_ReturnsFailure()` | JwtOptionsValidatorTests |
| 110 | public | ` public void Validate_AccessTokenEqualToRefreshToken_ReturnsFailure()` | JwtOptionsValidatorTests |
| 128 | public | ` public void Validate_AccessTokenShorterThanRefreshToken_ReturnsSuccess()` | JwtOptionsValidatorTests |
| 18 | public | ` public void AddLybtServerConfiguration_RegistersJwtOptions()` | ServerConfigurationExtensionsTests |
| 36 | public | ` public void AddLybtServerConfiguration_RegistersDatabaseOptions()` | ServerConfigurationExtensionsTests |
| 54 | public | ` public void AddLybtServerConfiguration_RegistersSecurityOptions()` | ServerConfigurationExtensionsTests |
| 71 | public | ` public void AddLybtServerConfiguration_RegistersSessionOptions()` | ServerConfigurationExtensionsTests |
| 87 | private | ` private static IConfiguration CreateTestConfiguration()` | ServerConfigurationExtensionsTests |
| 18 | public | ` public void Constructor_Default_SetsDefaultMessage()` | AppExceptionTests |
| 30 | public | ` public void Constructor_WithMessage_SetsMessage()` | AppExceptionTests |
| 43 | public | ` public void Constructor_WithMessageAndInnerException_SetsProperties()` | AppExceptionTests |
| 58 | public | ` public void Constructor_WithTypedErrorCode_SetsAllProperties()` | AppExceptionTests |
| 77 | public | ` public void Constructor_WithTypedErrorCode_UserMessageDefaultsToMessage()` | AppExceptionTests |
| 100 | public | ` public void GetHttpStatusCode_WithTypedErrorCode_ReturnsCorrectStatus(EC errorCode, int expectedStatus)` | AppExceptionTests |
| 113 | public | ` public void GetHttpStatusCode_WithoutTypedErrorCode_Returns500()` | AppExceptionTests |
| 134 | public | ` public void Category_WithTypedErrorCode_ReturnsCorrectCategory(EC errorCode, ErrorCategory expectedCategory)` | AppExceptionTests |
| 147 | public | ` public void Category_WithoutTypedErrorCode_ReturnsGeneral()` | AppExceptionTests |
| 18 | public | ` public void BusinessException_DefaultConstructor_SetsDefaultMessage()` | BusinessExceptionTests |
| 28 | public | ` public void BusinessException_WithMessage_SetsMessage()` | BusinessExceptionTests |
| 41 | public | ` public void BusinessException_WithBusinessRule_SetsBusinessRule()` | BusinessExceptionTests |
| 56 | public | ` public void BusinessException_WithTypedErrorCode_SetsProperties()` | BusinessExceptionTests |
| 77 | public | ` public void NotFoundException_WithResourceInfo_SetsProperties()` | BusinessExceptionTests |
| 94 | public | ` public void NotFoundException_WithTypedErrorCode_SetsCorrectStatus()` | BusinessExceptionTests |
| 108 | public | ` public void NotFoundException_StaticFactory_User_CreatesCorrectException()` | BusinessExceptionTests |
| 127 | public | ` public void ValidationException_WithFieldAndMessage_SetsProperties()` | BusinessExceptionTests |
| 143 | public | ` public void ValidationException_WithMultipleErrors_SetsAllErrors()` | BusinessExceptionTests |
| 162 | public | ` public void ValidationException_AddError_AppendsToExisting()` | BusinessExceptionTests |
| 181 | public | ` public void ConflictException_WithMessage_SetsProperties()` | BusinessExceptionTests |
| 195 | public | ` public void ConflictException_WithTypedErrorCode_SetsCorrectProperties()` | BusinessExceptionTests |
| 209 | public | ` public void ConflictException_StaticFactory_MedicalCaseVersion_CreatesCorrectException()` | BusinessExceptionTests |
| 230 | public | ` public void UnauthorizedException_Default_Returns401()` | BusinessExceptionTests |
| 240 | public | ` public void UnauthorizedException_WithTypedErrorCode_SetsProperties()` | BusinessExceptionTests |
| 255 | public | ` public void UnauthorizedException_StaticFactory_InvalidPassword_CreatesCorrectException()` | BusinessExceptionTests |
| 20 | public | ` public void GetModuleName_GeneralErrors_ReturnsGeneral(ErrorCode errorCode, string expectedModule)` | ErrorCodeTests |
| 33 | public | ` public void GetModuleName_UserErrors_ReturnsUsersAuth(ErrorCode errorCode, string expectedModule)` | ErrorCodeTests |
| 45 | public | ` public void GetModuleName_PatientErrors_ReturnsPatients(ErrorCode errorCode, string expectedModule)` | ErrorCodeTests |
| 57 | public | ` public void GetModuleName_MedicalCaseErrors_ReturnsMedicalCase(ErrorCode errorCode, string expectedModule)` | ErrorCodeTests |
| 69 | public | ` public void GetModuleName_OtherModules_ReturnsCorrectModule(ErrorCode errorCode, string expectedModule)` | ErrorCodeTests |
| 82 | public | ` public void GetModuleName_HerbMcceeErrors_ReturnsHerbs(ErrorCode errorCode, string expectedModule)` | ErrorCodeTests |
| 101 | public | ` public void ToHttpStatusCode_ValidationErrors_Returns400(ErrorCode errorCode, int expectedStatus)` | ErrorCodeTests |
| 114 | public | ` public void ToHttpStatusCode_AuthenticationErrors_Returns401(ErrorCode errorCode, int expectedStatus)` | ErrorCodeTests |
| 127 | public | ` public void ToHttpStatusCode_AuthorizationErrors_Returns403(ErrorCode errorCode, int expectedStatus)` | ErrorCodeTests |
| 141 | public | ` public void ToHttpStatusCode_NotFoundErrors_Returns404(ErrorCode errorCode, int expectedStatus)` | ErrorCodeTests |
| 153 | public | ` public void ToHttpStatusCode_ConflictErrors_Returns409(ErrorCode errorCode, int expectedStatus)` | ErrorCodeTests |
| 165 | public | ` public void ToHttpStatusCode_BusinessRuleErrors_Returns422(ErrorCode errorCode, int expectedStatus)` | ErrorCodeTests |
| 175 | public | ` public void ToHttpStatusCode_UnknownError_Returns500()` | ErrorCodeTests |
| 191 | public | ` public void ToCategory_ValidationErrors_ReturnsValidation(ErrorCode errorCode, ErrorCategory expectedCategory)` | ErrorCodeTests |
| 204 | public | ` public void ToCategory_AuthErrors_ReturnsAuthentication(ErrorCode errorCode, ErrorCategory expectedCategory)` | ErrorCodeTests |
| 216 | public | ` public void ToCategory_NotFoundErrors_ReturnsResource(ErrorCode errorCode, ErrorCategory expectedCategory)` | ErrorCodeTests |
| 229 | public | ` public void ToCategory_SystemErrors_ReturnsSystem(ErrorCode errorCode, ErrorCategory expectedCategory)` | ErrorCodeTests |
| 241 | public | ` public void ToCategory_ConcurrencyErrors_ReturnsConcurrency(ErrorCode errorCode, ErrorCategory expectedCategory)` | ErrorCodeTests |
| 261 | public | ` public void ToFormattedString_ReturnsCorrectFormat(ErrorCode errorCode, string expectedFormat)` | ErrorCodeTests |
| 275 | public | ` public void HerbMcceeCodes_AllDefined_HaveMessages()` | ErrorCodeTests |
| 308 | public | ` public void PatientMcceeCodes_AllDefined_HaveMessages()` | ErrorCodeTests |
| 338 | public | ` public void FormulaMcceeCodes_AllDefined_HaveMessages()` | ErrorCodeTests |
| 374 | public | ` public void MedicalCaseMcceeCodes_AllDefined_HaveMessages()` | ErrorCodeTests |
| 430 | public | ` public void MedicalCaseMcceeCodes_PermissionErrors_Return403(ErrorCode errorCode, int expectedStatus)` | ErrorCodeTests |
| 440 | public | ` public void MedicalCaseMcceeCodes_BusinessErrors_Return422(ErrorCode errorCode, int expectedStatus)` | ErrorCodeTests |
| 457 | public | ` public void AuthMcceeCodes_AllReturn401(ErrorCode errorCode, int expectedStatus)` | ErrorCodeTests |
| 470 | public | ` public void ErrorCode_AllValues_AreUnique()` | ErrorCodeTests |
| 487 | public | ` public void ErrorCode_AllValues_HaveErrorMessages()` | ErrorCodeTests |
| 18 | public | ` public void TokenExpired_SetsCorrectErrorCode()` | UnauthorizedExceptionTests |
| 28 | public | ` public void TokenExpired_SetsCorrectUserMessage()` | UnauthorizedExceptionTests |
| 38 | public | ` public void TokenExpired_SetsCorrectFailureReason()` | UnauthorizedExceptionTests |
| 48 | public | ` public void TokenExpired_ReturnsHttpStatus401()` | UnauthorizedExceptionTests |
| 58 | public | ` public void TokenExpired_ErrorCodeFormattedString_ContainsCorrectValue()` | UnauthorizedExceptionTests |
| 72 | public | ` public void AuthAccessTokenExpired_HasCorrectValue()` | UnauthorizedExceptionTests |
| 79 | public | ` public void AuthAccessTokenExpired_ToHttpStatusCode_Returns401()` | UnauthorizedExceptionTests |
| 89 | public | ` public void AuthAccessTokenExpired_GetUserMessage_ReturnsMeaningfulMessage()` | UnauthorizedExceptionTests |
| 16 | public | ` public LoggingLevelManagerTests()` | LoggingLevelManagerTests |
| 22 | public | ` public void Constructor_ShouldSetDefaultLevel()` | LoggingLevelManagerTests |
| 30 | public | ` public void EnableDebugMode_ShouldLowerLevel()` | LoggingLevelManagerTests |
| 42 | public | ` public void EnableDebugMode_WithDuration_ShouldSetExpiration()` | LoggingLevelManagerTests |
| 52 | public | ` public void EnableDebugMode_WithoutDuration_ShouldNotSetExpiration()` | LoggingLevelManagerTests |
| 61 | public | ` public void DisableDebugMode_ShouldRestoreDefaultLevel()` | LoggingLevelManagerTests |
| 74 | public | ` public void SetLevel_ShouldChangeMinimumLevel()` | LoggingLevelManagerTests |
| 81 | public | ` public void GetStatus_WhenNotInDebugMode_ShouldReturnInactive()` | LoggingLevelManagerTests |
| 91 | public | ` public void GetStatus_WhenInDebugMode_ShouldReturnActive()` | LoggingLevelManagerTests |
| 101 | public | ` public void Dispose_ShouldNotThrow()` | LoggingLevelManagerTests |
| 111 | public | ` public void Dispose_CalledTwice_ShouldNotThrow()` | LoggingLevelManagerTests |
| 120 | public | ` public void Dispose()` | LoggingLevelManagerTests |
| 17 | public | ` public void Mask_WithNullValue_ShouldReturnEmpty()` | SensitiveDataMaskerTests |
| 24 | public | ` public void Mask_WithEmptyValue_ShouldReturnEmpty()` | SensitiveDataMaskerTests |
| 31 | public | ` public void Mask_WithFullMode_ShouldReturnHiddenText()` | SensitiveDataMaskerTests |
| 38 | public | ` public void Mask_WithHashMode_ShouldReturnRedactedWithHash()` | SensitiveDataMaskerTests |
| 47 | public | ` public void Mask_WithPartialMode_PhoneNumber_ShouldMaskMiddle()` | SensitiveDataMaskerTests |
| 56 | public | ` public void Mask_WithDefaultMode_LongString_ShouldShowFirstAndLast()` | SensitiveDataMaskerTests |
| 65 | public | ` public void Mask_WithDefaultMode_ShortString_ShouldReturnStars()` | SensitiveDataMaskerTests |
| 84 | public | ` public void IsSensitiveFieldName_ShouldDetectCorrectly(string? fieldName, bool expected)` | SensitiveDataMaskerTests |
| 94 | public | ` public void SanitizeText_WithPassword_ShouldRedact()` | SensitiveDataMaskerTests |
| 102 | public | ` public void SanitizeText_WithBearerToken_ShouldRedact()` | SensitiveDataMaskerTests |
| 111 | public | ` public void SanitizeText_WithNullInput_ShouldReturnEmpty()` | SensitiveDataMaskerTests |
| 121 | public | ` public void MaskUri_WithSensitiveParams_ShouldRedact()` | SensitiveDataMaskerTests |
| 129 | public | ` public void MaskUri_WithNull_ShouldReturnEmpty()` | SensitiveDataMaskerTests |
| 139 | public | ` public void SanitizeException_WithNull_ShouldReturnEmpty()` | SensitiveDataMaskerTests |
| 145 | public | ` public void SanitizeException_WithException_ShouldContainTypeName()` | SensitiveDataMaskerTests |
| 158 | public | ` public void MaskObject_WithSensitiveProperties_ShouldMask()` | SensitiveDataMaskerTests |
| 19 | public | ` public void Validate_WithValidPassword_ShouldPass(string password, bool expectedValid)` | PasswordPolicyValidatorTests |
| 33 | public | ` public void Validate_WithNullPassword_ShouldReturnError()` | PasswordPolicyValidatorTests |
| 44 | public | ` public void Validate_WithEmptyPassword_ShouldReturnError()` | PasswordPolicyValidatorTests |
| 55 | public | ` public void Validate_WithShortPassword_ShouldReturnError()` | PasswordPolicyValidatorTests |
| 66 | public | ` public void Validate_WithoutUppercase_ShouldReturnError()` | PasswordPolicyValidatorTests |
| 77 | public | ` public void Validate_WithoutLowercase_ShouldReturnError()` | PasswordPolicyValidatorTests |
| 88 | public | ` public void Validate_WithoutDigit_ShouldReturnError()` | PasswordPolicyValidatorTests |
| 99 | public | ` public void Validate_WithoutSpecialChar_ShouldReturnError()` | PasswordPolicyValidatorTests |
| 112 | public | ` public void Validate_WithRepeatingCharacters_ShouldReturnError(string password)` | PasswordPolicyValidatorTests |
| 126 | public | ` public void Validate_WithSequentialNumbers_ShouldReturnError(string password)` | PasswordPolicyValidatorTests |
| 139 | public | ` public void Validate_WithSequentialLetters_ShouldReturnError(string password)` | PasswordPolicyValidatorTests |
| 154 | public | ` public void Validate_WithCommonPassword_ShouldReturnError(string password)` | PasswordPolicyValidatorTests |
| 169 | public | ` public void CalculateStrength_WithNullPassword_ShouldReturn0()` | PasswordPolicyValidatorTests |
| 179 | public | ` public void CalculateStrength_WithEmptyPassword_ShouldReturn0()` | PasswordPolicyValidatorTests |
| 192 | public | ` public void CalculateStrength_WithValidPassword_ShouldReturnScore(string password, int minScore)` | PasswordPolicyValidatorTests |
| 205 | public | ` public void CalculateStrength_WithCommonPassword_ShouldReturnLowScore(string password)` | PasswordPolicyValidatorTests |
| 219 | public | ` public void GetStrengthLevel_WithNullPassword_ShouldReturnWeak()` | PasswordPolicyValidatorTests |
| 229 | public | ` public void GetStrengthLevel_WithWeakPassword_ShouldReturnWeakOrFair()` | PasswordPolicyValidatorTests |
| 241 | public | ` public void GetStrengthLevel_WithStrongPassword_ShouldReturnVeryStrong()` | PasswordPolicyValidatorTests |
| 256 | public | ` public void Policy_Constants_ShouldHaveCorrectValues()` | PasswordPolicyValidatorTests |
| 18 | public | ` public void SecureEquals_WithSamePasswords_ShouldReturnTrue()` | PasswordHelperTests |
| 32 | public | ` public void SecureEquals_WithDifferentPasswords_ShouldReturnFalse()` | PasswordHelperTests |
| 46 | public | ` public void SecureEquals_WithNullPasswords_ShouldReturnTrue()` | PasswordHelperTests |
| 58 | public | ` public void SecureEquals_WithOneNullPassword_ShouldReturnFalse(string? password1, string? password2)` | PasswordHelperTests |
| 68 | public | ` public void SecureEquals_WithDifferentLengths_ShouldReturnFalse()` | PasswordHelperTests |
| 83 | public | ` public void ValidatePassword_WithValidPassword_ShouldReturnValidResult()` | PasswordHelperTests |
| 100 | public | ` public void ValidatePassword_WithEmptyPassword_ShouldReturnInvalidResult(string? password)` | PasswordHelperTests |
| 111 | public | ` public void ValidatePassword_WithShortPassword_ShouldReturnInvalidResult()` | PasswordHelperTests |
| 125 | public | ` public void ValidatePassword_WithoutUppercase_ShouldReturnInvalidResult()` | PasswordHelperTests |
| 139 | public | ` public void ValidatePassword_WithoutLowercase_ShouldReturnInvalidResult()` | PasswordHelperTests |
| 153 | public | ` public void ValidatePassword_WithoutDigits_ShouldReturnInvalidResult()` | PasswordHelperTests |
| 167 | public | ` public void ValidatePassword_WithoutSpecialChars_ShouldReturnInvalidResult()` | PasswordHelperTests |
| 181 | public | ` public void ValidatePassword_WithCommonPassword_ShouldReturnInvalidResult()` | PasswordHelperTests |
| 195 | public | ` public void ValidatePassword_WithValidationErrors_ShouldGenerateSuggestions()` | PasswordHelperTests |
| 218 | public | ` public void CheckPasswordStrength_WithDifferentPasswords_ShouldReturnCorrectStrength(string? password, PasswordStrength expectedStrength)` | PasswordHelperTests |
| 228 | public | ` public void CheckPasswordStrength_WithCommonPassword_ShouldHaveLowerStrength()` | PasswordHelperTests |
| 249 | public | ` public void IsCommonPassword_WithDifferentPasswords_ShouldReturnCorrectResult(string? password, bool expected)` | PasswordHelperTests |
| 259 | public | ` public void GenerateSecurePassword_WithDefaultParameters_ShouldGenerateValidPassword()` | PasswordHelperTests |
| 277 | public | ` public void GenerateSecurePassword_WithDifferentLengths_ShouldGenerateCorrectLength(int length)` | PasswordHelperTests |
| 287 | public | ` public void GenerateSecurePassword_WithOnlyLowercase_ShouldOnlyContainLowercase()` | PasswordHelperTests |
| 302 | public | ` public void GenerateSecurePassword_WithTooShortLength_ShouldThrowException()` | PasswordHelperTests |
| 310 | public | ` public void GenerateSecurePassword_WithNoCharacterTypes_ShouldThrowException()` | PasswordHelperTests |
| 322 | public | ` public void GenerateSecurePassword_MultipleCalls_ShouldGenerateDifferentPasswords()` | PasswordHelperTests |
| 333 | public | ` public void GenerateSecurePassword_WithSpecificTypes_ShouldContainRequiredTypes()` | PasswordHelperTests |
| 351 | public | ` public void PasswordValidationResult_DefaultConstructor_ShouldInitializeCorrectly()` | PasswordHelperTests |
| 365 | public | ` public void PasswordStrength_Values_ShouldHaveCorrectOrder()` | PasswordHelperTests |
| 380 | public | ` public void GenerateTemporaryPassword_ReturnsValidPassword()` | PasswordHelperTests |
| 392 | public | ` public void GenerateTemporaryPassword_CalledMultipleTimes_ReturnsUniquePasswords()` | PasswordHelperTests |
| 406 | public | ` public void GenerateSalt_WithDefaultLength_ReturnsValidSalt()` | PasswordHelperTests |
| 417 | public | ` public void GenerateSalt_WithCustomLength_ReturnsValidSalt()` | PasswordHelperTests |
| 20 | public | ` public void Validate_WithValidRequest_ShouldPass()` | LoginRequestValidatorTests |
| 40 | public | ` public void Validate_WithVariousValidInputs_ShouldPass(string username, string password)` | LoginRequestValidatorTests |
| 64 | public | ` public void Validate_WithEmptyUsername_ShouldFail(string? username)` | LoginRequestValidatorTests |
| 82 | public | ` public void Validate_WithUsernameTooLong_ShouldFail()` | LoginRequestValidatorTests |
| 100 | public | ` public void Validate_WithUsernameAtMaxLength_ShouldPass()` | LoginRequestValidatorTests |
| 124 | public | ` public void Validate_WithEmptyPassword_ShouldFail(string? password)` | LoginRequestValidatorTests |
| 144 | public | ` public void Validate_WithPasswordTooShort_ShouldFail(string password)` | LoginRequestValidatorTests |
| 162 | public | ` public void Validate_WithPasswordAtMinLength_ShouldPass()` | LoginRequestValidatorTests |
| 183 | public | ` public void Validate_ShouldReturnCorrectErrorMessages()` | LoginRequestValidatorTests |
| 20 | public | ` public void Validate_WithValidInput_ShouldPass()` | FormulaInputDtoValidatorTests |
| 33 | public | ` public void Validate_WithMinimalValidInput_ShouldPass()` | FormulaInputDtoValidatorTests |
| 65 | public | ` public void Validate_WithEmptyName_ShouldFail(string? name)` | FormulaInputDtoValidatorTests |
| 80 | public | ` public void Validate_WithNameTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 95 | public | ` public void Validate_WithNameAtMaxLength_ShouldPass()` | FormulaInputDtoValidatorTests |
| 113 | public | ` public void Validate_WithEffectTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 128 | public | ` public void Validate_WithDescriptionTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 143 | public | ` public void Validate_WithUsageTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 158 | public | ` public void Validate_WithIndicationsTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 173 | public | ` public void Validate_WithRemarkTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 192 | public | ` public void Validate_WithEmptyHerbs_ShouldFail()` | FormulaInputDtoValidatorTests |
| 207 | public | ` public void Validate_WithNullHerbs_ShouldFail()` | FormulaInputDtoValidatorTests |
| 222 | public | ` public void Validate_WithValidHerbs_ShouldPass()` | FormulaInputDtoValidatorTests |
| 244 | public | ` public void Validate_HerbItem_WithEmptyHerbName_ShouldFail()` | FormulaInputDtoValidatorTests |
| 262 | public | ` public void Validate_HerbItem_WithHerbNameTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 280 | public | ` public void Validate_HerbItem_WithZeroDosage_ShouldFail()` | FormulaInputDtoValidatorTests |
| 298 | public | ` public void Validate_HerbItem_WithDosageOver1000_ShouldFail()` | FormulaInputDtoValidatorTests |
| 316 | public | ` public void Validate_HerbItem_WithEmptyUnit_ShouldFail()` | FormulaInputDtoValidatorTests |
| 334 | public | ` public void Validate_HerbItem_WithUnitTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 352 | public | ` public void Validate_HerbItem_WithProcessingMethodTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 370 | public | ` public void Validate_HerbItem_WithUsageTooLong_ShouldFail()` | FormulaInputDtoValidatorTests |
| 392 | public | ` public void Validate_ShouldReturnCorrectErrorMessages()` | FormulaInputDtoValidatorTests |
| 414 | private | ` private static FormulaInputDto CreateValidFormulaInputDto()` | FormulaInputDtoValidatorTests |
| 21 | public | ` public void Validate_WithValidInput_ShouldPass()` | HerbInputDtoValidatorTests |
| 34 | public | ` public void Validate_WithMinimalValidInput_ShouldPass()` | HerbInputDtoValidatorTests |
| 59 | public | ` public void Validate_WithEmptyName_ShouldFail(string? name)` | HerbInputDtoValidatorTests |
| 74 | public | ` public void Validate_WithNameTooLong_ShouldFail()` | HerbInputDtoValidatorTests |
| 88 | public | ` public void Validate_WithNameAtMaxLength_ShouldPass()` | HerbInputDtoValidatorTests |
| 109 | public | ` public void Validate_WithEmptyUnit_ShouldFail(string? unit)` | HerbInputDtoValidatorTests |
| 124 | public | ` public void Validate_WithUnitTooLong_ShouldFail()` | HerbInputDtoValidatorTests |
| 143 | public | ` public void Validate_WithValidUnit_ShouldPass(string unit)` | HerbInputDtoValidatorTests |
| 161 | public | ` public void Validate_WithZeroPrice_ShouldFail()` | HerbInputDtoValidatorTests |
| 176 | public | ` public void Validate_WithNegativePrice_ShouldFail()` | HerbInputDtoValidatorTests |
| 191 | public | ` public void Validate_WithPriceOverMax_ShouldFail()` | HerbInputDtoValidatorTests |
| 211 | public | ` public void Validate_WithValidPrice_ShouldPass(decimal price)` | HerbInputDtoValidatorTests |
| 225 | public | ` public void Validate_WithPriceAtMaxValue_ShouldPass()` | HerbInputDtoValidatorTests |
| 243 | public | ` public void Validate_WithPinYinCodeTooLong_ShouldFail()` | HerbInputDtoValidatorTests |
| 258 | public | ` public void Validate_WithCategoryTooLong_ShouldFail()` | HerbInputDtoValidatorTests |
| 273 | public | ` public void Validate_WithOriginTooLong_ShouldFail()` | HerbInputDtoValidatorTests |
| 288 | public | ` public void Validate_WithSpecTooLong_ShouldFail()` | HerbInputDtoValidatorTests |
| 303 | public | ` public void Validate_WithEffectTooLong_ShouldFail()` | HerbInputDtoValidatorTests |
| 318 | public | ` public void Validate_WithUsageTooLong_ShouldFail()` | HerbInputDtoValidatorTests |
| 333 | public | ` public void Validate_WithRemarkTooLong_ShouldFail()` | HerbInputDtoValidatorTests |
| 352 | public | ` public void Validate_ShouldReturnCorrectErrorMessages()` | HerbInputDtoValidatorTests |
| 376 | private | ` private static HerbInputDto CreateValidHerbInputDto()` | HerbInputDtoValidatorTests |
| 21 | public | ` public void Validate_WithValidInput_ShouldPass()` | MedicalCaseInputDtoValidatorTests |
| 34 | public | ` public void Validate_WithEmptyOptionalRemark_ShouldPass()` | MedicalCaseInputDtoValidatorTests |
| 51 | public | ` public void Validate_WithEmptyPatientId_ShouldFail()` | MedicalCaseInputDtoValidatorTests |
| 66 | public | ` public void Validate_WithValidPatientId_ShouldPass()` | MedicalCaseInputDtoValidatorTests |
| 84 | public | ` public void Validate_WithEmptyUserId_ShouldFail()` | MedicalCaseInputDtoValidatorTests |
| 99 | public | ` public void Validate_WithValidUserId_ShouldPass()` | MedicalCaseInputDtoValidatorTests |
| 117 | public | ` public void Validate_ShouldReturnCorrectErrorMessages()` | MedicalCaseInputDtoValidatorTests |
| 139 | private | ` private static MedicalCaseInputDto CreateValidMedicalCaseInputDto()` | MedicalCaseInputDtoValidatorTests |
| 22 | public | ` public void Validate_WithValidInput_ShouldPass()` | PatientInputDtoValidatorTests |
| 35 | public | ` public void Validate_WithMinimalValidInput_ShouldPass()` | PatientInputDtoValidatorTests |
| 53 | public | ` public void Validate_WithEmptyOptionalFields_ShouldPass()` | PatientInputDtoValidatorTests |
| 78 | public | ` public void Validate_WithEmptyName_ShouldFail(string? name)` | PatientInputDtoValidatorTests |
| 93 | public | ` public void Validate_WithNameTooLong_ShouldFail()` | PatientInputDtoValidatorTests |
| 108 | public | ` public void Validate_WithNameAtMaxLength_ShouldPass()` | PatientInputDtoValidatorTests |
| 129 | public | ` public void Validate_WithValidGender_ShouldPass(Gender gender)` | PatientInputDtoValidatorTests |
| 143 | public | ` public void Validate_WithInvalidGender_ShouldFail()` | PatientInputDtoValidatorTests |
| 162 | public | ` public void Validate_WithFutureBirthDate_ShouldFail()` | PatientInputDtoValidatorTests |
| 177 | public | ` public void Validate_WithValidBirthDate_ShouldPass()` | PatientInputDtoValidatorTests |
| 191 | public | ` public void Validate_WithTodayBirthDate_ShouldPass()` | PatientInputDtoValidatorTests |
| 205 | public | ` public void Validate_WithNullBirthDate_ShouldPass()` | PatientInputDtoValidatorTests |
| 223 | public | ` public void Validate_WithInvalidIdNumber_ShouldFail()` | PatientInputDtoValidatorTests |
| 241 | public | ` public void Validate_With18DigitIdNumber_ShouldPass(string idNumber)` | PatientInputDtoValidatorTests |
| 258 | public | ` public void Validate_WithInvalidIdNumberFormat_ShouldFail(string idNumber)` | PatientInputDtoValidatorTests |
| 276 | public | ` public void Validate_WithInvalidPhoneNumber_ShouldFail()` | PatientInputDtoValidatorTests |
| 295 | public | ` public void Validate_WithValidPhoneNumber_ShouldPass(string phoneNumber)` | PatientInputDtoValidatorTests |
| 313 | public | ` public void Validate_WithInvalidPhoneFormat_ShouldFail(string phoneNumber)` | PatientInputDtoValidatorTests |
| 331 | public | ` public void Validate_ShouldReturnCorrectErrorMessages()` | PatientInputDtoValidatorTests |
| 355 | private | ` private static PatientInputDto CreateValidPatientInputDto()` | PatientInputDtoValidatorTests |
| 20 | public | ` public void Validate_WithValidInput_ShouldPass()` | PrescriptionInputDtoValidatorTests |
| 33 | public | ` public void Validate_WithMinimalValidInput_ShouldPass()` | PrescriptionInputDtoValidatorTests |
| 59 | public | ` public void Validate_WithEmptyMedicalCaseId_OnCreate_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 75 | public | ` public void Validate_WithEmptyMedicalCaseId_OnUpdate_ShouldPass()` | PrescriptionInputDtoValidatorTests |
| 94 | public | ` public void Validate_WithReferencedFormulasTooLong_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 109 | public | ` public void Validate_WithAdviceTooLong_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 124 | public | ` public void Validate_WithRemarkTooLong_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 146 | public | ` public void Validate_WithInvalidDiscount_ShouldFail(decimal discount)` | PrescriptionInputDtoValidatorTests |
| 165 | public | ` public void Validate_WithValidDiscount_ShouldPass(decimal discount)` | PrescriptionInputDtoValidatorTests |
| 183 | public | ` public void Validate_WithZeroDosageCount_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 198 | public | ` public void Validate_WithNegativeDosageCount_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 213 | public | ` public void Validate_WithDosageCountOver100_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 233 | public | ` public void Validate_WithValidDosageCount_ShouldPass(int dosageCount)` | PrescriptionInputDtoValidatorTests |
| 251 | public | ` public void Validate_WithEmptyItems_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 265 | public | ` public void Validate_WithNullItems_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 280 | public | ` public void Validate_WithValidItems_ShouldPass()` | PrescriptionInputDtoValidatorTests |
| 297 | public | ` public void Validate_Item_WithEmptyHerbId_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 315 | public | ` public void Validate_Item_WithZeroDosage_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 333 | public | ` public void Validate_Item_WithDosageOver1000_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 351 | public | ` public void Validate_Item_WithUsageTooLong_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 369 | public | ` public void Validate_Item_WithRemarkTooLong_ShouldFail()` | PrescriptionInputDtoValidatorTests |
| 391 | public | ` public void Validate_ShouldReturnCorrectErrorMessages()` | PrescriptionInputDtoValidatorTests |
| 417 | private | ` private static PrescriptionInputDto CreateValidPrescriptionInputDto()` | PrescriptionInputDtoValidatorTests |
| 18 | public | ` public CorrelationIdMiddlewareTests()` | CorrelationIdMiddlewareTests |
| 24 | public | ` public async Task InvokeAsync_GeneratesNewCorrelationId_WhenNotProvided()` | CorrelationIdMiddlewareTests |
| 48 | public | ` public async Task InvokeAsync_UsesExistingCorrelationId_WhenProvided()` | CorrelationIdMiddlewareTests |
| 73 | public | ` public async Task InvokeAsync_StoresCorrelationIdInHttpContext()` | CorrelationIdMiddlewareTests |
| 96 | public | ` public void GetCorrelationId_ReturnsNA_WhenNotSet()` | CorrelationIdMiddlewareTests |
| 109 | public | ` public void CorrelationIdHeader_HasCorrectValue()` | CorrelationIdMiddlewareTests |
| 116 | public | ` public async Task InvokeAsync_GeneratesShortCorrelationId()` | CorrelationIdMiddlewareTests |
| 15 | public | ` public void RegisterInfrastructureServices_WithoutConnectionString_ShouldThrowInvalidOperationException()` | DatabaseServiceCollectionExtensionsTests |
| 38 | public | ` public void RegisterInfrastructureServices_WithConnectionStringInDatabaseSection_ShouldNotThrow()` | DatabaseServiceCollectionExtensionsTests |
### LYBT.Tools.PasswordHashGenerator

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 11 | default(private) | ` static int Main(string[] args)` | Program |
| 74 | default(private) | ` static void ShowHelp()` | Program |
| 88 | default(private) | ` static string? GetDefaultAdminPassword()` | Program |
| 115 | default(private) | ` static void GeneratePasswordHash(string password, UserRole role)` | Program |
### LYBT.WebAPI

| 行号 | 可见性 | 签名 | 类 |
|------|--------|------|----|
| 17 | public | ` public static IServiceCollection AddProblemDetailsConfiguration(this IServiceCollection services)` | ProblemDetailsConfiguration |
| 56 | private | ` private static string MapStatusCodeToSeverity(int statusCode) => (statusCode switch { >= 500 => ErrorSeverity.Critical, >= 400 => ErrorSeverity.Warni…` | ProblemDetailsConfiguration |
| 67 | private | ` private static string GetProblemTypeUri(int statusCode) => ProblemTypeUris.GetByStatusCode(statusCode);` | ProblemDetailsConfiguration |
| 26 | public | ` public AuthController( ISender sender, ILogger<AuthController> logger)` | AuthController |
| 39 | public | ` public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)` | AuthController |
| 68 | public | ` public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequest request)` | AuthController |
| 89 | public | ` public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request)` | AuthController |
| 111 | public | ` public async Task<IActionResult> AutoLoginAsync([FromBody] AutoLoginRequest request)` | AuthController |
| 125 | public | ` public async Task<IActionResult> ValidateTokenFromHeaderAsync()` | AuthController |
| 22 | public | ` public ConfigurationController( ISystemConfigurationService configurationService, ILogger<ConfigurationController> logger)` | ConfigurationController |
| 35 | public | ` public async Task<IActionResult> GetConfiguration(CancellationToken cancellationToken)` | ConfigurationController |
| 48 | public | ` public async Task<IActionResult> GetValue(string key, CancellationToken cancellationToken)` | ConfigurationController |
| 61 | public | ` public async Task<IActionResult> SetValue(string key, [FromBody] string value, CancellationToken cancellationToken)` | ConfigurationController |
| 77 | public | ` public async Task<IActionResult> UpdateConfiguration([FromBody] Dictionary<string, string> settings, CancellationToken cancellationToken)` | ConfigurationController |
| 93 | public | ` public async Task<IActionResult> ValidateProduction(CancellationToken cancellationToken)` | ConfigurationController |
| 23 | public | ` public DeployController(IHostApplicationLifetime lifetime, ILogger<DeployController> logger)` | DeployController |
| 31 | public | ` public async Task<IActionResult> Upload(IFormFile file)` | DeployController |
| 54 | public | ` public IActionResult Restart([FromBody] RestartConfirmDto? request)` | DeployController |
| 25 | public | ` public DiagnosticsController( LoggingLevelManager loggingLevelManager, ILogger<DiagnosticsController> logger)` | DiagnosticsController |
| 38 | public | ` public IActionResult GetLoggingStatus()` | DiagnosticsController |
| 60 | public | ` public IActionResult EnableDebugMode([FromBody] EnableDebugModeRequest? request)` | DiagnosticsController |
| 92 | public | ` public IActionResult DisableDebugMode()` | DiagnosticsController |
| 110 | public | ` public IActionResult SetLoggingLevel([FromBody] SetLoggingLevelRequest request)` | DiagnosticsController |
| 28 | public | ` public FormulasController(ISender sender, ILogger<FormulasController> logger, IFormulaService formulaService)` | FormulasController |
| 39 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | FormulasController |
| 59 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | FormulasController |
| 80 | public | ` public async Task<IActionResult> Create([FromBody] FormulaInputDto input, CancellationToken ct)` | FormulasController |
| 101 | public | ` public async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto input, CancellationToken ct)` | FormulasController |
| 126 | public | ` public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)` | FormulasController |
| 153 | public | ` public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)` | FormulasController |
| 179 | public | ` public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)` | FormulasController |
| 203 | public | ` public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | FormulasController |
| 217 | public | ` public async Task<IActionResult> Import([FromBody] FormulaBatchImportInputDto request, CancellationToken ct)` | FormulasController |
| 243 | public | ` public async Task<IActionResult> GetPendingValidation(CancellationToken ct)` | FormulasController |
| 261 | public | ` public async Task<IActionResult> ValidateHerb( Guid formulaId, Guid herbItemId, [FromBody] ValidateFormulaHerbInputDto request, CancellationToken ct)` | FormulasController |
| 291 | public | ` public async Task<IActionResult> BatchEnable( [FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)` | FormulasController |
| 311 | public | ` public async Task<IActionResult> BatchDisable( [FromBody] BatchDeleteInputDto dto, CancellationToken ct = default)` | FormulasController |
| 27 | public | ` public HealthController(IHealthCheckService healthCheckService, ILogger<HealthController> logger)` | HealthController |
| 51 | public | ` public IActionResult Ping()` | HealthController |
| 59 | private | ` private IActionResult BuildHealthStatus(string status)` | HealthController |
| 76 | public | ` public async Task<IActionResult> GetDetailedHealth()` | HealthController |
| 28 | public | ` public HerbsController(ISender sender, ILogger<HerbsController> logger, IHerbService herbService)` | HerbsController |
| 39 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | HerbsController |
| 57 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | HerbsController |
| 75 | public | ` public async Task<IActionResult> Create([FromBody] HerbInputDto input, CancellationToken ct)` | HerbsController |
| 98 | public | ` public async Task<IActionResult> Update(Guid id, [FromBody] HerbInputDto input, CancellationToken ct)` | HerbsController |
| 126 | public | ` public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)` | HerbsController |
| 153 | public | ` public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)` | HerbsController |
| 179 | public | ` public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)` | HerbsController |
| 199 | public | ` public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | HerbsController |
| 214 | public | ` public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken ct)` | HerbsController |
| 237 | public | ` public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)` | HerbsController |
| 253 | public | ` public async Task<IActionResult> BatchCheckReference( [FromBody] HerbBatchCheckReferenceInputDto dto, CancellationToken ct)` | HerbsController |
| 270 | public | ` public async Task<IActionResult> BatchEnable( [FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | HerbsController |
| 290 | public | ` public async Task<IActionResult> BatchDisable( [FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | HerbsController |
| 29 | public | ` public MedicalCasesController( ISender sender, ILogger<MedicalCasesController> logger, IMedicalCaseCommandService medicalCaseCommandService, IMedical…` | MedicalCasesController |
| 45 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | MedicalCasesController |
| 72 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | MedicalCasesController |
| 93 | public | ` public async Task<IActionResult> Create([FromBody] MedicalCaseInputDto input, CancellationToken ct)` | MedicalCasesController |
| 123 | public | ` public async Task<IActionResult> Update( Guid id, [FromBody] MedicalCaseInputDto input, CancellationToken ct)` | MedicalCasesController |
| 154 | public | ` public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)` | MedicalCasesController |
| 175 | public | ` public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | MedicalCasesController |
| 200 | public | ` public override async Task<IActionResult> SetPrescriptionFlag( Guid id, [FromBody] SetPrescriptionFlagRequest request, CancellationToken ct)` | MedicalCasesController |
| 224 | public | ` public override async Task<IActionResult> RecordPrint( Guid id, [FromBody] RecordPrintRequest request, CancellationToken ct)` | MedicalCasesController |
| 249 | public | ` public async Task<IActionResult> UpdateStatus( Guid id, [FromBody] MedicalCaseStatusInputDto request, CancellationToken ct)` | MedicalCasesController |
| 281 | public | ` public async Task<IActionResult> CloseMedicalCase(Guid id, CancellationToken ct)` | MedicalCasesController |
| 303 | public | ` public async Task<IActionResult> Suspend( Guid id, [FromBody] ConsultationInputDto? request = null, CancellationToken ct = default)` | MedicalCasesController |
| 326 | public | ` public async Task<IActionResult> CancelMedicalCase( Guid id, [FromBody] CancelMedicalCaseRequest? request = null, CancellationToken ct = default)` | MedicalCasesController |
| 28 | public | ` public PatientsController(ISender sender, ILogger<PatientsController> logger, IPatientService patientService)` | PatientsController |
| 39 | public | ` public override async Task<IActionResult> GetList( [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, Cance…` | PatientsController |
| 61 | public | ` public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)` | PatientsController |
| 78 | public | ` public async Task<IActionResult> Create([FromBody] PatientInputDto input, CancellationToken ct)` | PatientsController |
| 99 | public | ` public async Task<IActionResult> Update(Guid id, [FromBody] PatientInputDto input, CancellationToken ct)` | PatientsController |
| 126 | public | ` public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)` | PatientsController |
| 153 | public | ` public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)` | PatientsController |
| 177 | public | ` public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)` | PatientsController |
| 201 | public | ` public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)` | PatientsController |
| 216 | public | ` public async Task<IActionResult> BatchImport([FromBody] PatientBatchImportInputDto request, CancellationToken ct)` | PatientsController |
| 239 | public | ` public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)` | PatientsController |
| 257 | public | ` public async Task<IActionResult> GetByIdNumber(string idNumber, CancellationToken ct)` | PatientsController |
| 270 | public | ` public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto dto, CancellationToken ct)` | PatientsController |
| 23 | public | ` public RegistrationsController( ISender sender, ILogger<RegistrationsController> logger)` | RegistrationsController |
| 36 | public | ` public override async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto, CancellationToken ct)` | RegistrationsController |
| 58 | public | ` public async Task<IActionResult> Create([FromBody] RegistrationInputDto input, CancellationToken ct)` | RegistrationsController |
| 77 | public | ` public override async Task<IActionResult> StartVisit(Guid id, CancellationToken ct)` | RegistrationsController |
| 95 | public | ` public override async Task<IActionResult> Cancel(Guid id, CancellationToken ct)` | RegistrationsController |
| 24 | public | ` public ReportsController( IReportService reportService, ILogger<ReportsController> logger)` | ReportsController |
| 37 | public | ` public async Task<IActionResult> GetDailyIncome( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken canc…` | ReportsController |
| 55 | public | ` public async Task<IActionResult> GetDailyConsultations( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationTok…` | ReportsController |
| 73 | public | ` public async Task<IActionResult> GetDailyHerbs( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToken cance…` | ReportsController |
| 91 | public | ` public async Task<IActionResult> GetIncomeTrend( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] ReportGran…` | ReportsController |
| 110 | public | ` public async Task<IActionResult> GetConsultationTrend( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] Repo…` | ReportsController |
| 129 | public | ` public async Task<IActionResult> GetDoctorPerformance( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, CancellationToke…` | ReportsController |
| 147 | public | ` public async Task<IActionResult> GetHerbRanking( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] int top = …` | ReportsController |
| 166 | public | ` public async Task<IActionResult> GetPatientFlow( [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] ReportGran…` | ReportsController |
| 17 | public | ` public UsersController( ISender sender, ILogger<UsersController> logger, IUserService userService)` | UsersController |
| 20 | public | ` public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)` | ApiServiceCollectionExtensions |
| 44 | public | ` public static IServiceCollection RegisterApiServices(this IServiceCollection services, IConfiguration configuration)` | ApiServiceCollectionExtensions |
| 166 | public | ` public static IServiceCollection ConfigureRateLimiting( this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environme…` | ApiServiceCollectionExtensions |
| 21 | public | ` public static IServiceCollection RegisterAuthenticationServices( this IServiceCollection services, IConfiguration configuration)` | AuthenticationServiceCollectionExtensions |
| 24 | public | ` public static IServiceCollection RegisterInfrastructureServices( this IServiceCollection services, IConfiguration configuration)` | DatabaseServiceCollectionExtensions |
| 16 | public | ` public static IHostBuilder ConfigureEnvironmentAwareHosting(this IHostBuilder hostBuilder)` | EnvironmentAwareHosting |
| 31 | public | ` public static void DisplayDevelopmentStartupInfo(this WebApplication app)` | EnvironmentAwareHosting |
| 45 | private | ` private static void DisplayDevelopmentConsoleHeader()` | EnvironmentAwareHosting |
| 67 | private | ` private static void DisplayStartupStatus(WebApplication app)` | EnvironmentAwareHosting |
| 101 | public | ` public static IApplicationBuilder UseDevelopmentRequestLogging(this IApplicationBuilder app)` | EnvironmentAwareHosting |
| 29 | public | ` public static LoggerConfiguration AddMSSqlServerSinkWithColumnOptions( this LoggerConfiguration loggerConfiguration, string? connectionString)` | SerilogMSSqlServerExtensions |
| 58 | private | ` private static ColumnOptions BuildColumnOptions()` | SerilogMSSqlServerExtensions |
| 33 | public | ` public static IServiceCollection RegisterAllApplicationServices( this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment …` | ServiceCollectionExtensions |
| 85 | private | ` private static IServiceCollection RegisterBusinessModules( this IServiceCollection services, IConfiguration configuration)` | ServiceCollectionExtensions |
| 124 | private | ` private static IServiceCollection RegisterControllerServices( this IServiceCollection services, IConfiguration configuration)` | ServiceCollectionExtensions |
| 218 | private | ` private static IServiceCollection ConfigurePerformanceOptimizations( this IServiceCollection services, IConfiguration configuration)` | ServiceCollectionExtensions |
| 256 | private | ` private static IServiceCollection AddSecurityServices( this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environmen…` | ServiceCollectionExtensions |
| 16 | public | ` public static async Task InitializeAllApplicationServices(this WebApplication app)` | UnifiedApplicationInitialization |
| 43 | private | ` private static async Task InitializeDatabaseAsync(this WebApplication app, IServiceScope scope)` | UnifiedApplicationInitialization |
| 72 | private | ` private static void InitializeConfigurationServices(this WebApplication app, IServiceScope scope)` | UnifiedApplicationInitialization |
| 134 | private | ` private static async Task LogApplicationStartupAsync(this WebApplication app, IServiceScope scope)` | UnifiedApplicationInitialization |
| 156 | private | ` private static Task HandleInitializationErrorAsync(this WebApplication app, IServiceScope scope, Exception ex)` | UnifiedApplicationInitialization |
| 175 | public | ` public static async Task DisplayDatabaseStatusAsync(this WebApplication app)` | UnifiedApplicationInitialization |
| 20 | public | ` public static WebApplication ConfigureAllMiddleware(this WebApplication app)` | UnifiedMiddlewareConfiguration |
| 185 | private | ` private static WebApplication ConfigureSwaggerMiddleware(this WebApplication app)` | UnifiedMiddlewareConfiguration |
| 15 | public | ` public ApiLoggingFilter(ILogger<ApiLoggingFilter> logger)` | ApiLoggingFilter |
| 20 | public | ` public async Task OnActionExecutionAsync( ActionExecutingContext context, ActionExecutionDelegate next)` | ApiLoggingFilter |
| 69 | private | ` private static string SanitizeParameters(IDictionary<string, object?> parameters)` | ApiLoggingFilter |
| 80 | private | ` private static string SanitizeValue(string key, object? value)` | ApiLoggingFilter |
| 16 | public | ` public DatabaseStartupDiagnostics( ILogger<DatabaseStartupDiagnostics> logger, IOptions<DatabaseOptions> dbOptions)` | DatabaseStartupDiagnostics |
| 24 | public | ` public async Task StartAsync(CancellationToken cancellationToken)` | DatabaseStartupDiagnostics |
| 121 | public | ` public Task StopAsync(CancellationToken cancellationToken)` | DatabaseStartupDiagnostics |
| 16 | public | ` public SqlServerHealthCheck(IOptions<DatabaseOptions> dbOptions)` | SqlServerHealthCheck |
| 21 | public | ` public async Task<HealthCheckResult> CheckHealthAsync( HealthCheckContext context, CancellationToken cancellationToken = default)` | SqlServerHealthCheck |
| 99 | private | ` private string GetSuggestion(int errorCode)` | SqlServerHealthCheck |
| 14 | public | ` public ClaimsNormalizationMiddleware( RequestDelegate next, ILogger<ClaimsNormalizationMiddleware> logger)` | ClaimsNormalizationMiddleware |
| 22 | public | ` public async Task InvokeAsync(HttpContext context)` | ClaimsNormalizationMiddleware |
| 77 | private | ` private static string? GetUserIdClaim(IEnumerable<Claim> claims)` | ClaimsNormalizationMiddleware |
| 88 | private | ` private static string? GetUserNameClaim(IEnumerable<Claim> claims)` | ClaimsNormalizationMiddleware |
| 100 | private | ` private static string? GetRoleClaim(IEnumerable<Claim> claims)` | ClaimsNormalizationMiddleware |
| 111 | private | ` private static void EnsureClaim(List<Claim> newClaims, IEnumerable<Claim> existingClaims, string claimType, string claimValue)` | ClaimsNormalizationMiddleware |
| 131 | public | ` public static IApplicationBuilder UseClaimsNormalization(this IApplicationBuilder builder)` | ClaimsNormalizationMiddlewareExtensions |
| 32 | public | ` public CorrelationIdMiddleware( RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)` | CorrelationIdMiddleware |
| 40 | public | ` public async Task InvokeAsync(HttpContext context)` | CorrelationIdMiddleware |
| 88 | public | ` public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)` | CorrelationIdMiddlewareExtensions |
| 98 | public | ` public static string GetCorrelationId(this HttpContext context)` | CorrelationIdMiddlewareExtensions |
| 12 | public | ` public SecurityHeadersMiddleware( RequestDelegate next, IWebHostEnvironment environment, ILogger<SecurityHeadersMiddleware> logger)` | SecurityHeadersMiddleware |
| 22 | public | ` public async Task InvokeAsync(HttpContext context)` | SecurityHeadersMiddleware |
| 30 | private | ` private void AddSecurityHeaders(HttpContext context)` | SecurityHeadersMiddleware |
| 72 | private | ` private static string GetProductionCspPolicy()` | SecurityHeadersMiddleware |
| 101 | private | ` private static string GetDevelopmentCspPolicy()` | SecurityHeadersMiddleware |
| 131 | public | ` public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)` | SecurityHeadersMiddlewareExtensions |
| 37 | public | ` public static async Task Main(string[] args)` | Program |
| 279 | private | ` private static void ValidateDefaultPasswordConfiguration(IConfiguration configuration, IWebHostEnvironment environment)` | Program |

## 附录：扫描脚本（内嵌，可复现）

```powershell
# LYBTZYZS 方法级统计基线扫描脚本 (S0) — 只读，不修改任何代码
# 口径：源码级正则 + Roslyn 采样校准（误差率见报告）
# 用法: pwsh -NoProfile -File scan.ps1
$ErrorActionPreference = 'Stop'
$root = 'D:\source\repos\LYBTZYZS'

# ---------- 1. 项目映射（sln 提取 csproj 相对路径 -> 项目名） ----------
$slnRaw = Get-Content "$root\LYBTZYZS.sln" -Raw
$projRe = [regex]'"([^"]+)", "([^"]+\.csproj)"'
$projByDir = @{}   # 项目目录(相对) -> 项目名
foreach ($m in $projRe.Matches($slnRaw)) {
    $name = $m.Groups[1].Value
    $rel = $m.Groups[2].Value
    $dir = [System.IO.Path]::GetDirectoryName($rel) -replace '\\', '/'
    $projByDir[$dir] = $name
}
# Tools/PasswordHashGenerator 不在 sln 中，手工补记
$projByDir['src/Tools/PasswordHashGenerator/PasswordHashGenerator'] = 'LYBT.Tools.PasswordHashGenerator'
$sortedProjDirs = $projByDir.Keys | Sort-Object { -$_.Length }

function Get-ProjectOf($file) {
    $rel = [System.IO.Path]::GetRelativePath($root, $file) -replace '\\', '/'
    foreach ($d in $sortedProjDirs) {
        if ($rel.StartsWith("$d/", [System.StringComparison]::OrdinalIgnoreCase)) { return $projByDir[$d] }
    }
    return 'UNKNOWN'
}

function Get-RelPath($file) {
    return [System.IO.Path]::GetRelativePath($root, $file) -replace '\\', '/'
}

# ---------- 2. 收集 .cs 文件（排除 bin/obj/Migrations/generated） ----------
$excludeRe = [regex]'(\\|\/)(bin|obj|Migrations)(\\|\/)'
$allCs = Get-ChildItem -Path "$root\src", "$root\tests" -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '(\\|\/)(bin|obj|Migrations)(\\|\/)' }
Write-Host ("[scan] cs files: " + $allCs.Count)

# ---------- 3. 逐文件解析（并行） ----------
# 每方法: {proj, file, line, type, typeKind, name, ret, visibility, sig, paramList, isCtor, isStatic, isOverride}
# 每类型: {proj, file, line, name, kind, topLevel}
$modsRe = '(?:public|private|protected|internal|static|virtual|override|abstract|sealed|partial|async|readonly|new|extern|unsafe|file)\s+'
$typeStartRe = [regex]('^\s*(?:(?:public|private|protected|internal)\s+)?(?:' + $modsRe + ')*(?<kind>record\s+(?:struct|class)|record|class|interface|struct|enum)\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*(?=\{|;|:|\bwhere\b|\(|$)')
$methodStartRe = [regex]('^\s*(?<mods>(?:' + $modsRe + ')*)(?<ret>[\w<>\[\],\.\?\s\*]+?)\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*\(')
$notMethodKw = [regex]'^(?:if|for|foreach|while|switch|catch|using|return|await|throw|yield|var|new|lock|fixed|sizeof|typeof|nameof|checked|unchecked|default|base|this|delegate|event)\b'

function Parse-File($f) {
    $proj = Get-ProjectOf $f.FullName
    $lines = [System.IO.File]::ReadAllLines($f.FullName)
    $types = @(); $methods = @()
    $typeStack = @()          # 类型声明行号（最近的在尾部）
    $depth = 0                # 当前花括号深度
    $curType = $null           # {line, name, kind, bodyDepth}
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $raw = $lines[$i]
        $t = $raw.TrimStart()
        if ($t -eq '' -or $t.StartsWith('//')) { continue }
        # ---- 类型声明检测 ----
        $tm = $typeStartRe.Match($raw)
        if ($tm.Success) {
            $kindRaw = ($tm.Groups['kind'].Value -replace '\s+', ' ')
            $tname = $tm.Groups['name'].Value
            $preDepth = $depth
            $types += [pscustomobject]@{ proj=$proj; file=(Get-RelPath $f.FullName); line=$i+1; name=$tname; kind=$kindRaw; topLevel=($typeStack.Count -eq 0); typeLine=$i+1 }
            # 括号更新（类型声明行内可能含 { 或 record 主构造 ( ... ) 无 {）
            $inStr = $false
            foreach ($ch in $raw.ToCharArray()) {
                if ($ch -eq '"') { $inStr = -not $inStr }
                elseif (-not $inStr -and $ch -eq '{') { $depth++ }
                elseif (-not $inStr -and $ch -eq '}') { $depth-- }
            }
            # 类型体深度 = 声明行处理前 depth + 1（类体必在声明下一层）
            $curType = @{ line = $i+1; name = $tname; kind = $kindRaw; bodyDepth = $preDepth + 1 }
            $typeStack += $i
            continue
        }
        # ---- 方法声明检测（仅在类型体内且深度 == 类型体深度） ----
        if ($curType -ne $null -and $depth -eq $curType.bodyDepth -and -not $notMethodKw.IsMatch($t)) {
            $mm = $methodStartRe.Match($raw)
            if ($mm.Success -and $mm.Groups['name'].Value -ne 'get' -and $mm.Groups['name'].Value -ne 'set') {
                $name = $mm.Groups['name'].Value
                $mods = $mm.Groups['mods'].Value
                $ret = $mm.Groups['ret'].Value.Trim()
                $isCtor = ($name -eq $curType.name)
                $isStatic = $mods -match '\bstatic\b'
                $isOverride = $mods -match '\boverride\b' -or $mods -match '\bvirtual\b' -or $mods -match '\babstract\b'
                # 构造函数/析构函数无返回类型，正则会把可见性吞进 ret 组 —— 单独从行首提取
                if ($isCtor -or $name -eq 'Finalize') {
                    $ctorVis = [regex]::Match($raw, '^\s*(?<v>(?:public|private|protected|internal)\s+)').Groups['v'].Value.Trim()
                    if ($ctorVis -eq '') { $ctorVis = 'default(private)' }
                    $vis = $ctorVis
                } else {
                    if ($mods -match '\bpublic\b') { $vis = 'public' }
                    elseif ($mods -match '\bprivate\b') { $vis = 'private' }
                    elseif ($mods -match '\bprotected\b' -and $mods -match '\binternal\b') { $vis = 'protected internal' }
                    elseif ($mods -match '\bprotected\b') { $vis = 'protected' }
                    elseif ($mods -match '\binternal\b') { $vis = 'internal' }
                    elseif ($curType.kind -match '^interface') { $vis = 'public (interface 默认)' }
                    else { $vis = 'default(private)' }
                }
                # ---- 跨行括号平衡收集完整签名 ----
                $sigLines = @($raw)
                $sig = $raw
                $open = 0; $inStr2 = $false
                foreach ($ch in $raw.ToCharArray()) {
                    if ($ch -eq '"') { $inStr2 = -not $inStr2 }
                    elseif (-not $inStr2 -and $ch -eq '(') { $open++ }
                    elseif (-not $inStr2 -and $ch -eq ')') { $open-- }
                }
                $j = $i
                while ($open -gt 0 -and $j -lt $lines.Count - 1) {
                    $j++
                    $nextLine = $lines[$j]
                    $sigLines += $nextLine
                    foreach ($ch in $nextLine.ToCharArray()) {
                        if ($ch -eq '"') { $inStr2 = -not $inStr2 }
                        elseif (-not $inStr2 -and $ch -eq '(') { $open++ }
                        elseif (-not $inStr2 -and $ch -eq ')') { $open-- }
                    }
                }
                $sig = ($sigLines -join ' ') -replace '\s+', ' '
                # 参数类型列表（括号内顶层逗号切分，取首词）
                $paramList = @()
                $popen = $sig.IndexOf('(')
                if ($popen -ge 0) {
                    $pclose = $sig.LastIndexOf(')')
                    if ($pclose -gt $popen) {
                        $inner = $sig.Substring($popen + 1, $pclose - $popen - 1)
                        if ($inner.Trim() -ne '') {
                            foreach ($part in Split-TopLevel $inner) {
                                $pw = $part.Trim() -split '\s+'
                                # 参数类型 = 第一个 token（去除 ref/out/in/params/this 修饰符与 [Attribute] 前缀）
                                if ($pw.Count -ge 1) {
                                    $ptype = $pw[0] -replace '^(ref|out|in|params|this)\s+', ''
                                    if ($ptype.StartsWith('[')) {
                                        $pi = 1
                                        while ($pi -lt $pw.Count -and $pw[$pi].StartsWith('[')) { $pi++ }
                                        if ($pi -lt $pw.Count) { $ptype = $pw[$pi] }
                                    }
                                    $paramList += $ptype
                                }
                            }
                        }
                    }
                }
                $methods += [pscustomobject]@{
                    proj=$proj; file=(Get-RelPath $f.FullName); line=$i+1; type=$curType.name; typeKind=$curType.kind;
                    name=$name; ret=$ret; visibility=$vis; sig=$sig; paramList=($paramList -join '|');
                    isCtor=$isCtor; isStatic=$isStatic; isOverride=$isOverride
                }
                # 方法签名行及跨行后的花括号深度更新（含方法体 { 若在签名行内）
                for ($k = $i; $k -le $j; $k++) {
                    foreach ($ch in $lines[$k].ToCharArray()) {
                        if ($ch -eq '"') { } # 简化：方法签名内字符串已由上面处理
                        elseif ($ch -eq '{') { $depth++ }
                        elseif ($ch -eq '}') { $depth-- }
                    }
                }
                $i = $j
                continue
            }
        }
        # ---- 普通行：花括号深度更新 ----
        $inStr3 = $false
        foreach ($ch in $raw.ToCharArray()) {
            if ($ch -eq '"') { $inStr3 = -not $inStr3 }
            elseif (-not $inStr3 -and $ch -eq '{') { $depth++ }
            elseif (-not $inStr3 -and $ch -eq '}') { $depth-- }
        }
    }
    return @{ types = $types; methods = $methods }
}

function Split-TopLevel($s) {
    $parts = @(); $cur = ''; $dep = 0; $qs = $false
    foreach ($ch in $s.ToCharArray()) {
        if ($ch -eq '"') { $qs = -not $qs; $cur += $ch; continue }
        if ($qs) { $cur += $ch; continue }
        if ($ch -eq '(' -or $ch -eq '<') { $dep++; $cur += $ch; continue }
        if ($ch -eq ')' -or $ch -eq '>') { $dep--; $cur += $ch; continue }
        if ($ch -eq ',' -and $dep -eq 0) { $parts += $cur; $cur = ''; continue }
        $cur += $ch
    }
    if ($cur.Trim() -ne '') { $parts += $cur }
    return $parts
}

$parsed = @(foreach ($f in $allCs) { Parse-File $f })

$allTypes = @(); $allMethods = @()
foreach ($r in $parsed) {
    $allTypes += $r.types
    $allMethods += $r.methods
}

Write-Host ("[scan] types: " + $allTypes.Count + ", methods: " + $allMethods.Count)

# ---------- 4. 按项目统计 ----------
$projStats = @{}
foreach ($p in ($allTypes + $allMethods | Select-Object -ExpandProperty proj -Unique)) {
    $ts = @($allTypes | Where-Object proj -eq $p)
    $ms = @($allMethods | Where-Object proj -eq $p)
    $pub = @($ms | Where-Object { $_.visibility -eq 'public' })
    $priv = @($ms | Where-Object { $_.visibility -ne 'public' })
    $projStats[$p] = [pscustomobject]@{
        proj=$p; types=$ts.Count; methods=$ms.Count; public=$pub.Count; private=$priv.Count;
        avgPerClass=if ($ts.Count -gt 0) { [math]::Round($ms.Count / $ts.Count, 2) } else { 0 }
    }
}

# ---------- 5. 输出 JSON ----------
$out = [pscustomobject]@{
    scannedFiles = $allCs.Count
    types = $allTypes
    methods = $allMethods
    projStats = @($projStats.Values | Sort-Object proj)
}
$out | ConvertTo-Json -Depth 6 | Set-Content -Path "$env:TEMP\lybt-method-audit\scan-result.json" -Encoding UTF8
Write-Host "[scan] done -> $env:TEMP\lybt-method-audit\scan-result.json"

```


