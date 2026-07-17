# LYBT.Desktop.Clinical

医生角色模块 -- 提供完整的诊疗工作流，从患者选择到医案编辑的全链路支持。

## 项目定位

Clinical 模块是 Doctor 角色的核心工作空间，包含诊疗主页、患者选择（含读卡器和待诊队列）、医案工作区（问诊编辑 + 处方编辑 + 编辑状态机）三大核心区域。采用 5 步工作流设计，支持医案暂存/恢复和多患者切换。

## 目录结构

```
LYBT.Desktop.Clinical/
├── ClinicalModule.cs                    # Prism IModule 入口
├── ViewModels/
│   ├── ClinicalHomeViewModel.cs         # 医生主页（今日统计 + 6 导航卡片）
│   ├── ClinicalWorkspaceViewModel.cs    # 左右分栏工作区
│   ├── PatientSelectionViewModel.cs     # 患者选择（列表 + 读卡器 + 待诊队列）
│   ├── PatientSelectionWorkspaceContext.cs
│   └── Workspace/
│       ├── MedicalCaseWorkspaceViewModel.cs  # 医案工作区（复合 VM）
│       ├── PendingQueueViewModel.cs          # 待诊队列
│       └── CardReaderViewModel.cs            # 身份证读卡器
└── Views/
    ├── ClinicalHomeView.xaml(.cs)
    ├── ClinicalWorkspaceView.xaml(.cs)
    ├── PatientSelectionView.xaml(.cs)
    ├── MedicalCaseWorkspaceView.xaml(.cs)
    ├── HerbManagementView.xaml(.cs)           # 薄包装
    ├── FormulaManagementView.xaml(.cs)        # 薄包装
    ├── PatientManagementView.xaml(.cs)        # 薄包装
    ├── MedicalCaseManagementView.xaml(.cs)    # 薄包装
    └── PendingQueueView.xaml(.cs)
```

## 核心组件

### ClinicalModule

**设计依据**: Prism IModule 标准入口，依赖 Patients 和 MedicalCase 业务模块。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| `ClinicalHomeViewModel` | ViewModel | 医生主页 VM |
| `PatientSelectionViewModel` | ViewModel | 患者选择 VM |
| `MedicalCaseWorkspaceViewModel` | ViewModel | 医案工作区 VM |
| `ClinicalHomeView` | Navigation | 医生主页 |
| `PatientSelectionView` | Navigation | 患者选择页 |
| `MedicalCaseWorkspaceView` | Navigation | 医案工作区 |
| `ClinicalWorkspaceView` | Navigation | 左右分栏工作区 |
| `HerbManagementView` | Navigation | 药材管理（薄包装） |
| `FormulaManagementView` | Navigation | 验方管理（薄包装） |
| `PatientManagementView` | Navigation | 患者管理（薄包装） |
| `MedicalCaseManagementView` | Navigation | 医案管理（薄包装） |

**模块依赖**: `PatientsModule`, `MedicalCaseModule`

### ClinicalHomeViewModel

**设计依据**: NavigableViewModelBase 子类，今日统计 + 6 个功能导航卡片。

| 属性 | 说明 |
|------|------|
| `CurrentUserName` | 当前医生姓名 |
| `TodayConsultationCount` | 今日问诊数 |
| `TodayPatientCount` | 今日患者数 |

| 命令 | 导航目标 |
|------|----------|
| `StartMedicalCase` | 开始诊疗（患者选择） |
| `NavigateToPatientManagement` | 患者管理 |
| `NavigateToMedicalCaseQuery` | 医案查询 |
| `NavigateToHerbLibrary` | 药材库 |
| `NavigateToFormulaLibrary` | 验方库 |
| `NavigateToRegistrationQueue` | 挂号队列 |

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

```
ClinicalModule
├── LYBT.Desktop.Contracts    # IAuthenticationService, INavigationCoordinator, IMedicalCaseService
├── LYBT.Desktop.Infrastructure  # NavigableViewModelBase, ChildViewModelBase, ViewNames
├── LYBT.Desktop.Patients     # IPatientService, IPatientCardReaderIntegration
├── LYBT.Desktop.MedicalCase  # IMedicalCaseService, EditModeStateMachine, WorkspaceMode/EditState
├── LYBT.Desktop.CardReader   # ICardReaderService
└── LYBT.Shared.Models        # DTOs
```

## 设计决策

1. **复合 VM 模式**: MedicalCaseWorkspaceViewModel 不是单一职责，而是整合问诊、处方、命令的复合体，通过子组件委托实现关注点分离
2. **EditModeStateMachine 转发表**: 使用 `Dictionary<(State, Event), State>` 驱动状态转换，线程安全锁 + 锁外事件触发防止死锁
3. **ChildViewModelBase**: PendingQueueViewModel 和 CardReaderViewModel 继承此基类，通过 `IWorkspaceHost` 接口与父 VM 通信
4. **薄包装 View**: 药材/验方/患者/医案管理视图复用业务模块的 MasterDetail Control

## 已知陷阱

- **编辑状态机重入**: `EditModeStateMachine` 有重入保护（`_isProcessingTransition`），重复触发 Save/Leave 会被静默忽略
- **PendingQueue 自动暂存**: 切换患者时如果 `SuspendCurrentCase` 委托为 null，编辑内容可能丢失（仅打印警告日志）
- **CardReader 事件订阅**: 必须在 `Dispose()` 中取消 `ConnectionStateChanged`/`CardReadCompleted`/`CardReadError` 事件订阅，否则内存泄漏
- **ClinicalWorkspace 缓存**: 5 分钟缓存可能导致数据过期，需要手动刷新
- **MedicalCaseWorkspace 导航参数**: 必须同时传 `MedicalCaseId`、`CurrentPatient`、`WorkspaceMode`、`InitialEditState`
