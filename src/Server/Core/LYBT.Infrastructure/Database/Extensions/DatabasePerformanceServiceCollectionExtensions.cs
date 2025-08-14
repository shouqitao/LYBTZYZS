using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using LYBT.Infrastructure.Database.Performance;

namespace LYBT.Infrastructure.Database.Extensions
{
    /// <summary>
    /// 数据库性能服务配置扩展 - UltraThink重构数据库优化
    /// 注册数据库性能监控和分析相关服务
    /// </summary>
    public static class DatabasePerformanceServiceCollectionExtensions
    {
        /// <summary>
        /// 添加数据库性能监控服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabasePerformanceServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // 配置选项
            services.Configure<DatabasePerformanceOptions>(
                configuration.GetSection("DatabasePerformance"));

            // 注册性能分析服务
            services.AddScoped<QueryPerformanceAnalyzer>();
            services.AddScoped<IDatabasePerformanceService, DatabasePerformanceService>();

            // 注册后台性能监控服务（可选）
            services.AddHostedService<DatabasePerformanceBackgroundService>();

            return services;
        }

        /// <summary>
        /// 添加数据库性能监控服务（使用默认配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabasePerformanceServices(
            this IServiceCollection services)
        {
            // 使用默认配置
            services.Configure<DatabasePerformanceOptions>(options =>
            {
                options.EnableCaching = true;
                options.CacheExpirationMinutes = 30;
                options.SlowQueryThresholdMs = 1000;
                options.AutoRunBenchmarks = false;
                options.AutoRunIntervalHours = 24;
            });

            services.AddScoped<QueryPerformanceAnalyzer>();
            services.AddScoped<IDatabasePerformanceService, DatabasePerformanceService>();

            return services;
        }

        /// <summary>
        /// 添加数据库性能监控服务（自定义配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configure">配置委托</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabasePerformanceServices(
            this IServiceCollection services,
            Action<DatabasePerformanceOptions> configure)
        {
            services.Configure(configure);

            services.AddScoped<QueryPerformanceAnalyzer>();
            services.AddScoped<IDatabasePerformanceService, DatabasePerformanceService>();

            return services;
        }
    }
}