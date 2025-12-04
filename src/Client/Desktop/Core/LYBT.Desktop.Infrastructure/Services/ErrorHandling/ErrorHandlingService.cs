using LYBT.Desktop.Infrastructure.Http;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LYBT.Desktop.Infrastructure.Services.ErrorHandling
{
    /// <summary>
    /// 用户通知服务实现 - UltraThink架构
    /// Issue #840: 从 IErrorHandlingService 更新为 IUserNotificationService
    /// refactor-logging-system: 集成ProblemDetails解析和CorrelationId日志
    /// </summary>
    public class ErrorHandlingService : IUserNotificationService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly ICommonDialogService? _dialogService;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger, ICommonDialogService? dialogService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService;
        }

        /// <inheritdoc/>
        public async Task HandleExceptionAsync(Exception exception, string? context = null)
        {
            var correlationId = CorrelationIdContext.CurrentOrNew;
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogError(
                    exception,
                    "异常处理 - 上下文: {Context}, 类型: {ExceptionType}, CorrelationId: {CorrelationId}",
                    context ?? "未知上下文",
                    exception.GetType().Name,
                    correlationId);

                var errorMessage = GetUserFriendlyMessage(exception);
                if (_dialogService != null)
                {
                    await _dialogService.ShowErrorAsync(errorMessage, "系统错误");
                }
            }
        }

        /// <summary>
        /// 处理ProblemDetails响应
        /// refactor-logging-system: 新增方法，处理服务器返回的RFC 7807错误
        /// </summary>
        /// <param name="problemDetails">ProblemDetails响应</param>
        /// <param name="context">上下文信息</param>
        public async Task HandleProblemDetailsAsync(ProblemDetailsResponse problemDetails, string? context = null)
        {
            var correlationId = problemDetails.CorrelationId ?? CorrelationIdContext.Current;
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                // 根据错误类型选择日志级别
                if (problemDetails.IsServerError)
                {
                    _logger.LogError(
                        "服务器错误 - 状态: {StatusCode}, 错误码: {ErrorCode}, 标题: {Title}, 详情: {Detail}, CorrelationId: {CorrelationId}",
                        problemDetails.Status,
                        problemDetails.ErrorCode,
                        problemDetails.Title,
                        problemDetails.Detail,
                        correlationId);
                }
                else if (problemDetails.IsValidationError)
                {
                    _logger.LogWarning(
                        "验证错误 - 状态: {StatusCode}, 错误码: {ErrorCode}, 验证错误: {ValidationErrors}, CorrelationId: {CorrelationId}",
                        problemDetails.Status,
                        problemDetails.ErrorCode,
                        problemDetails.GetValidationErrorMessage(),
                        correlationId);
                }
                else
                {
                    _logger.LogWarning(
                        "业务错误 - 状态: {StatusCode}, 错误码: {ErrorCode}, 标题: {Title}, 详情: {Detail}, CorrelationId: {CorrelationId}",
                        problemDetails.Status,
                        problemDetails.ErrorCode,
                        problemDetails.Title,
                        problemDetails.Detail,
                        correlationId);
                }

                // 获取用户友好的错误消息
                var userMessage = problemDetails.IsValidationError
                    ? problemDetails.GetValidationErrorMessage() ?? problemDetails.GetUserMessage()
                    : problemDetails.GetUserMessage();

                if (_dialogService != null)
                {
                    await _dialogService.ShowErrorAsync(userMessage, problemDetails.Title ?? "错误");
                }
            }
        }

        /// <inheritdoc/>
        public async Task ShowErrorAsync(string message, string? title = null)
        {
            _logger.LogError("错误消息: {Message}", message);

            if (_dialogService != null)
            {
                await _dialogService.ShowErrorAsync(message, title ?? "错误");
            }
        }

        /// <inheritdoc/>
        public async Task ShowSuccessAsync(string message, string? title = null)
        {
            _logger.LogInformation("成功消息: {Message}", message);

            if (_dialogService != null)
            {
                await _dialogService.ShowInfoAsync(message, title ?? "成功");
            }
        }

        /// <inheritdoc/>
        public async Task ShowWarningAsync(string message, string? title = null)
        {
            _logger.LogWarning("警告消息: {Message}", message);

            if (_dialogService != null)
            {
                await _dialogService.ShowWarningAsync(message, title ?? "警告");
            }
        }

        /// <inheritdoc/>
        public async Task ShowInfoAsync(string message, string? title = null)
        {
            _logger.LogInformation("信息消息: {Message}", message);

            if (_dialogService != null)
            {
                await _dialogService.ShowInfoAsync(message, title ?? "信息");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ShowConfirmAsync(string message, string? title = null)
        {
            _logger.LogInformation("确认对话框: {Message}", message);

            if (_dialogService != null)
            {
                return await _dialogService.ShowConfirmAsync(message, title ?? "确认");
            }

            return false;
        }

        /// <inheritdoc/>
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
        /// refactor-logging-system: 增强日志，添加CorrelationId
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception exception)
                {
                    var correlationId = CorrelationIdContext.CurrentOrNew;
                    using (LogContext.PushProperty("CorrelationId", correlationId))
                    {
                        _logger.LogCritical(
                            exception,
                            "应用程序域未处理异常 - 类型: {ExceptionType}, 是否终止: {IsTerminating}, CorrelationId: {CorrelationId}",
                            exception.GetType().FullName,
                            e.IsTerminating,
                            correlationId);
                        _ = Task.Run(async () => await HandleExceptionAsync(exception, "AppDomain未处理异常"));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "处理应用程序域未处理异常时发生错误");
            }
        }

        /// <summary>
        /// 处理未观察到的任务异常
        /// refactor-logging-system: 增强日志，添加CorrelationId
        /// </summary>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                var correlationId = CorrelationIdContext.CurrentOrNew;
                using (LogContext.PushProperty("CorrelationId", correlationId))
                {
                    _logger.LogError(
                        e.Exception,
                        "未观察到的任务异常 - 类型: {ExceptionType}, 内部异常数: {InnerExceptionCount}, CorrelationId: {CorrelationId}",
                        e.Exception?.GetType().FullName,
                        e.Exception?.InnerExceptions?.Count ?? 0,
                        correlationId);
                    _ = Task.Run(async () => await HandleExceptionAsync(e.Exception!, "TaskScheduler未观察异常"));
                    e.SetObserved(); // 标记异常已被观察，防止应用程序崩溃
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "处理未观察任务异常时发生错误");
            }
        }

        private static string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "参数不能为空",
                ArgumentException => "参数值无效",
                InvalidOperationException => "当前操作无效",
                UnauthorizedAccessException => "访问被拒绝",
                TimeoutException => "操作超时",
                _ => "发生了未知错误，请稍后重试"
            };
        }
    }
}
