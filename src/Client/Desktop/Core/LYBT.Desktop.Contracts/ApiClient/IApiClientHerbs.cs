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
/// 药材管理 API 子接口——CRUD、导入/导出、批量操作。
/// </summary>
/// <remarks>
/// <para>Combines methods from IHerbApi (remote) and ILocalHerbApi (local).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientHerbs : IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>
{
    /// <summary>
    /// 分页获取药材列表，支持可选的分类筛选。
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <param name="category">Category filter (optional).</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        string? category = null,
        CancellationToken ct = default);

    /// <summary>
    /// 按 ID 获取药材详情。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 创建新药材。
    /// </summary>
    /// <param name="request">Herb input data.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 更新现有药材。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    /// <param name="request">Herb input data.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 删除药材（软删除）。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse> DeleteHerbAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 批量导入药材数据。
    /// </summary>
    /// <param name="request">Batch import input data.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 下载药材导入模板。
    /// </summary>
    /// <returns>Template file stream.</returns>
    Task<HttpResponseMessage> ExportTemplateAsync(CancellationToken ct = default);

    /// <summary>
    /// 将药材数据导出（JSON，2026-08-13：Excel→JSON）。
    /// </summary>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>JSON file stream with herb data.</returns>
    Task<HttpResponseMessage> ExportHerbsAsync(string? keyword = null, CancellationToken ct = default);

    /// <summary>
    /// 切换药材状态（启用/禁用）。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 批量删除药材（软删除）。
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 恢复软删除的药材。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 批量启用药材。
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 批量禁用药材。
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    /// <param name="ct">取消令牌</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request, CancellationToken ct = default);

    // ========== 泛型段接口默认实现（转发到上方实体命名方法，实现类无需改动） ==========

    Task<ApiResponse<PagedResult<HerbListDto>>> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.GetPagedAsync(
        int page, int pageSize, string? keyword, string? category,
        CancellationToken ct)
        => GetHerbsAsync(page, pageSize, keyword, category, ct);

    Task<ApiResponse<HerbDetailDto>> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.GetByIdAsync(Guid id, CancellationToken ct)
        => GetHerbByIdAsync(id, ct);

    Task<ApiResponse<HerbDetailDto>> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.CreateAsync(HerbInputDto request, CancellationToken ct)
        => CreateHerbAsync(request, ct);

    Task<ApiResponse<HerbDetailDto>> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.UpdateAsync(Guid id, HerbInputDto request, CancellationToken ct)
        => UpdateHerbAsync(id, request, ct);

    Task<ApiResponse> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.DeleteAsync(Guid id, CancellationToken ct)
        => DeleteHerbAsync(id, ct);
}
