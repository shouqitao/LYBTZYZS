using LYBT.Entities.Patients;
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
    Task<List<Patient>> GetPatientsAsync(
        [Refit.Query] string? keyword = null,
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 20);

    [Refit.Get("/api/patients/{id}")]
    Task<Patient> GetPatientByIdAsync(Guid id);

    [Refit.Post("/api/patients")]
    Task<Patient> CreatePatientAsync([Refit.Body] PatientInputDto request);

    [Refit.Put("/api/patients/{id}")]
    Task<Patient> UpdatePatientAsync(Guid id, [Refit.Body] PatientInputDto request);

    [Refit.Delete("/api/patients/{id}")]
    Task DeletePatientAsync(Guid id);
}
