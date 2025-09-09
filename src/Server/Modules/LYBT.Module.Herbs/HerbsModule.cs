using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Mapping;
using LYBT.Module.Herbs.Repositories;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Herbs
{

    /// <summary>
    /// 药材模块注册 - UltraThink标准化重构
    /// 负责注册药材相关的所有服务、仓储和映射配置
    /// 采用UltraThink双层架构：QueryService + BusinessService 专业分离
    /// </summary>
    public static class HerbsModule
    {

        /// <summary>
        /// 注册药材模块服务 - UltraThink双层架构标准
        /// </summary>
        public static IServiceCollection AddHerbsModule(this IServiceCollection services)
        {
            // 仓储层
            services.AddScoped<IHerbRepository, HerbRepository>();

            // UltraThink双层架构服务 - 查询和业务逻辑分离
            services.AddScoped<HerbQueryService>();
            services.AddScoped<HerbBusinessService>();

            // 主服务 - UltraThink纯委托模式，委托给专业服务层
            services.AddScoped<IHerbService, HerbService>();

            // AutoMapper配置
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<HerbMappingProfile>();
            });

            return services;
        }
    }
}
