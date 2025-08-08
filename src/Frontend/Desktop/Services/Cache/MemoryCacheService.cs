using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Cache;

namespace LYBT.WPF.Client.Services.Cache
{
    /// <summary>
    /// 基于 Microsoft.Extensions.Caching.Memory 的企业级内存缓存服务
    /// </summary>
    public class MemoryCacheService : ICacheService, IDisposable
    {
        #region 私有字段

        private readonly IMemoryCache _memoryCache;
        private readonly CacheOptions _options;
        private readonly ILogger<MemoryCacheService> _logger;
        
        private readonly ConcurrentDictionary<string, CacheEntryBase> _entries;
        private readonly ConcurrentDictionary<string, List<string>> _dependencies;
        private readonly CacheStatistics _statistics;
        private readonly Timer? _cleanupTimer;
        
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化内存缓存服务
        /// </summary>
        /// <param name="memoryCache">内存缓存实例</param>
        /// <param name="options">缓存选项</param>
        /// <param name="logger">日志记录器</param>
        public MemoryCacheService(
            IMemoryCache memoryCache,
            CacheOptions options,
            ILogger<MemoryCacheService> logger)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 验证配置
            var validation = _options.Validate();
            if (!validation.IsValid)
            {
                throw new ArgumentException($"缓存配置无效: {validation.GetErrorSummary()}");
            }

            _entries = new ConcurrentDictionary<string, CacheEntryBase>();
            _dependencies = new ConcurrentDictionary<string, List<string>>();
            _statistics = new CacheStatistics
            {
                StartTime = DateTime.Now
            };

            // 启动后台清理定时器
            if (_options.EnableBackgroundCleanup)
            {
                _cleanupTimer = new Timer(PerformCleanup, null, (int)_options.CleanupInterval.TotalMilliseconds, (int)_options.CleanupInterval.TotalMilliseconds);
            }

            _logger.LogInformation("内存缓存服务已启动，配置: 最大项数={MaxItems}, 最大内存={MaxMemory}MB, 清理间隔={CleanupInterval}",
                _options.MaxItemCount, _options.MaxMemorySize / 1024 / 1024, _options.CleanupInterval);
        }

        #endregion

        #region 同步方法

        /// <summary>
        /// 获取缓存项
        /// </summary>
        public T? Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("缓存键不能为空", nameof(key));

            try
            {
                var result = _memoryCache.Get<T>(key);
                
                // 更新统计
                if (_options.EnableStatistics)
                {
                    lock (_lockObject)
                    {
                        if (result != null)
                        {
                            _statistics.HitCount++;
                            
                            // 更新访问统计
                            if (_entries.TryGetValue(key, out var entry))
                            {
                                entry.UpdateAccessStats();
                            }
                        }
                        else
                        {
                            _statistics.MissCount++;
                        }
                    }
                }

                if (_options.EnableDetailedLogging)
                {
                    _logger.LogDebug("缓存获取 键={Key}, 结果={Result}", key, result != null ? "命中" : "未命中");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缓存项时发生异常，键={Key}", key);
                return default;
            }
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            Set(key, value, CachePolicy.Sliding(expiration));
        }

        /// <summary>
        /// 设置缓存项（使用缓存策略）
        /// </summary>
        public void Set<T>(string key, T value, CachePolicy policy)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("缓存键不能为空", nameof(key));

            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            try
            {
                // 检查是否需要淘汰
                CheckAndEvictIfNeeded();

                var options = new MemoryCacheEntryOptions();

                // 设置过期时间
                if (policy.AbsoluteExpiration.HasValue)
                {
                    options.AbsoluteExpiration = policy.AbsoluteExpiration.Value;
                }

                if (policy.SlidingExpiration.HasValue)
                {
                    options.SlidingExpiration = policy.SlidingExpiration.Value;
                }

                // 设置优先级
                options.Priority = policy.Priority switch
                {
                    CachePriority.Low => CacheItemPriority.Low,
                    CachePriority.Normal => CacheItemPriority.Normal,
                    CachePriority.High => CacheItemPriority.High,
                    CachePriority.NeverRemove => CacheItemPriority.NeverRemove,
                    _ => CacheItemPriority.Normal
                };

                // 设置移除回调
                options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    OnItemRemoved(evictedKey.ToString() ?? string.Empty, reason);
                });

                // 存储到内存缓存
                _memoryCache.Set(key, value, options);

                // 维护内部跟踪
                var entry = CacheEntry<T>.Create(key, value, policy);
                _entries.AddOrUpdate(key, entry, (k, v) => entry);

                // 处理依赖关系
                if (policy.Dependencies != null && policy.Dependencies.Count > 0 && _options.EnableDependencyInvalidation)
                {
                    foreach (var dependency in policy.Dependencies)
                    {
                        _dependencies.AddOrUpdate(dependency,
                            new List<string> { key },
                            (k, list) =>
                            {
                                if (!list.Contains(key))
                                    list.Add(key);
                                return list;
                            });
                    }
                }

                // 更新统计
                if (_options.EnableStatistics)
                {
                    lock (_lockObject)
                    {
                        _statistics.ItemCount = _entries.Count;
                        _statistics.EstimatedMemoryUsage += entry.EstimatedSize;
                    }
                }

                if (_options.EnableDetailedLogging)
                {
                    var expiration = policy.SlidingExpiration?.ToString() ?? policy.AbsoluteExpiration?.ToString() ?? "未设置";
                    _logger.LogDebug("缓存设置 键={Key}, 过期={Expiration}, 大小={Size}字节", 
                        key, expiration, entry.EstimatedSize);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置缓存项时发生异常，键={Key}", key);
            }
        }

        /// <summary>
        /// 尝试获取缓存项
        /// </summary>
        public bool TryGet<T>(string key, out T? value)
        {
            value = default;

            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                return _memoryCache.TryGetValue(key, out value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "尝试获取缓存项时发生异常，键={Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                _memoryCache.Remove(key);
                
                var removed = _entries.TryRemove(key, out var entry);
                
                // 清理依赖关系
                if (_options.EnableDependencyInvalidation)
                {
                    CleanupDependencies(key);
                }

                // 更新统计
                if (removed && _options.EnableStatistics)
                {
                    lock (_lockObject)
                    {
                        _statistics.ItemCount = _entries.Count;
                        _statistics.EstimatedMemoryUsage -= entry!.EstimatedSize;
                    }
                }

                if (_options.EnableDetailedLogging)
                {
                    _logger.LogDebug("缓存移除 键={Key}, 结果={Result}", key, removed ? "成功" : "失败");
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除缓存项时发生异常，键={Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 检查缓存项是否存在
        /// </summary>
        public bool Exists(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return _entries.ContainsKey(key) && _memoryCache.TryGetValue(key, out _);
        }

        #endregion

        #region 异步方法

        /// <summary>
        /// 异步获取或创建缓存项
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
        {
            return await GetOrCreateAsync(key, factory, CachePolicy.Sliding(expiration));
        }

        /// <summary>
        /// 异步获取或创建缓存项（使用缓存策略）
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, CachePolicy policy)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("缓存键不能为空", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // 先尝试从缓存获取
            if (TryGet<T>(key, out var cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            try
            {
                // 执行工厂方法创建新值
                var newValue = await factory();
                
                // 存储到缓存
                Set(key, newValue, policy);
                
                return newValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "异步创建缓存项时发生异常，键={Key}", key);
                throw;
            }
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量设置缓存项
        /// </summary>
        public void SetMany(Dictionary<string, object> items, TimeSpan expiration)
        {
            if (items == null || items.Count == 0)
                return;

            var policy = CachePolicy.Sliding(expiration);
            
            foreach (var item in items)
            {
                Set(item.Key, item.Value, policy);
            }
        }

        /// <summary>
        /// 批量获取缓存项
        /// </summary>
        public Dictionary<string, object?> GetMany(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, object?>();
            
            if (keys == null)
                return result;

            foreach (var key in keys)
            {
                if (_memoryCache.TryGetValue(key, out var value))
                {
                    result[key] = value;
                }
                else
                {
                    result[key] = null;
                }
            }

            return result;
        }

        /// <summary>
        /// 批量移除缓存项
        /// </summary>
        public int RemoveMany(IEnumerable<string> keys)
        {
            if (keys == null)
                return 0;

            int removedCount = 0;
            
            foreach (var key in keys)
            {
                if (Remove(key))
                {
                    removedCount++;
                }
            }

            return removedCount;
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 按模式移除缓存项
        /// </summary>
        public int RemoveByPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return 0;

            try
            {
                // 将通配符模式转换为正则表达式
                var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

                var keysToRemove = _entries.Keys
                    .Where(key => regex.IsMatch(key))
                    .ToList();

                return RemoveMany(keysToRemove);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按模式移除缓存项时发生异常，模式={Pattern}", pattern);
                return 0;
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            try
            {
                // 获取所有键并移除
                var allKeys = _entries.Keys.ToList();
                RemoveMany(allKeys);

                if (_options.EnableDetailedLogging)
                {
                    _logger.LogInformation("已清空所有缓存，共移除 {Count} 项", allKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空缓存时发生异常");
            }
        }

        /// <summary>
        /// 清空指定分区的缓存
        /// </summary>
        public void ClearPartition(string partition)
        {
            if (string.IsNullOrEmpty(partition))
                return;

            try
            {
                var partitionKeys = _entries.Values
                    .Where(entry => entry.Partition == partition)
                    .Select(entry => entry.Key)
                    .ToList();

                var removedCount = RemoveMany(partitionKeys);

                if (_options.EnableDetailedLogging)
                {
                    _logger.LogInformation("已清空分区 {Partition} 的缓存，共移除 {Count} 项", partition, removedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空分区缓存时发生异常，分区={Partition}", partition);
            }
        }

        /// <summary>
        /// 触发缓存清理
        /// </summary>
        public int Cleanup()
        {
            return PerformCleanupInternal();
        }

        #endregion

        #region 统计与监控

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                _statistics.ItemCount = _entries.Count;
                return new CacheStatistics
                {
                    HitCount = _statistics.HitCount,
                    MissCount = _statistics.MissCount,
                    ItemCount = _statistics.ItemCount,
                    EstimatedMemoryUsage = _statistics.EstimatedMemoryUsage,
                    StartTime = _statistics.StartTime,
                    LastCleanupTime = _statistics.LastCleanupTime,
                    CleanupCount = _statistics.CleanupCount
                };
            }
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            lock (_lockObject)
            {
                _statistics.HitCount = 0;
                _statistics.MissCount = 0;
                _statistics.StartTime = DateTime.Now;
            }

            _logger.LogInformation("缓存统计信息已重置");
        }

        /// <summary>
        /// 获取所有缓存键
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return _entries.Keys.ToList();
        }

        /// <summary>
        /// 获取缓存项数量
        /// </summary>
        public int Count => _entries.Count;

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查并在需要时执行淘汰
        /// </summary>
        private void CheckAndEvictIfNeeded()
        {
            var itemCount = _entries.Count;
            var memoryUsage = _statistics.EstimatedMemoryUsage;

            // 检查项数阈值
            if (itemCount >= _options.MaxItemCount * _options.LruEvictionThreshold)
            {
                EvictByLru();
            }

            // 检查内存阈值
            if (memoryUsage >= _options.MaxMemorySize * _options.MemoryEvictionThreshold)
            {
                EvictByMemoryPressure();
            }
        }

        /// <summary>
        /// 基于LRU策略淘汰
        /// </summary>
        private void EvictByLru()
        {
            try
            {
                var itemsToEvict = (int)(_entries.Count * _options.EvictionPercentage);
                if (itemsToEvict == 0) itemsToEvict = 1;

                var lruItems = _entries.Values
                    .Where(e => e.Priority != CachePriority.NeverRemove)
                    .OrderBy(e => e.LastAccessedAt)
                    .ThenBy(e => e.Priority)
                    .Take(itemsToEvict)
                    .Select(e => e.Key)
                    .ToList();

                var evictedCount = RemoveMany(lruItems);

                if (_options.EnableDetailedLogging)
                {
                    _logger.LogDebug("LRU淘汰完成，移除 {Count} 项", evictedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LRU淘汰时发生异常");
            }
        }

        /// <summary>
        /// 基于内存压力淘汰
        /// </summary>
        private void EvictByMemoryPressure()
        {
            try
            {
                var targetMemory = _options.MaxMemorySize * (1 - _options.EvictionPercentage);
                long freedMemory = 0;

                var candidatesForEviction = _entries.Values
                    .Where(e => e.Priority != CachePriority.NeverRemove)
                    .OrderByDescending(e => e.EstimatedSize) // 先移除大的
                    .ThenBy(e => e.LastAccessedAt) // 然后按LRU
                    .ToList();

                var keysToRemove = new List<string>();
                
                foreach (var entry in candidatesForEviction)
                {
                    keysToRemove.Add(entry.Key);
                    freedMemory += entry.EstimatedSize;
                    
                    if (_statistics.EstimatedMemoryUsage - freedMemory <= targetMemory)
                        break;
                }

                var evictedCount = RemoveMany(keysToRemove);

                if (_options.EnableDetailedLogging)
                {
                    _logger.LogDebug("内存压力淘汰完成，移除 {Count} 项，释放 {Memory}MB", 
                        evictedCount, freedMemory / 1024 / 1024);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "内存压力淘汰时发生异常");
            }
        }

        /// <summary>
        /// 执行清理（移除过期项）
        /// </summary>
        private void PerformCleanup(object? state)
        {
            try
            {
                var cleanedCount = PerformCleanupInternal();
                
                if (_options.EnableDetailedLogging && cleanedCount > 0)
                {
                    _logger.LogDebug("缓存清理完成，移除 {Count} 个过期项", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行缓存清理时发生异常");
            }
        }

        /// <summary>
        /// 内部清理实现（返回清理数量）
        /// </summary>
        private int PerformCleanupInternal()
        {
            try
            {
                var expiredKeys = _entries.Values
                    .Where(entry => entry.IsExpired)
                    .Select(entry => entry.Key)
                    .ToList();

                var cleanedCount = RemoveMany(expiredKeys);

                // 更新统计
                if (_options.EnableStatistics)
                {
                    lock (_lockObject)
                    {
                        _statistics.LastCleanupTime = DateTime.Now;
                        _statistics.CleanupCount++;
                    }
                }

                if (_options.EnableDetailedLogging && cleanedCount > 0)
                {
                    _logger.LogDebug("缓存清理完成，移除 {Count} 个过期项", cleanedCount);
                }

                return cleanedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行缓存清理时发生异常");
                return 0;
            }
        }

        /// <summary>
        /// 项被移除时的回调
        /// </summary>
        private void OnItemRemoved(string key, EvictionReason reason)
        {
            // 从内部跟踪中移除
            _entries.TryRemove(key, out var entry);

            // 清理依赖关系
            if (_options.EnableDependencyInvalidation)
            {
                CleanupDependencies(key);
                
                // 如果是依赖失效，需要移除相关项
                if (reason == EvictionReason.TokenExpired && _dependencies.TryGetValue(key, out var dependentKeys))
                {
                    foreach (var dependentKey in dependentKeys.ToList())
                    {
                        Remove(dependentKey);
                    }
                }
            }

            // 更新统计
            if (entry != null && _options.EnableStatistics)
            {
                lock (_lockObject)
                {
                    _statistics.EstimatedMemoryUsage -= entry.EstimatedSize;
                }
            }

            if (_options.EnableDetailedLogging)
            {
                _logger.LogDebug("缓存项被移除 键={Key}, 原因={Reason}", key, reason);
            }
        }

        /// <summary>
        /// 清理依赖关系
        /// </summary>
        private void CleanupDependencies(string key)
        {
            // 移除该键作为依赖的记录
            _dependencies.TryRemove(key, out _);

            // 从其他依赖列表中移除该键
            foreach (var kvp in _dependencies.ToList())
            {
                if (kvp.Value.Contains(key))
                {
                    kvp.Value.Remove(key);
                    if (kvp.Value.Count == 0)
                    {
                        _dependencies.TryRemove(kvp.Key, out _);
                    }
                }
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _cleanupTimer?.Dispose();
                Clear();
                
                _logger.LogInformation("内存缓存服务已释放");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放内存缓存服务时发生异常");
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion
    }
}