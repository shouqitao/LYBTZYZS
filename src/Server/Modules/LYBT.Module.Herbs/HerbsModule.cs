using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Repositories;
using LYBT.Module.Herbs.Services;
using LYBT.Module.Herbs.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Herbs
{
    /// <summary>
    /// 药材管理模块服务注册入口 - UltraThink v2.0架构
    /// 注册Service + Helper模式的所有依赖
    /// </summary>
    public static class HerbsModule
    {
        /// <summary>
        /// 注册药材相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services)
        {
            // Repository层
            services.AddScoped<IHerbRepository, HerbRepository>();
            
            // Helper层 - UltraThink Helper模式
            services.AddScoped<HerbQueryHelper>();
            services.AddScoped<HerbValidationHelper>();
            services.AddScoped<HerbBusinessHelper>();
            
            // Service层 - 继承BaseService
            services.AddScoped<IHerbService, HerbService>();
        }
    }
}