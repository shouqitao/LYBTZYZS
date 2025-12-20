using System.Net.Sockets;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.ExceptionHandling.Handlers;

/// <summary>
/// Desktop端异常处理器实现
/// consolidate-exception-handling: 从LYBT.Desktop.Foundation迁移
/// </summary>
public class DesktopExceptionHandler : IDesktopExceptionHandler
{
    private readonly ILogger<DesktopExceptionHandler> _logger;

    public DesktopExceptionHandler(ILogger<DesktopExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void HandleException(Exception exception, string? context = null)
    {
        LogExceptionInternal(exception, context ?? "Unknown", null);
    }

    /// <inheritdoc/>
    public Task HandleExceptionAsync(Exception exception, string? context = null)
    {
        HandleException(exception, context);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void LogException(Exception exception, ExceptionSeverity severity = ExceptionSeverity.Error)
    {
        var logLevel = severity switch
        {
            ExceptionSeverity.Information => LogLevel.Information,
            ExceptionSeverity.Warning => LogLevel.Warning,
            ExceptionSeverity.Error => LogLevel.Error,
            ExceptionSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Error
        };

        _logger.Log(logLevel, exception, "异常发生 - 类型: {ExceptionType}", exception.GetType().Name);
    }

    /// <inheritdoc/>
    public string GetUserFriendlyMessage(Exception exception)
    {
        return ExceptionMessageMapper.GetUserFriendlyMessage(exception);
    }

    /// <inheritdoc/>
    public bool CanRetry(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            HttpRequestException => true,
            TaskCanceledException => true,
            SocketException => true,
            _ => false
        };
    }

    #region 全局异常处理

    private bool _isRegistered;

    /// <inheritdoc/>
    public void RegisterGlobalExceptionHandlers()
    {
        if (_isRegistered) return;

        try
        {
            _logger.LogInformation("注册全局异常处理器");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _isRegistered = true;
            _logger.LogInformation("全局异常处理器注册完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册全局异常处理器失败");
        }
    }

    /// <inheritdoc/>
    public void UnregisterGlobalExceptionHandlers()
    {
        if (!_isRegistered) return;

        try
        {
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

            _isRegistered = false;
            _logger.LogInformation("全局异常处理器已注销");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注销全局异常处理器失败");
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
            {
                _logger.LogCritical(
                    exception,
                    "应用程序域未处理异常 - 类型: {ExceptionType}, 是否终止: {IsTerminating}",
                    exception.GetType().FullName,
                    e.IsTerminating);

                _ = Task.Run(() => HandleExceptionAsync(exception, "AppDomain未处理异常"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "处理应用程序域未处理异常时发生错误");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            _logger.LogError(
                e.Exception,
                "未观察到的任务异常 - 类型: {ExceptionType}, 内部异常数: {InnerExceptionCount}",
                e.Exception?.GetType().FullName,
                e.Exception?.InnerExceptions?.Count ?? 0);

            _ = Task.Run(() => HandleExceptionAsync(e.Exception!, "TaskScheduler未观察异常"));
            e.SetObserved();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "处理未观察任务异常时发生错误");
        }
    }

    #endregion

    private void LogExceptionInternal(Exception exception, string methodName, string? context)
    {
        var logLevel = DetermineLogLevel(exception);
        var message = "服务方法执行异常 - 方法: {MethodName}, 上下文: {Context}, 异常: {ExceptionType}";

        switch (logLevel)
        {
            case LogLevel.Error:
                _logger.LogError(exception, message, methodName, context ?? "无", exception.GetType().Name);
                break;
            case LogLevel.Warning:
                _logger.LogWarning(exception, message, methodName, context ?? "无", exception.GetType().Name);
                break;
            default:
                _logger.LogInformation(exception, message, methodName, context ?? "无", exception.GetType().Name);
                break;
        }
    }

    private static LogLevel DetermineLogLevel(Exception exception)
    {
        return exception switch
        {
            OutOfMemoryException => LogLevel.Error,
            UnauthorizedAccessException => LogLevel.Error,
            ArgumentNullException => LogLevel.Warning,
            ArgumentException => LogLevel.Warning,
            InvalidOperationException => LogLevel.Warning,
            HttpRequestException => LogLevel.Information,
            TimeoutException => LogLevel.Information,
            _ => LogLevel.Error
        };
    }

    #region ServiceResult支持（从IExceptionHandler合并）

    /// <inheritdoc/>
    public ServiceResult<T> HandleException<T>(Exception exception, string methodName, string? context = null)
    {
        LogExceptionInternal(exception, methodName, context);
        var userMessage = GetUserFriendlyMessage(exception);

        if (!string.IsNullOrWhiteSpace(context))
            userMessage = $"{context}: {userMessage}";

        return ServiceResult<T>.Failure(userMessage);
    }

    /// <inheritdoc/>
    public ServiceResult HandleExceptionWithResult(Exception exception, string methodName, string? context = null)
    {
        LogExceptionInternal(exception, methodName, context);
        var userMessage = GetUserFriendlyMessage(exception);

        if (!string.IsNullOrWhiteSpace(context))
            userMessage = $"{context}: {userMessage}";

        return ServiceResult.Failure(userMessage);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<ServiceResult<T>>> operation, string methodName, string? context = null)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex, methodName, context);
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> SafeExecuteAsync(Func<Task<ServiceResult>> operation, string methodName, string? context = null)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            return HandleExceptionWithResult(ex, methodName, context);
        }
    }

    #endregion
}
