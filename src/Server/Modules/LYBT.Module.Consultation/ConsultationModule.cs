using FluentValidation;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Mapping;
using LYBT.Module.Consultation.Repositories;
using LYBT.Module.Consultation.Services;
using LYBT.Module.Consultation.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Consultation
{
    /// <summary>
    /// 问诊模块服务注册（简化版本）
    /// </summary>
    public static class ConsultationModule
    {
        /// <summary>
        /// 注册问诊模块服务
        /// </summary>
        public static IServiceCollection AddConsultationModule(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册仓储
            services.AddScoped<IConsultationRepository, ConsultationRepository>();
            // services.AddScoped<IConsultationRecordRepository, ConsultationRecordRepository>();  // 不存在的类型

            // 注册服务实现类（统一使用Shared接口）
            services.AddScoped<LYBT.Shared.Interfaces.Services.IConsultationService, ConsultationService>();

            // services.AddScoped<IDiagnosisService, DiagnosisService>();  // 不存在的类型

            // 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<ConsultationCreateDtoValidator>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

            // 模块无特殊配置需求（通用配置在appsettings.json）

            return services;
        }

        /// <summary>
        /// 配置问诊模块中间件（如有需要）
        /// </summary>
        public static IApplicationBuilder UseConsultationModule(this IApplicationBuilder app)
        {
            // 当前无特殊中间件需求
            return app;
        }

        /// <summary>
        /// 验证模块健康状态
        /// </summary>
        public static IHealthChecksBuilder AddConsultationModuleHealthCheck(this IHealthChecksBuilder builder)
        {
            // TODO: 待创建健康检查类
            return builder;
        }
    }
}
