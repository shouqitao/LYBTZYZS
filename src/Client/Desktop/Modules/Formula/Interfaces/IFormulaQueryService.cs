using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Interfaces;

/// <summary>
/// 验方查询服务接口 - UltraThink简化版本对应后端实际API
/// 移除过度开发功能，仅保留后端支持的基本查询功能
/// </summary>
public interface IFormulaQueryService
{

    #region 基础查询功能 - 对应后端FormulasController实际API

    /// <summary>
    /// 获取分页验方列表 (对应 GET /formulas)
    /// </summary>
    Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query);

    /// <summary>
    /// 根据ID获取验方详情 (对应 GET /formulas/{id})
    /// </summary>
    Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 搜索验方 (对应 GET /formulas/search)
    /// </summary>
    Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto searchDto);

    /// <summary>
    /// 根据关键词搜索验方 - 简化版本
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 获取验方模板 (对应 GET /formulas/templates)
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync();

    /// <summary>
    /// 根据类型获取验方 (对应 GET /formulas/by-type/{type})
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string type);

    /// <summary>
    /// 获取验方分类 (对应 GET /formulas/categories)
    /// </summary>
    Task<ServiceResult<List<string>>> GetCategoriesAsync();


    #endregion 基础查询功能 - 对应后端FormulasController实际API

    #region 基础统计 - 简化版本基于现有数据计算

    /// <summary>
    /// 获取基础统计信息 (简化版，基于现有数据计算)
    /// </summary>
    Task<ServiceResult<FormulaStatisticsDto>> GetBasicStatisticsAsync();

    #endregion 基础统计 - 简化版本基于现有数据计算
}
