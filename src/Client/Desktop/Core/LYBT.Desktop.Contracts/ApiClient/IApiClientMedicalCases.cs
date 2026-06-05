// ---------------------------------------------------------------------------
// IApiClientMedicalCases — Medical Case API Sub-Interface
// ---------------------------------------------------------------------------
// Unified interface combining IMedicalCaseApi (remote) and ILocalMedicalCaseApi (local).
// No Refit attributes — implementations route to the correct backend.
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// Medical case API sub-interface — CRUD, status transitions, prescriptions, audit logs.
/// Largest API interface with 19+ methods.
/// </summary>
/// <remarks>
/// <para>Combines methods from IMedicalCaseApi (remote) and ILocalMedicalCaseApi (local).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientMedicalCases
{
    /// <summary>
    /// Get medical case list with pagination.
    /// OpenSpec: fix-history-copy-all-patients — added includeAllDoctors parameter.
    /// OpenSpec: post-release-cleanup — unified return to MedicalCaseListDto.
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <param name="includeAllDoctors">Include cases from all doctors (admin use).</param>
    Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        bool includeAllDoctors = false);

    /// <summary>
    /// Unified medical case query endpoint.
    /// OpenSpec: optimize-medicalcase-api — consolidates multiple query methods.
    /// </summary>
    /// <param name="queryType">Query type filter.</param>
    /// <param name="patientId">Patient ID (required for ByPatient/Unfinished/Recent).</param>
    /// <param name="doctorId">Doctor ID (optional).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <param name="pageIndex">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="includeAllDoctors">Include cases from all doctors.</param>
    /// <param name="limit">Result limit (used with Recent query type).</param>
    Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync(
        MedicalCaseQueryType queryType = MedicalCaseQueryType.All,
        Guid? patientId = null,
        Guid? doctorId = null,
        string? keyword = null,
        int pageIndex = 1,
        int pageSize = 20,
        bool includeAllDoctors = false,
        int? limit = null);

    /// <summary>
    /// Get medical case detail by ID.
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id);

    /// <summary>
    /// Get pending cases (Status=Draft/Active).
    /// OpenSpec: unify-pending-query-api — added patientId parameter.
    /// </summary>
    /// <param name="patientId">Patient ID filter (optional).</param>
    Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(Guid? patientId = null);

    /// <summary>
    /// Cross-case search with pagination.
    /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-015)
    /// </summary>
    /// <param name="patientName">Patient name filter (optional).</param>
    /// <param name="diagnosisKeyword">Diagnosis keyword filter (optional).</param>
    /// <param name="startDate">Start date filter (optional).</param>
    /// <param name="endDate">End date filter (optional).</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync(
        string? patientName = null,
        string? diagnosisKeyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Create a new medical case.
    /// Epic #1961: Uses unified MedicalCaseInputDto.
    /// </summary>
    /// <param name="request">Medical case input data.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseInputDto request);

    /// <summary>
    /// Delete a medical case (soft delete).
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    Task<ApiResponse> DeleteMedicalCaseAsync(Guid id);

    /// <summary>
    /// Set prescription flag.
    /// Task 3.4 (#1661): Auto-save on RadioBox change.
    /// </summary>
    /// <param name="medicalCaseId">Medical case ID.</param>
    /// <param name="request">Prescription flag request.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(
        Guid medicalCaseId,
        SetPrescriptionFlagRequest request);

    /// <summary>
    /// Close a medical case (mark as Completed).
    /// Epic #1676 Phase 4 Task 4.1
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id);

    /// <summary>
    /// Suspend a medical case.
    /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-010)
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="request">Consultation input data (optional).</param>
    Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync(
        Guid id,
        ConsultationInputDto? request = null);

    /// <summary>
    /// Cancel a medical case (soft delete + audit log).
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="request">Cancel request data (optional).</param>
    Task<ApiResponse> CancelMedicalCaseAsync(
        Guid id,
        CancelMedicalCaseRequestDto? request = null);

    /// <summary>
    /// Update medical case status.
    /// Issue #2243: Fix Suspend and Complete functionality.
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="request">Status update data.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(
        Guid id,
        MedicalCaseStatusInputDto request);

    /// <summary>
    /// Get current user's permissions for a medical case.
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    Task<ApiResponse<MedicalCasePermissionDto>> GetPermissionsAsync(Guid id);

    /// <summary>
    /// Get medical case audit logs with pagination.
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    Task<ApiResponse<MedicalCaseAuditLogPagedResultDto>> GetAuditLogsAsync(
        Guid id,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Aggregate save (diagnosis + prescription in one call).
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.5)
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="request">Unified input DTO with diagnosis and prescription data.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(
        Guid id,
        MedicalCaseInputDto request);

    /// <summary>
    /// Record print completion — write back print status.
    /// T2-X8-04~08
    /// </summary>
    /// <param name="medicalCaseId">Medical case ID.</param>
    /// <param name="request">Print completion data.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintCompletedAsync(
        Guid medicalCaseId,
        PrintCompletedRequest request);

    /// <summary>
    /// Add print log — record print success/failure.
    /// T4-S5-02
    /// </summary>
    /// <param name="medicalCaseId">Medical case ID.</param>
    /// <param name="request">Print log data.</param>
    Task<ApiResponse<object>> AddPrintLogAsync(
        Guid medicalCaseId,
        PrintLogInputDto request);

    /// <summary>
    /// Batch delete medical cases.
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    /// <summary>
    /// Batch get medical case details (solves N+1 query problem).
    /// OpenSpec: consolidate-medicalcase-detail-queries
    /// </summary>
    /// <param name="request">Batch query parameters (max 50 IDs).</param>
    Task<ApiResponse<List<MedicalCaseDetailDto>>> GetBatchDetailsAsync(BatchDetailQueryDto request);
}
