using System.Net.Http;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Exceptions;

/// <summary>
/// 标准异常处理器实现 - DT-006技术债务修复
/// 提供统一的异常处理逻辑和用户友好的错误消息
/// </summary>
public class StandardExceptionHandler : IExceptionHandler
{
    private readonly ILogger<StandardExceptionHandler> _logger;

    public StandardExceptionHandler(ILogger<StandardExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理异常并返回带数据类型的ServiceResult
    /// </summary>
    public ServiceResult<T> HandleException<T>(Exception exception, string methodName, string? context = null)
    {
        // 1. 记录详细的错误日志用于技术排查
        LogException(exception, methodName, context);

        // 2. 获取用户友好的错误消息
        var userMessage = ExceptionMessageMapper.GetUserFriendlyMessage(exception);

        // 3. 添加上下文信息到错误消息
        if (!string.IsNullOrWhiteSpace(context))
        {
            userMessage = $"{context}: {userMessage}";
        }

        // 4. 返回统一格式的失败结果
        return ServiceResult<T>.Failure(userMessage);
    }

    /// <summary>
    /// 处理异常并返回无数据的ServiceResult
    /// </summary>
    public ServiceResult HandleException(Exception exception, string methodName, string? context = null)
    {
        // 1. 记录详细的错误日志用于技术排查
        LogException(exception, methodName, context);

        // 2. 获取用户友好的错误消息
        var userMessage = ExceptionMessageMapper.GetUserFriendlyMessage(exception);

        // 3. 添加上下文信息到错误消息
        if (!string.IsNullOrWhiteSpace(context))
        {
            userMessage = $"{context}: {userMessage}";
        }

        // 4. 返回统一格式的失败结果
        return ServiceResult.Failure(userMessage);
    }

    /// <summary>
    /// 安全执行带数据类型的操作，自动处理异常
    /// </summary>
    public async Task<ServiceResult<T>> HandleException<T>(Func<Task<ServiceResult<T>>> operation, string methodName, string? context = null)
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

    /// <summary>
    /// 安全执行无数据的操作，自动处理异常
    /// </summary>
    public async Task<ServiceResult> HandleException(Func<Task<ServiceResult>> operation, string methodName, string? context = null)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            return HandleException(ex, methodName, context);
        }
    }

    /// <summary>
    /// 安全执行支持取消令牌的操作，自动处理异常 - DT-011取消令牌支持
    /// </summary>
    public async Task<ServiceResult<T>> HandleException<T>(Func<CancellationToken, Task<ServiceResult<T>>> operation, string methodName, string? context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查操作是否已被取消
            cancellationToken.ThrowIfCancellationRequested();

            return await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 操作被取消，返回专门的取消消息
            var cancelMessage = !string.IsNullOrWhiteSpace(context)
                ? $"{context}: 操作已被用户取消"
                : "操作已被用户取消";

            _logger.LogInformation("操作被取消 - 方法: {MethodName}, 上下文: {Context}", methodName, context);
            return ServiceResult<T>.Failure(cancelMessage);
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex, methodName, context);
        }
    }

    /// <summary>
    /// 安全执行支持取消令牌的无返回值操作，自动处理异常 - DT-011取消令牌支持
    /// </summary>
    public async Task<ServiceResult> HandleException(Func<CancellationToken, Task<ServiceResult>> operation, string methodName, string? context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查操作是否已被取消
            cancellationToken.ThrowIfCancellationRequested();

            return await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 操作被取消，返回专门的取消消息
            var cancelMessage = !string.IsNullOrWhiteSpace(context)
                ? $"{context}: 操作已被用户取消"
                : "操作已被用户取消";

            _logger.LogInformation("操作被取消 - 方法: {MethodName}, 上下文: {Context}", methodName, context);
            return ServiceResult.Failure(cancelMessage);
        }
        catch (Exception ex)
        {
            return HandleException(ex, methodName, context);
        }
    }

    /// <summary>
    /// 记录异常日志 - 统一的日志记录格式
    /// </summary>
    private void LogException(Exception exception, string methodName, string? context)
    {
        var logLevel = DetermineLogLevel(exception);

        var logMessage = "服务方法执行异常 - 方法: {MethodName}";
        var logArgs = new List<object> { methodName };

        if (!string.IsNullOrWhiteSpace(context))
        {
            logMessage += ", 上下文: {Context}";
            logArgs.Add(context);
        }

        logMessage += ", 异常类型: {ExceptionType}, 异常消息: {ExceptionMessage}";
        logArgs.Add(exception.GetType().Name);
        logArgs.Add(exception.Message);

        // 根据异常级别选择合适的日志级别
        switch (logLevel)
        {
            case LogLevel.Error:
                _logger.LogError(exception, logMessage, logArgs.ToArray());
                break;
            case LogLevel.Warning:
                _logger.LogWarning(exception, logMessage, logArgs.ToArray());
                break;
            case LogLevel.Information:
                _logger.LogInformation(exception, logMessage, logArgs.ToArray());
                break;
            default:
                _logger.LogError(exception, logMessage, logArgs.ToArray());
                break;
        }
    }

    /// <summary>
    /// 根据异常类型确定合适的日志级别
    /// </summary>
    private static LogLevel DetermineLogLevel(Exception exception)
    {
        return exception switch
        {
            // 严重错误 - Error级别
            OutOfMemoryException => LogLevel.Error,
            StackOverflowException => LogLevel.Error,
            UnauthorizedAccessException => LogLevel.Error,

            // 业务逻辑错误 - Warning级别 (注意继承关系顺序)
            ArgumentNullException => LogLevel.Warning,
            ArgumentException => LogLevel.Warning,
            InvalidOperationException => LogLevel.Warning,

            // 网络和外部依赖错误 - Information级别(用于统计分析)
            HttpRequestException => LogLevel.Information,
            TimeoutException => LogLevel.Information,

            // 未知异常默认为Error
            _ => LogLevel.Error
        };
    }
}
