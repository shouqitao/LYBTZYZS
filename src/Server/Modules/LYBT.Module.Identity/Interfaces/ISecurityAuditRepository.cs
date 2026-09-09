using LYBT.Entities.Auth;

namespace LYBT.Module.Identity.Interfaces;

public interface ISecurityAuditRepository
{
    Task AddAsync(SecurityAuditLog log, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>分页查询安全审计日志（CreatedAt 倒序；US-SHELL-014）</summary>
    Task<(List<SecurityAuditLog> Items, int TotalCount)> GetPagedAsync(
        string? eventType,
        string? userName,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
