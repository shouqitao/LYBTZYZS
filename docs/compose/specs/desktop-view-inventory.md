# Desktop View 全景图 — 代码对齐的 View / Control / Dialog 清单

> 版本: v1.2 | 日期: 2026-09-23 | 状态: 已对齐代码（2026-09-23 收尾批次：`ConfigExportImportView` + `SessionTimeoutWarningDialog` 落地；B-07：`FirstRunSetupView` → `InitializationWizardView` 同步）
> 事实源: 代码实际（`src/Client/Desktop` 全量扫描，排除 `bin/`、`obj/`）
> 关联: [desktop-layout-framework.md](../../07-ui-ux/desktop-layout-framework.md)（三栏框架 SSOT）、[desktop-ui-detailed-design.md](../../07-ui-ux/desktop-ui-detailed-design.md)（页面详细规格）
> 品牌: 凌隐宝堂中医诊所（系统名「凌隐宝堂中医诊所管理系统」）

---

## 计数声明（口径）

本文所有数字来自 `src/Client/Desktop` 的 XAML/ViewModel 文件扫描（2026-09-13），口径如下——引用数字时必须声明同一口径：

| 类别 | 数量 | 口径 |
|------|:----:|------|
| **View** | **30** | 页面/导航级 XAML：`*/Views/*.xaml`（含角色台 `Roles/*/Views/`、`Reports/Views/`、`Receptionist/Views/`，含 Shell 的 `MainWindow`/`AppShell`/`HeaderControl`/`SideNavControl`/`FooterControl`/`AccountSettingsView`） |
| **Control** | **33** | 内嵌组件：`*/Controls/*.xaml`（含共享设计系统控件与模块内嵌控件；`Shell/Controls/AccountSettingsControl` 亦归此） |
| **Dialog** | **7** | `*/Dialogs/**/*.xaml`，经 `RegisterDialog` 注册（Shell 的 3 个位于 `Shell/Dialogs/Views/`） |
| **Root** | **1** | `Shell/App.xaml`（应用级资源，非视图） |
| **ViewModel** | **55** | ViewModel 文件数（每文件 1 个 VM 类型） |
| 资源/模板 XAML | 11 | `Core/LYBT.Desktop.Controls/Themes/*`、`Converters/*`、`Core/LYBT.Desktop.Printing/Templates/*`（不计入视图） |

> XAML 合计 **82** = 视图 **71**（View 30 + Control 33 + Dialog 7 + Root 1）+ 资源/模板 **11**。
>
> **口径例外（路径计为 View，实际按对话框使用）**：`Auth/Views/ServerConfigView.xaml` 与 `Auth/Views/InitializationWizardView.xaml` 物理位于 `Views/`，两者均经 `RegisterDialog` 作为**对话框**注册（`InitializationWizardView` 同时经 `RegisterForNavigation` 可导航——B-07 双入口）；本文按路径口径计入 View 表，同时在「对话框清单」中以调用方视角列出。

---

## 设计原则

1. **每个 View 对应一个导航目标**（Prism Region 注册或 `RegisterForNavigation`），不是每个弹窗
2. **按用户故事驱动**：从登录开始，跟随每个角色的工作时序排列
3. **角色决定入口**：`RoleRegistry` 按角色加载不同模块 → 不同角色看到不同 View
4. **细节可粗糙**：本文档先定 View 清单和导航关系，不深入控件细节
5. **主页卡片导航 + 侧栏角色矩阵（2026-09-13 校正）**：每个角色的 Home View 是导航中心，以功能卡片网格展示该角色入口（`AdminHomeView`/`ReceptionistHomeView`/`SysadminHomeView`/`ClinicalWorkspaceView` 均有卡片区）；**侧栏并非只放全局操作**——`SideNavControl` 绑定 `SideNavViewModel.GroupedNavigationItems`，由 `NavigationManager.BuildNavigationItems(role)` 按角色生成导航矩阵（主页 + 2 个业务入口，按「临床/目录/管理」分组），侧栏**底部固定区**承载全局操作（深色模式/退出，退出经 `IShellLogoutService` 守卫）；**个人资料入口在顶栏 Header**（`HeaderViewModel.EditProfileCommand` → `AccountSettingsView`）

---

## 全局 View 清单（按层/模块，共 30）

### Shell 层（所有角色共享，6）

| # | View 名 | 路径 | 职责 | 绑定 VM | 导航入口 | 需求依据 |
|---|---------|------|------|---------|----------|----------|
| S-01 | **MainWindow** | `src/Client/Desktop/Shell/Views/MainWindow.xaml` | 主窗口：`LoginRegion`（未登录）+ `AppShell`（已登录）切换容器 | `MainWindowViewModel`（`ShellViewMappings` 显式映射） | 应用启动（`App.xaml.cs` 创建并显示） | US-SHELL-001 / US-SHELL-005 |
| S-02 | **AppShell** | `src/Client/Desktop/Shell/Views/AppShell.xaml` | 登录后主框架：Header(48) + 下部 [SideNav(240/64) + 右列(内容区 + Footer32)]；内容区为唯一 Prism Region（`RegionNames.ContentRegion` = `"ContentRegion"`，无其他内容区常量） | 无独立 VM（继承宿主 `MainWindowViewModel` 的 DataContext） | 由 MainWindow 内嵌（非导航目标） | [待确认]（布局依据 `desktop-layout-framework.md`） |
| S-03 | **HeaderControl** | `src/Client/Desktop/Shell/Views/HeaderControl.xaml` | 顶部应用栏：当前用户显示名/角色/首字 + **个人资料入口** | `HeaderViewModel`（`ShellViewMappings` 显式映射） | 由 AppShell 内嵌 | US-SHELL-004 |
| S-04 | **SideNavControl** | `src/Client/Desktop/Shell/Views/SideNavControl.xaml` | 左侧导航：**角色导航矩阵**（`GroupedNavigationItems` 按分组渲染）+ 底部固定区（深色模式/退出） | `SideNavViewModel`（`ShellViewMappings` 显式映射） | 由 AppShell 内嵌 | US-SHELL-003 / US-SHELL-005 |
| S-05 | **FooterControl** | `src/Client/Desktop/Shell/Views/FooterControl.xaml` | 底部状态栏（h32）：连接模式/API 状态/当前用户/时间 | `FooterViewModel`（`ShellViewMappings` 显式映射） | 由 AppShell 内嵌 | US-SHELL-007 |
| S-06 | **AccountSettingsView** | `src/Client/Desktop/Shell/Views/AccountSettingsView.xaml` | 个人资料页（姓名/密码/会话相关设置），宿主 `AccountSettingsControl` | `AccountSettingsViewModel`（经内嵌 `AccountSettingsControl` 的 `AutoWireViewModel`） | 顶栏 `EditProfileCommand`（`MenuManager.ExecuteAccountSettings`）→ `ViewNames.AccountSettings` | US-SHELL-004 / US-USER-008 / US-USER-009 |

> **Shell 侧栏事实（2026-09-13）**：`SideNavControl` 承载角色导航矩阵（`NavigationManager.BuildNavigationItems` 按角色生成：每角色恰好 3 项 = 标题「主页」+ 2 个业务入口，`NavigationItem.Group` 取值为「临床」/「目录」/「管理」）+ 底部全局操作（深色模式/退出）；个人资料入口在顶栏 Header。
> **绑定机制**：`HeaderControl`/`SideNavControl`/`FooterControl` 的约定 VM 名与实际 VM 类名不一致，Prism 约定解析会**静默失败**，故经 `ShellViewMappings`（5 条显式映射，`App.ConfigureViewModelLocator` 调用）解析。侧栏状态 SSOT = `ISidebarStateManager`；主题 SSOT = `IThemeService`；登出唯一入口 = `IShellLogoutService`。

### Auth 模块（登录入口，3）

| # | View 名 | 路径 | 职责 | 绑定 VM | 导航入口 | 需求依据 |
|---|---------|------|------|---------|----------|----------|
| A-01 | **LoginView** | `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginView.xaml` | 登录页（用户名 + 密码 + 模式选择/服务器配置入口） | `LoginViewModel` | 启动流程导航至 `LoginRegion`（`AuthenticationModule.RegisterForNavigation`） | US-AUTH-001 |
| A-02 | **ServerConfigView** | `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/ServerConfigView.xaml` | 服务器地址配置（远程模式）——**经 `RegisterDialog` 作对话框使用** | `ServerConfigViewModel` | `LoginViewModel.OpenSettings` → `ShowDialog(nameof(ServerConfigView))` | US-SHELL-007 |
| A-03 | **InitializationWizardView** | `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/InitializationWizardView.xaml` | 首次初始化向导（5 步：欢迎+模式选择 / 模式配置（含「测试连接」）/ 诊所信息 / 初始管理员 / 校验+完成）——**`RegisterForNavigation`（可导航）+ `RegisterDialog`（模态）双入口**，第 5 步写 `first_run_done.flag` | `InitializationWizardViewModel`（基类 `ConnectionTestViewModelBase`） | ① `ShellEventCoordinator.OnLoginSucceeded`（sysadmin + `IFirstRunStateService.IsFirstRun`）② `SysadminHomeView` 手动入口 | US-SHELL-011 |

### MedicalCase 模块（3）

| # | View 名 | 路径 | 职责 | 绑定 VM | 导航入口 | 需求依据 |
|---|---------|------|------|---------|----------|----------|
| M-01 | **ReportsHomeView** | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Reports/Views/ReportsHomeView.xaml` | 统计报表（收入/就诊/药材排行/趋势） | `ReportsHomeViewModel`（`ReportsModule` 显式 `ViewModelLocationProvider.Register`） | `AdminHomeViewModel.NavigateToReports` / `ClinicalHomeViewModel.NavigateToReports` | US-REPORT-001~004 |
| M-02 | **AuditLogView** | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml` | 医案审计日志（20 字段差异） | `AuditLogViewModel` | `AdminHomeViewModel.NavigateToAuditLog` / `ClinicalHomeViewModel.NavigateToAuditLog` / `MedicalCaseWorkspaceViewModel.ExecuteViewAuditLogs`（带 `MedicalCaseId`） | US-MC-017 |
| M-03 | **MedicalCaseMasterDetailView** | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseMasterDetailView.xaml` | 医案主从容器（宿主 `MedicalCaseMasterDetailControl`，`DataContext="{Binding}"` 透传） | `MedicalCaseMasterDetailViewModel` | `WorkspaceNavigationHandler` 离开工作台时导航（`ViewNames.MedicalCaseMasterDetail`） | US-MC-005 / US-MC-006 |

### Registrations 模块（1）

| # | View 名 | 路径 | 职责 | 绑定 VM | 导航入口 | 需求依据 |
|---|---------|------|------|---------|----------|----------|
| R-01 | **RegistrationListView** | `src/Client/Desktop/Modules/LYBT.Desktop.Registrations/Views/RegistrationListView.xaml` | 挂号队列（列表 + 新建/接诊/取消，按角色显示操作） | `RegistrationListViewModel` | 侧栏「新建挂号」（Receptionist）/「挂号」卡片（各 Home）/ `ReceptionistHomeViewModel.NavigateToRegistrationQueue` | US-REG-001 / US-REG-002 / US-REG-004 / US-REG-006 |

### Sysadmin 角色（SuperAdmin，5）

| # | View 名 | 路径 | 职责 | 绑定 VM | 导航入口 | 需求依据 |
|---|---------|------|------|---------|----------|----------|
| SY-01 | **SysadminHomeView** | `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin/Views/SysadminHomeView.xaml` | **运维设置主页**：配置中心（双模式 Tab）+ 功能入口卡片 + 状态卡片 | `SysadminHomeViewModel` | `SuperAdminRoleDefinition.HomeViewName`（登录后首页）/ 侧栏「主页」 | US-SHELL-018 |
| SY-02 | **BackupManagementView** | `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin/Views/BackupManagementView.xaml` | 备份文件列表 + 手动备份 + 恢复 | `BackupManagementViewModel` | 侧栏「备份管理」（SuperAdmin）/ `SysadminHomeView` 本地模式「备份恢复」Tab 内嵌 | US-SHELL-013 |
| SY-03 | **DeploymentView** | `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin/Views/DeploymentView.xaml` | 远程部署上传 + 服务重启 | `DeploymentViewModel` | 侧栏「部署管理」/ `SysadminHomeViewModel.NavigateToDeployment` | US-SHELL-020 |
| SY-04 | **LogLevelControlView** | `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin/Views/LogLevelControlView.xaml` | 运行时日志级别调整 | `LogLevelControlViewModel` | `SysadminHomeViewModel.NavigateToLogLevelControl`（功能入口卡片） | US-LOG-005 / US-SYS-006 |
| SY-05 | **SecurityAuditLogView** | `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin/Views/SecurityAuditLogView.xaml` | 安全审计日志查看（仅远程；本地空态） | `SecurityAuditLogViewModel` | `SysadminHomeViewModel.NavigateToSecurityAuditLog` | US-SHELL-014 / US-LOG-004 |

> **SY-01 内嵌子 VM（非独立视图）**：`ConfigurationCenterViewModel`（配置中心「配置」Tab，含诊所信息/会话/连接/安全策略/功能开关/读卡器诊断组）、`CardReaderDiagnosticsViewModel`（读卡器诊断面板，US-SHELL-019）、`ServerConfigSectionViewModel`（「服务端配置」Tab，仅远程，US-SHELL-018 双模式面板 / ADR-0014）；另有 `SysadminHomeViewModel.Dashboard`（数据库/系统信息状态卡）与用户管理入口（`NavigateToUserManagement`）。

### Admin 角色（3）

| # | View 名 | 路径 | 职责 | 绑定 VM | 导航入口 | 需求依据 |
|---|---------|------|------|---------|----------|----------|
| ADM-01 | **AdminHomeView** | `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml` | 管理工作台主页（功能卡片网格：用户/药材/患者/验方/医案/诊所设置/报表/审计日志） | `AdminHomeViewModel` | `AdminRoleDefinition.HomeViewName`（登录后首页）/ 侧栏「主页」 | US-SHELL-003 |
| ADM-02 | **UserManagementView** | `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/UserManagementView.xaml` | 用户管理（宿主 `UserMasterDetailControl`，列表 + CRUD + 批量操作） | `UserMasterDetailViewModel`（经内嵌控件 `AutoWireViewModel`） | 侧栏「用户管理」（Admin）/ `AdminHomeViewModel.NavigateToUserManagement` / `SysadminHomeViewModel.NavigateToUserManagement` | US-USER-001~012 |
| ADM-03 | **SystemSettingsView** | `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/SystemSettingsView.xaml` | **诊所设置**（诊所名称/地址/电话等业务信息，`clinic-settings.json` 热更新；含保存/重置） | `SystemSettingsViewModel` | `AdminHomeViewModel.NavigateToSystemSettings`（「诊所设置」卡片） | [待确认]（诊所配置化，需求编号未在需求库定位） |

> **Admin 权限边界**：Admin 只读查看挂号/医案，不可创建/取消/接诊；药材/用户管理可 CRUD；验方可 CRUD。诊所设置（ADM-03）与运维设置（SY-01）职责不同，不重叠。

### Clinical 角色（Doctor / Receptionist，9）

| # | View 名 | 路径 | 职责 | 绑定 VM | 导航入口 | 需求依据 |
|---|---------|------|------|---------|----------|----------|
| DOC-01 | **ClinicalWorkspaceView** | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml` | ⭐ **医生首页**：患者选择（内嵌 `PatientSelectionControl`）+ 选中患者信息/历史 + 开始接诊 | `ClinicalWorkspaceViewModel` | `DoctorRoleDefinition.HomeViewName`（`RoleRegistry` 注册：Doctor 首页 = `ClinicalWorkspaceView`）/ 侧栏「主页」 | US-MC-001~004 |
| DOC-02 | ~~PendingQueueView~~ | — | ~~独立待诊队列页~~ → **已合并**：队列 UI 内嵌 `PatientSelectionView`（`PendingQueueViewModel` 保留为子 VM） | — | — | US-REG-004 / US-REG-008 |
| DOC-03 | **PatientSelectionView** | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml` | 患者选择/搜索 + 读卡（`CardReader`）+ 待诊队列（`PendingQueue`） | `PatientSelectionViewModel` | 侧栏「患者选择」（Doctor）/ `WorkspaceNavigationHandler` 返回（`ViewNames.PatientSelection`） | US-MC-001 / US-MC-006 / US-REG-004 |
| DOC-04 | **MedicalCaseWorkspaceView** | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml` | 医案详情工作台（宿主 `BaseDetailContainer`，诊断 + 处方 + 编辑态/完整性指示） | `MedicalCaseWorkspaceViewModel` | 侧栏「医案工作台」（Doctor）/ `PatientSelectionViewModel`、`ClinicalWorkspaceViewModel`、`CardReaderViewModel`、`PendingQueueViewModel` 导航进入 | US-MC-002 / US-MC-004 |
| DOC-05 | **MedicalCaseManagementView** | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseManagementView.xaml` | 医案列表（宿主 `MedicalCaseMasterDetailControl`，按角色过滤） | `MedicalCaseMasterDetailViewModel`（经内嵌控件） | `ClinicalHomeViewModel.NavigateToMedicalCaseQuery` / `AdminHomeViewModel.NavigateToMedicalCaseManagement` | US-MC-005 / US-MC-006 / US-MC-007 |
| DOC-06 | **FormulaManagementView** | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/FormulaManagementView.xaml` | 验方管理（宿主 `FormulaMasterDetailControl`，创建/共享/验证/导入导出） | `FormulaMasterDetailViewModel`（经内嵌控件） | `ClinicalHomeViewModel.NavigateToFormulaLibrary` / `AdminHomeViewModel.NavigateToFormulaManagement` | US-FORM-001~014 |
| DOC-07 | **HerbManagementView** | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/HerbManagementView.xaml` | 药材管理（宿主 `HerbMasterDetailControl`；医生为只读查看 + 缓存） | `HerbMasterDetailViewModel`（经内嵌控件） | 侧栏「药材/验方」（Admin）/ `ClinicalHomeViewModel.NavigateToHerbLibrary` / `AdminHomeViewModel.NavigateToHerbManagement` | US-HERB-001 / US-HERB-014 |
| DOC-08 | **PatientManagementView** | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientManagementView.xaml` | 患者管理（宿主 `PatientMasterDetailControl`，CRUD + 导入导出 + 读卡登记） | `PatientMasterDetailViewModel`（经内嵌控件） | 侧栏「患者管理」（Receptionist）/ `ReceptionistHomeViewModel.NavigateToPatientManagement` / 各 Home 卡片 / 工作台与患者选择页导航 | US-PAT-001~014 |
| DOC-09 | **ClinicalHomeView** | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml` | 临床工作台主页（卡片网格 + 今日统计）——**存在但非 Doctor 首页（保留）**，仅作 `RoleRegistry.DefaultHomeView` fallback | `ClinicalHomeViewModel` | 仅未注册角色的 fallback（`RoleRegistry.DefaultHomeView`）；无侧栏入口 | US-SHELL-003 |
| REC-01 | **ReceptionistHomeView** | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Receptionist/Views/ReceptionistHomeView.xaml` | 前台工作台主页（搜索患者/新建挂号/新患者 + 今日统计 + 功能卡片） | `ReceptionistHomeViewModel` | `ReceptionistRoleDefinition.HomeViewName`（登录后首页）/ 侧栏「主页」 | US-SHELL-003 |

> **Receptionist 权限边界**：仅患者 CRUD + 挂号创建/取消；不可查看药材/验方/医案/用户管理/打印/报表。
> **Receptionist 新建挂号 = 对话框**：`RegistrationCreateDialog`（见下表），非独立页面——侧栏「新建挂号」与 Home 卡片均导航到 `RegistrationListView`，由 `RegistrationListViewModel.CreateRegistrationCommand` 弹出对话框。

---

## 对话框清单（7，经 `RegisterDialog` 注册）

| # | Dialog 名 | 路径 | 职责 | 绑定 VM | 调用方 | 需求依据 |
|---|-----------|------|------|---------|--------|----------|
| SD-01 | **ConfirmationDialog** | `src/Client/Desktop/Shell/Dialogs/Views/ConfirmationDialog.xaml` | 通用确认弹窗（删除/操作确认） | `ConfirmationDialogViewModel` | `DialogManager.ShowConfirmationAsync`（全局） | 通用 |
| SD-02 | **InputDialog** | `src/Client/Desktop/Shell/Dialogs/Views/InputDialog.xaml` | 通用输入弹窗 | `InputDialogViewModel` | 已注册；当前代码无调用点（`[待确认]` 是否保留） | 通用 |
| SD-03 | **MessageDialog** | `src/Client/Desktop/Shell/Dialogs/Views/MessageDialog.xaml` | 通用消息提示弹窗 | `MessageDialogViewModel` | `DialogManager`（成功/失败/信息提示，全局） | US-ERR-002 |
| DD-01 | **FormulaImportDialog** | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialog.xaml` | 从验方库导入药材到处方 | `FormulaImportDialogViewModel` | `MedicalCaseCommandsViewModel`（处方工具条「验方导入」） | US-FORM-001 / US-MC-002 |
| DD-02 | **HistoryCopyDialog** | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml` | 从历史医案复制处方微调 | `HistoryCopyDialogViewModel` | `MedicalCaseCommandsViewModel`（「复制历史处方」） | US-MC-019 |
| DD-03 | **UnsavedChangesDialog** | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/UnsavedChangesDialog.xaml` | 未保存修改确认（保存/放弃/取消） | `UnsavedChangesDialogViewModel` | `WorkspaceNavigationHandler`（离开工作台守卫） | BR-002 |
| DD-04 | **RegistrationCreateDialog** | `src/Client/Desktop/Modules/LYBT.Desktop.Registrations/Dialogs/RegistrationCreateDialog.xaml` | 新建挂号（选患者 → 选医生 → 确认 → Waiting） | `RegistrationCreateDialogViewModel` | `RegistrationListViewModel.CreateRegistrationCommand` | US-REG-001 |

> **口径注**：`Auth/Views/ServerConfigView` 与 `Auth/Views/InitializationWizardView` 也经 `RegisterDialog` 作对话框使用（后者另经 `RegisterForNavigation` 可导航，B-07），但按路径口径计入 View 表（见「计数声明」口径例外）。

---

## 共享与内嵌控件清单（33）

### Core 共享控件（16，`src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/`）

| # | 控件 | 路径（相对 `.../Controls/`） | 用途 |
|---|------|------------------------------|------|
| C-01 | **BaseDetailContainer** | `BaseDetailContainer.xaml` | 详情页外壳（标题/返回/编辑态/操作按钮区），如 `MedicalCaseWorkspaceView` 宿主 |
| C-02 | **BreadcrumbBar** | `BreadcrumbBar.xaml` | 面包屑导航条 |
| C-03 | **DataGridToolbar** | `DataGridToolbar.xaml` | 列表工具条（创建/刷新 + 附加内容），如 `RegistrationListView` |
| C-04 | **DetailToolbar** | `DetailToolbar.xaml` | 详情工具条 |
| C-05 | **EmptyState** | `EmptyState.xaml` | 空态占位 |
| C-06 | **FormulaViewControl** | `FormulaView/FormulaViewControl.xaml` | 验方只读展示（绑定 `FormulaViewControl` 所在模块 VM 数据） |
| C-07 | **HerbItemControl** | `HerbItem/HerbItemControl.xaml` | 单味药材条目（VM `HerbItemControlViewModel`） |
| C-08 | **HerbListControl** | `HerbList/HerbListControl.xaml` | 处方药材列表（VM `HerbListControlViewModel`） |
| C-09 | **InfoCard** | `InfoCard.xaml` | 信息卡片容器 |
| C-10 | **LoadingOverlay** | `LoadingOverlay.xaml` | 加载遮罩 |
| C-11 | **MasterDetailLayout** | `MasterDetailLayout.xaml` | 主从布局骨架（列表 + 详情） |
| C-12 | **PatientInfoCardControl** | `PatientInfoCardControl.xaml` | 患者信息卡（只读） |
| C-13 | **SearchBox** | `SearchBox.xaml` | 搜索输入框 |
| C-14 | **StatusBadge** | `StatusBadge.xaml` | 状态徽章 |
| C-15 | **ToastControl** | `Toast/ToastControl.xaml` | 轻提示 |
| C-16 | **UnifiedPaginationBar** | `UnifiedPaginationBar.xaml` | 统一分页条 |

### 模块内嵌控件（16）

| # | 控件 | 路径 | 用途 | 绑定 VM |
|---|------|------|------|---------|
| CM-01 | **FormulaEditControl** | `src/Client/Desktop/Modules/LYBT.Desktop.Catalog/Controls/FormulaEditControl.xaml` | 验方编辑表单 | 由宿主 MasterDetail 提供 DataContext |
| CM-02 | **FormulaMasterDetailControl** | `.../Catalog/Controls/FormulaMasterDetailControl.xaml` | 验方主从（列表 + 详情 + 编辑） | `FormulaMasterDetailViewModel`（`AutoWireViewModel`） |
| CM-03 | **HerbEditControl** | `.../Catalog/Controls/HerbEditControl.xaml` | 药材编辑表单 | 由宿主提供 DataContext |
| CM-04 | **HerbMasterDetailControl** | `.../Catalog/Controls/HerbMasterDetailControl.xaml` | 药材主从 | `HerbMasterDetailViewModel`（`AutoWireViewModel`） |
| CM-05 | **HerbViewControl** | `.../Catalog/Controls/HerbViewControl.xaml` | 药材只读展示 | 由宿主提供 DataContext |
| CM-06 | **MedicalCaseEditControl** | `.../MedicalCase/Controls/MedicalCaseEditControl.xaml` | 医案编辑表单 | 由宿主提供 DataContext |
| CM-07 | **MedicalCaseMasterDetailControl** | `.../MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml` | 医案主从 | `MedicalCaseMasterDetailViewModel`（`AutoWireViewModel`） |
| CM-08 | **MedicalCaseViewControl** | `.../MedicalCase/Controls/MedicalCaseViewControl.xaml` | 医案只读展示 | 由宿主提供 DataContext |
| CM-09 | **WorkflowStepIndicator** | `.../MedicalCase/Controls/WorkflowStepIndicator.xaml` | 三步工作流步骤指示器 | 由宿主提供 DataContext |
| CM-10 | **PatientEditControl** | `.../Patients/Controls/PatientEditControl.xaml` | 患者编辑表单 | 由宿主提供 DataContext |
| CM-11 | **PatientMasterDetailControl** | `.../Patients/Controls/PatientMasterDetailControl.xaml` | 患者主从 | `PatientMasterDetailViewModel`（`AutoWireViewModel`） |
| CM-12 | **PatientSelectionControl** | `.../Patients/Controls/PatientSelectionControl.xaml` | 患者选择面板（内嵌于 `ClinicalWorkspaceView`） | 由宿主提供 DataContext |
| CM-13 | **PatientViewControl** | `.../Patients/Controls/PatientViewControl.xaml` | 患者只读展示 | 由宿主提供 DataContext |
| CM-14 | **UserEditControl** | `.../Users/Controls/UserEditControl.xaml` | 用户编辑表单 | 由宿主提供 DataContext |
| CM-15 | **UserMasterDetailControl** | `.../Users/Controls/UserMasterDetailControl.xaml` | 用户主从 | `UserMasterDetailViewModel`（`AutoWireViewModel`） |
| CM-16 | **UserViewControl** | `.../Users/Controls/UserViewControl.xaml` | 用户只读展示 | 由宿主提供 DataContext |

### Shell 控件（1）

| # | 控件 | 路径 | 用途 | 绑定 VM |
|---|------|------|------|---------|
| CS-01 | **AccountSettingsControl** | `src/Client/Desktop/Shell/Controls/AccountSettingsControl.xaml` | 个人资料表单主体（由 `AccountSettingsView` 宿主） | `AccountSettingsViewModel`（`AutoWireViewModel` + `ShellViewMappings`） |

---

## 未建视图与能力归属（历史条目收编）

原 v0.1 清单中的以下条目**在代码中不存在独立视图**；其能力若已由别处承载，按下表归属说明，统一标注 `[未建视图]` / `已合并`（后续批次已真实落地的条目改标 ✅）：

| 原编号 | 原条目 | 现状 | 能力归属（代码实际） |
|--------|--------|------|----------------------|
| W-01 | ~~`InitializationWizardView`~~ | ✅ **已建（2026-09-23 B-07）** | 首次初始化向导 = `Modules/LYBT.Desktop.Auth/Views/InitializationWizardView.xaml`（5 步，`RegisterForNavigation` + `RegisterDialog` 双入口，US-SHELL-011）；旧单屏 `FirstRunSetupView` 已删除 |
| SY-05 | `CardReaderDiagnosticsView` | `[未建视图]` | 读卡器诊断面板内嵌于 `SysadminHomeView`（子 VM `CardReaderDiagnosticsViewModel`，US-SHELL-019 已落地），非独立导航页 |
| SY-07 | ~~`ConfigExportImportView`~~ | ✅ **已建（2026-09-23 收尾批次）** | 配置包导出/导入 = `Roles/LYBT.Desktop.Admin/Sysadmin/Views/ConfigExportImportView.xaml`（VM `ConfigExportImportViewModel`；`ViewNames.ConfigExportImport` + `SysadminModule.RegisterForNavigation`；`SysadminHomeView` 第 7 个功能卡「配置导入导出」；US-SHELL-016）；业务数据 JSON 导入导出仍分散在各 MasterDetail 页面 |
| SY-08 | `ServerConfigPanelView` | `[未建视图]` | 服务端配置面板内嵌于 `SysadminHomeView`「服务端配置」Tab（子 VM `ServerConfigSectionViewModel`，仅远程模式，US-SHELL-018 / ADR-0014） |
| AD-01 | ~~`SessionTimeoutWarningDialog`~~ | ✅ **已建（2026-09-23 收尾批次）** | 会话超时预警 = `Shell/Dialogs/Views/SessionTimeoutWarningDialog.xaml`（VM `SessionTimeoutWarningDialogViewModel`，`Shell/App.xaml.cs` `RegisterDialog`）+ `Shell/Services/Session/SessionTimeoutMonitor.cs`（US-AUTH-014）；mm:ss 倒计时 + 「续期」/「退出」 |
| DD-04 | `UnfinishedCaseDialog` | `[未建视图]` | 未完成医案处理由 `PendingQueueViewModel`（挂起医案继续/新建分支）与 `UnsavedChangesDialog` 共同承载 |
| DD-05 | `PrintPreviewDialog` | `[未建视图]` | 处方预览由 `Core/LYBT.Desktop.Printing/Services/PrescriptionPreviewWindowBuilder` 以代码构建窗口（无 XAML 对话框），打印经 `PrescriptionPrintExecutor` |
| DOC-02 | `PendingQueueView` | **已合并** | XAML 已删除；队列 UI 内嵌 `PatientSelectionView`，`PendingQueueViewModel` 保留为子 VM |

> 以上条目均**无独立 XAML 文件**，故不计入本文 View/Dialog 计数。

---

## 导航时序图（按角色）

### 全局启动流程

```
App 启动
  │
  ├─ 单实例互斥检查（US-SHELL-001）
  │   └─ 已有实例 → 拒绝启动
  │
  ├─ Splash Screen（Logo + 进度条 ≥1s）
  │   ├─ Step 1: ErrorHandling 初始化
  │   ├─ Step 2: CoreServices 初始化（API 客户端/缓存/映射）
  │   ├─ Step 3: ApiHealthCheck（后台，不阻塞）
  │   └─ Step 4: Warmup（预加载资源）
  │
  ├─ API 不可达？
  │   ├─ 是 → 提示"切换到本地模式"按钮 → ServerConfigView（A-02，对话框）
  │   └─ 否 → 进入登录
  │
  └─ MainWindow（S-01）→ LoginRegion → LoginView（A-01）← ★ 所有角色的入口
```

### 登录后壳层装配（所有角色）

```
LoginView（A-01）登录成功
  │
  └─ MainWindow（S-01）→ AppShell（S-02）
      ├─ HeaderControl（S-03）── 当前用户显示名/角色 + [个人资料] → AccountSettingsView（S-06）
      ├─ SideNavControl（S-04）── 角色导航矩阵（NavigationManager.BuildNavigationItems）
      │     ├─ [主页] → 角色 HomeViewName（RoleRegistry）
      │     ├─ [业务入口 ×2]（按角色：患者选择/医案工作台、新建挂号/患者管理、
      │     │                 用户管理/药材·验方、备份管理/部署管理）
      │     └─ 底部固定区：[深色模式]（IThemeService）/ [退出]（IShellLogoutService 守卫）
      ├─ ContentRegion ← 各 View 导航目标（唯一内容区）
      └─ FooterControl（S-05）── 连接模式/API 状态/当前用户/时间
```

### Sysadmin 导航时序

```
LoginView（A-01）
  │
  └─ SysadminHomeView（SY-01）← 运维设置主页（SuperAdminRoleDefinition.HomeViewName）
      │
      ├─ [Tab: 配置]（ConfigurationCenterViewModel）
      │     ├─ 诊所信息        ← US-SHELL-018
      │     ├─ 会话设置        ← US-SHELL-018
      │     ├─ 连接设置        ← US-SHELL-018 / US-SHELL-007
      │     ├─ 安全策略        ← US-SHELL-018
      │     ├─ 功能开关（热更新）← US-SHELL-018
      │     ├─ 读卡器诊断      ← US-SHELL-019（CardReaderDiagnosticsViewModel）
      │     └─ [按钮] 重启本地服务（仅本地模式）
      │
      ├─ [Tab: 服务端配置]（仅远程，ServerConfigSectionViewModel）← US-SHELL-018 双模式 / ADR-0014
      │
      ├─ [Tab: 备份恢复]（仅本地，内嵌 BackupManagementView）
      │
      ├─ [卡片] 用户管理      → UserManagementView（ADM-02）
      ├─ [卡片] 日志控制      → LogLevelControlView（SY-04）
      ├─ [卡片] 部署管理      → DeploymentView（SY-03）
      ├─ [卡片] 安全审计      → SecurityAuditLogView（SY-05，仅远程）
      ├─ [侧栏] 备份管理      → BackupManagementView（SY-02）
      ├─ [侧栏] 部署管理      → DeploymentView（SY-03）
      │
      └─ [卡片: 状态] 数据库状态 / 系统信息（SysadminHomeViewModel.Dashboard，只读）
```

### Admin 导航时序

```
LoginView（A-01）
  │
  └─ AdminHomeView（ADM-01）← 管理工作台主页（AdminRoleDefinition.HomeViewName）
      │
      ├─ [卡片: 用户管理]     → UserManagementView（ADM-02）
      ├─ [卡片: 药材管理]     → HerbManagementView（DOC-07）
      ├─ [卡片: 患者管理]     → PatientManagementView（DOC-08）
      ├─ [卡片: 验方管理]     → FormulaManagementView（DOC-06）
      ├─ [卡片: 医案管理]     → MedicalCaseManagementView（DOC-05，只读查看全部）
      ├─ [卡片: 诊所设置]     → SystemSettingsView（ADM-03）
      ├─ [卡片: 统计报表]     → ReportsHomeView（M-01）
      ├─ [卡片: 审计日志]     → AuditLogView（M-02）
      ├─ [侧栏] 用户管理      → UserManagementView（ADM-02）
      ├─ [侧栏] 药材/验方     → HerbManagementView（DOC-07）
      └─ [顶栏: 个人资料]     → AccountSettingsView（S-06）
```

### Doctor 导航时序（核心诊疗流程）

```
LoginView（A-01）
  │
  └─ ClinicalWorkspaceView（DOC-01）← ★ Doctor 首页（RoleRegistry 注册；非 ClinicalHomeView）
      │    内嵌 PatientSelectionControl（患者选择/搜索）
      │
      ├─ 选中患者 → 患者信息（只读）+ 历史就诊（PatientHistory）
      ├─ [按钮] 开始接诊 → StartConsultation → MedicalCaseWorkspaceView（DOC-04）
      ├─ [按钮] 新增患者 → PatientManagementView（DOC-08，Action=AddNew）
      │
      │  ═══════════════════════════════════════════════
      │  ★ 核心诊疗流程（时序从上到下）
      │  ═══════════════════════════════════════════════
      │
      ├─ [侧栏: 患者选择]     → PatientSelectionView（DOC-03）
      │     ├─ 读卡（CardReader 子 VM：手动读卡/自动读卡）
      │     ├─ 待诊队列（PendingQueue 子 VM：US-REG-004/008 实时刷新）
      │     │     └─ StartVisit（Waiting→InProgress + 创建医案，US-REG-005）
      │     └─ QuickVisit（US-REG-002 两步：建 Waiting 挂号 → StartVisit）
      │
      │  ──── StartVisit 完成后 ────
      │
      ├─ [侧栏: 医案工作台]   → MedicalCaseWorkspaceView（DOC-04）← ⭐ 诊疗主界面
      │     ├─ 区域: 患者信息（只读，BaseDetailContainer 标题区）
      │     ├─ 区域: 中医诊断（主诉/现病史/既往史 + 望闻问切 + 辨证，BR-003 必填）
      │     ├─ 区域: 处方编辑
      │     │     ├─ HerbListControl（药材列表控件，复用）
      │     │     ├─ [按钮] 验方导入 → FormulaImportDialog（DD-01）
      │     │     ├─ [按钮] 复制历史处方 → HistoryCopyDialog（DD-02）
      │     │     └─ 药材数量 + 总金额（实时计算）
      │     ├─ [操作按钮组] 保存（US-MC-002）/ 挂起（US-MC-013）/ 完成（US-MC-011，BR-003 校验）/ 取消（US-MC-014）
      │     ├─ [按钮] 查看审计日志 → AuditLogView（M-02，带 MedicalCaseId）
      │     └─ 离开守卫 → UnsavedChangesDialog（DD-03）；离开后 → MedicalCaseMasterDetailView（M-03）
      │
      │  ═══════════════════════════════════════════════
      │  ★ 医案管理（非诊疗流程，日常管理）
      │  ═══════════════════════════════════════════════
      │
      ├─ [卡片: 医案查询]     → MedicalCaseManagementView（DOC-05）
      ├─ [卡片: 验方库]       → FormulaManagementView（DOC-06）
      ├─ [卡片: 药材库]       → HerbManagementView（DOC-07）← 只读 + 缓存
      ├─ [卡片: 患者管理]     → PatientManagementView（DOC-08）
      ├─ [卡片: 挂号]         → RegistrationListView（R-01）
      ├─ [卡片: 报表]         → ReportsHomeView（M-01）
      ├─ [卡片: 审计日志]     → AuditLogView（M-02）
      └─ [顶栏: 个人资料]     → AccountSettingsView（S-06）
```

> **说明**：`ClinicalHomeView`（DOC-09）存在但**不是 Doctor 首页**（仅 `RoleRegistry.DefaultHomeView` fallback，无侧栏入口）；Doctor 首页由 `DoctorRoleDefinition.HomeViewName => ViewNames.ClinicalWorkspace` 注册为 `ClinicalWorkspaceView`。

### Receptionist 导航时序

```
LoginView（A-01）
  │
  └─ ReceptionistHomeView（REC-01）← 前台工作台主页（ReceptionistRoleDefinition.HomeViewName）
      │
      ├─ [搜索框] 查找患者（拼音码/身份证号）
      ├─ [按钮] 新建挂号 → RegistrationListView（R-01，参数 Action=Create）
      │             └─ 创建挂号对话框 → RegistrationCreateDialog（DD-04）
      │                   ├─ 选患者 → 选医生 → 确认 → Waiting
      │                   └─ 取消挂号（仅当天 Waiting + 无医案，US-REG-006）
      ├─ [按钮] 新患者登记 → PatientManagementView（DOC-08，参数 Action=Create）
      ├─ [卡片: 患者管理]   → PatientManagementView（DOC-08）
      │             ├─ 新患者登记（读卡/手动，US-CARD-001）
      │             ├─ 查找/编辑患者
      │             └─ 导入/导出（JSON）
      ├─ [卡片: 挂号队列]   → RegistrationListView（R-01）
      ├─ [卡片: 读卡登记]   → 读卡流程（CardReader 子 VM；US-CARD-001）
      ├─ [侧栏] 新建挂号    → RegistrationListView（R-01）
      ├─ [侧栏] 患者管理    → PatientManagementView（DOC-08）
      └─ [顶栏: 个人资料]   → AccountSettingsView（S-06）

  ⚠️ Receptionist 不可访问：药材/验方/医案/用户管理/打印/报表
```

---

## View ↔ ViewModel 绑定机制（2026-09-13 定案）

| 机制 | 适用范围 | 说明 |
|------|----------|------|
| **Prism 约定 + `AutoWireViewModel="True"`** | 多数 View / Control | 约定名与 VM 类名一致时自动解析（如 `LoginView` → `LoginViewModel`） |
| **`ShellViewMappings` 显式映射** | `MainWindow`、`AccountSettingsControl`、`HeaderControl`、`SideNavControl`、`FooterControl` | 约定名不匹配（如 `HeaderControl` 的约定名 `HeaderControlViewModel` 不存在，实际为 `HeaderViewModel`）时，Prism **静默**跳过赋值并让控件继承宿主 DataContext，导致绑定大面积失效且无异常——故 5 条映射在 `App.ConfigureViewModelLocator` 统一注册，并由守卫测试 `ShellViewViewModelBindingTests` 覆盖 |
| **MasterDetail 控件承载 VM** | `UserManagementView`/`PatientManagementView`/`HerbManagementView`/`FormulaManagementView`/`MedicalCaseManagementView`/`MedicalCaseMasterDetailView` | 外层 View 自身不设 `AutoWireViewModel`，由内嵌 `*MasterDetailControl` 解析对应 VM；外层 View 仅作导航壳 |
| **`ViewModelLocationProvider.Register` 显式** | `ReportsHomeView` | `ReportsModule` 注册 `ReportsHomeView` → `ReportsHomeViewModel` |
| **无独立 VM** | `AppShell` | 继承宿主 `MainWindowViewModel` 的 DataContext（侧栏状态经 `ISidebarStateManager` 共享） |

**SSOT 摘要**：侧栏状态 = `ISidebarStateManager`；主题 = `IThemeService`；登出 = `IShellLogoutService`（活跃医案守卫）；导航项 = `NavigationManager`（`INavigationManager`）；角色首页 = `RoleRegistry` + `*RoleDefinition.HomeViewName`。

---

## §drawio 同步差异清单

> 本节仅列出 `desktop-view-inventory.drawio` / `desktop-view-flowcharts.drawio` 需同步的节点标签差异，**本轮不修改 drawio 文件**（XML 风险）。

### `desktop-view-inventory.drawio`

| # | 操作 | 原标签 | 建议标签 | 依据 |
|---|------|--------|----------|------|
| D1-01 | 删 | `CardReaderDiagnosticsView` | —（删除；能力已内嵌） | 无独立视图；`SysadminHomeView` 内嵌 `CardReaderDiagnosticsViewModel` |
| D1-02 | 删 | `PendingQueueView` | —（删除；已合并） | XAML 已删；队列 UI 内嵌 `PatientSelectionView` |
| D1-03 | 改 | `配置导入导出` | `ConfigExportImportView（配置导入导出，2026-09-23 已建）` | 独立视图已落地（US-SHELL-016）——drawio 节点应**保留并改名**（原「删」结论作废） |
| D1-04 | 改 | `侧边栏: 账户 / 主题 / 退出` | `侧栏: 角色导航矩阵（主页+业务入口）+ 底部（主题/退出）；个人资料在顶栏` | `SideNavControl` 绑定 `GroupedNavigationItems`；`HeaderViewModel.EditProfileCommand` |
| D1-05 | 改 | `ClinicalHomeView（临床工作台）` | `ClinicalWorkspaceView（医生首页）` | `DoctorRoleDefinition.HomeViewName = ViewNames.ClinicalWorkspace` |
| D1-06 | 增 | — | `ClinicalHomeView（保留；非医生首页，fallback）` | `RoleRegistry.DefaultHomeView` |
| D1-07 | 增 | — | `SystemSettingsView（诊所设置）` | `AdminModule.RegisterForNavigation<SystemSettingsView>` + `AdminHome` 卡片 |
| D1-08 | 增 | — | `AuditLogView（医案审计日志）` | `MedicalCaseModule.RegisterForNavigation<AuditLogView>` |
| D1-09 | 增 | — | `RegistrationCreateDialog（新建挂号）` | `RegistrationModule.RegisterDialog` |
| D1-10 | 增 | — | `AppShell / HeaderControl / SideNavControl / FooterControl` | `Shell/Views/*`（Shell 层 6 个 View） |
| D1-11 | 改 | `读卡器诊断`（若作为独立节点） | `SysadminHomeView › 配置中心 › 读卡器诊断（内嵌面板）` | 同 D1-01 |
| D1-12 | 改 | `配置面板（Tab/分组）`（若为独立节点） | `SysadminHomeView › 配置中心 Tab（配置 / 服务端配置 / 备份恢复）` | `SysadminHomeView.xaml` TabControl |
| D1-13 | 改 | `主页卡片=导航入口` | `主页卡片=导航入口（各角色 Home）+ 侧栏角色矩阵` | 设计原则 5 校正 |
| D1-14 | 改 | `实线=已有 \| 虚线黄=待新建 \| 主页卡片=导航入口` | `实线=已有 \| 虚线黄=未建视图（能力归属见清单） \| 主页卡片=导航入口` | 「未建视图与能力归属」小节 |

### `desktop-view-flowcharts.drawio`

| # | 操作 | 原标签 | 建议标签 | 依据 |
|---|------|--------|----------|------|
| D2-01 | 改 | `★ InitializationWizardView&#xa;5步强制向导` | `★ InitializationWizardView&#xa;5步初始化向导（已建，2026-09-23 B-07）` | `Auth/Views/InitializationWizardView.xaml`；US-SHELL-011 已实现（原「`[未建视图]`；改由 `FirstRunSetupView` 承载」结论作废） |
| D2-02 | 删 | `PendingQueueView&#xa;待诊队列&#xa;(实时推送 + QuickVisit)` | `PatientSelectionView&#xa;患者选择 + 待诊队列 + 读卡` | `PendingQueueView` 已合并 |
| D2-03 | 删 | `CardReaderDiagnostics&#xa;读卡器诊断` | `SysadminHomeView&#xa;配置中心 › 读卡器诊断（内嵌）` | 同 D1-01 |
| D2-04 | 改 | `ConfigExportImport&#xa;配置导入导出` | `ConfigExportImportView&#xa;配置导入导出（已建，2026-09-23）` | 独立视图已落地（US-SHELL-016）——**保留节点**（原「删」结论作废） |
| D2-05 | 删 | `PrintPreviewDialog&#xa;打印预览` | `处方预览窗口（代码构建，无 XAML）` | `PrescriptionPreviewWindowBuilder` |
| D2-06 | 改 | `★ ClinicalWorkspaceView&#xa;（诊疗主界面 — 最核心的View）` | `★ ClinicalWorkspaceView&#xa;（医生首页 / 诊疗主界面）` | `RoleRegistry` 注册 |
| D2-07 | 改 | `+ ClinicalHomeView&#xa;(临床工作台)` | `ClinicalHomeView（保留；非医生首页，仅 fallback）` | 同 D1-06 |
| D2-08 | 改 | `ClinicalHomeView&#xa;（临床工作台主页）` | `ClinicalWorkspaceView&#xa;（医生首页：患者选择 + 开始接诊）` | 同 D1-05 |
| D2-09 | 增 | — | `HeaderControl&#xa;（个人资料入口）` → `AccountSettingsView` | `HeaderViewModel.EditProfileCommand` |
| D2-10 | 增 | — | `SideNavControl&#xa;（角色导航矩阵：主页 + 2 业务入口 + 底部主题/退出）` | `NavigationManager.BuildNavigationItems` |
| D2-11 | 增 | — | `AppShell&#xa;（Header + SideNav + ContentRegion + Footer）` | `Shell/Views/AppShell.xaml` |
| D2-12 | 改 | `RegistrationListView&#xa;挂号创建 + 取消` | `RegistrationListView&#xa;挂号队列（创建经 RegistrationCreateDialog）` | `RegistrationListViewModel.CreateRegistrationCommand` |
| D2-13 | 增 | — | `RegistrationCreateDialog&#xa;新建挂号（对话框）` | 同 D1-09 |
| D2-14 | 改 | `★ SysadminHomeView&#xa;（配置中心主页）` | `★ SysadminHomeView&#xa;（运维设置主页：配置中心 + 功能入口卡片）` | `SysadminHomeView.xaml` |
| D2-15 | 改 | `Step1: 改密码 → … → Step5: 完成注销` | `Step1: 欢迎+模式选择 → Step2: 模式配置（含「测试连接」）→ Step3: 诊所信息 → Step4: 初始管理员 → Step5: 校验+完成` | `InitializationWizardViewModel`（`InitializationWizardStep` 5 步）；US-SHELL-011 已实现（2026-09-23 B-07） |
| D2-16 | 改 | `角色 → 模块加载矩阵（RoleRegistry）` | `角色 → 模块加载矩阵（RoleRegistry）+ 侧栏导航矩阵（NavigationManager）` | `RoleDefinition.RequiredModules` + `BuildNavigationItems` |

---

## 残留待确认

1. **`InputDialog`（SD-02）**：已注册但代码中无调用点——保留或下线？
2. **`SystemSettingsView`（ADM-03）需求依据**：诊所设置（`clinic-settings.json` 热更新）在需求库未定位到 US 编号——是否需要补立 US？
3. **`MedicalCaseMasterDetailView`（M-03）入口**：代码仅见 `WorkspaceNavigationHandler` 导航进入，是否有其他入口待确认。
4. ~~**会话超时提醒（原 AD-01）**：US-AUTH-005 的 30 秒倒计时对话框在代码中不存在，提醒机制是否已有替代方案待产品确认。~~ **已闭环（2026-09-23 收尾批次）**：`SessionTimeoutWarningDialog`（`Shell/Dialogs/Views/`）+ `SessionTimeoutMonitor`（`Shell/Services/Session/`）已落地——US-AUTH-014，默认提前 `ClientSession:WarningBeforeTimeoutMinutes`（2 分钟）弹 mm:ss 倒计时 + 「续期」/「退出」，0 = 关闭。原「US-AUTH-005」归属有误（US-AUTH-005 实为令牌验证）。
5. **侧栏矩阵条目数差异**：设计 SSOT [desktop-layout-framework.md](../../07-ui-ux/desktop-layout-framework.md) §角色×菜单矩阵每角色列出 4~8 项，代码 `NavigationManager.BuildNavigationItems` 每角色仅生成 3 项（主页 + 2 业务入口，标注为「C+ 角色矩阵」）——以代码为准还是补齐设计矩阵，待确认。
6. **`ClinicalHomeView`（DOC-09）去留**：当前仅作 `RoleRegistry.DefaultHomeView` fallback，是否保留。
