using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// 智能加载服务 - UltraThink用户体验优化
    /// 提供预测性预加载、智能缓存和资源优先级管理
    /// </summary>
    public class SmartLoadingService : ISmartLoadingService
    {
        private readonly ILogger<SmartLoadingService> _logger;
        private readonly IMemoryCache _cache;
        // AI预测功能已删除 - UltraThink简化
        private readonly ISmartConcurrencyManager _concurrencyManager;
        
        // 加载队列和优先级管理
        private readonly ConcurrentDictionary<string, LoadingTask> _loadingTasks;
        private readonly PriorityQueue<LoadRequest, int> _loadQueue;
        private readonly SemaphoreSlim _loadSemaphore;
        
        // 性能监控
        private readonly ConcurrentDictionary<string, LoadingMetrics> _metrics;
        private readonly Timer _metricsTimer;
        
        // 配置
        private readonly SmartLoadingConfiguration _config;

        public SmartLoadingService(
            ILogger<SmartLoadingService> logger,
            IMemoryCache cache,
            ISmartConcurrencyManager concurrencyManager,
            SmartLoadingConfiguration config)
        {
            _logger = logger;
            _cache = cache;
            _concurrencyManager = concurrencyManager;
            _config = config;
            
            _loadingTasks = new ConcurrentDictionary<string, LoadingTask>();
            _loadQueue = new PriorityQueue<LoadRequest, int>();
            _loadSemaphore = new SemaphoreSlim(_config.MaxConcurrentLoads);
            _metrics = new ConcurrentDictionary<string, LoadingMetrics>();
            
            // 启动性能监控
            _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// 智能加载资源
        /// </summary>
        public async Task<T> LoadAsync<T>(
            string resourceKey,
            Func<CancellationToken, Task<T>> loadFunc,
            LoadingOptions options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= LoadingOptions.Default;
            
            try
            {
                // 1. 检查缓存
                if (_cache.TryGetValue<T>(resourceKey, out var cachedValue))
                {
                    _logger.LogDebug("从缓存加载资源: {ResourceKey}", resourceKey);
                    UpdateMetrics(resourceKey, true, 0);
                    
                    // 异步刷新缓存（如果需要）
                    if (ShouldRefreshCache(resourceKey, options))
                    {
                        _ = RefreshCacheAsync(resourceKey, loadFunc, options);
                    }
                    
                    return cachedValue;
                }
                
                // 2. 检查是否正在加载
                if (_loadingTasks.TryGetValue(resourceKey, out var existingTask))
                {
                    _logger.LogDebug("等待正在进行的加载: {ResourceKey}", resourceKey);
                    return await WaitForExistingLoadAsync<T>(existingTask, cancellationToken);
                }
                
                // 3. 创建新的加载任务
                var loadingTask = new LoadingTask
                {
                    Key = resourceKey,
                    StartTime = DateTime.UtcNow,
                    CompletionSource = new TaskCompletionSource<object>()
                };
                
                if (!_loadingTasks.TryAdd(resourceKey, loadingTask))
                {
                    // 并发添加失败，等待已存在的任务
                    if (_loadingTasks.TryGetValue(resourceKey, out existingTask))
                    {
                        return await WaitForExistingLoadAsync<T>(existingTask, cancellationToken);
                    }
                }
                
                // 4. 执行智能加载
                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    // 获取加载优先级
                    var priority = CalculatePriority(resourceKey, options);
                    
                    // 等待并发限制
                    await _loadSemaphore.WaitAsync(cancellationToken);
                    
                    try
                    {
                        // AI预测功能已删除 - UltraThink简化
                        
                        // 执行加载
                        _logger.LogDebug("开始加载资源: {ResourceKey}, 优先级: {Priority}", resourceKey, priority);
                        var result = await loadFunc(cancellationToken);
                        
                        // 缓存结果
                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            SlidingExpiration = options.CacheDuration ?? TimeSpan.FromMinutes(5),
                            Priority = options.CachePriority
                        };
                        
                        if (options.CacheSize.HasValue)
                        {
                            cacheOptions.Size = options.CacheSize.Value;
                        }
                        
                        _cache.Set(resourceKey, result, cacheOptions);
                        
                        // 完成任务
                        loadingTask.CompletionSource.SetResult(result);
                        
                        // 更新指标
                        stopwatch.Stop();
                        UpdateMetrics(resourceKey, false, stopwatch.ElapsedMilliseconds);
                        
                        // AI预测预加载功能已删除 - UltraThink简化
                        
                        _logger.LogInformation("成功加载资源: {ResourceKey}, 耗时: {ElapsedMs}ms", 
                            resourceKey, stopwatch.ElapsedMilliseconds);
                        
                        return result;
                    }
                    finally
                    {
                        _loadSemaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    loadingTask.CompletionSource.SetException(ex);
                    _logger.LogError(ex, "加载资源失败: {ResourceKey}", resourceKey);
                    throw;
                }
                finally
                {
                    _loadingTasks.TryRemove(resourceKey, out _);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("加载被取消: {ResourceKey}", resourceKey);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载资源时发生意外错误: {ResourceKey}", resourceKey);
                throw;
            }
        }

        /// <summary>
        /// 批量预加载
        /// </summary>
        public async Task PreloadBatchAsync(
            IEnumerable<string> resourceKeys,
            Func<string, CancellationToken, Task<object>> loadFunc,
            CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();
            
            foreach (var key in resourceKeys)
            {
                // 检查是否已缓存
                if (_cache.TryGetValue(key, out _))
                {
                    continue;
                }
                
                // 异步预加载
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await LoadAsync(
                            key,
                            ct => loadFunc(key, ct),
                            new LoadingOptions { Priority = LoadPriority.Low },
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "预加载失败: {ResourceKey}", key);
                    }
                }, cancellationToken);
                
                tasks.Add(task);
                
                // 限制并发预加载数量
                if (tasks.Count >= _config.MaxConcurrentPreloads)
                {
                    await Task.WhenAny(tasks);
                    tasks.RemoveAll(t => t.IsCompleted);
                }
            }
            
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 智能预加载 - 已删除AI预测功能
        /// </summary>
        public async Task SmartPreloadAsync(string currentContext, CancellationToken cancellationToken = default)
        {
            // AI预测智能预加载功能已删除 - UltraThink简化
            await Task.CompletedTask;
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        public async Task CleanupExpiredCacheAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // 获取缓存统计
                    var stats = _cache.GetCurrentStatistics();
                    
                    if (stats != null)
                    {
                        _logger.LogInformation("缓存统计 - 条目数: {EntryCount}, 命中率: {HitRatio:P2}",
                            stats.CurrentEntryCount,
                            stats.TotalHits / (double)(stats.TotalHits + stats.TotalMisses));
                    }
                    
                    // 缓存压缩功能已移除 - .NET现代版本会自动处理内存压力和过期条目
                    // MemoryCache会在内存不足时自动清理最少使用的条目
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理缓存时出错");
                }
            });
        }

        /// <summary>
        /// 获取加载性能报告
        /// </summary>
        public LoadingPerformanceReport GetPerformanceReport()
        {
            var metrics = _metrics.Values.ToList();
            
            if (!metrics.Any())
            {
                return new LoadingPerformanceReport();
            }
            
            return new LoadingPerformanceReport
            {
                TotalLoads = metrics.Sum(m => m.LoadCount),
                CacheHits = metrics.Sum(m => m.CacheHits),
                CacheHitRate = metrics.Sum(m => m.CacheHits) / (double)metrics.Sum(m => m.LoadCount),
                AverageLoadTime = metrics.Average(m => m.AverageLoadTime),
                MaxLoadTime = metrics.Max(m => m.MaxLoadTime),
                MinLoadTime = metrics.Min(m => m.MinLoadTime),
                TopResources = metrics
                    .OrderByDescending(m => m.LoadCount)
                    .Take(10)
                    .Select(m => new ResourceMetrics
                    {
                        ResourceKey = m.ResourceKey,
                        LoadCount = m.LoadCount,
                        AverageLoadTime = m.AverageLoadTime,
                        CacheHitRate = m.CacheHits / (double)m.LoadCount
                    })
                    .ToList()
            };
        }

        #region Private Methods

        private async Task<T> WaitForExistingLoadAsync<T>(LoadingTask task, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() => task.CompletionSource.TrySetCanceled()))
            {
                var result = await task.CompletionSource.Task;
                return (T)result;
            }
        }

        private bool ShouldRefreshCache(string resourceKey, LoadingOptions options)
        {
            if (!options.EnableBackgroundRefresh)
            {
                return false;
            }
            
            // 基于访问频率和最后更新时间决定
            if (_metrics.TryGetValue(resourceKey, out var metrics))
            {
                var accessRate = metrics.LoadCount / (DateTime.UtcNow - metrics.FirstLoadTime).TotalMinutes;
                return accessRate > _config.RefreshThreshold;
            }
            
            return false;
        }

        private async Task RefreshCacheAsync<T>(
            string resourceKey,
            Func<CancellationToken, Task<T>> loadFunc,
            LoadingOptions options)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1)); // 短暂延迟避免立即刷新
                    
                    var result = await loadFunc(CancellationToken.None);
                    
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = options.CacheDuration ?? TimeSpan.FromMinutes(5),
                        Priority = options.CachePriority
                    };
                    
                    _cache.Set(resourceKey, result, cacheOptions);
                    
                    _logger.LogDebug("后台刷新缓存成功: {ResourceKey}", resourceKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "后台刷新缓存失败: {ResourceKey}", resourceKey);
                }
            });
        }

        private int CalculatePriority(string resourceKey, LoadingOptions options)
        {
            var basePriority = options.Priority switch
            {
                LoadPriority.Critical => 0,
                LoadPriority.High => 10,
                LoadPriority.Normal => 50,
                LoadPriority.Low => 100,
                _ => 50
            };
            
            // AI行为分析功能已删除 - UltraThink简化
            return basePriority;
        }

        // TriggerPredictivePreloadAsync方法已删除 - UltraThink简化

        private void UpdateMetrics(string resourceKey, bool isCacheHit, long loadTimeMs)
        {
            _metrics.AddOrUpdate(resourceKey,
                key => new LoadingMetrics
                {
                    ResourceKey = key,
                    LoadCount = 1,
                    CacheHits = isCacheHit ? 1 : 0,
                    TotalLoadTime = isCacheHit ? 0 : loadTimeMs,
                    AverageLoadTime = isCacheHit ? 0 : loadTimeMs,
                    MaxLoadTime = loadTimeMs,
                    MinLoadTime = loadTimeMs,
                    FirstLoadTime = DateTime.UtcNow,
                    LastLoadTime = DateTime.UtcNow
                },
                (key, existing) =>
                {
                    existing.LoadCount++;
                    if (isCacheHit)
                    {
                        existing.CacheHits++;
                    }
                    else
                    {
                        existing.TotalLoadTime += loadTimeMs;
                        existing.AverageLoadTime = existing.TotalLoadTime / (existing.LoadCount - existing.CacheHits);
                        existing.MaxLoadTime = Math.Max(existing.MaxLoadTime, loadTimeMs);
                        existing.MinLoadTime = Math.Min(existing.MinLoadTime, loadTimeMs);
                    }
                    existing.LastLoadTime = DateTime.UtcNow;
                    return existing;
                });
        }

        private void CollectMetrics(object state)
        {
            try
            {
                var report = GetPerformanceReport();
                
                _logger.LogInformation("加载性能报告 - 总加载: {TotalLoads}, 缓存命中率: {HitRate:P2}, 平均加载时间: {AvgTime:F2}ms",
                    report.TotalLoads,
                    report.CacheHitRate,
                    report.AverageLoadTime);
                
                // 清理旧的指标数据
                var cutoffTime = DateTime.UtcNow.AddHours(-1);
                var keysToRemove = _metrics
                    .Where(kvp => kvp.Value.LastLoadTime < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in keysToRemove)
                {
                    _metrics.TryRemove(key, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "收集性能指标时出错");
            }
        }

        #endregion

        public void Dispose()
        {
            _metricsTimer?.Dispose();
            _loadSemaphore?.Dispose();
        }
    }

    #region Supporting Classes

    public interface ISmartLoadingService
    {
        Task<T> LoadAsync<T>(string resourceKey, Func<CancellationToken, Task<T>> loadFunc, 
            LoadingOptions options = null, CancellationToken cancellationToken = default);
        Task PreloadBatchAsync(IEnumerable<string> resourceKeys, 
            Func<string, CancellationToken, Task<object>> loadFunc, CancellationToken cancellationToken = default);
        Task SmartPreloadAsync(string currentContext, CancellationToken cancellationToken = default);
        Task CleanupExpiredCacheAsync();
        LoadingPerformanceReport GetPerformanceReport();
    }

    public class LoadingOptions
    {
        public LoadPriority Priority { get; set; } = LoadPriority.Normal;
        public TimeSpan? CacheDuration { get; set; }
        public CacheItemPriority CachePriority { get; set; } = CacheItemPriority.Normal;
        public long? CacheSize { get; set; }
        public bool EnableBackgroundRefresh { get; set; } = true;
        public bool EnablePredictivePreload { get; set; } = true;
        
        public static LoadingOptions Default => new LoadingOptions();
    }

    public enum LoadPriority
    {
        Critical = 0,
        High = 1,
        Normal = 2,
        Low = 3
    }

    public class LoadingTask
    {
        public string Key { get; set; }
        public DateTime StartTime { get; set; }
        public TaskCompletionSource<object> CompletionSource { get; set; }
    }

    public class LoadRequest
    {
        public string ResourceKey { get; set; }
        public Func<CancellationToken, Task<object>> LoadFunc { get; set; }
        public LoadingOptions Options { get; set; }
        public TaskCompletionSource<object> CompletionSource { get; set; }
    }

    public class LoadingMetrics
    {
        public string ResourceKey { get; set; }
        public int LoadCount { get; set; }
        public int CacheHits { get; set; }
        public long TotalLoadTime { get; set; }
        public double AverageLoadTime { get; set; }
        public long MaxLoadTime { get; set; }
        public long MinLoadTime { get; set; }
        public DateTime FirstLoadTime { get; set; }
        public DateTime LastLoadTime { get; set; }
    }

    public class LoadingPerformanceReport
    {
        public int TotalLoads { get; set; }
        public int CacheHits { get; set; }
        public double CacheHitRate { get; set; }
        public double AverageLoadTime { get; set; }
        public long MaxLoadTime { get; set; }
        public long MinLoadTime { get; set; }
        public List<ResourceMetrics> TopResources { get; set; } = new List<ResourceMetrics>();
    }

    public class ResourceMetrics
    {
        public string ResourceKey { get; set; }
        public int LoadCount { get; set; }
        public double AverageLoadTime { get; set; }
        public double CacheHitRate { get; set; }
    }

    public class SmartLoadingConfiguration
    {
        public int MaxConcurrentLoads { get; set; } = 10;
        public int MaxConcurrentPreloads { get; set; } = 5;
        public int MaxPreloadItems { get; set; } = 20;
        public double RefreshThreshold { get; set; } = 0.5; // 每分钟访问次数
    }

    #endregion
}