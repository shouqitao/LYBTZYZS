using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Enrichers;

namespace LYBT.Desktop.Core.Services.Monitoring
{
    /// <summary>
    /// 结构化日志服务 - UltraThink Stage 5.3.1 核心组件
    /// 
    /// 功能特性：
    /// 1. 结构化日志记录
    /// 2. 上下文自动注入
    /// 3. 智能日志级别
    /// 4. 性能优化
    /// 5. 敏感信息脱敏
    /// </summary>
    public interface IStructuredLoggingService
    {
        /// <summary>
        /// 记录业务操作
        /// </summary>
        void LogBusinessOperation(string operation, object? data = null, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0);

        /// <summary>
        /// 记录性能指标
        /// </summary>
        void LogPerformanceMetric(string metricName, double value, Dictionary<string, object>? tags = null);

        /// <summary>
        /// 记录安全事件
        /// </summary>
        void LogSecurityEvent(string eventType, string details, SecurityEventLevel level = SecurityEventLevel.Info);

        /// <summary>
        /// 记录错误详情
        /// </summary>
        void LogError(Exception exception, string context, Dictionary<string, object>? additionalData = null);

        /// <summary>
        /// 创建操作范围
        /// </summary>
        IDisposable BeginOperationScope(string operationName, Dictionary<string, object>? properties = null);

        /// <summary>
        /// 设置用户上下文
        /// </summary>
        void SetUserContext(string userId, string userName, string role);

        /// <summary>
        /// 调整日志级别
        /// </summary>
        void AdjustLogLevel(LogEventLevel level);

        /// <summary>
        /// 获取日志统计
        /// </summary>
        LoggingStatistics GetStatistics();
    }

    /// <summary>
    /// 结构化日志服务实现
    /// </summary>
    public class StructuredLoggingService : IStructuredLoggingService, IDisposable
    {
        #region 私有字段

        private readonly Logger _logger;
        private readonly LoggingLevelSwitch _levelSwitch;
        private readonly ILogger<StructuredLoggingService> _microsoftLogger;
        
        // 统计信息
        private long _totalLogCount = 0;
        private long _errorCount = 0;
        private long _warningCount = 0;
        private long _performanceLogCount = 0;
        private long _businessLogCount = 0;
        private long _securityLogCount = 0;
        
        // 用户上下文
        private string? _currentUserId;
        private string? _currentUserName;
        private string? _currentUserRole;
        
        // 智能级别调整
        private readonly Timer _levelAdjustmentTimer;
        private DateTime _lastHighLoadTime = DateTime.MinValue;

        #endregion

        #region 构造函数

        public StructuredLoggingService(ILogger<StructuredLoggingService> microsoftLogger)
        {
            _microsoftLogger = microsoftLogger;
            _levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            
            // 配置Serilog
            _logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_levelSwitch)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithEnvironmentUserName()
                // .Enrich.WithMachineName() - 移除冲突的扩展方法调用
                // .Enrich.WithThreadId() - 移除不可用的扩展方法调用
                .Enrich.WithProperty("Application", "LYBT中医诊所系统")
                .Enrich.WithProperty("Version", GetAssemblyVersion())
                .WriteTo.Debug(outputTemplate: GetOutputTemplate())
                .WriteTo.File(
                    path: GetLogFilePath(),
                    outputTemplate: GetOutputTemplate(),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 104857600, // 100MB
                    rollOnFileSizeLimit: true,
                    shared: true)
                .CreateLogger();
            
            // 启动智能级别调整定时器
            _levelAdjustmentTimer = new Timer(
                AdjustLogLevelBasedOnLoad,
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1));
            
            _logger.Information("结构化日志服务已初始化");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 记录业务操作
        /// </summary>
        public void LogBusinessOperation(string operation, object? data = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Interlocked.Increment(ref _businessLogCount);
            Interlocked.Increment(ref _totalLogCount);
            
            using (LogContext.PushProperty("OperationType", "Business"))
            using (LogContext.PushProperty("Operation", operation))
            using (LogContext.PushProperty("CallerMember", memberName))
            using (LogContext.PushProperty("CallerFile", Path.GetFileName(filePath)))
            using (LogContext.PushProperty("CallerLine", lineNumber))
            using (PushUserContext())
            {
                if (data != null)
                {
                    var sanitizedData = SanitizeData(data);
                    _logger.Information("业务操作: {Operation} {@Data}", operation, sanitizedData);
                }
                else
                {
                    _logger.Information("业务操作: {Operation}", operation);
                }
            }
        }

        /// <summary>
        /// 记录性能指标
        /// </summary>
        public void LogPerformanceMetric(string metricName, double value, Dictionary<string, object>? tags = null)
        {
            Interlocked.Increment(ref _performanceLogCount);
            Interlocked.Increment(ref _totalLogCount);
            
            using (LogContext.PushProperty("MetricType", "Performance"))
            using (LogContext.PushProperty("MetricName", metricName))
            using (LogContext.PushProperty("MetricValue", value))
            using (PushUserContext())
            {
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        LogContext.PushProperty(tag.Key, tag.Value);
                    }
                }
                
                _logger.Information("性能指标: {MetricName} = {Value:F2}", metricName, value);
            }
        }

        /// <summary>
        /// 记录安全事件
        /// </summary>
        public void LogSecurityEvent(string eventType, string details, SecurityEventLevel level = SecurityEventLevel.Info)
        {
            Interlocked.Increment(ref _securityLogCount);
            Interlocked.Increment(ref _totalLogCount);
            
            using (LogContext.PushProperty("EventType", "Security"))
            using (LogContext.PushProperty("SecurityEventType", eventType))
            using (LogContext.PushProperty("SecurityLevel", level))
            using (PushUserContext())
            {
                var logLevel = level switch
                {
                    SecurityEventLevel.Critical => LogEventLevel.Fatal,
                    SecurityEventLevel.Warning => LogEventLevel.Warning,
                    SecurityEventLevel.Info => LogEventLevel.Information,
                    _ => LogEventLevel.Information
                };
                
                _logger.Write(logLevel, "安全事件: {EventType} - {Details}", eventType, details);
            }
        }

        /// <summary>
        /// 记录错误详情
        /// </summary>
        public void LogError(Exception exception, string context, Dictionary<string, object>? additionalData = null)
        {
            Interlocked.Increment(ref _errorCount);
            Interlocked.Increment(ref _totalLogCount);
            
            using (LogContext.PushProperty("ErrorContext", context))
            using (LogContext.PushProperty("ExceptionType", exception.GetType().Name))
            using (LogContext.PushProperty("ExceptionMessage", exception.Message))
            using (PushUserContext())
            {
                if (additionalData != null)
                {
                    foreach (var data in additionalData)
                    {
                        LogContext.PushProperty($"Error_{data.Key}", data.Value);
                    }
                }
                
                _logger.Error(exception, "错误发生在: {Context}", context);
            }
            
            // 同时记录到Microsoft.Extensions.Logging
            _microsoftLogger.LogError(exception, "错误发生在: {Context}", context);
        }

        /// <summary>
        /// 创建操作范围
        /// </summary>
        public IDisposable BeginOperationScope(string operationName, Dictionary<string, object>? properties = null)
        {
            var disposables = new List<IDisposable>
            {
                LogContext.PushProperty("OperationName", operationName),
                LogContext.PushProperty("OperationId", Guid.NewGuid()),
                LogContext.PushProperty("OperationStartTime", DateTime.Now)
            };
            
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    disposables.Add(LogContext.PushProperty(prop.Key, prop.Value));
                }
            }
            
            disposables.Add(PushUserContext());
            
            _logger.Information("开始操作: {OperationName}", operationName);
            
            return new OperationScope(operationName, disposables, this);
        }

        /// <summary>
        /// 设置用户上下文
        /// </summary>
        public void SetUserContext(string userId, string userName, string role)
        {
            _currentUserId = userId;
            _currentUserName = userName;
            _currentUserRole = role;
            
            _logger.Information("用户上下文已设置: {UserName} ({UserId}) - {Role}", userName, userId, role);
        }

        /// <summary>
        /// 调整日志级别
        /// </summary>
        public void AdjustLogLevel(LogEventLevel level)
        {
            _levelSwitch.MinimumLevel = level;
            _logger.Information("日志级别已调整为: {Level}", level);
        }

        /// <summary>
        /// 获取日志统计
        /// </summary>
        public LoggingStatistics GetStatistics()
        {
            return new LoggingStatistics
            {
                TotalLogCount = _totalLogCount,
                ErrorCount = _errorCount,
                WarningCount = _warningCount,
                PerformanceLogCount = _performanceLogCount,
                BusinessLogCount = _businessLogCount,
                SecurityLogCount = _securityLogCount,
                CurrentLogLevel = _levelSwitch.MinimumLevel.ToString(),
                LogFilePath = GetLogFilePath(),
                LastHighLoadTime = _lastHighLoadTime
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 推送用户上下文
        /// </summary>
        private IDisposable PushUserContext()
        {
            var disposables = new List<IDisposable>();
            
            if (!string.IsNullOrEmpty(_currentUserId))
            {
                disposables.Add(LogContext.PushProperty("UserId", _currentUserId));
                disposables.Add(LogContext.PushProperty("UserName", _currentUserName));
                disposables.Add(LogContext.PushProperty("UserRole", _currentUserRole));
            }
            
            return new CompositeDisposable(disposables);
        }

        /// <summary>
        /// 数据脱敏
        /// </summary>
        private object SanitizeData(object data)
        {
            // 这里应该实现实际的数据脱敏逻辑
            // 例如：移除密码、身份证号、电话号码等敏感信息
            
            if (data is Dictionary<string, object> dict)
            {
                var sanitized = new Dictionary<string, object>();
                foreach (var kvp in dict)
                {
                    if (IsSensitiveField(kvp.Key))
                    {
                        sanitized[kvp.Key] = "***已脱敏***";
                    }
                    else
                    {
                        sanitized[kvp.Key] = kvp.Value;
                    }
                }
                return sanitized;
            }
            
            return data;
        }

        /// <summary>
        /// 判断是否为敏感字段
        /// </summary>
        private bool IsSensitiveField(string fieldName)
        {
            var sensitiveFields = new[] 
            { 
                "password", "pwd", "secret", "token", "key", 
                "身份证", "电话", "手机", "银行卡", "密码",
                "idcard", "phone", "mobile", "bankcard"
            };
            
            var lowerFieldName = fieldName.ToLower();
            return Array.Exists(sensitiveFields, field => lowerFieldName.Contains(field));
        }

        /// <summary>
        /// 基于负载调整日志级别
        /// </summary>
        private void AdjustLogLevelBasedOnLoad(object? state)
        {
            try
            {
                // 获取系统负载（简化实现）
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var memoryUsageMB = process.WorkingSet64 / (1024.0 * 1024.0);
                
                // 高负载时降低日志级别
                if (memoryUsageMB > 500)
                {
                    if (_levelSwitch.MinimumLevel < LogEventLevel.Warning)
                    {
                        _levelSwitch.MinimumLevel = LogEventLevel.Warning;
                        _lastHighLoadTime = DateTime.Now;
                        _logger.Warning("系统负载较高，自动调整日志级别为Warning");
                    }
                }
                // 低负载时恢复日志级别
                else if (memoryUsageMB < 300)
                {
                    if (_levelSwitch.MinimumLevel > LogEventLevel.Information)
                    {
                        _levelSwitch.MinimumLevel = LogEventLevel.Information;
                        _logger.Information("系统负载正常，恢复日志级别为Information");
                    }
                }
            }
            catch (Exception ex)
            {
                _microsoftLogger.LogError(ex, "自动调整日志级别时发生错误");
            }
        }

        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        private string GetLogFilePath()
        {
            var basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LYBT",
                "Logs");
            
            Directory.CreateDirectory(basePath);
            
            return Path.Combine(basePath, "lybt-.log");
        }

        /// <summary>
        /// 获取输出模板
        /// </summary>
        private string GetOutputTemplate()
        {
            return "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj} " +
                   "{Properties:j}{NewLine}{Exception}";
        }

        /// <summary>
        /// 获取程序集版本
        /// </summary>
        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString() ?? "1.0.0";
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _levelAdjustmentTimer?.Dispose();
            _logger?.Dispose();
            
            var stats = GetStatistics();
            _microsoftLogger.LogInformation(
                "结构化日志服务已释放 - 总日志: {Total}, 错误: {Errors}, 业务: {Business}, 性能: {Performance}",
                stats.TotalLogCount, stats.ErrorCount, stats.BusinessLogCount, stats.PerformanceLogCount);
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 操作范围
        /// </summary>
        private class OperationScope : IDisposable
        {
            private readonly string _operationName;
            private readonly List<IDisposable> _disposables;
            private readonly StructuredLoggingService _loggingService;
            private readonly DateTime _startTime;

            public OperationScope(string operationName, List<IDisposable> disposables, StructuredLoggingService loggingService)
            {
                _operationName = operationName;
                _disposables = disposables;
                _loggingService = loggingService;
                _startTime = DateTime.Now;
            }

            public void Dispose()
            {
                var duration = DateTime.Now - _startTime;
                _loggingService._logger.Information(
                    "完成操作: {OperationName} - 耗时: {Duration:F2}ms",
                    _operationName, duration.TotalMilliseconds);
                
                foreach (var disposable in _disposables)
                {
                    disposable?.Dispose();
                }
            }
        }

        /// <summary>
        /// 组合Disposable
        /// </summary>
        private class CompositeDisposable : IDisposable
        {
            private readonly List<IDisposable> _disposables;

            public CompositeDisposable(List<IDisposable> disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                foreach (var disposable in _disposables)
                {
                    disposable?.Dispose();
                }
            }
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 安全事件级别
    /// </summary>
    public enum SecurityEventLevel
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// 日志统计
    /// </summary>
    public class LoggingStatistics
    {
        public long TotalLogCount { get; set; }
        public long ErrorCount { get; set; }
        public long WarningCount { get; set; }
        public long PerformanceLogCount { get; set; }
        public long BusinessLogCount { get; set; }
        public long SecurityLogCount { get; set; }
        public string CurrentLogLevel { get; set; } = string.Empty;
        public string LogFilePath { get; set; } = string.Empty;
        public DateTime LastHighLoadTime { get; set; }
    }

    #endregion
}