using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Application.Queries;

public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, Result<ValidateTokenResult>>
{
    private readonly IJwtService _jwtService;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ILogger<ValidateTokenQueryHandler> _logger;

    public ValidateTokenQueryHandler(
        IJwtService jwtService,
        IAuthSessionRepository authSessionRepository,
        ILogger<ValidateTokenQueryHandler> logger)
    {
        _jwtService = jwtService;
        _authSessionRepository = authSessionRepository;
        _logger = logger;
    }

    public async Task<Result<ValidateTokenResult>> Handle(
        ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        var principal = _jwtService.ValidateToken(request.Token);
        if (principal == null)
        {
            _logger.LogDebug("[Handler] Token validation - Valid=False (JWT invalid)");
            return Result<ValidateTokenResult>.Success(new ValidateTokenResult(false, null, null, null));
        }

        var tokenHash = ComputeTokenHash(request.Token);
        var session = await _authSessionRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (session == null)
        {
            _logger.LogDebug("[Handler] Token validation - Valid=False (session not found)");
            return Result<ValidateTokenResult>.Failure(ErrorCode.AuthTokenRevoked, "登录会话不存在");
        }

        if (session.IsRevoked)
        {
            _logger.LogWarning("[Handler] Token validation - Valid=False (session revoked) SessionId={SessionId}", session.Id);
            return Result<ValidateTokenResult>.Failure(ErrorCode.AuthTokenRevoked, "登录已失效，请重新登录");
        }

        if (session.IsExpired())
        {
            _logger.LogDebug("[Handler] Token validation - Valid=False (session expired) SessionId={SessionId}", session.Id);
            return Result<ValidateTokenResult>.Failure(ErrorCode.AuthAccessTokenExpired, "访问令牌已过期，请重新登录");
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = principal.FindFirst(ClaimTypes.Name)?.Value;
        var role = principal.FindFirst(ClaimTypes.Role)?.Value;

        _logger.LogDebug("[Handler] Token validation - Valid=True SessionId={SessionId}", session.Id);
        return Result<ValidateTokenResult>.Success(new ValidateTokenResult(true, userId, userName, role));
    }

    private static string ComputeTokenHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}


