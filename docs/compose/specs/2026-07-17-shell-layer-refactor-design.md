# Shell 层重构设计文档

## [S1] 问题

Shell 层（Desktop 客户端主框架）经过多轮迭代，部分组件职责过重：

- **NavigationCoordinator**（483 行，7 个依赖）：同时承担导航路由、历史管理、前进/后退、面包屑、Region 监控、懒加载、登录区域管理
- **MainWindowViewModel**（340 行，10+ 依赖）：协调登录状态、导航、菜单、状态栏、主题、对话框
- **MenuManager** 权限硬编码：角色判断散布在 `IsUserManagementVisible` 等属性中

## [S2] 目标

1. 将 `NavigationCoordinator` 从 483 行拆分为 ~100 行核心路由 + 3 个独立服务
2. 保持 `INavigationCoordinator` 接口不变，现有调用方零改动
3. 提取 MainWindowViewModel 中的对话框辅助方法
4. MenuManager 用 `IRoleRegistry.GetPermissions()` 替代硬编码角色判断

## [S3] 非目标

- 不改变 Prism 模块化架构
- 不改变事件协调器（ShellEventCoordinator）设计
- 不改变启动管道（StartupPipeline）设计
- 不引入新的 DI 容器或 MVVM 框架

## [S4] 设计：NavigationCoordinator 拆分

### 4.1 拆分方案

将 `NavigationCoordinator`（483 行）拆分为 4 个组件：

```
NavigationCoordinator (保留核心路由，~100行)
├── NavigationHistoryService     ← 历史/前进/后退/面包屑
├── ModuleLazyLoader             ← ViewToModuleMap + EnsureModuleLoaded
├── RegionMonitor                ← Region 集合监控 + 导航事件日志
└── INavigationCoordinator (接口不变)
```

### 4.2 NavigationHistoryService

**职责**：导航历史、前进栈、面包屑管理

**新增接口**：

```csharp
public interface INavigationHistoryService
{
    IReadOnlyList<string> NavigationHistory { get; }
    bool CanNavigateForward { get; }
    IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; }
    
    void RecordNavigation(string? fromView, string toView);
    string? PopForwardStack();
    void PushForwardStack(string viewName);
    void ClearHistory();
    void NavigateToBreadcrumb(BreadcrumbItem item);
}
```

**实现**：从 NavigationCoordinator 提取 `_navigationHistory`、`_forwardStack`、`_breadcrumbs` 及相关方法。

### 4.3 ModuleLazyLoader

**职责**：业务模块懒加载

**新增接口**：

```csharp
public interface IModuleLazyLoader
{
    void EnsureModuleLoaded(string viewName);
}
```

**实现**：从 NavigationCoordinator 提取 `ViewToModuleMap` 字典和 `EnsureModuleLoaded` 方法。

### 4.4 RegionMonitor

**职责**：Region 集合监控、导航事件日志

**新增接口**：

```csharp
public interface IRegionMonitor : IDisposable
{
    void StartMonitoring();
    void StopMonitoring();
}
```

**实现**：从 NavigationCoordinator 提取 `SubscribeToRegionCollection`、`UnsubscribeFromRegionCollection`、`OnRegionsCollectionChanged`、`SubscribeToRegionNavigationEvents`。

### 4.5 重构后的 NavigationCoordinator

**职责**：仅保留导航路由核心逻辑

```csharp
public class NavigationCoordinator : INavigationCoordinator
{
    private readonly IRegionManager _regionManager;
    private readonly ISessionManager _sessionManager;
    private readonly IRoleRegistry _roleRegistry;
    private readonly INavigationHistoryService _historyService;
    private readonly IModuleLazyLoader _moduleLazyLoader;
    private readonly ILogger<NavigationCoordinator> _logger;
    private readonly IUserNotificationService? _userNotificationService;

    public void NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
    {
        _moduleLazyLoader.EnsureModuleLoaded(viewName);
        // ... 核心路由逻辑 (~50 行)
    }

    public void NavigateBack()
    {
        var currentView = CurrentView;
        var previousView = _historyService.PopForwardStack();
        if (previousView != null)
            _historyService.PushForwardStack(currentView!);
        // ... Prism Journal.GoBack()
    }

    public void NavigateForward()
    {
        var viewName = _historyService.PopForwardStack();
        if (viewName != null)
            NavigateTo(viewName);
    }
}
```

**预计行数**：~100 行（从 483 行减少 80%）

## [S5] 设计：MainWindowViewModel 微调

### 5.1 提取 ShellDialogHelper

将对话框辅助方法提取为独立服务：

```csharp
public class ShellDialogHelper
{
    private readonly ICommonDialogService? _dialogService;
    private readonly IToastService? _toastService;
    private readonly ILogger _logger;

    public async Task<bool> ShowConfirmationAsync(string message, string title = "确认");
    public async Task ShowSuccessMessageAsync(string message);
    public async Task ShowErrorMessageAsync(string message);
    public async Task ShowWarningMessageAsync(string message);
}
```

**影响**：MainWindowViewModel 减少 ~50 行，对话框逻辑可复用。

### 5.2 MenuManager 权限解耦

**当前**（硬编码）：

```csharp
public bool IsUserManagementVisible => _sessionManager.CurrentUser?.Role is UserRole.Admin or UserRole.SuperAdmin;
```

**重构后**（通过 RoleRegistry）：

```csharp
public bool IsUserManagementVisible
{
    get
    {
        var role = _sessionManager.CurrentUser?.Role;
        if (role == null) return false;
        var definition = _roleRegistry.GetDefinition(role.Value);
        return definition?.RequiredModules.Contains("UsersModule") ?? false;
    }
}
```

**影响**：新增角色时无需修改 MenuManager。

## [S6] 设计：DI 注册优化

将 `ServiceCollectionExtensions.RegisterAllServices()` 按功能域拆分：

```csharp
public static void RegisterAllServices(this IContainerRegistry containerRegistry)
{
    var configuration = RegisterConfiguration(containerRegistry);
    containerRegistry.RegisterLogging();
    containerRegistry.RegisterCacheServices();
    containerRegistry.RegisterRepositories(configuration);
    containerRegistry.RegisterHttpServices(configuration);
    containerRegistry.AddUnifiedApiClient(configuration);
    
    // 按功能域拆分
    containerRegistry.RegisterAuthServices();
    containerRegistry.RegisterNavigationServices();
    containerRegistry.RegisterStartupServices();
    containerRegistry.RegisterInfrastructureServices();
    containerRegistry.RegisterApplicationServices();
    containerRegistry.AddViewModelServices();
}
```

## [S7] 实现顺序

| 阶段 | 任务 | 预计工时 |
|------|------|----------|
| Phase 1 | 创建 `NavigationHistoryService` | 2h |
| Phase 2 | 创建 `ModuleLazyLoader` | 1h |
| Phase 3 | 创建 `RegionMonitor` | 1h |
| Phase 4 | 重构 `NavigationCoordinator` | 2h |
| Phase 5 | 提取 `ShellDialogHelper` | 1h |
| Phase 6 | MenuManager 权限解耦 | 1h |
| Phase 7 | DI 注册拆分 | 1h |
| Phase 8 | 测试验证 | 2h |
| **合计** | | **11h** |

## [S8] 验证标准

1. `dotnet build` 编译通过
2. 现有测试全部通过
3. NavigationCoordinator 行数 ≤ 120 行
4. INavigationCoordinator 接口签名不变
5. 所有现有导航功能正常（登录→主页→各模块→后退→前进→面包屑）
6. 角色切换后菜单可见性正确
