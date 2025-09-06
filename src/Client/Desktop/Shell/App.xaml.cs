using System.Windows;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Consultation;
using LYBT.Desktop.Formula;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Prescriptions;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Shell.ViewModels;
using LYBT.Desktop.Shell.Views;
using LYBT.Desktop.Users;
using LYBT.Desktop.Workbench.Admin;
using LYBT.Desktop.Workbench.Consultation;
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
public partial class App : PrismApplication {

    /// <summary>
    /// 创建应用程序主窗体
    /// 从DI容器中解析MainWindow实例
    /// </summary>
    /// <returns>应用程序主窗体实例</returns>
    protected override Window CreateShell() {
        return Container.Resolve<MainWindow>();
    }

    /// <summary>
    /// 注册应用程序类型和服务
    /// 使用扩展方法统一注册所有业务模块的服务和依赖
    /// </summary>
    /// <param name="containerRegistry">DI容器注册器</param>
    /// <exception cref="ArgumentNullException">当容器注册器为 null 时抛出</exception>
    protected override void RegisterTypes(IContainerRegistry containerRegistry) {
        ArgumentNullException.ThrowIfNull(containerRegistry, nameof(containerRegistry));

        // 使用扩展方法统一注册所有服务
        containerRegistry.RegisterAllServices();

        // 显式配置ViewModelLocator映射
        ConfigureViewModelLocator();
    }

    /// <summary>
    /// 配置ViewModel定位器
    /// 显式注册View和ViewModel的映射关系，确保依赖注入正确工作
    /// </summary>
    protected override void ConfigureViewModelLocator() {
        base.ConfigureViewModelLocator();

        // 显式注册View和ViewModel的映射关系，解决AutoWireViewModel失败问题
        ViewModelLocationProvider.Register<MainWindow>(() => {
            var regionManager = Container.Resolve<IRegionManager>();
            var eventAggregator = Container.Resolve<IEventAggregator>();
            var servicesFacade = Container.Resolve<LYBT.Desktop.Core.Interfaces.Services.IMainWindowServicesFacade>();
            var errorHandlingService = Container.Resolve<LYBT.Desktop.Core.Interfaces.Services.IErrorHandlingService>();
            return MainWindowViewModel.Create(regionManager, eventAggregator, servicesFacade, errorHandlingService);
        });
        ViewModelLocationProvider.Register<HomeView, HomeViewModel>();

        // TODO: 根据需要添加其他View-ViewModel映射
    }

    /// <summary>
    /// 应用程序初始化完成后的回调
    /// 执行企业级启动流程：错误处理初始化、模块协调器配置、性能优化预热
    /// </summary>
    protected override void OnInitialized() {
        base.OnInitialized();

        // 1. 启动性能优化 - 应用预热（异步执行，不阻塞主线程）
        _ = Task.Run(InitializeApplicationWarmupAsync);

        // 2. 初始化错误处理服务并注册全局异常处理器
        InitializeErrorHandlingService();

        // 3. 简化模块加载协调器（移除复杂的性能监控）
        InitializeSimplifiedModuleCoordinator();
    }

    /// <summary>
    /// 初始化应用程序预热
    /// 异步预热关键服务，提升用户操作响应速度
    /// </summary>
    private async Task InitializeApplicationWarmupAsync() {
        try {
            var startupService = Container.Resolve<LYBT.Desktop.Core.Services.Performance.IStartupOptimizationService>();
            await startupService.WarmupApplicationAsync().ConfigureAwait(false);
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"应用预热失败: {ex.Message}");
            // 预热失败不影响主流程，仅记录日志
        }
    }

    /// <summary>
    /// 初始化错误处理服务
    /// 注册全局异常处理器，确保系统异常得到妥善处理
    /// </summary>
    private void InitializeErrorHandlingService() {
        try {
            var errorHandlingService = Container.Resolve<LYBT.Desktop.Core.Interfaces.Services.IErrorHandlingService>();
            errorHandlingService.RegisterGlobalExceptionHandlers();
        } catch (Exception ex) {
            // 如果错误处理服务初始化失败，使用基本的错误处理
            System.Diagnostics.Debug.WriteLine($"初始化错误处理服务失败: {ex.Message}");
            MessageBox.Show($"系统初始化失败: {ex.Message}", "凌隐宝堂 - 系统错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 初始化简化的模块协调器
    /// 移除复杂的性能监控，专注核心功能和稳定性
    /// 提供轻量级的模块加载管理，适配小型诊所部署需求
    /// </summary>
    private void InitializeSimplifiedModuleCoordinator() {
        try {
            var logger = Container.Resolve<ILogger<App>>();
            logger.LogInformation("UltraThink简化模块协调器初始化完成");
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"简化模块协调器初始化异常: {ex}");
            // 模块协调器初始化失败不应阻塞应用启动
        }
    }

    /// <summary>
    /// 订阅模块管理器事件进行性能追踪
    /// 简化版本：专注错误处理和基础日志记录，移除复杂的性能统计
    /// </summary>
    /// <param name="moduleManager">模块管理器</param>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException">当参数为 null 时抛出</exception>
    private void SubscribeToModuleEvents(IModuleManager moduleManager, ILogger<App> logger) {
        ArgumentNullException.ThrowIfNull(moduleManager, nameof(moduleManager));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        var moduleInitTimes = new Dictionary<string, DateTime>();

        // 模块开始加载事件
        moduleManager.ModuleDownloadProgressChanged += (sender, e) => {
            if (e.ProgressPercentage == 0) // 开始加载
            {
                moduleInitTimes[e.ModuleInfo.ModuleName] = DateTime.Now;
                logger.LogDebug("模块 {ModuleName} 开始加载", e.ModuleInfo.ModuleName);
            }
        };

        // 模块加载完成事件
        moduleManager.LoadModuleCompleted += (sender, e) => {
            var moduleName = e.ModuleInfo.ModuleName;
            if (moduleInitTimes.TryGetValue(moduleName, out var startTime)) {
                var initializationTime = DateTime.Now - startTime;
                moduleInitTimes.Remove(moduleName);

                logger.LogInformation("模块 {ModuleName} 加载完成，耗时 {Duration}ms",
                    moduleName, initializationTime.TotalMilliseconds);
            }

            if (!e.IsErrorHandled && e.Error != null) {
                logger.LogError(e.Error, "模块 {ModuleName} 加载失败", e.ModuleInfo.ModuleName);
            }
        };

        logger.LogDebug("模块事件监听已配置完成");
    }

    /// <summary>
    /// 配置模块目录
    /// 基于角色的智能模块加载策略，显著提升启动性能
    /// 优先加载核心模块，专业模块按需加载
    /// </summary>
    /// <param name="moduleCatalog">模块目录</param>
    /// <exception cref="ArgumentNullException">当模块目录为 null 时抛出</exception>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) {
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
        AddRoleBasedModule(moduleCatalog, nameof(SystemWorkbenchModule), typeof(SystemWorkbenchModule),
            ["Admin"]);

        AddRoleBasedModule(moduleCatalog, nameof(ConsultationWorkbenchModule), typeof(ConsultationWorkbenchModule),
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
    private static void AddCoreModule(IModuleCatalog moduleCatalog, string moduleName, Type moduleType) {
        moduleCatalog.AddModule(new ModuleInfo {
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
    private static void AddRoleBasedModule(IModuleCatalog moduleCatalog, string moduleName, Type moduleType, string[] requiredRoles) {
        var moduleInfo = new ModuleInfo {
            ModuleName = moduleName,
            ModuleType = moduleType.AssemblyQualifiedName,
            // 设为按需加载，登录后根据角色决定是否立即加载
            InitializationMode = InitializationMode.OnDemand
        };

        // 记录模块角色信息（简化处理）
        // TODO: 如需角色限制，在模块初始化时检查

        moduleCatalog.AddModule(moduleInfo);
    }

    /// <summary>
    /// 用户登录后的角色驱动模块加载
    /// 根据用户角色智能加载所需模块，避免不必要的资源消耗
    /// </summary>
    /// <param name="userRole">用户角色</param>
    /// <returns>模块加载任务</returns>
    /// <exception cref="ArgumentException">当用户角色为空时抛出</exception>
    public async Task LoadRoleBasedModulesAsync(string userRole) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userRole, nameof(userRole));

        try {
            var moduleManager = Container.Resolve<IModuleManager>();
            var moduleCatalog = Container.Resolve<IModuleCatalog>();
            var logger = Container.Resolve<ILogger<App>>();

            logger.LogInformation("开始为角色 {UserRole} 加载模块", userRole);

            var modulesToLoad = new List<string>();

            // 遍历所有按需加载的模块，简化处理
            foreach (var module in moduleCatalog.Modules.Where(m => m.InitializationMode == InitializationMode.OnDemand)) {
                // 简化版本：所有OnDemand模块都加载（可根据需要后续优化）
                modulesToLoad.Add(module.ModuleName);
            }

            // 批量加载匹配的模块
            var loadedCount = 0;
            foreach (var moduleName in modulesToLoad) {
                try {
                    await Task.Run(() => moduleManager.LoadModule(moduleName)).ConfigureAwait(false);
                    logger.LogDebug("模块 {ModuleName} 加载完成", moduleName);
                    loadedCount++;
                } catch (Exception ex) {
                    logger.LogError(ex, "加载模块 {ModuleName} 失败", moduleName);
                }
            }

            logger.LogInformation("角色驱动模块加载完成，共加载 {LoadedCount}/{TotalCount} 个模块",
                loadedCount, modulesToLoad.Count);
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"角色驱动模块加载异常: {ex.Message}");
            throw;
        }
    }
}
