using System.Windows;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Shell.Services;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Consultation;
using LYBT.Desktop.Formula;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Prescriptions;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.ViewModels;
using LYBT.Desktop.Shell.Views;
using LYBT.Desktop.Users;
using LYBT.Shared.Models.Enums;
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
        containerRegistry.Register<HomeViewModel>();
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
        ViewModelLocationProvider.Register<HomeView, HomeViewModel>();

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
    /// </summary>
    private async Task InitializeApplicationAsync()
    {
        try
        {
            // 解析启动引导服务
            _bootstrapper = Container.Resolve<IApplicationBootstrapper>();

            // Phase 1: 错误处理初始化
            _performanceMonitor?.EndStage();
            _performanceMonitor?.StartStage("错误处理初始化");
            _splashScreen?.UpdateStatus("正在初始化错误处理...");
            _bootstrapper.InitializeErrorHandlingService();

            // Phase 2: 模块协调器初始化
            _performanceMonitor?.EndStage();
            _performanceMonitor?.StartStage("模块协调器初始化");
            _splashScreen?.UpdateStatus("正在初始化模块协调器...");
            _bootstrapper.InitializeSimplifiedModuleCoordinator();

            // Phase 3: 核心服务初始化（必须成功）
            _performanceMonitor?.EndStage();
            _performanceMonitor?.StartStage("核心服务初始化");
            _splashScreen?.UpdateStatus("正在初始化核心服务...");
            await _bootstrapper.InitializeCoreServicesAsync();

            // Phase 4: 应用预热
            _performanceMonitor?.EndStage();
            _performanceMonitor?.StartStage("应用预热");
            _splashScreen?.UpdateStatus("正在预热应用程序...");
            await _bootstrapper.InitializeApplicationWarmupAsync();

            // Phase 5: 完成启动，显示主窗口
            await Dispatcher.InvokeAsync(() =>
            {
                _performanceMonitor?.EndStage();
                _performanceMonitor?.Finish();

                _splashScreen?.Close();
                _splashScreen = null;

                MainWindow?.Show();
            });
        }
        catch (Exception ex)
        {
            // ✅ Fail-Fast: 显示错误对话框，终止应用
            await Dispatcher.InvokeAsync(() =>
            {
                _performanceMonitor?.Finish();
                _splashScreen?.Close();

                var logger = Container.Resolve<ILogger<App>>();
                logger.LogCritical(ex, "应用初始化失败");

                var result = System.Windows.MessageBox.Show(
                    "应用初始化失败，无法继续运行。\n\n" +
                    $"错误类型：{ex.GetType().Name}\n" +
                    $"错误信息：{ex.Message}\n\n" +
                    "可能原因：\n" +
                    "1. WebAPI服务未启动（检查 http://localhost:5001）\n" +
                    "2. 数据库连接失败\n" +
                    "3. 配置文件错误\n\n" +
                    "是否查看详细日志？",
                    "凌隐宝堂 - 初始化失败",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Error);

                if (result == System.Windows.MessageBoxResult.Yes)
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

                Application.Current.Shutdown(1);
            });
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

        // ========== 基础业务模块 - 登录后加载 ==========
        // 患者管理 - 多数业务的基础
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.OnDemand);

        // ========== 功能模块 - 按需加载 ==========
        // 药材管理 - 独立功能,可延迟加载
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.OnDemand);

        // 方剂管理 - 依赖药材
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);

        // 诊疗管理 - 依赖患者
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.OnDemand);

        // 病历管理 - 复杂依赖
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.OnDemand);

        // 处方管理 - 最复杂依赖
        moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.OnDemand);

        // ========== 工作台模块 - 用户触发加载 ==========

        // 管理工作台 - 管理员角色使用
        moduleCatalog.AddModule<AdminWorkstation.AdminWorkstationModule>(InitializationMode.OnDemand);

        // 诊疗工作台 - 医生角色使用
        moduleCatalog.AddModule<ClinicalWorkstation.ClinicalWorkstationModule>(InitializationMode.OnDemand);

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
