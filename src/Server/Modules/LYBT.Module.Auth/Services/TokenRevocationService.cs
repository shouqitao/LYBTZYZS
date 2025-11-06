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
        try
        {
            // 查找未撤销的Token
            var tokenRecord = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsRevoked);

            if (tokenRecord == null)
            {
                _logger.LogWarning("Token not found or already revoked: {Token}", token);
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
                _logger.LogError(auditEx, "Failed to log audit event for token revocation");
                // 审计日志失败不影响主操作
            }

            _logger.LogInformation(
                "Token revoked successfully. UserId: {UserId}, UserType: {UserType}, Reason: {Reason}",
                tokenRecord.UserId, tokenRecord.UserType, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }

    /// <summary>
    /// 批量撤销用户所有未撤销的RefreshToken
    /// </summary>
    public async Task<int> RevokeAllUserTokensAsync(Guid userId, string userType, string reason)
    {
        try
        {
            // 查找用户所有未撤销的Token
            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.UserType == userType && !t.IsRevoked)
                .ToListAsync();

            if (!tokens.Any())
            {
                _logger.LogInformation(
                    "No active tokens found for user. UserId: {UserId}, UserType: {UserType}",
                    userId, userType);
                return 0;
            }

            var revokedCount = 0;
            var now = DateTime.UtcNow;

            // 批量撤销
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = now;
                token.RevokedReason = reason;
                revokedCount++;
            }

            await _context.SaveChangesAsync();

            // 记录审计日志（失败不影响主操作）
            try
            {
                await LogAuditEventAsync(new SecurityAuditLog
                {
                    Id = Guid.NewGuid(),
                    EventType = "TokenRevoked",
                    UserId = userId,
                    UserType = userType,
                    Success = true,
                    Metadata = JsonSerializer.Serialize(new
                    {
                        reason,
                        revokedCount,
                        batchOperation = true,
                        revokedAt = now
                    }),
                    CreatedAt = now
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx, "Failed to log audit event for batch token revocation");
                // 审计日志失败不影响主操作
            }

            _logger.LogInformation(
                "Revoked {Count} tokens for user. UserId: {UserId}, UserType: {UserType}, Reason: {Reason}",
                revokedCount, userId, userType, reason);

            return revokedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all user tokens. UserId: {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// 查询Token是否已撤销
    /// </summary>
    public async Task<bool> IsTokenRevokedAsync(string token)
    {
        try
        {
            // 利用覆盖索引 IX_RefreshTokens_IsRevoked_Token 优化查询
            var tokenRecord = await _context.RefreshTokens
                .Where(t => t.Token == token)
                .Select(t => new { t.IsRevoked })
                .FirstOrDefaultAsync();

            return tokenRecord?.IsRevoked ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token revocation status");
            return false;
        }
    }

    /// <summary>
    /// 记录安全审计日志
    /// </summary>
    private async Task LogAuditEventAsync(SecurityAuditLog auditLog)
    {
        await _context.SecurityAuditLogs.AddAsync(auditLog);
    }
}
