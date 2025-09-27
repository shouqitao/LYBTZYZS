using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Services;
using LYBT.Module.Consultation.Repositories;
using FluentValidation;
using LYBT.Shared.Models.Contracts.Consultation;

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
            services.AddScoped<IConsultationRecordRepository, ConsultationRecordRepository>();
            
            // 注册服务
            services.AddScoped<IConsultationService, ConsultationService>();
            services.AddScoped<IConsultationQueryService, ConsultationQueryService>();
            services.AddScoped<IDiagnosisService, DiagnosisService>();
            
            // 注册验证器
            services.AddScoped<IValidator<ConsultationCreateDto>, ConsultationCreateDtoValidator>();
            services.AddScoped<IValidator<DiagnosisDto>, DiagnosisDtoValidator>();
            
            // 注册AutoMapper配置
            services.AddAutoMapper(typeof(ConsultationMappingProfile));
            
            // 注册模块特定的配置
            services.Configure<ConsultationModuleOptions>(configuration.GetSection("Modules:Consultation"));
            
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
            return builder.AddCheck<ConsultationModuleHealthCheck>("consultation_module");
        }
    }
}