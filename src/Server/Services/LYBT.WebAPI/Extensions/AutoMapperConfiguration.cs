using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LYBT.WebAPI.Extensions
{

    /// <summary>
    /// AutoMapper 配置扩展
    /// </summary>
    public static class AutoMapperConfiguration
    {

        /// <summary>
        /// 添加 AutoMapper 配置（兼容 AutoMapper 15.0.1）
        /// </summary>
        public static IServiceCollection AddAutoMapperConfiguration(this IServiceCollection services)
        {
            // 获取所有包含 MappingProfile 的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name?.StartsWith("LYBT.") == true)
                .ToArray();

            // 使用 AddAutoMapper 扩展方法（AutoMapper 15.0.1 方式）
            services.AddAutoMapper(cfg =>
            {
                // 扫描程序集中的所有 Profile
                cfg.AddMaps(assemblies);
            }, assemblies);

            return services;
        }
    }
}