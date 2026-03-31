using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCases.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Repositories
{
    internal class MedicalCaseAuditLogRepository : IMedicalCaseAuditLogRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<MedicalCaseAuditLogRepository> _logger;

        public MedicalCaseAuditLogRepository(AppDbContext dbContext, ILogger<MedicalCaseAuditLogRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task AddAsync(MedicalCaseAuditLog log, CancellationToken cancellationToken = default)
        {
            await _dbContext.MedicalCaseAuditLogs.AddAsync(log, cancellationToken);
            _logger.LogDebug("[REPO] MedicalCaseAuditLog.Add - MedicalCaseId={MedicalCaseId} OperationType={OperationType}",
                log.MedicalCaseId, log.OperationType);
        }

        public async Task<List<MedicalCaseAuditLog>> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.MedicalCaseAuditLogs
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
            var query = _dbContext.MedicalCaseAuditLogs
                .Where(l => l.MedicalCaseId == medicalCaseId)
                .OrderByDescending(l => l.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (logs, totalCount);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
