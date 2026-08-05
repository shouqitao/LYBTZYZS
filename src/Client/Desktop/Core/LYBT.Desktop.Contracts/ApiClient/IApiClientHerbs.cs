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
    Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        string? category = null);

    /// <summary>
    /// 按 ID 获取药材详情。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id);

    /// <summary>
    /// 创建新药材。
    /// </summary>
    /// <param name="request">Herb input data.</param>
    Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request);

    /// <summary>
    /// 更新现有药材。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    /// <param name="request">Herb input data.</param>
    Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request);

    /// <summary>
    /// 删除药材（软删除）。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    Task<ApiResponse> DeleteHerbAsync(Guid id);

    /// <summary>
    /// 批量导入药材数据。
    /// </summary>
    /// <param name="request">Batch import input data.</param>
    Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request);

    /// <summary>
    /// 下载药材导入模板。
    /// </summary>
    /// <returns>Template file stream.</returns>
    Task<HttpResponseMessage> ExportTemplateAsync();

    /// <summary>
    /// 将药材数据导出到 Excel。
    /// </summary>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <returns>Excel file stream with herb data.</returns>
    Task<HttpResponseMessage> ExportHerbsAsync(string? keyword = null);

    /// <summary>
    /// 切换药材状态（启用/禁用）。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id);

    /// <summary>
    /// 批量删除药材（软删除）。
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    /// <summary>
    /// 恢复软删除的药材。
    /// </summary>
    /// <param name="id">Herb ID.</param>
    Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id);

    /// <summary>
    /// 批量启用药材。
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request);

    /// <summary>
    /// 批量禁用药材。
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request);

    // ========== Local-only methods ==========

    /// <summary>
    /// 获取全部药材分类（仅本地模式）。
    /// </summary>
    Task<List<string>> GetCategoriesAsync();

    // ========== 泛型段接口默认实现（转发到上方实体命名方法，实现类无需改动） ==========

    Task<ApiResponse<PagedResult<HerbListDto>>> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.GetPagedAsync(
        int page, int pageSize, string? keyword, string? category)
        => GetHerbsAsync(page, pageSize, keyword, category);

    Task<ApiResponse<HerbDetailDto>> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.GetByIdAsync(Guid id)
        => GetHerbByIdAsync(id);

    Task<ApiResponse<HerbDetailDto>> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.CreateAsync(HerbInputDto request)
        => CreateHerbAsync(request);

    Task<ApiResponse<HerbDetailDto>> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.UpdateAsync(Guid id, HerbInputDto request)
        => UpdateHerbAsync(id, request);

    Task<ApiResponse> IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>.DeleteAsync(Guid id)
        => DeleteHerbAsync(id);
}
