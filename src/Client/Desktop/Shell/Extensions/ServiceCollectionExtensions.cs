using System.Net.Http;
using AutoMapper;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Core.Mapping;
using LYBT.Desktop.Infrastructure;
using LYBT.Desktop.Services;
using LYBT.Desktop.Services.Handlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Extensions
{

    /// <summary>
    /// 服务注册扩展方法
    ///
    /// 服务生命周期管理策略（Prism 8.1.97 + DryIoc）：
    ///
    /// 1. Singleton（单例）- 全应用程序生命周期
    ///    - 基础设施服务：ILoggerFactory, IMemoryCache, HttpClient, IThemeService
    ///    - 认证服务：AuthService, UserService（保持会话状态）
    ///    - 系统服务：IPermissionService, IUserSessionManager, INavigationService
    ///    - 工作台服务：IWorkbenchRouter, IMainWindowServicesFacade
    ///
    /// 2. Scoped（作用域）- 按需创建，同一作用域内复用
    ///    - 业务服务：PatientService, HerbService, FormulaService
    ///    - API客户端：IAuthApi, IUserApi, IPatientApi等
    ///    - 流程服务：MedicalCaseService, ConsultationService
    ///    - 聚合服务：PrescriptionsService（依赖多个服务）
    ///
    /// 3. Transient（瞬态）- 每次请求创建新实例
    ///    - 处理器：AuthHeaderHandler
    ///    - 临时对象：对话框、临时处理器
    ///
    /// 分层注册策略（避免循环依赖）：
    /// - Layer 1: 基础设施（无依赖）
    /// - Layer 2: 认证模块（依赖Layer 1）
    /// - Layer 3: 业务数据（依赖Layer 2）
    /// - Layer 4: 流程协调（依赖Layer 3）
    /// - Layer 5: 聚合服务（依赖Layer 4）
    ///
    /// 重构：2025-01-23 UltraThink架构优化
    /// - 业务Module重命名为Service避免与Prism IModule混淆
    /// - 添加集中式NavigationService解决导航分散问题
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// 注册所有服务
        /// </summary>
        public static void RegisterAllServices(this IContainerRegistry containerRegistry)
        {
            // 注册应用程序启动引导服务（避免Service Locator反模式）
            RegisterBootstrapServices(containerRegistry);
            
            RegisterLogging(containerRegistry);
            RegisterAutoMapper(containerRegistry);
            RegisterCacheServices(containerRegistry);
            RegisterHttpServices(containerRegistry);
            RegisterApiServices(containerRegistry);
            RegisterBusinessServices(containerRegistry);
            RegisterErrorHandlingServices(containerRegistry);
            RegisterDialogs(containerRegistry);
            RegisterPerformanceServices(containerRegistry);
            RegisterUltraThinkServices(containerRegistry);
            RegisterModuleServicesManually(containerRegistry); // 简化：直接使用手动注册

            // ViewModels和Views通过Prism的ViewModelLocator自动解析，无需手动注册
        }

        /// <summary>
        /// 注册启动引导相关服务（避免Service Locator反模式）
        /// </summary>
        private static void RegisterBootstrapServices(IContainerRegistry containerRegistry)
        {
            // 注册应用程序初始化服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService,
                LYBT.Desktop.Shell.Services.ApplicationInitializationService>();
            
            // 注册错误处理服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IErrorHandlingService,
                LYBT.Desktop.Core.Services.ErrorHandling.UnifiedErrorHandlingService>();
            
            // 注册启动优化服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IStartupOptimizationService,
                LYBT.Desktop.Core.Services.Performance.StartupOptimizationService>();
        }

        /// <summary>
        /// 注册UltraThink高级服务
        /// </summary>
        private static void RegisterUltraThinkServices(IContainerRegistry containerRegistry)
        {
            // Phase I: 简化主题服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Theming.IThemeService,
                LYBT.Desktop.Core.Services.Theming.ThemeService>();

            // UltraThink Phase H: 高级功能优化服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IStartupOptimizationService,
                LYBT.Desktop.Core.Services.Performance.StartupOptimizationService>();

            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Settings.IUserPreferencesService,
                LYBT.Desktop.Core.Services.Settings.UserPreferencesService>();
        }

        /// <summary>
        /// 注册统一错误处理服务 - UltraThink简化版 + DT-006优化
        /// </summary>
        private static void RegisterErrorHandlingServices(IContainerRegistry containerRegistry)
        {
            // 注册统一错误处理器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Services.IStandardErrorHandler,
                LYBT.Desktop.Infrastructure.Services.StandardErrorHandler>();

            // DT-006: 统一异常处理服务注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Exceptions.IExceptionHandler,
                LYBT.Desktop.Core.Services.Exceptions.StandardExceptionHandler>();
        }

        /// <summary>
        /// 注册日志服务
        /// </summary>
        private static void RegisterLogging(IContainerRegistry containerRegistry)
        {
            // 注册简单的控制台日志提供程序
            containerRegistry.RegisterSingleton<ILoggerFactory>(() =>
            {
                return LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));
            });

            // 注册泛型日志接口
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
        }

        /// <summary>
        /// 注册AutoMapper
        /// </summary>
        private static void RegisterAutoMapper(IContainerRegistry containerRegistry)
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            });

            var mapper = mapperConfig.CreateMapper();
            containerRegistry.RegisterInstance<IMapper>(mapper);
        }

        /// <summary>
        /// 注册缓存服务 - 优化缓存配置，避免重复注册
        /// </summary>
        private static void RegisterCacheServices(IContainerRegistry containerRegistry)
        {
            // 优化内存缓存服务配置，添加性能监控选项
            containerRegistry.RegisterSingleton<IMemoryCache>(() =>
            {
                var options = new MemoryCacheOptions
                {
                    SizeLimit = 1000, // 优化：设置合理的缓存大小限制
                    CompactionPercentage = 0.25, // 当达到限制时压缩25%
                    ExpirationScanFrequency = TimeSpan.FromMinutes(5) // 每5分钟清理过期项
                };
                return new MemoryCache(options);
            });
        }

        /// <summary>
        /// 注册HTTP相关服务
        /// </summary>
        private static void RegisterHttpServices(IContainerRegistry containerRegistry)
        {
            // 注册基础HttpClient
            containerRegistry.RegisterSingleton<HttpClient>(() =>
            {
                return HttpClientFactory.CreateBasicClient(ApiConfiguration.BaseUrl);
            });
        }

        /// <summary>
        /// 注册API服务 - UltraThink统一API客户端管理器
        /// </summary>
        private static void RegisterApiServices(IContainerRegistry containerRegistry)
        {
            // 注册认证处理器
            containerRegistry.Register<AuthHeaderHandler>();

            // 注册统一API客户端管理器 - 替代原有8个独立API客户端
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager,
                LYBT.Desktop.Infrastructure.Api.UnifiedApiClientManager>();

            // 优化API接口注册：使用单例模式避免重复解析，消除循环依赖风险
            // 使用延迟解析模式，只在首次访问时解析UnifiedApiClientManager
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IAuthApi>(container =>
            {
                var manager = container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>();
                return manager.AuthApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IUserApi>(container =>
            {
                var manager = container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>();
                return manager.UserApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IPatientApi>(container =>
            {
                var manager = container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>();
                return manager.PatientApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IHerbApi>(container =>
            {
                var manager = container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>();
                return manager.HerbApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IFormulaApi>(container =>
            {
                var manager = container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>();
                return manager.FormulaApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IConsultationApi>(container =>
            {
                var manager = container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>();
                return manager.ConsultationApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IPrescriptionApi>(container =>
            {
                var manager = container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>();
                return manager.PrescriptionApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IMedicalCaseApi>(container =>
            {
                var manager = container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>();
                return manager.MedicalCaseApi;
            });

            // 注册通用API服务 - UltraThink统一架构：使用完整版Http.ApiService
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Http.IApiService, LYBT.Desktop.Core.Http.ApiService>();
        }

        /// <summary>
        /// DT-003优化: 分层模块服务注册 - 按依赖层级防止循环依赖
        /// 基于依赖分析结果的5层注册策略，确保服务解析顺序正确
        /// </summary>
        private static void RegisterModuleServicesManually(IContainerRegistry containerRegistry)
        {
            // Layer 1: 基础层 - 无外部依赖的基础模块（优先注册）
            RegisterLayer1BasicModules(containerRegistry);

            // Layer 2: 认证层 - 依赖基础层
            RegisterLayer2AuthModules(containerRegistry);

            // Layer 3: 业务数据层 - 依赖认证层
            RegisterLayer3BusinessDataModules(containerRegistry);

            // Layer 4: 流程协调层 - 依赖业务数据层
            RegisterLayer4ProcessModules(containerRegistry);

            // Layer 5: 聚合服务层 - 依赖流程协调层
            RegisterLayer5AggregationModules(containerRegistry);
        }

        /// <summary>
        /// Layer 1: 基础模块注册 - Herbs, Formula (无外部依赖)
        /// 性能优化：改为Scoped注册，避免启动时立即实例化
        /// </summary>
        private static void RegisterLayer1BasicModules(IContainerRegistry containerRegistry)
        {
            // Herbs模块 - 基础药材数据，无外部依赖
            // 改为Scoped以支持懒加载，提升启动性能
            containerRegistry.Register<LYBT.Desktop.Herbs.Services.HerbService>();
            containerRegistry.Register<LYBT.Shared.Interfaces.Services.IHerbService,
                LYBT.Desktop.Herbs.Services.HerbService>();

            // Formula模块 - 验方模板数据，无外部依赖
            // 改为Scoped以支持懒加载，提升启动性能
            containerRegistry.Register<LYBT.Desktop.Formula.Services.FormulaService>();
            containerRegistry.Register<LYBT.Shared.Interfaces.Services.IFormulaService,
                LYBT.Desktop.Formula.Services.FormulaService>();
        }

        /// <summary>
        /// Layer 2: 认证模块注册 - 依赖基础层
        /// </summary>
        private static void RegisterLayer2AuthModules(IContainerRegistry containerRegistry)
        {
            // Auth模块 - DT-001/DT-002修复: 适配器模式 + 标准IoC注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Auth.Services.AuthService>();
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IAuthService,
                LYBT.Desktop.Auth.Services.AuthService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService,
                LYBT.Desktop.Auth.Services.AuthServiceAdapter>();

            // Users模块服务注册已移至UsersModule.RegisterTypes - Prism 8.x最佳实践
        }

        /// <summary>
        /// Layer 3: 业务数据模块注册 - 依赖认证层
        /// 性能优化：改为Scoped注册，避免启动时立即实例化
        /// </summary>
        private static void RegisterLayer3BusinessDataModules(IContainerRegistry containerRegistry)
        {
            // Patients模块 - 患者档案管理
            // 改为Scoped以支持懒加载，提升启动性能
            containerRegistry.Register<LYBT.Desktop.Patients.Services.PatientService>();
            containerRegistry.Register<LYBT.Shared.Interfaces.Services.IPatientService,
                LYBT.Desktop.Patients.Services.PatientService>();
        }

        /// <summary>
        /// Layer 4: 流程协调模块注册 - 依赖业务数据层
        /// 性能优化：改为Scoped注册，避免启动时立即实例化
        /// </summary>
        private static void RegisterLayer4ProcessModules(IContainerRegistry containerRegistry)
        {
            // MedicalCase模块 - 医案流程管理
            // Service Locator重构后，统一使用单一Service模式
            containerRegistry.Register<LYBT.Desktop.MedicalCase.Services.MedicalCaseService>();
            containerRegistry.Register<LYBT.Shared.Interfaces.Services.IMedicalCaseService,
                LYBT.Desktop.MedicalCase.Services.MedicalCaseService>();

            // Consultation模块 - 诊断流程
            // Service Locator重构后，统一使用单一Service模式
            containerRegistry.Register<LYBT.Desktop.Consultation.Services.ConsultationService>();
            containerRegistry.Register<LYBT.Shared.Interfaces.Services.IConsultationService,
                LYBT.Desktop.Consultation.Services.ConsultationService>();
        }

        /// <summary>
        /// Layer 5: 聚合服务模块注册 - 依赖流程协调层
        /// 性能优化：改为Scoped注册，避免启动时立即实例化
        /// </summary>
        private static void RegisterLayer5AggregationModules(IContainerRegistry containerRegistry)
        {
            // Prescriptions模块 - 处方聚合服务（依赖Herbs, Formula, Consultation）
            // Service Locator重构后，统一使用单一Service模式
            containerRegistry.Register<LYBT.Desktop.Prescriptions.Services.PrescriptionsService>();
            containerRegistry.Register<LYBT.Shared.Interfaces.Services.IPrescriptionService,
                LYBT.Desktop.Prescriptions.Services.PrescriptionsService>();
        }

        /// <summary>
        /// 注册业务服务
        /// </summary>
        private static void RegisterBusinessServices(IContainerRegistry containerRegistry)
        {
            RegisterCoreServices(containerRegistry);
            RegisterDomainServices(containerRegistry);
        }

        /// <summary>
        /// 注册核心基础服务（非业务模块服务）
        /// </summary>
        private static void RegisterCoreServices(IContainerRegistry containerRegistry)
        {
            // 注册集中式导航服务 - 解决导航逻辑分散问题
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Navigation.INavigationService,
                LYBT.Desktop.Core.Services.Navigation.NavigationService>();
            // 权限服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IPermissionService, PermissionService>();

            // 凭据服务 - 使用强化的 DPAPI 保护版本
            containerRegistry.RegisterSingleton<ICredentialService, SecureCredentialService>();

            // 统一会话管理服务 - Phase 2重构: 统一所有Session相关接口
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Session.UnifiedSessionManager>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Session.IUnifiedSessionManager, 
                LYBT.Desktop.Core.Services.Session.UnifiedSessionManager>();
            
            // 向后兼容性支持 - 映射到统一Session管理器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IUserSessionManager>(provider =>
                provider.Resolve<LYBT.Desktop.Core.Services.Session.IUnifiedSessionManager>() as LYBT.Desktop.Core.Interfaces.Services.IUserSessionManager
                ?? throw new InvalidOperationException("UnifiedSessionManager must implement IUserSessionManager"));
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.ITokenManager>(provider =>
                provider.Resolve<LYBT.Desktop.Core.Services.Session.IUnifiedSessionManager>() as LYBT.Desktop.Core.Interfaces.Services.ITokenManager
                ?? throw new InvalidOperationException("UnifiedSessionManager must implement ITokenManager"));
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.ISessionManager>(provider =>
                provider.Resolve<LYBT.Desktop.Core.Services.Session.IUnifiedSessionManager>() as LYBT.Desktop.Core.Interfaces.Services.ISessionManager
                ?? throw new InvalidOperationException("UnifiedSessionManager must implement ISessionManager"));
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.INotificationService,
                LYBT.Desktop.Core.Services.NotificationService>();

            // 注意：双层架构服务（QueryService/BusinessService）现在由自动发现系统处理
            // DT-002修复: 避免工厂委托，使用标准注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IErrorHandlingService,
                LYBT.Desktop.Core.Services.ErrorHandling.UnifiedErrorHandlingService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Workbench.Core.IWorkbenchRouter, LYBT.Desktop.Workbench.Core.WorkbenchRouter>();

            // 主窗口服务门面 - 简化MainWindowViewModel的依赖注入
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IMainWindowServicesFacade,
                LYBT.Desktop.Core.Services.MainWindowServicesFacade>();

            // P7-03: 处方打印服务 - UltraThink标准打印系统
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IPrescriptionPrintService,
                LYBT.Desktop.Core.Services.PrescriptionPrintService>();

            // P7-04: 用户体验优化服务 - UltraThink用户体验增强
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IUserExperienceService,
                LYBT.Desktop.Core.Services.UserExperienceService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IKeyboardShortcutService,
                LYBT.Desktop.Core.Services.KeyboardShortcutService>();
        }

        /// <summary>
        /// 注册领域业务服务
        /// </summary>
        private static void RegisterDomainServices(IContainerRegistry containerRegistry)
        {
            // 注意：8个业务模块(Auth/Users/Patients/Herbs/Formula/Consultation/Prescriptions/MedicalCase)
            // 现在通过自动发现系统统一注册，无需在各自的XxxModule.RegisterTypes中重复注册
            // 这消除了双重注册风险，简化了模块开发
        }

        /// <summary>
        /// 注册对话框服务
        /// </summary>
        private static void RegisterDialogs(IContainerRegistry containerRegistry)
        {
            // 精简对话框服务 - Phase 2重构：SimplifiedDialogService
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService,
                LYBT.Desktop.Core.Services.SimplifiedDialogService>();

            // 注册业务对话框（在服务启动后动态注册）
            containerRegistry.RegisterInstance<Action<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService>>(RegisterBusinessDialogs);
        }

        /// <summary>
        /// 注册业务对话框Views
        /// </summary>
        private static void RegisterBusinessDialogs(LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService dialogService)
        {
            // Phase 2简化：业务对话框使用约定优于配置，无需手动注册
            // 注释：对话框服务已简化，使用约定优于配置模式
        }

        /// <summary>
        /// 注册性能优化服务
        /// </summary>
        private static void RegisterPerformanceServices(IContainerRegistry containerRegistry)
        {
            // UltraThink清理：移除过度工程的ModuleLoadingCoordinator
            // 小诊所系统不需要复杂的模块加载协调功能
        }

        #region 辅助方法

        // UltraThink统一API客户端管理器已替代原有的独立API服务注册方式
        // 所有API客户端现由UnifiedApiClientManager统一管理，提供更好的一致性和可维护性
        #endregion 辅助方法
    }
}
