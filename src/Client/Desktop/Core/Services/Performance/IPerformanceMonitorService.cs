using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// UltraThink Phase 5.4: 性能监控服务接口
    /// 提供性能指标收集、分析和报告
    /// </summary>
    public interface IPerformanceMonitorService
    {
        /// <summary>
        /// 开始性能监控会话
        /// </summary>
        IPerformanceSession StartSession(string operationName, string? category = null);

        /// <summary>
        /// 记录操作性能
        /// </summary>
        Task RecordOperationAsync(string operationName, TimeSpan duration, bool success, string? details = null);

        /// <summary>
        /// 记录内存使用情况
        /// </summary>
        void RecordMemoryUsage(long memoryUsage, string? component = null);

        /// <summary>
        /// 记录UI响应时间
        /// </summary>
        void RecordUIResponseTime(string uiElement, TimeSpan responseTime);

        /// <summary>
        /// 获取性能统计
        /// </summary>
        PerformanceStatistics GetStatistics(TimeSpan? timeRange = null);

        /// <summary>
        /// 获取性能报告
        /// </summary>
        Task<PerformanceReport> GenerateReportAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// 清理旧的性能数据
        /// </summary>
        Task CleanupOldDataAsync(TimeSpan retentionPeriod);

        /// <summary>
        /// 设置性能阈值
        /// </summary>
        void SetPerformanceThresholds(PerformanceThresholds thresholds);

        /// <summary>
        /// 性能警告事件
        /// </summary>
        event EventHandler<PerformanceWarningEventArgs> PerformanceWarning;
    }

    /// <summary>
    /// 性能监控会话
    /// </summary>
    public interface IPerformanceSession : IDisposable
    {
        string OperationName { get; }
        string? Category { get; }
        DateTime StartTime { get; }
        
        /// <summary>
        /// 添加标记点
        /// </summary>
        void AddMilestone(string name);
        
        /// <summary>
        /// 设置操作结果
        /// </summary>
        void SetResult(bool success, string? details = null);
        
        /// <summary>
        /// 添加自定义指标
        /// </summary>
        void AddMetric(string name, object value);
    }

    /// <summary>
    /// 性能统计信息
    /// </summary>
    public class PerformanceStatistics
    {
        public TimeSpan AverageOperationTime { get; set; }
        public TimeSpan MaxOperationTime { get; set; }
        public TimeSpan MinOperationTime { get; set; }
        public long TotalOperations { get; set; }
        public long SuccessfulOperations { get; set; }
        public long FailedOperations { get; set; }
        public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations * 100 : 0;
        
        public long CurrentMemoryUsage { get; set; }
        public long PeakMemoryUsage { get; set; }
        public long AverageMemoryUsage { get; set; }
        
        public Dictionary<string, TimeSpan> OperationsByCategory { get; set; } = new();
        public Dictionary<string, long> MemoryByComponent { get; set; } = new();
        public Dictionary<string, TimeSpan> UIResponseTimes { get; set; } = new();
    }

    /// <summary>
    /// 性能报告
    /// </summary>
    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public PerformanceStatistics Statistics { get; set; } = new();
        public List<PerformanceIssue> Issues { get; set; } = new();
        public List<PerformanceRecommendation> Recommendations { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 性能问题
    /// </summary>
    public class PerformanceIssue
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string Component { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// 性能建议
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string[] ActionItems { get; set; } = Array.Empty<string>();
        public string EstimatedImpact { get; set; } = string.Empty;
        public string EstimatedEffort { get; set; } = string.Empty;
    }

    /// <summary>
    /// 性能阈值
    /// </summary>
    public class PerformanceThresholds
    {
        public TimeSpan MaxOperationTime { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan MaxUIResponseTime { get; set; } = TimeSpan.FromMilliseconds(100);
        public long MaxMemoryUsage { get; set; } = 500 * 1024 * 1024; // 500MB
        public double MinSuccessRate { get; set; } = 95.0; // 95%
        public int MaxConcurrentOperations { get; set; } = 10;
    }

    /// <summary>
    /// 性能警告事件参数
    /// </summary>
    public class PerformanceWarningEventArgs : EventArgs
    {
        public string WarningType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public object? Value { get; set; }
        public object? Threshold { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}