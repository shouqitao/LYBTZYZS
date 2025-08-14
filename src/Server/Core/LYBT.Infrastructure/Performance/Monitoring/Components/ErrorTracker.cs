using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LYBT.Infrastructure.Performance.Monitoring.Components
{
    /// <summary>
    /// 错误追踪器 - UltraThink专门化组件
    /// 职责单一：专注统一错误收集、分类、预警机制
    /// 代码干净：清晰的错误处理和分析逻辑
    /// 性能出色：高效的错误统计和趋势分析
    /// </summary>
    public class ErrorTracker
    {
        private readonly ILogger<ErrorTracker> _logger;
        private readonly ConcurrentQueue<ErrorRecord> _errorBuffer;
        private readonly ConcurrentDictionary<string, CriticalError> _criticalErrorsCache;
        private readonly ConcurrentQueue<ErrorRecord> _pendingProcessing;
        private readonly object _statisticsLock = new object();
        
        // 错误追踪配置
        private readonly int _maxErrorBufferSize = 25000;
        private readonly TimeSpan _criticalErrorThreshold = TimeSpan.FromMinutes(5); // 5分钟内重复出现视为关键错误
        private readonly int _criticalErrorCountThreshold = 3;
        private readonly Dictionary<string, ErrorSeverity> _exceptionSeverityMap;

        public ErrorTracker(ILogger<ErrorTracker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorBuffer = new ConcurrentQueue<ErrorRecord>();
            _criticalErrorsCache = new ConcurrentDictionary<string, CriticalError>();
            _pendingProcessing = new ConcurrentQueue<ErrorRecord>();
            _exceptionSeverityMap = InitializeExceptionSeverityMap();
        }

        #region 核心错误追踪方法

        /// <summary>
        /// 追踪应用程序错误
        /// </summary>
        public async Task TrackErrorAsync(Exception exception, string context, CancellationToken cancellationToken = default)
        {
            try
            {
                var errorRecord = new ErrorRecord
                {
                    ErrorId = Guid.NewGuid().ToString("N")[..8],
                    Exception = exception,
                    Context = context,
                    Timestamp = DateTime.UtcNow,
                    Severity = DetermineErrorSeverity(exception),
                    AdditionalData = ExtractAdditionalData(exception, context)
                };

                // 添加到缓冲区和待处理队列
                _errorBuffer.Enqueue(errorRecord);
                _pendingProcessing.Enqueue(errorRecord);

                // 维护缓冲区大小
                while (_errorBuffer.Count > _maxErrorBufferSize)
                {
                    _errorBuffer.TryDequeue(out _);
                }

                _logger.LogDebug("记录错误：{ErrorType} in {Context}，错误ID：{ErrorId}", 
                    exception.GetType().Name, context, errorRecord.ErrorId);

                // 实时检查是否为关键错误
                await CheckForCriticalErrorAsync(errorRecord, cancellationToken);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录错误失败");
                // 错误追踪器本身的错误不应影响业务流程
            }
        }

        /// <summary>
        /// 追踪性能问题
        /// </summary>
        public async Task TrackPerformanceIssueAsync(string description, CancellationToken cancellationToken = default)
        {
            try
            {
                var performanceException = new PerformanceException(description);
                await TrackErrorAsync(performanceException, "Performance Monitor", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录性能问题失败");
            }
        }

        /// <summary>
        /// 获取错误统计报告
        /// </summary>
        public async Task<ErrorStatisticsReport> GetErrorStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始生成错误统计报告：{StartTime} - {EndTime}", startTime, endTime);

                var filteredErrors = GetErrorRecords(startTime, endTime);

                var report = new ErrorStatisticsReport
                {
                    ReportStartTime = startTime,
                    ReportEndTime = endTime,
                    TotalErrors = filteredErrors.Count,
                    CriticalErrors = filteredErrors.Count(e => e.Severity >= ErrorSeverity.Critical),
                    UnhandledExceptions = filteredErrors.Count(e => e.Context.Contains("Unhandled", StringComparison.OrdinalIgnoreCase))
                };

                // 错误类型分布
                report.ErrorTypeDistribution = filteredErrors
                    .GroupBy(e => e.Exception.GetType().Name)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 错误来源分布
                report.ErrorSourceDistribution = filteredErrors
                    .GroupBy(e => e.Context)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 最常见错误类型
                report.MostCommonErrorType = report.ErrorTypeDistribution
                    .OrderByDescending(kvp => kvp.Value)
                    .FirstOrDefault().Key ?? "None";

                // 关键错误
                report.TopCriticalErrors = await GetCriticalErrorsAsync(10, cancellationToken);

                // 错误趋势
                report.ErrorTrends = GenerateErrorTrends(filteredErrors);

                // 计算错误率（假设有基准请求数）
                var timeSpan = endTime - startTime;
                var estimatedRequests = Math.Max(1, (int)(timeSpan.TotalHours * 100)); // 估算每小时100个请求
                report.ErrorRate = (double)report.TotalErrors / estimatedRequests;

                _logger.LogInformation("错误统计报告生成完成：总错误数={TotalErrors}，关键错误数={CriticalErrors}，错误率={ErrorRate:P4}",
                    report.TotalErrors, report.CriticalErrors, report.ErrorRate);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成错误统计报告失败");
                throw;
            }
        }

        /// <summary>
        /// 获取关键错误列表
        /// </summary>
        public async Task<List<CriticalError>> GetCriticalErrorsAsync(int topCount = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var criticalErrors = _criticalErrorsCache.Values
                    .OrderByDescending(ce => ce.OccurrenceCount)
                    .ThenByDescending(ce => ce.LastOccurrence)
                    .Take(topCount)
                    .ToList();

                _logger.LogDebug("获取关键错误列表完成：{Count}个错误", criticalErrors.Count);
                return await Task.FromResult(criticalErrors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取关键错误列表失败");
                throw;
            }
        }

        #endregion

        #region 周期性错误处理

        /// <summary>
        /// 处理待处理的错误
        /// </summary>
        public async Task ProcessPendingErrorsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始处理待处理的错误");

                var processedCount = 0;
                while (_pendingProcessing.TryDequeue(out var errorRecord) && processedCount < 100)
                {
                    await ProcessSingleErrorAsync(errorRecord, cancellationToken);
                    processedCount++;
                }

                // 清理过期的关键错误
                await CleanupExpiredCriticalErrorsAsync(cancellationToken);

                if (processedCount > 0)
                {
                    _logger.LogDebug("处理待处理错误完成：{ProcessedCount}个", processedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理待处理错误失败");
            }
        }

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 初始化错误追踪器
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("初始化ErrorTracker");
                
                // 执行初始化逻辑
                await Task.CompletedTask;
                
                _logger.LogInformation("ErrorTracker初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化ErrorTracker失败");
                throw;
            }
        }

        /// <summary>
        /// 关闭错误追踪器
        /// </summary>
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("关闭ErrorTracker");

                // 处理剩余的待处理错误
                await ProcessPendingErrorsAsync(cancellationToken);

                _logger.LogInformation("ErrorTracker关闭完成，共处理了{ErrorCount}个错误", _errorBuffer.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭ErrorTracker失败");
                throw;
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 获取指定时间范围的错误记录
        /// </summary>
        private List<ErrorRecord> GetErrorRecords(DateTime startTime, DateTime endTime)
        {
            return _errorBuffer
                .Where(record => record.Timestamp >= startTime && record.Timestamp <= endTime)
                .OrderBy(record => record.Timestamp)
                .ToList();
        }

        /// <summary>
        /// 检查是否为关键错误
        /// </summary>
        private async Task CheckForCriticalErrorAsync(ErrorRecord errorRecord, CancellationToken cancellationToken)
        {
            try
            {
                var errorKey = GenerateErrorKey(errorRecord.Exception);
                var now = DateTime.UtcNow;

                // 检查是否已存在相同的关键错误
                if (_criticalErrorsCache.TryGetValue(errorKey, out var existingError))
                {
                    // 更新现有关键错误
                    existingError.OccurrenceCount++;
                    existingError.LastOccurrence = now;
                    
                    if (now - existingError.FirstOccurrence <= _criticalErrorThreshold && 
                        existingError.OccurrenceCount >= _criticalErrorCountThreshold)
                    {
                        existingError.Severity = ErrorSeverity.Critical;
                        _logger.LogWarning("关键错误频率增加：{ErrorType}，发生次数：{Count}，达到关键阈值", 
                            errorRecord.Exception.GetType().Name, existingError.OccurrenceCount);
                    }
                }
                else
                {
                    // 创建新的关键错误条目
                    var criticalError = new CriticalError
                    {
                        ErrorType = errorRecord.Exception.GetType().Name,
                        Message = errorRecord.Exception.Message,
                        StackTrace = errorRecord.Exception.StackTrace ?? string.Empty,
                        Source = errorRecord.Exception.Source ?? string.Empty,
                        FirstOccurrence = now,
                        LastOccurrence = now,
                        OccurrenceCount = 1,
                        Severity = errorRecord.Severity,
                        Context = errorRecord.Context,
                        AdditionalData = new Dictionary<string, object>(errorRecord.AdditionalData)
                    };

                    _criticalErrorsCache[errorKey] = criticalError;
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查关键错误失败");
            }
        }

        /// <summary>
        /// 处理单个错误
        /// </summary>
        private async Task ProcessSingleErrorAsync(ErrorRecord errorRecord, CancellationToken cancellationToken)
        {
            try
            {
                // 分析错误模式
                var errorPattern = AnalyzeErrorPattern(errorRecord);
                
                // 根据错误严重程度执行相应动作
                if (errorRecord.Severity >= ErrorSeverity.Critical)
                {
                    await HandleCriticalErrorAsync(errorRecord, cancellationToken);
                }
                else if (errorRecord.Severity >= ErrorSeverity.High)
                {
                    await HandleHighPriorityErrorAsync(errorRecord, cancellationToken);
                }

                // 更新错误统计
                UpdateErrorStatistics(errorRecord);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理单个错误失败：{ErrorId}", errorRecord.ErrorId);
            }
        }

        /// <summary>
        /// 处理关键错误
        /// </summary>
        private async Task HandleCriticalErrorAsync(ErrorRecord errorRecord, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogCritical("检测到关键错误：{ErrorType} - {Message}\n上下文：{Context}\n堆栈跟踪：{StackTrace}",
                    errorRecord.Exception.GetType().Name, 
                    errorRecord.Exception.Message,
                    errorRecord.Context,
                    errorRecord.Exception.StackTrace);

                // 这里可以添加关键错误的特殊处理逻辑
                // 例如：发送邮件、调用Webhook、写入特殊日志等
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理关键错误失败");
            }
        }

        /// <summary>
        /// 处理高优先级错误
        /// </summary>
        private async Task HandleHighPriorityErrorAsync(ErrorRecord errorRecord, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogError("检测到高优先级错误：{ErrorType} - {Message}，上下文：{Context}",
                    errorRecord.Exception.GetType().Name, 
                    errorRecord.Exception.Message,
                    errorRecord.Context);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理高优先级错误失败");
            }
        }

        /// <summary>
        /// 清理过期的关键错误
        /// </summary>
        private async Task CleanupExpiredCriticalErrorsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var expiredKeys = new List<string>();
                var cutoffTime = DateTime.UtcNow.AddHours(-24); // 24小时前

                foreach (var kvp in _criticalErrorsCache)
                {
                    if (kvp.Value.LastOccurrence < cutoffTime)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _criticalErrorsCache.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("清理过期关键错误：{Count}个", expiredKeys.Count);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期关键错误失败");
            }
        }

        /// <summary>
        /// 确定错误严重程度
        /// </summary>
        private ErrorSeverity DetermineErrorSeverity(Exception exception)
        {
            var exceptionType = exception.GetType().Name;
            
            if (_exceptionSeverityMap.TryGetValue(exceptionType, out var severity))
            {
                return severity;
            }

            // 根据异常类型默认分配严重程度
            return exception switch
            {
                OutOfMemoryException => ErrorSeverity.Critical,
                StackOverflowException => ErrorSeverity.Critical,
                AccessViolationException => ErrorSeverity.Critical,
                ArgumentNullException => ErrorSeverity.Medium,
                ArgumentException => ErrorSeverity.Medium,
                InvalidOperationException => ErrorSeverity.Medium,
                NotImplementedException => ErrorSeverity.Low,
                _ => ErrorSeverity.Medium
            };
        }

        /// <summary>
        /// 提取附加数据
        /// </summary>
        private Dictionary<string, object> ExtractAdditionalData(Exception exception, string context)
        {
            var additionalData = new Dictionary<string, object>
            {
                ["ExceptionType"] = exception.GetType().FullName ?? "Unknown",
                ["Context"] = context,
                ["MachineName"] = Environment.MachineName,
                ["ProcessId"] = Environment.ProcessId,
                ["ThreadId"] = Environment.CurrentManagedThreadId,
                ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };

            // 添加异常特定数据
            if (exception.Data.Count > 0)
            {
                additionalData["ExceptionData"] = exception.Data.Cast<object>().ToArray();
            }

            if (exception.InnerException != null)
            {
                additionalData["InnerExceptionType"] = exception.InnerException.GetType().Name;
                additionalData["InnerExceptionMessage"] = exception.InnerException.Message;
            }

            return additionalData;
        }

        /// <summary>
        /// 生成错误键
        /// </summary>
        private string GenerateErrorKey(Exception exception)
        {
            // 基于异常类型和消息的前100个字符生成键
            var message = exception.Message.Length > 100 
                ? exception.Message[..100] 
                : exception.Message;
            
            return $"{exception.GetType().Name}:{message}".GetHashCode().ToString();
        }

        /// <summary>
        /// 分析错误模式
        /// </summary>
        private string AnalyzeErrorPattern(ErrorRecord errorRecord)
        {
            // 简单的错误模式分析
            var exception = errorRecord.Exception;
            
            return exception switch
            {
                OutOfMemoryException => "Memory_Exhaustion",
                TimeoutException => "Timeout_Issue",
                ArgumentNullException => "Null_Reference",
                SqlException => "Database_Error",
                UnauthorizedAccessException => "Authorization_Failure",
                _ => "General_Error"
            };
        }

        /// <summary>
        /// 更新错误统计
        /// </summary>
        private void UpdateErrorStatistics(ErrorRecord errorRecord)
        {
            lock (_statisticsLock)
            {
                // 这里可以添加错误统计的更新逻辑
                // 例如：更新内存中的统计计数器、写入数据库等
            }
        }

        /// <summary>
        /// 生成错误趋势
        /// </summary>
        private List<ErrorTrend> GenerateErrorTrends(List<ErrorRecord> errorRecords)
        {
            return errorRecords
                .GroupBy(e => new { 
                    TimeSlot = new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0),
                    ErrorType = e.Exception.GetType().Name 
                })
                .Select(g => new ErrorTrend
                {
                    TimeSlot = g.Key.TimeSlot,
                    ErrorType = g.Key.ErrorType,
                    ErrorCount = g.Count(),
                    Direction = DetermineTrendDirection(g.Key.ErrorType, g.Key.TimeSlot, errorRecords)
                })
                .OrderBy(t => t.TimeSlot)
                .ToList();
        }

        /// <summary>
        /// 确定趋势方向
        /// </summary>
        private TrendDirection DetermineTrendDirection(string errorType, DateTime timeSlot, List<ErrorRecord> allErrors)
        {
            var previousHour = timeSlot.AddHours(-1);
            
            var currentCount = allErrors.Count(e => 
                e.Exception.GetType().Name == errorType &&
                e.Timestamp >= timeSlot && e.Timestamp < timeSlot.AddHours(1));
                
            var previousCount = allErrors.Count(e => 
                e.Exception.GetType().Name == errorType &&
                e.Timestamp >= previousHour && e.Timestamp < previousHour.AddHours(1));

            if (currentCount > previousCount) return TrendDirection.Increasing;
            if (currentCount < previousCount) return TrendDirection.Decreasing;
            return TrendDirection.Stable;
        }

        /// <summary>
        /// 初始化异常严重程度映射
        /// </summary>
        private Dictionary<string, ErrorSeverity> InitializeExceptionSeverityMap()
        {
            return new Dictionary<string, ErrorSeverity>
            {
                ["OutOfMemoryException"] = ErrorSeverity.Critical,
                ["StackOverflowException"] = ErrorSeverity.Critical,
                ["AccessViolationException"] = ErrorSeverity.Critical,
                ["SqlException"] = ErrorSeverity.High,
                ["TimeoutException"] = ErrorSeverity.High,
                ["UnauthorizedAccessException"] = ErrorSeverity.High,
                ["ArgumentNullException"] = ErrorSeverity.Medium,
                ["ArgumentException"] = ErrorSeverity.Medium,
                ["InvalidOperationException"] = ErrorSeverity.Medium,
                ["NotSupportedException"] = ErrorSeverity.Low,
                ["NotImplementedException"] = ErrorSeverity.Low
            };
        }

        #endregion

        #region 内部数据类

        /// <summary>
        /// 错误记录
        /// </summary>
        private class ErrorRecord
        {
            public string ErrorId { get; set; } = string.Empty;
            public Exception Exception { get; set; } = null!;
            public string Context { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public ErrorSeverity Severity { get; set; }
            public Dictionary<string, object> AdditionalData { get; set; } = new();
        }

        /// <summary>
        /// 性能异常（自定义异常）
        /// </summary>
        private class PerformanceException : Exception
        {
            public PerformanceException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// SQL异常（简化版本）
        /// </summary>
        private class SqlException : Exception
        {
            public SqlException(string message) : base(message)
            {
            }
        }

        #endregion
    }
}