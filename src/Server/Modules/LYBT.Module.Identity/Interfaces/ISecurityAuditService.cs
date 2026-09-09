using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Identity.Interfaces;

public interface ISecurityAuditService
{
    Task RecordEventAsync(SecurityAuditEvent auditEvent, CancellationToken ct = default);

    /// <summary>分页查询安全审计日志（US-SHELL-014 读操作）</summary>
    Task<Result<PagedResult<SecurityAuditLogDto>>> GetLogsAsync(
        SecurityAuditLogQueryDto query,
        CancellationToken ct = default);
}
