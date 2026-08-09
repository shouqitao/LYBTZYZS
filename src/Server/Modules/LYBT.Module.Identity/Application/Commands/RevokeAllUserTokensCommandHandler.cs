using MediatR;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 撤销用户全部令牌命令处理器（Token 族旋转）。
/// </summary>
public class RevokeAllUserTokensCommandHandler : IRequestHandler<RevokeAllUserTokensCommand, Result<bool>>
{
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly ILogger<RevokeAllUserTokensCommandHandler> _logger;

    public RevokeAllUserTokensCommandHandler(
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService,
        ILogger<RevokeAllUserTokensCommandHandler> logger)
    {
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        RevokeAllUserTokensCommand request, CancellationToken cancellationToken)
    {
        await _authSessionRepository.RevokeAllUserSessionsAsync(request.UserId, request.Reason, cancellationToken);

        await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
        {
            UserId = request.UserId,
            EventType = "AllTokensRevoked",
            Details = request.Reason,
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation("[Handler] All tokens revoked - UserId={UserId} Reason={Reason}", request.UserId, request.Reason);

        return Result<bool>.Success(true);
    }
}
