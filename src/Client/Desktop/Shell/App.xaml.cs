using System.Windows;
using LYBT.Desktop.Admin;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Clinical;
using LYBT.Desktop.Consultation;
using LYBT.Desktop.Formula;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Prescriptions;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.ViewModels;
using LYBT.Desktop.Shell.Views;
using LYBT.Desktop.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Shell;

/// <summary>应用程序主入口 - WPF应用程序核心启动器，提供智能模块加载和角色驱动初始化</summary>
public partial class App : PrismApplication
{
    private IApplicationBootstrapper? _bootstrapper;
    private StartupPerformanceMonitor? _performanceMonitor;
    private SplashScreenWindow? _splashScreen;

    /// <summary>应用程序启动入口</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        _splashScreen = new SplashScreenWindow();
        _splashScreen.Show();
        _splashScreen.UpdateStatus("正在初始化应用程序...");
        base.OnStartup(e);
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

    /// <summary>异步初始化应用程序（Fail-Fast错误处理）</summary>
    private async Task InitializeApplicationAsync()
    {
        try
        {
            _bootstrapper = Container.Resolve<IApplicationBootstrapper>();
            InitializeErrorHandling();
            InitializeModuleCoordinator();
            await InitializeCoreServicesAsync();
            await InitializeApplicationWarmupAsync();
            await ShowMainWindowAfterInitializationAsync();
        }
        catch (Exception ex)
        {
            await HandleInitializationFailureAsync(ex);
        }
    }

    /// <summary>错误处理初始化</summary>
    private void InitializeErrorHandling()
    {
        _performanceMonitor?.EndStage();
        _performanceMonitor?.StartStage("错误处理初始化");
        _splashScreen?.UpdateStatus("正在初始化错误处理...");
        _bootstrapper!.InitializeErrorHandlingService();
    }

    /// <summary>模块协调器初始化</summary>
    private void InitializeModuleCoordinator()
    {
        _performanceMonitor?.EndStage();
        _performanceMonitor?.StartStage("模块协调器初始化");
        _splashScreen?.UpdateStatus("正在初始化模块协调器...");
        _bootstrapper!.InitializeSimplifiedModuleCoordinator();
    }

    /// <summary>核心服务初始化</summary>
    private async Task InitializeCoreServicesAsync()
    {
        _performanceMonitor?.EndStage();
        _performanceMonitor?.StartStage("核心服务初始化");
        _splashScreen?.UpdateStatus("正在初始化核心服务...");
        await _bootstrapper!.InitializeCoreServicesAsync();

        _splashScreen?.UpdateStatus("正在检查API连接...");
        var appStateService = Container.Resolve<IApplicationStateService>();
        await appStateService.CheckApiHealthAsync(timeoutSeconds: 10);
    }

    /// <summary>应用预热</summary>
    private async Task InitializeApplicationWarmupAsync()
    {
        _performanceMonitor?.EndStage();
        _performanceMonitor?.StartStage("应用预热");
        _splashScreen?.UpdateStatus("正在预热应用程序...");
        await _bootstrapper!.InitializeApplicationWarmupAsync();
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
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.WhenAvailable);
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

    /// <summary>检查是否有可用的控制台窗口</summary>
    private static bool HasConsole()
    {
        try { _ = System.Console.WindowHeight; return true; }
        catch { return false; }
    }
}
