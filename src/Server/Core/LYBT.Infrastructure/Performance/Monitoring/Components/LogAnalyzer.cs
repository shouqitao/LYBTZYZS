using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LYBT.Infrastructure.Performance.Monitoring.Components
{
    /// <summary>
    /// 日志分析器 - UltraThink专门化组件
    /// 职责单一：专注智能日志分析、异常模式识别、日志洞察生成
    /// 代码干净：清晰的日志解析逻辑和模式匹配算法
    /// 性能出色：高效的日志处理和实时异常检测
    /// </summary>
    public class LogAnalyzer
    {
        private readonly ILogger<LogAnalyzer> _logger;
        private readonly ConcurrentQueue<LogEntry> _logBuffer;
        private readonly ConcurrentDictionary<string, LogPattern> _knownPatterns;
        private readonly ConcurrentQueue<LogAnomaly> _detectedAnomalies;
        private readonly object _analysisLock = new object();
        
        // 日志分析配置
        private readonly int _maxLogBufferSize = 50000;
        private readonly int _patternDetectionThreshold = 5; // 模式检测最小出现次数
        private readonly TimeSpan _anomalyDetectionWindow = TimeSpan.FromMinutes(15);
        
        // 预定义的错误模式
        private readonly Dictionary<string, Regex> _errorPatterns;
        private readonly Dictionary<string, PatternSeverity> _patternSeverityMap;

        public LogAnalyzer(ILogger<LogAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logBuffer = new ConcurrentQueue<LogEntry>();
            _knownPatterns = new ConcurrentDictionary<string, LogPattern>();
            _detectedAnomalies = new ConcurrentQueue<LogAnomaly>();

            // 初始化错误模式
            _errorPatterns = InitializeErrorPatterns();
            _patternSeverityMap = InitializePatternSeverityMap();
        }

        #region 核心分析方法

        /// <summary>
        /// 分析指定时间范围内的日志
        /// </summary>
        public async Task<LogAnalysisResult> AnalyzeLogsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始分析日志：{StartTime} - {EndTime}", startTime, endTime);

                var filteredLogs = GetLogEntries(startTime, endTime);
                
                var result = new LogAnalysisResult
                {
                    AnalysisStartTime = startTime,
                    AnalysisEndTime = endTime,
                    TotalLogEntries = filteredLogs.Count,
                    ErrorLogCount = filteredLogs.Count(l => l.Level == LogLevel.Error),
                    WarningLogCount = filteredLogs.Count(l => l.Level == LogLevel.Warning),
                    InfoLogCount = filteredLogs.Count(l => l.Level == LogLevel.Information),
                    LogLevelDistribution = GetLogLevelDistribution(filteredLogs)
                };

                // 检测日志模式
                result.DetectedPatterns = await DetectLogPatternsAsync(filteredLogs, cancellationToken);

                // 检测异常
                result.Anomalies = await DetectAnomaliesInLogsAsync(filteredLogs, cancellationToken);

                // 提取顶级错误消息
                result.TopErrorMessages = GetTopErrorMessages(filteredLogs, 10);

                // 生成分析洞察
                result.AnalysisInsights = GenerateAnalysisInsights(result);

                _logger.LogInformation("日志分析完成：总日志数={Total}，错误数={Errors}，模式数={Patterns}，异常数={Anomalies}",
                    result.TotalLogEntries, result.ErrorLogCount, result.DetectedPatterns.Count, result.Anomalies.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析日志失败");
                throw;
            }
        }

        /// <summary>
        /// 检测异常日志模式
        /// </summary>
        public async Task<List<LogPattern>> DetectAnomalousPatterns(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始检测异常日志模式");

                var recentLogs = GetLogEntries(DateTime.UtcNow.Subtract(_anomalyDetectionWindow), DateTime.UtcNow);
                var patterns = await DetectLogPatternsAsync(recentLogs, cancellationToken);

                // 过滤异常模式（高频率、高严重级别）
                var anomalousPatterns = patterns
                    .Where(p => p.Frequency > _patternDetectionThreshold * 2 || p.Severity >= PatternSeverity.High)
                    .OrderByDescending(p => p.Frequency)
                    .ToList();

                _logger.LogInformation("检测到异常日志模式数量：{Count}", anomalousPatterns.Count);
                return anomalousPatterns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测异常日志模式失败");
                throw;
            }
        }

        /// <summary>
        /// 添加日志条目到分析缓冲区
        /// </summary>
        public async Task AddLogEntryAsync(string message, LogLevel level, DateTime timestamp, 
            string? category = null, Exception? exception = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    Message = message,
                    Level = level,
                    Timestamp = timestamp,
                    Category = category ?? "Unknown",
                    Exception = exception?.ToString(),
                    ThreadId = Environment.CurrentManagedThreadId
                };

                _logBuffer.Enqueue(logEntry);

                // 维护缓冲区大小
                while (_logBuffer.Count > _maxLogBufferSize)
                {
                    _logBuffer.TryDequeue(out _);
                }

                // 实时异常检测
                if (level >= LogLevel.Error)
                {
                    await DetectRealTimeAnomalyAsync(logEntry, cancellationToken);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加日志条目失败");
            }
        }

        #endregion

        #region 周期性日志分析

        /// <summary>
        /// 周期性日志分析
        /// </summary>
        public async Task PeriodicLogAnalysisAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始周期性日志分析");

                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddMinutes(-15); // 分析最近15分钟的日志

                var analysisResult = await AnalyzeLogsAsync(startTime, endTime, cancellationToken);

                // 检查是否有需要关注的问题
                if (analysisResult.ErrorLogCount > 10) // 15分钟内超过10个错误
                {
                    _logger.LogWarning("检测到高错误频率：15分钟内发生{ErrorCount}个错误", analysisResult.ErrorLogCount);
                }

                if (analysisResult.Anomalies.Any(a => a.Severity >= AnomalySeverity.Major))
                {
                    _logger.LogWarning("检测到重大日志异常：{Count}个", 
                        analysisResult.Anomalies.Count(a => a.Severity >= AnomalySeverity.Major));
                }

                // 更新已知模式
                foreach (var pattern in analysisResult.DetectedPatterns)
                {
                    _knownPatterns.AddOrUpdate(pattern.PatternId, pattern, (key, existing) =>
                    {
                        existing.Frequency += pattern.Frequency;
                        existing.LastOccurrence = pattern.LastOccurrence;
                        return existing;
                    });
                }

                _logger.LogDebug("周期性日志分析完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "周期性日志分析失败");
            }
        }

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 初始化日志分析器
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("初始化LogAnalyzer");
                
                // 执行初始化逻辑
                await Task.CompletedTask;
                
                _logger.LogInformation("LogAnalyzer初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化LogAnalyzer失败");
                throw;
            }
        }

        /// <summary>
        /// 关闭日志分析器
        /// </summary>
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("关闭LogAnalyzer");

                // 执行最后一次分析
                await PeriodicLogAnalysisAsync(cancellationToken);

                _logger.LogInformation("LogAnalyzer关闭完成，处理了{BufferCount}条日志", _logBuffer.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭LogAnalyzer失败");
                throw;
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 获取指定时间范围的日志条目
        /// </summary>
        private List<LogEntry> GetLogEntries(DateTime startTime, DateTime endTime)
        {
            return _logBuffer
                .Where(entry => entry.Timestamp >= startTime && entry.Timestamp <= endTime)
                .OrderBy(entry => entry.Timestamp)
                .ToList();
        }

        /// <summary>
        /// 检测日志模式
        /// </summary>
        private async Task<List<LogPattern>> DetectLogPatternsAsync(List<LogEntry> logEntries, CancellationToken cancellationToken)
        {
            var patterns = new List<LogPattern>();

            try
            {
                // 按消息模板分组
                var messageGroups = logEntries
                    .GroupBy(entry => ExtractMessageTemplate(entry.Message))
                    .Where(g => g.Count() >= _patternDetectionThreshold)
                    .ToList();

                foreach (var group in messageGroups)
                {
                    var pattern = new LogPattern
                    {
                        MessageTemplate = group.Key,
                        Frequency = group.Count(),
                        FirstOccurrence = group.Min(e => e.Timestamp),
                        LastOccurrence = group.Max(e => e.Timestamp),
                        ExampleMessages = group.Take(3).Select(e => e.Message).ToList(),
                        Severity = DeterminePatternSeverity(group.Key, group.ToList())
                    };

                    pattern.PatternDescription = GeneratePatternDescription(pattern);
                    patterns.Add(pattern);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测日志模式失败");
            }

            return patterns.OrderByDescending(p => p.Frequency).ToList();
        }

        /// <summary>
        /// 在日志中检测异常
        /// </summary>
        private async Task<List<LogAnomaly>> DetectAnomaliesInLogsAsync(List<LogEntry> logEntries, CancellationToken cancellationToken)
        {
            var anomalies = new List<LogAnomaly>();

            try
            {
                // 检测错误激增
                var errorSpikes = DetectErrorSpikes(logEntries);
                anomalies.AddRange(errorSpikes);

                // 检测未知错误模式
                var unknownErrorPatterns = DetectUnknownErrorPatterns(logEntries);
                anomalies.AddRange(unknownErrorPatterns);

                // 检测性能异常
                var performanceAnomalies = DetectPerformanceAnomalies(logEntries);
                anomalies.AddRange(performanceAnomalies);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测日志异常失败");
            }

            return anomalies.OrderByDescending(a => a.Severity).ToList();
        }

        /// <summary>
        /// 实时异常检测
        /// </summary>
        private async Task DetectRealTimeAnomalyAsync(LogEntry logEntry, CancellationToken cancellationToken)
        {
            try
            {
                // 检查是否匹配已知的严重错误模式
                foreach (var pattern in _errorPatterns)
                {
                    if (pattern.Value.IsMatch(logEntry.Message))
                    {
                        var anomaly = new LogAnomaly
                        {
                            AnomalyType = "Critical Error Pattern",
                            Description = $"检测到严重错误模式：{pattern.Key}",
                            Severity = _patternSeverityMap.TryGetValue(pattern.Key, out var severity) 
                                ? (AnomalySeverity)(int)severity 
                                : AnomalySeverity.Major,
                            DetectedAt = DateTime.UtcNow,
                            Evidence = logEntry.Message,
                            RecommendedAction = GetRecommendedAction(pattern.Key)
                        };

                        _detectedAnomalies.Enqueue(anomaly);
                        
                        _logger.LogWarning("实时检测到日志异常：{AnomalyType} - {Description}", 
                            anomaly.AnomalyType, anomaly.Description);
                        break;
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "实时异常检测失败");
            }
        }

        /// <summary>
        /// 提取消息模板
        /// </summary>
        private string ExtractMessageTemplate(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "Empty";

            // 简单的模板提取：替换数字、GUID、时间戳等为占位符
            var template = message;
            
            // 替换数字
            template = Regex.Replace(template, @"\b\d+\b", "{number}");
            
            // 替换GUID
            template = Regex.Replace(template, @"\b[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}\b", "{guid}");
            
            // 替换时间戳
            template = Regex.Replace(template, @"\d{4}-\d{2}-\d{2}[T\s]\d{2}:\d{2}:\d{2}", "{timestamp}");
            
            // 替换文件路径
            template = Regex.Replace(template, @"[C-Z]:\\[\w\\\s.-]+", "{filepath}");

            return template;
        }

        /// <summary>
        /// 确定模式严重程度
        /// </summary>
        private PatternSeverity DeterminePatternSeverity(string template, List<LogEntry> entries)
        {
            var errorCount = entries.Count(e => e.Level == LogLevel.Error);
            var warningCount = entries.Count(e => e.Level == LogLevel.Warning);
            
            if (errorCount > entries.Count * 0.8) return PatternSeverity.Critical;
            if (errorCount > entries.Count * 0.5) return PatternSeverity.High;
            if (warningCount > entries.Count * 0.7) return PatternSeverity.Medium;
            
            return PatternSeverity.Low;
        }

        /// <summary>
        /// 生成模式描述
        /// </summary>
        private string GeneratePatternDescription(LogPattern pattern)
        {
            var severity = pattern.Severity switch
            {
                PatternSeverity.Critical => "严重",
                PatternSeverity.High => "高",
                PatternSeverity.Medium => "中",
                PatternSeverity.Low => "低",
                _ => "未知"
            };

            return $"{severity}级别日志模式，出现{pattern.Frequency}次";
        }

        /// <summary>
        /// 获取日志级别分布
        /// </summary>
        private Dictionary<string, int> GetLogLevelDistribution(List<LogEntry> entries)
        {
            return entries
                .GroupBy(e => e.Level.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取顶级错误消息
        /// </summary>
        private List<string> GetTopErrorMessages(List<LogEntry> entries, int count)
        {
            return entries
                .Where(e => e.Level == LogLevel.Error)
                .GroupBy(e => ExtractMessageTemplate(e.Message))
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.First().Message)
                .ToList();
        }

        /// <summary>
        /// 生成分析洞察
        /// </summary>
        private List<string> GenerateAnalysisInsights(LogAnalysisResult result)
        {
            var insights = new List<string>();

            if (result.ErrorLogCount > result.TotalLogEntries * 0.1)
            {
                insights.Add($"错误日志比例较高（{(double)result.ErrorLogCount / result.TotalLogEntries:P2}），需要关注应用程序稳定性");
            }

            if (result.DetectedPatterns.Any(p => p.Severity >= PatternSeverity.High))
            {
                insights.Add($"发现{result.DetectedPatterns.Count(p => p.Severity >= PatternSeverity.High)}个高严重级别的日志模式");
            }

            if (result.Anomalies.Any(a => a.Severity >= AnomalySeverity.Major))
            {
                insights.Add($"检测到{result.Anomalies.Count(a => a.Severity >= AnomalySeverity.Major)}个重大异常");
            }

            if (insights.Count == 0)
            {
                insights.Add("日志状态正常，未发现明显异常");
            }

            return insights;
        }

        /// <summary>
        /// 检测错误激增
        /// </summary>
        private List<LogAnomaly> DetectErrorSpikes(List<LogEntry> entries)
        {
            var anomalies = new List<LogAnomaly>();

            var errorsByMinute = entries
                .Where(e => e.Level == LogLevel.Error)
                .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, e.Timestamp.Minute, 0))
                .ToDictionary(g => g.Key, g => g.Count());

            var avgErrorsPerMinute = errorsByMinute.Any() ? errorsByMinute.Values.Average() : 0;
            var threshold = Math.Max(5, avgErrorsPerMinute * 3); // 至少5个或3倍平均值

            foreach (var kvp in errorsByMinute.Where(kvp => kvp.Value > threshold))
            {
                anomalies.Add(new LogAnomaly
                {
                    AnomalyType = "Error Spike",
                    Description = $"在{kvp.Key:HH:mm}检测到错误激增，{kvp.Value}个错误",
                    Severity = AnomalySeverity.Major,
                    DetectedAt = DateTime.UtcNow,
                    Evidence = $"错误数量：{kvp.Value}，阈值：{threshold:F1}",
                    RecommendedAction = "检查应用程序在该时间段的运行状况"
                });
            }

            return anomalies;
        }

        /// <summary>
        /// 检测未知错误模式
        /// </summary>
        private List<LogAnomaly> DetectUnknownErrorPatterns(List<LogEntry> entries)
        {
            var anomalies = new List<LogAnomaly>();

            var unknownErrors = entries
                .Where(e => e.Level == LogLevel.Error)
                .Where(e => !_errorPatterns.Values.Any(pattern => pattern.IsMatch(e.Message)))
                .GroupBy(e => ExtractMessageTemplate(e.Message))
                .Where(g => g.Count() >= 3)
                .ToList();

            foreach (var group in unknownErrors)
            {
                anomalies.Add(new LogAnomaly
                {
                    AnomalyType = "Unknown Error Pattern",
                    Description = $"发现未知错误模式，出现{group.Count()}次",
                    Severity = AnomalySeverity.Moderate,
                    DetectedAt = DateTime.UtcNow,
                    Evidence = group.Key,
                    RecommendedAction = "分析该错误模式并考虑添加到已知模式库"
                });
            }

            return anomalies;
        }

        /// <summary>
        /// 检测性能异常
        /// </summary>
        private List<LogAnomaly> DetectPerformanceAnomalies(List<LogEntry> entries)
        {
            var anomalies = new List<LogAnomaly>();

            var performanceKeywords = new[] { "slow", "timeout", "performance", "延迟", "超时", "缓慢" };
            
            var performanceIssues = entries
                .Where(e => performanceKeywords.Any(keyword => 
                    e.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(e => ExtractMessageTemplate(e.Message))
                .Where(g => g.Count() >= 2)
                .ToList();

            foreach (var group in performanceIssues)
            {
                anomalies.Add(new LogAnomaly
                {
                    AnomalyType = "Performance Issue",
                    Description = $"检测到性能相关日志模式，出现{group.Count()}次",
                    Severity = AnomalySeverity.Moderate,
                    DetectedAt = DateTime.UtcNow,
                    Evidence = group.Key,
                    RecommendedAction = "检查相关组件的性能表现"
                });
            }

            return anomalies;
        }

        /// <summary>
        /// 初始化错误模式
        /// </summary>
        private Dictionary<string, Regex> InitializeErrorPatterns()
        {
            return new Dictionary<string, Regex>
            {
                ["OutOfMemoryException"] = new Regex(@"OutOfMemoryException|内存不足", RegexOptions.IgnoreCase),
                ["NullReferenceException"] = new Regex(@"NullReferenceException|空引用", RegexOptions.IgnoreCase),
                ["SqlException"] = new Regex(@"SqlException|数据库.*错误", RegexOptions.IgnoreCase),
                ["TimeoutException"] = new Regex(@"TimeoutException|超时|timeout", RegexOptions.IgnoreCase),
                ["UnauthorizedException"] = new Regex(@"UnauthorizedException|未授权|unauthorized", RegexOptions.IgnoreCase),
                ["FileNotFoundException"] = new Regex(@"FileNotFoundException|文件.*不存在", RegexOptions.IgnoreCase)
            };
        }

        /// <summary>
        /// 初始化模式严重程度映射
        /// </summary>
        private Dictionary<string, PatternSeverity> InitializePatternSeverityMap()
        {
            return new Dictionary<string, PatternSeverity>
            {
                ["OutOfMemoryException"] = PatternSeverity.Critical,
                ["NullReferenceException"] = PatternSeverity.High,
                ["SqlException"] = PatternSeverity.High,
                ["TimeoutException"] = PatternSeverity.Medium,
                ["UnauthorizedException"] = PatternSeverity.Medium,
                ["FileNotFoundException"] = PatternSeverity.Low
            };
        }

        /// <summary>
        /// 获取推荐操作
        /// </summary>
        private string GetRecommendedAction(string patternKey)
        {
            return patternKey switch
            {
                "OutOfMemoryException" => "检查内存使用情况，考虑增加内存或优化内存使用",
                "NullReferenceException" => "检查相关代码的空值处理",
                "SqlException" => "检查数据库连接和查询语句",
                "TimeoutException" => "检查网络连接和超时配置",
                "UnauthorizedException" => "检查身份验证和授权配置",
                "FileNotFoundException" => "检查文件路径和文件是否存在",
                _ => "进一步分析错误原因并采取相应措施"
            };
        }

        #endregion

        #region 内部数据类

        /// <summary>
        /// 日志条目
        /// </summary>
        private class LogEntry
        {
            public string Message { get; set; } = string.Empty;
            public LogLevel Level { get; set; }
            public DateTime Timestamp { get; set; }
            public string Category { get; set; } = string.Empty;
            public string? Exception { get; set; }
            public int ThreadId { get; set; }
        }

        #endregion
    }
}