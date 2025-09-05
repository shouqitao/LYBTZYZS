using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Prescriptions.Mapping;
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
            // 仓储层
            services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

            // UltraThink双层架构服务 - 查询和业务逻辑分离
            services.AddScoped<IPrescriptionQueryService, PrescriptionQueryService>();
            services.AddScoped<IPrescriptionBusinessService, PrescriptionBusinessService>();

            // 主服务 - UltraThink纯委托模式，委托给专业服务层
            services.AddScoped<IPrescriptionService, PrescriptionService>();
            services.AddScoped<IIntelligentPrescriptionService, IntelligentPrescriptionService>();

            // AutoMapper配置
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<PrescriptionMappingProfile>();
            });

            return services;
        }
    }
}