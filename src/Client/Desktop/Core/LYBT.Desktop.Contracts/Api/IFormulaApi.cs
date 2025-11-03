using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Contracts.Api
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
        Task<ApiResponse<PagedResult<FormulaDto>>> GetFormulasAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取验方详情
        /// </summary>
        [Refit.Get("/api/v1/formulas/{id}")]
        Task<ApiResponse<FormulaDto>> GetFormulaByIdAsync(Guid id);

        /// <summary>
        /// 创建验方
        /// </summary>
        [Refit.Post("/api/v1/formulas")]
        Task<ApiResponse<FormulaDto>> CreateFormulaAsync([Refit.Body] FormulaInputDto request);

        /// <summary>
        /// 更新验方
        /// </summary>
        [Refit.Put("/api/v1/formulas/{id}")]
        Task<ApiResponse<FormulaDto>> UpdateFormulaAsync(Guid id, [Refit.Body] FormulaInputDto request);

        /// <summary>
        /// 删除验方
        /// </summary>
        [Refit.Delete("/api/v1/formulas/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteFormulaAsync(Guid id);

        /// <summary>
        /// 克隆验方
        /// </summary>
        [Refit.Post("/api/v1/formulas/{id}/clone")]
        Task<ApiResponse<FormulaDto>> CloneFormulaAsync(Guid id);

        /// <summary>
        /// 获取待校验的验方列表 (Issue #1349)
        /// </summary>
        [Refit.Get("/api/v1/formulas/pending-validation")]
        Task<ApiResponse<List<FormulaDto>>> GetPendingValidationFormulasAsync();

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        [Refit.Post("/api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate")]
        Task<ApiResponse<ApiResponse>> ValidateFormulaHerbAsync(
            Guid formulaId,
            Guid herbItemId,
            [Refit.Body] Guid selectedHerbId);
    }
}
