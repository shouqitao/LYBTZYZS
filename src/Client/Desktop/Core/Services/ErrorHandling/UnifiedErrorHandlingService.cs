using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Prism.Events;
using LYBT.Shared.Models.Contracts.Common;
using SharedCommon = LYBT.Shared.Models.Contracts.Common.SharedCommon;

namespace LYBT.Desktop.Core.Services.ErrorHandling
{
    /// <summary>
    /// 统一错误处理服务 - 第3阶段质量优化
    /// 提供统一的错误处理和用户通知机制
    /// </summary>
    public class UnifiedErrorHandlingService : IErrorHandlingService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<UnifiedErrorHandlingService> _logger;
        private readonly ICustomDialogService _dialogService;

        public event EventHandler<SharedCommon.HandledError>? ErrorOccurred;
        public event EventHandler<SharedCommon.HandledError>? CriticalErrorOccurred;
        public ICustomDialogService? CustomDialogService => _dialogService;

        public UnifiedErrorHandlingService(
            IEventAggregator eventAggregator,
            ILogger<UnifiedErrorHandlingService> logger,
            ICustomDialogService dialogService)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        /// <summary>
        /// 处理错误
        /// </summary>
        public async Task HandleErrorAsync(Exception exception, string? context = null)
        {
            // 记录详细错误日志
            LogError(exception, context);

            // 获取用户友好的错误消息
            var userMessage = GetUserFriendlyMessage(exception);
            
            // 显示用户友好的错误消息
            await ShowUserFriendlyErrorAsync(userMessage, "操作失败");

            // 发布错误事件供其他组件处理
            PublishErrorEvent(exception, context, userMessage);

            // 特殊错误处理
            await HandleSpecialErrors(exception);
        }

        /// <summary>
        /// 显示用户友好的错误消息
        /// </summary>
        public async Task ShowUserFriendlyErrorAsync(string message, string? title = null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await _dialogService.ShowErrorAsync(message, title ?? "错误");
            });
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void LogError(Exception exception, string? context = null)
        {
            var logLevel = GetLogLevel(exception);
            var message = context != null 
                ? $"错误发生在: {context}" 
                : "发生未处理的错误";

            _logger.Log(logLevel, exception, message);

            // 记录额外的诊断信息
            if (exception.Data.Count > 0)
            {
                foreach (var key in exception.Data.Keys)
                {
                    _logger.LogDebug("错误数据 - {Key}: {Value}", key, exception.Data[key]);
                }
            }

            // 记录内部异常
            var innerException = exception.InnerException;
            var depth = 0;
            while (innerException != null && depth < 5)
            {
                _logger.LogError(innerException, "内部异常 #{Depth}", ++depth);
                innerException = innerException.InnerException;
            }
        }

        /// <summary>
        /// 注册全局异常处理器
        /// </summary>
        public void RegisterGlobalExceptionHandlers()
        {
            // 处理UI线程异常
            System.Windows.Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // 处理非UI线程异常
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // 处理Task异常
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _logger.LogInformation("全局异常处理器已注册");
        }

        /// <summary>
        /// 取消注册全局异常处理器
        /// </summary>
        public void UnregisterGlobalExceptionHandlers()
        {
            System.Windows.Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

            _logger.LogInformation("全局异常处理器已取消注册");
        }

        #region 私有方法

        /// <summary>
        /// 获取用户友好的错误消息
        /// </summary>
        public string GetUserFriendlyMessage(Exception exception, string? defaultMessage = null)
        {
            return exception switch
            {
                ValidationException => "输入的数据不符合要求，请检查后重试。",
                UnauthorizedAccessException => "您没有权限执行此操作。",
                TimeoutException => "操作超时，请检查网络连接后重试。",
                HttpRequestException => "网络连接异常，请稍后重试。",
                TaskCanceledException => "操作已被取消。",
                InvalidOperationException => "当前状态下无法执行此操作。",
                ArgumentException => "提供的参数不正确，请检查输入。",
                NotSupportedException => "此功能暂不支持。",
                OutOfMemoryException => "系统内存不足，请关闭其他程序后重试。",
                _ => defaultMessage ?? "系统遇到了一个问题，我们正在努力修复。如果问题持续，请联系技术支持。"
            };
        }

        /// <summary>
        /// 处理异常并返回处理后的错误信息
        /// </summary>
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

            LogError(exception, context?.Operation);
            ErrorOccurred?.Invoke(this, handledError);

            if (handledError.Severity == SharedCommon.ErrorSeverity.Critical)
            {
                CriticalErrorOccurred?.Invoke(this, handledError);
            }

            return handledError;
        }

        /// <summary>
        /// 异步处理异常
        /// </summary>
        public async Task<SharedCommon.HandledError> HandleExceptionAsync(Exception exception, ErrorContext? context = null)
        {
            var handledError = HandleException(exception, context);
            await ShowErrorAsync(handledError);
            return handledError;
        }

        /// <summary>
        /// 显示错误通知给用户
        /// </summary>
        public async Task ShowErrorAsync(SharedCommon.HandledError handledError, bool showDialog = true)
        {
            if (!showDialog) return;

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var title = handledError.Severity switch
                {
                    SharedCommon.ErrorSeverity.Warning => "警告",
                    SharedCommon.ErrorSeverity.Critical => "严重错误",
                    _ => "错误"
                };

                await _dialogService.ShowErrorAsync(handledError.UserMessage, title);
            });
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public async Task LogErrorAsync(SharedCommon.HandledError handledError)
        {
            await Task.Run(() =>
            {
                var logLevel = handledError.Severity switch
                {
                    SharedCommon.ErrorSeverity.Information => LogLevel.Information,
                    SharedCommon.ErrorSeverity.Warning => LogLevel.Warning,
                    SharedCommon.ErrorSeverity.Critical => LogLevel.Critical,
                    _ => LogLevel.Error
                };

                _logger.Log(logLevel, handledError.OriginalException, 
                    "错误ID: {ErrorId}, 消息: {Message}, 分类: {Category}", 
                    handledError.Id, handledError.UserMessage, handledError.Category);
            });
        }

        /// <summary>
        /// 检查异常是否可重试
        /// </summary>
        public bool CanRetry(Exception exception)
        {
            return exception is HttpRequestException or 
                   TimeoutException or 
                   TaskCanceledException;
        }

        /// <summary>
        /// 获取异常的错误分类
        /// </summary>
        public SharedCommon.ErrorCategory GetErrorCategory(Exception exception)
        {
            return exception switch
            {
                ValidationException => SharedCommon.ErrorCategory.Validation,
                UnauthorizedAccessException => SharedCommon.ErrorCategory.Authorization,
                HttpRequestException => SharedCommon.ErrorCategory.Network,
                TimeoutException => SharedCommon.ErrorCategory.Network,
                InvalidOperationException => SharedCommon.ErrorCategory.Business,
                ArgumentException => SharedCommon.ErrorCategory.Validation,
                NotSupportedException => SharedCommon.ErrorCategory.System,
                OutOfMemoryException => SharedCommon.ErrorCategory.System,
                _ => SharedCommon.ErrorCategory.Unknown
            };
        }

        /// <summary>
        /// 获取异常的严重程度
        /// </summary>
        public SharedCommon.ErrorSeverity GetErrorSeverity(Exception exception)
        {
            return exception switch
            {
                ValidationException => SharedCommon.ErrorSeverity.Warning,
                TaskCanceledException => SharedCommon.ErrorSeverity.Information,
                OutOfMemoryException => SharedCommon.ErrorSeverity.Critical,
                AccessViolationException => SharedCommon.ErrorSeverity.Critical,
                StackOverflowException => SharedCommon.ErrorSeverity.Critical,
                _ => SharedCommon.ErrorSeverity.Error
            };
        }

        /// <summary>
        /// 获取建议的恢复操作
        /// </summary>
        public string[] GetSuggestedActions(Exception exception)
        {
            return exception switch
            {
                ValidationException => new[] { "检查输入数据", "查看字段要求" },
                UnauthorizedAccessException => new[] { "重新登录", "检查权限设置" },
                HttpRequestException => new[] { "检查网络连接", "稍后重试" },
                TimeoutException => new[] { "检查网络速度", "减少数据量", "稍后重试" },
                OutOfMemoryException => new[] { "关闭其他程序", "重启应用程序" },
                _ => new[] { "重试操作", "联系技术支持" }
            };
        }

        /// <summary>
        /// 安全执行操作
        /// </summary>
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

        /// <summary>
        /// 安全执行操作并返回结果
        /// </summary>
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
        /// 获取日志级别
        /// </summary>
        private LogLevel GetLogLevel(Exception exception)
        {
            return exception switch
            {
                ValidationException => LogLevel.Warning,
                UnauthorizedAccessException => LogLevel.Warning,
                TaskCanceledException => LogLevel.Information,
                OutOfMemoryException => LogLevel.Critical,
                _ => LogLevel.Error
            };
        }

        /// <summary>
        /// 发布错误事件
        /// </summary>
        private void PublishErrorEvent(Exception exception, string? context, string userMessage)
        {
            try
            {
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(new ErrorOccurredEventArgs
                {
                    ErrorMessage = userMessage,
                    Exception = exception,
                    Source = context,
                    IsCritical = IsCriticalError(exception)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布错误事件失败");
            }
        }

        /// <summary>
        /// 判断是否为严重错误
        /// </summary>
        private bool IsCriticalError(Exception exception)
        {
            return exception is OutOfMemoryException or 
                   AccessViolationException or 
                   StackOverflowException;
        }

        /// <summary>
        /// 处理特殊错误
        /// </summary>
        private async Task HandleSpecialErrors(Exception exception)
        {
            switch (exception)
            {
                case UnauthorizedAccessException:
                    // 发布登出事件
                    _eventAggregator.GetEvent<LogoutEvent>().Publish(new LogoutEventArgs
                    {
                        Reason = "权限验证失败",
                        LogoutTime = DateTime.Now
                    });
                    break;

                case OutOfMemoryException:
                    // 尝试释放内存
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    
                    await ShowUserFriendlyErrorAsync(
                        "系统内存不足，建议保存工作并重启应用程序。", 
                        "内存警告");
                    break;
            }
        }

        #endregion

        #region 事件处理器

        private void OnDispatcherUnhandledException(object sender, 
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "UI线程未处理异常");
            
            Task.Run(async () => await HandleErrorAsync(e.Exception, "UI线程"));
            
            // 标记为已处理，防止应用崩溃
            e.Handled = !IsCriticalError(e.Exception);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                _logger.LogCritical(exception, "应用域未处理异常，终止状态: {IsTerminating}", e.IsTerminating);
                
                if (!e.IsTerminating)
                {
                    Task.Run(async () => await HandleErrorAsync(exception, "应用域"));
                }
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "Task未观察异常");
            
            Task.Run(async () => await HandleErrorAsync(e.Exception, "异步任务"));
            
            // 标记为已观察，防止进程终止
            e.SetObserved();
        }

        #endregion
    }

    /// <summary>
    /// 错误上下文信息
    /// </summary>
    // ErrorContext已移至LYBT.Shared.Models.Contracts.Common

    /// <summary>
    /// 错误恢复策略
    /// </summary>
    public interface IErrorRecoveryStrategy
    {
        bool CanRecover(Exception exception);
        Task<bool> TryRecoverAsync(Exception exception);
    }

    /// <summary>
    /// 网络错误恢复策略
    /// </summary>
    public class NetworkErrorRecoveryStrategy : IErrorRecoveryStrategy
    {
        private readonly ILogger<NetworkErrorRecoveryStrategy> _logger;

        public NetworkErrorRecoveryStrategy(ILogger<NetworkErrorRecoveryStrategy> logger)
        {
            _logger = logger;
        }

        public bool CanRecover(Exception exception)
        {
            return exception is HttpRequestException or TimeoutException;
        }

        public async Task<bool> TryRecoverAsync(Exception exception)
        {
            _logger.LogInformation("尝试恢复网络错误");
            
            // 等待一段时间后重试
            await Task.Delay(TimeSpan.FromSeconds(5));
            
            // 这里可以添加网络连接检查逻辑
            return true;
        }
    }
}