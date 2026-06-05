// ---------------------------------------------------------------------------
// IApiClientHerbs — Herb Management API Sub-Interface
// ---------------------------------------------------------------------------
// Unified interface combining IHerbApi (remote) and ILocalHerbApi (local).
// No Refit attributes — implementations route to the correct backend.
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// Herb management API sub-interface — CRUD, import/export, batch operations.
/// </summary>
/// <remarks>
/// <para>Combines methods from IHerbApi (remote) and ILocalHerbApi (local).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientHerbs
{
    /// <summary>
    /// Get herb list with pagination and optional category filter.
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <param name="category">Category filter (optional).</param>
    Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        string? category = null);

    /// <summary>
    /// Get herb detail by ID.
    /// </summary>
    /// <param name="id">Herb ID.</param>
    Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id);

    /// <summary>
    /// Create a new herb.
    /// </summary>
    /// <param name="request">Herb input data.</param>
    Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request);

    /// <summary>
    /// Update an existing herb.
    /// </summary>
    /// <param name="id">Herb ID.</param>
    /// <param name="request">Herb input data.</param>
    Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request);

    /// <summary>
    /// Delete a herb (soft delete).
    /// </summary>
    /// <param name="id">Herb ID.</param>
    Task<ApiResponse> DeleteHerbAsync(Guid id);

    /// <summary>
    /// Batch import herb data.
    /// </summary>
    /// <param name="request">Batch import input data.</param>
    Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request);

    /// <summary>
    /// Download herb import template.
    /// </summary>
    /// <returns>Template file stream.</returns>
    Task<HttpResponseMessage> ExportTemplateAsync();

    /// <summary>
    /// Export herb data to Excel.
    /// </summary>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <returns>Excel file stream with herb data.</returns>
    Task<HttpResponseMessage> ExportHerbsAsync(string? keyword = null);

    /// <summary>
    /// Toggle herb status (enable/disable).
    /// </summary>
    /// <param name="id">Herb ID.</param>
    Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id);

    /// <summary>
    /// Restore a soft-deleted herb.
    /// </summary>
    /// <param name="id">Herb ID.</param>
    Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id);

    /// <summary>
    /// Batch delete herbs (soft delete).
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    /// <summary>
    /// Batch enable herbs.
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request);

    /// <summary>
    /// Batch disable herbs.
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request);

    // ========== Local-only methods ==========

    /// <summary>
    /// Get all herb categories (local mode only).
    /// </summary>
    Task<List<string>> GetCategoriesAsync();
}
