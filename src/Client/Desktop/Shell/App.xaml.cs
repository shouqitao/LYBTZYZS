using System.Windows;
using LYBT.Desktop.Admin;    // Issue #1553: 管理员角色模块
using LYBT.Desktop.Auth;
using LYBT.Desktop.Clinical; // Issue #1553: 医生角色模块
using LYBT.Desktop.Consultation;
using LYBT.Desktop.Formula;
using LYBT.Desktop.Foundation.Application; // Issue #1823: IApplicationStateService
using LYBT.Desktop.Foundation.Security;   // Issue #1865: Token清理逻辑
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

/// <summary>
/// 应用程序主入口 - WPF应用程序核心启动器
/// 采用UltraThink架构标准,使用C# 12现代化特性
/// 提供智能模块加载、角色驱动初始化和企业级错误处理
/// 集成Prism.DryIoc容器管理,支持7个业务模块的统一协调
/// 优化启动性能,提供角色基础的模块按需加载策略
/// 适配小型诊所部署环境,确保系统快速启动和稳定运行
/// </summary>
public partial class App : PrismApplication
{
    private IApplicationBootstrapper? _bootstrapper;
    private StartupPerformanceMonitor? _performanceMonitor;
    private SplashScreenWindow? _splashScreen;

    /// <summary>
    /// 应用程序启动入口
    /// Issue #1239: 修复 Prism 生命周期 - 同步调用 base.OnStartup
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. 立即显示 Splash Screen（同步）
        _splashScreen = new SplashScreenWindow();
        _splashScreen.Show();
        _splashScreen.UpdateStatus("正在初始化应用程序...");

        // 2. ✅ 同步调用 base.OnStartup（触发 Prism 生命周期）
        // Prism 会依次调用：CreateShell → InitializeShell → OnInitialized
        base.OnStartup(e);
    }

    /// <summary>
    /// 创建应用程序主窗体
    /// Issue #1221: 不自动显示，由 OnInitialized 在启动完成后显示
    /// </summary>
    protected override Window CreateShell()
    {
        var mainWindow = Container.Resolve<MainWindow>();
        return mainWindow;
    }

    /// <summary>
    /// 初始化主窗口
    /// Issue #1221: 调用 base 初始化，但先不显示窗口
    /// </summary>
    protected override void InitializeShell(Window shell)
    {
        // 调用 base 以完成 Prism 的初始化（包括 DataContext 设置）
        base.InitializeShell(shell);

        // 但立即隐藏窗口，让 Splash Screen 先显示
        // 主窗口将在 OnInitialized 的异步任务完成后显示
        shell.Hide();
    }

    /// <summary>
    /// 注册应用程序类型和服务
    /// 使用扩展方法统一注册所有业务模块的服务和依赖
    /// </summary>
    /// <param name="containerRegistry">DI容器注册器</param>
    /// <exception cref="ArgumentNullException">当容器注册器为 null 时抛出</exception>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry, nameof(containerRegistry));

        // 注册启动引导服务(替代原有的直接Container.Resolve调用)
        containerRegistry.RegisterSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();

        // 注册应用初始化服务
        containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService,
            LYBT.Desktop.Shell.Services.ApplicationInitializationService>();

        // 使用扩展方法统一注册所有服务
        containerRegistry.RegisterAllServices();

        // Issue #1239 修复: 显式注册 ViewModels（Prism 8.x 要求）
        // ViewModelLocationProvider 只是映射关系，ViewModel 本身需要在容器中注册
        containerRegistry.Register<MainWindowViewModel>();  // Transient lifetime for ViewModels

        // Epic #1676 Phase 2: 注册全局对话框
        containerRegistry.RegisterDialog<Dialogs.Views.ConfirmationDialog,
            Dialogs.ViewModels.ConfirmationDialogViewModel>();
    }

    /// <summary>
    /// 配置ViewModel定位器
    /// 显式注册View和ViewModel的映射关系,确保依赖注入正确工作
    /// </summary>
    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();

        // Prism 8.x最佳实践:直接使用容器解析,无需工厂方法
        // Prism 8.x最佳实践:使用类型映射避免Container.Resolve
        // 通过泛型重载让框架自动解析依赖,而不是手动调用容器
        ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();

        // Note: 其他View-ViewModel映射通过Prism自动发现机制处理
    }

    /// <summary>
    /// 应用程序初始化完成后的回调
    /// Issue #1239: 在 Prism 生命周期中执行异步初始化
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 初始化性能监控
        _performanceMonitor = new StartupPerformanceMonitor(Container.Resolve<ILoggerFactory>());
        _performanceMonitor.StartMonitoring();
        _performanceMonitor.StartStage("应用初始化");

        // 设置控制台编码为UTF-8,解决Visual Studio输出窗口中文日志乱码问题 (Issue #993)
        try
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch (System.IO.IOException)
        {
            // 无控制台窗口时忽略
        }

        // ✅ 在 Prism 生命周期中执行异步初始化
        _ = InitializeApplicationAsync();
    }

    /// <summary>
    /// 异步初始化应用程序
    /// Issue #1239: 实现 Fail-Fast 错误处理
    /// Issue #1795: 优化复杂方法，从84行拆分为25+6个辅助方法
    /// </summary>
    private async Task InitializeApplicationAsync()
    {
        try
        {
            // 解析启动引导服务
            _bootstrapper = Container.Resolve<IApplicationBootstrapper>();

            // Issue #1795: 提取初始化阶段方法
            InitializeErrorHandling();
            InitializeModuleCoordinator();
            await InitializeCoreServicesAsync();
            await InitializeApplicationWarmupAsync();

            // Issue #1795: 提取显示主窗口方法
            await ShowMainWindowAfterInitializationAsync();
        }
        catch (Exception ex)
        {
            // Issue #1795: 提取异常处理方法
            await HandleInitializationFailureAsync(ex);
        }
    }

    /// <summary>
    /// 错误处理初始化（Issue #1795：提取方法）
    /// </summary>
    private void InitializeErrorHandling()
    {
        _performanceMonitor?.EndStage();
        _performanceMonitor?.StartStage("错误处理初始化");
        _splashScreen?.UpdateStatus("正在初始化错误处理...");
        _bootstrapper!.InitializeErrorHandlingService();
    }

    /// <summary>
    /// 模块协调器初始化（Issue #1795：提取方法）
    /// </summary>
    private void InitializeModuleCoordinator()
    {
        _performanceMonitor?.EndStage();
        _performanceMonitor?.StartStage("模块协调器初始化");
        _splashScreen?.UpdateStatus("正在初始化模块协调器...");
        _bootstrapper!.InitializeSimplifiedModuleCoordinator();
    }

    /// <summary>
    /// 核心服务初始化（Issue #1795：提取方法）
    /// </summary>
    private async Task InitializeCoreServicesAsync()
    {
        _performanceMonitor?.EndStage();
        _performanceMonitor?.StartStage("核心服务初始化");
        _splashScreen?.UpdateStatus("正在初始化核心服务...");
        await _bootstrapper!.InitializeCoreServicesAsync();

        // Issue #1823: API健康检查前置 - 避免登录界面延迟
        _splashScreen?.UpdateStatus("正在检查API连接...");
        var appStateService = Container.Resolve<IApplicationStateService>();
        await appStateService.CheckApiHealthAsync(timeoutSeconds: 10);
    }

    /// <summary>
    /// 应用预热（Issue #1795：提取方法）
    /// </summary>
    private async Task InitializeApplicationWarmupAsync()
    {
        _performanceMonitor?.EndStage();
        _performanceMonitor?.StartStage("应用预热");
        _splashScreen?.UpdateStatus("正在预热应用程序...");
        await _bootstrapper!.InitializeApplicationWarmupAsync();
    }

    /// <summary>
    /// 完成启动，显示主窗口（Issue #1795：提取方法）
    /// </summary>
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

    /// <summary>
    /// 处理初始化失败（Issue #1795：提取方法）
    /// </summary>
    private async Task HandleInitializationFailureAsync(Exception ex)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            _performanceMonitor?.Finish();
            _splashScreen?.Close();

            var logger = Container.Resolve<ILogger<App>>();
            logger.LogCritical(ex, "应用初始化失败");

            var errorMessage = BuildInitializationErrorMessage(ex);
            var result = System.Windows.MessageBox.Show(
                errorMessage,
                "凌隐宝堂 - 初始化失败",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Error);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                TryOpenLogFolder();
            }

            Application.Current.Shutdown(1);
        });
    }

    /// <summary>
    /// 构建初始化错误消息（Issue #1795：提取方法）
    /// </summary>
    private string BuildInitializationErrorMessage(Exception ex)
    {
        return "应用初始化失败，无法继续运行。\n\n" +
               $"错误类型：{ex.GetType().Name}\n" +
               $"错误信息：{ex.Message}\n\n" +
               "可能原因：\n" +
               "1. WebAPI服务未启动（检查 http://localhost:5001）\n" +
               "2. 数据库连接失败\n" +
               "3. 配置文件错误\n\n" +
               "是否查看详细日志？";
    }

    /// <summary>
    /// 尝试打开日志文件夹（Issue #1795：提取方法）
    /// </summary>
    private void TryOpenLogFolder()
    {
        try
        {
            var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
            System.Diagnostics.Process.Start("explorer.exe", logPath);
        }
        catch
        {
            // 忽略打开日志文件夹的错误
        }
    }

    /// <summary>
    /// 配置模块目录
    /// 基于角色的智能模块加载策略,显著提升启动性能
    /// 优先加载核心模块,专业模块按需加载
    /// </summary>
    /// <param name="moduleCatalog">模块目录</param>
    /// <exception cref="ArgumentNullException">当模块目录为 null 时抛出</exception>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        ArgumentNullException.ThrowIfNull(moduleCatalog, nameof(moduleCatalog));

        // ========== 核心模块 - 立即加载 ==========
        // 认证模块 - 所有功能的基础
        moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);

        // 用户模块 - 基础权限管理
        moduleCatalog.AddModule<UsersModule>(InitializationMode.WhenAvailable);

        // Issue #1553: 角色主页模块 - 登录后立即需要
        moduleCatalog.AddModule<ClinicalModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<AdminModule>(InitializationMode.WhenAvailable);

        // ========== 基础业务模块 - 医案流程依赖链（Issue #1564）==========
        // 患者管理 - 医案流程Step 1依赖
        // Issue #1564: 改为WhenAvailable，因为MedicalCaseModule依赖此模块（Step 1患者选择）
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.WhenAvailable);

        // ========== 功能模块 - 医案流程依赖链（Issue #1564）==========
        // 药材管理 - 处方模块依赖
        // Issue #1564: 改为WhenAvailable，因为PrescriptionsModule依赖此模块
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.WhenAvailable);

        // 方剂管理 - 依赖药材，处方模块依赖
        // Issue #1564: 改为WhenAvailable，因为PrescriptionsModule依赖此模块
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.WhenAvailable);

        // 诊疗管理 - 依赖患者，处方模块依赖
        // Issue #1564: 改为WhenAvailable，因为PrescriptionsModule依赖此模块
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.WhenAvailable);

        // 处方管理 - 医案流程Step 3依赖
        // Issue #1564: 改为WhenAvailable，因为MedicalCaseModule（WhenAvailable）依赖此模块
        moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.WhenAvailable);

        // 病历管理 - 核心医疗流程（Epic #1494），启动时加载以支持"开始接诊"功能
        // Issue #1564: 依赖Prescriptions/Consultation/Herbs/Formula模块，确保依赖链完整
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.WhenAvailable);

        base.ConfigureModuleCatalog(moduleCatalog);
    }

    /// <summary>
    /// 用户登录后的角色驱动模块加载
    /// 根据用户角色智能加载所需模块,避免不必要的资源消耗
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <returns>模块加载任务</returns>
    /// <exception cref="ArgumentException">当用户角色为空时抛出</exception>
    public async Task LoadRoleBasedModulesAsync(string userRole)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userRole, nameof(userRole));

        try
        {
            // 确保启动引导服务已初始化
            if (_bootstrapper == null)
            {
                throw new InvalidOperationException("应用程序启动引导服务未初始化");
            }

            // 将字符串角色转换为枚举
            if (Enum.TryParse<UserRole>(userRole, out var role))
            {
                await _bootstrapper.LoadModulesForRoleAsync(role);
            }
            else
            {
                throw new ArgumentException($"无效的用户角色: {userRole}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"角色驱动模块加载异常: {ex.Message}");
            throw;
        }
    }
}
