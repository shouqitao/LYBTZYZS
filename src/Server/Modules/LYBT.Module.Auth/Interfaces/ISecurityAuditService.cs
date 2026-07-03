using LYBT.Module.Auth.Models;

namespace LYBT.Module.Auth.Interfaces;

public interface ISecurityAuditService
{
    Task RecordEventAsync(SecurityAuditEvent auditEvent, CancellationToken ct = default);
}
