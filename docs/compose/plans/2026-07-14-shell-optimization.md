# Shell层优化实施计划

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/2026-07-14-shell-optimization-report.md)

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复Shell层深度检查发现的3个Critical问题和2个Major问题

**Architecture:** 
1. 修复启动管线双重初始化 - 删除CoreServicesStartupStep中的重复调用
2. 统一健康检查系统 - 保留ApiHealthMonitor，删除HealthCheckCoordinator
3. 消除ContainerLocator反模式 - 改用构造函数注入
4. 修复SessionManager.SessionExpired事件
5. 修复SessionManager线程安全问题

**Tech Stack:** .NET 8, WPF/Prism, CommunityToolkit.Mvvm, MaterialDesignInXAML

## Global Constraints

- 保持向后兼容，不破坏现有功能
- 每个Task独立可测试
- 遵循项目代码规范（中文业务文档/注释，英文标识符）
- 使用CommunityToolkit.Mvvm的[ObservableProperty]和[RelayCommand]

---

## Task 1: 修复启动管线双重初始化

**Covers:** Critical Issue C1

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/Startup/Steps/CoreServicesStartupStep.cs`

**Interfaces:**
- Consumes: IApplicationInitializationService
- Produces: 无重复初始化的启动管线

- [ ] **Step 1: 检查当前CoreServicesStartupStep实现**

  读取 `src/Client/Desktop/Shell/Services/Startup/Steps/CoreServicesStartupStep.cs`

- [ ] **Step 2: 修改CoreServicesStartupStep**

  将 `InitializeCoreServicesAsync()` 改为空操作，因为其他步骤已经处理了这些功能：

```csharp
public async Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
{
    progress?.Report("核心服务初始化（已由其他步骤处理）...");

    try
    {
        // 核心服务初始化已由以下步骤分别处理：
        // - ErrorHandlingStartupStep: 错误处理
        // - ModuleCoordinatorStartupStep: 模块协调器
        // - WarmupStartupStep: 应用预热
        _logger.LogInformation("核心服务初始化完成（委托给专用步骤）");

        return StartupStepResult.Succeeded(TimeSpan.Zero);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "核心服务初始化失败");
        return StartupStepResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("核心服务初始化", ex), ex);
    }
}
```

- [ ] **Step 3: 运行构建验证**

```bash
dotnet build LYBTZYZS.sln --no-restore --verbosity minimal
```

预期：构建成功，0错误

- [ ] **Step 4: 提交更改**

```bash
git add src/Client/Desktop/Shell/Services/Startup/Steps/CoreServicesStartupStep.cs
git commit -m "fix(shell): 消除启动管线双重初始化 - CoreServicesStartupStep委托给专用步骤"
```

---

## Task 2: 统一健康检查系统

**Covers:** Critical Issue C2

**Files:**
- Delete: `src/Client/Desktop/Shell/Services/HealthCheck/HealthCheckCoordinator.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Consumes: IApiHealthMonitor
- Produces: 统一的健康检查系统

- [ ] **Step 1: 检查HealthCheckCoordinator的使用情况**

  搜索 `IHealthCheckCoordinator` 的所有引用

- [ ] **Step 2: 修改ServiceCollectionExtensions**

  注释掉HealthCheckCoordinator的注册：

```csharp
// Shell架构整合 - HealthCheckCoordinator服务 (已由ApiHealthMonitor替代)
// containerRegistry.RegisterSingleton<IHealthCheckCoordinator, HealthCheckCoordinator>();
```

- [ ] **Step 3: 修改MainWindowViewModel**

  移除IHealthCheckCoordinator的注入和使用，改用IApiHealthMonitor

- [ ] **Step 4: 运行构建验证**

```bash
dotnet build LYBTZYZS.sln --no-restore --verbosity minimal
```

预期：构建成功，0错误

- [ ] **Step 5: 提交更改**

```bash
git add src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "fix(shell): 统一健康检查系统 - 移除HealthCheckCoordinator，使用ApiHealthMonitor"
```

---

## Task 3: 消除ContainerLocator反模式

**Covers:** Critical Issue C3

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/DialogHostService.cs`

**Interfaces:**
- Consumes: IDialogService
- Produces: 无ContainerLocator的DialogHostService

- [ ] **Step 1: 检查DialogHostService当前实现**

  读取 `src/Client/Desktop/Shell/Services/DialogHostService.cs`

- [ ] **Step 2: 修改DialogHostService**

  通过构造函数注入IDialogService：

```csharp
public class DialogHostService : IDialogHostService
{
    private const string RootDialog = "RootDialog";
    private readonly IDialogService _dialogService;

    public DialogHostService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        // 使用Prism IDialogService而不是ContainerLocator
        var parameters = new DialogParameters
        {
            { "Message", message },
            { "Title", title }
        };

        var result = await _dialogService.ShowDialogAsync("ConfirmationDialog", parameters);
        return result.Result == ButtonResult.OK;
    }

    public async Task<T?> ShowCustomDialogAsync<T>(object dialogContent) where T : class
    {
        var result = await DialogHost.Show(dialogContent, RootDialog);
        return result as T;
    }
}
```

- [ ] **Step 3: 运行构建验证**

```bash
dotnet build LYBTZYZS.sln --no-restore --verbosity minimal
```

预期：构建成功，0错误

- [ ] **Step 4: 提交更改**

```bash
git add src/Client/Desktop/Shell/Services/DialogHostService.cs
git commit -m "fix(shell): 消除ContainerLocator反模式 - 使用构造函数注入"
```

---

## Task 4: 修复SessionManager.SessionExpired事件

**Covers:** Major Issue M1

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs`

**Interfaces:**
- Consumes: IAuthenticationService
- Produces: 正确触发SessionExpired事件的SessionManager

- [ ] **Step 1: 检查SessionManager当前实现**

  读取 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs`

- [ ] **Step 2: 修改SessionManager**

  在ClearSession方法中触发SessionExpired事件：

```csharp
public void ClearSession()
{
    var wasAuthenticated = IsAuthenticated;
    _cachedUser = null;
    _authService.ClearAuthInfo();
    
    if (wasAuthenticated)
    {
        SessionExpired?.Invoke(this, EventArgs.Empty);
        SessionChanged?.Invoke(this, new SessionChangedEventArgs(false));
    }
}
```

- [ ] **Step 3: 移除pragma warning disable**

  删除第17-19行的 `#pragma warning disable CS0067`

- [ ] **Step 4: 运行构建验证**

```bash
dotnet build LYBTZYZS.sln --no-restore --verbosity minimal
```

预期：构建成功，0错误

- [ ] **Step 5: 提交更改**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs
git commit -m "fix(shell): 修复SessionManager.SessionExpired事件 - 在ClearSession时触发"
```

---

## Task 5: 修复SessionManager线程安全

**Covers:** Major Issue M2

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs`

**Interfaces:**
- Consumes: IAuthenticationService
- Produces: 线程安全的SessionManager

- [ ] **Step 1: 检查SessionManager当前实现**

  读取 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs`

- [ ] **Step 2: 修改SessionManager**

  添加锁机制确保线程安全：

```csharp
public class SessionManager : ISessionManager
{
    private readonly IAuthenticationService _authService;
    private readonly object _lock = new();
    private UserDetailDto? _cachedUser;
    
    // ... 其他代码保持不变 ...

    public UserDetailDto? CurrentUser
    {
        get
        {
            lock (_lock)
            {
                if (_cachedUser == null)
                    _cachedUser = _authService.GetCurrentUser();
                return _cachedUser;
            }
        }
    }

    public void SetSession(UserDetailDto user, string accessToken, string? refreshToken = null)
    {
        lock (_lock)
        {
            _cachedUser = user ?? throw new ArgumentNullException(nameof(user));
        }
        ArgumentNullException.ThrowIfNull(accessToken);
        SessionChanged?.Invoke(this, new SessionChangedEventArgs(true, user));
    }

    public void ClearSession()
    {
        lock (_lock)
        {
            _cachedUser = null;
        }
        // ... 其余代码保持不变 ...
    }
}
```

- [ ] **Step 3: 运行构建验证**

```bash
dotnet build LYBTZYZS.sln --no-restore --verbosity minimal
```

预期：构建成功，0错误

- [ ] **Step 4: 提交更改**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SessionManager.cs
git commit -m "fix(shell): 修复SessionManager线程安全 - 添加锁机制"
```

---

## Task 6: 修复NavigationManager硬编码视图名

**Covers:** Minor Issue m1

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs`
- Modify: `src/Client/Desktop/Shell/Services/NavigationManager.cs`

**Interfaces:**
- Consumes: ViewNames常量
- Produces: 使用常量的NavigationManager

- [ ] **Step 1: 在ViewNames中添加常量**

```csharp
#region 诊断视图

/// <summary>日志级别控制</summary>
public const string LogLevelControl = "LogLevelControlView";

#endregion
```

- [ ] **Step 2: 修改NavigationManager**

```csharp
items.Add(CreateNavItem("日志控制", ViewNames.LogLevelControl, "Tune", "管理"));
```

- [ ] **Step 3: 运行构建验证**

```bash
dotnet build LYBTZYZS.sln --no-restore --verbosity minimal
```

预期：构建成功，0错误

- [ ] **Step 4: 提交更改**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs src/Client/Desktop/Shell/Services/NavigationManager.cs
git commit -m "fix(shell): 修复NavigationManager硬编码视图名 - 使用ViewNames常量"
```

---

## Self-Review

### 1. 覆盖完整性
- [x] Critical C1: 启动管线双重初始化 ✓ (Task 1)
- [x] Critical C2: 健康检查系统重复 ✓ (Task 2)
- [x] Critical C3: ContainerLocator反模式 ✓ (Task 3)
- [x] Major M1: SessionExpired事件未触发 ✓ (Task 4)
- [x] Major M2: SessionManager线程安全 ✓ (Task 5)
- [x] Minor m1: 硬编码视图名 ✓ (Task 6)

### 2. 无占位符
所有步骤都有具体的代码和验证方法，无TBD/TODO。

### 3. 类型一致性
所有文件路径和符号名称与代码库一致。

---

## Execution Handoff

Plan saved. How would you like to execute it?

- **Subagent, always**: Fresh subagent per task — remember for future sessions
- **Subagent, this time**: Fresh subagent per task — just this once
- **Inline, always**: Execute in this session — remember for future sessions
- **Inline, this time**: Execute in this session — just this once
