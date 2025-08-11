using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Exceptions;

namespace LYBT.Desktop.Core.Logging
{
    /// <summary>
    /// 结构化日志服务 - 提供丰富的上下文信息
    /// </summary>
    public class StructuredLoggingService : IStructuredLoggingService
    {
        private readonly ILogger<StructuredLoggingService> _logger;
        private readonly ILogContextProvider _contextProvider;
        private readonly string _logFilePath;
        private readonly object _fileLock = new();
        
        // 性能计数器
        private readonly Dictionary<string, Stopwatch> _performanceTimers = new();
        
        public StructuredLoggingService(
            ILogger<StructuredLoggingService> logger,
            ILogContextProvider contextProvider)
        {
            _logger = logger;
            _contextProvider = contextProvider;
            
            // 配置日志文件路径
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LYBT", "Logs");
            Directory.CreateDirectory(logDirectory);
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(logDirectory, $"lybt_{timestamp}.log");
        }
        
        #region 基础日志方法
        
        public void LogTrace(string message, params object[] args)
        {
            LogWithContext(LogLevel.Trace, message, args);
        }
        
        public void LogDebug(string message, params object[] args)
        {
            LogWithContext(LogLevel.Debug, message, args);
        }
        
        public void LogInformation(string message, params object[] args)
        {
            LogWithContext(LogLevel.Information, message, args);
        }
        
        public void LogWarning(string message, params object[] args)
        {
            LogWithContext(LogLevel.Warning, message, args);
        }
        
        public void LogError(Exception exception, string message, params object[] args)
        {
            LogExceptionWithContext(LogLevel.Error, exception, message, args);
        }
        
        public void LogCritical(Exception exception, string message, params object[] args)
        {
            LogExceptionWithContext(LogLevel.Critical, exception, message, args);
        }
        
        #endregion
        
        #region 结构化日志方法
        
        /// <summary>
        /// 记录操作日志
        /// </summary>
        public void LogOperation(string operationName, object parameters = null, 
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerFile = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var logEntry = new StructuredLogEntry
            {
                Level = LogLevel.Information,
                Category = "Operation",
                Message = $"执行操作: {operationName}",
                OperationName = operationName,
                Parameters = parameters,
                CallerInfo = new CallerInfo
                {
                    MemberName = callerName,
                    FilePath = callerFile,
                    LineNumber = callerLine
                }
            };
            
            WriteStructuredLog(logEntry);
        }
        
        /// <summary>
        /// 记录性能日志
        /// </summary>
        public IDisposable BeginPerformanceLog(string operationName)
        {
            var stopwatch = Stopwatch.StartNew();
            var key = $"{operationName}_{Guid.NewGuid()}";
            
            lock (_performanceTimers)
            {
                _performanceTimers[key] = stopwatch;
            }
            
            LogDebug($"开始性能监控: {operationName}");
            
            return new PerformanceLogger(this, key, operationName);
        }
        
        /// <summary>
        /// 记录审计日志
        /// </summary>
        public void LogAudit(string action, string entityType, object entityId, 
            object oldValue = null, object newValue = null)
        {
            var logEntry = new StructuredLogEntry
            {
                Level = LogLevel.Information,
                Category = "Audit",
                Message = $"审计: {action} - {entityType}",
                AuditInfo = new AuditInfo
                {
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId?.ToString(),
                    OldValue = oldValue,
                    NewValue = newValue,
                    UserId = _contextProvider.GetCurrentUserId(),
                    Timestamp = DateTime.Now
                }
            };
            
            WriteStructuredLog(logEntry);
        }
        
        /// <summary>
        /// 记录安全日志
        /// </summary>
        public void LogSecurity(string eventType, string description, 
            SecurityEventSeverity severity = SecurityEventSeverity.Medium)
        {
            var logEntry = new StructuredLogEntry
            {
                Level = severity switch
                {
                    SecurityEventSeverity.Low => LogLevel.Information,
                    SecurityEventSeverity.Medium => LogLevel.Warning,
                    SecurityEventSeverity.High => LogLevel.Error,
                    SecurityEventSeverity.Critical => LogLevel.Critical,
                    _ => LogLevel.Warning
                },
                Category = "Security",
                Message = $"安全事件: {eventType}",
                SecurityInfo = new SecurityInfo
                {
                    EventType = eventType,
                    Description = description,
                    Severity = severity,
                    IpAddress = _contextProvider.GetClientIpAddress(),
                    UserId = _contextProvider.GetCurrentUserId()
                }
            };
            
            WriteStructuredLog(logEntry);
        }
        
        /// <summary>
        /// 记录业务事件
        /// </summary>
        public void LogBusinessEvent(string eventName, object eventData = null)
        {
            var logEntry = new StructuredLogEntry
            {
                Level = LogLevel.Information,
                Category = "Business",
                Message = $"业务事件: {eventName}",
                BusinessEvent = new BusinessEventInfo
                {
                    EventName = eventName,
                    EventData = eventData,
                    Timestamp = DateTime.Now
                }
            };
            
            WriteStructuredLog(logEntry);
        }
        
        #endregion
        
        #region 私有方法
        
        private void LogWithContext(LogLevel level, string message, params object[] args)
        {
            var logEntry = CreateLogEntry(level, message, args);
            WriteStructuredLog(logEntry);
        }
        
        private void LogExceptionWithContext(LogLevel level, Exception exception, 
            string message, params object[] args)
        {
            var logEntry = CreateLogEntry(level, message, args);
            
            // 添加异常信息
            if (exception is AppException appEx)
            {
                logEntry.ExceptionInfo = new ExceptionInfo
                {
                    Type = exception.GetType().FullName,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    Category = appEx.Category.ToString(),
                    Severity = appEx.Severity.ToString(),
                    ErrorCode = appEx.ErrorCode,
                    CorrelationId = appEx.CorrelationId,
                    IsRetryable = appEx.IsRetryable,
                    RetryCount = appEx.RetryCount
                };
            }
            else
            {
                logEntry.ExceptionInfo = new ExceptionInfo
                {
                    Type = exception.GetType().FullName,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                };
            }
            
            WriteStructuredLog(logEntry);
        }
        
        private StructuredLogEntry CreateLogEntry(LogLevel level, string message, object[] args)
        {
            return new StructuredLogEntry
            {
                Level = level,
                Message = args.Length > 0 ? string.Format(message, args) : message,
                Context = _contextProvider.GetCurrentContext()
            };
        }
        
        private void WriteStructuredLog(StructuredLogEntry entry)
        {
            // 添加时间戳和上下文
            entry.Timestamp = DateTime.Now;
            entry.ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            entry.ProcessId = Process.GetCurrentProcess().Id;
            
            // 写入到ILogger
            _logger.Log(entry.Level, entry.Message);
            
            // 异步写入到文件
            Task.Run(() => WriteToFileAsync(entry));
        }
        
        private async Task WriteToFileAsync(StructuredLogEntry entry)
        {
            try
            {
                var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                lock (_fileLock)
                {
                    File.AppendAllTextAsync(_logFilePath, json + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // 日志写入失败，尝试写入事件日志
                try
                {
                    EventLog.WriteEntry("LYBT", $"日志写入失败: {ex.Message}", EventLogEntryType.Error);
                }
                catch
                {
                    // 忽略
                }
            }
        }
        
        internal void EndPerformanceLog(string key, string operationName)
        {
            Stopwatch stopwatch;
            lock (_performanceTimers)
            {
                if (_performanceTimers.TryGetValue(key, out stopwatch))
                {
                    _performanceTimers.Remove(key);
                }
            }
            
            if (stopwatch != null)
            {
                stopwatch.Stop();
                var logEntry = new StructuredLogEntry
                {
                    Level = LogLevel.Information,
                    Category = "Performance",
                    Message = $"性能监控完成: {operationName}",
                    PerformanceInfo = new PerformanceInfo
                    {
                        OperationName = operationName,
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                        ElapsedTicks = stopwatch.ElapsedTicks
                    }
                };
                
                WriteStructuredLog(logEntry);
            }
        }
        
        #endregion
        
        #region 内部类
        
        private class PerformanceLogger : IDisposable
        {
            private readonly StructuredLoggingService _service;
            private readonly string _key;
            private readonly string _operationName;
            
            public PerformanceLogger(StructuredLoggingService service, string key, string operationName)
            {
                _service = service;
                _key = key;
                _operationName = operationName;
            }
            
            public void Dispose()
            {
                _service.EndPerformanceLog(_key, _operationName);
            }
        }
        
        #endregion
    }
    
    #region 日志模型
    
    /// <summary>
    /// 结构化日志条目
    /// </summary>
    public class StructuredLogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public int ThreadId { get; set; }
        public int ProcessId { get; set; }
        
        // 上下文信息
        public LogContext Context { get; set; }
        public CallerInfo CallerInfo { get; set; }
        
        // 特定类型信息
        public string OperationName { get; set; }
        public object Parameters { get; set; }
        public ExceptionInfo ExceptionInfo { get; set; }
        public PerformanceInfo PerformanceInfo { get; set; }
        public AuditInfo AuditInfo { get; set; }
        public SecurityInfo SecurityInfo { get; set; }
        public BusinessEventInfo BusinessEvent { get; set; }
    }
    
    public class LogContext
    {
        public string CorrelationId { get; set; }
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string MachineName { get; set; }
        public string ApplicationVersion { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
    }
    
    public class CallerInfo
    {
        public string MemberName { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
    }
    
    public class ExceptionInfo
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
        public string ErrorCode { get; set; }
        public string CorrelationId { get; set; }
        public bool IsRetryable { get; set; }
        public int RetryCount { get; set; }
    }
    
    public class PerformanceInfo
    {
        public string OperationName { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public long ElapsedTicks { get; set; }
    }
    
    public class AuditInfo
    {
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class SecurityInfo
    {
        public string EventType { get; set; }
        public string Description { get; set; }
        public SecurityEventSeverity Severity { get; set; }
        public string IpAddress { get; set; }
        public string UserId { get; set; }
    }
    
    public class BusinessEventInfo
    {
        public string EventName { get; set; }
        public object EventData { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public enum SecurityEventSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    #endregion
}