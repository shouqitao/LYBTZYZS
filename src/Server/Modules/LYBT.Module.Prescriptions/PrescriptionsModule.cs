using FluentValidation;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Prescriptions.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Prescriptions
{

    /// <summary>
    /// 处方模块注册 - 标准三层架构
    /// 负责注册处方相关的所有服务、仓储和验证器
    /// 采用标准三层架构：Controller → Service → Repository
    /// </summary>
    public static class PrescriptionsModule
    {

        /// <summary>
        /// 注册处方模块服务 - 标准三层架构
        /// </summary>
        public static IServiceCollection AddPrescriptionsModule(this IServiceCollection services)
        {
            // 仓储层 - 统一实现
            services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

            // 服务层 - UltraThink架构重构后的统一服务
            services.AddScoped<IPrescriptionService, PrescriptionService>();

            // Issue #1551: 处方编号生成服务
            services.AddScoped<IPrescriptionNumberService, PrescriptionNumberService>();

            // 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<PrescriptionCreateDtoValidator>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

            return services;
        }
    }
}
