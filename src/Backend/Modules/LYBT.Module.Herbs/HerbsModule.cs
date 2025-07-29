using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Repositories;
using LYBT.Module.Herbs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Herbs {

    /// <summary>
    /// 药材管理模块服务注册入口
    /// </summary>
    public static class HerbsModule {

        /// <summary>
        /// 注册药材相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IHerbRepository, HerbRepository>();
            services.AddScoped<IHerbService, HerbService>();
        }
    }
}