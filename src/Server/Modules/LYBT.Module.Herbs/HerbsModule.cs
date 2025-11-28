using FluentValidation;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Repositories;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Validators.Herbs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            // services.AddScoped<IHerbCategoryRepository, HerbCategoryRepository>();

            // 注册服务实现类（统一使用Shared接口）
            services.AddScoped<IHerbService, HerbService>();

            // services.AddScoped<IHerbCategoryService, HerbCategoryService>();

            // Epic #1731: 注册Herbs模块Validators
            services.AddValidatorsFromAssemblyContaining<HerbInputDtoValidator>();

            // OpenSpec: add-global-audit-system - 审计服务
            services.AddScoped<IAuditService<Herb>, EntityAuditService<Herb>>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

            // 模块无特殊配置需求（通用配置在appsettings.json）

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

    }
}
