using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 共享通用类型定义
    /// </summary>
    public static class SharedCommon
    {
        /// <summary>
        /// 错误类别枚举
        /// </summary>
        public enum ErrorCategory
        {
            /// <summary>
            /// 未知错误
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// 网络连接错误
            /// </summary>
            Network = 1,

            /// <summary>
            /// 身份验证错误
            /// </summary>
            Authentication = 2,

            /// <summary>
            /// 授权/权限错误
            /// </summary>
            Authorization = 3,

            /// <summary>
            /// 数据验证错误
            /// </summary>
            Validation = 4,

            /// <summary>
            /// 业务逻辑错误
            /// </summary>
            Business = 5,

            /// <summary>
            /// 数据访问错误
            /// </summary>
            DataAccess = 6,

            /// <summary>
            /// 配置错误
            /// </summary>
            Configuration = 7,

            /// <summary>
            /// 文件系统错误
            /// </summary>
            FileSystem = 8,

            /// <summary>
            /// 并发冲突错误
            /// </summary>
            Concurrency = 9,

            /// <summary>
            /// 超时错误
            /// </summary>
            Timeout = 10,

            /// <summary>
            /// 服务不可用
            /// </summary>
            ServiceUnavailable = 11,

            /// <summary>
            /// 资源不存在
            /// </summary>
            ResourceNotFound = 12,

            /// <summary>
            /// 系统内部错误
            /// </summary>
            System = 13,

            /// <summary>
            /// 内部错误
            /// </summary>
            Internal = 14
        }

        /// <summary>
        /// 错误严重程度枚举
        /// </summary>
        public enum ErrorSeverity
        {
            /// <summary>
            /// 信息级别
            /// </summary>
            Information = 0,

            /// <summary>
            /// 警告级别
            /// </summary>
            Warning = 1,

            /// <summary>
            /// 错误级别
            /// </summary>
            Error = 2,

            /// <summary>
            /// 严重错误级别
            /// </summary>
            Critical = 3
        }

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