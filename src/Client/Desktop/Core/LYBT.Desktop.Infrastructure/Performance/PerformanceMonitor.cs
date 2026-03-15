using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using LYBT.Desktop.Contracts.Performance;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Performance
{
    /// <summary>
    /// 性能监控服务实现
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor>? _logger;
        private readonly Dictionary<string, Stopwatch> _activeTimings = new();
        private readonly Dictionary<string, long> _memorySnapshots = new();
        private readonly List<PerformanceMetric> _completedMetrics = new();
        private readonly object _lock = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        public PerformanceMonitor(ILogger<PerformanceMonitor>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void StartTiming(string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("操作名称不能为空", nameof(operationName));

            lock (_lock)
            {
                if (_activeTimings.ContainsKey(operationName))
                {
                    _logger?.LogWarning("操作 {OperationName} 的计时已在进行中，将重新开始", operationName);
                    _activeTimings[operationName].Restart();
                }
                else
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    _activeTimings[operationName] = stopwatch;
                }

                // 记录内存基线
                _memorySnapshots[$"{operationName}_start"] = GC.GetTotalMemory(true);
            }

            _logger?.LogDebug("开始计时: {OperationName}", operationName);
        }

        /// <inheritdoc/>
        public long StopTiming(string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("操作名称不能为空", nameof(operationName));

            long durationMs;
            long memoryBefore;
            long memoryAfter;

            lock (_lock)
            {
                if (!_activeTimings.TryGetValue(operationName, out var stopwatch))
                {
                    _logger?.LogWarning("操作 {OperationName} 没有正在进行的计时", operationName);
                    return 0;
                }

                stopwatch.Stop();
                durationMs = stopwatch.ElapsedMilliseconds;
                _activeTimings.Remove(operationName);

                // 获取内存数据
                _memorySnapshots.TryGetValue($"{operationName}_start", out memoryBefore);
                memoryAfter = GC.GetTotalMemory(false);
                _memorySnapshots[$"{operationName}_end"] = memoryAfter;

                // 创建性能指标
                var metric = new PerformanceMetric
                {
                    OperationName = operationName,
                    DurationMs = durationMs,
                    MemoryBeforeBytes = memoryBefore,
                    MemoryAfterBytes = memoryAfter,
                    Timestamp = DateTime.UtcNow
                };

                _completedMetrics.Add(metric);

                // 触发事件
                MetricRecorded?.Invoke(this, new PerformanceMetricRecordedEventArgs(metric));

                // 根据性能等级记录日志
                LogPerformanceMetric(metric);
            }

            return durationMs;
        }

        /// <inheritdoc/>
        public long RecordMemoryBaseline(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("标签不能为空", nameof(label));

            long memory = GC.GetTotalMemory(true);

            lock (_lock)
            {
                _memorySnapshots[label] = memory;
            }

            _logger?.LogDebug("内存快照 [{Label}]: {MemoryBytes} bytes ({MemoryMB:F2} MB)",
                label, memory, memory / (1024.0 * 1024.0));

            return memory;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, long> GetMemorySnapshots()
        {
            lock (_lock)
            {
                return new Dictionary<string, long>(_memorySnapshots);
            }
        }

        /// <inheritdoc/>
        public PerformanceMetric? GetMetric(string operationName)
        {
            lock (_lock)
            {
                return _completedMetrics.FirstOrDefault(m => m.OperationName == operationName);
            }
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<PerformanceMetric> GetAllMetrics()
        {
            lock (_lock)
            {
                return _completedMetrics.ToList().AsReadOnly();
            }
        }

        /// <inheritdoc/>
        public PerformanceReport GenerateReport()
        {
            lock (_lock)
            {
                return new PerformanceReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    Metrics = _completedMetrics.ToList().AsReadOnly()
                };
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (_lock)
            {
                _activeTimings.Clear();
                _memorySnapshots.Clear();
                _completedMetrics.Clear();
            }

            _logger?.LogDebug("性能监控数据已清除");
        }

        /// <inheritdoc/>
        public event EventHandler<PerformanceMetricRecordedEventArgs>? MetricRecorded;

        private void LogPerformanceMetric(PerformanceMetric metric)
        {
            var level = metric.Level;
            var message = "[{Level}] {OperationName}: {DurationMs}ms, 内存: {MemoryDelta}";

            switch (level)
            {
                case PerformanceLevel.Excellent:
                    _logger?.LogDebug(message, "优秀", metric.OperationName, metric.DurationMs, metric.FormattedMemoryDelta);
                    break;
                case PerformanceLevel.Good:
                    _logger?.LogInformation(message, "良好", metric.OperationName, metric.DurationMs, metric.FormattedMemoryDelta);
                    break;
                case PerformanceLevel.Acceptable:
                    _logger?.LogWarning(message, "可接受", metric.OperationName, metric.DurationMs, metric.FormattedMemoryDelta);
                    break;
                case PerformanceLevel.Poor:
                    _logger?.LogError(message, "需优化", metric.OperationName, metric.DurationMs, metric.FormattedMemoryDelta);
                    break;
            }
        }
    }
}
