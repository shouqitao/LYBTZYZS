// ---------------------------------------------------------------------------
// IApiClientFormulas — Formula Management API Sub-Interface
// ---------------------------------------------------------------------------
// Unified interface combining IFormulaApi (remote) and ILocalFormulaApi (local).
// No Refit attributes — implementations route to the correct backend.
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// 验方管理 API 子接口——CRUD、克隆、导入/导出、批量操作。
/// </summary>
/// <remarks>
/// <para>Combines methods from IFormulaApi (remote) and ILocalFormulaApi (local).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientFormulas : IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>
{
    /// <summary>
    /// 分页获取验方列表，支持可选的分类筛选。
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <param name="category">Category filter (optional).</param>
    Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        string? category = null);

    /// <summary>
    /// 按 ID 获取验方详情。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id);

    /// <summary>
    /// 创建新验方。
    /// </summary>
    /// <param name="request">Formula input data.</param>
    Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request);

    /// <summary>
    /// 更新现有验方。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    /// <param name="request">Formula input data.</param>
    Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request);

    /// <summary>
    /// 删除验方（软删除）。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    Task<ApiResponse> DeleteFormulaAsync(Guid id);

    /// <summary>
    /// 克隆验方。
    /// </summary>
    /// <param name="id">Source formula ID.</param>
    Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id);

    /// <summary>
    /// 切换验方状态（启用/禁用）。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id);

    /// <summary>
    /// 批量删除验方。
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    /// <summary>
    /// 批量导入验方数据。
    /// </summary>
    /// <param name="request">Batch import input data.</param>
    Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request);

    /// <summary>
    /// 将验方数据导出（JSON 含 Herbs 明细，2026-08-13：Excel→JSON；P2 category 筛选）。
    /// </summary>
    /// <param name="category">Category filter (optional).</param>
    /// <returns>JSON file stream with formula data.</returns>
    Task<HttpResponseMessage> ExportFormulasAsync(string? category = null);

    /// <summary>
    /// 下载验方导入模板。
    /// </summary>
    /// <returns>Template file stream.</returns>
    Task<HttpResponseMessage> ExportTemplateAsync();

    /// <summary>
    /// 恢复软删除的验方。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id);

    /// <summary>
    /// 批量启用验方。
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request);

    /// <summary>
    /// 批量禁用验方。
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request);

    /// <summary>
    /// 获取待校验的验方。
    /// </summary>
    Task<ApiResponse<List<FormulaListDto>>> GetPendingValidationAsync();

    /// <summary>
    /// 校验验方中的药材条目（绑定到系统药材库）。
    /// </summary>
    /// <param name="formulaId">Formula ID.</param>
    /// <param name="herbItemId">Herb item ID in the formula.</param>
    /// <param name="request">Validation request with selected herb ID.</param>
    Task<ApiResponse<FormulaHerbItemDto>> ValidateHerbAsync(
        Guid formulaId,
        Guid herbItemId,
        ValidateFormulaHerbInputDto request);

    // ========== 泛型段接口默认实现（转发到上方实体命名方法，实现类无需改动） ==========

    Task<ApiResponse<PagedResult<FormulaListDto>>> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.GetPagedAsync(
        int page, int pageSize, string? keyword, string? category)
        => GetFormulasAsync(page, pageSize, keyword, category);

    Task<ApiResponse<FormulaDetailDto>> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.GetByIdAsync(Guid id)
        => GetFormulaByIdAsync(id);

    Task<ApiResponse<FormulaDetailDto>> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.CreateAsync(FormulaInputDto request)
        => CreateFormulaAsync(request);

    Task<ApiResponse<FormulaDetailDto>> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.UpdateAsync(Guid id, FormulaInputDto request)
        => UpdateFormulaAsync(id, request);

    Task<ApiResponse> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.DeleteAsync(Guid id)
        => DeleteFormulaAsync(id);
}
