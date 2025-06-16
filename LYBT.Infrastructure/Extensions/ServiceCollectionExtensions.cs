using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Infrastructure.Extensions {
    /// <summary>
    /// 基础服务注入扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services) {
            services.AddSingleton<SnowflakeIdGenerator, SnowflakeIdGenerator>();
            return services;
        }
    }
}
