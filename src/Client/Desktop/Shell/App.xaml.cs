using System.Windows;
using LYBT.Desktop.Admin;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Clinical;
using LYBT.Desktop.Contracts.Services;
// [已删除] using LYBT.Desktop.Consultation; - 模块已废弃，功能已迁移到MedicalCase模块
using LYBT.Desktop.Formula;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.Infrastructure.Logging;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Prescriptions;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
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

    /// <summary>应用程序启动入口</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // refactor-logging-system: 初始化Serilog日志系统
        DesktopSerilogConfiguration.Initialize();
        Log.Information("应用程序启动");

        _splashScreen = new SplashScreenWindow();
        _splashScreen.Show();
        _splashScreen.UpdateStatus("正在初始化应用程序...");
        base.OnStartup(e);
    }

    /// <summary>应用程序退出</summary>
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("应用程序退出");
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
        containerRegistry.RegisterDialog<Dialogs.Views.EntityAuditLogDialog, Dialogs.ViewModels.EntityAuditLogDialogViewModel>();
        containerRegistry.RegisterDialog<Dialogs.Views.ApiConnectionFailedDialog, Dialogs.ViewModels.ApiConnectionFailedDialogViewModel>();
    }

    /// <summary>配置ViewModel定位器</summary>
    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();
        ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
    }

    /// <summary>应用程序初始化完成后的回调</summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _performanceMonitor = new StartupPerformanceMonitor(Container.Resolve<ILoggerFactory>());
        _performanceMonitor.StartMonitoring();
        _performanceMonitor.StartStage("应用初始化");

        // 设置控制台编码为UTF-8（WPF应用默认无控制台，需先检查）
        if (HasConsole())
        {
            try { System.Console.OutputEncoding = System.Text.Encoding.UTF8; }
            catch (System.IO.IOException) { }
        }

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

            // enhance-shell-connection-dialog: 循环执行支持重试
            while (true)
            {
                var result = await _startupPipeline.ExecuteAsync(progress);

                if (result.Success)
                {
                    await ShowMainWindowAfterInitializationAsync();
                    return;
                }

                // API健康检查失败时显示恢复对话框
                if (result.FailedStepName == "API健康检查")
                {
                    // 获取失败步骤的详细异常信息
                    Exception? stepException = null;
                    if (result.StepResults.TryGetValue(result.FailedStepName, out var stepResult))
                    {
                        stepException = stepResult.Exception;
                    }

                    var action = await HandleApiConnectionFailureAsync(
                        result.ErrorMessage ?? "API服务不可用",
                        stepException);

                    switch (action)
                    {
                        case RecoveryAction.Retry:
                            // 重置管道状态，继续循环
                            _startupPipeline.Reset();
                            _splashScreen?.UpdateStatus("正在重试连接...");
                            continue;

                        case RecoveryAction.OfflineMode:
                            // v2.0: 启动离线模式
                            throw new NotImplementedException("离线模式将在v2.0实现");

                        case RecoveryAction.Exit:
                        default:
                            Application.Current.Shutdown(1);
                            return;
                    }
                }

                // 其他步骤失败，使用原有处理
                throw new InvalidOperationException(
                    $"启动步骤 '{result.FailedStepName}' 执行失败: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            await HandleInitializationFailureAsync(ex);
        }
    }

    /// <summary>处理API连接失败</summary>
    /// <remarks>enhance-shell-connection-dialog: 显示恢复对话框并返回用户选择的操作</remarks>
    private async Task<RecoveryAction> HandleApiConnectionFailureAsync(string errorMessage, Exception? exception)
    {
        // 临时隐藏启动画面，显示对话框
        await Dispatcher.InvokeAsync(() => _splashScreen?.Hide());

        try
        {
            var recoveryService = Container.Resolve<IApiConnectionRecoveryService>();
            var apiEndpoint = GetApiEndpoint();

            return await recoveryService.ShowConnectionFailedDialogAsync(
                errorMessage,
                exception,
                apiEndpoint);
        }
        finally
        {
            // 如果用户选择重试，重新显示启动画面
            await Dispatcher.InvokeAsync(() => _splashScreen?.Show());
        }
    }

    /// <summary>获取API端点地址</summary>
    private string GetApiEndpoint()
    {
        try
        {
            var apiOptions = Container.Resolve<ApiClientOptions>();
            return apiOptions.BaseUrl;
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>注册启动步骤到管道</summary>
    private void RegisterStartupSteps()
    {
        // 从DI容器解析并注册所有启动步骤
        var steps = new[]
        {
            Container.Resolve<IStartupStep>("ErrorHandling"),
            Container.Resolve<IStartupStep>("ModuleCoordinator"),
            Container.Resolve<IStartupStep>("CoreServices"),
            Container.Resolve<IStartupStep>("ApiHealthCheck"),
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
        // [已删除] ConsultationModule - 功能已迁移到MedicalCase模块的ConsultationPanelViewModel
        moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.WhenAvailable);

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

    /// <summary>检查是否有可用的控制台窗口（使用Windows API避免异常）</summary>
    private static bool HasConsole()
    {
        return GetConsoleWindow() != IntPtr.Zero;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();
}
