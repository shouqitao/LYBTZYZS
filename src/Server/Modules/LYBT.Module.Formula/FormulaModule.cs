using FluentValidation;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Repositories;
using LYBT.Module.Formulas.Services;
using LYBT.Shared.Validators.Formula;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Formulas
{
    /// <summary>
    /// 验方模块注册 - 标准三层架构 + DDD CQRS
    /// 负责注册验方相关的所有服务、仓储和验证器.
    /// </summary>
    public static class FormulaModule
    {
        /// <summary>
        /// 注册验方模块服务.
        /// </summary>
        public static IServiceCollection AddFormulaModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Legacy仓储（给旧FormulaService使用）
            services.AddScoped<IFormulaRepositoryLegacy, FormulaRepository>();
            // 统一服务 - 合并查询和业务逻辑
            services.AddScoped<IFormulaService, FormulaService>();
            services.AddScoped<IFormulaImportExportService, FormulaImportExportService>();
            // 注册共享验证器
            services.AddValidatorsFromAssemblyContaining<FormulaInputDtoValidator>();
            // DDD层注册
            services.AddFormulaModuleDDD(configuration);
            return services;
        }

        /// <summary>
        /// 注册DDD CQRS层服务（Domain + Application + Infrastructure）.
        /// </summary>
        private static IServiceCollection AddFormulaModuleDDD(this IServiceCollection services, IConfiguration configuration)
        {
            // Infrastructure层
            services.AddDbContext<Infrastructure.FormulaDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<Interfaces.IFormulaRepository, Infrastructure.FormulaRepository>();

            // Application层 - MediatR
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(Application.Commands.CreateFormulaCommand).Assembly));

            // Application层 - FluentValidation验证器
            services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateFormulaValidator>();

            return services;
        }
    }
}


