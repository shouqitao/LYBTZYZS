using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Auth.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Repositories
{
    internal class SecurityAuditRepository : BaseRepository<SecurityAuditLog>, ISecurityAuditRepository
    {
        public SecurityAuditRepository(AppDbContext dbContext, ILogger<SecurityAuditRepository> logger)
            : base(dbContext, logger)
        {
        }

        public override async Task<SecurityAuditLog> AddAsync(SecurityAuditLog log, CancellationToken cancellationToken = default)
        {
            await _context.SecurityAuditLogs.AddAsync(log, cancellationToken);
            _logger.LogDebug("[REPO] SecurityAudit.Add - EventType={EventType}", log.EventType);
            return log;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
