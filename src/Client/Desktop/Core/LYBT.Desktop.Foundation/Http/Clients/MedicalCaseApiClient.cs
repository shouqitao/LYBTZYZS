// ---------------------------------------------------------------------------
// MedicalCaseApiClient — Refit adapter for IApiClientMedicalCases
// ---------------------------------------------------------------------------
// Delegates all calls to IMedicalCaseApi (Refit-generated HTTP client).
// Handles return type conversion for CancelMedicalCaseAsync (Refit.IApiResponse → ApiResponse).
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// 医案 API 客户端——包装 IMedicalCaseApi（Refit）以实现 IApiClientMedicalCases。
/// </summary>
internal sealed class MedicalCaseApiClient : IApiClientMedicalCases
{
    private readonly IMedicalCaseApi _api;

    /// <summary>
    /// 初始化 <see cref="MedicalCaseApiClient"/> 类的新实例。
    /// </summary>
    /// <param name="api">Refit-generated medical case API client.</param>
    public MedicalCaseApiClient(IMedicalCaseApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync(
        int page = 1, int pageSize = 20, string? keyword = null, bool includeAllDoctors = false,
        CancellationToken ct = default)
        => _api.GetMedicalCasesAsync(page, pageSize, keyword, includeAllDoctors, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync(
        MedicalCaseQueryType queryType = MedicalCaseQueryType.All,
        Guid? patientId = null,
        Guid? doctorId = null,
        string? keyword = null,
        int pageIndex = 1,
        int pageSize = 20,
        bool includeAllDoctors = false,
        int? limit = null,
        CancellationToken ct = default)
        => _api.QueryMedicalCasesAsync(queryType, patientId, doctorId, keyword, pageIndex, pageSize, includeAllDoctors, limit, ct);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetMedicalCaseByIdAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(Guid? patientId = null, CancellationToken ct = default)
        => _api.GetPendingCasesAsync(patientId, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync(
        string? patientName = null,
        string? diagnosisKeyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
        => _api.SearchMedicalCasesAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize, ct);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseInputDto request, CancellationToken ct = default)
        => _api.CreateMedicalCaseAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse> DeleteMedicalCaseAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteMedicalCaseAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(
        Guid medicalCaseId, SetPrescriptionFlagRequest request,
        CancellationToken ct = default)
        => _api.SetPrescriptionFlagAsync(medicalCaseId, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id, CancellationToken ct = default)
        => _api.CloseCaseAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync(Guid id, ConsultationInputDto? request = null, CancellationToken ct = default)
        => _api.SuspendAsync(id, request, ct);

    /// <inheritdoc />
    /// <remarks>
    /// The underlying IMedicalCaseApi returns Refit.IApiResponse; this adapter converts it
    /// to the shared ApiResponse type used by IApiClientMedicalCases.
    /// </remarks>
    public async Task<ApiResponse> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequest? request = null, CancellationToken ct = default)
    {
        var refitResponse = await _api.CancelMedicalCaseAsync(id, request, ct).ConfigureAwait(false);
        if (refitResponse.IsSuccessStatusCode)
            return new ApiResponse { Success = true, Message = "操作成功" };

        return new ApiResponse { Success = false, Message = refitResponse.ReasonPhrase ?? "操作失败" };
    }

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request, CancellationToken ct = default)
        => _api.UpdateStatusAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(Guid id, MedicalCaseInputDto request, CancellationToken ct = default)
        => _api.SaveAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _api.BatchDeleteAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCasePermissionsDto>> GetPermissionsAsync(Guid id, CancellationToken ct = default)
        => _api.GetPermissionsAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintAsync(Guid id, RecordPrintRequest request, CancellationToken ct = default)
        => _api.RecordPrintAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid id, int page = 1, int pageSize = 20, CancellationToken ct = default)
        => _api.GetAuditLogsAsync(id, page, pageSize, ct);
}
