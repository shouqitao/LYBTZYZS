using FluentValidation;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Validation;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Module.MedicalCases.Infrastructure;
using LYBT.Module.MedicalCases.Services;
using LYBT.Shared.Models.Validators.MedicalCase;
using Microsoft.Extensions.Configuration;
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
        public static IServiceCollection AddMedicalCaseModule(this IServiceCollection services, IConfiguration configuration)
        {
            // ADR-0017: 注册医案模块自己的 DbContext（同库，连接串与 AppDbContext 一致）
            services.AddModuleDbContext<Infrastructure.MedicalCaseDbContext>(configuration);

            // 仓储层 - 统一实现
            services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();
            services.AddScoped<IMedicalCaseReferenceRepository, MedicalCaseReferenceRepository>();

            // 服务层 - Phase 3: CQRS拆分（Command/Query/State分离）
            services.AddScoped<IMedicalCaseCommandService, MedicalCaseCommandService>();
            services.AddScoped<IMedicalCaseQueryService, MedicalCaseQueryService>();
            services.AddScoped<IMedicalCaseStateService, MedicalCaseStateService>();
            // P1-10: 运营时间服务（诊所本地时区日界）
            services.AddScoped<IMedicalCaseTimeService, MedicalCaseTimeService>();
            // T1.2: 状态守卫统一入口
            services.AddScoped<Guards.MedicalCaseStateGuard>();

            // U3-1: 处方内部操作与生命周期服务（从CommandService拆分）
            services.AddScoped<MedicalCasePrescriptionService>();
            services.AddScoped<PrescriptionItemService>();

            // Architecture Fix: 注册跨模块服务接口，供Patients模块使用
            services.AddScoped<IMedicalCaseCrossModuleService, MedicalCaseCrossModuleService>();

            // Epic #1961: 注册验证器 - 使用统一的 MedicalCaseInputDtoValidator
            services.AddValidatorsFromAssemblyContaining<MedicalCaseInputDtoValidator>();

            // Mapperly映射器 - 无状态单例
            services.AddSingleton<MedicalCaseMapper>();

            // 注册 MediatR（Application 层 Command/Query Handler，Handler 内部委托现有 Service）
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(CreateMedicalCaseCommand).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            return services;
        }
    }
}


