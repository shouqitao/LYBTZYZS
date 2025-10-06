using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Options;
using LYBT.Module.Herbs.Repositories;
using LYBT.Module.Herbs.Services;
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

            // 注册服务实现类
            services.AddScoped<HerbService>();

            // 注册 Module 内部接口
            services.AddScoped<IHerbService>(sp => sp.GetRequiredService<HerbService>());

            // 注册跨平台契约接口（供 WebAPI Controller 和 Desktop Client 使用）
            services.AddScoped<LYBT.Shared.Interfaces.Services.IHerbService>(sp =>
                sp.GetRequiredService<HerbService>());

            // services.AddScoped<IHerbCategoryService, HerbCategoryService>();

            // 注册验证器 - 暂时注释，待修复验证器后启用
            // services.AddScoped<IValidator<HerbCreateDto>, HerbCreateDtoValidator>();
            // services.AddScoped<IValidator<HerbUpdateDto>, HerbUpdateDtoValidator>();

            // 注册AutoMapper配置 - 暂时注释，待创建配置文件后启用
            // services.AddAutoMapper(typeof(HerbMappingProfile));

            // 注册模块特定的配置(带启动验证)
            services.AddOptions<HerbModuleOptions>()
                .Bind(configuration.GetSection("Modules:Herbs"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

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
