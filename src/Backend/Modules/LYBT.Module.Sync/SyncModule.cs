using LYBT.Module.Sync.Interfaces;
using LYBT.Module.Sync.Repositories;
using LYBT.Module.Sync.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Sync {

    /// <summary>
    /// 数据同步模块服务注册入口
    /// </summary>
    public static class SyncModule {

        /// <summary>
        /// 注册数据同步相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<ISyncRepository, SyncRepository>();
            services.AddScoped<ISyncService, SyncService>();
        }
    }
}