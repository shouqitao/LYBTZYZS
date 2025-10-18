using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using System.Net.Http;

namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>
    /// 服务注册扩展方法 - ADR-002 合规版本
    ///
    /// 架构说明：
    /// - 移除 Business Service 层（Desktop.Services.Business.*）
    /// - Repository 由各业务模块自行注册
    /// - 保留 Infrastructure Service（Foundation/Infrastructure 层）
    /// - ViewModel 直接调用 Repository + Infrastructure Service
    ///
    /// 服务生命周期策略（Prism + DryIoc）：
    /// - Singleton: 基础设施服务（Logger, Cache, HttpClient, AuthService）
    /// - Transient: ViewModel（由 Prism 自动管理）
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册所有服务
        /// </summary>
        public static void RegisterAllServices(this IContainerRegistry containerRegistry)
        {
            var configuration = RegisterConfiguration(containerRegistry);
            RegisterLogging(containerRegistry);
            RegisterCacheServices(containerRegistry);
            RegisterHttpServices(containerRegistry, configuration);
            RegisterFoundationServices(containerRegistry);
            RegisterPresentationServices(containerRegistry);  // Issue #1239 修复: 添加 Presentation 层服务注册
            RegisterInfrastructureServices(containerRegistry);
            RegisterCommandServices(containerRegistry);
            RegisterApplicationServices(containerRegistry);
        }

        /// <summary>
        /// 注册配置服务
        /// </summary>
        private static IConfiguration RegisterConfiguration(IContainerRegistry containerRegistry)
        {
            // WPF Prism 不会自动注册 IConfiguration，需要手动创建和注册
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            containerRegistry.RegisterInstance<IConfiguration>(configuration);
            return configuration;
        }

        /// <summary>
        /// 注册日志服务
        /// </summary>
        private static void RegisterLogging(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ILoggerFactory>(() =>
            {
                return LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));
            });

            // 注册 ILogger<T> - 为每个需要日志的服务单独注册
            // ViewModelBase 通过 ILoggerFactory.CreateLogger(GetType()) 创建自己的 Logger
            // 以下服务需要 ILogger<T> 构造函数注入：
            
            // Infrastructure 层服务
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Infrastructure.Services.MainWindowServicesFacade>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Infrastructure.Services.MainWindowServicesFacade>());
            
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Infrastructure.Services.StandardErrorHandler>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Infrastructure.Services.StandardErrorHandler>());
            
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Infrastructure.Services.KeyboardShortcutService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Infrastructure.Services.KeyboardShortcutService>());

            // Foundation 层服务
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Foundation.Http.ApiService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Foundation.Http.ApiService>());
            
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Foundation.Http.AuthorizationMessageHandler>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Foundation.Http.AuthorizationMessageHandler>());
            
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Foundation.Security.AuthenticationService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Foundation.Security.AuthenticationService>());
            
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Foundation.Security.TokenStorageService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Foundation.Security.TokenStorageService>());

            // Issue #1245 修复: 注册 UsernameStorageService 的 Logger
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Foundation.Security.UsernameStorageService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Foundation.Security.UsernameStorageService>());

            // Issue #1246 修复: 注册 SecureCredentialStorage 的 Logger
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Foundation.Security.SecureCredentialStorage>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Foundation.Security.SecureCredentialStorage>());

            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Foundation.Modules.ModuleLoadingService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Foundation.Modules.ModuleLoadingService>());
            
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Foundation.Performance.StartupOptimizationService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Foundation.Performance.StartupOptimizationService>());

            // Presentation 层服务
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Presentation.Notifications.NotificationService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Presentation.Notifications.NotificationService>());
            
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Presentation.Notifications.UnifiedErrorHandlingService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Presentation.Notifications.UnifiedErrorHandlingService>());

            // Shell 层服务
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Shell.App>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Shell.App>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Shell.Services.ApplicationInitializationService>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Shell.Services.ApplicationInitializationService>());
            
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Shell.Services.Bootstrap.ApplicationBootstrapper>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Shell.Services.Bootstrap.ApplicationBootstrapper>());

            // Issue #1239 修复: 注册所有 Prism 模块的 ILogger<T>
            // 业务模块 (8个)
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Auth.AuthenticationModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Auth.AuthenticationModule>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Users.UsersModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Users.UsersModule>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Patients.PatientsModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Patients.PatientsModule>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Consultation.ConsultationModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Consultation.ConsultationModule>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.MedicalCase.MedicalCaseModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.MedicalCase.MedicalCaseModule>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Prescriptions.PrescriptionsModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Prescriptions.PrescriptionsModule>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Herbs.HerbsModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Herbs.HerbsModule>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Formula.FormulaModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Formula.FormulaModule>());

            // 工作台模块 (2个)
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.AdminWorkstation.AdminWorkstationModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.AdminWorkstation.AdminWorkstationModule>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.ClinicalWorkstation.ClinicalWorkstationModule>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.ClinicalWorkstation.ClinicalWorkstationModule>());

            // Issue #1239 修复: 业务模块 Repositories (7个)
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Users.Repositories.UserRepository>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Users.Repositories.UserRepository>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Patients.Repositories.PatientRepository>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Patients.Repositories.PatientRepository>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Consultation.Repositories.ConsultationRepository>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Consultation.Repositories.ConsultationRepository>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Herbs.Repositories.HerbRepository>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Herbs.Repositories.HerbRepository>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Formula.Repositories.FormulaRepository>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Formula.Repositories.FormulaRepository>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.MedicalCase.Repositories.MedicalCaseRepository>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.MedicalCase.Repositories.MedicalCaseRepository>());
            containerRegistry.RegisterSingleton<ILogger<LYBT.Desktop.Prescriptions.Repositories.PrescriptionRepository>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<LYBT.Desktop.Prescriptions.Repositories.PrescriptionRepository>());
        }

        /// <summary>
        /// 注册缓存服务
        /// </summary>
        private static void RegisterCacheServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IMemoryCache>(() =>
            {
                var options = new MemoryCacheOptions
                {
                    SizeLimit = 1000,
                    CompactionPercentage = 0.25,
                    ExpirationScanFrequency = TimeSpan.FromMinutes(5)
                };
                return new MemoryCache(options);
            });
        }

        /// <summary>
        /// 注册HTTP相关服务
        /// </summary>
        private static void RegisterHttpServices(IContainerRegistry containerRegistry, IConfiguration config)
        {
            // 获取 API 配置
            var apiBaseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
            var ignoreSslErrors = config.GetValue<bool>("ApiSettings:IgnoreSslErrors", false);
            
            // Issue #1239 修复: 在 Prism 容器中注册 AuthorizationMessageHandler
            containerRegistry.RegisterSingleton<LYBT.Desktop.Foundation.Http.AuthorizationMessageHandler>();

            // Issue #1239 修复: 手动创建带有 AuthorizationMessageHandler 的 HttpClient
            // 不使用 ServiceCollection，因为 AuthorizationMessageHandler 依赖 Prism 容器中的服务
            containerRegistry.RegisterSingleton<HttpClient>(resolver =>
            {
                // 1. 创建基础 HttpClientHandler
                var httpHandler = new HttpClientHandler();
                if (ignoreSslErrors)
                {
                    httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                }

                // 2. 从 Prism 容器解析 AuthorizationMessageHandler
                var authHandler = resolver.Resolve<LYBT.Desktop.Foundation.Http.AuthorizationMessageHandler>();
                authHandler.InnerHandler = httpHandler;

                // 3. 使用 authHandler 创建 HttpClient（自动添加 Bearer Token）
                var httpClient = new HttpClient(authHandler)
                {
                    BaseAddress = new Uri(apiBaseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };

                return httpClient;
            });

            // Issue #1239 修复: 使用延迟解析注册 Refit 客户端（避免在注册阶段解析 HttpClient）
            // 所有 Refit 客户端共享同一个 HttpClient 实例（包含 AuthorizationMessageHandler）
            containerRegistry.RegisterSingleton<LYBT.Desktop.Contracts.Api.IAuthApi>(resolver =>
                Refit.RestService.For<LYBT.Desktop.Contracts.Api.IAuthApi>(resolver.Resolve<HttpClient>()));
            
            containerRegistry.RegisterSingleton<LYBT.Desktop.Contracts.Api.IPatientApi>(resolver =>
                Refit.RestService.For<LYBT.Desktop.Contracts.Api.IPatientApi>(resolver.Resolve<HttpClient>()));
            
            containerRegistry.RegisterSingleton<LYBT.Desktop.Contracts.Api.IUserApi>(resolver =>
                Refit.RestService.For<LYBT.Desktop.Contracts.Api.IUserApi>(resolver.Resolve<HttpClient>()));
            
            containerRegistry.RegisterSingleton<LYBT.Desktop.Contracts.Api.IConsultationApi>(resolver =>
                Refit.RestService.For<LYBT.Desktop.Contracts.Api.IConsultationApi>(resolver.Resolve<HttpClient>()));
            
            containerRegistry.RegisterSingleton<LYBT.Desktop.Contracts.Api.IHerbApi>(resolver =>
                Refit.RestService.For<LYBT.Desktop.Contracts.Api.IHerbApi>(resolver.Resolve<HttpClient>()));
            
            containerRegistry.RegisterSingleton<LYBT.Desktop.Contracts.Api.IFormulaApi>(resolver =>
                Refit.RestService.For<LYBT.Desktop.Contracts.Api.IFormulaApi>(resolver.Resolve<HttpClient>()));
            
            containerRegistry.RegisterSingleton<LYBT.Desktop.Contracts.Api.IMedicalCaseApi>(resolver =>
                Refit.RestService.For<LYBT.Desktop.Contracts.Api.IMedicalCaseApi>(resolver.Resolve<HttpClient>()));
            
            containerRegistry.RegisterSingleton<LYBT.Desktop.Contracts.Api.IPrescriptionApi>(resolver =>
                Refit.RestService.For<LYBT.Desktop.Contracts.Api.IPrescriptionApi>(resolver.Resolve<HttpClient>()));
        }

        /// <summary>
        /// 注册 Foundation 层服务（Infrastructure Services）
        /// </summary>
        private static void RegisterFoundationServices(IContainerRegistry containerRegistry)
        {
            // 认证服务 - Foundation/Security
        containerRegistry.RegisterSingleton<IAuthenticationService, AuthenticationService>();

        // Token 存储服务 - Foundation/Security
        containerRegistry.RegisterSingleton<ITokenStorageService, TokenStorageService>();

        // Issue #1245 修复: 用户名存储服务 - Foundation/Security
        containerRegistry.RegisterSingleton<LYBT.Desktop.Foundation.Security.IUsernameStorageService,
            LYBT.Desktop.Foundation.Security.UsernameStorageService>();

        // Issue #1246 修复: 安全凭据存储服务（密码加密）- Foundation/Security
        containerRegistry.RegisterSingleton<LYBT.Desktop.Foundation.Security.ISecureCredentialStorage,
            LYBT.Desktop.Foundation.Security.SecureCredentialStorage>();

        // API 健康检查服务 - Foundation/HealthCheck
        containerRegistry.RegisterSingleton<LYBT.Desktop.Foundation.HealthCheck.IApiHealthCheckService,
            LYBT.Desktop.Foundation.HealthCheck.ApiHealthCheckService>();

        // Issue #1239 修复: 注册 API 服务基类 - Foundation/Http
        containerRegistry.RegisterSingleton<LYBT.Desktop.Foundation.Http.IApiService,
            LYBT.Desktop.Foundation.Http.ApiService>();

        // Issue #1239 修复: 注册启动优化服务 - Foundation/Performance
        containerRegistry.RegisterSingleton<LYBT.Desktop.Foundation.Performance.IStartupOptimizationService,
            LYBT.Desktop.Foundation.Performance.StartupOptimizationService>();
        }

        /// <summary>
        /// 注册 Presentation 层服务
        /// </summary>
        private static void RegisterPresentationServices(IContainerRegistry containerRegistry)
        {
            // 通知服务 - Presentation/Notifications
            containerRegistry.RegisterSingleton<LYBT.Desktop.Presentation.Notifications.INotificationService,
                LYBT.Desktop.Presentation.Notifications.NotificationService>();

            // 错误处理服务 - Presentation/Notifications
            containerRegistry.RegisterSingleton<LYBT.Desktop.Presentation.Notifications.IErrorHandlingService,
                LYBT.Desktop.Presentation.Notifications.UnifiedErrorHandlingService>();

            // 注意：PatientSelector组件使用反射进行手动映射,不需要AutoMapper配置
            // 原因：Presentation层不能引用Modules层(避免循环依赖)
        }

        /// <summary>
        /// 注册 Infrastructure 层服务
        /// </summary>
        private static void RegisterInfrastructureServices(IContainerRegistry containerRegistry)
        {
            // 会话管理器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.ISessionManager,
                LYBT.Desktop.Infrastructure.Services.SessionManager>();

            // 用户通知服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService,
                LYBT.Desktop.Infrastructure.Services.UserNotificationService>();

            // 主窗口服务门面
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IMainWindowServicesFacade,
                LYBT.Desktop.Infrastructure.Services.MainWindowServicesFacade>();

            // 标准错误处理器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Services.IStandardErrorHandler,
                LYBT.Desktop.Infrastructure.Services.StandardErrorHandler>();

            // 处方打印服务已移除（等待 Issue #1202 实现新的统一打印系统）
            // 新的打印服务将在 Desktop.Presentation/Print/ 中实现，使用 QuestPDF

            // 键盘快捷键服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Services.IKeyboardShortcutService,
                LYBT.Desktop.Infrastructure.Services.KeyboardShortcutService>();

            // 功能开关服务 (Issue #1477 #1479 架构纠正v2)
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IFeatureToggleService,
                LYBT.Desktop.Infrastructure.Services.FeatureToggleService>();

            // 注意：UserExperienceService 已移至 Presentation 层（UI体验服务应属于 Presentation 层）
            // 如需使用，请在 App.xaml.cs 中调用 services.AddDesktopPresentation()
        }

        /// <summary>
        /// 注册全局命令和模块管理服务
        /// </summary>
        private static void RegisterCommandServices(IContainerRegistry containerRegistry)
        {
            // 全局命令系统
            containerRegistry.RegisterSingleton<IApplicationCommands, ApplicationCommands>();

            // 模块加载服务
            containerRegistry.RegisterSingleton<IModuleLoadingService, ModuleLoadingService>();
        }

        /// <summary>
        /// 注册应用程序启动服务
        /// </summary>
        private static void RegisterApplicationServices(IContainerRegistry containerRegistry)
        {
            // 应用程序初始化服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService,
                LYBT.Desktop.Shell.Services.ApplicationInitializationService>();

            // 应用程序启动引导服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.Bootstrap.IApplicationBootstrapper,
                LYBT.Desktop.Shell.Services.Bootstrap.ApplicationBootstrapper>();
        }
    }
}
