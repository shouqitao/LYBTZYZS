using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCases.Infrastructure;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Infrastructure
{
    public class MedicalCaseReferenceRepository : BaseRepository<MedicalCase, MedicalCaseDbContext>, IMedicalCaseReferenceRepository
    {
        public MedicalCaseReferenceRepository(MedicalCaseDbContext dbContext, ILogger<MedicalCaseReferenceRepository> logger)
            : base(dbContext, logger)
        {
        }

        public async Task<int> CountUnfinishedAsync(Guid patientId, CancellationToken ct = default)
        {
            return await _context.MedicalCases
                .Where(mc => mc.PatientId == patientId && !mc.IsDeleted
                    && (mc.CaseStatus == MedicalCaseStatus.Active || mc.CaseStatus == MedicalCaseStatus.Suspended))
                .CountAsync(ct);
        }

        /// <summary>批量计数（P3 US-PAT-010）</summary>
    public async Task<Dictionary<Guid, int>> CountAllBatchAsync(IEnumerable<Guid> patientIds, CancellationToken ct = default)
    {
        var ids = patientIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, int>();

        return await _context.MedicalCases
            .AsNoTracking()
            .Where(m => !m.IsDeleted && ids.Contains(m.PatientId))
            .GroupBy(m => m.PatientId)
            .Select(g => new { PatientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PatientId, x => x.Count, ct);
    }

    public async Task<int> CountAllAsync(Guid patientId, CancellationToken ct = default)
        {
            return await _context.MedicalCases
                .Where(mc => mc.PatientId == patientId && !mc.IsDeleted)
                .CountAsync(ct);
        }

        public async Task<List<MedicalCaseReferenceDto>> GetRecentAsync(Guid patientId, int count, CancellationToken ct = default)
        {
            return await _context.MedicalCases
                .Where(mc => mc.PatientId == patientId && !mc.IsDeleted)
                .OrderByDescending(mc => mc.CreatedAt)
                .Take(count)
                .Select(mc => new MedicalCaseReferenceDto
                {
                    MedicalCaseId = mc.Id,
                    CaseNumber = mc.CaseNumber ?? string.Empty,
                    CreatedAt = mc.CreatedAt,
                    Status = mc.CaseStatus.ToString()
                })
                .ToListAsync(ct);
        }
    }
}


