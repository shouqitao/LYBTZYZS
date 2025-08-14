using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace LYBT.Infrastructure.Performance
{
    /// <summary>
    /// CQRS操作性能统计
    /// </summary>
    public class CQRSOperationStats
    {
        public string OperationType { get; set; } // Command / Query
        public string OperationName { get; set; }
        public int ExecutionCount { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public double MinExecutionTimeMs { get; set; }
        public double MaxExecutionTimeMs { get; set; }
        public double P95ExecutionTimeMs { get; set; }
        public double P99ExecutionTimeMs { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public double SuccessRate { get; set; }
        public DateTime LastExecuted { get; set; }
        public DateTime FirstExecuted { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// CQRS性能数据点
    /// </summary>
    public class CQRSPerformanceDataPoint
    {
        public string OperationType { get; set; }
        public string OperationName { get; set; }
        public double ExecutionTimeMs { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorType { get; set; }
        public Dictionary<string, object> Tags { get; set; } = new();
    }

    /// <summary>
    /// CQRS性能监控器
    /// </summary>
    public class CQRSPerformanceMonitor : BackgroundService
    {
        private readonly IPerformanceCollector _collector;
        private readonly ILogger<CQRSPerformanceMonitor> _logger;
        
        private readonly ConcurrentDictionary<string, List<double>> _executionTimes = new();
        private readonly ConcurrentDictionary<string, CQRSOperationStats> _operationStats = new();
        private readonly ConcurrentQueue<CQRSPerformanceDataPoint> _dataPoints = new();
        
        private readonly Timer _aggregationTimer;
        private readonly ReaderWriterLockSlim _statsLock = new();

        public CQRSPerformanceMonitor(
            IPerformanceCollector collector,
            ILogger<CQRSPerformanceMonitor> logger)
        {
            _collector = collector;
            _logger = logger;
            
            // 每30秒聚合一次统计数据
            _aggregationTimer = new Timer(AggregateStats, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// 记录CQRS操作性能
        /// </summary>
        public void RecordOperation(
            string operationType,
            string operationName,
            double executionTimeMs,
            bool isSuccess,
            string errorType = null,
            Dictionary<string, object> tags = null)
        {
            var dataPoint = new CQRSPerformanceDataPoint
            {
                OperationType = operationType,
                OperationName = operationName,
                ExecutionTimeMs = executionTimeMs,
                IsSuccess = isSuccess,
                Timestamp = DateTime.UtcNow,
                ErrorType = errorType,
                Tags = tags ?? new Dictionary<string, object>()
            };

            _dataPoints.Enqueue(dataPoint);

            // 记录到性能收集器
            _collector.Histogram($"cqrs.{operationType.ToLower()}.duration", executionTimeMs, new Dictionary<string, object>
            {
                ["operation_type"] = operationType,
                ["operation_name"] = operationName,
                ["is_success"] = isSuccess,
                ["error_type"] = errorType
            });

            _collector.Counter($"cqrs.{operationType.ToLower()}.count", 1, new Dictionary<string, object>
            {
                ["operation_type"] = operationType,
                ["operation_name"] = operationName,
                ["status"] = isSuccess ? "success" : "error"
            });

            // 更新内存中的执行时间
            var key = $"{operationType}.{operationName}";
            _executionTimes.AddOrUpdate(key, 
                new List<double> { executionTimeMs },
                (k, existing) => 
                {
                    lock (existing)
                    {
                        existing.Add(executionTimeMs);
                        
                        // 保持最近1000次记录
                        if (existing.Count > 1000)
                        {
                            existing.RemoveRange(0, 100);
                        }
                    }
                    return existing;
                });
        }

        /// <summary>
        /// 获取操作统计信息
        /// </summary>
        public Dictionary<string, CQRSOperationStats> GetOperationStats()
        {
            _statsLock.EnterReadLock();
            try
            {
                return new Dictionary<string, CQRSOperationStats>(_operationStats);
            }
            finally
            {
                _statsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 获取特定操作的统计信息
        /// </summary>
        public CQRSOperationStats GetOperationStats(string operationType, string operationName)
        {
            var key = $"{operationType}.{operationName}";
            
            _statsLock.EnterReadLock();
            try
            {
                return _operationStats.TryGetValue(key, out var stats) ? stats : null;
            }
            finally
            {
                _statsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public CQRSPerformanceReport GetPerformanceReport()
        {
            var stats = GetOperationStats();
            var report = new CQRSPerformanceReport
            {
                GeneratedAt = DateTime.UtcNow,
                TotalOperations = stats.Values.Sum(s => s.ExecutionCount),
                AverageResponseTime = stats.Values.Any() ? stats.Values.Average(s => s.AverageExecutionTimeMs) : 0,
                OverallSuccessRate = stats.Values.Any() ? stats.Values.Average(s => s.SuccessRate) : 0,
                CommandStats = stats.Values.Where(s => s.OperationType == "Command").ToList(),
                QueryStats = stats.Values.Where(s => s.OperationType == "Query").ToList()
            };

            // 识别慢操作
            var slowThreshold = 1000; // 1秒
            report.SlowOperations = stats.Values
                .Where(s => s.AverageExecutionTimeMs > slowThreshold)
                .OrderByDescending(s => s.AverageExecutionTimeMs)
                .ToList();

            // 识别错误率高的操作
            var errorThreshold = 0.05; // 5%
            report.ErrorProneOperations = stats.Values
                .Where(s => s.SuccessRate < (1 - errorThreshold))
                .OrderBy(s => s.SuccessRate)
                .ToList();

            return report;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    
                    // 记录系统级别的性能指标
                    RecordSystemMetrics();
                    
                    // 生成性能报告
                    var report = GetPerformanceReport();
                    LogPerformanceSummary(report);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CQRS performance monitoring background service");
                }
            }
        }

        /// <summary>
        /// 聚合统计数据
        /// </summary>
        private void AggregateStats(object state)
        {
            try
            {
                var processedDataPoints = new List<CQRSPerformanceDataPoint>();
                
                // 处理队列中的数据点
                while (_dataPoints.TryDequeue(out var dataPoint))
                {
                    processedDataPoints.Add(dataPoint);
                }

                if (!processedDataPoints.Any()) return;

                _statsLock.EnterWriteLock();
                try
                {
                    // 按操作类型和名称分组
                    var groupedData = processedDataPoints.GroupBy(dp => $"{dp.OperationType}.{dp.OperationName}");

                    foreach (var group in groupedData)
                    {
                        var key = group.Key;
                        var dataPoints = group.ToList();
                        var operationType = dataPoints.First().OperationType;
                        var operationName = dataPoints.First().OperationName;

                        var existingStats = _operationStats.GetOrAdd(key, k => new CQRSOperationStats
                        {
                            OperationType = operationType,
                            OperationName = operationName,
                            FirstExecuted = DateTime.UtcNow
                        });

                        // 更新统计信息
                        var executionTimes = _executionTimes.TryGetValue(key, out var times) ? times : new List<double>();
                        
                        lock (executionTimes)
                        {
                            if (executionTimes.Any())
                            {
                                existingStats.ExecutionCount = executionTimes.Count;
                                existingStats.AverageExecutionTimeMs = Math.Round(executionTimes.Average(), 2);
                                existingStats.MinExecutionTimeMs = Math.Round(executionTimes.Min(), 2);
                                existingStats.MaxExecutionTimeMs = Math.Round(executionTimes.Max(), 2);
                                
                                // 计算百分位数
                                var sortedTimes = executionTimes.OrderBy(t => t).ToList();
                                existingStats.P95ExecutionTimeMs = Math.Round(GetPercentile(sortedTimes, 0.95), 2);
                                existingStats.P99ExecutionTimeMs = Math.Round(GetPercentile(sortedTimes, 0.99), 2);
                            }
                        }

                        var successCount = dataPoints.Count(dp => dp.IsSuccess);
                        var errorCount = dataPoints.Count(dp => !dp.IsSuccess);
                        
                        existingStats.SuccessCount += successCount;
                        existingStats.ErrorCount += errorCount;
                        existingStats.SuccessRate = existingStats.ExecutionCount > 0 ? 
                            Math.Round((double)existingStats.SuccessCount / existingStats.ExecutionCount, 4) : 0;
                        existingStats.LastExecuted = dataPoints.Max(dp => dp.Timestamp);
                    }
                }
                finally
                {
                    _statsLock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating CQRS performance stats");
            }
        }

        /// <summary>
        /// 计算百分位数
        /// </summary>
        private double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (!sortedValues.Any()) return 0;
            
            var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
            index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
            return sortedValues[index];
        }

        /// <summary>
        /// 记录系统指标
        /// </summary>
        private void RecordSystemMetrics()
        {
            using var monitor = new SystemPerformanceMonitor();
            var sysInfo = monitor.GetCurrentInfo();
            
            _collector.Gauge("system.cpu.usage_percent", sysInfo.CpuUsagePercent);
            _collector.Gauge("system.memory.used_bytes", sysInfo.MemoryUsedBytes);
            _collector.Gauge("system.memory.usage_percent", sysInfo.MemoryUsagePercent);
            _collector.Gauge("system.thread_count", sysInfo.ThreadCount);
            _collector.Gauge("system.gc.gen0_collections", sysInfo.GcGen0Collections);
            _collector.Gauge("system.gc.gen1_collections", sysInfo.GcGen1Collections);
            _collector.Gauge("system.gc.gen2_collections", sysInfo.GcGen2Collections);
            _collector.Gauge("system.gc.total_memory", sysInfo.GcTotalMemory);
        }

        /// <summary>
        /// 记录性能摘要日志
        /// </summary>
        private void LogPerformanceSummary(CQRSPerformanceReport report)
        {
            _logger.LogInformation("CQRS Performance Summary: {TotalOperations} operations, {AverageResponseTime:F2}ms avg response, {OverallSuccessRate:P2} success rate",
                report.TotalOperations, report.AverageResponseTime, report.OverallSuccessRate);

            if (report.SlowOperations.Any())
            {
                _logger.LogWarning("Slow operations detected: {SlowOperationsCount}", report.SlowOperations.Count);
                foreach (var slowOp in report.SlowOperations.Take(5))
                {
                    _logger.LogWarning("Slow operation: {OperationType}.{OperationName} - {AverageTime:F2}ms avg",
                        slowOp.OperationType, slowOp.OperationName, slowOp.AverageExecutionTimeMs);
                }
            }

            if (report.ErrorProneOperations.Any())
            {
                _logger.LogWarning("Error-prone operations detected: {ErrorProneOperationsCount}", report.ErrorProneOperations.Count);
                foreach (var errorOp in report.ErrorProneOperations.Take(5))
                {
                    _logger.LogWarning("Error-prone operation: {OperationType}.{OperationName} - {SuccessRate:P2} success rate",
                        errorOp.OperationType, errorOp.OperationName, errorOp.SuccessRate);
                }
            }
        }

        public override void Dispose()
        {
            _aggregationTimer?.Dispose();
            _statsLock?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// CQRS性能报告
    /// </summary>
    public class CQRSPerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalOperations { get; set; }
        public double AverageResponseTime { get; set; }
        public double OverallSuccessRate { get; set; }
        public List<CQRSOperationStats> CommandStats { get; set; } = new();
        public List<CQRSOperationStats> QueryStats { get; set; } = new();
        public List<CQRSOperationStats> SlowOperations { get; set; } = new();
        public List<CQRSOperationStats> ErrorProneOperations { get; set; } = new();
    }
}