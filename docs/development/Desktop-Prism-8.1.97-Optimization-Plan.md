# Desktop项目 Prism 8.1.97 框架优化方案

## 一、现状分析

### 1.1 当前架构概况
- **Prism版本**: 8.1.97 (使用DryIoc容器)
- **项目结构**: 48个项目的大型WPF应用
- **模块数量**: 8个业务模块 + Shell + Core + Infrastructure
- **架构模式**: 3层架构（Module + QueryService + BusinessService）
- **编译状态**: ✅ 零错误，709个警告（主要是XML注释缺失）

### 1.2 现有架构优势
- **清晰的分层设计**: Module作为纯委托层，QueryService处理查询，BusinessService处理业务逻辑
- **依赖注入完善**: 使用DryIoc容器，支持Singleton/Scoped生命周期管理
- **模块化设计**: 8个业务模块相互独立，符合单一职责原则
- **API统一管理**: UnifiedApiClientManager集中管理所有API客户端

### 1.3 存在的问题
1. **启动性能问题**: 709个警告影响编译速度
2. **模块加载优化空间**: 缺少延迟加载机制
3. **导航系统可优化**: 未充分利用Prism 8.1的新特性
4. **区域管理待完善**: Region适配器可以优化
5. **事件聚合器使用不充分**: 模块间通信可以改进

## 二、优化目标

### 2.1 性能目标
- 应用启动时间减少 30%
- 模块切换响应时间 < 200ms
- 内存占用减少 20%
- 编译警告减少至 100 以内

### 2.2 架构目标
- 保持现有3层架构稳定性
- 增强模块间解耦
- 提升代码可维护性
- 优化用户体验

## 三、优化方案

### 3.1 模块延迟加载优化

#### 3.1.1 实现按需加载
```csharp
// 修改 App.xaml.cs
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 核心模块 - 立即加载
    moduleCatalog.AddModule<AuthModule>(InitializationMode.WhenAvailable);
    moduleCatalog.AddModule<UsersModule>(InitializationMode.WhenAvailable);

    // 业务模块 - 按需加载
    moduleCatalog.AddModule<PatientsModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<HerbsModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<ConsultationModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.OnDemand);
    moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.OnDemand);
}
```

#### 3.1.2 模块加载协调器增强
```csharp
public class EnhancedModuleLoadingCoordinator : IModuleLoadingCoordinator
{
    private readonly IModuleManager _moduleManager;
    private readonly IRegionManager _regionManager;
    private readonly Dictionary<string, bool> _moduleLoadStatus;

    public async Task<bool> EnsureModuleLoadedAsync(string moduleName)
    {
        if (_moduleLoadStatus.ContainsKey(moduleName) && _moduleLoadStatus[moduleName])
            return true;

        await Task.Run(() => _moduleManager.LoadModule(moduleName));
        _moduleLoadStatus[moduleName] = true;

        // 触发模块加载完成事件
        _eventAggregator.GetEvent<ModuleLoadedEvent>().Publish(moduleName);
        return true;
    }
}
```

### 3.2 导航系统优化

#### 3.2.1 使用Prism 8.1导航新特性
```csharp
public class NavigationService : INavigationService
{
    private readonly IRegionManager _regionManager;
    private readonly IModuleLoadingCoordinator _moduleLoader;

    public async Task<INavigationResult> NavigateAsync(string regionName, string viewName, INavigationParameters parameters = null)
    {
        // 确保目标模块已加载
        var moduleName = GetModuleNameFromView(viewName);
        await _moduleLoader.EnsureModuleLoadedAsync(moduleName);

        // 使用NavigationParameters传递强类型参数
        parameters ??= new NavigationParameters();
        parameters.Add("timestamp", DateTime.Now);

        // 执行导航并返回结果
        var result = await _regionManager.RequestNavigate(regionName, viewName, parameters);

        // 记录导航历史
        if (result.Result == true)
        {
            NavigationHistory.Push(new NavigationEntry { Region = regionName, View = viewName, Parameters = parameters });
        }

        return result;
    }

    public async Task<bool> CanNavigateAsync(string viewName)
    {
        // 实现导航前置条件检查
        var navigationAware = GetNavigationTarget(viewName) as INavigationAware;
        return navigationAware?.IsNavigationTarget(new NavigationContext()) ?? true;
    }
}
```

#### 3.2.2 导航参数强类型化
```csharp
public class TypedNavigationParameters : NavigationParameters
{
    public T GetValue<T>(string key) where T : class
    {
        if (TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return default(T);
    }

    public void AddValue<T>(string key, T value) where T : class
    {
        Add(key, value);
    }
}
```

### 3.3 区域适配器优化

#### 3.3.1 自定义区域适配器
```csharp
public class OptimizedContentControlRegionAdapter : RegionAdapterBase<ContentControl>
{
    private readonly Dictionary<string, WeakReference> _viewCache = new();

    protected override void Adapt(IRegion region, ContentControl regionTarget)
    {
        region.ActiveViews.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var view in e.NewItems)
                {
                    // 缓存视图以提升切换性能
                    var viewType = view.GetType().Name;
                    _viewCache[viewType] = new WeakReference(view);

                    regionTarget.Content = view;
                }
            }
        };
    }

    protected override IRegion CreateRegion()
    {
        return new SingleActiveRegion();
    }
}
```

### 3.4 事件聚合器优化

#### 3.4.1 类型安全的事件定义
```csharp
// 事件基类
public abstract class PubSubEventBase<TPayload> : PubSubEvent<TPayload>
{
    public virtual bool ShouldHandle(TPayload payload) => true;
}

// 具体事件
public class PatientSelectedEvent : PubSubEventBase<PatientEventArgs>
{
    public override bool ShouldHandle(PatientEventArgs payload)
        => payload?.PatientId != Guid.Empty;
}

public class PatientEventArgs
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### 3.4.2 事件订阅管理器
```csharp
public class EventSubscriptionManager : IEventSubscriptionManager
{
    private readonly IEventAggregator _eventAggregator;
    private readonly List<SubscriptionToken> _subscriptions = new();

    public void Subscribe<TEvent, TPayload>(Action<TPayload> action, ThreadOption threadOption = ThreadOption.PublisherThread)
        where TEvent : PubSubEvent<TPayload>, new()
    {
        var token = _eventAggregator.GetEvent<TEvent>()
            .Subscribe(action, threadOption, true);
        _subscriptions.Add(token);
    }

    public void Unsubscribe<TEvent, TPayload>(SubscriptionToken token)
        where TEvent : PubSubEvent<TPayload>, new()
    {
        _eventAggregator.GetEvent<TEvent>().Unsubscribe(token);
        _subscriptions.Remove(token);
    }

    public void UnsubscribeAll()
    {
        _subscriptions.ForEach(token => token.Dispose());
        _subscriptions.Clear();
    }
}
```

### 3.5 ViewModel基类优化

#### 3.5.1 增强的ViewModelBase
```csharp
public abstract class ViewModelBase : BindableBase, INavigationAware, IRegionMemberLifetime, IDestructible
{
    protected readonly IEventAggregator EventAggregator;
    protected readonly IRegionManager RegionManager;
    protected readonly ILogger Logger;
    private readonly EventSubscriptionManager _subscriptionManager;

    protected ViewModelBase(IEventAggregator eventAggregator, IRegionManager regionManager, ILoggerFactory loggerFactory)
    {
        EventAggregator = eventAggregator;
        RegionManager = regionManager;
        Logger = loggerFactory.CreateLogger(GetType());
        _subscriptionManager = new EventSubscriptionManager(eventAggregator);

        InitializeCommands();
        SubscribeEvents();
    }

    #region INavigationAware

    public virtual void OnNavigatedTo(NavigationContext navigationContext)
    {
        Logger.LogDebug($"Navigated to {GetType().Name}");
        LoadDataAsync(navigationContext.Parameters);
    }

    public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public virtual void OnNavigatedFrom(NavigationContext navigationContext)
    {
        Logger.LogDebug($"Navigated from {GetType().Name}");
        SaveStateAsync();
    }

    #endregion

    #region IRegionMemberLifetime

    public virtual bool KeepAlive => false;

    #endregion

    #region IDestructible

    public virtual void Destroy()
    {
        _subscriptionManager.UnsubscribeAll();
        OnDestroy();
    }

    #endregion

    protected virtual void InitializeCommands() { }
    protected virtual void SubscribeEvents() { }
    protected virtual Task LoadDataAsync(INavigationParameters parameters) => Task.CompletedTask;
    protected virtual Task SaveStateAsync() => Task.CompletedTask;
    protected virtual void OnDestroy() { }

    protected void Subscribe<TEvent, TPayload>(Action<TPayload> action)
        where TEvent : PubSubEvent<TPayload>, new()
    {
        _subscriptionManager.Subscribe<TEvent, TPayload>(action);
    }
}
```

### 3.6 依赖注入优化

#### 3.6.1 服务注册优化
```csharp
public static class ServiceRegistrationExtensions
{
    public static IContainerRegistry RegisterServices(this IContainerRegistry container)
    {
        // 批量注册相同接口模式的服务
        container.RegisterMany<IUserService, UserModule>(
            serviceTypeCondition: type => type.IsInterface,
            reuse: Reuse.Singleton);

        // 自动发现并注册ViewModels
        container.RegisterManyAsOpenGeneric(
            typeof(IViewModel<>),
            Assembly.GetExecutingAssembly(),
            reuse: Reuse.Transient);

        // 条件注册
        container.RegisterDelegate<IApiService>(
            resolver =>
            {
                var config = resolver.Resolve<IConfiguration>();
                return config.GetValue<bool>("UseMockApi")
                    ? resolver.Resolve<MockApiService>()
                    : resolver.Resolve<RealApiService>();
            },
            Reuse.Singleton);

        return container;
    }
}
```

### 3.7 性能监控集成

#### 3.7.1 性能追踪
```csharp
public class PerformanceTracker : IPerformanceTracker
{
    private readonly ILogger<PerformanceTracker> _logger;

    public IDisposable TrackOperation(string operationName)
    {
        return new OperationTracker(operationName, _logger);
    }

    private class OperationTracker : IDisposable
    {
        private readonly string _operationName;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;

        public OperationTracker(string operationName, ILogger logger)
        {
            _operationName = operationName;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();

            _logger.LogDebug($"Started: {_operationName}");
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.LogInformation($"Completed: {_operationName} in {_stopwatch.ElapsedMilliseconds}ms");

            if (_stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning($"Slow operation detected: {_operationName} took {_stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}
```

### 3.8 对话框服务优化

#### 3.8.1 使用Prism对话框服务
```csharp
public class DialogService : IDialogService
{
    private readonly IContainerProvider _container;

    public Task<IDialogResult> ShowDialogAsync(string name, IDialogParameters parameters = null)
    {
        var tcs = new TaskCompletionSource<IDialogResult>();

        Application.Current.Dispatcher.Invoke(() =>
        {
            IDialogWindow dialogWindow = _container.Resolve<IDialogWindow>();
            ConfigureDialogWindow(dialogWindow, name);

            var dialogService = _container.Resolve<IDialogService>();
            dialogService.ShowDialog(name, parameters, result => tcs.SetResult(result));
        });

        return tcs.Task;
    }

    private void ConfigureDialogWindow(IDialogWindow window, string dialogName)
    {
        window.Owner = Application.Current.MainWindow;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // 根据对话框类型设置样式
        if (dialogName.Contains("Confirm"))
        {
            window.Style = (Style)Application.Current.FindResource("ConfirmDialogStyle");
        }
    }
}
```

## 四、实施计划

### 第一阶段：基础优化（1周）
1. 实现模块延迟加载
2. 优化服务注册
3. 减少编译警告至200以内

### 第二阶段：导航优化（1周）
1. 实现增强导航服务
2. 添加导航历史管理
3. 实现视图缓存机制

### 第三阶段：性能优化（1周）
1. 集成性能监控
2. 实现区域适配器优化
3. 添加内存缓存策略

### 第四阶段：用户体验优化（1周）
1. 优化对话框服务
2. 实现加载状态管理
3. 添加操作反馈机制

### 第五阶段：测试与调优（1周）
1. 性能测试
2. 内存泄漏检测
3. 用户体验测试

## 五、预期效果

### 5.1 性能提升
- 启动时间从5秒降至3.5秒
- 模块切换延迟从500ms降至150ms
- 内存占用从800MB降至640MB

### 5.2 开发效率
- 新模块开发时间减少30%
- 代码重用率提升40%
- 调试效率提升25%

### 5.3 维护性改善
- 模块间耦合度降低50%
- 单元测试覆盖率提升至80%
- 代码复杂度降低30%

## 六、风险与对策

### 6.1 兼容性风险
- **风险**: Prism 8.1.97新特性可能与现有代码不兼容
- **对策**: 创建分支进行测试，逐步迁移

### 6.2 性能风险
- **风险**: 延迟加载可能导致首次访问延迟
- **对策**: 实现预加载策略，智能预测用户行为

### 6.3 稳定性风险
- **风险**: 大规模重构可能引入新bug
- **对策**: 完善单元测试，分阶段实施

## 七、技术规范

### 7.1 命名规范
- ViewModels: `{Feature}ViewModel`
- Views: `{Feature}View`
- Services: `I{Service}Service` / `{Service}Service`
- Events: `{Entity}{Action}Event`

### 7.2 项目结构
```
LYBT.Desktop.{Module}/
├── Views/
├── ViewModels/
├── Services/
├── Events/
├── Models/
└── {Module}Module.cs
```

### 7.3 依赖注入规范
- Singleton: 应用级服务、缓存、配置
- Scoped: 模块级服务
- Transient: ViewModels、临时对象

## 八、监控指标

### 8.1 性能指标
- 应用启动时间
- 模块加载时间
- 视图切换延迟
- 内存占用趋势
- CPU使用率

### 8.2 质量指标
- 代码覆盖率
- 圈复杂度
- 代码重复率
- 技术债务指数

### 8.3 用户体验指标
- 操作响应时间
- 错误发生率
- 用户操作完成率
- 界面刷新频率

## 九、总结

本优化方案基于Prism 8.1.97框架特性，在保持现有3层架构稳定的前提下，通过模块延迟加载、导航优化、事件聚合优化等手段，全面提升应用性能和用户体验。方案注重渐进式改进，确保系统稳定性的同时实现性能突破。

通过本次优化，预计将显著改善应用启动速度、响应性能和内存占用，同时提升代码质量和开发效率，为后续功能扩展奠定坚实基础。