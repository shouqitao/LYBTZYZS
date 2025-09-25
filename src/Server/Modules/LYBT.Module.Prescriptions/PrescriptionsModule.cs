using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Mapping;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Prescriptions
{

    /// <summary>
    /// 处方模块注册 - UltraThink标准化重构
    /// 负责注册处方相关的所有服务、仓储和映射配置
    /// 采用UltraThink双层架构：QueryService + BusinessService 专业分离
    /// </summary>
    public static class PrescriptionsModule
    {

        /// <summary>
        /// 注册处方模块服务 - UltraThink双层架构标准
        /// </summary>
        public static IServiceCollection AddPrescriptionsModule(this IServiceCollection services)
        {
            // 仓储层 - 统一实现
            services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

            // 服务层 - UltraThink架构重构后的统一服务
            services.AddScoped<IPrescriptionService, PrescriptionService>();

            // AutoMapper配置
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<PrescriptionMappingProfile>();
            });

            return services;
        }
    }
}
