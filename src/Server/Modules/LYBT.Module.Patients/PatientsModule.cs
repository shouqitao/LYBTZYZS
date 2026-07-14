using FluentValidation;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Application.Validators;
using LYBT.Module.Patients.Infrastructure;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Validators.Patients;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Patients
{
    /// <summary>
    /// 患者模块服务注册
    /// </summary>
    public static class PatientsModule
    {
        /// <summary>
        /// 注册患者模块服务
        /// </summary>
        public static IServiceCollection AddPatientsModule(this IServiceCollection services)
        {
            // 注册仓储（使用AppDbContext）
            services.AddScoped<IPatientRepository, LYBT.Module.Patients.Infrastructure.PatientRepository>();

            // 注册跨模块服务（替代 CrossModuleService 中的患者查询逻辑）
            services.AddScoped<IPatientCrossModuleService, PatientCrossModuleService>();

            // Epic #1731: 注册Patients模块Validators
            services.AddValidatorsFromAssemblyContaining<PatientInputDtoValidator>();

            // 注册 MediatR（Application层）
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(CreatePatientCommand).Assembly));

            // 注册 Application 层验证器
            services.AddValidatorsFromAssemblyContaining<CreatePatientValidator>();

            return services;
        }
    }
}


