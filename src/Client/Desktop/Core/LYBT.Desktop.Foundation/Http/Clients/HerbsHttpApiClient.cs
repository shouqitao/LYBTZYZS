// ---------------------------------------------------------------------------
// HerbsHttpApiClient — HttpClient adapter for IApiClientHerbs
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientHerbs (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式中药 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class HerbsHttpApiClient : HttpApiClientBase, IApiClientHerbs
{
    public HerbsHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public async Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsAsync(
        int page, int pageSize, string? keyword, string? category)
    {
        var url = BuildPagedUrl("/api/v1/herbs", page, pageSize, ("keyword", keyword), ("category", category));
        return await GetPagedAndWrapAsync<HerbListDto>(url);
    }

    public Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id)
        => GetAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}");

    public Task<ApiResponse<HerbDetailDto>> CreateHerbAsync(HerbInputDto request)
        => PostAndWrapAsync<HerbDetailDto>("/api/v1/herbs", request);

    public Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, HerbInputDto request)
        => PutAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}", request);

    public Task<ApiResponse> DeleteHerbAsync(Guid id)
        => DeleteVoidAsync($"/api/v1/herbs/{id}");

    public Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(HerbBatchImportInputDto request)
        => PostAndWrapAsync<HerbBatchImportResultDto>("/api/v1/herbs/batch-import", request);

    public Task<HttpResponseMessage> ExportTemplateAsync()
        => GetResponseAsync("/api/v1/herbs/import-template");

    public async Task<HttpResponseMessage> ExportHerbsAsync(string? keyword)
    {
        var url = "/api/v1/herbs/export";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"?keyword={Uri.EscapeDataString(keyword)}";
        return await GetResponseAsync(url);
    }

    public Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}/toggle-status");

    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/herbs/batch-delete", request);

    public Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id)
        => PostAndWrapAsync<HerbDetailDto>($"/api/v1/herbs/{id}/restore");

    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/herbs/batch-enable", request);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/herbs/batch-disable", request);

    public Task<List<string>> GetCategoriesAsync()
        => GetRawAsync<List<string>>("/api/v1/herbs/categories");
}
