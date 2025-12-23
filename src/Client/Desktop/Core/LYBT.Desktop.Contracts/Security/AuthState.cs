namespace LYBT.Desktop.Contracts.Security;

/// <summary>
/// 统一认证状态枚举
/// OpenSpec: refactor-auth-role-system (Phase 1.1)
/// 合并原有LoginState和LoginFlowState，提供完整的认证流程状态
/// </summary>
public enum AuthState
{
    /// <summary>
    /// 空闲状态（未登录）
    /// </summary>
    Idle = 0,

    /// <summary>
    /// 正在验证凭证（手动登录）
    /// </summary>
    Authenticating = 1,

    /// <summary>
    /// 正在验证Token（自动登录）
    /// </summary>
    ValidatingToken = 2,

    /// <summary>
    /// 正在加载用户资料/启动会话
    /// </summary>
    LoadingProfile = 3,

    /// <summary>
    /// 正在加载模块
    /// </summary>
    LoadingModules = 4,

    /// <summary>
    /// 正在导航到首页
    /// </summary>
    Navigating = 5,

    /// <summary>
    /// 已认证（登录成功）
    /// </summary>
    Authenticated = 10,

    /// <summary>
    /// 认证失败
    /// </summary>
    Failed = 20,

    /// <summary>
    /// 正在登出
    /// </summary>
    LoggingOut = 30,

    /// <summary>
    /// 会话已过期
    /// </summary>
    SessionExpired = 40,

    /// <summary>
    /// 正在刷新Token
    /// </summary>
    RefreshingToken = 50
}

/// <summary>
/// 认证事件枚举
/// 用于触发状态机状态转换
/// </summary>
public enum AuthEvent
{
    /// <summary>
    /// 开始手动登录
    /// </summary>
    StartLogin,

    /// <summary>
    /// 开始自动登录
    /// </summary>
    StartAutoLogin,

    /// <summary>
    /// 凭证验证成功
    /// </summary>
    CredentialsValidated,

    /// <summary>
    /// Token验证成功
    /// </summary>
    TokenValidated,

    /// <summary>
    /// 用户资料加载完成
    /// </summary>
    ProfileLoaded,

    /// <summary>
    /// 模块加载完成
    /// </summary>
    ModulesLoaded,

    /// <summary>
    /// 导航完成
    /// </summary>
    NavigationCompleted,

    /// <summary>
    /// 登录失败
    /// </summary>
    LoginFailure,

    /// <summary>
    /// 开始登出
    /// </summary>
    StartLogout,

    /// <summary>
    /// 登出成功
    /// </summary>
    LogoutSuccess,

    /// <summary>
    /// 登出失败
    /// </summary>
    LogoutFailure,

    /// <summary>
    /// 会话过期
    /// </summary>
    SessionExpire,

    /// <summary>
    /// 开始刷新Token
    /// </summary>
    StartTokenRefresh,

    /// <summary>
    /// Token刷新成功
    /// </summary>
    TokenRefreshSuccess,

    /// <summary>
    /// Token刷新失败
    /// </summary>
    TokenRefreshFailure,

    /// <summary>
    /// 重置状态机
    /// </summary>
    Reset
}

/// <summary>
/// 认证状态变更事件参数
/// </summary>
public class AuthStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 变更前状态
    /// </summary>
    public AuthState PreviousState { get; }

    /// <summary>
    /// 变更后状态
    /// </summary>
    public AuthState CurrentState { get; }

    /// <summary>
    /// 触发事件
    /// </summary>
    public AuthEvent Trigger { get; }

    /// <summary>
    /// 状态消息（用于UI显示）
    /// </summary>
    public string? StatusMessage { get; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime Timestamp { get; }

    public AuthStateChangedEventArgs(
        AuthState previousState,
        AuthState currentState,
        AuthEvent trigger,
        string? statusMessage = null)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        Trigger = trigger;
        StatusMessage = statusMessage;
        Timestamp = DateTime.Now;
    }
}
