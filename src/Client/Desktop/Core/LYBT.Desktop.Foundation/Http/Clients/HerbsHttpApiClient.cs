// ---------------------------------------------------------------------------
// HerbsHttpApiClient — HttpClient adapter for IApiClientHerbs
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientHerbs (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using System.Net.Http;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式中药 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class HerbsHttpApiClient : HttpApiClientBase, IApiClientHerbs
{
    public HerbsHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

    public async Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync(
        int page, int pageSize, string? keyword, string? category,
        CancellationToken ct = default)
    {
        var url = BuildPagedUrl("/api/v1/herbs", page, pageSize, ("keyword", keyword), ("category", category));
        return await GetPagedAndWrapAsync<HerbListDto>(url, ct);
    }

    public Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id, CancellationToken ct = default)
        => GetAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}", ct);

    public Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<HerbDetailDto>("/api/v1/herbs", request, ct);

    public Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request, CancellationToken ct = default)
        => PutAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}", request, ct);

    public Task<ApiResponse> DeleteHerbAsync(Guid id, CancellationToken ct = default)
        => DeleteVoidAsync($"/api/v1/herbs/{id}", ct);

    public Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<HerbBatchImportResultDto>("/api/v1/herbs/batch-import", request, ct);

    public Task<HttpResponseMessage> ExportTemplateAsync(CancellationToken ct = default)
        => GetResponseAsync("/api/v1/herbs/import-template", ct);

    public async Task<HttpResponseMessage> ExportHerbsAsync(string? keyword, CancellationToken ct = default)
    {
        var url = "/api/v1/herbs/export";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"?keyword={Uri.EscapeDataString(keyword)}";
        return await GetResponseAsync(url, ct);
    }

    public Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default)
        => PostAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}/toggle-status", ct: ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/herbs/batch-delete", request, ct);

    public Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default)
        => PostAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}/restore", ct: ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/herbs/batch-enable", request, ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/herbs/batch-disable", request, ct);
}
