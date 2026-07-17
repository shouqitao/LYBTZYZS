using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Shared.Models;
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
using LYBT.Desktop.Navigation;
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

            containerRegistry.RegisterRepositories(configuration);

            containerRegistry.RegisterHttpServices(configuration);
            containerRegistry.AddUnifiedApiClient(configuration);
            RegisterFoundationServices(containerRegistry);
            RegisterPresentationServices(containerRegistry);
            RegisterInfrastructureServices(containerRegistry);
            RegisterCommandServices(containerRegistry);
            RegisterApplicationServices(containerRegistry);
            containerRegistry.AddViewModelServices();
        }

        /// <summary>注册配置服务</summary>
        private static IConfiguration RegisterConfiguration(IContainerRegistry containerRegistry)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("clinic-settings.json", optional: true, reloadOnChange: true)
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
            containerRegistry.RegisterSingleton<ITokenManager, TokenManager>();
            containerRegistry.RegisterSingleton<ICredentialVault, CredentialVault>();
            containerRegistry.RegisterSingleton<IPhotoStorageService, DpapiPhotoStorageService>(); // C2: 照片 DPAPI 加密存储
            containerRegistry.RegisterSingleton<IAuthenticationStateMachine, AuthenticationStateMachine>();
            containerRegistry.RegisterSingleton<ILogoutService, LogoutService>();
            containerRegistry.RegisterSingleton<ITokenValidator, LocalTokenValidator>();
            containerRegistry.RegisterSingleton<IUsernameStorageService, UsernameStorageService>();
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
            containerRegistry.RegisterSingleton<MenuManager>();
            containerRegistry.RegisterSingleton<INavigationCoordinator, NavigationCoordinator>();
            containerRegistry.RegisterSingleton<NavigationManager>();
            containerRegistry.RegisterSingleton<StatusBarManager>();

            // Navigation 服务拆分
            containerRegistry.RegisterSingleton<INavigationHistoryService, NavigationHistoryService>();
            containerRegistry.RegisterSingleton<IModuleLazyLoader, ModuleLazyLoader>();
            containerRegistry.RegisterSingleton<IRegionMonitor, RegionMonitor>();
            containerRegistry.RegisterSingleton<ShellDialogHelper>();
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

            containerRegistry.RegisterSingleton<IPrescriptionSettingsService, PrescriptionSettingsService>();
            containerRegistry.RegisterSingleton<IClinicSettingsService, ClinicSettingsService>();
            containerRegistry.RegisterSingleton<ICommonDialogService, CommonDialogService>();
            // IPrintService<T> 由 PrintingModule 注册，此处不重复

            // refactor-auth-role-system Phase 2.1: 可扩展角色注册表
            containerRegistry.RegisterSingleton<IRoleRegistry>(resolver =>
            {
                var logger = resolver.Resolve<ILogger<RoleRegistry>>();
                var registry = new RoleRegistry(logger);

                // 注册内置角色定义 (refactor-auth-role-system Phase 2.3.3)
                registry.Register(new AdminRoleDefinition());
                registry.Register(new SuperAdminRoleDefinition());
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
            // IApplicationBootstrapper 由 App.xaml.cs 注册，此处不重复
            containerRegistry.RegisterSingleton<IApplicationStateService, ApplicationStateService>();

            // Shell启动流程重构 - Phase 1 新增服务
            containerRegistry.RegisterSingleton<ISessionLifecycleManager, SessionLifecycleManager>();

            // Shell启动流程重构 - Phase 2 新增服务
            containerRegistry.RegisterSingleton<ILoginCoordinator, LoginCoordinator>();

            // MainWindowViewModel 拆分 - 登录状态管理与事件协调
            containerRegistry.RegisterSingleton<ILoginStateManager, LoginStateManager>();
            containerRegistry.RegisterSingleton<ShellEventCoordinator>();

            // Shell启动流程重构 - Phase 3 新增服务
            containerRegistry.RegisterSingleton<IStartupPipeline, StartupPipeline>();
            containerRegistry.Register<IStartupStep, ErrorHandlingStartupStep>("ErrorHandling");
            containerRegistry.Register<IStartupStep, ModuleCoordinatorStartupStep>("ModuleCoordinator");
            containerRegistry.Register<IStartupStep, LocalWebApiStartupStep>("LocalWebApi");
            // API健康检查 - 5秒超时，后台异步执行（Transient生命周期，每次解析新实例）
            containerRegistry.Register<ApiHealthCheckStartupStep>();
            containerRegistry.Register<IStartupStep, WarmupStartupStep>("Warmup");

            // 全局API健康监控器（断路器+订阅模式）
            containerRegistry.RegisterSingleton<IApiHealthMonitor, ApiHealthMonitor>();

            // 连接配置服务（URL持久化 + IsLocal判断）
            containerRegistry.RegisterSingleton<IConnectionSettingsService, ConnectionSettingsService>();

            // API路由器（查询当前连接状态）
            containerRegistry.RegisterSingleton<IApiRouter, ApiRouter>();
        }
    }
}
