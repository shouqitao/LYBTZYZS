using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Security.Services
{
    /// <summary>
    /// 基于数据库的JWT令牌存储服务 - UltraThink安全优化
    /// 实现令牌的持久化存储、撤销检查和会话管理
    /// </summary>
    public class DatabaseTokenStoreService : ITokenStoreService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DatabaseTokenStoreService> _logger;

        public DatabaseTokenStoreService(
            AppDbContext dbContext,
            ILogger<DatabaseTokenStoreService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 存储访问令牌
        /// </summary>
        public async Task<bool> StoreAccessTokenAsync(string tokenId, TokenStoreInfo tokenInfo)
        {
            try
            {
                var entity = new TokenStoreEntity
                {
                    TokenId = tokenId,
                    UserId = tokenInfo.UserId,
                    TokenHash = tokenInfo.TokenHash,
                    TokenType = "access_token",
                    ClientIP = tokenInfo.ClientIP,
                    SessionId = tokenInfo.SessionId,
                    DeviceId = tokenInfo.DeviceId,
                    UserAgent = tokenInfo.UserAgent,
                    ExpiresAt = tokenInfo.ExpiresAt,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.TokenStore.Add(entity);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("访问令牌已存储: {TokenId}, 用户: {UserId}", tokenId, tokenInfo.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "存储访问令牌失败: {TokenId}", tokenId);
                return false;
            }
        }

        /// <summary>
        /// 检查令牌是否已撤销
        /// </summary>
        public async Task<bool> IsTokenRevokedAsync(string tokenId)
        {
            try
            {
                var token = await _dbContext.TokenStore
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TokenId == tokenId);

                if (token == null)
                {
                    // 令牌不存在，可能是未存储或已过期清理
                    return false;
                }

                // 检查是否已撤销或过期
                var isRevoked = token.IsRevoked || token.ExpiresAt < DateTime.UtcNow;

                if (!isRevoked && token.LastUsedAt != DateTime.UtcNow.Date)
                {
                    // 更新最后使用时间和使用次数
                    await UpdateTokenUsageAsync(tokenId);
                }

                return isRevoked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查令牌撤销状态失败: {TokenId}", tokenId);
                return true; // 发生错误时保守处理，认为令牌无效
            }
        }

        /// <summary>
        /// 撤销指定令牌
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string tokenId, string reason = "用户注销")
        {
            try
            {
                var token = await _dbContext.TokenStore
                    .FirstOrDefaultAsync(t => t.TokenId == tokenId);

                if (token == null)
                {
                    _logger.LogWarning("尝试撤销不存在的令牌: {TokenId}", tokenId);
                    return false;
                }

                if (token.IsRevoked)
                {
                    _logger.LogWarning("令牌已被撤销: {TokenId}", tokenId);
                    return true;
                }

                token.IsRevoked = true;
                token.RevokeReason = reason;
                token.RevokedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("令牌已撤销: {TokenId}, 原因: {Reason}", tokenId, reason);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销令牌失败: {TokenId}", tokenId);
                return false;
            }
        }

        /// <summary>
        /// 撤销用户的所有令牌
        /// </summary>
        public async Task<int> RevokeAllUserTokensAsync(Guid userId, string reason = "安全操作")
        {
            try
            {
                var tokens = await _dbContext.TokenStore
                    .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                if (tokens.Count == 0)
                {
                    _logger.LogInformation("用户无有效令牌需要撤销: {UserId}", userId);
                    return 0;
                }

                var revokedCount = 0;
                var now = DateTime.UtcNow;

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokeReason = reason;
                    token.RevokedAt = now;
                    revokedCount++;
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("用户所有令牌已撤销: {UserId}, 数量: {Count}, 原因: {Reason}", 
                    userId, revokedCount, reason);

                return revokedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销用户所有令牌失败: {UserId}", userId);
                return -1;
            }
        }

        /// <summary>
        /// 存储刷新令牌
        /// </summary>
        public async Task<bool> StoreRefreshTokenAsync(string refreshToken, StoredRefreshToken tokenInfo)
        {
            try
            {
                var entity = new RefreshTokenStoreEntity
                {
                    RefreshToken = refreshToken,
                    AccessTokenId = tokenInfo.AccessTokenId ?? string.Empty,
                    UserId = tokenInfo.UserId,
                    Username = tokenInfo.Username,
                    Role = tokenInfo.Role,
                    ClientIP = tokenInfo.ClientIP,
                    SessionId = tokenInfo.SessionId,
                    DeviceId = tokenInfo.DeviceId,
                    IsLongTerm = tokenInfo.IsLongTerm,
                    ExpiresAt = tokenInfo.ExpiresAt,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.RefreshTokenStore.Add(entity);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("刷新令牌已存储: 用户 {UserId}", tokenInfo.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "存储刷新令牌失败: 用户 {UserId}", tokenInfo.UserId);
                return false;
            }
        }

        /// <summary>
        /// 获取存储的刷新令牌
        /// </summary>
        public async Task<StoredRefreshToken?> GetStoredRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var entity = await _dbContext.RefreshTokenStore
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);

                if (entity == null)
                {
                    return null;
                }

                // 检查是否过期、已使用或已撤销
                if (entity.ExpiresAt < DateTime.UtcNow || entity.IsUsed || entity.IsRevoked)
                {
                    return null;
                }

                return new StoredRefreshToken
                {
                    AccessTokenId = entity.AccessTokenId,
                    UserId = entity.UserId,
                    Username = entity.Username,
                    Role = entity.Role,
                    ClientIP = entity.ClientIP,
                    SessionId = entity.SessionId,
                    DeviceId = entity.DeviceId,
                    ExpiresAt = entity.ExpiresAt,
                    IsLongTerm = entity.IsLongTerm
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取刷新令牌失败");
                return null;
            }
        }

        /// <summary>
        /// 撤销刷新令牌
        /// </summary>
        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string reason = "令牌刷新")
        {
            try
            {
                var token = await _dbContext.RefreshTokenStore
                    .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);

                if (token == null)
                {
                    _logger.LogWarning("尝试撤销不存在的刷新令牌");
                    return false;
                }

                token.IsUsed = true;
                token.IsRevoked = true;
                token.RevokeReason = reason;
                token.RevokedAt = DateTime.UtcNow;
                token.UsedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("刷新令牌已撤销: 用户 {UserId}", token.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销刷新令牌失败");
                return false;
            }
        }

        /// <summary>
        /// 记录可疑活动
        /// </summary>
        public async Task LogSuspiciousActivityAsync(
            string activityType, 
            string? tokenId = null, 
            Guid? userId = null, 
            string? clientIP = null, 
            string? userAgent = null, 
            string? details = null)
        {
            try
            {
                var riskScore = CalculateRiskScore(activityType, details);
                var severity = DetermineSeverity(riskScore);

                var entity = new SuspiciousTokenActivityEntity
                {
                    ActivityType = activityType,
                    TokenId = tokenId,
                    UserId = userId,
                    ClientIP = clientIP,
                    UserAgent = userAgent,
                    Details = details,
                    Severity = severity,
                    RiskScore = riskScore,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.SuspiciousTokenActivity.Add(entity);
                await _dbContext.SaveChangesAsync();

                _logger.LogWarning("可疑令牌活动已记录: {ActivityType}, 风险评分: {RiskScore}, 严重程度: {Severity}", 
                    activityType, riskScore, severity);

                // 高风险活动立即通知
                if (riskScore >= 80)
                {
                    _logger.LogCritical("检测到高风险令牌活动: {ActivityType}, IP: {ClientIP}, 详情: {Details}", 
                        activityType, clientIP, details);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录可疑令牌活动失败");
            }
        }

        /// <summary>
        /// 清理过期令牌
        /// </summary>
        public async Task<int> CleanupExpiredTokensAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var cleanupDate = now.AddDays(-7); // 保留7天过期数据用于审计

                // 清理过期的访问令牌
                var expiredTokens = await _dbContext.TokenStore
                    .Where(t => t.ExpiresAt < cleanupDate)
                    .CountAsync();

                if (expiredTokens > 0)
                {
                    await _dbContext.TokenStore
                        .Where(t => t.ExpiresAt < cleanupDate)
                        .ExecuteDeleteAsync();
                }

                // 清理过期的刷新令牌
                var expiredRefreshTokens = await _dbContext.RefreshTokenStore
                    .Where(t => t.ExpiresAt < cleanupDate)
                    .CountAsync();

                if (expiredRefreshTokens > 0)
                {
                    await _dbContext.RefreshTokenStore
                        .Where(t => t.ExpiresAt < cleanupDate)
                        .ExecuteDeleteAsync();
                }

                var totalCleaned = expiredTokens + expiredRefreshTokens;

                if (totalCleaned > 0)
                {
                    _logger.LogInformation("已清理过期令牌: 访问令牌 {AccessTokens}, 刷新令牌 {RefreshTokens}", 
                        expiredTokens, expiredRefreshTokens);
                }

                return totalCleaned;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期令牌失败");
                return -1;
            }
        }

        /// <summary>
        /// 更新令牌使用记录
        /// </summary>
        private async Task UpdateTokenUsageAsync(string tokenId)
        {
            try
            {
                var token = await _dbContext.TokenStore
                    .FirstOrDefaultAsync(t => t.TokenId == tokenId);

                if (token != null)
                {
                    token.LastUsedAt = DateTime.UtcNow;
                    token.UsageCount++;
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新令牌使用记录失败: {TokenId}", tokenId);
            }
        }

        /// <summary>
        /// 计算风险评分
        /// </summary>
        private static int CalculateRiskScore(string activityType, string? details)
        {
            var baseScore = activityType switch
            {
                "IP不匹配" => 60,
                "签名无效" => 90,
                "令牌伪造" => 95,
                "暴力破解" => 85,
                "异常访问模式" => 70,
                "过期令牌使用" => 40,
                "重放攻击" => 80,
                _ => 50
            };

            // 根据详细信息调整评分
            if (!string.IsNullOrEmpty(details))
            {
                if (details.Contains("multiple", StringComparison.OrdinalIgnoreCase))
                    baseScore += 10;
                if (details.Contains("rapid", StringComparison.OrdinalIgnoreCase))
                    baseScore += 15;
                if (details.Contains("foreign", StringComparison.OrdinalIgnoreCase))
                    baseScore += 5;
            }

            return Math.Min(100, Math.Max(0, baseScore));
        }

        /// <summary>
        /// 确定严重程度
        /// </summary>
        private static string DetermineSeverity(int riskScore)
        {
            return riskScore switch
            {
                >= 90 => "Critical",
                >= 70 => "High",
                >= 50 => "Medium",
                _ => "Low"
            };
        }
    }

    /// <summary>
    /// 令牌存储信息
    /// </summary>
    public class TokenStoreInfo
    {
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public string ClientIP { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public string? DeviceId { get; set; }
        public string? UserAgent { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// 存储的刷新令牌（扩展版）
    /// </summary>
    public class StoredRefreshToken
    {
        public string? AccessTokenId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ClientIP { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public string? DeviceId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsLongTerm { get; set; }
    }
}