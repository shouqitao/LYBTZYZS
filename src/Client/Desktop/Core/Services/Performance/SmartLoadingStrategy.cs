using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Configuration;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// UltraThink Phase 5.4: 智能加载策略服务
    /// 提供延迟加载、预测性加载和自适应缓存策略
    /// </summary>
    public interface ISmartLoadingStrategy
    {
        /// <summary>
        /// 延迟加载数据
        /// </summary>
        Task<T> LoadLazilyAsync<T>(string key, Func<Task<T>> loader, SmartLoadingOptions? options = null);

        /// <summary>
        /// 预测性加载
        /// </summary>
        void PreloadPredictively<T>(string key, Func<Task<T>> loader, int priority = 0);

        /// <summary>
        /// 批量加载
        /// </summary>
        Task<Dictionary<string, T>> LoadBatchAsync<T>(Dictionary<string, Func<Task<T>>> loaders, BatchLoadingOptions? options = null);

        /// <summary>
        /// 智能预取
        /// </summary>
        void EnableSmartPrefetch(string category, PrefetchStrategy strategy);

        /// <summary>
        /// 获取加载统计
        /// </summary>
        LoadingStatistics GetStatistics();

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        Task CleanupExpiredAsync();

        /// <summary>
        /// 加载完成事件
        /// </summary>
        event EventHandler<LoadingCompletedEventArgs> LoadingCompleted;
    }

    /// <summary>
    /// 智能加载策略实现
    /// </summary>
    public class SmartLoadingStrategy : ISmartLoadingStrategy, IDisposable
    {
        private readonly ILogger<SmartLoadingStrategy> _logger;
        private readonly IAppConfiguration _configuration;
        private readonly IUIPerformanceOptimizer _performanceOptimizer;
        
        private readonly ConcurrentDictionary<string, CachedItem<object>> _cache = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _loadingSemaphores = new();
        private readonly ConcurrentQueue<PrefetchRequest> _prefetchQueue = new();
        private readonly ConcurrentDictionary<string, SmartLoadingMetrics> _loadingMetrics = new();
        
        private readonly Timer _prefetchTimer;
        private readonly Timer _cleanupTimer;
        private LoadingStatistics _statistics = new();
        private readonly object _statisticsLock = new object();

        public event EventHandler<LoadingCompletedEventArgs>? LoadingCompleted;

        public SmartLoadingStrategy(
            ILogger<SmartLoadingStrategy> logger,
            IAppConfiguration configuration,
            IUIPerformanceOptimizer performanceOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _performanceOptimizer = performanceOptimizer ?? throw new ArgumentNullException(nameof(performanceOptimizer));

            // 启动预取处理定时器（每100ms处理一次）
            _prefetchTimer = new Timer(ProcessPrefetchQueue, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
            
            // 启动清理定时器（每10分钟清理一次）
            _cleanupTimer = new Timer(async _ => await CleanupExpiredAsync(), null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            _logger.LogInformation("智能加载策略服务已启动");
        }

        public async Task<T> LoadLazilyAsync<T>(string key, Func<Task<T>> loader, SmartLoadingOptions? options = null)
        {
            options ??= SmartLoadingOptions.Default;
            
            // 检查缓存
            if (_cache.TryGetValue(key, out var cachedItem) && !IsExpired(cachedItem, options))
            {
                UpdateCacheHit(key);
                return (T)cachedItem.Value;
            }

            // 获取加载信号量，防止重复加载
            var semaphore = _loadingSemaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            
            using var session = _performanceOptimizer.StartUIPerformanceSession($"LazyLoad_{key}");
            
            try
            {
                await semaphore.WaitAsync();
                
                // 双重检查，可能其他线程已经加载了
                if (_cache.TryGetValue(key, out cachedItem) && !IsExpired(cachedItem, options))
                {
                    UpdateCacheHit(key);
                    return (T)cachedItem.Value;
                }

                session.AddMilestone("StartLoading");
                
                var startTime = DateTime.UtcNow;
                var result = await loader();
                var duration = DateTime.UtcNow - startTime;

                session.AddMilestone("LoadingCompleted");

                // 缓存结果
                var newCachedItem = new CachedItem<object>
                {
                    Value = result!,
                    CachedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(options.CacheDuration),
                    AccessCount = 1,
                    LastAccessed = DateTime.UtcNow
                };

                _cache.AddOrUpdate(key, newCachedItem, (k, existing) => newCachedItem);

                // 更新指标
                UpdateLoadingMetrics(key, duration, true, result?.GetType().Name ?? "Unknown");

                session.AddMilestone("CacheUpdated");
                OnLoadingCompleted(key, duration, true, typeof(T).Name);

                _logger.LogDebug("延迟加载完成: {Key}，耗时: {Duration}ms", key, duration.TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                UpdateLoadingMetrics(key, TimeSpan.Zero, false, typeof(T).Name);
                OnLoadingCompleted(key, TimeSpan.Zero, false, typeof(T).Name, ex.Message);
                
                _logger.LogError(ex, "延迟加载失败: {Key}", key);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void PreloadPredictively<T>(string key, Func<Task<T>> loader, int priority = 0)
        {
            if (_cache.ContainsKey(key)) return; // 已存在，无需预加载

            var request = new PrefetchRequest
            {
                Key = key,
                Loader = async () => await loader(),
                Priority = priority,
                RequestedAt = DateTime.UtcNow,
                DataType = typeof(T).Name
            };

            _prefetchQueue.Enqueue(request);
            
            lock (_statisticsLock)
            {
                _statistics.TotalPrefetchRequests++;
            }

            _logger.LogDebug("添加预测性加载请求: {Key}，优先级: {Priority}", key, priority);
        }

        public async Task<Dictionary<string, T>> LoadBatchAsync<T>(Dictionary<string, Func<Task<T>>> loaders, BatchLoadingOptions? options = null)
        {
            options ??= BatchLoadingOptions.Default;
            var results = new Dictionary<string, T>();
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);

            using var session = _performanceOptimizer.StartUIPerformanceSession($"BatchLoad_{typeof(T).Name}_{loaders.Count}");

            foreach (var kvp in loaders)
            {
                tasks.Add(ProcessBatchItem(kvp.Key, kvp.Value, results, semaphore, options));
            }

            session.AddMilestone("BatchTasksCreated");
            session.SetElementCount(loaders.Count);

            await Task.WhenAll(tasks);
            
            session.AddMilestone("BatchLoadingCompleted");

            lock (_statisticsLock)
            {
                _statistics.TotalBatchLoads++;
                _statistics.TotalBatchItems += loaders.Count;
            }

            _logger.LogInformation("批量加载完成: {Count}个项目，类型: {Type}", results.Count, typeof(T).Name);
            return results;
        }

        public void EnableSmartPrefetch(string category, PrefetchStrategy strategy)
        {
            // 这里可以实现更复杂的预取策略
            _logger.LogInformation("启用智能预取: {Category}，策略: {Strategy}", category, strategy);
        }

        public LoadingStatistics GetStatistics()
        {
            lock (_statisticsLock)
            {
                return new LoadingStatistics
                {
                    TotalLoads = _statistics.TotalLoads,
                    CacheHits = _statistics.CacheHits,
                    CacheMisses = _statistics.CacheMisses,
                    AverageLoadTime = _statistics.AverageLoadTime,
                    TotalPrefetchRequests = _statistics.TotalPrefetchRequests,
                    TotalBatchLoads = _statistics.TotalBatchLoads,
                    TotalBatchItems = _statistics.TotalBatchItems,
                    CacheSize = _cache.Count,
                    LoadingMetrics = new Dictionary<string, SmartLoadingMetrics>(_loadingMetrics),
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        public async Task CleanupExpiredAsync()
        {
            try
            {
                var expiredKeys = new List<string>();
                var currentTime = DateTime.UtcNow;

                foreach (var kvp in _cache)
                {
                    if (kvp.Value.ExpiresAt < currentTime)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                    _loadingSemaphores.TryRemove(key, out var semaphore);
                    semaphore?.Dispose();
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("清理过期缓存项: {Count}", expiredKeys.Count);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理过期缓存失败");
            }
        }

        #region 私有方法

        private async Task ProcessBatchItem<T>(string key, Func<Task<T>> loader, Dictionary<string, T> results, 
            SemaphoreSlim semaphore, BatchLoadingOptions options)
        {
            await semaphore.WaitAsync();
            
            try
            {
                var result = await LoadLazilyAsync(key, loader, new SmartLoadingOptions 
                { 
                    CacheDuration = options.CacheDuration 
                });
                
                lock (results)
                {
                    results[key] = result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "批量加载项失败: {Key}", key);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void ProcessPrefetchQueue(object? state)
        {
            var processedCount = 0;
            var maxProcess = 5; // 每次最多处理5个预取请求

            while (_prefetchQueue.TryDequeue(out var request) && processedCount < maxProcess)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await request.Loader();
                        _logger.LogDebug("预取完成: {Key}", request.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "预取失败: {Key}", request.Key);
                    }
                });

                processedCount++;
            }
        }

        private bool IsExpired(CachedItem<object> item, SmartLoadingOptions options)
        {
            return DateTime.UtcNow > item.ExpiresAt;
        }

        private void UpdateCacheHit(string key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                item.AccessCount++;
                item.LastAccessed = DateTime.UtcNow;
            }

            lock (_statisticsLock)
            {
                _statistics.CacheHits++;
                _statistics.TotalLoads++;
            }
        }

        private void UpdateLoadingMetrics(string key, TimeSpan duration, bool success, string dataType)
        {
            var metrics = _loadingMetrics.GetOrAdd(key, _ => new SmartLoadingMetrics
            {
                Key = key,
                DataType = dataType
            });

            metrics.TotalLoads++;
            if (success)
            {
                metrics.SuccessfulLoads++;
                metrics.TotalLoadTime = metrics.TotalLoadTime.Add(duration);
                metrics.AverageLoadTime = TimeSpan.FromMilliseconds(
                    metrics.TotalLoadTime.TotalMilliseconds / metrics.SuccessfulLoads);
                
                if (duration > metrics.MaxLoadTime)
                    metrics.MaxLoadTime = duration;
            }
            else
            {
                metrics.FailedLoads++;
            }

            metrics.LastAccessed = DateTime.UtcNow;

            lock (_statisticsLock)
            {
                _statistics.CacheMisses++;
                _statistics.TotalLoads++;
                
                if (success)
                {
                    var totalTime = _statistics.AverageLoadTime.TotalMilliseconds * (_statistics.TotalLoads - 1) + duration.TotalMilliseconds;
                    _statistics.AverageLoadTime = TimeSpan.FromMilliseconds(totalTime / _statistics.TotalLoads);
                }
            }
        }

        private void OnLoadingCompleted(string key, TimeSpan duration, bool success, string dataType, string? errorMessage = null)
        {
            var args = new LoadingCompletedEventArgs
            {
                Key = key,
                Duration = duration,
                Success = success,
                DataType = dataType,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };

            LoadingCompleted?.Invoke(this, args);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _prefetchTimer?.Dispose();
            _cleanupTimer?.Dispose();
            
            foreach (var semaphore in _loadingSemaphores.Values)
            {
                semaphore?.Dispose();
            }
            
            _loadingSemaphores.Clear();
            _cache.Clear();
        }

        #endregion
    }

    #region 支持类型

    /// <summary>
    /// 智能加载选项
    /// </summary>
    public class SmartLoadingOptions
    {
        public static SmartLoadingOptions Default => new() { CacheDuration = TimeSpan.FromMinutes(30) };
        
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);
        public int RetryCount { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public bool UseBackgroundLoading { get; set; } = true;
    }

    /// <summary>
    /// 批量加载选项
    /// </summary>
    public class BatchLoadingOptions
    {
        public static BatchLoadingOptions Default => new() { MaxConcurrency = 5 };
        
        public int MaxConcurrency { get; set; } = 5;
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// 预取策略
    /// </summary>
    public enum PrefetchStrategy
    {
        Immediate,
        OnIdle,
        Predictive,
        UserBehaviorBased
    }

    /// <summary>
    /// 预取请求
    /// </summary>
    internal class PrefetchRequest
    {
        public string Key { get; set; } = string.Empty;
        public Func<Task<object>> Loader { get; set; } = null!;
        public int Priority { get; set; }
        public DateTime RequestedAt { get; set; }
        public string DataType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 缓存项
    /// </summary>
    public class CachedItem<T>
    {
        public T Value { get; set; } = default!;
        public DateTime CachedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
    }

    /// <summary>
    /// 加载统计
    /// </summary>
    public class LoadingStatistics
    {
        public long TotalLoads { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double CacheHitRate => TotalLoads > 0 ? (double)CacheHits / TotalLoads * 100 : 0;
        public TimeSpan AverageLoadTime { get; set; }
        public long TotalPrefetchRequests { get; set; }
        public long TotalBatchLoads { get; set; }
        public long TotalBatchItems { get; set; }
        public int CacheSize { get; set; }
        public Dictionary<string, SmartLoadingMetrics> LoadingMetrics { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// 智能加载指标
    /// </summary>
    public class SmartLoadingMetrics
    {
        public string Key { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public long TotalLoads { get; set; }
        public long SuccessfulLoads { get; set; }
        public long FailedLoads { get; set; }
        public TimeSpan TotalLoadTime { get; set; }
        public TimeSpan AverageLoadTime { get; set; }
        public TimeSpan MaxLoadTime { get; set; }
        public DateTime LastAccessed { get; set; }
        public double SuccessRate => TotalLoads > 0 ? (double)SuccessfulLoads / TotalLoads * 100 : 0;
    }

    /// <summary>
    /// 加载完成事件参数
    /// </summary>
    public class LoadingCompletedEventArgs : EventArgs
    {
        public string Key { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string DataType { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}