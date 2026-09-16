// ---------------------------------------------------------------------------
// PatientsHttpApiClient — HttpClient adapter for IApiClientPatients
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientPatients (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using System.Net.Http;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式患者 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class PatientsHttpApiClient : HttpApiClientBase, IApiClientPatients
{
    public PatientsHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

    public async Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync(
        int page, int pageSize, string? keyword,
        CancellationToken ct = default)
    {
        var url = BuildPagedUrl("/api/v1/patients", page, pageSize, ("keyword", keyword));
        return await GetPagedAndWrapAsync<PatientListDto>(url, ct);
    }

    public Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id, CancellationToken ct = default)
        => GetAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}", ct);

    public Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<PatientDetailDto>("/api/v1/patients", request, ct);

    public Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request, CancellationToken ct = default)
        => PutAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}", request, ct);

    public Task<ApiResponse> DeletePatientAsync(Guid id, CancellationToken ct = default)
        => DeleteVoidAsync($"/api/v1/patients/{id}", ct);

    public Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<PatientBatchImportResultDto>("/api/v1/patients/batch-import", request, ct);

    public Task<HttpResponseMessage> ExportTemplateAsync(CancellationToken ct = default)
        => GetResponseAsync("/api/v1/patients/import-template", ct);

    public async Task<HttpResponseMessage> ExportPatientsAsync(string? keyword, CancellationToken ct = default)
    {
        var url = "/api/v1/patients/export";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"?keyword={Uri.EscapeDataString(keyword)}";
        return await GetResponseAsync(url, ct);
    }

    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/patients/batch-delete", request, ct);

    public Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default)
        => PostAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}/toggle-status", ct: ct);

    public Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default)
        => PostAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}/restore", ct: ct);
}
