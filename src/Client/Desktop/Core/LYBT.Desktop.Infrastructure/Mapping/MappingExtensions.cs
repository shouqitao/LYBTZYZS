using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.Infrastructure.Mapping
{
    /// <summary>
    /// 映射扩展类 - AutoMapper配置
    /// </summary>
    public static class MappingExtensions
    {
        /// <summary>
        /// 添加映射配置
        /// </summary>
        public static IServiceCollection AddMappingConfiguration(this IServiceCollection services)
        {
            // AutoMapper配置逻辑
            return services;
        }
    }
}
