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
/// Formula management API sub-interface — CRUD, clone, import/export, batch operations.
/// </summary>
/// <remarks>
/// <para>Combines methods from IFormulaApi (remote) and ILocalFormulaApi (local).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientFormulas
{
    /// <summary>
    /// Get formula list with pagination and optional category filter.
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
    /// Get formula detail by ID.
    /// </summary>
    /// <param name="id">Formula ID.</param>
    Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id);

    /// <summary>
    /// Create a new formula.
    /// </summary>
    /// <param name="request">Formula input data.</param>
    Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request);

    /// <summary>
    /// Update an existing formula.
    /// </summary>
    /// <param name="id">Formula ID.</param>
    /// <param name="request">Formula input data.</param>
    Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request);

    /// <summary>
    /// Delete a formula (soft delete).
    /// </summary>
    /// <param name="id">Formula ID.</param>
    Task<ApiResponse> DeleteFormulaAsync(Guid id);

    /// <summary>
    /// Clone a formula.
    /// </summary>
    /// <param name="id">Source formula ID.</param>
    Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id);

    /// <summary>
    /// Toggle formula status (enable/disable).
    /// </summary>
    /// <param name="id">Formula ID.</param>
    Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id);

    /// <summary>
    /// Restore a soft-deleted formula.
    /// </summary>
    /// <param name="id">Formula ID.</param>
    Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id);

    /// <summary>
    /// Batch delete formulas.
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    /// <summary>
    /// Batch enable formulas.
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request);

    /// <summary>
    /// Batch disable formulas.
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request);

    /// <summary>
    /// Batch import formula data.
    /// OpenSpec: standardize-api-naming - REQ-API-002 batch URL pattern
    /// </summary>
    /// <param name="request">Batch import input data.</param>
    Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request);

    /// <summary>
    /// Export formula data to Excel.
    /// </summary>
    /// <param name="category">Category filter (optional).</param>
    /// <returns>Excel file stream with formula data.</returns>
    Task<HttpResponseMessage> ExportFormulasAsync(string? category = null);

    /// <summary>
    /// Download formula import template.
    /// </summary>
    /// <returns>Template file stream.</returns>
    Task<HttpResponseMessage> ExportTemplateAsync();

    // ========== Local-only methods ==========

    /// <summary>
    /// Get all formula categories (local mode only).
    /// </summary>
    Task<List<string>> GetCategoriesAsync();
}
