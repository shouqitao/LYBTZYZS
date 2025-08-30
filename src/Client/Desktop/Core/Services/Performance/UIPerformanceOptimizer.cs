using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Configuration;

namespace LYBT.Desktop.Core.Services.Performance
{

    /// <summary>
    /// UI性能优化器实现
    /// </summary>
    public class UIPerformanceOptimizer : IUIPerformanceOptimizer
    {
        private readonly ILogger<UIPerformanceOptimizer> _logger;
        private readonly Dispatcher _dispatcher;
        private readonly ConcurrentDictionary<string, CacheItem<object>> _dataCache = new();
        private readonly ConcurrentQueue<(Action Action, int Priority)> _pendingUIUpdates = new();
        private readonly ConcurrentDictionary<string, UIPerformanceSession> _activeSessions = new();
        private readonly Timer _batchUpdateTimer;
        private readonly Timer _cacheCleanupTimer;
        
        private UIPerformanceThresholds _thresholds = new();
        private UIPerformanceStatistics _statistics = new();
        private readonly object _statisticsLock = new object();

        public event EventHandler<UIPerformanceWarningEventArgs>? PerformanceWarning;

        public UIPerformanceOptimizer(ILogger<UIPerformanceOptimizer> logger)
        {
            _logger = logger;
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            
            // 启动批量更新定时器（每50ms执行一次）
            _batchUpdateTimer = new Timer(ProcessBatchUpdates, null, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
            
            // 启动缓存清理定时器（每5分钟执行一次）
            _cacheCleanupTimer = new Timer(CleanupExpiredCache, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("UI性能优化器已启动");
        }

        /// <summary>
        /// 执行UI性能友好的长时间操作
        /// </summary>
        public async Task ExecuteLongRunningOperationAsync<T>(
            Func<Task<T>> operation,
            Action<T> onCompleted,
            Action<Exception>? onError = null,
            string loadingMessage = "正在加载...")
        {
            try
            {
                // 降低UI线程优先级，让后台操作有更多CPU时间
                var originalPriority = _dispatcher.Thread.Priority;
                _dispatcher.Thread.Priority = System.Threading.ThreadPriority.BelowNormal;

                // 显示加载指示器
                await _dispatcher.InvokeAsync(() =>
                {
                    // 这里可以集成全局加载指示器
                    _logger.LogDebug("开始长时间操作: {Message}", loadingMessage);
                }, DispatcherPriority.Normal);

                // 在后台线程执行操作
                var result = await Task.Run(async () =>
                {
                    try
                    {
                        return await operation();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "长时间操作执行失败");
                        throw;
                    }
                });

                // 恢复UI线程优先级
                _dispatcher.Thread.Priority = originalPriority;

                // 在UI线程处理结果
                await _dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        onCompleted(result);
                        _logger.LogDebug("长时间操作完成: {Message}", loadingMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理操作结果时发生错误");
                        onError?.Invoke(ex);
                    }
                }, DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行长时间操作时发生错误: {Message}", loadingMessage);
                
                await _dispatcher.InvokeAsync(() =>
                {
                    onError?.Invoke(ex);
                }, DispatcherPriority.Normal);
            }
        }

        /// <summary>
        /// 优化UI渲染性能
        /// </summary>
        public void OptimizeUIRendering(FrameworkElement element)
        {
            if (element == null) return;

            try
            {
                // 启用渲染缓存优化
                System.Windows.Media.RenderOptions.SetCachingHint(element, System.Windows.Media.CachingHint.Cache);
                System.Windows.Media.RenderOptions.SetCacheInvalidationThresholdMinimum(element, 0.5);
                System.Windows.Media.RenderOptions.SetCacheInvalidationThresholdMaximum(element, 2.0);

                // 启用文本渲染优化
                System.Windows.Media.TextOptions.SetTextFormattingMode(element, System.Windows.Media.TextFormattingMode.Display);
                System.Windows.Media.TextOptions.SetTextRenderingMode(element, System.Windows.Media.TextRenderingMode.Auto);

                _logger.LogDebug("UI渲染优化已应用到元素: {ElementType}", element.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "应用UI渲染优化时发生错误: {ElementType}", element.GetType().Name);
            }
        }

        /// <summary>
        /// 启用虚拟化优化
        /// </summary>
        public void EnableVirtualizationOptimization(FrameworkElement container)
        {
            if (container == null) return;

            try
            {
                // 启用UI虚拟化（适用于ListBox、ListView等）
                if (container is System.Windows.Controls.ItemsControl itemsControl)
                {
                    System.Windows.Controls.VirtualizingPanel.SetIsVirtualizing(itemsControl, true);
                    System.Windows.Controls.VirtualizingPanel.SetVirtualizationMode(itemsControl, 
                        System.Windows.Controls.VirtualizationMode.Recycling);
                    System.Windows.Controls.VirtualizingPanel.SetScrollUnit(itemsControl, 
                        System.Windows.Controls.ScrollUnit.Item);
                }

                _logger.LogDebug("虚拟化优化已应用到容器: {ContainerType}", container.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "应用虚拟化优化时发生错误: {ContainerType}", container.GetType().Name);
            }
        }

        /// <summary>
        /// 预热UI控件
        /// </summary>
        public async Task WarmupUIControlsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    // 预创建常用控件实例以预热JIT编译
                    _dispatcher.InvokeAsync(() =>
                    {
                        var dummyButton = new System.Windows.Controls.Button();
                        var dummyTextBox = new System.Windows.Controls.TextBox();
                        var dummyListView = new System.Windows.Controls.ListView();
                        var dummyDataGrid = new System.Windows.Controls.DataGrid();

                        // 触发控件初始化
                        dummyButton.UpdateLayout();
                        dummyTextBox.UpdateLayout();
                        dummyListView.UpdateLayout();
                        dummyDataGrid.UpdateLayout();

                        _logger.LogDebug("UI控件预热完成");
                    }, DispatcherPriority.Background);
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UI控件预热时发生错误");
            }
        }

        /// <summary>
        /// 优化ItemsControl的虚拟化
        /// </summary>
        public void OptimizeVirtualization(System.Windows.Controls.ItemsControl itemsControl, int threshold = 100)
        {
            if (itemsControl == null) return;

            try
            {
                var itemCount = itemsControl.Items?.Count ?? 0;
                
                if (itemCount > threshold)
                {
                    // 启用虚拟化
                    VirtualizingPanel.SetIsVirtualizing(itemsControl, true);
                    VirtualizingPanel.SetVirtualizationMode(itemsControl, System.Windows.Controls.VirtualizationMode.Recycling);
                    VirtualizingPanel.SetScrollUnit(itemsControl, ScrollUnit.Item);
                    
                    // 设置虚拟化策略
                    VirtualizingPanel.SetCacheLengthUnit(itemsControl, VirtualizationCacheLengthUnit.Item);
                    VirtualizingPanel.SetCacheLength(itemsControl, new VirtualizationCacheLength(20, 20));
                    
                    // 更新统计
                    lock (_statisticsLock)
                    {
                        _statistics.VirtualizedControls[itemsControl.GetType().Name] = itemCount;
                    }
                    
                    _logger.LogDebug("已为 {ControlType} 启用虚拟化，项目数: {ItemCount}", 
                        itemsControl.GetType().Name, itemCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "优化虚拟化失败: {ControlType}", itemsControl.GetType().Name);
            }
        }

        /// <summary>
        /// 延迟加载数据
        /// </summary>
        public async Task<T> LoadLazilyAsync<T>(Func<Task<T>> loadFunc, string cacheKey, TimeSpan? cacheExpiry = null)
        {
            try
            {
                var expiry = cacheExpiry ?? TimeSpan.FromMinutes(30);
                
                // 检查缓存
                if (_dataCache.TryGetValue(cacheKey, out var cachedItem) && !cachedItem.IsExpired)
                {
                    cachedItem.HitCount++;
                    cachedItem.LastAccessed = DateTime.Now;
                    
                    lock (_statisticsLock)
                    {
                        _statistics.CacheHitRates[cacheKey] = cachedItem.HitCount;
                    }
                    
                    return (T)cachedItem.Value;
                }

                // 缓存未命中，加载数据
                var stopwatch = Stopwatch.StartNew();
                var data = await loadFunc();
                stopwatch.Stop();

                // 缓存数据
                var cacheItem = new CacheItem<object>
                {
                    Value = data!,
                    ExpiresAt = DateTime.Now.Add(expiry)
                };
                
                _dataCache.AddOrUpdate(cacheKey, cacheItem, (key, existing) => cacheItem);
                
                // 更新性能统计
                lock (_statisticsLock)
                {
                    _statistics.OperationTimes[cacheKey] = stopwatch.Elapsed;
                    _statistics.TotalUIOperations++;
                }

                _logger.LogDebug("延迟加载完成: {CacheKey}，耗时: {Duration}ms", cacheKey, stopwatch.Elapsed.TotalMilliseconds);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "延迟加载失败: {CacheKey}", cacheKey);
                throw;
            }
        }

        /// <summary>
        /// 批量UI更新
        /// </summary>
        public void BatchUIUpdates(Action updateAction, int priority = 0)
        {
            if (updateAction == null) return;

            _pendingUIUpdates.Enqueue((updateAction, priority));
        }

        /// <summary>
        /// 开始UI性能监控会话
        /// </summary>
        public IUIPerformanceSession StartUIPerformanceSession(string operationName, FrameworkElement? targetElement = null)
        {
            var session = new UIPerformanceSession(operationName, targetElement, this, _logger);
            _activeSessions[session.SessionId] = session;
            return session;
        }

        /// <summary>
        /// 预加载数据
        /// </summary>
        public void PreloadData<T>(Func<Task<T>> loadFunc, string cacheKey, int priority = 0)
        {
            if (loadFunc == null || string.IsNullOrEmpty(cacheKey)) return;

            // 如果数据已经缓存，无需预加载
            if (_dataCache.ContainsKey(cacheKey)) return;

            Task.Run(async () =>
            {
                try
                {
                    await LoadLazilyAsync(loadFunc, cacheKey);
                    _logger.LogDebug("预加载完成: {CacheKey}", cacheKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "预加载失败: {CacheKey}", cacheKey);
                }
            });
        }

        /// <summary>
        /// 清理UI缓存
        /// </summary>
        public async Task ClearCacheAsync(string? pattern = null)
        {
            try
            {
                var removedCount = 0;
                var keysToRemove = new List<string>();

                foreach (var kvp in _dataCache)
                {
                    if (pattern == null || kvp.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    if (_dataCache.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }

                _logger.LogInformation("缓存清理完成，清理项目数: {RemovedCount}，模式: {Pattern}", 
                    removedCount, pattern ?? "全部");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理缓存失败");
                throw;
            }
        }

        /// <summary>
        /// 获取UI性能统计
        /// </summary>
        public UIPerformanceStatistics GetPerformanceStatistics()
        {
            lock (_statisticsLock)
            {
                var stats = new UIPerformanceStatistics
                {
                    AverageRenderTime = _statistics.AverageRenderTime,
                    MaxRenderTime = _statistics.MaxRenderTime,
                    AverageLayoutTime = _statistics.AverageLayoutTime,
                    MaxLayoutTime = _statistics.MaxLayoutTime,
                    TotalUIOperations = _statistics.TotalUIOperations,
                    SlowUIOperations = _statistics.SlowUIOperations,
                    LastUpdated = DateTime.Now
                };

                stats.OperationTimes = new Dictionary<string, TimeSpan>(_statistics.OperationTimes);
                stats.CacheHitRates = new Dictionary<string, int>(_statistics.CacheHitRates);
                stats.VirtualizedControls = new Dictionary<string, int>(_statistics.VirtualizedControls);

                return stats;
            }
        }

        /// <summary>
        /// 设置UI性能阈值
        /// </summary>
        public void SetPerformanceThresholds(UIPerformanceThresholds thresholds)
        {
            _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
            _logger.LogInformation("UI性能阈值已更新");
        }

        #region 私有方法

        private void ProcessBatchUpdates(object? state)
        {
            if (_pendingUIUpdates.IsEmpty) return;

            var updateList = new List<(Action Action, int Priority)>();
            
            // 收集待处理的更新
            while (_pendingUIUpdates.TryDequeue(out var update) && updateList.Count < 10)
            {
                updateList.Add(update);
            }

            if (!updateList.Any()) return;

            // 按优先级排序
            updateList.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            _dispatcher.BeginInvoke(() =>
            {
                var stopwatch = Stopwatch.StartNew();
                
                foreach (var (action, _) in updateList)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "批量UI更新执行失败");
                    }
                }
                
                stopwatch.Stop();
                
                // 检查性能阈值
                if (stopwatch.Elapsed > _thresholds.MaxUIOperationTime)
                {
                    OnPerformanceWarning("BatchUpdateTime", 
                        $"批量UI更新耗时超过阈值: {stopwatch.Elapsed.TotalMilliseconds:F2}ms",
                        "BatchUpdater", null, stopwatch.Elapsed, _thresholds.MaxUIOperationTime);
                }
                
            }, DispatcherPriority.Background);
        }

        private void CleanupExpiredCache(object? state)
        {
            try
            {
                var expiredKeys = new List<string>();
                
                foreach (var kvp in _dataCache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _dataCache.TryRemove(key, out _);
                }

                if (expiredKeys.Any())
                {
                    _logger.LogDebug("清理过期缓存项: {ExpiredCount}", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理过期缓存失败");
            }
        }

        internal void EndSession(string sessionId, TimeSpan renderTime, TimeSpan layoutTime)
        {
            if (_activeSessions.TryRemove(sessionId, out _))
            {
                lock (_statisticsLock)
                {
                    // 更新渲染时间统计
                    if (_statistics.AverageRenderTime == TimeSpan.Zero)
                    {
                        _statistics.AverageRenderTime = renderTime;
                    }
                    else
                    {
                        var totalMs = (_statistics.AverageRenderTime.TotalMilliseconds * _statistics.TotalUIOperations + renderTime.TotalMilliseconds) / (_statistics.TotalUIOperations + 1);
                        _statistics.AverageRenderTime = TimeSpan.FromMilliseconds(totalMs);
                    }

                    if (renderTime > _statistics.MaxRenderTime)
                    {
                        _statistics.MaxRenderTime = renderTime;
                    }

                    // 更新布局时间统计
                    if (_statistics.AverageLayoutTime == TimeSpan.Zero)
                    {
                        _statistics.AverageLayoutTime = layoutTime;
                    }
                    else
                    {
                        var totalMs = (_statistics.AverageLayoutTime.TotalMilliseconds * _statistics.TotalUIOperations + layoutTime.TotalMilliseconds) / (_statistics.TotalUIOperations + 1);
                        _statistics.AverageLayoutTime = TimeSpan.FromMilliseconds(totalMs);
                    }

                    if (layoutTime > _statistics.MaxLayoutTime)
                    {
                        _statistics.MaxLayoutTime = layoutTime;
                    }

                    _statistics.TotalUIOperations++;

                    // 检查慢操作
                    if (renderTime > _thresholds.MaxRenderTime || layoutTime > _thresholds.MaxLayoutTime)
                    {
                        _statistics.SlowUIOperations++;
                    }
                }

                // 发出性能警告
                if (renderTime > _thresholds.MaxRenderTime)
                {
                    OnPerformanceWarning("RenderTime", 
                        $"渲染时间超过阈值: {renderTime.TotalMilliseconds:F2}ms",
                        sessionId, null, renderTime, _thresholds.MaxRenderTime);
                }

                if (layoutTime > _thresholds.MaxLayoutTime)
                {
                    OnPerformanceWarning("LayoutTime", 
                        $"布局时间超过阈值: {layoutTime.TotalMilliseconds:F2}ms",
                        sessionId, null, layoutTime, _thresholds.MaxLayoutTime);
                }
            }
        }

        private void OnPerformanceWarning(string warningType, string message, string? elementName, 
            string? operationName, TimeSpan? duration, TimeSpan? threshold)
        {
            var args = new UIPerformanceWarningEventArgs
            {
                WarningType = warningType,
                Message = message,
                ElementName = elementName,
                OperationName = operationName,
                Duration = duration,
                Threshold = threshold
            };

            _logger.LogWarning("UI性能警告: {WarningType} - {Message}", warningType, message);
            PerformanceWarning?.Invoke(this, args);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _batchUpdateTimer?.Dispose();
            _cacheCleanupTimer?.Dispose();
            _dataCache?.Clear();
            _activeSessions?.Clear();
        }

        #endregion
    }

    #region 支持类型定义

    /// <summary>
    /// UI性能监控会话实现
    /// </summary>
    internal class UIPerformanceSession : IUIPerformanceSession
    {
        private readonly UIPerformanceOptimizer _optimizer;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;
        private readonly List<(string Name, TimeSpan Time)> _milestones = new();
        private bool _disposed;
        
        public string SessionId { get; } = Guid.NewGuid().ToString();
        public string OperationName { get; }
        public FrameworkElement? TargetElement { get; }
        public DateTime StartTime { get; }
        
        private TimeSpan _renderTime;
        private TimeSpan _layoutTime;
        private int _elementCount;

        public UIPerformanceSession(string operationName, FrameworkElement? targetElement, 
            UIPerformanceOptimizer optimizer, ILogger logger)
        {
            OperationName = operationName;
            TargetElement = targetElement;
            _optimizer = optimizer;
            _logger = logger;
            StartTime = DateTime.Now;
            _stopwatch = Stopwatch.StartNew();
        }

        public void AddMilestone(string name)
        {
            if (_disposed) return;
            _milestones.Add((name, _stopwatch.Elapsed));
            _logger.LogDebug("UI性能里程碑 [{OperationName}]: {Name} 在 {Elapsed}ms", 
                OperationName, name, _stopwatch.Elapsed.TotalMilliseconds);
        }

        public void RecordRenderTime(TimeSpan renderTime)
        {
            if (_disposed) return;
            _renderTime = renderTime;
        }

        public void RecordLayoutTime(TimeSpan layoutTime)
        {
            if (_disposed) return;
            _layoutTime = layoutTime;
        }

        public void SetElementCount(int count)
        {
            if (_disposed) return;
            _elementCount = count;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopwatch.Stop();
            
            try
            {
                _optimizer.EndSession(SessionId, _renderTime, _layoutTime);
                
                _logger.LogDebug("UI性能会话完成 [{OperationName}]: 总耗时 {TotalTime}ms，元素数量 {ElementCount}，里程碑 {MilestoneCount}", 
                    OperationName, _stopwatch.Elapsed.TotalMilliseconds, _elementCount, _milestones.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UI性能会话结束失败: {OperationName}", OperationName);
            }
        }
    }

    #endregion
}