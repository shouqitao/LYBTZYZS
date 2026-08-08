# A-30-S0 方法级统计基线 — 死方法候选初筛

> 生成：Mimo Code（S0 只读审查）｜任务书：`task-a30-s0-method-baseline-2026-08-08.md`
> 基线 commit：`7edf020f5`｜生成时间：2026-08-09 02:10
> 性质：**机器初筛候选清单**——全仓 grep `方法名(` 调用计数 = 定义数（即除定义行外 0 调用）
> ⚠️ **全部候选均需 S1-S4 符号级复核后方可判定**：public 方法可能被反射/DI/MediatR/XAML 绑定/`nameof` 调用，grep 不可见

## 0. 初筛口径

| 项 | 说明 |
|----|------|
| 判定式 | 方法名全仓 `方法名(` 出现次数 == 该名字定义数（正则匹配含定义行自身）→ 除定义外 0 调用 |
| 范围 | **业务代码**（src/，排除 Tests 项目）；排除 `bin/obj/Migrations` |
| 已知盲区 | ① 反射调用（MediatR `Handle`/Prism `RegisterTypes`/XAML 绑定/`nameof`）grep 不可见；② 注释/字符串中的同名文本会计入调用计数（方向相反，只会漏报不会误报）|
| 框架反射模式 | `Handle`(MediatR)/`RegisterTypes`(Prism)/`OnInitialized`/`OnNavigatedTo`/`Convert`/`ConvertBack`(XAML) 等已知框架调用，**不视为真死方法**，单独标注 |
| 红线 | 本报告**不判定**应删除；全部候选标注"待符号级复核" |

## 1. 总览

- 死方法候选总数：**1095**（占业务方法 6118 的 17.9%）
- 可见性分布：public 715 / private 213 / protected 38 / internal 4 / 接口默认 0 / default 40
- 已知框架反射模式（Handle/RegisterTypes/生命周期/XAML 转换等）：**86** 项（不列入高置信）
- 非框架模式候选：**1009** 项（仍需符号级复核）

## 2. 高置信候选（private/protected/internal + 非框架模式）

> private/protected/internal 方法无反射调用面（本仓内），0 调用基本可确认死；仍列"待符号级复核"以防 `nameof`/partial 类同文件引用。

- 高置信候选数：**254**

| 项目 | 类 | 方法 | 行号 | 可见性 |
|------|----|------|------|--------|
| LYBT.Desktop.Admin | DeploymentViewModel | `SelectFile` | 36 | private |
| LYBT.Desktop.Admin | LogLevelControlViewModel | `SetLevelAsync` | 59 | private |
| LYBT.Desktop.Admin | LogLevelControlViewModel | `EnableDebugAsync` | 78 | private |
| LYBT.Desktop.Admin | LogLevelControlViewModel | `DisableDebugAsync` | 97 | private |
| LYBT.Desktop.Admin | AdminHomeViewModel | `NavigateToUserManagement` | 69 | private |
| LYBT.Desktop.Admin | AdminHomeViewModel | `NavigateToHerbManagement` | 75 | private |
| LYBT.Desktop.Admin | AdminHomeViewModel | `NavigateToPatientManagement` | 81 | private |
| LYBT.Desktop.Admin | AdminHomeViewModel | `NavigateToFormulaManagement` | 87 | private |
| LYBT.Desktop.Admin | AdminHomeViewModel | `NavigateToMedicalCaseManagement` | 93 | private |
| LYBT.Desktop.Admin | AdminHomeViewModel | `NavigateToReports` | 105 | private |
| LYBT.Desktop.Admin | SystemSettingsViewModel | `ResetAsync` | 250 | private |
| LYBT.Desktop.Admin | SystemSettingsViewModel | `BrowseBackupPathAsync` | 288 | private |
| LYBT.Desktop.Admin | SystemSettingsViewModel | `SaveServerConfigAsync` | 353 | private |
| LYBT.Desktop.Admin | SystemSettingsViewModel | `ValidateConfigAsync` | 386 | private |
| LYBT.Desktop.Auth | ConnectionStatusViewModel | `SwitchToLocal` | 125 | private |
| LYBT.Desktop.Auth | ConnectionStatusViewModel | `SwitchToRemote` | 152 | private |
| LYBT.Desktop.Auth | ConnectionStatusViewModel | `RetryApiCheckAsync` | 179 | private |
| LYBT.Desktop.Auth | ConnectionStatusViewModel | `OnApiStatusChanged` | 213 | private |
| LYBT.Desktop.Auth | ConnectionStatusViewModel | `OnConnectionModeChanged` | 237 | private |
| LYBT.Desktop.Auth | FirstRunSetupViewModel | `TestConnectionAsync` | 76 | private |
| LYBT.Desktop.Auth | FirstRunSetupViewModel | `CanTestConnection` | 114 | private |
| LYBT.Desktop.Auth | FirstRunSetupViewModel | `UseLocalMode` | 142 | private |
| LYBT.Desktop.Auth | LoginViewModel | `ExecuteLoginAsync` | 240 | private |
| LYBT.Desktop.Auth | LoginViewModel | `ExecuteOpenSettings` | 270 | private |
| LYBT.Desktop.Auth | LoginViewModel | `ExecuteCloseApplicationAsync` | 284 | private |
| LYBT.Desktop.Auth | LoginViewModel | `OnCredentialsPropertyChanged` | 296 | private |
| LYBT.Desktop.Auth | LoginViewModel | `OnConnectionStatusPropertyChanged` | 301 | private |
| LYBT.Desktop.Auth | ServerConfigViewModel | `TestConnectionAsync` | 88 | private |
| LYBT.Desktop.Auth | ServerConfigViewModel | `CanTestConnection` | 122 | private |
| LYBT.Desktop.Auth | ServerConfigViewModel | `CanSaveOnly` | 171 | private |
| LYBT.Desktop.Auth | ServerConfigViewModel | `SaveOnlyAsync` | 178 | private |
| LYBT.Desktop.Auth | LoginView | `OnDataContextChanged` | 18 | private |
| LYBT.Desktop.Auth | LoginView | `OnPasswordChanged` | 28 | private |
| LYBT.Desktop.Clinical | ReceptionistHomeViewModel | `NavigateToPatientManagement` | 88 | private |
| LYBT.Desktop.Clinical | ReceptionistHomeViewModel | `NavigateToRegistrationQueue` | 92 | private |
| LYBT.Desktop.Clinical | ReceptionistHomeViewModel | `NavigateToCardReaderAsync` | 96 | private |
| LYBT.Desktop.Clinical | ReceptionistHomeViewModel | `CreateNewPatient` | 151 | private |
| LYBT.Desktop.Clinical | ReceptionistHomeViewModel | `CreateNewRegistration` | 157 | private |
| LYBT.Desktop.Clinical | ReceptionistHomeViewModel | `SearchPatientAsync` | 163 | private |
| LYBT.Desktop.Clinical | ClinicalHomeViewModel | `NavigateToPatientManagement` | 97 | private |
| LYBT.Desktop.Clinical | ClinicalHomeViewModel | `NavigateToMedicalCaseQuery` | 107 | private |
| LYBT.Desktop.Clinical | ClinicalHomeViewModel | `NavigateToHerbLibrary` | 117 | private |
| LYBT.Desktop.Clinical | ClinicalHomeViewModel | `NavigateToFormulaLibrary` | 127 | private |
| LYBT.Desktop.Clinical | ClinicalHomeViewModel | `NavigateToRegistrationQueue` | 138 | private |
| LYBT.Desktop.Clinical | ClinicalHomeViewModel | `EditProfile` | 148 | private |
| LYBT.Desktop.Clinical | ClinicalWorkspaceViewModel | `CanStartConsultation` | 143 | private |
| LYBT.Desktop.Clinical | ClinicalWorkspaceViewModel | `OnCacheInvalidated` | 312 | private |
| LYBT.Desktop.Clinical | MedicalCaseWorkspaceViewModel | `OnEditStateChangedFsm` | 378 | private |
| LYBT.Desktop.Clinical | MedicalCaseWorkspaceViewModel | `ExecuteSaveChanges` | 494 | private |
| LYBT.Desktop.Clinical | MedicalCaseWorkspaceViewModel | `OnConsultationCompleted` | 504 | private |
| LYBT.Desktop.Clinical | MedicalCaseWorkspaceViewModel | `OnPrescriptionCompleted` | 507 | private |
| LYBT.Desktop.Clinical | MedicalCaseWorkspaceViewModel | `OnChildPropertyChanged` | 510 | private |
| LYBT.Desktop.Clinical | MedicalCaseWorkspaceViewModel | `ExecuteViewPatientHistory` | 524 | private |
| LYBT.Desktop.Clinical | MedicalCaseWorkspaceViewModel | `ExecuteViewAuditLogs` | 531 | private |
| LYBT.Desktop.Clinical | PatientSelectionViewModel | `BackToHome` | 156 | private |
| LYBT.Desktop.Clinical | PatientSelectionViewModel | `CanStartMedicalCase` | 241 | private |
| LYBT.Desktop.Clinical | CardReaderViewModel | `OnCardReadCompleted` | 415 | private |
| LYBT.Desktop.Clinical | PendingQueueViewModel | `SelectAsync` | 69 | private |
| LYBT.Desktop.Clinical | ClinicalWorkspaceView | `PatientSelectionControl_PatientDoubleClicked` | 22 | private |
| LYBT.Desktop.Clinical | PatientSelectionView | `PatientSelectionControl_PatientDoubleClicked` | 21 | private |
| LYBT.Desktop.Controls | BaseDetailContainer | `OnGoBackCommandChanged` | 115 | private |
| LYBT.Desktop.Controls | BreadcrumbBar | `OnNavigationPathChanged` | 37 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnAllHerbsChanged` | 74 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnViewModelItemChanged` | 124 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnControlPreviewMouseDown` | 196 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnTextBoxPreviewKeyDown` | 233 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnTextBoxTextChanged` | 296 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnTextBoxGotFocus` | 311 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnTextBoxLostFocus` | 321 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnListBoxPreviewMouseDown` | 355 | private |
| LYBT.Desktop.Controls | HerbItemControl | `FindParent` | 372 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnDosageKeyDown` | 429 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnDosageGotFocus` | 445 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnContextMenuOpening` | 454 | private |
| LYBT.Desktop.Controls | HerbItemControl | `OnDeleteMenuItemClick` | 462 | private |
| LYBT.Desktop.Controls | HerbListControl | `OnAllHerbsChanged` | 77 | private |
| LYBT.Desktop.Controls | HerbListControl | `OnDuplicateStrategyChanged` | 101 | private |
| LYBT.Desktop.Controls | HerbListControl | `OnHerbItemsPropertyChanged` | 128 | private |
| LYBT.Desktop.Controls | HerbListControl | `OnViewModelListChanged` | 178 | private |
| LYBT.Desktop.Controls | HerbListControl | `OnHerbItemChanged` | 291 | private |
| LYBT.Desktop.Controls | HerbListControl | `OnHerbItemDeleteRequested` | 296 | private |
| LYBT.Desktop.Controls | HerbListControl | `OnHerbItemNextRequested` | 313 | private |
| LYBT.Desktop.Controls | HerbListControl | `FindVisualChild` | 348 | private |
| LYBT.Desktop.Controls | HerbListControlViewModel | `CanClearAll` | 95 | private |
| LYBT.Desktop.Controls | HerbListControlViewModel | `CanSortByRole` | 119 | private |
| LYBT.Desktop.Controls | LoadingOverlay | `OnDelayTimerTick` | 58 | private |
| LYBT.Desktop.Controls | MasterDetailLayout | `OnLoaded` | 26 | private |
| LYBT.Desktop.Controls | MasterDetailLayout | `OnUnloaded` | 36 | private |
| LYBT.Desktop.Controls | MasterDetailLayout | `OnWindowSizeChanged` | 44 | private |
| LYBT.Desktop.Controls | SearchBox | `ExecuteClear` | 70 | private |
| LYBT.Desktop.Controls | StatusBadge | `OnStatusChanged` | 113 | private |
| LYBT.Desktop.Controls | StatusBadge | `OnBadgeTypeChanged` | 121 | private |
| LYBT.Desktop.Controls | BindingProxy | `CreateInstanceCore` | 15 | protected |
| LYBT.Desktop.Formula | FormulaEditorViewModel | `OnFormulaPropertyChanged` | 156 | private |
| LYBT.Desktop.Formula | FormulaMasterDetailViewModel | `CanToggleStatus` | 238 | private |
| LYBT.Desktop.Formula | FormulaMasterDetailViewModel | `CanCopyFormula` | 278 | private |
| LYBT.Desktop.Formula | FormulaMasterDetailViewModel | `CanDeleteHerb` | 310 | private |
| LYBT.Desktop.Formula | FormulaMasterDetailViewModel | `SearchByCategoryAsync` | 314 | private |
| LYBT.Desktop.Formula | FormulaMasterDetailViewModel | `OnSelfPropertyChanged` | 354 | private |
| LYBT.Desktop.Foundation | HttpApiClientBase | `HttpApiClientBase` | 40 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `ToJsonContent` | 47 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `DeserializeAsync` | 53 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `DeserializeEnvelopeAsync` | 63 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `BuildQueryString` | 117 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `GetAndWrapAsync` | 165 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `GetRawAsync` | 172 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `PostAndWrapAsync` | 180 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `PostRawAsync` | 195 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `PutAndWrapAsync` | 203 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `SendAndWrapAsync` | 215 | protected |
| LYBT.Desktop.Foundation | HttpApiClientBase | `GetPagedAndWrapAsync` | 222 | protected |
| LYBT.Desktop.Foundation | EntityApiClientRepositoryBase | `EntityApiClientRepositoryBase` | 31 | protected |
| LYBT.Desktop.Foundation | AuthenticationStateMachine | `AuthenticationStateMachine` | 131 | internal |
| LYBT.Desktop.Foundation | AuthenticationStateMachine | `ForceState` | 206 | internal |
| LYBT.Desktop.Foundation | TokenLifecycleService | `OnMonitorTick` | 219 | private |
| LYBT.Desktop.Foundation | TokenRefreshResult | `TokenRefreshResult` | 80 | private |
| LYBT.Desktop.Foundation | ConnectionModeService | `OnUrlChanged` | 188 | private |
| LYBT.Desktop.Herbs | HerbEditorViewModel | `OnHerbPropertyChanged` | 103 | private |
| LYBT.Desktop.Herbs | HerbMasterDetailViewModel | `CanToggleStatus` | 231 | private |
| LYBT.Desktop.Herbs | HerbMasterDetailViewModel | `CanCopyHerb` | 250 | private |
| LYBT.Desktop.Herbs | HerbMasterDetailViewModel | `SearchByCategoryAsync` | 267 | private |
| LYBT.Desktop.Infrastructure | DataGridSelectionBehavior | `OnShowCheckBoxColumnChanged` | 72 | private |
| LYBT.Desktop.Infrastructure | DataGridSelectionBehavior | `DataGrid_Loaded` | 93 | private |
| LYBT.Desktop.Infrastructure | DataGridSelectionBehavior | `DataGrid_SelectionChanged` | 281 | private |
| LYBT.Desktop.Infrastructure | DataGridSelectionBehavior | `OnSelectedItemsChanged` | 309 | private |
| LYBT.Desktop.Infrastructure | DataGridSelectionBehavior | `FindVisualParent` | 347 | private |
| LYBT.Desktop.Infrastructure | PasswordBoxHelper | `OnBoundPasswordChanged` | 24 | private |
| LYBT.Desktop.Infrastructure | PasswordBoxHelper | `PasswordBox_PasswordChanged` | 38 | private |
| LYBT.Desktop.Infrastructure | CardReaderService | `AutoReadCallback` | 269 | private |
| LYBT.Desktop.Infrastructure | CardReaderService | `OnReaderConnectionStateChanged` | 315 | private |
| LYBT.Desktop.Infrastructure | DesktopExceptionHandler | `OnUnhandledException` | 113 | private |
| LYBT.Desktop.Infrastructure | DesktopExceptionHandler | `OnUnobservedTaskException` | 134 | private |
| LYBT.Desktop.Infrastructure | RegionMonitor | `OnRegionsCollectionChanged` | 42 | private |
| LYBT.Desktop.Infrastructure | ApplicationTickService | `OnTimerTick` | 100 | private |
| LYBT.Desktop.Infrastructure | UserActivityTracker | `OnPreProcessInput` | 192 | private |
| LYBT.Desktop.Infrastructure | UserActivityTracker | `OnTick` | 213 | private |
| LYBT.Desktop.Infrastructure | WpfUiThreadDispatcher | `WpfUiThreadDispatcher` | 17 | internal |
| LYBT.Desktop.Infrastructure | DialogViewModelBase | `DialogViewModelBase` | 76 | protected |
| LYBT.Desktop.Infrastructure | DialogViewModelBase | `CloseDialogWithResult` | 125 | protected |
| LYBT.Desktop.Infrastructure | DialogViewModelBase | `TryGetDialogParameter` | 174 | protected |
| LYBT.Desktop.Infrastructure | NavigableViewModelBase | `ExecuteWithErrorHandlingAsync` | 22 | protected |
| LYBT.Desktop.Infrastructure | NavigableViewModelBase | `ExecuteWithErrorHandlingAsync` | 62 | protected |
| LYBT.Desktop.Infrastructure | NavigableViewModelBase | `RunOnUIThread` | 102 | protected |
| LYBT.Desktop.Infrastructure | NavigableViewModelBase | `RunOnUIThreadAsync` | 110 | protected |
| LYBT.Desktop.Infrastructure | NavigableViewModelBase | `NavigableViewModelBase` | 205 | protected |
| LYBT.Desktop.Infrastructure | NavigableViewModelBase | `AddDisposable` | 284 | protected |
| LYBT.Desktop.Infrastructure | ValidatableModelBase | `ValidatableModelBase` | 39 | protected |
| LYBT.Desktop.Infrastructure | ChildViewModelBase | `ChildViewModelBase` | 16 | protected |
| LYBT.Desktop.Infrastructure | MasterDetailCommandGroup | `ClearSearchAsync` | 47 | private |
| LYBT.Desktop.Infrastructure | MasterDetailCommandGroup | `CanGoToFirstPage` | 82 | private |
| LYBT.Desktop.Infrastructure | MasterDetailCommandGroup | `CanGoToPreviousPage` | 83 | private |
| LYBT.Desktop.Infrastructure | MasterDetailCommandGroup | `CanGoToNextPage` | 84 | private |
| LYBT.Desktop.Infrastructure | MasterDetailCommandGroup | `CanGoToLastPage` | 85 | private |
| LYBT.Desktop.Infrastructure | MasterDetailCommandGroup | `CanCreateNew` | 214 | private |
| LYBT.Desktop.Infrastructure | MasterDetailCommandGroup | `CanSave` | 216 | private |
| LYBT.Desktop.Infrastructure | MasterDetailCommandGroup | `CanCancel` | 217 | private |
| LYBT.Desktop.Infrastructure | MasterDetailCommandGroup | `CanRestore` | 219 | private |
| LYBT.Desktop.Infrastructure | ServiceEventBridge | `ForwardPropertyChanged` | 40 | private |
| LYBT.Desktop.Infrastructure | ServiceEventBridge | `OnLoadingPropertyChanged` | 43 | private |
| LYBT.Desktop.Infrastructure | ServiceEventBridge | `OnPaginationPropertyChanged` | 59 | private |
| LYBT.Desktop.Infrastructure | ServiceEventBridge | `OnPaginationPageChanged` | 74 | private |
| LYBT.Desktop.Infrastructure | ServiceEventBridge | `OnSelectionPropertyChanged` | 80 | private |
| LYBT.Desktop.Infrastructure | ServiceEventBridge | `OnSelectionSelectionChanged` | 89 | private |
| LYBT.Desktop.Infrastructure | ServiceEventBridge | `OnDetailEditorPropertyChanged` | 95 | private |
| LYBT.Desktop.Infrastructure | ServiceEventBridge | `OnErrorHandlerPropertyChanged` | 116 | private |
| LYBT.Desktop.Infrastructure | BaseStatusHandler | `BaseStatusHandler` | 16 | protected |
| LYBT.Desktop.Infrastructure | MasterDetailViewModelBase | `MasterDetailViewModelBase` | 118 | protected |
| LYBT.Desktop.Infrastructure | MasterDetailViewModelBase | `CanGoToFirstPage` | 147 | private |
| LYBT.Desktop.Infrastructure | MasterDetailViewModelBase | `CanGoToPreviousPage` | 148 | private |
| LYBT.Desktop.Infrastructure | MasterDetailViewModelBase | `CanGoToNextPage` | 149 | private |
| LYBT.Desktop.Infrastructure | MasterDetailViewModelBase | `CanGoToLastPage` | 150 | private |
| LYBT.Desktop.Infrastructure | MasterDetailViewModelBase | `CanCreateNew` | 151 | private |
| LYBT.Desktop.Infrastructure | MasterDetailViewModelBase | `CanSave` | 153 | private |
| LYBT.Desktop.Infrastructure | MasterDetailViewModelBase | `CanCancel` | 154 | private |
| LYBT.Desktop.Infrastructure | MasterDetailViewModelBase | `CanRestore` | 156 | private |
| LYBT.Desktop.MedicalCase | WorkflowStepIndicator | `OnCurrentStepChanged` | 33 | private |
| LYBT.Desktop.MedicalCase | HistoryCopyDialogViewModel | `ShowMoreCurrentPatient` | 272 | private |
| LYBT.Desktop.MedicalCase | HistoryCopyDialogViewModel | `ToggleAllPatients` | 281 | private |
| LYBT.Desktop.MedicalCase | UnsavedChangesDialogViewModel | `Discard` | 38 | private |
| LYBT.Desktop.MedicalCase | ReportsHomeViewModel | `GoToToday` | 60 | private |
| LYBT.Desktop.MedicalCase | PrintResult | `PrintResult` | 321 | private |
| LYBT.Desktop.MedicalCase | MedicalCaseCommandsViewModel | `ExecuteSaveAsync` | 115 | private |
| LYBT.Desktop.MedicalCase | MedicalCaseCommandsViewModel | `ExecuteSuspendAsync` | 149 | private |
| LYBT.Desktop.MedicalCase | MedicalCaseCommandsViewModel | `ExecuteCompleteAsync` | 183 | private |
| LYBT.Desktop.MedicalCase | MedicalCaseCommandsViewModel | `ExecutePrintAsync` | 220 | private |
| LYBT.Desktop.MedicalCase | MedicalCaseCommandsViewModel | `ExecuteExportPdfAsync` | 254 | private |
| LYBT.Desktop.MedicalCase | MedicalCaseCommandsViewModel | `ExecuteEnterEditMode` | 293 | private |
| LYBT.Desktop.MedicalCase | MedicalCaseCommandsViewModel | `ExecuteImportFormula` | 302 | private |
| LYBT.Desktop.MedicalCase | MedicalCaseCommandsViewModel | `ExecuteCopyHistory` | 317 | private |
| LYBT.Desktop.MedicalCase | MedicalCaseCommandsViewModel | `ExecuteClearHerbsAsync` | 339 | private |
| LYBT.Desktop.MedicalCase | PrescriptionEditorViewModel | `OnItemsCollectionChanged` | 85 | private |
| LYBT.Desktop.Patients | PatientSelectionControl | `PatientDataGrid_MouseDoubleClick` | 43 | private |
| LYBT.Desktop.Patients | PatientSelectionControl | `GetPropertyValue` | 63 | private |
| LYBT.Desktop.Patients | PatientCardReaderViewModel | `CanReadCard` | 86 | private |
| LYBT.Desktop.Patients | PatientEditorViewModel | `OnPatientPropertyChanged` | 96 | private |
| LYBT.Desktop.Patients | PatientMasterDetailViewModel | `CanViewMedicalRecords` | 250 | private |
| LYBT.Desktop.Patients | PatientMasterDetailViewModel | `CanNewConsultation` | 262 | private |
| LYBT.Desktop.Patients | PatientMasterDetailViewModel | `CanReadCard` | 301 | private |
| LYBT.Desktop.Registrations | RegistrationCreateDialogViewModel | `SelectPatient` | 178 | private |
| LYBT.Desktop.Registrations | RegistrationCreateDialogViewModel | `ClearPatientSelection` | 189 | private |
| LYBT.Desktop.Registrations | SignalRClient | `OnReconnectedAsync` | 116 | private |
| LYBT.Desktop.Registrations | SignalRClient | `OnClosedAsync` | 123 | private |
| LYBT.Desktop.Registrations | RegistrationListViewModel | `OnRegistrationRefreshed` | 160 | private |
| LYBT.Desktop.Registrations | RegistrationListViewModel | `CreateRegistration` | 179 | private |
| LYBT.Desktop.Registrations | RegistrationListViewModel | `CanStartVisit` | 255 | private |
| LYBT.Desktop.Registrations | RegistrationListViewModel | `CancelRegistrationAsync` | 263 | private |
| LYBT.Desktop.Registrations | RegistrationListViewModel | `CanCancelRegistration` | 298 | private |
| LYBT.Desktop.Shell | App | `CreateShell` | 80 | protected |
| LYBT.Desktop.Shell | App | `InitializeShell` | 83 | protected |
| LYBT.Desktop.Shell | AccountSettingsControl | `OnDataContextChanged` | 23 | private |
| LYBT.Desktop.Shell | AccountSettingsControl | `OnOldPasswordChanged` | 35 | private |
| LYBT.Desktop.Shell | AccountSettingsControl | `OnNewPasswordChanged` | 44 | private |
| LYBT.Desktop.Shell | AccountSettingsControl | `OnConfirmPasswordChanged` | 53 | private |
| LYBT.Desktop.Shell | PrismConfigurationExtensions | `RegisterOptions` | 53 | private |
| LYBT.Desktop.Shell | StringResources | `StringResources` | 32 | internal |
| LYBT.Desktop.Shell | LoginCoordinator | `OnStateMachineStateChanged` | 287 | private |
| LYBT.Desktop.Shell | MenuManager | `ExecuteAccountSettings` | 118 | private |
| LYBT.Desktop.Shell | MenuManager | `ExecuteNavigateToHome` | 123 | private |
| LYBT.Desktop.Shell | MenuManager | `ExecuteNavigateToSystemSettings` | 129 | private |
| LYBT.Desktop.Shell | MenuManager | `ExecuteNavigateBack` | 136 | private |
| LYBT.Desktop.Shell | MenuManager | `ExecuteNavigateForward` | 144 | private |
| LYBT.Desktop.Shell | MenuManager | `ExecuteShowHelp` | 180 | private |
| LYBT.Desktop.Shell | MenuManager | `ExecuteShowSettings` | 187 | private |
| LYBT.Desktop.Shell | SessionLifecycleManager | `OnUserActivitySessionExpired` | 312 | private |
| LYBT.Desktop.Shell | ShellEventCoordinator | `OnLoginSucceeded` | 56 | private |
| LYBT.Desktop.Shell | ShellEventCoordinator | `OnPasswordChanged` | 115 | private |
| LYBT.Desktop.Shell | ShellEventCoordinator | `OnProfileUpdated` | 143 | private |
| LYBT.Desktop.Shell | ShellEventCoordinator | `OnLogoutRequested` | 206 | private |
| LYBT.Desktop.Shell | StatusBarManager | `OnHealthStatusChanged` | 84 | private |
| LYBT.Desktop.Shell | StatusBarManager | `OnConnectionUrlChanged` | 95 | private |
| LYBT.Desktop.Shell | StatusBarManager | `OnConnectionModeChanged` | 105 | private |
| LYBT.Desktop.Shell | AccountSettingsViewModel | `SaveProfileAsync` | 82 | private |
| LYBT.Desktop.Shell | AccountSettingsViewModel | `CanSaveProfile` | 135 | private |
| LYBT.Desktop.Shell | AccountSettingsViewModel | `CanChangePassword` | 207 | private |
| LYBT.Desktop.Shell | MainWindowViewModel | `RetryHealthCheckAsync` | 175 | private |
| LYBT.Desktop.Shell | MainWindowViewModel | `ToggleSidebar` | 186 | private |
| LYBT.Desktop.Shell | MainWindowViewModel | `OnTick` | 195 | private |
| LYBT.Desktop.Shell | MainWindowViewModel | `OnLoginStateChanged` | 200 | private |
| LYBT.Desktop.Shell | MainWindowViewModel | `OnLoginSuccessHandled` | 211 | private |
| LYBT.Desktop.Shell | MainWindow | `OnWindowLoaded` | 31 | private |
| LYBT.Desktop.Users | UserMasterDetailViewModel | `CanClearFilters` | 341 | private |
| LYBT.Desktop.Users | UserMasterDetailViewModel | `OnDetailPropertyChanged` | 388 | private |
| LYBT.Infrastructure | EntityOptimizationExtensions | `ConfigureGlobalQueryFilter` | 58 | private |
| LYBT.Infrastructure | BaseRepository | `BaseRepository` | 22 | protected |
| LYBT.Infrastructure | BaseService | `BaseService` | 13 | protected |
| LYBT.Infrastructure | BaseService | `BaseService` | 25 | protected |
| LYBT.Infrastructure | BaseApiController | `BaseApiController` | 19 | protected |
| LYBT.Infrastructure | BaseCrudController | `BaseCrudController` | 17 | protected |
| LYBT.Infrastructure | ControllerBaseExtensions | `CreateModuleErrorResponse` | 141 | private |
| LYBT.Module.MedicalCase | BaseMedicalCasesController | `BaseMedicalCasesController` | 24 | protected |
| LYBT.Module.Registration | BaseRegistrationsController | `BaseRegistrationsController` | 18 | protected |
| LYBT.Module.Users | UserCrossModuleMapper | `ToNonNullString` | 30 | private |
| LYBT.Module.Users | UserCrossModuleMapper | `ToLockoutEnd` | 35 | private |
| LYBT.Module.Users | BaseUsersController | `BaseUsersController` | 27 | protected |

## 3. 全量候选清单（按项目分组，含框架模式标注）

### LYBT.Desktop.Admin（31 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| AdminModule | `RegisterTypes` | 18 | public | 框架反射模式 |
| SystemSettingsService | `SystemSettingsService` | 18 | public | 待符号级复核 |
| AuthHealthService | `AuthHealthService` | 14 | public | 待符号级复核 |
| SysadminModule | `RegisterTypes` | 18 | public | 框架反射模式 |
| DeploymentViewModel | `DeploymentViewModel` | 27 | public | 待符号级复核 |
| DeploymentViewModel | `SelectFile` | 36 | private | 待符号级复核 |
| LogLevelControlViewModel | `LogLevelControlViewModel` | 25 | public | 待符号级复核 |
| LogLevelControlViewModel | `SetLevelAsync` | 59 | private | 待符号级复核 |
| LogLevelControlViewModel | `EnableDebugAsync` | 78 | private | 待符号级复核 |
| LogLevelControlViewModel | `DisableDebugAsync` | 97 | private | 待符号级复核 |
| SysadminHomeViewModel | `SysadminHomeViewModel` | 24 | public | 待符号级复核 |
| DeploymentView | `DeploymentView` | 7 | public | 待符号级复核 |
| LogLevelControlView | `LogLevelControlView` | 10 | public | 待符号级复核 |
| SysadminHomeView | `SysadminHomeView` | 10 | public | 待符号级复核 |
| AdminHomeViewModel | `AdminHomeViewModel` | 46 | public | 待符号级复核 |
| AdminHomeViewModel | `NavigateToUserManagement` | 69 | private | 待符号级复核 |
| AdminHomeViewModel | `NavigateToHerbManagement` | 75 | private | 待符号级复核 |
| AdminHomeViewModel | `NavigateToPatientManagement` | 81 | private | 待符号级复核 |
| AdminHomeViewModel | `NavigateToFormulaManagement` | 87 | private | 待符号级复核 |
| AdminHomeViewModel | `NavigateToMedicalCaseManagement` | 93 | private | 待符号级复核 |
| AdminHomeViewModel | `NavigateToReports` | 105 | private | 待符号级复核 |
| AdminHomeViewModel | `IsNavigationTarget` | 167 | public | 待符号级复核 |
| SystemSettingsViewModel | `SystemSettingsViewModel` | 141 | public | 待符号级复核 |
| SystemSettingsViewModel | `ResetAsync` | 250 | private | 待符号级复核 |
| SystemSettingsViewModel | `BrowseBackupPathAsync` | 288 | private | 待符号级复核 |
| SystemSettingsViewModel | `SaveServerConfigAsync` | 353 | private | 待符号级复核 |
| SystemSettingsViewModel | `ValidateConfigAsync` | 386 | private | 待符号级复核 |
| AdminHomeView | `AdminHomeView` | 11 | public | 待符号级复核 |
| SystemSettingsView | `SystemSettingsView` | 10 | public | 待符号级复核 |
| UserManagementView | `UserManagementView` | 15 | public | 待符号级复核 |
| UserManagementView | `IsNavigationTarget` | 29 | public | 待符号级复核 |
### LYBT.Desktop.Auth（32 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| AuthenticationModule | `RegisterTypes` | 28 | public | 框架反射模式 |
| ConnectionStatusViewModel | `SwitchToLocal` | 125 | private | 待符号级复核 |
| ConnectionStatusViewModel | `SwitchToRemote` | 152 | private | 待符号级复核 |
| ConnectionStatusViewModel | `RetryApiCheckAsync` | 179 | private | 待符号级复核 |
| ConnectionStatusViewModel | `OnApiStatusChanged` | 213 | private | 待符号级复核 |
| ConnectionStatusViewModel | `OnConnectionModeChanged` | 237 | private | 待符号级复核 |
| FirstRunSetupViewModel | `FirstRunSetupViewModel` | 40 | public | 待符号级复核 |
| FirstRunSetupViewModel | `OnTestStatusChanged` | 60 | default(private) | 待符号级复核 |
| FirstRunSetupViewModel | `OnIsRemoteAvailableChanged` | 67 | default(private) | 待符号级复核 |
| FirstRunSetupViewModel | `TestConnectionAsync` | 76 | private | 待符号级复核 |
| FirstRunSetupViewModel | `CanTestConnection` | 114 | private | 待符号级复核 |
| FirstRunSetupViewModel | `UseLocalMode` | 142 | private | 待符号级复核 |
| LoginCredentialsViewModel | `OnRememberUsernameChanged` | 128 | default(private) | 待符号级复核 |
| LoginCredentialsViewModel | `OnRememberPasswordChanged` | 139 | default(private) | 待符号级复核 |
| LoginCredentialsViewModel | `OnUsernameChanged` | 154 | default(private) | 待符号级复核 |
| LoginViewModel | `LoginViewModel` | 146 | public | 待符号级复核 |
| LoginViewModel | `ExecuteLoginAsync` | 240 | private | 待符号级复核 |
| LoginViewModel | `ExecuteOpenSettings` | 270 | private | 待符号级复核 |
| LoginViewModel | `ExecuteCloseApplicationAsync` | 284 | private | 待符号级复核 |
| LoginViewModel | `OnCredentialsPropertyChanged` | 296 | private | 待符号级复核 |
| LoginViewModel | `OnConnectionStatusPropertyChanged` | 301 | private | 待符号级复核 |
| ServerConfigViewModel | `ServerConfigViewModel` | 34 | public | 待符号级复核 |
| ServerConfigViewModel | `OnTestStatusChanged` | 59 | default(private) | 待符号级复核 |
| ServerConfigViewModel | `TestConnectionAsync` | 88 | private | 待符号级复核 |
| ServerConfigViewModel | `CanTestConnection` | 122 | private | 待符号级复核 |
| ServerConfigViewModel | `CanSaveOnly` | 171 | private | 待符号级复核 |
| ServerConfigViewModel | `SaveOnlyAsync` | 178 | private | 待符号级复核 |
| FirstRunSetupView | `FirstRunSetupView` | 10 | public | 待符号级复核 |
| LoginView | `LoginView` | 11 | public | 待符号级复核 |
| LoginView | `OnDataContextChanged` | 18 | private | 待符号级复核 |
| LoginView | `OnPasswordChanged` | 28 | private | 待符号级复核 |
| ServerConfigView | `ServerConfigView` | 10 | public | 待符号级复核 |
### LYBT.Desktop.Clinical（52 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| ClinicalModule | `RegisterTypes` | 22 | public | 框架反射模式 |
| ReceptionistHomeViewModel | `ReceptionistHomeViewModel` | 59 | public | 待符号级复核 |
| ReceptionistHomeViewModel | `NavigateToPatientManagement` | 88 | private | 待符号级复核 |
| ReceptionistHomeViewModel | `NavigateToRegistrationQueue` | 92 | private | 待符号级复核 |
| ReceptionistHomeViewModel | `NavigateToCardReaderAsync` | 96 | private | 待符号级复核 |
| ReceptionistHomeViewModel | `CreateNewPatient` | 151 | private | 待符号级复核 |
| ReceptionistHomeViewModel | `CreateNewRegistration` | 157 | private | 待符号级复核 |
| ReceptionistHomeViewModel | `SearchPatientAsync` | 163 | private | 待符号级复核 |
| ReceptionistHomeView | `ReceptionistHomeView` | 10 | public | 待符号级复核 |
| ClinicalHomeViewModel | `ClinicalHomeViewModel` | 59 | public | 待符号级复核 |
| ClinicalHomeViewModel | `NavigateToPatientManagement` | 97 | private | 待符号级复核 |
| ClinicalHomeViewModel | `NavigateToMedicalCaseQuery` | 107 | private | 待符号级复核 |
| ClinicalHomeViewModel | `NavigateToHerbLibrary` | 117 | private | 待符号级复核 |
| ClinicalHomeViewModel | `NavigateToFormulaLibrary` | 127 | private | 待符号级复核 |
| ClinicalHomeViewModel | `NavigateToRegistrationQueue` | 138 | private | 待符号级复核 |
| ClinicalHomeViewModel | `EditProfile` | 148 | private | 待符号级复核 |
| ClinicalHomeViewModel | `IsNavigationTarget` | 214 | public | 待符号级复核 |
| ClinicalWorkspaceViewModel | `ClinicalWorkspaceViewModel` | 92 | public | 待符号级复核 |
| ClinicalWorkspaceViewModel | `OnSelectedPatientChanged` | 114 | default(private) | 待符号级复核 |
| ClinicalWorkspaceViewModel | `CanStartConsultation` | 143 | private | 待符号级复核 |
| ClinicalWorkspaceViewModel | `OnCacheInvalidated` | 312 | private | 待符号级复核 |
| ClinicalWorkspaceViewModel | `IsNavigationTarget` | 333 | public | 待符号级复核 |
| MedicalCaseWorkspaceViewModel | `MedicalCaseWorkspaceViewModel` | 254 | public | 待符号级复核 |
| MedicalCaseWorkspaceViewModel | `IsNavigationTarget` | 345 | public | 待符号级复核 |
| MedicalCaseWorkspaceViewModel | `OnEditStateChangedFsm` | 378 | private | 待符号级复核 |
| MedicalCaseWorkspaceViewModel | `ExecuteSaveChanges` | 494 | private | 待符号级复核 |
| MedicalCaseWorkspaceViewModel | `OnConsultationCompleted` | 504 | private | 待符号级复核 |
| MedicalCaseWorkspaceViewModel | `OnPrescriptionCompleted` | 507 | private | 待符号级复核 |
| MedicalCaseWorkspaceViewModel | `OnChildPropertyChanged` | 510 | private | 待符号级复核 |
| MedicalCaseWorkspaceViewModel | `ExecuteViewPatientHistory` | 524 | private | 待符号级复核 |
| MedicalCaseWorkspaceViewModel | `ExecuteViewAuditLogs` | 531 | private | 待符号级复核 |
| PatientSelectionViewModel | `PatientSelectionViewModel` | 105 | public | 待符号级复核 |
| PatientSelectionViewModel | `OnSelectedPatientChanged` | 145 | default(private) | 待符号级复核 |
| PatientSelectionViewModel | `BackToHome` | 156 | private | 待符号级复核 |
| PatientSelectionViewModel | `CanStartMedicalCase` | 241 | private | 待符号级复核 |
| PatientSelectionViewModel | `IsNavigationTarget` | 460 | public | 待符号级复核 |
| CardReaderViewModel | `ManualReadCardAsync` | 132 | public | 待符号级复核 |
| CardReaderViewModel | `ToggleAutoRead` | 169 | public | 待符号级复核 |
| CardReaderViewModel | `OnCardReadCompleted` | 415 | private | 待符号级复核 |
| PendingQueueViewModel | `SelectAsync` | 69 | private | 待符号级复核 |
| WorkspaceStateManager | `public` | 122 | default(private) | 待符号级复核 |
| ClinicalHomeView | `ClinicalHomeView` | 11 | public | 待符号级复核 |
| ClinicalWorkspaceView | `ClinicalWorkspaceView` | 14 | public | 待符号级复核 |
| ClinicalWorkspaceView | `PatientSelectionControl_PatientDoubleClicked` | 22 | private | 待符号级复核 |
| FormulaManagementView | `FormulaManagementView` | 17 | public | 待符号级复核 |
| HerbManagementView | `HerbManagementView` | 17 | public | 待符号级复核 |
| MedicalCaseManagementView | `MedicalCaseManagementView` | 17 | public | 待符号级复核 |
| MedicalCaseWorkspaceView | `MedicalCaseWorkspaceView` | 13 | public | 待符号级复核 |
| PatientManagementView | `PatientManagementView` | 17 | public | 待符号级复核 |
| PatientSelectionView | `PatientSelectionView` | 12 | public | 待符号级复核 |
| PatientSelectionView | `PatientSelectionControl_PatientDoubleClicked` | 21 | private | 待符号级复核 |
| PendingQueueView | `PendingQueueView` | 11 | public | 待符号级复核 |
### LYBT.Desktop.Contracts（41 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| IApiClientFormulas | `GetCategoriesAsync` | 136 | public (interface 默认) | 待符号级复核 |
| IApiClientHerbs | `GetCategoriesAsync` | 114 | public (interface 默认) | 待符号级复核 |
| IApiClientRegistrations | `GetRegistrationsAsync` | 82 | public (interface 默认) | 待符号级复核 |
| IApiClientRegistrations | `QuickVisitAsync` | 88 | public (interface 默认) | 待符号级复核 |
| IApiClientRegistrations | `DeleteRegistrationAsync` | 94 | public (interface 默认) | 待符号级复核 |
| MedicalCaseNavigationParameters | `ForManagementView` | 61 | public | 待符号级复核 |
| MedicalCaseNavigationParameters | `ForManagementEdit` | 78 | public | 待符号级复核 |
| PerformanceReport | `GetFormattedReport` | 53 | public | 待符号级复核 |
| PerformanceReport | `GetJsonReport` | 101 | public | 待符号级复核 |
| IPerformanceMonitor | `RecordMemoryBaseline` | 30 | public (interface 默认) | 待符号级复核 |
| IPerformanceMonitor | `GetMemorySnapshots` | 35 | public (interface 默认) | 待符号级复核 |
| IPerformanceMonitor | `GetMetric` | 42 | public (interface 默认) | 待符号级复核 |
| IPerformanceMonitor | `GetAllMetrics` | 47 | public (interface 默认) | 待符号级复核 |
| IPerformanceMonitor | `GenerateReport` | 53 | public (interface 默认) | 待符号级复核 |
| CommandResult | `bool` | 32 | public | 待符号级复核 |
| CommandResult | `bool` | 54 | public | 待符号级复核 |
| IRoleRegistry | `GetAllDefinitions` | 27 | public (interface 默认) | 待符号级复核 |
| IRoleRegistry | `IsRegistered` | 33 | public (interface 默认) | 待符号级复核 |
| IAuthenticationStateMachine | `FireAsync` | 46 | public (interface 默认) | 待符号级复核 |
| IAuthenticationStateMachine | `CanFire` | 53 | public (interface 默认) | 待符号级复核 |
| IAuthenticationStateMachine | `GetPermittedEvents` | 63 | public (interface 默认) | 待符号级复核 |
| IApiHealthMonitor | `StopMonitoringAsync` | 60 | public (interface 默认) | 待符号级复核 |
| IApiHealthMonitor | `ResetCircuitBreaker` | 66 | public (interface 默认) | 待符号级复核 |
| ICommonDialogService | `ShowInfoAsync` | 30 | public (interface 默认) | 待符号级复核 |
| ICommonDialogService | `ShowInputAsync` | 70 | public (interface 默认) | 待符号级复核 |
| ICommonDialogService | `ShowOpenFileDialogAsync` | 78 | public (interface 默认) | 待符号级复核 |
| ICommonDialogService | `ShowSaveFileDialogAsync` | 87 | public (interface 默认) | 待符号级复核 |
| IConnectionModeService | `TestLocalConnectionAsync` | 78 | public (interface 默认) | 待符号级复核 |
| IDesktopCacheManager | `InvalidateAll` | 36 | public (interface 默认) | 待符号级复核 |
| ILoginCoordinator | `HandleLoginSuccessAsync` | 59 | public (interface 默认) | 待符号级复核 |
| ILoginCoordinator | `GetDiagnostics` | 70 | public (interface 默认) | 待符号级复核 |
| INavigationCoordinator | `SubscribeToRegionCollection` | 121 | public (interface 默认) | 待符号级复核 |
| INavigationCoordinator | `UnsubscribeFromRegionCollection` | 126 | public (interface 默认) | 待符号级复核 |
| IPatientService | `BatchDeletePatientsAsync` | 36 | public (interface 默认) | 待符号级复核 |
| ISessionManager | `SetSession` | 48 | public (interface 默认) | 待符号级复核 |
| ISessionManager | `HasRole` | 70 | public (interface 默认) | 待符号级复核 |
| ISessionManager | `IsAdmin` | 75 | public (interface 默认) | 待符号级复核 |
| ISessionManager | `GetCurrentUserRoleDisplay` | 80 | public (interface 默认) | 待符号级复核 |
| IStartupPipeline | `GetDiagnostics` | 68 | public (interface 默认) | 待符号级复核 |
| StartupStepResult | `SkippedResult` | 128 | public | 待符号级复核 |
| IUserNotificationService | `ShowInfoAsync` | 34 | public (interface 默认) | 待符号级复核 |
### LYBT.Desktop.Controls（70 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| BaseDetailContainer | `BaseDetailContainer` | 14 | public | 待符号级复核 |
| BaseDetailContainer | `OnGoBackCommandChanged` | 115 | private | 待符号级复核 |
| BreadcrumbBar | `BreadcrumbBar` | 16 | public | 待符号级复核 |
| BreadcrumbBar | `OnNavigationPathChanged` | 37 | private | 待符号级复核 |
| DataGridToolbar | `DataGridToolbar` | 17 | public | 待符号级复核 |
| DetailToolbar | `DetailToolbar` | 16 | public | 待符号级复核 |
| EmptyState | `EmptyState` | 17 | public | 待符号级复核 |
| FormulaViewControl | `FormulaViewControl` | 10 | public | 待符号级复核 |
| HerbItemControl | `OnAllHerbsChanged` | 74 | private | 待符号级复核 |
| HerbItemControl | `HerbItemControl` | 102 | public | 待符号级复核 |
| HerbItemControl | `OnViewModelItemChanged` | 124 | private | 待符号级复核 |
| HerbItemControl | `OnControlPreviewMouseDown` | 196 | private | 待符号级复核 |
| HerbItemControl | `OnTextBoxPreviewKeyDown` | 233 | private | 待符号级复核 |
| HerbItemControl | `OnTextBoxTextChanged` | 296 | private | 待符号级复核 |
| HerbItemControl | `OnTextBoxGotFocus` | 311 | private | 待符号级复核 |
| HerbItemControl | `OnTextBoxLostFocus` | 321 | private | 待符号级复核 |
| HerbItemControl | `OnListBoxPreviewMouseDown` | 355 | private | 待符号级复核 |
| HerbItemControl | `FindParent` | 372 | private | 待符号级复核 |
| HerbItemControl | `OnDosageKeyDown` | 429 | private | 待符号级复核 |
| HerbItemControl | `OnDosageGotFocus` | 445 | private | 待符号级复核 |
| HerbItemControl | `OnContextMenuOpening` | 454 | private | 待符号级复核 |
| HerbItemControl | `OnDeleteMenuItemClick` | 462 | private | 待符号级复核 |
| HerbItemControlViewModel | `OnHerbNameChanged` | 125 | default(private) | 待符号级复核 |
| HerbItemControlViewModel | `OnSelectedHerbChanged` | 133 | default(private) | 待符号级复核 |
| HerbItemControlViewModel | `OnAllHerbsChanged` | 154 | default(private) | 待符号级复核 |
| HerbListControl | `OnAllHerbsChanged` | 77 | private | 待符号级复核 |
| HerbListControl | `OnDuplicateStrategyChanged` | 101 | private | 待符号级复核 |
| HerbListControl | `OnHerbItemsPropertyChanged` | 128 | private | 待符号级复核 |
| HerbListControl | `HerbListControl` | 162 | public | 待符号级复核 |
| HerbListControl | `OnViewModelListChanged` | 178 | private | 待符号级复核 |
| HerbListControl | `OnHerbItemChanged` | 291 | private | 待符号级复核 |
| HerbListControl | `OnHerbItemDeleteRequested` | 296 | private | 待符号级复核 |
| HerbListControl | `OnHerbItemNextRequested` | 313 | private | 待符号级复核 |
| HerbListControl | `FindVisualChild` | 348 | private | 待符号级复核 |
| HerbListControlViewModel | `OnAllHerbsChanged` | 64 | default(private) | 待符号级复核 |
| HerbListControlViewModel | `CanClearAll` | 95 | private | 待符号级复核 |
| HerbListControlViewModel | `CanSortByRole` | 119 | private | 待符号级复核 |
| HerbListControlViewModel | `MoveItem` | 249 | public | 待符号级复核 |
| HerbListControlViewModel | `GetNextEmptySlotIndex` | 296 | public | 待符号级复核 |
| InfoCard | `InfoCard` | 11 | public | 待符号级复核 |
| LoadingOverlay | `LoadingOverlay` | 21 | public | 待符号级复核 |
| LoadingOverlay | `OnDelayTimerTick` | 58 | private | 待符号级复核 |
| MasterDetailLayout | `MasterDetailLayout` | 19 | public | 待符号级复核 |
| MasterDetailLayout | `OnLoaded` | 26 | private | 待符号级复核 |
| MasterDetailLayout | `OnUnloaded` | 36 | private | 待符号级复核 |
| MasterDetailLayout | `OnWindowSizeChanged` | 44 | private | 待符号级复核 |
| PatientInfoCardControl | `PatientInfoCardControl` | 12 | public | 待符号级复核 |
| SearchBox | `SearchBox` | 18 | public | 待符号级复核 |
| SearchBox | `ExecuteClear` | 70 | private | 待符号级复核 |
| StatusBadge | `StatusBadge` | 105 | public | 待符号级复核 |
| StatusBadge | `OnStatusChanged` | 113 | private | 待符号级复核 |
| StatusBadge | `OnBadgeTypeChanged` | 121 | private | 待符号级复核 |
| StatusBadge | `static` | 195 | default(private) | 待符号级复核 |
| ToastControl | `ToastControl` | 18 | public | 待符号级复核 |
| UnifiedPaginationBar | `UnifiedPaginationBar` | 10 | public | 待符号级复核 |
| BooleanToVisibilityConverter | `ConvertBack` | 21 | public | 框架反射模式 |
| BoolToBrushConverter | `ConvertBack` | 27 | public | 框架反射模式 |
| BoolToColorConverter | `ConvertBack` | 32 | public | 框架反射模式 |
| BoolToIntConverter | `ConvertBack` | 14 | public | 框架反射模式 |
| DecocteMethodToVisibilityConverter | `ConvertBack` | 29 | public | 框架反射模式 |
| EnumDescriptionConverter | `ConvertBack` | 46 | public | 框架反射模式 |
| FirstCharacterConverter | `ConvertBack` | 21 | public | 框架反射模式 |
| InverseBooleanConverter | `ConvertBack` | 24 | public | 框架反射模式 |
| InverseBooleanToVisibilityConverter | `ConvertBack` | 24 | public | 框架反射模式 |
| InverseNullToVisibilityConverter | `ConvertBack` | 24 | public | 框架反射模式 |
| NullToVisibilityConverter | `ConvertBack` | 22 | public | 框架反射模式 |
| StringToVisibilityConverter | `ConvertBack` | 25 | public | 框架反射模式 |
| BindingProxy | `CreateInstanceCore` | 15 | protected | 待符号级复核 |
| ResponsiveLayoutHelper | `GetOptimalColumnCount` | 43 | public | 待符号级复核 |
| DuplicateDosageStrategyExtensions | `GetDisplayName` | 62 | public | 待符号级复核 |
### LYBT.Desktop.Formula（13 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| FormulaEditControl | `FormulaEditControl` | 16 | public | 待符号级复核 |
| FormulaMasterDetailControl | `FormulaMasterDetailControl` | 12 | public | 待符号级复核 |
| FormulaModule | `RegisterTypes` | 26 | public | 框架反射模式 |
| FormulaSearchProvider | `FormulaSearchProvider` | 16 | public | 待符号级复核 |
| FormulaService | `FormulaService` | 21 | public | 待符号级复核 |
| FormulaEditorViewModel | `OnFormulaPropertyChanged` | 156 | private | 待符号级复核 |
| FormulaMasterDetailViewModel | `FormulaMasterDetailViewModel` | 57 | public | 待符号级复核 |
| FormulaMasterDetailViewModel | `CanToggleStatus` | 238 | private | 待符号级复核 |
| FormulaMasterDetailViewModel | `CanCopyFormula` | 278 | private | 待符号级复核 |
| FormulaMasterDetailViewModel | `CanDeleteHerb` | 310 | private | 待符号级复核 |
| FormulaMasterDetailViewModel | `SearchByCategoryAsync` | 314 | private | 待符号级复核 |
| FormulaMasterDetailViewModel | `OnSelfPropertyChanged` | 354 | private | 待符号级复核 |
| FormulaStatusHandler | `FormulaStatusHandler` | 18 | public | 待符号级复核 |
### LYBT.Desktop.Foundation（86 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| DesktopCacheManager | `DesktopCacheManager` | 24 | public | 待符号级复核 |
| DesktopCacheManager | `InvalidateAll` | 94 | public | 待符号级复核 |
| ClientErrorMessageMapper | `GetSafeMessageWithTrackingCode` | 365 | public | 待符号级复核 |
| ClientErrorMessageMapper | `GetMessageWithTrackingCode` | 381 | public | 待符号级复核 |
| ClientErrorMessageMapper | `GetFullTrackingCode` | 404 | public | 待符号级复核 |
| ApiHealthCheckService | `ApiHealthCheckService` | 17 | public | 待符号级复核 |
| FormulaApiClient | `GetCategoriesAsync` | 100 | public | 待符号级复核 |
| FormulasHttpApiClient | `GetCategoriesAsync` | 78 | public | 待符号级复核 |
| HerbApiClient | `GetCategoriesAsync` | 87 | public | 待符号级复核 |
| HerbsHttpApiClient | `GetCategoriesAsync` | 68 | public | 待符号级复核 |
| RegistrationApiClient | `GetRegistrationsAsync` | 64 | public | 待符号级复核 |
| RegistrationApiClient | `QuickVisitAsync` | 69 | public | 待符号级复核 |
| RegistrationApiClient | `DeleteRegistrationAsync` | 74 | public | 待符号级复核 |
| RegistrationsHttpApiClient | `GetRegistrationsAsync` | 55 | public | 待符号级复核 |
| RegistrationsHttpApiClient | `QuickVisitAsync` | 62 | public | 待符号级复核 |
| RegistrationsHttpApiClient | `DeleteRegistrationAsync` | 65 | public | 待符号级复核 |
| HttpApiClientBase | `HttpApiClientBase` | 40 | protected | 待符号级复核 |
| HttpApiClientBase | `ToJsonContent` | 47 | protected | 待符号级复核 |
| HttpApiClientBase | `DeserializeAsync` | 53 | protected | 待符号级复核 |
| HttpApiClientBase | `DeserializeEnvelopeAsync` | 63 | protected | 待符号级复核 |
| HttpApiClientBase | `BuildQueryString` | 117 | protected | 待符号级复核 |
| HttpApiClientBase | `GetAndWrapAsync` | 165 | protected | 待符号级复核 |
| HttpApiClientBase | `GetRawAsync` | 172 | protected | 待符号级复核 |
| HttpApiClientBase | `PostAndWrapAsync` | 180 | protected | 待符号级复核 |
| HttpApiClientBase | `PostRawAsync` | 195 | protected | 待符号级复核 |
| HttpApiClientBase | `PutAndWrapAsync` | 203 | protected | 待符号级复核 |
| HttpApiClientBase | `SendAndWrapAsync` | 215 | protected | 待符号级复核 |
| HttpApiClientBase | `GetPagedAndWrapAsync` | 222 | protected | 待符号级复核 |
| HttpClientApiClientExtensions | `AddHttpClientApiClient` | 43 | public | 待符号级复核 |
| HttpClientApiClientExtensions | `AddHttpClientApiClient` | 86 | public | 待符号级复核 |
| RefitApiClientExtensions | `AddRefitApiClient` | 34 | public | 待符号级复核 |
| RetryPolicyExtensions | `CreateCompositePolicy` | 86 | public | 待符号级复核 |
| IModuleLoadingService | `GetLoadedModules` | 22 | public (interface 默认) | 待符号级复核 |
| IModuleLoadingService | `LoadModulesAsync` | 39 | public (interface 默认) | 待符号级复核 |
| ModuleLoadingService | `ModuleLoadingService` | 19 | public | 待符号级复核 |
| ModuleLoadingService | `GetLoadedModules` | 83 | public | 待符号级复核 |
| ModuleLoadingService | `LoadModulesAsync` | 99 | public | 待符号级复核 |
| EntityApiClientRepositoryBase | `EntityApiClientRepositoryBase` | 31 | protected | 待符号级复核 |
| AuthenticationService | `AuthenticationService` | 28 | public | 待符号级复核 |
| AuthenticationService | `IsLoggedInAsync` | 47 | public | 待符号级复核 |
| AuthenticationService | `CheckConnectionAsync` | 221 | public | 待符号级复核 |
| AuthenticationStateMachine | `AuthenticationStateMachine` | 120 | public | 待符号级复核 |
| AuthenticationStateMachine | `AuthenticationStateMachine` | 131 | internal | 待符号级复核 |
| AuthenticationStateMachine | `CanFire` | 140 | public | 待符号级复核 |
| AuthenticationStateMachine | `FireAsync` | 180 | public | 待符号级复核 |
| AuthenticationStateMachine | `GetPermittedEvents` | 192 | public | 待符号级复核 |
| AuthenticationStateMachine | `ForceState` | 206 | internal | 待符号级复核 |
| CredentialVault | `CredentialVault` | 23 | public | 待符号级复核 |
| CredentialVault | `VerifyIntegrityAsync` | 407 | public | 待符号级复核 |
| CredentialVault | `MigrateOldFormatAsync` | 446 | public | 待符号级复核 |
| CredentialVault | `HasValidTokenAsync` | 491 | public | 待符号级复核 |
| DpapiPhotoStorageService | `DpapiPhotoStorageService` | 17 | public | 待符号级复核 |
| DpapiPhotoStorageService | `LoadPhotoAsync` | 71 | public | 待符号级复核 |
| DpapiPhotoStorageService | `DeletePhotoAsync` | 111 | public | 待符号级复核 |
| DpapiPhotoStorageService | `PhotoExists` | 138 | public | 待符号级复核 |
| IAuthenticationService | `IsLoggedInAsync` | 18 | public (interface 默认) | 待符号级复核 |
| IAuthenticationService | `CheckConnectionAsync` | 59 | public (interface 默认) | 待符号级复核 |
| ICredentialVault | `VerifyIntegrityAsync` | 78 | public (interface 默认) | 待符号级复核 |
| ICredentialVault | `MigrateOldFormatAsync` | 84 | public (interface 默认) | 待符号级复核 |
| ICredentialVault | `HasValidTokenAsync` | 91 | public (interface 默认) | 待符号级复核 |
| ILogoutService | `ProcessPendingServerLogoutsAsync` | 37 | public (interface 默认) | 待符号级复核 |
| IPhotoStorageService | `LoadPhotoAsync` | 22 | public (interface 默认) | 待符号级复核 |
| IPhotoStorageService | `DeletePhotoAsync` | 29 | public (interface 默认) | 待符号级复核 |
| IPhotoStorageService | `PhotoExists` | 34 | public (interface 默认) | 待符号级复核 |
| ITokenManager | `SetTokens` | 39 | public (interface 默认) | 待符号级复核 |
| ITokenManager | `ClearTokens` | 44 | public (interface 默认) | 待符号级复核 |
| ITokenManager | `IsTokenValid` | 50 | public (interface 默认) | 待符号级复核 |
| ITokenManager | `IsTokenExpiringSoon` | 57 | public (interface 默认) | 待符号级复核 |
| ITokenValidator | `ValidateAndGetUserInfoAsync` | 28 | public (interface 默认) | 待符号级复核 |
| LocalTokenValidator | `LocalTokenValidator` | 29 | public | 待符号级复核 |
| LocalTokenValidator | `ValidateAndGetUserInfoAsync` | 177 | public | 待符号级复核 |
| LogoutService | `LogoutService` | 37 | public | 待符号级复核 |
| LogoutService | `ProcessPendingServerLogoutsAsync` | 141 | public | 待符号级复核 |
| TokenLifecycleService | `TokenLifecycleService` | 45 | public | 待符号级复核 |
| TokenLifecycleService | `OnMonitorTick` | 219 | private | 待符号级复核 |
| TokenManager | `TokenManager` | 23 | public | 待符号级复核 |
| TokenManager | `SetTokens` | 66 | public | 待符号级复核 |
| TokenManager | `ClearTokens` | 89 | public | 待符号级复核 |
| TokenManager | `IsTokenValid` | 102 | public | 待符号级复核 |
| TokenManager | `IsTokenExpiringSoon` | 122 | public | 待符号级复核 |
| TokenRefreshResult | `TokenRefreshResult` | 80 | private | 待符号级复核 |
| TokenStorageService | `TokenStorageService` | 27 | public | 待符号级复核 |
| UsernameStorageService | `UsernameStorageService` | 18 | public | 待符号级复核 |
| ConnectionModeService | `ConnectionModeService` | 45 | public | 待符号级复核 |
| ConnectionModeService | `TestLocalConnectionAsync` | 123 | public | 待符号级复核 |
| ConnectionModeService | `OnUrlChanged` | 188 | private | 待符号级复核 |
### LYBT.Desktop.Herbs（12 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| HerbEditControl | `HerbEditControl` | 16 | public | 待符号级复核 |
| HerbMasterDetailControl | `HerbMasterDetailControl` | 12 | public | 待符号级复核 |
| HerbViewControl | `HerbViewControl` | 11 | public | 待符号级复核 |
| HerbsModule | `RegisterTypes` | 27 | public | 框架反射模式 |
| HerbSearchProvider | `HerbSearchProvider` | 15 | public | 待符号级复核 |
| RemoteHerbService | `RemoteHerbService` | 21 | public | 待符号级复核 |
| HerbStatusHandler | `HerbStatusHandler` | 19 | public | 待符号级复核 |
| HerbEditorViewModel | `OnHerbPropertyChanged` | 103 | private | 待符号级复核 |
| HerbMasterDetailViewModel | `HerbMasterDetailViewModel` | 50 | public | 待符号级复核 |
| HerbMasterDetailViewModel | `CanToggleStatus` | 231 | private | 待符号级复核 |
| HerbMasterDetailViewModel | `CanCopyHerb` | 250 | private | 待符号级复核 |
| HerbMasterDetailViewModel | `SearchByCategoryAsync` | 267 | private | 待符号级复核 |
### LYBT.Desktop.Infrastructure（172 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| DataGridSelectionBehavior | `GetShowCheckBoxColumn` | 32 | public | 待符号级复核 |
| DataGridSelectionBehavior | `SetShowCheckBoxColumn` | 35 | public | 待符号级复核 |
| DataGridSelectionBehavior | `SetSelectedItems` | 52 | public | 待符号级复核 |
| DataGridSelectionBehavior | `OnShowCheckBoxColumnChanged` | 72 | private | 待符号级复核 |
| DataGridSelectionBehavior | `DataGrid_Loaded` | 93 | private | 待符号级复核 |
| DataGridSelectionBehavior | `DataGrid_SelectionChanged` | 281 | private | 待符号级复核 |
| DataGridSelectionBehavior | `OnSelectedItemsChanged` | 309 | private | 待符号级复核 |
| DataGridSelectionBehavior | `FindVisualParent` | 347 | private | 待符号级复核 |
| PasswordBoxHelper | `GetBoundPassword` | 18 | public | 待符号级复核 |
| PasswordBoxHelper | `OnBoundPasswordChanged` | 24 | private | 待符号级复核 |
| PasswordBoxHelper | `PasswordBox_PasswordChanged` | 38 | private | 待符号级复核 |
| ICardReaderFactory | `GetSupportedReaders` | 14 | public (interface 默认) | 待符号级复核 |
| CardReaderModule | `RegisterTypes` | 26 | public | 框架反射模式 |
| CardReaderServiceCollectionExtensions | `AddCardReaderServices` | 45 | public | 待符号级复核 |
| IPatientCardReaderIntegration | `MatchPatientAsync` | 47 | public (interface 默认) | 待符号级复核 |
| HuaDaNativeMethods | `HD_ReadCard` | 48 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetCertNo` | 87 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetSex` | 91 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetNation` | 95 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetBirth` | 99 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetAddress` | 103 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetDepartemt` | 107 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetEffectDate` | 111 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetExpireDate` | 115 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetBmpFileData` | 129 | public | 待符号级复核 |
| HuaDaNativeMethods | `GetBmpFile` | 137 | public | 待符号级复核 |
| HuaDaNativeMethods | `PtrToString` | 146 | public | 待符号级复核 |
| HuaDaNativeMethods | `IsDllAvailable` | 157 | public | 待符号级复核 |
| CardReaderFactory | `CardReaderFactory` | 47 | public | 待符号级复核 |
| CardReaderFactory | `GetSupportedReaders` | 55 | public | 待符号级复核 |
| CardReaderService | `CardReaderService` | 50 | public | 待符号级复核 |
| CardReaderService | `AutoReadCallback` | 269 | private | 待符号级复核 |
| CardReaderService | `OnReaderConnectionStateChanged` | 315 | private | 待符号级复核 |
| ApplicationCommands | `ApplicationCommands` | 36 | public | 待符号级复核 |
| ViewModelServicesExtensions | `AddListViewServices` | 63 | public | 待符号级复核 |
| ViewModelServicesExtensions | `AddMasterDetailServices` | 77 | public | 待符号级复核 |
| DesktopExceptionHandler | `DesktopExceptionHandler` | 17 | public | 待符号级复核 |
| DesktopExceptionHandler | `LogException` | 36 | public | 待符号级复核 |
| DesktopExceptionHandler | `CanRetry` | 57 | public | 待符号级复核 |
| DesktopExceptionHandler | `UnregisterGlobalExceptionHandlers` | 95 | public | 待符号级复核 |
| DesktopExceptionHandler | `OnUnhandledException` | 113 | private | 待符号级复核 |
| DesktopExceptionHandler | `OnUnobservedTaskException` | 134 | private | 待符号级复核 |
| DesktopExceptionHandler | `SafeExecuteAsync` | 216 | public | 待符号级复核 |
| DesktopExceptionHandler | `SafeExecuteAsync` | 229 | public | 待符号级复核 |
| IDesktopExceptionHandler | `LogException` | 24 | public (interface 默认) | 待符号级复核 |
| IDesktopExceptionHandler | `CanRetry` | 34 | public (interface 默认) | 待符号级复核 |
| IDesktopExceptionHandler | `UnregisterGlobalExceptionHandlers` | 47 | public (interface 默认) | 待符号级复核 |
| IDesktopExceptionHandler | `SafeExecuteAsync` | 66 | public (interface 默认) | 待符号级复核 |
| IDesktopExceptionHandler | `SafeExecuteAsync` | 71 | public (interface 默认) | 待符号级复核 |
| ModuleLazyLoader | `ModuleLazyLoader` | 38 | public | 待符号级复核 |
| NavigationCoordinator | `NavigationCoordinator` | 27 | public | 待符号级复核 |
| NavigationCoordinator | `SubscribeToRegionCollection` | 294 | public | 待符号级复核 |
| NavigationCoordinator | `UnsubscribeFromRegionCollection` | 297 | public | 待符号级复核 |
| NavigationHistoryService | `NavigationHistoryService` | 20 | public | 待符号级复核 |
| NavigationServices | `NavigationServices` | 8 | public | 待符号级复核 |
| RegionMonitor | `RegionMonitor` | 15 | public | 待符号级复核 |
| RegionMonitor | `OnRegionsCollectionChanged` | 42 | private | 待符号级复核 |
| PerformanceMonitor | `PerformanceMonitor` | 26 | public | 待符号级复核 |
| PerformanceMonitor | `RecordMemoryBaseline` | 108 | public | 待符号级复核 |
| PerformanceMonitor | `GetMemorySnapshots` | 127 | public | 待符号级复核 |
| PerformanceMonitor | `GetMetric` | 136 | public | 待符号级复核 |
| PerformanceMonitor | `GetAllMetrics` | 145 | public | 待符号级复核 |
| PerformanceMonitor | `GenerateReport` | 154 | public | 待符号级复核 |
| RoleRegistry | `GetAllDefinitions` | 57 | public | 待符号级复核 |
| RoleRegistry | `IsRegistered` | 63 | public | 待符号级复核 |
| ActiveConsultationService | `ActiveConsultationService` | 18 | public | 待符号级复核 |
| ApplicationTickService | `ApplicationTickService` | 48 | public | 待符号级复核 |
| ApplicationTickService | `OnTimerTick` | 100 | private | 待符号级复核 |
| AsyncExecutor | `AsyncExecutor` | 15 | public | 待符号级复核 |
| AsyncExecutor | `ExecuteSafelyAsync` | 22 | public | 待符号级复核 |
| AsyncExecutor | `ExecuteSafelyAsync` | 38 | public | 待符号级复核 |
| AsyncExecutor | `ExecuteWithRetryAsync` | 53 | public | 待符号级复核 |
| AsyncExecutor | `ExecuteWithRetryAsync` | 84 | public | 待符号级复核 |
| AsyncExecutor | `ExecuteOnUIThread` | 114 | public | 待符号级复核 |
| AsyncExecutor | `ExecuteOnUIThreadAsync` | 127 | public | 待符号级复核 |
| AsyncExecutor | `ExecuteWithTimeoutAsync` | 139 | public | 待符号级复核 |
| ClinicSettingsService | `ClinicSettingsService` | 33 | public | 待符号级复核 |
| CommonDialogService | `ShowInfoAsync` | 23 | public | 待符号级复核 |
| CommonDialogService | `ShowInputAsync` | 78 | public | 待符号级复核 |
| CommonDialogService | `ShowOpenFileDialogAsync` | 87 | public | 待符号级复核 |
| CommonDialogService | `ShowSaveFileDialogAsync` | 102 | public | 待符号级复核 |
| DialogManager | `DialogManager` | 19 | public | 待符号级复核 |
| DialogManager | `ShowInfoAsync` | 43 | public | 待符号级复核 |
| DialogManager | `ShowInputAsync` | 93 | public | 待符号级复核 |
| DialogManager | `ShowDialogAsync` | 120 | public | 待符号级复核 |
| ErrorHandler | `ErrorHandler` | 22 | public | 待符号级复核 |
| IAsyncExecutor | `ExecuteSafelyAsync` | 16 | public (interface 默认) | 待符号级复核 |
| IAsyncExecutor | `ExecuteSafelyAsync` | 26 | public (interface 默认) | 待符号级复核 |
| IAsyncExecutor | `ExecuteWithRetryAsync` | 36 | public (interface 默认) | 待符号级复核 |
| IAsyncExecutor | `ExecuteWithRetryAsync` | 51 | public (interface 默认) | 待符号级复核 |
| IAsyncExecutor | `ExecuteOnUIThread` | 61 | public (interface 默认) | 待符号级复核 |
| IAsyncExecutor | `ExecuteOnUIThreadAsync` | 67 | public (interface 默认) | 待符号级复核 |
| IAsyncExecutor | `ExecuteWithTimeoutAsync` | 76 | public (interface 默认) | 待符号级复核 |
| IDialogManager | `ShowInfoAsync` | 36 | public (interface 默认) | 待符号级复核 |
| IDialogManager | `ShowInputAsync` | 53 | public (interface 默认) | 待符号级复核 |
| IDialogManager | `ShowDialogAsync` | 62 | public (interface 默认) | 待符号级复核 |
| ISelectionService | `SelectMultiple` | 44 | public (interface 默认) | 待符号级复核 |
| ISelectionService | `ToggleSelection` | 50 | public (interface 默认) | 待符号级复核 |
| SelectionChangedEventArgs | `SelectionChangedEventArgs` | 71 | public | 待符号级复核 |
| ListViewServices | `ListViewServices` | 30 | public | 待符号级复核 |
| MasterDetailServices | `MasterDetailServices` | 48 | public | 待符号级复核 |
| INotificationService | `ShowInfoAsync` | 37 | public (interface 默认) | 待符号级复核 |
| INotificationService | `ShowLoading` | 57 | public (interface 默认) | 待符号级复核 |
| INotificationService | `HideLoading` | 62 | public (interface 默认) | 待符号级复核 |
| NotificationService | `NotificationService` | 16 | public | 待符号级复核 |
| NotificationService | `ShowInfoAsync` | 75 | public | 待符号级复核 |
| NotificationService | `ShowLoading` | 122 | public | 待符号级复核 |
| NotificationService | `HideLoading` | 143 | public | 待符号级复核 |
| PaginationService | `OnPageSizeChanged` | 108 | default(private) | 待符号级复核 |
| SelectionService | `SelectionService` | 32 | public | 待符号级复核 |
| SelectionService | `SelectMultiple` | 60 | public | 待符号级复核 |
| SelectionService | `ToggleSelection` | 75 | public | 待符号级复核 |
| SelectionService | `OnSelectedItemChanged` | 116 | default(private) | 待符号级复核 |
| SessionManager | `SessionManager` | 21 | public | 待符号级复核 |
| SessionManager | `SetSession` | 43 | public | 待符号级复核 |
| SessionManager | `HasRole` | 73 | public | 待符号级复核 |
| SessionManager | `IsAdmin` | 74 | public | 待符号级复核 |
| SessionManager | `GetCurrentUserRoleDisplay` | 75 | public | 待符号级复核 |
| ToastService | `ToastService` | 20 | public | 待符号级复核 |
| UserActivityTracker | `OnPreProcessInput` | 192 | private | 待符号级复核 |
| UserActivityTracker | `OnTick` | 213 | private | 待符号级复核 |
| UserNotificationService | `ShowInfoAsync` | 68 | public | 待符号级复核 |
| WpfUiThreadDispatcher | `WpfUiThreadDispatcher` | 11 | public | 待符号级复核 |
| WpfUiThreadDispatcher | `WpfUiThreadDispatcher` | 17 | internal | 待符号级复核 |
| DialogViewModelBase | `CanCloseDialog` | 47 | public | 待符号级复核 |
| DialogViewModelBase | `OnDialogClosed` | 52 | public | 待符号级复核 |
| DialogViewModelBase | `OnDialogOpened` | 61 | public | 待符号级复核 |
| DialogViewModelBase | `DialogViewModelBase` | 76 | protected | 待符号级复核 |
| DialogViewModelBase | `CloseDialogWithResult` | 125 | protected | 待符号级复核 |
| DialogViewModelBase | `TryGetDialogParameter` | 174 | protected | 待符号级复核 |
| HerbItemViewModelBase | `OnHerbNameChanged` | 80 | default(private) | 待符号级复核 |
| HerbItemViewModelBase | `OnSelectedHerbChanged` | 88 | default(private) | 待符号级复核 |
| NavigableViewModelBase | `ExecuteWithErrorHandlingAsync` | 22 | protected | 待符号级复核 |
| NavigableViewModelBase | `ExecuteWithErrorHandlingAsync` | 62 | protected | 待符号级复核 |
| NavigableViewModelBase | `RunOnUIThread` | 102 | protected | 待符号级复核 |
| NavigableViewModelBase | `RunOnUIThreadAsync` | 110 | protected | 待符号级复核 |
| NavigableViewModelBase | `NavigableViewModelBase` | 205 | protected | 待符号级复核 |
| NavigableViewModelBase | `AddDisposable` | 284 | protected | 待符号级复核 |
| NavigableViewModelBase | `IsNavigationTarget` | 23 | public | 待符号级复核 |
| NavigableViewModelBase | `ConfirmNavigationRequest` | 75 | public | 待符号级复核 |
| ValidatableModelBase | `ValidatableModelBase` | 39 | protected | 待符号级复核 |
| ChildViewModelBase | `ChildViewModelBase` | 16 | protected | 待符号级复核 |
| MasterDetailCommandGroup | `MasterDetailCommandGroup` | 17 | public | 待符号级复核 |
| MasterDetailCommandGroup | `ClearSearchAsync` | 47 | private | 待符号级复核 |
| MasterDetailCommandGroup | `CanGoToFirstPage` | 82 | private | 待符号级复核 |
| MasterDetailCommandGroup | `CanGoToPreviousPage` | 83 | private | 待符号级复核 |
| MasterDetailCommandGroup | `CanGoToNextPage` | 84 | private | 待符号级复核 |
| MasterDetailCommandGroup | `CanGoToLastPage` | 85 | private | 待符号级复核 |
| MasterDetailCommandGroup | `CanCreateNew` | 214 | private | 待符号级复核 |
| MasterDetailCommandGroup | `CanSave` | 216 | private | 待符号级复核 |
| MasterDetailCommandGroup | `CanCancel` | 217 | private | 待符号级复核 |
| MasterDetailCommandGroup | `CanRestore` | 219 | private | 待符号级复核 |
| ServiceEventBridge | `ServiceEventBridge` | 19 | public | 待符号级复核 |
| ServiceEventBridge | `ForwardPropertyChanged` | 40 | private | 待符号级复核 |
| ServiceEventBridge | `OnLoadingPropertyChanged` | 43 | private | 待符号级复核 |
| ServiceEventBridge | `OnPaginationPropertyChanged` | 59 | private | 待符号级复核 |
| ServiceEventBridge | `OnPaginationPageChanged` | 74 | private | 待符号级复核 |
| ServiceEventBridge | `OnSelectionPropertyChanged` | 80 | private | 待符号级复核 |
| ServiceEventBridge | `OnSelectionSelectionChanged` | 89 | private | 待符号级复核 |
| ServiceEventBridge | `OnDetailEditorPropertyChanged` | 95 | private | 待符号级复核 |
| ServiceEventBridge | `OnErrorHandlerPropertyChanged` | 116 | private | 待符号级复核 |
| BaseStatusHandler | `BaseStatusHandler` | 16 | protected | 待符号级复核 |
| MasterDetailViewModelBase | `MasterDetailViewModelBase` | 118 | protected | 待符号级复核 |
| MasterDetailViewModelBase | `ClearSearchAsync` | 133 | default(private) | 待符号级复核 |
| MasterDetailViewModelBase | `CanGoToFirstPage` | 147 | private | 待符号级复核 |
| MasterDetailViewModelBase | `CanGoToPreviousPage` | 148 | private | 待符号级复核 |
| MasterDetailViewModelBase | `CanGoToNextPage` | 149 | private | 待符号级复核 |
| MasterDetailViewModelBase | `CanGoToLastPage` | 150 | private | 待符号级复核 |
| MasterDetailViewModelBase | `CanCreateNew` | 151 | private | 待符号级复核 |
| MasterDetailViewModelBase | `CanSave` | 153 | private | 待符号级复核 |
| MasterDetailViewModelBase | `CanCancel` | 154 | private | 待符号级复核 |
| MasterDetailViewModelBase | `CanRestore` | 156 | private | 待符号级复核 |
### LYBT.Desktop.MedicalCase（59 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| MedicalCaseEditControl | `MedicalCaseEditControl` | 18 | public | 待符号级复核 |
| MedicalCaseMasterDetailControl | `MedicalCaseMasterDetailControl` | 12 | public | 待符号级复核 |
| MedicalCaseViewControl | `MedicalCaseViewControl` | 19 | public | 待符号级复核 |
| WorkflowStepIndicator | `WorkflowStepIndicator` | 14 | public | 待符号级复核 |
| WorkflowStepIndicator | `OnCurrentStepChanged` | 33 | private | 待符号级复核 |
| FormulaImportDialog | `FormulaImportDialog` | 11 | public | 待符号级复核 |
| FormulaImportDialogViewModel | `OnSearchTextChanged` | 94 | default(private) | 待符号级复核 |
| FormulaImportDialogViewModel | `OnSelectedCategoryChanged` | 102 | default(private) | 待符号级复核 |
| FormulaImportDialogViewModel | `OnSelectedFormulaChanged` | 110 | default(private) | 待符号级复核 |
| FormulaImportDialogViewModel | `FormulaImportDialogViewModel` | 122 | public | 待符号级复核 |
| HistoryCopyDialog | `HistoryCopyDialog` | 11 | public | 待符号级复核 |
| HistoryCopyDialogViewModel | `OnSearchTextChanged` | 149 | default(private) | 待符号级复核 |
| HistoryCopyDialogViewModel | `OnStartDateChanged` | 157 | default(private) | 待符号级复核 |
| HistoryCopyDialogViewModel | `OnEndDateChanged` | 165 | default(private) | 待符号级复核 |
| HistoryCopyDialogViewModel | `OnSelectedCaseChanged` | 173 | default(private) | 待符号级复核 |
| HistoryCopyDialogViewModel | `OnIsShowingAllPatientsChanged` | 181 | default(private) | 待符号级复核 |
| HistoryCopyDialogViewModel | `OnIsShowingAllCurrentPatientChanged` | 198 | default(private) | 待符号级复核 |
| HistoryCopyDialogViewModel | `HistoryCopyDialogViewModel` | 210 | public | 待符号级复核 |
| HistoryCopyDialogViewModel | `ShowMoreCurrentPatient` | 272 | private | 待符号级复核 |
| HistoryCopyDialogViewModel | `ToggleAllPatients` | 281 | private | 待符号级复核 |
| UnsavedChangesDialog | `UnsavedChangesDialog` | 10 | public | 待符号级复核 |
| UnsavedChangesDialogViewModel | `UnsavedChangesDialogViewModel` | 17 | public | 待符号级复核 |
| UnsavedChangesDialogViewModel | `Discard` | 38 | private | 待符号级复核 |
| IEditModeStateMachine | `CanFire` | 26 | public (interface 默认) | 待符号级复核 |
| IEditModeStateMachine | `GetPermittedEvents` | 35 | public (interface 默认) | 待符号级复核 |
| MedicalCaseModule | `RegisterTypes` | 35 | public | 框架反射模式 |
| WorkspaceState | `EnterReadOnlyMode` | 88 | public | 待符号级复核 |
| ReportsModule | `RegisterTypes` | 21 | public | 框架反射模式 |
| ReportService | `ReportService` | 19 | public | 待符号级复核 |
| ReportsHomeViewModel | `ReportsHomeViewModel` | 33 | public | 待符号级复核 |
| ReportsHomeViewModel | `OnSelectedDateChanged` | 41 | default(private) | 待符号级复核 |
| ReportsHomeViewModel | `IsNavigationTarget` | 52 | public | 待符号级复核 |
| ReportsHomeViewModel | `GoToToday` | 60 | private | 待符号级复核 |
| ReportsHomeView | `ReportsHomeView` | 7 | public | 待符号级复核 |
| AuditLogService | `AuditLogService` | 20 | public | 待符号级复核 |
| MedicalCaseCommandService | `MedicalCaseCommandService` | 21 | public | 待符号级复核 |
| MedicalCaseLifecycleService | `MedicalCaseLifecycleService` | 23 | public | 待符号级复核 |
| MedicalCaseQueryService | `MedicalCaseQueryService` | 18 | public | 待符号级复核 |
| MedicalCaseService | `MedicalCaseService` | 29 | public | 待符号级复核 |
| MedicalCaseService | `GetByIdSimpleAsync` | 240 | public | 待符号级复核 |
| MedicalCaseService | `CancelMedicalCaseViaApiAsync` | 330 | public | 待符号级复核 |
| AuditLogViewModel | `AuditLogViewModel` | 30 | public | 待符号级复核 |
| EditModeStateMachine | `CanFire` | 93 | public | 待符号级复核 |
| EditModeStateMachine | `GetPermittedEvents` | 170 | public | 待符号级复核 |
| PrescriptionPrintHandler | `PrescriptionPrintHandler` | 35 | public | 待符号级复核 |
| PrintResult | `PrintResult` | 321 | private | 待符号级复核 |
| MedicalCaseMasterDetailViewModel | `MedicalCaseMasterDetailViewModel` | 58 | public | 待符号级复核 |
| MedicalCaseCommandsViewModel | `ExecuteSaveAsync` | 115 | private | 待符号级复核 |
| MedicalCaseCommandsViewModel | `ExecuteSuspendAsync` | 149 | private | 待符号级复核 |
| MedicalCaseCommandsViewModel | `ExecuteCompleteAsync` | 183 | private | 待符号级复核 |
| MedicalCaseCommandsViewModel | `ExecutePrintAsync` | 220 | private | 待符号级复核 |
| MedicalCaseCommandsViewModel | `ExecuteExportPdfAsync` | 254 | private | 待符号级复核 |
| MedicalCaseCommandsViewModel | `ExecuteEnterEditMode` | 293 | private | 待符号级复核 |
| MedicalCaseCommandsViewModel | `ExecuteImportFormula` | 302 | private | 待符号级复核 |
| MedicalCaseCommandsViewModel | `ExecuteCopyHistory` | 317 | private | 待符号级复核 |
| MedicalCaseCommandsViewModel | `ExecuteClearHerbsAsync` | 339 | private | 待符号级复核 |
| PrescriptionEditorViewModel | `OnItemsCollectionChanged` | 85 | private | 待符号级复核 |
| AuditLogView | `AuditLogView` | 7 | public | 待符号级复核 |
| MedicalCaseMasterDetailView | `MedicalCaseMasterDetailView` | 12 | public | 待符号级复核 |
### LYBT.Desktop.Patients（20 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| PatientEditControl | `PatientEditControl` | 18 | public | 待符号级复核 |
| PatientMasterDetailControl | `PatientMasterDetailControl` | 12 | public | 待符号级复核 |
| PatientSelectionControl | `PatientSelectionControl` | 28 | public | 待符号级复核 |
| PatientSelectionControl | `PatientDataGrid_MouseDoubleClick` | 43 | private | 待符号级复核 |
| PatientSelectionControl | `GetPropertyValue` | 63 | private | 待符号级复核 |
| PatientViewControl | `PatientViewControl` | 11 | public | 待符号级复核 |
| PatientItem | `UpdateFromDto` | 163 | public | 待符号级复核 |
| PatientsModule | `RegisterTypes` | 33 | public | 框架反射模式 |
| PatientCardReaderIntegration | `PatientCardReaderIntegration` | 21 | public | 待符号级复核 |
| PatientCardReaderIntegration | `MatchPatientAsync` | 148 | public | 待符号级复核 |
| PatientService | `PatientService` | 21 | public | 待符号级复核 |
| PatientService | `BatchDeletePatientsAsync` | 98 | public | 待符号级复核 |
| PatientStatusHandler | `PatientStatusHandler` | 17 | public | 待符号级复核 |
| PatientCardReaderViewModel | `PatientCardReaderViewModel` | 23 | public | 待符号级复核 |
| PatientCardReaderViewModel | `CanReadCard` | 86 | private | 待符号级复核 |
| PatientEditorViewModel | `OnPatientPropertyChanged` | 96 | private | 待符号级复核 |
| PatientMasterDetailViewModel | `PatientMasterDetailViewModel` | 75 | public | 待符号级复核 |
| PatientMasterDetailViewModel | `CanViewMedicalRecords` | 250 | private | 待符号级复核 |
| PatientMasterDetailViewModel | `CanNewConsultation` | 262 | private | 待符号级复核 |
| PatientMasterDetailViewModel | `CanReadCard` | 301 | private | 待符号级复核 |
### LYBT.Desktop.Printing（11 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| IPrintService | `BatchPrintAsync` | 40 | public (interface 默认) | 待符号级复核 |
| PrintingModule | `RegisterTypes` | 21 | public | 框架反射模式 |
| PrescriptionDocumentBuilder | `PrescriptionDocumentBuilder` | 29 | public | 待符号级复核 |
| PrescriptionPreviewWindowBuilder | `PrescriptionPreviewWindowBuilder` | 21 | public | 待符号级复核 |
| PrescriptionPrintExecutor | `PrescriptionPrintExecutor` | 20 | public | 待符号级复核 |
| PrescriptionPrintService | `PrescriptionPrintService` | 22 | public | 待符号级复核 |
| PrescriptionPrintService | `BatchPrintAsync` | 173 | public | 待符号级复核 |
| PrescriptionContinuationA4Template | `PrescriptionContinuationA4Template` | 12 | public | 待符号级复核 |
| PrescriptionContinuationTemplate | `PrescriptionContinuationTemplate` | 12 | public | 待符号级复核 |
| PrescriptionPrintA4Template | `PrescriptionPrintA4Template` | 11 | public | 待符号级复核 |
| PrescriptionPrintTemplate | `PrescriptionPrintTemplate` | 10 | public | 待符号级复核 |
### LYBT.Desktop.Registrations（20 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| RegistrationCreateDialog | `RegistrationCreateDialog` | 10 | public | 待符号级复核 |
| RegistrationCreateDialogViewModel | `RegistrationCreateDialogViewModel` | 57 | public | 待符号级复核 |
| RegistrationCreateDialogViewModel | `SelectPatient` | 178 | private | 待符号级复核 |
| RegistrationCreateDialogViewModel | `ClearPatientSelection` | 189 | private | 待符号级复核 |
| RegistrationCreateDialogViewModel | `OnPatientSearchTextChanged` | 198 | default(private) | 待符号级复核 |
| RegistrationCreateDialogViewModel | `OnSelectedPatientChanged` | 207 | default(private) | 待符号级复核 |
| RegistrationCreateDialogViewModel | `OnSelectedDoctorChanged` | 212 | default(private) | 待符号级复核 |
| RegistrationModule | `RegisterTypes` | 28 | public | 框架反射模式 |
| RemoteRegistrationService | `RemoteRegistrationService` | 21 | public | 待符号级复核 |
| SignalRClient | `SignalRClient` | 38 | public | 待符号级复核 |
| SignalRClient | `OnReconnectedAsync` | 116 | private | 待符号级复核 |
| SignalRClient | `OnClosedAsync` | 123 | private | 待符号级复核 |
| RegistrationListViewModel | `RegistrationListViewModel` | 77 | public | 待符号级复核 |
| RegistrationListViewModel | `OnRegistrationRefreshed` | 160 | private | 待符号级复核 |
| RegistrationListViewModel | `CreateRegistration` | 179 | private | 待符号级复核 |
| RegistrationListViewModel | `CanStartVisit` | 255 | private | 待符号级复核 |
| RegistrationListViewModel | `CancelRegistrationAsync` | 263 | private | 待符号级复核 |
| RegistrationListViewModel | `CanCancelRegistration` | 298 | private | 待符号级复核 |
| RegistrationListViewModel | `OnSelectedRegistrationChanged` | 345 | default(private) | 待符号级复核 |
| RegistrationListView | `RegistrationListView` | 11 | public | 待符号级复核 |
### LYBT.Desktop.Shell（81 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| App | `CreateShell` | 80 | protected | 待符号级复核 |
| App | `InitializeShell` | 83 | protected | 待符号级复核 |
| App | `RegisterTypes` | 90 | protected | 框架反射模式 |
| AccountSettingsControl | `AccountSettingsControl` | 14 | public | 待符号级复核 |
| AccountSettingsControl | `OnDataContextChanged` | 23 | private | 待符号级复核 |
| AccountSettingsControl | `OnOldPasswordChanged` | 35 | private | 待符号级复核 |
| AccountSettingsControl | `OnNewPasswordChanged` | 44 | private | 待符号级复核 |
| AccountSettingsControl | `OnConfirmPasswordChanged` | 53 | private | 待符号级复核 |
| ConfirmationDialogViewModel | `ConfirmationDialogViewModel` | 86 | public | 待符号级复核 |
| InputDialogViewModel | `InputDialogViewModel` | 62 | public | 待符号级复核 |
| InputDialogViewModel | `OnInputValueChanged` | 138 | default(private) | 待符号级复核 |
| MessageDialogViewModel | `MessageDialogViewModel` | 90 | public | 待符号级复核 |
| ConfirmationDialog | `ConfirmationDialog` | 11 | public | 待符号级复核 |
| InputDialog | `InputDialog` | 10 | public | 待符号级复核 |
| MessageDialog | `MessageDialog` | 10 | public | 待符号级复核 |
| PrismConfigurationExtensions | `RegisterOptions` | 53 | private | 待符号级复核 |
| StringResources | `StringResources` | 32 | internal | 待符号级复核 |
| AppStartupOrchestrator | `AppStartupOrchestrator` | 19 | public | 待符号级复核 |
| ApplicationBootstrapper | `ApplicationBootstrapper` | 23 | public | 待符号级复核 |
| EmbeddedLocalWebApiService | `EmbeddedLocalWebApiService` | 27 | public | 待符号级复核 |
| ApiHealthMonitor | `ApiHealthMonitor` | 33 | public | 待符号级复核 |
| ApiHealthMonitor | `StopMonitoringAsync` | 75 | public | 待符号级复核 |
| ApiHealthMonitor | `ResetCircuitBreaker` | 103 | public | 待符号级复核 |
| LoginCoordinator | `LoginCoordinator` | 40 | public | 待符号级复核 |
| LoginCoordinator | `HandleLoginSuccessAsync` | 183 | public | 待符号级复核 |
| LoginCoordinator | `GetDiagnostics` | 271 | public | 待符号级复核 |
| LoginCoordinator | `OnStateMachineStateChanged` | 287 | private | 待符号级复核 |
| LoginStateManager | `LoginStateManager` | 57 | public | 待符号级复核 |
| MenuManager | `MenuManager` | 29 | public | 待符号级复核 |
| MenuManager | `ExecuteAccountSettings` | 118 | private | 待符号级复核 |
| MenuManager | `ExecuteNavigateToHome` | 123 | private | 待符号级复核 |
| MenuManager | `ExecuteNavigateToSystemSettings` | 129 | private | 待符号级复核 |
| MenuManager | `ExecuteNavigateBack` | 136 | private | 待符号级复核 |
| MenuManager | `ExecuteNavigateForward` | 144 | private | 待符号级复核 |
| MenuManager | `ExecuteShowHelp` | 180 | private | 待符号级复核 |
| MenuManager | `ExecuteShowSettings` | 187 | private | 待符号级复核 |
| NavigationManager | `NavigationManager` | 32 | public | 待符号级复核 |
| NavigationManager | `OnSelectedNavItemChanged` | 42 | default(private) | 待符号级复核 |
| ISessionLifecycleManager | `UpdateTokenExpiration` | 95 | public (interface 默认) | 待符号级复核 |
| ISessionLifecycleManager | `RecordUserActivity` | 100 | public (interface 默认) | 待符号级复核 |
| ISessionLifecycleManager | `GetDiagnostics` | 105 | public (interface 默认) | 待符号级复核 |
| SessionBasedCurrentUserProvider | `SessionBasedCurrentUserProvider` | 9 | public | 待符号级复核 |
| SessionLifecycleManager | `SessionLifecycleManager` | 32 | public | 待符号级复核 |
| SessionLifecycleManager | `UpdateTokenExpiration` | 214 | public | 待符号级复核 |
| SessionLifecycleManager | `RecordUserActivity` | 226 | public | 待符号级复核 |
| SessionLifecycleManager | `GetDiagnostics` | 237 | public | 待符号级复核 |
| SessionLifecycleManager | `OnUserActivitySessionExpired` | 312 | private | 待符号级复核 |
| ShellDialogHelper | `ShellDialogHelper` | 15 | public | 待符号级复核 |
| ShellEventCoordinator | `ShellEventCoordinator` | 31 | public | 待符号级复核 |
| ShellEventCoordinator | `OnLoginSucceeded` | 56 | private | 待符号级复核 |
| ShellEventCoordinator | `OnPasswordChanged` | 115 | private | 待符号级复核 |
| ShellEventCoordinator | `OnProfileUpdated` | 143 | private | 待符号级复核 |
| ShellEventCoordinator | `OnLogoutRequested` | 206 | private | 待符号级复核 |
| ShellEventServices | `ShellEventServices` | 11 | public | 待符号级复核 |
| ShellServices | `ShellServices` | 9 | public | 待符号级复核 |
| StartupPipeline | `StartupPipeline` | 27 | public | 待符号级复核 |
| StartupPipeline | `GetDiagnostics` | 219 | public | 待符号级复核 |
| ApiHealthCheckStartupStep | `ApiHealthCheckStartupStep` | 18 | public | 待符号级复核 |
| ErrorHandlingStartupStep | `ErrorHandlingStartupStep` | 18 | public | 待符号级复核 |
| LocalWebApiStartupStep | `LocalWebApiStartupStep` | 16 | public | 待符号级复核 |
| ModuleCoordinatorStartupStep | `ModuleCoordinatorStartupStep` | 18 | public | 待符号级复核 |
| StatusBarManager | `StatusBarManager` | 55 | public | 待符号级复核 |
| StatusBarManager | `OnHealthStatusChanged` | 84 | private | 待符号级复核 |
| StatusBarManager | `OnConnectionUrlChanged` | 95 | private | 待符号级复核 |
| StatusBarManager | `OnConnectionModeChanged` | 105 | private | 待符号级复核 |
| AccountSettingsViewModel | `AccountSettingsViewModel` | 67 | public | 待符号级复核 |
| AccountSettingsViewModel | `SaveProfileAsync` | 82 | private | 待符号级复核 |
| AccountSettingsViewModel | `CanSaveProfile` | 135 | private | 待符号级复核 |
| AccountSettingsViewModel | `CanChangePassword` | 207 | private | 待符号级复核 |
| AccountSettingsViewModel | `IsNavigationTarget` | 272 | public | 待符号级复核 |
| MainWindowViewModel | `OnIsDarkModeChanged` | 73 | default(private) | 待符号级复核 |
| MainWindowViewModel | `MainWindowViewModel` | 100 | public | 待符号级复核 |
| MainWindowViewModel | `RetryHealthCheckAsync` | 175 | private | 待符号级复核 |
| MainWindowViewModel | `OnIsSidebarExpandedChanged` | 180 | default(private) | 待符号级复核 |
| MainWindowViewModel | `ToggleSidebar` | 186 | private | 待符号级复核 |
| MainWindowViewModel | `OnTick` | 195 | private | 待符号级复核 |
| MainWindowViewModel | `OnLoginStateChanged` | 200 | private | 待符号级复核 |
| MainWindowViewModel | `OnLoginSuccessHandled` | 211 | private | 待符号级复核 |
| AccountSettingsView | `AccountSettingsView` | 10 | public | 待符号级复核 |
| MainWindow | `MainWindow` | 18 | public | 待符号级复核 |
| MainWindow | `OnWindowLoaded` | 31 | private | 待符号级复核 |
### LYBT.Desktop.Users（14 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| UserEditControl | `UserEditControl` | 17 | public | 待符号级复核 |
| UserMasterDetailControl | `UserMasterDetailControl` | 13 | public | 待符号级复核 |
| UserViewControl | `UserViewControl` | 11 | public | 待符号级复核 |
| UserItem | `UpdateFromDto` | 240 | public | 待符号级复核 |
| RemoteUserService | `RemoteUserService` | 22 | public | 待符号级复核 |
| UsersModule | `RegisterTypes` | 28 | public | 框架反射模式 |
| IUserStatusHandler | `CanRestore` | 37 | public (interface 默认) | 待符号级复核 |
| UserPasswordHandler | `UserPasswordHandler` | 19 | public | 待符号级复核 |
| UserStatusHandler | `UserStatusHandler` | 21 | public | 待符号级复核 |
| UserStatusHandler | `CanRestore` | 67 | public | 待符号级复核 |
| UserEditorViewModel | `UserEditorViewModel` | 27 | public | 待符号级复核 |
| UserMasterDetailViewModel | `UserMasterDetailViewModel` | 110 | public | 待符号级复核 |
| UserMasterDetailViewModel | `CanClearFilters` | 341 | private | 待符号级复核 |
| UserMasterDetailViewModel | `OnDetailPropertyChanged` | 388 | private | 待符号级复核 |
### LYBT.Infrastructure（31 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| CacheInvalidationService | `CacheInvalidationService` | 17 | public | 待符号级复核 |
| SystemConfigurationService | `SystemConfigurationService` | 21 | public | 待符号级复核 |
| AppDbContextFactory | `CreateDbContext` | 16 | public | 待符号级复核 |
| EntityOptimizationExtensions | `ConfigureGlobalQueryFilter` | 58 | private | 待符号级复核 |
| DatabaseInitializationService | `DatabaseInitializationService` | 24 | public | 待符号级复核 |
| DbContextAccessor | `DbContextAccessor` | 13 | public | 待符号级复核 |
| BusinessExceptionHandler | `BusinessExceptionHandler` | 18 | public | 待符号级复核 |
| SystemExceptionHandler | `SystemExceptionHandler` | 18 | public | 待符号级复核 |
| SystemExceptionHandler | `private` | 86 | default(private) | 待符号级复核 |
| IHealthCheckService | `GetOverallStatusAsync` | 21 | public (interface 默认) | 待符号级复核 |
| LogCleanupService | `LogCleanupService` | 21 | public | 待符号级复核 |
| BaseRepository | `BaseRepository` | 22 | protected | 待符号级复核 |
| SystemLogRepository | `SystemLogRepository` | 15 | public | 待符号级复核 |
| SensitiveDataJsonConverterFactory | `CanConvert` | 25 | public | 待符号级复核 |
| SensitiveDataJsonConverterFactory | `CreateConverter` | 68 | public | 待符号级复核 |
| SensitiveDataJsonConverter | `SensitiveDataJsonConverter` | 84 | public | 待符号级复核 |
| BaseService | `BaseService` | 13 | protected | 待符号级复核 |
| BaseService | `BaseService` | 25 | protected | 待符号级复核 |
| IHerbCrossModuleService | `GetHerbByNameOrPinyinAsync` | 15 | public (interface 默认) | 待符号级复核 |
| IHerbCrossModuleService | `CheckHerbReferenceAsync` | 18 | public (interface 默认) | 待符号级复核 |
| IUserCrossModuleService | `UpdateUserPasswordHashAsync` | 18 | public (interface 默认) | 待符号级复核 |
| IUserCrossModuleService | `UserExistsAsync` | 21 | public (interface 默认) | 待符号级复核 |
| HealthCheckService | `GetOverallStatusAsync` | 91 | public | 待符号级复核 |
| ValidationBehavior | `ValidationBehavior` | 15 | public | 待符号级复核 |
| ValidationBehavior | `Handle` | 20 | public | 框架反射模式 |
| BaseApiController | `BaseApiController` | 19 | protected | 待符号级复核 |
| BaseApiController | `protected` | 29 | default(private) | 待符号级复核 |
| BaseCrudController | `BaseCrudController` | 17 | protected | 待符号级复核 |
| BaseCrudController | `GetList` | 29 | public | 待符号级复核 |
| BaseCrudController | `BatchDelete` | 68 | public | 待符号级复核 |
| ControllerBaseExtensions | `CreateModuleErrorResponse` | 141 | private | 待符号级复核 |
### LYBT.LocalWebAPI（56 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| AuthController | `AuthController` | 19 | public | 待符号级复核 |
| AuthController | `AutoLogin` | 58 | public | 待符号级复核 |
| ConfigurationController | `ConfigurationController` | 23 | public | 待符号级复核 |
| ConfigurationController | `GetAll` | 31 | public | 待符号级复核 |
| DeployController | `DeployController` | 14 | public | 待符号级复核 |
| DeployController | `Upload` | 17 | public | 待符号级复核 |
| DiagnosticsController | `DiagnosticsController` | 26 | public | 待符号级复核 |
| DiagnosticsController | `GetDbInfo` | 35 | public | 待符号级复核 |
| DiagnosticsController | `GetVersion` | 57 | public | 待符号级复核 |
| DiagnosticsController | `GetRecentLogs` | 77 | public | 待符号级复核 |
| DiagnosticsController | `GetLoggingStatus` | 99 | public | 待符号级复核 |
| DiagnosticsController | `SetLoggingLevel` | 160 | public | 待符号级复核 |
| FormulasController | `FormulasController` | 25 | public | 待符号级复核 |
| FormulasController | `GetList` | 37 | public | 待符号级复核 |
| FormulasController | `BatchDelete` | 163 | public | 待符号级复核 |
| FormulasController | `BatchImport` | 213 | public | 待符号级复核 |
| FormulasController | `GetPendingValidation` | 227 | public | 待符号级复核 |
| FormulasController | `ValidateHerb` | 239 | public | 待符号级复核 |
| FormulasController | `BatchEnable` | 273 | public | 待符号级复核 |
| FormulasController | `BatchDisable` | 290 | public | 待符号级复核 |
| HealthController | `HealthController` | 22 | public | 待符号级复核 |
| HealthController | `GetHealth` | 33 | public | 待符号级复核 |
| HealthController | `Ping` | 49 | public | 待符号级复核 |
| HealthController | `GetDetails` | 61 | public | 待符号级复核 |
| HerbsController | `HerbsController` | 25 | public | 待符号级复核 |
| HerbsController | `GetList` | 35 | public | 待符号级复核 |
| HerbsController | `BatchDelete` | 180 | public | 待符号级复核 |
| HerbsController | `BatchImport` | 192 | public | 待符号级复核 |
| HerbsController | `BatchCheckReference` | 219 | public | 待符号级复核 |
| HerbsController | `BatchEnable` | 233 | public | 待符号级复核 |
| HerbsController | `BatchDisable` | 250 | public | 待符号级复核 |
| MedicalCasesController | `GetList` | 37 | public | 待符号级复核 |
| MedicalCasesController | `GetByStatus` | 90 | public | 待符号级复核 |
| MedicalCasesController | `GetPending` | 103 | public | 待符号级复核 |
| MedicalCasesController | `BatchDelete` | 165 | public | 待符号级复核 |
| MedicalCasesController | `CloseCase` | 205 | public | 待符号级复核 |
| MedicalCasesController | `SuspendCase` | 222 | public | 待符号级复核 |
| MedicalCasesController | `CancelCase` | 239 | public | 待符号级复核 |
| PatientsController | `PatientsController` | 25 | public | 待符号级复核 |
| PatientsController | `GetList` | 37 | public | 待符号级复核 |
| PatientsController | `GetByIdNumber` | 118 | public | 待符号级复核 |
| PatientsController | `BatchCheckReference` | 201 | public | 待符号级复核 |
| PatientsController | `BatchDelete` | 214 | public | 待符号级复核 |
| PatientsController | `BatchImport` | 227 | public | 待符号级复核 |
| ReportsController | `ReportsController` | 18 | public | 待符号级复核 |
| ReportsController | `GetDailyIncome` | 24 | public | 待符号级复核 |
| ReportsController | `GetDailyConsultations` | 46 | public | 待符号级复核 |
| ReportsController | `GetDailyHerbs` | 67 | public | 待符号级复核 |
| LocalAutoLoginCommandHandler | `LocalAutoLoginCommandHandler` | 26 | public | 待符号级复核 |
| LocalAutoLoginCommandHandler | `Handle` | 36 | public | 框架反射模式 |
| LocalLoginCommandHandler | `LocalLoginCommandHandler` | 21 | public | 待符号级复核 |
| LocalLoginCommandHandler | `Handle` | 31 | public | 框架反射模式 |
| LocalRefreshTokenCommandHandler | `LocalRefreshTokenCommandHandler` | 21 | public | 待符号级复核 |
| LocalRefreshTokenCommandHandler | `Handle` | 29 | public | 框架反射模式 |
| LocalValidateTokenQueryHandler | `LocalValidateTokenQueryHandler` | 15 | public | 待符号级复核 |
| LocalValidateTokenQueryHandler | `Handle` | 20 | public | 框架反射模式 |
### LYBT.Module.Auth（14 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| AutoLoginCommandHandler | `Handle` | 22 | public | 框架反射模式 |
| LoginCommandHandler | `Handle` | 54 | public | 框架反射模式 |
| LogoutCommandHandler | `LogoutCommandHandler` | 17 | public | 待符号级复核 |
| LogoutCommandHandler | `Handle` | 27 | public | 框架反射模式 |
| RefreshTokenCommandHandler | `Handle` | 32 | public | 框架反射模式 |
| RevokeAllUserTokensCommandHandler | `RevokeAllUserTokensCommandHandler` | 18 | public | 待符号级复核 |
| RevokeAllUserTokensCommandHandler | `Handle` | 28 | public | 框架反射模式 |
| ValidateTokenQueryHandler | `Handle` | 28 | public | 框架反射模式 |
| AuthDbContext | `AuthDbContext` | 24 | public | 待符号级复核 |
| AuthSessionRepository | `AuthSessionRepository` | 14 | public | 待符号级复核 |
| SecurityAuditRepository | `SecurityAuditRepository` | 10 | public | 待符号级复核 |
| AuthCrossModuleService | `AuthCrossModuleService` | 15 | public | 待符号级复核 |
| JwtService | `JwtService` | 28 | public | 待符号级复核 |
| SecurityAuditService | `SecurityAuditService` | 13 | public | 待符号级复核 |
### LYBT.Module.Formula（29 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| BatchDeleteFormulasCommandHandler | `BatchDeleteFormulasCommandHandler` | 15 | public | 待符号级复核 |
| BatchDeleteFormulasCommandHandler | `Handle` | 21 | public | 框架反射模式 |
| BatchDisableFormulasCommandHandler | `BatchDisableFormulasCommandHandler` | 15 | public | 待符号级复核 |
| BatchDisableFormulasCommandHandler | `Handle` | 20 | public | 框架反射模式 |
| BatchEnableFormulasCommandHandler | `BatchEnableFormulasCommandHandler` | 15 | public | 待符号级复核 |
| BatchEnableFormulasCommandHandler | `Handle` | 20 | public | 框架反射模式 |
| BatchImportFormulasCommandHandler | `Handle` | 17 | public | 框架反射模式 |
| CreateFormulaCommandHandler | `CreateFormulaCommandHandler` | 20 | public | 待符号级复核 |
| CreateFormulaCommandHandler | `Handle` | 27 | public | 框架反射模式 |
| DeleteFormulaCommandHandler | `DeleteFormulaCommandHandler` | 18 | public | 待符号级复核 |
| DeleteFormulaCommandHandler | `Handle` | 25 | public | 框架反射模式 |
| RestoreFormulaCommandHandler | `RestoreFormulaCommandHandler` | 17 | public | 待符号级复核 |
| RestoreFormulaCommandHandler | `Handle` | 22 | public | 框架反射模式 |
| ToggleFormulaStatusCommandHandler | `ToggleFormulaStatusCommandHandler` | 18 | public | 待符号级复核 |
| ToggleFormulaStatusCommandHandler | `Handle` | 23 | public | 框架反射模式 |
| UpdateFormulaCommandHandler | `UpdateFormulaCommandHandler` | 17 | public | 待符号级复核 |
| UpdateFormulaCommandHandler | `Handle` | 22 | public | 框架反射模式 |
| ValidateFormulaHerbCommandHandler | `Handle` | 19 | public | 框架反射模式 |
| FormulaDtoMapper | `ToHerbItemDto` | 71 | public | 待符号级复核 |
| GetPendingValidationQueryHandler | `Handle` | 18 | public | 框架反射模式 |
| BatchDisableFormulasValidator | `BatchDisableFormulasValidator` | 11 | public | 待符号级复核 |
| BatchEnableFormulasValidator | `BatchEnableFormulasValidator` | 11 | public | 待符号级复核 |
| CreateFormulaValidator | `CreateFormulaValidator` | 14 | public | 待符号级复核 |
| FormulaBatchImportCommandValidator | `FormulaBatchImportCommandValidator` | 8 | public | 待符号级复核 |
| RestoreFormulaValidator | `RestoreFormulaValidator` | 11 | public | 待符号级复核 |
| ToggleFormulaStatusValidator | `ToggleFormulaStatusValidator` | 11 | public | 待符号级复核 |
| UpdateFormulaValidator | `UpdateFormulaValidator` | 11 | public | 待符号级复核 |
| FormulaDbContext | `FormulaDbContext` | 19 | public | 待符号级复核 |
| FormulaService | `FormulaService` | 16 | public | 待符号级复核 |
### LYBT.Module.Herbs（33 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| BatchDeleteHerbsCommandHandler | `BatchDeleteHerbsCommandHandler` | 20 | public | 待符号级复核 |
| BatchDeleteHerbsCommandHandler | `Handle` | 28 | public | 框架反射模式 |
| BatchDisableHerbsCommandHandler | `BatchDisableHerbsCommandHandler` | 15 | public | 待符号级复核 |
| BatchDisableHerbsCommandHandler | `Handle` | 20 | public | 框架反射模式 |
| BatchEnableHerbsCommandHandler | `BatchEnableHerbsCommandHandler` | 15 | public | 待符号级复核 |
| BatchEnableHerbsCommandHandler | `Handle` | 20 | public | 框架反射模式 |
| BatchImportHerbsCommandHandler | `BatchImportHerbsCommandHandler` | 19 | public | 待符号级复核 |
| BatchImportHerbsCommandHandler | `Handle` | 24 | public | 框架反射模式 |
| CreateHerbCommandHandler | `CreateHerbCommandHandler` | 17 | public | 待符号级复核 |
| CreateHerbCommandHandler | `Handle` | 23 | public | 框架反射模式 |
| DeleteHerbCommandHandler | `DeleteHerbCommandHandler` | 15 | public | 待符号级复核 |
| DeleteHerbCommandHandler | `Handle` | 21 | public | 框架反射模式 |
| RestoreHerbCommandHandler | `RestoreHerbCommandHandler` | 17 | public | 待符号级复核 |
| RestoreHerbCommandHandler | `Handle` | 22 | public | 框架反射模式 |
| ToggleHerbStatusCommandHandler | `ToggleHerbStatusCommandHandler` | 18 | public | 待符号级复核 |
| ToggleHerbStatusCommandHandler | `Handle` | 23 | public | 框架反射模式 |
| UpdateHerbCommandHandler | `UpdateHerbCommandHandler` | 17 | public | 待符号级复核 |
| UpdateHerbCommandHandler | `Handle` | 22 | public | 框架反射模式 |
| CheckHerbReferenceQueryHandler | `CheckHerbReferenceQueryHandler` | 16 | public | 待符号级复核 |
| CheckHerbReferenceQueryHandler | `Handle` | 24 | public | 框架反射模式 |
| CheckHerbReferenceQueryHandler | `Handle` | 57 | public | 框架反射模式 |
| BatchDisableHerbsValidator | `BatchDisableHerbsValidator` | 11 | public | 待符号级复核 |
| BatchEnableHerbsValidator | `BatchEnableHerbsValidator` | 11 | public | 待符号级复核 |
| CreateHerbValidator | `CreateHerbValidator` | 11 | public | 待符号级复核 |
| HerbBatchImportCommandValidator | `HerbBatchImportCommandValidator` | 8 | public | 待符号级复核 |
| RestoreHerbValidator | `RestoreHerbValidator` | 11 | public | 待符号级复核 |
| ToggleHerbStatusValidator | `ToggleHerbStatusValidator` | 11 | public | 待符号级复核 |
| UpdateHerbValidator | `UpdateHerbValidator` | 11 | public | 待符号级复核 |
| HerbReferenceRepository | `HerbReferenceRepository` | 17 | public | 待符号级复核 |
| HerbsDbContext | `HerbsDbContext` | 37 | public | 待符号级复核 |
| HerbCrossModuleService | `HerbCrossModuleService` | 19 | public | 待符号级复核 |
| HerbCrossModuleService | `GetHerbByNameOrPinyinAsync` | 40 | public | 待符号级复核 |
| HerbCrossModuleService | `CheckHerbReferenceAsync` | 59 | public | 待符号级复核 |
### LYBT.Module.MedicalCase（23 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| BaseMedicalCasesController | `BaseMedicalCasesController` | 24 | protected | 待符号级复核 |
| BaseMedicalCasesController | `GetPatientConsultations` | 89 | public | 待符号级复核 |
| BaseMedicalCasesController | `GetPatientPrescriptions` | 104 | public | 待符号级复核 |
| BaseMedicalCasesController | `GetConsultations` | 119 | public | 待符号级复核 |
| BaseMedicalCasesController | `GetPrescriptions` | 131 | public | 待符号级复核 |
| BaseMedicalCasesController | `GetBatchDetails` | 143 | public | 待符号级复核 |
| BaseMedicalCasesController | `GetPermissions` | 161 | public | 待符号级复核 |
| BaseMedicalCasesController | `GetAuditLogs` | 175 | public | 待符号级复核 |
| BaseMedicalCasesController | `SetPrescriptionFlag` | 192 | public | 待符号级复核 |
| BaseMedicalCasesController | `RecordPrint` | 213 | public | 待符号级复核 |
| BaseMedicalCasesController | `AddPrintLog` | 234 | public | 待符号级复核 |
| MedicalCaseDbContext | `MedicalCaseDbContext` | 34 | public | 待符号级复核 |
| MedicalCaseMapper | `ToPrescriptionEntity` | 105 | public | 待符号级复核 |
| MedicalCaseMapper | `UpdatePrescriptionEntity` | 120 | public | 待符号级复核 |
| MedicalCaseMapper | `ToPrescriptionItemDto` | 132 | public | 待符号级复核 |
| MedicalCaseReferenceRepository | `MedicalCaseReferenceRepository` | 14 | public | 待符号级复核 |
| MedicalCaseCommandService | `MedicalCaseCommandService` | 34 | public | 待符号级复核 |
| MedicalCaseCrossModuleService | `MedicalCaseCrossModuleService` | 18 | public | 待符号级复核 |
| MedicalCasePrescriptionService | `MedicalCasePrescriptionService` | 19 | public | 待符号级复核 |
| MedicalCaseQueryService | `MedicalCaseQueryService` | 25 | public | 待符号级复核 |
| MedicalCaseServiceHelper | `ExecuteWithConcurrencyRetryAsync` | 113 | public | 待符号级复核 |
| MedicalCaseStateService | `MedicalCaseStateService` | 32 | public | 待符号级复核 |
| PrescriptionItemService | `PrescriptionItemService` | 20 | public | 待符号级复核 |
### LYBT.Module.Patients（22 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| BatchDeletePatientsCommandHandler | `BatchDeletePatientsCommandHandler` | 20 | public | 待符号级复核 |
| BatchDeletePatientsCommandHandler | `Handle` | 28 | public | 框架反射模式 |
| BatchImportPatientsCommandHandler | `BatchImportPatientsCommandHandler` | 18 | public | 待符号级复核 |
| BatchImportPatientsCommandHandler | `Handle` | 23 | public | 框架反射模式 |
| CreatePatientCommandHandler | `CreatePatientCommandHandler` | 17 | public | 待符号级复核 |
| CreatePatientCommandHandler | `Handle` | 23 | public | 框架反射模式 |
| DeletePatientCommandHandler | `DeletePatientCommandHandler` | 17 | public | 待符号级复核 |
| DeletePatientCommandHandler | `Handle` | 25 | public | 框架反射模式 |
| RestorePatientCommandHandler | `RestorePatientCommandHandler` | 17 | public | 待符号级复核 |
| RestorePatientCommandHandler | `Handle` | 22 | public | 框架反射模式 |
| TogglePatientStatusCommandHandler | `TogglePatientStatusCommandHandler` | 20 | public | 待符号级复核 |
| TogglePatientStatusCommandHandler | `Handle` | 28 | public | 框架反射模式 |
| UpdatePatientCommandHandler | `UpdatePatientCommandHandler` | 17 | public | 待符号级复核 |
| UpdatePatientCommandHandler | `Handle` | 22 | public | 框架反射模式 |
| BatchCheckPatientReferenceQueryHandler | `Handle` | 19 | public | 框架反射模式 |
| CheckPatientReferenceQueryHandler | `Handle` | 20 | public | 框架反射模式 |
| CreatePatientValidator | `CreatePatientValidator` | 11 | public | 待符号级复核 |
| RestorePatientValidator | `RestorePatientValidator` | 11 | public | 待符号级复核 |
| UpdatePatientValidator | `UpdatePatientValidator` | 11 | public | 待符号级复核 |
| PatientsDbContext | `PatientsDbContext` | 16 | public | 待符号级复核 |
| PatientCrossModuleService | `PatientCrossModuleService` | 18 | public | 待符号级复核 |
| PatientService | `PatientService` | 17 | public | 待符号级复核 |
### LYBT.Module.Registration（24 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| CancelRegistrationCommandHandler | `CancelRegistrationCommandHandler` | 17 | public | 待符号级复核 |
| CancelRegistrationCommandHandler | `Handle` | 25 | public | 框架反射模式 |
| CreateRegistrationCommandHandler | `CreateRegistrationCommandHandler` | 21 | public | 待符号级复核 |
| CreateRegistrationCommandHandler | `Handle` | 31 | public | 框架反射模式 |
| QuickVisitCommandHandler | `Handle` | 19 | public | 框架反射模式 |
| StartVisitCommandHandler | `StartVisitCommandHandler` | 22 | public | 待符号级复核 |
| StartVisitCommandHandler | `Handle` | 32 | public | 框架反射模式 |
| GetRegistrationQueryHandler | `GetRegistrationQueryHandler` | 17 | public | 待符号级复核 |
| GetRegistrationQueryHandler | `Handle` | 23 | public | 框架反射模式 |
| GetRegistrationsQueryHandler | `GetRegistrationsQueryHandler` | 18 | public | 待符号级复核 |
| GetRegistrationsQueryHandler | `Handle` | 24 | public | 框架反射模式 |
| GetWaitingQueueQueryHandler | `GetWaitingQueueQueryHandler` | 17 | public | 待符号级复核 |
| GetWaitingQueueQueryHandler | `Handle` | 23 | public | 框架反射模式 |
| CreateRegistrationValidator | `CreateRegistrationValidator` | 11 | public | 待符号级复核 |
| QuickVisitCommandValidator | `QuickVisitCommandValidator` | 8 | public | 待符号级复核 |
| BaseRegistrationsController | `BaseRegistrationsController` | 18 | protected | 待符号级复核 |
| BaseRegistrationsController | `GetList` | 29 | public | 待符号级复核 |
| BaseRegistrationsController | `BatchDelete` | 77 | public | 待符号级复核 |
| BaseRegistrationsController | `GetQueue` | 88 | public | 待符号级复核 |
| RegistrationConnectionManager | `CountConnections` | 28 | public | 待符号级复核 |
| RegistrationHub | `RegistrationHub` | 21 | public | 待符号级复核 |
| RegistrationDbContext | `RegistrationDbContext` | 16 | public | 待符号级复核 |
| NotificationService | `NotificationService` | 18 | public | 待符号级复核 |
| RegistrationCrossModuleService | `RegistrationCrossModuleService` | 15 | public | 待符号级复核 |
### LYBT.Module.Reports（2 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| ReportRepository | `ReportRepository` | 16 | public | 待符号级复核 |
| ReportService | `ReportService` | 15 | public | 待符号级复核 |
### LYBT.Module.Users（37 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| BatchDeleteUsersCommandHandler | `BatchDeleteUsersCommandHandler` | 16 | public | 待符号级复核 |
| BatchDeleteUsersCommandHandler | `Handle` | 21 | public | 框架反射模式 |
| BatchDisableUsersCommandHandler | `BatchDisableUsersCommandHandler` | 16 | public | 待符号级复核 |
| BatchDisableUsersCommandHandler | `Handle` | 21 | public | 框架反射模式 |
| BatchEnableUsersCommandHandler | `BatchEnableUsersCommandHandler` | 16 | public | 待符号级复核 |
| BatchEnableUsersCommandHandler | `Handle` | 21 | public | 框架反射模式 |
| ChangePasswordCommandHandler | `ChangePasswordCommandHandler` | 16 | public | 待符号级复核 |
| ChangePasswordCommandHandler | `Handle` | 24 | public | 框架反射模式 |
| ChangeProfileCommandHandler | `ChangeProfileCommandHandler` | 17 | public | 待符号级复核 |
| ChangeProfileCommandHandler | `Handle` | 22 | public | 框架反射模式 |
| CreateUserCommandHandler | `CreateUserCommandHandler` | 18 | public | 待符号级复核 |
| CreateUserCommandHandler | `Handle` | 24 | public | 框架反射模式 |
| DeleteUserCommandHandler | `DeleteUserCommandHandler` | 18 | public | 待符号级复核 |
| DeleteUserCommandHandler | `Handle` | 26 | public | 框架反射模式 |
| ResetPasswordCommandHandler | `ResetPasswordCommandHandler` | 17 | public | 待符号级复核 |
| ResetPasswordCommandHandler | `Handle` | 25 | public | 框架反射模式 |
| RestoreUserCommandHandler | `RestoreUserCommandHandler` | 15 | public | 待符号级复核 |
| RestoreUserCommandHandler | `Handle` | 20 | public | 框架反射模式 |
| ToggleUserStatusCommandHandler | `ToggleUserStatusCommandHandler` | 18 | public | 待符号级复核 |
| ToggleUserStatusCommandHandler | `Handle` | 26 | public | 框架反射模式 |
| UpdateUserCommandHandler | `UpdateUserCommandHandler` | 17 | public | 待符号级复核 |
| UpdateUserCommandHandler | `Handle` | 22 | public | 框架反射模式 |
| UserCrossModuleMapper | `ToNonNullString` | 30 | private | 待符号级复核 |
| UserCrossModuleMapper | `ToLockoutEnd` | 35 | private | 待符号级复核 |
| ChangeProfileValidator | `ChangeProfileValidator` | 11 | public | 待符号级复核 |
| CreateUserValidator | `CreateUserValidator` | 11 | public | 待符号级复核 |
| UpdateUserValidator | `UpdateUserValidator` | 11 | public | 待符号级复核 |
| BaseUsersController | `BaseUsersController` | 27 | protected | 待符号级复核 |
| BaseUsersController | `GetList` | 37 | public | 待符号级复核 |
| BaseUsersController | `BatchDelete` | 152 | public | 待符号级复核 |
| BaseUsersController | `ChangeProfile` | 219 | public | 待符号级复核 |
| BaseUsersController | `BatchEnable` | 268 | public | 待符号级复核 |
| BaseUsersController | `BatchDisable` | 289 | public | 待符号级复核 |
| UsersDbContext | `UsersDbContext` | 13 | public | 待符号级复核 |
| UserCrossModuleService | `UserCrossModuleService` | 22 | public | 待符号级复核 |
| UserCrossModuleService | `UpdateUserPasswordHashAsync` | 53 | public | 待符号级复核 |
| UserCrossModuleService | `UserExistsAsync` | 64 | public | 待符号级复核 |
### LYBT.Shared.Configuration（1 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| LoginRateLimitOptions | `LoginRateLimitOptions` | 64 | public | 待符号级复核 |
### LYBT.Shared.ExceptionHandling（26 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| AppException | `AppException` | 44 | public | 待符号级复核 |
| AppException | `AppException` | 48 | public | 待符号级复核 |
| AppException | `AppException` | 52 | public | 待符号级复核 |
| AppException | `AppException` | 56 | public | 待符号级复核 |
| AppException | `AppException` | 64 | public | 待符号级复核 |
| AppException | `AppException` | 75 | public | 待符号级复核 |
| AppException | `AppException` | 87 | public | 待符号级复核 |
| ConflictException | `MedicalCaseVersion` | 80 | public | 待符号级复核 |
| ConflictException | `MedicalCaseLocked` | 94 | public | 待符号级复核 |
| ConflictException | `Duplicate` | 106 | public | 待符号级复核 |
| ValidationException | `AddError` | 76 | public | 待符号级复核 |
| ApiException | `ApiException` | 37 | public | 待符号级复核 |
| ApiException | `ApiException` | 42 | public | 待符号级复核 |
| ApiException | `ApiException` | 47 | public | 待符号级复核 |
| ApiException | `ApiException` | 52 | public | 待符号级复核 |
| ApiException | `ApiException` | 60 | public | 待符号级复核 |
| ApiException | `Forbidden` | 87 | public | 待符号级复核 |
| ApiException | `ServiceUnavailable` | 90 | public | 待符号级复核 |
| UnauthorizedException | `UnauthorizedException` | 26 | public | 待符号级复核 |
| UnauthorizedException | `UnauthorizedException` | 32 | public | 待符号级复核 |
| UnauthorizedException | `UnauthorizedException` | 39 | public | 待符号级复核 |
| UnauthorizedException | `UnauthorizedException` | 46 | public | 待符号级复核 |
| UnauthorizedException | `InvalidPassword` | 53 | public | 待符号级复核 |
| UnauthorizedException | `InvalidRefreshToken` | 56 | public | 待符号级复核 |
| UnauthorizedException | `UserLocked` | 62 | public | 待符号级复核 |
| UnauthorizedException | `PasswordChangeRequired` | 65 | public | 待符号级复核 |
### LYBT.Shared.Logging（11 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| ActivityCorrelationIdProvider | `SetCorrelationId` | 24 | public | 待符号级复核 |
| ActivityCorrelationIdProvider | `GetCorrelationIdOrNew` | 33 | public | 待符号级复核 |
| ICorrelationIdProvider | `SetCorrelationId` | 19 | public (interface 默认) | 待符号级复核 |
| CorrelationIdEnricher | `Enrich` | 45 | public | 待符号级复核 |
| LoggerConfigurationExtensions | `WriteToConsoleWithTemplate` | 69 | public | 待符号级复核 |
| LoggerConfigurationExtensions | `WriteToFileWithTemplate` | 92 | public | 待符号级复核 |
| LoggingLevelManager | `LoggingLevelManager` | 42 | public | 待符号级复核 |
| SensitiveDataDestructuringPolicy | `TryDestructure` | 18 | public | 待符号级复核 |
| SensitiveDataMasker | `MaskObject` | 142 | public | 待符号级复核 |
| SensitiveDataMasker | `SanitizeException` | 275 | public | 待符号级复核 |
| SanitizingJsonConverter | `CanConvert` | 306 | public | 待符号级复核 |
### LYBT.Shared.Models（13 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| PagedResult | `PagedResult` | 17 | public | 待符号级复核 |
| PagedResult | `PagedResult` | 25 | public | 待符号级复核 |
| Result | `ValidationFailure` | 75 | public | 待符号级复核 |
| ErrorCodeExtensions | `GetModuleName` | 286 | public | 待符号级复核 |
| PasswordHelper | `GenerateTemporaryPassword` | 45 | public | 待符号级复核 |
| PasswordHelper | `GenerateSalt` | 76 | public | 待符号级复核 |
| PasswordHelper | `ValidatePassword` | 98 | public | 待符号级复核 |
| PasswordHelper | `SecureEquals` | 318 | public | 待符号级复核 |
| LoginRequestValidator | `LoginRequestValidator` | 11 | public | 待符号级复核 |
| FormulaInputDtoValidator | `FormulaInputDtoValidator` | 12 | public | 待符号级复核 |
| HerbInputDtoValidator | `HerbInputDtoValidator` | 13 | public | 待符号级复核 |
| MedicalCaseInputDtoValidator | `MedicalCaseInputDtoValidator` | 26 | public | 待符号级复核 |
| PatientInputDtoValidator | `PatientInputDtoValidator` | 13 | public | 待符号级复核 |
### LYBT.Tools.PasswordHashGenerator（1 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| Program | `Main` | 11 | default(private) | 待符号级复核 |
### LYBT.WebAPI（58 项）

| 类 | 方法 | 行号 | 可见性 | 备注 |
|----|------|------|--------|------|
| AuthController | `AuthController` | 26 | public | 待符号级复核 |
| AuthController | `AutoLoginAsync` | 111 | public | 待符号级复核 |
| AuthController | `ValidateTokenFromHeaderAsync` | 125 | public | 待符号级复核 |
| ConfigurationController | `ConfigurationController` | 22 | public | 待符号级复核 |
| ConfigurationController | `GetConfiguration` | 35 | public | 待符号级复核 |
| ConfigurationController | `UpdateConfiguration` | 77 | public | 待符号级复核 |
| ConfigurationController | `ValidateProduction` | 93 | public | 待符号级复核 |
| DeployController | `DeployController` | 23 | public | 待符号级复核 |
| DeployController | `Upload` | 31 | public | 待符号级复核 |
| DiagnosticsController | `DiagnosticsController` | 25 | public | 待符号级复核 |
| DiagnosticsController | `GetLoggingStatus` | 38 | public | 待符号级复核 |
| DiagnosticsController | `SetLoggingLevel` | 110 | public | 待符号级复核 |
| FormulasController | `FormulasController` | 28 | public | 待符号级复核 |
| FormulasController | `GetList` | 39 | public | 待符号级复核 |
| FormulasController | `BatchDelete` | 203 | public | 待符号级复核 |
| FormulasController | `GetPendingValidation` | 243 | public | 待符号级复核 |
| FormulasController | `ValidateHerb` | 261 | public | 待符号级复核 |
| FormulasController | `BatchEnable` | 291 | public | 待符号级复核 |
| FormulasController | `BatchDisable` | 311 | public | 待符号级复核 |
| HealthController | `HealthController` | 27 | public | 待符号级复核 |
| HealthController | `Ping` | 51 | public | 待符号级复核 |
| HealthController | `GetDetailedHealth` | 76 | public | 待符号级复核 |
| HerbsController | `HerbsController` | 28 | public | 待符号级复核 |
| HerbsController | `GetList` | 39 | public | 待符号级复核 |
| HerbsController | `BatchDelete` | 199 | public | 待符号级复核 |
| HerbsController | `BatchImport` | 214 | public | 待符号级复核 |
| HerbsController | `BatchCheckReference` | 253 | public | 待符号级复核 |
| HerbsController | `BatchEnable` | 270 | public | 待符号级复核 |
| HerbsController | `BatchDisable` | 290 | public | 待符号级复核 |
| MedicalCasesController | `GetList` | 45 | public | 待符号级复核 |
| MedicalCasesController | `BatchDelete` | 175 | public | 待符号级复核 |
| MedicalCasesController | `SetPrescriptionFlag` | 200 | public | 待符号级复核 |
| MedicalCasesController | `RecordPrint` | 224 | public | 待符号级复核 |
| MedicalCasesController | `CloseMedicalCase` | 281 | public | 待符号级复核 |
| MedicalCasesController | `CancelMedicalCase` | 326 | public | 待符号级复核 |
| PatientsController | `PatientsController` | 28 | public | 待符号级复核 |
| PatientsController | `GetList` | 39 | public | 待符号级复核 |
| PatientsController | `BatchDelete` | 201 | public | 待符号级复核 |
| PatientsController | `BatchImport` | 216 | public | 待符号级复核 |
| PatientsController | `GetByIdNumber` | 257 | public | 待符号级复核 |
| PatientsController | `BatchCheckReference` | 270 | public | 待符号级复核 |
| ReportsController | `ReportsController` | 24 | public | 待符号级复核 |
| ReportsController | `GetDailyIncome` | 37 | public | 待符号级复核 |
| ReportsController | `GetDailyConsultations` | 55 | public | 待符号级复核 |
| ReportsController | `GetDailyHerbs` | 73 | public | 待符号级复核 |
| ReportsController | `GetIncomeTrend` | 91 | public | 待符号级复核 |
| ReportsController | `GetConsultationTrend` | 110 | public | 待符号级复核 |
| ReportsController | `GetDoctorPerformance` | 129 | public | 待符号级复核 |
| ReportsController | `GetHerbRanking` | 147 | public | 待符号级复核 |
| ReportsController | `GetPatientFlow` | 166 | public | 待符号级复核 |
| ApiLoggingFilter | `ApiLoggingFilter` | 15 | public | 待符号级复核 |
| ApiLoggingFilter | `OnActionExecutionAsync` | 20 | public | 待符号级复核 |
| DatabaseStartupDiagnostics | `DatabaseStartupDiagnostics` | 16 | public | 待符号级复核 |
| SqlServerHealthCheck | `SqlServerHealthCheck` | 16 | public | 待符号级复核 |
| ClaimsNormalizationMiddleware | `ClaimsNormalizationMiddleware` | 14 | public | 待符号级复核 |
| CorrelationIdMiddleware | `CorrelationIdMiddleware` | 32 | public | 待符号级复核 |
| SecurityHeadersMiddleware | `SecurityHeadersMiddleware` | 12 | public | 待符号级复核 |
| Program | `Main` | 37 | public | 待符号级复核 |

## 4. 移交说明

- S1-S4 复核方法：serena `find_referencing_symbols`（符号级）+ grep 扩展（`nameof`/反射字符串）+ 源码走查
- 框架反射模式名单为启发式初筛，S1-S4 仍需逐项确认（如某 `Handle` 可能确为死）
- A-22 已知 MedicalCase 6 个死方法在此清单内的，直接引用 A-22 结论


