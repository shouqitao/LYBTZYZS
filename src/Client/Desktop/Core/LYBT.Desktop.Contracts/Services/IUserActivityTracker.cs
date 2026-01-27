namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 用户活动追踪服务接口
/// OpenSpec: refactor-token-sliding-expiration (AUTH-001, AUTH-002, AUTH-003)
/// 追踪用户UI交互活动,检测不活跃状态,触发会话过期事件
/// </summary>
public interface IUserActivityTracker
{
    /// <summary>
    /// 用户最后活动时间
    /// </summary>
    DateTime LastActivityTime { get; }

    /// <summary>
    /// 用户是否活跃(在配置的超时时间内有活动)
    /// </summary>
    bool IsUserActive { get; }

    /// <summary>
    /// 距离不活跃超时的剩余时间
    /// </summary>
    TimeSpan TimeUntilInactive { get; }

    /// <summary>
    /// 是否正在追踪
    /// </summary>
    bool IsTracking { get; }

    // OpenSpec: simplify-auth-architecture - 移除SessionExpiring事件，不再显示警告

    /// <summary>
    /// 会话已过期事件(需要登出)
    /// </summary>
    event EventHandler? SessionExpired;

    /// <summary>
    /// 开始追踪用户活动
    /// </summary>
    void StartTracking();

    /// <summary>
    /// 停止追踪
    /// </summary>
    void StopTracking();

    /// <summary>
    /// 重置活动计时器(用户操作或刷新Token成功后调用)
    /// </summary>
    void ResetActivity();
}

// OpenSpec: simplify-auth-architecture - SessionExpiringEventArgs已移除
