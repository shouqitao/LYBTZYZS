using System;
using LYBT.WPF.Client.Core.Models.Common;

namespace LYBT.WPF.Client.Core.Exceptions
{
    /// <summary>
    /// 认证异常
    /// </summary>
    public class AuthenticationException : Exception
    {
        /// <summary>
        /// 认证错误类型
        /// </summary>
        public AuthenticationErrorType ErrorType { get; }

        /// <summary>
        /// 用户友好消息
        /// </summary>
        public string UserMessage { get; }

        /// <summary>
        /// 是否需要重新登录
        /// </summary>
        public bool RequiresReLogin { get; }

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ErrorSeverity Severity { get; }

        public AuthenticationException(string userMessage, AuthenticationErrorType errorType = AuthenticationErrorType.Unknown, bool requiresReLogin = true, ErrorSeverity severity = ErrorSeverity.Error)
            : base(userMessage)
        {
            UserMessage = userMessage;
            ErrorType = errorType;
            RequiresReLogin = requiresReLogin;
            Severity = severity;
        }

        public AuthenticationException(string userMessage, Exception innerException, AuthenticationErrorType errorType = AuthenticationErrorType.Unknown, bool requiresReLogin = true, ErrorSeverity severity = ErrorSeverity.Error)
            : base(userMessage, innerException)
        {
            UserMessage = userMessage;
            ErrorType = errorType;
            RequiresReLogin = requiresReLogin;
            Severity = severity;
        }

        public AuthenticationException(string message, string userMessage, AuthenticationErrorType errorType = AuthenticationErrorType.Unknown, bool requiresReLogin = true, ErrorSeverity severity = ErrorSeverity.Error)
            : base(message)
        {
            UserMessage = userMessage;
            ErrorType = errorType;
            RequiresReLogin = requiresReLogin;
            Severity = severity;
        }

        /// <summary>
        /// 创建Token过期异常
        /// </summary>
        public static AuthenticationException TokenExpired()
        {
            return new AuthenticationException(
                "登录已过期，请重新登录",
                AuthenticationErrorType.TokenExpired,
                requiresReLogin: true,
                severity: ErrorSeverity.Warning
            );
        }

        /// <summary>
        /// 创建无效凭据异常
        /// </summary>
        public static AuthenticationException InvalidCredentials()
        {
            return new AuthenticationException(
                "用户名或密码错误",
                AuthenticationErrorType.InvalidCredentials,
                requiresReLogin: false,
                severity: ErrorSeverity.Error
            );
        }

        /// <summary>
        /// 创建权限不足异常
        /// </summary>
        public static AuthenticationException InsufficientPermissions()
        {
            return new AuthenticationException(
                "权限不足，无法执行此操作",
                AuthenticationErrorType.InsufficientPermissions,
                requiresReLogin: false,
                severity: ErrorSeverity.Error
            );
        }

        /// <summary>
        /// 创建账户被锁定异常
        /// </summary>
        public static AuthenticationException AccountLocked()
        {
            return new AuthenticationException(
                "账户已被锁定，请联系管理员",
                AuthenticationErrorType.AccountLocked,
                requiresReLogin: false,
                severity: ErrorSeverity.Error
            );
        }
    }

    /// <summary>
    /// 认证错误类型
    /// </summary>
    public enum AuthenticationErrorType
    {
        /// <summary>
        /// 未知错误
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Token已过期
        /// </summary>
        TokenExpired = 1,

        /// <summary>
        /// 无效的凭据
        /// </summary>
        InvalidCredentials = 2,

        /// <summary>
        /// 权限不足
        /// </summary>
        InsufficientPermissions = 3,

        /// <summary>
        /// 账户被锁定
        /// </summary>
        AccountLocked = 4,

        /// <summary>
        /// Token无效
        /// </summary>
        InvalidToken = 5,

        /// <summary>
        /// 未授权访问
        /// </summary>
        Unauthorized = 6
    }
}