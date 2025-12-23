using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 认证错误处理器接口 - 专注于认证相关的错误处理
    /// refactor-auth-role-system Phase 1.3
    /// </summary>
    /// <remarks>
    /// 职责：
    /// 1. 将认证异常转换为用户友好消息
    /// 2. 记录安全审计日志
    /// 3. 判断错误是否可重试
    /// 4. 提供标准化的认证错误码
    /// </remarks>
    public interface IAuthenticationErrorHandler
    {
        /// <summary>
        /// 处理认证错误
        /// </summary>
        /// <param name="exception">认证异常</param>
        /// <param name="username">用户名（可选，用于审计日志）</param>
        /// <param name="context">上下文信息</param>
        void HandleAuthenticationError(Exception exception, string? username = null, string? context = null);

        /// <summary>
        /// 处理认证错误（异步）
        /// </summary>
        Task HandleAuthenticationErrorAsync(Exception exception, string? username = null, string? context = null);

        /// <summary>
        /// 获取认证错误的用户友好消息
        /// </summary>
        /// <param name="exception">认证异常</param>
        /// <returns>用户友好的错误消息</returns>
        string GetUserFriendlyMessage(Exception exception);

        /// <summary>
        /// 根据错误码获取用户友好消息
        /// </summary>
        /// <param name="errorCode">认证错误码</param>
        /// <returns>用户友好的错误消息</returns>
        string GetUserFriendlyMessage(AuthErrorCode errorCode);

        /// <summary>
        /// 判断认证错误是否可重试
        /// </summary>
        /// <param name="exception">认证异常</param>
        /// <returns>是否可重试</returns>
        bool CanRetry(Exception exception);

        /// <summary>
        /// 判断是否需要强制重新登录
        /// </summary>
        /// <param name="exception">认证异常</param>
        /// <returns>是否需要重新登录</returns>
        bool RequiresReLogin(Exception exception);

        /// <summary>
        /// 获取认证错误码
        /// </summary>
        /// <param name="exception">认证异常</param>
        /// <returns>认证错误码</returns>
        AuthErrorCode GetErrorCode(Exception exception);

        /// <summary>
        /// 记录安全审计日志
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="username">用户名</param>
        /// <param name="success">是否成功</param>
        /// <param name="details">详细信息</param>
        void LogSecurityAudit(string eventType, string? username, bool success, string? details = null);

        /// <summary>
        /// 记录安全审计日志（异步）
        /// </summary>
        Task LogSecurityAuditAsync(string eventType, string? username, bool success, string? details = null);
    }
}
