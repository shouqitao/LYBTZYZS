using Microsoft.Extensions.DependencyInjection;
using LYBT.Module.Records.Interfaces;
using LYBT.Module.Records.Repositories;
using LYBT.Module.Records.Services;

namespace LYBT.Module.Records {
    /// <summary>
    /// 病历模块服务注册入口
    /// </summary>
    public static class RecordsModule {
        /// <summary>
        /// 注册病历相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IRecordRepository, RecordRepository>();
            services.AddScoped<IRecordService, RecordService>();
        }
    }
}
