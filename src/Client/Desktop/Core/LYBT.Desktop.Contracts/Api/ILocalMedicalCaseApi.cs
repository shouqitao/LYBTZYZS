using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

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
}
