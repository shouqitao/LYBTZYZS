using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LYBT.Infrastructure.Caching {

    /// <summary>
    /// 分布式缓存服务实现
    /// </summary>
    public class DistributedCacheService : ICacheService {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<DistributedCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public DistributedCacheService(
            IDistributedCache distributedCache,
            ILogger<DistributedCacheService> logger) {
            _distributedCache = distributedCache;
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
                var cachedValue = await _distributedCache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedValue)) {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// 设置缓存值
        /// </summary>
        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class {
            try {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var options = new DistributedCacheEntryOptions();

                if (expiry.HasValue) {
                    options.SetAbsoluteExpiration(expiry.Value);
                } else {
                    // 默认1小时过期
                    options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                }

                await _distributedCache.SetStringAsync(key, serializedValue, options);
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 删除缓存值
        /// </summary>
        public async Task<bool> RemoveAsync(string key) {
            try {
                await _distributedCache.RemoveAsync(key);
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(string key) {
            try {
                var value = await _distributedCache.GetStringAsync(key);
                return !string.IsNullOrEmpty(value);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 批量删除缓存（根据模式）
        /// 注意：IDistributedCache不直接支持模式删除，这里提供基础实现
        /// </summary>
        public async Task<long> RemoveByPatternAsync(string pattern) {
            // IDistributedCache接口不支持模式删除
            // 需要具体的缓存实现（如Redis）来支持此功能
            _logger.LogWarning("Pattern-based removal is not supported by IDistributedCache. Pattern: {Pattern}", pattern);
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
                await _distributedCache.RefreshAsync(key);
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error refreshing cache for key: {Key}", key);
                return false;
            }
        }
    }
}