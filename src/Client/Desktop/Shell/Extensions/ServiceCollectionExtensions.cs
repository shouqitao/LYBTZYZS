using System.Net.Http;
using LYBT.Desktop.Admin;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Auth.Services;
using LYBT.Desktop.Clinical;
using LYBT.Desktop.Consultation;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Formula;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Formula.ViewModels.Components;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.Herbs.Components;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.MedicalCase.Components;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Desktop.Prescriptions;
using LYBT.Desktop.Prescriptions.Services;
using LYBT.Desktop.Presentation.Notifications;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Users;
using LYBT.Desktop.Users.Repositories;
using LYBT.Desktop.Users.ViewModels.Components;
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
            RegisterComponentLoggers(containerRegistry);
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
            RegisterLogger<MainWindowServicesFacade>(containerRegistry);
            RegisterLogger<StandardErrorHandler>(containerRegistry);
            RegisterLogger<KeyboardShortcutService>(containerRegistry);
            RegisterLogger<RoleNavigationService>(containerRegistry);
        }

        /// <summary>
        /// 注册Foundation层Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterFoundationLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<ApiService>(containerRegistry);
            RegisterLogger<AuthorizationMessageHandler>(containerRegistry);
            RegisterLogger<TokenRefreshHandler>(containerRegistry); // Issue #1838: Token自动刷新
            RegisterLogger<AuthenticationService>(containerRegistry);
            RegisterLogger<TokenStorageService>(containerRegistry);
            RegisterLogger<UsernameStorageService>(containerRegistry);
            RegisterLogger<SecureCredentialStorage>(containerRegistry);
            // Issue #1862-1864: Token认证安全重构 - 新增Logger
            RegisterLogger<SecureTokenStorage>(containerRegistry);
            RegisterLogger<LocalTokenValidator>(containerRegistry);
            RegisterLogger<ModuleLoadingService>(containerRegistry);
            RegisterLogger<StartupOptimizationService>(containerRegistry);
        }

        /// <summary>
        /// 注册Presentation和Shell层Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterPresentationAndShellLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<NotificationService>(containerRegistry);
            RegisterLogger<UnifiedErrorHandlingService>(containerRegistry);
            RegisterLogger<App>(containerRegistry);
            RegisterLogger<ApplicationInitializationService>(containerRegistry);
            RegisterLogger<ApplicationBootstrapper>(containerRegistry);
            RegisterLogger<ApplicationStateService>(containerRegistry); // Issue #1823: API健康检查前置
            RegisterLogger<NavigationManager>(containerRegistry); // Issue #1823: NavigationManager需要Logger
            RegisterLogger<MenuManager>(containerRegistry); // Issue #1823: MenuManager需要Logger
        }

        /// <summary>
        /// 注册业务模块Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
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

        /// <summary>
        /// 注册Repository层Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterRepositoryLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<UserRepository>(containerRegistry);
            RegisterLogger<PatientRepository>(containerRegistry);
            RegisterLogger<HerbRepository>(containerRegistry);
            RegisterLogger<FormulaRepository>(containerRegistry);
            RegisterLogger<MedicalCaseRepository>(containerRegistry);
        }

        /// <summary>
        /// 注册业务服务Logger
        /// Issue #1789: 从RegisterLogging提取，分组管理Logger注册
        /// </summary>
        private static void RegisterServiceLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<PrescriptionEditorService>(containerRegistry);
            RegisterLogger<SystemSettingsService>(containerRegistry); // Epic #1832 Phase 2: SystemSettings服务Logger
        }

        /// <summary>
        /// 注册Component层Logger（CommandHandler等）
        /// 修复管理界面DI错误
        /// </summary>
        private static void RegisterComponentLoggers(IContainerRegistry containerRegistry)
        {
            // CommandHandler Loggers
            RegisterLogger<UserCommandHandler>(containerRegistry);
            RegisterLogger<FormulaCommandHandler>(containerRegistry);
            RegisterLogger<PatientCommandHandler>(containerRegistry); // Issue #1834: 添加PatientCommandHandler Logger

            // DataManager Loggers（Issue #1831: 修复管理界面导航问题 + Logger类型统一）
            RegisterLogger<HerbDataManager>(containerRegistry);
            RegisterLogger<MedicalCaseDataManager>(containerRegistry);
            RegisterLogger<FormulaDataManager>(containerRegistry);
            RegisterLogger<PatientDataManager>(containerRegistry); // 修复PatientDetailView DI错误

            // Validator Loggers（CRUD统一模式升级）
            RegisterLogger<PatientValidator>(containerRegistry); // 修复PatientValidator DI错误

            // Issue #2072: Formula组件Logger（8列DataGrid验方录入功能）
            RegisterLogger<FormulaHerbFilterManager>(containerRegistry); // 修复FormulaDetailView DI错误
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
            containerRegistry.RegisterSingleton<AuthorizationMessageHandler>();

            // Issue #1838: 注册 TokenRefreshHandler
            containerRegistry.RegisterSingleton<TokenRefreshHandler>();

            // Issue #1239 修复 + Issue #1838: 手动创建带有 TokenRefreshHandler 和 AuthorizationMessageHandler 的 HttpClient
            // 不使用 ServiceCollection，因为 Handler 依赖 Prism 容器中的服务
            // Handler 链: HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → HttpClient
            containerRegistry.RegisterSingleton<HttpClient>(resolver =>
            {
                // 1. 创建基础 HttpClientHandler
                var httpHandler = new HttpClientHandler();
                if (ignoreSslErrors)
                {
                    httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                }

                // 2. 从 Prism 容器解析 TokenRefreshHandler（先检查Token过期并刷新）
                var tokenRefreshHandler = resolver.Resolve<TokenRefreshHandler>();
                tokenRefreshHandler.InnerHandler = httpHandler;

                // 3. 从 Prism 容器解析 AuthorizationMessageHandler（添加Bearer Token到请求头）
                var authHandler = resolver.Resolve<AuthorizationMessageHandler>();
                authHandler.InnerHandler = tokenRefreshHandler;

                // 4. 使用 authHandler 创建 HttpClient（自动刷新Token + 自动添加 Bearer Token）
                var httpClient = new HttpClient(authHandler)
                {
                    BaseAddress = new Uri(apiBaseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };

                return httpClient;
            });

            // Issue #1239 修复: 使用延迟解析注册 Refit 客户端（避免在注册阶段解析 HttpClient）
            // 所有 Refit 客户端共享同一个 HttpClient 实例（包含 AuthorizationMessageHandler）
            containerRegistry.RegisterSingleton<IAuthApi>(resolver =>
                Refit.RestService.For<IAuthApi>(resolver.Resolve<HttpClient>()));

            containerRegistry.RegisterSingleton<IPatientApi>(resolver =>
                Refit.RestService.For<IPatientApi>(resolver.Resolve<HttpClient>()));

            containerRegistry.RegisterSingleton<IUserApi>(resolver =>
                Refit.RestService.For<IUserApi>(resolver.Resolve<HttpClient>()));

            containerRegistry.RegisterSingleton<IConsultationApi>(resolver =>
                Refit.RestService.For<IConsultationApi>(resolver.Resolve<HttpClient>()));

            containerRegistry.RegisterSingleton<IHerbApi>(resolver =>
                Refit.RestService.For<IHerbApi>(resolver.Resolve<HttpClient>()));

            containerRegistry.RegisterSingleton<IFormulaApi>(resolver =>
                Refit.RestService.For<IFormulaApi>(resolver.Resolve<HttpClient>()));

            containerRegistry.RegisterSingleton<IMedicalCaseApi>(resolver =>
                Refit.RestService.For<IMedicalCaseApi>(resolver.Resolve<HttpClient>()));

            containerRegistry.RegisterSingleton<IPrescriptionApi>(resolver =>
                Refit.RestService.For<IPrescriptionApi>(resolver.Resolve<HttpClient>()));
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

            // Issue #1862-1864: Token认证安全重构 - 新增服务
            // 加密Token存储（使用DPAPI）
            containerRegistry.RegisterSingleton<ITokenStorage, SecureTokenStorage>();
            // 客户端JWT验证器
            containerRegistry.RegisterSingleton<ITokenValidator, LocalTokenValidator>();

            // Issue #1245 修复: 用户名存储服务 - Foundation/Security
            containerRegistry.RegisterSingleton<IUsernameStorageService, UsernameStorageService>();

            // Issue #1246 修复: 安全凭据存储服务（密码加密）- Foundation/Security
            containerRegistry.RegisterSingleton<ISecureCredentialStorage, SecureCredentialStorage>();

            // Issue #1825: 连接设置服务（远程/本地模式切换）- Auth/Services
            containerRegistry.RegisterSingleton<IConnectionSettingsService, ConnectionSettingsService>();

            // Epic #1832 Phase 2: 系统设置服务 - Admin/Services
            containerRegistry.RegisterSingleton<ISystemSettingsService, SystemSettingsService>();

            // API 健康检查服务 - Foundation/HealthCheck
            containerRegistry.RegisterSingleton<IApiHealthCheckService, ApiHealthCheckService>();

            // Issue #1239 修复: 注册 API 服务基类 - Foundation/Http
            containerRegistry.RegisterSingleton<IApiService, ApiService>();

            // Issue #1239 修复: 注册启动优化服务 - Foundation/Performance
            containerRegistry.RegisterSingleton<IStartupOptimizationService, StartupOptimizationService>();
        }

        /// <summary>
        /// 注册 Presentation 层服务
        /// Issue #1790: 添加NavigationManager和MenuManager注册
        /// </summary>
        private static void RegisterPresentationServices(IContainerRegistry containerRegistry)
        {
            // 通知服务 - Presentation/Notifications
            containerRegistry.RegisterSingleton<INotificationService, NotificationService>();

            // 错误处理服务 - Presentation/Notifications
            containerRegistry.RegisterSingleton<IErrorHandlingService, UnifiedErrorHandlingService>();

            // Issue #1790: 注册Shell层导航和菜单管理器
            containerRegistry.RegisterSingleton<NavigationManager>();
            containerRegistry.RegisterSingleton<MenuManager>();

            // 注意：PatientSelector组件使用反射进行手动映射,不需要AutoMapper配置
            // 原因：Presentation层不能引用Modules层(避免循环依赖)
        }

        /// <summary>
        /// 注册 Infrastructure 层服务
        /// </summary>
        private static void RegisterInfrastructureServices(IContainerRegistry containerRegistry)
        {
            // 会话管理器
            containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();

            // 用户通知服务
            containerRegistry.RegisterSingleton<IUserNotificationService, UserNotificationService>();

            // 主窗口服务门面
            containerRegistry.RegisterSingleton<IMainWindowServicesFacade, MainWindowServicesFacade>();

            // 标准错误处理器
            containerRegistry.RegisterSingleton<IStandardErrorHandler, StandardErrorHandler>();

            // 处方打印服务已移除（等待 Issue #1202 实现新的统一打印系统）
            // 新的打印服务将在 Desktop.Presentation/Print/ 中实现，使用 QuestPDF

            // 键盘快捷键服务
            containerRegistry.RegisterSingleton<IKeyboardShortcutService, KeyboardShortcutService>();

            // 功能开关服务 (Issue #1477 #1479 架构纠正v2)
            containerRegistry.RegisterSingleton<IFeatureToggleService, FeatureToggleService>();

            // Issue #1553: 角色导航服务 - 根据用户角色导航到对应的主页
            containerRegistry.RegisterSingleton<IRoleNavigationService, RoleNavigationService>();

            // Epic #1934: 通用对话框服务 - 支持批量导入/导出功能的文件对话框
            containerRegistry.RegisterSingleton<ICommonDialogService, CommonDialogService>();

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
            containerRegistry.RegisterSingleton<IApplicationInitializationService, ApplicationInitializationService>();

            // 应用程序启动引导服务
            containerRegistry.RegisterSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();

            // 应用程序状态服务 - Issue #1823: API健康检查前置优化
            containerRegistry.RegisterSingleton<IApplicationStateService, ApplicationStateService>();
        }
    }
}
