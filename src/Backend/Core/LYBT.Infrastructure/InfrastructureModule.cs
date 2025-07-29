using LYBT.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Infrastructure {

    /// <summary>
    /// 基础设施模块入口
    /// 提供统一的服务注册和配置管理
    /// </summary>
    public static class InfrastructureModule {

        /// <summary>
        /// 添加完整的基础设施服务（推荐使用）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
            return services.AddInfrastructureServices(configuration);
        }

        /// <summary>
        /// 添加认证模块
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAuthenticationModule(this IServiceCollection services, IConfiguration configuration) {
            services.AddJwtAuthentication(configuration);
            services.AddAuthConfiguration(configuration);
            return services;
        }

        /// <summary>
        /// 添加缓存模块
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCachingModule(this IServiceCollection services, IConfiguration configuration) {
            services.AddCachingServices(configuration);
            return services;
        }

        /// <summary>
        /// 添加统一日志模块
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLoggingModule(this IServiceCollection services) {
            services.AddUnifiedLogging();
            return services;
        }

        /// <summary>
        /// 添加统一配置模块
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddConfigurationModule(this IServiceCollection services) {
            services.AddUnifiedConfiguration();
            return services;
        }

        /// <summary>
        /// 添加数据库模块
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabaseModule(this IServiceCollection services, IConfiguration configuration) {
            services.AddInfrastructureDbContext(configuration);
            return services;
        }

        /// <summary>
        /// 添加存储模块
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddStorageModule(this IServiceCollection services, IConfiguration configuration) {
            // TODO: 添加存储服务配置
            return services;
        }

        /// <summary>
        /// 添加核心模块（日志 + 配置 + 数据库）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCoreModules(this IServiceCollection services, IConfiguration configuration) {
            services.AddDatabaseModule(configuration);
            services.AddLoggingModule();
            services.AddConfigurationModule();
            return services;
        }
    }
}