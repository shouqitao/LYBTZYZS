using System.Windows;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Consultation;
using LYBT.Desktop.Core.Services.Performance;
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
using LYBT.Desktop.Workbench.Medical;
using Microsoft.Extensions.Logging;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Regions;

namespace LYBT.Desktop.Shell;

/// <summary>
/// 应用程序主入口 - WPF应用程序核心启动器
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 提供智能模块加载、角色驱动初始化和企业级错误处理
/// 集成Prism.DryIoc容器管理，支持8个业务模块的统一协调
/// 优化启动性能，提供角色基础的模块按需加载策略
/// 适配小型诊所部署环境，确保系统快速启动和稳定运行
/// </summary>
public partial class App : PrismApplication
{
    private IApplicationBootstrapper? _bootstrapper;

    /// <summary>
    /// 创建应用程序主窗体
    /// 从DI容器中解析MainWindow实例
    /// 注：这是Prism框架的标准做法，此处使用Container.Resolve是必需的
    /// </summary>
    /// <returns>应用程序主窗体实例</returns>
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
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

        // 注册启动引导服务（替代原有的直接Container.Resolve调用）
        containerRegistry.RegisterSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
        
        // 注册应用初始化服务
        containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService, 
            LYBT.Desktop.Shell.Services.ApplicationInitializationService>();

        // 使用扩展方法统一注册所有服务
        containerRegistry.RegisterAllServices();

        // 显式配置ViewModelLocator映射
        ConfigureViewModelLocator();
    }

    /// <summary>
    /// 配置ViewModel定位器
    /// 显式注册View和ViewModel的映射关系，确保依赖注入正确工作
    /// </summary>
    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();

        // Prism 8.x最佳实践：直接使用容器解析，无需工厂方法
        ViewModelLocationProvider.Register<MainWindow>(() => Container.Resolve<MainWindowViewModel>());
        ViewModelLocationProvider.Register<HomeView, HomeViewModel>();

        // Note: 其他View-ViewModel映射通过Prism自动发现机制处理
    }

    /// <summary>
    /// 应用程序初始化完成后的回调
    /// 使用注入的ApplicationBootstrapper服务，避免Service Locator反模式
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 使用注入的启动引导服务（避免Container.Resolve）
        try
        {
            // 获取启动引导服务
            _bootstrapper = Container.Resolve<IApplicationBootstrapper>();
            
            // 初始化错误处理（同步操作）
            _bootstrapper.InitializeErrorHandlingService();
            
            // 初始化模块协调器
            _bootstrapper.InitializeSimplifiedModuleCoordinator();
            
            // 异步初始化核心服务
            _ = Task.Run(async () => 
            {
                await _bootstrapper.InitializeCoreServicesAsync();
                await _bootstrapper.InitializeApplicationWarmupAsync();
            });
        }
        catch (Exception ex)
        {
            // 降级处理：如果初始化服务未正确注册，记录错误但继续启动
            System.Diagnostics.Debug.WriteLine($"应用初始化失败: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"应用初始化失败: {ex.Message}", 
                "凌隐宝堂 - 系统错误",
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Error);
        }
    }









    /// <summary>
    /// 配置模块目录
    /// 基于角色的智能模块加载策略，显著提升启动性能
    /// 优先加载核心模块，专业模块按需加载
    /// </summary>
    /// <param name="moduleCatalog">模块目录</param>
    /// <exception cref="ArgumentNullException">当模块目录为 null 时抛出</exception>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        ArgumentNullException.ThrowIfNull(moduleCatalog, nameof(moduleCatalog));

        // 1. 核心必需模块（所有角色都需要）
        AddCoreModule(moduleCatalog, nameof(AuthenticationModule), typeof(AuthenticationModule));
        AddCoreModule(moduleCatalog, nameof(UsersModule), typeof(UsersModule));

        // 2. 基础业务模块（医疗相关角色必需）
        AddCoreModule(moduleCatalog, nameof(PatientsModule), typeof(PatientsModule));

        // 3. 专业功能模块（按需加载，提升启动速度）
        AddRoleBasedModule(moduleCatalog, nameof(ConsultationModule), typeof(ConsultationModule),
            ["Doctor", "Admin"]);

        AddRoleBasedModule(moduleCatalog, nameof(MedicalCaseModule), typeof(MedicalCaseModule),
            ["Doctor", "Admin"]);

        AddRoleBasedModule(moduleCatalog, nameof(HerbsModule), typeof(HerbsModule),
            ["Doctor", "Pharmacist", "Admin"]);

        AddRoleBasedModule(moduleCatalog, nameof(PrescriptionsModule), typeof(PrescriptionsModule),
            ["Doctor", "Pharmacist", "Admin"]);

        AddRoleBasedModule(moduleCatalog, nameof(FormulaModule), typeof(FormulaModule),
            ["Doctor", "Admin"]);

        // 4. 工作台模块（基于角色智能加载）
        // SystemWorkbenchModule已删除

        AddRoleBasedModule(moduleCatalog, nameof(MedicalWorkbenchModule), typeof(MedicalWorkbenchModule),
            ["Doctor", "Admin"]);

        base.ConfigureModuleCatalog(moduleCatalog);
    }

    /// <summary>
    /// 添加核心模块
    /// 核心模块在应用启动时立即加载
    /// </summary>
    /// <param name="moduleCatalog">模块目录</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="moduleType">模块类型</param>
    private static void AddCoreModule(IModuleCatalog moduleCatalog, string moduleName, Type moduleType)
    {
        moduleCatalog.AddModule(new ModuleInfo
        {
            ModuleName = moduleName,
            ModuleType = moduleType.AssemblyQualifiedName,
            InitializationMode = InitializationMode.WhenAvailable
        });
    }

    /// <summary>
    /// 添加基于角色的智能模块配置
    /// 根据用户角色决定模块加载时机，提升启动性能
    /// </summary>
    /// <param name="moduleCatalog">模块目录</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="moduleType">模块类型</param>
    /// <param name="requiredRoles">所需角色数组</param>
    private static void AddRoleBasedModule(IModuleCatalog moduleCatalog, string moduleName, Type moduleType, string[] requiredRoles)
    {
        var moduleInfo = new ModuleInfo
        {
            ModuleName = moduleName,
            ModuleType = moduleType.AssemblyQualifiedName,

            // 设为按需加载，登录后根据角色决定是否立即加载
            InitializationMode = InitializationMode.OnDemand
        };

        // 记录模块角色信息（简化处理，当前不限制角色访问）
        moduleCatalog.AddModule(moduleInfo);
    }

    /// <summary>
    /// 用户登录后的角色驱动模块加载
    /// 根据用户角色智能加载所需模块，避免不必要的资源消耗
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
            if (Enum.TryParse<LYBT.Shared.Models.Contracts.Users.UserRole>(userRole, out var role))
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
