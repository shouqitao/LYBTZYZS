using FluentValidation;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Module.MedicalCases.Repositories;
using LYBT.Module.MedicalCases.Services;
using LYBT.Shared.Validators.MedicalCase;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.MedicalCases
{

    /// <summary>
    /// 医疗案例模块注册 - CQRS架构
    /// 负责注册医疗案例相关的所有服务、仓储和验证器
    /// Phase 3: 拆分为Command/Query/State三个职责单一的Service
    /// </summary>
    public static class MedicalCaseModule
    {

        /// <summary>
        /// 注册医疗案例模块服务 - CQRS架构
        /// </summary>
        public static IServiceCollection AddMedicalCaseModule(this IServiceCollection services)
        {
            // 仓储层 - 统一实现
            services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();
            services.AddScoped<IMedicalCaseReferenceRepository, MedicalCaseReferenceRepository>();

            // 服务层 - Phase 3: CQRS拆分（Command/Query/State分离）
            services.AddScoped<IMedicalCaseCommandService, MedicalCaseCommandService>();
            services.AddScoped<IMedicalCaseQueryService, MedicalCaseQueryService>();
            services.AddScoped<IMedicalCaseStateService, MedicalCaseStateService>();

            // Architecture Fix: 注册跨模块服务接口，供Patients模块使用
            services.AddScoped<IMedicalCaseCrossModuleService, MedicalCaseReferenceService>();

            // Epic #1961: 注册验证器 - 使用统一的 MedicalCaseInputDtoValidator
            services.AddValidatorsFromAssemblyContaining<MedicalCaseInputDtoValidator>();

            // 注册 MediatR（Application层）
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(CreateMedicalCaseCommand).Assembly));

            // Mapperly映射器 - 无状态单例
            services.AddSingleton<MedicalCaseMapper>();

            return services;
        }
    }
}


