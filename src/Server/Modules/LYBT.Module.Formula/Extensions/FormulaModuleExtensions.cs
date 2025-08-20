using LYBT.Module.Formula.Helpers;
using LYBT.Module.Formula.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Formula.Extensions
{
    /// <summary>
    /// 验方模块服务注册扩展
    /// </summary>
    public static class FormulaModuleExtensions
    {
        /// <summary>
        /// 注册验方模块的所有服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddFormulaModule(this IServiceCollection services)
        {
            // 注册Helper类（按依赖顺序）
            services.AddScoped<FormulaValidationHelper>();
            services.AddScoped<FormulaCalculationHelper>();
            services.AddScoped<FormulaQueryHelper>();

            // 注册主服务（依赖Helper类）
            services.AddScoped<IFormulaService, FormulaService>();

            // TODO: 如果需要Repository层，在此处注册
            // services.AddScoped<IFormulaRepository, FormulaRepository>();

            return services;
        }
    }
}