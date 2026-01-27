namespace LYBT.Desktop.Shell.Services.Session;

/// <summary>
/// 会话状态枚举
/// 定义用户会话的各个状态
/// OpenSpec: simplify-auth-architecture - 移除Expiring状态，简化为4状态
/// </summary>
public enum SessionState
{
    /// <summary>
    /// 未认证 - 用户尚未登录
    /// </summary>
    Unauthenticated = 0,

    /// <summary>
    /// 已认证 - 用户已登录，会话有效
    /// </summary>
    Authenticated = 1,

    /// <summary>
    /// 已过期 - Token已过期，需要重新登录
    /// </summary>
    Expired = 2,

    /// <summary>
    /// 刷新中 - 正在刷新Token
    /// </summary>
    Refreshing = 3
}

/// <summary>
/// 会话生命周期管理接口
/// 集中管理用户会话状态和Token生命周期
/// 与Infrastructure.ISessionManager不同，本接口专注于会话状态机和Token生命周期
/// </summary>
public interface ISessionLifecycleManager
{
    /// <summary>
    /// 当前会话状态
    /// </summary>
    SessionState CurrentState { get; }

    /// <summary>
    /// 当前用户是否已认证
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 当前用户名（未登录时为null）
    /// </summary>
    string? CurrentUserName { get; }

    /// <summary>
    /// 当前用户角色（未登录时为null）
    /// </summary>
    string? CurrentUserRole { get; }

    /// <summary>
    /// Token剩余有效时间（未认证时为null）
    /// </summary>
    TimeSpan? TokenRemainingTime { get; }

    /// <summary>
    /// 会话状态变化事件
    /// </summary>
    event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// 会话已过期事件（用于触发重新登录）
    /// OpenSpec: simplify-auth-architecture - 移除SessionExpiring事件，直接静默过期
    /// </summary>
    event EventHandler? SessionExpired;

    /// <summary>
    /// 启动会话（登录成功后调用）
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="userRole">用户角色</param>
    /// <param name="tokenExpiresAt">Token过期时间</param>
    Task StartSessionAsync(string userName, string userRole, DateTime tokenExpiresAt);

    /// <summary>
    /// 结束会话（登出时调用）
    /// </summary>
    Task EndSessionAsync();

    /// <summary>
    /// 刷新Token（Token即将过期时调用）
    /// </summary>
    /// <returns>刷新是否成功</returns>
    Task<bool> RefreshTokenAsync();

    /// <summary>
    /// 更新Token过期时间（Token刷新成功后调用）
    /// </summary>
    /// <param name="newExpiresAt">新的过期时间</param>
    void UpdateTokenExpiration(DateTime newExpiresAt);

    /// <summary>
    /// 记录用户活动（延长会话）
    /// </summary>
    void RecordUserActivity();

    /// <summary>
    /// 获取会话诊断信息
    /// </summary>
    SessionDiagnostics GetDiagnostics();
}

/// <summary>
/// 会话状态变化事件参数
/// </summary>
public class SessionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 前一个状态
    /// </summary>
    public SessionState PreviousState { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public SessionState CurrentState { get; }

    /// <summary>
    /// 变化时间戳
    /// </summary>
    public DateTime Timestamp { get; }

    public SessionStateChangedEventArgs(SessionState previousState, SessionState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        Timestamp = DateTime.Now;
    }
}

// OpenSpec: simplify-auth-architecture - SessionExpiringWarningEventArgs已移除，不再显示过期警告

/// <summary>
/// 会话诊断信息（用于调试和问题排查）
/// </summary>
public record SessionDiagnostics(
    SessionState CurrentState,
    string? UserName,
    string? UserRole,
    DateTime? SessionStartTime,
    DateTime? TokenExpiresAt,
    TimeSpan? TokenRemainingTime,
    DateTime? LastActivityTime,
    int TokenRefreshCount,
    DateTime? LastTokenRefreshTime
);
