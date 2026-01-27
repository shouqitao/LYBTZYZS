using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// 认证事件定义
/// OpenSpec: refactor-login-authentication (Phase 3.1)
/// 提供跨组件通信的Prism PubSubEvent事件
/// </summary>
public static class AuthEvents
{
    #region 登录相关事件

    /// <summary>
    /// 登录成功事件
    /// 当用户成功登录（手动或自动登录）时触发
    /// </summary>
    public class LoginSucceededEvent : PubSubEvent<LoginSucceededPayload> { }

    /// <summary>
    /// 登录失败事件
    /// 当登录尝试失败时触发
    /// </summary>
    public class LoginFailedEvent : PubSubEvent<LoginFailedPayload> { }

    #endregion

    #region 登出相关事件

    /// <summary>
    /// 登出完成事件
    /// 当用户完成登出时触发
    /// </summary>
    public class LogoutCompletedEvent : PubSubEvent<LogoutCompletedPayload> { }

    /// <summary>
    /// 服务端登出失败事件
    /// 当服务端登出失败并加入重试队列时触发
    /// </summary>
    public class ServerLogoutFailedEvent : PubSubEvent<ServerLogoutFailedPayload> { }

    /// <summary>
    /// 待处理登出已清空事件
    /// 当所有待重试的服务端登出都已处理完成时触发
    /// </summary>
    public class PendingLogoutsClearedEvent : PubSubEvent<PendingLogoutsClearedPayload> { }

    #endregion

    #region 密码相关事件

    /// <summary>
    /// 密码修改成功事件
    /// Issue #1906: 当用户修改密码成功后触发，导航到登录界面
    /// </summary>
    public class PasswordChangedEvent : PubSubEvent<PasswordChangedPayload> { }

    #endregion

    #region Token相关事件

    /// <summary>
    /// Token刷新成功事件
    /// </summary>
    public class TokenRefreshSucceededEvent : PubSubEvent<TokenRefreshSucceededPayload> { }

    /// <summary>
    /// Token刷新失败事件
    /// </summary>
    public class TokenRefreshFailedEvent : PubSubEvent<TokenRefreshFailedPayload> { }

    // OpenSpec: simplify-auth-architecture - 移除SessionExpiringEvent，不再显示警告

    /// <summary>
    /// 会话已过期事件
    /// 当Token过期需要重新登录时触发
    /// </summary>
    public class SessionExpiredEvent : PubSubEvent<SessionExpiredPayload> { }

    #endregion
}

#region 事件载荷定义

/// <summary>
/// 登录成功载荷
/// </summary>
public record LoginSucceededPayload
{
    /// <summary>
    /// 登录用户信息
    /// </summary>
    public required UserDetailDto User { get; init; }

    /// <summary>
    /// Token过期时间
    /// </summary>
    public required DateTime TokenExpiresAt { get; init; }

    /// <summary>
    /// 是否为自动登录
    /// </summary>
    public bool IsAutoLogin { get; init; }

    /// <summary>
    /// 登录时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 登录失败载荷
/// </summary>
public record LoginFailedPayload
{
    /// <summary>
    /// 尝试登录的用户名
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 失败原因
    /// </summary>
    public required LoginFailureReason Reason { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 是否为自动登录尝试
    /// </summary>
    public bool IsAutoLoginAttempt { get; init; }

    /// <summary>
    /// 失败时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 登录失败原因
/// </summary>
public enum LoginFailureReason
{
    /// <summary>
    /// 未知错误
    /// </summary>
    Unknown,

    /// <summary>
    /// 用户名或密码错误
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// 账户被禁用
    /// </summary>
    AccountDisabled,

    /// <summary>
    /// 账户被锁定
    /// </summary>
    AccountLocked,

    /// <summary>
    /// 网络错误
    /// </summary>
    NetworkError,

    /// <summary>
    /// 服务器错误
    /// </summary>
    ServerError,

    /// <summary>
    /// Token无效（自动登录）
    /// </summary>
    TokenInvalid,

    /// <summary>
    /// Token已过期（自动登录）
    /// </summary>
    TokenExpired
}

/// <summary>
/// 登出完成载荷
/// </summary>
public record LogoutCompletedPayload
{
    /// <summary>
    /// 登出的用户名
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 本地登出是否完成
    /// </summary>
    public bool LocalLogoutCompleted { get; init; }

    /// <summary>
    /// 服务端登出是否完成
    /// </summary>
    public bool ServerLogoutCompleted { get; init; }

    /// <summary>
    /// 服务端登出是否已加入重试队列
    /// </summary>
    public bool ServerLogoutQueued { get; init; }

    /// <summary>
    /// 登出时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 服务端登出失败载荷
/// </summary>
public record ServerLogoutFailedPayload
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 失败原因
    /// </summary>
    public required ServerLogoutFailureReason Reason { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 是否已加入重试队列
    /// </summary>
    public bool QueuedForRetry { get; init; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 待处理登出已清空载荷
/// </summary>
public record PendingLogoutsClearedPayload
{
    /// <summary>
    /// 成功处理的登出数量
    /// </summary>
    public int ProcessedCount { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Token刷新成功载荷
/// </summary>
public record TokenRefreshSucceededPayload
{
    /// <summary>
    /// 新Token过期时间
    /// </summary>
    public required DateTime NewExpiresAt { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Token刷新失败载荷
/// </summary>
public record TokenRefreshFailedPayload
{
    /// <summary>
    /// 失败原因
    /// </summary>
    public required TokenRefreshFailureReason Reason { get; init; }

    /// <summary>
    /// 用户友好的错误消息
    /// </summary>
    public required string UserMessage { get; init; }

    /// <summary>
    /// 详细错误消息（用于日志）
    /// </summary>
    public string? DetailedMessage { get; init; }

    /// <summary>
    /// 是否需要重新登录
    /// </summary>
    public bool RequiresReLogin { get; init; }

    /// <summary>
    /// 是否可重试
    /// </summary>
    public bool IsRetryable { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// OpenSpec: simplify-auth-architecture - SessionExpiringPayload已移除

/// <summary>
/// 会话已过期载荷
/// </summary>
public record SessionExpiredPayload
{
    /// <summary>
    /// 过期的用户名
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 过期原因
    /// </summary>
    public SessionExpiredReason Reason { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 密码修改载荷
/// </summary>
/// <remarks>
/// Issue #1906: 密码修改成功后用于通知导航到登录界面
/// </remarks>
public record PasswordChangedPayload
{
    /// <summary>
    /// 修改密码的用户名
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 是否需要重新登录
    /// </summary>
    public bool RequiresReLogin { get; init; } = true;

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 会话过期原因
/// </summary>
public enum SessionExpiredReason
{
    /// <summary>
    /// Token自然过期
    /// </summary>
    TokenExpired,

    /// <summary>
    /// Token刷新失败
    /// </summary>
    RefreshFailed,

    /// <summary>
    /// 用户被踢出
    /// </summary>
    ForcedLogout,

    /// <summary>
    /// 用户长时间不活动
    /// </summary>
    InactivityTimeout
}

#endregion
