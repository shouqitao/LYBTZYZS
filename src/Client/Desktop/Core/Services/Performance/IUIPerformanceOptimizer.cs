using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// UltraThink Phase 5.4: UI性能优化器接口
    /// 提供UI虚拟化、延迟加载和响应性优化
    /// </summary>
    public interface IUIPerformanceOptimizer
    {
        /// <summary>
        /// 优化ItemsControl的虚拟化
        /// </summary>
        void OptimizeVirtualization(ItemsControl itemsControl, int threshold = 100);

        /// <summary>
        /// 实施延迟加载策略
        /// </summary>
        Task<T> LoadLazilyAsync<T>(Func<Task<T>> loadFunc, string cacheKey, TimeSpan? cacheExpiry = null);

        /// <summary>
        /// 批量UI更新
        /// </summary>
        void BatchUIUpdates(Action updateAction, int priority = 0);

        /// <summary>
        /// 测量UI操作性能
        /// </summary>
        IUIPerformanceSession StartUIPerformanceSession(string operationName, FrameworkElement? targetElement = null);

        /// <summary>
        /// 预加载数据
        /// </summary>
        void PreloadData<T>(Func<Task<T>> loadFunc, string cacheKey, int priority = 0);

        /// <summary>
        /// 清理UI缓存
        /// </summary>
        Task ClearCacheAsync(string? pattern = null);

        /// <summary>
        /// 获取UI性能统计
        /// </summary>
        UIPerformanceStatistics GetPerformanceStatistics();

        /// <summary>
        /// 设置UI性能阈值
        /// </summary>
        void SetPerformanceThresholds(UIPerformanceThresholds thresholds);

        /// <summary>
        /// UI性能警告事件
        /// </summary>
        event EventHandler<UIPerformanceWarningEventArgs> PerformanceWarning;
    }

    /// <summary>
    /// UI性能监控会话
    /// </summary>
    public interface IUIPerformanceSession : IDisposable
    {
        string OperationName { get; }
        FrameworkElement? TargetElement { get; }
        
        /// <summary>
        /// 添加UI里程碑
        /// </summary>
        void AddMilestone(string name);
        
        /// <summary>
        /// 记录渲染时间
        /// </summary>
        void RecordRenderTime(TimeSpan renderTime);
        
        /// <summary>
        /// 记录布局时间
        /// </summary>
        void RecordLayoutTime(TimeSpan layoutTime);
        
        /// <summary>
        /// 设置元素数量
        /// </summary>
        void SetElementCount(int count);
    }

    /// <summary>
    /// UI性能统计
    /// </summary>
    public class UIPerformanceStatistics
    {
        public TimeSpan AverageRenderTime { get; set; }
        public TimeSpan MaxRenderTime { get; set; }
        public TimeSpan AverageLayoutTime { get; set; }
        public TimeSpan MaxLayoutTime { get; set; }
        public int TotalUIOperations { get; set; }
        public int SlowUIOperations { get; set; }
        public double SlowOperationPercentage => TotalUIOperations > 0 ? (double)SlowUIOperations / TotalUIOperations * 100 : 0;
        
        public Dictionary<string, TimeSpan> OperationTimes { get; set; } = new();
        public Dictionary<string, int> CacheHitRates { get; set; } = new();
        public Dictionary<string, int> VirtualizedControls { get; set; } = new();
        
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// UI性能阈值
    /// </summary>
    public class UIPerformanceThresholds
    {
        public TimeSpan MaxRenderTime { get; set; } = TimeSpan.FromMilliseconds(16); // 60fps
        public TimeSpan MaxLayoutTime { get; set; } = TimeSpan.FromMilliseconds(8);
        public TimeSpan MaxUIOperationTime { get; set; } = TimeSpan.FromMilliseconds(100);
        public int MaxElementsForVirtualization { get; set; } = 100;
        public double MinCacheHitRate { get; set; } = 80.0; // 80%
        public int MaxConcurrentUIOperations { get; set; } = 5;
    }

    /// <summary>
    /// UI性能警告事件参数
    /// </summary>
    public class UIPerformanceWarningEventArgs : EventArgs
    {
        public string WarningType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ElementName { get; set; }
        public string? OperationName { get; set; }
        public TimeSpan? Duration { get; set; }
        public TimeSpan? Threshold { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 缓存项
    /// </summary>
    public class CacheItem<T>
    {
        public T Value { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; }
        public int HitCount { get; set; }
        public DateTime LastAccessed { get; set; } = DateTime.Now;
        
        public bool IsExpired => DateTime.Now > ExpiresAt;
    }

    /// <summary>
    /// UI操作优先级
    /// </summary>
    public enum UIOperationPriority
    {
        Background = 0,
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// 虚拟化模式
    /// </summary>
    public enum VirtualizationMode
    {
        None,
        Standard,
        Recycling,
        Adaptive
    }
}