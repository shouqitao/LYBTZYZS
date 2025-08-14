using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Caching;

namespace LYBT.Infrastructure.Caching
{
    /// <summary>
    /// 混合缓存服务 - UltraThink重构性能优化
    /// 实现L1(内存缓存) + L2(分布式缓存)的多级缓存策略
    /// 预期性能提升: 5-10倍查询速度提升
    /// </summary>
    public class HybridCacheService : IMemoryCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<HybridCacheService> _logger;
        
        // 缓存配置
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public HybridCacheService(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            ILogger<HybridCacheService> logger)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Synchronous Methods

        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default(T);

            try
            {
                // L1缓存检查
                if (_memoryCache.TryGetValue(key, out T cachedValue))
                {
                    _logger.LogDebug("Cache HIT (L1) for key: {Key}", key);
                    return cachedValue;
                }

                // L2缓存检查 (同步方式)
                var distributedValue = _distributedCache.GetString(key);
                if (!string.IsNullOrEmpty(distributedValue))
                {
                    _logger.LogDebug("Cache HIT (L2) for key: {Key}", key);
                    
                    var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, SerializerOptions);
                    
                    // 回填到L1缓存
                    _memoryCache.Set(key, deserializedValue, GetL1CacheOptions());
                    
                    return deserializedValue;
                }

                _logger.LogDebug("Cache MISS for key: {Key}", key);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
                return default(T);
            }
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key) || value == null)
                return;

            try
            {
                var l1Options = GetL1CacheOptions(expiration);
                var l2Options = GetL2CacheOptions(expiration);

                // L1缓存设置
                _memoryCache.Set(key, value, l1Options);

                // L2缓存设置
                var serializedValue = JsonSerializer.Serialize(value, SerializerOptions);
                _distributedCache.SetString(key, serializedValue, l2Options);

                _logger.LogDebug("Cache SET for key: {Key}, L1 expiry: {L1Expiry}, L2 expiry: {L2Expiry}", 
                    key, l1Options.AbsoluteExpirationRelativeToNow, l2Options.AbsoluteExpirationRelativeToNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            }
        }

        public void Set<T>(string key, T value, CacheOptions options)
        {
            Set(key, value, options.Duration);
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            value = Get<T>(key);
            return value != null;
        }

        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                _memoryCache.Remove(key);
                _distributedCache.Remove(key);
                
                _logger.LogDebug("Cache REMOVE for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
            }
        }

        public void Clear()
        {
            try
            {
                // 内存缓存清理
                if (_memoryCache is MemoryCache mc)
                {
                    mc.Clear();
                }

                // 分布式缓存没有通用的Clear方法，需要特定实现处理
                _logger.LogDebug("Cache CLEAR executed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
        }

        #endregion

        #region Asynchronous Methods

        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default(T);

            try
            {
                // L1缓存检查
                if (_memoryCache.TryGetValue(key, out T cachedValue))
                {
                    _logger.LogDebug("Cache HIT (L1) for key: {Key}", key);
                    return cachedValue;
                }

                // L2缓存检查
                var distributedValue = await _distributedCache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(distributedValue))
                {
                    _logger.LogDebug("Cache HIT (L2) for key: {Key}", key);
                    
                    var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, SerializerOptions);
                    
                    // 回填到L1缓存
                    _memoryCache.Set(key, deserializedValue, GetL1CacheOptions());
                    
                    return deserializedValue;
                }

                _logger.LogDebug("Cache MISS for key: {Key}", key);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value async for key: {Key}", key);
                return default(T);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key) || value == null)
                return;

            try
            {
                var l1Options = GetL1CacheOptions(expiration);
                var l2Options = GetL2CacheOptions(expiration);

                // L1缓存设置
                _memoryCache.Set(key, value, l1Options);

                // L2缓存设置
                var serializedValue = JsonSerializer.Serialize(value, SerializerOptions);
                await _distributedCache.SetStringAsync(key, serializedValue, l2Options);

                _logger.LogDebug("Cache SET ASYNC for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value async for key: {Key}", key);
            }
        }

        public Task SetAsync<T>(string key, T value, CacheOptions options)
        {
            return SetAsync(key, value, options.Duration);
        }

        public async Task<(bool exists, T value)> TryGetValueAsync<T>(string key)
        {
            var value = await GetAsync<T>(key);
            return (value != null, value);
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                _memoryCache.Remove(key);
                await _distributedCache.RemoveAsync(key);
                
                _logger.LogDebug("Cache REMOVE ASYNC for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache value async for key: {Key}", key);
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                Clear(); // 调用同步版本
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache async");
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            _logger.LogDebug("Cache-and-fill executing factory for key: {Key}", key);
            
            try
            {
                var value = await factory();
                if (value != null)
                {
                    await SetAsync(key, value, expiration);
                }
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrSetAsync factory for key: {Key}", key);
                throw;
            }
        }

        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions options)
        {
            return GetOrSetAsync(key, factory, options.Duration);
        }

        #endregion

        #region Private Helper Methods

        private MemoryCacheEntryOptions GetL1CacheOptions(TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                // L1缓存使用相对较短的过期时间，避免内存占用过多
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal
            };

            return options;
        }

        private DistributedCacheEntryOptions GetL2CacheOptions(TimeSpan? expiration = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                // L2缓存使用更长的过期时间
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(60),
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };

            return options;
        }

        #endregion
    }

    /// <summary>
    /// 智能缓存键生成器 - 基于业务规则的缓存键管理
    /// </summary>
    public static class CacheKeyGenerator
    {
        private const string KeySeparator = ":";
        
        public static string ForUser(Guid userId) => $"user{KeySeparator}{userId}";
        public static string ForUserList(int pageIndex, int pageSize, string searchTerm = null) 
            => $"user{KeySeparator}list{KeySeparator}{pageIndex}{KeySeparator}{pageSize}{KeySeparator}{searchTerm ?? "all"}";
            
        public static string ForPatient(Guid patientId) => $"patient{KeySeparator}{patientId}";
        public static string ForPatientList(int pageIndex, int pageSize, string searchTerm = null)
            => $"patient{KeySeparator}list{KeySeparator}{pageIndex}{KeySeparator}{pageSize}{KeySeparator}{searchTerm ?? "all"}";
            
        public static string ForHerb(Guid herbId) => $"herb{KeySeparator}{herbId}";
        public static string ForHerbList(int pageIndex, int pageSize, string category = null)
            => $"herb{KeySeparator}list{KeySeparator}{pageIndex}{KeySeparator}{pageSize}{KeySeparator}{category ?? "all"}";
            
        public static string ForPrescription(Guid prescriptionId) => $"prescription{KeySeparator}{prescriptionId}";
        public static string ForPrescriptionsByPatient(Guid patientId) => $"prescription{KeySeparator}patient{KeySeparator}{patientId}";
        
        public static string ForFormula(Guid formulaId) => $"formula{KeySeparator}{formulaId}";
        public static string ForFormulaTemplates() => $"formula{KeySeparator}templates";
        
        public static string ForStatistics(string type, DateTime? startDate, DateTime? endDate)
            => $"stats{KeySeparator}{type}{KeySeparator}{startDate?.ToString("yyyyMMdd") ?? "all"}{KeySeparator}{endDate?.ToString("yyyyMMdd") ?? "all"}";
    }
}