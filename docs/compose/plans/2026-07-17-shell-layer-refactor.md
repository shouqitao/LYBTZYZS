# Shell Layer Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 NavigationCoordinator 从 483 行拆分为 ~100 行核心路由 + 3 个独立服务，保持 INavigationCoordinator 接口不变。

**Architecture:** 导航层拆分为 NavigationHistoryService（历史/面包屑）、ModuleLazyLoader（懒加载）、RegionMonitor（Region 监控），NavigationCoordinator 仅保留路由核心。同时提取 MainWindowViewModel 对话框辅助方法、解耦 MenuManager 权限。

**Tech Stack:** C# / .NET 8 / Prism / CommunityToolkit.Mvvm / DryIoc

## Global Constraints

- **接口不变**: `INavigationCoordinator` 公共签名不得修改，现有调用方零改动
- **DI 注册**: 新服务在 `ServiceCollectionExtensions.RegisterPresentationServices()` 中注册为 Singleton
- **命名空间**: 新服务放在 `LYBT.Desktop.Shell.Services` 或 `LYBT.Desktop.Navigation`
- **代码风格**: 中文业务注释，英文标识符，PascalCase 公共 / _camelCase 私有
- **测试**: 每个新服务需有对应的单元测试

---

## File Structure

### 新建文件

| 文件 | 职责 |
|------|------|
| `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationHistoryService.cs` | 导航历史、前进栈、面包屑管理 |
| `src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationHistoryService.cs` | 历史服务接口 |
| `src/Client/Desktop/Core/LYBT.Desktop.Navigation/ModuleLazyLoader.cs` | 业务模块懒加载 |
| `src/Client/Desktop/Core/LYBT.Desktop.Navigation/IModuleLazyLoader.cs` | 懒加载接口 |
| `src/Client/Desktop/Core/LYBT.Desktop.Navigation/RegionMonitor.cs` | Region 集合监控 |
| `src/Client/Desktop/Core/LYBT.Desktop.Navigation/IRegionMonitor.cs` | 监控接口 |
| `src/Client/Desktop/Shell/Services/ShellDialogHelper.cs` | 对话框辅助方法 |
| `tests/LYBT.Tests.Desktop/Navigation/NavigationHistoryServiceTests.cs` | 历史服务测试 |
| `tests/LYBT.Tests.Desktop/Navigation/ModuleLazyLoaderTests.cs` | 懒加载测试 |

### 修改文件

| 文件 | 修改内容 |
|------|----------|
| `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs` | 注入新服务，移除提取的逻辑 |
| `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` | 注入 ShellDialogHelper，移除对话框辅助方法 |
| `src/Client/Desktop/Shell/Services/MenuManager.cs` | 用 IRoleRegistry 替代硬编码角色判断 |
| `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs` | 注册新服务 |

---

### Task 1: 创建 INavigationHistoryService 接口和实现

**Covers:** [S4], [S4.2]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationHistoryService.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationHistoryService.cs`

**Interfaces:**
- Produces: `INavigationHistoryService` (后续 Task 2 注入 NavigationCoordinator)

- [ ] **Step 1: 创建接口**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationHistoryService.cs
using LYBT.Desktop.Controls.Models;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// 导航历史服务接口 — 管理导航历史、前进栈、面包屑
/// </summary>
public interface INavigationHistoryService
{
    /// <summary>导航历史记录</summary>
    IReadOnlyList<string> NavigationHistory { get; }

    /// <summary>是否可以前进</summary>
    bool CanNavigateForward { get; }

    /// <summary>当前面包屑列表</summary>
    IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; }

    /// <summary>记录一次导航</summary>
    void RecordNavigation(string? fromView, string toView);

    /// <summary>从前进栈弹出</summary>
    string? PopForwardStack();

    /// <summary>推入前进栈</summary>
    void PushForwardStack(string viewName);

    /// <summary>清除历史</summary>
    void ClearHistory();

    /// <summary>跳转到指定面包屑</summary>
    void NavigateToBreadcrumb(BreadcrumbItem item);
}
```

- [ ] **Step 2: 创建实现**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationHistoryService.cs
using System.Collections.ObjectModel;
using LYBT.Desktop.Controls.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// 导航历史服务实现
/// </summary>
public class NavigationHistoryService : INavigationHistoryService
{
    private const int MaxHistorySize = 20;
    private readonly ILogger<NavigationHistoryService> _logger;
    private readonly List<string> _navigationHistory = new();
    private readonly Stack<string> _forwardStack = new();
    private readonly List<BreadcrumbItem> _breadcrumbs = new();

    public IReadOnlyList<string> NavigationHistory => _navigationHistory.AsReadOnly();
    public bool CanNavigateForward => _forwardStack.Count > 0;
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs => _breadcrumbs.AsReadOnly();

    public NavigationHistoryService(ILogger<NavigationHistoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordNavigation(string? fromView, string toView)
    {
        if (_navigationHistory.Count >= MaxHistorySize)
            _navigationHistory.RemoveAt(0);
        _navigationHistory.Add(toView);
        _forwardStack.Clear();
        UpdateBreadcrumbs(fromView, toView);
        _logger.LogDebug("导航记录: {From} -> {To}", fromView, toView);
    }

    public string? PopForwardStack()
    {
        if (_forwardStack.Count == 0) return null;
        return _forwardStack.Pop();
    }

    public void PushForwardStack(string viewName)
    {
        _forwardStack.Push(viewName);
    }

    public void ClearHistory()
    {
        _navigationHistory.Clear();
        _forwardStack.Clear();
        _breadcrumbs.Clear();
        _logger.LogDebug("导航历史已清除");
    }

    public void NavigateToBreadcrumb(BreadcrumbItem item)
    {
        if (item.IsCurrent) return;
        _logger.LogInformation("面包屑导航: {Title}", item.Title);
    }

    private void UpdateBreadcrumbs(string? fromView, string toView)
    {
        var toTitle = toView?.Replace("View", "") ?? toView;

        if (fromView == null)
            _breadcrumbs.Clear();

        for (var i = 0; i < _breadcrumbs.Count; i++)
        {
            if (_breadcrumbs[i].IsCurrent)
            {
                _breadcrumbs[i] = new BreadcrumbItem(
                    _breadcrumbs[i].Title, _breadcrumbs[i].ViewName, false);
                break;
            }
        }

        _breadcrumbs.Add(new BreadcrumbItem(
            toTitle ?? toView ?? "Unknown", toView ?? "Unknown", true));
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationHistoryService.cs src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationHistoryService.cs
git commit -m "feat(navigation): add NavigationHistoryService"
```

---

### Task 2: 创建 IModuleLazyLoader 接口和实现

**Covers:** [S4], [S4.3]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/IModuleLazyLoader.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/ModuleLazyLoader.cs`

**Interfaces:**
- Produces: `IModuleLazyLoader` (后续 Task 5 注入 NavigationCoordinator)

- [ ] **Step 1: 创建接口**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/IModuleLazyLoader.cs
namespace LYBT.Desktop.Navigation;

/// <summary>
/// 业务模块懒加载接口
/// </summary>
public interface IModuleLazyLoader
{
    /// <summary>确保目标视图所属的业务模块已加载</summary>
    void EnsureModuleLoaded(string viewName);
}
```

- [ ] **Step 2: 创建实现**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/ModuleLazyLoader.cs
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// 业务模块懒加载实现
/// </summary>
public class ModuleLazyLoader : IModuleLazyLoader
{
    private readonly IModuleLoadingService? _moduleLoadingService;
    private readonly ILogger<ModuleLazyLoader> _logger;

    /// <summary>
    /// 视图名 → 业务模块名映射
    /// </summary>
    private static readonly Dictionary<string, string> ViewToModuleMap = new(StringComparer.Ordinal)
    {
        { ViewNames.PatientManagement, "PatientsModule" },
        { ViewNames.PatientSelection, "PatientsModule" },
        { ViewNames.ClinicalWorkspace, "PatientsModule" },
        { ViewNames.HerbManagement, "HerbsModule" },
        { ViewNames.FormulaManagement, "FormulaModule" },
        { ViewNames.UserManagement, "UsersModule" },
        { ViewNames.MedicalCaseManagement, "MedicalCaseModule" },
        { ViewNames.MedicalCaseWorkspace, "MedicalCaseModule" },
        { ViewNames.MedicalCaseMasterDetail, "MedicalCaseModule" },
        { ViewNames.RegistrationList, "RegistrationModule" },
        { ViewNames.ReportsHome, "ReportsModule" },
        { ViewNames.AuditLog, "MedicalCaseModule" },
        { ViewNames.SystemSettings, "AdminModule" },
        { ViewNames.LogLevelControl, "SysadminModule" },
        { ViewNames.Deployment, "SysadminModule" },
    };

    public ModuleLazyLoader(
        IModuleLoadingService? moduleLoadingService,
        ILogger<ModuleLazyLoader> logger)
    {
        _moduleLoadingService = moduleLoadingService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void EnsureModuleLoaded(string viewName)
    {
        if (_moduleLoadingService == null) return;
        if (!ViewToModuleMap.TryGetValue(viewName, out var moduleName)) return;
        if (_moduleLoadingService.IsModuleLoaded(moduleName)) return;

        try
        {
            _logger.LogDebug("懒加载业务模块: {ModuleName}（触发视图: {ViewName}）", moduleName, viewName);
            _moduleLoadingService.LoadModuleAsync(moduleName).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "懒加载模块 {ModuleName} 失败", moduleName);
        }
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/IModuleLazyLoader.cs src/Client/Desktop/Core/LYBT.Desktop.Navigation/ModuleLazyLoader.cs
git commit -m "feat(navigation): add ModuleLazyLoader"
```

---

### Task 3: 创建 IRegionMonitor 接口和实现

**Covers:** [S4], [S4.4]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/IRegionMonitor.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/RegionMonitor.cs`

**Interfaces:**
- Consumes: Prism `IRegionManager`
- Produces: `IRegionMonitor` (后续 Task 5 注入 NavigationCoordinator)

- [ ] **Step 1: 创建接口**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/IRegionMonitor.cs
namespace LYBT.Desktop.Navigation;

/// <summary>
/// Region 集合监控接口
/// </summary>
public interface IRegionMonitor : IDisposable
{
    /// <summary>开始监控</summary>
    void StartMonitoring();

    /// <summary>停止监控</summary>
    void StopMonitoring();
}
```

- [ ] **Step 2: 创建实现**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/RegionMonitor.cs
using System.Collections.Specialized;
using Prism.Regions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// Region 集合监控实现
/// </summary>
public class RegionMonitor : IRegionMonitor
{
    private readonly IRegionManager _regionManager;
    private readonly ILogger<RegionMonitor> _logger;

    public RegionMonitor(IRegionManager regionManager, ILogger<RegionMonitor> logger)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void StartMonitoring()
    {
        _regionManager.Regions.CollectionChanged += OnRegionsCollectionChanged;
        foreach (var region in _regionManager.Regions)
            SubscribeToRegionNavigationEvents(region);
        _logger.LogDebug("Region 导航监控已启用");
    }

    public void StopMonitoring()
    {
        try
        {
            _regionManager.Regions.CollectionChanged -= OnRegionsCollectionChanged;
            _logger.LogDebug("Region 导航监控已取消");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "取消 Region 监控失败");
        }
    }

    private void OnRegionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (IRegion region in e.NewItems)
                SubscribeToRegionNavigationEvents(region);
        }
    }

    private void SubscribeToRegionNavigationEvents(IRegion region)
    {
        region.NavigationService.Navigating += (s, e) =>
            _logger.LogDebug("导航中: Region={RegionName}, Target={Uri}", region.Name, e.Uri);
        region.NavigationService.Navigated += (s, e) =>
            _logger.LogDebug("导航完成: Region={RegionName}, Uri={Uri}", region.Name, e.Uri);
        region.NavigationService.NavigationFailed += (s, e) =>
            _logger.LogError(e.Error, "导航失败: {RegionName} -> {Uri}", region.Name, e.Uri);
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/IRegionMonitor.cs src/Client/Desktop/Core/LYBT.Desktop.Navigation/RegionMonitor.cs
git commit -m "feat(navigation): add RegionMonitor"
```

---

### Task 4: 注册新服务到 DI 容器

**Covers:** [S6]

**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: Task 1-3 创建的接口
- Produces: DI 注册（Singleton）

- [ ] **Step 1: 在 RegisterPresentationServices 中添加注册**

在 `ServiceCollectionExtensions.cs` 的 `RegisterPresentationServices` 方法末尾添加：

```csharp
// Navigation 服务拆分
containerRegistry.RegisterSingleton<INavigationHistoryService, NavigationHistoryService>();
containerRegistry.RegisterSingleton<IModuleLazyLoader, ModuleLazyLoader>();
containerRegistry.RegisterSingleton<IRegionMonitor, RegionMonitor>();
```

需要在文件顶部添加 using：

```csharp
using LYBT.Desktop.Navigation;
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(di): register navigation services"
```

---

### Task 5: 重构 NavigationCoordinator

**Covers:** [S4], [S4.5], [S7]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`

**Interfaces:**
- Consumes: `INavigationHistoryService`, `IModuleLazyLoader`, `IRegionMonitor`
- Produces: 精简后的 `NavigationCoordinator` (~100 行)

- [ ] **Step 1: 修改构造函数，注入新服务**

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

    public NavigationCoordinator(
        IRegionManager regionManager,
        ISessionManager sessionManager,
        IRoleRegistry roleRegistry,
        INavigationHistoryService historyService,
        IModuleLazyLoader moduleLazyLoader,
        ILogger<NavigationCoordinator> logger,
        IUserNotificationService? userNotificationService = null)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _roleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _moduleLazyLoader = moduleLazyLoader ?? throw new ArgumentNullException(nameof(moduleLazyLoader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userNotificationService = userNotificationService;
    }
```

- [ ] **Step 2: 修改 NavigateTo 方法，使用新服务**

```csharp
public void NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
{
    try
    {
        _moduleLazyLoader.EnsureModuleLoaded(viewName);
        var fromView = CurrentView;
        _logger.LogInformation("导航到 {ViewName}", viewName);
        var navParams = ConvertToNavigationParameters(parameters);

        _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
        {
            if (result.Result == true)
            {
                _historyService.RecordNavigation(fromView, viewName);
                NavigationChanged?.Invoke(this, new NavigationChangedEventArgs(fromView, viewName, parameters));
            }
            else
            {
                var errorMessage = result.Error?.Message ?? "未知错误";
                _logger.LogError("导航失败：{ViewName}，错误：{Error}", viewName, errorMessage);
                _userNotificationService?.ShowErrorAsync($"无法打开页面：{errorMessage}");
            }
        }, navParams);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导航到 {ViewName} 时发生异常", viewName);
        _userNotificationService?.ShowErrorAsync($"导航失败：{ex.Message}");
    }
}
```

- [ ] **Step 3: 修改 NavigateBack/NavigateForward，使用新服务**

```csharp
public void NavigateBack()
{
    try
    {
        var region = _regionManager.Regions[RegionNames.ContentRegion];
        if (region?.NavigationService?.Journal?.CanGoBack == true)
        {
            var currentView = CurrentView;
            if (currentView != null)
                _historyService.PushForwardStack(currentView);
            region.NavigationService.Journal.GoBack();
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导航回退失败");
        _userNotificationService?.ShowErrorAsync($"导航回退失败：{ex.Message}");
    }
}

public bool CanNavigateForward => _historyService.CanNavigateForward;

public void NavigateForward()
{
    var viewName = _historyService.PopForwardStack();
    if (viewName != null)
        NavigateTo(viewName);
}
```

- [ ] **Step 4: 修改属性和方法，委托给新服务**

```csharp
public IReadOnlyList<string> NavigationHistory => _historyService.NavigationHistory;
public IReadOnlyList<BreadcrumbItem> Breadcrumbs => _historyService.Breadcrumbs;

public void ClearHistory() => _historyService.ClearHistory();

public void NavigateToBreadcrumb(BreadcrumbItem item)
{
    if (item.IsCurrent) return;
    NavigateTo(item.ViewName);
}
```

- [ ] **Step 5: 删除已提取的字段和方法**

删除以下字段：
- `_navigationHistory`
- `_forwardStack`
- `_breadcrumbs`
- `MaxHistorySize` 常量

删除以下方法：
- `UpdateBreadcrumbs`
- `SubscribeToRegionCollection`
- `UnsubscribeFromRegionCollection`
- `OnRegionsCollectionChanged`
- `SubscribeToRegionNavigationEvents`

删除 `ViewToModuleMap` 字典和 `EnsureModuleLoaded` 方法。

- [ ] **Step 6: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 7: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs
git commit -m "refactor(navigation): extract history/lazyload/monitor from NavigationCoordinator"
```

---

### Task 6: 提取 ShellDialogHelper

**Covers:** [S5], [S5.1]

**Files:**
- Create: `src/Client/Desktop/Shell/Services/ShellDialogHelper.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Produces: `ShellDialogHelper` (注入 MainWindowViewModel)

- [ ] **Step 1: 创建 ShellDialogHelper**

```csharp
// src/Client/Desktop/Shell/Services/ShellDialogHelper.cs
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 对话框辅助方法 — 从 MainWindowViewModel 提取
/// </summary>
public class ShellDialogHelper
{
    private readonly ICommonDialogService? _dialogService;
    private readonly IToastService? _toastService;
    private readonly ILogger _logger;

    public ShellDialogHelper(
        ICommonDialogService? dialogService,
        IToastService? toastService,
        ILogger<ShellDialogHelper> logger)
    {
        _dialogService = dialogService;
        _toastService = toastService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ShowSuccessMessageAsync(string message)
    {
        if (_toastService != null)
            await Task.Run(() => _toastService.ShowSuccess(message));
        else
            _logger.LogWarning("ToastService 不可用: {Message}", message);
    }

    public async Task ShowErrorMessageAsync(string message)
    {
        if (_toastService != null)
            await Task.Run(() => _toastService.ShowError(message));
        else
            _logger.LogError("ToastService 不可用: {Message}", message);
    }

    public async Task ShowWarningMessageAsync(string message)
    {
        if (_dialogService != null)
            await _dialogService.ShowWarningAsync(message, "警告");
        else
            _logger.LogWarning("CommonDialogService 不可用: {Message}", message);
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        if (_dialogService != null)
            return await _dialogService.ShowConfirmAsync(message, title);
        _logger.LogWarning("CommonDialogService 不可用: {Message}", message);
        return false;
    }
}
```

- [ ] **Step 2: 修改 MainWindowViewModel，注入 ShellDialogHelper**

在构造函数中添加 `ShellDialogHelper` 参数，并替换对话框辅助方法：

```csharp
private readonly ShellDialogHelper _dialogHelper;

// 构造函数中
_dialogHelper = dialogHelper ?? throw new ArgumentNullException(nameof(dialogHelper));

// 替换方法
protected virtual async Task ShowSuccessMessageAsync(string message) =>
    await _dialogHelper.ShowSuccessMessageAsync(message);

protected virtual async Task ShowErrorMessageAsync(string message) =>
    await _dialogHelper.ShowErrorMessageAsync(message);

protected virtual async Task ShowWarningMessageAsync(string message) =>
    await _dialogHelper.ShowWarningMessageAsync(message);

protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认") =>
    await _dialogHelper.ShowConfirmationAsync(message, title);
```

- [ ] **Step 3: 在 ServiceCollectionExtensions 中注册**

在 `RegisterPresentationServices` 中添加：

```csharp
containerRegistry.RegisterSingleton<ShellDialogHelper>();
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Shell/Services/ShellDialogHelper.cs src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "refactor(shell): extract ShellDialogHelper from MainWindowViewModel"
```

---

### Task 7: MenuManager 权限解耦

**Covers:** [S5], [S5.2]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/MenuManager.cs`

**Interfaces:**
- Consumes: `IRoleRegistry`（已有）
- Produces: 使用 RoleRegistry 的权限判断

- [ ] **Step 1: 修改 IsUserManagementVisible**

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

- [ ] **Step 2: 修改 IsSystemSettingsVisible**

```csharp
public bool IsSystemSettingsVisible
{
    get
    {
        var role = _sessionManager.CurrentUser?.Role;
        if (role == null) return false;
        return role.Value is UserRole.SuperAdmin;
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/Services/MenuManager.cs
git commit -m "refactor(menu): decouple role checks via IRoleRegistry"
```

---

### Task 8: 测试验证

**Covers:** [S8]

**Files:**
- Modify: 现有测试项目

- [ ] **Step 1: 运行现有测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: 所有测试通过

- [ ] **Step 2: 手动验证导航功能**

验证清单：
1. 登录 → 主页显示正确
2. 侧边栏导航 → 各模块视图切换正常
3. 后退按钮 → 返回上一页
4. 前进按钮 → 前进到下一页
5. 面包屑 → 点击跳转正确
6. 角色切换 → 菜单可见性正确

- [ ] **Step 3: 最终 Commit**

```bash
git add -A
git commit -m "test(navigation): verify all navigation features work"
```

---

## Execution Handoff<tool_call>
<function=question>
<parameter=questions>[{"question": "实现计划已保存。如何执行？", "header": "Execution", "options": [{"label": "Inline, this time", "description": "在当前会话中逐步执行"}, {"label": "Subagent, this time", "description": "每个 Task 启动独立 subagent"}, {"label": "Inline, always", "description": "记住：以后都在当前会话执行"}, {"label": "Subagent, always", "description": "记住：以后都用 subagent"}]}]