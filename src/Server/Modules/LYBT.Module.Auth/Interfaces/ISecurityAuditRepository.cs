using LYBT.Entities.Auth;

namespace LYBT.Module.Auth.Interfaces;

public interface ISecurityAuditRepository
{
    Task AddAsync(SecurityAuditLog log, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
