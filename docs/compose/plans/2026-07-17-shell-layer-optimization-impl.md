# Shell 层全面优化实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 基于 WPF Prism 最佳实践优化 Shell 层，提升类型安全、启动性能和代码质量

**Architecture:** 强类型导航参数 + 模块预加载 + 导航防抖 + Region 超时 + 最佳实践文档

**Tech Stack:** .NET 8, WPF, Prism.DryIoc, CommunityToolkit.Mvvm, MaterialDesignThemes

## Global Constraints

- 所有修改必须通过 `dotnet build` 编译
- 保持 `INavigationCoordinator` 接口向后兼容（旧方法保留）
- 使用 `CommunityToolkit.Mvvm` 而非 Prism BindableBase
- 中文业务文档/注释，英文标识符/commit
- commit 格式：`feat/fix/docs/refactor/test(模块): 描述`

---

### Task 1: 创建强类型导航参数类

**Covers:** [S4]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationParameters/NavigationParams.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/INavigationCoordinator.cs`

**Interfaces:**
- Produces: `UserManagementNavParams`, `MedicalCaseNavParams`, `RegistrationNavParams` records

- [ ] **Step 1: 创建导航参数记录类**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationParameters/NavigationParams.cs
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Navigation.NavigationParameters;

/// <summary>用户管理导航参数</summary>
public record UserManagementNavParams(UserRole? DefaultRoleFilter = null);

/// <summary>医案导航参数</summary>
public record MedicalCaseNavParams(Guid PatientId, Guid? MedicalCaseId = null);

/// <summary>挂号导航参数</summary>
public record RegistrationNavParams(Guid? DoctorId = null);
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationParameters/
git commit -m "feat(Navigation): add strongly-typed navigation parameter records"
```

---

### Task 2: INavigationCoordinator 新增泛型重载

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/INavigationCoordinator.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`

**Interfaces:**
- Consumes: `NavigationParams` records from Task 1
- Produces: `NavigateTo<TParams>` method on `INavigationCoordinator`

- [ ] **Step 1: 在 INavigationCoordinator 接口添加泛型方法**

在 `INavigationCoordinator.cs` 的 `#region 基础导航` 中添加：

```csharp
/// <summary>
/// 导航到指定视图（强类型参数）
/// </summary>
void NavigateTo<TParams>(string viewName, TParams parameters) where TParams : class;
```

- [ ] **Step 2: 在 NavigationCoordinator 实现泛型方法**

在 `NavigationCoordinator.cs` 的 `#region 导航路由核心` 中添加：

```csharp
/// <summary>导航到指定视图（强类型参数）</summary>
public void NavigateTo<TParams>(string viewName, TParams parameters) where TParams : class
{
    ArgumentNullException.ThrowIfNull(parameters);
    var dict = new Dictionary<string, object>();
    foreach (var prop in typeof(TParams).GetProperties())
    {
        var value = prop.GetValue(parameters);
        if (value != null)
            dict[prop.Name] = value;
    }
    NavigateTo(viewName, dict);
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/INavigationCoordinator.cs
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs
git commit -m "feat(Navigation): add generic NavigateTo<TParams> overload"
```

---

### Task 3: NavigationManager 使用强类型参数

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/NavigationManager.cs`

**Interfaces:**
- Consumes: `UserManagementNavParams` from Task 1

- [ ] **Step 1: 修改 BuildNavigationItems 中的用户管理导航**

将 `NavigationManager.cs` 第 97-114 行的用户管理导航改为：

```csharp
if (modules.Contains("UsersModule") && role is UserRole.Admin or UserRole.SuperAdmin)
{
    if (role == UserRole.SuperAdmin)
    {
        items.Add(new NavigationItem
        {
            Title = "用户管理",
            ViewName = ViewNames.UserManagement,
            IconKind = "AccountTie",
            Command = new RelayCommand(() => _navigationCoordinator.NavigateTo(
                ViewNames.UserManagement,
                new UserManagementNavParams(DefaultRoleFilter: UserRole.Admin))),
            Group = "管理"
        });
    }
    else
    {
        items.Add(CreateNavItem("用户管理", ViewNames.UserManagement, "AccountTie", "管理"));
    }
}
```

- [ ] **Step 2: 添加 using 语句**

在文件顶部添加：

```csharp
using LYBT.Desktop.Navigation.NavigationParameters;
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/Services/NavigationManager.cs
git commit -m "refactor(Shell): use strongly-typed UserManagementNavParams"
```

---

### Task 4: IModuleLazyLoader 添加预加载方法

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IModuleLazyLoader.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/ModuleLazyLoader.cs`

**Interfaces:**
- Produces: `PreloadModules(UserRole role)` method

- [ ] **Step 1: 在 IModuleLazyLoader 接口添加 PreloadModules**

```csharp
/// <summary>
/// 预加载指定角色的高频模块
/// </summary>
void PreloadModules(UserRole role);
```

- [ ] **Step 2: 在 ModuleLazyLoader 实现预加载**

```csharp
public void PreloadModules(UserRole role)
{
    var modulesToPreload = role switch
    {
        UserRole.Doctor => new[] { "PatientsModule", "HerbsModule", "FormulaModule", "MedicalCaseModule" },
        UserRole.Receptionist => new[] { "PatientsModule", "RegistrationModule" },
        UserRole.Admin => new[] { "UsersModule", "ReportsModule" },
        UserRole.SuperAdmin => new[] { "UsersModule", "ReportsModule", "SysadminModule" },
        _ => Array.Empty<string>()
    };

    foreach (var moduleName in modulesToPreload)
    {
        try
        {
            if (_moduleManagercatalog.Modules.All(m => m.ModuleName != moduleName))
                continue;
            _moduleManager.LoadModule(moduleName);
            _logger.LogDebug("预加载模块: {ModuleName}", moduleName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预加载模块失败: {ModuleName}", moduleName);
        }
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IModuleLazyLoader.cs
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/ModuleLazyLoader.cs
git commit -m "feat(Navigation): add module preloading by role"
```

---

### Task 5: ShellEventCoordinator 触发预加载

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs`

**Interfaces:**
- Consumes: `IModuleLazyLoader.PreloadModules` from Task 4

- [ ] **Step 1: 在构造函数注入 IModuleLazyLoader**

```csharp
private readonly IModuleLazyLoader _moduleLazyLoader;

public ShellEventCoordinator(
    ILoginStateManager loginStateManager,
    IEventAggregator eventAggregator,
    IUserActivityTracker userActivityTracker,
    ILoginCoordinator loginCoordinator,
    ITokenLifecycleService tokenLifecycleService,
    INavigationCoordinator navigationCoordinator,
    MenuManager menuManager,
    NavigationManager navigationManager,
    IModuleLazyLoader moduleLazyLoader, // 新增
    IUiThreadDispatcher uiDispatcher,
    ILogger<ShellEventCoordinator> logger)
{
    // ... 原有赋值
    _moduleLazyLoader = moduleLazyLoader ?? throw new ArgumentNullException(nameof(moduleLazyLoader));
    // ... 原有逻辑
}
```

- [ ] **Step 2: 在 OnLoginSucceeded 中添加预加载**

在 `_menuManager.RefreshMenuVisibility()` 之后添加：

```csharp
// 背景预加载高频模块
_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    _moduleLazyLoader.PreloadModules(args.User.Role);
});
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs
git commit -m "feat(Shell): trigger module preloading after login"
```

---

### Task 6: NavigationCoordinator 添加导航防抖

**Covers:** [S7]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`

**Interfaces:**
- Consumes: 无新依赖

- [ ] **Step 1: 添加防抖字段和常量**

在 `NavigationCoordinator` 类中添加：

```csharp
private DateTime _lastNavigationTime = DateTime.MinValue;
private string? _lastNavigationView;
private const int NavigationDebounceMs = 300;
```

- [ ] **Step 2: 在 NavigateTo 方法开头添加防抖逻辑**

```csharp
public void NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
{
    try
    {
        // 防抖：300ms 内不重复导航到同一视图
        if (viewName == _lastNavigationView && 
            viewName == CurrentView &&
            (DateTime.UtcNow - _lastNavigationTime).TotalMilliseconds < NavigationDebounceMs)
        {
            _logger.LogDebug("导航防抖：忽略重复请求 {ViewName}", viewName);
            return;
        }
        
        _lastNavigationTime = DateTime.UtcNow;
        _lastNavigationView = viewName;
        
        _moduleLazyLoader.EnsureModuleLoaded(viewName);
        // ... 原有逻辑
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs
git commit -m "feat(Navigation): add navigation debounce (300ms)"
```

---

### Task 7: NavigationCoordinator 添加 Region 导航超时

**Covers:** [S8]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`

**Interfaces:**
- Consumes: `IUserNotificationService` (已注入)

- [ ] **Step 1: 添加超时常量**

```csharp
private const int NavigationTimeoutSeconds = 10;
```

- [ ] **Step 2: 修改 NavigateTo 中的 RequestNavigate 调用**

将现有的 `_regionManager.RequestNavigate` 回调改为带超时的版本：

```csharp
var tcs = new TaskCompletionSource<bool>();
var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(NavigationTimeoutSeconds));
timeoutCts.Token.Register(() => tcs.TrySetResult(false));

_regionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
{
    tcs.TrySetResult(result.Result == true);
    
    if (result.Result == true)
    {
        _historyService.RecordNavigation(fromView, viewName);
        NavigationChanged?.Invoke(this, new NavigationChangedEventArgs(fromView, viewName, parameters));
        _logger.LogDebug("导航成功: {FromView} -> {ToView}", fromView, viewName);
    }
    else
    {
        var errorMessage = result.Error?.Message ?? "未知错误";
        _logger.LogError("导航失败：{ViewName}，错误：{Error}", viewName, errorMessage);
        _userNotificationService?.ShowErrorAsync($"无法打开页面：{errorMessage}");
    }
}, navParams);

_ = Task.Run(async () =>
{
    if (!await tcs.Task)
    {
        _logger.LogWarning("导航超时: {ViewName} ({TimeoutSeconds}s)", viewName, NavigationTimeoutSeconds);
        _userNotificationService?.ShowWarningAsync($"页面加载超时：{viewName}");
    }
});
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs
git commit -m "feat(Navigation): add region navigation timeout (10s)"
```

---

### Task 8: AppStartupOrchestrator 完善启动耗时统计

**Covers:** [S9]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/AppStartupOrchestrator.cs`

**Interfaces:**
- Consumes: 无新依赖

- [ ] **Step 1: 添加 Stopwatch 包引用**

在文件顶部添加：

```csharp
using System.Diagnostics;
```

- [ ] **Step 2: 在 RunStartupAsync 中添加总耗时统计**

```csharp
public async Task RunStartupAsync()
{
    var totalStopwatch = Stopwatch.StartNew();
    try
    {
        _logger.LogInformation("启动管道开始执行");
        // ... 原有逻辑
    }
    catch (Exception ex)
    {
        // ... 原有异常处理
    }
    finally
    {
        totalStopwatch.Stop();
        _logger.LogInformation("启动管道总耗时: {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/Services/AppStartupOrchestrator.cs
git commit -m "feat(Shell): add startup pipeline total timing"
```

---

### Task 9: DI Singleton 依赖链审计

**Covers:** [S6]

**Files:**
- Audit: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- 无代码修改，仅审计

- [ ] **Step 1: 列出所有 Singleton 注册**

在 `ServiceCollectionExtensions.cs` 中搜索 `RegisterSingleton`，列出所有 Singleton 及其依赖。

- [ ] **Step 2: 审计依赖链**

检查每个 Singleton 的构造函数依赖：
- 如果依赖也是 Singleton → 安全
- 如果依赖是 Transient → 危险，需记录

- [ ] **Step 3: 记录审计结果**

在 `docs/compose/reports/2026-07-17-di-singleton-audit.md` 中记录审计结果。

- [ ] **Step 4: Commit**

```bash
git add docs/compose/reports/2026-07-17-di-singleton-audit.md
git commit -m "docs(Shell): add DI singleton dependency chain audit"
```

---

### Task 10: Shell 最佳实践文档

**Covers:** [S10]

**Files:**
- Create: `docs/05-development/standards/SHELL-BEST-PRACTICES.md`

**Interfaces:**
- 无代码依赖

- [ ] **Step 1: 创建文档**

```markdown
# Shell 层最佳实践

## 1. 导航规范

### 1.1 使用 ViewNames 常量
所有导航必须使用 `ViewNames` 常量，禁止硬编码字符串。

### 1.2 使用强类型导航参数
优先使用 `NavigateTo<TParams>` 泛型重载，而非 `Dictionary<string, object>`。

### 1.3 导航失败处理
`NavigationCoordinator.NavigateTo` 内部已处理异常，调用方无需额外 try-catch。

## 2. 模块加载规范

### 2.1 模块分类
- **核心模块**：`WhenAvailable`（AuthenticationModule, ClinicalModule, AdminModule, ReceptionistModule, SysadminModule）
- **业务模块**：`OnDemand`（PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule, RegistrationModule）

### 2.2 模块依赖
使用 `[ModuleDependency]` 声明模块依赖，Prism 自动处理加载顺序。

### 2.3 模块预加载
登录成功后自动预加载高频模块（由 `ShellEventCoordinator` 触发）。

## 3. DI 注册规范

### 3.1 生命周期选择
- **Singleton**：无状态服务（导航、事件协调、角色注册表）
- **Transient**：有状态服务（ViewModel、Repository）
- **Scoped**：按会话隔离的服务

### 3.2 避免 Singleton → Transient
Singleton 依赖 Transient 会导致捕获过期实例。审计依赖链。

## 4. 事件通信规范

### 4.1 Shell 内部通信
使用 C# event（如 `LoginStateChanged`、`LogoutRequested`）。

### 4.2 跨模块通信
使用 Prism `EventAggregator`（如 `AuthEvents.PasswordChangedEvent`）。

### 4.3 事件定义位置
事件定义在 `LYBT.Desktop.Infrastructure.Events` 共享程序集中。

## 5. MVVM 规范

### 5.1 ViewModel 基类
所有 VM 继承 `NavigableViewModelBase`（已实现 `IEditable`、`INavigationAware`）。

### 5.2 属性和命令
使用 `CommunityToolkit.Mvvm`：
- `[ObservableProperty]` 替代手动 `OnPropertyChanged`
- `[RelayCommand]` 替代 `DelegateCommand`

### 5.3 ViewModel 映射
在模块的 `RegisterTypes` 中使用 `ViewModelLocationProvider.Register` 注册映射。

## 6. 代码风格

### 6.1 命名
- 公共成员：PascalCase
- 私有字段：_camelCase
- 接口：I 前缀

### 6.2 注释
- 中文业务文档/注释
- 英文标识符/commit
- 无 Emoji（除非要求）
```

- [ ] **Step 2: Commit**

```bash
git add docs/05-development/standards/SHELL-BEST-PRACTICES.md
git commit -m "docs: add Shell layer best practices guide"
```

---

### Task 11: 最终编译验证和测试

**Covers:** [S12]

**Files:**
- 无新文件

**Interfaces:**
- 无新依赖

- [ ] **Step 1: 完整编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 2: 运行架构测试**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests passed

- [ ] **Step 3: 运行 Desktop 测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: All tests passed

- [ ] **Step 4: 验证强类型导航参数**

检查 `NavigationManager.BuildNavigationItems` 中的 `UserManagementNavParams` 使用。

- [ ] **Step 5: 验证模块预加载**

检查 `ShellEventCoordinator.OnLoginSucceeded` 中的 `PreloadModules` 调用。

- [ ] **Step 6: 验证导航防抖**

检查 `NavigationCoordinator.NavigateTo` 中的防抖逻辑。

- [ ] **Step 7: 验证 Region 超时**

检查 `NavigationCoordinator.NavigateTo` 中的超时机制。

- [ ] **Step 8: 验证启动耗时统计**

检查 `AppStartupOrchestrator.RunStartupAsync` 中的 `Stopwatch`。

- [ ] **Step 9: Commit 最终状态**

```bash
git add -A
git commit -m "chore: Shell layer optimization complete - all verifications passed"
```
