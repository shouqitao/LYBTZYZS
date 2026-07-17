# Shell 层全面优化设计文档（基于 WPF Prism 最佳实践）

## [S1] 问题

Shell 层经过多轮迭代，整体架构已较好（导航已拆分、事件协调已独立、启动管道已模块化），但与 Prism 官方最佳实践对比仍有优化空间：

1. **导航参数类型不安全**：使用 `Dictionary<string, object>` 传递参数，存在拼写错误风险
2. **模块加载策略可优化**：高频模块未预加载，首次导航有延迟
3. **DI 依赖链需审计**：Singleton 依赖 Transient 的潜在问题
4. **EventAggregator 使用不统一**：Shell 内部混合使用 C# event 和 Prism EventAggregator
5. **启动流程可增强**：缺少导航防抖和 Region 超时机制
6. **MVVM 规范可加强**：部分 VM 未统一继承基类

## [S2] 目标

1. 引入强类型导航参数，消除字符串拼写风险
2. 优化模块加载策略，添加背景预加载
3. 审计 DI Singleton 依赖链，确保无过期实例问题
4. 统一事件模型（保持当前 C# event 模式，无需改为 EventAggregator）
5. 添加导航防抖和 Region 超时机制
6. 完善启动耗时统计
7. 生成 Shell 层最佳实践文档

## [S3] 非目标

- 不改变 Prism 模块化架构
- 不引入新的 DI 容器或 MVVM 框架
- 不改变 UI 框架（MDIX）
- 不重构已有模块（Patients、Herbs、MedicalCase 等）

## [S4] 设计：强类型导航参数

### 4.1 问题

当前导航参数使用 `Dictionary<string, object>`：

```csharp
_navigationCoordinator.NavigateTo(ViewNames.UserManagement,
    new Dictionary<string, object> { { "DefaultRoleFilter", UserRole.Admin } });
```

风险：键名拼写错误在编译期无法发现。

### 4.2 解决方案

引入强类型导航参数类：

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationParameters/
public record UserManagementNavParams(UserRole? DefaultRoleFilter = null);
public record MedicalCaseNavParams(Guid PatientId, Guid? MedicalCaseId = null);
public record RegistrationNavParams(Guid? DoctorId = null);
```

`INavigationCoordinator` 新增泛型重载：

```csharp
public interface INavigationCoordinator
{
    void NavigateTo(string viewName, IDictionary<string, object>? parameters = null);
    void NavigateTo<TParams>(string viewName, TParams parameters) where TParams : class;
}
```

### 4.3 影响范围

- `NavigationManager.BuildNavigationItems`：3 处 `Dictionary` 参数
- `ClinicalWorkspaceViewModel`：多处导航调用
- `PatientSelectionViewModel`：导航到医案

### 4.4 兼容性

旧的 `NavigateTo(string, IDictionary)` 保留，新代码使用泛型重载。

## [S5] 设计：模块背景预加载

### 5.1 问题

高频业务模块（Patients、Herbs、MedicalCase）设为 `OnDemand`，首次导航时才加载，有明显延迟。

### 5.2 解决方案

在 `ShellEventCoordinator.OnLoginSucceeded` 中添加背景预加载：

```csharp
private void OnLoginSucceeded(object? sender, LoginSuccessEventArgs args)
{
    _uiDispatcher.InvokeAsync(() =>
    {
        _loginStateManager.ApplyLoginSuccess(args.User);
        _navigationCoordinator.ClearLoginRegion();
        _userActivityTracker.StartTracking();
        _ = _tokenLifecycleService.StartMonitoringFromStorageAsync();
        _menuManager.RefreshMenuVisibility();
        _navigationManager.NavigationItems = _navigationManager.BuildNavigationItems(args.User.Role);
        LoginSuccessHandled?.Invoke(this, EventArgs.Empty);
    });

    // 背景预加载高频模块
    _ = Task.Run(async () =>
    {
        await Task.Delay(2000);
        _moduleLazyLoader.PreloadModules(args.User.Role);
    });
}
```

`IModuleLazyLoader` 新增方法：

```csharp
public interface IModuleLazyLoader
{
    void EnsureModuleLoaded(string viewName);
    void PreloadModules(UserRole role); // 新增
}
```

### 5.3 预加载模块列表

根据角色决定预加载：

| 角色 | 预加载模块 |
|------|-----------|
| Doctor | PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule |
| Receptionist | PatientsModule, RegistrationModule |
| Admin | UsersModule, ReportsModule |
| SuperAdmin | UsersModule, ReportsModule, SysadminModule |

## [S6] 设计：DI Singleton 依赖链审计

### 6.1 审计规则

Prism/DryIoc 最佳实践：
- **Singleton 依赖 Singleton**：安全
- **Singleton 依赖 Transient**：危险（捕获过期实例）
- **Transient 依赖 Singleton**：安全
- **Transient 依赖 Transient**：安全（每次新建）

### 6.2 审计范围

`ServiceCollectionExtensions` 中所有 `RegisterSingleton` 调用：

```csharp
// 需要审计的 Singleton 注册
RegisterSingleton<StatusBarManager>()          // 依赖 IApiHealthMonitor (Singleton ✓)
RegisterSingleton<NavigationManager>()         // 依赖 INavigationCoordinator (Singleton ✓)
RegisterSingleton<MenuManager>()               // 依赖 INavigationCoordinator (Singleton ✓)
RegisterSingleton<ShellEventCoordinator>()     // 依赖多个 Singleton ✓
RegisterSingleton<LoginStateManager>()         // 依赖多个 Singleton ✓
RegisterSingleton<IRoleRegistry>()             // 无外部依赖 ✓
RegisterSingleton<IApiHealthMonitor>()         // 依赖 IApiHealthCheckService (Singleton ✓)
```

### 6.3 验证方法

使用 `serena_get_diagnostics_for_file` 检查编译警告，手动审计依赖链。

## [S7] 设计：导航防抖

### 7.1 问题

用户快速连续点击导航项时，可能触发多次导航请求，导致 UI 闪烁或 Region 状态异常。

### 7.2 解决方案

在 `NavigationCoordinator.NavigateTo` 中添加防抖：

```csharp
private DateTime _lastNavigationTime = DateTime.MinValue;
private const int NavigationDebounceMs = 300;

public void NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
{
    // 防抖：300ms 内不重复导航到同一视图
    if (viewName == CurrentView && 
        (DateTime.UtcNow - _lastNavigationTime).TotalMilliseconds < NavigationDebounceMs)
    {
        _logger.LogDebug("导航防抖：忽略重复请求 {ViewName}", viewName);
        return;
    }
    
    _lastNavigationTime = DateTime.UtcNow;
    // ... 原有导航逻辑
}
```

## [S8] 设计：Region 导航超时

### 8.1 问题

`RegionManager.RequestNavigate` 是异步操作，如果目标视图的 ViewModel 构造函数阻塞，导航会挂起。

### 8.2 解决方案

添加导航超时机制：

```csharp
public void NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
{
    _moduleLazyLoader.EnsureModuleLoaded(viewName);
    var fromView = CurrentView;
    var navParams = ConvertToNavigationParameters(parameters);
    var tcs = new TaskCompletionSource<bool>();

    var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    timeoutCts.Token.Register(() => tcs.TrySetResult(false));

    _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
    {
        tcs.TrySetResult(result.Result == true);
        // ... 原有回调逻辑
    }, navParams);

    _ = Task.Run(async () =>
    {
        if (!await tcs.Task)
        {
            _logger.LogWarning("导航超时或失败: {ViewName}", viewName);
            _userNotificationService?.ShowWarningAsync($"页面加载超时：{viewName}");
        }
    });
}
```

## [S9] 设计：启动耗时统计完善

### 9.1 当前状态

`AppStartupOrchestrator` 已有 `IStartupPipeline` 记录各步骤耗时。

### 9.2 增强

在 `AppStartupOrchestrator.RunStartupAsync` 中添加总耗时统计：

```csharp
public async Task RunStartupAsync()
{
    var totalStopwatch = Stopwatch.StartNew();
    // ... 原有逻辑
    totalStopwatch.Stop();
    _logger.LogInformation("启动管道总耗时: {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);
}
```

## [S10] 设计：Shell 层最佳实践文档

优化完成后，在 `docs/05-development/standards/` 下创建 `SHELL-BEST-PRACTICES.md`，内容包括：

### 10.1 导航规范
- 使用 `ViewNames` 常量
- 使用强类型导航参数
- 导航失败必须处理异常

### 10.2 模块加载规范
- 核心模块：`WhenAvailable`
- 业务模块：`OnDemand` + 背景预加载
- 使用 `[ModuleDependency]` 声明依赖

### 10.3 DI 注册规范
- Singleton 用于无状态服务
- Transient 用于有状态服务
- 避免 Singleton → Transient 依赖

### 10.4 事件通信规范
- Shell 内部：C# event
- 跨模块：Prism EventAggregator
- 事件定义在共享程序集中

### 10.5 MVVM 规范
- 所有 VM 继承 `NavigableViewModelBase`
- 使用 `CommunityToolkit.Mvvm` 属性/命令
- ViewModel 映射在模块 `RegisterTypes` 中注册

## [S11] 实现顺序

| 阶段 | 任务 | 预计工时 |
|------|------|----------|
| Phase 1 | 强类型导航参数 | 2h |
| Phase 2 | Singleton 依赖链审计 | 1h |
| Phase 3 | 模块背景预加载 | 2h |
| Phase 4 | 导航防抖 | 1h |
| Phase 5 | Region 导航超时 | 2h |
| Phase 6 | 启动耗时统计完善 | 1h |
| Phase 7 | Shell 最佳实践文档 | 2h |
| Phase 8 | 测试验证 | 2h |
| **合计** | | **13h** |

## [S12] 验证标准

1. `dotnet build` 编译通过
2. 现有测试全部通过
3. 强类型导航参数编译期类型安全
4. 模块预加载减少首次导航延迟 > 50%
5. 导航防抖生效（300ms 内重复点击不触发多次导航）
6. Region 超时机制生效（10s 超时提示用户）
7. Shell 最佳实践文档完整
