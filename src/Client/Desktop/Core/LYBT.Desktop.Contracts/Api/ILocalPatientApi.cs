using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Patient endpoints.
/// Used when ApiRouter switches to local/offline mode.
/// </summary>
public interface ILocalPatientApi
{
    [Refit.Get("/api/v1/patients")]
    Task<List<PatientListDto>> GetPatientsAsync(
        [Refit.Query] string? keyword = null,
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 20);

    [Refit.Get("/api/v1/patients/{id}")]
    Task<PatientDetailDto> GetPatientByIdAsync(Guid id);

    [Refit.Post("/api/v1/patients")]
    Task<PatientDetailDto> CreatePatientAsync([Refit.Body] PatientInputDto request);

    [Refit.Put("/api/v1/patients/{id}")]
    Task<PatientDetailDto> UpdatePatientAsync(Guid id, [Refit.Body] PatientInputDto request);

    [Refit.Delete("/api/v1/patients/{id}")]
    Task DeletePatientAsync(Guid id);

    [Refit.Post("/api/v1/patients/{id}/toggle-status")]
    Task<PatientDetailDto> ToggleStatusAsync(Guid id);

    [Refit.Post("/api/v1/patients/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

    [Refit.Get("/api/v1/patients/export")]
    Task<List<PatientDetailDto>> ExportPatientsAsync([Refit.Query] string? keyword = null);

    [Refit.Get("/api/v1/patients/import-template")]
    Task<object> ExportTemplateAsync();

    [Refit.Post("/api/v1/patients/import")]
    Task<PatientBatchImportResultDto> BatchImportAsync([Refit.Body] PatientBatchImportInputDto request);
}
