using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.MedicalCases.Interfaces
{
    public interface IMedicalCaseReferenceRepository
    {
        Task<int> CountUnfinishedAsync(Guid patientId, CancellationToken ct = default);
        Task<int> CountAllAsync(Guid patientId, CancellationToken ct = default);
        Task<List<MedicalCaseReferenceDto>> GetRecentAsync(Guid patientId, int count, CancellationToken ct = default);
    }
}
