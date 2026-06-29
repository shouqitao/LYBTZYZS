using FluentValidation;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Validators.Patients;
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

            // 注册服务实现类（统一使用Shared接口）
            services.AddScoped<IPatientService, PatientService>();

            // 注册跨模块服务（替代 CrossModuleService 中的患者查询逻辑）
            services.AddScoped<IPatientCrossModuleService, PatientCrossModuleService>();

            // Epic #1731: 注册Patients模块Validators
            services.AddValidatorsFromAssemblyContaining<PatientInputDtoValidator>();

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
