using System.Diagnostics;

namespace LYBT.Infrastructure.Logging
{
    /// <summary>
    /// 性能跟踪器实现 - UltraThink性能监控
    /// 职责单一：专注性能数据收集和跟踪
    /// 代码干净：清晰的检查点管理和指标收集
    /// 性能出色：低开销的实时性能监控
    /// </summary>
    internal class PerformanceTracker : IPerformanceTracker
    {
        private readonly UnifiedLogger _logger;
        private readonly Stopwatch _stopwatch;
        private readonly Dictionary<string, object> _context = new();
        private readonly Dictionary<string, double> _metrics = new();
        private readonly List<Checkpoint> _checkpoints = new();
        private bool _disposed = false;

        public string Id { get; } = Guid.NewGuid().ToString();
        public string Operation { get; }
        public DateTime StartTime { get; }
        public TimeSpan Duration => _stopwatch.Elapsed;

        public PerformanceTracker(string operation, UnifiedLogger logger)
        {
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            StartTime = DateTime.UtcNow;
            _stopwatch = Stopwatch.StartNew();
            
            // 记录初始系统指标
            RecordInitialMetrics();
        }

        /// <summary>
        /// 添加上下文数据
        /// </summary>
        public void AddContext(string key, object value)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(value);
            
            _context[key] = value;
        }

        /// <summary>
        /// 添加性能指标
        /// </summary>
        public void AddMetric(string name, double value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            
            _metrics[name] = value;
        }

        /// <summary>
        /// 标记检查点
        /// </summary>
        public void Checkpoint(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            
            var checkpoint = new Checkpoint
            {
                Name = name,
                Timestamp = DateTime.UtcNow,
                ElapsedTime = _stopwatch.Elapsed
            };
            
            _checkpoints.Add(checkpoint);
        }

        /// <summary>
        /// 完成跟踪
        /// </summary>
        public async Task CompleteAsync(string? result = null)
        {
            if (_disposed) return;
            
            _stopwatch.Stop();
            
            // 记录最终系统指标
            RecordFinalMetrics();
            
            // 通知logger完成跟踪
            await _logger.CompletePerformanceTracking(this, result);
            
            Dispose();
        }

        /// <summary>
        /// 获取上下文数据
        /// </summary>
        public object GetContext()
        {
            var context = new Dictionary<string, object>(_context);
            
            if (_checkpoints.Count > 0)
            {
                context["Checkpoints"] = _checkpoints.Select(c => new
                {
                    c.Name,
                    c.Timestamp,
                    ElapsedMs = c.ElapsedTime.TotalMilliseconds
                }).ToList();
            }
            
            return context;
        }

        /// <summary>
        /// 获取性能指标
        /// </summary>
        public PerformanceMetrics GetMetrics()
        {
            var metrics = new PerformanceMetrics
            {
                CustomMetrics = new Dictionary<string, double>(_metrics)
            };
            
            // 添加系统指标
            try
            {
                var process = Process.GetCurrentProcess();
                metrics.CpuUsagePercent = CalculateCpuUsage();
                metrics.MemoryUsageMB = process.WorkingSet64 / 1024 / 1024;
            }
            catch (Exception)
            {
                // 如果无法获取系统指标，使用默认值
                metrics.CpuUsagePercent = 0;
                metrics.MemoryUsageMB = 0;
            }
            
            return metrics;
        }

        /// <summary>
        /// 记录初始指标
        /// </summary>
        private void RecordInitialMetrics()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                _metrics["InitialMemoryMB"] = process.WorkingSet64 / 1024.0 / 1024.0;
                _metrics["InitialCpuTime"] = process.TotalProcessorTime.TotalMilliseconds;
                
                // 记录GC信息
                _metrics["InitialGen0Collections"] = GC.CollectionCount(0);
                _metrics["InitialGen1Collections"] = GC.CollectionCount(1);
                _metrics["InitialGen2Collections"] = GC.CollectionCount(2);
                _metrics["InitialTotalMemory"] = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            }
            catch (Exception)
            {
                // 忽略获取初始指标时的异常
            }
        }

        /// <summary>
        /// 记录最终指标
        /// </summary>
        private void RecordFinalMetrics()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var finalMemoryMB = process.WorkingSet64 / 1024.0 / 1024.0;
                var finalCpuTime = process.TotalProcessorTime.TotalMilliseconds;
                
                _metrics["FinalMemoryMB"] = finalMemoryMB;
                _metrics["FinalCpuTime"] = finalCpuTime;
                
                // 计算差值
                if (_metrics.ContainsKey("InitialMemoryMB"))
                {
                    _metrics["MemoryDeltaMB"] = finalMemoryMB - _metrics["InitialMemoryMB"];
                }
                
                if (_metrics.ContainsKey("InitialCpuTime"))
                {
                    _metrics["CpuDeltaMs"] = finalCpuTime - _metrics["InitialCpuTime"];
                }
                
                // GC指标
                _metrics["FinalGen0Collections"] = GC.CollectionCount(0);
                _metrics["FinalGen1Collections"] = GC.CollectionCount(1);
                _metrics["FinalGen2Collections"] = GC.CollectionCount(2);
                _metrics["FinalTotalMemory"] = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                
                // GC差值
                if (_metrics.ContainsKey("InitialGen0Collections"))
                {
                    _metrics["Gen0CollectionsDelta"] = _metrics["FinalGen0Collections"] - _metrics["InitialGen0Collections"];
                    _metrics["Gen1CollectionsDelta"] = _metrics["FinalGen1Collections"] - _metrics["InitialGen1Collections"];
                    _metrics["Gen2CollectionsDelta"] = _metrics["FinalGen2Collections"] - _metrics["InitialGen2Collections"];
                    _metrics["TotalMemoryDeltaMB"] = _metrics["FinalTotalMemory"] - _metrics["InitialTotalMemory"];
                }
            }
            catch (Exception)
            {
                // 忽略获取最终指标时的异常
            }
        }

        /// <summary>
        /// 计算CPU使用率（简化实现）
        /// </summary>
        private double CalculateCpuUsage()
        {
            // 这是一个简化的CPU使用率计算
            // 实际生产环境中可能需要更复杂的计算逻辑
            if (_metrics.ContainsKey("InitialCpuTime") && _metrics.ContainsKey("CpuDeltaMs"))
            {
                var cpuDelta = _metrics["CpuDeltaMs"];
                var wallClockTime = Duration.TotalMilliseconds;
                
                if (wallClockTime > 0)
                {
                    return (cpuDelta / wallClockTime) * 100.0;
                }
            }
            
            return 0;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _stopwatch?.Stop();
            _checkpoints.Clear();
            _context.Clear();
            _metrics.Clear();
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 检查点信息
    /// </summary>
    internal class Checkpoint
    {
        public string Name { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}