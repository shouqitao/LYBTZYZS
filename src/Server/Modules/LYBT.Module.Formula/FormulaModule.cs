using FluentValidation;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Formula.Repositories;
using LYBT.Module.Formula.Services;
using LYBT.Module.Formula.Validators;
using LYBT.Server.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Formula
{

    /// <summary>
    /// 验方模块注册 - 标准三层架构
    /// 负责注册验方相关的所有服务、仓储和验证器.
    /// 采用标准三层架构：Controller → Service → Repository.
    /// </summary>
    public static class FormulaModule
    {

        /// <summary>
        /// 注册验方模块服务 - 简化架构标准.
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddFormulaModule(this IServiceCollection services)
        {
            // 仓储层
            services.AddScoped<IFormulaRepository, FormulaRepository>();

            // 统一服务 - 合并查询和业务逻辑
            services.AddScoped<IFormulaService, FormulaService>();

            // 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<FormulaCreateDtoValidator>();

            // AutoMapper配置已在UnifiedServiceRegistration中集中注册

            return services;
        }
    }
}
