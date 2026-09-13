# Desktop UI/UX 详细设计文档
> 版本: v2.1 | 日期: 2026-09-13 | 基于: R01→R22 22 轮独立调研 + R23 综合
> 模型: DeepSeek V4 Flash | 状态: 定版（数量与页面清单已对齐代码，2026-09-13）| 覆盖: View 30 / Control 33 / Dialog 7 / ViewModel 55（XAML 合计 82 = 视图 71 + 资源模板 11）+ 147 个 US

> **计数口径**（代码实际 `src/Client/Desktop`，排除 bin/obj）: **View 30** = 页面/导航级 `*/Views/*.xaml`（含角色台 `Roles/*/Views/`、`Reports/Views/`、`Receptionist/Views/`，以及 Shell 的 MainWindow/AppShell/HeaderControl/SideNavControl/FooterControl/AccountSettingsView）；**Control 33** = 内嵌组件 `*/Controls/*.xaml`（共享设计系统 16 + 模块内嵌 17，`Shell/Controls/AccountSettingsControl` 归此）；**Dialog 7** = `*/Dialogs/**/*.xaml`（经 `RegisterDialog` 注册，Shell 的 3 个位于 `Dialogs/Views/`）；**Root 1** = `Shell/App.xaml`（应用级资源，非视图）；**ViewModel 55** = VM 文件数（每文件 1 个 VM 类型）。注: `Auth/Views/ServerConfigView.xaml`、`Auth/Views/FirstRunSetupView.xaml` 物理位于 `Views/`（按路径计入 View），但经 `RegisterDialog` 作为**对话框**注册；`Shell/Controls/AccountSettingsControl.xaml` 位于 `Controls/`，但其宿主 `AccountSettingsView` 计入 View。

> **R23 说明**: 本文档为 R01-R22 的去重合并与优先级排序后的综合产出，代码 vs 需求 vs 设计已交叉验证，差异已标注于 §10 问题清单。

## 1. 设计系统概要

### 1.1 设计 Token（源 `TcmBrands.xaml`/`Spacing.xaml`/`Surfaces.xaml`）
- **主色**: 中医青 `#1A5C3A`（`TcmPrimaryBrush`，品牌）、辅金 `#C9A86A`、中性灰 `#F5F7FA` 背景、文本 `#1A1A1A`/`#6B7280`
- **字体**: 思源黑体 14px 正文、16px 标题、12px 辅助，行高 1.5
- **间距**: 8pt 基准 4/8/16/24/32（`Spacing.xaml:8`），禁止 10 等非 8 倍数
- **圆角**: 卡片 8、按钮 6、输入框 6
- **投影**: Elevation 0-5：Card `0 2 8 rgba(0,0,0,0.08)` 1 级、Dialog `0 8 24` 4 级、Toast `0 4 12` 5 级（高于 Dialog）
- **图标**: `Icons.xaml` 线性图标 20×20，主色或灰 600

### 1.2 布局模式定义
| 模式 | 用途 | 结构 | 示例页面 |
|------|------|------|----------|
| Master-Detail | 患者/药材/验方/用户/挂号 | 左 380 固定 + 右弹性详情/编辑 + 顶部 `DataGridToolbar` + 底部 `UnifiedPaginationBar` | `PatientMasterDetailControl` |
| Workspace | 医案工作台 | 顶患者卡 + 左右分栏（左辨证 45% 右处方 55%）+ 底部操作栏 | `MedicalCaseWorkspaceView` |
| Dashboard Grid | 各角色首页 | 2-4 列 `InfoCard` 网格 + 顶部统计 + 趋势区 | `ClinicalHomeView` |
| Form Dialog | 新增/编辑 | 居中 640×480，`BaseDetailContainer` 表单+底部 `Save/Cancel` | `PatientEditControl` |
| Split View | 挂号队列 | 左队列 `DataGrid` + 右 `PatientViewControl` | `PatientSelectionView`（内嵌 PendingQueue） |

### 1.3 共享控件清单（`LYBT.Desktop.Controls` 16 个）
> 口径：`Core/LYBT.Desktop.Controls/Controls/*` 下的 16 个内嵌组件（不含模块内嵌控件，见 §1.3.1）。属性列以 XAML 依赖属性（`DependencyProperty`）为准。

| 控件 | 文件 | 复用页面 | 关键属性 |
|------|------|----------|----------|
| MasterDetailLayout | `Controls/MasterDetailLayout.xaml` | 患者/药材/验方/医案/用户（Master-Detail 五处） | `MasterContent/DetailContent/EmptyContent/HeaderContent/HasSelection/MasterWidth(380)/DetailWidth` |
| DataGridToolbar | `Controls/DataGridToolbar.xaml` | 所有列表 | `CreateCommand/RefreshCommand/ExportCommand/BatchDeleteCommand/BatchEnableCommand/BatchDisableCommand/AdditionalContent`，统一顺序 `[搜索] → [新建][导入][导出] → [批量删除]` |
| UnifiedPaginationBar | `Controls/UnifiedPaginationBar.xaml` | 所有分页 | `TotalCount/TotalPages/CurrentPage/PageSize(默认 20)/PageSizes` + `First/Previous/Next/Last/PageSizeChanged` 命令，文案 `共 {n} 条` |
| SearchBox | `Controls/SearchBox.xaml` | 所有列表 | `SearchText/Placeholder/SearchCommand`（输入防抖由 VM 侧统一 500ms） |
| InfoCard | `Controls/InfoCard.xaml` | 各角色首页 | `Title/ShowTitle/Content`（数值与趋势经 `Content` 传入） |
| StatusBadge | `Controls/StatusBadge.xaml` | 所有状态列 | `Status/BadgeType/DisplayText`，`BadgeBackground/BadgeForeground` 可覆写 |
| BreadcrumbBar | `Controls/BreadcrumbBar.xaml` | 主界面/详情 | `NavigationPath` + `NavigateCommand`（完整路径可回溯） |
| EmptyState | `Controls/EmptyState.xaml` | 空列表/空报表 | `Icon/Title/Subtitle/ActionText/ActionCommand`，文案统一 `暂无{实体}` |
| LoadingOverlay | `Controls/LoadingOverlay.xaml` | 加载中 | `IsLoading/IsOverlayVisible/LoadingText`（文案必需） |
| ToastControl | `Controls/Toast/ToastControl.xaml` | 全局 | `Message` + `Show(message, ToastType, durationMs=3000)`，`ToastType = Info/Success/Warning/Error` |
| PatientInfoCardControl | `Controls/PatientInfoCardControl.xaml` | 医案工作台/患者选择 | `Patient/DisplayMode/ShowHistoryButton/HistoryCommand/ShowVisitCount` |
| HerbListControl | `Controls/HerbList/HerbListControl.xaml` | 处方/验方组成 | `HerbItems/AllHerbs/IsEditMode/Columns/DuplicateStrategy`，支持拖拽排序 |
| HerbItemControl | `Controls/HerbItem/HerbItemControl.xaml` | 处方/验方行 | `IsEditMode/AllHerbs/ItemIndex` |
| BaseDetailContainer | `Controls/BaseDetailContainer.xaml` | 详情/表单容器 | `Title/IsEditMode/ViewContent/EditContent/FooterContent/IsDirty/ShowEditButton/SaveButtonText/IsLoading/LoadingMessage` + `GoBack/SwitchToEdit/Save/Cancel/Print/Help` 命令 |
| DetailToolbar | `Controls/DetailToolbar.xaml` | 详情工具栏 | `Title/IsEditMode/EditCommand/SaveCommand/CancelCommand/DeleteCommand/ShowDeleteButton` |
| FormulaViewControl | `Controls/FormulaView/FormulaViewControl.xaml` | 验方详情 | `Formula/ShowSystemInfo` |

#### 1.3.1 模块内嵌控件（17 个，归属各业务模块，不计入共享表）
| 归属模块 | 控件（`*/Controls/*.xaml`） | 数量 | 宿主 |
|----------|--------------------------|------|------|
| Catalog | `FormulaEditControl`、`FormulaMasterDetailControl`、`HerbEditControl`、`HerbMasterDetailControl`、`HerbViewControl` | 5 | `FormulaManagementView`、`HerbManagementView`（薄包装 View 复用 Master-Detail 控件；编辑/预览控件由 Master-Detail 内部组合） |
| MedicalCase | `MedicalCaseEditControl`、`MedicalCaseMasterDetailControl`、`MedicalCaseViewControl`、`WorkflowStepIndicator` | 4 | `MedicalCaseManagementView`、`MedicalCaseMasterDetailView`、`MedicalCaseWorkspaceView`、`HistoryCopyDialog`（复用只读预览） |
| Patients | `PatientEditControl`、`PatientMasterDetailControl`、`PatientSelectionControl`、`PatientViewControl` | 4 | `PatientManagementView`、`PatientSelectionView`、`ClinicalWorkspaceView` |
| Users | `UserEditControl`、`UserMasterDetailControl`、`UserViewControl` | 3 | `UserManagementView` |
| Shell | `AccountSettingsControl` | 1 | `AccountSettingsView` |

> 合计 16（共享）+ 17（模块内嵌）= **33**（Control 口径，见封面）。`WorkflowStepIndicator` 属 MedicalCase 模块内嵌控件，不再计入共享表。

## 2. 角色-页面-权限矩阵

| 页面 | Sysadmin | Admin | Doctor | Receptionist | 权限源 |
|------|----------|-------|--------|--------------|--------|
| 登录 | ✅ | ✅ | ✅ | ✅ | AllowAnonymous |
| 首次运行向导 | ✅ | ❌ | ❌ | ❌ | 仅首次 `InitialSetupToken` |
| 服务器配置 | ✅ | ✅ | ✅ | ✅ | 登录前 |
| Sysadmin首页 | ✅ | ❌ | ❌ | ❌ | `SysAdminOnly` |
| Admin首页 | ❌ | ✅ | ❌ | ❌ | `AdminOrSuperAdmin` |
| 临床首页 | ❌ | ❌ | ✅ | ❌ | `Doctor` |
| 前台首页 | ❌ | ❌ | ❌ | ✅ | `Receptionist` |
| 用户管理 | ✅(仅Admin) | ✅(仅Doctor/Receptionist) | ❌ | ❌ | 分级 `Sysadmin→Admin→Doctor/Receptionist` |
| 患者管理 | ❌ | ✅ | ✅ | ✅ | 三角色 |
| 患者选择 | ❌ | ❌ | ✅ | ✅ | 挂号/就诊 |
| 药材管理 | ❌ | ✅ | 仅查看 | ❌ | `AdminOrSuperAdmin` 写 |
| 验方管理 | ❌ | ✅ | ✅(仅自己) | ❌ | `DoctorOrAdmin` |
| 医案管理 | ❌ | ✅(全部) | ✅(仅自己) | ❌ | 行级 `doctorIdFilter` |
| 医案工作台 | ❌ | ❌ | ✅ | ❌ | `DoctorOnly` + `IsLocked` |
| 待诊队列 | ❌ | ❌ | ✅ | ✅ | 共享，`doctor-{id}` 分组 |
| 挂号列表 | ❌ | ✅只读 | ✅仅自己 | ✅全部 | `ReceptionistOnly` 取消 |
| 报表首页 | ❌ | ✅ | ✅ | ❌ | `DoctorOrAdmin` + 行级 |
| 审计日志 | ❌ | ✅ | ✅仅自己 | ❌ | 医案审计 |
| 系统设置 | ❌ | ✅ | ❌ | ❌ | `AdminOrSuperAdmin` |
| 备份恢复 | ✅ | ❌ | ❌ | ❌ | `SysAdminOnly` 高危 |
| 日志级别 | ✅ | ❌ | ❌ | ❌ | `SysAdminOnly` |
| 部署 | ✅ | ❌ | ❌ | ❌ | `SysAdminOnly` |
| 安全审计日志 | ✅ | ❌ | ❌ | ❌ | `SysAdminOnly`（US-SHELL-014，本地/远程均有 `SecurityAuditController`） |
| 个人资料 | ✅ | ✅ | ✅ | ✅ | 登录后全角色，顶栏 `HeaderControl` 入口（`Ctrl+,`） |

> SSOT `04-permissions.md`，`12-permissions-matrix.md` 为视图；前端 `CanExecute` 与后端 `Policy` 同源 `IPermissionService`（R17）。
> 首页口径（代码实际）: Doctor 首页 = `ClinicalWorkspaceView`（`DoctorRoleDefinition.HomeViewName`），`ClinicalHomeView` 存在但非首页；Admin/Sysadmin/Receptionist 首页分别为 `AdminHomeView`/`SysadminHomeView`/`ReceptionistHomeView`。

## 3. 导航流程图

### 3.1 Sysadmin
```
Login → SysadminHomeView [Tab: 配置|服务端配置(仅远程)|备份恢复(仅本地)]
  ├─ Tab 配置 → ConfigCenter（诊所/会话/连接/安全/功能开关 5 节）+ CardReaderDiagnostics（读卡诊断）
  ├─ Tab 服务端配置 → ServerConfigSectionViewModel (6 节 API, IsRemoteMode 可见)
  ├─ Tab 备份恢复 → BackupManagementView (IsLocalMode 可见, 二次确认+倒计时)
  └─ 快捷入口 → UserManagementView | LogLevelControlView | DeploymentView | SecurityAuditLogView
```

### 3.2 Admin
```
Login → AdminHomeView (快捷卡片: 用户/药材/患者/验方/医案/系统设置/报表/审计日志)
  ├─ 用户管理 → UserManagementView (Master-Detail, 批量二次确认)
  ├─ 患者管理 → PatientManagementView
  ├─ 药材/验方 → HerbManagementView | FormulaManagementView
  ├─ 医案管理 → MedicalCaseManagementView
  ├─ 挂号列表(只读) → RegistrationListView
  ├─ 报表 → ReportsHomeView
  ├─ 审计日志 → AuditLogView
  └─ 系统设置 → SystemSettingsView (乐观锁)
```

### 3.3 Doctor
```
Login → ClinicalWorkspaceView (患者列表 + 看诊工作区一体化)
  ├─ 患者管理 → PatientManagementView → 选患者 → MedicalCaseWorkspaceView
  ├─ 医案管理 → MedicalCaseMasterDetailView → 选医案 → MedicalCaseWorkspaceView
  ├─ 药材(只读) → HerbManagementView
  ├─ 验方 → FormulaManagementView
  ├─ 挂号 → RegistrationListView
  └─ 报表 → ReportsHomeView
```

### 3.4 Receptionist
```
Login → ReceptionistHomeView (叫号横幅+挂号/患者快捷)
  ├─ 挂号列表 → RegistrationListView (新建/取消/ReceptionistOnly)
  ├─ 患者管理 → PatientManagementView (新建 + 读身份证主按钮)
  └─ 患者选择 → PatientSelectionView (SearchBox + 读卡 + 内嵌待诊队列)
```

> 导航经 `ViewNames` 强类型常量（`Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs`）+ `INavigationCoordinator.NavigateTo(viewName)` / `NavigateTo<TParams>(viewName, params)` 写入内容区 `RegionNames.ContentRegion`（常量值 `"ContentRegion"`，唯一宿主 `AppShell.xaml` 的 `ContentControl`）（R09），`KeepAlive` 列表页保持搜索/分页。

## 4. 页面详细规格（30 个 View，口径见封面）

> 本节按主题合并小节；全部 30 个 View 均在此登记（含 Shell 组件与薄包装 View），对话框与未建视图对照见 §4.22 / §4.23。

### 4.1 登录 (`LoginView.xaml`)
```
┌─────────────────────────────────────────────┐
│  ┌─────────────┐  ┌─────────────────────┐   │
│  │  左侧品牌    │  │  右侧表单           │   │
│  │  🌿 LOGO    │  │  用户名 [____]      │   │
│  │  诊所名(可绑)│  │  密码   [____]      │   │
│  │ 凌隐宝堂中医 │  │  ☑记住密码   ○自动登录│   │
│  │ 诊所管理系统 │  │  [      登录      ] │   │
│  └─────────────┘  │   ●在线/离线 [远程|本地] │   │
│  底部: 连接模式/API配置 | ⚙API配置(开 ServerConfigView)
└─────────────────────────────────────────────┘
```
| # | 控件 | 类型 | 位置 | 大小 | 说明 |
|---|------|------|------|------|------|
|1|Logo|Image|左侧居中|120|中医图标|
|2|系统名|TextBlock|Logo下|Auto|16pt 粗体 大标题 `ClinicName`（绑定配置，空回退 `凌隐宝堂中医诊所`）|
|3|副标题|TextBlock|系统名下|Auto|`凌隐宝堂中医诊所管理系统`（权威品牌名，见 §10 #58）|
|4|用户名|TextBox|右侧|320×40|必填 3-32，`TabIndex0`|
|5|密码|PasswordBox|用户名下|320×40|6-128，`TabIndex1`|
|6|记住密码|CheckBox|密码下左|Auto|`IsRememberMe`|
|7|自动登录|CheckBox|记住右|Auto|`IsAutoLogin`（两端对齐，Grid 两列）|
|8|登录|Button|表单底|320×48|主色 `IsDefault`，`Loading`禁用|
|9|连接切换|ToggleButton|状态栏左|Auto|`Remote/Local`|
|10|API状态|StatusBadge|状态栏|Auto|绿/红/黄|
|11|API配置|Button|状态栏右|Auto|⚙+API配置 文字，点击开 `ServerConfigView`（复用 `OpenSettingsCommand`）|
|12|状态栏|Border|底部|高度 48|统一 48，含切换/状态/API配置|
| 按钮 | 前置 | 逻辑 | 后置 |
|------|------|------|------|
|登录|用户名非空+密码非空|1.Loading 2.`POST /api/v1/auth/login` 3.成功→`ITokenStorage.Save`→导航主界面 4.失败→Toast|成功关登录开MainWindow；失败恢复按钮|
|记住|—|勾选→`ICredentialVault.Save`|无|
|切换|—|`SetModeAsync` + `CheckRemoteAvailableAsync` + `NotifyCanExecuteChanged`|更新文案|
| 状态流转 | 初始→输入中→提交中→成功(导航)/失败(红 Snackbar “用户名或密码错误”/`423 锁定`) |
| 异常 | 网络红 Snackbar“网络失败”；API 不可用 StatusBadge 红；锁定等 15min |

### 4.2 首次运行向导 (`FirstRunSetupView.xaml`)
5 步 `StepIndicator`：欢迎→系统检查→创建 SuperAdmin(`POST /auth/setup` + `InitialSetupToken`)→诊所信息→完成，`Next/Back` + 进度条，`TabIndex` 0-4，焦点首输入。

### 4.3 服务器配置 (`ServerConfigView.xaml`)
`API地址` 输入 + `TestConnection` + `Save` + `ConnectionStatusViewModel` 卡（`RemoteUrl` 自动探测 `UrlChanged→CheckRemoteAvailableAsync`）；`Save` 成功 `InfoCard` “✓ 已保存 12:34”。

### 4.4 主界面 (`MainWindow.xaml`)
```
┌──────────────────────────────────────────────────┐
│ 顶部: Logo | 诊所名 | 弹性 | 👤头像+姓名+角色      │
│ (HeaderControl 48px)                              │
├────────┬─────────────────────────────────────────┤
│ 侧栏   │  面包屑: 首页 › 当前页 (28px)            │
│ 240/64 │  ─────────────────────────────────────── │
│ 功能   │  内容区 Region ContentRegion             │
│ 导航   │  卡片网格/列表 + 分页                     │
│ 分组   │                                          │
│ 临床   │                                          │
│ 目录   │                                          │
│ 管理   │  状态栏 (FooterControl 32px)             │
└────────┴─────────────────────────────────────────┘
```
- 结构: `MainWindow` 内 `LoginRegion`（未登录）与 `AppShell`（已登录）按 `IsLoggedIn` 切换；`AppShell` = Header(48) + [SideNav(240/64) + 右列(面包屑 28 + ContentRegion + Footer 32)]
- 侧边栏 `SideNavControl`: `ListBox` 绑定 `GroupedNavigationItems`（`NavigationManager.BuildNavigationItems` 按角色生成固定 3 项 = `主页` + 2 业务入口，`Group` 标签取「临床/目录/管理」），展开/收拢由 `ISidebarStateManager` 状态 SSOT 驱动；底部为**全局操作**（深色模式开关 → `IThemeService`、`退出` → `IShellLogoutService`）
- 面包屑在 `AppShell` 内由 `SelectedNavItem.Title` 渲染（`首页 › 当前页`），两级为主
- 个人资料入口在**顶栏** `HeaderControl` 的 `EditProfileCommand`（`Ctrl+,`），不在侧栏
- `InputBindings`（`MainWindow.xaml`）: `Ctrl+N` 新建患者、`Ctrl+Shift+C` 开始医案、`F1` 帮助、`Ctrl+,` 设置、`Ctrl+M` 折叠侧栏、`Alt+←/→` 后退/前进、`Alt+Home` 回首页

### 4.4.1 Shell 组件（4 个，属 View 口径）
| View | 文件 | ViewModel | 绑定方式 | 要点 |
|------|------|-----------|----------|------|
| `AppShell` | `Shell/Views/AppShell.xaml` | 无独立 VM（宿主 `MainWindowViewModel` 代理） | 构造装配 | `ContentRegion` 唯一宿主；侧栏宽度绑定宿主 `SidebarWidth`（代理 `ISidebarStateManager`，不再跨元素取 DataContext） |
| `HeaderControl` | `Shell/Views/HeaderControl.xaml` | `HeaderViewModel` | `AutoWire=True` + `ShellViewMappings` | 品牌块「医」+ 诊所名；右侧用户区 = 分隔线 + `AccountCircle` + 姓名 + 角色，点击 `EditProfileCommand` |
| `SideNavControl` | `Shell/Views/SideNavControl.xaml` | `SideNavViewModel` | `AutoWire=True` + `ShellViewMappings` | 汉堡 `IsSidebarExpanded`；`GroupedNavigationItems`（角色导航矩阵）；底部深色模式 `IsDarkMode` + `退出` `LogoutCommand` |
| `FooterControl` | `Shell/Views/FooterControl.xaml` | `FooterViewModel` | `AutoWire=True` + `ShellViewMappings` | `ApiStatusText` / `ConnectionModeDisplay` / `CurrentTimeDisplay`（只读状态栏） |

> **绑定机制（2026-09-13 定案）**: `Header/SideNav/Footer` 三控件 `AutoWireViewModel="True"` 但 Prism 约定名（`HeaderControlViewModel` 等）**不存在**，约定解析会静默失败并继承宿主 DataContext → 由 `Shell/ShellViewMappings.cs` 提供 **5 条显式映射**（`MainWindow`、`AccountSettingsControl`、`HeaderControl`、`SideNavControl`、`FooterControl`），经 `App.ConfigureViewModelLocator` 的 `ShellViewMappings.Register()` 注册，守卫测试 `ShellViewViewModelBindingTests` 与本表同源校验。SSOT: 侧栏状态 = `ISidebarStateManager`、主题 = `IThemeService`、登出（活跃医案守卫）= `IShellLogoutService`。

### 4.5 患者管理 (`PatientManagementView` + `PatientMasterDetailControl`)
- **布局**: `MasterDetailLayout` 左 380 列表 + 右 `PatientViewControl`/`PatientEditControl` + 顶部 `DataGridToolbar` 统一顺序 `[搜索]|[新建][导入][导出]|[批量删除]` + 底部 `UnifiedPaginationBar` `共 {n} 条`
- **列表列**: 姓名/性别/年龄/电话/`StatusBadge` + `RowStyle` 虚拟化 `EnableRowVirtualization`
- **按钮**: 新建 `POST /patients` 201，编辑 `PUT /patients/{id}`，删除 `DELETE` 软删 `AdminOrSuperAdmin` 二次确认，恢复 `POST /restore`，批量删除 `POST /batch-delete` Max100，导出 `GET /export` JSON + Excel
- **校验**: 姓名 2-50 必填，电话 11 位 `ExistsByPhoneAsync` 防抖 500ms，身份证 18 位唯一过滤索引 `IsDeleted=0`，`Validation.ErrorTemplate` 仅红框+Tooltip
- **空/错**: `EmptyState` “暂无患者，去创建” + 按钮；`Error` 红图标“加载失败 重试”

### 4.6 患者选择 (`PatientSelectionView`)
`SearchBox` + `PatientSelectionControl` 列表 + 右 `读身份证` 主按钮常驻（图标+文字），`CardReader` 状态灯；读卡 `ICardReader.ReadCard → FindPatientByIdNumberAsync` 未找到 `QuickCreatePatientAsync` 自动填充。

### 4.7 药材管理 (`HerbManagementView`)
`MasterDetail` `HerbViewControl`/`HerbEditControl`，字段 名称/拼音/分类/性味/产地/规格/单位/单价/成本/功效/用法，`IsShared`；按钮 新建/编辑/删除/启用/禁用/批量启用/禁用/批量删除/导入/导出（写 `AdminOrSuperAdmin`，读 `DoctorOrAdmin`），`CheckReference` 引用检查。

### 4.8 验方管理 (`FormulaManagementView`)
`MasterDetail` `FormulaViewControl` 含药材组成表格 `HerbName/Dosage/Unit`；工具栏 新建/编辑/复制/模板/导入/导出 + **`待校验`**（✅ 2026-09-09 B-13：切换 Draft 待办列表——`GET pending-validation` 分页 + 列表「校验」列 待校验/已验证）；待校验模式详情为**校验面板**——每行未绑定药材 ComboBox 选系统药材 + 校验绑定（`POST /formulas/{id}/herbs/{itemId}/validate`，全部绑定自动晋升 Validated）；导入走 JSON 模板 + `POST batch-import`（模板下载 2026-08-13 起为 JSON）。

### 4.9 医案管理 (`MedicalCaseMasterDetailView`)
`MasterDetail` `MedicalCaseViewControl` 只读 + `StatusBadge` Draft/Active/Completed/Locked/Suspended；列表 患者/医生/状态/创建时间/是否打印；按钮 新建 `DoctorOnly` /查看/编辑/完成/关闭/挂起/取消/打印/审计。

### 4.10 医案工作台 (`MedicalCaseWorkspaceView` 187 + ViewModel 558 — 核心)
```
┌─────────────────────────────────────────────┐
│ 患者信息卡 PatientInfoCardControl            │
├──────────────┬──────────────────────────────┤
│ 左 45% 辨证  │ 右 55% 处方 (常驻, 非 Tab)    │
│ 主诉/现病史  │ HerbListControl + 总价/折扣   │
│ 舌诊/脉诊/诊 │ 医嘱 + 拖拽排序               │
│ 断            │                              │
├──────────────┴──────────────────────────────┤
│ 底部栏: [保存] [完成] [打印] [取消] + Step 1-5│
│ EditReason 折叠区 (Completeness.RequiresEditReason 时展开红*)│
└─────────────────────────────────────────────┘
```
- **ViewModel**: 薄壳 `ConsultationEditor/PrescriptionEditor/Commands` + `EditModeStateMachine` + `WorkspaceStateManager` + `WorkspaceNavigationHandler`
- **处方**: `PrescriptionItemViewModel` 集合，`HerbSearchProvider` 填充单价，`AllHerbs` + 库存 `StatusBadge`（预留）
- **保存**: `MedicalCaseInputDto → POST/PUT /medicalcases` 经 `MedicalCaseStateGuard.EnsureCanEdit`（`IsLocked/EditReason/IsPrinted/异人`）`
- **完成**: `CompleteAsync` → `Completed` + 挂号联动 `Completed` + `CanComplete` 校验
- **打印**: `IPrintService.PreviewAsync → PrescriptionPrintTemplate` FixedDocument
- **交互**: 行单击选中/双击编辑统一，`Ctrl+S` 保存，拖拽排序 `GongSolutions.WPF.DragDrop`，`EditReason` 条件展开

### 4.11 ~~待诊队列 (`PendingQueueView`)~~ → 已合并
> XAML 2026-08-29 删除。功能已嵌入 `PatientSelectionView`（`PendingQueueViewModel` 作为子组件）。参见 §4.6 PatientSelectionView。

### 4.12 挂号列表 (`RegistrationListView`)
`MasterDetail` + `RegistrationCreateDialog` 新建；字段 患者/医生/队列号/状态/费用/来源；按钮 新建 `Receptionist/Doctor`、取消 `ReceptionistOnly` 二次确认、开始就诊 `DoctorOnly start-visit` 原子建档 `MedicalCaseId`；单患者单 `Waiting/InProgress` 过滤唯一索引 `UX_Registrations_PatientId_Pending`；状态机 `Waiting→InProgress→Completed/Cancelled`。

### 4.13 角色首页（临床首页/临床工作台/前台首页）
- `ClinicalHomeView`（`Roles/LYBT.Desktop.Clinical/Views/`，VM `ClinicalHomeViewModel`，`RegisterForNavigation` 于 `ClinicalModule`）: 待诊大卡置顶红点 + 今日接诊/患者总数小卡 + 趋势区懒加载；入口命令 `StartMedicalCaseCommand` 与 `NavigateToPatientManagement / RegistrationQueue / MedicalCaseQuery / HerbLibrary / FormulaLibrary / Reports / AuditLog`
- `ClinicalWorkspaceView`（**Doctor 首页**，`DoctorRoleDefinition.HomeViewName => ViewNames.ClinicalWorkspace`）: 左 `PatientSelectionControl` + 右 `PatientHistory` 列表，命令 `StartConsultationCommand`（开始看诊）、`NewPatientCommand`；患者双击经 code-behind `PatientSelectionControl_PatientDoubleClicked` 进入医案工作台
- `ReceptionistHomeView`（VM `ReceptionistHomeViewModel`，`Roles/LYBT.Desktop.Clinical/Receptionist/Views/`）: 叫号横幅“当前叫号 张三” + 患者搜索（`KeyBinding Key=Return → SearchPatientCommand`）+ `CreateNewRegistrationCommand` / `CreateNewPatientCommand` + 今日挂号统计 + 快捷入口 `NavigateToPatientManagement / RegistrationQueue / CardReader`

> ⚠️ **口径校正**: 早期清单写「Doctor 首页 = `ClinicalHomeView`」，代码实际为 **`ClinicalWorkspaceView`**（`RoleRegistry` 角色定义）；`ClinicalHomeView` 仍存在且可导航，但非 Doctor 首页。

### 4.14 报表首页 (`ReportsHomeView`)
`DateRangePicker` + `Granularity` 日/周/月 + 4 趋势图（收入/接诊/药材排行/绩效）+ `ReportTimeBuckets.Build` 分桶；`LoadingOverlay Skeleton` + `EmptyState` “暂无数据 选其他时间” +  `ExportExcel`；`doctorIdFilter` 行级二次校验。

### 4.15 审计日志 (`AuditLogView`)
表格 操作人/时间/操作类型/字段差异 `ChangedFields/OldValues/NewValues` + `ReportRepository` 聚合；`GET /medicalcases/{id}/audit-logs|history`。

### 4.16 管理员首页/系统设置/用户管理
- `AdminHomeView`（`Roles/LYBT.Desktop.Admin/Views/`，VM `AdminHomeViewModel`）: 仪表盘卡片 + 8 个快捷入口（`NavigateToUserManagement / HerbManagement / PatientManagement / FormulaManagement / MedicalCaseManagement / SystemSettings / Reports / AuditLog`）
- `SystemSettingsView`（VM `SystemSettingsViewModel`）: 诊所名称/地址/电话（`SaveCommand` / `ResetCommand`，`ClinicSettingsService` 热更新 + `RowVersion` 乐观锁“已被他人修改”）；另含备份路径选择（`BrowseBackupPathCommand`）与服务端配置区（`LoadServerConfigCommand` / `ValidateConfigCommand` / `SaveServerConfigCommand`）
- `UserManagementView`（薄包装 View，无独立 VM）: 内嵌 `UserMasterDetailControl`（`UsersModule` 经 `ViewModelLocationProvider.Register` 显式映射到 `UserMasterDetailViewModel`，因约定名不匹配）；Master-Detail，用户 CRUD 分级 `Sysadmin→Admin→Doctor/Receptionist` + 禁用/重置密码 `SysAdminOnly` + 批量二次确认 + 密码确认

### 4.17 运维首页/备份/部署/日志/安全审计
- `SysadminHomeView`（VM `SysadminHomeViewModel`）: 3 个 Tab —— `配置`（`ConfigCenter`：诊所/会话/连接/安全/功能开关 5 节 + `RestartLocalServiceCommand`；并内嵌 `CardReaderDiagnostics` 读卡诊断 `RunDiagnosticsCommand` / `SaveSettingsCommand` / `ReportLines`）、`服务端配置`（`IsRemoteMode` 可见，`ServerConfig` 节列表 `SaveSectionCommand` / `RestartServerCommand`）、`备份恢复`（`IsLocalMode` 可见，内嵌 `<views:BackupManagementView/>`）；并附 4 个快捷入口按钮（用户管理/日志级别/部署/安全审计）
  - 三个**子 VM 无独立 XAML**（均为 `SysadminHomeView` 内嵌子 VM）: `ConfigurationCenterViewModel`、`ServerConfigSectionViewModel`、`CardReaderDiagnosticsViewModel` —— 对应早期清单中的 `ConfigExportImportView`(SY-07)、`ServerConfigPanelView`(SY-08)、`CardReaderDiagnosticsView`(SY-05) 三个**未建视图**（见 §4.23）
- `BackupManagementView`: 上次备份/文件数/总大小 + 手动备份 `ProgressBar` + 文件列表 `DataGrid` + 恢复 `POST /backup/restore` 红警告+输入“确认恢复”+5s倒计时
- `DeploymentView`: 上传 `nupkg` `ProgressBar` + 类型校验 + `RELEASES` 预览 + 重启 `POST /deploy/restart`
- `LogLevelControlView`: `LoggingLevelManager` 运行时 `ComboBox`，切 `Debug` 警告“30分钟后回退” + 定时器
- `SecurityAuditLogView`: 见 §4.19
- 未建视图（`CardReaderDiagnosticsView` / `ConfigExportImportView` / `ServerConfigPanelView`）对照见 §4.22

### 4.18 医案管理列表 (`Roles/LYBT.Desktop.Clinical/Views/MedicalCaseManagementView.xaml`)
- **路径/VM**: 薄包装 View（无独立 VM），设计期 `d:DataContext = MedicalCaseMasterDetailViewModel`；运行时由内嵌控件解析
- **布局**: 单元素 `<medicalCaseControls:MedicalCaseMasterDetailControl />`（约 23 行），即 Master-Detail = 左医案列表 + 右 `MedicalCaseViewControl`/`MedicalCaseEditControl` + `DetailToolbar`
- **交互/命令**: 完全复用 MedicalCase 模块控件命令（新建 `DoctorOnly`、查看/编辑/完成/关闭/挂起/取消/打印/审计，详见 §4.9）
- **导航**: `ViewNames.MedicalCaseManagement`，`ClinicalModule.RegisterForNavigation`；Admin 首页快捷入口 `NavigateToMedicalCaseManagementCommand`
- **API**: 同 §4.9 与 §8 医案端点（`/api/v1/medicalcases*`）
- **与 §4.9 的区别**: `MedicalCaseMasterDetailView`（模块 `Views/`）与 `MedicalCaseManagementView`（角色台 `Roles/Clinical/Views/`）都是同一控件的薄包装，前者供模块导航、后者供角色台导航

### 4.19 安全审计日志 (`Roles/LYBT.Desktop.Admin/Sysadmin/Views/SecurityAuditLogView.xaml`)
- **VM**: `SecurityAuditLogViewModel`（`AutoWireViewModel="True"`）
- **布局**: 顶部 `GoBackCommand` + `PageTitle` + 过滤区（`FilterEventType` / `FilterUserName` 输入 + `SearchCommand` / `ClearFiltersCommand`）→ `DataGrid`（时间/用户/事件类型/IP/结果/失败原因）→ 选中行详情（`SelectedLog.Details`）→ 底部自建分页（`PreviousPageCommand` / `NextPageCommand`，`第 {n} 页 / 共 {m} 页 (共 {k} 条)`）
- **状态**: 空态文案 `EmptyMessage`；加载中“加载中...”；结果列以 Badge 呈现成功/失败
- **数据**: `ISecurityAuditQueryService.GetLogsAsync` → `GET /api/v1/security-audit`（`page/pageSize/eventType/userName/from/to`，见 §8）；本地/远程数据源由 `IConnectionModeService` 决定；需求 US-SHELL-014（**仅 SuperAdmin**）
- **导航**: `ViewNames.SecurityAuditLog` + `SysadminModule.RegisterForNavigation`；唯一入口为 `SysadminHomeView` 快捷入口 `NavigateToSecurityAuditLogCommand`
- **与 §4.15 的区别**: `AuditLogView` 是**医案审计**（MedicalCase 模块，`GET /medicalcases/{id}/audit-logs|history`）；本页是**安全事件审计**（登录/权限等安全事件），二者不同源

### 4.20 个人资料 (`Shell/Views/AccountSettingsView.xaml` + `Shell/Controls/AccountSettingsControl.xaml`)
- **路径/VM**: `AccountSettingsView` 为 10 行薄包装（`<controls:AccountSettingsControl/>`，无 `AutoWireViewModel`）；VM `AccountSettingsViewModel` 在 `App.xaml.cs` **显式** `Register` + `RegisterForNavigation<Views.AccountSettingsView>()`
- **布局**: 头部头像（`CurrentUser.RealName` 首字）+ 姓名 + 角色 → 编辑区（姓名 `EditRealName` / 手机号 `EditPhoneNumber` / 邮箱 `EditEmail`）→ 只读区（用户名/角色/注册时间/最后登录）
- **交互/命令**: `SaveProfileCommand`（`CanSaveProfile` 门控）、`ChangePasswordCommand`、`GoBackCommand`
- **入口**: 顶栏 `HeaderControl` 用户区 `EditProfileCommand`（提示 `个人资料 (Ctrl+,)`）；模态承载于 `MainWindow` 的 `materialDesign:DialogHost Identifier="RootDialog"`
- **API**: `[待确认]`（资料保存/改密端点未在文档既有 §8 中登记）

### 4.21 对话框清单（Dialog 7 个）
| # | Dialog | 文件 | ViewModel | 注册位置 |
|---|--------|------|-----------|----------|
|1| `RegistrationCreateDialog` | `Modules/LYBT.Desktop.Registrations/Dialogs/` | `RegistrationCreateDialogViewModel` | `RegistrationModule.RegisterDialog` |
|2| `FormulaImportDialog` | `Modules/LYBT.Desktop.MedicalCase/Dialogs/` | `FormulaImportDialogViewModel` | `MedicalCaseModule.RegisterDialog` |
|3| `HistoryCopyDialog` | `Modules/LYBT.Desktop.MedicalCase/Dialogs/` | `HistoryCopyDialogViewModel` | `MedicalCaseModule.RegisterDialog`（内部复用 `MedicalCaseViewControl` 只读预览） |
|4| `UnsavedChangesDialog` | `Modules/LYBT.Desktop.MedicalCase/Dialogs/` | `UnsavedChangesDialogViewModel` | `MedicalCaseModule.RegisterDialog` |
|5| `ConfirmationDialog` | `Shell/Dialogs/Views/` | `ConfirmationDialogViewModel` | `App.RegisterTypes` |
|6| `MessageDialog` | `Shell/Dialogs/Views/` | `MessageDialogViewModel` | `App.RegisterTypes` |
|7| `InputDialog` | `Shell/Dialogs/Views/` | `InputDialogViewModel` | `App.RegisterTypes` |

> 口径提示: `Auth/Views/ServerConfigView.xaml` 与 `Auth/Views/FirstRunSetupView.xaml` 经 `AuthenticationModule.RegisterDialog` 注册为**对话框**，但物理位于 `Views/`，按封面口径计入 **View（+2）**、不计入 Dialog 7。

### 4.22 未建视图对照表（引用真实承载者）
| 清单代码 | 早期视图名 | 状态 | 真实承载者 |
|----------|-----------|------|-----------|
| W-01 | `InitializationWizardView` | `[未建视图]` | 首次运行承载者 = `FirstRunSetupView`（5 步向导，`AuthenticationModule.RegisterDialog`，需求 US-SHELL-011） |
| SY-05 | `CardReaderDiagnosticsView` | `[未建视图]` | `SysadminHomeView`「配置」Tab 内嵌子 VM `CardReaderDiagnosticsViewModel`（`RunDiagnosticsCommand` / `SaveSettingsCommand` / `ReportLines`，无独立 XAML） |
| SY-07 | `ConfigExportImportView` | `[未建视图]` | `SysadminHomeView` 内嵌子 VM `ConfigurationCenterViewModel`（配置导入导出为其能力，非独立页面） |
| SY-08 | `ServerConfigPanelView` | `[未建视图]` | `SysadminHomeView`「服务端配置」Tab → 子 VM `ServerConfigSectionViewModel`（仅远程模式可见） |
| AD-01 | `SessionTimeoutWarningDialog` | `[未建视图]` | `[待确认]` —— 现存 7 个 Dialog 中无会话超时提示实现 |
| DD-04 | `UnfinishedCaseDialog` | `[未建视图]` | 未保存离开确认由 `UnsavedChangesDialog`（MedicalCase 模块）承载 |
| DD-05 | `PrintPreviewDialog` | `[未建视图]` | 打印预览经 `IPrintService.PreviewAsync` → `PrescriptionPrintTemplate` FixedDocument（§4.10），非独立对话框 |
| — | `RegistrationCreateView` | `[未建视图]` | 实际为对话框 `RegistrationCreateDialog`（§4.21） |
| DOC-02 | `PendingQueueView` | 已删除（2026-08-29） | 功能并入 `PatientSelectionView`（`PendingQueueViewModel` 作子组件），见 §4.11 |

### 4.23 View 覆盖核对表（30 个，与封面口径同源）
| # | View | 所在小节 | # | View | 所在小节 |
|---|------|---------|---|------|---------|
|1| `LoginView` | §4.1 |16| `SysadminHomeView` | §4.17 |
|2| `FirstRunSetupView` | §4.2 |17| `BackupManagementView` | §4.17 |
|3| `ServerConfigView` | §4.3 |18| `DeploymentView` | §4.17 |
|4| `MainWindow` | §4.4 |19| `LogLevelControlView` | §4.17 |
|5| `AppShell` | §4.4.1 |20| `SecurityAuditLogView` | §4.19 |
|6| `HeaderControl` | §4.4.1 |21| `MedicalCaseManagementView` | §4.18 |
|7| `SideNavControl` | §4.4.1 |22| `MedicalCaseMasterDetailView` | §4.9 |
|8| `FooterControl` | §4.4.1 |23| `MedicalCaseWorkspaceView` | §4.10 |
|9| `AccountSettingsView` | §4.20 |24| `RegistrationListView` | §4.12 |
|10| `PatientManagementView` | §4.5 |25| `ReportsHomeView` | §4.14 |
|11| `PatientSelectionView` | §4.6 |26| `AuditLogView` | §4.15 |
|12| `HerbManagementView` | §4.7 |27| `ClinicalHomeView` | §4.13 |
|13| `FormulaManagementView` | §4.8 |28| `ClinicalWorkspaceView` | §4.13 |
|14| `AdminHomeView` | §4.16 |29| `ReceptionistHomeView` | §4.13 |
|15| `SystemSettingsView` | §4.16 |30| `UserManagementView` | §4.16 |

> 合计 **30**（= 封面 View 口径）。`PendingQueueView` 已于 2026-08-29 删除、不计入。

## 5. 跨页数据流
| 数据 | 来源 | 目标 | 方式 | 备注 |
|------|------|------|------|------|
| PatientId | 患者列表 | 医案工作台 | `NavigationParameters["PatientId"]` 强类型 `MedicalCaseNavigationParameters` | `INavigationCoordinator` |
| RegistrationId | 挂号列表 | 医案工作台 | 同上 | `start-visit` 返回 `MedicalCaseId` |
| MedicalCaseId | 医案列表 | 工作台/审计 | 同上 + `WorkspaceState` 持久化 `LastPatientId` | 异常恢复回填 |
| HerbId | 药材列表 | 验方/处方 | `IHerbSearchProvider` 委托 `ICatalogQueryService` | `AllHerbs` 55% 常驻 |
| FormulaId | 验方列表 | 医案 FormulaImportDialog | `Dialog` 参数 | 克隆/导入 |
| UserId | 登录 | 全局 | `ICurrentUserProvider` + `ClaimsPrincipal` | `NameIdentifier` |

## 6. 状态机定义
| 实体 | 状态 | 转换 | 守卫 |
|------|------|------|------|
| Registration | Waiting→InProgress→Completed | `StartVisit` 仅 Waiting→InProgress 原子；`Complete` 仅 InProgress→Completed | `RegistrationStateGuard.EnsureCanCancel` + 唯一索引 `Status IN (0,1)` |
| Registration | Waiting→Cancelled | `Cancel` 需 `MedicalCaseId==null` 且 `CaseStatus!=Completed` | 同上，前端 `ReceptionistOnly` |
| MedicalCase | Draft→Active→Completed→Locked | `Save` Draft→Active；`Complete` Active→Completed；隔天 `IsLocked` | `MedicalCaseStateGuard.EnsureCanEdit/EnsureNotLocked` + `IMedicalCaseTimeService.IsLocked` |
| MedicalCase | Completed→Completed(编辑) | 需 `EditReason` | 同上，`RequiresEditReason` 展开 |
| Patient | Enabled↔Disabled | `ToggleStatus/Restore` | `AdminOrSuperAdmin` |
| Herb/Formula | Enabled↔Disabled | `ToggleStatus/BatchEnable/Disable` | `AdminOrSuperAdmin` |
| User | Enabled↔Disabled | `ToggleStatus` + 撤销 Token 族 | `AdminOrSuperAdmin` + `RevokeAllUserTokens` |

## 7. 验证规则汇总
| 字段 | 必填 | 格式 | 范围 | 唯一性 | 备注 |
|------|------|------|------|--------|------|
| PatientName | ✅ | 中文/英文 | 2-50 | ❌ | 失焦 + 提交双校验文案统一 `ValidationMessages` |
| Gender | ✅ | 枚举 | Unknown/Male/Female | — | — |
| PhoneNumber | ❌ | 11 位数字 | 7-20 | 查重 `ExistsByPhoneAsync` 防抖 500ms | 加密 `AesGcm` 列 200 |
| IdNumber | ❌ | 18 位 | 15-50 | 唯一过滤索引 `IsDeleted=0` 内存查询 | 加密，列 200 |
| BirthDate | ❌ | Date | <Today | — | 计算 Age |
| HerbName | ✅ | 文本 | 2-50 | 唯一 | — |
| Price | ❌ | decimal | 0-9999.99 | — | `decimal(10,2)` |
| FormulaName | ✅ | 文本 | 2-100 | 同名+组成查重 | — |
| Dosage | ✅ | decimal | >0 | — | `FormulaHerbItem` |
| Registration PatientName | ✅ | 文本 | 2-100 | — | `RegistrationInputDtoValidator` |
| RegistrationFee | ❌ | decimal | ≥0 | — | `UserBasicDto.RegistrationFee` 带出 |
| UserName | ✅创建 | 3-32 正则 | — | 唯一 | `UserInputDtoValidator` |
| Password | ✅创建 | 6-128 | — | — | `ConfirmPassword` 一致 |

## 8. API 端点映射
| 页面操作 | API | 方法 | 请求体 | 响应 | 权限 |
|---------|-----|------|--------|------|------|
| 登录 | /api/v1/auth/login | POST | LoginRequest | LoginResponse+Token | AllowAnonymous |
| 刷新 | /api/v1/auth/refresh | POST | RefreshTokenRequest | LoginResponse | AllowAnonymous |
| 患者列表 | /api/v1/patients | GET | ?page&pageSize&keyword&role&status | PagedResult | DoctorOrAdminOrReceptionist |
| 创建患者 | /api/v1/patients | POST | PatientInputDto | PatientDetailDto 201 | 同上 |
| 更新患者 | /api/v1/patients/{id} | PUT | PatientInputDto | PatientDetailDto | 同上 |
| 删除患者 | /api/v1/patients/{id} | DELETE | — | 204 | AdminOrSuperAdmin |
| 恢复患者 | /api/v1/patients/{id}/restore | POST | — | PatientDetailDto | AdminOrSuperAdmin |
| 批量删除 | /api/v1/patients/batch-delete | POST | BatchDeleteInputDto | BatchResult | AdminOrSuperAdmin |
| 按身份证查询 | /api/v1/patients/by-id-number/{id} | GET | — | PatientDetailDto | 同上 |
| 药材列表 | /api/v1/herbs | GET | ?page&keyword | PagedResult | DoctorOrAdmin |
| 创建药材 | /api/v1/herbs | POST | HerbInputDto | HerbDetailDto 201 | AdminOrSuperAdmin |
| 更新药材 | /api/v1/herbs/{id} | PUT | HerbInputDto | HerbDetailDto | AdminOrSuperAdmin |
| 删除药材 | /api/v1/herbs/{id} | DELETE | — | 204 | AdminOrSuperAdmin |
| 切换药材状态 | /api/v1/herbs/{id}/toggle-status | POST | — | HerbDetailDto | AdminOrSuperAdmin |
| 批量导入药材 | /api/v1/herbs/batch-import | POST | HerbBatchImportInputDto | ImportResult | AdminOrSuperAdmin |
| 验方列表 | /api/v1/formulas | GET | ?page&keyword | PagedResult | DoctorOrAdmin |
| 创建验方 | /api/v1/formulas | POST | FormulaInputDto | FormulaDetailDto 201 | DoctorOrAdmin |
| 更新验方 | /api/v1/formulas/{id} | PUT | FormulaInputDto | FormulaDetailDto | DoctorOrAdmin |
| 删除验方 | /api/v1/formulas/{id} | DELETE | — | 204 | DoctorOrAdmin |
| 批量导入验方 | /api/v1/formulas/batch-import | POST | List<FormulaImportItemDto> | ImportResult | AdminOrSuperAdmin |
| 克隆验方 | /api/v1/formulas/{id}/clone | POST | — | FormulaDetailDto | DoctorOrAdmin |
| 待校验验方 | /api/v1/formulas/pending-validation | GET | ?page | PagedResult | DoctorOrAdmin |
| 创建医案 | /api/v1/medicalcases | POST | MedicalCaseInputDto | MedicalCaseDetailDto | DoctorOnly |
| 更新医案 | /api/v1/medicalcases/{id} | PUT | MedicalCaseInputDto | MedicalCaseDetailDto | DoctorOrAdmin |
| 获取医案列表 | /api/v1/medicalcases | GET | ?page&keyword | PagedResult | DoctorOrAdmin |
| 完成医案 | /api/v1/medicalcases/{id}/complete | POST | — | MedicalCaseDetailDto | DoctorOrAdmin |
| 打印医案 | /api/v1/medicalcases/{id}/print | POST | PrintRequest | PrintResult | DoctorOnly |
| 审计日志 | /api/v1/medicalcases/{id}/audit-logs | GET | — | List<AuditLog> | DoctorOrAdmin |
| 安全审计日志 | /api/v1/security-audit | GET | ?page&pageSize&eventType&userName&from&to | PagedResult<SecurityAuditLogDto> | SysAdminOnly（US-SHELL-014） |
| 创建挂号 | /api/v1/registrations | POST | RegistrationInputDto | RegistrationDto 201 | DoctorOrReceptionist |
| 取消挂号 | /api/v1/registrations/{id}/cancel | POST | — | 204 | ReceptionistOnly |
| 开始就诊 | /api/v1/registrations/{id}/start-visit | POST | — | MedicalCaseId | DoctorOnly |
| 待诊队列 | /api/v1/registrations/pending | GET | ?doctorId | List<Registration> | DoctorOrReceptionist |
| 报表-收入 | /api/v1/reports/income | GET | ?start&end&granularity | IncomeTrendDto | DoctorOrAdmin |
| 报表-接诊 | /api/v1/reports/consultation | GET | ?start&end | ConsultationTrendDto | DoctorOrAdmin |
| 报表-绩效 | /api/v1/reports/doctor-performance | GET | ?start&end | List<DoctorPerformanceDto> | DoctorOrAdmin |
| 报表-排行 | /api/v1/reports/herb-ranking | GET | ?top | List<HerbUsageItemDto> | DoctorOrAdmin |
| 用户列表 | /api/v1/users | GET | ?page&role | PagedResult | AdminOrSuperAdmin |
| 创建用户 | /api/v1/users | POST | UserInputDto | UserDetailDto 201 | AdminOrSuperAdmin |
| 禁用用户 | /api/v1/users/{id}/toggle-status | POST | — | UserDetailDto | AdminOrSuperAdmin |
| 配置 | /api/v1/configuration | GET/PUT | — | ConfigDto | SysAdminOnly |
| 健康检查 | /health | GET | — | Healthy | AllowAnonymous |
| 备份 | /api/v1/backup | POST/GET | — | BackupDto | SysAdminOnly |

## 9. 共享控件复用矩阵
> 与 §1.3 同源：仅列 `LYBT.Desktop.Controls` 的 16 个共享控件；模块内嵌控件（含 `WorkflowStepIndicator`）见 §1.3.1。

| 控件 | 使用页面 | 说明 | 关键属性 |
|------|---------|------|----------|
| MasterDetailLayout | 患者/药材/验方/医案/用户（Master-Detail 五处） | 统一主从 | `MasterWidth=380` |
| DataGridToolbar | 所有列表 | 搜索+新建+导入/导出+批量 | 统一顺序 |
| UnifiedPaginationBar | 所有分页 | 页码 | `共 {n} 条` |
| SearchBox | 所有列表 | 实时搜索 | `SearchText/Placeholder/SearchCommand`（VM 侧 500ms 防抖） |
| InfoCard | 各角色首页 | 统计卡 | `Title/ShowTitle/Content` |
| StatusBadge | 所有状态列 | 启用/禁用/完成 | `Status` → `DisplayText` |
| BreadcrumbBar | 主界面/详情 | 面包屑 | `NavigationPath` 完整路径可回溯 |
| EmptyState | 空列表/空报表 | 占位 | `暂无{实体} 去创建` |
| LoadingOverlay | 加载中 | 遮罩 | `IsLoading/LoadingText` 强制文案 |
| ToastControl | 全局 | 提示 | `Show(msg, ToastType)`，`Info/Success/Warning/Error` |
| DetailToolbar | 详情 | 编辑/删除/打印 | `EditCommand/DeleteCommand/SaveCommand/ShowDeleteButton` |
| PatientInfoCardControl | 医案工作台/患者选择 | 患者信息 | `Patient/DisplayMode/ShowHistoryButton` |
| HerbListControl | 处方/验方组成 | 药材列表 | `HerbItems/AllHerbs/DuplicateStrategy` 拖拽排序 |
| HerbItemControl | 处方/验方行 | 单行药材 | `IsEditMode/AllHerbs/ItemIndex` |
| BaseDetailContainer | 详情/表单容器 | 查看↔编辑切换 | `ViewContent/EditContent/IsDirty/SaveCommand` |
| FormulaViewControl | 验方详情 | 只读验方 | `Formula/ShowSystemInfo` |

## 10. 跨 R 去重后问题清单（P0/P1/P2）

| # | 问题 | 来源 | 级别 | 状态 |
|---|------|------|------|------|
| 1 | 药材/验方查看 Receptionist 误有权限（类级 `DoctorOrReceptionist`） | R02,R17,R22 | P1 | 待修：类级改为 `DoctorOrAdmin` |
| 2 | 打印无 `DoctorOnly` 细分 | R02,R17 | P1 | 待修 |
| 3 | 挂号取消无 `ReceptionistOnly` 细分 | R02,R17 | P1 | 待修 |
| 4 | 报表行级仅 Controller 下推，Service 无二次校验 | R07,R17 | P1 | 待修：`ReportService` 加 `ICurrentUserProvider` |
| 5 | 待诊队列无超时分级 | R01,R15 | P2 | 待办：>30黄 >60红 |
| 6 | 报表空数据无 `EmptyState` | R02,R16 | P2 | 待办 |
| 7 | 系统设置保存无持久确认 | R02,R12 | P2 | 待办：`✓ 已保存` |
| 8 | 挂号医生下拉无可用性 | R03,R15 | P2 | 待办：`StatusBadge` |
| 9 | 备份恢复文案风险不足 | R04,R22 | P1 | 待修：红警告+倒计时 |
| 10 | 日志级别无影响提示 | R04,R12 | P2 | 待办 |
| 11 | 部署上传无进度 | R04,R16 | P2 | 待办 |
| 12 | 跨页上下文丢失 | R05,R11 | P2 | 待办：`LastPatientId` 持久化 |
| 13 | StartVisit 无防重遮罩 | R05,R12 | P1 | 待修：`LoadingOverlay` 全屏 |
| 14 | Catalog 列表重复 | R06,R21 | P2 | 待办：泛型模板 |
| 15 | 验方导入无行级预览 | R06,R12 | P2 | 待办 |
| 16 | ViewNames 与 Region 不一致 | R09,R15 | P2 | 待办 |
| 17 | 导航参数明文无类型 | R09 | P2 | 待办：强类型 record |
| 18 | 列表 `KeepAlive` 未启用 | R09,R11 | P2 | 待办 |
| 19 | ComboBox 单向绑定 | R10 | P1 | 待修：`Mode=TwoWay` |
| 20 | CanExecute 未与校验联动 | R10,R18 | P1 | 待修 |
| 21 | 集合 `List` 非 Observable | R10 | P2 | 待办 |
| 22 | Session 切换残留 | R11 | P1 | 待修：登出重置 |
| 23 | 配置路径双轨 | R08,R11 | P2 | 待办：统一 `%LOCALAPPDATA%` |
| 24 | 保存失败无重试 | R12 | P2 | 待办：`Snackbar` 重试 Action |
| 25 | 422/500 未分级 | R12 | P2 | 待办：黄/红区分 |
| 26 | 行双击不一致 | R13 | P2 | 待办：单击选中双击编辑 |
| 27 | 快捷键未统一 | R13 | P2 | 待办：`Ctrl+S/P/Esc` |
| 28 | 处方拖拽未实现 | R13 | P2 | 待办 |
| 29 | 主色混用紫 | R14 | P1 | 待修：`TcmPrimaryBrush` |
| 30 | 间距非 8pt | R14 | P2 | 待办 |
| 31 | 投影层级错 | R14 | P2 | 待办 |
| 32 | 侧边栏无折叠 | R15 | P2 | 待办 |
| 33 | 面包屑仅一级 | R15 | P2 | 待办：完整路径 |
| 34 | 首页卡片权重失衡 | R15 | P2 | 待办：待诊大卡 |
| 35 | Loading 文案缺失 | R16 | P2 | 待办：强制文案 |
| 36 | 成功无常驻 | R16 | P2 | 待办 |
| 37 | 空/错视觉混用 | R16 | P2 | 待办 |
| 38 | 文案不一致 | R18 | P2 | 待办：统一 `ValidationMessages` |
| 39 | 查重无防抖 | R18 | P2 | 待办：500ms |
| 40 | ErrorTemplate 遮挡 | R18 | P2 | 待办：仅红框+Tooltip |
| 41 | 虚拟化未显式 | R19 | P2 | 待办 |
| 42 | 分页预加载未用 | R19 | P2 | 待办 |
| 43 | 报表懒加载未做 | R19 | P2 | 待办：首屏仅 income |
| 44 | TabIndex 未显式 | R20 | P2 | 待办 |
| 45 | 焦点未自动 | R20 | P2 | 待办 |
| 46 | 读屏 Name 未绑 | R20 | P2 | 待办 |
| 47 | 工具栏顺序不一致 | R21 | P2 | 待办 |
| 48 | 空文案不一致 | R21 | P2 | 待办 |
| 49 | 分页文案不一致 | R21 | P2 | 待办 |
| 50 | 设计稿与代码不一致 | R22 | P1 | 待修：补 4 图 |
| 51 | 推送无设计 | R22 | P2 | 待办 |
| 52 | 备份文案与需求不符 | R22 | P1 | 待修 |
| 53 | 封面/§4 数量口径与代码不符（v2.0 称 25 View + 24 Control + 51+ VM + 「17 页」） | 本轮修订（2026-09-13，依据代码实际 `src/Client/Desktop`） | P1 | 已修：封面 → **View 30 / Control 33 / Dialog 7 / ViewModel 55**（XAML 82 = 视图 71 + 资源模板 11），并在封面声明计数口径 |
| 54 | §1.3 共享控件表**重复**列出 `BreadcrumbBar`、`UnifiedPaginationBar`（各两次），缺 `BaseDetailContainer`/`FormulaViewControl`/`HerbItemControl`，且混入模块控件 `WorkflowStepIndicator` | 本轮修订（同上） | P2 | 已修：去重 + 补全 `LYBT.Desktop.Controls` 16 个 + 模块内嵌 17 个另列 §1.3.1；§9 矩阵同源更新 |
| 55 | §4 仅覆盖 17 页，未覆盖 `SecurityAuditLogView`/`MedicalCaseManagementView`/`AccountSettingsView`/`AppShell`/`HeaderControl`/`SideNavControl`/`FooterControl` 等 | 本轮修订（同上） | P1 | 已修：§4 覆盖全部 30 View（新增 §4.4.1/§4.18/§4.19/§4.20），并补 §4.21 对话框清单、§4.23 覆盖核对表 |
| 56 | 8 个视图名在代码中不存在（`InitializationWizardView`/`CardReaderDiagnosticsView`/`ConfigExportImportView`/`ServerConfigPanelView`/`SessionTimeoutWarningDialog`/`UnfinishedCaseDialog`/`PrintPreviewDialog`/`RegistrationCreateView`） | 本轮修订（同上） | P2 | 已修：§4.22 逐条标 `[未建视图]` 并给出真实承载者（W-01→`FirstRunSetupView`+US-SHELL-011；SY-05/07/08→`SysadminHomeView` 内嵌子 VM；DD-04→`UnsavedChangesDialog` 等） |
| 57 | §3/§4 引用与代码不符：内容区 Region 曾写作 `RegionNames.MainContent`（实际 `RegionNames.ContentRegion`）、`INavigationCoordinator.NavigateToMedicalCaseWorkspace(params)`（实际 `NavigateTo<TParams>`）、全局快捷键 `Ctrl+S/Ctrl+P/Esc`（实际见 `MainWindow.InputBindings`）、Sysadmin「Tab 配置/备份/日志/部署」（实际 3 Tab + 4 快捷入口）、侧栏分组曾写作「主页/业务/管理」（实际 `Group` = 临床/目录/管理） | 本轮修订（同上） | P2 | 已修：全文统一为代码实际口径 |

| 58 | 品牌文案未统一：`LoginView.xaml` 硬编码「中医诊所管理系统」4 处（副标题/版本/版权/登录提示），权威名为「凌隐宝堂中医诊所管理系统」 | 本轮修订（2026-09-13 品牌核对） | P2 | 待修（代码侧文案统一，文档已按权威名表述） |

> 共 52 项去重后：P1 12 项，P2 40 项，无 P0；R01-R22 已交叉引用，去重合并完成。⚠️ 问题状态应以 13c-current-status.md 为准（本文档为设计时统计）。
> 本轮（v2.1, 2026-09-13）新增 **#53-#58**（6 项，依据代码实际 `src/Client/Desktop`）: #53-#57 为文档-代码对齐修订（已闭合），#58 为品牌核对新发现项（代码侧待修）。

## 11. 实施建议
1. **P1 优先**（12 项，2 周）：权限细分 4 项 + 表单/绑定 3 项 + 保存/会话 3 项 + 设计对齐 2 项
2. **P2 分批**（40 项，4 周）：按页面聚合（患者/药材/医案/报表/运维）每 Sprint 10 项
3. **验证**：`build 0/0 + arch 91/91` 为门禁，每 P1 闭合后 `grep -r` 权限/校验一致性检查

> 本文档为 R01→R22 的综合（R23），可直接作为 `desktop-ui-detailed-design.md` 定版，`R01-R22` 22 份独立报告已归档于 `docs/compose/reports/ui-research-R*.md`。

---


---

## 附录 A：登录后统一框架（已确认）

> 决策依据：`docs/compose/reports/ui-layout-framework-decision.md`（12项功能需求对照，方案2得...[truncated]
分11/12，方案1=8/12，方案3=8/12）

### A.1 框架结构

```
┌──────────────────────────────────────────────────────────┐
│  头栏 48px (固定)                                         │
│  [☰汉堡] [面包屑 Level1›2›3] [全局搜索] [🔔通知] [👤头像] │
├──────────┬───────────────────────────────────────────────┤
│          │                                               │
│  侧边栏  │  ContentRegion (可内含 SplitView)              │
│  240/64px│  ┌──────────┬───────────────┐                │
│  可折叠   │  │ Master   │ Detail(可滚)  │                │
│  分组折叠 │  └──────────┴───────────────┘                │
│  临床/目录│  ← GridSplitter 12px 拖拽                    │
│  管理     │                                               │
├──────────┴───────────────────────────────────────────────┤
│  状态栏 32px (固定): API | 模式 | 用户 | 时间              │
└──────────────────────────────────────────────────────────┘
```

### A.2 固定元素规格

| 元素 | 位置 | 大小 | 内容 | 交互 |
|------|------|------|------|------|
| **头栏** | 窗口顶部 | 高 48px，全宽 | 汉堡按钮 + 面包屑 + 全局搜索 + 通知 + 头像下拉 | 汉堡折叠侧栏 Ctrl+M |
| **侧边栏** | 窗口左侧 | 宽 240px（折叠 64px 图标轨） | Logo + 分组导航（临床/目录/管理） + 账户/主题/退出 | 分组折叠，角色菜单动态过滤 |
| **状态栏** | 窗口底部 | 高 32px，全宽 | API状态灯 + 连接模式 + 用户角色 + 用户名 + 时间 | 只读 |
| **面包屑** | 头栏内 | 弹性 | 首页 > 当前模块 > 当前页面 | 点击可返回上级 |
| **GridSplitter** | Master/Detail 之间 | 宽 12px | 拖拽分隔线 | 悬停高亮，拖拽调整比例 |

### A.3 侧边栏菜单（按角色过滤）

| 角色 | 可见菜单项 |
|------|-----------|
| **Sysadmin** | 运维首页、备份恢复、日志级别、部署管理 |
| **Admin** | 首页、用户管理、患者管理、药材管理、验方管理、报表、系统设置 |
| **Doctor** | 首页、患者管理、医案管理、待诊队列、药材查看、验方管理、报表 |
| **Receptionist** | 首页、患者管理、挂号列表、待诊队列 |

### A.4 内容区域布局模式

| 模式 | 适用页面 | 结构 |
|------|---------|------|
| **Master-Detail** | 患者/药材/验方/用户/挂号管理 | 左 380px 固定 Master + 右弹性 Detail + GridSplitter |
| **Workspace** | 医案工作台 | 顶部患者卡 + 中部分栏（诊断/处方） |
| **Dashboard Grid** | 各角色首页 | 2-4 列 InfoCard 网格 |
| **Form Dialog** | 新增/编辑弹窗 | 居中 640×480 表单 |
| **Split View** | 待诊队列 | 左队列 + 右患者详情 |

### A.5 生成设计稿 Prompt 模板

```
设计要求：
1. 窗口尺寸 1440×900，Material Design 风格
2. 顶部 48px 头栏：汉堡按钮 + 面包屑导航 + 全局搜索 + 通知图标 + 用户头像
3. 左侧 240px 可折叠侧边栏（折叠后 64px 图标轨）：
   - 顶部 Logo + 系统名称
   - 中间分组导航菜单（临床/目录/管理分组）
   - 底部账户设置/主题切换/退出
4. 内容区域：页面特定内容 + GridSplitter（列表页）
5. 底部 32px 状态栏：API状态灯 + 连接模式 + 用户角色 + 用户名 + 时间
6. 统一设计 Token：主色 #1A5C3A，辅金 #C9A86A，背景 #F5F7FA
7. 侧边栏背景：MaterialDesign.Brush.Primary (Brown)
8. 所有文字使用思源黑体/微软雅黑
9. 间距基准 8pt (4/8/16/24/32)
10. 品牌名统一显示"凌隐宝堂中医诊所管理系统"（顶栏品牌块为"凌隐宝堂中医诊所"）
```
