using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.MedicalCases.Interfaces
{
    public interface IMedicalCaseReferenceService
    {
        Task<int> CountUnfinishedMedicalCasesAsync(Guid patientId);
        Task<int> CountMedicalCasesAsync(Guid patientId);
        Task<List<MedicalCaseReferenceDto>> GetRecentMedicalCasesAsync(Guid patientId, int count);
    }
}
