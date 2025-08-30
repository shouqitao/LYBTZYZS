using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Formula.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Formula
{
    /// <summary>
    /// 经验方模板模块服务注册入口 - UltraThink v2.0修复完成
    /// </summary>
    public static class FormulaModule
    {
        /// <summary>
        /// 注册模板仓储与服务
        /// </summary>
        public static void Register(IServiceCollection services)
        {
            // UltraThink v2.0修复：重新启用Formula服务注册
            services.AddScoped<IFormulaService, FormulaService>();
        }
    }
}