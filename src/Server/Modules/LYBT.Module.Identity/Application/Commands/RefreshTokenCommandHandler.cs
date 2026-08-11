using System.Security.Cryptography;
using System.Text;
using MediatR;
using LYBT.Entities.Auth;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Identity.Application.Commands;

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
                // P3 (US-AUTH-006): 重放检测 → 撤销该用户全部会话（防重放持续利用）
                await _authSessionRepository.RevokeAllUserSessionsAsync(
                    oldSession.UserId, "重放检测：令牌已撤销，撤销全部会话", cancellationToken);
                await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
                {
                    UserId = oldSession.UserId,
                    EventType = "TokenRefresh",
                    IsSuccess = false,
                    FailureReason = "Replay detected: token already revoked - all sessions revoked"
                }, cancellationToken);
                return Result<LoginResponse>.Failure(ErrorCode.AuthTokenRevoked, ErrorMessages.Get(ErrorCode.AuthTokenRevoked));
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

        if (oldSession == null)
        {
            _logger.LogWarning("[Handler] Token refresh failed - session not found for token hash");
            await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
            {
                EventType = "TokenRefresh",
                IsSuccess = false,
                FailureReason = "Session not found"
            }, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorCode.AuthTokenInvalid, "会话不存在，请重新登录");
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
            oldSession!.UserId,
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
