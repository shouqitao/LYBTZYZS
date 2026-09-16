// ---------------------------------------------------------------------------
// MedicalCasesHttpApiClient — HttpClient adapter for IApiClientMedicalCases
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientMedicalCases (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.ApiClient;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式医案 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class MedicalCasesHttpApiClient : HttpApiClientBase, IApiClientMedicalCases
{
    public MedicalCasesHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

    public async Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync(
        int page, int pageSize, string? keyword, bool includeAllDoctors,
        CancellationToken ct = default)
    {
        var url = $"/api/v1/medicalcases?page={page}&pageSize={pageSize}&includeAllDoctors={includeAllDoctors.ToString().ToLower()}";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";
        return await GetAndWrapAsync<PagedResult<MedicalCaseListDto>>(url, ct);
    }

    public async Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync(
        MedicalCaseQueryType queryType, Guid? patientId, Guid? doctorId, string? keyword,
        int pageIndex, int pageSize, bool includeAllDoctors, int? limit,
        CancellationToken ct = default)
    {
        var url = $"/api/v1/medicalcases/query?queryType={queryType}&pageIndex={pageIndex}&pageSize={pageSize}&includeAllDoctors={includeAllDoctors.ToString().ToLower()}";
        if (patientId.HasValue) url += $"&patientId={patientId.Value}";
        if (doctorId.HasValue) url += $"&doctorId={doctorId.Value}";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (limit.HasValue) url += $"&limit={limit.Value}";
        return await GetAndWrapAsync<PagedResult<MedicalCaseListDto>>(url, ct);
    }

    public Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id, CancellationToken ct = default)
        => GetAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}", ct);

    public async Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(Guid? patientId, CancellationToken ct = default)
    {
        var url = "/api/v1/medicalcases/pending";
        if (patientId.HasValue) url += $"?patientId={patientId.Value}";
        return await GetAndWrapAsync<List<PendingMedicalCaseDto>>(url, ct);
    }

    public async Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync(
        string? patientName, string? diagnosisKeyword, DateTime? startDate, DateTime? endDate, int page, int pageSize,
        CancellationToken ct = default)
    {
        var url = $"/api/v1/medicalcases/search?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(patientName)) url += $"&patientName={Uri.EscapeDataString(patientName)}";
        if (!string.IsNullOrWhiteSpace(diagnosisKeyword)) url += $"&diagnosisKeyword={Uri.EscapeDataString(diagnosisKeyword)}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:O}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:O}";
        return await GetAndWrapAsync<PagedResult<MedicalCaseDetailDto>>(url, ct);
    }

    public Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<MedicalCaseDetailDto>("/api/v1/medicalcases", request, ct);

    public Task<ApiResponse> DeleteMedicalCaseAsync(Guid id, CancellationToken ct = default)
        => DeleteVoidAsync($"/api/v1/medicalcases/{id}", ct);

    public Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(Guid medicalCaseId, SetPrescriptionFlagRequest request, CancellationToken ct = default)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{medicalCaseId}/prescription-flag", request, ct);

    public Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id, CancellationToken ct = default)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}/close", ct: ct);

    public Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync(Guid id, ConsultationInputDto? request, CancellationToken ct = default)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}/suspend", request, ct);

    public async Task<ApiResponse> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequest? request, CancellationToken ct = default)
    {
        await PutVoidAsync($"/api/v1/medicalcases/{id}/cancel", request, ct);
        return WrapSuccess();
    }

    public Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request, CancellationToken ct = default)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}/status", request, ct);

    public Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(Guid id, MedicalCaseInputDto request, CancellationToken ct = default)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}", request, ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/medicalcases/batch-delete", request, ct);

    public Task<ApiResponse<MedicalCasePermissionsDto>> GetPermissionsAsync(Guid id, CancellationToken ct = default)
        => GetAndWrapAsync<MedicalCasePermissionsDto>($"/api/v1/medicalcases/{id}/permissions", ct);

    public Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintAsync(Guid id, RecordPrintRequest request, CancellationToken ct = default)
        => PutAndWrapAsync<MedicalCaseDetailDto>($"/api/v1/medicalcases/{id}/print-completed", request, ct);

    public Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid id, int page, int pageSize, CancellationToken ct = default)
        => GetAndWrapAsync<PagedResult<AuditLogDto>>($"/api/v1/medicalcases/{id}/audit-logs?page={page}&pageSize={pageSize}", ct);
}
