using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LYBT.Infrastructure.Performance.Monitoring.Components
{
    /// <summary>
    /// 性能监控器 - UltraThink专门化组件
    /// 职责单一：专注API性能监控、数据库查询性能分析
    /// 代码干净：清晰的性能数据收集和分析逻辑
    /// 性能出色：高效的性能指标计算和趋势分析
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly ConcurrentDictionary<string, ApiPerformanceSession> _activeSessions;
        private readonly ConcurrentQueue<ApiPerformanceResult> _performanceHistory;
        private readonly object _historyLock = new object();
        
        // 性能阈值配置
        private readonly int _maxHistorySize = 10000;
        private readonly long _slowResponseThresholdMs = 2000;
        private readonly double _errorRateThreshold = 0.05; // 5%

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeSessions = new ConcurrentDictionary<string, ApiPerformanceSession>();
            _performanceHistory = new ConcurrentQueue<ApiPerformanceResult>();
        }

        #region 核心监控方法

        /// <summary>
        /// 开始API监控会话
        /// </summary>
        public async Task<string> StartApiMonitoringAsync(string apiEndpoint, CancellationToken cancellationToken = default)
        {
            var monitoringId = Guid.NewGuid().ToString("N")[..8];
            
            try
            {
                var session = new ApiPerformanceSession
                {
                    MonitoringId = monitoringId,
                    ApiEndpoint = apiEndpoint,
                    StartTime = DateTime.UtcNow,
                    Stopwatch = Stopwatch.StartNew()
                };

                _activeSessions[monitoringId] = session;
                
                _logger.LogDebug("开始API性能监控：{Endpoint}，监控ID：{Id}", apiEndpoint, monitoringId);
                return await Task.FromResult(monitoringId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始API监控失败：{Endpoint}", apiEndpoint);
                throw;
            }
        }

        /// <summary>
        /// 结束API监控会话
        /// </summary>
        public async Task<ApiPerformanceResult> StopApiMonitoringAsync(string monitoringId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_activeSessions.TryRemove(monitoringId, out var session))
                {
                    _logger.LogWarning("未找到监控会话：{MonitoringId}", monitoringId);
                    return new ApiPerformanceResult { MonitoringId = monitoringId };
                }

                session.Stopwatch.Stop();
                session.EndTime = DateTime.UtcNow;

                var result = new ApiPerformanceResult
                {
                    MonitoringId = monitoringId,
                    ApiEndpoint = session.ApiEndpoint,
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    ResponseTimeMs = session.Stopwatch.ElapsedMilliseconds,
                    StatusCode = session.StatusCode,
                    RequestSizeBytes = session.RequestSizeBytes,
                    ResponseSizeBytes = session.ResponseSizeBytes,
                    ErrorMessage = session.ErrorMessage,
                    AdditionalMetrics = new Dictionary<string, object>(session.AdditionalMetrics)
                };

                // 添加到历史记录
                await AddToHistoryAsync(result, cancellationToken);

                // 分析性能并记录见解
                AnalyzePerformanceResult(result);

                _logger.LogDebug("结束API性能监控：{Endpoint}，耗时：{ElapsedMs}ms", 
                    session.ApiEndpoint, result.ResponseTimeMs);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "结束API监控失败：{MonitoringId}", monitoringId);
                throw;
            }
        }

        /// <summary>
        /// 更新API监控会话信息
        /// </summary>
        public async Task UpdateApiMonitoringSessionAsync(string monitoringId, int statusCode, 
            long requestSize = 0, long responseSize = 0, string? errorMessage = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_activeSessions.TryGetValue(monitoringId, out var session))
                {
                    session.StatusCode = statusCode;
                    session.RequestSizeBytes = requestSize;
                    session.ResponseSizeBytes = responseSize;
                    session.ErrorMessage = errorMessage;

                    _logger.LogDebug("更新API监控会话：{MonitoringId}，状态码：{StatusCode}", monitoringId, statusCode);
                }
                else
                {
                    _logger.LogWarning("更新监控会话失败，未找到会话：{MonitoringId}", monitoringId);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新API监控会话失败：{MonitoringId}", monitoringId);
            }
        }

        #endregion

        #region 性能报告生成

        /// <summary>
        /// 生成性能报告
        /// </summary>
        public async Task<PerformanceReport> GeneratePerformanceReportAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始生成性能报告：{StartTime} - {EndTime}", startTime, endTime);

                var filteredResults = GetPerformanceResults(startTime, endTime);
                
                if (!filteredResults.Any())
                {
                    return new PerformanceReport
                    {
                        ReportStartTime = startTime,
                        ReportEndTime = endTime,
                        PerformanceInsights = new List<string> { "在指定时间范围内未发现性能数据" }
                    };
                }

                var report = new PerformanceReport
                {
                    ReportStartTime = startTime,
                    ReportEndTime = endTime,
                    TotalApiCalls = filteredResults.Count,
                    AverageResponseTimeMs = filteredResults.Average(r => r.ResponseTimeMs),
                    MedianResponseTimeMs = CalculateMedian(filteredResults.Select(r => (double)r.ResponseTimeMs)),
                    MaxResponseTimeMs = filteredResults.Max(r => r.ResponseTimeMs),
                    MinResponseTimeMs = filteredResults.Min(r => r.ResponseTimeMs),
                    ErrorCount = filteredResults.Count(r => r.StatusCode >= 400 || !string.IsNullOrEmpty(r.ErrorMessage)),
                    SlowestApis = filteredResults.OrderByDescending(r => r.ResponseTimeMs).Take(10).ToList(),
                    MostFrequentApis = GetMostFrequentApis(filteredResults, 10),
                    StatusCodeDistribution = GetStatusCodeDistribution(filteredResults)
                };

                report.ErrorRate = report.TotalApiCalls > 0 ? (double)report.ErrorCount / report.TotalApiCalls : 0;
                report.PerformanceInsights = GeneratePerformanceInsights(report);

                _logger.LogInformation("性能报告生成完成：API调用数={TotalCalls}，平均响应时间={AvgTime}ms，错误率={ErrorRate:P2}",
                    report.TotalApiCalls, report.AverageResponseTimeMs, report.ErrorRate);

                return await Task.FromResult(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成性能报告失败");
                throw;
            }
        }

        #endregion

        #region 周期性数据收集

        /// <summary>
        /// 收集性能指标
        /// </summary>
        public async Task CollectPerformanceMetricsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始收集周期性性能指标");

                // 清理过期的活动会话（超过1小时）
                var expiredSessions = _activeSessions
                    .Where(kvp => DateTime.UtcNow - kvp.Value.StartTime > TimeSpan.FromHours(1))
                    .ToList();

                foreach (var expired in expiredSessions)
                {
                    _activeSessions.TryRemove(expired.Key, out _);
                    _logger.LogWarning("清理过期的监控会话：{MonitoringId}，API：{Endpoint}", 
                        expired.Key, expired.Value.ApiEndpoint);
                }

                // 分析最近的性能趋势
                await AnalyzePerformanceTrendsAsync(cancellationToken);

                _logger.LogDebug("周期性性能指标收集完成，活动会话数：{ActiveSessions}，历史记录数：{HistoryCount}",
                    _activeSessions.Count, _performanceHistory.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "收集性能指标失败");
            }
        }

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 初始化性能监控器
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("初始化PerformanceMonitor");
                
                // 执行初始化逻辑
                await Task.CompletedTask;
                
                _logger.LogInformation("PerformanceMonitor初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化PerformanceMonitor失败");
                throw;
            }
        }

        /// <summary>
        /// 关闭性能监控器
        /// </summary>
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("关闭PerformanceMonitor");

                // 完成所有活动会话
                var activeSessions = _activeSessions.Values.ToList();
                foreach (var session in activeSessions)
                {
                    await StopApiMonitoringAsync(session.MonitoringId, cancellationToken);
                }

                _logger.LogInformation("PerformanceMonitor关闭完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭PerformanceMonitor失败");
                throw;
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 添加到历史记录
        /// </summary>
        private async Task AddToHistoryAsync(ApiPerformanceResult result, CancellationToken cancellationToken)
        {
            lock (_historyLock)
            {
                _performanceHistory.Enqueue(result);
                
                // 维护历史记录大小
                while (_performanceHistory.Count > _maxHistorySize)
                {
                    _performanceHistory.TryDequeue(out _);
                }
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取性能结果
        /// </summary>
        private List<ApiPerformanceResult> GetPerformanceResults(DateTime startTime, DateTime endTime)
        {
            lock (_historyLock)
            {
                return _performanceHistory
                    .Where(r => r.StartTime >= startTime && r.EndTime <= endTime)
                    .ToList();
            }
        }

        /// <summary>
        /// 分析性能结果
        /// </summary>
        private void AnalyzePerformanceResult(ApiPerformanceResult result)
        {
            try
            {
                if (result.ResponseTimeMs > _slowResponseThresholdMs)
                {
                    _logger.LogWarning("检测到慢API：{Endpoint}，响应时间：{ResponseTime}ms", 
                        result.ApiEndpoint, result.ResponseTimeMs);
                }

                if (result.StatusCode >= 500)
                {
                    _logger.LogWarning("检测到服务器错误：{Endpoint}，状态码：{StatusCode}，错误消息：{Error}", 
                        result.ApiEndpoint, result.StatusCode, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析性能结果失败");
            }
        }

        /// <summary>
        /// 计算中位数
        /// </summary>
        private double CalculateMedian(IEnumerable<double> values)
        {
            var sortedValues = values.OrderBy(x => x).ToList();
            var count = sortedValues.Count;
            
            if (count == 0) return 0;
            if (count % 2 == 1) return sortedValues[count / 2];
            
            return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2.0;
        }

        /// <summary>
        /// 获取最频繁的API
        /// </summary>
        private List<ApiPerformanceResult> GetMostFrequentApis(List<ApiPerformanceResult> results, int topCount)
        {
            return results
                .GroupBy(r => r.ApiEndpoint)
                .OrderByDescending(g => g.Count())
                .Take(topCount)
                .SelectMany(g => g.Take(1))
                .ToList();
        }

        /// <summary>
        /// 获取状态码分布
        /// </summary>
        private Dictionary<string, int> GetStatusCodeDistribution(List<ApiPerformanceResult> results)
        {
            return results
                .GroupBy(r => GetStatusCodeCategory(r.StatusCode))
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取状态码类别
        /// </summary>
        private string GetStatusCodeCategory(int statusCode)
        {
            return statusCode switch
            {
                >= 200 and < 300 => "2xx Success",
                >= 300 and < 400 => "3xx Redirection",
                >= 400 and < 500 => "4xx Client Error",
                >= 500 => "5xx Server Error",
                _ => "Other"
            };
        }

        /// <summary>
        /// 生成性能见解
        /// </summary>
        private List<string> GeneratePerformanceInsights(PerformanceReport report)
        {
            var insights = new List<string>();

            if (report.ErrorRate > _errorRateThreshold)
            {
                insights.Add($"错误率偏高（{report.ErrorRate:P2}），建议检查应用程序健康状况");
            }

            if (report.AverageResponseTimeMs > _slowResponseThresholdMs)
            {
                insights.Add($"平均响应时间偏长（{report.AverageResponseTimeMs:F2}ms），建议优化性能");
            }

            if (report.MaxResponseTimeMs > _slowResponseThresholdMs * 3)
            {
                insights.Add($"存在极慢的API调用（{report.MaxResponseTimeMs}ms），需要重点关注");
            }

            if (insights.Count == 0)
            {
                insights.Add("API性能表现良好，继续保持当前水平");
            }

            return insights;
        }

        /// <summary>
        /// 分析性能趋势
        /// </summary>
        private async Task AnalyzePerformanceTrendsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var recentResults = GetPerformanceResults(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
                
                if (recentResults.Count > 10) // 有足够的数据进行趋势分析
                {
                    var avgResponseTime = recentResults.Average(r => r.ResponseTimeMs);
                    var recentErrorRate = recentResults.Count(r => r.StatusCode >= 400) / (double)recentResults.Count;

                    if (avgResponseTime > _slowResponseThresholdMs)
                    {
                        _logger.LogWarning("最近一小时平均响应时间过长：{AvgTime}ms", avgResponseTime);
                    }

                    if (recentErrorRate > _errorRateThreshold)
                    {
                        _logger.LogWarning("最近一小时错误率过高：{ErrorRate:P2}", recentErrorRate);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析性能趋势失败");
            }
            
            await Task.CompletedTask;
        }

        #endregion

        #region 内部数据类

        /// <summary>
        /// API性能监控会话
        /// </summary>
        private class ApiPerformanceSession
        {
            public string MonitoringId { get; set; } = string.Empty;
            public string ApiEndpoint { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public Stopwatch Stopwatch { get; set; } = new();
            public int StatusCode { get; set; }
            public long RequestSizeBytes { get; set; }
            public long ResponseSizeBytes { get; set; }
            public string? ErrorMessage { get; set; }
            public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
        }

        #endregion
    }
}