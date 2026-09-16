# LYBT.Desktop.Clinical

医生角色模块 -- 提供完整的诊疗工作流，从患者选择到医案编辑的全链路支持。

## 项目定位

Clinical 模块是 Doctor 角色的核心工作空间，包含诊疗主页、患者选择（含读卡器和待诊队列）、医案工作区（问诊编辑 + 处方编辑 + 编辑状态机）三大核心区域。采用 5 步工作流设计，支持医案暂存/恢复和多患者切换。

## 目录结构

```
LYBT.Desktop.Clinical/
├── ClinicalModule.cs                    # Prism IModule 入口（依赖 Patients/MedicalCase/Registration/CardReader）
├── ViewModels/
│   ├── ClinicalHomeViewModel.cs         # 诊疗主页（今日统计 + 导航卡片）
│   ├── ClinicalWorkspaceViewModel.cs    # 左右分栏工作区（左侧 PatientSelectionControl + 右侧看诊区）
│   ├── MedicalCaseWorkspaceViewModel.cs # 医案工作区复合 VM（实现 IMedicalCaseWorkspaceContext / IWorkspaceHost / IMedicalCaseDataProvider）
│   ├── PatientSelectionViewModel.cs     # 患者选择（列表 + 读卡器 + 待诊队列）
│   ├── PatientSelectionWorkspaceContext.cs
│   └── Workspace/
│       ├── PendingQueueViewModel.cs     # 待诊队列（ChildViewModelBase）
│       ├── CardReaderViewModel.cs       # 身份证读卡器（ChildViewModelBase）
│       ├── WorkspaceNavigationHandler.cs # 工作区导航处理
│       └── WorkspaceStateManager.cs     # 工作区状态管理
├── Views/
│   ├── ClinicalHomeView.xaml(.cs)
│   ├── ClinicalWorkspaceView.xaml(.cs)
│   ├── PatientSelectionView.xaml(.cs)
│   ├── MedicalCaseWorkspaceView.xaml(.cs)
│   ├── HerbManagementView.xaml(.cs)           # 薄包装，复用 Catalog 模块 Control
│   ├── FormulaManagementView.xaml(.cs)        # 薄包装，复用 Catalog 模块 Control
│   ├── PatientManagementView.xaml(.cs)        # 薄包装，复用 Patients 模块 Control
│   └── MedicalCaseManagementView.xaml(.cs)    # 薄包装，复用 MedicalCase 模块 Control
└── Receptionist/                        # 前台角色台（原 Receptionist 模块，并入 Clinical）
    ├── Views/ReceptionistHomeView.xaml(.cs)
    └── ViewModels/ReceptionistHomeViewModel.cs
```

> `PendingQueueView.xaml` 已于 2026-08-29 删除（未导航注册的死页）。待诊队列 UI 内嵌在 `PatientSelectionView`，由 `PendingQueueViewModel` 驱动。

## 视图 / ViewModel 清单

**计数口径**：View = 页面/导航级 XAML（`*/Views/*.xaml`，含 `Receptionist/Views/`）；Control = 内嵌组件（本模块无 `Controls/`）；Dialog = `*/Dialogs/**/*.xaml`（本模块无）；ViewModel 按「每文件 1 个 VM 类型」计。

| 类别 | 数量 | 明细 |
|------|------|------|
| View | 9 | `ClinicalHomeView`、`ClinicalWorkspaceView`、`PatientSelectionView`、`MedicalCaseWorkspaceView`、`HerbManagementView`、`FormulaManagementView`、`PatientManagementView`、`MedicalCaseManagementView`、`Receptionist/Views/ReceptionistHomeView` |
| Control | 0 | — |
| Dialog | 0 | — |
| ViewModel | 7 | `ClinicalHomeViewModel`、`ClinicalWorkspaceViewModel`、`MedicalCaseWorkspaceViewModel`、`PatientSelectionViewModel`、`Receptionist/ViewModels/ReceptionistHomeViewModel`、`Workspace/` 两个子 VM（`PendingQueueViewModel`/`CardReaderViewModel`）；`WorkspaceNavigationHandler`/`WorkspaceStateManager`/`PatientSelectionWorkspaceContext` 为辅助类，非 VM |

> 全桌面口径：View 30 / Control 33 / Dialog 7 / ViewModel 55（代码实际：`src/Client/Desktop`）。

## 核心组件

### ClinicalModule

**设计依据**: Prism IModule 标准入口，编译期引用 Patients / Catalog / MedicalCase / Registrations 四个业务模块（用于嵌入其 Control）。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| `ClinicalHomeViewModel` | ViewModel | 诊疗主页 VM |
| `PatientSelectionViewModel` | ViewModel | 患者选择 VM |
| `MedicalCaseWorkspaceViewModel` | ViewModel | 医案工作区 VM |
| `ClinicalWorkspaceViewModel` | ViewModel | 左右分栏工作区 VM（P2 修复：此前未注册，靠 DryIoc 具体类型兜底） |
| `Receptionist.ViewModels.ReceptionistHomeViewModel` | ViewModel | 前台工作台 VM |
| `ClinicalHomeView` | Navigation | 诊疗主页 |
| `PatientSelectionView` | Navigation | 患者选择页 |
| `MedicalCaseWorkspaceView` | Navigation | 医案工作区 |
| `ClinicalWorkspaceView` | Navigation | 左右分栏工作区 |
| `HerbManagementView` | Navigation | 药材管理（薄包装） |
| `FormulaManagementView` | Navigation | 验方管理（薄包装） |
| `PatientManagementView` | Navigation | 患者管理（薄包装） |
| `MedicalCaseManagementView` | Navigation | 医案管理（薄包装） |
| `Receptionist.Views.ReceptionistHomeView` | Navigation | 前台工作台主页 |

**模块依赖**: `PatientsModule`, `MedicalCaseModule`, `RegistrationModule`, `CardReaderModule`

> `CardReaderModule` 位于 `LYBT.Desktop.Infrastructure/CardReader`，非独立项目。

### ClinicalHomeViewModel

**设计依据**: NavigableViewModelBase 子类，今日统计 + 功能导航卡片（`[RelayCommand]` 源生成）。

| 属性 | 说明 |
|------|------|
| `CurrentUserName` | 当前医生姓名（默认「医生」） |
| `TodayConsultationCount` | 今日问诊数 |
| `PendingCaseCount` | 待处理医案数 |

| 命令 | 导航目标（`ViewNames`） |
|------|----------|
| `StartMedicalCase` | `ClinicalWorkspace`（临床工作台） |
| `NavigateToPatientManagement` | `PatientManagement` |
| `NavigateToMedicalCaseQuery` | `MedicalCaseManagement` |
| `NavigateToHerbLibrary` | `HerbManagement` |
| `NavigateToFormulaLibrary` | `FormulaManagement` |
| `NavigateToRegistrationQueue` | `RegistrationList` |
| `NavigateToReports` | `ReportsHome` |
| `NavigateToAuditLog` | `AuditLog` |
| `EditProfile` / `ChangePassword` | `AccountSettings`（改密码带 `Tab=Password` 参数） |

> `StartMedicalCase` 导航的是 `ClinicalWorkspaceView`，而 **Doctor 角色的首页也是 `ClinicalWorkspaceView`**（`RoleRegistry` 注册）；`ClinicalHomeView` 仍存在并注册为导航目标，但不是 Doctor 首页。

### ClinicalWorkspaceViewModel

**设计依据**: NavigableViewModelBase 子类，左右分栏布局（左侧患者列表 + 右侧工作区），5 分钟缓存。

- 左侧: `PatientSelectionView`（患者列表 + 读卡器 + 待诊队列）
- 右侧: `MedicalCaseWorkspaceView`（医案编辑工作区）
- 缓存策略: 5 分钟内返回复用已有实例

### MedicalCaseWorkspaceViewModel

**设计依据**: 复合 ViewModel，整合问诊编辑器、处方编辑器和操作命令，内嵌编辑状态机。

| 组件 | 说明 |
|------|------|
| ConsultationEditor | 问诊信息编辑 |
| PrescriptionEditor | 处方编辑 |
| EditModeStateMachine | 编辑状态机（FSM） |
| IWorkspaceHost | 工作区宿主接口（错误提示、忙碌状态） |

**5 步工作流**: 患者选择 → 问诊记录 → 处方编辑 → 保存审核 → 完成归档

**编辑状态机**: `EditModeStateMachine` 实现 `IEditModeStateMachine`，使用转发表驱动状态转换：

| 状态 | 可触发事件 | 目标状态 |
|------|-----------|----------|
| `ReadOnly` | `EnterEdit` | `Editing` |
| `Editing` | `ExitEdit` / `MakeChange` / `Save` | `ReadOnly` / `DirtyEditing` / `Saving` |
| `DirtyEditing` | `ExitEdit` / `Save` / `RequestLeave` | `LeavingConfirming` / `Saving` / `LeavingConfirming` |
| `Saving` | `SaveCompleted` / `SaveFailed` | `ReadOnly` / `TransitionBlocked` |
| `LeavingConfirming` | `LeaveConfirmed` / `LeaveCancelled` / `Save` | `ReadOnly` / `DirtyEditing` / `Saving` |

### PatientSelectionViewModel

**设计依据**: 患者选择复合组件，整合患者列表、读卡器、待诊队列三个子视图。

| 子组件 | 说明 |
|--------|------|
| 患者列表 | 搜索、选择患者 |
| CardReaderViewModel | 身份证读卡器（手动/自动读取） |
| PendingQueueViewModel | 待诊队列（含挂起医案处理） |

- **挂起医案处理**: 切换患者时自动暂存当前医案，选择挂起医案时弹窗询问「继续看诊」或「新建医案」

### PendingQueueViewModel

**设计依据**: ChildViewModelBase 子类，管理待诊队列和患者切换逻辑。

- 自动暂存: 切换患者时，当前编辑模式调用 `SuspendCurrentCase` 委托，查看模式调用 `_medicalCaseService.SuspendAsync()`
- 挂起医案弹窗: 「确定」继续看诊原医案，「取消」关闭原医案并新建
- 注册状态映射: `Waiting→Suspended`, `InProgress→Active`, `Completed→Completed`

### CardReaderViewModel

**设计依据**: ChildViewModelBase 子类，身份证读卡器集成，支持手动/自动读取模式。

| 功能 | 说明 |
|------|------|
| `ManualReadCardAsync` | 手动读取身份证 |
| `ToggleAutoRead` | 切换自动读卡模式（500ms 轮询） |
| `MaskIdNumber` | 身份证号脱敏（保留前 6 后 4） |
| `ProcessPatientFromCardAsync` | 读卡后查找/创建患者并导航 |

- **读卡流程**: 读取身份证 → 查找患者 → 已有则导航到医案，未找到则提示创建

## 依赖关系

### 依赖（编译时 ProjectReference）

| 项目 | 用途 |
|------|------|
| LYBT.Desktop.Foundation | 基础设施（`Application`、`Security`、`ExceptionHandling`） |
| LYBT.Desktop.Infrastructure | `NavigableViewModelBase`、`ChildViewModelBase`、`Constants.ViewNames`、`Extensions`、`CardReader`（`ICardReaderService`） |
| LYBT.Desktop.Contracts | `Services`（`IMedicalCaseService`、`IRegistrationService`、`IPatientService`、`IUserService`、`INavigationCoordinator`）、`Enums`/`Models`（`WorkspaceMode`/`EditState`）、`CrossModule` 搜索提供者 |
| LYBT.Desktop.Catalog | `HerbMasterDetailControl` / `FormulaMasterDetailControl`（薄包装 XAML 嵌入） |
| LYBT.Desktop.Patients | `PatientMasterDetailControl` / `PatientSelectionControl`（薄包装 XAML 嵌入） |
| LYBT.Desktop.MedicalCase | `MedicalCaseMasterDetailControl` / `MedicalCaseEditControl` / `MedicalCaseViewControl`、`EditModeStateMachine`、Workspace 子 VM |
| LYBT.Desktop.Registrations | 挂号队列视图引用（`ViewNames.RegistrationList`） |
| LYBT.Shared.Models | DTOs |

NuGet：`Prism.Core` / `Prism.DryIoc` / `Prism.Wpf`、`CommunityToolkit.Mvvm`、`Microsoft.Extensions.Logging.Abstractions`。

### 被依赖

| 消费方 | 说明 |
|--------|------|
| Shell `App.ConfigureModuleCatalog` | `ClinicalModule` 以 `InitializationMode.OnDemand` 按需加载（Doctor / Receptionist 角色台；导航时由 `ModuleLazyLoader` 触发） |
| `Roles/LYBT.Desktop.Admin` | **无编译期引用**：Admin 的卡片按视图名导航到 `PatientManagement`/`HerbManagement`/`FormulaManagement`/`MedicalCaseManagement`，这些视图由 `ClinicalModule`（`OnDemand`，首次导航时懒加载）注册，运行时解析 |

## 设计决策

1. **复合 VM 模式**: MedicalCaseWorkspaceViewModel 不是单一职责，而是整合问诊、处方、命令的复合体，通过子组件委托实现关注点分离
2. **EditModeStateMachine 转发表**: 使用 `Dictionary<(State, Event), State>` 驱动状态转换，线程安全锁 + 锁外事件触发防止死锁
3. **ChildViewModelBase**: PendingQueueViewModel 和 CardReaderViewModel 继承此基类，通过 `IWorkspaceHost` 接口与父 VM 通信
4. **薄包装 View**: 药材/验方/患者/医案管理视图复用业务模块的 MasterDetail Control（View 在角色台，Control 在业务模块）
5. **前台角色内聚**: 前台工作台（`Receptionist/`）作为子命名空间并入 Clinical 模块，不再有独立 Receptionist 模块

## 已知陷阱

- **编辑状态机重入**: `EditModeStateMachine` 有重入保护（`_isProcessingTransition`），重复触发 Save/Leave 会被静默忽略
- **PendingQueue 自动暂存**: 切换患者时如果 `SuspendCurrentCase` 委托为 null，编辑内容可能丢失（仅打印警告日志）
- **CardReader 事件订阅**: 必须在 `Dispose()` 中取消 `ConnectionStateChanged`/`CardReadCompleted`/`CardReadError` 事件订阅，否则内存泄漏
- **ClinicalWorkspace 缓存**: 5 分钟缓存可能导致数据过期，需要手动刷新
- **MedicalCaseWorkspace 导航参数**: 必须同时传 `MedicalCaseId`、`CurrentPatient`、`WorkspaceMode`、`InitialEditState`
- **Doctor 首页不是 ClinicalHomeView**: `RoleRegistry` 注册 Doctor 首页 = `ClinicalWorkspaceView`；改动「首页跳转」逻辑时以 `RoleRegistry` 为准

---

2026-09-13 docs 复盘：与代码对齐（View/VM 清单、目录树、依赖）
