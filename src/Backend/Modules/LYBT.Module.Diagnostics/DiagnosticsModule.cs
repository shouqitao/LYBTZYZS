using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Diagnostics {

    /// <summary>
    /// 诊断治疗模块服务注册
    /// </summary>
    public static class DiagnosticsModule {

        /// <summary>
        /// 添加诊断治疗模块服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDiagnosticsModule(this IServiceCollection services) {
            
            // TODO: 注册仓储和服务
            // services.AddScoped<IRegistrationRepository, RegistrationRepository>();
            // services.AddScoped<IRegistrationService, RegistrationService>();
            // services.AddScoped<IQueueingRepository, QueueingRepository>();
            // services.AddScoped<IQueueingService, QueueingService>();
            // services.AddScoped<IDiagnosisTreatmentRepository, DiagnosisTreatmentRepository>();
            // services.AddScoped<IDiagnosisTreatmentService, DiagnosisTreatmentService>();
            // services.AddScoped<IRecordRepository, RecordRepository>();
            // services.AddScoped<IRecordService, RecordService>();

            return services;
        }
    }
}