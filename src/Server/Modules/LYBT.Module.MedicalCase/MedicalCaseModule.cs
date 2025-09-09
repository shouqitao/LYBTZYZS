using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Mapping;
using LYBT.Module.MedicalCase.Repositories;
using LYBT.Module.MedicalCase.Services;
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
            // 仓储层
            services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();

            // UltraThink双层架构服务 - 查询和业务逻辑分离
            services.AddScoped<IMedicalCaseQueryService, MedicalCaseQueryService>();
            services.AddScoped<IMedicalCaseBusinessService, MedicalCaseBusinessService>();

            // 主服务 - UltraThink纯委托模式，委托给专业服务层
            services.AddScoped<IMedicalCaseService, MedicalCaseService>();

            // AutoMapper配置
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MedicalCaseMappingProfile>();
            });

            return services;
        }
    }
}
