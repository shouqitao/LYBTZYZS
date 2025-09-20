using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formula.Interfaces
{

    /// <summary>
    /// 验方查询服务接口 - UltraThink双层架构Query层抽象
    /// 职责：分页查询、筛选、推荐、分类等查询相关功能
    /// </summary>
    public interface IFormulaQueryService
    {

        /// <summary>
        /// 分页查询验方
        /// </summary>
        Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query);

        /// <summary>
        /// 搜索验方（带分页）
        /// </summary>
        Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 获取验方列表（可选关键词筛选）
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null);

        /// <summary>
        /// 获取所有验方
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync();

        /// <summary>
        /// 获取验方模板
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync();

        /// <summary>
        /// 按类型获取验方
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType);

        /// <summary>
        /// 按关键词和分类搜索验方
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword, string? category);

        /// <summary>
        /// 关键词搜索验方
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取验方分类列表
        /// </summary>
        Task<ServiceResult<List<string>>> GetCategoriesAsync();

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);
    }
}