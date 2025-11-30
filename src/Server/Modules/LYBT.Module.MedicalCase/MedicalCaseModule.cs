using FluentValidation;
using LYBT.Module.MedicalCases.Interfaces;
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

            // 服务层 - Phase 3: CQRS拆分（Command/Query/State分离）
            services.AddScoped<IMedicalCaseCommandService, MedicalCaseCommandService>();
            services.AddScoped<IMedicalCaseQueryService, MedicalCaseQueryService>();
            services.AddScoped<IMedicalCaseStateService, MedicalCaseStateService>();

            // OpenSpec: refactor-medicalcase-management - 权限服务
            services.AddScoped<IMedicalCasePermissionService, MedicalCasePermissionService>();

            // OpenSpec: refactor-medicalcase-management - 审计服务 (LIFECYCLE-008)
            services.AddScoped<IMedicalCaseAuditService, MedicalCaseAuditService>();

            // Epic #1961: 注册验证器 - 使用统一的 MedicalCaseInputDtoValidator
            services.AddValidatorsFromAssemblyContaining<MedicalCaseInputDtoValidator>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

            return services;
        }
    }
}
