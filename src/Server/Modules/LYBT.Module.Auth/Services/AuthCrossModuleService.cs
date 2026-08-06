using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// 认证域跨模块服务实现 — 供 Users 等模块触发令牌撤销与安全审计。
/// </summary>
public class AuthCrossModuleService : IAuthCrossModuleService
{
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;

    public AuthCrossModuleService(
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService)
    {
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
    }

    public async Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
        => await _authSessionRepository.RevokeAllUserSessionsAsync(userId, reason, cancellationToken);

    public async Task RecordSecurityAuditAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
        => await _securityAuditService.RecordEventAsync(auditEvent, cancellationToken);
}
