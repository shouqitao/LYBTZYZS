using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Mapping;
using LYBT.Module.Consultation.Repositories;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Consultation
{

    /// <summary>
    /// 看诊模块注册 - UltraThink标准化重构
    /// 负责注册看诊相关的所有服务、仓储和映射配置
    /// 采用UltraThink双层架构：QueryService + BusinessService 专业分离
    /// </summary>
    public static class ConsultationModule
    {

        /// <summary>
        /// 注册看诊模块服务 - UltraThink双层架构标准
        /// </summary>
        public static IServiceCollection AddConsultationModule(this IServiceCollection services)
        {
            // 仓储层
            services.AddScoped<IConsultationRepository, ConsultationRepository>();

            // UltraThink双层架构服务 - 查询和业务逻辑分离
            services.AddScoped<IConsultationQueryService, ConsultationQueryService>();
            services.AddScoped<IConsultationBusinessService, ConsultationBusinessService>();

            // 主服务 - UltraThink纯委托模式，委托给专业服务层
            services.AddScoped<IConsultationService, ConsultationService>();

            // AutoMapper配置
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<ConsultationMappingProfile>();
            });

            return services;
        }
    }
}
