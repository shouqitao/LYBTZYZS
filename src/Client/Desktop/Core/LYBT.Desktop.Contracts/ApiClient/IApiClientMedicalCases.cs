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
/// 医案 API 子接口——CRUD、状态流转、处方。
/// 最大的 API 接口，含 19+ 个方法。
/// </summary>
/// <remarks>
/// <para>Combines methods from IMedicalCaseApi (remote) and ILocalMedicalCaseApi (local).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientMedicalCases
{
    /// <summary>
    /// 分页获取医案列表。
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
    /// 统一医案查询端点。
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
    /// 按 ID 获取医案详情。
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id);

    /// <summary>
    /// 获取未完成医案（Status=Draft/Active）。
    /// </summary>
    /// <param name="patientId">Patient ID filter (optional).</param>
    Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync(Guid? patientId = null);

    /// <summary>
    /// 跨医案分页搜索。
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
    /// 创建新医案。
    /// Epic #1961：使用统一 MedicalCaseInputDto。
    /// </summary>
    /// <param name="request">Medical case input data.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseInputDto request);

    /// <summary>
    /// 删除医案（软删除）。
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    Task<ApiResponse> DeleteMedicalCaseAsync(Guid id);

    /// <summary>
    /// 设置处方标志。
    /// Task 3.4 (#1661)：RadioBox 变化时自动保存。
    /// </summary>
    /// <param name="medicalCaseId">Medical case ID.</param>
    /// <param name="request">Prescription flag request.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(
        Guid medicalCaseId,
        SetPrescriptionFlagRequest request);

    /// <summary>
    /// 关闭医案（标记为 Completed）。
    /// Epic #1676 Phase 4 Task 4.1
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id);

    /// <summary>
    /// 挂起医案。
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="request">Consultation input data (optional).</param>
    Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync(
        Guid id,
        ConsultationInputDto? request = null);

    /// <summary>
    /// 取消医案（软删除 + 审计日志）。
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="request">Cancel request data (optional).</param>
    Task<ApiResponse> CancelMedicalCaseAsync(
        Guid id,
        CancelMedicalCaseRequest? request = null);

    /// <summary>
    /// 更新医案状态。
    /// Issue #2243：修复挂起与完成功能。
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="request">Status update data.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(
        Guid id,
        MedicalCaseStatusInputDto request);

    /// <summary>
    /// 聚合保存（一次调用保存诊断 + 处方）。
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="request">Unified input DTO with diagnosis and prescription data.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(
        Guid id,
        MedicalCaseInputDto request);

    /// <summary>
    /// 批量删除医案。
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    /// <summary>
    /// 获取当前用户的医案权限。
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    Task<ApiResponse<MedicalCasePermissionsDto>> GetPermissionsAsync(Guid id);

    /// <summary>
    /// 记录医案打印完成。
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="request">Print record request.</param>
    Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintAsync(
        Guid id,
        RecordPrintRequest request);

    /// <summary>
    /// 获取医案的审计日志（分页）。
    /// </summary>
    /// <param name="id">Medical case ID.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    Task<ApiResponse<PagedResult<AuditLogDto>>> GetAuditLogsAsync(
        Guid id,
        int page = 1,
        int pageSize = 20);

}
