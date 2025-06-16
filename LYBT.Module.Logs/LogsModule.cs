using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Logs.Repositories;
using LYBT.Module.Logs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Logs {
    /// <summary>
    /// 日志模块服务注册入口，完成日志仓储与服务注册
    /// </summary>
    public static class LogsModule {
        /// <summary>
        /// 注册日志仓储与服务到依赖注入容器
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<ILogService, LogService>();
        }
    }
}
