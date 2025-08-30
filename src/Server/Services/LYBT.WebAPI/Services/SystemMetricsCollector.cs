using System.Collections.Concurrent;
using System.Diagnostics;
using LYBT.WebAPI.Middleware;

namespace LYBT.WebAPI.Services
{
    /// <summary>
    /// 系统指标收集器接口
    /// </summary>
    public interface ISystemMetricsCollector
    {
        /// <summary>
        /// 记录请求性能指标
        /// </summary>
        Task RecordRequestMetricsAsync(RequestPerformanceMetrics metrics);

        /// <summary>
        /// 获取API性能统计
        /// </summary>
        Task<ApiPerformanceStats> GetApiPerformanceStatsAsync();

        /// <summary>
        /// 获取错误统计
        /// </summary>
        Task<ErrorStats> GetErrorStatsAsync();

        /// <summary>
        /// 获取系统性能趋势
        /// </summary>
        Task<SystemPerformanceTrend> GetPerformanceTrendAsync(TimeSpan period);

        /// <summary>
        /// 获取热点API统计
        /// </summary>
        Task<List<ApiEndpointStats>> GetHotApiEndpointsAsync(int topCount = 10);

        /// <summary>
        /// 清理过期指标数据
        /// </summary>
        Task CleanupExpiredMetricsAsync();
    }

    /// <summary>
    /// 系统指标收集器实现
    /// </summary>
    public class SystemMetricsCollector : ISystemMetricsCollector
    {
        private readonly ILogger<SystemMetricsCollector> _logger;
        private readonly ConcurrentQueue<RequestPerformanceMetrics> _requestMetrics;
        private readonly ConcurrentDictionary<string, ApiEndpointMetrics> _endpointMetrics;
        private readonly ConcurrentQueue<SystemSnapshot> _systemSnapshots;
        private readonly Timer _cleanupTimer;
        private readonly Timer _snapshotTimer;

        // 配置
        private readonly TimeSpan _metricsRetentionPeriod = TimeSpan.FromHours(24);
        private readonly int _maxMetricsInMemory = 10000;
        
        public SystemMetricsCollector(ILogger<SystemMetricsCollector> logger)
        {
            _logger = logger;
            _requestMetrics = new ConcurrentQueue<RequestPerformanceMetrics>();
            _endpointMetrics = new ConcurrentDictionary<string, ApiEndpointMetrics>();
            _systemSnapshots = new ConcurrentQueue<SystemSnapshot>();

            // 定期清理过期数据 (每小时)
            _cleanupTimer = new Timer(async _ => await CleanupExpiredMetricsAsync(), 
                null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            // 定期收集系统快照 (每分钟)
            _snapshotTimer = new Timer(async _ => await TakeSystemSnapshotAsync(),
                null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public async Task RecordRequestMetricsAsync(RequestPerformanceMetrics metrics)
        {
            try
            {
                // 添加到请求指标队列
                _requestMetrics.Enqueue(metrics);

                // 更新端点指标
                var endpointKey = $"{metrics.Method}:{NormalizePath(metrics.Path)}";
                _endpointMetrics.AddOrUpdate(endpointKey,
                    new ApiEndpointMetrics
                    {
                        Method = metrics.Method,
                        Path = NormalizePath(metrics.Path),
                        RequestCount = 1,
                        TotalDuration = metrics.Duration,
                        SuccessCount = metrics.Success ? 1 : 0,
                        ErrorCount = metrics.Success ? 0 : 1,
                        AverageResponseTime = metrics.Duration,
                        LastAccessTime = metrics.Timestamp
                    },
                    (key, existing) =>
                    {
                        existing.RequestCount++;
                        existing.TotalDuration = existing.TotalDuration.Add(metrics.Duration);
                        existing.AverageResponseTime = TimeSpan.FromTicks(existing.TotalDuration.Ticks / existing.RequestCount);
                        
                        if (metrics.Success)
                            existing.SuccessCount++;
                        else
                            existing.ErrorCount++;

                        existing.LastAccessTime = metrics.Timestamp;
                        
                        // 更新最快和最慢响应时间
                        if (metrics.Duration < existing.FastestResponseTime || existing.FastestResponseTime == TimeSpan.Zero)
                            existing.FastestResponseTime = metrics.Duration;
                        
                        if (metrics.Duration > existing.SlowestResponseTime)
                            existing.SlowestResponseTime = metrics.Duration;

                        return existing;
                    });

                // 限制内存中的指标数量
                await LimitMetricsInMemoryAsync();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录请求指标时发生错误");
            }
        }

        public Task<ApiPerformanceStats> GetApiPerformanceStatsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var recentMetrics = _requestMetrics
                    .Where(m => now - m.Timestamp < TimeSpan.FromHours(1))
                    .ToList();

                if (!recentMetrics.Any())
                {
                    return Task.FromResult(new ApiPerformanceStats());
                }

                var successfulRequests = recentMetrics.Where(m => m.Success).ToList();
                var failedRequests = recentMetrics.Where(m => !m.Success).ToList();

                return Task.FromResult(new ApiPerformanceStats
                {
                    TotalRequests = recentMetrics.Count,
                    SuccessfulRequests = successfulRequests.Count,
                    FailedRequests = failedRequests.Count,
                    SuccessRate = (double)successfulRequests.Count / recentMetrics.Count,
                    AverageResponseTime = TimeSpan.FromTicks((long)recentMetrics.Average(m => m.Duration.Ticks)),
                    MedianResponseTime = CalculateMedian(recentMetrics.Select(m => m.Duration).ToList()),
                    P95ResponseTime = CalculatePercentile(recentMetrics.Select(m => m.Duration).ToList(), 0.95),
                    P99ResponseTime = CalculatePercentile(recentMetrics.Select(m => m.Duration).ToList(), 0.99),
                    RequestsPerMinute = recentMetrics.Count / 60.0,
                    GeneratedAt = now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取API性能统计时发生错误");
                return Task.FromResult(new ApiPerformanceStats());
            }
        }

        public Task<ErrorStats> GetErrorStatsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var recentMetrics = _requestMetrics
                    .Where(m => now - m.Timestamp < TimeSpan.FromHours(1))
                    .ToList();

                var errorMetrics = recentMetrics.Where(m => !m.Success).ToList();

                var errorsByStatus = errorMetrics
                    .GroupBy(m => m.StatusCode)
                    .ToDictionary(g => g.Key, g => g.Count());

                var errorsByEndpoint = errorMetrics
                    .GroupBy(m => $"{m.Method}:{NormalizePath(m.Path)}")
                    .ToDictionary(g => g.Key, g => g.Count());

                var errorsByException = errorMetrics
                    .Where(m => !string.IsNullOrEmpty(m.Exception))
                    .GroupBy(m => m.Exception!)
                    .ToDictionary(g => g.Key, g => g.Count());

                return Task.FromResult(new ErrorStats
                {
                    TotalErrors = errorMetrics.Count,
                    ErrorRate = recentMetrics.Any() ? (double)errorMetrics.Count / recentMetrics.Count : 0,
                    ErrorsByStatusCode = errorsByStatus,
                    ErrorsByEndpoint = errorsByEndpoint,
                    ErrorsByExceptionType = errorsByException,
                    MostCommonError = errorsByException.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "None",
                    GeneratedAt = now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取错误统计时发生错误");
                return Task.FromResult(new ErrorStats());
            }
        }

        public Task<SystemPerformanceTrend> GetPerformanceTrendAsync(TimeSpan period)
        {
            try
            {
                var now = DateTime.UtcNow;
                var startTime = now - period;

                var relevantMetrics = _requestMetrics
                    .Where(m => m.Timestamp >= startTime)
                    .OrderBy(m => m.Timestamp)
                    .ToList();

                var relevantSnapshots = _systemSnapshots
                    .Where(s => s.Timestamp >= startTime)
                    .OrderBy(s => s.Timestamp)
                    .ToList();

                // 按时间间隔分组数据点
                var intervalMinutes = Math.Max(1, (int)period.TotalMinutes / 100); // 最多100个数据点
                var groupedMetrics = relevantMetrics
                    .GroupBy(m => new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day,
                        m.Timestamp.Hour, m.Timestamp.Minute / intervalMinutes * intervalMinutes, 0))
                    .ToDictionary(g => g.Key, g => g.ToList());

                var trendPoints = groupedMetrics.Select(kvp => new TrendDataPoint
                {
                    Timestamp = kvp.Key,
                    RequestCount = kvp.Value.Count,
                    AverageResponseTime = TimeSpan.FromTicks((long)kvp.Value.Average(m => m.Duration.Ticks)),
                    ErrorCount = kvp.Value.Count(m => !m.Success),
                    SuccessRate = (double)kvp.Value.Count(m => m.Success) / kvp.Value.Count
                }).OrderBy(tp => tp.Timestamp).ToList();

                return Task.FromResult(new SystemPerformanceTrend
                {
                    Period = period,
                    StartTime = startTime,
                    EndTime = now,
                    TrendPoints = trendPoints,
                    SystemSnapshots = relevantSnapshots,
                    GeneratedAt = now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取性能趋势时发生错误");
                return Task.FromResult(new SystemPerformanceTrend { Period = period });
            }
        }

        public Task<List<ApiEndpointStats>> GetHotApiEndpointsAsync(int topCount = 10)
        {
            try
            {
                var result = _endpointMetrics.Values
                    .OrderByDescending(m => m.RequestCount)
                    .Take(topCount)
                    .Select(m => new ApiEndpointStats
                    {
                        Method = m.Method,
                        Path = m.Path,
                        RequestCount = m.RequestCount,
                        AverageResponseTime = m.AverageResponseTime,
                        SuccessRate = (double)m.SuccessCount / (m.SuccessCount + m.ErrorCount),
                        ErrorCount = m.ErrorCount,
                        FastestResponseTime = m.FastestResponseTime,
                        SlowestResponseTime = m.SlowestResponseTime,
                        LastAccessTime = m.LastAccessTime
                    })
                    .ToList();
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取热点API端点时发生错误");
                return Task.FromResult(new List<ApiEndpointStats>());
            }
        }

        public async Task CleanupExpiredMetricsAsync()
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - _metricsRetentionPeriod;

                // 清理请求指标
                var metricsToRemove = new List<RequestPerformanceMetrics>();
                while (_requestMetrics.TryPeek(out var metrics) && metrics.Timestamp < cutoffTime)
                {
                    if (_requestMetrics.TryDequeue(out var removed))
                    {
                        metricsToRemove.Add(removed);
                    }
                }

                // 清理系统快照
                while (_systemSnapshots.TryPeek(out var snapshot) && snapshot.Timestamp < cutoffTime)
                {
                    _systemSnapshots.TryDequeue(out _);
                }

                // 清理不活跃的端点指标
                var inactiveEndpoints = _endpointMetrics
                    .Where(kvp => kvp.Value.LastAccessTime < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var endpoint in inactiveEndpoints)
                {
                    _endpointMetrics.TryRemove(endpoint, out _);
                }

                _logger.LogDebug("清理了 {RemovedMetrics} 个请求指标和 {RemovedEndpoints} 个端点指标",
                    metricsToRemove.Count, inactiveEndpoints.Count);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期指标时发生错误");
            }
        }

        private async Task LimitMetricsInMemoryAsync()
        {
            try
            {
                while (_requestMetrics.Count > _maxMetricsInMemory)
                {
                    _requestMetrics.TryDequeue(out _);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "限制内存中指标数量时发生错误");
            }
        }

        private async Task TakeSystemSnapshotAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                
                var snapshot = new SystemSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    CpuUsagePercent = await GetCurrentCpuUsageAsync(),
                    MemoryUsageMB = process.WorkingSet64 / 1024 / 1024,
                    ManagedMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    GCGen0Collections = GC.CollectionCount(0),
                    GCGen1Collections = GC.CollectionCount(1),
                    GCGen2Collections = GC.CollectionCount(2)
                };

                _systemSnapshots.Enqueue(snapshot);

                // 限制快照数量
                while (_systemSnapshots.Count > 1440) // 24小时的分钟数
                {
                    _systemSnapshots.TryDequeue(out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取系统快照时发生错误");
            }
        }

        private static async Task<double> GetCurrentCpuUsageAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
                
                await Task.Delay(100);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return Math.Round(cpuUsageTotal * 100, 2);
            }
            catch
            {
                return 0;
            }
        }

        private static string NormalizePath(string path)
        {
            // 规范化路径，移除参数值
            if (string.IsNullOrEmpty(path))
                return "/";

            // 简单的ID参数替换
            var normalizedPath = path;
            
            // 替换GUID参数
            normalizedPath = System.Text.RegularExpressions.Regex.Replace(
                normalizedPath, 
                @"/[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}",
                "/{id}");
            
            // 替换数字ID参数
            normalizedPath = System.Text.RegularExpressions.Regex.Replace(
                normalizedPath,
                @"/\d+(?=/|$)",
                "/{id}");

            return normalizedPath;
        }

        private static TimeSpan CalculateMedian(List<TimeSpan> values)
        {
            if (!values.Any()) return TimeSpan.Zero;

            var sorted = values.OrderBy(v => v.Ticks).ToList();
            var middle = sorted.Count / 2;
            
            if (sorted.Count % 2 == 0)
            {
                return TimeSpan.FromTicks((sorted[middle - 1].Ticks + sorted[middle].Ticks) / 2);
            }
            
            return sorted[middle];
        }

        private static TimeSpan CalculatePercentile(List<TimeSpan> values, double percentile)
        {
            if (!values.Any()) return TimeSpan.Zero;

            var sorted = values.OrderBy(v => v.Ticks).ToList();
            var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
            index = Math.Max(0, Math.Min(sorted.Count - 1, index));
            
            return sorted[index];
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _snapshotTimer?.Dispose();
        }
    }

    // 数据模型
    public class ApiEndpointMetrics
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long RequestCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public long SuccessCount { get; set; }
        public long ErrorCount { get; set; }
        public TimeSpan FastestResponseTime { get; set; }
        public TimeSpan SlowestResponseTime { get; set; }
        public DateTime LastAccessTime { get; set; }
    }

    public class ApiPerformanceStats
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan MedianResponseTime { get; set; }
        public TimeSpan P95ResponseTime { get; set; }
        public TimeSpan P99ResponseTime { get; set; }
        public double RequestsPerMinute { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class ErrorStats
    {
        public int TotalErrors { get; set; }
        public double ErrorRate { get; set; }
        public Dictionary<int, int> ErrorsByStatusCode { get; set; } = new();
        public Dictionary<string, int> ErrorsByEndpoint { get; set; } = new();
        public Dictionary<string, int> ErrorsByExceptionType { get; set; } = new();
        public string MostCommonError { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    public class SystemPerformanceTrend
    {
        public TimeSpan Period { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<TrendDataPoint> TrendPoints { get; set; } = new();
        public List<SystemSnapshot> SystemSnapshots { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class TrendDataPoint
    {
        public DateTime Timestamp { get; set; }
        public int RequestCount { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public int ErrorCount { get; set; }
        public double SuccessRate { get; set; }
    }

    public class SystemSnapshot
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageMB { get; set; }
        public long ManagedMemoryMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public int GCGen0Collections { get; set; }
        public int GCGen1Collections { get; set; }
        public int GCGen2Collections { get; set; }
    }

    public class ApiEndpointStats
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long RequestCount { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public double SuccessRate { get; set; }
        public long ErrorCount { get; set; }
        public TimeSpan FastestResponseTime { get; set; }
        public TimeSpan SlowestResponseTime { get; set; }
        public DateTime LastAccessTime { get; set; }
    }
}