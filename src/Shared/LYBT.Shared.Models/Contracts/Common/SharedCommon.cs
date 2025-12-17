using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 共享通用类型定义
    /// OpenSpec: unify-enums-to-shared - 移除嵌套枚举定义，使用统一的LYBT.Shared.Models.Enums.ErrorCategory/ErrorSeverity
    /// </summary>
    public static class SharedCommon
    {
        /// <summary>
        /// 处理后的错误信息
        /// </summary>
        public class HandledError
        {
            /// <summary>
            /// 错误ID
            /// </summary>
            public Guid Id { get; set; } = Guid.NewGuid();

            /// <summary>
            /// 用户友好的错误消息
            /// </summary>
            public string UserMessage { get; set; } = string.Empty;

            /// <summary>
            /// 技术错误消息
            /// </summary>
            public string TechnicalMessage { get; set; } = string.Empty;

            /// <summary>
            /// 错误类别
            /// </summary>
            public ErrorCategory Category { get; set; } = ErrorCategory.Unknown;

            /// <summary>
            /// 错误严重程度
            /// </summary>
            public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

            /// <summary>
            /// 是否可重试
            /// </summary>
            public bool CanRetry { get; set; }

            /// <summary>
            /// 建议的操作
            /// </summary>
            public string[] SuggestedActions { get; set; } = Array.Empty<string>();

            /// <summary>
            /// 时间戳
            /// </summary>
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// 错误上下文
            /// </summary>
            public ErrorContext? Context { get; set; }

            /// <summary>
            /// 原始异常
            /// </summary>
            public Exception? OriginalException { get; set; }
        }
    }
}
