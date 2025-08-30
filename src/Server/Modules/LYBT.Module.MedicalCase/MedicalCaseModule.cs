using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Repositories;
using LYBT.Module.MedicalCase.Services;
using LYBT.Module.MedicalCase.Mapping;
using LYBT.Module.MedicalCase.Helpers;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.MedicalCase
{
    /// <summary>
    /// 医疗案例模块注册 - 模块标准化重构
    /// 负责注册医疗案例相关的所有服务、仓储和映射配置
    /// </summary>
    public static class MedicalCaseModule
    {
        /// <summary>
        /// 注册医疗案例模块服务
        /// </summary>
        public static IServiceCollection AddMedicalCaseModule(this IServiceCollection services)
        {
            // 注册仓储服务
            services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();

            // UltraThink Helper模式：注册业务助手类
            services.AddScoped<MedicalCaseQueryHelper>();
            services.AddScoped<MedicalCaseValidationHelper>();
            services.AddScoped<MedicalCaseBusinessHelper>();

            // 注册业务服务
            services.AddScoped<IMedicalCaseService, MedicalCaseService>();

            // 注册AutoMapper配置
            services.AddAutoMapper(cfg => 
            {
                cfg.AddProfile<MedicalCaseMappingProfile>();
            });

            return services;
        }
    }
}