# simplify-workspace-architecture 设计文档

## 架构对比

### 当前架构 (11类, ~3000行)

```
MedicalCaseWorkspaceViewModel (1393行)
├── WorkspaceState (217行)
├── WorkspaceStatusDisplay (130行)
├── MedicalCaseEditModeStateMachine (361行)
├── MedicalCaseWorkspaceCoordinator (257行)
├── WorkspacePendingQueueHandler (363行) [回调委托x6]
├── MedicalCaseNavigationHandler (230行) [回调委托x5]
├── MedicalCaseDataLoader (195行)
├── PrescriptionPrintHandler (233行)
├── PrescriptionImportHandler (101行)
└── DataProviderAdapters (112行) [4个适配器]
```

### 目标架构 (5类, ~1600行)

```
MedicalCaseWorkspaceViewModel (~450行)
├── WorkspaceState (~250行) [含状态显示]
├── MedicalCaseEditModeStateMachine (~300行)
├── MedicalCaseCoordinator (~350行) [含数据加载]
└── PrescriptionPrintHandler (~230行)
```

---

## Phase 1: Item类实现接口

### 1.1 定义简化接口

**文件**: `Interfaces/IDataProvider.cs`

现有IDataProvider接口保持不变：
```csharp
public interface IDataProvider
{
    ConsultationInputDto? GetConsultationData();
    PrescriptionInputDto? GetPrescriptionData();
}

public interface IValidatable
{
    string ValidationMessage { get; set; }
    bool Validate();
}
```

### 1.2 ConsultationItem实现接口

**文件**: `Models/Items/ConsultationItem.cs`

```csharp
public partial class ConsultationItem : ObservableObject, IDataProvider, IValidatable
{
    // 现有属性保持不变...

    #region IDataProvider实现

    public ConsultationInputDto? GetConsultationData()
    {
        return ConsultationMapper.Instance.ToInputDto(this);
    }

    public PrescriptionInputDto? GetPrescriptionData() => null;

    #endregion

    #region IValidatable实现

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public bool Validate()
    {
        if (!IsDiagnosisComplete)
        {
            ValidationMessage = "请填写中医诊断";
            return false;
        }
        ValidationMessage = string.Empty;
        return true;
    }

    #endregion
}
```

### 1.3 PrescriptionItem实现接口

**文件**: `Models/Items/PrescriptionItem.cs`

```csharp
public partial class PrescriptionItem : ObservableObject, IDataProvider, IValidatable
{
    // 现有属性保持不变...

    #region IDataProvider实现

    public ConsultationInputDto? GetConsultationData() => null;

    public PrescriptionInputDto? GetPrescriptionData()
    {
        return PrescriptionMapper.Instance.ToInputDto(this);
    }

    #endregion

    #region IValidatable实现

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    /// <summary>
    /// 是否启用处方验证（不开处方时跳过验证）
    /// </summary>
    public bool ValidationEnabled { get; set; } = true;

    public bool Validate()
    {
        if (!ValidationEnabled)
        {
            ValidationMessage = string.Empty;
            return true;
        }

        if (!IsValid)
        {
            ValidationMessage = "请添加至少一种药材";
            return false;
        }
        ValidationMessage = string.Empty;
        return true;
    }

    #endregion
}
```

### 1.4 删除DataProviderAdapters.cs

删除文件后，ViewModel中的使用方式变更：

```csharp
// Before
var consultationProvider = new ConsultationDataProviderAdapter(_consultation);
var consultationValidator = new ConsultationValidatorAdapter(_consultation);

// After
var consultationProvider = _consultation; // ConsultationItem直接就是IDataProvider
var consultationValidator = _consultation; // ConsultationItem直接就是IValidatable
```

---

## Phase 2: 合并DataLoader到Coordinator

### 2.1 MedicalCaseCoordinator扩展

**文件**: `ViewModels/Components/MedicalCaseCoordinator.cs`

```csharp
public class MedicalCaseCoordinator
{
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IMedicalCaseRepository _repository;
    private readonly ILogger<MedicalCaseCoordinator> _logger;

    #region 数据缓存 (从DataLoader合并)

    public MedicalCaseDetailDto? CachedMedicalCase { get; private set; }
    public ConsultationDetailDto? CachedConsultation { get; private set; }
    public PrescriptionDetailDto? CachedPrescription { get; private set; }

    #endregion

    #region 数据加载 (从DataLoader合并)

    public async Task<(bool success, MedicalCaseDetailDto? detail, string? errorMessage)>
        LoadMedicalCaseAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("[COORD] LoadMedicalCase - Id={Id}", medicalCaseId);

            var detail = await _medicalCaseService.GetByIdSimpleAsync(medicalCaseId);
            if (detail == null)
            {
                return (false, null, "未找到医案数据");
            }

            // 缓存数据
            CachedMedicalCase = detail;
            CachedConsultation = detail.Consultation;
            CachedPrescription = detail.Prescription;

            return (true, detail, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[COORD] LoadMedicalCase failed - Id={Id}", medicalCaseId);
            return (false, null, ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载医案", ex));
        }
    }

    public void ClearCache()
    {
        CachedMedicalCase = null;
        CachedConsultation = null;
        CachedPrescription = null;
    }

    #endregion

    #region 聚合保存操作 (现有代码)

    public async Task<AggregateSaveResult> SaveAsync(
        Guid medicalCaseId,
        IDataProvider? consultationProvider,
        IDataProvider? prescriptionProvider,
        string? remark = null,
        string? editReason = null)
    {
        // 现有实现保持不变
    }

    public async Task<LifecycleResult> SaveDraftAsync(...)
    {
        // 现有实现保持不变
    }

    public async Task<LifecycleResult> CompleteAsync(...)
    {
        // 现有实现保持不变
    }

    public async Task<LifecycleResult> CancelAsync(...)
    {
        // 现有实现保持不变
    }

    #endregion
}
```

### 2.2 删除MedicalCaseDataLoader.cs

---

## Phase 3: 合并StatusDisplay到WorkspaceState

### 3.1 WorkspaceState扩展

**文件**: `ViewModels/Components/WorkspaceState.cs`

```csharp
public partial class WorkspaceState : ObservableObject
{
    // === 现有状态属性 ===

    #region 忙碌状态
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _busyMessage;
    #endregion

    #region 患者信息
    [ObservableProperty] private string _patientName = string.Empty;
    [ObservableProperty] private string _patientInfo = string.Empty;
    // ... 其他患者属性
    #endregion

    #region 完成状态
    [ObservableProperty] private bool _canPrintPrescription;
    [ObservableProperty] private bool _canComplete;
    #endregion

    // === 从StatusDisplay合并的状态显示 ===

    #region 诊断状态显示 (从StatusDisplay合并)

    [ObservableProperty]
    private string _consultationStatusText = string.Empty;

    [ObservableProperty]
    private Brush _consultationStatusColor = Brushes.Gray;

    public void UpdateConsultationStatus(bool isEditing, bool hasValidDiagnosis)
    {
        if (isEditing)
        {
            ConsultationStatusText = hasValidDiagnosis ? "已填写" : "编辑中";
            ConsultationStatusColor = hasValidDiagnosis
                ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))  // Green
                : new SolidColorBrush(Color.FromRgb(0x38, 0x8B, 0xFD)); // Blue
        }
        else
        {
            ConsultationStatusText = "已完成";
            ConsultationStatusColor = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)); // Green
        }
    }

    #endregion

    #region 处方状态显示 (从StatusDisplay合并)

    [ObservableProperty]
    private string _prescriptionStatusText = string.Empty;

    [ObservableProperty]
    private string _prescriptionStatusSummary = string.Empty;

    [ObservableProperty]
    private Brush _prescriptionStatusColor = Brushes.Gray;

    [ObservableProperty]
    private bool _showPrescriptionStatus;

    public void UpdatePrescriptionStatus(int itemCount, bool needsPrescription, bool isCompleted)
    {
        ShowPrescriptionStatus = needsPrescription || itemCount > 0;

        if (!needsPrescription)
        {
            PrescriptionStatusText = "不开处方";
            PrescriptionStatusSummary = string.Empty;
            PrescriptionStatusColor = Brushes.Gray;
            return;
        }

        PrescriptionStatusText = itemCount > 0 ? $"{itemCount}味药材" : "未开方";
        PrescriptionStatusSummary = isCompleted ? "已完成" : "编辑中";
        PrescriptionStatusColor = isCompleted
            ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))  // Green
            : new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)); // Orange
    }

    #endregion

    #region 重置 (扩展)

    public void Reset()
    {
        // 现有重置逻辑
        IsBusy = false;
        BusyMessage = null;
        PatientName = string.Empty;
        // ...

        // 状态显示重置
        ConsultationStatusText = string.Empty;
        ConsultationStatusColor = Brushes.Gray;
        PrescriptionStatusText = string.Empty;
        PrescriptionStatusSummary = string.Empty;
        PrescriptionStatusColor = Brushes.Gray;
        ShowPrescriptionStatus = false;
    }

    #endregion
}
```

### 3.2 删除WorkspaceStatusDisplay.cs

---

## Phase 4: 待诊队列逻辑回归ViewModel

### 4.1 ViewModel待诊队列方法

将WorkspacePendingQueueHandler的逻辑直接实现在ViewModel中：

```csharp
public partial class MedicalCaseWorkspaceViewModel
{
    #region 待诊队列

    private ObservableCollection<PendingCaseDto> _pendingCases = new();
    public ObservableCollection<PendingCaseDto> PendingCases
    {
        get => _pendingCases;
        set => SetProperty(ref _pendingCases, value);
    }

    [ObservableProperty]
    private PendingCaseDto? _selectedPendingCase;

    [RelayCommand]
    private async Task RefreshPendingQueueAsync()
    {
        if (State.IsRefreshingPendingQueue) return;

        try
        {
            State.IsRefreshingPendingQueue = true;

            var result = await _pendingQueueService.GetTodayPendingAsync();
            if (result.success)
            {
                PendingCases = new ObservableCollection<PendingCaseDto>(result.data ?? []);
            }
        }
        finally
        {
            State.IsRefreshingPendingQueue = false;
        }
    }

    [RelayCommand]
    private async Task SelectPendingCaseAsync(PendingCaseDto? pendingCase)
    {
        if (pendingCase == null) return;

        // 检查是否有未保存的修改
        if (EditModeStateMachine.HasUnsavedChanges)
        {
            var result = await ShowSaveConfirmationAsync();
            if (result == SaveConfirmationResult.Cancel) return;
            if (result == SaveConfirmationResult.Save)
            {
                await SaveDraftAsync();
            }
        }

        // 加载新医案
        await LoadMedicalCaseAsync(pendingCase.MedicalCaseId);
    }

    #endregion
}
```

### 4.2 删除WorkspacePendingQueueHandler.cs

---

## Phase 5: 导航逻辑回归ViewModel

### 5.1 ViewModel导航方法

将MedicalCaseNavigationHandler的逻辑直接实现在ViewModel中：

```csharp
public partial class MedicalCaseWorkspaceViewModel
{
    #region 导航

    [RelayCommand]
    private async Task NavigateBackAsync()
    {
        try
        {
            // Management模式
            if (EditModeStateMachine.WorkspaceMode == WorkspaceMode.Management)
            {
                if (EditModeStateMachine.IsReadOnly)
                {
                    _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseMasterDetail);
                    return;
                }

                // 编辑模式：确认保存
                var shouldNavigate = await HandleManagementLeaveAsync();
                if (shouldNavigate)
                {
                    _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseMasterDetail);
                }
                return;
            }

            // Clinical模式：三选项对话框
            var result = await HandleClinicalLeaveAsync();
            if (result.CanLeave)
            {
                _navigationCoordinator.NavigateTo(ViewNames.PatientSelection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VM] NavigateBack failed");
        }
    }

    private async Task<bool> HandleManagementLeaveAsync()
    {
        var dialogResult = await _commonDialogService.ShowUnsavedChangesAsync();

        switch (dialogResult)
        {
            case ButtonResult.Yes: // 保存
                await SaveDraftAsync();
                EditModeStateMachine.EnterReadOnlyMode();
                return true;
            case ButtonResult.No: // 放弃
                return true;
            default: // 取消
                return false;
        }
    }

    private async Task<LeaveResult> HandleClinicalLeaveAsync()
    {
        var message = "您将离开看诊界面，是否暂存当前医案？";
        var result = await _commonDialogService.ShowTripleChoiceAsync(message, "离开确认");

        switch (result)
        {
            case TripleChoiceResult.Yes: // 暂存
                await SaveDraftAsync();
                return LeaveResult.AllowLeave();
            case TripleChoiceResult.No: // 取消医案
                await CancelMedicalCaseAsync();
                return LeaveResult.AllowLeave();
            default: // 继续看诊
                return LeaveResult.CancelLeave();
        }
    }

    #endregion
}
```

### 5.2 删除MedicalCaseNavigationHandler.cs

---

## Phase 6: 处方导入简化

### 6.1 扩展方法替代Handler

**文件**: `Extensions/PrescriptionImportExtensions.cs`

```csharp
public static class PrescriptionImportExtensions
{
    /// <summary>
    /// 将验方药材转换为HerbItemDto列表
    /// </summary>
    public static IReadOnlyList<HerbItemDto> ToHerbItemDtos(
        this FormulaDetailDto formula,
        List<FormulaHerbItemDto> herbs)
    {
        if (formula == null || herbs == null || !herbs.Any())
            return Array.Empty<HerbItemDto>();

        return herbs
            .Where(h => h.HerbId.HasValue)
            .Select(h => new HerbItemDto
            {
                HerbId = h.HerbId!.Value,
                HerbName = h.HerbName ?? string.Empty,
                Dosage = h.Dosage,
                DecocteMethod = h.DecocteMethod
            })
            .ToList();
    }

    /// <summary>
    /// 将历史处方药材转换为HerbItemDto列表
    /// </summary>
    public static IReadOnlyList<HerbItemDto> ToHerbItemDtos(
        this List<PrescriptionItemDto> items)
    {
        if (items == null || !items.Any())
            return Array.Empty<HerbItemDto>();

        return items
            .Select(i => new HerbItemDto
            {
                HerbId = i.HerbId,
                HerbName = i.HerbName ?? string.Empty,
                Dosage = i.Dosage,
                DecocteMethod = i.DecocteMethod,
                UnitPrice = i.UnitPrice
            })
            .ToList();
    }
}
```

### 6.2 删除PrescriptionImportHandler.cs

---

## 文件变更清单

### 新建文件
| 文件 | 说明 |
|------|------|
| `Extensions/PrescriptionImportExtensions.cs` | 处方导入扩展方法 |

### 修改文件
| 文件 | 变更 |
|------|------|
| `Models/Items/ConsultationItem.cs` | 实现IDataProvider, IValidatable |
| `Models/Items/PrescriptionItem.cs` | 实现IDataProvider, IValidatable |
| `ViewModels/Components/WorkspaceState.cs` | 合并StatusDisplay |
| `ViewModels/Components/MedicalCaseCoordinator.cs` | 合并DataLoader，重命名 |
| `MedicalCaseWorkspaceViewModel.cs` | 回归待诊队列和导航逻辑 |

### 删除文件
| 文件 | 理由 |
|------|------|
| `ViewModels/Components/DataProviderAdapters.cs` | Item直接实现接口 |
| `ViewModels/Components/WorkspaceStatusDisplay.cs` | 合并到WorkspaceState |
| `ViewModels/Components/WorkspacePendingQueueHandler.cs` | 逻辑回归ViewModel |
| `Services/MedicalCaseNavigationHandler.cs` | 逻辑回归ViewModel |
| `Services/MedicalCaseDataLoader.cs` | 合并到Coordinator |
| `ViewModels/Components/PrescriptionImportHandler.cs` | 改为扩展方法 |

### 保持不变
| 文件 | 说明 |
|------|------|
| `ViewModels/Components/MedicalCaseEditModeStateMachine.cs` | 状态机模式有效 |
| `ViewModels/Components/PrescriptionPrintHandler.cs` | 打印是独立领域 |

---

## XAML绑定变更

### StatusDisplay → State

```xml
<!-- Before -->
<TextBlock Text="{Binding StatusDisplay.ConsultationStatusText}" />
<TextBlock Foreground="{Binding StatusDisplay.ConsultationStatusColor}" />

<!-- After -->
<TextBlock Text="{Binding State.ConsultationStatusText}" />
<TextBlock Foreground="{Binding State.ConsultationStatusColor}" />
```

---

## 验证策略

### 每Phase验证
```bash
dotnet build LYBT.Desktop.MedicalCase.csproj -c Release --no-restore
```

### 功能验证清单
- [ ] Clinical模式：新建医案
- [ ] Clinical模式：暂存/完成医案
- [ ] Clinical模式：待诊队列切换
- [ ] Clinical模式：返回导航
- [ ] Management模式：查看医案
- [ ] Management模式：编辑医案
- [ ] Management模式：保存/取消
- [ ] 处方打印预览
- [ ] 验方导入

---

## 预估结果

| 组件 | 当前行数 | 目标行数 |
|------|---------|---------|
| MedicalCaseWorkspaceViewModel | 1393 | ~450 |
| WorkspaceState | 217+130 | ~250 |
| MedicalCaseEditModeStateMachine | 361 | 361 |
| MedicalCaseCoordinator | 257+195 | ~350 |
| PrescriptionPrintHandler | 233 | 233 |
| 扩展方法 | 0 | ~50 |
| **总计** | **2786** | **~1694** |
| **减少** | - | **~1092行 (39%)** |
