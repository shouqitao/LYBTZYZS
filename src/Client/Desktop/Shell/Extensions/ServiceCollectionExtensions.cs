using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Roles;
using LYBT.Desktop.Infrastructure.Roles.Definitions;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.Services.Notifications;
// OpenSpec: create-printing-module - 独立打印模块
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Desktop.Printing.Services;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.Services.HealthCheck;
using LYBT.Desktop.Shell.Services.Login;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Desktop.Shell.Services.Startup;
using LYBT.Desktop.Shell.Services.Startup.Steps;
using LYBT.Shared.Configuration.Extensions;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.ExceptionHandling.Handlers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prism.Ioc;
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
            containerRegistry.RegisterLogging();
            RegisterCacheServices(containerRegistry);

            // OpenSpec: implement-local-mode - 读取连接模式并注册对应的 DataSource
            var connectionMode = GetConnectionMode(configuration);
            // S6-02: 注册 ConnectionMode 为单例，供 MenuManager 等服务使用
            containerRegistry.RegisterInstance(connectionMode);
            containerRegistry.RegisterDataSources(connectionMode, configuration);
            containerRegistry.RegisterDataSourceLoggers(connectionMode);

            containerRegistry.RegisterHttpServices(configuration);
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

        /// <summary>注册缓存服务</summary>
        private static void RegisterCacheServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IMemoryCache>(() => new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1000, CompactionPercentage = 0.25, ExpirationScanFrequency = TimeSpan.FromMinutes(5)
            }));
            containerRegistry.RegisterSingleton<IDesktopCacheManager, LYBT.Desktop.Foundation.Caching.DesktopCacheManager>();
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
