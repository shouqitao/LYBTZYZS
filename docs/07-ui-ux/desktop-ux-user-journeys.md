# Desktop 用户旅程（UX User Journeys）

> 版本: v1.0 | 日期: 2026-09-13 | 依据: 代码实际（src/Client/Desktop）+ 事实清单口径

> **计数口径（全文统一）**：View **30**（页面/导航级 XAML：`*/Views/*.xaml`，含 `Roles/*/Views/`、`Reports/Views/`、Shell 的 MainWindow/AppShell/Header/SideNav/Footer/AccountSettingsView）、Control **33**（`*/Controls/*.xaml` 内嵌组件）、Dialog **7**（`*/Dialogs/**/*.xaml`，经 `RegisterDialog` 注册）、Root 1（`Shell/App.xaml`）；ViewModel 文件 **55**；XAML 合计 **82** = 视图 71 + 资源/模板 11。

## 目录

1. [角色与首页口径](#1-角色与首页口径)
2. [旅程总览](#2-旅程总览)
3. [通用旅程骨架：登录 → Shell](#3-通用旅程骨架登录--shell)
4. [Sysadmin（UserRole.SuperAdmin · 系统运维）旅程](#4-sysadminuserrolesuperadmin--系统运维旅程)
5. [Admin（UserRole.Admin · 管理员）旅程](#5-adminuserroleadmin--管理员旅程)
6. [Doctor（UserRole.Doctor · 医生）旅程 — 核心诊疗链](#6-doctoruserroledoctor--医生旅程--核心诊疗链)
7. [Receptionist（UserRole.Receptionist · 前台接待）旅程 — 前台链](#7-receptionistuserrolereceptionist--前台接待旅程--前台链)
8. [跨角色交接点](#8-跨角色交接点)
9. [已知文档-代码落差](#9-已知文档-代码落差)
10. [事实来源](#10-事实来源)

---

## 1. 角色与首页口径

代码中的角色枚举为 `UserRole`（`src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs`），**没有** `Sysadmin` 枚举值；需求文档中的「Sysadmin」对应 `UserRole.SuperAdmin`。

| 需求用语 | 枚举值 | DisplayName | 首页 View（`RoleRegistry`） | 角色定义类 |
|---------|--------|-------------|--------------------------|-----------|
| Sysadmin | `UserRole.SuperAdmin = 100` | 系统运维 | `SysadminHomeView` | `SuperAdminRoleDefinition` |
| Admin | `UserRole.Admin = 10` | 管理员 | `AdminHomeView` | `AdminRoleDefinition` |
| Doctor | `UserRole.Doctor = 1` | 医生 | `ClinicalWorkspaceView` | `DoctorRoleDefinition` |
| Receptionist | `UserRole.Receptionist = 0` | 前台接待 | `ReceptionistHomeView` | `ReceptionistRoleDefinition` |

- 首页名由 `RoleRegistry.GetHomeViewName(role)` 解析；**未注册角色 fallback 到 `ClinicalHomeView`**（`RoleRegistry.DefaultHomeView`）——`ClinicalHomeView` 仍然存在，但**不是 Doctor 首页**。
- 登录成功后由 `LoginCoordinator.NavigateToRoleHomeAsync(user)` → `INavigationCoordinator.NavigateToHome(role)` 进入角色首页。
- 侧栏导航由 `NavigationManager.BuildNavigationItems(role)` 生成，**每个角色固定 3 项**：1 个「主页」+ 2 个业务项；分组取值为 **`临床` / `目录` / `管理`**（`SideNavControl` 经 `PropertyGroupDescription(nameof(NavigationItem.Group))` 分组渲染）。

| 角色 | 导航项 1 | 导航项 2 | 导航项 3 | 分组 |
|------|---------|---------|---------|------|
| Sysadmin | 主页 → `SysadminHomeView` | 备份管理 → `BackupManagementView` | 部署管理 → `DeploymentView` | 临床 / 管理 / 管理 |
| Admin | 主页 → `AdminHomeView` | 用户管理 → `UserManagementView` | 药材/验方 → `HerbManagementView` | 临床 / 管理 / 目录 |
| Doctor | 主页 → `ClinicalWorkspaceView` | 患者选择 → `PatientSelectionView` | 医案工作台 → `MedicalCaseWorkspaceView` | 临床 × 3 |
| Receptionist | 主页 → `ReceptionistHomeView` | 新建挂号 → `RegistrationListView` | 患者管理 → `PatientManagementView` | 临床 × 3 |

---

## 2. 旅程总览

```
                       ┌───────────────────────────────┐
                       │ MainWindow（登录态门控）        │
                       │  IsNotLoggedIn → LoginRegion   │
                       │  IsLoggedIn    → AppShell      │
                       └───────────────┬───────────────┘
                                       │ LoginCommand
                    ┌──────────────────▼──────────────────┐
                    │ LoginCoordinator.NavigateToRoleHome │
                    └──────────────────┬──────────────────┘
        ┌──────────────┬───────────────┼───────────────┬──────────────┐
        ▼              ▼               ▼               ▼
  SysadminHomeView AdminHomeView ClinicalWorkspace  ReceptionistHome
        │              │              View              │
        │              │               │               │
   运维/备份/部署   用户/目录/报表   接诊→工作台→完成  建档→挂号→取消
        └──────────────┴───────┬───────┴───────────────┘
                               ▼
                  HeaderControl（个人资料）/ SideNav（主题·退出）
                  FooterControl（API 状态徽标 / 连接模式 / 时间）
```

层次固定为三栏框架：`HeaderControl`（48px）→ 主体（`SideNavControl` + 内容区）→ `FooterControl`（32px）；三控件 VM 经 `ShellViewMappings` 显式映射解析（`AutoWireViewModel` 约定名不匹配）。

---

## 3. 通用旅程骨架：登录 → Shell

| # | 步骤 | 承载界面 | 进入方式 | 关键命令 | 涉及 VM |
|---|------|---------|---------|---------|--------|
| 1 | 启动 | `MainWindow`（`Shell/Views/MainWindow.xaml`） | 应用启动后即显示，`LoginRegion` 常驻 | — | `MainWindowViewModel` |
| 2 | 输入凭据 | `LoginView`（View） | `LoginRegion` 区域内容 | 无（`Username`/`Password` 绑定） | `LoginViewModel` |
| 3 | 登录 | `LoginView` | 按钮 / `LoginCommand` | `LoginCommand`（`AsyncRelayCommand`，`CanExecute` 要求用户名、密码非空且 `!IsLoading`） | `LoginViewModel`、`LoginCredentialsViewModel`、`ConnectionStatusViewModel` |
| 4 | 后台初始化 | `LoginView` | 进入页面后自动执行 4 步：首运行向导 → 保存凭据 → API 状态 → 连接模式 | —（`BackgroundInitAsync`） | `LoginViewModel` |
| 5 | 角色首页 | `SysadminHomeView` / `AdminHomeView` / `ClinicalWorkspaceView` / `ReceptionistHomeView` | 导航（`NavigateToHome(role)`） | — | 对应首页 VM |
| 6 | 全局操作 | `SideNavControl` / `HeaderControl` | 侧栏底部 / 顶栏用户区 | `LogoutCommand`（`IShellLogoutService.RequestLogoutAsync`，含活跃医案守卫）、`EditProfileCommand`（→ `AccountSettingsView`） | `SideNavViewModel`、`HeaderViewModel` |
| 7 | 收尾 | `MainWindow` | 登出后切回 `LoginRegion` | `LogoutRequested` → `ShowLoginDialog()` → `RequestNavigate(LoginRegion, ViewNames.Login)` | `LoginStateManager`、`ShellEventCoordinator` |

> 首次运行：`FirstRunSetupView` 与 `ServerConfigView` 物理位于 `Auth/Views/`，但经 `RegisterDialog` 注册为**对话框**（口径按路径仍计入 View）。

---

## 4. Sysadmin（UserRole.SuperAdmin · 系统运维）旅程

| # | 步骤 | 承载界面（x:Class） | 进入方式 | 关键命令 | 涉及 VM |
|---|------|------------------|---------|---------|--------|
| 1 | 登录 → 首页 | `SysadminHomeView` | `RoleRegistry` 首页 | — | `SysadminHomeViewModel` |
| 2 | 运维配置（内嵌面板） | `SysadminHomeView` 内部区域 | 首页卡片/面板，**非独立 View** | `ConfigCenter.SaveClinicCommand` / `SaveSessionCommand` / `TestConnectionCommand` / `SaveConnectionCommand` / `SaveSecurityCommand` / `SaveFeatureTogglesCommand` / `RestartLocalServiceCommand` | `ConfigurationCenterViewModel` |
| 3 | 读卡器诊断（内嵌面板） | `SysadminHomeView` 内部区域 | 首页面板 | `CardReaderDiagnostics.RunDiagnosticsCommand` / `SaveSettingsCommand` | `CardReaderDiagnosticsViewModel` |
| 4 | 服务端配置（内嵌面板） | `SysadminHomeView` 内部区域 | 首页面板 | `ServerConfig.SaveSectionCommand` / `RestartServerCommand` | `ServerConfigSectionViewModel` |
| 5 | 用户管理 | `UserManagementView` | 首页按钮 | `NavigateToUserManagementCommand` | `SysadminHomeViewModel` → `UserMasterDetailViewModel` |
| 6 | 日志级别 | `LogLevelControlView` | 首页按钮 | `NavigateToLogLevelControlCommand` | `LogLevelControlViewModel` |
| 7 | 部署管理 | `DeploymentView` | 侧栏「部署管理」/ 首页按钮 | `NavigateToDeploymentCommand`；`UploadCommand` 等 | `DeploymentViewModel` |
| 8 | 安全审计 | `SecurityAuditLogView` | 首页按钮 | `NavigateToSecurityAuditLogCommand` | `SecurityAuditLogViewModel` |
| 9 | 备份管理 | `BackupManagementView` | 侧栏「备份管理」 | 备份/恢复命令 | `BackupManagementViewModel` |
| 10 | 收尾 | `SideNavControl` | 侧栏底部 | `LogoutCommand` | `SideNavViewModel` |

> 步骤 2–4 的 `CardReaderDiagnosticsViewModel` / `ConfigurationCenterViewModel` / `ServerConfigSectionViewModel` **只有 VM 没有独立 View**，是 `SysadminHomeView` 的内嵌子 VM；历史上文档中的 `CardReaderDiagnosticsView`(SY-05)、`ConfigExportImportView`(SY-07)、`ServerConfigPanelView`(SY-08) 在代码中**不存在**。

---

## 5. Admin（UserRole.Admin · 管理员）旅程

| # | 步骤 | 承载界面（x:Class） | 进入方式 | 关键命令 | 涉及 VM |
|---|------|------------------|---------|---------|--------|
| 1 | 登录 → 首页 | `AdminHomeView` | `RoleRegistry` 首页 | — | `AdminHomeViewModel` |
| 2 | 用户管理 | `UserManagementView` | 首页卡片按钮 / 侧栏「用户管理」 | `NavigateToUserManagementCommand` | `AdminHomeViewModel` → `UserMasterDetailViewModel` |
| 3 | 药材 / 验方 | `HerbManagementView`、`FormulaManagementView` | 侧栏「药材/验方」（→`HerbManagementView`）/ 首页卡片 | `NavigateToHerbManagementCommand`、`NavigateToFormulaManagementCommand` | `HerbMasterDetailViewModel`、`FormulaMasterDetailViewModel` |
| 4 | 患者管理 | `PatientManagementView` | 首页卡片 | `NavigateToPatientManagementCommand` | `PatientMasterDetailViewModel` |
| 5 | 医案管理 | `MedicalCaseManagementView` | 首页卡片 | `NavigateToMedicalCaseManagementCommand` | `MedicalCaseMasterDetailViewModel` |
| 6 | 报表 | `ReportsHomeView` | 首页卡片 | `NavigateToReportsCommand` | `ReportsHomeViewModel` |
| 7 | 系统设置 | `SystemSettingsView` | 首页卡片 | `NavigateToSystemSettingsCommand` | `SystemSettingsViewModel` |
| 8 | 审计日志 | `AuditLogView` | 首页卡片 | `NavigateToAuditLogCommand` | `AuditLogViewModel` |
| 9 | 收尾 | `SideNavControl` | 侧栏底部 | `LogoutCommand` | `SideNavViewModel` |

> Admin 在挂号队列中拥有「前台」权限（`RegistrationListViewModel.IsReceptionist = role ∈ {Receptionist, Admin, SuperAdmin}`），可用于新建/取消挂号；其进入 `RegistrationListView` 的路径 `[待确认]`（侧栏未列出该页面）。

---

## 6. Doctor（UserRole.Doctor · 医生）旅程 — 核心诊疗链

### 6.1 完整链路

```
挂号(Waiting) ──StartVisitCommand──► 接诊(InProgress) ──导航──► 医案工作台
   ▲  前台创建                                                      │
   │                                                   保存 / 挂起 / 完成
   └────────────── 取消挂号（前台，Waiting+Receptionist 来源）◄──────┘
                                                                     │
                                        打印处方单（仅 Completed）◄───┘
```

### 6.2 分步

| # | 步骤 | 承载界面（x:Class） | 进入方式 | 关键命令 | 涉及 VM |
|---|------|------------------|---------|---------|--------|
| 1 | 登录 → 首页 | `ClinicalWorkspaceView` | `RoleRegistry` 首页（**非 `ClinicalHomeView`**） | — | `ClinicalWorkspaceViewModel` |
| 2 | 选择患者 | `ClinicalWorkspaceView` 左栏 `PatientSelectionControl` | 单击选中 / **双击患者** | 双击 → `PatientSelectionControl_PatientDoubleClicked` → `StartConsultationCommand`（`CanStartConsultation` = 已选患者） | `ClinicalWorkspaceViewModel` |
| 3 | 待诊队列 | `PatientSelectionView` 左栏 | 侧栏「患者选择」 | `PendingQueue.RefreshCommand`、`PendingQueue.SelectCommand` | `PendingQueueViewModel`（子 VM） |
| 4 | 读卡建档 | `PatientSelectionView` 左栏 | 面板按钮 | `CardReader.ManualReadCardCommand`、`CardReader.ToggleAutoReadCommand` | `CardReaderViewModel`（子 VM） |
| 5 | 接诊 | `RegistrationListView` | 侧栏（Doctor 无「新建挂号」项）→ `[待确认]` 具体路径；列表工具栏「接诊」按钮（`Visibility=IsDoctor`） | `StartVisitCommand`（`CanExecute`：`SelectedRegistration.Status == Waiting && !IsBusy`）→ `IRegistrationService.StartVisitAsync` → `PUT /api/v1/registrations/{id}/start-visit` | `RegistrationListViewModel` |
| 6 | 进入工作台 | `MedicalCaseWorkspaceView` | 接诊成功后导航，参数用 **`MedicalCaseNav.ForExistingCase`**：`MedicalCaseId` / `CurrentPatient` / `WorkspaceMode.Clinical` / `InitialEditState.Editing`（契约见 [desktop-ui-detailed-design.md §5](desktop-ui-detailed-design.md#5-跨页数据流--导航契约)） | — | `MedicalCaseWorkspaceViewModel` |
| 7 | 编辑诊疗内容 | `MedicalCaseWorkspaceView` | 内嵌编辑区 | `ConsultationEditor.*`、`PrescriptionEditor.*` | `ConsultationEditorViewModel`、`PrescriptionEditorViewModel` |
| 8 | 保存 | 同上 | 工作台按钮 | `Commands.SaveCommand`（`CanSave` = `State.IsEditing`） | `MedicalCaseCommandsViewModel` |
| 9 | 挂起 | 同上 | 工作台按钮 | `Commands.SuspendCommand` | `MedicalCaseCommandsViewModel` |
| 10 | 完成 | 同上 | 工作台按钮 | `Commands.CompleteCommand`（`CanComplete` = `ShowCompleteButton && CanComplete`） | `MedicalCaseCommandsViewModel` |
| 11 | 打印 / 导出 | 同上 / `BaseDetailContainer` | 按钮「打印处方单」「导出PDF」；详情容器 `Ctrl+P` | `Commands.PrintCommand` → `PrescriptionPrintHandler.PrintPreviewAsync`；`Commands.ExportPdfCommand`；**仅已完成（Completed）医案可打印** | `MedicalCaseCommandsViewModel`、`PrescriptionPrintHandler` |
| 12 | 辅助操作 | 同上 | 按钮 / 对话框 | `ImportFormulaCommand`（`FormulaImportDialog`）、`CopyHistoryCommand`（`HistoryCopyDialog`）、`ClearHerbsCommand`、`EnterEditModeCommand` | `MedicalCaseCommandsViewModel` |
| 13 | 收尾 | `PatientSelectionView` / `MedicalCaseMasterDetailView` | 返回 | `BackCommand` → `WorkspaceNavigationHandler`（Clinical → `PatientSelectionView`；Management 只读 → `MedicalCaseMasterDetailView`） | `MedicalCaseWorkspaceViewModel` |

> 「未保存更改」由 `WorkspaceNavigationHandler` 弹出 `UnsavedChangesDialog`（`DialogViewModelBase` 派生 VM `UnsavedChangesDialogViewModel`）。

---

## 7. Receptionist（UserRole.Receptionist · 前台接待）旅程 — 前台链

### 7.1 完整链路

```
患者建档(PatientManagementView) ──CreateNewCommand──► PatientEditControl ──SaveCommand──► 患者就绪
                                                                                            │
挂号创建(RegistrationCreateDialog) ◄──CreateRegistrationCommand── RegistrationListView ◄────┘
        │ ConfirmCommand
        ▼
   挂号(Waiting) ──[患者到场但爽约]──► CancelRegistrationCommand ──► Cancelled
```

### 7.2 分步

| # | 步骤 | 承载界面（x:Class） | 进入方式 | 关键命令 | 涉及 VM |
|---|------|------------------|---------|---------|--------|
| 1 | 登录 → 首页 | `ReceptionistHomeView` | `RoleRegistry` 首页 | — | `ReceptionistHomeViewModel` |
| 2 | 首页检索 | `ReceptionistHomeView` | 搜索框（**回车**触发） | `SearchPatientCommand`（`KeyBinding Key="Return"`） | `ReceptionistHomeViewModel` |
| 3 | 患者建档 | `PatientManagementView` | 首页「新建患者」/ 侧栏「患者管理」 | `CreateNewPatientCommand`、`NavigateToPatientManagementCommand` → `PatientMasterDetailViewModel.CreateNewCommand` → `PatientEditControl` | `PatientMasterDetailViewModel`、`PatientEditorViewModel` |
| 4 | 患者资料维护 | `PatientEditControl`（Control） | 主从详情右侧编辑态 | `SaveCommand`、`CancelCommand`、`DeleteCommand`（基类命令） | `PatientEditorViewModel` |
| 5 | 批量建档/导出 | `PatientMasterDetailControl` 工具栏 | 工具栏附加按钮 | `ImportPatientsCommand`、`ExportPatientsCommand`、`DownloadImportTemplateCommand` | `PatientMasterDetailViewModel` |
| 6 | 读卡录入 | `PatientMasterDetailControl` | 工具栏按钮 | `ReadCardCommand`（`CanReadCard` 绑定 `IsCardReaderConnected`） | `PatientMasterDetailViewModel` / `PatientCardReaderViewModel` |
| 7 | 挂号 | `RegistrationListView` | 侧栏「新建挂号」/ 首页「新建挂号」 | `CreateRegistrationCommand`（`CanCreateRegistration` = `IsReceptionist && !IsBusy`）→ `IDialogService.ShowDialog("RegistrationCreateDialog")` | `RegistrationListViewModel` |
| 8 | 填写挂号 | `RegistrationCreateDialog`（Dialog） | 对话框 | `SearchPatientsCommand`、`SelectPatientCommand`、`ClearPatientSelectionCommand`、`ConfirmCommand`（`RegistrationCreateDialogViewModel` 重写 `CanConfirm`/`Confirm`）→ `CreateAsync`（`Source = RegistrationSource.Receptionist`）→ `CloseDialog(ButtonResult.OK)` | `RegistrationCreateDialogViewModel` |
| 9 | 取消挂号 | `RegistrationListView` | 工具栏「取消挂号」（`Visibility=IsReceptionist`） | `CancelRegistrationCommand`（`CanExecute`：`IsReceptionist && Status == Waiting && Source == Receptionist && !IsBusy`） | `RegistrationListViewModel` |
| 10 | 队列维护 | `RegistrationListView` | 自动（30s `PeriodicTimer`）与事件 | `RefreshCommand`；订阅 `RegistrationRefreshedEvent` | `RegistrationListViewModel` |
| 11 | 收尾 | `SideNavControl` | 侧栏底部 | `LogoutCommand` | `SideNavViewModel` |

---

## 8. 跨角色交接点

| 交接 | 上游产物 | 下游消费 | 传递方式 |
|------|---------|---------|---------|
| 前台 → 医生 | 挂号记录（`RegistrationStatus.Waiting`） | 医生「接诊」按钮 | `RegistrationRefreshedEvent` + `IRegistrationService.StartVisitAsync` |
| 接诊 → 工作台 | `MedicalCaseId`、`PatientDetailDto` | `MedicalCaseWorkspaceView` | **`MedicalCaseNav.ForExistingCase`**（`MedicalCaseId` / `CurrentPatient` / `WorkspaceMode` / `InitialEditState`；ViewRoleAccess 含 Receptionist） |
| 工作台 → 打印 | 已完成医案 | `PrescriptionPrintService` | `PrescriptionPrintHandler` → `IPrintService<PrescriptionPrintModel>` |
| 任意角色 → 个人资料 | `AccountSettingsView` | 顶栏用户区 | `HeaderViewModel.EditProfileCommand` → `MenuManager.EditProfileCommand` → `NavigateTo(ViewNames.AccountSettings)` |
| 会话失效 → 登录 | `TokenLifecycleState.Expired` | `LoginRegion` | `LoginStateManager.HandleTokenExpiredAsync` → `LogoutRequested` → `ShowLoginDialog()` |

---

## 9. 已知文档-代码落差

| # | 项 | 既有文档/需求口径 | 代码实际 |
|---|----|----------------|---------|
| 1 | 侧栏导航 | 曾称「侧栏仅保留全局操作（个人资料/主题/退出），不放功能导航」 | `SideNavControl` 绑定 `GroupedNavigationItems`，承载按角色的导航矩阵；底部才是主题/退出 |
| 2 | 侧栏分组名 | 常被写作「主页/业务/管理」 | 实为 **`临床` / `目录` / `管理`** |
| 3 | Doctor 首页 | 曾写 `ClinicalHomeView` | `RoleRegistry` 注册 **`ClinicalWorkspaceView`**；`ClinicalHomeView` 仅为未注册角色 fallback |
| 4 | 命令名 | `CompleteVisit` / `SaveMedicalCase` | 代码中不存在；等价物为 `CompleteCommand` / `SaveCommand`（`MedicalCaseCommandsViewModel`） |
| 5 | 读卡命令绑定 | — | `PatientSelectionView.xaml` 绑 `CardReader.ReadCardCommand`，而 `CardReaderViewModel` 源生成的是 `ManualReadCardCommand`（**绑定名不一致，需复核**） |
| 6 | 快捷键 Ctrl+, | 顶栏 ToolTip 标注「个人资料 (Ctrl+,)」 | `Ctrl+,` 实际绑定 `ShowSettingsCommand` → Toast「用户设置功能将在未来版本中实现」；个人资料入口是顶栏按钮 `EditProfileCommand` |
| 7 | 患者域快捷入口 | `PatientMasterDetailViewModel.NewConsultationCommand` / `ViewMedicalRecordsCommand` | 命令体仅日志 + `// FUTURE` 注释，**未实际导航** |
| 8 | 落地页 | 清单中的 `PendingQueueView`(DOC-02) | 已确认删除；待诊队列内嵌于 `PatientSelectionView`，`PendingQueueViewModel` 保留 |

---

## 10. 事实来源

- 计数与清单口径：以 `src/Client/Desktop` 源码扫描为准（排除 `bin/`/`obj/`，口径见 [`DESKTOP_ARCHITECTURE_STANDARD.md`](../../src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md) §13.5）；全量清单见 [`desktop-view-inventory.md`](../compose/specs/desktop-view-inventory.md)。正文一律表述为「代码实际（src/Client/Desktop）」。
- 角色与首页：`Core/LYBT.Desktop.Infrastructure/Roles/RoleRegistry.cs`、`Roles/Definitions/{SuperAdmin,Admin,Doctor,Receptionist}RoleDefinition.cs`、`Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs`。
- 导航：`Shell/Services/NavigationManager.cs`、`Core/LYBT.Desktop.Infrastructure/Navigation/NavigationCoordinator.cs`、`Shell/Services/Login/LoginCoordinator.cs`、`Shell/Services/ShellEventCoordinator.cs`。
- Shell 三控件：`Shell/Views/{HeaderControl,SideNavControl,FooterControl}.xaml` + `Shell/ViewModels/{HeaderViewModel,SideNavViewModel,FooterViewModel}.cs`、`Shell/Services/ISidebarStateManager.cs`。
- 诊疗链：`Modules/LYBT.Desktop.Registrations/ViewModels/RegistrationListViewModel.cs`、`Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs`、`Roles/LYBT.Desktop.Clinical/ViewModels/{ClinicalWorkspaceViewModel,PatientSelectionViewModel}.cs`、`Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/{PendingQueueViewModel,CardReaderViewModel,WorkspaceNavigationHandler}.cs`。
- 打印：`Core/LYBT.Desktop.Printing/Interfaces/IPrintService.cs`、`Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/PrescriptionPrintHandler.cs`。
- 交叉文档：[desktop-ui-requirements.md](./desktop-ui-requirements.md)、[desktop-layout-framework.md](./desktop-layout-framework.md)、[desktop-ui-detailed-design.md](./desktop-ui-detailed-design.md)、[desktop-ux-interaction-spec.md](./desktop-ux-interaction-spec.md)、[desktop-ux-error-handling.md](./desktop-ux-error-handling.md)、[desktop-ux-loading-states.md](./desktop-ux-loading-states.md)。
