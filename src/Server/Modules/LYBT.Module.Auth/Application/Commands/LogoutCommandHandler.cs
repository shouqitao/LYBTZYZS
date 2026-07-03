using System.Security.Cryptography;
using System.Text;
using MediatR;
using LYBT.Module.Auth.Domain.Events;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Application.Commands;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
{
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IPublisher _publisher;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService,
        IPublisher publisher,
        ILogger<LogoutCommandHandler> logger)
    {
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        LogoutCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;

        if (!string.IsNullOrEmpty(input.RefreshToken))
        {
            var tokenHash = ComputeTokenHash(input.RefreshToken);
            var session = await _authSessionRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            if (session != null && session.IsValid())
            {
                session.Logout();
                await _authSessionRepository.UpdateAsync(session, cancellationToken);

                await _publisher.Publish(
                    new SessionRevokedEvent(session.Id, session.UserId),
                    cancellationToken);

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

    private static string ComputeTokenHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
