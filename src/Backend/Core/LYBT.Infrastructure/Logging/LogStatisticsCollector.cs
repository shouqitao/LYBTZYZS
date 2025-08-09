using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LYBT.Infrastructure.Logging
{
    /// <summary>
    /// 日志统计收集器 - UltraThink监控优化
    /// 职责单一：专注日志统计数据收集和分析
    /// 代码干净：清晰的统计分类和数据聚合
    /// 性能出色：高效的并发统计和实时计算
    /// </summary>
    internal class LogStatisticsCollector
    {
        private readonly ConcurrentDictionary<LogLevel, long> _logCountsByLevel = new();
        private readonly ConcurrentDictionary<LogCategory, long> _logCountsByCategory = new();
        private readonly ConcurrentDictionary<string, OperationStatistics> _operationStats = new();
        private readonly ConcurrentDictionary<string, long> _exceptionStats = new();
        private readonly ConcurrentQueue<LogEntry> _recentLogs = new();
        private readonly ConcurrentDictionary<string, UserActivity> _userActivity = new();
        
        private readonly object _performanceStatsLock = new object();
        private readonly List<PerformanceDataPoint> _performanceData = new();
        
        private long _totalLogCount = 0;
        private const int MaxRecentLogs = 10000;
        private const int MaxOperationStats = 1000;

        /// <summary>
        /// 记录日志条目
        /// </summary>
        public void RecordLog(LogEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            
            // 增加总计数
            Interlocked.Increment(ref _totalLogCount);
            
            // 按级别统计
            _logCountsByLevel.AddOrUpdate(entry.Level, 1, (key, value) => value + 1);
            
            // 按类别统计
            _logCountsByCategory.AddOrUpdate(entry.Category, 1, (key, value) => value + 1);
            
            // 异常统计
            if (entry.Exception != null)
            {
                var exceptionType = entry.Exception.GetType().Name;
                _exceptionStats.AddOrUpdate(exceptionType, 1, (key, value) => value + 1);
            }
            
            // 用户活动统计
            if (!string.IsNullOrEmpty(entry.UserId))
            {
                _userActivity.AddOrUpdate(entry.UserId, new UserActivity
                {
                    UserId = entry.UserId,
                    LastActivity = entry.Timestamp,
                    ActivityCount = 1
                }, (key, existing) => new UserActivity
                {
                    UserId = key,
                    LastActivity = entry.Timestamp,
                    ActivityCount = existing.ActivityCount + 1
                });
            }
            
            // 保存最近的日志
            _recentLogs.Enqueue(entry);
            if (_recentLogs.Count > MaxRecentLogs)
            {
                _recentLogs.TryDequeue(out _);
            }
        }

        /// <summary>
        /// 添加性能数据
        /// </summary>
        public void AddPerformanceData(string operation, TimeSpan duration, PerformanceMetrics? metrics)
        {
            ArgumentException.ThrowIfNullOrEmpty(operation);
            
            // 更新操作统计
            _operationStats.AddOrUpdate(operation, new OperationStatistics
            {
                Name = operation,
                TotalCalls = 1,
                TotalDuration = duration,
                AverageDuration = duration,
                MinDuration = duration,
                MaxDuration = duration,
                ErrorCount = 0,
                LastCall = DateTime.UtcNow
            }, (key, existing) =>
            {
                var newTotalCalls = existing.TotalCalls + 1;
                var newTotalDuration = existing.TotalDuration + duration;
                
                return new OperationStatistics
                {
                    Name = operation,
                    TotalCalls = newTotalCalls,
                    TotalDuration = newTotalDuration,
                    AverageDuration = TimeSpan.FromTicks(newTotalDuration.Ticks / newTotalCalls),
                    MinDuration = duration < existing.MinDuration ? duration : existing.MinDuration,
                    MaxDuration = duration > existing.MaxDuration ? duration : existing.MaxDuration,
                    ErrorCount = existing.ErrorCount,
                    LastCall = DateTime.UtcNow
                };
            });
            
            // 保存性能数据点
            if (metrics != null)
            {
                lock (_performanceStatsLock)
                {
                    _performanceData.Add(new PerformanceDataPoint
                    {
                        Timestamp = DateTime.UtcNow,
                        Operation = operation,
                        Duration = duration,
                        Metrics = metrics
                    });
                    
                    // 限制性能数据点数量，保留最近的
                    if (_performanceData.Count > MaxRecentLogs)
                    {
                        _performanceData.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// 记录操作错误
        /// </summary>
        public void RecordOperationError(string operation)
        {
            ArgumentException.ThrowIfNullOrEmpty(operation);
            
            _operationStats.AddOrUpdate(operation, new OperationStatistics
            {
                Name = operation,
                TotalCalls = 1,
                ErrorCount = 1,
                LastCall = DateTime.UtcNow
            }, (key, existing) => new OperationStatistics
            {
                Name = existing.Name,
                TotalCalls = existing.TotalCalls,
                TotalDuration = existing.TotalDuration,
                AverageDuration = existing.AverageDuration,
                MinDuration = existing.MinDuration,
                MaxDuration = existing.MaxDuration,
                ErrorCount = existing.ErrorCount + 1,
                LastCall = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 生成统计报告
        /// </summary>
        public async Task<LogStatistics> GenerateStatisticsAsync(TimeSpan timeRange)
        {
            var cutoffTime = DateTime.UtcNow - timeRange;
            var relevantLogs = _recentLogs.Where(log => log.Timestamp >= cutoffTime).ToList();
            
            var statistics = new LogStatistics
            {
                TimeRange = timeRange,
                TotalLogs = relevantLogs.Count,
                LogsByLevel = new Dictionary<LogLevel, long>(),
                LogsByCategory = new Dictionary<LogCategory, long>(),
                HotOperations = new List<HotOperation>(),
                ExceptionTypes = new Dictionary<string, int>(),
                AverageMetrics = await CalculateAverageMetricsAsync(timeRange)
            };
            
            // 按级别统计（仅限时间范围内）
            foreach (var log in relevantLogs)
            {
                statistics.LogsByLevel.TryGetValue(log.Level, out var levelCount);
                statistics.LogsByLevel[log.Level] = levelCount + 1;
                
                statistics.LogsByCategory.TryGetValue(log.Category, out var categoryCount);
                statistics.LogsByCategory[log.Category] = categoryCount + 1;
                
                if (log.Exception != null)
                {
                    var exceptionType = log.Exception.GetType().Name;
                    statistics.ExceptionTypes.TryGetValue(exceptionType, out var exceptionCount);
                    statistics.ExceptionTypes[exceptionType] = exceptionCount + 1;
                }
            }
            
            // 生成热点操作列表
            statistics.HotOperations = GenerateHotOperations(timeRange);
            
            // 计算活跃用户数
            statistics.ActiveUsers = _userActivity.Values
                .Count(ua => ua.LastActivity >= cutoffTime);
            
            return statistics;
        }

        /// <summary>
        /// 生成热点操作
        /// </summary>
        private List<HotOperation> GenerateHotOperations(TimeSpan timeRange)
        {
            var cutoffTime = DateTime.UtcNow - timeRange;
            
            return _operationStats.Values
                .Where(op => op.LastCall >= cutoffTime)
                .Select(op => new HotOperation
                {
                    Name = op.Name,
                    Count = op.TotalCalls,
                    AverageTimeMs = op.AverageDuration.TotalMilliseconds,
                    ErrorCount = op.ErrorCount
                })
                .OrderByDescending(ho => ho.Count)
                .Take(20) // 只返回前20个热点操作
                .ToList();
        }

        /// <summary>
        /// 计算平均性能指标
        /// </summary>
        private async Task<PerformanceMetrics?> CalculateAverageMetricsAsync(TimeSpan timeRange)
        {
            var cutoffTime = DateTime.UtcNow - timeRange;
            
            List<PerformanceDataPoint> relevantData;
            lock (_performanceStatsLock)
            {
                relevantData = _performanceData
                    .Where(pd => pd.Timestamp >= cutoffTime)
                    .ToList();
            }
            
            if (relevantData.Count == 0)
                return null;
            
            var avgMetrics = new PerformanceMetrics
            {
                CpuUsagePercent = relevantData.Average(pd => pd.Metrics.CpuUsagePercent),
                MemoryUsageMB = Convert.ToInt64(relevantData.Average(pd => pd.Metrics.MemoryUsageMB)),
                DatabaseQueries = Convert.ToInt32(relevantData.Average(pd => pd.Metrics.DatabaseQueries)),
                CacheHits = Convert.ToInt32(relevantData.Average(pd => pd.Metrics.CacheHits)),
                CacheMisses = Convert.ToInt32(relevantData.Average(pd => pd.Metrics.CacheMisses)),
                HttpRequests = Convert.ToInt32(relevantData.Average(pd => pd.Metrics.HttpRequests)),
                ExceptionCount = Convert.ToInt32(relevantData.Average(pd => pd.Metrics.ExceptionCount))
            };
            
            // 计算自定义指标的平均值
            var allCustomMetricKeys = relevantData
                .SelectMany(pd => pd.Metrics.CustomMetrics.Keys)
                .Distinct()
                .ToList();
            
            foreach (var key in allCustomMetricKeys)
            {
                var values = relevantData
                    .Where(pd => pd.Metrics.CustomMetrics.ContainsKey(key))
                    .Select(pd => pd.Metrics.CustomMetrics[key])
                    .ToList();
                
                if (values.Count > 0)
                {
                    avgMetrics.CustomMetrics[key] = values.Average();
                }
            }
            
            return await Task.FromResult(avgMetrics);
        }

        /// <summary>
        /// 清理过期数据
        /// </summary>
        public void CleanupExpiredData(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            
            // 清理过期的操作统计
            var expiredOperations = _operationStats
                .Where(kvp => kvp.Value.LastCall < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var operation in expiredOperations)
            {
                _operationStats.TryRemove(operation, out _);
            }
            
            // 清理过期的用户活动
            var expiredUsers = _userActivity
                .Where(kvp => kvp.Value.LastActivity < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var userId in expiredUsers)
            {
                _userActivity.TryRemove(userId, out _);
            }
            
            // 清理过期的性能数据
            lock (_performanceStatsLock)
            {
                var validData = _performanceData
                    .Where(pd => pd.Timestamp >= cutoffTime)
                    .ToList();
                
                _performanceData.Clear();
                _performanceData.AddRange(validData);
            }
            
            // 清理最近日志队列中的过期日志
            var recentLogsToKeep = new List<LogEntry>();
            while (_recentLogs.TryDequeue(out var log))
            {
                if (log.Timestamp >= cutoffTime)
                {
                    recentLogsToKeep.Add(log);
                }
            }
            
            foreach (var log in recentLogsToKeep)
            {
                _recentLogs.Enqueue(log);
            }
        }

        /// <summary>
        /// 获取当前统计概要
        /// </summary>
        public StatisticsSummary GetCurrentSummary()
        {
            var now = DateTime.UtcNow;
            var hourAgo = now.AddHours(-1);
            
            var recentLogsCount = _recentLogs.Count(log => log.Timestamp >= hourAgo);
            var recentErrorsCount = _recentLogs.Count(log => 
                log.Timestamp >= hourAgo && 
                (log.Level == LogLevel.Error || log.Level == LogLevel.Critical));
            
            return new StatisticsSummary
            {
                TotalLogs = Interlocked.Read(ref _totalLogCount),
                RecentLogsCount = recentLogsCount,
                RecentErrorsCount = recentErrorsCount,
                ActiveOperations = _operationStats.Count,
                ActiveUsers = _userActivity.Count(kvp => kvp.Value.LastActivity >= hourAgo),
                TopOperations = _operationStats.Values
                    .OrderByDescending(op => op.TotalCalls)
                    .Take(5)
                    .Select(op => $"{op.Name} ({op.TotalCalls} calls)")
                    .ToList()
            };
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void Reset()
        {
            _logCountsByLevel.Clear();
            _logCountsByCategory.Clear();
            _operationStats.Clear();
            _exceptionStats.Clear();
            _userActivity.Clear();
            
            while (_recentLogs.TryDequeue(out _)) { }
            
            lock (_performanceStatsLock)
            {
                _performanceData.Clear();
            }
            
            Interlocked.Exchange(ref _totalLogCount, 0);
        }
    }

    /// <summary>
    /// 操作统计信息
    /// </summary>
    internal class OperationStatistics
    {
        public string Name { get; set; } = string.Empty;
        public long TotalCalls { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public long ErrorCount { get; set; }
        public DateTime LastCall { get; set; }
    }

    /// <summary>
    /// 用户活动信息
    /// </summary>
    internal class UserActivity
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; }
        public long ActivityCount { get; set; }
    }

    /// <summary>
    /// 性能数据点
    /// </summary>
    internal class PerformanceDataPoint
    {
        public DateTime Timestamp { get; set; }
        public string Operation { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public PerformanceMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// 统计概要
    /// </summary>
    public class StatisticsSummary
    {
        public long TotalLogs { get; set; }
        public int RecentLogsCount { get; set; }
        public int RecentErrorsCount { get; set; }
        public int ActiveOperations { get; set; }
        public int ActiveUsers { get; set; }
        public List<string> TopOperations { get; set; } = new();
    }
}