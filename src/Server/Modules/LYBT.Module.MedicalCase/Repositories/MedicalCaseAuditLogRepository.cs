using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCases.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Repositories
{
    internal class MedicalCaseAuditLogRepository : BaseRepository<MedicalCaseAuditLog>, IMedicalCaseAuditLogRepository
    {
        public MedicalCaseAuditLogRepository(AppDbContext dbContext, ILogger<MedicalCaseAuditLogRepository> logger)
            : base(dbContext, logger)
        {
        }

        public override async Task<MedicalCaseAuditLog> AddAsync(MedicalCaseAuditLog log, CancellationToken cancellationToken = default)
        {
            await _context.MedicalCaseAuditLogs.AddAsync(log, cancellationToken);
            _logger.LogDebug("[REPO] MedicalCaseAuditLog.Add - MedicalCaseId={MedicalCaseId} OperationType={OperationType}",
                log.MedicalCaseId, log.OperationType);
            return log;
        }

        public async Task<List<MedicalCaseAuditLog>> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        {
            return await _context.MedicalCaseAuditLogs
                .Where(l => l.MedicalCaseId == medicalCaseId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<MedicalCaseAuditLog> Logs, int TotalCount)> GetPagedByMedicalCaseIdAsync(
            Guid medicalCaseId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.MedicalCaseAuditLogs
                .Where(l => l.MedicalCaseId == medicalCaseId)
                .OrderByDescending(l => l.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (logs, totalCount);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
