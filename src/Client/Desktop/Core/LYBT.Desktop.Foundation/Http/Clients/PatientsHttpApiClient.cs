// ---------------------------------------------------------------------------
// PatientsHttpApiClient — HttpClient adapter for IApiClientPatients
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientPatients (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式患者 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class PatientsHttpApiClient : HttpApiClientBase, IApiClientPatients
{
    public PatientsHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public async Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync(
        int page, int pageSize, string? keyword)
    {
        var url = BuildPagedUrl("/api/v1/patients", page, pageSize, ("keyword", keyword));
        return await GetPagedAndWrapAsync<PatientListDto>(url);
    }

    public Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id)
        => GetAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}");

    public Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request)
        => PostAndWrapAsync<PatientDetailDto>("/api/v1/patients", request);

    public Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request)
        => PutAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}", request);

    public Task<ApiResponse> DeletePatientAsync(Guid id)
        => DeleteVoidAsync($"/api/v1/patients/{id}");

    public Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request)
        => PostAndWrapAsync<PatientBatchImportResultDto>("/api/v1/patients/batch-import", request);

    public Task<HttpResponseMessage> ExportTemplateAsync()
        => GetResponseAsync("/api/v1/patients/import-template");

    public async Task<HttpResponseMessage> ExportPatientsAsync(string? keyword)
    {
        var url = "/api/v1/patients/export";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"?keyword={Uri.EscapeDataString(keyword)}";
        return await GetResponseAsync(url);
    }

    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/patients/batch-delete", request);

    public Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}/toggle-status");

    public Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id)
        => PostAndWrapAsync<PatientDetailDto>($"/api/v1/patients/{id}/restore");
}
