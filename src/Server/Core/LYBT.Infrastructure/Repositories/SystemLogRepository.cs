using LYBT.Entities.Common;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Repositories;

/// <summary>
/// 系统日志仓储实现 — 只读查询
/// </summary>
public class SystemLogRepository : ISystemLogRepository
{
    private readonly AppDbContext _context;

    public SystemLogRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<SystemLog>> GetRecentLogsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.SystemLogs
            .AsNoTracking()
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
