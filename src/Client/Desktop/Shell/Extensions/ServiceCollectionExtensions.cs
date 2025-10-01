using System.Net.Http;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Services.Modules;
using LYBT.Desktop.Services.Performance;
using LYBT.Desktop.Services.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
    ///    - 工作台服务：IWorkstationRouter, IMainWindowServicesFacade
    ///
    /// 2. Scoped（作用域） 按需创建，同一作用域内复用
    ///    - 业务服务：PatientService, HerbService, FormulaService
    ///    - API客户端：IAuthApi, IUserApi, IPatientApi�?
    ///    - 流程服务：MedicalCaseService, ConsultationService
    ///    - 聚合服务：PrescriptionsService（依赖多个服务）
    ///
    /// 3. Transient（瞬态）- 每次请求创建新实�?
    ///    - 处理器：AuthHeaderHandler
    ///    - 临时对象：对话框、临时处理器
    ///
    /// 分层注册策略（避免循环依赖）�?
    /// - Layer 1: 基础设施（无依赖�?
    /// - Layer 2: 认证模块（依赖Layer 1�?
    /// - Layer 3: 业务数据（依赖Layer 2�?
    /// - Layer 4: 流程协调（依赖Layer 3�?
    /// - Layer 5: 聚合服务（依赖Layer 4�?
    ///
    /// 重构�?025-01-23 UltraThink架构优化
    /// - 业务Module重命名为Service避免与Prism IModule混淆
    /// - 添加集中式NavigationService解决导航分散问题
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// 注册所有服�?
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
            RegisterCoreServices(containerRegistry); // Issue #837: 添加缺失的核心服务注册
            RegisterErrorHandlingServices(containerRegistry);
            RegisterDialogs(containerRegistry);
            RegisterPerformanceServices(containerRegistry);
            RegisterUltraThinkServices(containerRegistry);
            RegisterNavigationServices(containerRegistry); // Phase 2: NavigationJournal支持
            RegisterCommandServices(containerRegistry); // Phase 3: CompositeCommand全局命令
            RegisterModuleServicesManually(containerRegistry); // 简化：直接使用手动注册

            // ViewModels和Views通过Prism的ViewModelLocator自动解析，无需手动注册
        }

        /// <summary>
        /// 注册启动引导相关服务（避免Service Locator反模式）
        /// </summary>
        private static void RegisterBootstrapServices(IContainerRegistry containerRegistry)
        {
            // Issue #838: 注册 IConfiguration - 必须在最前面,因为其他服务依赖它
            // WPF Prism 不会自动注册 IConfiguration,需要手动创建和注册
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            containerRegistry.RegisterInstance<Microsoft.Extensions.Configuration.IConfiguration>(configuration);

            // Issue #840: 注册用户通知服务
            // MainWindowViewModel 使用 IUserNotificationService 进行简单消息提示
            // 系统级错误处理由 UnifiedErrorHandlingService 负责
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService,
                LYBT.Desktop.Infrastructure.Services.UserNotificationService>();

            // Issue #841: 注册通知服务 - 必须在 UnifiedErrorHandlingService 之前
            // UnifiedErrorHandlingService 依赖 INotificationService,因此必须先注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Notifications.INotificationService,
                LYBT.Desktop.Services.Notifications.NotificationService>();

            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.ErrorHandling.IErrorHandlingService,
                LYBT.Desktop.Services.ErrorHandling.UnifiedErrorHandlingService>();

            // 注册启动优化服务
            containerRegistry.RegisterSingleton<IStartupOptimizationService,
                StartupOptimizationService>();

            // Issue #841 Fix #2: 注册应用程序初始化服务 - 必须在所有依赖项之后
            // ApplicationInitializationService 依赖 IErrorHandlingService 和 IStartupOptimizationService
            // 因此必须在它们之后注册,确保 DI 容器可以正确解析依赖链
            containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService,
                LYBT.Desktop.Shell.Services.ApplicationInitializationService>();
        }

        /// <summary>
        /// 注册UltraThink高级服务
        /// </summary>
        private static void RegisterUltraThinkServices(IContainerRegistry containerRegistry)
        {
            // Phase I: 简化主题服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Theming.IThemeService,
                LYBT.Desktop.Services.Theming.ThemeService>();

            // Note: IStartupOptimizationService 实际在 RegisterBootstrapServices 中注册（lines 104-105）
            // RegisterPerformanceServices 当前为空实现，未来可能扩展性能监控服务
        }

        /// <summary>
        /// 注册导航服务 - Phase 2: NavigationJournal支持
        /// </summary>
        private static void RegisterNavigationServices(IContainerRegistry containerRegistry)
        {
        }

        /// <summary>
        /// 注册全局命令和模块管理服�?
        /// </summary>
        private static void RegisterCommandServices(IContainerRegistry containerRegistry)
        {
            // 注册全局命令系统（Phase 3�?
            containerRegistry.RegisterSingleton<IApplicationCommands,
                ApplicationCommands>();

            // 注册模块加载服务（Phase 3�?
            containerRegistry.RegisterSingleton<IModuleLoadingService,
                ModuleLoadingService>();
        }

        /// <summary>
        /// 注册统一错误处理服务 - UltraThink简化版 + DT-006优化
        /// </summary>
        private static void RegisterErrorHandlingServices(IContainerRegistry containerRegistry)
        {
            // 注册统一错误处理器 - 使用 Infrastructure 层的实现
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Services.IStandardErrorHandler,
                LYBT.Desktop.Infrastructure.Services.StandardErrorHandler>();
        }

        /// <summary>
        /// 注册日志服务
        /// </summary>
        private static void RegisterLogging(IContainerRegistry containerRegistry)
        {
            // 注册简单的控制台日志提供程�?
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
                // 注册各模块的MappingProfile
                cfg.AddProfile(new LYBT.Desktop.Herbs.Mappings.MappingProfile());
                cfg.AddProfile(new LYBT.Desktop.Auth.Mappings.MappingProfile());
                // TODO: 其他模块的MappingProfile需要在此添加
            });

            var mapper = mapperConfig.CreateMapper();
            containerRegistry.RegisterInstance<IMapper>(mapper);
        }

        /// <summary>
        /// 注册缓存服务 - 优化缓存配置，避免重复注�?
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
                    ExpirationScanFrequency = TimeSpan.FromMinutes(5) // �?分钟清理过期�?
                };
                return new MemoryCache(options);
            });
        }

        /// <summary>
        /// 注册HTTP相关服务
        /// </summary>
        private static void RegisterHttpServices(IContainerRegistry containerRegistry)
        {
            // Issue #835: 注册 IHttpClientFactory 用于 AuthService
            // Prism/DryIoc 需要使用 RegisterInstance 来注册 HttpClient
            containerRegistry.RegisterSingleton<IHttpClientFactory>(() =>
            {
                return new SimpleHttpClientFactory();
            });

            // 兼容性:保留单例 HttpClient 供其他旧代码使用
            containerRegistry.RegisterSingleton<HttpClient>(() =>
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:5001") // 从配置读取
                };
                return client;
            });
        }

        /// <summary>
        /// 简单的 HttpClientFactory 实现 - MVP版本
        /// 生产环境可升级为 Microsoft.Extensions.Http
        /// </summary>
        private class SimpleHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                return new HttpClient();
            }
        }

        /// <summary>
        /// 注册API服务 - UltraThink统一API客户端管理器
        /// </summary>
        private static void RegisterApiServices(IContainerRegistry containerRegistry)
        {
            // 注册通用API服务 - 使用 Core_New 的实现
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Http.IApiService,
                LYBT.Desktop.Services.Http.ApiService>();
        }

        /// <summary>
        /// DT-003优化: 分层模块服务注册 - 按依赖层级防止循环依�?
        /// 基于依赖分析结果�?层注册策略，确保服务解析顺序正确
        /// </summary>
        private static void RegisterModuleServicesManually(IContainerRegistry containerRegistry)
        {
            // Layer 1: 基础�?- 无外部依赖的基础模块（优先注册）
            RegisterLayer1BasicModules(containerRegistry);

            // Layer 2: 认证�?- 依赖基础�?
            RegisterLayer2AuthModules(containerRegistry);

            // Layer 3: 业务数据�?- 依赖认证�?
            RegisterLayer3BusinessDataModules(containerRegistry);

            // Layer 4: 流程协调�?- 依赖业务数据�?
            RegisterLayer4ProcessModules(containerRegistry);

            // Layer 5: 聚合服务�?- 依赖流程协调�?
            RegisterLayer5AggregationModules(containerRegistry);
        }

        /// <summary>
        /// Layer 1: 基础模块注册 - Herbs, Formula (无外部依�?
        /// 性能优化：改为Scoped注册，避免启动时立即实例�?
        /// </summary>
        private static void RegisterLayer1BasicModules(IContainerRegistry containerRegistry)
        {
        }

        /// <summary>
        /// Layer 2: 认证模块注册 - 依赖基础�?
        /// </summary>
        private static void RegisterLayer2AuthModules(IContainerRegistry containerRegistry)
        {
        }

        /// <summary>
        /// Layer 3: 业务数据模块注册 - 依赖认证�?
        /// 性能优化：改为Scoped注册，避免启动时立即实例�?
        /// </summary>
        private static void RegisterLayer3BusinessDataModules(IContainerRegistry containerRegistry)
        {
        }

        /// <summary>
        /// Layer 4: 流程协调模块注册 - 依赖业务数据�?
        /// 性能优化：改为Scoped注册，避免启动时立即实例�?
        /// </summary>
        private static void RegisterLayer4ProcessModules(IContainerRegistry containerRegistry)
        {
        }

        /// <summary>
        /// Layer 5: 聚合服务模块注册 - 依赖流程协调�?
        /// 性能优化：改为Scoped注册，避免启动时立即实例�?
        /// </summary>
        private static void RegisterLayer5AggregationModules(IContainerRegistry containerRegistry)
        {
        }

        /// <summary>
        /// 注册业务服务 - UltraThink架构 with Repository Pattern
        /// </summary>
        private static void RegisterBusinessServices(IContainerRegistry containerRegistry)
        {
            // 注册Repository层 - 使用 Core_New 的实现
            containerRegistry.RegisterScoped<IPatientRepository,
                LYBT.Desktop.Services.Repositories.PatientRepository>();
            containerRegistry.RegisterScoped<IUserRepository,
                LYBT.Desktop.Services.Repositories.UserRepository>();
            containerRegistry.RegisterScoped<IMedicalCaseRepository,
                LYBT.Desktop.Services.Repositories.MedicalCaseRepository>();
            containerRegistry.RegisterScoped<IPrescriptionRepository,
                LYBT.Desktop.Services.Repositories.PrescriptionRepository>();
            containerRegistry.RegisterScoped<IHerbRepository,
                LYBT.Desktop.Services.Repositories.HerbRepository>();
            containerRegistry.RegisterScoped<IFormulaRepository,
                LYBT.Desktop.Services.Repositories.FormulaRepository>();
            containerRegistry.RegisterScoped<IConsultationRepository,
                LYBT.Desktop.Services.Repositories.ConsultationRepository>();

            // Issue #835: 注册认证服务(使用 Shared.Interfaces)
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IAuthService,
                LYBT.Desktop.Services.Business.AuthService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Business.ITokenStorageService,
                LYBT.Desktop.Services.Business.TokenStorageService>();

            // Issue #835: 注册 IAuthenticationService 适配器(供 MainWindowViewModel 使用)
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Auth.IAuthenticationService,
                LYBT.Desktop.Services.Auth.AuthenticationService>();

            // Issue #842: 注册业务服务(使用 Shared.Interfaces)
            containerRegistry.RegisterScoped<LYBT.Shared.Interfaces.Services.IPatientService,
                LYBT.Desktop.Services.Business.PatientService>();
            containerRegistry.RegisterScoped<LYBT.Shared.Interfaces.Services.IUserService,
                LYBT.Desktop.Services.Business.UserService>();
            containerRegistry.RegisterScoped<LYBT.Shared.Interfaces.Services.IMedicalCaseService,
                LYBT.Desktop.Services.Business.MedicalCaseService>();
            containerRegistry.RegisterScoped<LYBT.Shared.Interfaces.Services.IPrescriptionService,
                LYBT.Desktop.Services.Business.PrescriptionService>();
            containerRegistry.RegisterScoped<LYBT.Shared.Interfaces.Services.IHerbService,
                LYBT.Desktop.Services.Business.HerbService>();
            containerRegistry.RegisterScoped<LYBT.Shared.Interfaces.Services.IFormulaService,
                LYBT.Desktop.Services.Business.FormulaService>();
            containerRegistry.RegisterScoped<LYBT.Shared.Interfaces.Services.IConsultationService,
                LYBT.Desktop.Services.Business.ConsultationService>();
        }

        /// <summary>
        /// 注册核心基础服务（非业务模块服务�?
        /// </summary>
        private static void RegisterCoreServices(IContainerRegistry containerRegistry)
        {
            // 主窗口服务门面 - 简化MainWindowViewModel的依赖注入
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IMainWindowServicesFacade,
                LYBT.Desktop.Infrastructure.Services.MainWindowServicesFacade>();

            // P7-03: 处方打印服务 - UltraThink标准打印系统
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Services.IPrescriptionPrintService,
                LYBT.Desktop.Infrastructure.Services.PrescriptionPrintService>();

            // P7-04: 用户体验优化服务 - UltraThink用户体验增强
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Services.IUserExperienceService,
                LYBT.Desktop.Infrastructure.Services.UserExperienceService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Services.IKeyboardShortcutService,
                LYBT.Desktop.Infrastructure.Services.KeyboardShortcutService>();
        }

        /// <summary>
        /// 注册领域业务服务
        /// </summary>
        private static void RegisterDomainServices(IContainerRegistry containerRegistry)
        {
            // 注意�?个业务模�?Auth/Users/Patients/Herbs/Formula/Consultation/Prescriptions/MedicalCase)
            // 现在通过自动发现系统统一注册，无需在各自的XxxModule.RegisterTypes中重复注�?
            // 这消除了双重注册风险，简化了模块开�?
        }

        /// <summary>
        /// 注册对话框服�?
        /// </summary>
        private static void RegisterDialogs(IContainerRegistry containerRegistry)
        {
            // Phase 3.4: 所有 Dialog 现在使用 Prism Dialog System
            // SimplifiedDialogService 和 ICustomDialogService 已删除
            // 各模块通过 containerRegistry.RegisterDialog&lt;TView, TViewModel&gt;() 注册
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
        // 所有API客户端现由UnifiedApiClientManager统一管理，提供更好的一致性和可维护�?
        #endregion 辅助方法
    }

}
