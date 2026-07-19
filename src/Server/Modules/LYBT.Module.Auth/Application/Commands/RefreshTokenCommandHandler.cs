using System.Security.Cryptography;
using System.Text;
using MediatR;
using LYBT.Module.Auth.Domain;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Application.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IJwtService _jwtService;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IJwtService jwtService,
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _jwtService = jwtService;
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(
        RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var oldTokenHash = ComputeTokenHash(request.Token);
        var oldSession = await _authSessionRepository.GetByTokenHashAsync(oldTokenHash, cancellationToken);

        if (oldSession != null && !oldSession.IsValid())
        {
            if (oldSession.IsRevoked)
            {
                _logger.LogWarning("[Handler] Token refresh rejected - replay detected (revoked) SessionId={SessionId}", oldSession.Id);
                await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
                {
                    EventType = "TokenRefresh",
                    IsSuccess = false,
                    FailureReason = "Replay detected: token already revoked"
                }, cancellationToken);
                return Result<LoginResponse>.Failure(ErrorCode.AuthTokenRevoked, "令牌已被撤销，请重新登录");
            }

            if (oldSession.LogoutTime.HasValue)
            {
                _logger.LogWarning("[Handler] Token refresh rejected - replay detected (logged out) SessionId={SessionId}", oldSession.Id);
                await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
                {
                    EventType = "TokenRefresh",
                    IsSuccess = false,
                    FailureReason = "Replay detected: session already logged out"
                }, cancellationToken);
                return Result<LoginResponse>.Failure(ErrorCode.AuthTokenRevoked, "会话已结束，请重新登录");
            }
        }

        var result = _jwtService.RefreshToken(request.Token);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("[Handler] Token refresh failed - {Error}", result.ErrorMessage);
            await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
            {
                EventType = "TokenRefresh",
                IsSuccess = false,
                FailureReason = result.ErrorMessage
            }, cancellationToken);
            return Result<LoginResponse>.Failure(
                result.ModuleErrorCode ?? ErrorCode.AuthInvalidCredentials,
                result.ErrorMessage ?? "令牌刷新失败");
        }

        if (oldSession != null)
        {
            oldSession.Logout();
            await _authSessionRepository.UpdateAsync(oldSession, cancellationToken);
            _logger.LogInformation("[Handler] Old session logged out - SessionId={SessionId}", oldSession.Id);
        }

        var newToken = result.Data!.Token;
        var newTokenHash = ComputeTokenHash(newToken);
        var newSession = AuthSession.Create(
            oldSession?.UserId ?? Guid.Empty,
            newTokenHash,
            result.Data.ExpiresAt,
            oldSession?.IpAddress ?? "unknown",
            oldSession?.UserAgent);
        await _authSessionRepository.AddAsync(newSession, cancellationToken);

        await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
        {
            EventType = "TokenRefresh",
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation("[Handler] Token refreshed successfully - OldSessionId={OldSessionId} NewSessionId={NewSessionId}",
            oldSession?.Id, newSession.Id);
        return Result<LoginResponse>.Success(result.Data);
    }

    private static string ComputeTokenHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
