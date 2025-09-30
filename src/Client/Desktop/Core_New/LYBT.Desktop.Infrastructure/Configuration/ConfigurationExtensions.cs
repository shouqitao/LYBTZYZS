using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.Infrastructure.Configuration
{
    /// <summary>
    /// 配置扩展类 - 基础设施配置
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// 添加基础设施配置
        /// </summary>
        public static IServiceCollection AddInfrastructureConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // 基础配置逻辑
            return services;
        }
    }
}