using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Identity.Interfaces;

public interface ISecurityAuditService
{
    Task RecordEventAsync(SecurityAuditEvent auditEvent, CancellationToken ct = default);
}
