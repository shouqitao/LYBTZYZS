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
        int page, int pageSize, string? keyword, string? category)
    {
        var url = BuildPagedUrl("/api/v1/formulas", page, pageSize, ("keyword", keyword), ("category", category));
        return await GetPagedAndWrapAsync<FormulaListDto>(url);
    }

    public Task<ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id)
        => GetAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}");

    public Task<ApiResponse<FormulaDetailDto>> CreateFormulaAsync(FormulaInputDto request)
        => PostAndWrapAsync<FormulaDetailDto>("/api/v1/formulas", request);

    public Task<ApiResponse<FormulaDetailDto>> UpdateFormulaAsync(Guid id, FormulaInputDto request)
        => PutAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}", request);

    public Task<ApiResponse> DeleteFormulaAsync(Guid id)
        => DeleteVoidAsync($"/api/v1/formulas/{id}");

    public Task<ApiResponse<FormulaDetailDto>> CloneFormulaAsync(Guid id)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}/clone");

    public Task<ApiResponse<FormulaDetailDto>> ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}/toggle-status");

    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/formulas/batch-delete", request);

    public Task<ApiResponse<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request)
        => PostAndWrapAsync<FormulaBatchImportResultDto>("/api/v1/formulas/batch-import", request);

    public async Task<HttpResponseMessage> ExportFormulasAsync(string? category)
    {
        var url = "/api/v1/formulas/export";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"?category={Uri.EscapeDataString(category)}";
        return await GetResponseAsync(url);
    }

    public Task<HttpResponseMessage> ExportTemplateAsync()
        => GetResponseAsync("/api/v1/formulas/import-template");

    public Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id)
        => PostAndWrapAsync<FormulaDetailDto>($"/api/v1/formulas/{id}/restore");

    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/formulas/batch-enable", request);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/formulas/batch-disable", request);

    public Task<ApiResponse<PagedResult<FormulaDetailDto>>> GetPendingValidationAsync(int page = 1, int pageSize = 20)
        => GetPagedAndWrapAsync<FormulaDetailDto>(BuildPagedUrl("/api/v1/formulas/pending-validation", page, pageSize));

    public Task<ApiResponse> ValidateHerbAsync(
        Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto request)
        => PostVoidAsync($"/api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate", request);
}
