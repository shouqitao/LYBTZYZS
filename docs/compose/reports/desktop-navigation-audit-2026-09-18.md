# Desktop 页面跳转 / View·ViewModel 审查报告

> 版本: v1.0 | 日期: 2026-09-18 | 状态: 审查完成（待按设计修复）
> 审查范围: `src/Client/Desktop/` 导航全链路
> 事实源: 代码（NavigationCoordinator / ViewNames / RoleDefinition / 各 Home·业务 VM / Dialog 注册）
> 关联设计: [desktop-navigation-viewmodel-design-2026-09-18.md](../specs/desktop-navigation-viewmodel-design-2026-09-18.md)
> 关联清单: [desktop-view-inventory.md](../specs/desktop-view-inventory.md)、正式文档 [desktop-ui-detailed-design.md](../../07-ui-ux/desktop-ui-detailed-design.md) §3/§5

---

## 〇、角色主页 SSOT

| 角色 | HomeView | 证据 |
|------|----------|------|
| Doctor | `ClinicalWorkspaceView` | `DoctorRoleDefinition.cs:37` |
| Receptionist | `ReceptionistHomeView` | `ReceptionistRoleDefinition.cs:34` |
| Admin | `AdminHomeView` | `AdminRoleDefinition.cs:36` |
| SuperAdmin | `SysadminHomeView` | `SuperAdminRoleDefinition.cs:33` |
| 未注册角色 fallback | `ClinicalHomeView` | `RoleRegistry.cs:21,64-65` |

登录跳转：`LoginCoordinator.NavigateToRoleHomeAsync`（`LoginCoordinator.cs:273-296`）→ `NavigationCoordinator.NavigateToHome(role)`。

核心导航入口：`NavigationCoordinator.NavigateTo`（`NavigationCoordinator.cs:121-195`）：
1. 角色守卫 `ViewRoleAccess`（`:32-56,318-337`）
2. 防抖 300ms
3. `ModuleLazyLoader.EnsureModuleLoadedAsync`
4. `RegionManager.RequestNavigate(ContentRegion, viewName, navParams)`

---

## 一、角色导航路径图

### Doctor（Home = ClinicalWorkspace）

```
Login → ClinicalWorkspace
  ├─ NavigationManager.cs:68-70  主页 → ClinicalWorkspace
  ├─ NavigationManager.cs:78     患者选择 → PatientSelection
  ├─ NavigationManager.cs:79     医案工作台 → MedicalCaseWorkspace  ⚠无参数
  │
  ├─ ClinicalWorkspaceViewModel.cs:127-135 StartConsultation
  │    → MedicalCaseWorkspace（ForClinical(patientId)）⚠缺 CurrentPatient/MedicalCaseId
  ├─ ClinicalWorkspaceViewModel.cs:147-153 NewPatient
  │    → PatientManagement(Action=AddNew) ⚠参数无人消费
  │
  ├─ PatientSelectionViewModel.cs:178-183 BackToHome
  │    → 基类 NavigateToHome() → ClinicalWorkspace ⚠绕过 NavigationCoordinator
  ├─ PatientSelectionViewModel.cs:193-197 NewPatient → PatientManagement
  ├─ PatientSelectionViewModel.cs:413-424 NavigateToMedicalCase
  │    → MedicalCaseWorkspace(MedicalCaseId+CurrentPatient+Mode+EditState) ✅
  │
  ├─ MedicalCaseWorkspaceViewModel.cs:532-536 ViewPatientHistory
  │    → PatientManagement（未带 PatientId）⚠
  ├─ MedicalCaseWorkspaceViewModel.cs:539-544 ViewAuditLogs
  │    → AuditLog(MedicalCaseId) ✅目标会消费
  ├─ WorkspaceNavigationHandler.cs:85 Back(Clinical) → PatientSelection
  │
  ├─ RegistrationListViewModel.cs:324-331 StartVisit
  │    → MedicalCaseWorkspace（完整参数）✅
  │
  ├─ ClinicalHomeViewModel（已注册但非 Doctor 主页，孤儿页）
  │    :92 ClinicalWorkspace | :102 PatientManagement | :112 MedicalCaseManagement
  │    :122 HerbManagement | :132 FormulaManagement | :143 RegistrationList
  │    :153 ReportsHome | :163 AuditLog | :173/:184 AccountSettings(Tab) ✅
  │
  ├─ MenuManager.cs:162 Ctrl+N → PatientManagement(Action=AddNew) ⚠
  ├─ MenuManager.cs:173 Ctrl+Shift+C → MedicalCaseWorkspace ⚠无参数
  ├─ MenuManager.cs:121 EditProfile → AccountSettings
  ├─ MenuManager.cs:132 SystemSettings → ❌Doctor 被守卫拦截
  └─ MainWindow.xaml:28-30 Alt+Left/Right/Home → Back/Forward/Home
```

### Receptionist（Home = ReceptionistHome）

```
Login → ReceptionistHome  （注册于 ClinicalModule.cs:45，OnDemand）
  ├─ NavigationManager.cs:82 挂号 → RegistrationList
  ├─ NavigationManager.cs:83 患者管理 → PatientManagement
  │
  ├─ ReceptionistHomeViewModel.cs:88-89  → PatientManagement
  ├─ :92-93  → RegistrationList
  ├─ :151-153 → PatientManagement(Action=Create) ⚠
  ├─ :157-159 → RegistrationList(Action=Create) ⚠
  ├─ :191-195 → RegistrationList(PatientId,PatientName) ⚠
  ├─ :200-203 → PatientManagement(SearchKeyword) ⚠
  ├─ :245-249 / :271-275 → RegistrationList(PatientId,PatientName) ⚠
  ├─ :242 确认框结果未判断即导航 ⚠
  │
  ├─ RegistrationListViewModel.cs:273 CreateRegistration → Dialog RegistrationCreateDialog
  ├─ RegistrationListViewModel.cs:331 StartVisit → MedicalCaseWorkspace
  │    ❌ ViewRoleAccess 仅 Doctor → 被拦截（前台接诊主路径断裂）
  │
  ├─ MenuManager.cs:173 QuickStart → MedicalCaseWorkspace ❌拦截
  ├─ MenuManager.cs:132 SystemSettings → ❌拦截
  └─ 顶栏 AccountSettings → 未列入 ViewRoleAccess，放行
```

### Admin（Home = AdminHome）

```
Login → AdminHome  （AdminModule WhenAvailable: App.xaml.cs:166）
  ├─ AdminHomeViewModel.cs:66  UserManagement
  ├─ :72  HerbManagement      （ClinicalModule 注册，需懒加载）
  ├─ :78  PatientManagement
  ├─ :84  FormulaManagement
  ├─ :90  MedicalCaseManagement
  ├─ :96  SystemSettings      （AdminModule 已注册）
  ├─ :102 ReportsHome         （ReportsModule OnDemand）
  ├─ :108 AuditLog            （MedicalCaseModule OnDemand）
  ├─ NavigationManager.cs:86-87 侧栏 UserManagement / HerbManagement
  ├─ MenuManager.cs:132 SystemSettings ✅
  ├─ MenuManager.cs:173 QuickStart → MedicalCaseWorkspace ❌仅 Doctor
  └─ AccountSettings ✅
```

### SuperAdmin（Home = SysadminHome）

```
Login → SysadminHome
  RequiredModules: UsersModule, SysadminModule, AdminModule, CardReaderModule
  （不含 Clinical/Reports/MedicalCase/Registration — 依赖懒加载）
  │
  ├─ SysadminHomeViewModel.cs:84  UserManagement
  ├─ :90  LogLevelControl
  ├─ :96  Deployment
  ├─ :102 SecurityAuditLog
  ├─ ⚠ 无 BackupManagement 命令（XAML :327-369 也无，仅侧栏 NavigationManager.cs:90）
  ├─ ViewRoleAccess 额外允许但主页/侧栏未直达：
  │    SystemSettings, PatientManagement, MedicalCaseManagement,
  │    HerbManagement, FormulaManagement, ReportsHome, AuditLog, MedicalCaseMasterDetail
  └─ MenuManager SystemSettings ✅；QuickStart ❌拦截
```

### ViewNames → RegisterForNavigation 核对

| ViewName | 注册位置 | 懒加载模块 |
|----------|----------|------------|
| ClinicalHome / ReceptionistHome / ClinicalWorkspace / PatientSelection / MedicalCaseWorkspace / Herb·Formula·Patient·MedicalCaseManagement | `ClinicalModule.cs:32-45` | ClinicalModule |
| AdminHome / SystemSettings / UserManagement | `AdminModule.cs:27-30` | AdminModule（WhenAvailable） |
| MedicalCaseMasterDetail / AuditLog | `MedicalCaseModule.cs:59-62` | MedicalCaseModule |
| RegistrationList | `RegistrationModule.cs:42` | RegistrationModule |
| ReportsHome | `Reports/ReportsModule.cs:30` | ReportsModule |
| SysadminHome / LogLevel / Deployment / Backup / SecurityAudit | `SysadminModule.cs:41-45` | SysadminModule（WhenAvailable） |
| Login | `AuthenticationModule.cs:40` | AuthenticationModule |
| AccountSettings | `App.xaml.cs:127` | Shell |

**结论**：ViewNames 常量均有 `RegisterForNavigation`，无「目标 View 未注册」类断裂。问题集中在角色守卫、参数契约、双导航路径、懒加载依赖。

---

## 二、跳转断裂点

| # | 来源 | 目标 | 问题 | 严重度 |
|---|------|------|------|--------|
| B1 | `RegistrationListViewModel.cs:331` StartVisit | MedicalCaseWorkspace | `ViewRoleAccess`（`NavigationCoordinator.cs:45`）仅 Doctor。前台接诊是核心业务流（Registrations AGENTS.md 明确 StartVisit→MedicalCaseWorkspace），Receptionist/Admin 点接诊被守卫拦截并 toast | **P0** |
| B2 | `MenuManager.cs:173`、`NavigationManager.cs:79` | MedicalCaseWorkspace | 无导航参数。`MedicalCaseWorkspaceViewModel.cs:327-328` 读到空 MedicalCaseId/CurrentPatient；`:398` 因 CurrentPatient==null 直接 return → 空白工作台 | **P0** |
| B3 | `ClinicalWorkspaceViewModel.cs:134-135` StartConsultation | MedicalCaseWorkspace | `ForClinical`（`MedicalCaseNavigationParameters.cs:38-45`）只带 PatientId/WorkspaceMode/InitialEditState，不带 CurrentPatient/MedicalCaseId。目标 VM **只读** MedicalCaseId+CurrentPatient，**不读** PatientId → 医生主页主路径断裂 | **P0** |
| B4 | `MenuManager.cs:132` SystemSettings | SystemSettings | Doctor/Receptionist 无权限（`NavigationCoordinator.cs:44`）。Ctrl+, 与全局菜单对所有角色可见 | **P1** |
| B5 | `MenuManager.cs:173`（Receptionist/Admin） | MedicalCaseWorkspace | 同 B1，非 Doctor 快捷键必然失败 | **P1** |
| B6 | SysadminHome | BackupManagement | 主页无导航命令与按钮；仅侧栏可达 | **P2** |
| B7 | ClinicalHomeViewModel 整页 | 多目标 | Doctor 主页已是 ClinicalWorkspace；ClinicalHome 无入口、注释过时（`ClinicalHomeViewModel.cs:17-19` 仍写→PatientSelection），命令成死路径 | **P2** |
| B8 | `PatientMasterDetailViewModel.cs:301-321` | 未实现 | ViewMedicalRecords/NewConsultation 仅 log + FUTURE，无 NavigateTo | **P1** |
| B9 | SuperAdmin → Herb/Reports 等 | Clinical/Reports 模块视图 | SuperAdmin RequiredModules 不含 Clinical/Reports/MedicalCase（`SuperAdminRoleDefinition.cs:15-25`）。ViewRoleAccess 允许访问，依赖 `ModuleLazyLoader`；加载失败静默 LogWarning（`ModuleLazyLoader.cs:81-84`）→ 空白页 | **P1** |
| B10 | RoleRegistry 未注册角色 | ClinicalHome | fallback `DefaultHomeView=ClinicalHome`（`RoleRegistry.cs:21`）→ 进入孤儿页 | **P3** |

---

## 三、状态传递问题

| # | 导航 | 参数 | 问题 |
|---|------|------|------|
| S1 | ClinicalWorkspace → MedicalCaseWorkspace（`ClinicalWorkspaceViewModel.cs:134`） | ForClinical: PatientId, WorkspaceMode, InitialEditState | **契约错位**：目标只消费 MedicalCaseId + CurrentPatient（`:327-331`）；PatientId 无人读；CurrentPatient 未传。正确对照：`PatientSelectionViewModel.cs:415-421`、`RegistrationListViewModel.cs:324-330` |
| S2 | Receptionist → RegistrationList（Action=Create / PatientId / PatientName） | `ReceptionistHomeViewModel.cs:159,191-195,245-249,271-275` | `RegistrationListViewModel.OnNavigatedToCore:140-147` **不读** NavigationParameters，只刷新队列。预填患者、自动打开新建弹窗均失效 |
| S3 | 多处 → PatientManagement（Action/SearchKeyword） | `MenuManager.cs:162`、`ReceptionistHomeViewModel.cs:153,200-203`、`ClinicalWorkspaceViewModel.cs:152` | `PatientMasterDetailViewModel` 无参数消费；`MasterDetailViewModelBase.cs:343-347` 仅 LoadListAsync；薄包装 `PatientManagementView.xaml` 无 code-behind OnNavigatedTo。参数全丢 |
| S4 | MedicalCaseWorkspace → PatientManagement（`:536`） | 无 | 日志有 PatientId（`:535`）但未传给目标 |
| S5 | AuditLog 入口无参 | MedicalCaseId | `AuditLogViewModel.cs:46-50` 仅在参数存在时加载；无参进入空白 |
| S6 | AccountSettings Tab=Password | `ClinicalHomeViewModel.cs:183-184` | `AccountSettingsViewModel.cs:280-284` 正确消费 ✅；但 Doctor 主页无「修改密码」入口 |
| S7 | UserManagement DefaultRoleFilter | `UserManagementView.xaml.cs:20-25`、`NavigationParams.cs:6` | View 端已实现，全仓库无调用方传入 → 死契约 |
| S8 | NavigateTo\<TParams\>（`NavigationCoordinator.cs:198-208`） | 反射扁平化 | 跳过 null；类型不匹配时 GetValue 静默 default(Guid) |
| S9 | 双导航 API | — | `NavigationCoordinator.NavigateTo`（守卫+懒加载+防抖+超时）vs `NavigableViewModelBase.NavigateTo`（`NavigableViewModelBase.Navigation.cs:171-176` 直调 RegionManager）并存。后者无守卫、无懒加载。`PatientSelectionViewModel.cs:183` BackToHome 走基类路径 |

---

## 四、返回路径问题

| # | 场景 | 问题 |
|---|------|------|
| R1 | 全局后退 | 仅 Alt+Left（`MainWindow.xaml:28`），UI 无可见后退按钮。`MenuManager.cs:113` CanExecute 依赖 Journal，但导航后未订阅 NavigationChanged 自动 RaiseCanExecuteChanged（仅手动 `:151-155`） |
| R2 | History vs Journal | `NavigationHistoryService` 只维护面包屑（`:25-32`）；NavigateBack 走 Prism Journal（`NavigationCoordinator.cs:232-252`）。两套历史不同步；ClearHistory 不清 Journal |
| R3 | MedicalCaseWorkspace 返回 | Clinical 模式 → PatientSelection（`WorkspaceNavigationHandler.cs:85`），与 Doctor 主页已是 ClinicalWorkspace 的布局脱节 |
| R4 | IsNavigationTarget=false（`MedicalCaseWorkspaceViewModel.cs:352`） | Journal GoBack 会按当时导航参数重建实例；从无参入口进入时返回仍是空工作台 |
| R5 | ClinicalWorkspace KeepAlive=true（`:79`） | 依赖 Region 是否保留实例；Journal 若 Remove 则 KeepAlive 无效 |
| R6 | Sysadmin 子页返回 | `SecurityAuditLogViewModel.cs:164`、`DeploymentViewModel.cs:123` 用 NavigateBack；Journal 空时仅 LogWarning（`NavigationCoordinator.cs:244`），无 UI 反馈、无 fallback 到 SysadminHome |
| R7 | AuditLog / 管理页 | 无统一业务内「返回」约定；多数 View 未绑定 NavigateToHome |
| R8 | 登录→主页 Journal | 登录与业务导航混在同一 ContentRegion Journal |

---

## 五、对话框/弹窗清单

| 对话框 | 触发方 | 注册方式 | 状态 |
|--------|--------|----------|------|
| MessageDialog | `DialogManager.cs:25-65` | `App.xaml.cs:124` | ✅ 参数键小写 message/title/type，与 `MessageDialogViewModel.cs:78-88` 一致 |
| ConfirmationDialog | `DialogManager.cs:81` | `App.xaml.cs:123` | ✅ 参数键 Pascal Message/Title（`:75-79`；VM `:99-100`） |
| InputDialog | 注册，审查范围内未见业务调用 | `App.xaml.cs:125` | ⚠ 调用点不明 |
| RegistrationCreateDialog | `RegistrationListViewModel.cs:273` | `RegistrationModule.cs:45` | ✅ 注册；**不消费**外部导航参数（→S2） |
| UnsavedChangesDialog | `WorkspaceNavigationHandler.cs:147` | `MedicalCaseModule.cs:55` | ✅ OnDemand 模块 |
| FormulaImportDialog | `MedicalCaseCommandsViewModel.cs:303` | `MedicalCaseModule.cs:53` | ✅ |
| HistoryCopyDialog | `MedicalCaseCommandsViewModel.cs:326` | `MedicalCaseModule.cs:54` | ✅ DialogParameters 传 PatientId/PatientName |
| ServerConfigView / FirstRunSetupView | Auth 启动/配置 | `AuthenticationModule.cs:43-46` | ✅ RegisterDialog |
| CommonDialogService（MessageBox） | 离开确认 `WorkspaceNavigationHandler.cs:110`；基类未保存确认 `NavigableViewModelBase.Navigation.cs:112` | DI（`ServiceCollectionExtensions.cs:184`） | ⚠ **System.MessageBox**（`CommonDialogService.cs:24-59`），与 MDIX 统一视觉及「Service 层无 MessageBox」约定冲突；三选项与 UnsavedChangesDialog 双轨 |
| FileDialogService | Excel 导出等 | DI `:186` | ✅ 仅 SaveFileDialog |
| Toast / UserNotificationService | 导航失败/权限拦截（`NavigationCoordinator.cs:129,169,180,193`） | Shell | ✅ 导航层用通知非弹窗 |
| ShellDialogHelper | MainWindow 辅助 | 构造注入 | ⚠ Success/Error 走 Toast，Warning/Confirm 走 MessageBox 混用（`ShellDialogHelper.cs:25-55`） |
| **DialogService.cs（任务指定路径）** | — | — | **不存在**。实际为 DialogManager + CommonDialogService + Prism IDialogService 多套并行 |

---

## 六、代码-文档偏差（待 SSOT 同步）

| 信息点 | 正式文档 | 代码现状 | 处理 |
|--------|----------|----------|------|
| 跨页 PatientId→医案工作台 | `desktop-ui-detailed-design.md` §5：`NavigationParameters["PatientId"]` 强类型 MedicalCaseNavigationParameters | MedicalCaseWorkspace **不读** PatientId；临床路径传 ForClinical 后目标空白 | 按本报告设计修复代码后，更新 §5 为完整参数契约 |
| Receptionist 导航含 PatientSelection | 同文档 §3.4 | 侧栏/主页入口无 PatientSelection；ViewRoleAccess 允许但无 UI | 设计定稿后同步 §3.4 |
| MedicalCaseWorkspace 角色 | 文档未明确限制 | ViewRoleAccess 仅 Doctor，与前台接诊冲突 | 设计将守卫矩阵写入正式文档 |

---

## 七、问题统计

| 类别 | P0 | P1 | P2 | P3 | 小计 |
|------|----|----|----|----|------|
| 跳转断裂 | 3 | 4 | 2 | 1 | 10 |
| 状态传递 | 3 | 5 | 1 | 0 | 9 |
| 返回路径 | 2 | 4 | 2 | 0 | 8 |
| 对话框 | 0 | 2 | 3 | 0 | 5 |

**Top 3 阻断项**：B1 前台接诊被角色守卫拦截；B2/B3 MedicalCaseWorkspace 参数契约错位导致空白工作台；S9 双导航路径绕过守卫。

---

## 八、审查方法与局限

- 静态代码审查（源码 + XAML + 模块注册 + RoleDefinition），未跑 UI 自动化。
- `IModuleLoadingService` 运行时行为（双模式下 LoadModule 失败率）未实测。
- 服务端授权策略与 ViewRoleAccess 是否双重允许 Receptionist 打开医案工作台，需对照 `docs/01-product/04-permissions.md` 与双控制器树再确认。

审查完成后进入设计：[desktop-navigation-viewmodel-design-2026-09-18.md](../specs/desktop-navigation-viewmodel-design-2026-09-18.md)。
