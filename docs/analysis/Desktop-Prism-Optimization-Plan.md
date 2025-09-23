# Desktop 项目 Prism 优化方案（历史调研）

> 重要说明：当前项目基线固定在 .NET 8 + Prism 8.1.97，暂无升级计划。本文件为历史调研与方案备忘，非当前实施计划。实施工作请以 docs/overview/guiding-philosophy.md 为准。


## 一、优化目标

### 1.1 主要目标
- **符合Prism最佳实践**: 将架构调整至Prism 9.0官方推荐模式
- **提升模块独立性**: 实现真正的模块化，支持独立开发和部署
- **改善系统性能**: 优化启动时间，减少内存占用
- **增强可维护性**: 简化依赖关系，提高代码可读性

### 1.2 量化指标
- 启动时间减少30%
- 内存占用减少20%
- 模块间耦合度降低50%
- 单元测试覆盖率提升至60%

## 二、优化策略

### 2.1 分阶段实施
- **Phase 1**: 基础架构调整（2周）
- **Phase 2**: 模块化重构（3周）
- **Phase 3**: 导航系统升级（2周）
- **Phase 4**: 性能优化（1周）
- **Phase 5**: 测试和文档（2周）

### 2.2 风险控制
- 保持功能兼容性
- 渐进式重构
- 充分的自动化测试
- 完整的回滚方案

## 三、详细优化方案

### Phase 1: 基础架构调整（第1-2周）

#### 1.1 清理Module定义混淆

**当前问题**:
```csharp
// 存在两种Module定义
AuthenticationModule : IModule  // Prism Module
AuthModule : IAuthService       // 业务服务
```

**优化方案**:
```csharp
// 1. 重命名业务服务，避免混淆
public class AuthService : BaseApiService<IAuthApi>, IAuthService
{
    // 纯业务逻辑实现
}

// 2. 保持Prism Module纯粹性
public class AuthModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 模块内服务注册
        containerRegistry.RegisterScoped<IAuthService, AuthService>();
        containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑
        var regionManager = containerProvider.Resolve<IRegionManager>();
        regionManager.RegisterViewWithRegion("LoginRegion", typeof(LoginView));
    }
}
```

#### 1.2 建立统一的服务生命周期策略

**生命周期管理规则**:
```csharp
public static class ServiceLifecyclePolicy
{
    // Singleton: 应用程序级服务
    // - ISessionManager, IThemeService, IConfiguration

    // Scoped: 模块级服务
    // - IAuthService, IUserService, IPatientService

    // Transient: 短生命周期对象
    // - ViewModels, Dialogs, Commands
}
```

**实现示例**:
```csharp
public static class ServiceRegistrationExtensions
{
    public static void RegisterModuleServices(this IContainerRegistry container)
    {
        // Singleton services
        container.RegisterSingleton<ISessionManager, SessionManager>();
        container.RegisterSingleton<IConfigurationService, ConfigurationService>();

        // Scoped services (per module)
        container.RegisterScoped<IAuthService, AuthService>();
        container.RegisterScoped<IUserService, UserService>();

        // Transient services
        container.Register<LoginViewModel>();
        container.Register<UserDetailViewModel>();
    }
}
```

### Phase 2: 模块化重构（第3-5周）

#### 2.1 实现真正的模块自治

**创建模块基类**:
```csharp
public abstract class ModuleBase : IModule
{
    protected IRegionManager RegionManager { get; private set; }
    protected IContainerProvider Container { get; private set; }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        Container = containerProvider;
        RegionManager = containerProvider.Resolve<IRegionManager>();

        ConfigureModule();
        RegisterViews();
        InitializeModule();
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        RegisterServices(containerRegistry);
        RegisterViewModels(containerRegistry);
        RegisterForNavigation(containerRegistry);
    }

    protected abstract void RegisterServices(IContainerRegistry containerRegistry);
    protected abstract void RegisterViewModels(IContainerRegistry containerRegistry);
    protected abstract void RegisterForNavigation(IContainerRegistry containerRegistry);
    protected abstract void ConfigureModule();
    protected abstract void RegisterViews();
    protected abstract void InitializeModule();
}
```

**模块实现示例**:
```csharp
public class PatientModule : ModuleBase
{
    protected override void RegisterServices(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterScoped<IPatientService, PatientService>();
        containerRegistry.RegisterScoped<IPatientRepository, PatientRepository>();
    }

    protected override void RegisterViewModels(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<PatientListViewModel>();
        containerRegistry.Register<PatientDetailViewModel>();
        containerRegistry.Register<PatientEditViewModel>();
    }

    protected override void RegisterForNavigation(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<PatientListView, PatientListViewModel>();
        containerRegistry.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();
        containerRegistry.RegisterForNavigation<PatientEditView, PatientEditViewModel>();
    }

    protected override void ConfigureModule()
    {
        // 配置模块特定设置
    }

    protected override void RegisterViews()
    {
        RegionManager.RegisterViewWithRegion(RegionNames.MainContent, typeof(PatientListView));
    }

    protected override void InitializeModule()
    {
        // 执行模块初始化逻辑
    }
}
```

#### 2.2 建立模块间通信协议

**定义共享接口**:
```csharp
// Shared/Interfaces/IModuleCommunication.cs
public interface IPatientSelectionService
{
    event EventHandler<PatientSelectedEventArgs> PatientSelected;
    void SelectPatient(Guid patientId);
    PatientDto GetSelectedPatient();
}

public interface IMedicalCaseService
{
    void StartNewCase(Guid patientId);
    void CompleteCase(Guid caseId);
    MedicalCaseDto GetActiveCase();
}
```

**使用事件聚合器进行松耦合通信**:
```csharp
// 定义事件
public class PatientSelectedEvent : PubSubEvent<PatientDto> { }
public class MedicalCaseStartedEvent : PubSubEvent<MedicalCaseDto> { }

// 发布事件
_eventAggregator.GetEvent<PatientSelectedEvent>().Publish(patient);

// 订阅事件
_eventAggregator.GetEvent<PatientSelectedEvent>()
    .Subscribe(OnPatientSelected, ThreadOption.UIThread);
```

### Phase 3: 导航系统升级（第6-7周）

#### 3.1 实现Region导航

**定义Region常量**:
```csharp
public static class RegionNames
{
    public const string MainContent = "MainContentRegion";
    public const string NavigationMenu = "NavigationMenuRegion";
    public const string StatusBar = "StatusBarRegion";
    public const string DialogHost = "DialogHostRegion";
    public const string ToolBar = "ToolBarRegion";
}
```

**Shell视图定义Region**:
```xml
<!-- MainWindow.xaml -->
<DockPanel>
    <!-- Navigation Menu -->
    <ContentControl DockPanel.Dock="Left"
                    prism:RegionManager.RegionName="{x:Static inf:RegionNames.NavigationMenu}"/>

    <!-- Status Bar -->
    <ContentControl DockPanel.Dock="Bottom"
                    prism:RegionManager.RegionName="{x:Static inf:RegionNames.StatusBar}"/>

    <!-- Tool Bar -->
    <ContentControl DockPanel.Dock="Top"
                    prism:RegionManager.RegionName="{x:Static inf:RegionNames.ToolBar}"/>

    <!-- Main Content -->
    <ContentControl prism:RegionManager.RegionName="{x:Static inf:RegionNames.MainContent}"/>
</DockPanel>
```

#### 3.2 实现URI导航

**导航服务封装**:
```csharp
public class NavigationService : INavigationService
{
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;

    public NavigationService(IRegionManager regionManager, IEventAggregator eventAggregator)
    {
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;
    }

    public void NavigateTo(string viewName, NavigationParameters parameters = null)
    {
        _regionManager.RequestNavigate(RegionNames.MainContent, viewName, parameters);
    }

    public void NavigateToDialog(string dialogName, NavigationParameters parameters = null)
    {
        _regionManager.RequestNavigate(RegionNames.DialogHost, dialogName, parameters);
    }

    public bool CanNavigateBack()
    {
        var region = _regionManager.Regions[RegionNames.MainContent];
        return region.NavigationService.Journal.CanGoBack;
    }

    public void NavigateBack()
    {
        var region = _regionManager.Regions[RegionNames.MainContent];
        region.NavigationService.Journal.GoBack();
    }
}
```

**ViewModel导航实现**:
```csharp
public class PatientListViewModel : BindableBase, INavigationAware
{
    private readonly INavigationService _navigationService;

    public DelegateCommand<PatientDto> ViewDetailsCommand { get; }

    public PatientListViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        ViewDetailsCommand = new DelegateCommand<PatientDto>(ViewDetails);
    }

    private void ViewDetails(PatientDto patient)
    {
        var parameters = new NavigationParameters
        {
            { "PatientId", patient.Id }
        };

        _navigationService.NavigateTo("PatientDetailView", parameters);
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 导航到此视图时执行
        LoadPatients();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true; // 复用实例
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 离开此视图时执行
    }
}
```

### Phase 4: 性能优化（第8周）

#### 4.1 模块延迟加载

```csharp
public class ModuleCatalog : IModuleCatalog
{
    public void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 核心模块立即加载
        moduleCatalog.AddModule<AuthModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<PatientModule>(InitializationMode.WhenAvailable);

        // 辅助模块按需加载
        moduleCatalog.AddModule<HerbModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<ReportModule>(InitializationMode.OnDemand);
    }
}
```

#### 4.2 优化事件聚合器使用

```csharp
public class OptimizedEventSubscription
{
    private readonly List<SubscriptionToken> _subscriptions = new();

    public void Subscribe<TEvent>(Action<TEvent> action) where TEvent : PubSubEvent<TEvent>, new()
    {
        var token = _eventAggregator.GetEvent<TEvent>()
            .Subscribe(action,
                ThreadOption.BackgroundThread,  // 后台线程处理
                false,                          // 弱引用
                e => true);                     // 过滤条件

        _subscriptions.Add(token);
    }

    public void Unsubscribe()
    {
        foreach (var token in _subscriptions)
        {
            token.Dispose();
        }
        _subscriptions.Clear();
    }
}
```

### Phase 5: 测试和文档（第9-10周）

#### 5.1 单元测试框架

```csharp
[TestClass]
public class PatientModuleTests
{
    private IContainerProvider _container;
    private IRegionManager _regionManager;

    [TestInitialize]
    public void Setup()
    {
        var containerRegistry = new ContainerRegistry();
        var module = new PatientModule();
        module.RegisterTypes(containerRegistry);

        _container = containerRegistry.Build();
        _regionManager = new RegionManager();
    }

    [TestMethod]
    public void PatientService_Should_Be_Registered()
    {
        var service = _container.Resolve<IPatientService>();
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void PatientListView_Should_Navigate_To_Details()
    {
        var viewModel = _container.Resolve<PatientListViewModel>();
        var patient = new PatientDto { Id = Guid.NewGuid() };

        viewModel.ViewDetailsCommand.Execute(patient);

        // 验证导航参数
        Assert.IsTrue(_navigationService.LastNavigationParameters.ContainsKey("PatientId"));
    }
}
```

#### 5.2 集成测试

```csharp
[TestClass]
public class ModuleIntegrationTests
{
    [TestMethod]
    public void All_Modules_Should_Load_Without_Errors()
    {
        var app = new TestableApp();
        app.Initialize();

        Assert.AreEqual(8, app.LoadedModules.Count);
        Assert.IsTrue(app.LoadedModules.All(m => m.State == ModuleState.Initialized));
    }
}
```

## 四、实施计划

### 4.1 时间线
```
Week 1-2:  基础架构调整
Week 3-5:  模块化重构
Week 6-7:  导航系统升级
Week 8:    性能优化
Week 9-10: 测试和文档
```

### 4.2 资源需求
- **开发人员**: 3名高级开发，2名中级开发
- **测试人员**: 2名测试工程师
- **架构师**: 1名（兼职）

### 4.3 里程碑
1. **M1 (Week 2)**: 基础架构调整完成
2. **M2 (Week 5)**: 模块化重构完成
3. **M3 (Week 7)**: 导航系统升级完成
4. **M4 (Week 8)**: 性能优化完成
5. **M5 (Week 10)**: 项目交付

## 五、风险管理

### 5.1 技术风险
| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 重构引入新Bug | 中 | 高 | 充分的自动化测试 |
| 性能下降 | 低 | 中 | 性能基准测试 |
| 兼容性问题 | 低 | 高 | 渐进式重构 |

### 5.2 项目风险
| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 进度延迟 | 中 | 中 | 预留缓冲时间 |
| 资源不足 | 低 | 高 | 提前锁定资源 |
| 需求变更 | 中 | 中 | 敏捷迭代 |

## 六、成功标准

### 6.1 技术指标
- ✅ 所有模块符合Prism最佳实践
- ✅ 启动时间 < 3秒
- ✅ 内存占用 < 500MB
- ✅ 单元测试覆盖率 > 60%

### 6.2 业务指标
- ✅ 零功能回归
- ✅ 用户体验无感知
- ✅ 维护成本降低30%

## 七、后续优化建议

### 7.1 短期（3个月内）
1. 实现模块热更新
2. 添加性能监控
3. 优化数据加载策略

### 7.2 中期（6个月内）
1. 微服务化改造准备
2. 引入CQRS模式
3. 实现离线支持

### 7.3 长期（12个月内）
1. 云原生架构迁移
2. AI辅助诊断集成
3. 多租户支持

---
*方案版本: 1.0*
*创建日期: 2025-09-23*
*目标版本: Prism 9.0.537*
