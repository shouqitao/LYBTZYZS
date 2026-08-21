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
using LYBT.Shared.Models.Spi;
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
            services.AddScoped<ICatalogCrossModuleService, CatalogCrossModuleService>();

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

            // 注册泛型验证器（closed generic，AssemblyScanner 跳过泛型定义类）
            // Toggle/Restore/BatchEnable/BatchDisable 孪生已收敛为泛型验证器（S1）
            services.AddScoped<IValidator<ToggleEntityStatusCommand<Herb, HerbDetailDto>>, ToggleEntityStatusValidator<Herb, HerbDetailDto>>(
                _ => new ToggleEntityStatusValidator<Herb, HerbDetailDto>("药材"));
            services.AddScoped<IValidator<ToggleEntityStatusCommand<Formula, FormulaDetailDto>>, ToggleEntityStatusValidator<Formula, FormulaDetailDto>>(
                _ => new ToggleEntityStatusValidator<Formula, FormulaDetailDto>("验方"));
            services.AddScoped<IValidator<RestoreEntityCommand<Herb, HerbDetailDto>>, RestoreEntityValidator<Herb, HerbDetailDto>>(
                _ => new RestoreEntityValidator<Herb, HerbDetailDto>("药材"));
            services.AddScoped<IValidator<RestoreEntityCommand<Formula, FormulaDetailDto>>, RestoreEntityValidator<Formula, FormulaDetailDto>>(
                _ => new RestoreEntityValidator<Formula, FormulaDetailDto>("验方"));
            services.AddScoped<IValidator<BatchEnableHerbsCommand>, BatchEntityIdsValidator<BatchEnableHerbsCommand>>(
                _ => new BatchEntityIdsValidator<BatchEnableHerbsCommand>("药材"));
            services.AddScoped<IValidator<BatchEnableFormulasCommand>, BatchEntityIdsValidator<BatchEnableFormulasCommand>>(
                _ => new BatchEntityIdsValidator<BatchEnableFormulasCommand>("验方"));
            services.AddScoped<IValidator<BatchDisableHerbsCommand>, BatchEntityIdsValidator<BatchDisableHerbsCommand>>(
                _ => new BatchEntityIdsValidator<BatchDisableHerbsCommand>("药材"));
            services.AddScoped<IValidator<BatchDisableFormulasCommand>, BatchEntityIdsValidator<BatchDisableFormulasCommand>>(
                _ => new BatchEntityIdsValidator<BatchDisableFormulasCommand>("验方"));

            // SPI 扩展点：新增库存实体仅新增 ICrossModuleReferenceChecker 实现并注册（不改现有删除链）
            services.AddSingleton<ICrossModuleReferenceChecker, Spi.CatalogReferenceChecker>();

            return services;
        }
    }
}
