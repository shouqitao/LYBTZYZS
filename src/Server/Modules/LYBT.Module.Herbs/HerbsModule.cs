using FluentValidation;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Herbs.Application.Commands;
using LYBT.Module.Herbs.Application.Validators;
using LYBT.Module.Herbs.Infrastructure;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Models.Validators.Herbs;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            services.AddDbContext<HerbsDbContext>((sp, options) =>
            {
                var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
                options.UseSqlServer(dbOptions.ConnectionString);
            });

            // 注册仓储
            services.AddScoped<IHerbRepository, HerbRepository>();
            services.AddScoped<IHerbReferenceRepository, HerbReferenceRepository>();

            // 注册跨模块服务（替代 CrossModuleService 中的药材查询逻辑）
            services.AddScoped<IHerbCrossModuleService, HerbCrossModuleService>();

            // 注册导入导出服务
            services.AddScoped<IHerbImportExportService, HerbImportExportService>();

            // Epic #1731: 注册Herbs模块Validators
            services.AddValidatorsFromAssemblyContaining<HerbInputDtoValidator>();

            // 注册 MediatR（Application层）
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(CreateHerbCommand).Assembly));

            // 注册 Application 层验证器
            services.AddValidatorsFromAssemblyContaining<CreateHerbValidator>();

            return services;
        }
    }
}


