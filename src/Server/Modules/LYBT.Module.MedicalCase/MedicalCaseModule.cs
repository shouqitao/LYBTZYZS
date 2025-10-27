using FluentValidation;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Repositories;
using LYBT.Module.MedicalCase.Services;
using LYBT.Module.MedicalCase.Validators;
using LYBT.Server.Interfaces.Services;
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
            services.AddScoped<Services.IMedicalCaseService, MedicalCaseService>();

            // 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<MedicalCaseCreateDtoValidator>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

            return services;
        }
    }
}
