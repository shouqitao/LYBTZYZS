using MediatR;
using LYBT.Module.Identity.Interfaces;
using LYBT.Module.Identity.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Identity.Application.Commands;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService,
        ILogger<LogoutCommandHandler> logger)
    {
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        LogoutCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;

        if (!string.IsNullOrEmpty(input.RefreshToken))
        {
            var tokenHash = TokenHashHelper.ComputeTokenHash(input.RefreshToken);
            var session = await _authSessionRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            if (session != null && session.IsValid())
            {
                session.Logout();
                await _authSessionRepository.UpdateAsync(session, cancellationToken);

                _logger.LogInformation("[Handler] Logout session revoked - SessionId={SessionId}", session.Id);
            }
        }

        await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
        {
            UserName = input.UserName,
            EventType = "Logout",
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation("[Handler] Logout completed - UserName={UserName}", input.UserName ?? "(unknown)");
        return Result<bool>.Success(true);
    }
}
