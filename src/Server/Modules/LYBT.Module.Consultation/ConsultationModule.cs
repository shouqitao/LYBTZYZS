using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Mapping;
using LYBT.Module.Consultation.Options;
using LYBT.Module.Consultation.Repositories;
using LYBT.Module.Consultation.Services;
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

            // 注册服务
            services.AddScoped<IConsultationService, ConsultationService>();
            services.AddScoped<IConsultationQueryService, ConsultationQueryService>();
            // services.AddScoped<IDiagnosisService, DiagnosisService>();  // 不存在的类型

            // 注册验证器 - 暂时注释，待创建后启用
            // services.AddScoped<IValidator<ConsultationCreateDto>, ConsultationCreateDtoValidator>();
            // services.AddScoped<IValidator<DiagnosisDto>, DiagnosisDtoValidator>();  // 不存在的类型

            // 注册AutoMapper配置
            services.AddAutoMapper(typeof(ConsultationMappingProfile));

            // 注册模块特定的配置(带启动验证)
            services.AddOptions<ConsultationModuleOptions>()
                .Bind(configuration.GetSection("Modules:Consultation"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

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
            // return builder.AddCheck<ConsultationModuleHealthCheck>("consultation_module");  // 待创建健康检查类
            return builder;
        }
    }
}
