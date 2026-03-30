using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.MedicalCases.Interfaces
{
    public interface IMedicalCaseReferenceService
    {
        Task<int> CountUnfinishedMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<int> CountMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<List<MedicalCaseReferenceDto>> GetRecentMedicalCasesAsync(Guid patientId, int count, CancellationToken cancellationToken = default);
    }
}
