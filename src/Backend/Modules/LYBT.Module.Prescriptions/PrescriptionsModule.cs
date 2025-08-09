using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Prescriptions.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Prescriptions
{
    /// <summary>
    /// 处方模块注册 - 模块标准化重构
    /// 负责注册处方相关的所有服务、仓储和映射配置
    /// </summary>
    public static class PrescriptionsModule
    {
        /// <summary>
        /// 注册处方模块服务
        /// </summary>
        public static IServiceCollection AddPrescriptionsModule(this IServiceCollection services)
        {
            // 注册仓储服务
            services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

            // 注册业务服务
            services.AddScoped<IPrescriptionService, PrescriptionService>();
            services.AddScoped<IIntelligentPrescriptionService, IntelligentPrescriptionService>();

            // 注册AutoMapper配置  
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<PrescriptionMappingProfile>();
            });

            return services;
        }
    }
}