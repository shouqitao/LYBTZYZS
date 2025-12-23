namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 登录状态枚举
    /// OpenSpec: refactor-login-authentication (Phase 2.1)
    /// 定义登录流程中的所有可能状态
    /// </summary>
    public enum LoginState
    {
        /// <summary>
        /// 未登录状态（初始状态）
        /// 允许转换到: LoggingIn, AutoLoggingIn
        /// </summary>
        NotLoggedIn = 0,

        /// <summary>
        /// 正在登录（用户输入凭据后）
        /// 允许转换到: LoggedIn, LoginFailed, NotLoggedIn
        /// </summary>
        LoggingIn = 1,

        /// <summary>
        /// 正在自动登录（使用AutoLoginToken）
        /// 允许转换到: LoggedIn, NotLoggedIn
        /// </summary>
        AutoLoggingIn = 2,

        /// <summary>
        /// 已登录状态
        /// 允许转换到: LoggingOut, SessionExpired, TokenRefreshing
        /// </summary>
        LoggedIn = 3,

        /// <summary>
        /// 登录失败（可重试）
        /// 允许转换到: LoggingIn, NotLoggedIn
        /// </summary>
        LoginFailed = 4,

        /// <summary>
        /// 正在登出
        /// 允许转换到: NotLoggedIn, LoggedIn（登出失败时回滚）
        /// </summary>
        LoggingOut = 5,

        /// <summary>
        /// 会话已过期（需要重新登录）
        /// 允许转换到: NotLoggedIn, LoggingIn
        /// </summary>
        SessionExpired = 6,

        /// <summary>
        /// 正在刷新Token
        /// 允许转换到: LoggedIn, SessionExpired
        /// </summary>
        TokenRefreshing = 7
    }

    /// <summary>
    /// 状态转换触发器
    /// </summary>
    public enum LoginTrigger
    {
        /// <summary>开始登录</summary>
        StartLogin,

        /// <summary>开始自动登录</summary>
        StartAutoLogin,

        /// <summary>登录成功</summary>
        LoginSuccess,

        /// <summary>登录失败</summary>
        LoginFailure,

        /// <summary>开始登出</summary>
        StartLogout,

        /// <summary>登出成功</summary>
        LogoutSuccess,

        /// <summary>登出失败</summary>
        LogoutFailure,

        /// <summary>会话过期</summary>
        SessionExpire,

        /// <summary>开始刷新Token</summary>
        StartTokenRefresh,

        /// <summary>Token刷新成功</summary>
        TokenRefreshSuccess,

        /// <summary>Token刷新失败</summary>
        TokenRefreshFailure,

        /// <summary>重置（返回未登录）</summary>
        Reset
    }
}
