# slim-workspace-viewmodel 设计文档

## 架构设计

### 目标架构

```
┌─────────────────────────────────────────────────────────────────────┐
│                   MedicalCaseWorkspaceView.xaml                      │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────────┐  │
│  │PatientInfo  │ │Consultation │ │Prescription │ │PendingQueue   │  │
│  │Region       │ │Region       │ │Region       │ │Region         │  │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│              MedicalCaseWorkspaceViewModel (<500行)                  │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ - WorkspaceState State (聚合状态)                            │   │
│  │ - ICommand SaveDraftCommand → Handler                        │   │
│  │ - ICommand CompleteCommand → Handler                         │   │
│  │ - ICommand CancelCommand → Handler                           │   │
│  │ - 协调子组件交互                                              │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                              │
           ┌──────────────────┼──────────────────┐
           ▼                  ▼                  ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│WorkspaceState    │ │Handler层          │ │Item/Model层      │
│(状态聚合)        │ │(业务逻辑)         │ │(数据模型)        │
├──────────────────┤ ├──────────────────┤ ├──────────────────┤
│IsBusy            │ │PendingQueue      │ │MedicalCaseItem   │
│IsReadOnly        │ │Handler           │ │ConsultationItem  │
│StatusMessage     │ │PrescriptionEdit  │ │PrescriptionItem  │
│PatientDisplay    │ │Handler           │ │PrescriptionHerb  │
│EditMode          │ │NavigationHandler │ │Item              │
└──────────────────┘ │WorkspaceCoord    │ └──────────────────┘
                     └──────────────────┘
```

## 设计模式详解

### 1. State对象模式

**问题**: ViewModel有20+个独立属性，每个都需要`[ObservableProperty]`标记。

**解决方案**: 创建`WorkspaceState`类聚合相关属性。

```csharp
// ======= 新文件: WorkspaceState.cs =======
namespace LYBT.Desktop.Clinical.ViewModels.Components
{
    /// <summary>
    /// 工作台状态聚合对象
    /// OpenSpec: slim-workspace-viewmodel - State对象模式
    /// </summary>
    public partial class WorkspaceState : ObservableObject
    {
        #region UI状态

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _busyMessage;

        [ObservableProperty]
        private bool _isReadOnly;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        #endregion

        #region 患者信息显示

        [ObservableProperty]
        private string _patientName = string.Empty;

        [ObservableProperty]
        private string _patientGender = string.Empty;

        [ObservableProperty]
        private int _patientAge;

        [ObservableProperty]
        private string _patientPhone = string.Empty;

        /// <summary>
        /// 格式化的患者信息（用于标题显示）
        /// </summary>
        public string PatientDisplayInfo =>
            string.IsNullOrEmpty(PatientName)
                ? "未选择患者"
                : $"{PatientName} ({PatientGender}, {PatientAge}岁)";

        #endregion

        #region 编辑模式

        [ObservableProperty]
        private MedicalCaseEditMode _editMode = MedicalCaseEditMode.View;

        public bool IsEditing => EditMode != MedicalCaseEditMode.View;

        public bool CanEdit => !IsReadOnly && !IsBusy;

        #endregion

        #region 方法

        /// <summary>
        /// 从患者DTO填充显示信息
        /// </summary>
        public void UpdateFromPatient(PatientDetailDto? patient)
        {
            if (patient == null)
            {
                PatientName = string.Empty;
                PatientGender = string.Empty;
                PatientAge = 0;
                PatientPhone = string.Empty;
                return;
            }

            PatientName = patient.Name;
            PatientGender = patient.Gender.GetDisplayName();
            PatientAge = patient.Age;
            PatientPhone = patient.Phone ?? string.Empty;
        }

        /// <summary>
        /// 设置忙碌状态
        /// </summary>
        public void SetBusy(bool busy, string? message = null)
        {
            IsBusy = busy;
            BusyMessage = busy ? message : null;
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            IsBusy = false;
            BusyMessage = null;
            IsReadOnly = false;
            StatusMessage = string.Empty;
            EditMode = MedicalCaseEditMode.View;
            PatientName = string.Empty;
            PatientGender = string.Empty;
            PatientAge = 0;
            PatientPhone = string.Empty;
        }

        #endregion
    }
}
```

**ViewModel中的使用**:

```csharp
// 替代20+个独立属性
public WorkspaceState State { get; } = new();

// XAML绑定路径变更
// 旧: {Binding IsBusy}
// 新: {Binding State.IsBusy}
```

### 2. Handler完全委托模式

**问题**: Handler已存在但ViewModel仍有重复逻辑。

**解决方案**: 将Command实现完全委托给Handler，ViewModel只负责定义和协调。

```csharp
// ======= 新文件: PrescriptionEditHandler.cs =======
namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 处方编辑Handler
    /// OpenSpec: slim-workspace-viewmodel - 完全委托模式
    /// </summary>
    public class PrescriptionEditHandler
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly ICommonDialogService _dialogService;
        private readonly ILogger<PrescriptionEditHandler> _logger;

        // 状态访问器（由ViewModel提供）
        private readonly Func<Guid> _getMedicalCaseId;
        private readonly Func<PrescriptionItem> _getPrescriptionItem;
        private readonly Func<bool> _getIsReadOnly;
        private readonly Action<bool, string?> _setIsBusy;

        public PrescriptionEditHandler(
            IMedicalCaseService medicalCaseService,
            ICommonDialogService dialogService,
            ILogger<PrescriptionEditHandler> logger,
            Func<Guid> getMedicalCaseId,
            Func<PrescriptionItem> getPrescriptionItem,
            Func<bool> getIsReadOnly,
            Action<bool, string?> setIsBusy)
        {
            _medicalCaseService = medicalCaseService;
            _dialogService = dialogService;
            _logger = logger;
            _getMedicalCaseId = getMedicalCaseId;
            _getPrescriptionItem = getPrescriptionItem;
            _getIsReadOnly = getIsReadOnly;
            _setIsBusy = setIsBusy;
        }

        /// <summary>
        /// 保存处方
        /// </summary>
        public async Task<bool> SavePrescriptionAsync()
        {
            var medicalCaseId = _getMedicalCaseId();
            if (medicalCaseId == Guid.Empty) return false;

            _setIsBusy(true, "保存处方中...");
            try
            {
                var prescriptionItem = _getPrescriptionItem();
                var inputDto = prescriptionItem.ToInputDto();

                var result = await _medicalCaseService.SavePrescriptionAsync(
                    medicalCaseId, inputDto);

                if (result != null)
                {
                    _logger.LogInformation("处方保存成功 - MedicalCaseId={Id}", medicalCaseId);
                    return true;
                }

                await _dialogService.ShowErrorAsync("保存处方失败");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方失败");
                await _dialogService.ShowErrorAsync($"保存处方失败: {ex.Message}");
                return false;
            }
            finally
            {
                _setIsBusy(false, null);
            }
        }

        /// <summary>
        /// 清空处方
        /// </summary>
        public async Task<bool> ClearPrescriptionAsync()
        {
            if (_getIsReadOnly()) return false;

            var confirmed = await _dialogService.ShowConfirmAsync(
                "确认清空", "确定要清空当前处方吗？");

            if (!confirmed) return false;

            _getPrescriptionItem().Clear();
            return true;
        }

        /// <summary>
        /// 检查是否可以编辑处方
        /// </summary>
        public bool CanEditPrescription()
        {
            return !_getIsReadOnly() && _getMedicalCaseId() != Guid.Empty;
        }
    }
}
```

**ViewModel中的使用**:

```csharp
// ViewModel只负责定义Command和委托
private PrescriptionEditHandler _prescriptionEditHandler;

private void InitializeHandlers()
{
    _prescriptionEditHandler = new PrescriptionEditHandler(
        _medicalCaseService,
        _dialogService,
        _loggerFactory.CreateLogger<PrescriptionEditHandler>(),
        () => MedicalCaseId,
        () => PrescriptionItem,
        () => State.IsReadOnly,
        (busy, msg) => State.SetBusy(busy, msg));
}

// Command定义简化
public AsyncDelegateCommand SavePrescriptionCommand { get; private set; }

private void InitializeCommands()
{
    SavePrescriptionCommand = new AsyncDelegateCommand(
        () => _prescriptionEditHandler.SavePrescriptionAsync(),
        () => _prescriptionEditHandler.CanEditPrescription());
}
```

### 3. 导航Handler模式

**问题**: INavigationAware实现过于复杂（~205行）。

**解决方案**: 提取导航逻辑到专门的Handler。

```csharp
// ======= 新文件: WorkspaceNavigationHandler.cs =======
namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 工作台导航Handler
    /// OpenSpec: slim-workspace-viewmodel - 导航逻辑提取
    /// </summary>
    public class WorkspaceNavigationHandler
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IPatientService _patientService;
        private readonly ILogger<WorkspaceNavigationHandler> _logger;

        public WorkspaceNavigationHandler(
            IMedicalCaseService medicalCaseService,
            IPatientService patientService,
            ILogger<WorkspaceNavigationHandler> logger)
        {
            _medicalCaseService = medicalCaseService;
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// 解析导航参数
        /// </summary>
        public NavigationData ParseNavigationContext(NavigationContext context)
        {
            var data = new NavigationData();

            // 解析医案ID
            if (context.Parameters.TryGetValue<Guid>("medicalCaseId", out var mcId))
            {
                data.MedicalCaseId = mcId;
            }

            // 解析患者ID
            if (context.Parameters.TryGetValue<Guid>("patientId", out var pId))
            {
                data.PatientId = pId;
            }

            // 解析只读模式
            if (context.Parameters.TryGetValue<bool>("isReadOnly", out var readOnly))
            {
                data.IsReadOnly = readOnly;
            }

            // 解析来源
            if (context.Parameters.TryGetValue<string>("source", out var source))
            {
                data.Source = source;
            }

            return data;
        }

        /// <summary>
        /// 加载工作台数据
        /// </summary>
        public async Task<WorkspaceLoadResult> LoadWorkspaceDataAsync(NavigationData navData)
        {
            var result = new WorkspaceLoadResult();

            try
            {
                // 加载医案
                if (navData.MedicalCaseId != Guid.Empty)
                {
                    result.MedicalCase = await _medicalCaseService
                        .GetByIdAsync(navData.MedicalCaseId);
                }

                // 加载患者
                var patientId = result.MedicalCase?.PatientId ?? navData.PatientId;
                if (patientId != Guid.Empty)
                {
                    result.Patient = await _patientService
                        .GetPatientDetailAsync(patientId);
                }

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载工作台数据失败");
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 检查是否可以离开（有未保存更改时）
        /// </summary>
        public bool CanLeave(bool hasUnsavedChanges, out string? warningMessage)
        {
            if (hasUnsavedChanges)
            {
                warningMessage = "有未保存的更改，确定要离开吗？";
                return false;
            }

            warningMessage = null;
            return true;
        }
    }

    public class NavigationData
    {
        public Guid MedicalCaseId { get; set; }
        public Guid PatientId { get; set; }
        public bool IsReadOnly { get; set; }
        public string? Source { get; set; }
    }

    public class WorkspaceLoadResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public MedicalCaseDetailDto? MedicalCase { get; set; }
        public PatientDetailDto? Patient { get; set; }
    }
}
```

### 4. 简化后的ViewModel结构

```csharp
/// <summary>
/// 医案工作台ViewModel
/// OpenSpec: slim-workspace-viewmodel - 重构后 < 500行
/// </summary>
public partial class MedicalCaseWorkspaceViewModel : NavigableViewModelBase
{
    #region Fields (~20行)

    private readonly ILogger<MedicalCaseWorkspaceViewModel> _logger;

    // Handlers (完全委托)
    private WorkspaceNavigationHandler _navigationHandler;
    private PrescriptionEditHandler _prescriptionEditHandler;
    private WorkspacePendingQueueHandler _pendingQueueHandler;
    private MedicalCaseWorkspaceCoordinator _coordinator;

    #endregion

    #region Properties (~50行)

    /// <summary>聚合状态对象</summary>
    public WorkspaceState State { get; } = new();

    /// <summary>医案ID</summary>
    [ObservableProperty]
    private Guid _medicalCaseId;

    /// <summary>医案数据</summary>
    [ObservableProperty]
    private MedicalCaseItem? _medicalCaseItem;

    /// <summary>诊断数据</summary>
    [ObservableProperty]
    private ConsultationItem? _consultationItem;

    /// <summary>处方数据</summary>
    [ObservableProperty]
    private PrescriptionItem? _prescriptionItem;

    #endregion

    #region Commands (~30行)

    public AsyncDelegateCommand SaveDraftCommand { get; private set; }
    public AsyncDelegateCommand CompleteCommand { get; private set; }
    public AsyncDelegateCommand CancelCommand { get; private set; }
    public AsyncDelegateCommand SavePrescriptionCommand { get; private set; }
    public AsyncDelegateCommand ClearPrescriptionCommand { get; private set; }
    public DelegateCommand NavigateBackCommand { get; private set; }

    #endregion

    #region Constructor (~50行)

    public MedicalCaseWorkspaceViewModel(
        IMedicalCaseService medicalCaseService,
        IPatientService patientService,
        IPendingQueueManager pendingQueueManager,
        ICommonDialogService dialogService,
        INavigationCoordinator navigationCoordinator,
        ISessionManager sessionManager,
        ILoggerFactory loggerFactory)
        : base(navigationCoordinator, sessionManager)
    {
        _logger = loggerFactory.CreateLogger<MedicalCaseWorkspaceViewModel>();

        InitializeHandlers(/* ... */);
        InitializeCommands();
    }

    private void InitializeHandlers(/* ... */)
    {
        // 初始化各Handler，传入状态访问器
        _navigationHandler = new WorkspaceNavigationHandler(/* ... */);
        _prescriptionEditHandler = new PrescriptionEditHandler(/* ... */);
        _pendingQueueHandler = new WorkspacePendingQueueHandler(/* ... */);
        _coordinator = new MedicalCaseWorkspaceCoordinator(/* ... */);
    }

    private void InitializeCommands()
    {
        SaveDraftCommand = new AsyncDelegateCommand(
            () => _coordinator.SaveDraftAsync(),
            () => _coordinator.CanSaveDraft());

        CompleteCommand = new AsyncDelegateCommand(
            () => _coordinator.CompleteAsync(),
            () => _coordinator.CanComplete());

        CancelCommand = new AsyncDelegateCommand(
            () => _coordinator.CancelAsync(),
            () => _coordinator.CanCancel());

        SavePrescriptionCommand = new AsyncDelegateCommand(
            () => _prescriptionEditHandler.SavePrescriptionAsync(),
            () => _prescriptionEditHandler.CanEditPrescription());

        // ... 其他命令
    }

    #endregion

    #region INavigationAware (~80行)

    public override async void OnNavigatedTo(NavigationContext context)
    {
        base.OnNavigatedTo(context);

        var navData = _navigationHandler.ParseNavigationContext(context);
        MedicalCaseId = navData.MedicalCaseId;
        State.IsReadOnly = navData.IsReadOnly;

        State.SetBusy(true, "加载中...");
        try
        {
            var result = await _navigationHandler.LoadWorkspaceDataAsync(navData);
            if (result.IsSuccess)
            {
                UpdateFromLoadResult(result);
            }
            else
            {
                await ShowErrorAsync(result.ErrorMessage ?? "加载失败");
            }
        }
        finally
        {
            State.SetBusy(false, null);
        }
    }

    public override void OnNavigatedFrom(NavigationContext context)
    {
        base.OnNavigatedFrom(context);
        // 清理逻辑
    }

    public override void ConfirmNavigationRequest(
        NavigationContext context,
        Action<bool> continuationCallback)
    {
        if (_navigationHandler.CanLeave(HasUnsavedChanges, out var warning))
        {
            continuationCallback(true);
            return;
        }

        // 显示确认对话框
        ShowConfirmLeaveDialog(warning, continuationCallback);
    }

    #endregion

    #region Private Methods (~100行)

    private void UpdateFromLoadResult(WorkspaceLoadResult result)
    {
        // 更新State
        State.UpdateFromPatient(result.Patient);

        // 更新数据项
        if (result.MedicalCase != null)
        {
            MedicalCaseItem = MedicalCaseItemMapper.ToItem(result.MedicalCase);
            ConsultationItem = result.MedicalCase.Consultation != null
                ? ConsultationMapper.ToItem(result.MedicalCase.Consultation)
                : new ConsultationItem();
            PrescriptionItem = result.MedicalCase.Prescription != null
                ? PrescriptionMapper.ToItem(result.MedicalCase.Prescription)
                : new PrescriptionItem();
        }

        // 更新命令状态
        RaiseCanExecuteChanged();
    }

    private void RaiseCanExecuteChanged()
    {
        SaveDraftCommand.RaiseCanExecuteChanged();
        CompleteCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
        SavePrescriptionCommand.RaiseCanExecuteChanged();
    }

    private bool HasUnsavedChanges =>
        ConsultationItem?.IsDirty == true ||
        PrescriptionItem?.IsDirty == true;

    #endregion
}
```

**预估行数**: ~330行

## XAML绑定迁移

### 绑定路径变更清单

| 旧路径 | 新路径 | 说明 |
|--------|--------|------|
| `{Binding IsBusy}` | `{Binding State.IsBusy}` | 忙碌状态 |
| `{Binding IsReadOnly}` | `{Binding State.IsReadOnly}` | 只读状态 |
| `{Binding StatusMessage}` | `{Binding State.StatusMessage}` | 状态消息 |
| `{Binding PatientName}` | `{Binding State.PatientName}` | 患者姓名 |
| `{Binding PatientGender}` | `{Binding State.PatientGender}` | 患者性别 |
| `{Binding PatientAge}` | `{Binding State.PatientAge}` | 患者年龄 |
| `{Binding EditMode}` | `{Binding State.EditMode}` | 编辑模式 |

### 兼容性适配器（可选）

如果需要保持XAML不变，可以在ViewModel中添加属性包装器：

```csharp
// 兼容性包装器 - 待后续提案移除
// OpenSpec: slim-workspace-viewmodel - 兼容设计
public bool IsBusy
{
    get => State.IsBusy;
    set => State.IsBusy = value;
}
```

## 文件变更清单

### 新增文件

| 文件 | 行数 | 说明 |
|------|------|------|
| `Components/WorkspaceState.cs` | ~120 | 状态聚合对象 |
| `Services/PrescriptionEditHandler.cs` | ~150 | 处方编辑Handler |
| `Services/WorkspaceNavigationHandler.cs` | ~120 | 导航逻辑Handler |

### 修改文件

| 文件 | 变更 |
|------|------|
| `MedicalCaseWorkspaceViewModel.cs` | 从1491行精简到<500行 |
| `MedicalCaseWorkspaceCoordinator.cs` | 扩展生命周期操作 |
| `WorkspacePendingQueueHandler.cs` | 接收更多委托逻辑 |
| `MedicalCaseWorkspaceView.xaml` | 更新绑定路径 |

### 删除内容

| 内容 | 说明 |
|------|------|
| ViewModel内嵌适配器类 | 移到独立文件或删除 |
| 重复的业务逻辑 | 已委托给Handler |
| 冗余属性定义 | 已聚合到State |

---

**设计者**: Claude Code
**日期**: 2026-01-12
**状态**: Draft
