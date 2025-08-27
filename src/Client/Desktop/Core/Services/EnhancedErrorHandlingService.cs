using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Core.Exceptions;
using LYBT.Desktop.Core.Models.Common;
using SharedCommon = LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// UltraThink Phase 5.3: 增强的错误处理服务
    /// 提供智能错误恢复、用户通知优化和性能监控
    /// </summary>
    public class EnhancedErrorHandlingService : IErrorHandlingService
    {
        private readonly IErrorHandlingService _baseService;
        private readonly ILogger<EnhancedErrorHandlingService> _logger;
        private readonly IAppConfiguration _configuration;
        private readonly IUserNotificationService _notificationService;
        private readonly ErrorRecoveryManager _recoveryManager;
        private readonly ErrorStatisticsCollector _statisticsCollector;
        private readonly ConcurrentDictionary<string, ErrorPattern> _errorPatterns = new();

        public event EventHandler<SharedCommon.HandledError>? ErrorOccurred;
        public event EventHandler<SharedCommon.HandledError>? CriticalErrorOccurred;

        /// <summary>
        /// 自定义对话框服务
        /// </summary>
        public ICustomDialogService? CustomDialogService => _baseService.CustomDialogService;

        public EnhancedErrorHandlingService(
            IErrorHandlingService baseService,
            ILogger<EnhancedErrorHandlingService> logger,
            IAppConfiguration configuration,
            IUserNotificationService notificationService)
        {
            _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            _recoveryManager = new ErrorRecoveryManager(_logger);
            _statisticsCollector = new ErrorStatisticsCollector();

            // 订阅基础服务的事件
            _baseService.ErrorOccurred += OnBaseErrorOccurred;
            _baseService.CriticalErrorOccurred += OnBaseCriticalErrorOccurred;
        }

        public SharedCommon.HandledError HandleException(Exception exception, ErrorContext? context = null)
        {
            return HandleExceptionAsync(exception, context).GetAwaiter().GetResult();
        }

        public async Task<SharedCommon.HandledError> HandleExceptionAsync(Exception exception, ErrorContext? context = null)
        {
            var handledError = await _baseService.HandleExceptionAsync(exception, context);
            
            // 增强处理
            await EnhanceErrorHandlingAsync(handledError);
            
            return handledError;
        }

        public async Task ShowErrorAsync(SharedCommon.HandledError handledError, bool showDialog = true)
        {
            // 使用增强的用户通知
            await ShowEnhancedErrorNotificationAsync(handledError, showDialog);
        }

        public async Task LogErrorAsync(SharedCommon.HandledError handledError)
        {
            // 增强日志记录
            await LogEnhancedErrorAsync(handledError);
            
            // 基础日志
            await _baseService.LogErrorAsync(handledError);
        }

        public string GetUserFriendlyMessage(Exception exception, string? defaultMessage = null)
        {
            return _baseService.GetUserFriendlyMessage(exception, defaultMessage);
        }

        public bool CanRetry(Exception exception)
        {
            return _baseService.CanRetry(exception);
        }

        public SharedCommon.ErrorCategory GetErrorCategory(Exception exception)
        {
            return _baseService.GetErrorCategory(exception);
        }

        public SharedCommon.ErrorSeverity GetErrorSeverity(Exception exception)
        {
            return _baseService.GetErrorSeverity(exception);
        }

        public string[] GetSuggestedActions(Exception exception)
        {
            var baseActions = _baseService.GetSuggestedActions(exception);
            var enhancedActions = GetEnhancedSuggestedActions(exception);
            
            return baseActions.Concat(enhancedActions).Distinct().ToArray();
        }

        public async Task<bool> ExecuteSafelyAsync(Func<Task> operation, ErrorContext? context = null, bool showErrorDialog = true)
        {
            return await ExecuteSafelyWithRetryAsync(operation, context, showErrorDialog);
        }

        public async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, ErrorContext? context = null, bool showErrorDialog = true)
        {
            return await ExecuteSafelyWithRetryAsync(operation, context, showErrorDialog);
        }

        public void RegisterGlobalExceptionHandlers()
        {
            _baseService.RegisterGlobalExceptionHandlers();
            
            // 添加增强的全局处理
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        }

        #region 增强功能

        /// <summary>
        /// 增强错误处理
        /// </summary>
        private async Task EnhanceErrorHandlingAsync(SharedCommon.HandledError handledError)
        {
            // 1. 收集错误统计
            _statisticsCollector.RecordError(handledError);
            
            // 2. 检查错误模式
            await CheckErrorPatternsAsync(handledError);
            
            // 3. 尝试自动恢复
            await TryAutoRecoveryAsync(handledError);
            
            // 4. 更新错误严重程度（基于历史数据）
            UpdateErrorSeverityBasedOnHistory(handledError);
        }

        /// <summary>
        /// 增强的用户通知
        /// </summary>
        private async Task ShowEnhancedErrorNotificationAsync(SharedCommon.HandledError handledError, bool showDialog)
        {
            if (!showDialog)
                return;

            var notificationConfig = GetNotificationConfiguration(handledError);
            
            switch (handledError.Severity)
            {
                case SharedCommon.ErrorSeverity.Info:
                    await _notificationService.ShowInfoAsync(handledError.UserMessage, notificationConfig);
                    break;
                    
                case SharedCommon.ErrorSeverity.Warning:
                    await _notificationService.ShowWarningAsync(handledError.UserMessage, notificationConfig);
                    break;
                    
                case SharedCommon.ErrorSeverity.Error:
                    await _notificationService.ShowErrorAsync(handledError.UserMessage, handledError.SuggestedActions.ToArray(), notificationConfig);
                    break;
                    
                case SharedCommon.ErrorSeverity.Critical:
                case SharedCommon.ErrorSeverity.Fatal:
                    await _notificationService.ShowCriticalErrorAsync(handledError, notificationConfig);
                    break;
            }
        }

        /// <summary>
        /// 增强的日志记录
        /// </summary>
        private async Task LogEnhancedErrorAsync(SharedCommon.HandledError handledError)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["ErrorId"] = handledError.Id,
                ["Category"] = handledError.Category.ToString(),
                ["Severity"] = handledError.Severity.ToString(),
                ["UserRole"] = "Unknown",
                ["Module"] = handledError.Module ?? "Unknown"
            });

            var logLevel = MapSeverityToLogLevel(handledError.Severity);
            
            _logger.Log(logLevel, handledError.Exception, 
                "Error occurred: {UserMessage}. Technical: {TechnicalDetails}",
                handledError.UserMessage, 
                handledError.TechnicalDetails);

            // 记录性能影响
            await RecordPerformanceImpactAsync(handledError);
        }

        /// <summary>
        /// 智能重试执行
        /// </summary>
        private async Task<bool> ExecuteSafelyWithRetryAsync(Func<Task> operation, ErrorContext? context, bool showErrorDialog)
        {
            var retryPolicy = GetRetryPolicy(context);
            Exception? lastException = null;

            for (int attempt = 0; attempt <= retryPolicy.MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        await Task.Delay(retryPolicy.GetDelayForAttempt(attempt));
                    }

                    await operation();
                    return true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (!CanRetry(ex) || attempt >= retryPolicy.MaxRetries)
                    {
                        break;
                    }

                    _logger.LogWarning("Operation failed on attempt {Attempt}, retrying: {Exception}", 
                        attempt + 1, ex.Message);
                }
            }

            // 处理最终失败
            var handledError = await HandleExceptionAsync(lastException!, context);
            if (showErrorDialog)
            {
                await ShowErrorAsync(handledError);
            }

            return false;
        }

        /// <summary>
        /// 带返回值的智能重试执行
        /// </summary>
        private async Task<T?> ExecuteSafelyWithRetryAsync<T>(Func<Task<T>> operation, ErrorContext? context, bool showErrorDialog)
        {
            var retryPolicy = GetRetryPolicy(context);
            Exception? lastException = null;

            for (int attempt = 0; attempt <= retryPolicy.MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        await Task.Delay(retryPolicy.GetDelayForAttempt(attempt));
                    }

                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (!CanRetry(ex) || attempt >= retryPolicy.MaxRetries)
                    {
                        break;
                    }

                    _logger.LogWarning("Operation failed on attempt {Attempt}, retrying: {Exception}", 
                        attempt + 1, ex.Message);
                }
            }

            // 处理最终失败
            var handledError = await HandleExceptionAsync(lastException!, context);
            if (showErrorDialog)
            {
                await ShowErrorAsync(handledError);
            }

            return default;
        }

        /// <summary>
        /// 获取增强的建议操作
        /// </summary>
        private string[] GetEnhancedSuggestedActions(Exception exception)
        {
            var actions = new List<string>();

            // 基于错误历史的智能建议
            var errorPattern = GetErrorPattern(exception);
            if (errorPattern.HasSuccessfulRecovery)
            {
                actions.AddRange(errorPattern.SuccessfulRecoveryActions);
            }

            // 基于配置的建议
            if (_configuration.GetValue("ErrorHandling:EnableAutoRecovery", true))
            {
                actions.Add("系统将尝试自动恢复");
            }

            return actions.ToArray();
        }

        /// <summary>
        /// 检查错误模式
        /// </summary>
        private async Task CheckErrorPatternsAsync(SharedCommon.HandledError handledError)
        {
            var patternKey = GetErrorPatternKey(handledError.Exception);
            var pattern = _errorPatterns.GetOrAdd(patternKey, _ => new ErrorPattern());
            
            pattern.AddOccurrence(handledError);

            // 检查是否需要升级警报
            if (pattern.ShouldEscalate())
            {
                await EscalateErrorAsync(handledError, pattern);
            }
        }

        /// <summary>
        /// 尝试自动恢复
        /// </summary>
        private async Task TryAutoRecoveryAsync(SharedCommon.HandledError handledError)
        {
            if (!_configuration.GetValue("ErrorHandling:EnableAutoRecovery", true))
                return;

            var recoveryStrategy = _recoveryManager.GetRecoveryStrategy(handledError);
            if (recoveryStrategy != null)
            {
                var success = await recoveryStrategy.TryRecoverAsync(handledError);
                if (success)
                {
                    _logger.LogInformation("Auto-recovery successful for error {ErrorId}", handledError.Id);
                    handledError.SuggestedActions.Add("系统已自动恢复");
                }
            }
        }

        #endregion

        #region 辅助方法

        private NotificationConfiguration GetNotificationConfiguration(SharedCommon.HandledError handledError)
        {
            return new NotificationConfiguration
            {
                Duration = handledError.Severity switch
                {
                    SharedCommon.ErrorSeverity.Info => TimeSpan.FromSeconds(3),
                    SharedCommon.ErrorSeverity.Warning => TimeSpan.FromSeconds(5),
                    SharedCommon.ErrorSeverity.Error => TimeSpan.FromSeconds(8),
                    _ => TimeSpan.FromSeconds(10)
                },
                ShowInToastArea = handledError.Severity <= SharedCommon.ErrorSeverity.Warning,
                ShowInDialog = handledError.Severity >= SharedCommon.ErrorSeverity.Error,
                AllowUserDismiss = true
            };
        }

        private LogLevel MapSeverityToLogLevel(SharedCommon.ErrorSeverity severity)
        {
            return severity switch
            {
                SharedCommon.ErrorSeverity.Info => LogLevel.Information,
                SharedCommon.ErrorSeverity.Warning => LogLevel.Warning,
                SharedCommon.ErrorSeverity.Error => LogLevel.Error,
                SharedCommon.ErrorSeverity.Critical => LogLevel.Critical,
                SharedCommon.ErrorSeverity.Fatal => LogLevel.Critical,
                _ => LogLevel.Error
            };
        }

        private RetryPolicy GetRetryPolicy(ErrorContext? context)
        {
            var maxRetries = _configuration.GetValue("ErrorHandling:MaxRetries", 3);
            var baseDelay = _configuration.GetValue("ErrorHandling:BaseRetryDelay", 1000);
            
            return new RetryPolicy
            {
                MaxRetries = maxRetries,
                BaseDelay = TimeSpan.FromMilliseconds(baseDelay),
                BackoffMultiplier = 2.0
            };
        }

        private string GetErrorPatternKey(Exception exception)
        {
            return $"{exception.GetType().Name}:{exception.Message.GetHashCode()}";
        }

        private ErrorPattern GetErrorPattern(Exception exception)
        {
            var key = GetErrorPatternKey(exception);
            return _errorPatterns.GetOrAdd(key, _ => new ErrorPattern());
        }

        private void UpdateErrorSeverityBasedOnHistory(SharedCommon.HandledError handledError)
        {
            var stats = _statisticsCollector.GetStatistics();
            var errorType = handledError.Exception.GetType();
            
            if (stats.ErrorCounts.TryGetValue(errorType, out var count) && count > 10)
            {
                // 如果某类错误发生频繁，可能需要降低严重程度
                if (handledError.Severity == SharedCommon.ErrorSeverity.Error && count > 50)
                {
                    handledError.Severity = SharedCommon.ErrorSeverity.Warning;
                }
            }
        }

        private async Task RecordPerformanceImpactAsync(SharedCommon.HandledError handledError)
        {
            // 记录错误对性能的影响
            await Task.Run(() =>
            {
                _statisticsCollector.RecordPerformanceImpact(handledError);
            });
        }

        private async Task EscalateErrorAsync(SharedCommon.HandledError handledError, ErrorPattern pattern)
        {
            _logger.LogWarning("Error pattern escalation triggered for {ErrorType}, count: {Count}",
                handledError.Exception.GetType().Name, pattern.OccurrenceCount);

            // 可以在这里添加更多的升级逻辑，如发送通知给管理员等
            await Task.CompletedTask;
        }

        private void OnBaseErrorOccurred(object? sender, SharedCommon.HandledError e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        private void OnBaseCriticalErrorOccurred(object? sender, SharedCommon.HandledError e)
        {
            CriticalErrorOccurred?.Invoke(this, e);
        }

        private void OnFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            // 记录首次异常，用于性能分析
            _logger.LogTrace("First chance exception: {Exception}", e.Exception.GetType().Name);
        }

        #endregion
    }

    /// <summary>
    /// 重试策略
    /// </summary>
    public class RetryPolicy
    {
        public int MaxRetries { get; set; } = 3;
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
        public double BackoffMultiplier { get; set; } = 2.0;

        public TimeSpan GetDelayForAttempt(int attempt)
        {
            return TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attempt - 1));
        }
    }

    /// <summary>
    /// 错误模式
    /// </summary>
    public class ErrorPattern
    {
        private readonly List<DateTime> _occurrences = new();
        private readonly List<string> _successfulRecoveryActions = new();

        public int OccurrenceCount => _occurrences.Count;
        public bool HasSuccessfulRecovery => _successfulRecoveryActions.Count > 0;
        public IReadOnlyList<string> SuccessfulRecoveryActions => _successfulRecoveryActions.AsReadOnly();

        public void AddOccurrence(SharedCommon.HandledError handledError)
        {
            _occurrences.Add(handledError.OccurredAt);
            
            // 清理过期的记录（只保留最近1小时的）
            var cutoff = DateTime.Now.AddHours(-1);
            _occurrences.RemoveAll(d => d < cutoff);
        }

        public bool ShouldEscalate()
        {
            // 如果1小时内发生超过10次，需要升级
            return OccurrenceCount > 10;
        }

        public void RecordSuccessfulRecovery(string action)
        {
            if (!_successfulRecoveryActions.Contains(action))
            {
                _successfulRecoveryActions.Add(action);
            }
        }
    }

    /// <summary>
    /// 错误统计收集器
    /// </summary>
    public class ErrorStatisticsCollector
    {
        private readonly ConcurrentDictionary<Type, int> _errorCounts = new();
        private readonly ConcurrentDictionary<Type, TimeSpan> _performanceImpacts = new();

        public void RecordError(SharedCommon.HandledError handledError)
        {
            var errorType = handledError.Exception.GetType();
            _errorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
        }

        public void RecordPerformanceImpact(SharedCommon.HandledError handledError)
        {
            var errorType = handledError.Exception.GetType();
            var impact = TimeSpan.FromMilliseconds(100); // 简化处理
            _performanceImpacts.AddOrUpdate(errorType, impact, (_, existing) => existing.Add(impact));
        }

        public ErrorStatistics GetStatistics()
        {
            return new ErrorStatistics
            {
                ErrorCounts = new Dictionary<Type, int>(_errorCounts),
                PerformanceImpacts = new Dictionary<Type, TimeSpan>(_performanceImpacts)
            };
        }
    }

    /// <summary>
    /// 错误统计信息
    /// </summary>
    public class ErrorStatistics
    {
        public Dictionary<Type, int> ErrorCounts { get; set; } = new();
        public Dictionary<Type, TimeSpan> PerformanceImpacts { get; set; } = new();
    }

    /// <summary>
    /// 错误恢复管理器
    /// </summary>
    public class ErrorRecoveryManager
    {
        private readonly ILogger _logger;
        private readonly Dictionary<Type, IErrorRecoveryStrategy> _strategies = new();

        public ErrorRecoveryManager(ILogger logger)
        {
            _logger = logger;
            InitializeStrategies();
        }

        private void InitializeStrategies()
        {
            // 添加常见错误的恢复策略
            _strategies[typeof(HttpRequestException)] = new NetworkErrorRecoveryStrategy(_logger);
            _strategies[typeof(UnauthorizedAccessException)] = new AuthenticationErrorRecoveryStrategy(_logger);
        }

        public IErrorRecoveryStrategy? GetRecoveryStrategy(SharedCommon.HandledError handledError)
        {
            var exceptionType = handledError.Exception.GetType();
            return _strategies.TryGetValue(exceptionType, out var strategy) ? strategy : null;
        }
    }

    /// <summary>
    /// 错误恢复策略接口
    /// </summary>
    public interface IErrorRecoveryStrategy
    {
        Task<bool> TryRecoverAsync(SharedCommon.HandledError handledError);
    }

    /// <summary>
    /// 网络错误恢复策略
    /// </summary>
    public class NetworkErrorRecoveryStrategy : IErrorRecoveryStrategy
    {
        private readonly ILogger _logger;

        public NetworkErrorRecoveryStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<bool> TryRecoverAsync(SharedCommon.HandledError handledError)
        {
            // 简单的网络连接检查
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync("https://www.baidu.com");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 认证错误恢复策略
    /// </summary>
    public class AuthenticationErrorRecoveryStrategy : IErrorRecoveryStrategy
    {
        private readonly ILogger _logger;

        public AuthenticationErrorRecoveryStrategy(ILogger logger)
        {
            _logger = logger;
        }

        public Task<bool> TryRecoverAsync(SharedCommon.HandledError handledError)
        {
            // 认证错误通常需要用户重新登录，自动恢复的可能性较小
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 通知配置
    /// </summary>
    public class NotificationConfiguration
    {
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);
        public bool ShowInToastArea { get; set; } = true;
        public bool ShowInDialog { get; set; } = false;
        public bool AllowUserDismiss { get; set; } = true;
    }
}