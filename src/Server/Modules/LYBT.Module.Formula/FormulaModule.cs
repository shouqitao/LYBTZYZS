using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Formula.Mapping;
using LYBT.Module.Formula.Repositories;
using LYBT.Module.Formula.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Formula
{

    /// <summary>
    /// 验方模块注册 - UltraThink标准化重构
    /// 负责注册验方相关的所有服务、仓储和映射配置.
    /// 采用UltraThink双层架构：QueryService + BusinessService 专业分离.
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

            // AutoMapper配置
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<FormulaMappingProfile>();
            });

            return services;
        }
    }
}
