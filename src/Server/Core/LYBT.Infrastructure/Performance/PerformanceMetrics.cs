using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;

namespace LYBT.Infrastructure.Performance
{
    /// <summary>
    /// 性能指标数据结构 - UltraThink重构性能监控架构
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Tags { get; set; } = new();
    }

    /// <summary>
    /// 操作性能上下文
    /// </summary>
    public class OperationPerformanceContext : IDisposable
    {
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly IPerformanceCollector _collector;
        private readonly Dictionary<string, object> _metadata;

        public OperationPerformanceContext(string operationName, IPerformanceCollector collector, Dictionary<string, object> metadata = null)
        {
            _operationName = operationName;
            _collector = collector;
            _metadata = metadata ?? new Dictionary<string, object>();
            _stopwatch = Stopwatch.StartNew();
        }

        public void AddMetadata(string key, object value)
        {
            _metadata[key] = value;
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            
            var metric = new PerformanceMetric
            {
                Name = $"{_operationName}.duration",
                Value = _stopwatch.ElapsedMilliseconds,
                Unit = "ms",
                Tags = new Dictionary<string, object>(_metadata)
                {
                    ["operation"] = _operationName,
                    ["duration_ticks"] = _stopwatch.ElapsedTicks
                }
            };

            _collector.Record(metric);
        }
    }

    /// <summary>
    /// 性能指标收集器接口
    /// </summary>
    public interface IPerformanceCollector
    {
        void Record(PerformanceMetric metric);
        void Counter(string name, int value = 1, Dictionary<string, object> tags = null);
        void Gauge(string name, double value, Dictionary<string, object> tags = null);
        void Histogram(string name, double value, Dictionary<string, object> tags = null);
        IDisposable StartTimer(string name, Dictionary<string, object> tags = null);
    }

    /// <summary>
    /// 内存性能指标收集器
    /// </summary>
    public class InMemoryPerformanceCollector : IPerformanceCollector
    {
        private readonly List<PerformanceMetric> _metrics = new();
        private readonly ReaderWriterLockSlim _lock = new();

        public void Record(PerformanceMetric metric)
        {
            _lock.EnterWriteLock();
            try
            {
                _metrics.Add(metric);
                
                // 保持最近10000条记录
                if (_metrics.Count > 10000)
                {
                    _metrics.RemoveRange(0, 1000);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Counter(string name, int value = 1, Dictionary<string, object> tags = null)
        {
            Record(new PerformanceMetric
            {
                Name = name,
                Value = value,
                Unit = "count",
                Tags = tags ?? new Dictionary<string, object>()
            });
        }

        public void Gauge(string name, double value, Dictionary<string, object> tags = null)
        {
            Record(new PerformanceMetric
            {
                Name = name,
                Value = value,
                Unit = "gauge",
                Tags = tags ?? new Dictionary<string, object>()
            });
        }

        public void Histogram(string name, double value, Dictionary<string, object> tags = null)
        {
            Record(new PerformanceMetric
            {
                Name = name,
                Value = value,
                Unit = "histogram",
                Tags = tags ?? new Dictionary<string, object>()
            });
        }

        public IDisposable StartTimer(string name, Dictionary<string, object> tags = null)
        {
            return new OperationPerformanceContext(name, this, tags);
        }

        /// <summary>
        /// 获取所有指标
        /// </summary>
        public List<PerformanceMetric> GetMetrics()
        {
            _lock.EnterReadLock();
            try
            {
                return new List<PerformanceMetric>(_metrics);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 获取指定时间范围内的指标
        /// </summary>
        public List<PerformanceMetric> GetMetrics(DateTime from, DateTime to)
        {
            _lock.EnterReadLock();
            try
            {
                var filtered = new List<PerformanceMetric>();
                foreach (var metric in _metrics)
                {
                    if (metric.Timestamp >= from && metric.Timestamp <= to)
                    {
                        filtered.Add(metric);
                    }
                }
                return filtered;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 清理指标
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _metrics.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// 系统性能信息
    /// </summary>
    public class SystemPerformanceInfo
    {
        public double CpuUsagePercent { get; set; }
        public long MemoryUsedBytes { get; set; }
        public long MemoryTotalBytes { get; set; }
        public double MemoryUsagePercent { get; set; }
        public long DiskUsedBytes { get; set; }
        public long DiskTotalBytes { get; set; }
        public double DiskUsagePercent { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public long GcGen0Collections { get; set; }
        public long GcGen1Collections { get; set; }
        public long GcGen2Collections { get; set; }
        public long GcTotalMemory { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 系统性能监控器
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class SystemPerformanceMonitor : IDisposable
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly Process _currentProcess;

        public SystemPerformanceMonitor()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _currentProcess = Process.GetCurrentProcess();
        }

        /// <summary>
        /// 获取当前系统性能信息
        /// </summary>
        public SystemPerformanceInfo GetCurrentInfo()
        {
            var gcInfo = GC.GetTotalMemory(false);
            
            return new SystemPerformanceInfo
            {
                CpuUsagePercent = Math.Round(_cpuCounter.NextValue(), 2),
                MemoryUsedBytes = _currentProcess.WorkingSet64,
                MemoryTotalBytes = GC.GetTotalMemory(false),
                MemoryUsagePercent = Math.Round((double)_currentProcess.WorkingSet64 / (1024 * 1024 * 1024), 2), // GB
                ThreadCount = _currentProcess.Threads.Count,
                HandleCount = _currentProcess.HandleCount,
                GcGen0Collections = GC.CollectionCount(0),
                GcGen1Collections = GC.CollectionCount(1),
                GcGen2Collections = GC.CollectionCount(2),
                GcTotalMemory = gcInfo
            };
        }

        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _currentProcess?.Dispose();
        }
    }
}