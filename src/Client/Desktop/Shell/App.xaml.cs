using System.Windows;
using LYBT.Desktop.Auth;
using LYBT.Desktop.AdminWorkstation;
using LYBT.Desktop.ClinicalWorkstation;
using LYBT.Desktop.Consultation;
using LYBT.Desktop.Services.Performance;
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
// TODO: Issue #815 Phase 3 - 恢复Workbench引用
// using LYBT.Desktop.Workbench.Medical;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Enums;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Regions;

namespace LYBT.Desktop.Shell;

/// <summary>
/// 应用程序主入�?- WPF应用程序核心启动�?
/// 采用UltraThink架构标准，使用C# 12现代化特�?
/// 提供智能模块加载、角色驱动初始化和企业级错误处理
/// 集成Prism.DryIoc容器管理，支�?个业务模块的统一协调
/// 优化启动性能，提供角色基础的模块按需加载策略
/// 适配小型诊所部署环境，确保系统快速启动和稳定运行
/// </summary>
public partial class App : PrismApplication
{
    private IApplicationBootstrapper? _bootstrapper;

    /// <summary>
    /// 创建应用程序主窗�?
    /// 从DI容器中解析MainWindow实例
    /// 注：这是Prism框架的标准做法，此处使用Container.Resolve是必需�?
    /// </summary>
    /// <returns>应用程序主窗体实�?/returns>
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    /// <summary>
    /// 注册应用程序类型和服�?
    /// 使用扩展方法统一注册所有业务模块的服务和依�?
    /// </summary>
    /// <param name="containerRegistry">DI容器注册�?/param>
    /// <exception cref="ArgumentNullException">当容器注册器�?null 时抛�?/exception>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry, nameof(containerRegistry));

        // 注册启动引导服务（替代原有的直接Container.Resolve调用�?
        containerRegistry.RegisterSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
        
        // 注册应用初始化服�?
        containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService, 
            LYBT.Desktop.Shell.Services.ApplicationInitializationService>();

        // 使用扩展方法统一注册所有服�?
        containerRegistry.RegisterAllServices();

        // 显式配置ViewModelLocator映射
        ConfigureViewModelLocator();
    }

    /// <summary>
    /// 配置ViewModel定位�?
    /// 显式注册View和ViewModel的映射关系，确保依赖注入正确工作
    /// </summary>
    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();

        // Prism 8.x最佳实践：直接使用容器解析，无需工厂方法
        // Prism 8.x最佳实践：使用类型映射避免Container.Resolve
        // 通过泛型重载让框架自动解析依赖，而不是手动调用容�?
        ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
        ViewModelLocationProvider.Register<HomeView, HomeViewModel>();

        // Note: 其他View-ViewModel映射通过Prism自动发现机制处理
    }

    /// <summary>
    /// 应用程序初始化完成后的回�?
    /// 使用注入的ApplicationBootstrapper服务，避免Service Locator反模�?
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 使用注入的启动引导服务（避免Container.Resolve�?
        try
        {
            // 获取启动引导服务
            // 注：此处Container.Resolve是可接受的，因为�?
            // 1. 位于组合�?App.xaml.cs)
            // 2. OnInitialized是重写方法，无法使用构造函数注�?
            // 3. 仅在应用启动时调用一�?
            _bootstrapper = Container.Resolve<IApplicationBootstrapper>();
            
            // 初始化错误处理（同步操作�?
            _bootstrapper.InitializeErrorHandlingService();
            
            // 初始化模块协调器
            _bootstrapper.InitializeSimplifiedModuleCoordinator();
            
            // 异步初始化核心服�?
            _ = Task.Run(async () => 
            {
                await _bootstrapper.InitializeCoreServicesAsync();
                await _bootstrapper.InitializeApplicationWarmupAsync();
            });
        }
        catch (Exception ex)
        {
            // 降级处理：如果初始化服务未正确注册，记录错误但继续启�?
            System.Diagnostics.Debug.WriteLine($"应用初始化失�? {ex.Message}");
            System.Windows.MessageBox.Show(
                $"应用初始化失�? {ex.Message}", 
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
    /// <exception cref="ArgumentNullException">当模块目录为 null 时抛�?/exception>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        ArgumentNullException.ThrowIfNull(moduleCatalog, nameof(moduleCatalog));

        // ========== 核心模块 - 立即加载 ==========
        // 认证模块 - 所有功能的基础
        moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);
        
        // 用户模块 - 基础权限管理
        moduleCatalog.AddModule<UsersModule>(InitializationMode.WhenAvailable);

        // ========== 基础业务模块 - 登录后加�?==========
        // 患者管�?- 多数业务的基础
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.OnDemand);

        // ========== 功能模块 - 按需加载 ==========
        // 药材管理 - 独立功能，可延迟加载
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.OnDemand);
        
        // 方剂管理 - 依赖药材
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);
        
        // 诊疗管理 - 依赖患�?
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.OnDemand);
        
        // 病历管理 - 复杂依赖
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.OnDemand);
        
        // 处方管理 - 最复杂依赖
        moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.OnDemand);

        // ========== 工作台模�?- 用户触发加载 ==========
        // 诊疗工作�?- 顶层集成模块
        // TODO: Issue #815 Phase 3 - 恢复诊疗工作台模�?
        // moduleCatalog.AddModule<MedicalWorkbenchModule>(InitializationMode.OnDemand);
        
        // 管理工作�?- 管理员角色使�?
        moduleCatalog.AddModule<AdminWorkstation.AdminWorkstationModule>(InitializationMode.OnDemand);
        
        // 诊疗工作�?- 医生角色使用
        moduleCatalog.AddModule<ClinicalWorkstation.ClinicalWorkstationModule>(InitializationMode.OnDemand);

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
    /// 添加基于角色的智能模块配�?
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
    /// 根据用户角色智能加载所需模块，避免不必要的资源消�?
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

            // 将字符串角色转换为枚�?
            if (Enum.TryParse<UserRole>(userRole, out var role))
            {
                await _bootstrapper.LoadModulesForRoleAsync(role);
            }
            else
            {
                throw new ArgumentException($"无效的用户角�? {userRole}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"角色驱动模块加载异常: {ex.Message}");
            throw;
        }
    }
}
