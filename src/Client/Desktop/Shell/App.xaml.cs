using System.Windows;
using LYBT.Desktop.Admin;
using LYBT.Desktop.Auth;
using LYBT.Desktop.CardReader;
using LYBT.Desktop.Clinical;
using LYBT.Desktop.Contracts.Performance;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Caching.Memory;
// [已删除] using LYBT.Desktop.Consultation; - 模块已废弃，功能已迁移到MedicalCase模块
using LYBT.Desktop.Formula;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.Infrastructure.Logging;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.Patients;
// [已删除] using LYBT.Desktop.Prescriptions; - 模块已移除
using LYBT.Desktop.Registration;
using LYBT.Desktop.Sync;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.Services.Startup.Steps;
using LYBT.Desktop.Shell.ViewModels;
using LYBT.Desktop.Shell.Views;
using LYBT.Desktop.Users;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Serilog;

namespace LYBT.Desktop.Shell;

/// <summary>应用程序主入口 - WPF应用程序核心启动器，提供智能模块加载和角色驱动初始化</summary>
public partial class App : PrismApplication
{
    private IApplicationBootstrapper? _bootstrapper;
    private IStartupPipeline? _startupPipeline;
    private StartupPerformanceMonitor? _performanceMonitor;
    private SplashScreenWindow? _splashScreen;

    // OpenSpec: implement-single-instance-mode - 单实例模式支持
    private static Mutex? _instanceMutex;
    private const string MutexName = "Global\\LYBTZYZS_Shell_Instance";
    private const string MainWindowTitle = "凌隐宝堂中医诊所管理系统";

    /// <summary>应用程序启动入口</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // OpenSpec: implement-single-instance-mode - 单实例检查（必须在任何初始化之前）
        if (!TryAcquireSingleInstance())
        {
            // 尝试激活已有窗口
            NativeMethods.ActivateExistingWindow(MainWindowTitle);
            Shutdown();
            return;
        }

        // 设置控制台编码为UTF-8（必须在Serilog初始化前，否则Console sink无法正确显示中文）
        SetConsoleEncoding();

        // refactor-logging-system: 初始化Serilog日志系统
        DesktopSerilogConfiguration.Initialize();
        Log.Information("应用程序启动");

        _splashScreen = new SplashScreenWindow();
        _splashScreen.Show();
        _splashScreen.UpdateStatus("正在初始化应用程序...");
        base.OnStartup(e);
    }

    /// <summary>尝试获取单实例锁</summary>
    /// <returns>true表示当前是唯一实例，false表示已有实例运行</returns>
    private static bool TryAcquireSingleInstance()
    {
        _instanceMutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            // 已有实例，释放当前创建的Mutex句柄
            _instanceMutex.Dispose();
            _instanceMutex = null;
            return false;
        }
        return true;
    }

    /// <summary>应用程序退出</summary>
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("应用程序退出，开始释放资源");

        // OpenSpec: implement-single-instance-mode - 按依赖顺序释放资源

        // 1. 停止定时服务
        SafeDispose(() =>
        {
            var tickService = Container.Resolve<IApplicationTickService>();
            tickService.Stop();
            (tickService as IDisposable)?.Dispose();
        }, "ApplicationTickService");

        SafeDispose(() =>
        {
            var tokenService = Container.Resolve<ITokenLifecycleService>();
            tokenService.StopMonitoring();
            tokenService.Dispose();
        }, "TokenLifecycleService");

        // 2. 释放用户活动追踪
        SafeDispose(() =>
        {
            var activityTracker = Container.Resolve<IUserActivityTracker>();
            (activityTracker as IDisposable)?.Dispose();
        }, "UserActivityTracker");

        // 3. 释放缓存
        SafeDispose(() =>
        {
            var cache = Container.Resolve<IMemoryCache>();
            cache.Dispose();
        }, "MemoryCache");

        // 4. 释放Mutex
        SafeDispose(() =>
        {
            _instanceMutex?.ReleaseMutex();
            _instanceMutex?.Dispose();
            _instanceMutex = null;
        }, "InstanceMutex");

        // 5. 关闭日志（最后执行）
        Log.Information("资源释放完成");
        DesktopSerilogConfiguration.CloseAndFlush();

        base.OnExit(e);
    }

    /// <summary>创建应用程序主窗体</summary>
    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    /// <summary>初始化主窗口</summary>
    protected override void InitializeShell(Window shell)
    {
        base.InitializeShell(shell);
        shell.Hide();
    }

    /// <summary>注册应用程序类型和服务</summary>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry, nameof(containerRegistry));

        containerRegistry.RegisterSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
        containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService,
            LYBT.Desktop.Shell.Services.ApplicationInitializationService>();
        containerRegistry.RegisterAllServices();
        containerRegistry.Register<MainWindowViewModel>();
        containerRegistry.RegisterDialog<Dialogs.Views.ConfirmationDialog, Dialogs.ViewModels.ConfirmationDialogViewModel>();
        // [已删除] ApiConnectionFailedDialog - OpenSpec: refactor-startup-connection-resilience
        // OpenSpec: fix-missing-dialogs - 统一消息对话框和输入对话框
        containerRegistry.RegisterDialog<Dialogs.Views.MessageDialog, Dialogs.ViewModels.MessageDialogViewModel>();
        containerRegistry.RegisterDialog<Dialogs.Views.InputDialog, Dialogs.ViewModels.InputDialogViewModel>();
        // OpenSpec: unify-dialog-to-prism - 统一到Prism DialogService
        containerRegistry.RegisterDialog<LYBT.Desktop.Infrastructure.Views.UnfinishedCaseDialog,
            LYBT.Desktop.Infrastructure.ViewModels.UnfinishedCaseDialogViewModel>();

        // OpenSpec: migrate-views-to-role-modules - 账户设置（合并个人资料+修改密码）
        containerRegistry.Register<ViewModels.AccountSettingsViewModel>();
        containerRegistry.RegisterForNavigation<Views.AccountSettingsView>();
    }

    /// <summary>配置ViewModel定位器</summary>
    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();
        ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
        ViewModelLocationProvider.Register<Controls.AccountSettingsControl, ViewModels.AccountSettingsViewModel>();
    }

    /// <summary>应用程序初始化完成后的回调</summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Phase 4 Task 4.4: 集成性能监控框架
        var performanceMonitor = Container.Resolve<IPerformanceMonitor>();
        performanceMonitor.StartTiming("App_Startup_Total");
        performanceMonitor.RecordMemoryBaseline("App_Startup_Memory");

        _performanceMonitor = new StartupPerformanceMonitor(Container.Resolve<ILoggerFactory>());
        _performanceMonitor.StartMonitoring();
        _performanceMonitor.StartStage("应用初始化");

        _ = InitializeApplicationAsync();
    }

    /// <summary>异步初始化应用程序（使用启动管道）</summary>
    /// <remarks>enhance-shell-connection-dialog: 支持API连接失败时的恢复对话框和重试机制</remarks>
    private async Task InitializeApplicationAsync()
    {
        try
        {
            // 保留bootstrapper用于角色模块加载
            _bootstrapper = Container.Resolve<IApplicationBootstrapper>();

            // 使用启动管道执行初始化流程
            _startupPipeline = Container.Resolve<IStartupPipeline>();
            RegisterStartupSteps();
            SubscribeToPipelineEvents();

            var progress = new Progress<string>(message => _splashScreen?.UpdateStatus(message));

            // OpenSpec: refactor-startup-connection-resilience - 非阻塞启动，API检查已设为非必需
            var result = await _startupPipeline.ExecuteAsync(progress);

            if (result.Success)
            {
                await ShowMainWindowAfterInitializationAsync();
                return;
            }

            // API健康检查失败不再阻塞启动（IsRequired=false），其他必需步骤失败仍抛异常
            throw new InvalidOperationException(
                $"启动步骤 '{result.FailedStepName}' 执行失败: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            await HandleInitializationFailureAsync(ex);
        }
    }

    // [已删除] HandleApiConnectionFailureAsync - OpenSpec: refactor-startup-connection-resilience
    // [已删除] GetApiEndpoint - OpenSpec: refactor-startup-connection-resilience

    /// <summary>注册启动步骤到管道</summary>
    private void RegisterStartupSteps()
    {
        // 从DI容器解析并注册所有启动步骤
        var steps = new[]
        {
            Container.Resolve<IStartupStep>("ErrorHandling"),
            Container.Resolve<IStartupStep>("ModuleCoordinator"),
            Container.Resolve<IStartupStep>("CoreServices"),
            // API健康检查 - 直接创建实例以使用特定超时配置（5秒）
            new ApiHealthCheckStartupStep(
                Container.Resolve<IApplicationStateService>(),
                Container.Resolve<ILogger<ApiHealthCheckStartupStep>>(),
                timeoutSeconds: 5),
            Container.Resolve<IStartupStep>("Warmup")
        };

        foreach (var step in steps)
        {
            _startupPipeline!.RegisterStep(step);
        }
    }

    /// <summary>订阅管道事件</summary>
    private void SubscribeToPipelineEvents()
    {
        _startupPipeline!.StepCompleted += (_, e) =>
        {
            _performanceMonitor?.EndStage();
            if (e.CompletedCount < e.TotalCount)
            {
                _performanceMonitor?.StartStage(e.StepName);
            }

            var logger = Container.Resolve<ILogger<App>>();
            if (e.Result.Success)
            {
                logger.LogInformation("启动步骤 {StepName} 完成，耗时 {Duration}ms",
                    e.StepName, e.Result.Duration.TotalMilliseconds);
            }
            else
            {
                logger.LogWarning("启动步骤 {StepName} 失败: {Error}",
                    e.StepName, e.Result.ErrorMessage);
            }
        };
    }

    /// <summary>完成启动，显示主窗口</summary>
    private async Task ShowMainWindowAfterInitializationAsync()
    {
        await Dispatcher.InvokeAsync(() =>
        {
            _performanceMonitor?.EndStage();
            _performanceMonitor?.Finish();

            // Phase 4 Task 4.4: 记录启动性能指标并输出报告
            try
            {
                var performanceMonitor = Container.Resolve<IPerformanceMonitor>();
                performanceMonitor.StopTiming("App_Startup_Total");
                performanceMonitor.RecordMemoryBaseline("App_Startup_Complete_Memory");

                var report = performanceMonitor.GenerateReport();
                var logger = Container.Resolve<ILogger<App>>();
                logger.LogInformation("应用程序启动性能报告:\n{PerformanceReport}", report.GetFormattedReport());
            }
            catch (Exception ex)
            {
                // 性能监控不应影响正常启动流程
                var logger = Container.Resolve<ILogger<App>>();
                logger.LogWarning(ex, "生成性能报告时发生错误");
            }

            _splashScreen?.Close();
            _splashScreen = null;
            MainWindow?.Show();
        });
    }

    /// <summary>处理初始化失败</summary>
    private async Task HandleInitializationFailureAsync(Exception ex)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            _performanceMonitor?.Finish();
            _splashScreen?.Close();

            var logger = Container.Resolve<ILogger<App>>();
            logger.LogCritical(ex, "应用初始化失败");

            var errorMessage = BuildInitializationErrorMessage(ex);
            var result = System.Windows.MessageBox.Show(errorMessage, "凌隐宝堂 - 初始化失败",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Error);

            if (result == System.Windows.MessageBoxResult.Yes)
                TryOpenLogFolder();

            Application.Current.Shutdown(1);
        });
    }

    /// <summary>构建初始化错误消息</summary>
    private static string BuildInitializationErrorMessage(Exception ex) =>
        $"应用初始化失败，无法继续运行。\n\n错误类型：{ex.GetType().Name}\n错误信息：{ex.Message}\n\n" +
        "可能原因：\n1. WebAPI服务未启动（检查 http://localhost:5001）\n2. 数据库连接失败\n3. 配置文件错误\n\n是否查看详细日志？";

    /// <summary>尝试打开日志文件夹</summary>
    private static void TryOpenLogFolder()
    {
        try { System.Diagnostics.Process.Start("explorer.exe", System.IO.Path.Combine(AppContext.BaseDirectory, "logs")); }
        catch { }
    }


    /// <summary>安全执行释放操作，捕获异常确保后续清理继续</summary>
    /// <remarks>OpenSpec: implement-single-instance-mode - 资源释放保护</remarks>
    private static void SafeDispose(Action disposeAction, string resourceName)
    {
        try
        {
            disposeAction();
            Log.Debug("已释放资源: {ResourceName}", resourceName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "释放资源失败: {ResourceName}", resourceName);
        }
    }

    /// <summary>配置模块目录 - 基于角色的智能模块加载策略</summary>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        ArgumentNullException.ThrowIfNull(moduleCatalog, nameof(moduleCatalog));

        // 核心模块 - 立即加载
        moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<UsersModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<ClinicalModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<AdminModule>(InitializationMode.WhenAvailable);

        // 业务模块 - 医案流程依赖链
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.WhenAvailable);
        // [已删除] ConsultationModule - 功能已迁移到MedicalCase模块的ConsultationItem（Entity→DTO→Item模式）
        // [已删除] PrescriptionsModule - 空壳模块已移除，功能已迁移到MedicalCase
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.WhenAvailable);

        // PRD: registration.md - 挂号管理模块
        moduleCatalog.AddModule<RegistrationModule>(InitializationMode.WhenAvailable);

        // OpenSpec: integrate-cardreader-module - 身份证读卡模块
        moduleCatalog.AddModule<CardReaderModule>(InitializationMode.WhenAvailable);

        // OpenSpec: implement-data-sync - 数据同步模块
        moduleCatalog.AddModule<SyncModule>(InitializationMode.WhenAvailable);

        base.ConfigureModuleCatalog(moduleCatalog);
    }

    /// <summary>用户登录后的角色驱动模块加载</summary>
    public async Task LoadRoleBasedModulesAsync(string userRole)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userRole, nameof(userRole));

        if (_bootstrapper == null)
            throw new InvalidOperationException("应用程序启动引导服务未初始化");

        if (Enum.TryParse<UserRole>(userRole, out var role))
            await _bootstrapper.LoadModulesForRoleAsync(role);
        else
            throw new ArgumentException($"无效的用户角色: {userRole}");
    }

    /// <summary>设置控制台编码为UTF-8（必须在Serilog初始化前调用）</summary>
    private static void SetConsoleEncoding()
    {
        if (HasConsole())
        {
            try
            {
                // 设置控制台代码页为UTF-8 (65001)
                SetConsoleOutputCP(65001);
                SetConsoleCP(65001);
                System.Console.OutputEncoding = System.Text.Encoding.UTF8;
                System.Console.InputEncoding = System.Text.Encoding.UTF8;
            }
            catch (System.IO.IOException)
            {
                // 忽略：某些环境可能不支持更改控制台编码
            }
        }
    }

    /// <summary>检查是否有可用的控制台窗口（使用Windows API避免异常）</summary>
    private static bool HasConsole()
    {
        return GetConsoleWindow() != IntPtr.Zero;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetConsoleCP(uint wCodePageID);
}
