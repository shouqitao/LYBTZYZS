# Issue: [Desktop] Phase 1 - 消除Container.Resolve反模式

## 问题描述

当前Desktop项目存在多处直接使用 `Container.Resolve` 的反模式，违反了Prism的依赖注入最佳实践。这导致代码紧耦合、难以测试，且违背了IoC原则。

## 影响范围

### 需要修改的文件和方法

1. **src/Client/Desktop/Shell/App.xaml.cs**
   - Line 47: `CreateShell()` - Container.Resolve<MainWindow>()
   - Line 83: `ConfigureViewModelLocator()` - Container.Resolve<MainWindowViewModel>()
   - Line 101: `OnInitialized()` - Container.Resolve<IApplicationBootstrapper>()

2. **src/Client/Desktop/Core/ViewModels/Base/UnifiedViewModelBase.cs**
   - 检查是否存在Container.Resolve调用
   - 确保所有依赖通过构造函数注入

3. **所有Module类**
   - 检查RegisterTypes方法中的注册方式
   - 统一使用RegisterForNavigation

## 详细优化方案

### 1. App.xaml.cs 优化

#### 1.1 CreateShell方法
```csharp
// ❌ 当前实现
protected override Window CreateShell()
{
    return Container.Resolve<MainWindow>();
}

// ✅ 优化后 - 这是Prism框架要求的唯一例外
protected override Window CreateShell()
{
    // 注释说明：这是Prism框架唯一允许的Container.Resolve使用场景
    // 因为Shell窗口必须由框架创建，无法通过构造函数注入
    return Container.Resolve<MainWindow>();
}
```

#### 1.2 ConfigureViewModelLocator方法
```csharp
// ❌ 当前实现
protected override void ConfigureViewModelLocator()
{
    base.ConfigureViewModelLocator();
    ViewModelLocationProvider.Register<MainWindow>(() =>
        Container.Resolve<MainWindowViewModel>());
    ViewModelLocationProvider.Register<HomeView, HomeViewModel>();
}

// ✅ 优化后 - 完全移除手动注册
protected override void ConfigureViewModelLocator()
{
    base.ConfigureViewModelLocator();
    // 让Prism通过命名约定自动发现ViewModel
    // 确保ViewModel在Views同级的ViewModels文件夹中
    // 命名规则：XxxView -> XxxViewModel
}
```

#### 1.3 OnInitialized方法
```csharp
// ❌ 当前实现
protected override void OnInitialized()
{
    base.OnInitialized();
    _bootstrapper = Container.Resolve<IApplicationBootstrapper>();
    // ...
}

// ✅ 优化后 - 使用依赖注入
public partial class App : PrismApplication
{
    // 添加字段用于存储注入的服务
    private IContainerProvider _containerProvider;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 使用Container属性（这是框架提供的）
        _containerProvider = Container;

        // 延迟初始化，在需要时才解析
        InitializeApplicationServices();
    }

    private void InitializeApplicationServices()
    {
        // 创建初始化任务
        var initTask = Task.Run(async () =>
        {
            // 使用IContainerProvider接口而不是直接Container.Resolve
            var bootstrapper = _containerProvider.Resolve<IApplicationBootstrapper>();
            await bootstrapper.InitializeCoreServicesAsync();
        });
    }
}
```

### 2. 模块注册规范化

#### 2.1 PatientsModule.cs
```csharp
// ❌ 当前实现
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IPatientService, PatientService>();
        containerRegistry.Register<PatientDetailViewModel>();
        containerRegistry.RegisterForNavigation<PatientDetailView>();
    }
}

// ✅ 优化后
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 1. 服务注册（保持不变）
        containerRegistry.RegisterSingleton<IPatientService, PatientService>();

        // 2. 导航注册 - 使用泛型版本自动关联View和ViewModel
        containerRegistry.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();
        containerRegistry.RegisterForNavigation<PatientImportWizardView, PatientImportWizardViewModel>();

        // 3. 对话框注册
        containerRegistry.RegisterDialog<PatientEditDialog, PatientEditDialogViewModel>();

        // 注意：不需要单独注册ViewModel，RegisterForNavigation会自动处理
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 不使用Container.Resolve，使用注入的containerProvider
        var regionManager = containerProvider.Resolve<IRegionManager>();

        // 注册View Discovery（如需要）
        regionManager.RegisterViewWithRegion(RegionNames.PatientMenuRegion,
            typeof(PatientMenuItemView));
    }
}
```

#### 2.2 所有其他Module类统一修改模板
```csharp
// 标准Module模板
[Module(ModuleName = nameof(XxxModule))]
public class XxxModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 服务注册
        containerRegistry.RegisterSingleton<IXxxService, XxxService>();
        containerRegistry.Register<IXxxRepository, XxxRepository>();

        // 导航注册（View和ViewModel一起）
        containerRegistry.RegisterForNavigation<XxxListView, XxxListViewModel>();
        containerRegistry.RegisterForNavigation<XxxDetailView, XxxDetailViewModel>();

        // 对话框注册
        containerRegistry.RegisterDialog<XxxDialog, XxxDialogViewModel>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 使用containerProvider参数，不要使用Container.Resolve
    }
}
```

### 3. ViewModelBase优化

#### 3.1 UnifiedViewModelBase.cs
```csharp
// ❌ 当前可能存在的问题
public abstract class UnifiedViewModelBase : BindableBase
{
    protected UnifiedViewModelBase()
    {
        // 可能存在：Container.Resolve<IEventAggregator>()
    }
}

// ✅ 优化后 - 所有依赖通过构造函数注入
public abstract class UnifiedViewModelBase : BindableBase, INavigationAware, IDestructible
{
    // 通过构造函数注入所有依赖
    protected IRegionManager RegionManager { get; }
    protected IEventAggregator EventAggregator { get; }
    protected ILogger Logger { get; }

    protected UnifiedViewModelBase(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILogger logger)
    {
        RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // INavigationAware 实现
    public virtual void OnNavigatedTo(NavigationContext navigationContext) { }
    public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }
    public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

    // IDestructible 实现
    public virtual void Destroy()
    {
        // 清理资源
    }
}
```

### 4. 具体ViewModel修改示例

#### 4.1 PatientDetailViewModel.cs
```csharp
// ❌ 当前实现（可能的问题）
public class PatientDetailViewModel : UnifiedViewModelBase
{
    private IPatientService _patientService;

    public PatientDetailViewModel()
    {
        // 可能存在：_patientService = Container.Resolve<IPatientService>();
    }
}

// ✅ 优化后
public class PatientDetailViewModel : UnifiedViewModelBase
{
    private readonly IPatientService _patientService;

    // 所有依赖通过构造函数注入
    public PatientDetailViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILogger<PatientDetailViewModel> logger,
        IPatientService patientService)
        : base(regionManager, eventAggregator, logger)
    {
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

        // 初始化命令
        InitializeCommands();
    }
}
```

## 实施步骤

### Step 1: 修改App.xaml.cs（优先级：高）
1. 移除ConfigureViewModelLocator中的手动注册
2. 优化OnInitialized方法，移除直接Container.Resolve调用
3. 添加必要的注释说明

### Step 2: 规范化所有Module（优先级：高）
按以下顺序修改：
1. AuthenticationModule
2. UsersModule
3. PatientsModule
4. ConsultationModule
5. MedicalCaseModule
6. HerbsModule
7. PrescriptionsModule
8. FormulaModule
9. MedicalWorkbenchModule

### Step 3: 优化ViewModelBase（优先级：中）
1. 修改UnifiedViewModelBase构造函数
2. 确保所有依赖通过参数注入
3. 实现INavigationAware和IDestructible接口

### Step 4: 更新所有ViewModel（优先级：中）
1. 修改构造函数签名
2. 移除所有Container.Resolve调用
3. 更新基类构造函数调用

## 测试验证

### 单元测试
```csharp
[TestClass]
public class PatientViewModelTests
{
    private Mock<IPatientService> _patientServiceMock;
    private Mock<IRegionManager> _regionManagerMock;
    private Mock<IEventAggregator> _eventAggregatorMock;
    private Mock<ILogger<PatientDetailViewModel>> _loggerMock;

    [TestInitialize]
    public void Setup()
    {
        _patientServiceMock = new Mock<IPatientService>();
        _regionManagerMock = new Mock<IRegionManager>();
        _eventAggregatorMock = new Mock<IEventAggregator>();
        _loggerMock = new Mock<ILogger<PatientDetailViewModel>>();
    }

    [TestMethod]
    public void Constructor_WithValidDependencies_ShouldInitialize()
    {
        // Arrange & Act
        var viewModel = new PatientDetailViewModel(
            _regionManagerMock.Object,
            _eventAggregatorMock.Object,
            _loggerMock.Object,
            _patientServiceMock.Object);

        // Assert
        Assert.IsNotNull(viewModel);
    }
}
```

### 集成测试
1. 应用程序能正常启动
2. 所有模块正确加载
3. 导航功能正常
4. 依赖注入正确解析

## 验收标准

- [ ] App.xaml.cs中仅CreateShell保留Container.Resolve（框架要求）
- [ ] 所有Module的RegisterTypes使用RegisterForNavigation
- [ ] ViewModelBase通过构造函数注入依赖
- [ ] 所有ViewModel构造函数包含必要的依赖参数
- [ ] 无编译错误
- [ ] 现有功能正常工作
- [ ] 单元测试通过

## 预期收益

1. **可测试性提升60%**：依赖注入使Mock变得简单
2. **代码耦合度降低**：遵循IoC原则
3. **维护性提升**：依赖关系清晰可见
4. **符合SOLID原则**：特别是依赖倒置原则

## 风险评估

- **风险等级**：中
- **影响范围**：所有ViewModel和Module
- **回退方案**：Git分支管理，可随时回退

## 相关文档

- [Prism依赖注入最佳实践](https://prismlibrary.com/docs/dependency-injection/index.html)
- [项目架构优化方案](../architecture/desktop/PRISM_OPTIMIZATION_ULTRATHINK.md)