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
        Task<ApiResponse> DeleteFormulaAsync(Guid id);

        /// <summary>
        /// 克隆验方
        /// </summary>
        [Refit.Post("/api/v1/formulas/{id}/clone")]
        Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id);
        // 原Issue #1349/#1348为FormulaValidationViewModel设计，该ViewModel已删除
        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        [Refit.Post("/api/v1/formulas/{id}/toggle-status")]
        Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id);
        /// <summary>
        /// 批量删除验方
        /// </summary>
        [Refit.Post("/api/v1/formulas/batch-delete")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);
        /// <summary>
        /// 批量导入验方数据
        /// </summary>
        [Refit.Post("/api/v1/formulas/batch-import")]
        Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync([Refit.Body] FormulaBatchImportInputDto request);

        /// <summary>
        /// 导出验方数据到Excel
        /// </summary>
        [Refit.Get("/api/v1/formulas/export")]
        Task<HttpResponseMessage> ExportFormulasAsync([Refit.Query] string? category = null);

        /// <summary>
        /// 下载验方导入模板
        /// </summary>
        [Refit.Get("/api/v1/formulas/import-template")]
        Task<HttpResponseMessage> ExportTemplateAsync();
    }
}
