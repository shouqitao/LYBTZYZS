using LYBT.Entities.Auth;
using LYBT.Module.Identity.Interfaces;

namespace LYBT.Module.Identity.Infrastructure;

public class SecurityAuditRepository : ISecurityAuditRepository
{
    private readonly IdentityDbContext _context;

    public SecurityAuditRepository(IdentityDbContext context) => _context = context;

    public async Task AddAsync(SecurityAuditLog log, CancellationToken ct = default)
    {
        await _context.SecurityAuditLogs.AddAsync(log, ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
