using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LYBT.Infrastructure.Caching.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// Token黑名单服务实现
    /// 基于缓存系统实现Token撤销和黑名单管理
    /// </summary>
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<TokenBlacklistService> _logger;

        // 缓存键前缀
        private const string BLACKLIST_PREFIX = "jwt:blacklist:";
        private const string USER_TOKENS_PREFIX = "jwt:user_tokens:";
        private const string STATISTICS_KEY = "jwt:blacklist:statistics";

        public TokenBlacklistService(
            ICacheService cacheService,
            ILogger<TokenBlacklistService> logger)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 将Token加入黑名单
        /// </summary>
        public async Task<bool> BlacklistTokenAsync(string jwtId, DateTime expirationTime, string reason = "Manual revocation")
        {
            try
            {
                if (string.IsNullOrEmpty(jwtId))
                {
                    _logger.LogWarning("尝试将空的JWT ID加入黑名单");
                    return false;
                }

                var blacklistEntry = new BlacklistEntry
                {
                    JwtId = jwtId,
                    RevokedAt = DateTime.UtcNow,
                    ExpiresAt = expirationTime,
                    Reason = reason
                };

                var cacheKey = BLACKLIST_PREFIX + jwtId;
                var cacheExpiration = expirationTime - DateTime.UtcNow;

                // 如果Token已经过期，不需要加入黑名单
                if (cacheExpiration <= TimeSpan.Zero)
                {
                    _logger.LogInformation("Token {JwtId} 已过期，无需加入黑名单", jwtId);
                    return true;
                }

                await _cacheService.SetAsync(cacheKey, blacklistEntry, cacheExpiration);

                _logger.LogInformation(
                    "Token {JwtId} 已加入黑名单，原因: {Reason}, 过期时间: {ExpiresAt}",
                    jwtId, reason, expirationTime);

                // 更新统计信息
                await UpdateStatisticsAsync(1);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "将Token {JwtId} 加入黑名单时发生错误", jwtId);
                return false;
            }
        }

        /// <summary>
        /// 将Token加入黑名单（从Claims中提取信息）
        /// </summary>
        public async Task<bool> BlacklistTokenAsync(ClaimsPrincipal principal, string reason = "Manual revocation")
        {
            try
            {
                var jwtId = ExtractJwtId(principal);
                var userId = ExtractUserId(principal);
                var expirationTime = ExtractExpiration(principal);

                if (string.IsNullOrEmpty(jwtId))
                {
                    _logger.LogWarning("无法从Claims中提取JWT ID");
                    return false;
                }

                var blacklistEntry = new BlacklistEntry
                {
                    JwtId = jwtId,
                    UserId = userId,
                    RevokedAt = DateTime.UtcNow,
                    ExpiresAt = expirationTime,
                    Reason = reason
                };

                var result = await BlacklistTokenAsync(jwtId, expirationTime, reason);

                // 如果有用户ID，记录用户Token关联
                if (!string.IsNullOrEmpty(userId))
                {
                    await RecordUserTokenAsync(userId, jwtId, expirationTime);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Claims加入黑名单时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 检查Token是否在黑名单中
        /// </summary>
        public async Task<bool> IsTokenBlacklistedAsync(string jwtId)
        {
            try
            {
                if (string.IsNullOrEmpty(jwtId))
                {
                    return false;
                }

                var cacheKey = BLACKLIST_PREFIX + jwtId;
                var entry = await _cacheService.GetAsync<BlacklistEntry>(cacheKey);

                var isBlacklisted = entry != null && !entry.IsExpired;

                if (isBlacklisted)
                {
                    _logger.LogWarning(
                        "检测到黑名单Token访问尝试，JwtId: {JwtId}, 撤销原因: {Reason}, 撤销时间: {RevokedAt}",
                        jwtId, entry?.Reason, entry?.RevokedAt);
                }

                return isBlacklisted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查Token {JwtId} 黑名单状态时发生错误", jwtId);
                // 发生错误时，为安全起见，假设Token未被撤销
                return false;
            }
        }

        /// <summary>
        /// 检查Token是否在黑名单中（从Claims中提取JTI）
        /// </summary>
        public async Task<bool> IsTokenBlacklistedAsync(ClaimsPrincipal principal)
        {
            var jwtId = ExtractJwtId(principal);
            return await IsTokenBlacklistedAsync(jwtId);
        }

        /// <summary>
        /// 批量撤销用户的所有Token
        /// </summary>
        public async Task<int> BlacklistAllUserTokensAsync(string userId, string reason = "User logout")
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return 0;
                }

                var userTokensKey = USER_TOKENS_PREFIX + userId;
                var userTokens = await _cacheService.GetAsync<List<string>>(userTokensKey);

                if (userTokens == null || userTokens.Count == 0)
                {
                    _logger.LogInformation("用户 {UserId} 没有活跃的Token需要撤销", userId);
                    return 0;
                }

                int revokedCount = 0;
                foreach (var jwtId in userTokens)
                {
                    // 获取Token的过期时间（这里简化处理，实际应该从Token中解析）
                    var expirationTime = DateTime.UtcNow.AddDays(7); // 假设最长7天过期
                    
                    if (await BlacklistTokenAsync(jwtId, expirationTime, reason))
                    {
                        revokedCount++;
                    }
                }

                // 清空用户Token记录
                await _cacheService.RemoveAsync(userTokensKey);

                _logger.LogInformation(
                    "用户 {UserId} 的 {Count} 个Token已全部撤销，原因: {Reason}",
                    userId, revokedCount, reason);

                return revokedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量撤销用户 {UserId} Token时发生错误", userId);
                return 0;
            }
        }

        /// <summary>
        /// 清理过期的黑名单条目
        /// </summary>
        public async Task<int> CleanupExpiredEntriesAsync()
        {
            try
            {
                // 使用模式匹配删除所有黑名单条目
                // 注意：这个操作会删除所有过期条目，实际实现中可能需要更精细的控制
                var removedCount = await _cacheService.RemoveByPatternAsync(BLACKLIST_PREFIX + "*");

                _logger.LogInformation("清理了 {Count} 个过期的黑名单条目", removedCount);

                // 更新最后清理时间
                var statistics = await GetStatisticsAsync();
                statistics.LastCleanupTime = DateTime.UtcNow;
                await _cacheService.SetAsync(STATISTICS_KEY, statistics, TimeSpan.FromDays(30));

                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期黑名单条目时发生错误");
                return 0;
            }
        }

        /// <summary>
        /// 获取黑名单统计信息
        /// </summary>
        public async Task<TokenBlacklistStatistics> GetStatisticsAsync()
        {
            try
            {
                var statistics = await _cacheService.GetAsync<TokenBlacklistStatistics>(STATISTICS_KEY);
                return statistics ?? new TokenBlacklistStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取黑名单统计信息时发生错误");
                return new TokenBlacklistStatistics();
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 从Claims中提取JWT ID
        /// </summary>
        private string ExtractJwtId(ClaimsPrincipal principal)
        {
            return principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;
        }

        /// <summary>
        /// 从Claims中提取用户ID
        /// </summary>
        private string ExtractUserId(ClaimsPrincipal principal)
        {
            return principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? string.Empty;
        }

        /// <summary>
        /// 从Claims中提取过期时间
        /// </summary>
        private DateTime ExtractExpiration(ClaimsPrincipal principal)
        {
            var expClaim = principal?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
            if (long.TryParse(expClaim, out var exp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
            }
            
            // 如果无法提取过期时间，默认设为1小时后过期
            return DateTime.UtcNow.AddHours(1);
        }

        /// <summary>
        /// 记录用户Token关联
        /// </summary>
        private async Task RecordUserTokenAsync(string userId, string jwtId, DateTime expirationTime)
        {
            try
            {
                var userTokensKey = USER_TOKENS_PREFIX + userId;
                var userTokens = await _cacheService.GetAsync<List<string>>(userTokensKey) ?? new List<string>();

                if (!userTokens.Contains(jwtId))
                {
                    userTokens.Add(jwtId);
                    var cacheExpiration = expirationTime - DateTime.UtcNow;
                    
                    if (cacheExpiration > TimeSpan.Zero)
                    {
                        await _cacheService.SetAsync(userTokensKey, userTokens, cacheExpiration);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录用户 {UserId} Token关联时发生错误", userId);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private async Task UpdateStatisticsAsync(int increment)
        {
            try
            {
                var statistics = await GetStatisticsAsync();
                statistics.TotalBlacklistedTokens += increment;

                // 检查是否是今天
                var today = DateTime.UtcNow.Date;
                var lastUpdate = statistics.LastCleanupTime?.Date;
                
                if (lastUpdate != today)
                {
                    statistics.TodayBlacklistedCount = increment;
                }
                else
                {
                    statistics.TodayBlacklistedCount += increment;
                }

                await _cacheService.SetAsync(STATISTICS_KEY, statistics, TimeSpan.FromDays(30));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新统计信息时发生错误");
            }
        }

        #endregion
    }
}