using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Patient endpoints.
/// Used when ApiRouter switches to local/offline mode.
/// </summary>
public interface ILocalPatientApi
{
    [Refit.Get("/api/patients")]
    Task<List<PatientListDto>> GetPatientsAsync(
        [Refit.Query] string? keyword = null,
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 20);

    [Refit.Get("/api/patients/{id}")]
    Task<PatientDetailDto> GetPatientByIdAsync(Guid id);

    [Refit.Post("/api/patients")]
    Task<PatientDetailDto> CreatePatientAsync([Refit.Body] PatientInputDto request);

    [Refit.Put("/api/patients/{id}")]
    Task<PatientDetailDto> UpdatePatientAsync(Guid id, [Refit.Body] PatientInputDto request);

    [Refit.Delete("/api/patients/{id}")]
    Task DeletePatientAsync(Guid id);

    [Refit.Post("/api/patients/{id}/toggle-status")]
    Task<PatientDetailDto> ToggleStatusAsync(Guid id);

    [Refit.Post("/api/patients/{id}/restore")]
    Task<PatientDetailDto> RestoreAsync(Guid id);

    [Refit.Post("/api/patients/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

    [Refit.Get("/api/patients/export")]
    Task<List<PatientDetailDto>> ExportPatientsAsync([Refit.Query] string? keyword = null);

    [Refit.Get("/api/patients/import-template")]
    Task<object> ExportTemplateAsync();

    [Refit.Post("/api/patients/import")]
    Task<PatientBatchImportResultDto> BatchImportAsync([Refit.Body] PatientBatchImportInputDto request);
}
