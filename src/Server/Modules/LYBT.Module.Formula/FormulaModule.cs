using FluentValidation;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Services;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Repositories;
using LYBT.Module.Formulas.Services;
using LYBT.Shared.Validators.Formula;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Formulas
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
            // OpenSpec: refactor-server-srp-patterns - 导入导出服务（从FormulaService拆分）
            services.AddScoped<IFormulaImportExportService, FormulaImportExportService>();
            // 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<FormulaInputDtoValidator>();
            // OpenSpec: add-global-audit-system - 审计服务
            services.AddScoped<IAuditService<Entities.Formulas.Formula>, EntityAuditService<Entities.Formulas.Formula>>();
            // AutoMapper配置已在UnifiedServiceRegistration中集中注册
            return services;
        }
    }
}
