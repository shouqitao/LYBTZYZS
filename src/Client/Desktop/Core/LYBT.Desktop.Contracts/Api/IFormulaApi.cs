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
        /// 获取验方列表（分页查询）
        /// </summary>
        [Refit.Get("/api/v1/formulas")]
        Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null,
            [Refit.Query] string? category = null);

        /// <summary>
        /// 获取验方详情
        /// </summary>
        [Refit.Get("/api/v1/formulas/{id}")]
        Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id);

        /// <summary>
        /// 创建验方
        /// </summary>
        [Refit.Post("/api/v1/formulas")]
        Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync([Refit.Body] FormulaInputDto request);

        /// <summary>
        /// 更新验方
        /// </summary>
        [Refit.Put("/api/v1/formulas/{id}")]
        Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, [Refit.Body] FormulaInputDto request);

        /// <summary>
        /// 删除验方
        /// </summary>
        [Refit.Delete("/api/v1/formulas/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteFormulaAsync(Guid id);

        /// <summary>
        /// 克隆验方
        /// </summary>
        [Refit.Post("/api/v1/formulas/{id}/clone")]
        Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id);

        /// <summary>
        /// 获取待校验的验方列表 (Issue #1349)
        /// </summary>
        [Refit.Get("/api/v1/formulas/pending-validation")]
        Task<ApiResponse<List<FormulaDetailDto>>> GetPendingValidationFormulasAsync();

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        [Refit.Post("/api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate")]
        Task<ApiResponse<ApiResponse>> ValidateFormulaHerbAsync(
            Guid formulaId,
            Guid herbItemId,
            [Refit.Body] Guid selectedHerbId);

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复 ==========

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        [Refit.Post("/api/v1/formulas/{id}/toggle-status")]
        Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复已删除的验方
        /// </summary>
        [Refit.Post("/api/v1/formulas/{id}/restore")]
        Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id);

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除验方
        /// </summary>
        [Refit.Post("/api/v1/formulas/batch-delete")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

        /// <summary>
        /// 批量启用验方
        /// </summary>
        [Refit.Post("/api/v1/formulas/batch-enable")]
        Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);

        /// <summary>
        /// 批量禁用验方
        /// </summary>
        [Refit.Post("/api/v1/formulas/batch-disable")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);
    }
}
