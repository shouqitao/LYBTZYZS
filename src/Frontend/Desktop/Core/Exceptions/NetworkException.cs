using System;
using System.Net;
using LYBT.WPF.Client.Core.Models.Common;

namespace LYBT.WPF.Client.Core.Exceptions
{
    /// <summary>
    /// 网络异常
    /// </summary>
    public class NetworkException : Exception
    {
        /// <summary>
        /// HTTP状态码
        /// </summary>
        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// 用户友好消息
        /// </summary>
        public string UserMessage { get; }

        /// <summary>
        /// 是否可重试
        /// </summary>
        public bool CanRetry { get; }

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ErrorSeverity Severity { get; }

        public NetworkException(string userMessage, bool canRetry = true, ErrorSeverity severity = ErrorSeverity.Error)
            : base(userMessage)
        {
            UserMessage = userMessage;
            CanRetry = canRetry;
            Severity = severity;
        }

        public NetworkException(string userMessage, HttpStatusCode statusCode, bool canRetry = true, ErrorSeverity severity = ErrorSeverity.Error)
            : base($"网络请求失败: {statusCode}")
        {
            UserMessage = userMessage;
            StatusCode = statusCode;
            CanRetry = canRetry;
            Severity = severity;
        }

        public NetworkException(string userMessage, Exception innerException, bool canRetry = true, ErrorSeverity severity = ErrorSeverity.Error)
            : base(userMessage, innerException)
        {
            UserMessage = userMessage;
            CanRetry = canRetry;
            Severity = severity;
        }

        public NetworkException(string message, string userMessage, HttpStatusCode? statusCode = null, Exception? innerException = null, bool canRetry = true, ErrorSeverity severity = ErrorSeverity.Error)
            : base(message, innerException)
        {
            UserMessage = userMessage;
            StatusCode = statusCode;
            CanRetry = canRetry;
            Severity = severity;
        }

        /// <summary>
        /// 根据状态码创建网络异常
        /// </summary>
        public static NetworkException FromStatusCode(HttpStatusCode statusCode, string? customMessage = null)
        {
            var (userMessage, canRetry, severity) = statusCode switch
            {
                HttpStatusCode.Unauthorized => ("登录已过期，请重新登录", false, ErrorSeverity.Warning),
                HttpStatusCode.Forbidden => ("无权限访问此资源", false, ErrorSeverity.Error),
                HttpStatusCode.NotFound => ("请求的资源不存在", false, ErrorSeverity.Error),
                HttpStatusCode.BadRequest => ("请求参数错误", false, ErrorSeverity.Error),
                HttpStatusCode.InternalServerError => ("服务器内部错误", true, ErrorSeverity.Error),
                HttpStatusCode.ServiceUnavailable => ("服务暂时不可用", true, ErrorSeverity.Error),
                HttpStatusCode.GatewayTimeout => ("请求超时", true, ErrorSeverity.Error),
                HttpStatusCode.TooManyRequests => ("请求过于频繁，请稍后重试", true, ErrorSeverity.Warning),
                _ => ("网络连接失败", true, ErrorSeverity.Error)
            };

            return new NetworkException(
                customMessage ?? userMessage,
                statusCode,
                canRetry,
                severity
            );
        }
    }
}