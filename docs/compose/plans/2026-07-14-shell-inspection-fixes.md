# Shell层深度检查修复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复Shell层深度检查发现的3个Critical + 12个Warning问题，提升系统稳定性和可维护性

**Architecture:** 按优先级分批修复：先修Critical登录/登出流程，再清理死代码和DI注册，最后处理线程安全和代码质量

**Tech Stack:** C# / .NET 8 / WPF / Prism.DryIoc / CommunityToolkit.Mvvm

## Global Constraints

- 遵循 AGENTS.md 编码规范：中文业务文档，英文标识符
- 修改后必须通过 `dotnet build LYBTZYZS.sln`
- 最小改动原则，不引入新功能
- 每个Task独立可验证

---

## Phase 1: Critical修复（登录/登出流程）

### Task 1: 修复LoginCoordinator.LogoutAsync状态机错误

**Covers:** C1
**Files:**
- Modify: `src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs:210-220`

**问题:** catch块中 `_stateMachine.Fire(AuthEvent.LogoutSuccess)` 应为失败事件

- [ ] **Step 1: 读取LoginCoordinator.cs确认当前代码**

```bash
# 确认文件存在并读取相关行
```

- [ ] **Step 2: 修复catch块中的状态机事件**

将 `LoginCoordinator.cs` 约第215行的：
```csharp
_stateMachine.Fire(AuthEvent.LogoutSuccess);
```
改为：
```csharp
_stateMachine.Fire(AuthEvent.LogoutFailure);
```

- [ ] **Step 3: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs
git commit -m "fix(auth): LogoutAsync异常时触发LogoutFailure而非LogoutSuccess"
```

---

### Task 2: 为LoginCoordinator.LoginAsync添加并发防护

**Covers:** C2
**Files:**
- Modify: `src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs`

**问题:** 无SemaphoreSlim防止并发登录请求

- [ ] **Step 1: 在LoginCoordinator类中添加SemaphoreSlim字段**

在 `_loginAttemptCount` 字段附近添加：
```csharp
private readonly SemaphoreSlim _loginLock = new(1, 1);
```

- [ ] **Step 2: 在LoginAsync入口添加锁**

在 `LoginAsync` 方法开头添加：
```csharp
await _loginLock.WaitAsync(cancellationToken);
```

在 try 块后添加 finally：
```csharp
finally
{
    _loginLock.Release();
}
```

- [ ] **Step 3: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs
git commit -m "fix(auth): LoginAsync添加SemaphoreSlim防止并发登录"
```

---

### Task 3: 修复MainWindowViewModel.PerformLogoutAsync事件发布

**Covers:** C3
**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:492-518`

**问题:** LogoutAsync异常后LogoutCompletedEvent不发布，UI状态不一致

- [ ] **Step 1: 重构PerformLogoutAsync确保事件发布**

将当前的：
```csharp
try
{
    await _loginCoordinator.LogoutAsync();
    EventAggregator.GetEvent<AuthEvents.LogoutCompletedEvent>().Publish(new LogoutCompletedPayload
    {
        LocalLogoutCompleted = true,
        ServerLogoutCompleted = true
    });
}
catch (Exception ex)
{
    Logger.LogWarning(ex, "登出处理异常");
}
```

改为：
```csharp
var localLogoutCompleted = false;
var serverLogoutCompleted = false;

try
{
    await _loginCoordinator.LogoutAsync();
    serverLogoutCompleted = true;
}
catch (Exception ex)
{
    Logger.LogWarning(ex, "登出处理异常");
}

localLogoutCompleted = true;
EventAggregator.GetEvent<AuthEvents.LogoutCompletedEvent>().Publish(new LogoutCompletedPayload
{
    LocalLogoutCompleted = localLogoutCompleted,
    ServerLogoutCompleted = serverLogoutCompleted
});
```

- [ ] **Step 2: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "fix(auth): PerformLogoutAsync异常后仍发布LogoutCompletedEvent"
```

---

## Phase 2: Warning修复（死代码清理 + DI注册）

### Task 4: 移除CoreServicesStartupStep和ApplicationInitializationService

**Covers:** W1, W2
**Files:**
- Delete: `src/Client/Desktop/Shell/Services/Startup/Steps/CoreServicesStartupStep.cs`
- Delete: `src/Client/Desktop/Shell/Services/ApplicationInitializationService.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:173`
- Modify: `src/Client/Desktop/Shell/Services/AppStartupOrchestrator.cs:60`

**问题:** CoreServicesStartupStep是空壳步骤，ApplicationInitializationService与启动步骤完全重复

- [ ] **Step 1: 从ServiceCollectionExtensions中移除ApplicationInitializationService注册**

删除第173行：
```csharp
containerRegistry.RegisterSingleton<IApplicationInitializationService, ApplicationInitializationService>();
```

- [ ] **Step 2: 从AppStartupOrchestrator.RegisterSteps中移除CoreServices步骤**

删除第60-61行：
```csharp
pipeline.RegisterStep(_container.Resolve<IStartupStep>("CoreServices"));
```

- [ ] **Step 3: 从ServiceCollectionExtensions中移除CoreServicesStartupStep注册**

删除第187行：
```csharp
containerRegistry.Register<IStartupStep, CoreServicesStartupStep>("CoreServices");
```

- [ ] **Step 4: 删除CoreServicesStartupStep.cs文件**

- [ ] **Step 5: 删除ApplicationInitializationService.cs文件**

- [ ] **Step 6: 检查并移除其他对这两个类型的引用**

```bash
# 搜索所有引用
rg "CoreServicesStartupStep|IApplicationInitializationService|ApplicationInitializationService" src/ --include="*.cs"
```

- [ ] **Step 7: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 8: 提交**

```bash
git add -A
git commit -m "refactor(shell): 移除CoreServicesStartupStep空壳步骤和ApplicationInitializationService死代码"
```

---

### Task 5: 统一StartupStep DI注册模式

**Covers:** W3
**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:185-195`
- Modify: `src/Client/Desktop/Shell/Services/AppStartupOrchestrator.cs:56-69`

**问题:** 部分Step通过DI resolve，部分手动new，ApiHealthCheckStartupStep注册但未消费

- [ ] **Step 1: 将所有StartupStep改为DI命名注册**

修改ServiceCollectionExtensions.cs中RegisterApplicationServices方法，将所有Step统一为命名注册：

```csharp
// 启动步骤 - 全部通过DI按名称解析
containerRegistry.Register<IStartupStep, ErrorHandlingStartupStep>("ErrorHandling");
containerRegistry.Register<IStartupStep, ModuleCoordinatorStartupStep>("ModuleCoordinator");
containerRegistry.Register<IStartupStep, LocalWebApiStartupStep>("LocalWebApi");
containerRegistry.Register<IStartupStep>(resolver =>
{
    var appState = resolver.Resolve<IApplicationStateService>();
    var logger = resolver.Resolve<ILogger<ApiHealthCheckStartupStep>>();
    return new ApiHealthCheckStartupStep(appState, logger, timeoutSeconds: 5);
}, "ApiHealthCheck");
containerRegistry.Register<IStartupStep, WarmupStartupStep>("Warmup");
```

- [ ] **Step 2: 修改AppStartupOrchestrator.RegisterSteps全部从DI解析**

```csharp
private void RegisterSteps(IStartupPipeline pipeline)
{
    pipeline.RegisterStep(_container.Resolve<IStartupStep>("ErrorHandling"));
    pipeline.RegisterStep(_container.Resolve<IStartupStep>("ModuleCoordinator"));
    pipeline.RegisterStep(_container.Resolve<IStartupStep>("LocalWebApi"));
    pipeline.RegisterStep(_container.Resolve<IStartupStep>("ApiHealthCheck"));
    pipeline.RegisterStep(_container.Resolve<IStartupStep>("Warmup"));
}
```

- [ ] **Step 3: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs src/Client/Desktop/Shell/Services/AppStartupOrchestrator.cs
git commit -m "refactor(shell): 统一StartupStep为DI命名注册，移除手动new"
```

---

### Task 6: NavigationManager和StatusBarManager显式注册为Singleton

**Covers:** W4
**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**问题:** 两个有状态管理类靠DryIoc自动解析等效Transient

- [ ] **Step 1: 在RegisterPresentationServices中添加显式注册**

```csharp
containerRegistry.RegisterSingleton<NavigationManager>();
containerRegistry.RegisterSingleton<StatusBarManager>();
```

- [ ] **Step 2: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs
git commit -m "refactor(shell): NavigationManager和StatusBarManager显式注册为Singleton"
```

---

## Phase 3: Warning修复（线程安全 + 事件管理）

### Task 7: 修复MainWindowViewModel事件订阅泄漏

**Covers:** W6
**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

**问题:** EventAggregator订阅的SubscriptionToken未在OnDisposing中Dispose

- [ ] **Step 1: 添加SubscriptionToken字段**

在类的字段区域添加：
```csharp
private SubscriptionToken? _passwordChangedToken;
private SubscriptionToken? _profileUpdatedToken;
private SubscriptionToken? _tokenLifecycleToken;
```

- [ ] **Step 2: 修改InitializeViewModel保存Token**

```csharp
_passwordChangedToken = Events.Subscribe<AuthEvents.PasswordChangedEvent, PasswordChangedPayload>(OnPasswordChanged);
_profileUpdatedToken = Events.Subscribe<AuthEvents.ProfileUpdatedEvent, ProfileUpdatedPayload>(OnProfileUpdated);
_tokenLifecycleToken = Events.Subscribe<TokenLifecycleStateChangedEvent, TokenLifecycleStateChangedEventArgs>(
    args => OnTokenLifecycleStateChangedAsync(args).SafeFireAndForget(ex => Logger.LogError(ex, "Token生命周期事件处理异常")));
```

- [ ] **Step 3: 在OnDisposing中取消订阅**

```csharp
_passwordChangedToken?.Dispose();
_profileUpdatedToken?.Dispose();
_tokenLifecycleToken?.Dispose();
```

- [ ] **Step 4: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "fix(shell): MainWindowViewModel OnDisposing取消所有EventAggregator订阅"
```

---

### Task 8: 修复ApiHealthMonitor线程安全

**Covers:** W9
**Files:**
- Modify: `src/Client/Desktop/Shell/Services/HealthCheck/ApiHealthMonitor.cs`

**问题:** _isChecking、_consecutiveFailures等字段无锁并发访问

- [ ] **Step 1: 将状态字段改为volatile或使用Interlocked**

将字段声明改为：
```csharp
private volatile bool _isChecking;
private volatile int _consecutiveFailures;
private volatile CircuitState _circuitState;
private volatile ApiHealthStatus _status;
```

- [ ] **Step 2: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Shell/Services/HealthCheck/ApiHealthMonitor.cs
git commit -m "fix(shell): ApiHealthMonitor状态字段添加volatile保证线程可见性"
```

---

### Task 9: 修复SessionLifecycleManager双重SessionExpired事件

**Covers:** W11
**Files:**
- Modify: `src/Client/Desktop/Shell/Services/Session/SessionLifecycleManager.cs`

**问题:** Token过期和用户活动过期可能同时触发两次SessionExpired

- [ ] **Step 1: 添加防重入标志**

在类中添加字段：
```csharp
private volatile bool _sessionExpiredFired;
```

- [ ] **Step 2: 修改OnTokenLifecycleStateChanged中的Expired处理**

```csharp
case TokenLifecycleState.Expired:
    if (!_sessionExpiredFired)
    {
        _sessionExpiredFired = true;
        TransitionTo(SessionState.Expired);
        SessionExpired?.Invoke(this, EventArgs.Empty);
    }
    break;
```

- [ ] **Step 3: 修改OnUserActivitySessionExpired**

```csharp
private void OnUserActivitySessionExpired(object? sender, EventArgs e)
{
    if (!_sessionExpiredFired)
    {
        _sessionExpiredFired = true;
        _logger.LogWarning("用户长时间不活跃，会话已过期");
        TransitionTo(SessionState.Expired);
        SessionExpired?.Invoke(this, EventArgs.Empty);
    }
}
```

- [ ] **Step 4: 在StartSessionAsync中重置标志**

在 `StartSessionAsync` 方法开头添加：
```csharp
_sessionExpiredFired = false;
```

- [ ] **Step 5: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 6: 提交**

```bash
git add src/Client/Desktop/Shell/Services/Session/SessionLifecycleManager.cs
git commit -m "fix(session): SessionLifecycleManager防止双重SessionExpired事件"
```

---

### Task 10: 修复ThemeService事件泄漏

**Covers:** W7
**Files:**
- Modify: `src/Client/Desktop/Shell/Services/ThemeService.cs`

**问题:** lambda事件处理器无法取消订阅，ThemeService未实现IDisposable

- [ ] **Step 1: 将lambda改为命名方法并保存委托引用**

```csharp
private EventHandler? _themeChangedHandler;

private void InitializeThemeSync()
{
    var themeManager = PaletteHelper.GetThemeManager();
    if (themeManager == null) return;

    _themeChangedHandler = (_, e) => { /* 原有逻辑 */ };
    themeManager.ThemeChanged += _themeChangedHandler;
}
```

- [ ] **Step 2: 让ThemeService实现IDisposable**

```csharp
public class ThemeService : IThemeService, IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        var themeManager = PaletteHelper.GetThemeManager();
        if (themeManager != null && _themeChangedHandler != null)
            themeManager.ThemeChanged -= _themeChangedHandler;
        _disposed = true;
    }
}
```

- [ ] **Step 3: 在DI注册中确认Singleton生命周期（已有）**

- [ ] **Step 4: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Shell/Services/ThemeService.cs
git commit -m "fix(shell): ThemeService事件处理器改为命名方法，实现IDisposable"
```

---

### Task 11: 修复MenuManager DelegateCommand async void

**Covers:** W8
**Files:**
- Modify: `src/Client/Desktop/Shell/Services/MenuManager.cs:137-141`

**问题:** DelegateCommand的Action构造函数期望同步委托，传入async lambda产生async void

- [ ] **Step 1: 将async lambda改为同步命令+FireAsync模式**

```csharp
QuickAddPatientCommand = new DelegateCommand(() => _ = ExecuteQuickAddPatientAsync());
QuickStartMedicalCaseCommand = new DelegateCommand(() => _ = ExecuteQuickStartMedicalCaseAsync());
ToggleThemeCommand = new DelegateCommand(() => _ = ExecuteToggleThemeAsync());
```

- [ ] **Step 2: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Shell/Services/MenuManager.cs
git commit -m "fix(shell): MenuManager DelegateCommand改为同步委托+FireAsync模式"
```

---

## Phase 4: Warning修复（导航一致性）

### Task 12: 修复NavigationManager模块名判断不一致

**Covers:** W5
**Files:**
- Modify: `src/Client/Desktop/Shell/Services/NavigationManager.cs:116`

**问题:** ReportsModule判断使用GetAllModules()而非RequiredModules

- [ ] **Step 1: 统一使用modules变量**

将第116行：
```csharp
if (definition.GetAllModules().Contains("ReportsModule"))
```
改为：
```csharp
if (modules.Contains("ReportsModule"))
```

- [ ] **Step 2: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Shell/Services/NavigationManager.cs
git commit -m "fix(nav): NavigationManager模块名判断统一使用RequiredModules"
```

---

## Phase 5: Info修复（代码质量）

### Task 13: 提取硬编码常量

**Covers:** I1
**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- Modify: `src/Client/Desktop/Shell/Services/NavigationManager.cs`

- [ ] **Step 1: 在MainWindowViewModel中提取侧边栏宽度常量**

```csharp
private const int SidebarCollapsedWidth = 60;
private const int SidebarExpandedWidth = 140;
```

将 `OnIsSidebarExpandedChanged` 中的：
```csharp
SidebarWidth = value ? 140 : 60;
```
改为：
```csharp
SidebarWidth = value ? SidebarExpandedWidth : SidebarCollapsedWidth;
```

- [ ] **Step 2: 在NavigationManager中提取模块名常量（可选，如果模块名已在ViewNames中定义则跳过）**

- [ ] **Step 3: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "refactor(shell): 提取侧边栏宽度为命名常量"
```

---

## 验证

### Task 14: 全量编译验证

- [ ] **Step 1: 完整编译**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 2: 运行Desktop测试**

```bash
dotnet test tests/LYBT.Tests.Desktop/ --no-restore
```

- [ ] **Step 3: 运行Architecture测试**

```bash
dotnet test tests/LYBT.Tests.Architecture/ --no-restore
```

- [ ] **Step 4: 检查是否有遗留引用**

```bash
rg "CoreServicesStartupStep|ApplicationInitializationService" src/ --include="*.cs"
```

预期：无结果

---

## 执行摘要

| Phase | Tasks | 预计耗时 | 依赖 |
|-------|-------|---------|------|
| Phase 1: Critical修复 | Task 1-3 | 15min | 无 |
| Phase 2: 死代码+DI | Task 4-6 | 20min | 无 |
| Phase 3: 线程安全+事件 | Task 7-11 | 25min | 无 |
| Phase 4: 导航一致性 | Task 12 | 5min | 无 |
| Phase 5: 代码质量 | Task 13 | 5min | 无 |
| 验证 | Task 14 | 10min | 所有Phase |
| **总计** | **14 tasks** | **~80min** | — |
