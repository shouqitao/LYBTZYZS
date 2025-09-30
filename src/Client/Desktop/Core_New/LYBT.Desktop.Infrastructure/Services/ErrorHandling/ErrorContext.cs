using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Infrastructure.Services.ErrorHandling
{
    /// <summary>
    /// 错误上下文信息
    /// 用于记录错误发生时的详细信息
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// 发生错误的操作名称
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// 发生错误的模块名称
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// 当前用户ID
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 附加信息
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 错误严重级别
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

        /// <summary>
        /// 是否需要重试
        /// </summary>
        public bool IsRetryable { get; set; } = false;
    }

    /// <summary>
    /// 错误严重级别枚举
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// 信息
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 严重错误
        /// </summary>
        Critical
    }
}