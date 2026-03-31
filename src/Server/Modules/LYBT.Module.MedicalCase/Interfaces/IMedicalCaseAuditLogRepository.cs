using LYBT.Entities.MedicalCases;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 医案审计日志 Repository 接口
    /// </summary>
    public interface IMedicalCaseAuditLogRepository
    {
        Task AddAsync(MedicalCaseAuditLog log, CancellationToken cancellationToken = default);

        Task<List<MedicalCaseAuditLog>> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default);

        Task<(List<MedicalCaseAuditLog> Logs, int TotalCount)> GetPagedByMedicalCaseIdAsync(
            Guid medicalCaseId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
