using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Module.Herbs.Repositories;
using FluentValidation;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs
{
    /// <summary>
    /// 中药模块服务注册（简化版本）
    /// </summary>
    public static class HerbsModule
    {
        /// <summary>
        /// 注册中药模块服务
        /// </summary>
        public static IServiceCollection AddHerbsModule(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册仓储
            services.AddScoped<IHerbRepository, HerbRepository>();
            services.AddScoped<IHerbCategoryRepository, HerbCategoryRepository>();
            
            // 注册服务
            services.AddScoped<IHerbService, HerbService>();
            services.AddScoped<IHerbQueryService, HerbQueryService>();
            services.AddScoped<IHerbCategoryService, HerbCategoryService>();
            
            // 注册验证器
            services.AddScoped<IValidator<HerbCreateDto>, HerbCreateDtoValidator>();
            services.AddScoped<IValidator<HerbUpdateDto>, HerbUpdateDtoValidator>();
            
            // 注册AutoMapper配置
            services.AddAutoMapper(typeof(HerbMappingProfile));
            
            // 注册模块特定的配置
            services.Configure<HerbModuleOptions>(configuration.GetSection("Modules:Herbs"));
            
            return services;
        }
        
        /// <summary>
        /// 配置中药模块中间件（如有需要）
        /// </summary>
        public static IApplicationBuilder UseHerbsModule(this IApplicationBuilder app)
        {
            // 当前无特殊中间件需求
            return app;
        }
        
        /// <summary>
        /// 验证模块健康状态
        /// </summary>
        public static IHealthChecksBuilder AddHerbsModuleHealthCheck(this IHealthChecksBuilder builder)
        {
            return builder.AddCheck<HerbsModuleHealthCheck>("herbs_module");
        }
    }
}