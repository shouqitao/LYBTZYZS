using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 认证域跨模块服务 — 供 Users 等模块触发令牌撤销与安全审计。
/// </summary>
public interface IAuthCrossModuleService
{
    /// <summary>批量撤销用户全部有效会话（Token 族旋转）。</summary>
    Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>记录安全审计事件。</summary>
    Task RecordSecurityAuditAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
