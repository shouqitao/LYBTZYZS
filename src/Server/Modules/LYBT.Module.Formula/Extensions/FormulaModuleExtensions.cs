using LYBT.Module.Formula.Services;
using LYBT.Module.Formula.Services.Core;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Formula.Extensions
{
    /// <summary>
    /// 验方模块服务注册扩展 - UltraThink扩展友好架构
    /// </summary>
    public static class FormulaModuleExtensions
    {
        /// <summary>
        /// 注册验方模块的所有服务 - 组合式架构，便于扩展
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddFormulaModule(this IServiceCollection services)
        {
            // 注册核心服务层 - 稳定的CRUD功能
            services.AddScoped<FormulaServiceCore>();
            
            // 注册专业服务层 - 便于独立扩展
            services.AddScoped<FormulaQueryService>();
            services.AddScoped<FormulaBusinessService>();

            // 注册主服务接口 - 组合协调各专业服务
            services.AddScoped<IFormulaService, FormulaService>();

            return services;
        }

        /// <summary>
        /// 扩展点：注册自定义验方服务（未来扩展用）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddFormulaExtensions(this IServiceCollection services)
        {
            // 预留扩展点：
            // services.AddScoped<FormulaAIService>();        // AI推荐服务
            // services.AddScoped<FormulaAuditService>();     // 审计服务  
            // services.AddScoped<FormulaExportService>();    // 导出服务
            // services.AddScoped<FormulaIntegrationService>(); // 第三方集成服务
            
            return services;
        }
    }
}