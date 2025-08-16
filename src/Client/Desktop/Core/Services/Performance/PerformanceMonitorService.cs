using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Configuration;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// UltraThink Phase 5.4: 性能监控服务实现
    /// 提供全面的性能数据收集和分析
    /// </summary>
    public class PerformanceMonitorService : IPerformanceMonitorService
    {
        private readonly ILogger<PerformanceMonitorService> _logger;
        private readonly IAppConfiguration _configuration;
        private readonly Timer _memoryMonitorTimer;
        private readonly ConcurrentQueue<PerformanceRecord> _performanceRecords = new();
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _componentMetrics = new();
        private readonly object _lockObject = new object();
        
        private PerformanceThresholds _thresholds = new();
        private long _currentMemoryUsage;
        private long _peakMemoryUsage;
        private readonly Process _currentProcess;

        public event EventHandler<PerformanceWarningEventArgs>? PerformanceWarning;

        public PerformanceMonitorService(ILogger<PerformanceMonitorService> logger, IAppConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            _currentProcess = Process.GetCurrentProcess();
            
            // 启动内存监控定时器
            _memoryMonitorTimer = new Timer(MonitorMemoryUsage, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            
            _logger.LogInformation("性能监控服务已启动");
        }

        public IPerformanceSession StartSession(string operationName, string? category = null)
        {
            return new PerformanceSession(operationName, category, this, _logger);
        }

        public async Task RecordOperationAsync(string operationName, TimeSpan duration, bool success, string? details = null)
        {
            var record = new PerformanceRecord
            {
                OperationName = operationName,
                Duration = duration,
                Success = success,
                Details = details,
                Timestamp = DateTime.Now,
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };

            _performanceRecords.Enqueue(record);
            
            // 更新组件指标
            UpdateComponentMetrics(operationName, duration, success);
            
            // 检查性能阈值
            CheckPerformanceThresholds(operationName, duration, success);
            
            await Task.CompletedTask;
        }

        public void RecordMemoryUsage(long memoryUsage, string? component = null)
        {
            lock (_lockObject)
            {
                _currentMemoryUsage = memoryUsage;
                if (memoryUsage > _peakMemoryUsage)
                {
                    _peakMemoryUsage = memoryUsage;
                }
            }

            if (component != null)
            {
                _componentMetrics.AddOrUpdate(component, 
                    new PerformanceMetrics { MemoryUsage = memoryUsage },
                    (key, existing) => 
                    {
                        existing.MemoryUsage = memoryUsage;
                        existing.PeakMemoryUsage = Math.Max(existing.PeakMemoryUsage, memoryUsage);
                        return existing;
                    });
            }

            // 检查内存阈值
            if (memoryUsage > _thresholds.MaxMemoryUsage)
            {
                OnPerformanceWarning("MemoryUsage", $"内存使用超过阈值: {FormatBytes(memoryUsage)}", 
                    component ?? "System", memoryUsage, _thresholds.MaxMemoryUsage);
            }
        }

        public void RecordUIResponseTime(string uiElement, TimeSpan responseTime)
        {
            var key = $"UI_{uiElement}";
            _componentMetrics.AddOrUpdate(key,
                new PerformanceMetrics { AverageUIResponseTime = responseTime },
                (k, existing) =>
                {
                    var count = existing.UIResponseCount + 1;
                    var totalTime = existing.AverageUIResponseTime.TotalMilliseconds * existing.UIResponseCount + responseTime.TotalMilliseconds;
                    existing.AverageUIResponseTime = TimeSpan.FromMilliseconds(totalTime / count);
                    existing.UIResponseCount = count;
                    existing.MaxUIResponseTime = responseTime > existing.MaxUIResponseTime ? responseTime : existing.MaxUIResponseTime;
                    return existing;
                });

            // 检查UI响应时间阈值
            if (responseTime > _thresholds.MaxUIResponseTime)
            {
                OnPerformanceWarning("UIResponseTime", $"UI响应时间超过阈值: {responseTime.TotalMilliseconds:F2}ms", 
                    uiElement, responseTime, _thresholds.MaxUIResponseTime);
            }
        }

        public PerformanceStatistics GetStatistics(TimeSpan? timeRange = null)
        {
            var cutoffTime = timeRange.HasValue ? DateTime.Now - timeRange.Value : DateTime.MinValue;
            var relevantRecords = _performanceRecords.Where(r => r.Timestamp >= cutoffTime).ToList();

            if (!relevantRecords.Any())
            {
                return new PerformanceStatistics
                {
                    CurrentMemoryUsage = _currentMemoryUsage,
                    PeakMemoryUsage = _peakMemoryUsage
                };
            }

            var statistics = new PerformanceStatistics
            {
                TotalOperations = relevantRecords.Count,
                SuccessfulOperations = relevantRecords.Count(r => r.Success),
                FailedOperations = relevantRecords.Count(r => !r.Success),
                AverageOperationTime = TimeSpan.FromMilliseconds(relevantRecords.Average(r => r.Duration.TotalMilliseconds)),
                MaxOperationTime = relevantRecords.Max(r => r.Duration),
                MinOperationTime = relevantRecords.Min(r => r.Duration),
                CurrentMemoryUsage = _currentMemoryUsage,
                PeakMemoryUsage = _peakMemoryUsage
            };

            // 按类别统计操作
            statistics.OperationsByCategory = relevantRecords
                .GroupBy(r => r.OperationName)
                .ToDictionary(g => g.Key, g => TimeSpan.FromMilliseconds(g.Average(r => r.Duration.TotalMilliseconds)));

            // 组件内存使用统计
            statistics.MemoryByComponent = _componentMetrics
                .Where(kvp => !kvp.Key.StartsWith("UI_"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.MemoryUsage);

            // UI响应时间统计
            statistics.UIResponseTimes = _componentMetrics
                .Where(kvp => kvp.Key.StartsWith("UI_"))
                .ToDictionary(kvp => kvp.Key.Substring(3), kvp => kvp.Value.AverageUIResponseTime);

            return statistics;
        }

        public async Task<PerformanceReport> GenerateReportAsync(DateTime startTime, DateTime endTime)
        {
            var relevantRecords = _performanceRecords
                .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
                .ToList();

            var statistics = GetStatistics(endTime - startTime);
            var issues = await AnalyzePerformanceIssuesAsync(relevantRecords);
            var recommendations = GenerateRecommendations(statistics, issues);

            return new PerformanceReport
            {
                GeneratedAt = DateTime.Now,
                StartTime = startTime,
                EndTime = endTime,
                Statistics = statistics,
                Issues = issues,
                Recommendations = recommendations,
                Summary = GenerateSummary(statistics, issues)
            };
        }

        public async Task CleanupOldDataAsync(TimeSpan retentionPeriod)
        {
            var cutoffTime = DateTime.Now - retentionPeriod;
            var recordsToKeep = new ConcurrentQueue<PerformanceRecord>();
            
            while (_performanceRecords.TryDequeue(out var record))
            {
                if (record.Timestamp >= cutoffTime)
                {
                    recordsToKeep.Enqueue(record);
                }
            }

            // 替换队列内容
            while (recordsToKeep.TryDequeue(out var record))
            {
                _performanceRecords.Enqueue(record);
            }

            _logger.LogInformation("性能数据清理完成，保留期限: {RetentionPeriod}", retentionPeriod);
            await Task.CompletedTask;
        }

        public void SetPerformanceThresholds(PerformanceThresholds thresholds)
        {
            _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
            _logger.LogInformation("性能阈值已更新");
        }

        #region 私有方法

        private void MonitorMemoryUsage(object? state)
        {
            try
            {
                _currentProcess.Refresh();
                var memoryUsage = _currentProcess.WorkingSet64;
                RecordMemoryUsage(memoryUsage, "System");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "内存监控失败");
            }
        }

        private void UpdateComponentMetrics(string operationName, TimeSpan duration, bool success)
        {
            _componentMetrics.AddOrUpdate(operationName,
                new PerformanceMetrics 
                { 
                    TotalOperations = 1,
                    SuccessfulOperations = success ? 1 : 0,
                    TotalDuration = duration,
                    MaxDuration = duration,
                    MinDuration = duration
                },
                (key, existing) =>
                {
                    existing.TotalOperations++;
                    if (success) existing.SuccessfulOperations++;
                    existing.TotalDuration = existing.TotalDuration.Add(duration);
                    existing.MaxDuration = duration > existing.MaxDuration ? duration : existing.MaxDuration;
                    existing.MinDuration = duration < existing.MinDuration ? duration : existing.MinDuration;
                    return existing;
                });
        }

        private void CheckPerformanceThresholds(string operationName, TimeSpan duration, bool success)
        {
            if (duration > _thresholds.MaxOperationTime)
            {
                OnPerformanceWarning("OperationTime", $"操作执行时间超过阈值: {duration.TotalMilliseconds:F2}ms", 
                    operationName, duration, _thresholds.MaxOperationTime);
            }

            var metrics = _componentMetrics.GetValueOrDefault(operationName);
            if (metrics != null && metrics.TotalOperations >= 10)
            {
                var successRate = (double)metrics.SuccessfulOperations / metrics.TotalOperations * 100;
                if (successRate < _thresholds.MinSuccessRate)
                {
                    OnPerformanceWarning("SuccessRate", $"操作成功率低于阈值: {successRate:F1}%", 
                        operationName, successRate, _thresholds.MinSuccessRate);
                }
            }
        }

        private void OnPerformanceWarning(string warningType, string message, string component, object? value, object? threshold)
        {
            var args = new PerformanceWarningEventArgs
            {
                WarningType = warningType,
                Message = message,
                Component = component,
                Value = value,
                Threshold = threshold
            };

            _logger.LogWarning("性能警告: {WarningType} - {Message}", warningType, message);
            PerformanceWarning?.Invoke(this, args);
        }

        private async Task<List<PerformanceIssue>> AnalyzePerformanceIssuesAsync(List<PerformanceRecord> records)
        {
            var issues = new List<PerformanceIssue>();

            // 分析慢操作
            var slowOperations = records
                .Where(r => r.Duration > _thresholds.MaxOperationTime)
                .GroupBy(r => r.OperationName)
                .Where(g => g.Count() >= 3)
                .ToList();

            foreach (var group in slowOperations)
            {
                issues.Add(new PerformanceIssue
                {
                    Type = "SlowOperation",
                    Description = $"操作 '{group.Key}' 频繁超时，发生 {group.Count()} 次",
                    Severity = "High",
                    Component = group.Key,
                    OccurredAt = group.Max(r => r.Timestamp),
                    Details = new Dictionary<string, object>
                    {
                        ["AverageDuration"] = TimeSpan.FromMilliseconds(group.Average(r => r.Duration.TotalMilliseconds)),
                        ["MaxDuration"] = group.Max(r => r.Duration),
                        ["Occurrences"] = group.Count()
                    }
                });
            }

            // 分析失败率高的操作
            var failureGroups = records
                .GroupBy(r => r.OperationName)
                .Where(g => g.Count() >= 5)
                .Where(g => (double)g.Count(r => !r.Success) / g.Count() > 0.1)
                .ToList();

            foreach (var group in failureGroups)
            {
                var failureRate = (double)group.Count(r => !r.Success) / group.Count() * 100;
                issues.Add(new PerformanceIssue
                {
                    Type = "HighFailureRate",
                    Description = $"操作 '{group.Key}' 失败率高达 {failureRate:F1}%",
                    Severity = failureRate > 30 ? "Critical" : "High",
                    Component = group.Key,
                    OccurredAt = group.Max(r => r.Timestamp),
                    Details = new Dictionary<string, object>
                    {
                        ["FailureRate"] = failureRate,
                        ["TotalAttempts"] = group.Count(),
                        ["Failures"] = group.Count(r => !r.Success)
                    }
                });
            }

            await Task.CompletedTask;
            return issues;
        }

        private List<PerformanceRecommendation> GenerateRecommendations(PerformanceStatistics statistics, List<PerformanceIssue> issues)
        {
            var recommendations = new List<PerformanceRecommendation>();

            // 基于内存使用的建议
            if (statistics.CurrentMemoryUsage > _thresholds.MaxMemoryUsage * 0.8)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Title = "优化内存使用",
                    Description = "当前内存使用接近阈值，建议进行内存优化",
                    Priority = "High",
                    Category = "Memory",
                    ActionItems = new[]
                    {
                        "检查是否存在内存泄漏",
                        "清理不必要的缓存数据",
                        "优化大对象的生命周期管理",
                        "考虑实施对象池模式"
                    }
                });
            }

            // 基于慢操作的建议
            var slowOperationIssues = issues.Where(i => i.Type == "SlowOperation").ToList();
            if (slowOperationIssues.Any())
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Title = "优化慢操作性能",
                    Description = $"发现 {slowOperationIssues.Count} 个慢操作问题需要优化",
                    Priority = "High",
                    Category = "Performance",
                    ActionItems = new[]
                    {
                        "分析慢操作的根本原因",
                        "考虑异步处理和并行优化",
                        "优化数据库查询和API调用",
                        "实施缓存策略减少重复计算"
                    }
                });
            }

            // 基于UI响应时间的建议
            var slowUIResponses = statistics.UIResponseTimes.Where(kvp => kvp.Value > _thresholds.MaxUIResponseTime).ToList();
            if (slowUIResponses.Any())
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Title = "优化UI响应性能",
                    Description = $"发现 {slowUIResponses.Count} 个UI元素响应缓慢",
                    Priority = "Medium",
                    Category = "UI",
                    ActionItems = new[]
                    {
                        "实施UI虚拟化减少渲染负担",
                        "优化数据绑定性能",
                        "使用异步加载避免UI阻塞",
                        "考虑延迟加载和按需渲染"
                    }
                });
            }

            return recommendations;
        }

        private string GenerateSummary(PerformanceStatistics statistics, List<PerformanceIssue> issues)
        {
            var summary = $"性能报告摘要:\n";
            summary += $"- 总操作数: {statistics.TotalOperations:N0}\n";
            summary += $"- 成功率: {statistics.SuccessRate:F1}%\n";
            summary += $"- 平均响应时间: {statistics.AverageOperationTime.TotalMilliseconds:F2}ms\n";
            summary += $"- 当前内存使用: {FormatBytes(statistics.CurrentMemoryUsage)}\n";
            summary += $"- 峰值内存使用: {FormatBytes(statistics.PeakMemoryUsage)}\n";
            summary += $"- 发现的问题数: {issues.Count}\n";

            var criticalIssues = issues.Count(i => i.Severity == "Critical");
            var highIssues = issues.Count(i => i.Severity == "High");
            
            if (criticalIssues > 0)
                summary += $"- 严重问题: {criticalIssues}\n";
            if (highIssues > 0)
                summary += $"- 高优先级问题: {highIssues}\n";

            return summary;
        }

        private static string FormatBytes(long bytes)
        {
            const long k = 1024;
            if (bytes < k) return $"{bytes} B";
            if (bytes < k * k) return $"{bytes / k:F1} KB";
            if (bytes < k * k * k) return $"{bytes / (k * k):F1} MB";
            return $"{bytes / (k * k * k):F1} GB";
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _memoryMonitorTimer?.Dispose();
            _currentProcess?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// 性能记录
    /// </summary>
    internal class PerformanceRecord
    {
        public string OperationName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
        public int ThreadId { get; set; }
    }

    /// <summary>
    /// 组件性能指标
    /// </summary>
    internal class PerformanceMetrics
    {
        public long TotalOperations { get; set; }
        public long SuccessfulOperations { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan MinDuration { get; set; } = TimeSpan.MaxValue;
        
        public long MemoryUsage { get; set; }
        public long PeakMemoryUsage { get; set; }
        
        public TimeSpan AverageUIResponseTime { get; set; }
        public TimeSpan MaxUIResponseTime { get; set; }
        public int UIResponseCount { get; set; }
    }

    /// <summary>
    /// 性能监控会话实现
    /// </summary>
    internal class PerformanceSession : IPerformanceSession
    {
        private readonly PerformanceMonitorService _monitorService;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;
        private readonly List<(string Name, TimeSpan Time)> _milestones = new();
        private readonly Dictionary<string, object> _metrics = new();
        private bool _disposed;
        
        public string OperationName { get; }
        public string? Category { get; }
        public DateTime StartTime { get; }
        
        private bool _success = true;
        private string? _details;

        public PerformanceSession(string operationName, string? category, PerformanceMonitorService monitorService, ILogger logger)
        {
            OperationName = operationName;
            Category = category;
            _monitorService = monitorService;
            _logger = logger;
            StartTime = DateTime.Now;
            _stopwatch = Stopwatch.StartNew();
        }

        public void AddMilestone(string name)
        {
            if (_disposed) return;
            _milestones.Add((name, _stopwatch.Elapsed));
        }

        public void SetResult(bool success, string? details = null)
        {
            if (_disposed) return;
            _success = success;
            _details = details;
        }

        public void AddMetric(string name, object value)
        {
            if (_disposed) return;
            _metrics[name] = value;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopwatch.Stop();
            var duration = _stopwatch.Elapsed;

            try
            {
                _ = _monitorService.RecordOperationAsync(OperationName, duration, _success, _details);
                
                if (_milestones.Any() || _metrics.Any())
                {
                    _logger.LogDebug("操作 '{OperationName}' 完成，耗时: {Duration}ms，里程碑: {Milestones}，指标: {Metrics}",
                        OperationName, duration.TotalMilliseconds, _milestones, _metrics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录性能会话失败");
            }
        }
    }
}