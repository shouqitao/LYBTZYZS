using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;

namespace LYBT.Module.Auth.Infrastructure;

public class SecurityAuditRepository : ISecurityAuditRepository
{
    private readonly AppDbContext _context;

    public SecurityAuditRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(SecurityAuditLog log, CancellationToken ct = default)
    {
        await _context.SecurityAuditLogs.AddAsync(log, ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
