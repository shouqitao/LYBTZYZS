using LYBT.Entities.Auth;
using LYBT.Module.Identity.Interfaces;
using Microsoft.EntityFrameworkCore;

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

    public async Task<(List<SecurityAuditLog> Items, int TotalCount)> GetPagedAsync(
        string? eventType,
        string? userName,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _context.SecurityAuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(x => x.EventType == eventType);

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var keyword = userName.Trim();
            query = query.Where(x => x.UserName != null && x.UserName.Contains(keyword));
        }

        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.CreatedAt <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
