using FluentValidation;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Repositories;
using LYBT.Module.MedicalCase.Services;
using LYBT.Module.MedicalCase.Validators;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.MedicalCase
{

    /// <summary>
    /// 医疗案例模块注册 - UltraThink标准化重构
    /// 负责注册医疗案例相关的所有服务、仓储和映射配置
    /// 采用UltraThink双层架构：QueryService + BusinessService 专业分离
    /// </summary>
    public static class MedicalCaseModule
    {

        /// <summary>
        /// 注册医疗案例模块服务 - UltraThink双层架构标准
        /// </summary>
        public static IServiceCollection AddMedicalCaseModule(this IServiceCollection services)
        {
            // 仓储层 - 统一实现
            services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();

            // 服务层 - UltraThink架构重构后的统一服务
            services.AddScoped<IMedicalCaseService, MedicalCaseService>();

            // 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<MedicalCaseCreateDtoValidator>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

            return services;
        }
    }
}
