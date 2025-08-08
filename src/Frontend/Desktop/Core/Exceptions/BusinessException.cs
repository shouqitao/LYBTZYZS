using System;
using LYBT.WPF.Client.Core.Models.Common;

namespace LYBT.WPF.Client.Core.Exceptions
{
    /// <summary>
    /// 业务异常
    /// </summary>
    public class BusinessException : Exception
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// 用户友好消息
        /// </summary>
        public string UserMessage { get; }

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ErrorSeverity Severity { get; }

        public BusinessException(string userMessage, string? errorCode = null, ErrorSeverity severity = ErrorSeverity.Error)
            : base(userMessage)
        {
            UserMessage = userMessage;
            ErrorCode = errorCode ?? "BUSINESS_ERROR";
            Severity = severity;
        }

        public BusinessException(string userMessage, Exception innerException, string? errorCode = null, ErrorSeverity severity = ErrorSeverity.Error)
            : base(userMessage, innerException)
        {
            UserMessage = userMessage;
            ErrorCode = errorCode ?? "BUSINESS_ERROR";
            Severity = severity;
        }

        public BusinessException(string message, string userMessage, string? errorCode = null, ErrorSeverity severity = ErrorSeverity.Error)
            : base(message)
        {
            UserMessage = userMessage;
            ErrorCode = errorCode ?? "BUSINESS_ERROR";
            Severity = severity;
        }

        public BusinessException(string message, string userMessage, Exception innerException, string? errorCode = null, ErrorSeverity severity = ErrorSeverity.Error)
            : base(message, innerException)
        {
            UserMessage = userMessage;
            ErrorCode = errorCode ?? "BUSINESS_ERROR";
            Severity = severity;
        }
    }
}