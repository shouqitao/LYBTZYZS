using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Module.FormulaTemplates.Repositories;
using LYBT.Module.FormulaTemplates.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.FormulaTemplates {

    /// <summary>
    /// 经验方模板模块服务注册入口
    /// </summary>
    public static class FormulaTemplatesModule {

        /// <summary>
        /// 注册模板仓储与服务
        /// </summary>
        public static void Register(IServiceCollection services) {
            services.AddScoped<IFormulaTemplateRepository, FormulaTemplateRepository>();
            services.AddScoped<IFormulaTemplateService, FormulaTemplateService>();
        }
    }
}