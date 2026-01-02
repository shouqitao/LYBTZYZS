# OpenSpec Proposal: slim-medicalcase-workspace-viewmodel

**Change ID**: slim-medicalcase-workspace-viewmodel
**Status**: applied (Phase 1-5, 8)
**Priority**: High
**Estimated Effort**: 6h (实际: 4h)
**Created**: 2025-12-30
**Applied**: 2025-12-30
**Updated**: 2026-01-01 (Phase 5 Handler清理完成, Phase 8 待诊队列死代码清理)

---

## 实施记录

### Phase 1: 组件类创建 - 已完成 (2025-12-30)

| 文件 | 状态 | 说明 |
|------|------|------|
| `Components/WorkspaceStatusDisplay.cs` | 创建 | 状态显示组件 (约120行，含兼容重载) |
| `Components/WorkspaceButtonState.cs` | 删除 | 与现有MedicalCaseEditModeStateMachine重复 |

**注意**: 实际枚举值与proposal设计不同
- `EditState`: `Editing`, `ReadOnly` (非 `New`, `Completed`)
- `WorkspaceMode`: `Clinical`, `Management` (非 `HistoricalEdit`)

### Phase 2: 兼容性处理 - 已完成 (2025-12-30)

在WorkspaceStatusDisplay中添加与现有ViewModel调用兼容的重载方法:
- `UpdatePrescriptionStatus(bool isCompleted, string? customText = null)`

### Phase 3-4: ViewModel集成 - 已完成 (2025-12-30)

| 修改 | 说明 |
|------|------|
| 添加`_statusDisplay`字段 | `WorkspaceStatusDisplay`实例 |
| 重构`UpdateConsultationStatus` | 委托给`_statusDisplay`执行，同步属性 |
| 重构`UpdatePrescriptionStatus` | 委托给`_statusDisplay`执行，同步属性 |

**设计决策**: 采用保守策略，保留ViewModel属性以保持向后兼容。状态计算逻辑提取到组件，ViewModel仅做同步。

### 编译验证

- 编译: 成功 (0警告, 0错误)
- 影响文件: 2个 (WorkspaceStatusDisplay.cs, MedicalCaseWorkspaceViewModel.cs)

---

### Phase 5: Handler清理 - 已完成 (2026-01-01)

**背景**: `herb-editor-control-refactoring`已归档，HerbListControl接管了大部分处方项管理功能。

**实施结果**:
| 文件 | 变更 | 行数变化 |
|------|------|----------|
| `PrescriptionItemHandler.cs` | 已删除(之前Phase) | -307行 |
| `PrescriptionImportHandler.cs` | 简化为纯DTO转换 | 292行 → 100行 (-192行) |
| **总计** | | **-499行** |

**保留的方法**:
- `ToHerbItemDtos(FormulaDetailDto, List<FormulaHerbItemDto>)` - 验方导入DTO转换
- `ToHerbItemDtos(List<PrescriptionItemDto>)` - 历史处方复制DTO转换

#### 5.1 职责重叠分析

| 功能 | 旧Handler | HerbListControl | 结论 |
|------|-----------|-----------------|------|
| 创建药材项 | `PrescriptionItemHandler.CreateHerbItem` | `CreateItemViewModel` | **重叠-可删** |
| 删除药材 | `PrescriptionItemHandler.DeleteHerbItem` | `DeleteAt` | **重叠-可删** |
| 紧凑列表 | `PrescriptionItemHandler.CompactHerbItems` | `Compact` | **重叠-可删** |
| 确保空行 | `PrescriptionItemHandler.EnsureMinimumBlankRows` | `EnsureSingleEmptySlot` | **重叠-可删** |
| 添加新行 | `PrescriptionItemHandler.AddNewRow` | `RequestNewSlot` | **重叠-可删** |
| 处方收集 | `PrescriptionItemHandler.CollectPrescriptionItems` | `ToDto` | **重叠-可删** |
| 重复检测 | `PrescriptionImportHandler.ProcessFormulaImport` | `AddHerbsAsync`内置 | **重叠-需简化** |
| 批量添加 | `PrescriptionImportHandler.AddHerbItemsToCollection` | `AddHerbs` | **重叠-可删** |
| 剂量合并 | ImportHandler内置逻辑 | `DuplicateStrategy` | **重叠-可删** |

#### 5.2 清理方案

**完全删除**:
- `PrescriptionItemHandler.cs` (307行) - 所有功能被控件接管

**简化重构**:
- `PrescriptionImportHandler.cs` (292行 → ~80行)
  - 删除: `AddHerbItemsToCollection` 方法
  - 简化: `ProcessFormulaImport` 仅做DTO转换，返回`List<HerbItemDto>`
  - 简化: `ProcessHistoryCopy` 仅做DTO转换，返回`List<HerbItemDto>`

#### 5.3 导入与HerbListControl对接方案

**新数据流**:
```
经验方/历史处方选择
    │
    ▼
Dialog返回原始数据 (FormulaDto / PrescriptionDto)
    │
    ▼
PrescriptionImportHandler.ToHerbItemDtos() ─────────────────┐
    │                                                        │
    │  纯DTO转换，无业务逻辑                                   │
    ▼                                                        │
List<HerbItemDto>                                            │
    │                                                        │
    ▼                                                        │
PrescriptionPanelViewModel.ImportHerbsAsync()                │
    │                                                        │
    │  通过View调用控件方法                                    │
    ▼                                                        │
HerbListControl.AddHerbs(dtos)  ◄────────────────────────────┘
    │
    │  控件内部处理:
    │  • 重复检测 (FindHerbIndex)
    │  • 剂量合并 (DuplicateStrategy)
    │  • 价格同步 (从AllHerbs获取最新价格)
    │
    ▼
触发 HerbListChanged 事件
    │
    ▼
View.OnHerbListChanged()
    │
    ▼
ViewModel.SetCurrentHerbList() + OnHerbListChanged()
    │
    ▼
更新统计、状态、金额
```

**关键接口变更**:

```csharp
// PrescriptionImportHandler.cs - 简化后
public class PrescriptionImportHandler
{
    /// <summary>
    /// 将经验方转换为HerbItemDto列表（纯数据转换）
    /// </summary>
    public List<HerbItemDto> ToHerbItemDtos(FormulaDetailDto formula)
    {
        return formula.Herbs.Select(h => new HerbItemDto
        {
            HerbId = h.HerbId,
            HerbName = h.HerbName,
            Dosage = h.Dosage,
            CookingMethod = h.CookingMethod
            // 注意: UnitPrice由控件从AllHerbs同步，不在此设置
        }).ToList();
    }

    /// <summary>
    /// 将历史处方转换为HerbItemDto列表（纯数据转换）
    /// </summary>
    public List<HerbItemDto> ToHerbItemDtos(PrescriptionDetailDto prescription)
    {
        return prescription.Items.Select(i => new HerbItemDto
        {
            HerbId = i.HerbId,
            HerbName = i.HerbName,
            Dosage = i.Dosage,
            CookingMethod = i.CookingMethod
            // 注意: UnitPrice由控件从AllHerbs同步，不在此设置
        }).ToList();
    }
}

// PrescriptionPanelViewModel.cs - 新导入方法
public partial class PrescriptionPanelViewModel
{
    /// <summary>
    /// 导入药材到控件（由View调用控件的AddHerbs方法）
    /// </summary>
    public event EventHandler<ImportHerbsRequestEventArgs>? ImportHerbsRequested;

    public void RequestImportHerbs(List<HerbItemDto> herbs)
    {
        ImportHerbsRequested?.Invoke(this, new ImportHerbsRequestEventArgs(herbs));
    }
}

// PrescriptionEditorPanel.xaml.cs - 处理导入请求
private void OnViewModelImportHerbsRequested(object? sender, ImportHerbsRequestEventArgs e)
{
    HerbListCtrl.AddHerbs(e.Herbs);
}
```

#### 5.4 实际收益

| 指标 | 变更前 | 变更后 | 减少 |
|------|--------|--------|------|
| PrescriptionItemHandler | 307行 | **已删除** | -307行 |
| PrescriptionImportHandler | 292行 | 100行 | -192行 |
| 总Handler代码 | 599行 | 100行 | **-499行 (83%)** |

---

### Phase 8: 待诊队列死代码清理 - 已完成 (2026-01-01)

**背景**: 分析发现MedicalCaseWorkspaceViewModel中的待诊队列UI属性和命令从未在XAML中绑定，属于死代码。待诊队列实际在PatientSelectionView中展示。

#### 8.1 死代码分析

| 代码类型 | 成员 | XAML绑定 | 结论 |
|----------|------|----------|------|
| UI属性 | `PendingQueue` | 无 | **死代码-删除** |
| UI属性 | `SelectedPendingCase` | 无 | **死代码-删除** |
| UI属性 | `IsRefreshingPendingQueue` | 无 | **死代码-删除** |
| UI属性 | `HasNoPendingCases` | 无 | **死代码-删除** |
| 命令 | `RefreshPendingQueueCommand` | 无 | **死代码-删除** |
| 命令 | `SelectPendingCaseCommand` | 无 | **死代码-删除** |
| Handler调用 | `_pendingQueueHandler.LoadPendingQueueAsync()` | N/A | **保留-业务逻辑** |

#### 8.2 保留原因

Handler内部调用被保留，用于以下场景的全局待诊队列状态同步：
- 保存草稿后刷新 (line 393)
- 取消挂起后刷新 (line 552)
- 完成医案后刷新 (line 735)
- 初始化时刷新 (line 780)

#### 8.3 清理内容

| 区域 | 变更 |
|------|------|
| 属性区域 (line 220-223) | 替换为注释说明 |
| 命令声明 (line 247-248) | 替换为注释说明 |
| 构造函数 (line 322) | 删除命令初始化和事件订阅 |

#### 8.4 当前行数

- **清理后**: 1046行
- **目标**: <750行 (需继续Phase 5 Handler重构)

---

## 1. Problem Statement

### 1.1 Current State

`MedicalCaseWorkspaceViewModel.cs` 有 **1183行**，包含：
- 37个字段
- 60+个属性
- 30+个方法

### 1.2 Industry Best Practices (调研结论)

| 来源 | 最佳实践 |
|------|----------|
| [StackOverflow](https://stackoverflow.com/questions/596797) | "ViewModel超过两屏代码就应该考虑拆分" |
| [marktinderholt.com](https://www.marktinderholt.com/software%20development/2009/03/20/mvvm-vm-composition.html) | ViewModel Composition: Root ViewModel包含子ViewModel作为属性 |
| [tutorialspoint MVVM Guide](https://www.tutorialspoint.com/mvvm/mvvm_quick_guide.htm) | "复杂屏幕分解为父子视图层级" |
| [rsuter.com](https://blog.rsuter.com/recommendations-best-practices-implementing-mvvm-xaml-net-applications/) | "View和ViewModel一对一关系" |
| [Microsoft MVVM Docs](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm) | "ViewModel负责协调View与Model的交互" |

### 1.3 Composition Pattern (推荐模式)

```csharp
// Root ViewModel (父)
public class RootViewModel : ObservableObject
{
    public ChildViewModelA ChildA { get; }
    public ChildViewModelB ChildB { get; }
    public ViewModel ActiveWorkspace { get; set; }
}

// View (XAML)
<Grid>
    <ChildViewA DataContext="{Binding ChildA}" />
    <ChildViewB DataContext="{Binding ChildB}" />
</Grid>
```

---

## 2. Current Architecture Analysis

### 2.1 Already Extracted Components (已提取)

```
ViewModels/Components/ (9个)
├── MedicalCaseEditModeStateMachine.cs    # 编辑模式状态机
├── MedicalCaseWorkspaceCoordinator.cs    # 保存/完成协调
├── PrescriptionValidator.cs              # 处方验证
├── PrescriptionCalculator.cs             # 处方计算
├── PrescriptionDataLoader.cs             # 处方数据加载
├── PrescriptionSaveHandler.cs            # 处方保存
├── PrescriptionImportHandler.cs          # 处方导入
├── PrescriptionItemHandler.cs            # 处方项管理
└── PrescriptionPrintHandler.cs           # 处方打印

Services/ (6个)
├── AuditRequirementChecker.cs            # 审核检查
├── MedicalCaseValidator.cs               # 医案验证
├── MedicalCaseDataLoader.cs              # 数据加载
├── MedicalCaseService.cs                 # 医案服务
├── MedicalCaseLifecycleHandler.cs        # 生命周期
└── MedicalCaseNavigationHandler.cs       # 导航处理
```

### 2.2 Remaining Responsibilities (待提取)

分析ViewModel中剩余的职责：

| 职责 | 类型 | 属性/方法数 | 行数估算 | 优先级 |
|------|------|------------|---------|--------|
| 状态显示计算 | Presentation | 8属性 + 2方法 | ~120行 | P0 |
| 按钮可见性 | Presentation | 6属性 | ~60行 | P0 |
| 待诊队列管理 | Sub-ViewModel | 4属性 + 3方法 | ~80行 | P1 |
| 患者信息显示 | Presentation | 5属性 + 2方法 | ~50行 | P2 |
| 子ViewModel协调 | Coordination | 2方法 | ~40行 | P1 |

---

## 3. Solution Design

### 3.1 Pattern Selection: Composite ViewModel

基于行业最佳实践，采用**Composite ViewModel Pattern**：

```
MedicalCaseWorkspaceViewModel (Root)
├── WorkspaceStatusDisplay (状态显示组件)
├── WorkspaceButtonState (按钮状态组件)
├── PendingQueueViewModel (待诊队列子ViewModel)
├── ConsultationPanelViewModel (诊断面板 - 已存在)
└── PrescriptionPanelViewModel (处方面板 - 已存在)
```

### 3.2 Component Design

#### 3.2.1 WorkspaceStatusDisplay (状态显示)

**职责**: 计算和管理诊断/处方的状态文本和颜色

```csharp
/// <summary>
/// 工作区状态显示组件
/// 遵循SRP原则，专注于状态计算和显示
/// </summary>
public partial class WorkspaceStatusDisplay : ObservableObject
{
    // === 诊断状态 ===
    [ObservableProperty] private string _consultationStatusText = string.Empty;
    [ObservableProperty] private Brush _consultationStatusColor = Brushes.Gray;

    // === 处方状态 ===
    [ObservableProperty] private string _prescriptionStatusText = string.Empty;
    [ObservableProperty] private string _prescriptionStatusSummary = string.Empty;
    [ObservableProperty] private Brush _prescriptionStatusSummaryColor = Brushes.Gray;
    [ObservableProperty] private Brush _prescriptionStatusBackground = Brushes.Transparent;
    [ObservableProperty] private bool _showPrescriptionStatus;

    /// <summary>
    /// 更新诊断状态显示
    /// </summary>
    public void UpdateConsultationStatus(EditState state, bool hasValidDiagnosis)
    {
        (ConsultationStatusText, ConsultationStatusColor) = state switch
        {
            EditState.New => ("待诊断", Brushes.Orange),
            EditState.Editing when hasValidDiagnosis => ("已填写", Brushes.Green),
            EditState.Editing => ("编辑中", Brushes.Blue),
            EditState.Completed => ("已完成", Brushes.Green),
            _ => ("未知", Brushes.Gray)
        };
    }

    /// <summary>
    /// 更新处方状态显示
    /// </summary>
    public void UpdatePrescriptionStatus(int itemCount, bool needsPrescription, bool isCompleted)
    {
        ShowPrescriptionStatus = needsPrescription || itemCount > 0;

        if (!needsPrescription)
        {
            PrescriptionStatusText = "不开处方";
            PrescriptionStatusSummary = string.Empty;
            PrescriptionStatusBackground = Brushes.LightGray;
            return;
        }

        PrescriptionStatusText = itemCount > 0 ? $"{itemCount}味药材" : "未开方";
        PrescriptionStatusSummary = isCompleted ? "已完成" : "编辑中";
        PrescriptionStatusSummaryColor = isCompleted ? Brushes.Green : Brushes.Orange;
        PrescriptionStatusBackground = itemCount > 0 ? Brushes.LightGreen : Brushes.LightYellow;
    }
}
```

#### 3.2.2 WorkspaceButtonState (按钮状态)

**职责**: 管理工作区按钮的可见性和可用性

```csharp
/// <summary>
/// 工作区按钮状态组件
/// 集中管理所有按钮的显示逻辑
/// </summary>
public partial class WorkspaceButtonState : ObservableObject
{
    [ObservableProperty] private bool _showCompleteButton;
    [ObservableProperty] private bool _showDraftButton;
    [ObservableProperty] private bool _showSaveButton;
    [ObservableProperty] private bool _showEditButton;
    [ObservableProperty] private bool _showEditButtonTopRight;
    [ObservableProperty] private bool _canComplete;
    [ObservableProperty] private bool _canPrintPrescription;

    /// <summary>
    /// 根据工作区状态更新所有按钮
    /// </summary>
    public void Update(EditState editState, WorkspaceMode mode, bool isFromManagement, bool hasUnsavedChanges)
    {
        var isNew = editState == EditState.New;
        var isEditing = editState == EditState.Editing;
        var isCompleted = editState == EditState.Completed;
        var isHistorical = mode == WorkspaceMode.HistoricalEdit;

        ShowCompleteButton = isNew || (isEditing && !isHistorical);
        ShowDraftButton = isNew || isEditing;
        ShowSaveButton = isHistorical && hasUnsavedChanges;
        ShowEditButton = isCompleted && !isFromManagement;
        ShowEditButtonTopRight = isCompleted && isFromManagement;
        CanPrintPrescription = isCompleted;
    }

    /// <summary>
    /// 更新完成按钮可用性
    /// </summary>
    public void UpdateCanComplete(bool hasValidDiagnosis, bool needsPrescription, int herbCount)
    {
        if (!hasValidDiagnosis)
        {
            CanComplete = false;
            return;
        }

        CanComplete = !needsPrescription || herbCount > 0;
    }
}
```

#### 3.2.3 PendingQueueViewModel (待诊队列)

**职责**: 管理待诊患者队列的展示和交互

```csharp
/// <summary>
/// 待诊队列视图模型
/// 独立的子ViewModel，管理待诊队列的完整生命周期
/// </summary>
public partial class PendingQueueViewModel : ObservableObject
{
    private readonly IPendingQueueManager _queueManager;

    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private PendingMedicalCaseDto? _selectedCase;

    public ObservableCollection<PendingMedicalCaseDto> Queue { get; } = new();
    public bool HasNoCases => Queue.Count == 0;

    // Commands
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<PendingMedicalCaseDto> SelectCommand { get; }

    // Events
    public event EventHandler<PendingMedicalCaseDto>? CaseSelected;
    public event EventHandler? QueueRefreshed;

    public PendingQueueViewModel(IPendingQueueManager queueManager)
    {
        _queueManager = queueManager;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SelectCommand = new AsyncRelayCommand<PendingMedicalCaseDto>(SelectAsync);
    }

    public async Task RefreshAsync()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            var cases = await _queueManager.GetPendingCasesAsync();

            Queue.Clear();
            foreach (var c in cases)
                Queue.Add(c);

            OnPropertyChanged(nameof(HasNoCases));
            QueueRefreshed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task SelectAsync(PendingMedicalCaseDto? pendingCase)
    {
        if (pendingCase == null) return;
        SelectedCase = pendingCase;
        CaseSelected?.Invoke(this, pendingCase);
    }
}
```

### 3.3 Refactored Root ViewModel Structure

```csharp
public partial class MedicalCaseWorkspaceViewModel : ViewModelBase, INavigationAware, IDisposable
{
    // === 子组件 (Composition) ===
    public WorkspaceStatusDisplay StatusDisplay { get; }
    public WorkspaceButtonState ButtonState { get; }
    public PendingQueueViewModel PendingQueue { get; }
    public ConsultationPanelViewModel ConsultationPanelViewModel { get; private set; }
    public PrescriptionPanelViewModel PrescriptionPanelViewModel { get; private set; }

    // === 现有服务依赖 ===
    private readonly MedicalCaseWorkspaceCoordinator _coordinator;
    private readonly MedicalCaseEditModeStateMachine _editModeStateMachine;
    private readonly MedicalCaseNavigationHandler _navigationHandler;
    private readonly MedicalCaseLifecycleHandler _lifecycleHandler;
    // ... 其他服务

    // === 核心状态 (保留在Root) ===
    [ObservableProperty] private Guid? _medicalCaseId;
    [ObservableProperty] private PatientDetailDto? _currentPatient;
    [ObservableProperty] private bool _needsPrescription;

    public MedicalCaseWorkspaceViewModel(
        WorkspaceStatusDisplay statusDisplay,
        WorkspaceButtonState buttonState,
        PendingQueueViewModel pendingQueue,
        // ... 其他依赖
    )
    {
        StatusDisplay = statusDisplay;
        ButtonState = buttonState;
        PendingQueue = pendingQueue;

        // 订阅子组件事件
        PendingQueue.CaseSelected += OnPendingCaseSelected;

        // 初始化命令 (委托给组件)
        RefreshPendingQueueCommand = PendingQueue.RefreshCommand;
    }

    // === 委托属性 (向后兼容) ===
    public string ConsultationStatusText => StatusDisplay.ConsultationStatusText;
    public Brush ConsultationStatusColor => StatusDisplay.ConsultationStatusColor;
    public bool ShowCompleteButton => ButtonState.ShowCompleteButton;
    public bool CanComplete => ButtonState.CanComplete;
    public ObservableCollection<PendingMedicalCaseDto> PendingQueueItems => PendingQueue.Queue;

    // === 委托命令 ===
    public IAsyncRelayCommand RefreshPendingQueueCommand { get; }
}
```

---

## 4. XAML Binding Update

### 4.1 Current Binding (向后兼容)

```xml
<!-- 保持原有绑定不变 -->
<TextBlock Text="{Binding ConsultationStatusText}" />
<Button Visibility="{Binding ShowCompleteButton, Converter=...}" />
```

### 4.2 New Direct Binding (推荐)

```xml
<!-- 直接绑定到子组件 -->
<TextBlock Text="{Binding StatusDisplay.ConsultationStatusText}" />
<Button Visibility="{Binding ButtonState.ShowCompleteButton, Converter=...}" />

<!-- 待诊队列绑定到子ViewModel -->
<ListView ItemsSource="{Binding PendingQueue.Queue}"
          SelectedItem="{Binding PendingQueue.SelectedCase}" />
```

---

## 5. Implementation Plan

### Phase 1: 创建组件类 (2h)

| Task | 文件 | 描述 |
|------|------|------|
| T1.1 | `Components/WorkspaceStatusDisplay.cs` | 状态显示组件 |
| T1.2 | `Components/WorkspaceButtonState.cs` | 按钮状态组件 |
| T1.3 | `ViewModels/PendingQueueViewModel.cs` | 待诊队列子ViewModel |

### Phase 2: 重构Root ViewModel (2h)

| Task | 描述 |
|------|------|
| T2.1 | 注入新组件 |
| T2.2 | 迁移状态计算逻辑到StatusDisplay |
| T2.3 | 迁移按钮逻辑到ButtonState |
| T2.4 | 迁移待诊队列逻辑到PendingQueueViewModel |
| T2.5 | 添加委托属性保持向后兼容 |

### Phase 3: 更新DI注册 (0.5h)

```csharp
// MedicalCaseModule.cs
containerRegistry.Register<WorkspaceStatusDisplay>();
containerRegistry.Register<WorkspaceButtonState>();
containerRegistry.Register<PendingQueueViewModel>();
```

### Phase 4: 验证和测试 (1.5h)

| Task | 描述 |
|------|------|
| T4.1 | 编译验证 |
| T4.2 | UI功能测试 |
| T4.3 | 添加组件单元测试 |

---

## 6. Expected Results

### 6.1 Line Count Reduction

| 阶段 | 行数 | 减少 |
|------|------|------|
| 当前 | 1183行 | - |
| Phase 1完成 | 1063行 | -120行 (StatusDisplay) |
| Phase 2完成 | 1003行 | -60行 (ButtonState) |
| Phase 3完成 | 923行 | -80行 (PendingQueue) |
| **最终** | **~900行** | **-283行 (24%)** |

### 6.2 Architecture Benefits

| 收益 | 说明 |
|------|------|
| **单一职责** | 每个组件负责一个明确职责 |
| **可测试性** | 组件可独立单元测试，不依赖UI |
| **可复用性** | StatusDisplay/ButtonState可用于其他Workspace |
| **可维护性** | 修改状态逻辑只需改对应组件 |
| **可扩展性** | 新增状态类型只需扩展组件 |

---

## 7. References

- [MVVM ViewModel Composition](https://www.marktinderholt.com/software%20development/2009/03/20/mvvm-vm-composition.html)
- [MVVM Best Practices](https://blog.rsuter.com/recommendations-best-practices-implementing-mvvm-xaml-net-applications/)
- [Microsoft MVVM Documentation](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [StackOverflow: SRP in ViewModel](https://stackoverflow.com/questions/596797)
- [TutorialsPoint MVVM Guide](https://www.tutorialspoint.com/mvvm/mvvm_quick_guide.htm)

---

## 8. Acceptance Criteria

- [ ] ViewModel行数降至950行以下
- [ ] 所有新组件有对应单元测试
- [ ] XAML绑定正常工作
- [ ] 现有功能无回归
- [ ] 编译无警告
