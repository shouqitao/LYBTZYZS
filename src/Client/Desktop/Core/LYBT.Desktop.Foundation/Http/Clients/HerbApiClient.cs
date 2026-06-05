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
/// Herb management API client — wraps IHerbApi (Refit) to implement IApiClientHerbs.
/// </summary>
internal sealed class HerbApiClient : IApiClientHerbs
{
    private readonly IHerbApi _api;

    /// <summary>
    /// Initializes a new instance of <see cref="HerbApiClient"/>.
    /// </summary>
    /// <param name="api">Refit-generated herb API client.</param>
    public HerbApiClient(IHerbApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync(
        int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        => _api.GetHerbsAsync(page, pageSize, keyword, category);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id)
        => _api.GetHerbByIdAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request)
        => _api.CreateHerbAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request)
        => _api.UpdateHerbAsync(id, request);

    /// <inheritdoc />
    public Task<ApiResponse> DeleteHerbAsync(Guid id)
        => _api.DeleteHerbAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request)
        => _api.BatchImportAsync(request);

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportTemplateAsync()
        => _api.ExportTemplateAsync();

    /// <inheritdoc />
    public Task<HttpResponseMessage> ExportHerbsAsync(string? keyword = null)
        => _api.ExportHerbsAsync(keyword);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id)
        => _api.ToggleStatusAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id)
        => _api.RestoreAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)
        => _api.BatchDeleteAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)
        => _api.BatchEnableAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)
        => _api.BatchDisableAsync(request);

    /// <inheritdoc />
    /// <remarks>Local-only method — not available in remote Refit mode.</remarks>
    public Task<List<string>> GetCategoriesAsync()
        => throw new NotSupportedException("GetCategoriesAsync is a local-only method and is not available in remote mode.");
}
