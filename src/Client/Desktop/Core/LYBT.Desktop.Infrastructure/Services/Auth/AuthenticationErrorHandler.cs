using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.Extensions.Logging;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Desktop.Infrastructure.Services.Auth
{
    /// <summary>
    /// 认证错误处理器实现 - 专注于认证相关的错误处理
    /// refactor-auth-role-system Phase 1.3
    /// </summary>
    /// <remarks>
    /// 职责：
    /// 1. 将认证异常转换为用户友好消息
    /// 2. 记录安全审计日志
    /// 3. 判断错误是否可重试
    /// 4. 提供标准化的认证错误码
    /// </remarks>
    public class AuthenticationErrorHandler : IAuthenticationErrorHandler
    {
        private readonly ILogger<AuthenticationErrorHandler> _logger;

        /// <summary>
        /// 认证错误码到用户友好消息的映射
        /// </summary>
        private static readonly Dictionary<AuthErrorCode, string> ErrorCodeMessages = new()
        {
            { AuthErrorCode.None, "操作成功" },
            { AuthErrorCode.InvalidCredentials, "用户名或密码错误" },
            { AuthErrorCode.UserNotFound, "用户不存在" },
            { AuthErrorCode.UserDisabled, "用户账号已被禁用，请联系管理员" },
            { AuthErrorCode.PasswordExpired, "密码已过期，请修改密码" },
            { AuthErrorCode.WeakPassword, "密码不符合安全要求" },
            { AuthErrorCode.TokenExpired, "登录已过期，请重新登录" },
            { AuthErrorCode.TokenInvalid, "登录凭据无效，请重新登录" },
            { AuthErrorCode.TokenRevoked, "登录已失效，请重新登录" },
            { AuthErrorCode.RefreshTokenExpired, "会话已过期，请重新登录" },
            { AuthErrorCode.RefreshTokenInvalid, "刷新凭据无效，请重新登录" },
            { AuthErrorCode.SessionNotFound, "会话不存在，请重新登录" },
            { AuthErrorCode.SessionExpired, "会话已到期，请重新登录" },
            { AuthErrorCode.ConcurrentSessionLimit, "登录设备数超过限制，请在其他设备退出后重试" },
            { AuthErrorCode.InternalError, "服务器内部错误，请稍后重试" },
            { AuthErrorCode.ServiceUnavailable, "服务暂时不可用，请稍后重试" }
        };

        /// <summary>
        /// 可重试的错误码
        /// </summary>
        private static readonly HashSet<AuthErrorCode> RetryableErrorCodes = new()
        {
            AuthErrorCode.InternalError,
            AuthErrorCode.ServiceUnavailable
        };

        /// <summary>
        /// 需要重新登录的错误码
        /// </summary>
        private static readonly HashSet<AuthErrorCode> ReLoginRequiredErrorCodes = new()
        {
            AuthErrorCode.TokenExpired,
            AuthErrorCode.TokenInvalid,
            AuthErrorCode.TokenRevoked,
            AuthErrorCode.RefreshTokenExpired,
            AuthErrorCode.RefreshTokenInvalid,
            AuthErrorCode.SessionNotFound,
            AuthErrorCode.SessionExpired,
            AuthErrorCode.UserDisabled
        };

        public AuthenticationErrorHandler(ILogger<AuthenticationErrorHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void HandleAuthenticationError(Exception exception, string? username = null, string? context = null)
        {
            var errorCode = GetErrorCode(exception);
            var message = GetUserFriendlyMessage(errorCode);

            // 记录错误日志
            _logger.LogError(
                exception,
                "认证错误 - 错误码: {ErrorCode}, 用户: {Username}, 上下文: {Context}, 消息: {Message}",
                errorCode, username ?? "(unknown)", context ?? "(none)", message);

            // 记录安全审计日志
            LogSecurityAudit("AuthenticationError", username, false,
                $"ErrorCode: {errorCode}, Context: {context}, Exception: {exception.GetType().Name}");
        }

        /// <inheritdoc/>
        public Task HandleAuthenticationErrorAsync(Exception exception, string? username = null, string? context = null)
        {
            HandleAuthenticationError(exception, username, context);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public string GetUserFriendlyMessage(Exception exception)
        {
            var errorCode = GetErrorCode(exception);
            return GetUserFriendlyMessage(errorCode);
        }

        /// <inheritdoc/>
        public string GetUserFriendlyMessage(AuthErrorCode errorCode)
        {
            if (ErrorCodeMessages.TryGetValue(errorCode, out var message))
            {
                return message;
            }

            // 尝试从枚举Description属性获取
            var descriptionAttr = typeof(AuthErrorCode)
                .GetField(errorCode.ToString())?
                .GetCustomAttribute<DescriptionAttribute>();

            return descriptionAttr?.Description ?? "认证失败，请重试";
        }

        /// <inheritdoc/>
        public bool CanRetry(Exception exception)
        {
            var errorCode = GetErrorCode(exception);
            return RetryableErrorCodes.Contains(errorCode);
        }

        /// <inheritdoc/>
        public bool RequiresReLogin(Exception exception)
        {
            var errorCode = GetErrorCode(exception);
            return ReLoginRequiredErrorCodes.Contains(errorCode);
        }

        /// <inheritdoc/>
        public AuthErrorCode GetErrorCode(Exception exception)
        {
            return exception switch
            {
                // 处理AppException及其子类
                AppException appEx when appEx.TypedErrorCode.HasValue =>
                    MapErrorCodeToAuthErrorCode(appEx.TypedErrorCode.Value),

                // 处理HTTP相关异常
                HttpRequestException httpEx => GetHttpErrorCode(httpEx),

                // 处理超时
                TimeoutException => AuthErrorCode.ServiceUnavailable,
                TaskCanceledException => AuthErrorCode.ServiceUnavailable,

                // 处理认证相关异常
                UnauthorizedAccessException => AuthErrorCode.InvalidCredentials,

                // 默认内部错误
                _ => AuthErrorCode.InternalError
            };
        }

        /// <inheritdoc/>
        public void LogSecurityAudit(string eventType, string? username, bool success, string? details = null)
        {
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            var status = success ? "成功" : "失败";

            _logger.Log(
                logLevel,
                "[安全审计] 事件: {EventType}, 用户: {Username}, 状态: {Status}, 详情: {Details}, 时间: {Timestamp}",
                eventType,
                username ?? "(anonymous)",
                status,
                details ?? "(none)",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <inheritdoc/>
        public Task LogSecurityAuditAsync(string eventType, string? username, bool success, string? details = null)
        {
            LogSecurityAudit(eventType, username, success, details);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 将ErrorCode枚举映射到AuthErrorCode
        /// </summary>
        private static AuthErrorCode MapErrorCodeToAuthErrorCode(EC errorCode)
        {
            // 根据ErrorCode的数值范围映射到AuthErrorCode
            var code = (int)errorCode;

            // 认证相关错误
            if (code == (int)EC.Unauthorized)
                return AuthErrorCode.InvalidCredentials;

            if (code == (int)EC.InvalidPassword)
                return AuthErrorCode.InvalidCredentials;

            if (code == (int)EC.UserDisabled)
                return AuthErrorCode.UserDisabled;

            if (code == (int)EC.UserLocked)
                return AuthErrorCode.UserDisabled;

            if (code == (int)EC.CredentialsExpired)
                return AuthErrorCode.TokenExpired;

            if (code == (int)EC.InvalidRefreshToken)
                return AuthErrorCode.RefreshTokenInvalid;

            if (code == (int)EC.PasswordChangeRequired)
                return AuthErrorCode.PasswordExpired;

            // Phase 1.3: 新增错误码映射
            if (code == (int)EC.TokenExpired)
                return AuthErrorCode.TokenExpired;

            if (code == (int)EC.SessionExpired)
                return AuthErrorCode.SessionExpired;

            if (code == (int)EC.DeviceMismatch)
                return AuthErrorCode.TokenInvalid; // 设备不匹配视为Token无效

            // 默认
            return AuthErrorCode.InternalError;
        }

        /// <summary>
        /// 从HTTP异常获取错误码
        /// </summary>
        private static AuthErrorCode GetHttpErrorCode(HttpRequestException httpException)
        {
            var message = httpException.Message;

            if (message.Contains("401"))
                return AuthErrorCode.InvalidCredentials;
            if (message.Contains("403"))
                return AuthErrorCode.UserDisabled;
            if (message.Contains("503") || message.Contains("502") || message.Contains("504"))
                return AuthErrorCode.ServiceUnavailable;

            return AuthErrorCode.InternalError;
        }
    }
}
