using FluentValidation;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Validation;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Module.Catalog.Application.Mappers;
using LYBT.Module.Catalog.Application.Validators;
using LYBT.Module.Catalog.Infrastructure;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Module.Catalog.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Validators.Formula;
using LYBT.Shared.Models.Validators.Herbs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Catalog
{
    /// <summary>
    /// 药材方剂目录模块服务注册（A-31-C3b 合并 HerbsModule + FormulaModule）。
    /// 写操作走 MediatR Handler（ValidationBehavior 管道），读操作走 ICatalogQueryService 直查（蓝图 §2.2）。
    /// </summary>
    public static class CatalogModule
    {
        /// <summary>
        /// 注册目录模块服务。
        /// </summary>
        public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册 DbContext（模块级）
            services.AddModuleDbContext<CatalogDbContext>(configuration);

            // 注册仓储
            services.AddScoped<IHerbRepository, HerbRepository>();
            services.AddScoped<IFormulaRepository, FormulaRepository>();
            services.AddScoped<IHerbReferenceRepository, HerbReferenceRepository>();

            // 注册跨模块服务（替代 CrossModuleService 中的药材查询逻辑）
            services.AddScoped<ICatalogService, CatalogCrossModuleService>();

            // 注册只读查询服务（合并 HerbService/FormulaService 孪生，差异点由工厂注入）
            services.AddScoped<ICatalogQueryService<HerbListDto, HerbDetailDto>>(sp =>
                new CatalogQueryService<Herb, HerbListDto, HerbDetailDto>(
                    sp.GetRequiredService<IHerbRepository>(),
                    CatalogDtoMapper.ToHerbListDto,
                    CatalogDtoMapper.ToHerbDetailDto,
                    ErrorCode.HerbNotFound));
            services.AddScoped<ICatalogQueryService<FormulaListDto, FormulaDetailDto>>(sp =>
                new CatalogQueryService<Formula, FormulaListDto, FormulaDetailDto>(
                    sp.GetRequiredService<IFormulaRepository>(),
                    CatalogDtoMapper.ToFormulaListDto,
                    CatalogDtoMapper.ToFormulaDetailDto,
                    ErrorCode.FormulaNotFound));

            // 注册共享验证器（Shared.Models 层）
            services.AddValidatorsFromAssemblyContaining<HerbInputDtoValidator>();
            services.AddValidatorsFromAssemblyContaining<FormulaInputDtoValidator>();

            // 注册 MediatR（Application层）
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(HerbCommandHandler).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // 注册 Application 层验证器
            services.AddValidatorsFromAssemblyContaining<CreateHerbValidator>();

            return services;
        }
    }
}
