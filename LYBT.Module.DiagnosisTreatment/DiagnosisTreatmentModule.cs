using Microsoft.Extensions.DependencyInjection;
using LYBT.Module.DiagnosisTreatment.Interfaces;
using LYBT.Module.DiagnosisTreatment.Repositories;
using LYBT.Module.DiagnosisTreatment.Services;

namespace LYBT.Module.DiagnosisTreatment {
    /// <summary>
    /// 诊疗模块服务注册入口
    /// </summary>
    public static class DiagnosisTreatmentModule {
        /// <summary>
        /// 注册诊疗相关依赖服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IDiagnosisTreatmentRepository, DiagnosisTreatmentRepository>();
            services.AddScoped<IDiagnosisTreatmentService, DiagnosisTreatmentService>();
        }
    }
}
