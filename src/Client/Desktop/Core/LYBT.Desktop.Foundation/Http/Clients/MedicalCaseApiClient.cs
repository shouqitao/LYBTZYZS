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
/// Medical case API client — wraps IMedicalCaseApi (Refit) to implement IApiClientMedicalCases.
/// </summary>
internal sealed class MedicalCaseApiClient : IApiClientMedicalCases
{
    private readonly IMedicalCaseApi _api;

    /// <summary>
    /// Initializes a new instance of <see cref="MedicalCaseApiClient"/>.
    /// </summary>
    /// <param name="api">Refit-generated medical case API client.</param>
    public MedicalCaseApiClient(IMedicalCaseApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync(
        int page = 1, int pageSize = 20, string? keyword = null, bool includeAllDoctors = false)
        => _api.GetMedicalCasesAsync(page, pageSize, keyword, includeAllDoctors);

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync(
        MedicalCaseQueryType queryType = MedicalCaseQueryType.All,
        Guid? patientId = null,
        Guid? doctorId = null,
        string? keyword = null,
        int pageIndex = 1,
        int pageSize = 20,
        bool includeAllDoctors = false,
        int? limit = null)
        => _api.QueryMedicalCasesAsync(queryType, patientId, doctorId, keyword, pageIndex, pageSize, includeAllDoctors, limit);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id)
        => _api.GetMedicalCaseByIdAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(Guid? patientId = null)
        => _api.GetPendingCasesAsync(patientId);

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync(
        string? patientName = null,
        string? diagnosisKeyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
        => _api.SearchMedicalCasesAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseInputDto request)
        => _api.CreateMedicalCaseAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse> DeleteMedicalCaseAsync(Guid id)
        => _api.DeleteMedicalCaseAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(
        Guid medicalCaseId, SetPrescriptionFlagRequest request)
        => _api.SetPrescriptionFlagAsync(medicalCaseId, request);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id)
        => _api.CloseCaseAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync(Guid id, ConsultationInputDto? request = null)
        => _api.SuspendAsync(id, request);

    /// <inheritdoc />
    /// <remarks>
    /// The underlying IMedicalCaseApi returns Refit.IApiResponse; this adapter converts it
    /// to the shared ApiResponse type used by IApiClientMedicalCases.
    /// </remarks>
    public async Task<ApiResponse> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request = null)
    {
        var refitResponse = await _api.CancelMedicalCaseAsync(id, request).ConfigureAwait(false);
        if (refitResponse.IsSuccessStatusCode)
            return new ApiResponse { Success = true, Message = "操作成功" };

        return new ApiResponse { Success = false, Message = refitResponse.ReasonPhrase ?? "操作失败" };
    }

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
        => _api.UpdateStatusAsync(id, request);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCasePermissionDto>> GetPermissionsAsync(Guid id)
        => _api.GetPermissionsAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseAuditLogPagedResultDto>> GetAuditLogsAsync(Guid id, int page = 1, int pageSize = 20)
        => _api.GetAuditLogsAsync(id, page, pageSize);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(Guid id, MedicalCaseInputDto request)
        => _api.SaveAsync(id, request);

    /// <inheritdoc />
    public Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
        => _api.RecordPrintCompletedAsync(medicalCaseId, request);

    /// <inheritdoc />
    public Task<ApiResponse<object>> AddPrintLogAsync(Guid medicalCaseId, PrintLogInputDto request)
        => _api.AddPrintLogAsync(medicalCaseId, request);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)
        => _api.BatchDeleteAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<List<MedicalCaseDetailDto>>> GetBatchDetailsAsync(BatchDetailQueryDto request)
        => _api.GetBatchDetailsAsync(request);
}
