using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Configuration;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// UltraThink Phase 5.4: 数据绑定优化器
    /// 优化WPF数据绑定性能，减少不必要的更新和提升响应速度
    /// </summary>
    public interface IDataBindingOptimizer
    {
        /// <summary>
        /// 优化控件的数据绑定
        /// </summary>
        void OptimizeBindings(FrameworkElement element);

        /// <summary>
        /// 批量优化数据绑定更新
        /// </summary>
        void BatchBindingUpdates(Action updateAction, int priority = 0);

        /// <summary>
        /// 启用智能绑定更新
        /// </summary>
        void EnableSmartBinding(INotifyPropertyChanged viewModel);

        /// <summary>
        /// 禁用智能绑定更新
        /// </summary>
        void DisableSmartBinding(INotifyPropertyChanged viewModel);

        /// <summary>
        /// 获取绑定统计
        /// </summary>
        BindingStatistics GetStatistics();

        /// <summary>
        /// 清理绑定缓存
        /// </summary>
        void ClearBindingCache();

        /// <summary>
        /// 绑定性能警告事件
        /// </summary>
        event EventHandler<BindingPerformanceWarningEventArgs> BindingWarning;
    }

    /// <summary>
    /// 数据绑定优化器实现
    /// </summary>
    public class DataBindingOptimizer : IDataBindingOptimizer, IDisposable
    {
        private readonly ILogger<DataBindingOptimizer> _logger;
        private readonly IAppConfiguration _configuration;
        private readonly IUIPerformanceOptimizer _performanceOptimizer;
        private readonly Dispatcher _dispatcher;
        
        private readonly ConcurrentDictionary<WeakReference, SmartPropertyChangedEventHandler> _smartBindings = new();
        private readonly ConcurrentQueue<(Action Action, int Priority)> _pendingBindingUpdates = new();
        private readonly ConcurrentDictionary<string, BindingMetrics> _bindingMetrics = new();
        
        private readonly Timer _batchUpdateTimer;
        private readonly Timer _cleanupTimer;
        private BindingStatistics _statistics = new();
        private readonly object _statisticsLock = new object();

        public event EventHandler<BindingPerformanceWarningEventArgs>? BindingWarning;

        public DataBindingOptimizer(
            ILogger<DataBindingOptimizer> logger,
            IAppConfiguration configuration,
            IUIPerformanceOptimizer performanceOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _performanceOptimizer = performanceOptimizer ?? throw new ArgumentNullException(nameof(performanceOptimizer));
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            // 批量更新定时器（每16ms执行一次，约60fps）
            var updateInterval = TimeSpan.FromMilliseconds(_configuration.Performance.UIUpdateThrottleMs);
            _batchUpdateTimer = new Timer(ProcessBatchUpdates, null, updateInterval, updateInterval);
            
            // 清理定时器（每5分钟执行一次）
            _cleanupTimer = new Timer(CleanupWeakReferences, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _logger.LogInformation("数据绑定优化器已启动");
        }

        public void OptimizeBindings(FrameworkElement element)
        {
            if (element == null) return;

            using var session = _performanceOptimizer.StartUIPerformanceSession($"OptimizeBindings_{element.GetType().Name}", element);
            
            try
            {
                var bindingExpressions = FindBindingExpressions(element);
                var optimizedCount = 0;

                foreach (var binding in bindingExpressions)
                {
                    if (OptimizeBinding(binding))
                    {
                        optimizedCount++;
                    }
                }

                session.SetElementCount(bindingExpressions.Count);
                session.AddMilestone($"OptimizedBindings_{optimizedCount}");

                lock (_statisticsLock)
                {
                    _statistics.TotalOptimizedElements++;
                    _statistics.TotalOptimizedBindings += optimizedCount;
                }

                _logger.LogDebug("优化绑定完成: {Element}，处理绑定: {Total}，优化: {Optimized}", 
                    element.GetType().Name, bindingExpressions.Count, optimizedCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "优化绑定失败: {Element}", element.GetType().Name);
            }
        }

        public void BatchBindingUpdates(Action updateAction, int priority = 0)
        {
            if (updateAction == null) return;

            _pendingBindingUpdates.Enqueue((updateAction, priority));
            
            lock (_statisticsLock)
            {
                _statistics.TotalBatchedUpdates++;
            }
        }

        public void EnableSmartBinding(INotifyPropertyChanged viewModel)
        {
            if (viewModel == null) return;

            var weakRef = new WeakReference(viewModel);
            var handler = new SmartPropertyChangedEventHandler(_logger, _configuration, this);
            
            _smartBindings.TryAdd(weakRef, handler);
            viewModel.PropertyChanged += handler.HandlePropertyChanged;

            lock (_statisticsLock)
            {
                _statistics.TotalSmartBindings++;
            }

            _logger.LogDebug("启用智能绑定: {ViewModelType}", viewModel.GetType().Name);
        }

        public void DisableSmartBinding(INotifyPropertyChanged viewModel)
        {
            if (viewModel == null) return;

            var toRemove = new List<WeakReference>();
            
            foreach (var kvp in _smartBindings)
            {
                if (kvp.Key.Target == viewModel)
                {
                    toRemove.Add(kvp.Key);
                    viewModel.PropertyChanged -= kvp.Value.HandlePropertyChanged;
                }
            }

            foreach (var weakRef in toRemove)
            {
                _smartBindings.TryRemove(weakRef, out _);
            }

            _logger.LogDebug("禁用智能绑定: {ViewModelType}", viewModel.GetType().Name);
        }

        public BindingStatistics GetStatistics()
        {
            lock (_statisticsLock)
            {
                return new BindingStatistics
                {
                    TotalOptimizedElements = _statistics.TotalOptimizedElements,
                    TotalOptimizedBindings = _statistics.TotalOptimizedBindings,
                    TotalSmartBindings = _statistics.TotalSmartBindings,
                    TotalBatchedUpdates = _statistics.TotalBatchedUpdates,
                    TotalPropertyChanges = _statistics.TotalPropertyChanges,
                    ThrottledUpdates = _statistics.ThrottledUpdates,
                    AverageUpdateTime = _statistics.AverageUpdateTime,
                    BindingMetrics = new Dictionary<string, BindingMetrics>(_bindingMetrics),
                    LastUpdated = DateTime.Now
                };
            }
        }

        public void ClearBindingCache()
        {
            _bindingMetrics.Clear();
            
            // 清理待处理的更新
            while (_pendingBindingUpdates.TryDequeue(out _)) { }

            _logger.LogInformation("绑定缓存已清理");
        }

        #region 私有方法

        private List<BindingExpression> FindBindingExpressions(DependencyObject obj)
        {
            var bindings = new List<BindingExpression>();
            FindBindingExpressionsRecursive(obj, bindings);
            return bindings;
        }

        private void FindBindingExpressionsRecursive(DependencyObject obj, List<BindingExpression> bindings)
        {
            if (obj == null) return;

            // 获取对象的所有依赖属性
            var properties = TypeDescriptor.GetProperties(obj, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });
            
            foreach (PropertyDescriptor property in properties)
            {
                var dpd = DependencyPropertyDescriptor.FromProperty(property);
                if (dpd?.DependencyProperty != null)
                {
                    var binding = BindingOperations.GetBindingExpression(obj, dpd.DependencyProperty);
                    if (binding != null)
                    {
                        bindings.Add(binding);
                    }
                }
            }

            // 递归处理子对象
            var childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                FindBindingExpressionsRecursive(child, bindings);
            }
        }

        private bool OptimizeBinding(BindingExpression bindingExpression)
        {
            try
            {
                var binding = bindingExpression.ParentBinding;
                var optimized = false;

                // 优化更新源触发器
                if (binding.UpdateSourceTrigger == UpdateSourceTrigger.Default)
                {
                    // 对于文本框，使用LostFocus而不是PropertyChanged减少频繁更新
                    if (bindingExpression.Target is System.Windows.Controls.TextBox)
                    {
                        binding.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
                        optimized = true;
                    }
                }

                // 优化绑定模式
                if (binding.Mode == BindingMode.Default && IsReadOnlyContext(bindingExpression))
                {
                    binding.Mode = BindingMode.OneWay;
                    optimized = true;
                }

                // 启用绑定验证缓存
                if (binding.ValidatesOnDataErrors && !binding.NotifyOnValidationError)
                {
                    binding.NotifyOnValidationError = true;
                    optimized = true;
                }

                return optimized;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "优化绑定时发生错误");
                return false;
            }
        }

        private bool IsReadOnlyContext(BindingExpression bindingExpression)
        {
            // 检查绑定目标是否是只读的上下文
            var target = bindingExpression.Target;
            var property = bindingExpression.TargetProperty;

            // 检查属性是否只读
            if (property.ReadOnly) return true;

            // 检查控件是否只读
            if (target is System.Windows.Controls.TextBox textBox && textBox.IsReadOnly) return true;
            if (target is System.Windows.Controls.Primitives.TextBoxBase textBoxBase && textBoxBase.IsReadOnly) return true;

            return false;
        }

        private void ProcessBatchUpdates(object? state)
        {
            if (_pendingBindingUpdates.IsEmpty) return;

            var updates = new List<(Action Action, int Priority)>();
            var maxUpdates = 50; // 每次最多处理50个更新

            // 收集待处理的更新
            while (_pendingBindingUpdates.TryDequeue(out var update) && updates.Count < maxUpdates)
            {
                updates.Add(update);
            }

            if (!updates.Any()) return;

            // 按优先级排序
            updates.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            _dispatcher.BeginInvoke(() =>
            {
                var startTime = DateTime.Now;
                var successCount = 0;

                foreach (var (action, _) in updates)
                {
                    try
                    {
                        action();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "批量绑定更新执行失败");
                    }
                }

                var duration = DateTime.Now - startTime;
                
                lock (_statisticsLock)
                {
                    _statistics.TotalProcessedBatchUpdates += successCount;
                    
                    // 更新平均处理时间
                    if (_statistics.AverageUpdateTime == TimeSpan.Zero)
                    {
                        _statistics.AverageUpdateTime = duration;
                    }
                    else
                    {
                        var totalMs = (_statistics.AverageUpdateTime.TotalMilliseconds + duration.TotalMilliseconds) / 2;
                        _statistics.AverageUpdateTime = TimeSpan.FromMilliseconds(totalMs);
                    }
                }

                // 检查是否需要性能警告
                if (duration.TotalMilliseconds > 50) // 超过50ms
                {
                    OnBindingWarning("SlowBatchUpdate", 
                        $"批量绑定更新耗时过长: {duration.TotalMilliseconds:F2}ms",
                        updates.Count, duration);
                }

            }, DispatcherPriority.Background);
        }

        private void CleanupWeakReferences(object? state)
        {
            try
            {
                var deadReferences = new List<WeakReference>();

                foreach (var kvp in _smartBindings)
                {
                    if (!kvp.Key.IsAlive)
                    {
                        deadReferences.Add(kvp.Key);
                    }
                }

                foreach (var deadRef in deadReferences)
                {
                    _smartBindings.TryRemove(deadRef, out _);
                }

                if (deadReferences.Count > 0)
                {
                    _logger.LogDebug("清理死亡的弱引用: {Count}", deadReferences.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理弱引用失败");
            }
        }

        internal void RecordPropertyChange(string propertyName, bool wasThrottled, TimeSpan processingTime)
        {
            var key = propertyName ?? "Unknown";
            var metrics = _bindingMetrics.GetOrAdd(key, _ => new BindingMetrics { PropertyName = key });

            metrics.TotalChanges++;
            if (wasThrottled)
            {
                metrics.ThrottledChanges++;
            }

            metrics.TotalProcessingTime = metrics.TotalProcessingTime.Add(processingTime);
            metrics.AverageProcessingTime = TimeSpan.FromMilliseconds(
                metrics.TotalProcessingTime.TotalMilliseconds / metrics.TotalChanges);

            if (processingTime > metrics.MaxProcessingTime)
            {
                metrics.MaxProcessingTime = processingTime;
            }

            metrics.LastChanged = DateTime.Now;

            lock (_statisticsLock)
            {
                _statistics.TotalPropertyChanges++;
                if (wasThrottled)
                {
                    _statistics.ThrottledUpdates++;
                }
            }
        }

        private void OnBindingWarning(string warningType, string message, int affectedCount, TimeSpan duration)
        {
            var args = new BindingPerformanceWarningEventArgs
            {
                WarningType = warningType,
                Message = message,
                AffectedCount = affectedCount,
                Duration = duration,
                Timestamp = DateTime.Now
            };

            _logger.LogWarning("绑定性能警告: {WarningType} - {Message}", warningType, message);
            BindingWarning?.Invoke(this, args);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _batchUpdateTimer?.Dispose();
            _cleanupTimer?.Dispose();
            
            // 清理智能绑定
            foreach (var kvp in _smartBindings)
            {
                if (kvp.Key.Target is INotifyPropertyChanged viewModel)
                {
                    viewModel.PropertyChanged -= kvp.Value.HandlePropertyChanged;
                }
            }
            
            _smartBindings.Clear();
            _bindingMetrics.Clear();
        }

        #endregion
    }

    #region 支持类型

    /// <summary>
    /// 智能属性变更事件处理器
    /// </summary>
    internal class SmartPropertyChangedEventHandler
    {
        private readonly ILogger _logger;
        private readonly IAppConfiguration _configuration;
        private readonly DataBindingOptimizer _optimizer;
        private readonly Dictionary<string, DateTime> _lastUpdateTimes = new();
        private readonly object _lockObject = new object();

        public SmartPropertyChangedEventHandler(ILogger logger, IAppConfiguration configuration, DataBindingOptimizer optimizer)
        {
            _logger = logger;
            _configuration = configuration;
            _optimizer = optimizer;
        }

        public void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var propertyName = e.PropertyName ?? "Unknown";
            var startTime = DateTime.Now;
            var wasThrottled = false;

            lock (_lockObject)
            {
                // 检查是否需要节流
                if (_lastUpdateTimes.TryGetValue(propertyName, out var lastUpdate))
                {
                    var timeSinceLastUpdate = DateTime.Now - lastUpdate;
                    var throttleThreshold = TimeSpan.FromMilliseconds(_configuration.Performance.UIUpdateThrottleMs);

                    if (timeSinceLastUpdate < throttleThreshold)
                    {
                        wasThrottled = true;
                        return; // 跳过这次更新
                    }
                }

                _lastUpdateTimes[propertyName] = DateTime.Now;
            }

            var processingTime = DateTime.Now - startTime;
            _optimizer.RecordPropertyChange(propertyName, wasThrottled, processingTime);
        }
    }

    /// <summary>
    /// 绑定统计
    /// </summary>
    public class BindingStatistics
    {
        public int TotalOptimizedElements { get; set; }
        public int TotalOptimizedBindings { get; set; }
        public int TotalSmartBindings { get; set; }
        public long TotalBatchedUpdates { get; set; }
        public long TotalProcessedBatchUpdates { get; set; }
        public long TotalPropertyChanges { get; set; }
        public long ThrottledUpdates { get; set; }
        public TimeSpan AverageUpdateTime { get; set; }
        public Dictionary<string, BindingMetrics> BindingMetrics { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        
        public double ThrottleRate => TotalPropertyChanges > 0 ? (double)ThrottledUpdates / TotalPropertyChanges * 100 : 0;
        public double BatchProcessingRate => TotalBatchedUpdates > 0 ? (double)TotalProcessedBatchUpdates / TotalBatchedUpdates * 100 : 0;
    }

    /// <summary>
    /// 绑定指标
    /// </summary>
    public class BindingMetrics
    {
        public string PropertyName { get; set; } = string.Empty;
        public long TotalChanges { get; set; }
        public long ThrottledChanges { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public TimeSpan MaxProcessingTime { get; set; }
        public DateTime LastChanged { get; set; }
        
        public double ThrottleRate => TotalChanges > 0 ? (double)ThrottledChanges / TotalChanges * 100 : 0;
    }

    /// <summary>
    /// 绑定性能警告事件参数
    /// </summary>
    public class BindingPerformanceWarningEventArgs : EventArgs
    {
        public string WarningType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int AffectedCount { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}