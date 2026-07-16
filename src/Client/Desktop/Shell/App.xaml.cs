using System.Windows;
using LYBT.Desktop.Admin;
using LYBT.Desktop.Receptionist;
using LYBT.Desktop.Auth;
using LYBT.Desktop.CardReader;
using LYBT.Desktop.Clinical;
using LYBT.Desktop.Formula;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.Infrastructure.Logging;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Registration;
using LYBT.Desktop.Reports;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.ViewModels;
using LYBT.Desktop.Shell.Views;
using LYBT.Desktop.Sysadmin;
using LYBT.Desktop.Users;
using LYBT.Shared.Models.Enums;
using MaterialDesignThemes.Wpf;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Serilog;

namespace LYBT.Desktop.Shell;

/// <summary>应用程序主入口 - WPF应用程序核心启动器，提供智能模块加载和角色驱动初始化</summary>
public partial class App : PrismApplication
{
    private static Mutex? _instanceMutex;
    private const string MutexName = "Global\\LYBTZYZS_Shell_Instance";
    private const string MainWindowTitle = "凌隐宝堂中医诊所管理系统";

    /// <summary>应用程序启动入口</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        if (!TryAcquireSingleInstance())
        {
            NativeMethods.ActivateExistingWindow(MainWindowTitle);
            Shutdown();
            return;
        }

        SetConsoleEncoding();
        DesktopSerilogConfiguration.Initialize();
        Log.Information("应用程序启动");

        base.OnStartup(e);
    }

    /// <summary>尝试获取单实例锁</summary>
    /// <returns>true表示当前是唯一实例，false表示已有实例运行</returns>
    private static bool TryAcquireSingleInstance()
    {
        _instanceMutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            _instanceMutex.Dispose();
            _instanceMutex = null;
            return false;
        }
        return true;
    }

    /// <summary>应用程序退出</summary>
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("应用程序退出");
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        _instanceMutex = null;
        DesktopSerilogConfiguration.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>创建应用程序主窗体</summary>
    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    /// <summary>初始化主窗口</summary>
    protected override void InitializeShell(Window shell)
    {
        // Don't hide — MainWindow shows immediately with login screen.
        // Startup pipeline runs in background via AppStartupOrchestrator.
    }

    /// <summary>注册应用程序类型和服务</summary>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry, nameof(containerRegistry));

        containerRegistry.RegisterSingleton<AppStartupOrchestrator>();
        containerRegistry.RegisterSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
        containerRegistry.RegisterAllServices();
        containerRegistry.Register<Views.MainWindow>();
        containerRegistry.Register<MainWindowViewModel>();
        containerRegistry.RegisterDialog<Dialogs.Views.ConfirmationDialog, Dialogs.ViewModels.ConfirmationDialogViewModel>();
        containerRegistry.RegisterDialog<Dialogs.Views.MessageDialog, Dialogs.ViewModels.MessageDialogViewModel>();
        containerRegistry.RegisterDialog<Dialogs.Views.InputDialog, Dialogs.ViewModels.InputDialogViewModel>();
        containerRegistry.RegisterDialog<LYBT.Desktop.Infrastructure.Views.UnfinishedCaseDialog,
            LYBT.Desktop.Infrastructure.ViewModels.UnfinishedCaseDialogViewModel>();
        containerRegistry.Register<ViewModels.AccountSettingsViewModel>();
        containerRegistry.RegisterForNavigation<Views.AccountSettingsView>();

        // MaterialDesignThemes ISnackbarMessageQueue registration
        containerRegistry.RegisterSingleton<MaterialDesignThemes.Wpf.ISnackbarMessageQueue, MaterialDesignThemes.Wpf.SnackbarMessageQueue>();

        containerRegistry.RegisterSingleton<IThemeService>(resolver =>
        {
            var configuration = resolver.Resolve<Microsoft.Extensions.Configuration.IConfiguration>();
            return new ThemeService(configuration);
        });
        // Infrastructure registrations: available for future callers, not yet injected anywhere.
        containerRegistry.RegisterSingleton<ISnackbarService, SnackbarService>();
        containerRegistry.RegisterSingleton<IDialogHostService, DialogHostService>();
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
        MainWindow?.Show();
        _ = Container.Resolve<AppStartupOrchestrator>().RunStartupAsync();
    }

    /// <summary>配置模块目录 - 基于角色的智能模块加载策略</summary>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        ArgumentNullException.ThrowIfNull(moduleCatalog, nameof(moduleCatalog));

        // 核心模块 - 立即加载
        moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);
        // UsersModule 列为业务模块，按需加载（由 NavigationCoordinator 在首次导航时触发）
        moduleCatalog.AddModule<UsersModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<ClinicalModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<AdminModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<ReceptionistModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<SysadminModule>(InitializationMode.WhenAvailable);

        // 业务模块 - 按需加载（首次导航到该模块视图时由 NavigationCoordinator 触发）
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.OnDemand);

        // PRD: registration.md - 挂号管理模块
        moduleCatalog.AddModule<RegistrationModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<CardReaderModule>(InitializationMode.OnDemand);

        // 统计报表模块
        moduleCatalog.AddModule<ReportsModule>(InitializationMode.OnDemand);

        base.ConfigureModuleCatalog(moduleCatalog);
    }

    /// <summary>用户登录后的角色驱动模块加载</summary>
    public async Task LoadRoleBasedModulesAsync(string userRole)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userRole, nameof(userRole));

        var bootstrapper = Container.Resolve<IApplicationBootstrapper>();
        if (Enum.TryParse<UserRole>(userRole, out var role))
            await bootstrapper.LoadModulesForRoleAsync(role);
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
