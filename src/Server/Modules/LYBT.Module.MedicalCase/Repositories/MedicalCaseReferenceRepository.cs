using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Repositories
{
    internal class MedicalCaseReferenceRepository : BaseRepository<MedicalCase>, IMedicalCaseReferenceRepository
    {
        public MedicalCaseReferenceRepository(AppDbContext dbContext, ILogger<MedicalCaseReferenceRepository> logger)
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
