using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for MedicalCase endpoints.
/// </summary>
public interface ILocalMedicalCaseApi
{
    [Refit.Get("/api/medicalcases")]
    Task<List<MedicalCaseListDto>> GetMedicalCasesAsync([Refit.Query] Guid? patientId = null);

    [Refit.Get("/api/medicalcases/{id}")]
    Task<MedicalCaseDetailDto> GetMedicalCaseByIdAsync(Guid id);

    [Refit.Post("/api/medicalcases")]
    Task<MedicalCaseDetailDto> CreateMedicalCaseAsync([Refit.Body] MedicalCaseInputDto request);

    [Refit.Put("/api/medicalcases/{id}")]
    Task<MedicalCaseDetailDto> UpdateMedicalCaseAsync(Guid id, [Refit.Body] MedicalCaseInputDto request);

    [Refit.Delete("/api/medicalcases/{id}")]
    Task DeleteMedicalCaseAsync(Guid id);

    /// <summary>
    /// Aggregate save (diagnosis + prescription in one call).
    /// </summary>
    [Refit.Put("/api/medicalcases/{id}")]
    Task<MedicalCaseDetailDto> SaveAsync(Guid id, [Refit.Body] MedicalCaseInputDto request);

    [Refit.Get("/api/medicalcases/search")]
    Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(
        [Refit.Query] string? patientName = null,
        [Refit.Query] string? diagnosisKeyword = null,
        [Refit.Query] DateTime? startDate = null,
        [Refit.Query] DateTime? endDate = null,
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 20);

    [Refit.Get("/api/medicalcases/query")]
    Task<PagedResult<MedicalCaseListDto>> QueryAsync(
        [Refit.Query] MedicalCaseQueryType queryType = MedicalCaseQueryType.All,
        [Refit.Query] Guid? patientId = null,
        [Refit.Query] Guid? doctorId = null,
        [Refit.Query] string? keyword = null,
        [Refit.Query] int pageIndex = 1,
        [Refit.Query] int pageSize = 20,
        [Refit.Query] bool includeAllDoctors = false,
        [Refit.Query] int? limit = null);

    [Refit.Post("/api/medicalcases/batch-details")]
    Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync([Refit.Body] List<Guid> ids);

    [Refit.Post("/api/medicalcases/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] List<Guid> ids);

    [Refit.Get("/api/medicalcases/{id}/permissions")]
    Task<MedicalCasePermissionDto> GetPermissionsAsync(Guid id);

    [Refit.Put("/api/medicalcases/{id}/close")]
    Task<MedicalCaseDetailDto> CloseCaseAsync(Guid id);

    [Refit.Put("/api/medicalcases/{id}/suspend")]
    Task<MedicalCaseDetailDto> SuspendCaseAsync(Guid id);

    [Refit.Put("/api/medicalcases/{id}/cancel")]
    Task CancelCaseAsync(Guid id);

    [Refit.Put("/api/medicalcases/{id}/status")]
    Task<MedicalCaseDetailDto> UpdateStatusAsync(Guid id, [Refit.Body] MedicalCaseStatusInputDto request);

    [Refit.Put("/api/medicalcases/{id}/prescription-flag")]
    Task<MedicalCaseDetailDto> SetPrescriptionFlagAsync(Guid id, [Refit.Body] SetPrescriptionFlagRequest request);

    [Refit.Put("/api/medicalcases/{id}/print-completed")]
    Task<MedicalCaseDetailDto> RecordPrintCompletedAsync(Guid id, [Refit.Body] PrintCompletedRequest request);
}
