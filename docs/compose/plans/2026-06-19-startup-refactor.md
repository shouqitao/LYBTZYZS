# 启动流程代码重构 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute.

**Goal:** 简化 App.xaml.cs 从启动到登录的嵌套调用逻辑，保持同样视觉效果。

**Architecture:** 提取 `AppStartupOrchestrator` 类封装启动流程（splash 管理 + pipeline 执行 + 错误处理），App.xaml.cs 仅负责 Prism 生命周期委托。

**Tech Stack:** C# / .NET 8 / WPF / Prism

---

### Task 1: 创建 AppStartupOrchestrator — 封装启动流程

**Files:**
- Create: `src/Client/Desktop/Shell/Services/AppStartupOrchestrator.cs`

- [ ] **Step 1: 创建 AppStartupOrchestrator**

将 App.xaml.cs 中的以下逻辑提取到独立类：
- Splash 创建/更新/关闭/FadeOut
- StartupPipeline 注册/执行/事件订阅
- 成功路径：ShowMainWindow
- 失败路径：错误对话框
- 性能监控（可选保留或删除——YAGNI）

```csharp
public class AppStartupOrchestrator
{
    private readonly IContainerProvider _container;
    private readonly ILogger<AppStartupOrchestrator> _logger;
    private SplashScreenWindow? _splash;

    public AppStartupOrchestrator(IContainerProvider container, ILogger<AppStartupOrchestrator> logger)
    {
        _container = container;
        _logger = logger;
    }

    public void ShowSplash()
    {
        _splash = new SplashScreenWindow();
        _splash.Show();
        _splash.UpdateStatus("正在初始化应用程序...");
    }

    public async Task RunStartupAsync(Window mainWindow)
    {
        try
        {
            var pipeline = _container.Resolve<IStartupPipeline>();
            RegisterSteps(pipeline);

            var progress = new Progress<string>(msg => _splash?.UpdateStatus(msg));
            var result = await pipeline.ExecuteAsync(progress);

            if (!result.Success)
                throw new InvalidOperationException($"启动步骤 '{result.FailedStepName}' 失败: {result.ErrorMessage}");

            await CloseSplashAsync();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "应用启动失败");
            _splash?.Close();
            // 显示错误对话框
            System.Windows.MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void RegisterSteps(IStartupPipeline pipeline)
    {
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("ErrorHandling"));
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("ModuleCoordinator"));
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("CoreServices"));
        pipeline.RegisterStep(new Services.Startup.Steps.LocalWebApiStartupStep(
            _container.Resolve<LYBT.Desktop.Contracts.Services.IEmbeddedLocalWebApiService>(),
            _container.Resolve<ILogger<Services.Startup.Steps.LocalWebApiStartupStep>>()));
        pipeline.RegisterStep(new ApiHealthCheckStartupStep(
            _container.Resolve<IApplicationStateService>(),
            _container.Resolve<ILogger<ApiHealthCheckStartupStep>>(),
            timeoutSeconds: 5));
        pipeline.RegisterStep(_container.Resolve<IStartupStep>("Warmup"));
    }

    private async Task CloseSplashAsync()
    {
        if (_splash == null) return;
        _splash.FadeOut();
        await Task.Delay(400);
        _splash.Close();
        _splash = null;
    }
}
```

- [ ] **Step 2: 构建验证**

### Task 2: 简化 App.xaml.cs — 委托给 Orchestrator

**Files:**
- Modify: `src/Client/Desktop/Shell/App.xaml.cs`

- [ ] **Step 1: 删除内联启动逻辑**

删除：`InitializeApplicationAsync`、`ShowMainWindowAfterInitializationAsync`、`HandleInitializationFailureAsync`、`RegisterStartupSteps`、`SubscribeToPipelineEvents`、`_splashScreen`、`_startupPipeline`、`_performanceMonitor`、`_bootstrapper`

- [ ] **Step 2: 简化 OnStartup**

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    if (!TryAcquireSingleInstance()) { NativeMethods.ActivateExistingWindow(MainWindowTitle); Shutdown(); return; }
    SetConsoleEncoding();
    DesktopSerilogConfiguration.Initialize();

    _orchestrator = Container.Resolve<AppStartupOrchestrator>();
    _orchestrator.ShowSplash();

    base.OnStartup(e);
}
```

- [ ] **Step 3: 简化 OnInitialized**

```csharp
protected override void OnInitialized()
{
    base.OnInitialized();
    _ = _orchestrator!.RunStartupAsync(MainWindow!);
}
```

- [ ] **Step 4: 注册 Orchestrator 到 DI**

在 RegisterTypes 中添加：
```csharp
containerRegistry.RegisterSingleton<AppStartupOrchestrator>();
```

- [ ] **Step 5: 构建验证 + Commit**

### Task 3: 清理 OnExit — 简化资源释放

**Files:**
- Modify: `src/Client/Desktop/Shell/App.xaml.cs`

- [ ] **Step 1: 简化 OnExit**

删除散落的 `Container.Resolve<>()` + SafeDispose 模式。改为：
```csharp
protected override void OnExit(ExitEventArgs e)
{
    Log.Information("应用程序退出");
    base.OnExit(e);
    _instanceMutex?.ReleaseMutex();
    _instanceMutex?.Dispose();
    DesktopSerilogConfiguration.CloseAndFlush();
}
```

DI 容器会在应用关闭时自动 Dispose 单例服务（TickService、TokenLifecycle、ActivityTracker 等都是 Singleton）。不需要手动逐个释放。

- [ ] **Step 2: 构建验证 + Commit**

### Task 4: 最终验证

- [ ] **Step 1: 全量构建**
- [ ] **Step 2: 验证启动流程**：Splash → Pipeline → FadeOut → MainWindow → LoginView
- [ ] **Step 3: Commit + Push**
