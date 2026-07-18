using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for MedicalCase endpoints.
/// </summary>
public interface ILocalMedicalCaseApi
{
    [Refit.Get("/api/v1/medicalcases")]
    Task<List<MedicalCaseListDto>> GetMedicalCasesAsync([Refit.Query] Guid? patientId = null);

    [Refit.Get("/api/v1/medicalcases/{id}")]
    Task<MedicalCaseDetailDto> GetMedicalCaseByIdAsync(Guid id);

    [Refit.Post("/api/v1/medicalcases")]
    Task<MedicalCaseDetailDto> CreateMedicalCaseAsync([Refit.Body] MedicalCaseInputDto request);

    [Refit.Delete("/api/v1/medicalcases/{id}")]
    Task DeleteMedicalCaseAsync(Guid id);

    /// <summary>
    /// Aggregate save (diagnosis + prescription in one call).
    /// </summary>
    [Refit.Put("/api/v1/medicalcases/{id}")]
    Task<MedicalCaseDetailDto> SaveAsync(Guid id, [Refit.Body] MedicalCaseInputDto request);

    [Refit.Get("/api/v1/medicalcases/search")]
    Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync(
        [Refit.Query] string? patientName = null,
        [Refit.Query] string? diagnosisKeyword = null,
        [Refit.Query] DateTime? startDate = null,
        [Refit.Query] DateTime? endDate = null,
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 20);

    [Refit.Get("/api/v1/medicalcases/query")]
    Task<PagedResult<MedicalCaseListDto>> QueryMedicalCasesAsync(
        [Refit.Query] MedicalCaseQueryType queryType = MedicalCaseQueryType.All,
        [Refit.Query] Guid? patientId = null,
        [Refit.Query] Guid? doctorId = null,
        [Refit.Query] string? keyword = null,
        [Refit.Query] int pageIndex = 1,
        [Refit.Query] int pageSize = 20,
        [Refit.Query] bool includeAllDoctors = false,
        [Refit.Query] int? limit = null);

    [Refit.Post("/api/v1/medicalcases/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

    [Refit.Put("/api/v1/medicalcases/{id}/close")]
    Task<MedicalCaseDetailDto> CloseCaseAsync(Guid id);

    [Refit.Put("/api/v1/medicalcases/{id}/suspend")]
    Task<MedicalCaseDetailDto> SuspendAsync(Guid id, [Refit.Body] ConsultationInputDto? request = null);

    [Refit.Put("/api/v1/medicalcases/{id}/cancel")]
    Task CancelMedicalCaseAsync(Guid id, [Refit.Body] CancelMedicalCaseRequestDto? request = null);

    [Refit.Put("/api/v1/medicalcases/{id}/status")]
    Task<MedicalCaseDetailDto> UpdateStatusAsync(Guid id, [Refit.Body] MedicalCaseStatusInputDto request);

    [Refit.Put("/api/v1/medicalcases/{id}/prescription-flag")]
    Task<MedicalCaseDetailDto> SetPrescriptionFlagAsync(Guid id, [Refit.Body] SetPrescriptionFlagRequest request);

    [Refit.Get("/api/v1/medicalcases/pending")]
    Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync([Refit.Query] Guid? patientId = null);

    /// <summary>
    /// 获取医案审计日志（分页）
    /// </summary>
    [Refit.Get("/api/v1/medicalcases/{id}/audit-logs")]
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        Guid id,
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 20);

    /// <summary>
    /// 获取医案操作权限
    /// </summary>
    [Refit.Get("/api/v1/medicalcases/{id}/permissions")]
    Task<MedicalCasePermissionsDto> GetPermissionsAsync(Guid id);

    /// <summary>
    /// 记录打印完成
    /// </summary>
    [Refit.Put("/api/v1/medicalcases/{id}/print-completed")]
    Task<MedicalCaseDetailDto> RecordPrintAsync(
        Guid id,
        [Refit.Body] RecordPrintRequest request);
}
