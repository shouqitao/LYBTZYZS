using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Runtime.Serialization;

namespace LYBT.Desktop.Core.Exceptions
{
    /// <summary>
    /// 应用程序异常基类 - 所有自定义异常的基类
    /// </summary>
    [Serializable]
    public class AppException : Exception
    {
        /// <summary>
        /// 错误类别
        /// </summary>
        public ErrorCategory Category { get; set; }
        
        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ErrorSeverity Severity { get; set; }
        
        /// <summary>
        /// 错误代码
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;
        
        /// <summary>
        /// 用户友好的错误消息
        /// </summary>
        public string UserFriendlyMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// 技术详情（用于日志）
        /// </summary>
        public string TechnicalDetails { get; set; } = string.Empty;
        
        /// <summary>
        /// 关联ID（用于追踪）
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;
        
        /// <summary>
        /// 发生时间
        /// </summary>
        public DateTime OccurredAt { get; set; }
        
        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool IsHandled { get; set; }
        
        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }
        
        /// <summary>
        /// 是否可重试
        /// </summary>
        public bool IsRetryable { get; set; }
        
        public AppException()
            : this("应用程序发生错误")
        {
        }
        
        public AppException(string message)
            : this(message, ErrorCategory.Unknown, ErrorSeverity.Error)
        {
        }
        
        public AppException(string message, Exception innerException)
            : this(message, ErrorCategory.Unknown, ErrorSeverity.Error, innerException)
        {
        }
        
        public AppException(string message, ErrorCategory category, ErrorSeverity severity)
            : base(message)
        {
            Category = category;
            Severity = severity;
            OccurredAt = DateTime.Now;
            ErrorCode = $"{category}_{Guid.NewGuid().ToString("N")[..8]}";
            CorrelationId = Guid.NewGuid().ToString();
            UserFriendlyMessage = GetDefaultUserMessage(category);
            TechnicalDetails = message;
            IsRetryable = DetermineRetryability(category);
        }
        
        public AppException(string message, ErrorCategory category, ErrorSeverity severity, Exception innerException)
            : base(message, innerException)
        {
            Category = category;
            Severity = severity;
            OccurredAt = DateTime.Now;
            ErrorCode = $"{category}_{Guid.NewGuid().ToString("N")[..8]}";
            CorrelationId = Guid.NewGuid().ToString();
            UserFriendlyMessage = GetDefaultUserMessage(category);
            TechnicalDetails = $"{message} | InnerException: {innerException?.Message}";
            IsRetryable = DetermineRetryability(category);
        }
        
        protected AppException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Category = (ErrorCategory)(info.GetValue(nameof(Category), typeof(ErrorCategory)) ?? ErrorCategory.Unknown);
            Severity = (ErrorSeverity)(info.GetValue(nameof(Severity), typeof(ErrorSeverity)) ?? ErrorSeverity.Error);
            ErrorCode = info.GetString(nameof(ErrorCode)) ?? string.Empty;
            UserFriendlyMessage = info.GetString(nameof(UserFriendlyMessage)) ?? string.Empty;
            TechnicalDetails = info.GetString(nameof(TechnicalDetails)) ?? string.Empty;
            CorrelationId = info.GetString(nameof(CorrelationId)) ?? string.Empty;
            OccurredAt = info.GetDateTime(nameof(OccurredAt));
            IsHandled = info.GetBoolean(nameof(IsHandled));
            RetryCount = info.GetInt32(nameof(RetryCount));
            IsRetryable = info.GetBoolean(nameof(IsRetryable));
        }
        
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Category), Category);
            info.AddValue(nameof(Severity), Severity);
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(UserFriendlyMessage), UserFriendlyMessage);
            info.AddValue(nameof(TechnicalDetails), TechnicalDetails);
            info.AddValue(nameof(CorrelationId), CorrelationId);
            info.AddValue(nameof(OccurredAt), OccurredAt);
            info.AddValue(nameof(IsHandled), IsHandled);
            info.AddValue(nameof(RetryCount), RetryCount);
            info.AddValue(nameof(IsRetryable), IsRetryable);
        }
        
        /// <summary>
        /// 获取默认的用户友好消息
        /// </summary>
        private static string GetDefaultUserMessage(ErrorCategory category)
        {
            return category switch
            {
                ErrorCategory.Network => "网络连接出现问题，请检查您的网络设置",
                ErrorCategory.Authentication => "身份验证失败，请重新登录",
                ErrorCategory.Authorization => "您没有权限执行此操作",
                ErrorCategory.Validation => "输入的数据不正确，请检查后重试",
                ErrorCategory.Business => "操作无法完成，请联系管理员",
                ErrorCategory.DataAccess => "无法访问数据，请稍后重试",
                ErrorCategory.Configuration => "系统配置错误，请联系技术支持",
                ErrorCategory.FileSystem => "文件操作失败，请检查文件权限",
                ErrorCategory.Concurrency => "数据已被其他用户修改，请刷新后重试",
                ErrorCategory.Timeout => "操作超时，请稍后重试",
                ErrorCategory.ServiceUnavailable => "服务暂时不可用，请稍后重试",
                ErrorCategory.ResourceNotFound => "请求的资源不存在",
                ErrorCategory.Internal => "系统内部错误，请联系技术支持",
                _ => "操作失败，请稍后重试"
            };
        }
        
        /// <summary>
        /// 确定是否可重试
        /// </summary>
        private static bool DetermineRetryability(ErrorCategory category)
        {
            return category switch
            {
                ErrorCategory.Network => true,
                ErrorCategory.Timeout => true,
                ErrorCategory.ServiceUnavailable => true,
                ErrorCategory.Concurrency => true,
                ErrorCategory.DataAccess => true,
                _ => false
            };
        }
        
        /// <summary>
        /// 创建带错误代码的异常
        /// </summary>
        public static AppException WithErrorCode(string errorCode, string message, ErrorCategory category, ErrorSeverity severity)
        {
            return new AppException(message, category, severity)
            {
                ErrorCode = errorCode
            };
        }
        
        /// <summary>
        /// 增加重试计数
        /// </summary>
        public void IncrementRetryCount()
        {
            RetryCount++;
            if (RetryCount >= 3)
            {
                IsRetryable = false;
            }
        }
    }
}