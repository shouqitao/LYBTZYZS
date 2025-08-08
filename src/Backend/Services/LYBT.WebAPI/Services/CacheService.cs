using LYBT.Infrastructure.Options;
using LYBT.WebAPI.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LYBT.WebAPI.Services
{
    /// <summary>
    /// 智能缓存服务实现
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly CacheOptions _options;
        private readonly ConcurrentDictionary<string, CacheEntry> _cacheEntries;
        private readonly ConcurrentDictionary<string, List<string>> _tagMappings;

        // 缓存统计
        private long _hitCount = 0;
        private long _missCount = 0;

        public CacheService(
            IMemoryCache memoryCache, 
            ILogger<CacheService> logger,
            IOptions<CacheOptions> options)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _options = options.Value;
            _cacheEntries = new ConcurrentDictionary<string, CacheEntry>();
            _tagMappings = new ConcurrentDictionary<string, List<string>>();
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            // 先尝试从缓存获取
            var cached = await GetAsync<T>(key);
            if (cached != null)
            {
                return cached;
            }

            // 缓存未命中，调用工厂方法获取数据
            var result = await factory();
            if (result != null)
            {
                await SetAsync(key, result, expiration);
            }

            return result;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out var cachedValue))
                {
                    Interlocked.Increment(ref _hitCount);
                    
                    if (_options.Statistics.Enabled)
                    {
                        _logger.LogDebug("Cache hit for key: {Key}", key);
                    }
                    
                    var result = cachedValue is T ? (T)cachedValue : default;
                    return Task.FromResult(result);
                }

                Interlocked.Increment(ref _missCount);
                
                if (_options.Statistics.Enabled)
                {
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                }
                
                return Task.FromResult<T?>(default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
                return Task.FromResult<T?>(default);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new MemoryCacheEntryOptions();
                var exp = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpiryMinutes);
                
                options.SetSlidingExpiration(exp);
                options.SetPriority(CacheItemPriority.Normal);
                
                // 设置回调清理元数据
                options.RegisterPostEvictionCallback((k, v, reason, state) =>
                {
                    _cacheEntries.TryRemove(k.ToString()!, out _);
                });

                _memoryCache.Set(key, value, options);

                // 记录缓存条目元数据
                if (_options.Statistics.Enabled)
                {
                    var module = ExtractModuleFromKey(key);
                    _cacheEntries.TryAdd(key, new CacheEntry
                    {
                        Key = key,
                        Module = module,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.Add(exp),
                        Size = EstimateSize(value)
                    });

                    _logger.LogDebug("Cache set for key: {Key}, Module: {Module}, Expiration: {Expiration}", 
                        key, module, exp);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _memoryCache.Remove(key);
                _cacheEntries.TryRemove(key, out _);
                
                if (_options.Statistics.Enabled)
                {
                    _logger.LogDebug("Cache removed for key: {Key}", key);
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var keysToRemove = _cacheEntries.Keys
                    .Where(key => key.Contains(pattern))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    await RemoveAsync(key);
                }

                if (_options.Statistics.Enabled)
                {
                    _logger.LogDebug("Cache removed by pattern: {Pattern}, Count: {Count}", pattern, keysToRemove.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache values by pattern: {Pattern}", pattern);
            }
        }

        public async Task RemoveByTagAsync(string tag)
        {
            try
            {
                if (_tagMappings.TryGetValue(tag, out var keys))
                {
                    foreach (var key in keys)
                    {
                        await RemoveAsync(key);
                    }
                    
                    _tagMappings.TryRemove(tag, out _);
                    
                    if (_options.Statistics.Enabled)
                    {
                        _logger.LogDebug("Cache removed by tag: {Tag}, Count: {Count}", tag, keys.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache values by tag: {Tag}", tag);
            }
        }

        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            try
            {
                var stats = new CacheStatistics
                {
                    TotalKeys = _cacheEntries.Count,
                    HitCount = (int)_hitCount,
                    MissCount = (int)_missCount,
                    TotalMemoryUsed = _cacheEntries.Values.Sum(e => e.Size)
                };

                stats.HitRate = stats.HitCount + stats.MissCount > 0 
                    ? (double)stats.HitCount / (stats.HitCount + stats.MissCount) 
                    : 0;

                // 按模块统计
                stats.KeysByModule = _cacheEntries.Values
                    .GroupBy(e => e.Module)
                    .ToDictionary(g => g.Key, g => g.Count());

                return await Task.FromResult(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return new CacheStatistics();
            }
        }

        public string GenerateKey(string module, string method, params object[] parameters)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append($"cache:{module.ToLower()}:{method.ToLower()}");

            if (parameters?.Length > 0)
            {
                var paramHash = HashParameters(parameters);
                keyBuilder.Append($":{paramHash}");
            }

            return keyBuilder.ToString();
        }

        public string GeneratePagedKey(string module, int page, int pageSize, string? filter = null)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append($"cache:{module.ToLower()}:paged:p{page}:s{pageSize}");

            if (!string.IsNullOrEmpty(filter))
            {
                var filterHash = HashString(filter);
                keyBuilder.Append($":f{filterHash}");
            }

            return keyBuilder.ToString();
        }

        public string GenerateListKey(string module, string? filter = null)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append($"cache:{module.ToLower()}:list");

            if (!string.IsNullOrEmpty(filter))
            {
                var filterHash = HashString(filter);
                keyBuilder.Append($":f{filterHash}");
            }

            return keyBuilder.ToString();
        }

        private string ExtractModuleFromKey(string key)
        {
            var parts = key.Split(':');
            return parts.Length >= 2 ? parts[1] : "unknown";
        }

        private long EstimateSize(object? value)
        {
            if (value == null) return 0;
            
            try
            {
                var json = JsonSerializer.Serialize(value);
                return Encoding.UTF8.GetByteCount(json);
            }
            catch
            {
                // 如果序列化失败，使用简单估算
                return value.ToString()?.Length ?? 0;
            }
        }

        private string HashParameters(object[] parameters)
        {
            var combined = string.Join("|", parameters.Select(p => p?.ToString() ?? "null"));
            return HashString(combined);
        }

        private string HashString(string input)
        {
            using var hash = SHA256.Create();
            var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes)[..8]; // 取前8个字符
        }

        /// <summary>
        /// 缓存条目元数据
        /// </summary>
        private class CacheEntry
        {
            public string Key { get; set; } = string.Empty;
            public string Module { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public long Size { get; set; }
        }
    }
}