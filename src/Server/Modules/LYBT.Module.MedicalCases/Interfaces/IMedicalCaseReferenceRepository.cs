using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.MedicalCases.Interfaces
{
    public interface IMedicalCaseReferenceRepository
    {
        Task<int> CountUnfinishedAsync(Guid patientId, CancellationToken ct = default);
        Task<int> CountAllAsync(Guid patientId, CancellationToken ct = default);

        /// <summary>批量计数（P3 US-PAT-010: 消除逐患者 N+1）</summary>
        Task<Dictionary<Guid, int>> CountAllBatchAsync(IEnumerable<Guid> patientIds, CancellationToken ct = default);
        Task<List<MedicalCaseReferenceDto>> GetRecentAsync(Guid patientId, int count, CancellationToken ct = default);
    }
}


