# Shell 层架构优化设计 — 方法级深化

## [S1] 问题

Shell 层经过多轮迭代，核心架构已较好（导航已拆分、事件协调已独立、启动管道已模块化、MasterDetail 已用组合模式），但与 WPF Prism + CommunityToolkit.Mvvm 最佳实践对比仍有以下方法级优化空间：

### 1.1 构造函数膨胀

| 类 | 当前参数数 | 问题 |
|---|---|---|
| `MainWindowViewModel` | **11** | 违反 SRP 嫌疑，难以测试 |
| `ShellEventCoordinator` | **10** | 同上 |
| `NavigationCoordinator` | **8** | 同上 |
| `MenuManager` | **7** | 同上 |

### 1.2 服务抽象不完整

- `MenuManager`、`NavigationManager`、`StatusBarManager`、`ShellDialogHelper` 注册为**具体类型**，非接口
- `MainWindowViewModel` 直接依赖具体类，无法 Mock 测试
- `ShellEventCoordinator` 直接依赖具体类 `MenuManager`、`NavigationManager`

### 1.3 HttpClientApiClient 体积过大

- 777 行，实现 8 个子接口的 96 个方法
- URL 构造重复 9+ 次（相同 boilerplate）
- `GetAndWrapAsync` / `PostAndWrapAsync` / `PutAndWrapAsync` / `DeleteVoidAsync` 等 helper 方法高度相似

### 1.4 Service Registration 分散

- 3 个 Extension 文件（共 448 行）职责重叠
- 旧 Refit 注册（`HttpServiceRegistrationExtensions`）仍存在但已废弃

---

## [S2] 目标

| # | 目标 | 度量 |
|---|------|------|
| G1 | MainWindowViewModel 构造函数从 11 减到 3-4 | 参数计数 |
| G2 | 所有 Shell 服务通过接口暴露 | 接口覆盖率 100% |
| G3 | HttpClientApiClient 从 777 行减到 ~400 行 | 行数 |
| G4 | Service Registration 合并为 1 个文件 | 文件数 |
| G5 | 编译通过，现有测试全绿 | `dotnet build` + `dotnet test` |

---

## [S3] 非目标

- 不改变任何功能行为、API 契约、ViewModel 公开方法签名
- 不引入新框架或新 DI 容器
- 不改变 Prism 模块化架构
- 不重构业务模块（Patients、Herbs、MedicalCase 等）

---

## [S4] 设计：MainWindowViewModel 构造函数瘦身（IShellServices 聚合）

### 4.1 现状

```csharp
public MainWindowViewModel(
    IViewModelServices services,          // 1
    INavigationCoordinator navigationCoordinator, // 2
    MenuManager menuManager,              // 3
    IActiveConsultationService activeConsultationService, // 4
    IApplicationTickService tickService,  // 5
    IThemeService themeService,           // 6
    StatusBarManager statusBarManager,    // 7
    NavigationManager navigationManager,  // 8
    ILoginStateManager loginStateManager, // 9
    ShellEventCoordinator shellEventCoordinator, // 10
    ShellDialogHelper dialogHelper)       // 11
```

### 4.2 方案：引入 IShellServices 聚合接口

```csharp
// src/Client/Desktop/Shell/Services/IShellServices.cs
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

// src/Client/Desktop/Shell/Services/ShellServices.cs
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

### 4.3 MainWindowViewModel 瘦身

```csharp
public partial class MainWindowViewModel : CoreViewModelBase
{
    private readonly IShellServices _shell;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly NavigationManager _navigationManager;

    public MainWindowViewModel(
        IViewModelServices services,
        IShellServices shell,
        INavigationCoordinator navigationCoordinator,
        NavigationManager navigationManager)
        : base(services)
    {
        _shell = shell;
        _navigationCoordinator = navigationCoordinator;
        _navigationManager = navigationManager;

        _shell.Tick.Tick += OnTick;
        _shell.Tick.Start();
        _shell.LoginState.LoginStateChanged += OnLoginStateChanged;
        _shell.Events.LoginSuccessHandled += OnLoginSuccessHandled;
    }

    // 所有命令/属性通过 _shell.xxx 访问
    public INavigationManager NavigationManager => _shell.Navigation;
    public ILoginStateManager LoginState => _shell.LoginState;
    public IStatusBarManager StatusBar => _shell.StatusBar;
    // ...
}
```

### 4.4 DI 注册

```csharp
// ServiceCollectionExtensions.cs
containerRegistry.RegisterSingleton<IShellServices, ShellServices>();
```

### 4.5 影响范围

- `MainWindowViewModel`：构造函数 + 字段访问（11 个字段 → 3 个）
- `ShellServices`：新文件
- `ServiceCollectionExtensions`：+1 行注册
- **不改变**任何公开属性/命令签名

---

## [S5] 设计：HttpClientApiClient 重构

### 5.1 提取 QueryStringBuilder

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/QueryStringBuilder.cs
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

### 5.2 统一 HTTP 方法执行

```csharp
// 替代 GetAndWrapAsync / PostAndWrapAsync / PutAndWrapAsync / DeleteVoidAsync
private async Task<ApiResponse<T>> SendAsync<T>(
    Func<HttpClient, Task<HttpResponseMessage>> requestFactory,
    CancellationToken ct = default)
{
    var client = _httpClientFactory.CreateClient();
    var response = await requestFactory(client);
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync(ct);
    var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
    return ApiResponse<T>.Success(result!);
}

private async Task<ApiResponse<T>> GetAndWrapAsync<T>(
    string url, CancellationToken ct = default)
    => await SendAsync<T>(client => client.GetAsync(url, ct), ct);

private async Task<ApiResponse<T>> PostAndWrapAsync<T>(
    string url, object body, CancellationToken ct = default)
    => await SendAsync<T>(client => client.PostAsJsonAsync(url, body, ct), ct);
```

### 5.3 统一分页方法

```csharp
// 替代 5 处重复的分页逻辑
private async Task<ApiResponse<PagedResult<T>>> GetPagedAndWrapAsync<T>(
    string url, CancellationToken ct = default)
{
    var response = await GetAndWrapAsync<PagedResult<T>>(url, ct);
    return response; // 已经是 ApiResponse<PagedResult<T>>
}
```

### 5.4 预期效果

| 指标 | 当前 | 目标 |
|------|------|------|
| 总行数 | 777 | ~400 |
| helper 方法 | 11 个独立 | 4 个统一 |
| URL 构造 | 9+ 处重复 | QueryStringBuilder 统一 |

---

## [S6] 设计：ShellEventCoordinator 构造函数瘦身

### 6.1 现状

```csharp
public ShellEventCoordinator(
    ILoginStateManager loginStateManager,    // 1
    IEventAggregator eventAggregator,        // 2
    IUserActivityTracker userActivityTracker, // 3
    ILoginCoordinator loginCoordinator,      // 4
    ITokenLifecycleService tokenLifecycleService, // 5
    INavigationCoordinator navigationCoordinator, // 6
    MenuManager menuManager,                 // 7
    NavigationManager navigationManager,     // 8
    IModuleLazyLoader moduleLazyLoader,      // 9
    IUiThreadDispatcher uiDispatcher,        // 10
    ILogger<ShellEventCoordinator> logger)   // 11
```

### 6.2 方案：引入 IShellEventServices 聚合

```csharp
// src/Client/Desktop/Shell/Services/IShellEventServices.cs
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

```csharp
public ShellEventCoordinator(
    IShellEventServices services,
    IEventAggregator eventAggregator,
    ILogger<ShellEventCoordinator> logger)
{
    _services = services;
    // ...
}
```

### 6.3 影响范围

- `ShellEventCoordinator`：构造函数 11 → 3
- `IShellEventServices`：新接口
- `ShellEventServices`：新类
- **不改变**任何事件处理逻辑

---

## [S7] 设计：Service Registration 合并

### 7.1 目标结构

```
Shell/Extensions/
├── ServiceCollectionExtensions.cs      # 主协调器（简化后 ~120 行）
├── ViewModelServicesExtensions.cs      # 不变（Infrastructure 层）
└── UnifiedApiClientExtensions.cs       # 保留，IApiClient 唯一注册点
```

### 7.2 具体步骤

**Step 1**: 将 `HttpServiceRegistrationExtensions.RegisterHttpServices()` 标记 `[Obsolete]`

**Step 2**: 将 `ServiceCollectionExtensions.RegisterAllServices()` 中的 `RegisterHttpServices()` 调用注释掉

**Step 3**: 确认所有 Repository 已迁移到 `IApiClient`

**Step 4**: 删除 `HttpServiceRegistrationExtensions.cs`

### 7.3 影响范围

- 删除 1 个文件（93 行）
- 简化 `ServiceCollectionExtensions`（-20 行）

---

## [S8] 设计：NavigationCoordinator 构造函数瘦身

### 8.1 现状

```csharp
public NavigationCoordinator(
    IRegionManager regionManager,         // 1
    ISessionManager sessionManager,       // 2
    IRoleRegistry roleRegistry,           // 3
    INavigationHistoryService historyService, // 4
    IModuleLazyLoader moduleLazyLoader,   // 5
    IRegionMonitor regionMonitor,         // 6
    ILogger<NavigationCoordinator> logger, // 7
    IUserNotificationService? userNotificationService) // 8
```

### 8.2 方案：引入 INavigationServices 聚合

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Navigation/INavigationServices.cs
public interface INavigationServices
{
    IRegionManager RegionManager { get; }
    ISessionManager SessionManager { get; }
    IRoleRegistry RoleRegistry { get; }
    INavigationHistoryService HistoryService { get; }
    IModuleLazyLoader ModuleLazyLoader { get; }
    IRegionMonitor RegionMonitor { get; }
    IUserNotificationService? UserNotificationService { get; }
}
```

```csharp
public NavigationCoordinator(
    INavigationServices services,
    ILogger<NavigationCoordinator> logger)
{
    _services = services;
    _logger = logger;
}
```

### 8.3 影响范围

- `NavigationCoordinator`：构造函数 8 → 2
- `INavigationServices`：新接口
- `NavigationServices`：新类

---

## [S9] 设计：MenuManager 接口化

### 9.1 现状

`MenuManager` 注册为具体类，`MainWindowViewModel` 直接依赖具体类型。

### 9.2 方案

```csharp
// src/Client/Desktop/Shell/Services/IMenuManager.cs
public interface IMenuManager
{
    DelegateCommand QuickAddPatientCommand { get; }
    DelegateCommand QuickStartMedicalCaseCommand { get; }
    DelegateCommand ShowHelpCommand { get; }
    DelegateCommand ShowSettingsCommand { get; }
    DelegateCommand ToggleThemeCommand { get; }
    ICommand SaveAllCommand { get; }
    ICommand RefreshAllCommand { get; }
    ICommand PrintCommand { get; }
    ICommand ExportCommand { get; }
    ICommand UndoCommand { get; }
    ICommand RedoCommand { get; }
    DelegateCommand EditProfileCommand { get; }
    DelegateCommand NavigateToHomeCommand { get; }
    DelegateCommand NavigateToSystemSettingsCommand { get; }
    DelegateCommand NavigateBackCommand { get; }
    DelegateCommand NavigateForwardCommand { get; }
    void RefreshMenuVisibility();
}
```

### 9.3 DI 注册变更

```csharp
// 旧
containerRegistry.RegisterSingleton<MenuManager>();
// 新
containerRegistry.RegisterSingleton<IMenuManager, MenuManager>();
```

### 9.4 影响范围

- `MainWindowViewModel`：字段类型 `MenuManager` → `IMenuManager`
- `ShellEventCoordinator`：字段类型 `MenuManager` → `IMenuManager`
- `ServiceCollectionExtensions`：注册改为接口
- **不改变**任何命令行为

---

## [S10] 设计：NavigationManager + StatusBarManager 接口化

### 10.1 NavigationManager

```csharp
// src/Client/Desktop/Shell/Services/INavigationManager.cs
public interface INavigationManager
{
    ObservableCollection<NavigationItem> NavigationItems { get; }
    IList<NavigationItem> BuildNavigationItems(UserRole role);
}
```

### 10.2 StatusBarManager

```csharp
// src/Client/Desktop/Shell/Services/IStatusBarManager.cs
public interface IStatusBarManager
{
    ApiHealthStatus ApiStatus { get; }
    string ConnectionUrl { get; }
    string LocalDbStatus { get; }
    void UpdateStatus(ApiHealthStatus status, string url, string localDbStatus);
}
```

### 10.3 影响范围

- `MainWindowViewModel`：字段类型改为接口
- `ShellEventCoordinator`：字段类型改为接口
- `ServiceCollectionExtensions`：注册改为接口
- **不改变**任何行为

---

## [S11] WPF 性能优化

### 11.1 XAML 虚拟化检查

在所有 `ListBox` / `DataGrid` 中确认：

```xaml
<ListBox VirtualizingStackPanel.IsVirtualizing="True"
         VirtualizingStackPanel.VirtualizationMode="Recycling"
         VirtualizingStackPanel.CacheLength="1,1"
         ScrollViewer.IsDeferredScrollingEnabled="True">
```

### 11.2 Binding 模式优化

- 不变数据用 `Mode=OneTime`
- `TextBox` 默认 `LostFocus`（不用 `UpdateSourceTrigger=PropertyChanged`，除非搜索框）
- `Visibility` 绑定用 `{x:Static converters:Cvt.BoolToVis}`

### 11.3 Freezable 资源

静态 `Brush` / `Pen` / `Geometry` 调用 `Freeze()` 释放动画系统开销。

---

## [S12] 实施顺序

| 顺序 | 任务 | 风险 | 预估工时 |
|------|------|------|----------|
| 1 | IShellServices + MainWindowViewModel 瘦身 | 低 | 2h |
| 2 | IShellEventServices + ShellEventCoordinator 瘦身 | 低 | 1.5h |
| 3 | INavigationServices + NavigationCoordinator 瘦身 | 低 | 1h |
| 4 | IMenuManager + MenuManager 接口化 | 低 | 0.5h |
| 5 | INavigationManager + IStatusBarManager 接口化 | 低 | 0.5h |
| 6 | HttpClientApiClient 重构（QueryStringBuilder + SendAsync） | 中 | 3h |
| 7 | Service Registration 合并（删除 HttpServiceRegistrationExtensions） | 低 | 0.5h |
| 8 | WPF XAML 性能检查 | 低 | 1h |
| **合计** | | | **10h** |

---

## [S13] 验证标准

1. `dotnet build LYBTZYZS.sln` 编译通过（0 errors）
2. `dotnet test tests/LYBT.Tests.Desktop/` 全部通过
3. MainWindowViewModel 构造函数参数 ≤ 4
4. ShellEventCoordinator 构造函数参数 ≤ 3
5. NavigationCoordinator 构造函数参数 ≤ 2
6. HttpClientApiClient 行数 ≤ 450
7. `HttpServiceRegistrationExtensions.cs` 已删除
8. 所有 Shell 服务通过接口暴露（grep `RegisterSingleton<MenuManager>` 无结果）

---

## [S14] 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| IShellServices 引入后子类绑定变化 | 低 | 保持所有公开属性/命令签名不变 |
| HttpClientApiClient 重构破坏 LocalWebAPI 调用 | 中 | 逐个 helper 方法提取，每步编译验证 |
| 接口化后 DI 注册遗漏 | 低 | 编译器会报 missing method 错误 |
| WPF XAML 虚拟化改变列表行为 | 低 | 仅添加属性，不改变模板 |
