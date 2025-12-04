using System.Net.Http;
using System.Net.Sockets;
using LYBT.Desktop.Foundation.Logging;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Exceptions
{
    /// <summary>
    /// 标准异常处理器实现 - 简化版本
    /// 提供统一的异常处理逻辑，遵循"适度设计、拒绝过度工程"原则
    /// refactor-logging-system: 增强日志，添加CorrelationId支持
    /// </summary>
    public class StandardExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<StandardExceptionHandler> _logger;

        public StandardExceptionHandler(ILogger<StandardExceptionHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理异常并返回服务结果
        /// </summary>
        public ServiceResult HandleException(Exception exception, string methodName, string? context = null)
        {
            LogException(exception, methodName, context);
            var userMessage = ExceptionMessageMapper.GetUserFriendlyMessage(exception);

            if (!string.IsNullOrWhiteSpace(context))
                userMessage = $"{context}: {userMessage}";

            return ServiceResult.Failure(userMessage);
        }

        /// <summary>
        /// 处理异常并返回泛型服务结果
        /// </summary>
        public ServiceResult<T> HandleException<T>(Exception exception, string methodName, string? context = null)
        {
            LogException(exception, methodName, context);
            var userMessage = ExceptionMessageMapper.GetUserFriendlyMessage(exception);

            if (!string.IsNullOrWhiteSpace(context))
                userMessage = $"{context}: {userMessage}";

            return ServiceResult<T>.Failure(userMessage);
        }

        /// <summary>
        /// 安全执行带数据类型的操作，自动处理异常
        /// </summary>
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

        /// <summary>
        /// 安全执行无数据的操作，自动处理异常
        /// </summary>
        public async Task<ServiceResult> SafeExecuteAsync(Func<Task<ServiceResult>> operation, string methodName, string? context = null)
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
        /// 记录异常日志
        /// refactor-logging-system: 增强日志，添加CorrelationId
        /// </summary>
        private void LogException(Exception exception, string methodName, string? context)
        {
            var logLevel = DetermineLogLevel(exception);
            var correlationId = CorrelationIdContext.CurrentOrNew;
            var message = "服务方法执行异常 - 方法: {MethodName}, 上下文: {Context}, 异常: {ExceptionType}, CorrelationId: {CorrelationId}";

            switch (logLevel)
            {
                case LogLevel.Error:
                    _logger.LogError(exception, message, methodName, context ?? "无", exception.GetType().Name, correlationId);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(exception, message, methodName, context ?? "无", exception.GetType().Name, correlationId);
                    break;
                default:
                    _logger.LogInformation(exception, message, methodName, context ?? "无", exception.GetType().Name, correlationId);
                    break;
            }
        }

        /// <summary>
        /// 根据异常类型确定日志级别
        /// </summary>
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

        #region IExceptionHandler 实现

        /// <summary>
        /// 处理异常（IExceptionHandler接口实现）
        /// </summary>
        public void HandleException(Exception exception, string? context = null)
        {
            LogException(exception, context ?? "Unknown", null);
        }

        /// <summary>
        /// 异步处理异常（IExceptionHandler接口实现）
        /// </summary>
        public Task HandleExceptionAsync(Exception exception, string? context = null)
        {
            HandleException(exception, context);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 记录异常（IExceptionHandler接口实现）
        /// refactor-logging-system: 增强日志，添加CorrelationId
        /// </summary>
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

            var correlationId = CorrelationIdContext.CurrentOrNew;
            _logger.Log(logLevel, exception, "异常发生 - 类型: {ExceptionType}, CorrelationId: {CorrelationId}", exception.GetType().Name, correlationId);
        }

        /// <summary>
        /// 获取用户友好的错误消息（IExceptionHandler接口实现）
        /// </summary>
        public string GetUserFriendlyMessage(Exception exception)
        {
            return ExceptionMessageMapper.GetUserFriendlyMessage(exception);
        }

        /// <summary>
        /// 判断是否可重试（IExceptionHandler接口实现）
        /// </summary>
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

        #endregion
    }
}
