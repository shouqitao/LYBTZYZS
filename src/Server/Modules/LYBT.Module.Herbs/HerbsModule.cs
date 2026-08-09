using FluentValidation;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Validation;
using LYBT.Module.Herbs.Application.Commands;
using LYBT.Module.Herbs.Application.Validators;
using LYBT.Module.Herbs.Infrastructure;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Models.Validators.Herbs;
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
            // 注册 DbContext（模块级）
            services.AddModuleDbContext<HerbsDbContext>(configuration);

            // 注册仓储
            services.AddScoped<IHerbRepository, HerbRepository>();
            services.AddScoped<IHerbReferenceRepository, HerbReferenceRepository>();

            // 注册跨模块服务（替代 CrossModuleService 中的药材查询逻辑）
            services.AddScoped<IHerbCrossModuleService, HerbCrossModuleService>();

            // 注册药材服务（替代 trivial MediatR Handler）
            services.AddScoped<IHerbService, HerbService>();

            // Epic #1731: 注册Herbs模块Validators
            services.AddValidatorsFromAssemblyContaining<HerbInputDtoValidator>();

            // 注册 MediatR（Application层）
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(CreateHerbCommand).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // 注册 Application 层验证器
            services.AddValidatorsFromAssemblyContaining<CreateHerbValidator>();

            return services;
        }
    }
}


