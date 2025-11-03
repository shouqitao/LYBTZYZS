using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.Presentation.Mapping
{
    /// <summary>
    /// Presentation层映射扩展类
    /// </summary>
    public static class PresentationMappingExtensions
    {
        /// <summary>
        /// 添加Presentation层映射配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddPresentationMapping(this IServiceCollection services)
        {
            // 添加AutoMapper配置
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<PatientSelectorMappingProfile>();
            });

            return services;
        }
    }
}
