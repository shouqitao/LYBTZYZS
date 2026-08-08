using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// 认证事件定义
/// 提供跨组件通信的Prism PubSubEvent事件
/// </summary>
public static class AuthEvents
{
    #region 登录相关事件

    /// <summary>
    /// 登录开始事件 (US-AUTH-013)
    /// 当登录流程开始前触发，用于 UI 状态显示
    /// </summary>
    public class LoginStartedEvent : PubSubEvent<LoginStartedPayload> { }

    #endregion

    #region 登出相关事件

    /// <summary>
    /// 登出开始事件 (US-AUTH-013)
    /// 当登出流程开始前触发，用于 UI 状态显示
    /// </summary>
    public class LogoutStartedEvent : PubSubEvent<LogoutStartedPayload> { }

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
    /// 会话延续事件 (US-AUTH-013)
    /// 当Token刷新成功时触发，表示用户会话已延续
    /// </summary>
    public class SessionExtendedEvent : PubSubEvent<SessionExtendedPayload> { }

    /// <summary>
    /// Token刷新成功事件
    /// </summary>
    public class TokenRefreshSucceededEvent : PubSubEvent<TokenRefreshSucceededPayload> { }

    /// <summary>
    /// Token刷新失败事件
    /// </summary>
    public class TokenRefreshFailedEvent : PubSubEvent<TokenRefreshFailedPayload> { }

    #endregion

    #region 资料更新相关事件

    /// <summary>
    /// 用户资料更新事件
    /// 当用户在 AccountSettings 修改资料成功后触发，用于跨 VM 同步 CurrentUser
    /// </summary>
    public class ProfileUpdatedEvent : PubSubEvent<ProfileUpdatedPayload> { }

    #endregion
}

#region 事件载荷定义

/// <summary>
/// 登录开始载荷 (US-AUTH-013)
/// </summary>
public record LoginStartedPayload
{
    /// <summary>尝试登录的用户名</summary>
    public string? UserName { get; init; }

    /// <summary>是否为自动登录尝试</summary>
    public bool IsAutoLogin { get; init; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 登出开始载荷 (US-AUTH-013)
/// </summary>
public record LogoutStartedPayload
{
    /// <summary>登出的用户名</summary>
    public string? UserName { get; init; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
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
/// 会话延续载荷 (US-AUTH-013)
/// </summary>
public record SessionExtendedPayload
{
    /// <summary>新的过期时间</summary>
    public required DateTime NewExpiresAt { get; init; }

    /// <summary>时间戳</summary>
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
/// 用户资料更新载荷
/// </summary>
public record ProfileUpdatedPayload
{
    /// <summary>更新后的用户完整信息</summary>
    public required UserDetailDto UpdatedUser { get; init; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

#endregion

#region Token生命周期事件

/// <summary>
/// Token生命周期状态变更事件
/// Issue #1864: 客户端Token生命周期管理
/// </summary>
public class TokenLifecycleStateChangedEvent : PubSubEvent<TokenLifecycleStateChangedEventArgs>
{
}

/// <summary>
/// Token生命周期状态变更事件参数
/// </summary>
public class TokenLifecycleStateChangedEventArgs
{
    public TokenLifecycleStateChangedEventArgs(
        TokenLifecycleState previousState,
        TokenLifecycleState currentState,
        TimeSpan? remainingTime = null)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        RemainingTime = remainingTime;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// 之前的状态
    /// </summary>
    public TokenLifecycleState PreviousState { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public TokenLifecycleState CurrentState { get; }

    /// <summary>
    /// Token剩余有效时间（仅在Active/Warning状态下有值）
    /// </summary>
    public TimeSpan? RemainingTime { get; }

    /// <summary>
    /// 状态变更时间戳
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// 是否需要用户交互（Warning状态时为true）
    /// </summary>
    public bool RequiresUserInteraction => CurrentState == TokenLifecycleState.Warning;

    /// <summary>
    /// 是否需要重新登录（Expired状态时为true）
    /// </summary>
    public bool RequiresReLogin => CurrentState == TokenLifecycleState.Expired;
}

#endregion
