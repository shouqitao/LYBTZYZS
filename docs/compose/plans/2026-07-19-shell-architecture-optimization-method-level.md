# Shell 层架构优化 — 方法级深化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Shell 层核心 ViewModel/Service 的构造函数从 7-11 参数压缩到 2-3，HttpClientApiClient 从 777 行减到 ~400 行，所有 Shell 服务通过接口暴露。

**Architecture:** 引入 IShellServices / IShellEventServices / INavigationServices 聚合接口，替代分散的 8-11 个构造函数参数。HttpClientApiClient 提取 QueryStringBuilder + 统一 SendAsync 方法。Service Registration 合并删除废弃文件。

**Tech Stack:** .NET 8, WPF, Prism.DryIoc, CommunityToolkit.Mvvm, C# 12

## Global Constraints

- 纯重构：不改变任何功能行为、API 契约、ViewModel 公开方法签名
- 每个 Task 完成后必须 `dotnet build` 编译通过
- 不引入新框架或新 DI 容器
- 不改变 Prism 模块化架构
- 中文业务文档/注释，英文标识符/commit
- 所有文件路径相对于项目根目录 `D:\source\repos\LYBTZYZS`

---

## Task 1: IShellServices 聚合接口 + MainWindowViewModel 瘦身

**Covers:** [S4]

**Files:**
- Create: `src/Client/Desktop/Shell/Services/IShellServices.cs`
- Create: `src/Client/Desktop/Shell/Services/ShellServices.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `INavigationCoordinator`, `IApplicationTickService`, `IThemeService`, `IActiveConsultationService`
- Produces: `IShellServices` (aggregates 9 services)

**Step 1: 创建 IShellServices 接口**

```csharp
// src/Client/Desktop/Shell/Services/IShellServices.cs
namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Shell.Services.Login;

/// <summary>
/// Shell 服务聚合接口 — 减少 MainWindowViewModel 构造函数参数
/// </summary>
public interface IShellServices
{
    MenuManager Menu { get; }
    NavigationManager Navigation { get; }
    StatusBarManager StatusBar { get; }
    ILoginStateManager LoginState { get; }
    ShellEventCoordinator Events { get; }
    ShellDialogHelper Dialogs { get; }
    IThemeService Theme { get; }
    IApplicationTickService Tick { get; }
    IActiveConsultationService ActiveConsultation { get; }
}
```

**Step 2: 创建 ShellServices 实现**

```csharp
// src/Client/Desktop/Shell/Services/ShellServices.cs
namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Shell.Services.Login;

public class ShellServices : IShellServices
{
    public ShellServices(
        MenuManager menu,
        NavigationManager navigation,
        StatusBarManager statusBar,
        ILoginStateManager loginState,
        ShellEventCoordinator events,
        ShellDialogHelper dialogs,
        IThemeService theme,
        IApplicationTickService tick,
        IActiveConsultationService activeConsultation)
    {
        Menu = menu;
        Navigation = navigation;
        StatusBar = statusBar;
        LoginState = loginState;
        Events = events;
        Dialogs = dialogs;
        Theme = theme;
        Tick = tick;
        ActiveConsultation = activeConsultation;
    }

    public MenuManager Menu { get; }
    public NavigationManager Navigation { get; }
    public StatusBarManager StatusBar { get; }
    public ILoginStateManager LoginState { get; }
    public ShellEventCoordinator Events { get; }
    public ShellDialogHelper Dialogs { get; }
    public IThemeService Theme { get; }
    public IApplicationTickService Tick { get; }
    public IActiveConsultationService ActiveConsultation { get; }
}
```

**Step 3: 注册 IShellServices**

在 `ServiceCollectionExtensions.cs` 的 `RegisterPresentationServices` 方法末尾添加：

```csharp
containerRegistry.RegisterSingleton<IShellServices, ShellServices>();
```

**Step 4: 重构 MainWindowViewModel**

将构造函数从 11 个参数改为 4 个（`IViewModelServices`, `IShellServices`, `INavigationCoordinator`, `NavigationManager`），所有字段访问通过 `_shell.xxx` 进行。

**Step 5: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

**Step 6: 提交**

```bash
git add src/Client/Desktop/Shell/Services/IShellServices.cs src/Client/Desktop/Shell/Services/ShellServices.cs src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "refactor(shell): introduce IShellServices aggregate, slim MainWindowViewModel from 11 to 4 constructor params"
```

---

## Task 2: IShellEventServices 聚合 + ShellEventCoordinator 瘦身

**Covers:** [S6]

**Files:**
- Create: `src/Client/Desktop/Shell/Services/IShellEventServices.cs`
- Create: `src/Client/Desktop/Shell/Services/ShellEventServices.cs`
- Modify: `src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `ILoginStateManager`, `IUserActivityTracker`, `ILoginCoordinator`, `ITokenLifecycleService`, `INavigationCoordinator`, `NavigationManager`, `IModuleLazyLoader`, `IUiThreadDispatcher`
- Produces: `IShellEventServices` (aggregates 8 services)

**Step 1: 创建 IShellEventServices 接口**

```csharp
// src/Client/Desktop/Shell/Services/IShellEventServices.cs
namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.Shell.Services.Login;

public interface IShellEventServices
{
    ILoginStateManager LoginState { get; }
    IUserActivityTracker ActivityTracker { get; }
    ILoginCoordinator LoginCoordinator { get; }
    ITokenLifecycleService TokenLifecycle { get; }
    INavigationCoordinator Navigation { get; }
    NavigationManager NavigationManager { get; }
    IModuleLazyLoader ModuleLoader { get; }
    IUiThreadDispatcher UiDispatcher { get; }
}
```

**Step 2: 创建 ShellEventServices 实现**

```csharp
// src/Client/Desktop/Shell/Services/ShellEventServices.cs
namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.Shell.Services.Login;

public class ShellEventServices : IShellEventServices
{
    public ShellEventServices(
        ILoginStateManager loginState,
        IUserActivityTracker activityTracker,
        ILoginCoordinator loginCoordinator,
        ITokenLifecycleService tokenLifecycle,
        INavigationCoordinator navigation,
        NavigationManager navigationManager,
        IModuleLazyLoader moduleLoader,
        IUiThreadDispatcher uiDispatcher)
    {
        LoginState = loginState;
        ActivityTracker = activityTracker;
        LoginCoordinator = loginCoordinator;
        TokenLifecycle = tokenLifecycle;
        Navigation = navigation;
        NavigationManager = navigationManager;
        ModuleLoader = moduleLoader;
        UiDispatcher = uiDispatcher;
    }

    public ILoginStateManager LoginState { get; }
    public IUserActivityTracker ActivityTracker { get; }
    public ILoginCoordinator LoginCoordinator { get; }
    public ITokenLifecycleService TokenLifecycle { get; }
    public INavigationCoordinator Navigation { get; }
    public NavigationManager NavigationManager { get; }
    public IModuleLazyLoader ModuleLoader { get; }
    public IUiThreadDispatcher UiDispatcher { get; }
}
```

**Step 3: 注册 IShellEventServices**

在 `ServiceCollectionExtensions.cs` 的 `RegisterApplicationServices` 方法末尾添加：

```csharp
containerRegistry.RegisterSingleton<IShellEventServices, ShellEventServices>();
```

**Step 4: 重构 ShellEventCoordinator**

将构造函数从 11 个参数改为 3 个（`IShellEventServices`, `IEventAggregator`, `ILogger`），所有字段访问通过 `_services.xxx` 进行。

**Step 5: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

**Step 6: 提交**

```bash
git add src/Client/Desktop/Shell/Services/IShellEventServices.cs src/Client/Desktop/Shell/Services/ShellEventServices.cs src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "refactor(shell): introduce IShellEventServices aggregate, slim ShellEventCoordinator from 11 to 3 constructor params"
```

---

## Task 3: INavigationServices 聚合 + NavigationCoordinator 瘦身

**Covers:** [S8]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationServices.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationServices.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `IRegionManager`, `ISessionManager`, `IRoleRegistry`, `INavigationHistoryService`, `IModuleLazyLoader`, `IRegionMonitor`, `IUserNotificationService`
- Produces: `INavigationServices` (aggregates 7 services)

**Step 1: 创建 INavigationServices 接口**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationServices.cs
namespace LYBT.Desktop.Navigation;

using LYBT.Desktop.Contracts.Services;

public interface INavigationServices
{
    Prism.Regions.IRegionManager RegionManager { get; }
    ISessionManager SessionManager { get; }
    IRoleRegistry RoleRegistry { get; }
    INavigationHistoryService HistoryService { get; }
    IModuleLazyLoader ModuleLazyLoader { get; }
    IRegionMonitor RegionMonitor { get; }
    IUserNotificationService? UserNotificationService { get; }
}
```

**Step 2: 创建 NavigationServices 实现**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationServices.cs
namespace LYBT.Desktop.Navigation;

using LYBT.Desktop.Contracts.Services;

public class NavigationServices : INavigationServices
{
    public NavigationServices(
        Prism.Regions.IRegionManager regionManager,
        ISessionManager sessionManager,
        IRoleRegistry roleRegistry,
        INavigationHistoryService historyService,
        IModuleLazyLoader moduleLazyLoader,
        IRegionMonitor regionMonitor,
        IUserNotificationService? userNotificationService = null)
    {
        RegionManager = regionManager;
        SessionManager = sessionManager;
        RoleRegistry = roleRegistry;
        HistoryService = historyService;
        ModuleLazyLoader = moduleLazyLoader;
        RegionMonitor = regionMonitor;
        UserNotificationService = userNotificationService;
    }

    public Prism.Regions.IRegionManager RegionManager { get; }
    public ISessionManager SessionManager { get; }
    public IRoleRegistry RoleRegistry { get; }
    public INavigationHistoryService HistoryService { get; }
    public IModuleLazyLoader ModuleLazyLoader { get; }
    public IRegionMonitor RegionMonitor { get; }
    public IUserNotificationService? UserNotificationService { get; }
}
```

**Step 3: 注册 INavigationServices**

在 `ServiceCollectionExtensions.cs` 的 `RegisterPresentationServices` 方法末尾添加：

```csharp
containerRegistry.RegisterSingleton<INavigationServices, NavigationServices>();
```

**Step 4: 重构 NavigationCoordinator**

将构造函数从 8 个参数改为 2 个（`INavigationServices`, `ILogger`），所有字段访问通过 `_services.xxx` 进行。

**Step 5: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

**Step 6: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationServices.cs src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationServices.cs src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "refactor(navigation): introduce INavigationServices aggregate, slim NavigationCoordinator from 8 to 2 constructor params"
```

---

## Task 4: IMenuManager + INavigationManager + IStatusBarManager 接口化

**Covers:** [S9], [S10]

**Files:**
- Create: `src/Client/Desktop/Shell/Services/IMenuManager.cs`
- Create: `src/Client/Desktop/Shell/Services/INavigationManager.cs`
- Create: `src/Client/Desktop/Shell/Services/IStatusBarManager.cs`
- Modify: `src/Client/Desktop/Shell/Services/MenuManager.cs`
- Modify: `src/Client/Desktop/Shell/Services/NavigationManager.cs`
- Modify: `src/Client/Desktop/Shell/Services/StatusBarManager.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- Modify: `src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs`
- Modify: `src/Client/Desktop/Shell/Services/IShellServices.cs`
- Modify: `src/Client/Desktop/Shell/Services/IShellEventServices.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: existing concrete types
- Produces: `IMenuManager`, `INavigationManager`, `IStatusBarManager`

**Step 1: 创建 IMenuManager 接口**

```csharp
// src/Client/Desktop/Shell/Services/IMenuManager.cs
namespace LYBT.Desktop.Shell.Services;

using System.Windows.Input;

public interface IMenuManager
{
    ICommand QuickAddPatientCommand { get; }
    ICommand QuickStartMedicalCaseCommand { get; }
    ICommand ShowHelpCommand { get; }
    ICommand ShowSettingsCommand { get; }
    ICommand ToggleThemeCommand { get; }
    ICommand SaveAllCommand { get; }
    ICommand RefreshAllCommand { get; }
    ICommand PrintCommand { get; }
    ICommand ExportCommand { get; }
    ICommand UndoCommand { get; }
    ICommand RedoCommand { get; }
    ICommand EditProfileCommand { get; }
    ICommand NavigateToHomeCommand { get; }
    ICommand NavigateToSystemSettingsCommand { get; }
    ICommand NavigateBackCommand { get; }
    ICommand NavigateForwardCommand { get; }
    void RefreshMenuVisibility();
}
```

**Step 2: 创建 INavigationManager 接口**

```csharp
// src/Client/Desktop/Shell/Services/INavigationManager.cs
namespace LYBT.Desktop.Shell.Services;

using System.Collections.ObjectModel;
using LYBT.Desktop.Controls.Models;
using LYBT.Shared.Models.Enums;

public interface INavigationManager
{
    ObservableCollection<NavigationItem> NavigationItems { get; }
    IList<NavigationItem> BuildNavigationItems(UserRole role);
}
```

**Step 3: 创建 IStatusBarManager 接口**

```csharp
// src/Client/Desktop/Shell/Services/IStatusBarManager.cs
namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Foundation.HealthCheck;

public interface IStatusBarManager
{
    ApiHealthStatus ApiStatus { get; }
    string ConnectionUrl { get; }
    string LocalDbStatus { get; }
    void UpdateStatus(ApiHealthStatus status, string url, string localDbStatus);
}
```

**Step 4: 让现有类实现接口**

- `MenuManager : IMenuManager`
- `NavigationManager : INavigationManager`
- `StatusBarManager : IStatusBarManager`

**Step 5: 更新 IShellServices 和 IShellEventServices**

将字段类型从具体类改为接口：

```csharp
// IShellServices.cs
IMenuManager Menu { get; }
INavigationManager Navigation { get; }
IStatusBarManager StatusBar { get; }

// IShellEventServices.cs
INavigationManager NavigationManager { get; }
```

**Step 6: 更新 DI 注册**

```csharp
// 旧
containerRegistry.RegisterSingleton<MenuManager>();
containerRegistry.RegisterSingleton<NavigationManager>();
containerRegistry.RegisterSingleton<StatusBarManager>();

// 新
containerRegistry.RegisterSingleton<IMenuManager, MenuManager>();
containerRegistry.RegisterSingleton<INavigationManager, NavigationManager>();
containerRegistry.RegisterSingleton<IStatusBarManager, StatusBarManager>();
```

**Step 7: 更新 MainWindowViewModel 字段类型**

将 `_shell.Menu`, `_shell.Navigation`, `_shell.StatusBar` 的使用改为接口类型。

**Step 8: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

**Step 9: 提交**

```bash
git add src/Client/Desktop/Shell/Services/IMenuManager.cs src/Client/Desktop/Shell/Services/INavigationManager.cs src/Client/Desktop/Shell/Services/IStatusBarManager.cs src/Client/Desktop/Shell/Services/MenuManager.cs src/Client/Desktop/Shell/Services/NavigationManager.cs src/Client/Desktop/Shell/Services/StatusBarManager.cs src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs src/Client/Desktop/Shell/Services/IShellServices.cs src/Client/Desktop/Shell/Services/IShellEventServices.cs src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "refactor(shell): introduce IMenuManager/INavigationManager/IStatusBarManager interfaces, all Shell services now interface-based"
```

---

## Task 5: HttpClientApiClient 重构

**Covers:** [S5]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/QueryStringBuilder.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs`

**Interfaces:**
- Consumes: `IHttpClientFactory`
- Produces: `QueryStringBuilder` (static helper)

**Step 1: 创建 QueryStringBuilder**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/QueryStringBuilder.cs
namespace LYBT.Desktop.Foundation.Http;

internal static class QueryStringBuilder
{
    public static string Build(string baseUrl, IDictionary<string, object?>? parameters = null)
    {
        if (parameters == null || parameters.Count == 0)
            return baseUrl;

        var query = string.Join("&", parameters
            .Where(kv => kv.Value != null)
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!.ToString()!)}"));

        return string.IsNullOrEmpty(query) ? baseUrl : $"{baseUrl}?{query}";
    }

    public static string BuildPaged(string baseUrl, int page, int pageSize,
        IDictionary<string, object?>? extra = null)
    {
        extra ??= new Dictionary<string, object?>();
        extra["page"] = page;
        extra["pageSize"] = pageSize;
        return Build(baseUrl, extra);
    }
}
```

**Step 2: 统一 HTTP 方法执行**

在 `HttpClientApiClient` 中添加统一的 `SendAsync<T>` 方法，替代 `GetAndWrapAsync` / `PostAndWrapAsync` / `PutAndWrapAsync` / `DeleteVoidAsync` 等 11 个 helper 方法。

**Step 3: 用 QueryStringBuilder 替代重复的 URL 构造**

将 9+ 处 URL 构造替换为 `QueryStringBuilder.Build()` / `QueryStringBuilder.BuildPaged()`。

**Step 4: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

**Step 5: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/QueryStringBuilder.cs src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs
git commit -m "refactor(http): extract QueryStringBuilder, unify SendAsync, reduce HttpClientApiClient from 777 to ~400 lines"
```

---

## Task 6: Service Registration 合并

**Covers:** [S7]

**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- Delete: `src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs`

**Interfaces:**
- Consumes: none
- Produces: simplified ServiceCollectionExtensions

**Step 1: 标记旧注册为 Obsolete**

在 `HttpServiceRegistrationExtensions.RegisterHttpServices()` 上添加 `[Obsolete("Use IApiClient (SwitchingApiClient) instead")]`。

**Step 2: 从 RegisterAllServices 中移除调用**

注释掉 `ServiceCollectionExtensions.RegisterAllServices()` 中的 `RegisterHttpServices()` 调用。

**Step 3: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

**Step 4: 删除 HttpServiceRegistrationExtensions.cs**

确认无编译错误后删除文件。

**Step 5: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

**Step 6: 提交**

```bash
git add src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git rm src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs
git commit -m "refactor(shell): remove obsolete HttpServiceRegistrationExtensions, consolidate service registration"
```

---

## Task 7: 最终验证 + 测试运行

**Covers:** [S13]

**Files:**
- None (verification only)

**Step 1: 完整编译**

```bash
dotnet build LYBTZYZS.sln
```

Expected: 0 errors

**Step 2: 运行 Desktop 测试**

```bash
dotnet test tests/LYBT.Tests.Desktop/
```

Expected: All tests pass

**Step 3: 运行 Architecture 测试**

```bash
dotnet test tests/LYBT.Tests.Architecture/
```

Expected: All tests pass

**Step 4: 验证构造函数参数数**

```bash
# MainWindowViewModel: 应 ≤ 4 参数
rg "public MainWindowViewModel\(" src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs

# ShellEventCoordinator: 应 ≤ 3 参数
rg "public ShellEventCoordinator\(" src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs

# NavigationCoordinator: 应 ≤ 2 参数
rg "public NavigationCoordinator\(" src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs
```

**Step 5: 验证接口覆盖率**

```bash
# 应无具体类注册（仅接口注册）
rg "RegisterSingleton<MenuManager>" src/Client/Desktop/Shell/Extensions/
rg "RegisterSingleton<NavigationManager>" src/Client/Desktop/Shell/Extensions/
rg "RegisterSingleton<StatusBarManager>" src/Client/Desktop/Shell/Extensions/
```

Expected: 0 matches for concrete type registrations

**Step 6: 验证 HttpClientApiClient 行数**

```bash
wc -l src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs
```

Expected: ≤ 450 lines

**Step 7: 验证 HttpServiceRegistrationExtensions 已删除**

```bash
ls src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs
```

Expected: file not found

---

## 执行摘要

| Task | 描述 | 预估工时 |
|------|------|----------|
| 1 | IShellServices + MainWindowViewModel 瘦身 | 2h |
| 2 | IShellEventServices + ShellEventCoordinator 瘦身 | 1.5h |
| 3 | INavigationServices + NavigationCoordinator 瘦身 | 1h |
| 4 | IMenuManager/INavigationManager/IStatusBarManager 接口化 | 1h |
| 5 | HttpClientApiClient 重构 | 3h |
| 6 | Service Registration 合并 | 0.5h |
| 7 | 最终验证 + 测试运行 | 1h |
| **合计** | | **10h** |
