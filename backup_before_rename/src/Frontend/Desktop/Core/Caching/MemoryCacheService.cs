using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Caching
{
    /// <summary>
    /// 智能内存缓存服务 - 支持多级缓存、LRU淘汰、自动过期
    /// </summary>
    public interface IMemoryCacheService
    {
        T? Get<T>(string key) where T : class;
        Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null) where T : class;
        void Set<T>(string key, T value, CacheOptions? options = null) where T : class;
        bool Remove(string key);
        void Clear();
        void ClearByPrefix(string prefix);
        CacheStatistics GetStatistics();
        void Compact(double percentage = 0.1);
    }

    /// <summary>
    /// 缓存选项
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// 绝对过期时间
        /// </summary>
        public TimeSpan? AbsoluteExpiration { get; set; }

        /// <summary>
        /// 滑动过期时间
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;

        /// <summary>
        /// 大小（用于内存限制计算）
        /// </summary>
        public long Size { get; set; } = 1;

        /// <summary>
        /// 是否使用弱引用
        /// </summary>
        public bool UseWeakReference { get; set; }

        /// <summary>
        /// 缓存级别
        /// </summary>
        public CacheLevel Level { get; set; } = CacheLevel.L1;

        /// <summary>
        /// 移除回调
        /// </summary>
        public Action<string, object, EvictionReason>? RemovedCallback { get; set; }

        /// <summary>
        /// 预设配置：短期缓存（5分钟）
        /// </summary>
        public static CacheOptions ShortTerm => new()
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(5),
            Priority = CacheItemPriority.Normal
        };

        /// <summary>
        /// 预设配置：中期缓存（30分钟）
        /// </summary>
        public static CacheOptions MediumTerm => new()
        {
            AbsoluteExpiration = TimeSpan.FromMinutes(30),
            Priority = CacheItemPriority.Normal
        };

        /// <summary>
        /// 预设配置：长期缓存（2小时）
        /// </summary>
        public static CacheOptions LongTerm => new()
        {
            AbsoluteExpiration = TimeSpan.FromHours(2),
            Priority = CacheItemPriority.Normal
        };

        /// <summary>
        /// 预设配置：滑动缓存（10分钟）
        /// </summary>
        public static CacheOptions Sliding => new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.Normal
        };
    }

    /// <summary>
    /// 缓存级别
    /// </summary>
    public enum CacheLevel
    {
        /// <summary>
        /// L1缓存 - 热数据，内存存储
        /// </summary>
        L1,

        /// <summary>
        /// L2缓存 - 温数据，可选磁盘存储
        /// </summary>
        L2,

        /// <summary>
        /// L3缓存 - 冷数据，压缩存储
        /// </summary>
        L3
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        public long TotalHits { get; set; }
        public long TotalMisses { get; set; }
        public double HitRate => TotalHits + TotalMisses > 0 
            ? (double)TotalHits / (TotalHits + TotalMisses) 
            : 0;
        public long CurrentItemCount { get; set; }
        public long EstimatedSize { get; set; }
        public long Evictions { get; set; }
        public DateTime LastCompaction { get; set; }
        public Dictionary<string, long> HitsByPrefix { get; set; } = new();
    }

    /// <summary>
    /// 内存缓存服务实现
    /// </summary>
    public class MemoryCacheService : IMemoryCacheService, IDisposable
    {
        private readonly IMemoryCache _l1Cache;
        private readonly ConcurrentDictionary<string, WeakReference> _weakCache;
        private readonly ConcurrentDictionary<string, CacheStatisticsEntry> _statistics;
        private readonly ConcurrentDictionary<string, object> _cacheKeys; // 用于跟踪所有缓存键
        private readonly ILogger<MemoryCacheService>? _logger;
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new();
        private long _totalHits;
        private long _totalMisses;
        private long _evictions;
        private DateTime _lastCompaction = DateTime.UtcNow;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MemoryCacheService(ILogger<MemoryCacheService>? logger = null, IMemoryCache? memoryCache = null)
        {
            _logger = logger;
            
            // 使用提供的缓存或创建新的
            _l1Cache = memoryCache ?? new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100 * 1024 * 1024, // 100MB限制
                CompactionPercentage = 0.1 // 压缩10%
            });
            
            _weakCache = new ConcurrentDictionary<string, WeakReference>();
            _statistics = new ConcurrentDictionary<string, CacheStatisticsEntry>();
            _cacheKeys = new ConcurrentDictionary<string, object>();
            
            // 启动清理定时器
            _cleanupTimer = new Timer(CleanupCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// 获取缓存项
        /// </summary>
        public T? Get<T>(string key) where T : class
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            // 先查L1缓存
            if (_l1Cache.TryGetValue(key, out T? item))
            {
                RecordHit(key);
                return item;
            }

            // 再查弱引用缓存
            if (_weakCache.TryGetValue(key, out var weakRef) && weakRef.IsAlive)
            {
                item = weakRef.Target as T;
                if (item != null)
                {
                    RecordHit(key);
                    // 提升到L1缓存
                    var options = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                        Size = 1
                    };
                    _l1Cache.Set(key, item, options);
                    return item;
                }
            }

            RecordMiss(key);
            return null;
        }

        /// <summary>
        /// 获取或创建缓存项（异步）
        /// </summary>
        public async Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null) 
            where T : class
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // 尝试从缓存获取
            var cached = Get<T>(key);
            if (cached != null)
                return cached;

            // 使用锁防止缓存击穿
            var lockKey = $"lock_{key}";
            var lockTaken = false;
            
            try
            {
                Monitor.TryEnter(_lockObject, TimeSpan.FromSeconds(10), ref lockTaken);
                if (lockTaken)
                {
                    // 双重检查
                    cached = Get<T>(key);
                    if (cached != null)
                        return cached;

                    // 创建新项
                    var sw = Stopwatch.StartNew();
                    var value = await factory();
                    sw.Stop();
                    
                    _logger?.LogDebug($"缓存未命中，创建耗时: {sw.ElapsedMilliseconds}ms, Key: {key}");
                    
                    if (value != null)
                    {
                        Set(key, value, options);
                    }
                    
                    return value;
                }
                else
                {
                    // 等待其他线程完成
                    await Task.Delay(100);
                    return Get<T>(key);
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_lockObject);
                }
            }
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        public void Set<T>(string key, T value, CacheOptions? options = null) where T : class
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            options ??= CacheOptions.MediumTerm;

            // 创建缓存选项
            var entryOptions = new MemoryCacheEntryOptions();
            
            if (options.AbsoluteExpiration.HasValue)
            {
                entryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpiration.Value;
            }
            
            if (options.SlidingExpiration.HasValue)
            {
                entryOptions.SlidingExpiration = options.SlidingExpiration.Value;
            }
            
            entryOptions.Priority = options.Priority;
            entryOptions.Size = options.Size;
            
            if (options.RemovedCallback != null)
            {
                entryOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    options.RemovedCallback(evictedKey.ToString()!, evictedValue!, reason);
                    if (reason == EvictionReason.Capacity || reason == EvictionReason.Replaced)
                    {
                        Interlocked.Increment(ref _evictions);
                    }
                    // 从跟踪列表中移除
                    _cacheKeys.TryRemove(evictedKey.ToString()!, out _);
                });
            }

            // 根据缓存级别处理
            switch (options.Level)
            {
                case CacheLevel.L1:
                    _l1Cache.Set(key, value, entryOptions);
                    _cacheKeys.TryAdd(key, value);
                    break;
                    
                case CacheLevel.L2:
                    // L2使用弱引用
                    _weakCache[key] = new WeakReference(value);
                    break;
                    
                case CacheLevel.L3:
                    // L3可以实现压缩存储
                    // 这里简化处理，使用弱引用
                    _weakCache[key] = new WeakReference(value);
                    break;
            }

            // 如果使用弱引用，同时存储
            if (options.UseWeakReference)
            {
                _weakCache[key] = new WeakReference(value);
            }

            _logger?.LogTrace($"缓存设置: Key={key}, Level={options.Level}, Size={options.Size}");
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            _l1Cache.Remove(key);
            _weakCache.TryRemove(key, out _);
            _cacheKeys.TryRemove(key, out _);
            
            return true;
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            // 清除所有已知的键
            foreach (var key in _cacheKeys.Keys.ToList())
            {
                _l1Cache.Remove(key);
            }
            
            _cacheKeys.Clear();
            _weakCache.Clear();
            _statistics.Clear();
            
            _totalHits = 0;
            _totalMisses = 0;
            _evictions = 0;
            
            _logger?.LogInformation("缓存已清空");
        }

        /// <summary>
        /// 根据前缀清除缓存
        /// </summary>
        public void ClearByPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return;

            var keys = _cacheKeys.Keys.Where(k => k.StartsWith(prefix)).ToList();
            
            foreach (var key in keys)
            {
                Remove(key);
            }
            
            var weakKeys = _weakCache.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in weakKeys)
            {
                _weakCache.TryRemove(key, out _);
            }
            
            _logger?.LogInformation($"清除前缀缓存: {prefix}, 移除 {keys.Count + weakKeys.Count} 项");
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            var stats = new CacheStatistics
            {
                TotalHits = _totalHits,
                TotalMisses = _totalMisses,
                CurrentItemCount = _cacheKeys.Count + _weakCache.Count,
                EstimatedSize = _cacheKeys.Count * 1024, // 估算值
                Evictions = _evictions,
                LastCompaction = _lastCompaction
            };

            // 按前缀统计
            foreach (var entry in _statistics)
            {
                var prefix = entry.Key.Split('_')[0];
                if (!stats.HitsByPrefix.ContainsKey(prefix))
                {
                    stats.HitsByPrefix[prefix] = 0;
                }
                stats.HitsByPrefix[prefix] += entry.Value.Hits;
            }

            return stats;
        }

        /// <summary>
        /// 压缩缓存（移除部分项以释放内存）
        /// </summary>
        public void Compact(double percentage = 0.1)
        {
            var itemsToRemove = (int)(_cacheKeys.Count * percentage);
            if (itemsToRemove <= 0)
                return;

            // 获取访问频率最低的项
            var leastUsed = _statistics
                .OrderBy(kvp => kvp.Value.LastAccess)
                .Take(itemsToRemove)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in leastUsed)
            {
                Remove(key);
            }

            // 清理弱引用
            var deadWeakRefs = _weakCache
                .Where(kvp => !kvp.Value.IsAlive)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in deadWeakRefs)
            {
                _weakCache.TryRemove(key, out _);
            }

            // 触发内存缓存压缩
            if (_l1Cache is MemoryCache memCache)
            {
                memCache.Compact(percentage);
            }

            _lastCompaction = DateTime.UtcNow;
            _logger?.LogInformation($"缓存压缩完成: 移除 {leastUsed.Count} 项, 清理 {deadWeakRefs.Count} 个死引用");
        }

        /// <summary>
        /// 记录命中
        /// </summary>
        private void RecordHit(string key)
        {
            Interlocked.Increment(ref _totalHits);
            
            _statistics.AddOrUpdate(key,
                k => new CacheStatisticsEntry { Hits = 1, LastAccess = DateTime.UtcNow },
                (k, v) =>
                {
                    v.Hits++;
                    v.LastAccess = DateTime.UtcNow;
                    return v;
                });
        }

        /// <summary>
        /// 记录未命中
        /// </summary>
        private void RecordMiss(string key)
        {
            Interlocked.Increment(ref _totalMisses);
            
            _statistics.AddOrUpdate(key,
                k => new CacheStatisticsEntry { Misses = 1, LastAccess = DateTime.UtcNow },
                (k, v) =>
                {
                    v.Misses++;
                    v.LastAccess = DateTime.UtcNow;
                    return v;
                });
        }

        /// <summary>
        /// 清理回调
        /// </summary>
        private void CleanupCallback(object? state)
        {
            try
            {
                // 清理死引用
                var deadKeys = _weakCache
                    .Where(kvp => !kvp.Value.IsAlive)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in deadKeys)
                {
                    _weakCache.TryRemove(key, out _);
                }

                // 清理过期统计
                var expiredStats = _statistics
                    .Where(kvp => DateTime.UtcNow - kvp.Value.LastAccess > TimeSpan.FromHours(1))
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in expiredStats)
                {
                    _statistics.TryRemove(key, out _);
                }

                if (deadKeys.Any() || expiredStats.Any())
                {
                    _logger?.LogDebug($"定期清理: 移除 {deadKeys.Count} 个死引用, {expiredStats.Count} 个过期统计");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "缓存清理失败");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            if (_l1Cache is IDisposable disposable)
            {
                disposable.Dispose();
            }
            Clear();
        }

        /// <summary>
        /// 缓存统计条目
        /// </summary>
        private class CacheStatisticsEntry
        {
            public long Hits { get; set; }
            public long Misses { get; set; }
            public DateTime LastAccess { get; set; }
        }
    }

    /// <summary>
    /// 缓存键生成器
    /// </summary>
    public static class CacheKeyGenerator
    {
        /// <summary>
        /// 生成缓存键
        /// </summary>
        public static string Generate(string prefix, params object[] parameters)
        {
            var key = prefix;
            if (parameters?.Length > 0)
            {
                key += "_" + string.Join("_", parameters.Select(p => p?.ToString() ?? "null"));
            }
            return key;
        }

        /// <summary>
        /// 生成类型化缓存键
        /// </summary>
        public static string Generate<T>(string method, params object[] parameters)
        {
            return Generate($"{typeof(T).Name}_{method}", parameters);
        }
    }
}