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
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        string? category = null,
        CancellationToken ct = default);

    /// <summary>
    /// 按 ID 获取验方详情。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 创建新验方。
    /// </summary>
    /// <param name="request">Formula input data.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 更新现有验方。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    /// <param name="request">Formula input data.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 删除验方（软删除）。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse> DeleteFormulaAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 克隆验方。
    /// </summary>
    /// <param name="id">Source formula ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 切换验方状态（启用/禁用）。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 批量删除验方。
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 批量导入验方数据。
    /// </summary>
    /// <param name="request">Batch import input data.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 将验方数据导出（JSON 含 Herbs 明细，2026-08-13：Excel→JSON；P2 category 筛选）。
    /// </summary>
    /// <param name="category">Category filter (optional).</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>JSON file stream with formula data.</returns>
    Task<HttpResponseMessage> ExportFormulasAsync(string? category = null, CancellationToken ct = default);

    /// <summary>
    /// 下载验方导入模板。
    /// </summary>
    /// <returns>Template file stream.</returns>
    Task<HttpResponseMessage> ExportTemplateAsync(CancellationToken ct = default);

    /// <summary>
    /// 恢复软删除的验方。
    /// </summary>
    /// <param name="id">Formula ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 批量启用验方。
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 批量禁用验方。
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 获取待校验的验方（分页——服务端返回带 Herbs 明细的 DetailDto）。
    /// </summary>
    Task<ApiResponse<PagedResult<FormulaDetailDto>>> GetPendingValidationAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// 校验验方中的药材条目（绑定到系统药材库）。
    /// </summary>
    /// <param name="formulaId">Formula ID.</param>
    /// <param name="herbItemId">Herb item ID in the formula.</param>
    /// <param name="request">Validation request with selected herb ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse> ValidateHerbAsync(
        Guid formulaId,
        Guid herbItemId,
        ValidateFormulaHerbInputDto request,
        CancellationToken ct = default);

    // ========== 泛型段接口默认实现（转发到上方实体命名方法，实现类无需改动） ==========

    Task<ApiResponse<PagedResult<FormulaListDto>>> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.GetPagedAsync(
        int page, int pageSize, string? keyword, string? category,
        CancellationToken ct)
        => GetFormulasAsync(page, pageSize, keyword, category, ct);

    Task<ApiResponse<FormulaDetailDto>> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.GetByIdAsync(Guid id, CancellationToken ct)
        => GetFormulaByIdAsync(id, ct);

    Task<ApiResponse<FormulaDetailDto>> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.CreateAsync(FormulaInputDto request, CancellationToken ct)
        => CreateFormulaAsync(request, ct);

    Task<ApiResponse<FormulaDetailDto>> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.UpdateAsync(Guid id, FormulaInputDto request, CancellationToken ct)
        => UpdateFormulaAsync(id, request, ct);

    Task<ApiResponse> IEntityApiSegment<FormulaListDto, FormulaDetailDto, FormulaInputDto>.DeleteAsync(Guid id, CancellationToken ct)
        => DeleteFormulaAsync(id, ct);
}
