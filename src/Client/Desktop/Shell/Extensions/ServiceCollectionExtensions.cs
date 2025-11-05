using System.Net.Http;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Commands;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

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
            RegisterLoggerFactory(containerRegistry);
            RegisterInfrastructureLoggers(containerRegistry);
            RegisterFoundationLoggers(containerRegistry);
            RegisterPresentationAndShellLoggers(containerRegistry);
            RegisterModuleLoggers(containerRegistry);
            RegisterRepositoryLoggers(containerRegistry);
            RegisterServiceLoggers(containerRegistry);
        }


        /// <summary>
        /// 注册LoggerFactory
        /// Issue #1789: 从RegisterLogging提取，封装LoggerFactory注册逻辑
        /// </summary>
        private static void RegisterLoggerFactory(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ILoggerFactory>(() =>
            {
                return LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));
            });
        }

        /// <summary>
        /// 通用Logger注册辅助方法
        /// Issue #1789: 从RegisterLogging提取，减少重复代码
        /// </summary>
        private static void RegisterLogger<T>(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ILogger<T>>(
                resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<T>());
        }

        /// <summary>
        /// 注册Infrastructure层Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterInfrastructureLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<LYBT.Desktop.Infrastructure.Services.MainWindowServicesFacade>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Infrastructure.Services.StandardErrorHandler>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Infrastructure.Services.KeyboardShortcutService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Infrastructure.Services.RoleNavigationService>(containerRegistry);
        }

        /// <summary>
        /// 注册Foundation层Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterFoundationLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<LYBT.Desktop.Foundation.Http.ApiService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Http.AuthorizationMessageHandler>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Security.AuthenticationService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Security.TokenStorageService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Security.UsernameStorageService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Security.SecureCredentialStorage>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Modules.ModuleLoadingService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Performance.StartupOptimizationService>(containerRegistry);
        }

        /// <summary>
        /// 注册Presentation和Shell层Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterPresentationAndShellLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<LYBT.Desktop.Presentation.Notifications.NotificationService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Presentation.Notifications.UnifiedErrorHandlingService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Shell.App>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Shell.Services.ApplicationInitializationService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Shell.Services.Bootstrap.ApplicationBootstrapper>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Application.ApplicationStateService>(containerRegistry); // Issue #1823: API健康检查前置
            RegisterLogger<LYBT.Desktop.Shell.Services.NavigationManager>(containerRegistry); // Issue #1823: NavigationManager需要Logger
            RegisterLogger<LYBT.Desktop.Shell.Services.MenuManager>(containerRegistry); // Issue #1823: MenuManager需要Logger
        }

        /// <summary>
        /// 注册业务模块Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterModuleLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<LYBT.Desktop.Auth.AuthenticationModule>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Users.UsersModule>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Patients.PatientsModule>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Consultation.ConsultationModule>(containerRegistry);
            RegisterLogger<LYBT.Desktop.MedicalCase.MedicalCaseModule>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Prescriptions.PrescriptionsModule>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Herbs.HerbsModule>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Formula.FormulaModule>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Clinical.ClinicalModule>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Admin.AdminModule>(containerRegistry);
        }

        /// <summary>
        /// 注册Repository层Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterRepositoryLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<LYBT.Desktop.Users.Repositories.UserRepository>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Patients.Repositories.PatientRepository>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Herbs.Repositories.HerbRepository>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Formula.Repositories.FormulaRepository>(containerRegistry);
            RegisterLogger<LYBT.Desktop.MedicalCase.Repositories.MedicalCaseRepository>(containerRegistry);
        }

        /// <summary>
        /// 注册业务服务Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterServiceLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<LYBT.Desktop.Prescriptions.Services.PrescriptionEditorService>(containerRegistry);
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
            var apiBaseUrl = config["Lybt:Client:Api:BaseUrl"] ?? "https://localhost:5001";
            var ignoreSslErrors = config.GetValue<bool>("Lybt:Client:Api:IgnoreSslErrors", false);

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

            // Issue #1825: 连接设置服务（远程/本地模式切换）- Auth/Services
            containerRegistry.RegisterSingleton<LYBT.Desktop.Auth.Services.IConnectionSettingsService,
                LYBT.Desktop.Auth.Services.ConnectionSettingsService>();

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
        /// Issue #1790: 添加NavigationManager和MenuManager注册
        /// </summary>
        private static void RegisterPresentationServices(IContainerRegistry containerRegistry)
        {
            // 通知服务 - Presentation/Notifications
            containerRegistry.RegisterSingleton<LYBT.Desktop.Presentation.Notifications.INotificationService,
                LYBT.Desktop.Presentation.Notifications.NotificationService>();

            // 错误处理服务 - Presentation/Notifications
            containerRegistry.RegisterSingleton<LYBT.Desktop.Presentation.Notifications.IErrorHandlingService,
                LYBT.Desktop.Presentation.Notifications.UnifiedErrorHandlingService>();

            // Issue #1790: 注册Shell层导航和菜单管理器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.NavigationManager>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.MenuManager>();

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

            // Issue #1553: 角色导航服务 - 根据用户角色导航到对应的主页
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Interfaces.IRoleNavigationService,
                LYBT.Desktop.Infrastructure.Services.RoleNavigationService>();

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

            // 应用程序状态服务 - Issue #1823: API健康检查前置优化
            containerRegistry.RegisterSingleton<LYBT.Desktop.Foundation.Application.IApplicationStateService,
                LYBT.Desktop.Foundation.Application.ApplicationStateService>();
        }
    }
}
