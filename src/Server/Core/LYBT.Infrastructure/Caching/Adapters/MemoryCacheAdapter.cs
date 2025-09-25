#nullable enable

using System.Collections.Concurrent;
using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Caching.Models;
using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Caching.Adapters
{
    /// <summary>
    /// IMemoryCache适配器 - 将IMemoryCache适配到统一ICacheService接口
    /// </summary>
    /// <remarks>
    /// <para>适配目标: 将现有IMemoryCache实现适配到新的统一缓存接口</para>
    /// <para>兼容性: 保持与现有代码的完全兼容</para>
    /// <para>性能: 最小化适配开销，直接委托到底层IMemoryCache</para>
    /// <para>统计: 增加命中率和使用情况统计</para>
    /// <para>配置驱动: 支持通过CacheOptions配置缓存策略</para>
    /// </remarks>
    public class MemoryCacheAdapter : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheAdapter> _logger;
        private readonly ConcurrentDictionary<string, bool> _keys;
        private readonly CacheStatistics _statistics;
        private readonly CacheOptions _cacheOptions;
        private readonly object _evictionLock = new object();
        private DateTime _lastEvictionLog = DateTime.MinValue;
        private DateTime _lastEvictionRateCalculation = DateTime.UtcNow;
        private long _evictionCountSinceLastCalculation = 0;

        /// <summary>
        /// 默认过期时间
        /// </summary>
        private TimeSpan DefaultExpiration => TimeSpan.FromMinutes(_cacheOptions?.Memory?.DefaultCacheDurationMinutes ?? 10);

        public MemoryCacheAdapter(
            IMemoryCache memoryCache,
            ILogger<MemoryCacheAdapter> logger,
            IOptions<CacheOptions> cacheOptions = null)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheOptions = cacheOptions?.Value ?? new CacheOptions();
            _keys = new ConcurrentDictionary<string, bool>();
            _statistics = new CacheStatistics();
        }

        #region 同步操作

        /// <inheritdoc/>
        public T? Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                if (_memoryCache.TryGetValue(key, out var value))
                {
                    _statistics.HitCount++;
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return value is T typedValue ? typedValue : default;
                }

                _statistics.MissCount++;
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
                return default;
            }
        }

        /// <inheritdoc/>
        public void Set<T>(string key, T value, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                var options = new MemoryCacheEntryOptions();
                var exp = expiration ?? DefaultExpiration;

                // 设置过期策略
                if (_cacheOptions.Memory.UseSlidingExpiration)
                {
                    options.SlidingExpiration = exp;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = exp;
                }

                // 设置缓存项大小
                if (_cacheOptions.Memory.DefaultItemSize > 0)
                {
                    options.Size = _cacheOptions.Memory.DefaultItemSize;
                }

                // 设置优先级
                var cacheItemPriority = GetCacheItemPriority(priority);
                options.Priority = cacheItemPriority;

                // 注册逐出回调
                options.RegisterPostEvictionCallback((k, v, reason, state) =>
                {
                    _keys.TryRemove(k.ToString()!, out _);

                    // 更新统计
                    if (reason == EvictionReason.Expired)
                    {
                        _statistics.ExpiredKeys++;
                    }
                    else if (reason == EvictionReason.Capacity || reason == EvictionReason.TokenExpired)
                    {
                        _statistics.EvictedKeys++;
                        _statistics.EvictionCount++;
                    }

                    // 记录逐出日志（如果启用）
                    if (_cacheOptions.Memory.LogEvictions)
                    {
                        LogEviction(k.ToString()!, reason, GetEstimatedSize(v));
                    }
                });

                _memoryCache.Set(key, value, options);
                _keys.TryAdd(key, true);
                _statistics.CurrentItemCount = _keys.Count;

                _logger.LogDebug("缓存设置 - 键: {Key}, 过期: {Expiration}, 优先级: {Priority}, 大小: {Size}",
                    key, exp, priority, options.Size);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            }
        }

        /// <summary>
        /// 将缓存优先级转换为MemoryCache优先级
        /// </summary>
        private CacheItemPriority GetCacheItemPriority(CachePriority priority)
        {
            return priority switch
            {
                CachePriority.Low => CacheItemPriority.Low,
                CachePriority.Normal => CacheItemPriority.Normal,
                CachePriority.High => CacheItemPriority.High,
                CachePriority.NeverRemove => CacheItemPriority.NeverRemove,
                _ => CacheItemPriority.Normal
            };
        }

        /// <summary>
        /// 记录逐出日志
        /// </summary>
        private void LogEviction(string key, EvictionReason reason, long estimatedSize)
        {
            // 更新逐出计数
            _evictionCountSinceLastCalculation++;
            _statistics.EvictedKeys++;

            // 限制日志频率，避免过多日志输出
            lock (_evictionLock)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastEvictionLog).TotalSeconds < 1)
                    return;

                _lastEvictionLog = now;
            }

            var eventId = new EventId(_cacheOptions.Monitoring.EventIds.HighEvictionRate, "CacheEviction");
            _logger.LogInformation(eventId,
                "缓存逐出 - 键前缀: {KeyPrefix}, 原因: {Reason}, 估算大小: {Size}B, 当前项数: {CurrentCount}",
                GetKeyPrefix(key), reason, estimatedSize, _keys.Count);
        }

        /// <summary>
        /// 获取键前缀（隐私保护）
        /// </summary>
        private string GetKeyPrefix(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "unknown";

            var colonIndex = key.IndexOf(':');
            return colonIndex > 0 ? key.Substring(0, Math.Min(colonIndex, 20)) : key.Substring(0, Math.Min(key.Length, 10));
        }

        /// <summary>
        /// 估算对象大小（简单估算）
        /// </summary>
        private long GetEstimatedSize(object obj)
        {
            if (obj == null)
                return 0;

            // 简单估算，实际应用中可以使用更精确的方法
            if (obj is string str)
                return str.Length * 2; // Unicode字符

            if (obj is byte[] bytes)
                return bytes.Length;

            // 默认估算
            return 100;
        }

        /// <inheritdoc/>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                var existed = _keys.ContainsKey(key);
                _memoryCache.Remove(key);
                _keys.TryRemove(key, out _);

                if (existed)
                {
                    _statistics.CurrentItemCount = _keys.Count;
                    _logger.LogDebug("Cache removed for key: {Key}", key);
                }

                return existed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            try
            {
                // IMemoryCache doesn't have a direct Clear method
                // We need to remove individual keys
                var keysToRemove = _keys.Keys.ToList();
                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                }

                _keys.Clear();
                _statistics.CurrentItemCount = 0;

                _logger.LogInformation("Cache cleared, removed {Count} keys", keysToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
        }

        /// <inheritdoc/>
        public bool Exists(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return _memoryCache.TryGetValue(key, out _);
        }

        #endregion

        #region 异步操作

        /// <inheritdoc/>
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            // Memory cache operations are synchronous, wrap in Task
            return Task.FromResult(Get<T>(key));
        }

        /// <inheritdoc/>
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal, CancellationToken cancellationToken = default)
        {
            Set(key, value, expiration, priority);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Remove(key));
        }

        /// <inheritdoc/>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // Try to get from cache first
            if (_memoryCache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
            {
                _statistics.HitCount++;
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return typedValue;
            }

            _statistics.MissCount++;
            _logger.LogDebug("Cache miss for key: {Key}, calling factory", key);

            // Call factory and cache result
            var result = await factory();
            Set(key, result, expiration, priority);

            return result;
        }

        #endregion

        #region 批量操作

        /// <inheritdoc/>
        public Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, T?>();

            foreach (var key in keys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var value = Get<T>(key);
                if (value != null)
                {
                    result[key] = value;
                }
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                Set(item.Key, item.Value, expiration);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<int> RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            int removedCount = 0;

            foreach (var key in keys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (Remove(key))
                {
                    removedCount++;
                }
            }

            return Task.FromResult(removedCount);
        }

        #endregion

        #region 模式操作

        /// <inheritdoc/>
        public Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(pattern))
                return Task.FromResult(0);

            var removedCount = 0;
            var keysToRemove = new List<string>();

            // Convert pattern to regex
            var regexPattern = pattern.Replace("*", ".*").Replace("?", ".");
            var regex = new System.Text.RegularExpressions.Regex(
                $"^{regexPattern}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (var key in _keys.Keys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (regex.IsMatch(key))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                if (Remove(key))
                {
                    removedCount++;
                }
            }

            _logger.LogDebug("Removed {Count} keys matching pattern: {Pattern}", removedCount, pattern);
            return Task.FromResult(removedCount);
        }

        /// <inheritdoc/>
        public Task<int> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            return RemoveByPatternAsync($"{prefix}*", cancellationToken);
        }

        #endregion

        #region 统计与监控

        /// <inheritdoc/>
        public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            // 计算逐出速率
            var now = DateTime.UtcNow;
            var timeSinceLastCalculation = (now - _lastEvictionRateCalculation).TotalMinutes;
            var evictionRate = timeSinceLastCalculation > 0 
                ? _evictionCountSinceLastCalculation / timeSinceLastCalculation 
                : 0;

            var stats = new CacheStatistics
            {
                TotalKeys = _keys.Count,
                HitCount = _statistics.HitCount,
                MissCount = _statistics.MissCount,
                ExpiredKeys = _statistics.ExpiredKeys,
                EvictedKeys = _statistics.EvictedKeys,
                UsedMemory = EstimateMemoryUsage(),
                CurrentItemCount = _keys.Count,
                TotalMemoryUsage = EstimateMemoryUsage(),
                MaxCapacity = _cacheOptions?.Memory?.SizeLimit,
                EvictionRate = evictionRate,
                EvictionCount = _statistics.EvictedKeys,
                Timestamp = DateTime.UtcNow
            };

            return Task.FromResult(stats);
        }

        private long EstimateMemoryUsage()
        {
            // Simple estimation based on key count
            // In a real implementation, you might want more accurate memory tracking
            return _keys.Count * 100; // Rough estimate of 100 bytes per cache entry
        }

        #endregion
    }
}
