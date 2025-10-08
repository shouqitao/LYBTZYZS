using AutoMapper;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Services.Modules;
using LYBT.Desktop.Services.Performance;
using LYBT.Desktop.Services.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using System.Net.Http;

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
            RegisterUltraThinkServices(containerRegistry);
            RegisterCommandServices(containerRegistry); // Phase 3: CompositeCommand全局命令

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

            // 读取 API 配置并设置为静态字段供 RegisterHttpServices 使用
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
            _ignoreSslErrors = configuration.GetValue<bool>("ApiSettings:IgnoreSslErrors", false);

            // Issue #840: 注册用户通知服务
            // MainWindowViewModel 使用 IUserNotificationService 进行简单消息提示
            // 系统级错误处理由 UnifiedErrorHandlingService 负责
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService,
                LYBT.Desktop.Infrastructure.Services.UserNotificationService>();

            // Issue #844: 统一通知服务接口 - 已完成 UltraThink 重构目标
            // 使用新版 INotificationService (LYBT.Desktop.Services.Notifications)
            // 提供同步+异步接口、确认对话框、加载状态、事件通知等完整功能
            // 替代旧版仅异步方法的简陋接口，提升用户体验和代码可维护性
            // 必须在 UnifiedErrorHandlingService 之前注册,因为后者依赖此服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Notifications.INotificationService,
                LYBT.Desktop.Services.Notifications.NotificationService>();

            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.ErrorHandling.IErrorHandlingService,
                LYBT.Desktop.Services.ErrorHandling.UnifiedErrorHandlingService>();

            // 注册启动优化服务 (ApplicationBootstrapper 依赖)
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Performance.IStartupOptimizationService,
                LYBT.Desktop.Services.Performance.StartupOptimizationService>();
        }

        // 静态字段用于在 RegisterBootstrapServices 和 RegisterHttpServices 之间传递配置
        private static string _apiBaseUrl = "https://localhost:5001";
        private static bool _ignoreSslErrors = false;

        // 保存MS DI ServiceProvider用于HTTP服务（避免被垃圾回收）
        private static IServiceProvider? _httpServiceProvider;

        /// <summary>
        /// 注册UltraThink高级服务
        /// </summary>
        private static void RegisterUltraThinkServices(IContainerRegistry containerRegistry)
        {
            // Phase I: 简化主题服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Theming.IThemeService,
                LYBT.Desktop.Services.Theming.ThemeService>();

            // Note: IStartupOptimizationService 实际在 RegisterBootstrapServices 中注册（lines 103-104）
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
                // Phase 2: 移除分散的模块 Mappings，准备集中配置
                // Phase 4 将在 Desktop.Services/Mapping/ 创建统一的 MappingProfile
                // TODO: Phase 4 - 添加统一的 Desktop.Services.Mapping.MappingProfile
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
            // 创建 ServiceCollection,使用 ServiceRegistration 配置 HttpClient
            var services = new ServiceCollection();
            
            // 调用 ServiceRegistration.AddDesktopServices() - 这会正确配置:
            // 1. AuthorizationMessageHandler (自动添加 JWT token)
            // 2. SSL 证书验证绕过 (开发环境)
            // 3. Named HttpClient "ApiService" 配置
            // 4. IApiService 及其依赖
            LYBT.Desktop.Services.ServiceRegistration.AddDesktopServices(services, _apiBaseUrl, _ignoreSslErrors);
            
            // 构建 ServiceProvider 并保存到静态字段（避免被垃圾回收）
            var serviceProvider = services.BuildServiceProvider();
            _httpServiceProvider = serviceProvider;

            // 从 ServiceProvider 中获取配置好的服务,注册到 Prism 容器
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            containerRegistry.RegisterInstance<IHttpClientFactory>(httpClientFactory);
            
            // 注册 IApiService - 已包含认证处理和 SSL 配置
            var apiService = serviceProvider.GetRequiredService<LYBT.Desktop.Services.Http.IApiService>();
            containerRegistry.RegisterInstance<LYBT.Desktop.Services.Http.IApiService>(apiService);
            
            // 兼容性:保留单例 HttpClient 供其他旧代码使用
            containerRegistry.RegisterSingleton<HttpClient>(() =>
            {
                return httpClientFactory.CreateClient("ApiService");
            });
        }

        /// <summary>
        /// 注册API服务 - UltraThink统一API客户端管理器
        /// </summary>
        private static void RegisterApiServices(IContainerRegistry containerRegistry)
        {
            // IApiService 已经在 RegisterHttpServices 中通过 ServiceRegistration.AddDesktopServices() 注册
            // 无需额外操作
        }

        /// <summary>
        /// 注册业务服务 - UltraThink架构 with Repository Pattern
        /// </summary>
        private static void RegisterBusinessServices(IContainerRegistry containerRegistry)
        {
            // Issue #1041: Repository层改为Singleton（WPF无请求作用域，Scoped无意义）
            containerRegistry.RegisterSingleton<IPatientRepository,
                LYBT.Desktop.Services.Repositories.PatientRepository>();
            containerRegistry.RegisterSingleton<IUserRepository,
                LYBT.Desktop.Services.Repositories.UserRepository>();
            containerRegistry.RegisterSingleton<IMedicalCaseRepository,
                LYBT.Desktop.Services.Repositories.MedicalCaseRepository>();
            containerRegistry.RegisterSingleton<IPrescriptionRepository,
                LYBT.Desktop.Services.Repositories.PrescriptionRepository>();
            containerRegistry.RegisterSingleton<IHerbRepository,
                LYBT.Desktop.Services.Repositories.HerbRepository>();
            containerRegistry.RegisterSingleton<IFormulaRepository,
                LYBT.Desktop.Services.Repositories.FormulaRepository>();
            containerRegistry.RegisterSingleton<IConsultationRepository,
                LYBT.Desktop.Services.Repositories.ConsultationRepository>();

            // UltraThink修复 Issue #856: 注册异常处理服务(业务服务依赖)
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Exceptions.IExceptionHandler,
                LYBT.Desktop.Services.Exceptions.StandardExceptionHandler>();

            // Issue #835: 注册认证服务(使用 Shared.Interfaces)
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IAuthService,
                LYBT.Desktop.Services.Business.AuthService>();

            // Issue #1039: 注册 Desktop 本地认证服务（ILocalAuthService 继承 IAuthService）
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Business.ILocalAuthService,
                LYBT.Desktop.Services.Business.AuthService>();

            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Business.ITokenStorageService,
                LYBT.Desktop.Services.Business.TokenStorageService>();

            // Issue #861: 注册用户名存储服务（记住用户名功能）
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Business.IUsernameStorageService,
                LYBT.Desktop.Services.Business.UsernameStorageService>();

            // Issue #835: 注册 IAuthenticationService 适配器(供 MainWindowViewModel 使用)
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Auth.IAuthenticationService,
                LYBT.Desktop.Services.Auth.AuthenticationService>();

            // Issue #1039: 注册 Desktop 本地认证服务（ILocalAuthService 继承 IAuthService）
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Business.ILocalAuthService,
                LYBT.Desktop.Services.Business.AuthService>();

            // Issue #1041: 业务服务改为Singleton（WPF无请求作用域，Scoped无意义）
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IPatientService,
                LYBT.Desktop.Services.Business.PatientService>();
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IUserService,
                LYBT.Desktop.Services.Business.UserService>();
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IMedicalCaseService,
                LYBT.Desktop.Services.Business.MedicalCaseService>();
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IPrescriptionService,
                LYBT.Desktop.Services.Business.PrescriptionService>();
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IHerbService,
                LYBT.Desktop.Services.Business.HerbService>();
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IFormulaService,
                LYBT.Desktop.Services.Business.FormulaService>();
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IConsultationService,
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

            // Issue #856: WebAPI 健康检查服务 - 登录界面状态指示器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.Interfaces.IApiHealthCheckService,
                LYBT.Desktop.Services.HealthCheck.ApiHealthCheckService>();

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
            // 注意：8个业务模块(Auth/Users/Patients/Herbs/Formula/Consultation/Prescriptions/MedicalCase)
            // 现在通过自动发现系统统一注册，无需在各自的XxxModule.RegisterTypes中重复注册
            // 这消除了双重注册风险，简化了模块开发
        }

        /// <summary>
        /// 注册对话框服务
        /// </summary>
        private static void RegisterDialogs(IContainerRegistry containerRegistry)
        {
            // Phase 3.4: 所有 Dialog 现在使用 Prism Dialog System
            // SimplifiedDialogService 和 ICustomDialogService 已删除
            // 各模块通过 containerRegistry.RegisterDialog<TView, TViewModel>() 注册
        }

        /// <summary>
        /// 注册性能优化服务
        /// </summary>
        private static void RegisterPerformanceServices(IContainerRegistry containerRegistry)
        {
            // UltraThink清理：移除过度工程的ModuleLoadingCoordinator
            // 小诊所系统不需要复杂的模块加载协调功能
        }
    }

}
