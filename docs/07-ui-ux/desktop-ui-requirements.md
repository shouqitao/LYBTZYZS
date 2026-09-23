# Desktop UI 需求文档
> 版本: v1.5 | 日期: 2026-09-23（收尾批次：数据导入导出独立页 `ConfigExportImportView` 已建；B-07 首次初始化向导；审计修正与计数口径见 §九）

> **基于**: [desktop-design-spec.md](./desktop-design-spec.md) + desktop-view-inventory.md（v0.1，落差已在本文件标注）+ `../../designs/*.pen` 设计稿 **36** 个
>
> **计数口径**: View **30** / Control **33** / Dialog **7** / ViewModel **55**（XAML 合计 **82** = 视图 **71** + 资源模板 **11**）——定义见 §九。

---

## 设计稿对应关系

> 全部 **36** 个设计稿（`../../designs/*.pen`，仓库内无 `.png` 产物）与代码承载者的对应；「代码状态」以代码实际（`src/Client/Desktop`）为准，无对应代码的界面标 `[未建视图]`。

| # | 设计稿文件 | 对应界面（代码） | 角色 | 优先级 | 代码状态 |
|---|-----------|-----------------|------|--------|---------|
| 1 | `../../designs/login.pen` | `LoginView` | All | P0 | ✅ 已实现 |
| 2 | `../../designs/main-window.pen` | `MainWindow` / `AppShell` / `HeaderControl` / `SideNavControl` / `FooterControl` | All | P0 | ✅ 已实现 |
| 3 | `../../designs/patient-list.pen` | `PatientManagementView`（薄包装 → `PatientMasterDetailControl`） | A/D/R | P0 | ✅ 已实现 |
| 4 | `../../designs/medical-case.pen` | `MedicalCaseWorkspaceView` | D | P0 | ✅ 已实现 |
| 5 | `../../designs/registration.pen` | `RegistrationListView` | R/D | P0 | ✅ 已实现 |
| 6 | `../../designs/admin-home.pen` | `AdminHomeView` | A | P0 | ✅ 已实现 |
| 7 | `../../designs/first-run.pen` | `InitializationWizardView` | S | P0 | ✅ 已实现（2026-09-23 B-07：5 步初始化向导；`RegisterForNavigation` + `RegisterDialog` 双入口） |
| 8 | `../../designs/reports.pen` | `ReportsHomeView` | A/D | P1 | ⚠️ 部分实现（3/8 端点） |
| 9 | `../../designs/user-management.pen` | `UserManagementView`（薄包装 → `UserMasterDetailControl`） | A | P1 | ✅ 已实现 |
| 10 | `../../designs/sysadmin-home.pen` | `SysadminHomeView` | S | P1 | ✅ 已实现 |
| 11 | `../../designs/account-settings.pen` | `AccountSettingsView`（薄包装 → `AccountSettingsControl`） | All | [待确认] | ✅ 已实现 |
| 12 | `../../designs/audit-log.pen` | `AuditLogView` | A/D | [待确认] | ✅ 已实现 |
| 13 | `../../designs/backup-management.pen` | `BackupManagementView` | S | [待确认] | ✅ 已实现 |
| 14 | `../../designs/cardreader-diagnostics.pen` | 读卡器诊断面板（内嵌 `SysadminHomeView` 区块） | S | [待确认] | ✅ 已实现（非独立视图：`CardReaderDiagnosticsViewModel` 为 `SysadminHomeView` 内嵌子 VM） |
| 15 | `../../designs/clinical-workspace.pen` | `ClinicalWorkspaceView`（Doctor 首页） | D | [待确认] | ✅ 已实现 |
| 16 | `../../designs/components.pen` | 组件库（`Core/LYBT.Desktop.Controls` 共享控件 + `Shell/Controls/AccountSettingsControl`） | All | [待确认] | ✅ 已实现（非页面帧） |
| 17 | `../../designs/data-import-export.pen` | 数据导入导出（`ConfigExportImportView`） | A/S | [待确认] | ✅ 已实现（2026-09-23 收尾批次：独立 `ConfigExportImportView`，US-SHELL-016；业务数据 JSON 导入导出仍由各 MasterDetail 承载） |
| 18 | `../../designs/deployment.pen` | `DeploymentView` | S | [待确认] | ✅ 已实现 |
| 19 | `../../designs/dialog-common.pen` | `ConfirmationDialog` / `InputDialog` / `MessageDialog` | All | [待确认] | ✅ 已实现 |
| 20 | `../../designs/dialog-formula-import.pen` | `FormulaImportDialog` | A/D | [待确认] | ✅ 已实现 |
| 21 | `../../designs/dialog-history-copy.pen` | `HistoryCopyDialog` | A/D | [待确认] | ✅ 已实现 |
| 22 | `../../designs/dialog-registration-create.pen` | `RegistrationCreateDialog` | R/D | [待确认] | ✅ 已实现（**对话框**，非 `RegistrationCreateView`） |
| 23 | `../../designs/form-formula-edit.pen` | `FormulaEditControl` | A/D | [待确认] | ✅ 已实现 |
| 24 | `../../designs/form-herb-edit.pen` | `HerbEditControl` | A/D | [待确认] | ✅ 已实现 |
| 25 | `../../designs/form-medical-case-edit.pen` | `MedicalCaseEditControl` | A/D | [待确认] | ✅ 已实现 |
| 26 | `../../designs/form-patient-edit.pen` | `PatientEditControl` | A/D/R | [待确认] | ✅ 已实现 |
| 27 | `../../designs/form-user-edit.pen` | `UserEditControl` | A | [待确认] | ✅ 已实现 |
| 28 | `../../designs/formula-management.pen` | `FormulaManagementView`（薄包装 → `FormulaMasterDetailControl`） | A/D | [待确认] | ✅ 已实现 |
| 29 | `../../designs/herb-management.pen` | `HerbManagementView`（薄包装 → `HerbMasterDetailControl`） | A/D | [待确认] | ✅ 已实现 |
| 30 | `../../designs/log-level.pen` | `LogLevelControlView` | S | [待确认] | ✅ 已实现 |
| 31 | `../../designs/medical-case-management.pen` | `MedicalCaseManagementView`（薄包装 → `MedicalCaseMasterDetailControl`） | A/D | [待确认] | ✅ 已实现 |
| 32 | `../../designs/patient-selection.pen` | `PatientSelectionView` | D | [待确认] | ✅ 已实现 |
| 33 | `../../designs/receptionist-home.pen` | `ReceptionistHomeView` | R | [待确认] | ✅ 已实现 |
| 34 | `../../designs/security-audit-log.pen` | `SecurityAuditLogView` | S | [待确认] | ✅ 已实现 |
| 35 | `../../designs/server-config.pen` | `ServerConfigView`（以 Dialog 注册） | All | [待确认] | ✅ 已实现 |
| 36 | `../../designs/system-settings.pen` | `SystemSettingsView` | A | [待确认] | ✅ 已实现 |

> 角色口径：按 `RoleDefinition.RequiredModules` 的模块归属推导（Admin/Doctor/Receptionist/SuperAdmin）；「All」= 基础模块（`AuthenticationModule`/`UsersModule`）即可达。
> 尚无设计稿的界面（3 个，见 §七）：`ClinicalHomeView`、`MedicalCaseMasterDetailView`、`UnsavedChangesDialog`。

---

## 一、通用页面（All 角色）

### 1.1 登录界面
- **设计稿**: `../../designs/login.pen` ✅
- **代码**: `LoginView.xaml` ✅ 已实现（`AuthenticationModule` → `RegisterForNavigation`，AutoWire → `LoginViewModel`）
- **功能**: 用户名/密码登录，记住密码，远程/本地模式切换，API 连接状态
- **交互**: 输入框实时验证，登录按钮 Loading 状态，错误提示（视图内绑定 `ErrorMessage`，并配合全局 Toast）
- **US**: US-AUTH-001/009
- **优先级**: P0

### 1.2 主界面
- **设计稿**: `../../designs/main-window.pen` ✅
- **代码**: `MainWindow.xaml` ✅ 已实现（宿主：`LoginRegion` + `AppShell` + `Snackbar`；`ContentRegion` 唯一且在 `AppShell` 内；2026-09-23 移除无消费者的 MDIX `DialogHost` 包裹层）
- **功能**: 顶部应用栏 `HeaderControl`（品牌 + 用户信息 + **个人资料入口**，h48）；左侧可折叠 `SideNavControl`（展开 240 / 收拢 64）；右侧 `ContentRegion` 工作台；底部 `FooterControl`（API 状态 + 连接模式 + 时间，h32）
- **侧栏内容（代码实际）**: 承载**角色导航矩阵**——`NavigationManager.BuildNavigationItems` 按角色生成，每角色 **3 项 = 标题「主页」+ 2 个业务入口**，`NavigationItem.Group` 取值为**「临床」/「目录」/「管理」**（Doctor：主页·临床 / 患者选择·临床 / 医案工作台·临床；Receptionist：主页·临床 / 新建挂号·临床 / 患者管理·临床；Admin：主页·临床 / 用户管理·管理 / 药材·验方·目录；SuperAdmin：主页·临床 / 备份管理·管理 / 部署管理·管理）；**底部固定区为全局操作**：深色模式切换 + 退出（`IShellLogoutService`，含活跃医案守卫）
- **交互**: 侧栏折叠/展开（`Ctrl+M`，汉堡按钮与快捷键同源）、菜单项图标+文字、角色感知导航项；全局快捷键 `Ctrl+N`/`Ctrl+Shift+C`/`F1`/`Ctrl+,`/`Alt+←`/`Alt+→`/`Alt+Home`
- **US**: US-SHELL-001/003/005
- **优先级**: P0

### 1.3 首次初始化向导
- **设计稿**: `../../designs/first-run.pen` ✅
- **代码**: `InitializationWizardView.xaml` ✅ 已实现（2026-09-23 B-07；VM `InitializationWizardViewModel`，基类 `ConnectionTestViewModelBase`）
- **注册方式**: **双入口**——`RegisterForNavigation<InitializationWizardView, InitializationWizardViewModel>`（可导航）+ `RegisterDialog<InitializationWizardView, InitializationWizardViewModel>`（模态对话框），同一 View/VM；按 `Views/` 路径口径计入 View 计数（见 §九）
- **触发**: ① `ShellEventCoordinator.OnLoginSucceeded`——登录用户为 sysadmin 且 `IFirstRunStateService.IsFirstRun` 时弹出（管理员创建需要 sysadmin 会话，故不在登录前弹出）；② `SysadminHomeView` 手动入口，可随时重新运行
- **功能**: 5 步向导：① 欢迎 + 模式选择（本地全栈 / 远程服务器）② 模式相关配置（本地：数据库连接 LocalDB / SQL Server；远程：服务器地址）——均含「测试连接」③ 诊所信息（名称/科室/地址/电话）④ 初始管理员账号创建（用户名/姓名/密码/确认密码，「创建管理员」；已存在同名账号时可跳过）⑤ 配置校验清单 + 完成（写入 `%LOCALAPPDATA%\LYBT\Desktop\first_run_done.flag`）
- **交互**: 步骤指示器（1-2-3-4-5），上一步/下一步/完成按钮，表单验证，提交中 `IsBusy` 遮罩；第 2 步远程分支须连接测试成功、第 4 步须创建成功方可继续；模式在第 2 步「下一步」应用（先测试后切换）
- **US**: US-SHELL-011
- **优先级**: P0

### 1.4 服务端连接配置
- **设计稿**: `../../designs/server-config.pen` ✅
- **代码**: `ServerConfigView.xaml` ✅ 已实现
- **注册方式**: 以**对话框**注册（`RegisterDialog<ServerConfigView, ServerConfigViewModel>`）；按 `Views/` 路径口径计入 View 计数（见 §九）
- **功能**: 服务器地址/端口配置，连接测试，远程/本地模式切换
- **优先级**: P1

---

## 二、Admin 角色页面

### 2.1 Admin 首页（仪表盘）
- **设计稿**: `../../designs/admin-home.pen` ✅
- **代码**: `AdminHomeView.xaml` ✅ 已实现（`AdminRoleDefinition.HomeViewName`）
- **功能**: 统计卡片（今日挂号/收入/医生/待办），快捷操作，系统状态
- **交互**: 卡片点击跳转，数据实时刷新
- **US**: US-SHELL-003
- **优先级**: P0

### 2.2 用户管理
- **设计稿**: `../../designs/user-management.pen` ✅
- **代码**: `UserManagementView.xaml` ✅ 已实现（薄包装 → `UserMasterDetailControl`；`UserMasterDetailViewModel` 经 `ViewModelLocationProvider` 显式映射）
- **功能**: 用户列表（搜索/筛选/分页），创建/编辑用户对话框，角色分配
- **交互**: 主从详情布局，批量操作，表单验证；支持 `DefaultRoleFilter` 导航参数（sysadmin 管理员账号管理）
- **US**: US-USER-001~012
- **优先级**: P1

### 2.3 诊所设置
- **设计稿**: `../../designs/system-settings.pen` ✅（独立导航页）
- **代码**: `SystemSettingsView.xaml` ✅ 已实现
- **功能**: 诊所配置（名称/地址/电话/科室/执照），功能开关，API 配置
- **交互**: 表单编辑，保存确认
- **US**: US-CFG-001~006
- **优先级**: P1

### 2.4 报表首页
- **设计稿**: `../../designs/reports.pen` ✅
- **代码**: `ReportsHomeView.xaml` ✅ 已实现（`ReportsModule`，AutoWire + 显式映射）
- **功能**: 收入/就诊/药材报表，趋势图表，日期范围筛选，导出
- **交互**: 标签页切换，图表交互，导出按钮，`IsLoading` 加载态与 `ErrorMessage` 错误态均已绑定
- **US**: US-REPORT-001~004
- **优先级**: P1

---

## 三、Sysadmin 角色页面

### 3.1 运维设置
- **设计稿**: `../../designs/sysadmin-home.pen` ✅
- **代码**: `SysadminHomeView.xaml` ✅ 已实现（`SuperAdminRoleDefinition.HomeViewName`）
- **功能**: 诊所信息/会话设置/连接设置/安全策略/功能开关/系统信息 + 备份恢复/远程部署/日志管理/安全审计/配置导入导出（独立页 `ConfigExportImportView`）/服务端配置；**读卡器诊断面板内嵌于本页**（`CardReaderDiagnosticsViewModel` 为内嵌子 VM，无独立 View）
- **交互**: 卡片网格布局，每个卡片有图标+操作按钮
- **US**: US-SHELL-018
- **优先级**: P1

### 3.2 数据库备份恢复
- **设计稿**: `../../designs/backup-management.pen` ✅
- **代码**: `BackupManagementView.xaml` ✅ 已实现（B-06：VM `BackupManagementViewModel` + 门面 `IBackupManagementService` → `IApiClient.Backup`）
- **功能**: 备份状态（上次备份/文件数/总大小/目录/保留期/自动备份说明）+ 手动全量/差异备份（可选压缩、加密+口令）+ 进度条（每秒轮询 `/api/v1/backup/status`）+ 文件列表（类型/时间/大小/保护/差异基准）+ 删除/清理过期 + 恢复工作流（整库或选择性表-记录、恢复前自动保护性备份、输入「确认恢复」+ 5 秒倒计时、恢复后提示重启应用）
- **双模式可达**: 侧栏「备份管理」（SuperAdmin，远程/本地均可用）与 SysadminHome 功能卡片「备份管理」；SysadminHomeView 内嵌「备份恢复」Tab 为**本地模式专用**（ADR-0014 本地全栈单面板，`IsLocalMode` 可见）
- **角色守卫**: `NavigationCoordinator.ViewRoleAccess[BackupManagement] = [SuperAdmin]`（B-06 收紧——原 Admin+SuperAdmin），对齐服务端 `SysAdminOnly`
- **US**: US-SHELL-013
- **优先级**: P1

### 3.3 远程部署
- **设计稿**: `../../designs/deployment.pen` ✅
- **代码**: `DeploymentView.xaml` ✅ 已实现
- **功能**: 远程服务器部署状态，版本信息，更新包上传与进度
- **US**: US-SHELL-020
- **优先级**: P1

### 3.4 日志级别控制
- **设计稿**: `../../designs/log-level.pen` ✅
- **代码**: `LogLevelControlView.xaml` ✅ 已实现
- **功能**: 当前日志级别展示，Debug 快捷开关（60 分钟），手动设置级别
- **优先级**: P1

### 3.5 安全审计日志
- **设计稿**: `../../designs/security-audit-log.pen` ✅
- **代码**: `SecurityAuditLogView.xaml` ✅ 已实现（2026-08-29）
- **功能**: 安全事件查询、分页、详情查看、空态与加载态
- **US**: US-SHELL-014
- **优先级**: P1

---

## 四、Doctor/临床角色页面

### 4.1 Doctor 首页（临床工作台）
- **设计稿**: `../../designs/clinical-workspace.pen` ✅
- **代码**: `ClinicalWorkspaceView.xaml` ✅ 已实现
- **说明**: **`DoctorRoleDefinition.HomeViewName = ClinicalWorkspaceView`**——即医生登录后的首页；左右分栏（左患者选择 + 右看诊工作区），内嵌 `PatientSelectionControl`
- **US**: US-SHELL-003
- **优先级**: P0

### 4.2 临床首页（fallback）
- **设计稿**: 无独立设计稿（复用 main-window 设计）
- **代码**: `ClinicalHomeView.xaml` ✅ 已实现
- **说明**: **非 Doctor 首页**；仅作为 `RoleRegistry` 未注册角色的 fallback 主页（`RoleRegistry.DefaultHomeView`）
- **功能**: 工作台概览，待诊患者数，今日收入，快捷操作（导航至 `ClinicalWorkspaceView`）
- **US**: US-SHELL-003
- **优先级**: 低（fallback）

### 4.3 医案工作台
- **设计稿**: `../../designs/medical-case.pen` ✅
- **代码**: `MedicalCaseWorkspaceView.xaml` ✅ 已实现（内嵌 `BaseDetailContainer` + `MedicalCaseViewControl` + `MedicalCaseEditControl`）
- **功能**: 患者信息卡片，诊断区域，处方编辑，操作面板，工作流步骤
- **交互**: 主从详情，步骤指示器，保存/挂起/完成/打印
- **US**: US-MC-001~020
- **优先级**: P0

### 4.4 患者管理
- **设计稿**: `../../designs/patient-list.pen` ✅
- **代码**: `PatientManagementView.xaml` ✅ 已实现（薄包装 → `PatientMasterDetailControl`）
- **功能**: 患者列表（搜索/筛选/分页），患者详情，创建/编辑患者
- **交互**: 主从详情布局，右侧详情面板
- **US**: US-PAT-001~014
- **优先级**: P0

### 4.5 患者选择
- **设计稿**: `../../designs/patient-selection.pen` ✅
- **代码**: `PatientSelectionView.xaml` ✅ 已实现（内嵌 `PatientSelectionControl` + `PatientInfoCardControl`；待诊队列内嵌于本页）
- **功能**: 患者列表 + 读卡器入口 + 待诊队列，选定患者后进入看诊
- **US**: US-REG-001/002/004/005
- **优先级**: P0

### 4.6 医案管理（列表）
- **设计稿**: `../../designs/medical-case-management.pen` ✅
- **代码**: `MedicalCaseManagementView.xaml` ✅ 已实现（薄包装 → `MedicalCaseMasterDetailControl`）
- **功能**: 医案列表主从管理（含状态徽标、编辑/查看）
- **US**: US-MC-001~020
- **优先级**: P1

### 4.7 医案主从详情
- **设计稿**: 无独立设计稿（并入 `medical-case-management.pen` 的主从面板）
- **代码**: `MedicalCaseMasterDetailView.xaml` ✅ 已实现（导航注册；内嵌 `MedicalCaseMasterDetailControl`）
- **优先级**: P1

### 4.8 医案审计日志
- **设计稿**: `../../designs/audit-log.pen` ✅
- **代码**: `AuditLogView.xaml` ✅ 已实现（医案域审计，`AuditLogViewModel`；区别于 Sysadmin 的安全审计 `SecurityAuditLogView`）
- **US**: US-MC-017
- **优先级**: P1

### 4.9 药材管理
- **设计稿**: `../../designs/herb-management.pen` ✅
- **代码**: `HerbManagementView.xaml` ✅ 已实现（薄包装 → `HerbMasterDetailControl`）
- **功能**: 药材列表，药材详情，创建/编辑药材
- **US**: US-HERB-001~014
- **优先级**: P1

### 4.10 验方管理
- **设计稿**: `../../designs/formula-management.pen` ✅
- **代码**: `FormulaManagementView.xaml` ✅ 已实现（薄包装 → `FormulaMasterDetailControl`）
- **功能**: 验方列表，验方详情，创建/编辑验方
- **US**: US-FORM-001~014
- **优先级**: P1

### 4.11 个人资料与安全设置
- **设计稿**: `../../designs/account-settings.pen` ✅
- **代码**: `AccountSettingsView.xaml` ✅ 已实现（薄包装 → `AccountSettingsControl`，`ShellViewMappings` 显式映射 → `AccountSettingsViewModel`；入口在顶栏 `HeaderControl`）
- **功能**: 个人资料编辑（姓名/手机/邮箱）、安全设置（修改密码）
- **优先级**: P1

---

## 五、Receptionist/前台角色页面

### 5.1 挂号管理
- **设计稿**: `../../designs/registration.pen` ✅
- **代码**: `RegistrationListView.xaml` ✅ 已实现（`RegistrationListViewModel`）
- **对话框**: `RegistrationCreateDialog.xaml` ✅ 已实现（**对话框**，非 `RegistrationCreateView`；设计稿 `../../designs/dialog-registration-create.pen`）
- **功能**: 挂号列表（搜索/筛选/分页），创建挂号对话框，挂号详情，待诊队列空态
- **交互**: 主从详情，创建对话框，患者选择
- **US**: US-REG-001~008
- **优先级**: P0

### 5.2 前台首页
- **设计稿**: `../../designs/receptionist-home.pen` ✅
- **代码**: `ReceptionistHomeView.xaml` ✅ 已实现（`ReceptionistRoleDefinition.HomeViewName`）
- **功能**: 前台工作台，挂号入口，排队查看（`Enter` 键触发患者搜索）
- **US**: US-SHELL-003
- **优先级**: P0

---

## 六、共享控件

> **控件清单权威来源**: [desktop-design-spec.md §8.1 共享控件规范](./desktop-design-spec.md)。
> 下表仅列实现状态。计数口径：Control 共 **33** 个（含共享设计系统控件 + 模块内嵌控件，见 §九）；下表列的是跨模块复用的共享控件与通用对话框。

| 控件 | 状态 |
|------|------|
| MasterDetailLayout | ✅ |
| BaseDetailContainer | ✅ |
| DataGridToolbar | ✅ |
| DetailToolbar | ✅ |
| SearchBox | ✅ |
| StatusBadge | ✅ |
| InfoCard | ✅ |
| EmptyState | ✅ |
| LoadingOverlay | ✅ |
| BreadcrumbBar | ✅ |
| UnifiedPaginationBar | ✅ |
| PatientInfoCardControl | ✅ |
| ToastControl | ✅ |
| WorkflowStepIndicator | ✅ |
| HerbListControl | ✅ |
| HerbItemControl | ✅ |
| FormulaViewControl | ✅ |
| PatientSelectionControl | ✅ |
| AccountSettingsControl | ✅ |
| ConfirmationDialog | ✅ |
| InputDialog | ✅ |
| MessageDialog | ✅ |

---

## 七、待完善/缺失

| # | 界面 | 状态 | 需要操作 |
|---|------|------|---------|
| 1 | `InitializationWizardView` | ✅ 已实现（2026-09-23 B-07） | 5 步初始化向导已交付（模式选择/模式配置+测试连接/诊所信息/初始管理员/校验完成） |
| 2 | `ReportsHomeView` | ⚠️ 仅 3/8 端点 | 需扩展趋势/绩效报表 |
| 3 | `SecurityAuditLogView` | ✅ 已实现 | 安全审计日志（2026-08-29） |
| 4 | 数据导入导出（`data-import-export.pen`） | ✅ 已实现（2026-09-23 收尾批次） | 独立 `ConfigExportImportView`（US-SHELL-016）已交付；业务数据 JSON 导入导出仍由各 MasterDetail 承载 |
| 5 | 读卡器诊断（`cardreader-diagnostics.pen`） | ✅ 已实现（内嵌） | 无独立 View，面板为 `SysadminHomeView` 内嵌区块（`CardReaderDiagnosticsViewModel`） |
| 6 | `ClinicalHomeView` | ⚠️ 非首页 | 仅 fallback；无独立设计稿 |
| 7 | `MedicalCaseMasterDetailView` | ⚠️ 缺设计稿 | 复用 `medical-case-management.pen` 主从面板 |
| 8 | `UnsavedChangesDialog` | ⚠️ 缺设计稿 | 对话框已实现，无对应 `.pen` |

---

## 八、设计稿中需修正的问题

| # | 问题 | 涉及文件 | 操作 |
|---|------|---------|------|
| 1 | 品牌名不统一（历史稿曾出现编造名「良医堂」） | `designs/*.pen` | 已核验：**无「良医堂」残留**；36 稿中 31 稿品牌为「凌隐宝堂」，`components.pen`/`dialog-common.pen`/`login.pen` 仍写作「中医诊所管理系统」→ 统一为**「凌隐宝堂中医诊所」** |
| 2 | 系统名称应为可配置占位符 | 相关设计稿 | 统一显示「凌隐宝堂中医诊所管理系统」，运行时按诊所配置替换 |
| 3 | 5 个对话框/表单稿无品牌区 | `dialog-common.pen`、`dialog-formula-import.pen`、`dialog-history-copy.pen`、`dialog-registration-create.pen`、`form-medical-case-edit.pen` | 无品牌区符合对话框形态；如加标题栏需与「凌隐宝堂中医诊所」一致 |
| 4 | 设计稿命名与实际承载者不一致（如需区分对话框与页面） | `dialog-*.pen` / `form-*.pen` | 对应 `*/Dialogs/**` 与 `*EditControl`；不要写成不存在的 `RegistrationCreateView` 等 |
| 5 | `data-import-export.pen` 无代码承载 | `data-import-export.pen` | 该界面为 `[未建视图]`：先补页面或将其能力并入 `SystemSettingsView`，再定稿 |

---

## 九、数据来源与口径

- **生成/校准日期**: 2026-09-13（机器扫描 `src/Client/Desktop`，排除 `bin/`、`obj/`）。
- **事实源**: 代码实际（`src/Client/Desktop`）。凡代码无法确认者，本文件标 `[待确认]`；代码中不存在的界面标 `[未建视图]`。

### 计数口径（全文唯一口径）

| 口径 | 数量 | 定义 |
|------|------|------|
| **View** | **30** | 页面/导航级 XAML：`*/Views/*.xaml`（含角色台 `Roles/*/Views/`、`Reports/Views/`、`Receptionist/Views/`，含 Shell 的 `MainWindow`/`AppShell`/`HeaderControl`/`SideNavControl`/`FooterControl`/`AccountSettingsView`） |
| **Control** | **33** | 内嵌组件：`*/Controls/*.xaml`（含共享设计系统控件与模块内嵌控件；`Shell/Controls/AccountSettingsControl` 亦归此类） |
| **Dialog** | **7** | `*/Dialogs/**/*.xaml`，经 `RegisterDialog` 注册（Shell 的 3 个位于 `Dialogs/Views/`） |
| **Root** | **1** | `Shell/App.xaml`（应用级资源，非视图） |
| **ViewModel** | **55** | `*ViewModel.cs` 文件数（每文件 1 个 VM 类型） |
| **XAML 合计** | **82** | = 视图 **71** + 资源/模板 **11**（`Core/LYBT.Desktop.Controls/Themes`、`.../Converters`、`Core/LYBT.Desktop.Printing/Templates`，不计入视图） |

> **路径口径例外**：`Auth/Views/ServerConfigView.xaml` 与 `Auth/Views/InitializationWizardView.xaml` 物理位于 `Views/`，但经 `RegisterDialog` 作为**对话框**注册（`InitializationWizardView` 另经 `RegisterForNavigation` 可导航）——按路径计为 View，按注册方式计为 Dialog；两类文档中均已注明。

### 其他校准口径

- **角色首页**（`RoleDefinition.HomeViewName`）：Admin = `AdminHomeView`、Doctor = `ClinicalWorkspaceView`、Receptionist = `ReceptionistHomeView`、SuperAdmin = `SysadminHomeView`；未注册角色 fallback = `ClinicalHomeView`（`RoleRegistry.DefaultHomeView`）。
- **侧栏职责**：`SideNavControl` 承载**角色导航矩阵**（`NavigationManager.BuildNavigationItems` 按角色生成：每角色 **3 项 = 标题「主页」+ 2 个业务入口**，`NavigationItem.Group` 取值为「临床」/「目录」/「管理」）+ **底部全局操作**（深色模式、退出）；**个人资料入口在顶栏** `HeaderControl`。历史文档「侧栏仅全局操作、不放功能导航」及「按主页/业务/管理分组」的表述与代码不符，已废弃。
- **内容区 Region**：唯一内容区常量 = `RegionNames.ContentRegion`（值为 `"ContentRegion"`），定义于 `Shell/Views/AppShell.xaml`；不存在 `MainContent` 区域。
- **Shell 绑定机制**：`MainWindow`/`HeaderControl`/`SideNavControl`/`FooterControl`/`AccountSettingsControl` 的 Prism 约定名不存在，统一经 `ShellViewMappings`（5 条显式映射，`App.ConfigureViewModelLocator` 调用）解析；侧栏状态 SSOT = `ISidebarStateManager`，主题 SSOT = `IThemeService`，登出唯一入口 = `IShellLogoutService`。
- **品牌**：**凌隐宝堂中医诊所**；系统名「凌隐宝堂中医诊所管理系统」。
- **逐项判定**（路径/VM/绑定/空态/加载态/错误态/键盘/状态徽标）见 [second-level-design-checklist.md](./second-level-design-checklist.md) §F。

---

## 十、代码实际界面索引（View 30 + Dialog 7）

> 口径同 §九；本索引用于消除「幽灵视图」——**下列之外的界面在代码中不存在**。

### Shell（`Shell/Views`，6 + 1 Root + 3 Dialog）

| 界面 | 路径 | 类型 / 注册 |
|------|------|------------|
| `MainWindow` | `Shell/Views/MainWindow.xaml` | View（宿主） |
| `AppShell` | `Shell/Views/AppShell.xaml` | View（纯组合，无 VM） |
| `HeaderControl` | `Shell/Views/HeaderControl.xaml` | View（Shell 控件） |
| `SideNavControl` | `Shell/Views/SideNavControl.xaml` | View（Shell 控件） |
| `FooterControl` | `Shell/Views/FooterControl.xaml` | View（Shell 控件） |
| `AccountSettingsView` | `Shell/Views/AccountSettingsView.xaml` | View（薄包装 → `AccountSettingsControl`） |
| `App`（Root） | `Shell/App.xaml` | 应用级资源（非视图） |
| `ConfirmationDialog` | `Shell/Dialogs/Views/ConfirmationDialog.xaml` | Dialog |
| `InputDialog` | `Shell/Dialogs/Views/InputDialog.xaml` | Dialog |
| `MessageDialog` | `Shell/Dialogs/Views/MessageDialog.xaml` | Dialog |

### Auth（`Modules/LYBT.Desktop.Auth`）

| 界面 | 路径 | 类型 / 注册 |
|------|------|------------|
| `LoginView` | `Modules/LYBT.Desktop.Auth/Views/LoginView.xaml` | View（导航） |
| `InitializationWizardView` | `Modules/LYBT.Desktop.Auth/Views/InitializationWizardView.xaml` | View（导航 + **Dialog 注册**，双入口） |
| `ServerConfigView` | `Modules/LYBT.Desktop.Auth/Views/ServerConfigView.xaml` | View（**Dialog 注册**） |

### MedicalCase / Registrations

| 界面 | 路径 | 类型 / 注册 |
|------|------|------------|
| `MedicalCaseMasterDetailView` | `Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseMasterDetailView.xaml` | View（导航） |
| `AuditLogView` | `Modules/LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml` | View（导航） |
| `ReportsHomeView` | `Modules/LYBT.Desktop.MedicalCase/Reports/Views/ReportsHomeView.xaml` | View（导航） |
| `RegistrationListView` | `Modules/LYBT.Desktop.Registrations/Views/RegistrationListView.xaml` | View（导航） |
| `FormulaImportDialog` | `Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialog.xaml` | Dialog |
| `HistoryCopyDialog` | `Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml` | Dialog |
| `UnsavedChangesDialog` | `Modules/LYBT.Desktop.MedicalCase/Dialogs/UnsavedChangesDialog.xaml` | Dialog |
| `RegistrationCreateDialog` | `Modules/LYBT.Desktop.Registrations/Dialogs/RegistrationCreateDialog.xaml` | Dialog |

### Admin / Sysadmin 角色台

| 界面 | 路径 | 类型 / 注册 |
|------|------|------------|
| `AdminHomeView` | `Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml` | View（导航） |
| `SystemSettingsView` | `Roles/LYBT.Desktop.Admin/Views/SystemSettingsView.xaml` | View（导航） |
| `UserManagementView` | `Roles/LYBT.Desktop.Admin/Views/UserManagementView.xaml` | View（导航，薄包装） |
| `SysadminHomeView` | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/SysadminHomeView.xaml` | View（导航） |
| `BackupManagementView` | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/BackupManagementView.xaml` | View（导航） |
| `DeploymentView` | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/DeploymentView.xaml` | View（导航） |
| `LogLevelControlView` | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/LogLevelControlView.xaml` | View（导航） |
| `SecurityAuditLogView` | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/SecurityAuditLogView.xaml` | View（导航） |

### Clinical / Receptionist 角色台

| 界面 | 路径 | 类型 / 注册 |
|------|------|------------|
| `ClinicalWorkspaceView` | `Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml` | View（导航，Doctor 首页） |
| `MedicalCaseWorkspaceView` | `Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml` | View（导航） |
| `ClinicalHomeView` | `Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml` | View（导航，fallback 首页） |
| `PatientSelectionView` | `Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml` | View（导航） |
| `PatientManagementView` | `Roles/LYBT.Desktop.Clinical/Views/PatientManagementView.xaml` | View（导航，薄包装） |
| `MedicalCaseManagementView` | `Roles/LYBT.Desktop.Clinical/Views/MedicalCaseManagementView.xaml` | View（导航，薄包装） |
| `HerbManagementView` | `Roles/LYBT.Desktop.Clinical/Views/HerbManagementView.xaml` | View（导航，薄包装） |
| `FormulaManagementView` | `Roles/LYBT.Desktop.Clinical/Views/FormulaManagementView.xaml` | View（导航，薄包装） |
| `ReceptionistHomeView` | `Roles/LYBT.Desktop.Clinical/Receptionist/Views/ReceptionistHomeView.xaml` | View（导航，Receptionist 首页） |

> 合计：View **30**（Shell 6 + Auth 3 + MedicalCase/Registrations 4 + Admin/Sysadmin 8 + Clinical/Receptionist 9）+ Dialog **7** = **37** 个界面；另有 Root **1**（`Shell/App.xaml`）。**不存在** `CardReaderDiagnosticsView`、`ServerConfigPanelView`、`UnfinishedCaseDialog`、`PrintPreviewDialog`、`PendingQueueView`、`RegistrationCreateView` 等视图（历史清单中的名称，均标注为 `[未建视图]`，其能力由上表真实承载者提供）。**注（2026-09-23 B-07）**：`InitializationWizardView` 已从「不存在」名单移除——该视图已真实落地（见 §1.3、Auth 索引表），旧单屏 `FirstRunSetupView` 已删除。**注（2026-09-23 收尾批次）**：`ConfigExportImportView`（`Admin/Sysadmin/Views/`，US-SHELL-016，见 §1.3 行 17 / §七 行 4）与 `SessionTimeoutWarningDialog`（`Shell/Dialogs/Views/`，US-AUTH-014）已真实落地，从「不存在」名单移除。
