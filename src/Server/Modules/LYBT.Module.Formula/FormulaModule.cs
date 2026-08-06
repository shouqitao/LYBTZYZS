using FluentValidation;
using LYBT.Infrastructure.Validation;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Services;
using LYBT.Shared.Models.Validators.Formula;
using LYBT.Shared.Configuration;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            services.AddDbContext<Infrastructure.FormulaDbContext>((sp, options) =>
            {
                var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
                var connectionString = ConnectionStringResolver.GetEffectiveConnectionString(dbOptions, configuration);
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("未配置数据库连接字符串");
                options.UseSqlServer(connectionString);
            });
            services.AddScoped<IFormulaRepository, Infrastructure.FormulaRepository>();

            // 注册验方服务（替代 trivial MediatR Handler）
            services.AddScoped<IFormulaService, FormulaService>();

            // Application层 - MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Application.Commands.CreateFormulaCommand).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Application层 - FluentValidation验证器
            services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateFormulaValidator>();

            return services;
        }
    }
}


