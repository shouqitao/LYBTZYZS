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

            // 注册应用程序初始化服�?
            containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService,
                LYBT.Desktop.Shell.Services.ApplicationInitializationService>();

            // Issue #837: 注册错误处理服务 - 使用简单的空实现适配器
            // MainWindowViewModel 需要 Infrastructure.Interfaces.IErrorHandlingService
            // 但实际错误处理由 UnifiedErrorHandlingService 完成
            // TODO: 未来统一两个 IErrorHandlingService 接口
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IErrorHandlingService,
                ErrorHandlingServiceStub>();

            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.ErrorHandling.IErrorHandlingService,
                LYBT.Desktop.Services.ErrorHandling.UnifiedErrorHandlingService>();

            // 注册启动优化服务
            containerRegistry.RegisterSingleton<IStartupOptimizationService,
                StartupOptimizationService>();
        }

        /// <summary>
        /// 注册UltraThink高级服务
        /// </summary>
        private static void RegisterUltraThinkServices(IContainerRegistry containerRegistry)
        {
            // Phase I: 简化主题服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Theming.IThemeService,
                LYBT.Desktop.Services.Theming.ThemeService>();

            // UltraThink Phase H: 高级功能优化服务
            containerRegistry.RegisterSingleton<IStartupOptimizationService,
                StartupOptimizationService>();

            // TODO: IUserPreferencesService 在 Core_New 中不存在,需要根据实际需要决定是否实现
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Settings.IUserPreferencesService,
            //     LYBT.Desktop.Services.Settings.UserPreferencesService>();
        }

        /// <summary>
        /// 注册导航服务 - Phase 2: NavigationJournal支持
        /// </summary>
        private static void RegisterNavigationServices(IContainerRegistry containerRegistry)
        {
            // TODO: EnhancedNavigationService 需要确认在 Core_New 中的正确位置
            // 可能在 Infrastructure.Services.Navigation 或 Services.Navigation
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Navigation.IEnhancedNavigationService,
            //     LYBT.Desktop.Infrastructure.Services.Navigation.EnhancedNavigationService>();
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

            // TODO: DT-006: StandardExceptionHandler 不实现 IExceptionHandler 接口，需要修改实现
            // containerRegistry.RegisterSingleton<IExceptionHandler,
            //     StandardExceptionHandler>();
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
            // TODO: AuthHeaderHandler 在 Core_New 中不存在，需要实现或移除
            // containerRegistry.Register<AuthHeaderHandler>();

            // TODO: UnifiedApiClientManager 类在 Core_New 中不存在，只有接口
            // 需要创建实现类或使用其他方式
            // containerRegistry.RegisterSingleton<IUnifiedApiClientManager,
            //     LYBT.Desktop.Services.Api.Managers.UnifiedApiClientManager>();

            // TODO: Core_New 中的 IUnifiedApiClientManager 接口简化了,不再提供各个 API 属性
            // 需要重新实现或者直接注册各个 API 客户端
            // 以下代码暂时注释,等待实现
            /*
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IAuthApi>(container =>
            {
                var manager = container.Resolve<IUnifiedApiClientManager>();
                return manager.AuthApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IUserApi>(container =>
            {
                var manager = container.Resolve<IUnifiedApiClientManager>();
                return manager.UserApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IPatientApi>(container =>
            {
                var manager = container.Resolve<IUnifiedApiClientManager>();
                return manager.PatientApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IHerbApi>(container =>
            {
                var manager = container.Resolve<IUnifiedApiClientManager>();
                return manager.HerbApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IFormulaApi>(container =>
            {
                var manager = container.Resolve<IUnifiedApiClientManager>();
                return manager.FormulaApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IConsultationApi>(container =>
            {
                var manager = container.Resolve<IUnifiedApiClientManager>();
                return manager.ConsultationApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IPrescriptionApi>(container =>
            {
                var manager = container.Resolve<IUnifiedApiClientManager>();
                return manager.PrescriptionApi;
            });
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Api.IMedicalCaseApi>(container =>
            {
                var manager = container.Resolve<IUnifiedApiClientManager>();
                return manager.MedicalCaseApi;
            });
            */

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
            // TODO: Herbs/Formula 模块服务在旧 Modules 文件夹中，需要确认它们在新架构中的位置
            // 这些服务可能已经移至 Core_New/Services 或者仍在 Modules 中
            // Herbs模块 - 基础药材数据，无外部依赖
            // containerRegistry.Register<LYBT.Desktop.Herbs.Services.HerbService>();
            // containerRegistry.Register<LYBT.Shared.Interfaces.Services.IHerbService,
            //     LYBT.Desktop.Herbs.Services.HerbService>();

            // Formula模块 - 验方模板数据，无外部依赖
            // containerRegistry.Register<LYBT.Desktop.Formula.Services.FormulaService>();
            // containerRegistry.Register<LYBT.Shared.Interfaces.Services.IFormulaService,
            //     LYBT.Desktop.Formula.Services.FormulaService>();
        }

        /// <summary>
        /// Layer 2: 认证模块注册 - 依赖基础�?
        /// </summary>
        private static void RegisterLayer2AuthModules(IContainerRegistry containerRegistry)
        {
            // TODO: Auth 模块服务在旧 Modules 文件夹中，需要确认它们在新架构中的位置
            // Auth模块 - DT-001/DT-002修复: 适配器模�?+ 标准IoC注册
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Auth.Services.AuthService>();
            // containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IAuthService,
            //     LYBT.Desktop.Auth.Services.AuthService>();
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Auth.IAuthenticationService,
            //     LYBT.Desktop.Auth.Services.AuthServiceAdapter>();

            // Users模块服务注册已移至UsersModule.RegisterTypes - Prism 8.x最佳实�?
        }

        /// <summary>
        /// Layer 3: 业务数据模块注册 - 依赖认证�?
        /// 性能优化：改为Scoped注册，避免启动时立即实例�?
        /// </summary>
        private static void RegisterLayer3BusinessDataModules(IContainerRegistry containerRegistry)
        {
            // TODO: Patients 模块服务在旧 Modules 文件夹中，需要确认它们在新架构中的位置
            // Patients模块 - 患者档案管�?
            // containerRegistry.Register<LYBT.Desktop.Patients.Services.PatientService>();
            // containerRegistry.Register<LYBT.Shared.Interfaces.Services.IPatientService,
            //     LYBT.Desktop.Patients.Services.PatientService>();
        }

        /// <summary>
        /// Layer 4: 流程协调模块注册 - 依赖业务数据�?
        /// 性能优化：改为Scoped注册，避免启动时立即实例�?
        /// </summary>
        private static void RegisterLayer4ProcessModules(IContainerRegistry containerRegistry)
        {
            // TODO: MedicalCase/Consultation 模块服务在旧 Modules 文件夹中，需要确认它们在新架构中的位置
            // MedicalCase模块 - 医案流程管理
            // containerRegistry.Register<LYBT.Desktop.MedicalCase.Services.MedicalCaseService>();
            // containerRegistry.Register<LYBT.Shared.Interfaces.Services.IMedicalCaseService,
            //     LYBT.Desktop.MedicalCase.Services.MedicalCaseService>();

            // Consultation模块 - 诊断流程
            // containerRegistry.Register<LYBT.Desktop.Consultation.Services.ConsultationService>();
            // containerRegistry.Register<LYBT.Shared.Interfaces.Services.IConsultationService,
            //     LYBT.Desktop.Consultation.Services.ConsultationService>();
        }

        /// <summary>
        /// Layer 5: 聚合服务模块注册 - 依赖流程协调�?
        /// 性能优化：改为Scoped注册，避免启动时立即实例�?
        /// </summary>
        private static void RegisterLayer5AggregationModules(IContainerRegistry containerRegistry)
        {
            // TODO: Prescriptions 模块服务在旧 Modules 文件夹中，需要确认它们在新架构中的位置
            // Prescriptions模块 - 处方聚合服务（依赖Herbs, Formula, Consultation�?
            // containerRegistry.Register<LYBT.Desktop.Prescriptions.Services.PrescriptionsService>();
            // containerRegistry.Register<LYBT.Shared.Interfaces.Services.IPrescriptionService,
            //     LYBT.Desktop.Prescriptions.Services.PrescriptionsService>();
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

            // TODO: Service层接口在Core_New中不存在，需要使用 Shared.Interfaces 或创建新接口
            // 暂时注释掉，等待接口定义
            /*
            containerRegistry.RegisterScoped<LYBT.Desktop.Services.Interfaces.IPatientService,
                LYBT.Desktop.Services.Business.PatientService>();
            containerRegistry.RegisterScoped<LYBT.Desktop.Services.Interfaces.IUserService,
                LYBT.Desktop.Services.Business.UserService>();
            containerRegistry.RegisterScoped<LYBT.Desktop.Services.Interfaces.IMedicalCaseService,
                LYBT.Desktop.Services.Business.MedicalCaseService>();
            containerRegistry.RegisterScoped<LYBT.Desktop.Services.Interfaces.IPrescriptionService,
                LYBT.Desktop.Services.Business.PrescriptionService>();
            containerRegistry.RegisterScoped<LYBT.Desktop.Services.Interfaces.IHerbService,
                LYBT.Desktop.Services.Business.HerbService>();
            containerRegistry.RegisterScoped<LYBT.Desktop.Services.Interfaces.IFormulaService,
                LYBT.Desktop.Services.Business.FormulaService>();
            containerRegistry.RegisterScoped<LYBT.Desktop.Services.Interfaces.IConsultationService,
                LYBT.Desktop.Services.Business.ConsultationService>();
            */
        }

        /// <summary>
        /// 注册核心基础服务（非业务模块服务�?
        /// </summary>
        private static void RegisterCoreServices(IContainerRegistry containerRegistry)
        {
            // TODO: 集中式导航服务在 Core_New 中需要确认位置
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Navigation.INavigationService,
            //     LYBT.Desktop.Services.Navigation.NavigationService>();

            // TODO: 权限服务在 Core_New 中需要确认实现
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IPermissionService,
            //     LYBT.Desktop.Services.Security.PermissionService>();

            // TODO: 凭据服务在 Core_New 中需要确认实现
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Security.ICredentialService,
            //     LYBT.Desktop.Services.Security.SecureCredentialService>();

            // TODO: 统一会话管理服务在 Core_New 中需要重新实现
            // Session 相关接口已移至 Infrastructure.Interfaces
            /*
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Session.UnifiedSessionManager>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Session.IUnifiedSessionManager,
                LYBT.Desktop.Services.Session.UnifiedSessionManager>();

            // 向后兼容性支�?- 映射到统一Session管理�?
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IUserSessionManager>(provider =>
                provider.Resolve<LYBT.Desktop.Services.Session.IUnifiedSessionManager>() as LYBT.Desktop.Infrastructure.Interfaces.IUserSessionManager
                ?? throw new InvalidOperationException("UnifiedSessionManager must implement IUserSessionManager"));
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.ITokenManager>(provider =>
                provider.Resolve<LYBT.Desktop.Services.Session.IUnifiedSessionManager>() as LYBT.Desktop.Infrastructure.Interfaces.ITokenManager
                ?? throw new InvalidOperationException("UnifiedSessionManager must implement ITokenManager"));
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.ISessionManager>(provider =>
                provider.Resolve<LYBT.Desktop.Services.Session.IUnifiedSessionManager>() as LYBT.Desktop.Infrastructure.Interfaces.ISessionManager
                ?? throw new InvalidOperationException("UnifiedSessionManager must implement ISessionManager"));
            */

            // 通知服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Notifications.INotificationService,
                LYBT.Desktop.Services.Notifications.NotificationService>();

            // 错误处理服务已在 RegisterBootstrapServices 中注册
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IErrorHandlingService,
            //     LYBT.Desktop.Services.ErrorHandling.UnifiedErrorHandlingService>();
            // TODO: Issue #815 Phase 3 - 恢复工作台路由服�?
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Workstation.Core.IWorkstationRouter, LYBT.Desktop.Workstation.Core.WorkstationRouter>();

            // 主窗口服务门�?- 简化MainWindowViewModel的依赖注�?
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

    /// <summary>
    /// Issue #837: ErrorHandlingService 空实现 - 临时适配器
    /// 用于满足 MainWindowViewModel 的 Infrastructure.Interfaces.IErrorHandlingService 依赖
    /// 实际错误处理由 UnifiedErrorHandlingService 完成
    /// TODO: 未来统一两个 IErrorHandlingService 接口
    /// </summary>
    public class ErrorHandlingServiceStub : LYBT.Desktop.Infrastructure.Interfaces.IErrorHandlingService
    {
        public Task HandleExceptionAsync(Exception exception, string? context = null) => Task.CompletedTask;
        public Task ShowErrorAsync(string message, string? title = null) => Task.CompletedTask;
        public Task ShowSuccessAsync(string message, string? title = null) => Task.CompletedTask;
        public Task ShowWarningAsync(string message, string? title = null) => Task.CompletedTask;
        public Task ShowInfoAsync(string message, string? title = null) => Task.CompletedTask;
        public Task<bool> ShowConfirmAsync(string message, string? title = null) => Task.FromResult(true);
        public void RegisterGlobalExceptionHandlers() { }
    }
}
