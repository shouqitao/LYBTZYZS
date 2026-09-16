// ---------------------------------------------------------------------------
// FormulasHttpApiClient — HttpClient adapter for IApiClientFormulas
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientFormulas (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using System.Net.Http;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式验方 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class FormulasHttpApiClient : HttpApiClientBase, IApiClientFormulas
{
    public FormulasHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

    public async Task<ApiResponse<PagedResult<FormulaListDto>>> GetFormulasAsync(
        int page, int pageSize, string? keyword, string? category,
        CancellationToken ct = default)
    {
        var url = BuildPagedUrl("/api/v1/formulas", page, pageSize, ("keyword", keyword), ("category", category));
        return await GetPagedAndWrapAsync<FormulaListDto>(url, ct);
    }

    public Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id, CancellationToken ct = default)
        => GetAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}", ct);

    public Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<FormulaDetailDto>("/api/v1/formulas", request, ct);

    public Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request, CancellationToken ct = default)
        => PutAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}", request, ct);

    public Task<ApiResponse> DeleteFormulaAsync(Guid id, CancellationToken ct = default)
        => DeleteVoidAsync($"/api/v1/formulas/{id}", ct);

    public Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id, CancellationToken ct = default)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}/clone", ct: ct);

    public Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}/toggle-status", ct: ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/formulas/batch-delete", request, ct);

    public Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<FormulaBatchImportResultDto>("/api/v1/formulas/batch-import", request, ct);

    public async Task<HttpResponseMessage> ExportFormulasAsync(string? category, CancellationToken ct = default)
    {
        var url = "/api/v1/formulas/export";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"?category={Uri.EscapeDataString(category)}";
        return await GetResponseAsync(url, ct);
    }

    public Task<HttpResponseMessage> ExportTemplateAsync(CancellationToken ct = default)
        => GetResponseAsync("/api/v1/formulas/import-template", ct);

    public Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}/restore", ct: ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/formulas/batch-enable", request, ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/formulas/batch-disable", request, ct);

    public Task<ApiResponse<PagedResult<FormulaDetailDto>>> GetPendingValidationAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        => GetPagedAndWrapAsync<FormulaDetailDto>(BuildPagedUrl("/api/v1/formulas/pending-validation", page, pageSize), ct);

    public Task<ApiResponse> ValidateHerbAsync(
        Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto request,
        CancellationToken ct = default)
        => PostVoidAsync($"/api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate", request, ct);
}
