using System.Text.Json;
using LYBT.Entities.Auth;
using LYBT.Module.Auth.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// Token撤销服务实现
/// </summary>
public class TokenRevocationService : ITokenRevocationService
{
    private readonly IRefreshTokenRepository _tokenRepository;
    private readonly ISecurityAuditRepository _auditRepository;
    private readonly ILogger<TokenRevocationService> _logger;

    public TokenRevocationService(
        IRefreshTokenRepository tokenRepository,
        ISecurityAuditRepository auditRepository,
        ILogger<TokenRevocationService> logger)
    {
        _tokenRepository = tokenRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    /// <summary>
    /// 撤销单个RefreshToken
    /// </summary>
    public async Task<bool> RevokeTokenAsync(string token, string reason)
    {
        // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
        // GetByTokenAsync返回不管是否已撤销；保留原始行为：已撤销则返回false
        var tokenRecord = await _tokenRepository.GetByTokenAsync(token);

        if (tokenRecord == null || tokenRecord.IsRevoked)
        {
            _logger.LogWarning("[SVC] Token.Revoke → NotFound - Token={Token}", token);
            return false;
        }

        // 标记Token为已撤销
        tokenRecord.IsRevoked = true;
        tokenRecord.RevokedAt = DateTime.UtcNow;
        tokenRecord.RevokedReason = reason;

        await _tokenRepository.SaveChangesAsync();

        // 记录审计日志（失败不影响主操作）
        try
        {
            await _auditRepository.AddAsync(new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                EventType = "TokenRevoked",
                UserId = tokenRecord.UserId,
                UserType = tokenRecord.UserType,
                Success = true,
                Metadata = JsonSerializer.Serialize(new
                {
                    reason,
                    tokenId = tokenRecord.Id,
                    revokedAt = tokenRecord.RevokedAt
                }),
                CreatedAt = DateTime.UtcNow
            });
            await _auditRepository.SaveChangesAsync();
        }
        catch (Exception auditEx)
        {
            _logger.LogWarning(auditEx, "[SVC] Token.Revoke → AuditFailed - TokenId={TokenId}", tokenRecord.Id);
            // 审计日志失败不影响主操作
        }

        _logger.LogInformation("[SVC] Token.Revoke completed - UserId={UserId} UserType={UserType} Reason={Reason}",
            tokenRecord.UserId, tokenRecord.UserType, reason);

        return true;
    }

    /// <summary>
    /// 查询Token是否已撤销
    /// </summary>
    public async Task<bool> IsTokenRevokedAsync(string token)
    {
        // eliminate-service-catch-return: 移除catch-return-false，异常由IExceptionHandler统一处理
        // 安全考量：查询异常时不应默认返回false(未撤销)，应让调用方决定如何处理
        var tokenRecord = await _tokenRepository.GetByTokenAsync(token);
        return tokenRecord?.IsRevoked ?? false;
    }
}
