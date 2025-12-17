using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Presentation.Notifications
{
    /// <summary>
    /// 错误处理服务接口
    /// </summary>
    public interface IErrorHandlingService
    {
        SharedCommon.HandledError HandleException(Exception exception, ErrorContext? context = null);
        Task<SharedCommon.HandledError> HandleExceptionAsync(Exception exception, ErrorContext? context = null);
        Task ShowErrorAsync(SharedCommon.HandledError handledError, bool showDialog = true);
        Task LogErrorAsync(SharedCommon.HandledError handledError);
        string GetUserFriendlyMessage(Exception exception, string? defaultMessage = null);
        bool CanRetry(Exception exception);
        ErrorCategory GetErrorCategory(Exception exception);
        ErrorSeverity GetErrorSeverity(Exception exception);
        string[] GetSuggestedActions(Exception exception);
        Task<bool> ExecuteSafelyAsync(Func<Task> operation, ErrorContext? context = null, bool showErrorDialog = true);
        Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, ErrorContext? context = null, bool showErrorDialog = true);

        event EventHandler<SharedCommon.HandledError>? ErrorOccurred;
        event EventHandler<SharedCommon.HandledError>? CriticalErrorOccurred;

        /// <summary>
        /// 注册全局异常处理器
        /// </summary>
        void RegisterGlobalExceptionHandlers();
    }

    /// <summary>
    /// 简化的统一错误处理服务实现
    /// </summary>
    public class UnifiedErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<UnifiedErrorHandlingService> _logger;
        private readonly INotificationService _notificationService;

        public event EventHandler<SharedCommon.HandledError>? ErrorOccurred;
        public event EventHandler<SharedCommon.HandledError>? CriticalErrorOccurred;

        public UnifiedErrorHandlingService(
            ILogger<UnifiedErrorHandlingService> logger,
            INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <inheritdoc/>
        public SharedCommon.HandledError HandleException(Exception exception, ErrorContext? context = null)
        {
            var handledError = new SharedCommon.HandledError
            {
                UserMessage = GetUserFriendlyMessage(exception),
                TechnicalMessage = exception.Message,
                Category = GetErrorCategory(exception),
                Severity = GetErrorSeverity(exception),
                OriginalException = exception,
                Context = context,
                SuggestedActions = GetSuggestedActions(exception),
                CanRetry = CanRetry(exception)
            };

            // 记录日志
            var logLevel = GetLogLevel(exception);
            _logger.Log(logLevel, exception, "处理错误: {Message}, 上下文: {Context}",
                exception.Message, context?.Operation ?? "未知");

            // 触发事件
            ErrorOccurred?.Invoke(this, handledError);
            if (handledError.Severity == ErrorSeverity.Critical)
            {
                CriticalErrorOccurred?.Invoke(this, handledError);
            }

            return handledError;
        }

        /// <inheritdoc/>
        public async Task<SharedCommon.HandledError> HandleExceptionAsync(Exception exception, ErrorContext? context = null)
        {
            var handledError = HandleException(exception, context);
            await LogErrorAsync(handledError);
            return handledError;
        }

        /// <inheritdoc/>
        public async Task ShowErrorAsync(SharedCommon.HandledError handledError, bool showDialog = true)
        {
            if (!showDialog) return;

            var title = handledError.Severity switch
            {
                ErrorSeverity.Warning => "警告",
                ErrorSeverity.Critical => "严重错误",
                _ => "错误"
            };

            await _notificationService.ShowErrorAsync(handledError.UserMessage, title);
        }

        /// <inheritdoc/>
        public async Task LogErrorAsync(SharedCommon.HandledError handledError)
        {
            await Task.Run(() =>
            {
                var logLevel = handledError.Severity switch
                {
                    ErrorSeverity.Info => LogLevel.Information,
                    ErrorSeverity.Warning => LogLevel.Warning,
                    ErrorSeverity.Critical => LogLevel.Critical,
                    _ => LogLevel.Error
                };

                _logger.Log(logLevel, handledError.OriginalException,
                    "错误详情 - ID: {ErrorId}, 分类: {Category}, 消息: {Message}",
                    handledError.Id, handledError.Category, handledError.UserMessage);
            });
        }

        /// <inheritdoc/>
        public string GetUserFriendlyMessage(Exception exception, string? defaultMessage = null)
        {
            return exception switch
            {
                ValidationException => "输入的数据不符合要求，请检查后重试",
                UnauthorizedAccessException => "您没有权限执行此操作",
                TimeoutException => "操作超时，请检查网络连接后重试",
                HttpRequestException => "网络连接异常，请稍后重试",
                TaskCanceledException => "操作已被取消",
                InvalidOperationException => "当前状态下无法执行此操作",
                ArgumentException => "提供的参数不正确，请检查输入",
                NotSupportedException => "此功能暂不支持",
                OutOfMemoryException => "系统内存不足，请关闭其他程序后重试",
                _ => defaultMessage ?? "系统遇到了一个问题，请稍后重试或联系技术支持"
            };
        }

        /// <inheritdoc/>
        public bool CanRetry(Exception exception)
        {
            return exception is HttpRequestException or TimeoutException or TaskCanceledException;
        }

        /// <inheritdoc/>
        public ErrorCategory GetErrorCategory(Exception exception)
        {
            return exception switch
            {
                ValidationException => ErrorCategory.Validation,
                UnauthorizedAccessException => ErrorCategory.Authorization,
                HttpRequestException or TimeoutException => ErrorCategory.Network,
                InvalidOperationException => ErrorCategory.Business,
                ArgumentException => ErrorCategory.Validation,
                NotSupportedException or OutOfMemoryException => ErrorCategory.System,
                _ => ErrorCategory.Unknown
            };
        }

        /// <inheritdoc/>
        public ErrorSeverity GetErrorSeverity(Exception exception)
        {
            return exception switch
            {
                ValidationException => ErrorSeverity.Warning,
                TaskCanceledException => ErrorSeverity.Info,
                OutOfMemoryException or AccessViolationException or StackOverflowException => ErrorSeverity.Critical,
                _ => ErrorSeverity.Error
            };
        }

        /// <inheritdoc/>
        public string[] GetSuggestedActions(Exception exception)
        {
            return exception switch
            {
                ValidationException => new[] { "检查输入数据", "查看字段要求" },
                UnauthorizedAccessException => new[] { "重新登录", "检查权限设置" },
                HttpRequestException or TimeoutException => new[] { "检查网络连接", "稍后重试" },
                OutOfMemoryException => new[] { "关闭其他程序", "重启应用程序" },
                _ => new[] { "重试操作", "联系技术支持" }
            };
        }

        /// <inheritdoc/>
        public async Task<bool> ExecuteSafelyAsync(Func<Task> operation, ErrorContext? context = null, bool showErrorDialog = true)
        {
            try
            {
                await operation();
                return true;
            }
            catch (Exception ex)
            {
                var handledError = await HandleExceptionAsync(ex, context);
                if (showErrorDialog)
                {
                    await ShowErrorAsync(handledError);
                }
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, ErrorContext? context = null, bool showErrorDialog = true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                var handledError = await HandleExceptionAsync(ex, context);
                if (showErrorDialog)
                {
                    await ShowErrorAsync(handledError);
                }
                return default;
            }
        }

        /// <summary>
        /// 注册全局异常处理器
        /// </summary>
        public void RegisterGlobalExceptionHandlers()
        {
            try
            {
                _logger.LogInformation("注册全局异常处理器");

                // 注册应用程序域未处理异常
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                // 注册任务调度器未观察到的任务异常
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                _logger.LogInformation("全局异常处理器注册完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册全局异常处理器失败");
            }
        }

        /// <summary>
        /// 处理应用程序域未处理异常
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception exception)
                {
                    _logger.LogCritical(exception, "应用程序域未处理异常");
                    _ = Task.Run(async () => await HandleExceptionAsync(exception, null));
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "处理应用程序域未处理异常时发生错误");
            }
        }

        /// <summary>
        /// 处理未观察到的任务异常
        /// </summary>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                _logger.LogError(e.Exception, "未观察到的任务异常");
                _ = Task.Run(async () => await HandleExceptionAsync(e.Exception, null));
                e.SetObserved(); // 标记异常已被观察，防止应用程序崩溃
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "处理未观察任务异常时发生错误");
            }
        }

        private LogLevel GetLogLevel(Exception exception)
        {
            return exception switch
            {
                ValidationException or UnauthorizedAccessException => LogLevel.Warning,
                TaskCanceledException => LogLevel.Information,
                OutOfMemoryException or AccessViolationException => LogLevel.Critical,
                _ => LogLevel.Error
            };
        }
    }
}
