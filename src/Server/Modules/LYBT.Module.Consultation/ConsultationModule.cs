using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Repositories;
using LYBT.Module.Consultation.Services;
using LYBT.Module.Consultation.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Consultation
{
    /// <summary>
    /// 看诊模块注册 - 模块标准化重构
    /// 负责注册看诊相关的所有服务、仓储和映射配置
    /// </summary>
    public static class ConsultationModule
    {
        /// <summary>
        /// 注册看诊模块服务
        /// </summary>
        public static IServiceCollection AddConsultationModule(this IServiceCollection services)
        {
            // 注册仓储服务
            services.AddScoped<IConsultationRepository, ConsultationRepository>();

            // 注册业务服务
            services.AddScoped<IConsultationService, ConsultationService>();

            // 注册AutoMapper配置
            services.AddAutoMapper(cfg => 
            {
                cfg.AddProfile<ConsultationMappingProfile>();
            });

            return services;
        }
    }
}