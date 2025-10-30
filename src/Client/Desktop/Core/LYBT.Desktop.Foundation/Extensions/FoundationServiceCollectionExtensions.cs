using System.Net.Http;
using LYBT.Desktop.Foundation.Caching;
using LYBT.Desktop.Foundation.Configuration;
using LYBT.Desktop.Foundation.Diagnostics;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.Foundation.Extensions
{
    /// <summary>
    /// Desktop Foundation 服务注册扩展方法
    /// Issue #1114 Phase 1 - 技术基础设施层服务注册
    /// </summary>
    public static class FoundationServiceCollectionExtensions
    {
        /// <summary>
        /// 注册Desktop Foundation层服务（技术基础设施）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDesktopFoundation(this IServiceCollection services, IConfiguration configuration)
        {
            // 缓存服务
            services.AddSingleton<CacheService>();

            // 配置服务
            services.AddSingleton<ConfigurationService>();

            // 注意：DiagnosticService是静态工具类，无需注册

            // 安全服务
            services.AddSingleton<SecurityService>();

            // 启动优化服务
            services.AddSingleton<IStartupOptimizationService, StartupOptimizationService>();

            // 模块加载服务
            services.AddSingleton<IModuleLoadingService, ModuleLoadingService>();

            // API健康检查服务
            services.AddSingleton<IApiHealthCheckService, ApiHealthCheckService>();

            // HTTP消息处理器
            services.AddTransient<AuthorizationMessageHandler>();

            return services;
        }

        /// <summary>
        /// 配置Foundation HTTP客户端（带认证和重试策略）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection ConfigureFoundationHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            var apiBaseUrl = configuration["Lybt:Client:Api:BaseUrl"] ?? "https://localhost:5001";

            services.AddHttpClient("FoundationClient", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthorizationMessageHandler>();

            return services;
        }
    }
}
