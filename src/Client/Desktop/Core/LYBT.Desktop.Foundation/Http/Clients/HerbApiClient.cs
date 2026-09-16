// ---------------------------------------------------------------------------
// HerbApiClient — Refit adapter for IApiClientHerbs
// ---------------------------------------------------------------------------
// Delegates remote calls to IHerbApi (Refit-generated HTTP client).
// Local-only methods throw NotSupportedException in remote mode.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// 药材管理 API 客户端——包装 IHerbApi（Refit）以实现 IApiClientHerbs。
/// </summary>
internal sealed class HerbApiClient : IApiClientHerbs
{
    private readonly IHerbApi _api;

    /// <summary>
    /// 初始化 <see cref="HerbApiClient"/> 类的新实例。
    /// </summary>
    /// <param name="api">Refit-generated herb API client.</param>
    public HerbApiClient(IHerbApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync(
        int page = 1, int pageSize = 20, string? keyword = null, string? category = null,
        CancellationToken ct = default)
        => _api.GetHerbsAsync(page, pageSize, keyword, category, ct);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetHerbByIdAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request, CancellationToken ct = default)
        => _api.CreateHerbAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request, CancellationToken ct = default)
        => _api.UpdateHerbAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse> DeleteHerbAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteHerbAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default)
        => _api.BatchImportAsync(request, ct);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportTemplateAsync(CancellationToken ct = default)
        => _api.ExportTemplateAsync(ct);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportHerbsAsync(string? keyword = null, CancellationToken ct = default)
        => _api.ExportHerbsAsync(keyword, ct);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default)
        => _api.ToggleStatusAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _api.BatchDeleteAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default)
        => _api.RestoreAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _api.BatchEnableAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _api.BatchDisableAsync(request, ct);
}
