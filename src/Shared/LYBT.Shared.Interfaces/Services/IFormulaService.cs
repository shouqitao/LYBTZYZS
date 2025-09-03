using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 验方服务接口 - UltraThink双层架构精简标准（小诊所适用）
    /// </summary>
    public interface IFormulaService
    {
        #region 查询操作 - QueryService专业负责
        
        /// <summary>
        /// 分页查询验方
        /// </summary>
        Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query);
        
        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 搜索验方
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);
        
        /// <summary>
        /// 获取验方模板
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync();
        
        /// <summary>
        /// 根据类型获取验方
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType);
        
        /// <summary>
        /// 获取验方分类
        /// </summary>
        Task<ServiceResult<List<string>>> GetCategoriesAsync();

        #endregion

        #region 业务操作 - BusinessService专业负责

        /// <summary>
        /// 创建新验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto);
        
        /// <summary>
        /// 更新验方信息
        /// </summary>
        Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto);
        
        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
        
        /// <summary>
        /// 启用验方
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用验方
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);
        
        /// <summary>
        /// 从处方创建验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name);

        #endregion

        #region 批量操作 - 必需功能（用户明确需求）

        /// <summary>
        /// 批量导入验方
        /// </summary>
        Task<ServiceResult<object>> ImportFormulasAsync(List<FormulaCreateDto> formulas);
        
        /// <summary>
        /// 导出验方数据
        /// </summary>
        Task<ServiceResult<byte[]>> ExportFormulasAsync(PagedQueryBaseDto query);

        #endregion
    }
}