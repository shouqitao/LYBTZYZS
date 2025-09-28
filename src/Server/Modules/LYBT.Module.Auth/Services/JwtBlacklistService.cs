using LYBT.Module.Auth.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// JWT黑名单服务实现
    /// 使用内存缓存存储被撤销的JWT Token ID
    /// </summary>
    public class JwtBlacklistService : IJwtBlacklistService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<JwtBlacklistService> _logger;
        private readonly ConcurrentDictionary<string, DateTime> _blacklistRegistry;
        private readonly object _statsLock = new object();
        private DateTime? _lastCleanupTime;
        private int _todayAddedCount;
        private DateTime _lastStatsReset;

        private const string BLACKLIST_KEY_PREFIX = "jwt_blacklist:";
        private const string STATS_TODAY_KEY = "blacklist_stats_today";

        public JwtBlacklistService(
            IMemoryCache cache,
            ILogger<JwtBlacklistService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _blacklistRegistry = new ConcurrentDictionary<string, DateTime>();
            _lastStatsReset = DateTime.UtcNow.Date;
        }

        /// <summary>
        /// 将Token添加到黑名单
        /// </summary>
        public Task<bool> AddToBlacklistAsync(string jwtId, DateTime expiration)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jwtId))
                {
                    _logger.LogWarning("尝试添加空的JWT ID到黑名单");
                    return Task.FromResult(false);
                }

                var cacheKey = BLACKLIST_KEY_PREFIX + jwtId;
                var cacheExpiration = expiration > DateTime.UtcNow ? expiration : DateTime.UtcNow.AddMinutes(5);

                // 添加到内存缓存，设置自动过期
                _cache.Set(cacheKey, true, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = cacheExpiration,
                    Priority = CacheItemPriority.Low // 低优先级，内存不足时优先清理
                });

                // 添加到注册表用于统计
                _blacklistRegistry.TryAdd(jwtId, expiration);

                // 更新统计
                UpdateTodayStats();

                _logger.LogInformation("JWT已添加到黑名单: {JwtId}, 过期时间: {Expiration}", jwtId, expiration);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加JWT到黑名单失败: {JwtId}", jwtId);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 检查Token是否在黑名单中
        /// </summary>
        public Task<bool> IsBlacklistedAsync(string jwtId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jwtId))
                    return Task.FromResult(false);

                var cacheKey = BLACKLIST_KEY_PREFIX + jwtId;
                var isBlacklisted = _cache.TryGetValue(cacheKey, out _);

                return Task.FromResult(isBlacklisted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查JWT黑名单状态失败: {JwtId}", jwtId);
                // 出现异常时，为了安全起见返回true（视为已被撤销）
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// 批量将Token添加到黑名单
        /// </summary>
        public async Task<int> AddMultipleToBlacklistAsync(IEnumerable<(string JwtId, DateTime Expiration)> tokenInfos)
        {
            int successCount = 0;

            try
            {
                foreach (var (jwtId, expiration) in tokenInfos)
                {
                    var success = await AddToBlacklistAsync(jwtId, expiration);
                    if (success)
                    {
                        successCount++;
                    }
                }

                _logger.LogInformation("批量添加JWT到黑名单完成: {SuccessCount}/{TotalCount}", 
                    successCount, tokenInfos.Count());

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量添加JWT到黑名单失败");
                return successCount;
            }
        }

        /// <summary>
        /// 清理过期的黑名单记录
        /// </summary>
        public Task<int> CleanupExpiredAsync()
        {
            int cleanedCount = 0;

            try
            {
                var expiredKeys = new List<string>();
                var currentTime = DateTime.UtcNow;

                // 查找过期的记录
                foreach (var kvp in _blacklistRegistry)
                {
                    if (kvp.Value <= currentTime)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                // 从注册表中移除过期记录
                foreach (var jwtId in expiredKeys)
                {
                    if (_blacklistRegistry.TryRemove(jwtId, out _))
                    {
                        cleanedCount++;
                    }
                }

                lock (_statsLock)
                {
                    _lastCleanupTime = DateTime.UtcNow;
                }

                if (cleanedCount > 0)
                {
                    _logger.LogInformation("清理过期黑名单记录: {CleanedCount} 个", cleanedCount);
                }

                return Task.FromResult(cleanedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期黑名单记录失败");
                return Task.FromResult(cleanedCount);
            }
        }

        /// <summary>
        /// 获取黑名单统计信息
        /// </summary>
        public Task<BlacklistStats> GetStatsAsync()
        {
            try
            {
                // 重置每日统计
                ResetDailyStatsIfNeeded();

                var stats = new BlacklistStats
                {
                    TotalCount = _blacklistRegistry.Count,
                    TodayAddedCount = _todayAddedCount,
                    LastCleanupTime = _lastCleanupTime,
                    MemoryUsage = GC.GetTotalMemory(false) // 近似内存使用量
                };

                return Task.FromResult(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取黑名单统计信息失败");
                return Task.FromResult(new BlacklistStats());
            }
        }

        #region 私有方法

        /// <summary>
        /// 更新今日统计
        /// </summary>
        private void UpdateTodayStats()
        {
            lock (_statsLock)
            {
                ResetDailyStatsIfNeeded();
                _todayAddedCount++;
            }
        }

        /// <summary>
        /// 如需要，重置每日统计
        /// </summary>
        private void ResetDailyStatsIfNeeded()
        {
            var today = DateTime.UtcNow.Date;
            if (_lastStatsReset < today)
            {
                _todayAddedCount = 0;
                _lastStatsReset = today;
            }
        }

        #endregion
    }
}