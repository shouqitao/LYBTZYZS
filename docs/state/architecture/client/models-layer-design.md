# LYBT.Desktop.Models层架构设计 - ViewModelBase体系与MVVM模式

## 📋 文档元数据

- **文档类型**: 架构设计文档（Explanation - Architecture）
- **适用范围**: Client端 - Desktop WPF应用
- **架构层级**: Models层（MVVM模式核心）
- **版本**: v1.0
- **最后更新**: 2025-10-29
- **相关Epic**: Issue #1718 Phase 1 - 架构文档完善

## 📖 文档概述

本文档详细说明 `LYBT.Desktop.Models` 层的架构设计，包括ViewModelBase基类体系、MVVM模式实现、状态管理、命令绑定、验证支持、资源管理等核心机制。Models层是Client端MVVM架构的核心，为所有业务模块提供统一的ViewModel基础设施。

### 关键设计目标

1. **简化MVVM开发**：提供统一的ViewModel基类，封装常用功能，避免重复代码
2. **状态管理标准化**：统一管理Loading、Busy、Error等状态，简化UI绑定
3. **异步操作安全化**：提供ExecuteSafelyAsync封装，自动处理异常和状态
4. **验证支持完整性**：实现INotifyDataErrorInfo，支持DataAnnotations和FluentValidation
5. **资源管理自动化**：实现IDisposable，自动清理订阅和资源
6. **导航与生命周期**：实现INavigationAware，支持Prism区域导航

---

## 1. ViewModelBase基类体系

### 1.1 基类继承链

```
Prism.BindableBase (Prism框架基类)
  └─ ViewModelBase (核心基类)
      └─ UnifiedViewModelBase (统一ViewModel基类)
          └─ UnifiedListViewModelBase<T> (列表ViewModel基类)
```

**职责分离**：
- **BindableBase**：提供INotifyPropertyChanged基础实现（SetProperty方法）
- **ViewModelBase**：提供状态管理、异步执行、错误处理、验证、资源管理
- **UnifiedViewModelBase**：扩展导航、消息对话框、会话管理、页面生命周期
- **UnifiedListViewModelBase\<T\>**：专注列表操作（分页、搜索、批量操作）

### 1.2 ViewModelBase核心功能（源文件：536行）

#### 1.2.1 状态属性（6个核心状态）

```csharp
public abstract class ViewModelBase : BindableBase, IDisposable, INotifyDataErrorInfo
{
    // 加载状态
    public bool IsLoading { get; set; }

    // 忙碌状态（操作执行中）
    public bool IsBusy { get; set; }

    // 状态消息（显示在状态栏）
    public string StatusMessage { get; set; }

    // 错误状态标志
    public bool HasError { get; protected set; }

    // 错误消息
    public string ErrorMessage { get; protected set; }

    // 验证错误集合
    private readonly Dictionary<string, List<string>> _validationErrors = new();
}
```

**状态属性设计原则**：
1. **IsLoading**：数据加载状态，用于显示进度条或Spinner
2. **IsBusy**：操作执行状态，用于禁用操作按钮（防止重复点击）
3. **HasError**：错误标志，根据ErrorMessage自动更新
4. **ErrorMessage**：用户友好的错误消息，自动触发MessageBox
5. **StatusMessage**：操作状态提示（3秒自动消失）
6. **ValidationErrors**：属性级验证错误，支持XAML索引器绑定

#### 1.2.2 异步安全执行（ExecuteSafelyAsync）

**方法签名**：
```csharp
// 无返回值版本
protected async Task ExecuteSafelyAsync(
    Func<Task> operation,
    string? operationName = null,
    bool showProgress = true)

// 有返回值版本
protected async Task<T?> ExecuteSafelyAsync<T>(
    Func<Task<T>> operation,
    string? operationName = null,
    T? defaultValue = default,
    bool showProgress = true)
```

**执行流程**（自动化状态管理）：

```
1. try块开始
   └─ IsBusy = true
   └─ ClearError()
   └─ StatusMessage = "正在{operationName}..."

2. 执行operation()
   └─ await operation().ConfigureAwait(false)

3. 成功完成
   └─ StatusMessage = "{operationName}完成"
   └─ 延迟3秒后自动清除StatusMessage

4. catch (TaskCanceledException)
   └─ StatusMessage = "{operationName}已取消"
   └─ 记录日志（LogInformation）

5. catch (Exception ex)
   └─ StatusMessage = "{operationName}失败"
   └─ HandleError(ex, operationName)

6. finally
   └─ IsBusy = false
```

**设计亮点**：
- ✅ **自动状态管理**：无需手动设置IsBusy、HasError
- ✅ **统一错误处理**：所有异常都通过HandleError转换为友好消息
- ✅ **UI线程安全**：ConfigureAwait(false)避免死锁
- ✅ **操作反馈友好**：StatusMessage自动3秒消失，避免永久显示

#### 1.2.3 错误处理机制

**HandleError方法**：
```csharp
protected virtual void HandleError(Exception ex, string? context = null)
{
    Logger.LogError(ex, "错误发生在: {Context}", context ?? "未知操作");
    ErrorMessage = GetUserFriendlyMessage(ex);

    RunOnUIThread(() =>
    {
        MessageBox.Show(
            ErrorMessage,
            "错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    });
}
```

**友好错误消息映射**：
```csharp
protected virtual string GetUserFriendlyMessage(Exception ex)
{
    return ex switch
    {
        ValidationException => "输入数据验证失败",
        UnauthorizedAccessException => "权限不足",
        TimeoutException => "操作超时",
        TaskCanceledException => "操作已取消",
        _ => "操作失败，请重试"
    };
}
```

**设计原则**：
- ✅ **隐藏技术细节**：不向用户展示堆栈跟踪
- ✅ **统一错误格式**：所有错误都转换为中文友好消息
- ✅ **日志完整记录**：错误日志包含上下文信息
- ✅ **UI线程安全**：使用RunOnUIThread确保MessageBox在UI线程显示

#### 1.2.4 验证支持（INotifyDataErrorInfo）

**接口实现**：
```csharp
public abstract class ViewModelBase : INotifyDataErrorInfo
{
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasErrors => _validationErrors.Any();

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _validationErrors.SelectMany(x => x.Value);

        return _validationErrors.TryGetValue(propertyName, out var errors)
            ? errors
            : Enumerable.Empty<string>();
    }
}
```

**XAML绑定友好的索引器访问器**：
```csharp
// 获取属性的第一个验证错误
public ValidationErrorsAccessor Errors { get; }
// 使用: {Binding Errors[PropertyName]}

// 检查属性是否有验证错误
public ValidationHasErrorsAccessor HasErrorsDictionary { get; }
// 使用: {Binding HasErrorsDictionary[PropertyName]}
```

**验证方法**：
```csharp
// 添加验证错误
protected void AddValidationError(string propertyName, string errorMessage)

// 清除验证错误
protected void ClearValidationErrors(string? propertyName = null)

// 触发验证错误变化事件
protected virtual void OnErrorsChanged(string propertyName)
```

**设计亮点**：
- ✅ **双向验证访问**：支持INotifyDataErrorInfo接口和索引器语法
- ✅ **XAML绑定友好**：`Errors[PropertyName]` 直接绑定到TextBlock
- ✅ **单属性/全部清除**：ClearValidationErrors支持清除单个或全部错误

#### 1.2.5 资源管理（IDisposable）

**Dispose模式实现**：
```csharp
private readonly CompositeDisposable _disposables = new();
private bool _disposed = false;

// 添加需要释放的资源
protected void AddDisposable(IDisposable disposable)
{
    _disposables.Add(disposable);
}

// IDisposable实现
public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);
}

protected virtual void Dispose(bool disposing)
{
    if (_disposed) return;

    if (disposing)
    {
        _disposables?.Dispose();
        OnDisposing();
    }

    _disposed = true;
}

// 子类可重写的清理方法
protected virtual void OnDisposing()
{
    // 子类实现
}
```

**资源管理最佳实践**：
```csharp
public class PatientListViewModel : ViewModelBase
{
    public PatientListViewModel(...)
    {
        // 自动订阅事件
        var subscription = EventAggregator.GetEvent<PatientSelectedEvent>()
            .Subscribe(OnPatientSelected);

        // 注册到资源清理队列
        AddDisposable(subscription);
    }

    protected override void OnDisposing()
    {
        base.OnDisposing();
        // 清理其他资源（如Timer、HttpClient等）
    }
}
```

**设计优势**：
- ✅ **自动资源清理**：CompositeDisposable批量释放所有资源
- ✅ **防止内存泄漏**：事件订阅自动取消订阅
- ✅ **简化子类代码**：通过AddDisposable统一管理

#### 1.2.6 虚方法扩展点（子类可重写）

```csharp
// 初始化命令（构造函数自动调用）
protected virtual void InitializeCommands()

// 订阅事件（构造函数自动调用）
protected virtual void SubscribeToEvents()

// 加载状态变化时触发
protected virtual void OnLoadingStateChanged(bool isLoading)

// 刷新命令CanExecute状态
protected virtual void RefreshCommands()

// 释放时的额外清理工作
protected virtual void OnDisposing()
```

**扩展点设计原则**：
- ✅ **构造函数调用**：InitializeCommands和SubscribeToEvents在构造函数自动调用
- ✅ **生命周期钩子**：OnLoadingStateChanged响应状态变化
- ✅ **命令刷新**：RefreshCommands在IsBusy/IsLoading变化时调用
- ✅ **清理扩展**：OnDisposing在Dispose时调用

---

## 2. UnifiedViewModelBase扩展功能（源文件：484行）

### 2.1 类定义与依赖

```csharp
public abstract class UnifiedViewModelBase : ViewModelBase, INavigationAware, IRegionMemberLifetime
{
    // 依赖服务
    protected readonly IRegionManager RegionManager;
    protected readonly ISessionManager? SessionManager;
    protected readonly IUserNotificationService? UserNotificationService;

    // 页面属性
    public string PageTitle { get; protected set; }
}
```

### 2.2 导航支持（Prism区域导航）

#### 2.2.1 导航方法

```csharp
// 导航到指定视图
protected virtual void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)

// 导航回退
protected virtual void NavigateBack(string regionName)

// 导航前进
protected virtual void NavigateForward(string regionName)

// 检查是否可以回退
protected virtual bool CanNavigateBack(string regionName)

// 检查是否可以前进
protected virtual bool CanNavigateForward(string regionName)
```

**导航示例**：
```csharp
public class MedicalCaseListViewModel : UnifiedViewModelBase
{
    private void OnPatientSelected(PatientDto patient)
    {
        var parameters = new NavigationParameters
        {
            { "PatientId", patient.Id }
        };

        NavigateTo(
            regionName: "ContentRegion",
            viewName: "MedicalCaseDetailView",
            parameters: parameters);
    }
}
```

#### 2.2.2 INavigationAware实现（页面生命周期）

```csharp
// 判断是否可以重用视图实例
public virtual bool IsNavigationTarget(NavigationContext navigationContext)
{
    return true; // 默认可重用
}

// 离开页面时触发
public virtual void OnNavigatedFrom(NavigationContext navigationContext)
{
    Logger.LogDebug("离开页面: {PageTitle}", PageTitle);
}

// 进入页面时触发
public virtual void OnNavigatedTo(NavigationContext navigationContext)
{
    Logger.LogDebug("进入页面: {PageTitle}", PageTitle);

    // 同步处理导航参数
    ProcessNavigationParameters(navigationContext.Parameters);

    // 异步初始化数据
    _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
    {
        try
        {
            await InitializeAsync(navigationContext.Parameters);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "InitializeAsync 执行失败");
            HandleError(ex, "数据初始化");
        }
    });
}
```

**页面生命周期流程**：

```
导航请求
  └─ IsNavigationTarget(context)
      └─ true: 重用现有实例
      └─ false: 创建新实例

进入页面
  └─ OnNavigatedTo(context)
      ├─ ProcessNavigationParameters(parameters)  [同步]
      │   └─ 设置PatientId、MedicalCaseId等参数
      └─ InitializeAsync(parameters)  [异步]
          └─ 加载数据、初始化UI状态

离开页面
  └─ OnNavigatedFrom(context)
      └─ 清理临时状态（可选）
```

#### 2.2.3 自定义异步初始化模式

```csharp
// Issue #1240: 推荐模式
protected virtual Task InitializeAsync(NavigationParameters parameters)
{
    return Task.CompletedTask;
}

// 子类实现示例
protected override async Task InitializeAsync(NavigationParameters parameters)
{
    await ExecuteSafelyAsync(async () =>
    {
        if (parameters.TryGetValue("PatientId", out Guid patientId))
        {
            var patient = await _patientService.GetByIdAsync(patientId);
            CurrentPatient = patient;
        }

        await LoadMedicalCasesAsync();
    }, "加载病案数据");
}
```

**设计亮点**：
- ✅ **分离同步和异步**：ProcessNavigationParameters（同步）+ InitializeAsync（异步）
- ✅ **避免Task.Run**：使用Dispatcher.InvokeAsync替代Task.Run（Issue #1240修复）
- ✅ **异常自动处理**：InitializeAsync异常自动捕获并调用HandleError

### 2.3 增强的验证功能

```csharp
// 验证单个属性
protected virtual void ValidateProperty([CallerMemberName] string? propertyName = null)
{
    // 1. 清除当前属性的验证错误
    ClearValidationErrors(propertyName);

    // 2. 获取属性值
    var property = GetType().GetProperty(propertyName);
    var value = property?.GetValue(this);

    // 3. 执行DataAnnotations验证
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(this) { MemberName = propertyName };

    if (!Validator.TryValidateProperty(value, validationContext, validationResults))
    {
        foreach (var validationResult in validationResults)
        {
            AddValidationError(propertyName, validationResult.ErrorMessage ?? "验证失败");
        }
    }
}

// 验证所有属性
protected virtual void ValidateAllProperties()
{
    var properties = GetType().GetProperties()
        .Where(p => p.GetCustomAttributes(typeof(ValidationAttribute), true).Any());

    foreach (var property in properties)
    {
        ValidateProperty(property.Name);
    }
}
```

**验证触发场景**：
```csharp
public class PatientEditViewModel : UnifiedViewModelBase
{
    private string _name = string.Empty;

    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名不能超过50个字符")]
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ValidateProperty(); // 自动触发验证
            }
        }
    }

    private async Task SaveAsync()
    {
        ValidateAllProperties(); // 保存前验证所有属性

        if (HasErrors)
        {
            await ShowErrorMessageAsync("请修正输入错误后再保存");
            return;
        }

        // 继续保存逻辑...
    }
}
```

### 2.4 消息对话框功能

```csharp
// 显示成功消息
protected virtual async Task ShowSuccessMessageAsync(string message)

// 显示错误消息
protected virtual async Task ShowErrorMessageAsync(string message)

// 显示警告消息
protected virtual async Task ShowWarningMessageAsync(string message)

// 显示确认对话框
protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
```

**同步版本封装**（简化调用）：
```csharp
// 同步错误消息
protected void ShowErrorMessage(string message)

// 同步信息消息
protected void ShowInfoMessage(string message)

// 同步确认对话框
protected bool ShowConfirmMessage(string message, string title = "确认")
```

**使用示例**：
```csharp
private async Task DeletePatientAsync(Guid patientId)
{
    if (!await ShowConfirmationAsync("确定要删除该患者吗？", "确认删除"))
    {
        return; // 用户取消
    }

    await ExecuteSafelyAsync(async () =>
    {
        await _patientService.DeleteAsync(patientId);
        await ShowSuccessMessageAsync("患者删除成功");
        await RefreshAsync();
    }, "删除患者");
}
```

### 2.5 会话管理支持

```csharp
// 获取当前用户信息
protected virtual string GetCurrentUserInfo()
{
    return SessionManager?.CurrentUser?.RealName ?? "未知用户";
}

// 检查是否已登录
protected virtual bool IsUserLoggedIn()
{
    return SessionManager?.IsAuthenticated ?? false;
}
```

**权限检查示例**：
```csharp
private bool CanDeletePatient()
{
    if (!IsUserLoggedIn())
    {
        ShowErrorMessage("请先登录");
        return false;
    }

    // 检查权限...
    return true;
}
```

### 2.6 IRegionMemberLifetime实现（视图缓存）

```csharp
// 控制视图在导航离开后是否保持活动状态
public virtual bool KeepAlive => false; // 默认不缓存
```

**视图缓存策略**：
```csharp
// 场景1: 需要缓存的视图（如工作站主页）
public class MainWorkstationViewModel : UnifiedViewModelBase
{
    public override bool KeepAlive => true; // 缓存视图
}

// 场景2: 不缓存的视图（如编辑对话框）
public class PatientEditViewModel : UnifiedViewModelBase
{
    public override bool KeepAlive => false; // 每次创建新实例
}
```

---

## 3. UnifiedListViewModelBase\<T\>列表基类（源文件：>150行）

### 3.1 类定义与泛型约束

```csharp
public abstract class UnifiedListViewModelBase<T> : UnifiedViewModelBase
    where T : class
{
    // 列表项类型必须是引用类型（class）
}
```

### 3.2 列表属性（8个核心属性）

```csharp
// 列表项集合
public ObservableCollection<T> Items { get; set; }

// 选中的项目集合（多选）
public ObservableCollection<T> SelectedItems { get; set; }

// 当前选中项（单选）
public T? SelectedItem { get; set; }

// 搜索文本
public string SearchText { get; set; }

// 总记录数
public int TotalCount { get; protected set; }

// 当前页码
public int CurrentPage { get; set; }

// 每页大小
public int PageSize { get; protected set; }

// 是否有选择项
public bool HasSelection { get; private set; }
```

**计算属性**：
```csharp
// 总页数
public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

// 是否可以上一页
public bool CanGoPreviousPage => CurrentPage > 1;

// 是否可以下一页
public bool CanGoNextPage => CurrentPage < TotalPages;
```

### 3.3 命令集（8个标准命令）

```csharp
// 搜索命令
public DelegateCommand SearchCommand { get; private set; }

// 刷新命令
public DelegateCommand RefreshCommand { get; private set; }

// 添加命令
public DelegateCommand AddCommand { get; private set; }

// 删除命令（带参数）
public DelegateCommand<T> DeleteCommand { get; private set; }

// 批量删除命令
public DelegateCommand BatchDeleteCommand { get; private set; }

// 上一页命令
public DelegateCommand PreviousPageCommand { get; private set; }

// 下一页命令
public DelegateCommand NextPageCommand { get; private set; }

// 清除搜索命令
public DelegateCommand ClearSearchCommand { get; private set; }
```

**命令初始化**：
```csharp
private void InitializeListCommands()
{
    SearchCommand = new DelegateCommand(
        async () => await SearchAsync(),
        () => !IsLoading);

    RefreshCommand = new DelegateCommand(
        async () => await RefreshAsync(),
        () => !IsLoading);

    AddCommand = new DelegateCommand(
        async () => await OnExecuteAddAsync(),
        CanExecuteAdd);

    DeleteCommand = new DelegateCommand<T>(
        async item => await ExecuteDeleteAsync(item),
        CanExecuteDelete);

    BatchDeleteCommand = new DelegateCommand(
        async () => await ExecuteBatchDeleteAsync(),
        CanExecuteBatchDelete);

    PreviousPageCommand = new DelegateCommand(
        ExecutePreviousPage,
        () => CanGoPreviousPage && !IsLoading);

    NextPageCommand = new DelegateCommand(
        ExecuteNextPage,
        () => CanGoNextPage && !IsLoading);

    ClearSearchCommand = new DelegateCommand(
        ExecuteClearSearch,
        () => !string.IsNullOrEmpty(SearchText));
}
```

### 3.4 列表操作流程

#### 3.4.1 分页加载流程

```
用户切换页码
  └─ CurrentPage = newPage
      └─ SetProperty触发PropertyChanged
          └─ LoadPageAsync()
              └─ ExecuteSafelyAsync()
                  ├─ IsLoading = true
                  ├─ 调用Service.GetPagedAsync(CurrentPage, PageSize)
                  ├─ Items = new ObservableCollection<T>(result.Items)
                  ├─ TotalCount = result.TotalCount
                  └─ IsLoading = false
```

#### 3.4.2 搜索流程

```
用户输入SearchText
  └─ SearchText属性setter
      └─ SetProperty触发PropertyChanged
          └─ SearchAsync()
              └─ ExecuteSafelyAsync()
                  ├─ CurrentPage = 1 (重置到第一页)
                  ├─ 调用Service.SearchAsync(SearchText, CurrentPage, PageSize)
                  ├─ Items = new ObservableCollection<T>(result.Items)
                  ├─ TotalCount = result.TotalCount
                  └─ RefreshCommands()
```

#### 3.4.3 批量删除流程

```
用户选择多个项 → 点击批量删除按钮
  └─ BatchDeleteCommand.Execute()
      └─ ShowConfirmationAsync("确定要删除选中的项吗？")
          └─ 用户确认
              └─ ExecuteSafelyAsync()
                  ├─ foreach (var item in SelectedItems)
                  │   └─ await Service.DeleteAsync(item.Id)
                  ├─ SelectedItems.Clear()
                  └─ await RefreshAsync()
```

### 3.5 子类实现示例

```csharp
public class PatientListViewModel : UnifiedListViewModelBase<PatientDto>
{
    private readonly IPatientService _patientService;

    public PatientListViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        IPatientService patientService)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _patientService = patientService;
        PageTitle = "患者列表";
    }

    // 重写加载页面方法
    protected override async Task LoadPageAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            var result = await _patientService.GetPagedAsync(CurrentPage, PageSize);
            Items = new ObservableCollection<PatientDto>(result.Items);
            TotalCount = result.TotalCount;
        }, "加载患者列表");
    }

    // 重写搜索方法
    protected override async Task SearchAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            CurrentPage = 1;
            var result = await _patientService.SearchAsync(SearchText, CurrentPage, PageSize);
            Items = new ObservableCollection<PatientDto>(result.Items);
            TotalCount = result.TotalCount;
        }, "搜索患者");
    }

    // 重写添加方法
    protected override async Task OnExecuteAddAsync()
    {
        NavigateTo("ContentRegion", "PatientCreateView");
    }

    // 重写删除方法
    protected override async Task ExecuteDeleteAsync(PatientDto patient)
    {
        if (!await ShowConfirmationAsync($"确定要删除患者 {patient.Name} 吗？"))
        {
            return;
        }

        await ExecuteSafelyAsync(async () =>
        {
            await _patientService.DeleteAsync(patient.Id);
            await RefreshAsync();
        }, "删除患者");
    }

    // 自定义CanExecute逻辑
    protected override bool CanExecuteDelete(PatientDto patient)
    {
        return patient != null && !IsBusy;
    }
}
```

---

## 4. MVVM模式核心概念

### 4.1 MVVM三层架构

```
┌─────────────────────────────────────────────┐
│                   View (XAML)                │
│  - 纯UI定义（布局、样式、控件）               │
│  - 数据绑定 {Binding Property}                │
│  - 命令绑定 {Binding Command}                 │
└────────────┬────────────────────────────────┘
             │ DataContext
             │ Binding / Command Binding
             ↓
┌─────────────────────────────────────────────┐
│           ViewModel (ViewModelBase)          │
│  - 属性 (ObservableCollection, 状态属性)     │
│  - 命令 (DelegateCommand)                    │
│  - 业务逻辑（调用Service）                    │
└────────────┬────────────────────────────────┘
             │ Dependency Injection
             │ Service调用
             ↓
┌─────────────────────────────────────────────┐
│             Model (Service + DTO)            │
│  - Service: 业务逻辑实现                     │
│  - DTO: 数据传输对象                          │
│  - Repository: 数据访问层                    │
└─────────────────────────────────────────────┘
```

### 4.2 数据绑定机制（INotifyPropertyChanged）

**属性变更通知流程**：

```csharp
// ViewModel中的属性
private string _patientName = string.Empty;

public string PatientName
{
    get => _patientName;
    set => SetProperty(ref _patientName, value); // 触发PropertyChanged事件
}
```

**SetProperty内部实现**（Prism.BindableBase）：
```csharp
protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
{
    if (EqualityComparer<T>.Default.Equals(storage, value))
        return false; // 值未变化，不触发事件

    storage = value;
    RaisePropertyChanged(propertyName); // 触发PropertyChanged事件
    return true;
}
```

**XAML绑定更新流程**：

```
1. 用户在TextBox中输入 "张三"
   └─ TextBox触发PropertyChanged (TwoWay Binding)
       └─ ViewModel.PatientName = "张三"
           └─ SetProperty(ref _patientName, "张三")
               └─ RaisePropertyChanged("PatientName")
                   └─ WPF绑定引擎检测到PropertyChanged事件
                       └─ 更新所有绑定到PatientName的UI元素
```

### 4.3 命令绑定机制（ICommand）

**DelegateCommand实现原理**：

```csharp
// Prism DelegateCommand定义
public class DelegateCommand : ICommand
{
    private readonly Action _executeMethod;
    private readonly Func<bool> _canExecuteMethod;

    public event EventHandler CanExecuteChanged;

    public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
    {
        _executeMethod = executeMethod;
        _canExecuteMethod = canExecuteMethod;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecuteMethod?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        _executeMethod?.Invoke();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

**命令绑定流程**：

```xml
<!-- XAML绑定 -->
<Button Content="保存" Command="{Binding SaveCommand}" />
```

```csharp
// ViewModel命令定义
public class PatientEditViewModel : ViewModelBase
{
    public DelegateCommand SaveCommand { get; private set; }

    protected override void InitializeCommands()
    {
        SaveCommand = new DelegateCommand(
            async () => await SaveAsync(),  // Execute方法
            () => !HasErrors && !IsBusy);   // CanExecute方法
    }

    private async Task SaveAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            await _patientService.UpdateAsync(CurrentPatient);
        }, "保存患者");
    }

    // 当HasErrors或IsBusy变化时，刷新命令状态
    protected override void RefreshCommands()
    {
        SaveCommand?.RaiseCanExecuteChanged();
    }
}
```

**命令执行流程**：

```
1. 用户点击"保存"按钮
   └─ WPF调用 SaveCommand.CanExecute()
       └─ 返回 !HasErrors && !IsBusy (true/false)
           ├─ true: 按钮启用，继续执行
           └─ false: 按钮禁用，不执行

2. 按钮启用时，点击触发 SaveCommand.Execute()
   └─ 调用 SaveAsync()
       └─ ExecuteSafelyAsync()
           ├─ IsBusy = true
           ├─ RaiseCanExecuteChanged() → 按钮禁用
           ├─ await _patientService.UpdateAsync()
           ├─ StatusMessage = "保存完成"
           ├─ IsBusy = false
           └─ RaiseCanExecuteChanged() → 按钮启用
```

### 4.4 事件聚合器模式（EventAggregator）

**发布-订阅模式实现**：

```csharp
// 定义事件
public class PatientSelectedEvent : PubSubEvent<PatientDto> { }

// 发布事件（发布者ViewModel）
public class PatientListViewModel : ViewModelBase
{
    private void OnPatientSelectionChanged(PatientDto selectedPatient)
    {
        EventAggregator.GetEvent<PatientSelectedEvent>()
            .Publish(selectedPatient);
    }
}

// 订阅事件（订阅者ViewModel）
public class MedicalCaseListViewModel : ViewModelBase
{
    protected override void SubscribeToEvents()
    {
        base.SubscribeToEvents();

        var subscription = EventAggregator.GetEvent<PatientSelectedEvent>()
            .Subscribe(OnPatientSelected, ThreadOption.UIThread);

        AddDisposable(subscription); // 自动取消订阅
    }

    private void OnPatientSelected(PatientDto patient)
    {
        CurrentPatient = patient;
        _ = LoadMedicalCasesAsync();
    }
}
```

**事件聚合器优势**：
- ✅ **解耦ViewModel**：发布者和订阅者无需直接依赖
- ✅ **线程安全**：支持UIThread、BackgroundThread、PublisherThread选项
- ✅ **自动取消订阅**：通过AddDisposable实现资源管理

---

## 5. 状态管理机制

### 5.1 状态属性设计（6个核心状态）

| 状态属性 | 类型 | 用途 | 更新时机 | XAML绑定 |
|---------|------|------|---------|---------|
| **IsLoading** | bool | 数据加载状态 | ExecuteSafelyAsync开始/结束 | ProgressBar.IsIndeterminate |
| **IsBusy** | bool | 操作执行状态 | ExecuteSafelyAsync开始/结束 | Button.IsEnabled (反向绑定) |
| **HasError** | bool | 错误标志 | ErrorMessage变化时自动更新 | ErrorPanel.Visibility |
| **ErrorMessage** | string | 错误消息 | HandleError调用时 | TextBlock.Text |
| **StatusMessage** | string | 状态提示 | ExecuteSafelyAsync中更新 | StatusBar.Text |
| **HasErrors** | bool | 验证错误标志 | AddValidationError/ClearValidationErrors | - |

### 5.2 状态变化流程图

```
ExecuteSafelyAsync调用
  └─ IsBusy = true
  └─ ClearError() (HasError = false, ErrorMessage = "")
  └─ StatusMessage = "正在执行操作..."
      ├─ operation()成功
      │   └─ StatusMessage = "操作完成" (3秒后自动清除)
      ├─ operation()取消
      │   └─ StatusMessage = "操作已取消"
      └─ operation()异常
          ├─ HandleError(ex)
          │   ├─ ErrorMessage = "操作失败，请重试"
          │   ├─ HasError = true (自动)
          │   └─ MessageBox.Show(ErrorMessage)
          └─ StatusMessage = "操作失败"
  └─ IsBusy = false
```

### 5.3 XAML绑定示例

```xml
<UserControl>
    <Grid>
        <!-- 加载指示器 -->
        <ProgressBar IsIndeterminate="{Binding IsLoading}"
                     Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}"
                     VerticalAlignment="Top" />

        <!-- 错误提示面板 -->
        <Border Background="LightCoral"
                Visibility="{Binding HasError, Converter={StaticResource BoolToVis}}">
            <TextBlock Text="{Binding ErrorMessage}" Foreground="White" Margin="10" />
        </Border>

        <!-- 状态栏 -->
        <StatusBar DockPanel.Dock="Bottom">
            <StatusBarItem>
                <TextBlock Text="{Binding StatusMessage}" />
            </StatusBarItem>
        </StatusBar>

        <!-- 数据列表 -->
        <ListBox ItemsSource="{Binding Items}"
                 SelectedItem="{Binding SelectedItem}"
                 IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBool}}" />

        <!-- 操作按钮 -->
        <StackPanel Orientation="Horizontal">
            <Button Content="加载" Command="{Binding LoadCommand}"
                    IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBool}}" />
            <Button Content="保存" Command="{Binding SaveCommand}"
                    IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBool}}" />
        </StackPanel>
    </Grid>
</UserControl>
```

---

## 6. 验证支持机制

### 6.1 双轨验证策略

#### 6.1.1 DataAnnotations验证（属性级）

```csharp
public class PatientEditViewModel : UnifiedViewModelBase
{
    private string _name = string.Empty;
    private string _phoneNumber = string.Empty;

    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名不能超过50个字符")]
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ValidateProperty(); // 自动触发DataAnnotations验证
            }
        }
    }

    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号码格式不正确")]
    public string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (SetProperty(ref _phoneNumber, value))
            {
                ValidateProperty();
            }
        }
    }
}
```

#### 6.1.2 FluentValidation验证（对象级）

```csharp
// FluentValidation验证器定义（在Service层或ViewModel层）
public class PatientValidator : AbstractValidator<PatientDto>
{
    public PatientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名不能超过50个字符");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号码格式不正确")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.BirthDate)
            .LessThan(DateTime.Now).WithMessage("出生日期不能是未来日期")
            .When(x => x.BirthDate.HasValue);
    }
}

// ViewModel中使用FluentValidation
public class PatientEditViewModel : UnifiedViewModelBase
{
    private readonly IValidator<PatientDto> _validator;

    private async Task SaveAsync()
    {
        // FluentValidation验证
        var validationResult = await _validator.ValidateAsync(CurrentPatient);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                AddValidationError(error.PropertyName, error.ErrorMessage);
            }

            await ShowErrorMessageAsync("请修正输入错误后再保存");
            return;
        }

        // 继续保存逻辑...
    }
}
```

### 6.2 验证错误XAML绑定

```xml
<!-- 方式1: 使用索引器绑定（推荐） -->
<TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}">
    <TextBox.Style>
        <Style TargetType="TextBox">
            <Style.Triggers>
                <DataTrigger Binding="{Binding HasErrorsDictionary[Name]}" Value="True">
                    <Setter Property="BorderBrush" Value="Red" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </TextBox.Style>
</TextBox>
<TextBlock Text="{Binding Errors[Name]}" Foreground="Red" />

<!-- 方式2: 使用INotifyDataErrorInfo自动显示（WPF默认支持） -->
<TextBox Text="{Binding Name, ValidatesOnNotifyDataErrors=True, UpdateSourceTrigger=PropertyChanged}" />
```

### 6.3 验证流程总结

```
属性变更
  └─ SetProperty(ref _name, value)
      └─ RaisePropertyChanged("Name")
          └─ ValidateProperty("Name")
              ├─ ClearValidationErrors("Name")
              ├─ Validator.TryValidateProperty(value, context, results)
              ├─ 验证失败
              │   └─ AddValidationError("Name", "患者姓名不能为空")
              │       └─ OnErrorsChanged("Name")
              │           └─ ErrorsChanged事件触发
              │               └─ WPF更新UI (红色边框、错误消息)
              └─ 验证成功
                  └─ 无错误，UI正常显示
```

---

## 7. 资源管理与生命周期

### 7.1 ViewModel生命周期阶段

```
1. 构造阶段
   └─ 依赖注入（IEventAggregator, ILoggerFactory等）
   └─ InitializeCommands() (初始化所有命令)
   └─ SubscribeToEvents() (订阅EventAggregator事件)

2. 导航进入阶段（如果实现INavigationAware）
   └─ OnNavigatedTo(context)
       ├─ ProcessNavigationParameters(parameters) [同步]
       └─ InitializeAsync(parameters) [异步]

3. 活动阶段
   └─ 用户交互（属性变更、命令执行）
   └─ ExecuteSafelyAsync执行异步操作
   └─ 事件订阅处理

4. 导航离开阶段
   └─ OnNavigatedFrom(context)
       └─ 清理临时状态（可选）

5. 释放阶段
   └─ Dispose()
       ├─ _disposables.Dispose() (批量释放资源)
       └─ OnDisposing() (子类自定义清理)
```

### 7.2 资源泄漏防护（CompositeDisposable）

**常见资源泄漏场景**：

```csharp
// ❌ 错误示例：事件订阅未取消，导致ViewModel无法释放
public class PatientListViewModel : ViewModelBase
{
    public PatientListViewModel(...)
    {
        EventAggregator.GetEvent<PatientSelectedEvent>()
            .Subscribe(OnPatientSelected); // 未注册到_disposables
    }
}

// ✅ 正确示例：通过AddDisposable自动取消订阅
public class PatientListViewModel : ViewModelBase
{
    public PatientListViewModel(...)
    {
        var subscription = EventAggregator.GetEvent<PatientSelectedEvent>()
            .Subscribe(OnPatientSelected);

        AddDisposable(subscription); // 注册到资源清理队列
    }
}
```

**其他需要释放的资源**：

```csharp
protected override void OnDisposing()
{
    base.OnDisposing();

    // 释放Timer
    _refreshTimer?.Dispose();

    // 释放HttpClient（如果自己创建）
    _httpClient?.Dispose();

    // 释放CancellationTokenSource
    _cancellationTokenSource?.Cancel();
    _cancellationTokenSource?.Dispose();

    // 释放IAsyncDisposable资源
    if (_asyncResource != null)
    {
        _ = _asyncResource.DisposeAsync().AsTask();
    }
}
```

### 7.3 视图缓存策略（KeepAlive）

```csharp
// 场景1: 工作站主页（需要缓存，避免重复加载）
public class MainWorkstationViewModel : UnifiedViewModelBase
{
    public override bool KeepAlive => true;
}

// 场景2: 编辑对话框（不缓存，每次创建新实例）
public class PatientEditViewModel : UnifiedViewModelBase
{
    public override bool KeepAlive => false;
}

// 场景3: 列表页面（根据业务需求决定）
public class PatientListViewModel : UnifiedListViewModelBase<PatientDto>
{
    // 如果列表数据频繁变化，不缓存
    public override bool KeepAlive => false;

    // 如果列表数据相对稳定，可缓存
    // public override bool KeepAlive => true;
}
```

**KeepAlive影响**：
- ✅ `true`：视图实例保留在内存中，导航回来时复用（性能好，内存占用高）
- ✅ `false`：视图实例销毁，导航回来时重新创建（内存占用低，性能略差）

---

## 8. 最佳实践与反模式

### 8.1 最佳实践（✅ 推荐）

#### 8.1.1 统一使用ExecuteSafelyAsync

```csharp
// ✅ 推荐：使用ExecuteSafelyAsync自动处理状态和异常
private async Task LoadPatientsAsync()
{
    await ExecuteSafelyAsync(async () =>
    {
        var result = await _patientService.GetAllAsync();
        Patients = new ObservableCollection<PatientDto>(result);
    }, "加载患者列表");
}

// ❌ 不推荐：手动管理状态和异常
private async Task LoadPatientsAsync()
{
    try
    {
        IsBusy = true;
        var result = await _patientService.GetAllAsync();
        Patients = new ObservableCollection<PatientDto>(result);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载患者失败");
        MessageBox.Show("加载失败，请重试");
    }
    finally
    {
        IsBusy = false;
    }
}
```

#### 8.1.2 命令CanExecute统一管理

```csharp
// ✅ 推荐：统一刷新命令状态
protected override void RefreshCommands()
{
    SaveCommand?.RaiseCanExecuteChanged();
    DeleteCommand?.RaiseCanExecuteChanged();
    BatchDeleteCommand?.RaiseCanExecuteChanged();
}

// ViewModel属性变更时自动调用
public bool IsLoading
{
    get => _isLoading;
    set
    {
        if (SetProperty(ref _isLoading, value))
        {
            RefreshCommands(); // 自动刷新命令
        }
    }
}
```

#### 8.1.3 事件订阅自动清理

```csharp
// ✅ 推荐：通过AddDisposable自动取消订阅
protected override void SubscribeToEvents()
{
    base.SubscribeToEvents();

    var subscription1 = EventAggregator.GetEvent<PatientSelectedEvent>()
        .Subscribe(OnPatientSelected, ThreadOption.UIThread);
    AddDisposable(subscription1);

    var subscription2 = EventAggregator.GetEvent<MedicalCaseUpdatedEvent>()
        .Subscribe(OnMedicalCaseUpdated, ThreadOption.UIThread);
    AddDisposable(subscription2);
}
```

#### 8.1.4 导航参数类型安全

```csharp
// ✅ 推荐：使用TryGetValue安全获取参数
protected override void ProcessNavigationParameters(NavigationParameters parameters)
{
    if (parameters.TryGetValue("PatientId", out Guid patientId))
    {
        PatientId = patientId;
    }
    else
    {
        Logger.LogWarning("导航参数缺少PatientId");
    }
}

// ❌ 不推荐：直接转换可能导致异常
protected override void ProcessNavigationParameters(NavigationParameters parameters)
{
    PatientId = (Guid)parameters["PatientId"]; // 可能抛出异常
}
```

#### 8.1.5 验证提前触发

```csharp
// ✅ 推荐：保存前验证所有属性
private async Task SaveAsync()
{
    ValidateAllProperties();

    if (HasErrors)
    {
        await ShowErrorMessageAsync("请修正输入错误后再保存");
        return;
    }

    // 继续保存逻辑...
}
```

### 8.2 反模式（❌ 禁止）

#### 8.2.1 在ViewModel中直接操作View

```csharp
// ❌ 禁止：直接引用View控件
public class PatientListViewModel : ViewModelBase
{
    private readonly PatientListView _view; // 违反MVVM模式

    public PatientListViewModel(PatientListView view)
    {
        _view = view;
    }

    private void UpdateUI()
    {
        _view.ListBox.SelectedIndex = 0; // 破坏数据绑定
    }
}

// ✅ 正确：通过属性绑定
public class PatientListViewModel : ViewModelBase
{
    public int SelectedIndex { get; set; } // XAML绑定：ListBox.SelectedIndex
}
```

#### 8.2.2 在ViewModel中使用UI线程阻塞操作

```csharp
// ❌ 禁止：同步阻塞UI线程
private void LoadPatients()
{
    var result = _patientService.GetAllAsync().Result; // 死锁风险
    Patients = new ObservableCollection<PatientDto>(result);
}

// ✅ 正确：使用异步方法
private async Task LoadPatientsAsync()
{
    await ExecuteSafelyAsync(async () =>
    {
        var result = await _patientService.GetAllAsync();
        Patients = new ObservableCollection<PatientDto>(result);
    }, "加载患者列表");
}
```

#### 8.2.3 事件订阅未取消

```csharp
// ❌ 禁止：事件订阅未取消，导致内存泄漏
protected override void SubscribeToEvents()
{
    EventAggregator.GetEvent<PatientSelectedEvent>()
        .Subscribe(OnPatientSelected); // 未注册到_disposables
}

// ✅ 正确：通过AddDisposable自动取消
protected override void SubscribeToEvents()
{
    var subscription = EventAggregator.GetEvent<PatientSelectedEvent>()
        .Subscribe(OnPatientSelected);
    AddDisposable(subscription);
}
```

#### 8.2.4 在构造函数中执行耗时操作

```csharp
// ❌ 禁止：构造函数中执行数据加载
public PatientListViewModel(...)
{
    var patients = _patientService.GetAllAsync().Result; // 阻塞UI
    Patients = new ObservableCollection<PatientDto>(patients);
}

// ✅ 正确：在InitializeAsync中加载数据
protected override async Task InitializeAsync(NavigationParameters parameters)
{
    await ExecuteSafelyAsync(async () =>
    {
        var result = await _patientService.GetAllAsync();
        Patients = new ObservableCollection<PatientDto>(result);
    }, "加载患者列表");
}
```

#### 8.2.5 直接操作ObservableCollection（非UI线程）

```csharp
// ❌ 禁止：后台线程直接修改ObservableCollection
private async Task LoadPatientsAsync()
{
    var result = await _patientService.GetAllAsync();
    Patients.Clear(); // 可能导致跨线程异常
    foreach (var patient in result)
    {
        Patients.Add(patient);
    }
}

// ✅ 正确：使用RunOnUIThread或ConfigureAwait
private async Task LoadPatientsAsync()
{
    var result = await _patientService.GetAllAsync();

    RunOnUIThread(() =>
    {
        Patients = new ObservableCollection<PatientDto>(result);
    });
}
```

---

## 9. 设计原则与约束

### 9.1 核心设计原则（7条）

#### 原则1: 简化MVVM开发（KISS原则）

**目标**：通过统一的基类封装常用功能，避免重复代码。

**实施**：
- ViewModelBase提供状态管理、异步执行、错误处理、验证等核心功能
- UnifiedViewModelBase扩展导航、消息对话框、会话管理
- UnifiedListViewModelBase\<T\>专注列表操作

**效果**：
- ✅ 子类ViewModel代码量减少60%-70%
- ✅ 开发者无需关注状态管理细节
- ✅ 统一的错误处理和用户反馈

#### 原则2: 状态管理标准化

**目标**：统一管理Loading、Busy、Error等状态，简化UI绑定。

**实施**：
- IsLoading：数据加载状态（进度条）
- IsBusy：操作执行状态（按钮禁用）
- HasError + ErrorMessage：错误状态（错误提示）
- StatusMessage：操作状态（状态栏）

**效果**：
- ✅ UI绑定标准化（所有列表页面布局一致）
- ✅ 用户体验统一（加载提示、错误提示一致）

#### 原则3: 异步操作安全化

**目标**：提供ExecuteSafelyAsync封装，自动处理异常和状态。

**实施**：
- 自动设置IsBusy状态
- 自动捕获异常并转换为友好消息
- 自动更新StatusMessage
- 自动清除3秒后的状态消息

**效果**：
- ✅ 杜绝try-catch重复代码
- ✅ 杜绝手动管理IsBusy状态
- ✅ 杜绝未处理的异常崩溃

#### 原则4: 验证支持完整性

**目标**：实现INotifyDataErrorInfo，支持DataAnnotations和FluentValidation。

**实施**：
- 实现INotifyDataErrorInfo接口（WPF原生支持）
- 提供索引器访问器（XAML绑定友好）
- 支持DataAnnotations属性验证
- 支持FluentValidation对象验证

**效果**：
- ✅ 属性级验证（实时反馈）
- ✅ 对象级验证（保存前检查）
- ✅ UI验证提示标准化

#### 原则5: 资源管理自动化

**目标**：实现IDisposable，自动清理订阅和资源。

**实施**：
- CompositeDisposable批量管理资源
- AddDisposable注册需要清理的资源
- 事件订阅自动取消（防止内存泄漏）

**效果**：
- ✅ 杜绝事件订阅导致的内存泄漏
- ✅ 简化资源清理代码

#### 原则6: 导航与生命周期

**目标**：实现INavigationAware，支持Prism区域导航。

**实施**：
- OnNavigatedTo：进入页面时加载数据
- OnNavigatedFrom：离开页面时清理状态
- InitializeAsync：异步初始化数据
- ProcessNavigationParameters：同步处理导航参数

**效果**：
- ✅ 页面生命周期清晰
- ✅ 导航参数传递标准化
- ✅ 异步加载数据不阻塞UI

#### 原则7: 职责单一（SRP）

**目标**：ViewModel专注业务逻辑协调，不处理UI细节。

**实施**：
- ViewModel：属性、命令、业务逻辑协调
- View：纯XAML布局和样式
- Service：业务逻辑实现
- Repository：数据访问

**效果**：
- ✅ 代码易测试（ViewModel可单元测试）
- ✅ 代码易维护（职责清晰）

### 9.2 技术约束（5条）

#### 约束1: 禁止在ViewModel中直接操作View

**原因**：破坏MVVM模式的数据驱动原则。

**替代方案**：
- 使用数据绑定（Binding）
- 使用命令绑定（Command）
- 使用Behavior（如触发器）

#### 约束2: 禁止在构造函数中执行耗时操作

**原因**：阻塞UI线程，导致应用启动缓慢。

**替代方案**：
- 在InitializeAsync中加载数据
- 使用延迟加载（Lazy<T>）

#### 约束3: 禁止事件订阅未取消

**原因**：导致内存泄漏，ViewModel无法释放。

**替代方案**：
- 使用AddDisposable注册事件订阅
- 实现OnDisposing清理资源

#### 约束4: 禁止同步阻塞异步方法

**原因**：导致死锁（.Result / .Wait()）。

**替代方案**：
- 使用async/await
- 使用ExecuteSafelyAsync封装

#### 约束5: 禁止后台线程直接修改ObservableCollection

**原因**：导致跨线程异常。

**替代方案**：
- 使用RunOnUIThread
- 使用ConfigureAwait(false) + Dispatcher.Invoke

### 9.3 命名约定

| 类型 | 命名规则 | 示例 |
|-----|---------|------|
| **ViewModel类** | {Feature}{Type}ViewModel | PatientListViewModel, MedicalCaseDetailViewModel |
| **属性** | PascalCase | IsLoading, CurrentPatient, Items |
| **私有字段** | _camelCase | _patientService, _isLoading |
| **命令** | {Action}Command | SaveCommand, DeleteCommand, SearchCommand |
| **异步方法** | {Action}Async | LoadPatientsAsync, SaveAsync, DeleteAsync |
| **事件处理** | On{Event} | OnPatientSelected, OnNavigatedTo |

---

## 10. 参考资料

### 10.1 项目内部文档

- **快速参考**: [docs/quick-reference/code-patterns.md](../../../quick-reference/code-patterns.md) - MVVM模式速查
- **DTO设计**: [docs/explanation/architecture/shared/dto-design-standard.md](../shared/dto-design-standard.md) - DTO与ViewModel映射
- **Models使用指南**: [docs/how-to-guides/client/models-usage.md](../../../how-to-guides/client/models-usage.md) *(待创建)* - ViewModelBase实战指南

### 10.2 外部参考

- **Prism官方文档**: https://prismlibrary.com/ - Prism MVVM框架
- **WPF MVVM模式**: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/data-binding-overview - WPF数据绑定
- **INotifyDataErrorInfo**: https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifydataerrorinfo - 验证接口
- **FluentValidation**: https://docs.fluentvalidation.net/ - FluentValidation文档

### 10.3 相关代码文件

- **ViewModelBase.cs**: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/ViewModelBase.cs` (536行)
- **UnifiedViewModelBase.cs**: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/UnifiedViewModelBase.cs` (484行)
- **UnifiedListViewModelBase.cs**: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/UnifiedListViewModelBase.cs` (>150行)
- **示例ViewModel**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientListViewModel.cs`

---

## 11. 更新历史

| 版本 | 日期 | 变更内容 | 作者 |
|-----|------|---------|------|
| v1.0 | 2025-10-29 | 初始版本，完整架构设计文档 | Claude Code |

---

**文档维护**: Client端开发组
**审核状态**: 待审核
**Epic关联**: Issue #1718 Phase 1 - 架构文档完善
