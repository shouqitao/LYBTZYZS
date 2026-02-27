using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Desktop.Admin;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.Infrastructure.Http;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Clinical;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Security;
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
using LYBT.Desktop.Herbs;
using LYBT.Desktop.Herbs.Repositories;
// OpenSpec: simplify-desktop-data-layer - HerbService已删除，功能合并到Repository
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Roles;
using LYBT.Desktop.Infrastructure.Roles.Definitions;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.Services.Notifications;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.Patients;
// OpenSpec: create-printing-module - 独立打印模块
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Desktop.Printing.Services;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Patients.Services; // PatientService
using LYBT.Desktop.Patients.ViewModels.Components;
// [已删除] using LYBT.Desktop.Prescriptions - 模块已移除
// [已删除] using LYBT.Desktop.Prescriptions.Services - 服务已迁移到MedicalCase
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.Services.HealthCheck;
using LYBT.Desktop.Shell.Services.Login;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Desktop.Shell.Services.Startup;
using LYBT.Desktop.Shell.Services.Startup.Steps;
using LYBT.Desktop.Users;
using LYBT.Desktop.Users.Repositories;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Configuration.Extensions;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.ExceptionHandling.Handlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

            // OpenSpec: implement-local-mode - 读取连接模式并注册对应的 DataSource
            var connectionMode = GetConnectionMode(configuration);
            // S6-02: 注册 ConnectionMode 为单例，供 MenuManager 等服务使用
            containerRegistry.RegisterInstance(connectionMode);
            containerRegistry.RegisterDataSources(connectionMode);
            RegisterDataSourceLoggers(containerRegistry, connectionMode);

            RegisterHttpServices(containerRegistry, configuration);
            RegisterFoundationServices(containerRegistry);
            RegisterPresentationServices(containerRegistry);
            RegisterInfrastructureServices(containerRegistry);
            RegisterCommandServices(containerRegistry);
            RegisterApplicationServices(containerRegistry);

            // OpenSpec: refactor-viewmodel-composition - 注册ViewModel组合服务
            containerRegistry.AddViewModelServices();
        }

        /// <summary>
        /// 从配置文件获取连接模式
        /// OpenSpec: implement-local-mode
        /// </summary>
        private static ConnectionMode GetConnectionMode(IConfiguration configuration)
        {
            var modeString = configuration["ConnectionMode"];
            if (Enum.TryParse<ConnectionMode>(modeString, ignoreCase: true, out var mode))
            {
                Log.Information("[LocalMode] 使用配置文件指定的连接模式: {Mode}", mode);
                return mode;
            }

            // 默认使用远程模式
            Log.Information("[LocalMode] 未指定连接模式，使用默认远程模式");
            return ConnectionMode.Remote;
        }

        /// <summary>
        /// 注册 DataSource 相关 Logger
        /// OpenSpec: implement-local-mode
        /// </summary>
        private static void RegisterDataSourceLoggers(IContainerRegistry containerRegistry, ConnectionMode mode)
        {
            if (mode == ConnectionMode.Local)
            {
                RegisterLogger<LYBT.Desktop.LocalData.Context.LocalDbContext>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.Initialization.DatabaseInitializer>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.Services.LocalAuthService>(containerRegistry);
                // OpenSpec: implement-data-sync
                RegisterLogger<LYBT.Desktop.LocalData.Services.SyncService>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalPatientDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalHerbDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalFormulaDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalMedicalCaseDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalUserDataSource>(containerRegistry);
            }
            else
            {
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemotePatientDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemoteHerbDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemoteFormulaDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemoteMedicalCaseDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemoteUserDataSource>(containerRegistry);
            }
        }

        /// <summary>注册配置服务</summary>
        private static IConfiguration RegisterConfiguration(IContainerRegistry containerRegistry)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            containerRegistry.RegisterInstance<IConfiguration>(configuration);

            // unify-configuration-system: 注册强类型配置
            containerRegistry.AddLybtClientConfiguration(configuration);

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
            // [已删除] RegisterLogger<RoleNavigationService> - OpenSpec: unify-navigation-architecture (ADR-7)
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
            RegisterLogger<TokenManager>(containerRegistry); // OpenSpec: refactor-login-authentication
            RegisterLogger<CredentialVault>(containerRegistry); // OpenSpec: refactor-login-authentication
            RegisterLogger<AuthenticationStateMachine>(containerRegistry); // OpenSpec: refactor-auth-role-system (Phase 1.1)
            RegisterLogger<LogoutService>(containerRegistry); // OpenSpec: refactor-login-authentication (Phase 2.3)
            RegisterLogger<UsernameStorageService>(containerRegistry);
            // OpenSpec: remove-secure-credential-storage - SecureCredentialStorage已移除
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
            // [已删除] RegisterLogger<NavigationManager> - OpenSpec: unify-navigation-architecture (ADR-7)
            RegisterLogger<MenuManager>(containerRegistry);
            RegisterLogger<NavigationCoordinator>(containerRegistry);

            // Shell启动流程重构 - Phase 1 新增Logger
            RegisterLogger<SessionLifecycleManager>(containerRegistry);

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
            RegisterLogger<MedicalCaseModule>(containerRegistry);
            // [已删除] RegisterLogger<PrescriptionsModule> - 模块已移除
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
            // [已删除] RegisterLogger<PrescriptionEditorService> - 服务已删除
            RegisterLogger<SystemSettingsService>(containerRegistry);
            // OpenSpec: create-printing-module - 打印服务Logger
            RegisterLogger<PrescriptionPrintService>(containerRegistry);
        }

        /// <summary>注册Component层Logger（CommandHandler/DataManager/Validator等）</summary>
        private static void RegisterComponentLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<UserService>(containerRegistry);
            // OpenSpec: standardize-service-layer - 统一使用Service命名
            RegisterLogger<FormulaService>(containerRegistry);
            RegisterLogger<PatientService>(containerRegistry);
            // OpenSpec: simplify-desktop-data-layer - HerbService已删除，功能合并到HerbRepository
            RegisterLogger<MedicalCaseService>(containerRegistry);
            // OpenSpec: cleanup-patient-dead-code - PatientStateManager已删除（死代码）
            RegisterLogger<PatientValidator>(containerRegistry);
            // LOG-012: LoggingHttpHandler日志
            RegisterLogger<LoggingHttpHandler>(containerRegistry);
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
            // unify-configuration-system: 使用强类型配置
            var apiOptions = new ApiClientOptions();
            config.GetSection(ApiClientOptions.SectionName).Bind(apiOptions);
            var apiBaseUrl = apiOptions.BaseUrl;
            var ignoreSslErrors = apiOptions.IgnoreSslErrors;

            containerRegistry.RegisterSingleton<AuthorizationMessageHandler>();
            containerRegistry.RegisterSingleton<TokenRefreshHandler>(resolver =>
            {
                var tokenStorage = resolver.Resolve<ITokenStorageService>();
                var credentialVault = resolver.Resolve<ICredentialVault>();
                var configuration = resolver.Resolve<IConfiguration>();
                var logger = resolver.Resolve<ILogger<TokenRefreshHandler>>();
                IUserActivityState? userActivityState = null;
                try { userActivityState = resolver.Resolve<IUserActivityState>(); }
                catch { /* 启动阶段可能尚未注册 */ }
                return new TokenRefreshHandler(tokenStorage, credentialVault, configuration, logger, userActivityState);
            });
            // OpenSpec: refactor-login-authentication (Phase 1.4) - 注册接口
            containerRegistry.Register<ITokenRefreshHandler>(resolver => resolver.Resolve<TokenRefreshHandler>());

            // LOG-012: 注册LoggingHttpHandler
            containerRegistry.RegisterSingleton<LoggingHttpHandler>();

            // Handler链: HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler → HttpClient
            // LOG-012 & LOG-013: LoggingHttpHandler记录请求/响应并添加traceparent header
            containerRegistry.RegisterSingleton<HttpClient>(resolver =>
            {
                var httpHandler = new HttpClientHandler();
                if (ignoreSslErrors)
                    httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                var tokenRefreshHandler = resolver.Resolve<TokenRefreshHandler>();
                tokenRefreshHandler.InnerHandler = httpHandler;
                var authHandler = resolver.Resolve<AuthorizationMessageHandler>();
                authHandler.InnerHandler = tokenRefreshHandler;
                // LOG-012: 添加日志Handler到链中
                var loggingHandler = resolver.Resolve<LoggingHttpHandler>();
                loggingHandler.InnerHandler = authHandler;

                return new HttpClient(loggingHandler) { BaseAddress = new Uri(apiBaseUrl), Timeout = TimeSpan.FromSeconds(30) };
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
            // OpenSpec: implement-data-sync - 同步API客户端
            containerRegistry.RegisterSingleton<ISyncApi>(r => RestService.For<ISyncApi>(r.Resolve<HttpClient>(), refitSettings));
        }

        /// <summary>注册Foundation层服务</summary>
        private static void RegisterFoundationServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IAuthenticationService, AuthenticationService>();
            containerRegistry.RegisterSingleton<ITokenStorageService, TokenStorageService>();
            containerRegistry.RegisterSingleton<ITokenManager, TokenManager>(); // OpenSpec: refactor-login-authentication
            containerRegistry.RegisterSingleton<ICredentialVault, CredentialVault>(); // OpenSpec: refactor-login-authentication
            containerRegistry.RegisterSingleton<IAuthenticationStateMachine, AuthenticationStateMachine>(); // OpenSpec: refactor-auth-role-system (Phase 1.1)
            containerRegistry.RegisterSingleton<ILogoutService, LogoutService>(); // OpenSpec: refactor-login-authentication (Phase 2.3)
            containerRegistry.RegisterSingleton<ITokenValidator, LocalTokenValidator>();
            containerRegistry.RegisterSingleton<IUsernameStorageService, UsernameStorageService>();
            // OpenSpec: remove-secure-credential-storage - ISecureCredentialStorage已移除
            // OpenSpec: refactor-startup-connection-resilience - IConnectionSettingsService已移除
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
            // [已删除] NavigationManager - OpenSpec: unify-navigation-architecture (ADR-7)
            containerRegistry.RegisterSingleton<MenuManager>();
            // OpenSpec: unify-navigation-architecture (ADR-3 + ADR-7) - 统一导航入口
            containerRegistry.RegisterSingleton<INavigationCoordinator, NavigationCoordinator>();
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
                // unify-configuration-system: 使用强类型配置
                var sessionOptions = resolver.Resolve<ClientSessionOptions>();
                return new UserActivityTracker(
                    logger,
                    tickService,
                    sessionOptions.InactivityTimeoutMinutes,
                    sessionOptions.WarningBeforeTimeoutMinutes,
                    sessionOptions.ActivityCheckIntervalSeconds);
            });
            containerRegistry.RegisterSingleton<IUserActivityTracker>(resolver => resolver.Resolve<UserActivityTracker>());
            containerRegistry.RegisterSingleton<IUserActivityState>(resolver => resolver.Resolve<UserActivityTracker>());
            containerRegistry.RegisterSingleton<IUserNotificationService, UserNotificationService>();
            containerRegistry.RegisterSingleton<IMainWindowServicesFacade, MainWindowServicesFacade>();
            containerRegistry.RegisterSingleton<IPrescriptionSettingsService, PrescriptionSettingsService>(); // OpenSpec: enhance-duplicate-herb-dialog
            containerRegistry.RegisterSingleton<IClinicSettingsService, ClinicSettingsService>(); // OpenSpec: print-prescription-slip
            // [已删除] IRoleNavigationService - OpenSpec: unify-navigation-architecture (ADR-7)
            containerRegistry.RegisterSingleton<ICommonDialogService, CommonDialogService>();
            // IPrintService<T> 由 PrintingModule 注册，此处不重复

            // refactor-auth-role-system Phase 2.1: 可扩展角色注册表
            containerRegistry.RegisterSingleton<IRoleRegistry>(resolver =>
            {
                var logger = resolver.Resolve<ILogger<RoleRegistry>>();
                var registry = new RoleRegistry(logger);

                // 注册内置角色定义 (refactor-auth-role-system Phase 2.3.3)
                registry.Register(new SuperAdminRoleDefinition());
                registry.Register(new AdminRoleDefinition());
                registry.Register(new DoctorRoleDefinition());
                registry.Register(new ReceptionistRoleDefinition());

                return registry;
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
            // IApplicationBootstrapper 由 App.xaml.cs 注册，此处不重复
            containerRegistry.RegisterSingleton<IApplicationStateService, ApplicationStateService>();

            // Shell启动流程重构 - Phase 1 新增服务
            containerRegistry.RegisterSingleton<ISessionLifecycleManager, SessionLifecycleManager>();

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
