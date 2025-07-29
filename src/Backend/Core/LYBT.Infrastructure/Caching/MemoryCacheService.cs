using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LYBT.Infrastructure.Caching {

    /// <summary>
    /// 内存缓存服务实现
    /// </summary>
    public class MemoryCacheService : ICacheService {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public MemoryCacheService(
            IMemoryCache memoryCache,
            ILogger<MemoryCacheService> logger) {
            _memoryCache = memoryCache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// 获取缓存值
        /// </summary>
        public async Task<T?> GetAsync<T>(string key) where T : class {
            try {
                await Task.CompletedTask;
                return _memoryCache.Get<T>(key);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error getting memory cache value for key: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// 设置缓存值
        /// </summary>
        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class {
            try {
                await Task.CompletedTask;
                var options = new MemoryCacheEntryOptions();

                if (expiry.HasValue) {
                    options.SetAbsoluteExpiration(expiry.Value);
                } else {
                    // 默认1小时过期
                    options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                }

                // 设置优先级
                options.SetPriority(CacheItemPriority.Normal);

                // 设置大小限制（可选）
                options.SetSize(1);

                _memoryCache.Set(key, value, options);
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error setting memory cache value for key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 删除缓存值
        /// </summary>
        public async Task<bool> RemoveAsync(string key) {
            try {
                await Task.CompletedTask;
                _memoryCache.Remove(key);
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error removing memory cache value for key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(string key) {
            try {
                await Task.CompletedTask;
                return _memoryCache.TryGetValue(key, out _);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error checking memory cache existence for key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 批量删除缓存（根据模式）
        /// 注意：IMemoryCache不直接支持模式删除
        /// </summary>
        public async Task<long> RemoveByPatternAsync(string pattern) {
            _logger.LogWarning("Pattern-based removal is not efficiently supported by IMemoryCache. Pattern: {Pattern}", pattern);
            await Task.CompletedTask;
            return 0;
        }

        /// <summary>
        /// 获取或设置缓存
        /// </summary>
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiry = null) where T : class {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null) {
                return cachedValue;
            }

            var value = await factory();
            if (value != null) {
                await SetAsync(key, value, expiry);
            }

            return value;
        }

        /// <summary>
        /// 刷新缓存过期时间
        /// </summary>
        public async Task<bool> RefreshAsync(string key, TimeSpan expiry) {
            try {
                await Task.CompletedTask;
                if (_memoryCache.TryGetValue(key, out var value)) {
                    // Use reflection to call SetAsync with the correct type argument
                    var valueType = value?.GetType() ?? typeof(object);
                    var method = typeof(MemoryCacheService).GetMethod(nameof(SetAsync))!
                        .MakeGenericMethod(valueType);
                    await (Task<bool>)method.Invoke(this, new object[] { key, value!, expiry })!;
                    return true;
                }
                return false;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error refreshing memory cache for key: {Key}", key);
                return false;
            }
        }
    }
}