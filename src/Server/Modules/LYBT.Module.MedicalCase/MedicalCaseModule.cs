using FluentValidation;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Repositories;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Validators.MedicalCase;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.MedicalCase
{

    /// <summary>
    /// 医疗案例模块注册 - 标准三层架构
    /// 负责注册医疗案例相关的所有服务、仓储和验证器
    /// 采用标准三层架构：Controller → Service → Repository
    /// </summary>
    public static class MedicalCaseModule
    {

        /// <summary>
        /// 注册医疗案例模块服务 - 标准三层架构
        /// </summary>
        public static IServiceCollection AddMedicalCaseModule(this IServiceCollection services)
        {
            // 仓储层 - 统一实现
            services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();

            // 服务层 - Epic #1612: 新接口实现（14方法，Write/Read/Helper分离）
            services.AddScoped<IMedicalCaseService, MedicalCaseService>();

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
