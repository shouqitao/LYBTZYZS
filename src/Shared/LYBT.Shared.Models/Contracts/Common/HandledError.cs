namespace LYBT.Shared.Models.Contracts.Common
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
        /// 错误发生时间
        /// </summary>
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 错误模块/组件名称
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// 错误代码
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 异常堆栈跟踪
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// 是否需要用户确认
        /// </summary>
        public bool RequiresUserAcknowledgment { get; set; }

        /// <summary>
        /// 相关的原始异常信息
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 创建网络错误
        /// </summary>
        public static HandledError NetworkError(string message, Exception? exception = null)
        {
            return new HandledError
            {
                Category = ErrorCategory.Network,
                Severity = ErrorSeverity.Error,
                UserMessage = message,
                TechnicalDetails = exception?.Message ?? string.Empty,
                Exception = exception,
                CanRetry = true
            };
        }

        /// <summary>
        /// 创建业务逻辑错误
        /// </summary>
        public static HandledError BusinessError(string message, Exception? exception = null)
        {
            return new HandledError
            {
                Category = ErrorCategory.Business,
                Severity = ErrorSeverity.Warning,
                UserMessage = message,
                TechnicalDetails = exception?.Message ?? string.Empty,
                Exception = exception,
                CanRetry = false
            };
        }

        /// <summary>
        /// 创建验证错误
        /// </summary>
        public static HandledError ValidationError(string message, Exception? exception = null)
        {
            return new HandledError
            {
                Category = ErrorCategory.Validation,
                Severity = ErrorSeverity.Info,
                UserMessage = message,
                TechnicalDetails = exception?.Message ?? string.Empty,
                Exception = exception,
                CanRetry = false
            };
        }

        /// <summary>
        /// 创建致命错误
        /// </summary>
        public static HandledError FatalError(string message, Exception? exception = null)
        {
            return new HandledError
            {
                Category = ErrorCategory.Unknown,
                Severity = ErrorSeverity.Fatal,
                UserMessage = message,
                TechnicalDetails = exception?.Message ?? string.Empty,
                Exception = exception,
                CanRetry = false,
                RequiresUserAcknowledgment = true
            };
        }
    }
}
