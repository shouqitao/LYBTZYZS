using System.Text.Json;
using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services;

/// <summary>
/// Token撤销服务实现
/// </summary>
public class TokenRevocationService : ITokenRevocationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TokenRevocationService> _logger;

    public TokenRevocationService(
        AppDbContext context,
        ILogger<TokenRevocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 撤销单个RefreshToken
    /// </summary>
    public async Task<bool> RevokeTokenAsync(string token, string reason)
    {
        // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
        // 查找未撤销的Token
        var tokenRecord = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token && !t.IsRevoked);

        if (tokenRecord == null)
        {
            _logger.LogWarning("[SVC] Token.Revoke → NotFound - Token={Token}", token);
            return false;
        }

        // 标记Token为已撤销
        tokenRecord.IsRevoked = true;
        tokenRecord.RevokedAt = DateTime.UtcNow;
        tokenRecord.RevokedReason = reason;

        await _context.SaveChangesAsync();

        // 记录审计日志（失败不影响主操作）
        try
        {
            await LogAuditEventAsync(new SecurityAuditLog
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

            await _context.SaveChangesAsync();
        }
        catch (Exception auditEx)
        {
            _logger.LogError(auditEx, "[SVC] Token.Revoke → AuditFailed - TokenId={TokenId}", tokenRecord.Id);
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
        // 利用覆盖索引 IX_RefreshTokens_IsRevoked_Token 优化查询
        var tokenRecord = await _context.RefreshTokens
            .Where(t => t.Token == token)
            .Select(t => new { t.IsRevoked })
            .FirstOrDefaultAsync();

        return tokenRecord?.IsRevoked ?? false;
    }

    /// <summary>
    /// 记录安全审计日志
    /// </summary>
    private async Task LogAuditEventAsync(SecurityAuditLog auditLog)
    {
        await _context.SecurityAuditLogs.AddAsync(auditLog);
    }
}
