using Prism.Events;

namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// Token相关事件聚合类
/// 统一管理Token刷新、生命周期变更等事件
/// </summary>
/// <remarks>
/// Issue #unify-event-system: 统一事件系统架构
/// 所有Token相关的跨模块事件通过此类发布
/// </remarks>
public static class TokenEvents
{
    #region 刷新事件

    /// <summary>
    /// Token刷新成功事件
    /// </summary>
    public class RefreshSucceededEvent : PubSubEvent<TokenRefreshSucceededPayload> { }

    /// <summary>
    /// Token刷新失败事件
    /// </summary>
    public class RefreshFailedEvent : PubSubEvent<TokenRefreshFailedPayload> { }

    #endregion

    #region 生命周期事件

    /// <summary>
    /// Token生命周期状态变更事件
    /// </summary>
    public class LifecycleChangedEvent : PubSubEvent<TokenLifecycleChangedPayload> { }

    // OpenSpec: simplify-auth-architecture - ExpiringEvent已移除，不再显示过期警告

    /// <summary>
    /// Token已过期事件
    /// </summary>
    public class ExpiredEvent : PubSubEvent<SessionExpiredPayload> { }

    #endregion
}

/// <summary>
/// Token生命周期变更事件载荷
/// </summary>
/// <remarks>
/// 使用record类型符合事件Payload规范(EVENT-002)
/// </remarks>
public record TokenLifecycleChangedPayload
{
    /// <summary>
    /// 之前的状态
    /// </summary>
    public required TokenLifecycleState PreviousState { get; init; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public required TokenLifecycleState CurrentState { get; init; }

    /// <summary>
    /// Token剩余有效时间（仅在Active/Warning状态下有值）
    /// </summary>
    public TimeSpan? RemainingTime { get; init; }

    /// <summary>
    /// 状态变更时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 是否需要用户交互（Warning状态时为true）
    /// </summary>
    public bool RequiresUserInteraction => CurrentState == TokenLifecycleState.Warning;

    /// <summary>
    /// 是否需要重新登录（Expired状态时为true）
    /// </summary>
    public bool RequiresReLogin => CurrentState == TokenLifecycleState.Expired;
}
