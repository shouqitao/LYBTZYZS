using LYBT.Entities.Auth;
using LYBT.Module.Auth.Interfaces;

namespace LYBT.Module.Auth.Infrastructure;

public class SecurityAuditRepository : ISecurityAuditRepository
{
    private readonly AuthDbContext _context;

    public SecurityAuditRepository(AuthDbContext context) => _context = context;

    public async Task AddAsync(SecurityAuditLog log, CancellationToken ct = default)
    {
        await _context.SecurityAuditLogs.AddAsync(log, ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
