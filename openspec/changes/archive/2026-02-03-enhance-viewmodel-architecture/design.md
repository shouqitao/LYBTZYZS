# enhance-viewmodel-architecture 设计文档

## 设计原则

1. **能统一就统一** - 消除所有特例
2. **能简单就不复杂** - KISS原则

---

## 现状分析

### 当前构造函数参数统计

| 基类 | 当前参数数量 | 参数列表 |
|------|-------------|----------|
| CoreViewModelBase | 2 | ILoggerFactory, IEventAggregator |
| DialogViewModelBase | 2 | 继承自CoreViewModelBase |
| NavigableViewModelBase | 7 | ILoggerFactory, IEventAggregator, IRegionManager, ISessionManager?, IUserNotificationService?, ICommonDialogService?, IRoleRegistry? |
| MasterDetailViewModelBase | 2 | IMasterDetailServices, ILoggerFactory (组合模式) |

### 子类构造函数模式

**当前模式** (AdminHomeViewModel为例):
```csharp
public AdminHomeViewModel(
    IRegionManager regionManager,           // 基类服务 1
    IEventAggregator eventAggregator,       // 基类服务 2
    ILoggerFactory loggerFactory,           // 基类服务 3
    IAuthenticationService authService,     // 业务依赖 1
    IDialogService dialogService,           // 业务依赖 2
    INavigationCoordinator navigationCoordinator)  // 业务依赖 3
    : base(loggerFactory, eventAggregator, regionManager)  // 只传3个基础参数
{
    // ...
}
```

**当前模式** (LoginViewModel为例):
```csharp
public LoginViewModel(
    ILoginCoordinator loginCoordinator,          // 业务依赖
    ILoggerFactory loggerFactory,                // 基类服务 1
    IEventAggregator eventAggregator,            // 基类服务 2
    IRegionManager regionManager,                // 基类服务 3
    IApplicationStateService applicationStateService,  // 业务依赖
    // ... 更多服务
    ICommonDialogService? dialogService = null)  // 基类可选服务
    : base(loggerFactory, eventAggregator, regionManager, null, null, dialogService)  // 传6个参数
{
    // ...
}
```

### 特例: AccountSettingsViewModel

```csharp
// 当前: 手动实现INavigationAware (违反统一原则)
public partial class AccountSettingsViewModel : CoreViewModelBase, INavigationAware
{
    // 手动字段
    private readonly ISessionManager _sessionManager;
    private readonly IRegionManager _regionManager;

    // 手动实现INavigationAware
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedTo(NavigationContext navigationContext) { /* ... */ }
    public void OnNavigatedFrom(NavigationContext navigationContext) { /* ... */ }
}
```

---

## 核心变更

### 变更1: IViewModelServices服务聚合 (简化)

**目的**: 将7个构造函数参数简化为1个

#### 接口定义 (新文件: Contracts/Services/IViewModelServices.cs)

```csharp
namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// ViewModel服务聚合接口
    /// OpenSpec: enhance-viewmodel-architecture
    ///
    /// 设计原则:
    /// - 聚合ViewModel基类所需的通用服务
    /// - 简化子类构造函数参数
    /// - 所有服务非空(DI保证)
    /// </summary>
    public interface IViewModelServices
    {
        /// <summary>
        /// 日志工厂
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Prism事件聚合器
        /// </summary>
        IEventAggregator EventAggregator { get; }

        /// <summary>
        /// Prism区域管理器
        /// </summary>
        IRegionManager RegionManager { get; }

        /// <summary>
        /// 会话管理器
        /// </summary>
        ISessionManager SessionManager { get; }

        /// <summary>
        /// 用户通知服务
        /// </summary>
        IUserNotificationService UserNotificationService { get; }

        /// <summary>
        /// 通用对话框服务
        /// </summary>
        ICommonDialogService CommonDialogService { get; }

        /// <summary>
        /// 角色注册表
        /// </summary>
        IRoleRegistry RoleRegistry { get; }
    }
}
```

#### 实现类 (新文件: Infrastructure/Services/ViewModelServices.cs)

```csharp
namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// ViewModel服务聚合实现
    /// OpenSpec: enhance-viewmodel-architecture
    /// </summary>
    public sealed class ViewModelServices : IViewModelServices
    {
        public ILoggerFactory LoggerFactory { get; }
        public IEventAggregator EventAggregator { get; }
        public IRegionManager RegionManager { get; }
        public ISessionManager SessionManager { get; }
        public IUserNotificationService UserNotificationService { get; }
        public ICommonDialogService CommonDialogService { get; }
        public IRoleRegistry RoleRegistry { get; }

        public ViewModelServices(
            ILoggerFactory loggerFactory,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            ISessionManager sessionManager,
            IUserNotificationService userNotificationService,
            ICommonDialogService commonDialogService,
            IRoleRegistry roleRegistry)
        {
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            UserNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));
            CommonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));
            RoleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
        }
    }
}
```

#### DI注册 (Shell/Extensions/ServiceCollectionExtensions.cs)

```csharp
// 添加到RegisterViewModelServices方法
services.AddSingleton<IViewModelServices, ViewModelServices>();
```

---

### 变更2: 基类构造函数重构

#### CoreViewModelBase 变更

```csharp
// 变更前
protected CoreViewModelBase(ILoggerFactory loggerFactory, IEventAggregator eventAggregator)
{
    LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    Logger = loggerFactory.CreateLogger(GetType());
}

// 变更后
protected IViewModelServices Services { get; }

protected CoreViewModelBase(IViewModelServices services)
{
    Services = services ?? throw new ArgumentNullException(nameof(services));
    LoggerFactory = services.LoggerFactory;
    EventAggregator = services.EventAggregator;
    Logger = services.LoggerFactory.CreateLogger(GetType());
}
```

#### NavigableViewModelBase 变更

```csharp
// 变更前 (7个参数)
protected NavigableViewModelBase(
    ILoggerFactory loggerFactory,
    IEventAggregator eventAggregator,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null,
    ICommonDialogService? commonDialogService = null,
    IRoleRegistry? roleRegistry = null)
    : base(loggerFactory, eventAggregator)
{
    RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
    SessionManager = sessionManager;
    UserNotificationService = userNotificationService;
    CommonDialogService = commonDialogService;
    RoleRegistry = roleRegistry;
}

// 变更后 (1个参数)
protected NavigableViewModelBase(IViewModelServices services)
    : base(services)
{
    RegionManager = services.RegionManager;
    SessionManager = services.SessionManager;
    UserNotificationService = services.UserNotificationService;
    CommonDialogService = services.CommonDialogService;
    RoleRegistry = services.RoleRegistry;
}
```

#### DialogViewModelBase 变更

```csharp
// 变更前
protected DialogViewModelBase(ILoggerFactory loggerFactory, IEventAggregator eventAggregator)
    : base(loggerFactory, eventAggregator)
{
}

// 变更后
protected DialogViewModelBase(IViewModelServices services)
    : base(services)
{
}
```

---

### 变更3: MasterDetailViewModelBase 特殊处理

**当前架构分析**:
MasterDetailViewModelBase 直接继承 ObservableObject (而非 NavigableViewModelBase)，使用组合模式委托给 IMasterDetailServices。

**决策**: 保持组合模式，增加IViewModelServices
- 保留IMasterDetailServices组合（CRUD/分页/搜索功能）
- 添加IViewModelServices（通用ViewModel服务）
- 继承链改为: ObservableObject → CoreViewModelBase → MasterDetailViewModelBase

```csharp
// 变更前
public abstract partial class MasterDetailViewModelBase<TListItem, TDetail>
    : ObservableObject, INavigationAware, IRegionMemberLifetime, IDisposable
{
    private readonly IMasterDetailServices<TListItem, TDetail> _services;

    protected MasterDetailViewModelBase(
        IMasterDetailServices<TListItem, TDetail> services,
        ILoggerFactory loggerFactory)
    {
        _services = services;
        Logger = loggerFactory.CreateLogger(GetType());
    }
}

// 变更后 - 改为继承CoreViewModelBase
public abstract partial class MasterDetailViewModelBase<TListItem, TDetail>
    : CoreViewModelBase, INavigationAware, IRegionMemberLifetime
{
    private readonly IMasterDetailServices<TListItem, TDetail> _masterDetailServices;

    protected IMasterDetailServices<TListItem, TDetail> MasterDetailServices => _masterDetailServices;

    protected MasterDetailViewModelBase(
        IViewModelServices services,
        IMasterDetailServices<TListItem, TDetail> masterDetailServices)
        : base(services)
    {
        _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
        SubscribeToServiceEvents();
    }
}
```

---

### 变更4: AccountSettingsViewModel统一 (消除特例)

#### 当前状态 (特例)

```csharp
public partial class AccountSettingsViewModel : CoreViewModelBase, INavigationAware
{
    // 手动字段 - 将由基类提供
    private readonly ISessionManager _sessionManager;
    private readonly IRegionManager _regionManager;
    private readonly IUserNotificationService _notificationService;

    // 手动INavigationAware实现
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        Logger.LogDebug("导航到账户设置页面");
        _ = LoadCurrentUserAsync();
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        _password = string.Empty;
        _newPassword = string.Empty;
        _confirmPassword = string.Empty;
    }
}
```

#### 目标状态 (统一)

```csharp
public partial class AccountSettingsViewModel : NavigableViewModelBase
{
    public AccountSettingsViewModel(
        IViewModelServices services,
        IAuthService authService,
        IUserRepository userRepository)
        : base(services)
    {
        _authService = authService;
        _userRepository = userRepository;
    }

    // 使用基类钩子替代手动实现
    protected override void OnNavigatedToCore(NavigationContext context)
    {
        Logger.LogDebug("导航到账户设置页面");
        _ = LoadCurrentUserAsync();
    }

    protected override void OnNavigatedFromCore(NavigationContext context)
    {
        // 清理密码字段
        _password = string.Empty;
        _newPassword = string.Empty;
        _confirmPassword = string.Empty;
    }
}
```

---

## 最终架构

```
ObservableObject
│
├── CoreViewModelBase
│   │  构造函数: (IViewModelServices services)
│   │  提供: Logger, IsBusy, ErrorMessage, EventAggregator, Services属性
│   │
│   ├── MainWindowViewModel [Shell容器, 不参与导航]
│   │
│   ├── DialogViewModelBase : IDialogAware
│   │   │  构造函数: (IViewModelServices services)
│   │   │
│   │   ├── ApiConnectionFailedDialogViewModel
│   │   ├── ConfirmationDialogViewModel
│   │   └── EntityAuditLogDialogViewModel
│   │
│   ├── NavigableViewModelBase : INavigationAware, IConfirmNavigationRequest, IRegionMemberLifetime
│   │   │  构造函数: (IViewModelServices services)
│   │   │  提供: PageTitle, IsLoading, HasUnsavedChanges, 导航方法, 对话框方法
│   │   │
│   │   ├── AdminHomeViewModel
│   │   ├── ClinicalHomeViewModel
│   │   ├── PatientSelectionViewModel
│   │   ├── LoginViewModel
│   │   ├── SystemSettingsViewModel
│   │   ├── MedicalCaseWorkspaceViewModel
│   │   └── AccountSettingsViewModel [统一后]
│   │
│   └── MasterDetailViewModelBase<TList,TDetail> : INavigationAware, IRegionMemberLifetime
│       │  构造函数: (IViewModelServices services, IMasterDetailServices<T1,T2> masterDetailServices)
│       │
│       ├── FormulaMasterDetailViewModel
│       ├── HerbMasterDetailViewModel
│       ├── MedicalCaseMasterDetailViewModel
│       ├── PatientMasterDetailViewModel
│       └── UserMasterDetailViewModel
│
└── HerbItemViewModelBase [模块专用, 不变]
```

---

## 子类构造函数重构模式

### NavigableViewModel子类 (6个)

```csharp
// 变更前 (AdminHomeViewModel为例)
public AdminHomeViewModel(
    IRegionManager regionManager,
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IAuthenticationService authService,
    IDialogService dialogService,
    INavigationCoordinator navigationCoordinator)
    : base(loggerFactory, eventAggregator, regionManager)

// 变更后
public AdminHomeViewModel(
    IViewModelServices services,              // 聚合服务
    IAuthenticationService authService,       // 业务依赖
    IDialogService dialogService,             // 业务依赖
    INavigationCoordinator navigationCoordinator)  // 业务依赖
    : base(services)
```

### DialogViewModel子类 (3个)

```csharp
// 变更前
public ConfirmationDialogViewModel(
    ILoggerFactory loggerFactory,
    IEventAggregator eventAggregator)
    : base(loggerFactory, eventAggregator)

// 变更后
public ConfirmationDialogViewModel(IViewModelServices services)
    : base(services)
```

### MasterDetailViewModel子类 (5个)

```csharp
// 变更前 (HerbMasterDetailViewModel为例)
public HerbMasterDetailViewModel(
    IMasterDetailServices<HerbListDto, HerbDetailDto> services,
    ILoggerFactory loggerFactory,
    IHerbRepository herbRepository)
    : base(services, loggerFactory)

// 变更后
public HerbMasterDetailViewModel(
    IViewModelServices services,
    IMasterDetailServices<HerbListDto, HerbDetailDto> masterDetailServices,
    IHerbRepository herbRepository)
    : base(services, masterDetailServices)
```

---

## 收益统计

| 指标 | 改进前 | 改进后 | 收益 |
|------|--------|--------|------|
| 基类构造函数参数 | 2-7个 | 1个 | **统一** |
| 子类构造函数参数 | N+3到N+7 | N+1 | **-2到-6个** |
| 架构统一性 | 92% | 100% | **完全统一** |
| 特例数量 | 1个 | 0个 | **消除** |
| ContainerLocator使用 | 2处 | 0处 | **消除** |

---

## 不做的事情 (KISS)

以下优化推迟，当前不实施:

| 优化项 | 推迟原因 | 触发条件 |
|--------|----------|----------|
| INavigableViewModel接口 | 无单元测试需求 | 编写ViewModel单元测试时 |
| IDialogViewModel接口 | 同上 | 同上 |
| IMasterDetailViewModel接口 | 同上 | 同上 |
| INavigationService抽象 | Prism耦合可接受 | 替换Prism框架时 |

---

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 大范围重构 | 中 | 分Phase执行，每Phase编译验证 |
| DI配置变更 | 低 | ViewModelServices单例注册 |
| MasterDetail继承变更 | 中 | 保留组合模式，仅改继承链 |
| AccountSettings功能变化 | 低 | OnNavigatedToCore钩子保持行为一致 |

---

**设计状态**: 待确认
**核心目标**: 统一 + 简化
**预估工时**: 6-8小时
