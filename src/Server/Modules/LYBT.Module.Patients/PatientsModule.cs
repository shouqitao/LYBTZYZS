using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Options;
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

            // 注册服务实现类
            services.AddScoped<PatientService>();

            // 注册 Module 内部接口
            services.AddScoped<IPatientService>(sp => sp.GetRequiredService<PatientService>());

            // 注册跨平台契约接口（供 WebAPI Controller 和 Desktop Client 使用）
            services.AddScoped<LYBT.Shared.Interfaces.Services.IPatientService>(sp =>
                sp.GetRequiredService<PatientService>());

            // services.AddScoped<IMedicalRecordService, MedicalRecordService>();

            // 注册验证器 - 暂时注释，待创建验证器后启用
            // services.AddScoped<IValidator<PatientCreateDto>, PatientCreateDtoValidator>();
            // services.AddScoped<IValidator<PatientUpdateDto>, PatientUpdateDtoValidator>();

            // 注册AutoMapper配置 - 暂时注释，待创建配置文件后启用
            // services.AddAutoMapper(typeof(PatientMappingProfile));

            // 注册模块特定的配置(带启动验证)
            services.AddOptions<PatientModuleOptions>()
                .Bind(configuration.GetSection("Modules:Patients"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

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
