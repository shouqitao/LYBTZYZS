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
            // 仓储层 - 统一实现
            services.AddScoped<IHerbRepository, HerbRepository>();

            // 服务层 - UltraThink架构重构后的统一服务
            services.AddScoped<IHerbService, HerbService>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

            return services;
        }
    }
}
