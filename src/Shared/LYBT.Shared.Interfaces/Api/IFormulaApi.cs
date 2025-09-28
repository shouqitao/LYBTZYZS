using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Shared.Interfaces.Api
{
    /// <summary>
    /// 验方API客户端接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IFormulaApi
{
    /// <summary>
    /// 获取验方列表（支持分页和查询）
    /// </summary>
    [Refit.Get("/api/v1/formulas")]
    Task<Refit.ApiResponse<PagedResult<FormulaDto>>> GetFormulasAsync(
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 20,
        [Refit.Query] string? keyword = null);

    /// <summary>
    /// 获取验方详情
    /// </summary>
    [Refit.Get("/api/v1/formulas/{id}")]
    Task<Refit.ApiResponse<FormulaDto>> GetFormulaByIdAsync(Guid id);

    /// <summary>
    /// 创建验方
    /// </summary>
    [Refit.Post("/api/v1/formulas")]
    Task<Refit.ApiResponse<FormulaDto>> CreateFormulaAsync([Refit.Body] FormulaCreateDto request);

    /// <summary>
    /// 更新验方
    /// </summary>
    [Refit.Put("/api/v1/formulas/{id}")]
    Task<Refit.ApiResponse<FormulaDto>> UpdateFormulaAsync(Guid id, [Refit.Body] FormulaUpdateDto request);

    /// <summary>
    /// 删除验方
    /// </summary>
    [Refit.Delete("/api/v1/formulas/{id}")]
    Task<Refit.ApiResponse<ApiResponse>> DeleteFormulaAsync(Guid id);
}
}