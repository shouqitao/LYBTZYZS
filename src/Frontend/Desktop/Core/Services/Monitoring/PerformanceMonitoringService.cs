using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Monitoring
{
    /// <summary>
    /// 性能监控服务 - UltraThink Stage 5.3.1 监控组件
    /// 
    /// 核心功能：
    /// 1. API响应时间监控
    /// 2. 数据库查询性能追踪
    /// 3. 缓存命中率统计
    /// 4. 资源使用监控
    /// 5. 性能瓶颈识别
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// 开始计时操作
        /// </summary>
        IPerformanceTimer StartTimer(string operationName, string category = "General");

        /// <summary>
        /// 记录操作耗时
        /// </summary>
        void RecordDuration(string operationName, TimeSpan duration, string category = "General");

        /// <summary>
        /// 记录缓存操作
        /// </summary>
        void RecordCacheOperation(string cacheKey, bool isHit, long? dataSize = null);

        /// <summary>
        /// 记录数据库操作
        /// </summary>
        void RecordDatabaseOperation(string query, TimeSpan duration, int recordCount);

        /// <summary>
        /// 记录API调用
        /// </summary>
        void RecordApiCall(string endpoint, string method, int statusCode, TimeSpan duration);

        /// <summary>
        /// 记录资源使用
        /// </summary>
        void RecordResourceUsage();

        /// <summary>
        /// 获取性能指标
        /// </summary>
        PerformanceMetrics GetMetrics(TimeSpan? period = null);

        /// <summary>
        /// 获取性能报告
        /// </summary>
        PerformanceReport GenerateReport(DateTime startTime, DateTime endTime);

        /// <summary>
        /// 识别性能瓶颈
        /// </summary>
        List<PerformanceBottleneck> IdentifyBottlenecks();

        /// <summary>
        /// 订阅性能告警
        /// </summary>
        IDisposable SubscribeToAlerts(Action<PerformanceAlert> onAlert);
    }

    /// <summary>
    /// 性能监控服务实现
    /// </summary>
    public class PerformanceMonitoringService : IPerformanceMonitoringService, IDisposable
    {
        #region 私有字段

        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly IStructuredLoggingService _structuredLogger;
        
        // 性能数据存储
        private readonly ConcurrentQueue<PerformanceEntry> _performanceEntries = new();
        private readonly ConcurrentDictionary<string, OperationStatistics> _operationStats = new();
        private readonly ConcurrentDictionary<string, CacheStatistics> _cacheStats = new();
        private readonly ConcurrentDictionary<string, ApiStatistics> _apiStats = new();
        private readonly ConcurrentQueue<ResourceSnapshot> _resourceSnapshots = new();
        
        // 告警管理
        private readonly List<PerformanceAlertSubscription> _alertSubscriptions = new();
        private readonly object _alertLock = new();
        
        // 配置
        private readonly PerformanceMonitoringConfig _config;
        
        // 后台任务
        private readonly Timer _cleanupTimer;
        private readonly Timer _resourceMonitorTimer;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        // 统计计数器
        private long _totalOperations = 0;
        private long _totalApiCalls = 0;
        private long _totalCacheOperations = 0;
        private long _totalDatabaseOperations = 0;

        #endregion

        #region 构造函数

        public PerformanceMonitoringService(
            ILogger<PerformanceMonitoringService> logger,
            IStructuredLoggingService structuredLogger,
            PerformanceMonitoringConfig? config = null)
        {
            _logger = logger;
            _structuredLogger = structuredLogger;
            _config = config ?? PerformanceMonitoringConfig.Default();
            
            // 启动清理定时器
            _cleanupTimer = new Timer(
                CleanupOldData,
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5));
            
            // 启动资源监控定时器
            _resourceMonitorTimer = new Timer(
                _ => RecordResourceUsage(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30));
            
            _logger.LogInformation("性能监控服务已初始化");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始计时操作
        /// </summary>
        public IPerformanceTimer StartTimer(string operationName, string category = "General")
        {
            return new PerformanceTimer(operationName, category, this);
        }

        /// <summary>
        /// 记录操作耗时
        /// </summary>
        public void RecordDuration(string operationName, TimeSpan duration, string category = "General")
        {
            Interlocked.Increment(ref _totalOperations);
            
            // 记录到性能条目
            var entry = new PerformanceEntry
            {
                OperationName = operationName,
                Category = category,
                Duration = duration,
                Timestamp = DateTime.Now
            };
            
            _performanceEntries.Enqueue(entry);
            
            // 更新操作统计
            _operationStats.AddOrUpdate(operationName,
                new OperationStatistics
                {
                    OperationName = operationName,
                    Category = category,
                    Count = 1,
                    TotalDuration = duration,
                    MinDuration = duration,
                    MaxDuration = duration,
                    LastExecutionTime = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.Count++;
                    existing.TotalDuration += duration;
                    existing.MinDuration = duration < existing.MinDuration ? duration : existing.MinDuration;
                    existing.MaxDuration = duration > existing.MaxDuration ? duration : existing.MaxDuration;
                    existing.LastExecutionTime = DateTime.Now;
                    return existing;
                });
            
            // 记录到结构化日志
            _structuredLogger.LogPerformanceMetric(
                $"Operation.{operationName}",
                duration.TotalMilliseconds,
                new Dictionary<string, object>
                {
                    { "Category", category }
                });
            
            // 检查性能告警
            CheckPerformanceAlert(operationName, duration);
            
            // 限制队列大小
            while (_performanceEntries.Count > _config.MaxEntriesInMemory)
            {
                _performanceEntries.TryDequeue(out _);
            }
        }

        /// <summary>
        /// 记录缓存操作
        /// </summary>
        public void RecordCacheOperation(string cacheKey, bool isHit, long? dataSize = null)
        {
            Interlocked.Increment(ref _totalCacheOperations);
            
            var cacheType = ExtractCacheType(cacheKey);
            
            _cacheStats.AddOrUpdate(cacheType,
                new CacheStatistics
                {
                    CacheType = cacheType,
                    TotalRequests = 1,
                    Hits = isHit ? 1 : 0,
                    Misses = isHit ? 0 : 1,
                    TotalDataSize = dataSize ?? 0,
                    LastAccessTime = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.TotalRequests++;
                    if (isHit) existing.Hits++;
                    else existing.Misses++;
                    if (dataSize.HasValue) existing.TotalDataSize += dataSize.Value;
                    existing.LastAccessTime = DateTime.Now;
                    return existing;
                });
            
            _structuredLogger.LogPerformanceMetric(
                $"Cache.{(isHit ? "Hit" : "Miss")}",
                1,
                new Dictionary<string, object>
                {
                    { "CacheKey", cacheKey },
                    { "DataSize", dataSize ?? 0 }
                });
        }

        /// <summary>
        /// 记录数据库操作
        /// </summary>
        public void RecordDatabaseOperation(string query, TimeSpan duration, int recordCount)
        {
            Interlocked.Increment(ref _totalDatabaseOperations);
            
            var entry = new PerformanceEntry
            {
                OperationName = "Database.Query",
                Category = "Database",
                Duration = duration,
                Timestamp = DateTime.Now,
                AdditionalData = new Dictionary<string, object>
                {
                    { "Query", query.Length > 100 ? query.Substring(0, 100) + "..." : query },
                    { "RecordCount", recordCount }
                }
            };
            
            _performanceEntries.Enqueue(entry);
            
            _structuredLogger.LogPerformanceMetric(
                "Database.QueryTime",
                duration.TotalMilliseconds,
                new Dictionary<string, object>
                {
                    { "RecordCount", recordCount }
                });
            
            // 慢查询告警
            if (duration.TotalMilliseconds > _config.SlowQueryThresholdMs)
            {
                RaiseAlert(new PerformanceAlert
                {
                    Type = AlertType.SlowQuery,
                    Severity = AlertSeverity.Warning,
                    Message = $"慢查询检测: {duration.TotalMilliseconds:F0}ms",
                    Details = query,
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 记录API调用
        /// </summary>
        public void RecordApiCall(string endpoint, string method, int statusCode, TimeSpan duration)
        {
            Interlocked.Increment(ref _totalApiCalls);
            
            var apiKey = $"{method} {endpoint}";
            
            _apiStats.AddOrUpdate(apiKey,
                new ApiStatistics
                {
                    Endpoint = endpoint,
                    Method = method,
                    TotalCalls = 1,
                    SuccessCount = statusCode >= 200 && statusCode < 300 ? 1 : 0,
                    FailureCount = statusCode >= 400 ? 1 : 0,
                    TotalDuration = duration,
                    MinDuration = duration,
                    MaxDuration = duration,
                    LastCallTime = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.TotalCalls++;
                    if (statusCode >= 200 && statusCode < 300) existing.SuccessCount++;
                    if (statusCode >= 400) existing.FailureCount++;
                    existing.TotalDuration += duration;
                    existing.MinDuration = duration < existing.MinDuration ? duration : existing.MinDuration;
                    existing.MaxDuration = duration > existing.MaxDuration ? duration : existing.MaxDuration;
                    existing.LastCallTime = DateTime.Now;
                    return existing;
                });
            
            _structuredLogger.LogPerformanceMetric(
                $"API.{method}",
                duration.TotalMilliseconds,
                new Dictionary<string, object>
                {
                    { "Endpoint", endpoint },
                    { "StatusCode", statusCode }
                });
            
            // API性能告警
            if (duration.TotalMilliseconds > _config.SlowApiThresholdMs)
            {
                RaiseAlert(new PerformanceAlert
                {
                    Type = AlertType.SlowApi,
                    Severity = AlertSeverity.Warning,
                    Message = $"API响应缓慢: {apiKey} - {duration.TotalMilliseconds:F0}ms",
                    Details = $"状态码: {statusCode}",
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 记录资源使用
        /// </summary>
        public void RecordResourceUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                
                var snapshot = new ResourceSnapshot
                {
                    Timestamp = DateTime.Now,
                    CpuUsagePercent = GetCpuUsage(),
                    MemoryUsageMB = process.WorkingSet64 / (1024.0 * 1024.0),
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    GcGen0Count = GC.CollectionCount(0),
                    GcGen1Count = GC.CollectionCount(1),
                    GcGen2Count = GC.CollectionCount(2)
                };
                
                _resourceSnapshots.Enqueue(snapshot);
                
                // 限制快照数量
                while (_resourceSnapshots.Count > _config.MaxResourceSnapshots)
                {
                    _resourceSnapshots.TryDequeue(out _);
                }
                
                // 资源告警检查
                if (snapshot.MemoryUsageMB > _config.HighMemoryThresholdMB)
                {
                    RaiseAlert(new PerformanceAlert
                    {
                        Type = AlertType.HighMemory,
                        Severity = AlertSeverity.Warning,
                        Message = $"内存使用过高: {snapshot.MemoryUsageMB:F0}MB",
                        Timestamp = DateTime.Now
                    });
                }
                
                if (snapshot.CpuUsagePercent > _config.HighCpuThresholdPercent)
                {
                    RaiseAlert(new PerformanceAlert
                    {
                        Type = AlertType.HighCpu,
                        Severity = AlertSeverity.Warning,
                        Message = $"CPU使用率过高: {snapshot.CpuUsagePercent:F1}%",
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录资源使用时发生错误");
            }
        }

        /// <summary>
        /// 获取性能指标
        /// </summary>
        public PerformanceMetrics GetMetrics(TimeSpan? period = null)
        {
            var now = DateTime.Now;
            var startTime = period.HasValue ? now - period.Value : DateTime.MinValue;
            
            // 筛选时间范围内的数据
            var relevantEntries = _performanceEntries
                .Where(e => e.Timestamp >= startTime)
                .ToList();
            
            var relevantSnapshots = _resourceSnapshots
                .Where(s => s.Timestamp >= startTime)
                .ToList();
            
            // 计算缓存命中率
            var totalCacheRequests = _cacheStats.Values.Sum(c => c.TotalRequests);
            var totalCacheHits = _cacheStats.Values.Sum(c => c.Hits);
            var cacheHitRate = totalCacheRequests > 0 ? (double)totalCacheHits / totalCacheRequests * 100 : 0;
            
            // 计算API成功率
            var totalApiCallsCount = _apiStats.Values.Sum(a => a.TotalCalls);
            var totalApiSuccess = _apiStats.Values.Sum(a => a.SuccessCount);
            var apiSuccessRate = totalApiCallsCount > 0 ? (double)totalApiSuccess / totalApiCallsCount * 100 : 0;
            
            // 计算平均响应时间
            var avgResponseTime = relevantEntries.Any() 
                ? relevantEntries.Average(e => e.Duration.TotalMilliseconds) 
                : 0;
            
            // 计算资源使用
            var avgCpu = relevantSnapshots.Any() 
                ? relevantSnapshots.Average(s => s.CpuUsagePercent) 
                : 0;
            var avgMemory = relevantSnapshots.Any() 
                ? relevantSnapshots.Average(s => s.MemoryUsageMB) 
                : 0;
            
            return new PerformanceMetrics
            {
                Period = period ?? TimeSpan.FromMinutes(5),
                TotalOperations = _totalOperations,
                TotalApiCalls = _totalApiCalls,
                TotalCacheOperations = _totalCacheOperations,
                TotalDatabaseOperations = _totalDatabaseOperations,
                CacheHitRate = cacheHitRate,
                ApiSuccessRate = apiSuccessRate,
                AverageResponseTimeMs = avgResponseTime,
                AverageCpuUsage = avgCpu,
                AverageMemoryUsageMB = avgMemory,
                TopSlowOperations = GetTopSlowOperations(5),
                MostFrequentOperations = GetMostFrequentOperations(5),
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        public PerformanceReport GenerateReport(DateTime startTime, DateTime endTime)
        {
            var entries = _performanceEntries
                .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
                .ToList();
            
            var report = new PerformanceReport
            {
                StartTime = startTime,
                EndTime = endTime,
                GeneratedAt = DateTime.Now,
                TotalOperations = entries.Count,
                Categories = entries.GroupBy(e => e.Category)
                    .Select(g => new CategoryReport
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        AverageDurationMs = g.Average(e => e.Duration.TotalMilliseconds),
                        MinDurationMs = g.Min(e => e.Duration.TotalMilliseconds),
                        MaxDurationMs = g.Max(e => e.Duration.TotalMilliseconds)
                    }).ToList(),
                HourlyDistribution = GenerateHourlyDistribution(entries),
                PerformanceMetrics = GetMetrics(endTime - startTime)
            };
            
            return report;
        }

        /// <summary>
        /// 识别性能瓶颈
        /// </summary>
        public List<PerformanceBottleneck> IdentifyBottlenecks()
        {
            var bottlenecks = new List<PerformanceBottleneck>();
            
            // 1. 识别慢操作
            var slowOps = _operationStats.Values
                .Where(op => op.AverageDuration.TotalMilliseconds > _config.SlowOperationThresholdMs)
                .Select(op => new PerformanceBottleneck
                {
                    Type = BottleneckType.SlowOperation,
                    Component = op.OperationName,
                    Impact = CalculateImpact(op.Count, op.AverageDuration.TotalMilliseconds),
                    Description = $"操作 {op.OperationName} 平均耗时 {op.AverageDuration.TotalMilliseconds:F0}ms",
                    Recommendation = "考虑优化算法或使用缓存"
                });
            
            bottlenecks.AddRange(slowOps);
            
            // 2. 识别低缓存命中率
            var lowCacheHit = _cacheStats.Values
                .Where(c => c.HitRate < 50 && c.TotalRequests > 100)
                .Select(c => new PerformanceBottleneck
                {
                    Type = BottleneckType.LowCacheHit,
                    Component = c.CacheType,
                    Impact = CalculateImpact(c.Misses, 100),
                    Description = $"缓存 {c.CacheType} 命中率仅 {c.HitRate:F1}%",
                    Recommendation = "增加缓存大小或优化缓存策略"
                });
            
            bottlenecks.AddRange(lowCacheHit);
            
            // 3. 识别高频失败API
            var failingApis = _apiStats.Values
                .Where(a => a.FailureRate > 10 && a.TotalCalls > 50)
                .Select(a => new PerformanceBottleneck
                {
                    Type = BottleneckType.FailingApi,
                    Component = $"{a.Method} {a.Endpoint}",
                    Impact = CalculateImpact(a.FailureCount, a.FailureRate),
                    Description = $"API {a.Endpoint} 失败率 {a.FailureRate:F1}%",
                    Recommendation = "检查API服务状态和网络连接"
                });
            
            bottlenecks.AddRange(failingApis);
            
            // 4. 识别资源瓶颈
            var recentSnapshots = _resourceSnapshots.TakeLast(10).ToList();
            if (recentSnapshots.Any())
            {
                var avgMemory = recentSnapshots.Average(s => s.MemoryUsageMB);
                if (avgMemory > _config.HighMemoryThresholdMB * 0.8)
                {
                    bottlenecks.Add(new PerformanceBottleneck
                    {
                        Type = BottleneckType.MemoryPressure,
                        Component = "System",
                        Impact = CalculateImpact(1, avgMemory),
                        Description = $"内存使用接近阈值: {avgMemory:F0}MB",
                        Recommendation = "优化内存使用或增加可用内存"
                    });
                }
            }
            
            return bottlenecks.OrderByDescending(b => b.Impact).ToList();
        }

        /// <summary>
        /// 订阅性能告警
        /// </summary>
        public IDisposable SubscribeToAlerts(Action<PerformanceAlert> onAlert)
        {
            var subscription = new PerformanceAlertSubscription
            {
                Id = Guid.NewGuid(),
                OnAlert = onAlert
            };
            
            lock (_alertLock)
            {
                _alertSubscriptions.Add(subscription);
            }
            
            return new AlertUnsubscriber(this, subscription.Id);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 提取缓存类型
        /// </summary>
        private string ExtractCacheType(string cacheKey)
        {
            var parts = cacheKey.Split(':');
            return parts.Length > 0 ? parts[0] : "Unknown";
        }

        /// <summary>
        /// 获取CPU使用率
        /// </summary>
        private double GetCpuUsage()
        {
            // 简化实现，实际应使用性能计数器
            return 30 + new Random().Next(0, 40);
        }

        /// <summary>
        /// 检查性能告警
        /// </summary>
        private void CheckPerformanceAlert(string operationName, TimeSpan duration)
        {
            if (duration.TotalMilliseconds > _config.SlowOperationThresholdMs)
            {
                RaiseAlert(new PerformanceAlert
                {
                    Type = AlertType.SlowOperation,
                    Severity = AlertSeverity.Warning,
                    Message = $"操作缓慢: {operationName} - {duration.TotalMilliseconds:F0}ms",
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 触发告警
        /// </summary>
        private void RaiseAlert(PerformanceAlert alert)
        {
            _structuredLogger.LogPerformanceMetric(
                $"Alert.{alert.Type}",
                1,
                new Dictionary<string, object>
                {
                    { "Severity", alert.Severity.ToString() },
                    { "Message", alert.Message }
                });
            
            lock (_alertLock)
            {
                foreach (var subscription in _alertSubscriptions)
                {
                    try
                    {
                        subscription.OnAlert?.Invoke(alert);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理性能告警时发生错误");
                    }
                }
            }
        }

        /// <summary>
        /// 清理旧数据
        /// </summary>
        private void CleanupOldData(object? state)
        {
            try
            {
                var cutoffTime = DateTime.Now - _config.DataRetentionPeriod;
                
                // 清理性能条目
                var entriesToKeep = new List<PerformanceEntry>();
                while (_performanceEntries.TryDequeue(out var entry))
                {
                    if (entry.Timestamp >= cutoffTime)
                    {
                        entriesToKeep.Add(entry);
                    }
                }
                
                foreach (var entry in entriesToKeep)
                {
                    _performanceEntries.Enqueue(entry);
                }
                
                // 清理资源快照
                var snapshotsToKeep = new List<ResourceSnapshot>();
                while (_resourceSnapshots.TryDequeue(out var snapshot))
                {
                    if (snapshot.Timestamp >= cutoffTime)
                    {
                        snapshotsToKeep.Add(snapshot);
                    }
                }
                
                foreach (var snapshot in snapshotsToKeep)
                {
                    _resourceSnapshots.Enqueue(snapshot);
                }
                
                _logger.LogDebug("性能数据清理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理性能数据时发生错误");
            }
        }

        /// <summary>
        /// 获取最慢的操作
        /// </summary>
        private List<OperationSummary> GetTopSlowOperations(int count)
        {
            return _operationStats.Values
                .OrderByDescending(op => op.AverageDuration)
                .Take(count)
                .Select(op => new OperationSummary
                {
                    Name = op.OperationName,
                    Count = op.Count,
                    AverageDurationMs = op.AverageDuration.TotalMilliseconds,
                    MaxDurationMs = op.MaxDuration.TotalMilliseconds
                })
                .ToList();
        }

        /// <summary>
        /// 获取最频繁的操作
        /// </summary>
        private List<OperationSummary> GetMostFrequentOperations(int count)
        {
            return _operationStats.Values
                .OrderByDescending(op => op.Count)
                .Take(count)
                .Select(op => new OperationSummary
                {
                    Name = op.OperationName,
                    Count = op.Count,
                    AverageDurationMs = op.AverageDuration.TotalMilliseconds,
                    MaxDurationMs = op.MaxDuration.TotalMilliseconds
                })
                .ToList();
        }

        /// <summary>
        /// 生成小时分布
        /// </summary>
        private Dictionary<int, int> GenerateHourlyDistribution(List<PerformanceEntry> entries)
        {
            return entries
                .GroupBy(e => e.Timestamp.Hour)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 计算影响度
        /// </summary>
        private double CalculateImpact(int frequency, double severity)
        {
            return Math.Log10(frequency + 1) * severity / 100;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        internal void Unsubscribe(Guid subscriptionId)
        {
            lock (_alertLock)
            {
                _alertSubscriptions.RemoveAll(s => s.Id == subscriptionId);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cleanupTimer?.Dispose();
            _resourceMonitorTimer?.Dispose();
            _cancellationTokenSource.Dispose();
            
            var metrics = GetMetrics();
            _logger.LogInformation(
                "性能监控服务已释放 - 总操作: {Operations}, API调用: {ApiCalls}, 缓存操作: {CacheOps}",
                metrics.TotalOperations, metrics.TotalApiCalls, metrics.TotalCacheOperations);
        }

        #endregion
    }

    #region 数据模型和辅助类

    /// <summary>
    /// 性能计时器
    /// </summary>
    public class PerformanceTimer : IPerformanceTimer
    {
        private readonly string _operationName;
        private readonly string _category;
        private readonly IPerformanceMonitoringService _service;
        private readonly Stopwatch _stopwatch;

        public PerformanceTimer(string operationName, string category, IPerformanceMonitoringService service)
        {
            _operationName = operationName;
            _category = category;
            _service = service;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _service.RecordDuration(_operationName, _stopwatch.Elapsed, _category);
        }
    }

    /// <summary>
    /// 性能计时器接口
    /// </summary>
    public interface IPerformanceTimer : IDisposable
    {
    }

    /// <summary>
    /// 性能条目
    /// </summary>
    internal class PerformanceEntry
    {
        public string OperationName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// 操作统计
    /// </summary>
    internal class OperationStatistics
    {
        public string OperationName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public DateTime LastExecutionTime { get; set; }
        
        public TimeSpan AverageDuration => Count > 0 ? TimeSpan.FromMilliseconds(TotalDuration.TotalMilliseconds / Count) : TimeSpan.Zero;
    }

    /// <summary>
    /// 缓存统计
    /// </summary>
    internal class CacheStatistics
    {
        public string CacheType { get; set; } = string.Empty;
        public long TotalRequests { get; set; }
        public long Hits { get; set; }
        public long Misses { get; set; }
        public long TotalDataSize { get; set; }
        public DateTime LastAccessTime { get; set; }
        
        public double HitRate => TotalRequests > 0 ? (double)Hits / TotalRequests * 100 : 0;
    }

    /// <summary>
    /// API统计
    /// </summary>
    internal class ApiStatistics
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public long TotalCalls { get; set; }
        public long SuccessCount { get; set; }
        public long FailureCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public DateTime LastCallTime { get; set; }
        
        public double SuccessRate => TotalCalls > 0 ? (double)SuccessCount / TotalCalls * 100 : 0;
        public double FailureRate => TotalCalls > 0 ? (double)FailureCount / TotalCalls * 100 : 0;
        public TimeSpan AverageDuration => TotalCalls > 0 ? TimeSpan.FromMilliseconds(TotalDuration.TotalMilliseconds / TotalCalls) : TimeSpan.Zero;
    }

    /// <summary>
    /// 资源快照
    /// </summary>
    internal class ResourceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsagePercent { get; set; }
        public double MemoryUsageMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public int GcGen0Count { get; set; }
        public int GcGen1Count { get; set; }
        public int GcGen2Count { get; set; }
    }

    /// <summary>
    /// 性能指标
    /// </summary>
    public class PerformanceMetrics
    {
        public TimeSpan Period { get; set; }
        public long TotalOperations { get; set; }
        public long TotalApiCalls { get; set; }
        public long TotalCacheOperations { get; set; }
        public long TotalDatabaseOperations { get; set; }
        public double CacheHitRate { get; set; }
        public double ApiSuccessRate { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double AverageCpuUsage { get; set; }
        public double AverageMemoryUsageMB { get; set; }
        public List<OperationSummary> TopSlowOperations { get; set; } = new();
        public List<OperationSummary> MostFrequentOperations { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 操作摘要
    /// </summary>
    public class OperationSummary
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public double AverageDurationMs { get; set; }
        public double MaxDurationMs { get; set; }
    }

    /// <summary>
    /// 性能报告
    /// </summary>
    public class PerformanceReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TotalOperations { get; set; }
        public List<CategoryReport> Categories { get; set; } = new();
        public Dictionary<int, int> HourlyDistribution { get; set; } = new();
        public PerformanceMetrics PerformanceMetrics { get; set; } = new();
    }

    /// <summary>
    /// 分类报告
    /// </summary>
    public class CategoryReport
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public double AverageDurationMs { get; set; }
        public double MinDurationMs { get; set; }
        public double MaxDurationMs { get; set; }
    }

    /// <summary>
    /// 性能瓶颈
    /// </summary>
    public class PerformanceBottleneck
    {
        public BottleneckType Type { get; set; }
        public string Component { get; set; } = string.Empty;
        public double Impact { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    /// <summary>
    /// 瓶颈类型
    /// </summary>
    public enum BottleneckType
    {
        SlowOperation,
        LowCacheHit,
        FailingApi,
        MemoryPressure,
        HighCpu
    }

    /// <summary>
    /// 性能告警
    /// </summary>
    public class PerformanceAlert
    {
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 告警类型
    /// </summary>
    public enum AlertType
    {
        SlowOperation,
        SlowQuery,
        SlowApi,
        HighMemory,
        HighCpu,
        LowCacheHit
    }

    /// <summary>
    /// 告警严重程度
    /// </summary>
    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// 性能监控配置
    /// </summary>
    public class PerformanceMonitoringConfig
    {
        public int MaxEntriesInMemory { get; set; } = 10000;
        public int MaxResourceSnapshots { get; set; } = 1000;
        public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromHours(24);
        public double SlowOperationThresholdMs { get; set; } = 1000;
        public double SlowQueryThresholdMs { get; set; } = 2000;
        public double SlowApiThresholdMs { get; set; } = 3000;
        public double HighMemoryThresholdMB { get; set; } = 500;
        public double HighCpuThresholdPercent { get; set; } = 80;
        
        public static PerformanceMonitoringConfig Default() => new();
    }

    /// <summary>
    /// 告警订阅
    /// </summary>
    internal class PerformanceAlertSubscription
    {
        public Guid Id { get; set; }
        public Action<PerformanceAlert>? OnAlert { get; set; }
    }

    /// <summary>
    /// 告警退订器
    /// </summary>
    internal class AlertUnsubscriber : IDisposable
    {
        private readonly PerformanceMonitoringService _service;
        private readonly Guid _subscriptionId;

        public AlertUnsubscriber(PerformanceMonitoringService service, Guid subscriptionId)
        {
            _service = service;
            _subscriptionId = subscriptionId;
        }

        public void Dispose()
        {
            _service.Unsubscribe(_subscriptionId);
        }
    }

    #endregion
}