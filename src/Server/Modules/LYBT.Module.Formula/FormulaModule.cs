using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Formula.Repositories;
using LYBT.Module.Formula.Services;
using LYBT.Module.Formula.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Formula
{
    /// <summary>
    /// 验方模块注册 - UltraThink标准化重构
    /// 负责注册验方相关的所有服务、仓储和映射配置
    /// 采用UltraThink双层架构：QueryService + BusinessService 专业分离
    /// </summary>
    public static class FormulaModule
    {
        /// <summary>
        /// 注册验方模块服务 - UltraThink双层架构标准
        /// </summary>
        public static IServiceCollection AddFormulaModule(this IServiceCollection services)
        {
            // 仓储层
            services.AddScoped<IFormulaRepository, FormulaRepository>();

            // UltraThink双层架构服务 - 查询和业务逻辑分离
            services.AddScoped<FormulaQueryService>();
            services.AddScoped<FormulaBusinessService>();

            // 主服务 - UltraThink纯委托模式，委托给专业服务层
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