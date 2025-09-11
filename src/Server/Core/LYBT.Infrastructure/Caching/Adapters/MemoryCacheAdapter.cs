#nullable enable

using LYBT.Infrastructure.Caching.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

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
    /// </remarks>
    public class MemoryCacheAdapter : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheAdapter> _logger;
        private readonly ConcurrentDictionary<string, bool> _keys;
        private readonly CacheStatistics _statistics;

        /// <summary>
        /// 默认过期时间
        /// </summary>
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

        public MemoryCacheAdapter(IMemoryCache memoryCache, ILogger<MemoryCacheAdapter> logger)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keys = new ConcurrentDictionary<string, bool>();
            _statistics = new CacheStatistics();
        }

        #region 同步操作

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

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            try
            {
                var options = new MemoryCacheEntryOptions();
                var exp = expiration ?? DefaultExpiration;
                
                options.SetSlidingExpiration(exp);
                options.RegisterPostEvictionCallback((k, v, reason, state) =>
                {
                    _keys.TryRemove(k.ToString()!, out _);
                    if (reason == EvictionReason.Expired)
                    {
                        _statistics.ExpiredKeys++;
                    }
                    else if (reason == EvictionReason.Capacity || reason == EvictionReason.TokenExpired)
                    {
                        _statistics.EvictedKeys++;
                    }
                });

                _memoryCache.Set(key, value, options);
                _keys.TryAdd(key, true);
                
                _logger.LogDebug("Cache set for key: {Key}, expiration: {Expiration}", key, exp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            }
        }

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
                
                _logger.LogInformation("Cache cleared, removed {Count} keys", keysToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
        }

        public bool Exists(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return _memoryCache.TryGetValue(key, out _);
        }

        #endregion

        #region 异步操作

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            // Memory cache operations are synchronous, wrap in Task
            return Task.FromResult(Get<T>(key));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            Set(key, value, expiration);
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Remove(key));
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
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
            Set(key, result, expiration);
            
            return result;
        }

        #endregion

        #region 批量操作

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

        public Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(pattern))
                return Task.FromResult(0);

            var removedCount = 0;
            var keysToRemove = new List<string>();

            // Convert pattern to regex
            var regexPattern = pattern.Replace("*", ".*").Replace("?", ".");
            var regex = new System.Text.RegularExpressions.Regex($"^{regexPattern}$", 
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

        public Task<int> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            return RemoveByPatternAsync($"{prefix}*", cancellationToken);
        }

        #endregion

        #region 统计与监控

        public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new CacheStatistics
            {
                TotalKeys = _keys.Count,
                HitCount = _statistics.HitCount,
                MissCount = _statistics.MissCount,
                ExpiredKeys = _statistics.ExpiredKeys,
                EvictedKeys = _statistics.EvictedKeys,
                UsedMemory = EstimateMemoryUsage(),
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