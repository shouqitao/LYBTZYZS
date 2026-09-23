# Desktop 二级界面设计清单

> **日期**: 2026-09-13（首版）| **更新**: 2026-09-23（B-07：`FirstRunSetupView` → `InitializationWizardView` 更名同步；收尾批次：`ConfigExportImportView` + `SessionTimeoutWarningDialog` 已建）| **基准**: [desktop-design-tokens.md](./desktop-design-tokens.md)
> **范围**: 一级页面 + 二级界面 + 需求驱动新增页面；**逐项判定见 §F（View 30 + Dialog 7 全量）**
> **计数口径**: View **30** / Control **33** / Dialog **7** / ViewModel **55**（XAML 合计 82 = 视图 71 + 资源模板 11）——定义见 §G

---

## A. 一级页面（原「缺失」清单，2026-09-13 复核）

| # | 页面 | 代码状态 | 需求来源 | 设计稿 |
|---|------|---------|---------|--------|
| A1 | 患者选择 PatientSelectionView | ✅ 已注册使用 | US-REG-001/002 | `../../designs/patient-selection.pen` |
| A2 | 待诊队列 PendingQueueView | ❌ **死代码已确认并删除（2026-08-29）**——队列 UI 内嵌 PatientSelectionView，`PendingQueueViewModel` 保留 | US-REG-004/005 | 不需要独立设计稿 |
| A3 | 临床首页 ClinicalHomeView | ⚠️ **半死已确认（2026-08-29）**——仅 `RoleRegistry.DefaultHomeView` fallback；Doctor 真实 Home=`ClinicalWorkspaceView` | US-SHELL-003 | 低优先级（fallback） |
| A4 | 前台首页 ReceptionistHomeView | ✅ 存在 | US-SHELL-003 | `../../designs/receptionist-home.pen` |
| A5 | 审计日志 AuditLogView | ✅ 存在（医案域，非安全审计） | US-MC-017 | `../../designs/audit-log.pen` |

> 审计报告：[desktop-view-design-audit-2026-08-29.md](../compose/reports/desktop-view-design-audit-2026-08-29.md)

## B. 对话框二级界面（代码已存在，必须设计）

| # | 界面 | 尺寸建议 | 内容 | 设计稿 |
|---|------|---------|------|--------|
| B1 | 新建挂号 RegistrationCreateDialog | 560×640 | 患者搜索(autocomplete)+医生选择+挂号类型+费用 | `../../designs/dialog-registration-create.pen` |
| B2 | 验方导入 FormulaImportDialog | 560×480 | 选择验方+药材预览+剂量调整 | `../../designs/dialog-formula-import.pen` |
| B3 | 历史复制 HistoryCopyDialog | 640×560 | 历史医案列表+预览+复制选项 | `../../designs/dialog-history-copy.pen` |
| B4 | 未保存更改 UnsavedChangesDialog | 420×240 | 警告图标+保存/不保存/取消 | ⚠️ 无设计稿 `[待确认]` |
| B5 | 通用对话框（确认/输入/消息合并设计） | 420×280 | 三种形态展示于一图 | `../../designs/dialog-common.pen` |
| B6 | 会话超时预警 SessionTimeoutWarningDialog | 420×240 | 剩余时间 mm:ss 倒计时 + 「续期」/「退出」 | ⚠️ 无设计稿 `[待确认]` |

> 说明：Shell 三个通用对话框为 `ConfirmationDialog` / `InputDialog` / `MessageDialog`（均在 `Shell/Dialogs/Views/`）；会话超时预警对话框为 `SessionTimeoutWarningDialog`（`Shell/Dialogs/Views/`，2026-09-23 收尾批次落地，US-AUTH-014）。

## C. 编辑表单二级界面（代码已存在，必须设计）

| # | 界面 | 形态 | 字段 | 设计稿 |
|---|------|------|------|--------|
| C1 | 用户编辑 UserEditControl | 表单页 | 用户名/姓名/角色/邮箱/电话/密码 | `../../designs/form-user-edit.pen` |
| C2 | 患者编辑 PatientEditControl | 表单页 | 姓名/性别/年龄/电话/身份证/地址/过敏史 | `../../designs/form-patient-edit.pen` |
| C3 | 药材编辑 HerbEditControl | 表单页 | 名称/拼音/性味/归经/功效/价格/库存 | `../../designs/form-herb-edit.pen` |
| C4 | 验方编辑 FormulaEditControl | 复合表单 | 名称/适应症+药材组成列表(可增删行)+用法用量 | `../../designs/form-formula-edit.pen` |
| C5 | 医案编辑 MedicalCaseEditControl | 工作台子页 | 诊断信息+处方编辑 | `../../designs/form-medical-case-edit.pen` |

## D. 需求驱动但代码未创建的页面

| # | 页面 | 需求来源 | 状态 |
|---|------|---------|------|
| D1 | 安全审计日志查看页 | US-SHELL-014 | ✅ **已建**：`SecurityAuditLogView`（2026-08-29），设计稿 `../../designs/security-audit-log.pen` |
| D2 | 数据导入导出页（JSON） | US-SHELL-016 / 已定案 JSON 格式 | ✅ **已建（2026-09-23 收尾批次）**：`ConfigExportImportView`（`Roles/LYBT.Desktop.Admin/Sysadmin/Views/`，VM `ConfigExportImportViewModel`，`SysadminHomeView` 第 7 个功能卡「配置导入导出」）；业务数据 JSON 导入导出仍由各 MasterDetail 承载；设计稿 `../../designs/data-import-export.pen` |
| D3 | 读卡器诊断面板 | US-CARD 系列 | ✅ **已建（非独立视图）**：面板内嵌于 `SysadminHomeView`，`CardReaderDiagnosticsViewModel` 为其子 VM；设计稿 `../../designs/cardreader-diagnostics.pen` |

## E. 详情查看类（并入主从布局详情面板，不单独出稿）

PatientViewControl / HerbViewControl / FormulaViewControl / UserViewControl
→ 这些渲染在 MasterDetail 右侧详情面板中，已包含在列表页设计稿内。
→ 如需独立展示形态，后续补充。

---

## F. 全量检查清单（View 30 + Dialog 7）

> **判定规则（口径，避免臆测）**
> - **路径存在**：文件存在于 `src/Client/Desktop`。
> - **有 VM**：`✅` = 视图/内嵌控件有可解析的 ViewModel；`⚠️` = 无自有 VM（薄包装，承载于内嵌 Control）或纯组合；`—` = 设计上无 VM。
> - **绑定机制**：AutoWire（Prism 约定）/ 显式映射（`ViewModelLocationProvider.Register`，含 `ShellViewMappings`）/ `RegisterDialog<TView,TVM>` 显式。
> - **空态**：`✅` = 声明 `EmptyState` / `MasterDetailLayout.EmptyContent`；`⚠️` = 未声明；`—` = 形态不适用。
> - **加载态**：`✅` = 绑定 `IsLoading` / `IsBusy` 或使用 `LoadingOverlay`。
> - **错误态**：`✅` = 视图内绑定 `ErrorMessage` / `HasError`；`⚠️` = 视图内未绑定（错误经基类 `NavigableViewModelBase.ShowErrorMessageAsync` → `IToastService` 全局提示）。
> - **键盘可达**：`✅` = 声明 `AccessKey` / `KeyBinding` / `TabIndex` / `IsTabStop` / `KeyboardNavigation`；`⚠️` = 无显式声明（仅 WPF 默认 Tab 顺序）。
> - **状态徽标**：`✅` = 使用 `StatusBadge` 控件。
> - **检查结果**：`✅ 达标` / `⚠️ 待补：<缺项>` / `—`（不适用）。复核时逐列打勾即可。

| # | 界面 | 路径存在 | 有 VM | 绑定机制 | 空态 | 加载态 | 错误态 | 键盘可达 | 状态徽标 | 检查结果 |
|---|------|---------|-------|---------|------|--------|--------|---------|---------|---------|
| 1 | `InitializationWizardView` | ✅ | ✅ | 显式：`RegisterForNavigation<InitializationWizardView, InitializationWizardViewModel>` + `RegisterDialog<InitializationWizardView, InitializationWizardViewModel>`（B-07 双入口） | ⚠️ | ✅ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/错误态/键盘可达/状态徽标（B-07 重写后待复评） |
| 2 | `LoginView` | ✅ | ✅ | AutoWire（约定 → `LoginViewModel`） | ⚠️ | ✅ | ✅ | ✅ | ⚠️ | ⚠️ 待补：空态/状态徽标 |
| 3 | `ServerConfigView` | ✅ | ✅ | 显式：`RegisterDialog<ServerConfigView, ServerConfigViewModel>` | ⚠️ | ✅ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/错误态/键盘可达/状态徽标 |
| 4 | `ReportsHomeView` | ✅ | ✅ | AutoWire + 显式映射（`ReportsModule`） | ⚠️ | ✅ | ✅ | ⚠️ | ⚠️ | ⚠️ 待补：空态/键盘可达/状态徽标 |
| 5 | `AuditLogView` | ✅ | ✅ | AutoWire（约定 → `AuditLogViewModel`） | ⚠️ | ✅ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/错误态/键盘可达/状态徽标 |
| 6 | `MedicalCaseMasterDetailView` | ✅ | ✅ | AutoWire（约定） | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 达标 |
| 7 | `RegistrationListView` | ✅ | ✅ | AutoWire（约定） | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | ⚠️ 待补：键盘可达/状态徽标 |
| 8 | `BackupManagementView` | ✅ | ✅ | AutoWire（约定） | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/加载态/错误态/键盘可达/状态徽标 |
| 9 | `DeploymentView` | ✅ | ✅ | AutoWire（约定） | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/加载态/错误态/键盘可达/状态徽标 |
| 10 | `LogLevelControlView` | ✅ | ✅ | AutoWire（约定） | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/加载态/错误态/键盘可达/状态徽标 |
| 11 | `SecurityAuditLogView` | ✅ | ✅ | AutoWire（约定） | ✅ | ✅ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：错误态/键盘可达/状态徽标 |
| 12 | `SysadminHomeView` | ✅ | ✅ | AutoWire（约定） | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/加载态/错误态/键盘可达/状态徽标 |
| 13 | `AdminHomeView` | ✅ | ✅ | AutoWire（约定） | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/加载态/错误态/键盘可达/状态徽标 |
| 14 | `SystemSettingsView` | ✅ | ✅ | AutoWire（约定） | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/加载态/错误态/键盘可达/状态徽标 |
| 15 | `UserManagementView` | ✅ | ✅ | 薄包装 → 内嵌 `UserMasterDetailControl`（显式 `ViewModelLocationProvider` → `UserMasterDetailViewModel`） | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 达标 |
| 16 | `ReceptionistHomeView` | ✅ | ✅ | AutoWire + 显式 `Register<ReceptionistHomeViewModel>` | ⚠️ | ⚠️ | ⚠️ | ✅ | ⚠️ | ⚠️ 待补：空态/加载态/错误态/状态徽标 |
| 17 | `ClinicalHomeView` | ✅ | ✅ | AutoWire（约定） | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/加载态/错误态/键盘可达/状态徽标 |
| 18 | `ClinicalWorkspaceView` | ✅ | ✅ | AutoWire + 显式 `Register<ClinicalWorkspaceViewModel>` | ✅ | ⚠️ | ⚠️ | ⚠️ | ✅ | ⚠️ 待补：加载态/错误态/键盘可达 |
| 19 | `FormulaManagementView` | ✅ | ✅ | 薄包装 → `FormulaMasterDetailControl`（显式映射 → `FormulaMasterDetailViewModel`） | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 达标 |
| 20 | `HerbManagementView` | ✅ | ✅ | 薄包装 → `HerbMasterDetailControl`（显式映射 → `HerbMasterDetailViewModel`） | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 达标 |
| 21 | `MedicalCaseManagementView` | ✅ | ✅ | 薄包装 → `MedicalCaseMasterDetailControl`（显式映射 → `MedicalCaseMasterDetailViewModel`） | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 达标 |
| 22 | `MedicalCaseWorkspaceView` | ✅ | ✅ | AutoWire + 显式 `Register<MedicalCaseWorkspaceViewModel>` | ⚠️ | ✅ | ✅ | ✅ | ✅ | ⚠️ 待补：空态 |
| 23 | `PatientManagementView` | ✅ | ✅ | 薄包装 → `PatientMasterDetailControl`（显式映射 → `PatientMasterDetailViewModel`） | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ 达标 |
| 24 | `PatientSelectionView` | ✅ | ✅ | AutoWire（约定） | ✅ | ⚠️ | ⚠️ | ⚠️ | ✅ | ⚠️ 待补：加载态/错误态/键盘可达 |
| 25 | `AccountSettingsView` | ✅ | ✅ | 薄包装 → `AccountSettingsControl`（`ShellViewMappings` 显式映射 → `AccountSettingsViewModel`） | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/加载态/错误态/键盘可达/状态徽标 |
| 26 | `AppShell` | ✅ | — | 无 VM（纯组合；`SidebarWidth` / `SelectedNavItem` 继承宿主 `MainWindowViewModel` 的 DataContext） | — | — | — | — | — | ✅ 达标（纯组合） |
| 27 | `FooterControl` | ✅ | ✅ | AutoWire + `ShellViewMappings` 显式映射 → `FooterViewModel` | — | — | — | — | — | ✅ 达标 |
| 28 | `HeaderControl` | ✅ | ✅ | AutoWire + `ShellViewMappings` 显式映射 → `HeaderViewModel` | — | — | — | — | — | ✅ 达标 |
| 29 | `MainWindow` | ✅ | ✅ | AutoWire + `ShellViewMappings` 显式映射 → `MainWindowViewModel` | — | — | — | ✅ | — | ✅ 达标 |
| 30 | `SideNavControl` | ✅ | ✅ | AutoWire + `ShellViewMappings` 显式映射 → `SideNavViewModel` | — | — | — | — | — | ✅ 达标 |
| 31 | `FormulaImportDialog` | ✅ | ✅ | 显式：`RegisterDialog<FormulaImportDialog, FormulaImportDialogViewModel>` | ⚠️ | ✅ | ⚠️ | ⚠️ | ✅ | ⚠️ 待补：空态/错误态/键盘可达 |
| 32 | `HistoryCopyDialog` | ✅ | ✅ | 显式：`RegisterDialog<HistoryCopyDialog, HistoryCopyDialogViewModel>` | ⚠️ | ✅ | ⚠️ | ⚠️ | ✅ | ⚠️ 待补：空态/错误态/键盘可达 |
| 33 | `UnsavedChangesDialog` | ✅ | ✅ | 显式：`RegisterDialog<UnsavedChangesDialog, UnsavedChangesDialogViewModel>` | — | — | — | ⚠️ | — | ⚠️ 待补：键盘可达 |
| 34 | `RegistrationCreateDialog` | ✅ | ✅ | 显式：`RegisterDialog<RegistrationCreateDialog, RegistrationCreateDialogViewModel>` | ⚠️ | ✅ | ⚠️ | ⚠️ | ⚠️ | ⚠️ 待补：空态/错误态/键盘可达/状态徽标 |
| 35 | `ConfirmationDialog` | ✅ | ✅ | 显式：`RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>` | — | — | — | ⚠️ | — | ⚠️ 待补：键盘可达 |
| 36 | `InputDialog` | ✅ | ✅ | 显式：`RegisterDialog<InputDialog, InputDialogViewModel>` | — | — | — | ⚠️ | — | ⚠️ 待补：键盘可达 |
| 37 | `MessageDialog` | ✅ | ✅ | 显式：`RegisterDialog<MessageDialog, MessageDialogViewModel>` | — | — | — | ⚠️ | — | ⚠️ 待补：键盘可达 |

> **例外说明**：`InitializationWizardView` / `ServerConfigView` 物理位于 `Auth/Views/`（计为 View），但经 `RegisterDialog` 以对话框方式呈现（`InitializationWizardView` 另经 `RegisterForNavigation` 可导航）——两者在「绑定机制」列已注明。
> **幽灵视图澄清**：`CardReaderDiagnosticsView`、`ServerConfigPanelView`、`UnfinishedCaseDialog`、`PrintPreviewDialog`、`PendingQueueView`、`RegistrationCreateView` 在代码中**不存在**——能力由上述真实承载者提供（读卡器诊断 → `SysadminHomeView` 内嵌；服务端配置 → `SysadminHomeView` 内嵌；新建挂号 → `RegistrationCreateDialog`）。**注（2026-09-23 B-07）**：`InitializationWizardView` 已从本清单移除——该视图已真实落地（`Auth/Views/InitializationWizardView.xaml`，5 步初始化向导，US-SHELL-011），旧单屏 `FirstRunSetupView` 已删除。**注（2026-09-23 收尾批次）**：`ConfigExportImportView`（`Admin/Sysadmin/Views/`，US-SHELL-016，见 §D D2）与 `SessionTimeoutWarningDialog`（`Shell/Dialogs/Views/`，US-AUTH-014，见 §B B6）已真实落地，从本名单移除。

---

## G. 数据来源与口径

- **生成/校准日期**: 2026-09-13（机器扫描 `src/Client/Desktop`，排除 `bin/`、`obj/`）。
- **事实源**: 代码实际（`src/Client/Desktop`）。无法从代码判定者标 `[待确认]`；代码中不存在的界面标 `[未建视图]`。
- **计数口径（与 [desktop-ui-requirements.md](./desktop-ui-requirements.md) §九 一致）**:
  - **View = 30**：页面/导航级 XAML（`*/Views/*.xaml`，含角色台、`Reports/Views/`、`Receptionist/Views/`，含 Shell 的 `MainWindow`/`AppShell`/`Header`/`SideNav`/`Footer`/`AccountSettingsView`）。
  - **Control = 33**：内嵌组件（`*/Controls/*.xaml`，含共享设计系统控件与模块内嵌控件）。
  - **Dialog = 7**：`*/Dialogs/**/*.xaml`（经 `RegisterDialog` 注册）。
  - **Root = 1**：`Shell/App.xaml`（应用级资源，非视图）。
  - **ViewModel = 55**：`*ViewModel.cs` 文件数（每文件 1 个 VM 类型）。
  - **XAML 合计 82** = 视图 **71** + 资源/模板 **11**。
- **判定依据（§F 各列）**: 扫描 `*/Views/*.xaml`、`*/Controls/*.xaml`、`*/Dialogs/**/*.xaml` 及被嵌套的控件 XAML（递归至 3 层），匹配 `EmptyState` / `MasterDetailLayout.EmptyContent`、`IsLoading` / `IsBusy` / `LoadingOverlay`、`ErrorMessage` / `HasError`、`AccessKey` / `KeyBinding` / `TabIndex` / `IsTabStop` / `KeyboardNavigation`、`StatusBadge`；VM 绑定按 `RegisterDialog<TView,TVM>`、`ViewModelLocationProvider.Register`、`ShellViewMappings.Mappings` 与 Prism 约定名核定。
- **相关事实源**: [desktop-ui-requirements.md](./desktop-ui-requirements.md)（页面需求与设计稿对应）、[desktop-ui-detailed-design.md](./desktop-ui-detailed-design.md)（详细设计）。

---

## 执行顺序

1. B 类对话框 ×5（立即开始——代码已存在，优先级最高；其中 B4 `UnsavedChangesDialog` 尚无设计稿）
2. C 类编辑表单 ×5（紧随其后）
3. A 类一级页面（`ClinicalHomeView` fallback 与 `MedicalCaseMasterDetailView` 待补稿）
4. D 类需求页面（`data-import-export.pen` 对应的 `[未建视图]` 待补代码）
5. §F 检查结果中 `⚠️ 待补` 项按「错误态 → 键盘可达 → 空态/加载态 → 状态徽标」优先级收敛

预计总量：随 §F `⚠️ 待补` 项收敛，按页面粒度分批
