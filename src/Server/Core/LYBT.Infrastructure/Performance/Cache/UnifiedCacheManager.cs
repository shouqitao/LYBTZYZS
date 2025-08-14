using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Collections.Concurrent;

namespace LYBT.Infrastructure.Performance.Cache
{
    /// <summary>
    /// 统一缓存管理器实现 - UltraThink性能优化核心
    /// 职责单一：专注缓存操作和性能统计
    /// 代码干净：清晰的接口和错误处理
    /// 性能出色：智能压缩、批处理和统计
    /// </summary>
    public class UnifiedCacheManager : IUnifiedCacheManager
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache? _distributedCache;
        private readonly ILogger<UnifiedCacheManager> _logger;
        private readonly CacheOptions _options;
        
        // 性能统计
        private long _hitCount = 0;
        private long _missCount = 0;
        private long _evictionCount = 0;
        
        // 内存使用追踪
        private readonly ConcurrentDictionary<string, (DateTime Created, long Size)> _memoryTracker 
            = new ConcurrentDictionary<string, (DateTime, long)>();

        public UnifiedCacheManager(
            IMemoryCache memoryCache,
            ILogger<UnifiedCacheManager> logger,
            CacheOptions? options = null,
            IDistributedCache? distributedCache = null)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _distributedCache = distributedCache;
            _options = options ?? new CacheOptions();
        }

        /// <summary>
        /// 获取缓存项 - 智能多层缓存
        /// </summary>
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) 
            where T : class
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            
            try
            {
                // 1. 先从内存缓存获取
                if (_memoryCache.TryGetValue(key, out var memoryValue))
                {
                    Interlocked.Increment(ref _hitCount);
                    _logger.LogDebug("缓存命中 [Memory]: {Key}", key);
                    return DeserializeValue<T>(memoryValue);
                }

                // 2. 如果有分布式缓存，从分布式缓存获取
                if (_distributedCache != null)
                {
                    var distributedValue = await _distributedCache.GetAsync(key, cancellationToken);
                    if (distributedValue != null)
                    {
                        var result = await DeserializeFromBytesAsync<T>(distributedValue, cancellationToken);
                        if (result != null)
                        {
                            // 将分布式缓存的数据放入内存缓存
                            await SetMemoryCacheAsync(key, result, _options.DefaultExpiration, cancellationToken);
                            
                            Interlocked.Increment(ref _hitCount);
                            _logger.LogDebug("缓存命中 [Distributed]: {Key}", key);
                            return result;
                        }
                    }
                }

                Interlocked.Increment(ref _missCount);
                _logger.LogDebug("缓存未命中: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缓存项失败: {Key}", key);
                Interlocked.Increment(ref _missCount);
                return null;
            }
        }

        /// <summary>
        /// 设置缓存项 - 智能压缩和多层存储
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) 
            where T : class
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(value);

            var effectiveExpiration = expiration ?? _options.DefaultExpiration;

            try
            {
                // 设置内存缓存
                await SetMemoryCacheAsync(key, value, effectiveExpiration, cancellationToken);

                // 如果有分布式缓存，同时设置分布式缓存
                if (_distributedCache != null)
                {
                    var serializedData = await SerializeToBytesAsync(value, cancellationToken);
                    var distributedOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = effectiveExpiration
                    };
                    
                    await _distributedCache.SetAsync(key, serializedData, distributedOptions, cancellationToken);
                    _logger.LogDebug("缓存已设置 [Memory + Distributed]: {Key}, 过期时间: {Expiration}", key, effectiveExpiration);
                }
                else
                {
                    _logger.LogDebug("缓存已设置 [Memory]: {Key}, 过期时间: {Expiration}", key, effectiveExpiration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置缓存项失败: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// 获取或设置缓存项 - 惰性加载模式
        /// </summary>
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) 
            where T : class
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(factory);

            // 先尝试获取缓存
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            try
            {
                _logger.LogDebug("执行工厂方法获取数据: {Key}", key);
                var stopwatch = Stopwatch.StartNew();
                
                // 执行工厂方法获取数据
                var value = await factory();
                
                stopwatch.Stop();
                _logger.LogDebug("工厂方法执行完成: {Key}, 耗时: {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);

                if (value != null)
                {
                    // 设置缓存
                    await SetAsync(key, value, expiration, cancellationToken);
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrSet工厂方法执行失败: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            try
            {
                // 移除内存缓存
                _memoryCache.Remove(key);
                _memoryTracker.TryRemove(key, out _);
                Interlocked.Increment(ref _evictionCount);

                // 移除分布式缓存
                if (_distributedCache != null)
                {
                    await _distributedCache.RemoveAsync(key, cancellationToken);
                }

                _logger.LogDebug("缓存项已移除: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除缓存项失败: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// 按模式移除缓存项 - 支持通配符
        /// </summary>
        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(pattern);

            try
            {
                var removedCount = 0;
                var keysToRemove = new List<string>();

                // 从内存追踪器中找到匹配的键
                foreach (var key in _memoryTracker.Keys)
                {
                    if (IsMatchPattern(key, pattern))
                    {
                        keysToRemove.Add(key);
                    }
                }

                // 批量移除
                foreach (var key in keysToRemove)
                {
                    await RemoveAsync(key, cancellationToken);
                    removedCount++;
                }

                _logger.LogInformation("按模式移除缓存完成: {Pattern}, 移除数量: {Count}", pattern, removedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按模式移除缓存失败: {Pattern}", pattern);
                throw;
            }
        }

        /// <summary>
        /// 批量设置缓存项
        /// </summary>
        public async Task SetBatchAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) 
            where T : class
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items.Count == 0) return;

            try
            {
                var tasks = items.Select(async kvp =>
                {
                    await SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken);
                });

                await Task.WhenAll(tasks);
                _logger.LogDebug("批量设置缓存完成: {Count}项", items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量设置缓存失败");
                throw;
            }
        }

        /// <summary>
        /// 批量获取缓存项
        /// </summary>
        public async Task<Dictionary<string, T?>> GetBatchAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) 
            where T : class
        {
            ArgumentNullException.ThrowIfNull(keys);

            var keyList = keys.ToList();
            if (keyList.Count == 0) return new Dictionary<string, T?>();

            try
            {
                var result = new Dictionary<string, T?>();
                var tasks = keyList.Select(async key =>
                {
                    var value = await GetAsync<T>(key, cancellationToken);
                    return new KeyValuePair<string, T?>(key, value);
                });

                var results = await Task.WhenAll(tasks);
                foreach (var kvp in results)
                {
                    result[kvp.Key] = kvp.Value;
                }

                _logger.LogDebug("批量获取缓存完成: {Count}项", keyList.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量获取缓存失败");
                throw;
            }
        }

        /// <summary>
        /// 检查缓存项是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            try
            {
                // 检查内存缓存
                if (_memoryCache.TryGetValue(key, out _))
                {
                    return true;
                }

                // 检查分布式缓存
                if (_distributedCache != null)
                {
                    var value = await _distributedCache.GetAsync(key, cancellationToken);
                    return value != null;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查缓存存在性失败: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = new CacheStatistics
                {
                    HitCount = Interlocked.Read(ref _hitCount),
                    MissCount = Interlocked.Read(ref _missCount),
                    EvictionCount = Interlocked.Read(ref _evictionCount),
                    TotalKeys = _memoryTracker.Count,
                    MemoryUsage = _memoryTracker.Values.Sum(v => v.Size)
                };

                _logger.LogDebug("缓存统计: 命中率={HitRate:P2}, 总键数={TotalKeys}, 内存使用={MemoryUsage}字节", 
                    stats.HitRate, stats.TotalKeys, stats.MemoryUsage);

                return await Task.FromResult(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缓存统计失败");
                return new CacheStatistics();
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public Task ClearAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 清空内存缓存 - 通过移除所有追踪的键
                var allKeys = _memoryTracker.Keys.ToList();
                foreach (var key in allKeys)
                {
                    _memoryCache.Remove(key);
                }
                _memoryTracker.Clear();

                // 重置统计计数器
                Interlocked.Exchange(ref _hitCount, 0);
                Interlocked.Exchange(ref _missCount, 0);
                Interlocked.Exchange(ref _evictionCount, 0);

                _logger.LogInformation("所有缓存已清空，统计已重置");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空缓存失败");
                throw;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 设置内存缓存
        /// </summary>
        private async Task SetMemoryCacheAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken) 
            where T : class
        {
            var memoryCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                Priority = CacheItemPriority.Normal
            };

            // 设置移除回调
            memoryCacheOptions.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                _memoryTracker.TryRemove(k.ToString() ?? string.Empty, out _);
                if (reason == EvictionReason.Expired || reason == EvictionReason.TokenExpired)
                {
                    Interlocked.Increment(ref _evictionCount);
                }
            });

            var serializedValue = await SerializeValueAsync(value, cancellationToken);
            _memoryCache.Set(key, serializedValue, memoryCacheOptions);

            // 追踪内存使用
            var size = EstimateSize(serializedValue);
            _memoryTracker.TryAdd(key, (DateTime.UtcNow, size));
        }

        /// <summary>
        /// 序列化值 - 支持压缩
        /// </summary>
        private async Task<object> SerializeValueAsync<T>(T value, CancellationToken cancellationToken) where T : class
        {
            var json = JsonSerializer.Serialize(value, _options.JsonSerializerOptions);
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            // 如果启用压缩且数据大于阈值
            if (_options.EnableCompression && jsonBytes.Length > _options.CompressionThreshold)
            {
                using var output = new MemoryStream();
                using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
                {
                    await gzip.WriteAsync(jsonBytes, 0, jsonBytes.Length, cancellationToken);
                }
                
                var compressedData = output.ToArray();
                _logger.LogDebug("数据已压缩: {OriginalSize} → {CompressedSize} 字节 ({CompressionRatio:P1})", 
                    jsonBytes.Length, compressedData.Length, 1.0 - (double)compressedData.Length / jsonBytes.Length);
                    
                return new CompressedCacheItem(compressedData);
            }

            return json;
        }

        /// <summary>
        /// 序列化为字节数组（分布式缓存）
        /// </summary>
        private async Task<byte[]> SerializeToBytesAsync<T>(T value, CancellationToken cancellationToken) where T : class
        {
            var serializedValue = await SerializeValueAsync(value, cancellationToken);
            
            if (serializedValue is CompressedCacheItem compressed)
            {
                return compressed.Data;
            }
            
            return System.Text.Encoding.UTF8.GetBytes(serializedValue.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 反序列化值
        /// </summary>
        private T? DeserializeValue<T>(object? value) where T : class
        {
            try
            {
                if (value == null)
                    return null;
                    
                if (value is CompressedCacheItem compressed)
                {
                    // 解压缩数据
                    using var input = new MemoryStream(compressed.Data);
                    using var gzip = new GZipStream(input, CompressionMode.Decompress);
                    using var reader = new StreamReader(gzip, System.Text.Encoding.UTF8);
                    var json = reader.ReadToEnd();
                    
                    return JsonSerializer.Deserialize<T>(json, _options.JsonSerializerOptions);
                }

                if (value is string jsonString)
                {
                    return JsonSerializer.Deserialize<T>(jsonString, _options.JsonSerializerOptions);
                }

                return value as T;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "反序列化缓存值失败: {Type}", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// 从字节数组反序列化
        /// </summary>
        private async Task<T?> DeserializeFromBytesAsync<T>(byte[] data, CancellationToken cancellationToken) where T : class
        {
            try
            {
                // 尝试作为压缩数据处理
                try
                {
                    using var input = new MemoryStream(data);
                    using var gzip = new GZipStream(input, CompressionMode.Decompress);
                    using var reader = new StreamReader(gzip, System.Text.Encoding.UTF8);
                    var json = await reader.ReadToEndAsync();
                    
                    return JsonSerializer.Deserialize<T>(json, _options.JsonSerializerOptions);
                }
                catch
                {
                    // 如果不是压缩数据，作为普通JSON处理
                    var json = System.Text.Encoding.UTF8.GetString(data);
                    return JsonSerializer.Deserialize<T>(json, _options.JsonSerializerOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从字节数组反序列化失败: {Type}", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// 估算对象大小
        /// </summary>
        private long EstimateSize(object value)
        {
            if (value is string str)
                return System.Text.Encoding.UTF8.GetByteCount(str);
            if (value is CompressedCacheItem compressed)
                return compressed.Data.Length;
            if (value is byte[] bytes)
                return bytes.Length;
                
            // 粗略估算
            return 100;
        }

        /// <summary>
        /// 模式匹配 - 支持简单通配符 * 和 ?
        /// </summary>
        private static bool IsMatchPattern(string input, string pattern)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
                return false;

            // 简单通配符匹配
            if (pattern.Contains('*'))
            {
                var parts = pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return true; // 只有 *
                
                var currentIndex = 0;
                foreach (var part in parts)
                {
                    var index = input.IndexOf(part, currentIndex, StringComparison.OrdinalIgnoreCase);
                    if (index == -1) return false;
                    currentIndex = index + part.Length;
                }
                return true;
            }

            return string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }

    /// <summary>
    /// 压缩缓存项
    /// </summary>
    internal class CompressedCacheItem
    {
        public byte[] Data { get; }

        public CompressedCacheItem(byte[] data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }
    }
}