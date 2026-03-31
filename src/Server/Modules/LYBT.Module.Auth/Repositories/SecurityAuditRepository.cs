using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Repositories
{
    internal class SecurityAuditRepository : ISecurityAuditRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<SecurityAuditRepository> _logger;

        public SecurityAuditRepository(AppDbContext dbContext, ILogger<SecurityAuditRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task AddAsync(SecurityAuditLog log, CancellationToken cancellationToken = default)
        {
            await _dbContext.SecurityAuditLogs.AddAsync(log, cancellationToken);
            _logger.LogDebug("[REPO] SecurityAudit.Add - EventType={EventType}", log.EventType);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
