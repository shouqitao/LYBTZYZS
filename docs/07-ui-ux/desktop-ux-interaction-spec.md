# Desktop 交互规范（Interaction Spec）

> 版本: v1.0 | 日期: 2026-09-13 | 依据: 代码实际（src/Client/Desktop）+ 事实清单口径

> **计数口径（全文统一）**：View **30** / Control **33** / Dialog **7**（`*/Dialogs/**/*.xaml`，经 `RegisterDialog` 注册）/ Root 1（`Shell/App.xaml`）；ViewModel **55**；XAML 合计 **82** = 视图 71 + 资源/模板 11。
> 本文只描述**代码中可证**的交互契约；无法从代码确认者标 `[待确认]`。

## 目录

1. [键盘交互](#1-键盘交互)
2. [焦点与 Tab 顺序](#2-焦点与-tab-顺序)
3. [鼠标交互](#3-鼠标交互)
4. [对话框交互契约](#4-对话框交互契约)
5. [工具栏统一顺序](#5-工具栏统一顺序)
6. [文案与空态口径](#6-文案与空态口径)
7. [事实来源](#7-事实来源)

---

## 1. 键盘交互

### 1.1 窗口级（`Shell/Views/MainWindow.xaml` 的 `Window.InputBindings`，共 8 条）

| 快捷键 | 命令（绑定） | 实际行为（`Shell/Services/MenuManager.cs`） |
|--------|------------|------------------------------------------|
| `Ctrl+N` | `QuickAddPatientCommand` | 导航 `PatientManagementView` 并携带 `Action = "AddNew"`，成功后 Toast「已切换到患者管理页面，准备添加新患者」 |
| `Ctrl+Shift+C` | `QuickStartMedicalCaseCommand` | 导航 `MedicalCaseWorkspaceView`，Toast「已开始诊疗流程，请选择患者」 |
| `F1` | `ShowHelpCommand` | Toast 展示快捷键说明（Ctrl+N / Ctrl+Shift+C / F1 / Alt+F4 / Ctrl+,） |
| `Ctrl+,`（`Key="OemComma"`） | `ShowSettingsCommand` | Toast「用户设置功能将在未来版本中实现」（**占位实现**；个人资料入口是顶栏按钮 `EditProfileCommand`） |
| `Ctrl+M` | `ToggleSidebarCommand` | `ISidebarStateManager.Toggle()`——与侧栏汉堡按钮同源 |
| `Alt+Left` | `NavigateBackCommand` | `INavigationCoordinator.NavigateBack()`，`CanExecute = CanNavigateBack` |
| `Alt+Right` | `NavigateForwardCommand` | `INavigationCoordinator.NavigateForward()`，`CanExecute = CanNavigateForward` |
| `Alt+Home` | `NavigateToHomeCommand` | `NavigateToHome()`（角色首页） |

### 1.2 详情容器级（`Core/LYBT.Desktop.Controls/Controls/BaseDetailContainer.xaml` 的 `UserControl.InputBindings`，共 4 条）

| 快捷键 | 命令 | 说明 |
|--------|------|------|
| `Ctrl+S` | `SaveCommand` | 详情保存 |
| `Ctrl+P` | `PrintCommand` | 详情打印（全部经 `RelativeSource AncestorType=UserControl` 向上取 VM 命令） |
| `Esc` | `CancelCommand` | 取消 / 返回 |
| `F1` | `HelpCommand` | 帮助 |

### 1.3 控件级

| 位置 | 快捷键 | 命令 |
|------|--------|------|
| `ReceptionistHomeView` | `Enter`（`Return`） | `SearchPatientCommand` |

### 1.4 未实现的键位口径

- 对话框 `Esc` 关闭 / `Enter` 确认：7 个 Dialog XAML 与通用对话框均**未设置** `IsCancel` / `IsDefault`（全仓 grep 无匹配）→ 需分别按键位 `[待确认]`。
- `Ctrl+F`（搜索）、`Ctrl+S`（页面级保存）、`F5`（刷新）在窗口级**未绑定**；既有 `desktop-ui-design-guide.md` §7 表格中的这些键位与代码不一致，以本文为准。
- 页面级保存/打印/取消由 `BaseDetailContainer` 的 `Ctrl+S` / `Ctrl+P` / `Esc` 提供，仅作用于挂载了该容器的详情页。

---

## 2. 焦点与 Tab 顺序

- **显式 `TabIndex` 仅存在于表单类控件**，用于固定字段遍历顺序：

| 控件 | TabIndex 取值范围 | 备注 |
|------|-----------------|------|
| `FormulaEditControl.xaml` | 1–6 | 名称/分类/性味/功效/用法/备注 |
| `HerbEditControl.xaml` | 1–10 | 含下拉框（状态）与价格字段 |
| `PatientEditControl.xaml` | 1–… | 姓名/拼音码/性别/… |
| `UserEditControl.xaml` | 1–… | 用户名/姓名/角色/… |
| `MedicalCaseEditControl.xaml` | 5–15 | 编辑区内操作按钮与处方明细字段 |

- **Shell 层（`HeaderControl` / `SideNavControl` / `FooterControl` / `AppShell`）未设置 `TabIndex`**，焦点顺序按 WPF 可视树默认顺序。
- **首焦点未显式指定**：全仓未使用 `FocusManager.FocusedElement` / `KeyboardNavigation.*` 属性；进入页面后焦点的具体落点 `[待确认]`。
- 侧栏展开态与收拢态分别使用 240 / 64 宽度（`ISidebarStateManager.SidebarWidth`），`IsNavTextVisible` 控制文字可见性——收拢时导航项仅图标，键盘遍历项数不变。
- 建议约定（尚未在代码中强制）`[待确认]`：对话框打开后焦点落在首个输入控件；列表页进入后焦点落在搜索框。当前代码无此保证。

---

## 3. 鼠标交互

### 3.1 单击 / 选中

| 行为 | 范围 | 证据 |
|------|------|------|
| 单行选中（`SelectionMode="Single"` + `SelectionUnit="FullRow"`） | `FormulaMasterDetailControl`、`HerbMasterDetailControl`、`MedicalCaseMasterDetailControl`、`PatientMasterDetailControl`、`PatientSelectionControl`、`UserMasterDetailControl` | 各控件 DataGrid 属性 |
| 单行选中（整行只读） | `RegistrationListView`（`SelectionMode="Single"`，`IsReadOnly="True"`） | `Views/RegistrationListView.xaml` |
| 勾选多选（批量操作前提） | 上述 5 个主从列表：`behaviors:DataGridSelectionBehavior.ShowCheckBoxColumn="True"` | `DataGridSelectionBehavior` 附加属性 |
| 多选驱动批量命令 | `BatchEnableCommand` / `BatchDisableCommand` / `BatchDeleteCommand`（`CanExecute = HasSelection`） | `MasterDetailViewModelBase` / `MasterDetailCommandGroup` |

### 3.2 双击

| 场景 | 行为 |
|------|------|
| `ClinicalWorkspaceView` 左栏患者列表双击 | 等同「开始看诊」：`PatientSelectionControl_PatientDoubleClicked` → `ClinicalWorkspaceViewModel.StartConsultationCommand`（`CanExecute` = 已选患者） |
| `PatientSelectionControl` 内部双击 | `PatientDataGrid_MouseDoubleClick` 读取 `SelectedPatient` 后触发 `PatientDoubleClicked` 事件 |
| 行双击与勾选框冲突 | `DataGridSelectionBehavior` 在行模板上挂 `PreviewMouseDoubleClick` 并置 `Handled = true`，避免双击干扰勾选 |

### 3.3 右键菜单

| 位置 | 菜单内容（命令） |
|------|----------------|
| `PatientMasterDetailControl` | 编辑 / 医案 / 危险操作分组（`EditCommand` 等，`ContextMenu` 经 `PlacementTarget.DataContext` 取 VM） |
| `FormulaMasterDetailControl` | 查看与编辑 / 验方管理 / 危险操作（含 `CopyFormulaCommand`、`ToggleStatusCommand`、`RestoreCommand`） |
| `HerbMasterDetailControl` | 编辑 / 管理 / 危险操作分组 |
| `UserMasterDetailControl` | 编辑 / 用户管理（`ResetPasswordCommand`、`ToggleUserStatusCommand`、`RestoreCommand`） |
| `MedicalCaseMasterDetailControl` | 查看详情（`EditCommand`） |
| `HerbItemControl` | 编辑模式下「删除」菜单项；`ContextMenuOpening` 中守卫 `IsEditMode`，非编辑态不开菜单 |

### 3.4 拖拽

- **拖拽排序、拖拽移动未实现**：全仓 grep `DragDrop` / `AllowDrop` / `DoDragDrop` / `Drop=` 均无匹配。
- 列级拖拽：仅 `PatientSelectionControl.xaml` 显式设置 `CanUserReorderColumns="True"`、`CanUserResizeColumns="True"`、`CanUserSortColumns="True"`；其余 DataGrid 未显式设置（WPF 默认值同样允许列重排/排序）→ 属框架默认能力，非设计约定。

---

## 4. 对话框交互契约

### 4.1 生命周期（`Core/LYBT.Desktop.Infrastructure/ViewModels/Base/DialogViewModelBase.cs`）

```
IDialogService.ShowDialog(name, params, callback)
        │
        ▼
OnDialogOpened(parameters)  ──log──►  OnDialogOpenedCore(parameters)   ← 子类重写点（取参/加载）
        │
        │  用户交互：ConfirmCommand / CancelCommand
        ▼
Confirm()  →  CanConfirm() 守卫（!IsBusy && !IsLoading）
Cancel()   →  直接关闭
        │
        ▼
CloseDialog(result) / CloseDialog(parameters, result)
        │  RequestClose?.Invoke(new DialogResult(...))
        ▼
CanCloseDialog()（默认 true）→ OnDialogClosed() ──log──► OnDialogClosedCore()  ← 子类重写点
        │
        ▼
回调 callback(IDialogResult)  ← 调用方按 ButtonResult 分支
```

| 契约项 | 代码事实 |
|--------|---------|
| 基类 | `DialogViewModelBase : NavigableViewModelBase, IDialogAware` |
| 可观察属性 | `Title`（[ObservableProperty]）；忙碌态复用基类 `IsLoading` / `IsBusy` |
| 必填参数 | `GetDialogParameter<T>(parameters, key)` —— 缺失时抛 `ArgumentException` |
| 可选参数 | `GetDialogParameter<T>(parameters, key, defaultValue)` |
| 确认守卫 | `CanConfirm() => !IsBusy && !IsLoading`；`IsLoading`/`IsBusy` 变更时 `ConfirmCommand.NotifyCanExecuteChanged()` |
| 默认按钮结果 | `Confirm` → `ButtonResult.OK`；`Cancel` → `ButtonResult.Cancel` |

### 4.2 已注册对话框清单（`RegisterDialog` 共 9 处）

| # | 对话框（View） | ViewModel | 注册处 | 用途 |
|---|---------------|-----------|--------|------|
| 1 | `ServerConfigView` | `ServerConfigViewModel` | `Auth/AuthenticationModule.cs` | 服务器配置（对话框形态，物理位于 `Views/`） |
| 2 | `FirstRunSetupView` | `FirstRunSetupViewModel` | `Auth/AuthenticationModule.cs` | 首次运行配置向导（同上） |
| 3 | `FormulaImportDialog` | `FormulaImportDialogViewModel` | `MedicalCase/MedicalCaseModule.cs` | 验方导入 |
| 4 | `HistoryCopyDialog` | `HistoryCopyDialogViewModel` | `MedicalCase/MedicalCaseModule.cs` | 历史医案复制 |
| 5 | `UnsavedChangesDialog` | `UnsavedChangesDialogViewModel` | `MedicalCase/MedicalCaseModule.cs` | 未保存更改（保存/不保存/取消） |
| 6 | `RegistrationCreateDialog` | `RegistrationCreateDialogViewModel` | `Registrations/RegistrationModule.cs` | 新建挂号 |
| 7 | `ConfirmationDialog` | `ConfirmationDialogViewModel` | `Shell/App.xaml.cs` | 通用确认 |
| 8 | `MessageDialog` | `MessageDialogViewModel` | `Shell/App.xaml.cs` | 通用消息（type = success/error/warning） |
| 9 | `InputDialog` | `InputDialogViewModel` | `Shell/App.xaml.cs` | 通用输入 |

> 计数口径：`Dialog` 分类（`*/Dialogs/**`）共 7 个；上表 9 处注册中包含 2 个 `Views/` 路径的对话框。`DialogViewModelBase` 的具体派生 VM 共 9 个：3 个 Shell 通用 + 4 个业务（医案 3 + 挂号 1）+ 2 个 Auth（经抽象中间层 `ConnectionTestViewModelBase` 派生）。

### 4.3 尺寸基线（来自对话框内容根元素，非 Window 属性）

- **1100×680**：`FormulaImportDialog`、`HistoryCopyDialog`
- **固定宽度**：`RegistrationCreateDialog` 480、`FirstRunSetupView` 460、`ServerConfigView` 440
- **最小尺寸**：`ConfirmationDialog` 400×200、`InputDialog` 400×180、`UnsavedChangesDialog` 380×160、`MessageDialog` 350×150

> `WindowStartupLocation` / `ResizeMode` 由 Prism 对话框宿主与 MaterialDesign `DialogHost` 决定，XAML 未显式设置 → 模态居中与否 `[待确认]`。

### 4.4 通用对话框服务的并行实现（写文档/新增页面时需择一）

| 服务 | 实现方式 | 典型场景 |
|------|---------|---------|
| `ICommonDialogService` | `System.Windows.MessageBox`（Warning/Error/YesNo/YesNoCancel） | Shell 层警告与确认 |
| `IDialogManager` | Prism `IDialogService.ShowDialog("MessageDialog"/"ConfirmationDialog")` | 风格统一的成功/失败/确认 |
| `IUserNotificationService` | `MessageBox`（经 `ClientErrorMessageMapper` 转文案） | 菜单命令的失败提示 |
| `ShellDialogHelper` | 成功/错误 → `IToastService`；警告/确认 → `ICommonDialogService` | Shell 聚合入口 |

> 不存在名为 `DialogService` 的类，也不存在 `DialogServiceExtensions`（与部分既有文档描述不符）。

---

## 5. 工具栏统一顺序

### 5.1 代码实际（`Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml`）

工具栏为**单行两栏**布局，另有**独立一行的搜索框**：

```
┌ 第 1 行：DataGridToolbar ──────────────────────────────────────────────────┐
│ [新增] [刷新] [AdditionalContent 插槽…] [导出]        [批量启用][批量禁用][批量删除] │
└───────────────────────────────────────────────────────────────────────────┘
┌ 第 2 行：SearchBox（Grid.Row="1"，Height=40，Margin="16,12"）───────────────┐
│ [搜索框：占位符按模块定制]                                                   │
└───────────────────────────────────────────────────────────────────────────┘
```

- 左组固定顺序：**新增 → 刷新 → AdditionalContent 插槽 → 导出**（`导出` 明确注释为「放在导入等附加按钮后面」）。
- 右组固定顺序：**批量启用 → 批量禁用 → 批量删除**（`批量删除` 使用 `ValidationErrorBrush` 红色强调）。
- 插槽内容（各模块）：验方 `[模板][导入][待校验][编辑]`；药材 `[模板][导入]`；患者 `[模板][导入][编辑]`；用户 `[编辑][更多操作▾]`；挂号 `[接诊]`（`IsDoctor`）/`[取消挂号]`（`IsReceptionist`）。
- 搜索框位置：`FormulaMasterDetailControl`、`HerbMasterDetailControl`、`MedicalCaseMasterDetailControl`、`PatientMasterDetailControl`、`UserMasterDetailControl` 均在工具栏下一行（验方在「校验模式」下隐藏，由提示条替代）。

### 5.2 文档口径与差异

| 口径 | 排列 |
|------|------|
| 本文档规范（与需求描述一致） | `[搜索] | [新建][导入][导出] | [批量删除]` 单行 |
| 代码实际 | 搜索框**独立成行**位于工具栏下方；批量操作为 3 个按钮 |
| 结论 | 以**代码实际**为准；如后续要收敛为单行规范，需修改 `DataGridToolbar` 与 5 个主从控件 → `[待确认]`（当前无此改动计划） |

---

## 6. 文案与空态口径

### 6.1 空态（`Core/LYBT.Desktop.Controls/Controls/EmptyState.xaml`）

| 属性 | 默认/用法 |
|------|----------|
| `Title` | 默认「暂无数据」 |
| `Subtitle` | 说明文字 |
| `Icon` | `Geometry` 图标 |
| `ActionText` / `ActionCommand` | 行动按钮 |

已使用位置（7 处）：
`FormulaMasterDetailControl`（「请选择验方」+ 新增验方）、`HerbMasterDetailControl`（「请选择药材」+ 新增药材）、`MedicalCaseMasterDetailControl`（「请选择医案」）、`PatientMasterDetailControl`（「请选择患者」+ 新增患者）、`PatientSelectionControl`（「请选择患者」+ 新建患者）、`RegistrationListView`（「暂无等待中的挂号记录」/「新建挂号开始工作」，`Visibility=IsQueueEmpty`）、`UserMasterDetailControl`（「请选择用户」+ 新增用户）。

> 主从布局中，上述空态均置于 `MasterDetailLayout.EmptyContent`；`HasSelection` 取反控制其可见性（`ShowDetailPanel = HasSelection || IsEditMode`）。

### 6.2 按钮与菜单文案

| 位置 | 文案 |
|------|------|
| 工具栏 | 新增、刷新、导出、批量启用、批量禁用、批量删除 |
| 业务操作 | 接诊、取消挂号、打印处方单、导出PDF、编辑、切换状态、恢复、重置密码、更多操作、显示已禁用、清除筛选、返回全部验方 |
| Shell | 侧栏「功能导航」分组标题与「退出」；顶栏诊所名「凌隐宝堂中医诊所」、用户区 ToolTip「个人资料 (Ctrl+,)」 |

> 品牌口径统一为**「凌隐宝堂中医诊所」**（客户端标题为「凌隐宝堂中医诊所管理系统」）。

### 6.3 状态提示口径

| 通道 | 时长/形态 | 详见 |
|------|----------|------|
| Toast（`IToastService`） | 默认 3000ms；`ShowError` 为 4000ms；`ToastType` ∈ {Info, Success, Warning, Error} | [desktop-ux-error-handling.md](./desktop-ux-error-handling.md) |
| 加载遮罩（`LoadingOverlay`） | 200ms 延迟显示；不确定态进度条 200×4 | [desktop-ux-loading-states.md](./desktop-ux-loading-states.md) |
| 字段校验 | `ValidatesOnNotifyDataErrors` + `ValidationErrorBrush`（`#B00020`） | [desktop-ux-error-handling.md](./desktop-ux-error-handling.md) |

---

## 7. 事实来源

- 键盘：`Shell/Views/MainWindow.xaml`、`Core/LYBT.Desktop.Controls/Controls/BaseDetailContainer.xaml`、`Shell/Services/MenuManager.cs`、`Shell/ViewModels/MainWindowViewModel.cs`、`Roles/LYBT.Desktop.Clinical/Receptionist/Views/ReceptionistHomeView.xaml`。
- 焦点/Tab：各 `*/Controls/*EditControl.xaml`、`Shell/Views/*.xaml`。
- 鼠标：`Core/LYBT.Desktop.Infrastructure/Behaviors/DataGridSelectionBehavior.cs`、5 个 `*MasterDetailControl.xaml`、`Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml(.cs)`、`Modules/LYBT.Desktop.Patients/Controls/PatientSelectionControl.xaml(.cs)`、`Modules/LYBT.Desktop.Catalog/Controls/HerbItem/HerbItemControl.xaml(.cs)`。
- 对话框：`Core/LYBT.Desktop.Infrastructure/ViewModels/Base/DialogViewModelBase.cs`、4 个模块 `*Module.cs` / `Shell/App.xaml.cs`、各 Dialog XAML。
- 工具栏：`Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml`、5 个主从控件与 `RegistrationListView.xaml`。
- 交叉文档：[desktop-layout-framework.md](./desktop-layout-framework.md)、[desktop-design-spec.md](./desktop-design-spec.md)、[desktop-ux-user-journeys.md](./desktop-ux-user-journeys.md)。
