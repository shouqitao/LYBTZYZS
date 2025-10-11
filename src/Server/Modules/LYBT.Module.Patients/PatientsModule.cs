using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Patients
{
    /// <summary>
    /// 患者模块服务注册（简化版本）
    /// </summary>
    public static class PatientsModule
    {
        /// <summary>
        /// 注册患者模块服务
        /// </summary>
        public static IServiceCollection AddPatientsModule(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册仓储
            services.AddScoped<IPatientRepository, PatientRepository>();
            // services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();

            // 注册服务实现类（统一使用Shared接口）
            services.AddScoped<LYBT.Shared.Interfaces.Services.IPatientService, PatientService>();

            // services.AddScoped<IMedicalRecordService, MedicalRecordService>();

            // 注册验证器 - 暂时注释，待创建验证器后启用
            // services.AddScoped<IValidator<PatientCreateDto>, PatientCreateDtoValidator>();
            // services.AddScoped<IValidator<PatientUpdateDto>, PatientUpdateDtoValidator>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

            // 模块无特殊配置需求（通用配置在appsettings.json）

            return services;
        }

        /// <summary>
        /// 配置患者模块中间件（如有需要）
        /// </summary>
        public static IApplicationBuilder UsePatientsModule(this IApplicationBuilder app)
        {
            // 当前无特殊中间件需求
            return app;
        }

    }
}
