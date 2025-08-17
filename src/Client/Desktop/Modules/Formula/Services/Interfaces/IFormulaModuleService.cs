using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Formula.Services.Interfaces
{
    /// <summary>
    /// Formula模块核心业务服务接口
    /// UltraThink模块化架构：模块内部服务，不依赖外部SharedServices
    /// </summary>
    public interface IFormulaModuleService
    {
        #region 基础CRUD操作
        
        /// <summary>
        /// 分页查询验方模板
        /// </summary>
        Task<ServiceResult<PagedResult<FormulaInfo>>> GetPagedAsync(PagedQueryBaseDto query);
        
        /// <summary>
        /// 根据ID获取验方模板
        /// </summary>
        Task<ServiceResult<FormulaInfo>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 创建验方模板
        /// </summary>
        Task<ServiceResult<FormulaInfo>> CreateAsync(FormulaCreateInfo createInfo);
        
        /// <summary>
        /// 更新验方模板
        /// </summary>
        Task<ServiceResult<FormulaInfo>> UpdateAsync(FormulaUpdateInfo updateInfo);
        
        /// <summary>
        /// 删除验方模板
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
        
        #endregion
        
        #region 业务特定操作
        
        /// <summary>
        /// 搜索验方模板
        /// </summary>
        Task<ServiceResult<PagedResult<FormulaInfo>>> SearchFormulasAsync(PagedQueryBaseDto request);
        
        /// <summary>
        /// 复制验方模板
        /// </summary>
        Task<ServiceResult<FormulaInfo>> CopyAsync(Guid id, string newName);
        
        /// <summary>
        /// 根据分类获取验方模板
        /// </summary>
        Task<ServiceResult<IEnumerable<FormulaInfo>>> GetByCategoryAsync(string category);
        
        /// <summary>
        /// 获取所有分类
        /// </summary>
        Task<ServiceResult<IEnumerable<string>>> GetCategoriesAsync();
        
        /// <summary>
        /// 验证验方模板数据
        /// </summary>
        Task<ServiceResult> ValidateAsync(FormulaInfo formulaInfo);
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 启用验方模板
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用验方模板
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);
        
        #endregion
        
        #region 导入导出功能
        
        /// <summary>
        /// 导入验方模板
        /// </summary>
        Task<ServiceResult<IEnumerable<FormulaInfo>>> ImportAsync(string filePath);
        
        /// <summary>
        /// 导出验方模板
        /// </summary>
        Task<ServiceResult> ExportAsync(IEnumerable<Guid> formulaIds, string filePath);
        
        /// <summary>
        /// 生成导入模板
        /// </summary>
        Task<ServiceResult> GenerateImportTemplateAsync(string filePath);
        
        #endregion
    }
}