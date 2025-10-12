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

            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
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
            // 创建 ServiceCollection 配置 HttpClient
            var services = new ServiceCollection();

            // 获取 API 配置
            var apiBaseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
            var ignoreSslErrors = config.GetValue<bool>("ApiSettings:IgnoreSslErrors", false);

            // 配置 HttpClient
            services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (ignoreSslErrors)
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                }
                return handler;
            });

            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            containerRegistry.RegisterInstance<IHttpClientFactory>(httpClientFactory);
            containerRegistry.RegisterSingleton<HttpClient>(() => httpClientFactory.CreateClient("ApiClient"));
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
