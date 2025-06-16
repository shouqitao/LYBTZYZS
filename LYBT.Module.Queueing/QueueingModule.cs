using Microsoft.Extensions.DependencyInjection;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Module.Queueing.Repositories;
using LYBT.Module.Queueing.Services;

namespace LYBT.Module.Queueing {
    /// <summary>
    /// 排队管理模块服务注册入口
    /// </summary>
    public static class QueueingModule {
        /// <summary>
        /// 注册排队相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IQueueingRepository, QueueingRepository>();
            services.AddScoped<IQueueingService, QueueingService>();
        }
    }
}
