# Desktop ViewModel + View/XAML 代码审查报告

> 审查范围: `src/Client/Desktop/` 全部 ViewModel 与 View/XAML
> 方法: 5 路并行只读审查（Shell/Auth、MedicalCase、Catalog+Patients+Users+Registrations、Roles、Infrastructure 基类+共享控件），逐文件核对 4 维度检查表（VM 质量 / XAML 质量 / 跨层一致性 / 架构合规）
> 日期: 2026-08-26 | 方式: 只读，未改代码

---

## P0 — 必须修复（编译/运行时错误）

| # | 文件 | 行 | 问题 | 修复建议 |
|---|------|-----|------|----------|
| 1 | `Core/LYBT.Desktop.Controls/Controls/BaseDetailContainer.xaml.cs`（触发于 `BaseDetailContainer.xaml:14/232/249`） | 113-156 | `OnGoBackCommandChanged` 回调内 `UpdateGoBackCommandWithDirtyCheck` 执行 `SetValue(GoBackCommandProperty, wrappedCommand)` —— **SetValue 再次触发同一 DP 的回调**，对 `wrappedCommand` 再次包装 → **无限递归 / StackOverflow**。任何视图绑定 `GoBackCommand`（如 MedicalCaseWorkspaceView 经 AncestorType=UserControl）即崩溃 | 回调内写入前判断 `e.NewValue` 是否已是包装命令（标志位或包装器类型检查），或改用单独内部 DP 存原始命令，仅作脏检查不重写 DP |
| 2 | `Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml` | 191、198 | `Consultation` / `Prescription` 绑定目标不存在 —— VM 暴露的是 `ConsultationEditor` / `PrescriptionEditor`（`MedicalCaseMasterDetailViewModel.cs:32-33`），非 `Consultation`/`Prescription` 属性 | 改为 `{Binding ConsultationEditor.Consultation}` / `{Binding PrescriptionEditor.Prescription}`，或补 VM 转发属性 |
| 3 | `Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` | 572-640（10 处） | 10 个 `Completeness.*` 绑定（DiagnosisComplete/PrescriptionDecisionComplete/PrescriptionContentComplete/PrescriptionItemCount/DosageCountComplete/DosageCount…）DataContext=`ElementName=Root`（控件自身，line 129），但控件**无 Completeness 依赖属性** → 全部静默失效 | 在控件加 `Completeness` DP 并由宿主 VM 传入，或移除这些失效绑定 |
| 4 | `Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialog.xaml` | 172 | 绑定 `Indications`（复数）但 `FormulaListDto.Indication`（单数）→ 断链 | 改 `{Binding Indication, TargetNullValue=暂无适应症}` |
| 5 | `Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml` | 62 | `DynamicResource "MaterialDesignBrush Primary"` —— 键拼写错误（应为 `MaterialDesign.Brush.Primary`）→ 资源不可解析，背景渲染失效 | 改 `{DynamicResource MaterialDesign.Brush.Primary}` |

## P1 — 建议修复（质量/规范问题）

| # | 文件 | 行 | 问题 | 修复建议 |
|---|------|-----|------|----------|
| 6 | `Shell/Views/AppShell.xaml:19` + `Shell/ViewModels/MainWindowViewModel.cs:64-173` + `Shell/ViewModels/SideNavViewModel.cs` | 19/64/165 | **侧边栏展开/收拢状态双份且不同步**：`AppShell` 列宽绑 `MainWindowViewModel.SidebarWidth`，汉堡按钮绑 `SideNavViewModel.IsSidebarExpanded` —— 两个独立 VM 各持一份状态，点击汉堡后列宽不随之变化（主交互失效） | 单一事实源：AppShell 列宽改绑 `SideNavViewModel.SidebarWidth`，或在 Shell 层合并为共享状态 |
| 7 | `Core/LYBT.Desktop.Infrastructure/ViewModels/Base/DialogViewModelBase.cs` | 32-33 | 重新声明 `[ObservableProperty] _isLoading` **遮蔽**基类 `IsLoading`，派生类两套 `IsLoading`/`IsNotLoading` 状态可能发散 | 删除子类重复声明，复用基类 `IsLoading` |
| 8 | `Core/LYBT.Desktop.Infrastructure/ViewModels/Base/EditorViewModelBase.cs` | 30、45 | `SubscribeContext()` 订阅 `Context.PropertyChanged`，但本类**未实现 IDisposable**，仅在 `Reset()` 退订 → 上下文变更未 Reset 时订阅泄漏 | 实现 IDisposable 并对称退订 |
| 9 | `Roles/LYBT.Desktop.Clinical/Receptionist/Views/ReceptionistHomeView.xaml` | 270 | 「刷新数据」按钮绑 `RefreshDataCommand`，VM（`ReceptionistHomeViewModel.cs` 命令 87/91/95/150/156/162）**无此命令** → 死按钮 | 绑定 VM 已有刷新命令或删按钮 |
| 10 | `Roles/LYBT.Desktop.Admin/Sysadmin/Views/SysadminHomeView.xaml` | 166-167 | `CommandParameter="{Binding Password, ElementName=NewUserPasswordBox}"` —— `PasswordBox.Password` **非 INPC**，绑定不会更新 → `SaveSecurityCommand` 永远收不到实际密码 | code-behind 在 PasswordChanged 时刷新参数，或改用 `PasswordBoxHelper.BoundPassword` |
| 11 | `Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml` | 170 | 状态栏绑基类 `StatusMessage`，但 VM 写入的是 `PageStatusMessage`（`PatientSelectionViewModel.cs:74/234/265/413`）→ 状态栏永不更新 | 改绑 `PageStatusMessage` |
| 12 | `Roles/LYBT.Desktop.Admin/Sysadmin/ViewModels/SysadminHomeViewModel.cs` | 63 | `_connectionMode.ModeChanged += OnModeChanged` **从不退订**（无 Dispose 重写退订；`_pollCts` 在 StartPolling 内 Dispose 但导航离开/VM 销毁未 StopPolling） | 重写 Dispose 退订 ModeChanged + 取消 `_pollCts` |
| 13 | `Modules/LYBT.Desktop.MedicalCase/Controls/WorkflowStepIndicator.xaml` | 44、54 | `ActiveStepStyle` / `CompletedStepStyle` 已定义但**从未应用**（无 `IsActive` 触发绑定）→ 当前步骤样式不生效 | 在步骤模板加 `IsActive` 触发器引用对应样式 |
| 14 | `Shell/Dialogs/Views/MessageDialog.xaml:86` + `Shell/Dialogs/Views/InputDialog.xaml:41,57` | 86/41/57 | 引用未定义主题样式 `ButtonPrimary` / `TextBoxExtend` → 回退默认样式 | 改用已定义 `MaterialDesign*` 样式或补资源 |
| 15 | `Shell/ViewModels/AccountSettingsViewModel.cs` | 67-74 | 构造函数对注入依赖（authService/userService/navigationCoordinator）**无 null 检查**（与兄弟 VM 不一致） | 补 `?? throw new ArgumentNullException` |
| 16 | `Modules/LYBT.Desktop.MedicalCase/ViewModels/AuditLogViewModel.cs` | 30-35 | 构造函数对注入依赖**无 null 检查** | 同上 |
| 17 | `Core/LYBT.Desktop.Controls/Controls/BreadcrumbBar.xaml(.cs)`（仅被 `BaseDetailContainer.xaml` 实例化） | — | `NavigationPath` 从未被任何视图赋值 → 面包屑处处渲染为空（死控件） | 宿主视图注入 NavigationPath，或移除死控件 |

## P2 — 可选优化

| # | 文件 | 行 | 问题 | 优化建议 |
|---|------|-----|------|----------|
| 18 | `Core/LYBT.Desktop.Controls/Controls/FormulaView/FormulaViewControl.xaml` | 28 | `HerbTagStyle` 硬编码浅色配色，不随深色主题 | 改用主题画刷 |
| 19 | `Core/LYBT.Desktop.Controls/Controls/StatusBadge.xaml.cs` | 210 | `TryFindResource` 一次性快照，注释宣称"随主题切换实时更新"但实际不更新 | 改用 DynamicResource 或绑定 |
| 20 | `Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml` / `DetailToolbar.xaml` / `PatientInfoCardControl.xaml` | — | 按钮前景/头像文本硬编码 `White` | 主题化 |
| 21 | `Core/LYBT.Desktop.Controls/Controls/Toast/ToastControl.xaml.cs` / `LoadingOverlay.xaml.cs` | — | Show/Hide 新建 Timer 未清旧；LoadingOverlay `_delayTimer` 卸载未停；均无 IDisposable | 复用一个计时器并在 Unloaded/Dispose 停 |
| 22 | `Modules/LYBT.Desktop.MedicalCase/ViewModels/Items/PrescriptionItemViewModel.cs` | 143-146 | `Discount` setter 未通知 `TotalPrice`（`TotalPrice`=SingleDosePrice*DosageCount*Discount，line 297）→ 改折扣界面不刷新总价 | setter 补 `RaisePropertyChanged(nameof(TotalPrice))` |
| 23 | `Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs` | 67-70 | 子 VM（ConsultationEditor/PrescriptionEditor）创建后**未在 Dispose 释放** | 重写 Dispose 调子 VM Dispose |
| 24 | `Modules/LYBT.Desktop.Catalog/ViewModels/FormulaMasterDetailViewModel.cs` | 511-516 | 空 `Dispose` 重写死代码 | 删除 |
| 25 | `Modules/LYBT.Desktop.Catalog/ViewModels/HerbMasterDetailViewModel.cs` | 124 | `_herbMapper.ToItem(result.Data)` 结果未用，仅作映射副作用 → 死 mapper 调用 | 删除该行与未用依赖 |
| 26 | `Modules/LYBT.Desktop.Registrations/ViewModels/RegistrationListViewModel.cs` | 91/157/198,231,292 | 缺部分 ctor null 守卫（`_registrationService` 未查）；冗余 `_eventAggregatorAccessor` 字段；异步命令不传 CancellationToken | 补守卫/删冗余/传 ct |
| 27 | `Modules/LYBT.Desktop.Registrations/Dialogs/RegistrationCreateDialogViewModel.cs` | 58-68 | ctor 注入无 null 检查 | 补守卫 |
| 28 | `Modules/LYBT.Desktop.Registrations/Dialogs/RegistrationCreateDialog.xaml` | 116 | 硬编码 `#228B22` 绿（有 TcmGreenBrush） | 主题化 |
| 29 | `Modules/LYBT.Desktop.Auth/ViewModels/ConnectionStatusViewModel.cs` | 220 | VM 直接调 `MessageBox.Show`（违反 MVVM） | 改用 Dialog/Toast 服务 |
| 30 | `Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs` | 95 | `IsAutoLogin` 普通 CLR 属性非 INPC（`[ObservableProperty]`） | 源生成器属性 |
| 31 | `Modules/LYBT.Desktop.Auth/Views/ServerConfigView.xaml:98` / `FirstRunSetupView.xaml:112` | 98/112 | 硬编码 `#E65100`（有 TcmOrangeBrush） | 主题化 |
| 32 | `Shell/Views/MainWindow.xaml` | 48 | `MainSnackbar` 有 `x:Name` 但无 code-behind 引用 | 去命名 |
| 33 | `Shell/Dialogs/Views/ConfirmationDialog.xaml` | — | 缺 `AutoWireViewModel`（Message/Input 有，parity 不一致） | 补 AutoWire 或确认 VM 解析 |
| 34 | `Shell/ViewModels/SideNavViewModel.cs` | — | `LogoutAsync` 绕过进行中医案离开守卫；`IsDarkMode` 未按当前主题初始化 | 复用 MainWindow 登出守卫；构造时读主题 |
| 35 | `Core/LYBT.Desktop.Controls/Controls/HerbItem/HerbItemControl.xaml` | 8/177 | 未用 `xmlns:local`；选中项硬编码 `Black` 前景 | 删 xmlns；主题化 |

## 统计

- 审查 VM 文件：**65**（含 Shell/Auth/Catalog/Patients/Users/Registrations/MedicalCase/Admin/Sysadmin/Clinical/Receptionist 模块 + Infrastructure 基类 + 共享控件 VM）
- 审查 View/XAML 文件：**82**（Shell Views+Dialogs + 8 模块 Views/Controls + Core 共享 Controls + 4 打印模板）
- P0 问题：**5** 个（1 崩溃 + 4 断链/失效资源）
- P1 问题：**12** 个
- P2 问题：**18** 个

## 架构结论（正面）

- MVVM 分层整体健康：VM 不触碰 View 元素；View code-behind 仅依赖属性/纯 UI 事件；共享控件（MasterDetailLayout/SearchBox/UnifiedPaginationBar/DataGridToolbar）依赖属性齐全
- DI 全部接口化（IViewModelServices / IMasterDetailServices / IServiceEventCallback / ICommandHost 缝）
- 基类 Dispose 链正确：`NavigableViewModelBase` 的 `EventSubscriptionManager`（line 67）在基类 Dispose 自动清理 `Events.Subscribe` 订阅（核验：`ClinicalWorkspaceViewModel:106` 的 CacheEvents 订阅由基类兜底，**非泄漏**）
- 导航参数链（MedicalCaseId/CurrentPatient/WorkspaceModeKey/InitialEditStateKey）跨 PatientSelection/PendingQueue/CardReader/Workspace `OnNavigatedTo` 一致

## 备注

- 本次为只读审查，未修改任何代码；P0 项建议优先排期修复（尤其 #1 BaseDetailContainer 递归崩溃与 #2/#3/#4 断链绑定）
- 发现「死控件」2 处（BreadcrumbBar 全局空渲染、WorkflowStepIndicator 步骤样式未应用）——功能声明与实现脱节的典型，建议核对需求
