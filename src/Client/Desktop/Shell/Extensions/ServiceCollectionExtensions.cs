using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Desktop.Admin;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Desktop.Auth.Services;
using LYBT.Desktop.Clinical;
using LYBT.Desktop.Consultation;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Formula;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Http;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.Herbs.Services;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Desktop.Prescriptions;
using LYBT.Desktop.Prescriptions.Services;
using LYBT.Desktop.Infrastructure.Services.Notifications;
using LYBT.Desktop.Infrastructure.Services.UserExperience;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.Services.Diagnostics;
using LYBT.Desktop.Shell.Services.HealthCheck;
using LYBT.Desktop.Shell.Services.Lifecycle;
using LYBT.Desktop.Shell.Services.Login;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Desktop.Shell.Services.Startup;
using LYBT.Desktop.Shell.Services.Startup.Steps;
using LYBT.Desktop.Users;
using LYBT.Desktop.Users.Repositories;
using LYBT.Shared.ExceptionHandling.Handlers;
using LYBT.Desktop.Users.ViewModels.Components;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Refit;
using Serilog;

namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>服务注册扩展方法 - Singleton用于基础设施服务，Transient用于ViewModel（Prism自动管理）</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>注册所有服务</summary>
        public static void RegisterAllServices(this IContainerRegistry containerRegistry)
        {
            var configuration = RegisterConfiguration(containerRegistry);
            RegisterLogging(containerRegistry);
            RegisterCacheServices(containerRegistry);
            RegisterHttpServices(containerRegistry, configuration);
            RegisterFoundationServices(containerRegistry);
            RegisterPresentationServices(containerRegistry);
            RegisterInfrastructureServices(containerRegistry);
            RegisterCommandServices(containerRegistry);
            RegisterApplicationServices(containerRegistry);
        }

        /// <summary>注册配置服务</summary>
        private static IConfiguration RegisterConfiguration(IContainerRegistry containerRegistry)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            containerRegistry.RegisterInstance<IConfiguration>(configuration);
            return configuration;
        }

        /// <summary>注册日志服务</summary>
        private static void RegisterLogging(IContainerRegistry containerRegistry)
        {
            RegisterLoggerFactory(containerRegistry);
            RegisterInfrastructureLoggers(containerRegistry);
            RegisterFoundationLoggers(containerRegistry);
            RegisterPresentationAndShellLoggers(containerRegistry);
            RegisterModuleLoggers(containerRegistry);
            RegisterRepositoryLoggers(containerRegistry);
            RegisterServiceLoggers(containerRegistry);
            RegisterComponentLoggers(containerRegistry);
        }

        /// <summary>注册LoggerFactory和泛型ILogger&lt;&gt;</summary>
        /// <remarks>refactor-logging-system: 使用Serilog作为日志提供程序</remarks>
        private static void RegisterLoggerFactory(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ILoggerFactory>(() =>
                LoggerFactory.Create(builder => builder.AddSerilog(dispose: false)));
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
        }

        /// <summary>通用Logger注册辅助方法</summary>
        private static void RegisterLogger<T>(IContainerRegistry containerRegistry) =>
            containerRegistry.RegisterSingleton<ILogger<T>>(resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<T>());

        /// <summary>注册Infrastructure层Logger</summary>
        private static void RegisterInfrastructureLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<MainWindowServicesFacade>(containerRegistry);
            RegisterLogger<StandardErrorHandler>(containerRegistry);
            RegisterLogger<KeyboardShortcutService>(containerRegistry);
            RegisterLogger<RoleNavigationService>(containerRegistry);
            RegisterLogger<ActiveConsultationService>(containerRegistry);
            RegisterLogger<ApplicationTickService>(containerRegistry);
            RegisterLogger<UserActivityTracker>(containerRegistry);
        }

        /// <summary>注册Foundation层Logger</summary>
        private static void RegisterFoundationLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<ApiService>(containerRegistry);
            RegisterLogger<AuthorizationMessageHandler>(containerRegistry);
            RegisterLogger<TokenRefreshHandler>(containerRegistry);
            RegisterLogger<AuthenticationService>(containerRegistry);
            RegisterLogger<TokenStorageService>(containerRegistry);
            RegisterLogger<UsernameStorageService>(containerRegistry);
            RegisterLogger<SecureCredentialStorage>(containerRegistry);
            RegisterLogger<SecureTokenStorage>(containerRegistry);
            RegisterLogger<LocalTokenValidator>(containerRegistry);
            RegisterLogger<ModuleLoadingService>(containerRegistry);
            RegisterLogger<StartupOptimizationService>(containerRegistry);
            RegisterLogger<TokenLifecycleService>(containerRegistry);
        }

        /// <summary>注册Presentation和Shell层Logger</summary>
        private static void RegisterPresentationAndShellLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<NotificationService>(containerRegistry);
            RegisterLogger<DesktopExceptionHandler>(containerRegistry);
            RegisterLogger<App>(containerRegistry);
            RegisterLogger<ApplicationInitializationService>(containerRegistry);
            RegisterLogger<ApplicationBootstrapper>(containerRegistry);
            RegisterLogger<ApplicationStateService>(containerRegistry);
            RegisterLogger<NavigationManager>(containerRegistry);
            RegisterLogger<MenuManager>(containerRegistry);

            // Shell启动流程重构 - Phase 1 新增Logger
            RegisterLogger<ApplicationLifecycle>(containerRegistry);
            RegisterLogger<SessionLifecycleManager>(containerRegistry);
            RegisterLogger<StartupDiagnostics>(containerRegistry);

            // Shell启动流程重构 - Phase 2 新增Logger
            RegisterLogger<LoginCoordinator>(containerRegistry);

            // Shell架构整合 - HealthCheckCoordinator Logger
            RegisterLogger<HealthCheckCoordinator>(containerRegistry);
        }

        /// <summary>注册业务模块Logger</summary>
        private static void RegisterModuleLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<AuthenticationModule>(containerRegistry);
            RegisterLogger<UsersModule>(containerRegistry);
            RegisterLogger<PatientsModule>(containerRegistry);
            RegisterLogger<ConsultationModule>(containerRegistry);
            RegisterLogger<MedicalCaseModule>(containerRegistry);
            RegisterLogger<PrescriptionsModule>(containerRegistry);
            RegisterLogger<HerbsModule>(containerRegistry);
            RegisterLogger<FormulaModule>(containerRegistry);
            RegisterLogger<ClinicalModule>(containerRegistry);
            RegisterLogger<AdminModule>(containerRegistry);
        }

        /// <summary>注册Repository层Logger</summary>
        private static void RegisterRepositoryLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<UserRepository>(containerRegistry);
            RegisterLogger<PatientRepository>(containerRegistry);
            RegisterLogger<HerbRepository>(containerRegistry);
            RegisterLogger<FormulaRepository>(containerRegistry);
            RegisterLogger<MedicalCaseRepository>(containerRegistry);
        }

        /// <summary>注册业务服务Logger</summary>
        private static void RegisterServiceLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<PrescriptionEditorService>(containerRegistry);
            RegisterLogger<SystemSettingsService>(containerRegistry);
        }

        /// <summary>注册Component层Logger（CommandHandler/DataManager/Validator等）</summary>
        private static void RegisterComponentLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<UserCommandHandler>(containerRegistry);
            RegisterLogger<FormulaCommandHandler>(containerRegistry);
            RegisterLogger<PatientCommandHandler>(containerRegistry);
            RegisterLogger<HerbDataManager>(containerRegistry);
            RegisterLogger<MedicalCaseDataManager>(containerRegistry);
            RegisterLogger<FormulaDataManager>(containerRegistry);
            RegisterLogger<PatientDataManager>(containerRegistry);
            RegisterLogger<PatientValidator>(containerRegistry);
        }

        /// <summary>注册缓存服务</summary>
        private static void RegisterCacheServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IMemoryCache>(() => new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1000, CompactionPercentage = 0.25, ExpirationScanFrequency = TimeSpan.FromMinutes(5)
            }));
        }

        /// <summary>注册HTTP相关服务</summary>
        /// <remarks>adopt-activity-api-tracing: HttpClient自动传播W3C TraceContext，无需自定义Handler</remarks>
        private static void RegisterHttpServices(IContainerRegistry containerRegistry, IConfiguration config)
        {
            var apiBaseUrl = config["Lybt:Client:Api:BaseUrl"] ?? "https://localhost:5001";
            var ignoreSslErrors = config.GetValue<bool>("Lybt:Client:Api:IgnoreSslErrors", false);

            containerRegistry.RegisterSingleton<AuthorizationMessageHandler>();
            containerRegistry.RegisterSingleton<TokenRefreshHandler>(resolver =>
            {
                var tokenStorage = resolver.Resolve<ITokenStorageService>();
                var configuration = resolver.Resolve<IConfiguration>();
                var logger = resolver.Resolve<ILogger<TokenRefreshHandler>>();
                IUserActivityState? userActivityState = null;
                try { userActivityState = resolver.Resolve<IUserActivityState>(); }
                catch { /* 启动阶段可能尚未注册 */ }
                return new TokenRefreshHandler(tokenStorage, configuration, logger, userActivityState);
            });

            // Handler链: HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → HttpClient
            // 注: HttpClient自动传播W3C TraceContext (traceparent header)
            containerRegistry.RegisterSingleton<HttpClient>(resolver =>
            {
                var httpHandler = new HttpClientHandler();
                if (ignoreSslErrors)
                    httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                var tokenRefreshHandler = resolver.Resolve<TokenRefreshHandler>();
                tokenRefreshHandler.InnerHandler = httpHandler;
                var authHandler = resolver.Resolve<AuthorizationMessageHandler>();
                authHandler.InnerHandler = tokenRefreshHandler;

                return new HttpClient(authHandler) { BaseAddress = new Uri(apiBaseUrl), Timeout = TimeSpan.FromSeconds(30) };
            });

            // Refit客户端共享HttpClient实例 - 配置JSON序列化以支持枚举字符串转换
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
                })
            };
            containerRegistry.RegisterSingleton<IAuthApi>(r => RestService.For<IAuthApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IPatientApi>(r => RestService.For<IPatientApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IUserApi>(r => RestService.For<IUserApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IHerbApi>(r => RestService.For<IHerbApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IFormulaApi>(r => RestService.For<IFormulaApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IMedicalCaseApi>(r => RestService.For<IMedicalCaseApi>(r.Resolve<HttpClient>(), refitSettings));
        }

        /// <summary>注册Foundation层服务</summary>
        private static void RegisterFoundationServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IAuthenticationService, AuthenticationService>();
            containerRegistry.RegisterSingleton<ITokenStorageService, TokenStorageService>();
            containerRegistry.RegisterSingleton<ITokenStorage, SecureTokenStorage>();
            containerRegistry.RegisterSingleton<ITokenValidator, LocalTokenValidator>();
            containerRegistry.RegisterSingleton<IUsernameStorageService, UsernameStorageService>();
            containerRegistry.RegisterSingleton<ISecureCredentialStorage, SecureCredentialStorage>();
            containerRegistry.RegisterSingleton<IConnectionSettingsService, ConnectionSettingsService>();
            containerRegistry.RegisterSingleton<ISystemSettingsService, SystemSettingsService>();
            containerRegistry.RegisterSingleton<IApiHealthCheckService, ApiHealthCheckService>();
            containerRegistry.RegisterSingleton<IApiService, ApiService>();
            containerRegistry.RegisterSingleton<IStartupOptimizationService, StartupOptimizationService>();
            containerRegistry.RegisterSingleton<ITokenLifecycleService, TokenLifecycleService>();
        }

        /// <summary>注册Presentation层服务</summary>
        private static void RegisterPresentationServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<INotificationService, NotificationService>();
            containerRegistry.RegisterSingleton<IDesktopExceptionHandler, DesktopExceptionHandler>();
            containerRegistry.RegisterSingleton<NavigationManager>();
            containerRegistry.RegisterSingleton<MenuManager>();
        }

        /// <summary>注册Infrastructure层服务</summary>
        private static void RegisterInfrastructureServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
            containerRegistry.RegisterSingleton<IActiveConsultationService, ActiveConsultationService>();
            containerRegistry.RegisterSingleton<IApplicationTickService, ApplicationTickService>();
            containerRegistry.RegisterSingleton<UserActivityTracker>(resolver =>
            {
                var logger = resolver.Resolve<ILogger<UserActivityTracker>>();
                var tickService = resolver.Resolve<IApplicationTickService>();
                var config = resolver.Resolve<IConfiguration>();
                var inactivityTimeout = config.GetValue("Lybt:Client:Session:InactivityTimeoutMinutes", 5);
                var warningBefore = config.GetValue("Lybt:Client:Session:WarningBeforeTimeoutMinutes", 0);
                var checkInterval = config.GetValue("Lybt:Client:Session:ActivityCheckIntervalSeconds", 30);
                return new UserActivityTracker(logger, tickService, inactivityTimeout, warningBefore, checkInterval);
            });
            containerRegistry.RegisterSingleton<IUserActivityTracker>(resolver => resolver.Resolve<UserActivityTracker>());
            containerRegistry.RegisterSingleton<IUserActivityState>(resolver => resolver.Resolve<UserActivityTracker>());
            containerRegistry.RegisterSingleton<IValidationService, ValidationService>();
            containerRegistry.RegisterSingleton<IUserNotificationService, UserNotificationService>();
            containerRegistry.RegisterSingleton<IMainWindowServicesFacade, MainWindowServicesFacade>();
            containerRegistry.RegisterSingleton<IStandardErrorHandler, StandardErrorHandler>();
            containerRegistry.RegisterSingleton<IKeyboardShortcutService, KeyboardShortcutService>();
            containerRegistry.RegisterSingleton<IFeatureToggleService, FeatureToggleService>();
            containerRegistry.RegisterSingleton<IPrescriptionSettingsService, PrescriptionSettingsService>(); // OpenSpec: enhance-duplicate-herb-dialog
            containerRegistry.RegisterSingleton<IClinicSettingsService, ClinicSettingsService>(); // OpenSpec: print-prescription-slip
            containerRegistry.RegisterSingleton<IRoleNavigationService, RoleNavigationService>();
            containerRegistry.RegisterSingleton<ICommonDialogService, CommonDialogService>();
            containerRegistry.RegisterSingleton<IUserExperienceService>(resolver =>
            {
                var logger = resolver.Resolve<ILogger<UserExperienceService>>();
                var notificationService = resolver.Resolve<INotificationService>();
                var tickService = resolver.Resolve<IApplicationTickService>();
                return new UserExperienceService(logger, notificationService, tickService);
            });
        }

        /// <summary>注册全局命令和模块管理服务</summary>
        private static void RegisterCommandServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IApplicationCommands, ApplicationCommands>();
            containerRegistry.RegisterSingleton<IModuleLoadingService, ModuleLoadingService>();
        }

        /// <summary>注册应用程序启动服务</summary>
        private static void RegisterApplicationServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IApplicationInitializationService, ApplicationInitializationService>();
            containerRegistry.RegisterSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
            containerRegistry.RegisterSingleton<IApplicationStateService, ApplicationStateService>();

            // Shell启动流程重构 - Phase 1 新增服务
            containerRegistry.RegisterSingleton<IApplicationLifecycle, ApplicationLifecycle>();
            containerRegistry.RegisterSingleton<ISessionLifecycleManager, SessionLifecycleManager>();
            containerRegistry.RegisterSingleton<IStartupDiagnostics, StartupDiagnostics>();

            // Shell启动流程重构 - Phase 2 新增服务
            containerRegistry.RegisterSingleton<ILoginCoordinator, LoginCoordinator>();

            // Shell启动流程重构 - Phase 3 新增服务
            containerRegistry.RegisterSingleton<IStartupPipeline, StartupPipeline>();
            containerRegistry.Register<IStartupStep, ErrorHandlingStartupStep>("ErrorHandling");
            containerRegistry.Register<IStartupStep, ModuleCoordinatorStartupStep>("ModuleCoordinator");
            containerRegistry.Register<IStartupStep, CoreServicesStartupStep>("CoreServices");
            containerRegistry.Register<IStartupStep, ApiHealthCheckStartupStep>("ApiHealthCheck");
            containerRegistry.Register<IStartupStep, WarmupStartupStep>("Warmup");

            // Shell架构整合 - HealthCheckCoordinator服务
            containerRegistry.RegisterSingleton<IHealthCheckCoordinator, HealthCheckCoordinator>();
        }
    }
}
