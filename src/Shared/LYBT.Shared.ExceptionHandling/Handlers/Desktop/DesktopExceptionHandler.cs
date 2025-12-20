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
