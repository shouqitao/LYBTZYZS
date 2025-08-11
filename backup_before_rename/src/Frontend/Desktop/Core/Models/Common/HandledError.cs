using System;
using System.Collections.Generic;
using LYBT.Desktop.Core.Exceptions;

namespace LYBT.Desktop.Core.Models.Common
{
    /// <summary>
    /// 处理后的错误信息
    /// </summary>
    public class HandledError
    {
        /// <summary>
        /// 错误ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 错误分类
        /// </summary>
        public ErrorCategory Category { get; set; }

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// 用户友好的错误消息
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// 技术详细信息（用于调试）
        /// </summary>
        public string TechnicalDetails { get; set; } = string.Empty;

        /// <summary>
        /// 建议的解决方案
        /// </summary>
        public List<string> SuggestedActions { get; set; } = new List<string>();

        /// <summary>
        /// 是否可重试
        /// </summary>
        public bool CanRetry { get; set; }

        /// <summary>
        /// 重试次数（如果支持重试）
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 错误上下文
        /// </summary>
        public ErrorContext Context { get; set; } = new ErrorContext();

        /// <summary>
        /// 原始异常
        /// </summary>
        public Exception? OriginalException { get; set; }

        /// <summary>
        /// 错误发生时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否需要用户确认
        /// </summary>
        public bool RequiresUserAcknowledgment { get; set; } = true;

        /// <summary>
        /// 添加建议操作
        /// </summary>
        public void AddSuggestedAction(string action)
        {
            if (!string.IsNullOrWhiteSpace(action) && !SuggestedActions.Contains(action))
            {
                SuggestedActions.Add(action);
            }
        }

        /// <summary>
        /// 创建网络错误
        /// </summary>
        public static HandledError CreateNetworkError(string userMessage, Exception? exception = null, ErrorContext? context = null)
        {
            return new HandledError
            {
                Category = ErrorCategory.Network,
                Severity = ErrorSeverity.Error,
                UserMessage = userMessage,
                TechnicalDetails = exception?.ToString() ?? string.Empty,
                OriginalException = exception,
                Context = context ?? new ErrorContext(),
                CanRetry = true,
                SuggestedActions = new List<string> { "检查网络连接", "稍后重试", "联系管理员" }
            };
        }

        /// <summary>
        /// 创建认证错误
        /// </summary>
        public static HandledError CreateAuthenticationError(string userMessage, Exception? exception = null, ErrorContext? context = null)
        {
            return new HandledError
            {
                Category = ErrorCategory.Authentication,
                Severity = ErrorSeverity.Error,
                UserMessage = userMessage,
                TechnicalDetails = exception?.ToString() ?? string.Empty,
                OriginalException = exception,
                Context = context ?? new ErrorContext(),
                CanRetry = false,
                SuggestedActions = new List<string> { "重新登录", "检查用户名和密码", "联系管理员" }
            };
        }

        /// <summary>
        /// 创建验证错误
        /// </summary>
        public static HandledError CreateValidationError(string userMessage, Exception? exception = null, ErrorContext? context = null)
        {
            return new HandledError
            {
                Category = ErrorCategory.Validation,
                Severity = ErrorSeverity.Warning,
                UserMessage = userMessage,
                TechnicalDetails = exception?.ToString() ?? string.Empty,
                OriginalException = exception,
                Context = context ?? new ErrorContext(),
                CanRetry = false,
                RequiresUserAcknowledgment = true,
                SuggestedActions = new List<string> { "检查输入数据", "修正错误信息" }
            };
        }

        /// <summary>
        /// 创建业务错误
        /// </summary>
        public static HandledError CreateBusinessError(string userMessage, Exception? exception = null, ErrorContext? context = null)
        {
            return new HandledError
            {
                Category = ErrorCategory.Business,
                Severity = ErrorSeverity.Error,
                UserMessage = userMessage,
                TechnicalDetails = exception?.ToString() ?? string.Empty,
                OriginalException = exception,
                Context = context ?? new ErrorContext(),
                CanRetry = false,
                SuggestedActions = new List<string> { "检查操作条件", "联系业务人员" }
            };
        }

        /// <summary>
        /// 创建系统错误
        /// </summary>
        public static HandledError CreateSystemError(string userMessage, Exception? exception = null, ErrorContext? context = null)
        {
            return new HandledError
            {
                Category = ErrorCategory.Internal,
                Severity = ErrorSeverity.Critical,
                UserMessage = userMessage,
                TechnicalDetails = exception?.ToString() ?? string.Empty,
                OriginalException = exception,
                Context = context ?? new ErrorContext(),
                CanRetry = true,
                MaxRetryCount = 1,
                SuggestedActions = new List<string> { "稍后重试", "重启应用程序", "联系技术支持" }
            };
        }
    }
}