# Prism 框架使用指南

**版本**: 1.0  
**创建日期**: 2025-11-10  
**适用范围**: LYBTZYZS 项目 WPF 客户端（Prism 8.x）  

---

## 📋 目录

1. [Region 区域管理](#region-区域管理)
2. [导航系统](#导航系统)
3. [命令系统](#命令系统)
4. [依赖注入](#依赖注入)
5. [事件聚合器](#事件聚合器)
6. [对话框服务](#对话框服务)

---

## Region 区域管理

### Region 定义

**Shell.xaml 中定义 Region**:

```xaml
<Window x:Class="LYBT.Desktop.Shell.Views.Shell"
        xmlns:prism="http://prismlibrary.com/">
    <Grid>
        <ContentControl prism:RegionManager.RegionName="MainRegion" />
    </Grid>
</Window>
```

**多 Region 布局**:

```xaml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    
    <!-- 顶部导航区域 -->
    <ContentControl Grid.Row="0" 
                    prism:RegionManager.RegionName="NavigationRegion" />
    
    <!-- 主内容区域 -->
    <ContentControl Grid.Row="1" 
                    prism:RegionManager.RegionName="MainRegion" />
</Grid>
```

### Region 注册

**在 Module 中注册 View**:

```csharp
public class HerbsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册导航页面
        containerRegistry.RegisterForNavigation<HerbManagementView>();
        containerRegistry.RegisterForNavigation<HerbDetailView>();
        containerRegistry.RegisterForNavigation<HerbCreateView>();
    }
    
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑
    }
}
```

### ViewModelLocator 自动连接

**XAML 中启用自动连接**:

```xaml
<UserControl x:Class="LYBT.Desktop.Herbs.Views.HerbManagementView"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">
    <!-- View 内容 -->
</UserControl>
```

**命名约定**:
- View: `HerbManagementView.xaml`
- ViewModel: `HerbManagementViewModel.cs`
- 位置: 同一 Module，Views 和 ViewModels 文件夹

**关键规则**:
- ✅ View 和 ViewModel 必须在同一个 Assembly
- ✅ 命名必须遵循约定（View 后缀 vs ViewModel 后缀）
- ✅ ViewModel 必须在 ViewModels 命名空间，View 在 Views 命名空间

---

## 导航系统

### 基本导航

**从 ViewModel 导航**:

```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    private readonly IRegionManager _regionManager;
    
    public HerbManagementViewModel(IRegionManager regionManager, ...)
        : base(...)
    {
        _regionManager = regionManager;
        NavigateToDetailCommand = new DelegateCommand<HerbDto>(OnNavigateToDetail);
    }
    
    private void OnNavigateToDetail(HerbDto herb)
    {
        var parameters = new NavigationParameters
        {
            { "herbId", herb.Id }
        };
        
        _regionManager.RequestNavigate("MainRegion", "HerbDetailView", parameters);
    }
}
```

### 导航参数

**传递参数**:

```csharp
// 发送方
var parameters = new NavigationParameters
{
    { "herbId", 123 },
    { "mode", "edit" }
};
_regionManager.RequestNavigate("MainRegion", "HerbDetailView", parameters);
```

**接收参数**:

```csharp
public class HerbDetailViewModel : UnifiedViewModelBase, INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 获取参数
        if (navigationContext.Parameters.TryGetValue<int>("herbId", out var herbId))
        {
            _ = LoadHerbAsync(herbId);
        }
        
        if (navigationContext.Parameters.TryGetValue<string>("mode", out var mode))
        {
            IsEditMode = mode == "edit";
        }
    }
    
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 是否重用现有实例
        return false; // false = 每次都创建新实例
    }
    
    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 离开页面时的清理逻辑
    }
}
```

### 导航回调

**导航完成回调**:

```csharp
_regionManager.RequestNavigate(
    "MainRegion", 
    "HerbDetailView", 
    parameters,
    result =>
    {
        if (result.Result == true)
        {
            Logger.LogInformation("导航成功");
        }
        else
        {
            Logger.LogError("导航失败: {Error}", result.Error);
        }
    });
```

### 导航守卫

**IConfirmNavigationRequest 实现**:

```csharp
public class HerbCreateViewModel : UnifiedViewModelBase, IConfirmNavigationRequest
{
    private bool _hasUnsavedChanges;
    
    public void ConfirmNavigationRequest(NavigationContext navigationContext, 
                                         Action<bool> continuationCallback)
    {
        if (_hasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "有未保存的修改，是否离开？", 
                "确认", 
                MessageBoxButton.YesNo);
                
            continuationCallback(result == MessageBoxResult.Yes);
        }
        else
        {
            continuationCallback(true);
        }
    }
}
```

---

## 命令系统

### DelegateCommand

**基本用法**:

```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    public DelegateCommand SaveCommand { get; private set; }
    
    public HerbManagementViewModel(...)
    {
        SaveCommand = new DelegateCommand(OnSave, CanSave);
    }
    
    private void OnSave()
    {
        // 执行保存逻辑
    }
    
    private bool CanSave()
    {
        return !IsBusy && IsValid;
    }
}
```

### DelegateCommand<T> 带参数命令

**XAML 绑定**:

```xaml
<Button Command="{Binding DeleteCommand}" 
        CommandParameter="{Binding}" />
```

**ViewModel 实现**:

```csharp
public DelegateCommand<HerbDto> DeleteCommand { get; private set; }

public HerbManagementViewModel(...)
{
    DeleteCommand = new DelegateCommand<HerbDto>(OnDelete, CanDelete);
}

private void OnDelete(HerbDto herb)
{
    if (herb == null) return;
    // 执行删除逻辑
}

private bool CanDelete(HerbDto herb)
{
    return herb != null && !IsBusy;
}
```

### 命令自动刷新

**ObservesProperty 自动观察**:

```csharp
SaveCommand = new DelegateCommand(OnSave, CanSave)
    .ObservesProperty(() => IsBusy)       // 观察 IsBusy 属性
    .ObservesProperty(() => IsValid)      // 观察 IsValid 属性
    .ObservesProperty(() => HerbName);    // 观察 HerbName 属性
```

**手动刷新**:

```csharp
private string _herbName;
public string HerbName
{
    get => _herbName;
    set
    {
        SetProperty(ref _herbName, value);
        SaveCommand.RaiseCanExecuteChanged(); // 手动通知命令刷新
    }
}
```

### 异步命令

**AsyncDelegateCommand**:

```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    public AsyncDelegateCommand LoadDataCommand { get; private set; }
    
    public HerbManagementViewModel(...)
    {
        LoadDataCommand = new AsyncDelegateCommand(LoadDataAsync, CanLoadData);
    }
    
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var herbs = await _herbRepository.GetAllAsync();
            Items.Clear();
            foreach (var herb in herbs)
            {
                Items.Add(herb);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private bool CanLoadData()
    {
        return !IsBusy;
    }
}
```

---

## 依赖注入

### 服务注册

**App.xaml.cs 中注册**:

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 单例服务
    containerRegistry.RegisterSingleton<IAuthenticationService, AuthenticationService>();
    containerRegistry.RegisterSingleton<ITokenStorageService, TokenStorageService>();
    
    // 瞬态服务（每次注入创建新实例）
    containerRegistry.Register<IHerbRepository, HerbRepository>();
    containerRegistry.Register<IPatientRepository, PatientRepository>();
    
    // 注册 Refit API 接口
    containerRegistry.RegisterSingleton(provider => 
    {
        return RestService.For<IHerbApi>(httpClient);
    });
    
    // 注册 ViewModel（导航用）
    containerRegistry.RegisterForNavigation<HerbManagementView>();
    containerRegistry.RegisterForNavigation<HerbDetailView>();
}
```

### 构造函数注入

**ViewModel 中注入依赖**:

```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    private readonly IHerbRepository _herbRepository;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;
    
    public HerbManagementViewModel(
        IHerbRepository herbRepository,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _herbRepository = herbRepository;
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;
    }
}
```

### 服务定位器（不推荐）

**仅在特殊情况使用**:

```csharp
// ⚠️ 不推荐：破坏依赖注入原则
var container = ContainerLocator.Container;
var service = container.Resolve<IHerbRepository>();

// ✅ 推荐：构造函数注入
public MyViewModel(IHerbRepository herbRepository) { }
```

---

## 事件聚合器

### 定义事件

**创建事件类**:

```csharp
public class HerbUpdatedEvent : PubSubEvent<HerbDto>
{
}

public class PatientSelectedEvent : PubSubEvent<int>
{
}
```

### 发布事件

**发送方**:

```csharp
public class HerbDetailViewModel : UnifiedViewModelBase
{
    private readonly IEventAggregator _eventAggregator;
    
    private async Task SaveAsync()
    {
        await _herbRepository.UpdateAsync(CurrentHerb);
        
        // 发布事件
        _eventAggregator.GetEvent<HerbUpdatedEvent>().Publish(CurrentHerb);
    }
}
```

### 订阅事件

**接收方**:

```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    public HerbManagementViewModel(IEventAggregator eventAggregator, ...)
        : base(eventAggregator, ...)
    {
        // 订阅事件
        eventAggregator.GetEvent<HerbUpdatedEvent>()
            .Subscribe(OnHerbUpdated, ThreadOption.UIThread, keepSubscriberReferenceAlive: true);
    }
    
    private void OnHerbUpdated(HerbDto updatedHerb)
    {
        // 处理事件
        var existingHerb = Items.FirstOrDefault(h => h.Id == updatedHerb.Id);
        if (existingHerb != null)
        {
            var index = Items.IndexOf(existingHerb);
            Items[index] = updatedHerb;
        }
    }
}
```

### 线程选项

```csharp
// PublisherThread - 在发布者线程执行
eventAggregator.GetEvent<MyEvent>()
    .Subscribe(OnMyEvent, ThreadOption.PublisherThread);

// UIThread - 在UI线程执行（WPF推荐）
eventAggregator.GetEvent<MyEvent>()
    .Subscribe(OnMyEvent, ThreadOption.UIThread);

// BackgroundThread - 在后台线程执行
eventAggregator.GetEvent<MyEvent>()
    .Subscribe(OnMyEvent, ThreadOption.BackgroundThread);
```

### 取消订阅

```csharp
private SubscriptionToken _subscriptionToken;

public void Subscribe()
{
    _subscriptionToken = _eventAggregator.GetEvent<HerbUpdatedEvent>()
        .Subscribe(OnHerbUpdated, ThreadOption.UIThread);
}

public void Unsubscribe()
{
    _subscriptionToken?.Dispose();
}

protected override void OnDisposing()
{
    Unsubscribe();
    base.OnDisposing();
}
```

---

## 对话框服务

### 定义对话框 ViewModel

**实现 IDialogAware**:

```csharp
public class ConfirmDeleteDialogViewModel : BindableBase, IDialogAware
{
    public string Title => "确认删除";
    
    public event Action<IDialogResult> RequestClose;
    
    private string _message;
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }
    
    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand CancelCommand { get; }
    
    public ConfirmDeleteDialogViewModel()
    {
        ConfirmCommand = new DelegateCommand(OnConfirm);
        CancelCommand = new DelegateCommand(OnCancel);
    }
    
    public bool CanCloseDialog() => true;
    
    public void OnDialogClosed()
    {
        // 对话框关闭后的清理
    }
    
    public void OnDialogOpened(IDialogParameters parameters)
    {
        // 接收参数
        Message = parameters.GetValue<string>("message");
    }
    
    private void OnConfirm()
    {
        var result = new DialogResult(ButtonResult.OK);
        RequestClose?.Invoke(result);
    }
    
    private void OnCancel()
    {
        var result = new DialogResult(ButtonResult.Cancel);
        RequestClose?.Invoke(result);
    }
}
```

### 注册对话框

**App.xaml.cs**:

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterDialog<ConfirmDeleteDialogView, ConfirmDeleteDialogViewModel>();
}
```

### 显示对话框

**调用方 ViewModel**:

```csharp
public class HerbManagementViewModel : UnifiedViewModelBase
{
    private readonly IDialogService _dialogService;
    
    public HerbManagementViewModel(IDialogService dialogService, ...)
    {
        _dialogService = dialogService;
    }
    
    private async Task DeleteHerbAsync(HerbDto herb)
    {
        // 显示确认对话框
        var parameters = new DialogParameters
        {
            { "message", $"确定要删除药材 {herb.Name} 吗？" }
        };
        
        _dialogService.ShowDialog(
            "ConfirmDeleteDialogView", 
            parameters, 
            result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    // 用户点击确认，执行删除
                    _ = ExecuteDeleteAsync(herb);
                }
            });
    }
}
```

---

## 最佳实践

### 1. Region 管理

- ✅ 使用有意义的 Region 名称（如 `MainRegion`、`NavigationRegion`）
- ✅ Region 只在 Shell 中定义，避免嵌套 Region
- ⚠️ 避免在 UserControl 中定义 Region

### 2. 导航系统

- ✅ 使用 NavigationParameters 传递参数
- ✅ 实现 INavigationAware 接收导航事件
- ✅ 使用 IConfirmNavigationRequest 防止意外离开
- ⚠️ 避免在 ViewModel 构造函数中执行耗时操作，使用 OnNavigatedTo

### 3. 命令系统

- ✅ 优先使用 ObservesProperty 自动刷新
- ✅ 异步操作使用 AsyncDelegateCommand
- ✅ 命令执行期间设置 IsBusy 状态
- ⚠️ 避免命令无限循环（详见 [WPF常见问题](wpf-common-issues.md#命令无限循环)）

### 4. 依赖注入

- ✅ 优先使用构造函数注入
- ✅ 单例服务用 RegisterSingleton
- ✅ 瞬态服务用 Register
- ⚠️ 避免使用 ServiceLocator 模式

### 5. 事件聚合器

- ✅ WPF 应用使用 ThreadOption.UIThread
- ✅ 在 Dispose 中取消订阅
- ✅ 使用强类型事件类
- ⚠️ 避免事件风暴（过多事件发布）

### 6. 对话框服务

- ✅ 使用 IDialogAware 接口
- ✅ 通过 DialogParameters 传递参数
- ✅ 使用 ButtonResult 返回结果
- ⚠️ 避免在对话框中执行复杂业务逻辑

---

## 相关文档

- [Client端架构总览](../explanation/architecture/client/README.md) - MVVM架构说明
- [WPF常见问题](wpf-common-issues.md) - 命令绑定、视觉树问题
- [UI设计规范](ui-design-guidelines.md) - XAML样式和模板
- [Foundation层设计](../explanation/architecture/client/foundation-design.md) - ViewModelBase设计

---

**最后更新**: 2025-11-10  
**维护者**: LYBTZYZS 开发团队  
**Prism 版本**: 8.1.97
