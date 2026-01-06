# Technical Design: standardize-viewmodel-framework

## 1. 架构设计

### 1.1 目标基类体系

```
CommunityToolkit.Mvvm.ObservableObject (框架基类)
│
├─ CoreViewModelBase (项目核心基类)
│   ├─ IsBusy, ErrorMessage, StatusMessage
│   ├─ Dispose管理
│   └─ UI线程调度
│
├─ NavigableViewModelBase : CoreViewModelBase, INavigationAware
│   ├─ 导航参数处理
│   ├─ 导航生命周期
│   └─ KeepAlive控制
│
├─ DialogViewModelBase : CoreViewModelBase, IDialogAware
│   ├─ 对话框参数
│   ├─ 结果返回
│   └─ 关闭请求
│
├─ ValidatingViewModelBase : CoreViewModelBase, INotifyDataErrorInfo
│   ├─ FluentValidation集成
│   ├─ 属性级验证
│   └─ 表单级验证
│
└─ PageViewModelBase : NavigableViewModelBase
    ├─ 页面标题
    ├─ 刷新机制
    └─ 权限控制
```

### 1.2 待删除的基类

迁移完成后删除：

| 基类 | 原因 | 替代方案 |
|------|------|----------|
| ViewModelBase | Prism BindableBase | CoreViewModelBase |
| LightViewModelBase | 功能重叠 | CoreViewModelBase |
| UnifiedViewModelBase | 功能整合 | NavigableViewModelBase |
| UnifiedListViewModelBase | 功能整合 | PageViewModelBase |
| DetailViewModelBase | 功能整合 | NavigableViewModelBase |
| ComposableViewModelBase | 功能整合 | CoreViewModelBase |
| ValidatableModelBase | 用于Item类 | 保留给Item |

## 2. 核心基类设计

### 2.1 CoreViewModelBase (已存在，需验证)

```csharp
/// <summary>
/// 核心ViewModel基类 - 提供最小必要功能
/// </summary>
public abstract partial class CoreViewModelBase : ObservableObject, IDisposable
{
    // === 可观察属性 (源生成器) ===
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    // === 计算属性 ===
    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // === 日志 ===
    protected ILogger Logger { get; }

    // === Dispose管理 ===
    private readonly CompositeDisposable _disposables = new();
    protected void AddDisposable(IDisposable disposable);

    // === UI线程 ===
    protected void RunOnUIThread(Action action);
    protected Task RunOnUIThreadAsync(Func<Task> action);
}
```

### 2.2 NavigableViewModelBase

```csharp
/// <summary>
/// 支持Prism导航的ViewModel基类
/// </summary>
public abstract partial class NavigableViewModelBase
    : CoreViewModelBase, INavigationAware, IRegionMemberLifetime, IConfirmNavigationRequest
{
    // === 导航状态 ===
    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private bool _isActive;

    // === IRegionMemberLifetime ===
    public virtual bool KeepAlive => true;

    // === INavigationAware ===
    public virtual void OnNavigatedTo(NavigationContext navigationContext)
    {
        IsActive = true;
        OnNavigatedToCore(navigationContext);
    }

    public virtual void OnNavigatedFrom(NavigationContext navigationContext)
    {
        IsActive = false;
        OnNavigatedFromCore(navigationContext);
    }

    public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        => true;

    // === IConfirmNavigationRequest ===
    public virtual void ConfirmNavigationRequest(
        NavigationContext navigationContext,
        Action<bool> continuationCallback)
    {
        continuationCallback(CanNavigateAway());
    }

    // === 可重写钩子 ===
    protected virtual void OnNavigatedToCore(NavigationContext context) { }
    protected virtual void OnNavigatedFromCore(NavigationContext context) { }
    protected virtual bool CanNavigateAway() => true;

    // === 导航参数提取 ===
    protected T? GetNavigationParameter<T>(NavigationContext context, string key)
    {
        if (context.Parameters.TryGetValue(key, out T value))
            return value;
        return default;
    }
}
```

### 2.3 DialogViewModelBase

```csharp
/// <summary>
/// 对话框ViewModel基类
/// </summary>
public abstract partial class DialogViewModelBase
    : CoreViewModelBase, IDialogAware
{
    // === 对话框标题 ===
    [ObservableProperty]
    private string _title = string.Empty;

    // === IDialogAware ===
    public event Action<IDialogResult>? RequestClose;

    public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed() { }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
        OnDialogOpenedCore(parameters);
    }

    // === 可重写钩子 ===
    protected virtual void OnDialogOpenedCore(IDialogParameters parameters) { }

    // === 关闭方法 ===
    protected void CloseDialog(ButtonResult result = ButtonResult.None)
    {
        RequestClose?.Invoke(new DialogResult(result));
    }

    protected void CloseDialog(IDialogParameters parameters, ButtonResult result = ButtonResult.OK)
    {
        RequestClose?.Invoke(new DialogResult(result, parameters));
    }

    // === 命令 (源生成器) ===
    [RelayCommand]
    protected virtual void Cancel()
    {
        CloseDialog(ButtonResult.Cancel);
    }
}
```

### 2.4 ValidatingViewModelBase

```csharp
/// <summary>
/// 带验证的ViewModel基类
/// </summary>
public abstract partial class ValidatingViewModelBase
    : CoreViewModelBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    // === INotifyDataErrorInfo ===
    public bool HasErrors => _errors.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _errors.SelectMany(e => e.Value);

        return _errors.TryGetValue(propertyName, out var errors)
            ? errors
            : Enumerable.Empty<string>();
    }

    // === 验证方法 ===
    protected void SetErrors(string propertyName, IEnumerable<string> errors)
    {
        _errors[propertyName] = errors.ToList();
        OnErrorsChanged(propertyName);
    }

    protected void ClearErrors(string? propertyName = null)
    {
        if (propertyName == null)
        {
            _errors.Clear();
            OnPropertyChanged(nameof(HasErrors));
        }
        else if (_errors.Remove(propertyName))
        {
            OnErrorsChanged(propertyName);
        }
    }

    protected virtual void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    // === FluentValidation集成 ===
    protected async Task<bool> ValidateAsync<T>(T instance, IValidator<T> validator)
    {
        ClearErrors();
        var result = await validator.ValidateAsync(instance);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                SetErrors(error.PropertyName, new[] { error.ErrorMessage });
            }
        }

        return result.IsValid;
    }
}
```

### 2.5 PageViewModelBase

```csharp
/// <summary>
/// 主内容页面ViewModel基类
/// </summary>
public abstract partial class PageViewModelBase : NavigableViewModelBase
{
    // === 页面信息 ===
    [ObservableProperty]
    private string _pageTitle = string.Empty;

    [ObservableProperty]
    private string _pageDescription = string.Empty;

    // === 刷新命令 ===
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        try
        {
            SetBusy(true, "正在刷新...");
            ClearError();
            await OnRefreshAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "刷新失败");
            SetError("刷新失败: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    protected virtual bool CanRefresh() => !IsBusy;

    protected virtual Task OnRefreshAsync() => Task.CompletedTask;

    // === 首次加载 ===
    protected override async void OnNavigatedToCore(NavigationContext context)
    {
        base.OnNavigatedToCore(context);

        if (!IsInitialized)
        {
            await InitializeAsync(context);
            IsInitialized = true;
        }
    }

    protected virtual Task InitializeAsync(NavigationContext context) => Task.CompletedTask;
}
```

## 3. 迁移模式

### 3.1 属性迁移

**Before (Prism BindableBase)**:
```csharp
private bool _isLoading;
public bool IsLoading
{
    get => _isLoading;
    set => SetProperty(ref _isLoading, value);
}
```

**After (CommunityToolkit)**:
```csharp
[ObservableProperty]
private bool _isLoading;
```

### 3.2 命令迁移

**Before (Prism DelegateCommand)**:
```csharp
public DelegateCommand SaveCommand { get; }
public DelegateCommand<Patient> SelectCommand { get; }

public MyViewModel()
{
    SaveCommand = new DelegateCommand(ExecuteSave, CanSave)
        .ObservesProperty(() => IsValid);
    SelectCommand = new DelegateCommand<Patient>(ExecuteSelect);
}

private async void ExecuteSave() { ... }
private bool CanSave() => IsValid && !IsBusy;
```

**After (CommunityToolkit RelayCommand)**:
```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private async Task SaveAsync() { ... }

private bool CanSave() => IsValid && !IsBusy;

[RelayCommand]
private void Select(Patient patient) { ... }
```

### 3.3 属性变更通知

**Before**:
```csharp
private string _name;
public string Name
{
    get => _name;
    set
    {
        if (SetProperty(ref _name, value))
        {
            RaisePropertyChanged(nameof(DisplayName));
            SaveCommand.RaiseCanExecuteChanged();
        }
    }
}
```

**After**:
```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(DisplayName))]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private string _name;

public string DisplayName => $"患者: {Name}";
```

## 4. Item类不迁移

### 4.1 原因

Item类必须保持BindableBase以兼容Mapperly：

```
Mapperly源生成器 ←→ Item类属性 (必须编译时可见)
                    ↓
              BindableBase显式属性 (可见)
              [ObservableProperty]生成属性 (不可见)
```

### 4.2 Item类标准

```csharp
/// <summary>
/// Item类示例 - 保持BindableBase
/// </summary>
public class PatientItem : BindableBase
{
    // 显式属性 - Mapperly可识别
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    // 计算属性 - 无需映射
    public string DisplayName => $"{Name} (ID: {Id:N})";
}
```

## 5. 迁移清单

### 5.1 Shell层 (优先级高)

| 文件 | 当前基类 | 目标基类 |
|------|----------|----------|
| MainWindowViewModel | ViewModelBase | CoreViewModelBase |
| AccountSettingsViewModel | ObservableObject | CoreViewModelBase |
| LoginViewModel | ViewModelBase | DialogViewModelBase |

### 5.2 Roles层

| 文件 | 当前基类 | 目标基类 |
|------|----------|----------|
| ClinicalHomeViewModel | NavigableViewModelBase | PageViewModelBase |
| AdminHomeViewModel | NavigableViewModelBase | PageViewModelBase |
| MedicalCaseWorkspaceViewModel | NavigableViewModelBase | PageViewModelBase |
| PatientSelectionViewModel | NavigableViewModelBase | DialogViewModelBase |

### 5.3 Modules层

按模块迁移，每模块包含：
- MasterDetailViewModel
- 各Dialog ViewModel
- Component ViewModel

## 6. 事件系统集成

### 6.1 事件订阅模式

**当前模式 (保持)**:
```csharp
// 使用 Prism PubSubEvent
EventAggregator.GetEvent<CaseEvents.ConsultationCompletedEvent>()
    .Subscribe(OnConsultationCompleted, ThreadOption.UIThread);
```

**问题**: 手动管理订阅生命周期容易遗漏，导致内存泄漏

**解决方案**: 在基类中提供自动管理的订阅方法

### 6.2 EventSubscriptionManager 设计

```csharp
/// <summary>
/// 事件订阅管理器 - 自动管理订阅生命周期
/// </summary>
public class EventSubscriptionManager : IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly List<SubscriptionToken> _tokens = new();

    public EventSubscriptionManager(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    /// <summary>
    /// 订阅事件 (UI线程)
    /// </summary>
    public void Subscribe<TEvent, TPayload>(Action<TPayload> handler)
        where TEvent : PubSubEvent<TPayload>, new()
    {
        var token = _eventAggregator.GetEvent<TEvent>()
            .Subscribe(handler, ThreadOption.UIThread);
        _tokens.Add(token);
    }

    /// <summary>
    /// 订阅事件 (指定线程选项)
    /// </summary>
    public void Subscribe<TEvent, TPayload>(
        Action<TPayload> handler,
        ThreadOption threadOption,
        bool keepSubscriberReferenceAlive = false,
        Predicate<TPayload>? filter = null)
        where TEvent : PubSubEvent<TPayload>, new()
    {
        var token = _eventAggregator.GetEvent<TEvent>()
            .Subscribe(handler, threadOption, keepSubscriberReferenceAlive, filter);
        _tokens.Add(token);
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    public void Publish<TEvent, TPayload>(TPayload payload)
        where TEvent : PubSubEvent<TPayload>, new()
    {
        _eventAggregator.GetEvent<TEvent>().Publish(payload);
    }

    public void Dispose()
    {
        foreach (var token in _tokens)
        {
            token.Dispose();
        }
        _tokens.Clear();
    }
}
```

### 6.3 基类中的事件支持

```csharp
public abstract partial class CoreViewModelBase : ObservableObject, IDisposable
{
    // === 事件管理 ===
    private EventSubscriptionManager? _eventManager;

    /// <summary>
    /// 事件订阅管理器 (延迟初始化)
    /// </summary>
    protected EventSubscriptionManager Events
    {
        get => _eventManager ??= new EventSubscriptionManager(EventAggregator);
    }

    /// <summary>
    /// Prism事件聚合器
    /// </summary>
    protected IEventAggregator EventAggregator { get; }

    // 构造函数
    protected CoreViewModelBase(ILoggerFactory loggerFactory, IEventAggregator eventAggregator)
    {
        LoggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger(GetType());
        EventAggregator = eventAggregator;
    }

    // Dispose时自动清理订阅
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _eventManager?.Dispose();
            _disposables.Dispose();
            OnDisposing();
        }
        base.Dispose(disposing);
    }
}
```

### 6.4 ViewModel中的事件使用

**Before (手动管理)**:
```csharp
public class MedicalCaseWorkspaceViewModel : ViewModelBase
{
    public MedicalCaseWorkspaceViewModel()
    {
        EventAggregator.GetEvent<CaseEvents.ConsultationCompletedEvent>()
            .Subscribe(OnConsultationCompleted, ThreadOption.UIThread);
        EventAggregator.GetEvent<CaseEvents.PrescriptionCompletedEvent>()
            .Subscribe(OnPrescriptionCompleted, ThreadOption.UIThread);
    }

    // 必须手动清理
    public override void Destroy()
    {
        EventAggregator.GetEvent<CaseEvents.ConsultationCompletedEvent>()
            .Unsubscribe(OnConsultationCompleted);
        EventAggregator.GetEvent<CaseEvents.PrescriptionCompletedEvent>()
            .Unsubscribe(OnPrescriptionCompleted);
    }
}
```

**After (自动管理)**:
```csharp
public partial class MedicalCaseWorkspaceViewModel : PageViewModelBase
{
    protected override void OnNavigatedToCore(NavigationContext context)
    {
        base.OnNavigatedToCore(context);

        // 使用Events管理器，Dispose时自动清理
        Events.Subscribe<CaseEvents.ConsultationCompletedEvent, CaseConsultationCompletedPayload>(
            OnConsultationCompleted);
        Events.Subscribe<CaseEvents.PrescriptionCompletedEvent, CasePrescriptionCompletedPayload>(
            OnPrescriptionCompleted);
    }

    private void OnConsultationCompleted(CaseConsultationCompletedPayload payload)
    {
        if (payload.MedicalCaseId != CurrentCaseId) return;
        // 处理逻辑...
    }

    // 无需手动Unsubscribe - Dispose自动处理
}
```

### 6.5 事件过滤器模式

```csharp
// 带过滤器的订阅
Events.Subscribe<CaseEvents.WorkspaceChangedEvent, WorkspaceChangedPayload>(
    OnWorkspaceChanged,
    ThreadOption.UIThread,
    keepSubscriberReferenceAlive: false,
    filter: payload => payload.MedicalCaseFlowId == CurrentFlowId
);
```

## 7. 导航机制详细设计

### 7.1 导航参数传递

**当前模式 (保持)**:
```csharp
// 导航时传递参数
var parameters = new NavigationParameters
{
    { "PatientId", patientId },
    { "Mode", EditMode.Create }
};
_regionManager.RequestNavigate(RegionNames.MainContent, "PatientDetail", parameters);
```

**在ViewModel中接收**:
```csharp
protected override void OnNavigatedToCore(NavigationContext context)
{
    PatientId = GetNavigationParameter<Guid>(context, "PatientId");
    Mode = GetNavigationParameter<EditMode>(context, "Mode");
}
```

### 7.2 导航确认 (未保存变更)

```csharp
public abstract partial class NavigableViewModelBase
    : CoreViewModelBase, INavigationAware, IConfirmNavigationRequest
{
    /// <summary>
    /// 是否有未保存的变更
    /// </summary>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    public void ConfirmNavigationRequest(
        NavigationContext navigationContext,
        Action<bool> continuationCallback)
    {
        if (HasUnsavedChanges)
        {
            // 显示确认对话框
            var result = ShowUnsavedChangesDialog();
            continuationCallback(result);
        }
        else
        {
            continuationCallback(true);
        }
    }

    /// <summary>
    /// 子类可重写以自定义未保存变更提示
    /// </summary>
    protected virtual bool ShowUnsavedChangesDialog()
    {
        // 默认实现：显示标准确认对话框
        return DialogService.ShowConfirmation(
            "有未保存的更改，确定要离开吗？",
            "未保存的更改");
    }
}
```

### 7.3 区域导航日志

```csharp
public abstract partial class PageViewModelBase : NavigableViewModelBase
{
    protected override void OnNavigatedToCore(NavigationContext context)
    {
        base.OnNavigatedToCore(context);
        Logger.LogInformation(
            "导航到页面: {PageType}, 参数: {@Parameters}",
            GetType().Name,
            context.Parameters.Select(p => new { p.Key, p.Value }));
    }

    protected override void OnNavigatedFromCore(NavigationContext context)
    {
        Logger.LogDebug("离开页面: {PageType}", GetType().Name);
        base.OnNavigatedFromCore(context);
    }
}
```

### 7.4 KeepAlive 策略

```csharp
public abstract partial class NavigableViewModelBase
    : CoreViewModelBase, IRegionMemberLifetime
{
    /// <summary>
    /// 是否在导航离开后保持实例
    /// </summary>
    /// <remarks>
    /// true: 视图实例保持，再次导航时复用 (适合重量级页面)
    /// false: 导航离开后销毁，每次导航创建新实例 (适合临时页面)
    /// </remarks>
    public virtual bool KeepAlive => true;
}

// 对话框默认不保持
public abstract partial class DialogViewModelBase
{
    public virtual bool KeepAlive => false;
}
```

## 8. 异步命令模式

### 8.1 带取消支持的异步命令

```csharp
public partial class DataLoadingViewModel : PageViewModelBase
{
    private CancellationTokenSource? _loadCts;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            SetBusy(true, "正在加载数据...");
            var data = await _api.GetDataAsync(cancellationToken);
            // 处理数据
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("数据加载已取消");
        }
        catch (Exception ex)
        {
            SetError($"加载失败: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // 自动生成 LoadDataCancelCommand
}
```

### 8.2 命令执行状态

```csharp
[RelayCommand]
private async Task SaveAsync()
{
    // SaveCommand.IsRunning 自动为 true
    await _api.SaveAsync(Data);
}

// XAML中绑定
// <Button Command="{Binding SaveCommand}"
//         IsEnabled="{Binding SaveCommand.IsRunning, Converter={StaticResource InverseBool}}" />
```

### 8.3 命令异常处理

```csharp
public abstract partial class CoreViewModelBase
{
    /// <summary>
    /// 异步执行包装 - 统一异常处理
    /// </summary>
    protected async Task ExecuteWithErrorHandlingAsync(
        Func<Task> action,
        string operationName,
        bool showBusy = true)
    {
        try
        {
            if (showBusy) SetBusy(true, $"正在{operationName}...");
            ClearError();
            await action();
        }
        catch (ApiException ex)
        {
            Logger.LogWarning(ex, "{Operation} API错误", operationName);
            SetError($"{operationName}失败: {ex.UserMessage}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Operation} 失败", operationName);
            SetError($"{operationName}失败: {ex.Message}");
        }
        finally
        {
            if (showBusy) SetBusy(false);
        }
    }
}

// 使用
[RelayCommand]
private Task SaveAsync() => ExecuteWithErrorHandlingAsync(
    async () =>
    {
        var dto = _mappingService.ToInputDto(CurrentItem);
        await _api.SaveAsync(dto);
    },
    "保存");
```

## 9. 验证策略

### 9.1 编译验证
- 零编译错误
- 零Mapperly警告 (Item类)

### 9.2 功能验证
- 导航正常 (进入/离开/参数传递)
- 命令执行正常 (同步/异步/取消)
- 属性绑定正常 (双向/单向)
- 事件订阅/发布正常
- 未保存变更提示正常

### 9.3 回归测试
- 现有单元测试通过
- UI功能手动验证清单
