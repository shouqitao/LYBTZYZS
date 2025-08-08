using LYBT.Module.Formula.Interfaces;
// using LYBT.Module.Formula.Repositories;
using LYBT.Module.Formula.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Formula
{

    /// <summary>
    /// 经验方模板模块服务注册入口
    /// </summary>
    public static class FormulaModule
    {

        /// <summary>
        /// 注册模板仓储与服务
        /// </summary>
        public static void Register(IServiceCollection services)
        {
            // 注册Formula服务
            services.AddScoped<IFormulaService, FormulaService>();
        }
    }
}