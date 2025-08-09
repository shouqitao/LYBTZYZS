using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.IO.Compression;
using System.Text;
using OfficeOpenXml;

namespace LYBT.Infrastructure.Logging
{
    /// <summary>
    /// 统一日志管理器实现 - UltraThink监控优化核心
    /// 职责单一：专注日志记录、监控和分析
    /// 代码干净：清晰的日志分类和性能跟踪
    /// 性能出色：异步批处理和智能存储
    /// </summary>
    public class UnifiedLogger : IUnifiedLogger, IHostedService, IDisposable
    {
        private readonly ILogger<UnifiedLogger> _systemLogger;
        private readonly IHostEnvironment _environment;
        
        // 内存存储（可扩展为数据库存储）
        private readonly ConcurrentQueue<LogEntry> _logQueue = new();
        private readonly ConcurrentDictionary<string, PerformanceTracker> _activeTrackers = new();
        private readonly Timer _flushTimer;
        private readonly Timer _cleanupTimer;
        
        // 配置参数
        private readonly string _logDirectory;
        private readonly int _maxQueueSize = 10000;
        private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _logRetentionPeriod = TimeSpan.FromDays(30);
        
        // 统计数据
        private readonly LogStatisticsCollector _statisticsCollector = new();

        public UnifiedLogger(ILogger<UnifiedLogger> systemLogger, IHostEnvironment environment)
        {
            _systemLogger = systemLogger ?? throw new ArgumentNullException(nameof(systemLogger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(_logDirectory);
            
            _flushTimer = new Timer(FlushLogsCallback, null, _flushInterval, _flushInterval);
            _cleanupTimer = new Timer(CleanupCallback, null, _cleanupInterval, _cleanupInterval);
            
            _systemLogger.LogInformation("统一日志管理器初始化完成，日志目录: {LogDirectory}", _logDirectory);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public async Task LogInfoAsync(string message, object? data = null,
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            await LogAsync(LogLevel.Information, LogCategory.General, message, null, data,
                memberName, filePath, lineNumber);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public async Task LogWarningAsync(string message, object? data = null,
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            await LogAsync(LogLevel.Warning, LogCategory.General, message, null, data,
                memberName, filePath, lineNumber);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public async Task LogErrorAsync(Exception exception, string message, object? data = null,
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            await LogAsync(LogLevel.Error, LogCategory.Error, message, exception, data,
                memberName, filePath, lineNumber);
        }

        /// <summary>
        /// 记录业务操作日志
        /// </summary>
        public async Task LogOperationAsync(string operation, string result, object? context = null,
            TimeSpan? duration = null, string? userId = null)
        {
            var entry = CreateLogEntry(LogLevel.Information, LogCategory.Operation,
                $"操作: {operation}, 结果: {result}");
            
            if (duration.HasValue)
                entry.Duration = duration.Value;
            
            if (!string.IsNullOrEmpty(userId))
                entry.UserId = userId;
            
            if (context != null)
                entry.Data["Context"] = context;
            
            await EnqueueLogAsync(entry);
        }

        /// <summary>
        /// 记录性能日志
        /// </summary>
        public async Task LogPerformanceAsync(string operation, TimeSpan duration,
            PerformanceMetrics? metrics = null, object? context = null)
        {
            var entry = CreateLogEntry(LogLevel.Information, LogCategory.Performance,
                $"性能: {operation}, 耗时: {duration.TotalMilliseconds:F2}ms");
            
            entry.Duration = duration;
            entry.Metrics = metrics;
            
            if (context != null)
                entry.Data["Context"] = context;
            
            await EnqueueLogAsync(entry);
            
            // 更新性能统计
            _statisticsCollector.AddPerformanceData(operation, duration, metrics);
        }

        /// <summary>
        /// 记录安全日志
        /// </summary>
        public async Task LogSecurityEventAsync(SecurityEventType eventType, string description,
            string? userId = null, string? ipAddress = null, object? additionalData = null)
        {
            var entry = CreateLogEntry(LogLevel.Warning, LogCategory.Security,
                $"安全事件: {eventType}, 描述: {description}");
            
            entry.UserId = userId;
            entry.IpAddress = ipAddress;
            entry.Data["EventType"] = eventType.ToString();
            
            if (additionalData != null)
                entry.Data["AdditionalData"] = additionalData;
            
            await EnqueueLogAsync(entry);
            
            // 高风险安全事件立即记录到系统日志
            if (IsHighRiskSecurityEvent(eventType))
            {
                _systemLogger.LogWarning("高风险安全事件: {EventType} - {Description}, 用户: {UserId}, IP: {IpAddress}",
                    eventType, description, userId, ipAddress);
            }
        }

        /// <summary>
        /// 记录审计日志
        /// </summary>
        public async Task LogAuditAsync(string action, string resource, string? oldValue, string? newValue,
            string? userId = null, object? metadata = null)
        {
            var entry = CreateLogEntry(LogLevel.Information, LogCategory.Audit,
                $"审计: {action} on {resource}");
            
            entry.UserId = userId;
            entry.Data["Action"] = action;
            entry.Data["Resource"] = resource;
            
            if (!string.IsNullOrEmpty(oldValue))
                entry.Data["OldValue"] = oldValue;
            
            if (!string.IsNullOrEmpty(newValue))
                entry.Data["NewValue"] = newValue;
            
            if (metadata != null)
                entry.Data["Metadata"] = metadata;
            
            await EnqueueLogAsync(entry);
        }

        /// <summary>
        /// 开始性能跟踪
        /// </summary>
        public IPerformanceTracker StartPerformanceTracking(string operation, object? context = null)
        {
            var tracker = new PerformanceTracker(operation, this);
            
            if (context != null)
                tracker.AddContext("InitialContext", context);
            
            _activeTrackers.TryAdd(tracker.Id, tracker);
            return tracker;
        }

        /// <summary>
        /// 批量日志记录
        /// </summary>
        public async Task LogBatchAsync(IEnumerable<LogEntry> entries)
        {
            var entriesList = entries.ToList();
            if (entriesList.Count == 0) return;
            
            try
            {
                foreach (var entry in entriesList)
                {
                    await EnqueueLogAsync(entry);
                }
                
                _systemLogger.LogDebug("批量日志记录完成: {Count}条", entriesList.Count);
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "批量日志记录失败");
                throw;
            }
        }

        /// <summary>
        /// 结构化查询日志
        /// </summary>
        public async Task<List<LogEntry>> QueryLogsAsync(LogQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                // 这里可以扩展为从数据库或文件系统查询
                // 当前从内存队列中查询（简化实现）
                var logs = _logQueue.ToList();
                
                // 应用筛选条件
                var filteredLogs = ApplyQueryFilters(logs, query);
                
                // 应用排序
                var sortedLogs = ApplySorting(filteredLogs, query);
                
                // 应用分页
                var pagedLogs = ApplyPagination(sortedLogs, query.Pagination);
                
                _systemLogger.LogDebug("日志查询完成: 查询到{Count}条记录", pagedLogs.Count);
                
                return await Task.FromResult(pagedLogs);
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "日志查询失败");
                throw;
            }
        }

        /// <summary>
        /// 获取日志统计
        /// </summary>
        public async Task<LogStatistics> GetStatisticsAsync(TimeSpan timeRange, CancellationToken cancellationToken = default)
        {
            try
            {
                var statistics = await _statisticsCollector.GenerateStatisticsAsync(timeRange);
                _systemLogger.LogDebug("日志统计生成完成: 时间范围{TimeRange}", timeRange);
                return statistics;
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "生成日志统计失败");
                throw;
            }
        }

        /// <summary>
        /// 导出日志
        /// </summary>
        public async Task<Stream> ExportLogsAsync(LogExportOptions options, CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = await QueryLogsAsync(options.Query, cancellationToken);
                
                // 限制导出记录数
                if (logs.Count > options.MaxRecords)
                {
                    logs = logs.Take(options.MaxRecords).ToList();
                    _systemLogger.LogWarning("日志导出记录数超限，已限制为{MaxRecords}条", options.MaxRecords);
                }
                
                var stream = await GenerateExportStreamAsync(logs, options, cancellationToken);
                
                _systemLogger.LogInformation("日志导出完成: {Count}条记录, 格式: {Format}",
                    logs.Count, options.Format);
                
                return stream;
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "日志导出失败");
                throw;
            }
        }

        /// <summary>
        /// 清理旧日志
        /// </summary>
        public async Task<int> CleanupLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var cleanupCount = 0;
                
                // 清理内存队列中的旧日志
                var allLogs = new List<LogEntry>();
                while (_logQueue.TryDequeue(out var log))
                {
                    if (log.Timestamp >= beforeDate)
                    {
                        allLogs.Add(log);
                    }
                    else
                    {
                        cleanupCount++;
                    }
                }
                
                // 将保留的日志重新入队
                foreach (var log in allLogs)
                {
                    _logQueue.Enqueue(log);
                }
                
                // 清理日志文件
                var fileCleanupCount = await CleanupLogFilesAsync(beforeDate, cancellationToken);
                cleanupCount += fileCleanupCount;
                
                if (cleanupCount > 0)
                {
                    _systemLogger.LogInformation("日志清理完成: 清理了{Count}条记录", cleanupCount);
                }
                
                return cleanupCount;
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "日志清理失败");
                throw;
            }
        }

        #region IHostedService 实现

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _systemLogger.LogInformation("启动统一日志服务");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _systemLogger.LogInformation("停止统一日志服务");
            
            // 最后一次刷新日志
            await FlushLogsAsync();
            
            // 停止定时器
            await _flushTimer.DisposeAsync();
            await _cleanupTimer.DisposeAsync();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 通用日志记录方法
        /// </summary>
        private async Task LogAsync(LogLevel level, LogCategory category, string message,
            Exception? exception = null, object? data = null,
            string? memberName = null, string? filePath = null, int lineNumber = 0)
        {
            var entry = CreateLogEntry(level, category, message, exception, memberName, filePath, lineNumber);
            
            if (data != null)
                entry.Data["Data"] = data;
            
            await EnqueueLogAsync(entry);
        }

        /// <summary>
        /// 创建日志条目
        /// </summary>
        private LogEntry CreateLogEntry(LogLevel level, LogCategory category, string message,
            Exception? exception = null, string? memberName = null, string? filePath = null, int lineNumber = 0)
        {
            return new LogEntry
            {
                Level = level,
                Category = category,
                Message = message,
                Exception = exception,
                Environment = _environment.EnvironmentName,
                CallerInfo = new CallerInfo
                {
                    MemberName = memberName,
                    FilePath = filePath,
                    LineNumber = lineNumber
                },
                OperationId = Activity.Current?.Id
            };
        }

        /// <summary>
        /// 日志入队
        /// </summary>
        private async Task EnqueueLogAsync(LogEntry entry)
        {
            if (_logQueue.Count >= _maxQueueSize)
            {
                // 队列满时，移除最旧的日志
                _logQueue.TryDequeue(out _);
            }
            
            _logQueue.Enqueue(entry);
            _statisticsCollector.RecordLog(entry);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 判断是否为高风险安全事件
        /// </summary>
        private bool IsHighRiskSecurityEvent(SecurityEventType eventType)
        {
            return eventType switch
            {
                SecurityEventType.DataBreach => true,
                SecurityEventType.UnauthorizedAccess => true,
                SecurityEventType.SuspiciousActivity => true,
                SecurityEventType.AccountLocked => true,
                _ => false
            };
        }

        /// <summary>
        /// 应用查询筛选条件
        /// </summary>
        private List<LogEntry> ApplyQueryFilters(List<LogEntry> logs, LogQuery query)
        {
            var filtered = logs.AsEnumerable();
            
            if (query.StartTime.HasValue)
                filtered = filtered.Where(l => l.Timestamp >= query.StartTime.Value);
            
            if (query.EndTime.HasValue)
                filtered = filtered.Where(l => l.Timestamp <= query.EndTime.Value);
            
            if (query.Level.HasValue)
                filtered = filtered.Where(l => l.Level == query.Level.Value);
            
            if (query.Category.HasValue)
                filtered = filtered.Where(l => l.Category == query.Category.Value);
            
            if (!string.IsNullOrEmpty(query.UserId))
                filtered = filtered.Where(l => l.UserId == query.UserId);
            
            if (!string.IsNullOrEmpty(query.OperationId))
                filtered = filtered.Where(l => l.OperationId == query.OperationId);
            
            if (!string.IsNullOrEmpty(query.MessageKeyword))
                filtered = filtered.Where(l => l.Message.Contains(query.MessageKeyword, StringComparison.OrdinalIgnoreCase));
            
            if (!string.IsNullOrEmpty(query.ExceptionType))
                filtered = filtered.Where(l => l.Exception?.GetType().Name.Contains(query.ExceptionType, StringComparison.OrdinalIgnoreCase) == true);
            
            if (!string.IsNullOrEmpty(query.MachineName))
                filtered = filtered.Where(l => l.MachineName.Equals(query.MachineName, StringComparison.OrdinalIgnoreCase));
            
            return filtered.ToList();
        }

        /// <summary>
        /// 应用排序
        /// </summary>
        private List<LogEntry> ApplySorting(List<LogEntry> logs, LogQuery query)
        {
            return query.SortBy.ToLower() switch
            {
                "timestamp" => query.SortDirection == SortDirection.Ascending 
                    ? logs.OrderBy(l => l.Timestamp).ToList()
                    : logs.OrderByDescending(l => l.Timestamp).ToList(),
                "level" => query.SortDirection == SortDirection.Ascending
                    ? logs.OrderBy(l => l.Level).ToList()
                    : logs.OrderByDescending(l => l.Level).ToList(),
                "category" => query.SortDirection == SortDirection.Ascending
                    ? logs.OrderBy(l => l.Category).ToList()
                    : logs.OrderByDescending(l => l.Category).ToList(),
                _ => query.SortDirection == SortDirection.Ascending
                    ? logs.OrderBy(l => l.Timestamp).ToList()
                    : logs.OrderByDescending(l => l.Timestamp).ToList()
            };
        }

        /// <summary>
        /// 应用分页
        /// </summary>
        private List<LogEntry> ApplyPagination(List<LogEntry> logs, PaginationQuery pagination)
        {
            return logs.Skip(pagination.Skip).Take(pagination.PageSize).ToList();
        }

        /// <summary>
        /// 生成导出流
        /// </summary>
        private async Task<Stream> GenerateExportStreamAsync(List<LogEntry> logs, LogExportOptions options, CancellationToken cancellationToken)
        {
            var stream = new MemoryStream();
            
            switch (options.Format)
            {
                case ExportFormat.Json:
                    await GenerateJsonExportAsync(logs, stream, options, cancellationToken);
                    break;
                case ExportFormat.Csv:
                    await GenerateCsvExportAsync(logs, stream, options, cancellationToken);
                    break;
                case ExportFormat.Excel:
                    await GenerateExcelExportAsync(logs, stream, options, cancellationToken);
                    break;
                case ExportFormat.Xml:
                    await GenerateXmlExportAsync(logs, stream, options, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"不支持的导出格式: {options.Format}");
            }
            
            if (options.Compress)
            {
                stream.Position = 0;
                var compressedStream = new MemoryStream();
                using (var gzipStream = new GZipStream(compressedStream, System.IO.Compression.CompressionLevel.Optimal, true))
                {
                    await stream.CopyToAsync(gzipStream, cancellationToken);
                }
                await stream.DisposeAsync();
                compressedStream.Position = 0;
                return compressedStream;
            }
            
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// 生成JSON导出
        /// </summary>
        private async Task GenerateJsonExportAsync(List<LogEntry> logs, Stream stream, LogExportOptions options, CancellationToken cancellationToken)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            await JsonSerializer.SerializeAsync(stream, logs, jsonOptions, cancellationToken);
        }

        /// <summary>
        /// 生成CSV导出
        /// </summary>
        private async Task GenerateCsvExportAsync(List<LogEntry> logs, Stream stream, LogExportOptions options, CancellationToken cancellationToken)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            
            // 写入标题行
            await writer.WriteLineAsync("Timestamp,Level,Category,Message,Exception,UserId,MachineName,Duration");
            
            // 写入数据行
            foreach (var log in logs)
            {
                var line = $"{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                          $"{log.Level}," +
                          $"{log.Category}," +
                          $"\"{log.Message.Replace("\"", "\"\"")}\"," +
                          $"\"{log.Exception?.Message?.Replace("\"", "\"\"") ?? ""}\"," +
                          $"{log.UserId ?? ""}," +
                          $"{log.MachineName}," +
                          $"{log.Duration?.TotalMilliseconds ?? 0}";
                
                await writer.WriteLineAsync(line);
            }
        }

        /// <summary>
        /// 生成Excel导出
        /// </summary>
        private async Task GenerateExcelExportAsync(List<LogEntry> logs, Stream stream, LogExportOptions options, CancellationToken cancellationToken)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Logs");
            
            // 设置标题
            var headers = new[] { "时间戳", "级别", "类别", "消息", "异常", "用户ID", "机器名", "持续时间(ms)" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }
            
            // 填充数据
            for (int row = 0; row < logs.Count; row++)
            {
                var log = logs[row];
                worksheet.Cells[row + 2, 1].Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                worksheet.Cells[row + 2, 2].Value = log.Level.ToString();
                worksheet.Cells[row + 2, 3].Value = log.Category.ToString();
                worksheet.Cells[row + 2, 4].Value = log.Message;
                worksheet.Cells[row + 2, 5].Value = log.Exception?.Message ?? "";
                worksheet.Cells[row + 2, 6].Value = log.UserId ?? "";
                worksheet.Cells[row + 2, 7].Value = log.MachineName;
                worksheet.Cells[row + 2, 8].Value = log.Duration?.TotalMilliseconds ?? 0;
            }
            
            // 自动调整列宽
            worksheet.Cells.AutoFitColumns();
            
            await package.SaveAsAsync(stream, cancellationToken);
        }

        /// <summary>
        /// 生成XML导出
        /// </summary>
        private async Task GenerateXmlExportAsync(List<LogEntry> logs, Stream stream, LogExportOptions options, CancellationToken cancellationToken)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteLineAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            await writer.WriteLineAsync("<Logs>");
            
            foreach (var log in logs)
            {
                await writer.WriteLineAsync("  <Log>");
                await writer.WriteLineAsync($"    <Timestamp>{log.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}</Timestamp>");
                await writer.WriteLineAsync($"    <Level>{log.Level}</Level>");
                await writer.WriteLineAsync($"    <Category>{log.Category}</Category>");
                await writer.WriteLineAsync($"    <Message><![CDATA[{log.Message}]]></Message>");
                if (log.Exception != null)
                    await writer.WriteLineAsync($"    <Exception><![CDATA[{log.Exception.Message}]]></Exception>");
                if (!string.IsNullOrEmpty(log.UserId))
                    await writer.WriteLineAsync($"    <UserId>{log.UserId}</UserId>");
                await writer.WriteLineAsync($"    <MachineName>{log.MachineName}</MachineName>");
                if (log.Duration.HasValue)
                    await writer.WriteLineAsync($"    <Duration>{log.Duration.Value.TotalMilliseconds}</Duration>");
                await writer.WriteLineAsync("  </Log>");
            }
            
            await writer.WriteLineAsync("</Logs>");
        }

        /// <summary>
        /// 刷新日志到持久存储
        /// </summary>
        private async Task FlushLogsAsync()
        {
            if (_logQueue.IsEmpty) return;
            
            try
            {
                var logsToFlush = new List<LogEntry>();
                while (_logQueue.TryDequeue(out var log) && logsToFlush.Count < 1000)
                {
                    logsToFlush.Add(log);
                }
                
                if (logsToFlush.Count > 0)
                {
                    await WriteLogsToFileAsync(logsToFlush);
                    _systemLogger.LogDebug("刷新日志到文件: {Count}条", logsToFlush.Count);
                }
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "刷新日志失败");
            }
        }

        /// <summary>
        /// 写入日志到文件
        /// </summary>
        private async Task WriteLogsToFileAsync(List<LogEntry> logs)
        {
            var fileName = $"logs_{DateTime.UtcNow:yyyyMMdd}.json";
            var filePath = Path.Combine(_logDirectory, fileName);
            
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            using var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            foreach (var log in logs)
            {
                var json = JsonSerializer.Serialize(log, jsonOptions);
                var bytes = Encoding.UTF8.GetBytes(json + Environment.NewLine);
                await fileStream.WriteAsync(bytes);
            }
        }

        /// <summary>
        /// 清理日志文件
        /// </summary>
        private async Task<int> CleanupLogFilesAsync(DateTime beforeDate, CancellationToken cancellationToken)
        {
            var cleanupCount = 0;
            
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "logs_*.json");
                
                foreach (var file in logFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.StartsWith("logs_") && fileName.Length == 13) // logs_yyyyMMdd
                    {
                        var dateStr = fileName.Substring(5);
                        if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var fileDate))
                        {
                            if (fileDate < beforeDate.Date)
                            {
                                File.Delete(file);
                                cleanupCount++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _systemLogger.LogError(ex, "清理日志文件失败");
            }
            
            return await Task.FromResult(cleanupCount);
        }

        /// <summary>
        /// 定时刷新回调
        /// </summary>
        private async void FlushLogsCallback(object? state)
        {
            await FlushLogsAsync();
        }

        /// <summary>
        /// 定时清理回调
        /// </summary>
        private async void CleanupCallback(object? state)
        {
            var cutoffDate = DateTime.UtcNow - _logRetentionPeriod;
            await CleanupLogsAsync(cutoffDate);
        }

        /// <summary>
        /// 内部完成性能跟踪
        /// </summary>
        internal async Task CompletePerformanceTracking(PerformanceTracker tracker, string? result = null)
        {
            _activeTrackers.TryRemove(tracker.Id, out _);
            
            await LogPerformanceAsync(tracker.Operation, tracker.Duration, tracker.GetMetrics(), tracker.GetContext());
            
            if (!string.IsNullOrEmpty(result))
            {
                await LogOperationAsync(tracker.Operation, result, tracker.GetContext(), tracker.Duration);
            }
        }

        #endregion

        #region IDisposable 实现

        public void Dispose()
        {
            // 最后一次刷新
            FlushLogsAsync().GetAwaiter().GetResult();
            
            _flushTimer?.Dispose();
            _cleanupTimer?.Dispose();
            
            // 清理活跃的性能跟踪器
            foreach (var tracker in _activeTrackers.Values)
            {
                tracker.Dispose();
            }
            _activeTrackers.Clear();
            
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}