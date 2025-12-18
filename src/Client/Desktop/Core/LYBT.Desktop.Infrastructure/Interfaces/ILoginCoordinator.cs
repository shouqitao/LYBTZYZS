using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Infrastructure.Interfaces;

/// <summary>
/// 登录流程协调器接口
/// 负责编排完整的登录流程，包括认证、会话启动、模块加载和导航
/// </summary>
public interface ILoginCoordinator
{
    /// <summary>
    /// 当前登录状态
    /// </summary>
    LoginFlowState CurrentState { get; }

    /// <summary>
    /// 是否已登录
    /// </summary>
    bool IsLoggedIn { get; }

    /// <summary>
    /// 当前登录用户信息
    /// </summary>
    UserDetailDto? CurrentUser { get; }

    /// <summary>
    /// 登录流程状态变更事件
    /// </summary>
    event EventHandler<LoginFlowStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// 登录成功事件（供外部组件订阅）
    /// </summary>
    event EventHandler<LoginSuccessEventArgs>? LoginSucceeded;

    /// <summary>
    /// 登出完成事件
    /// </summary>
    event EventHandler? LogoutCompleted;

    /// <summary>
    /// 执行完整的登录流程
    /// 包括：认证 → 启动会话 → 加载模块 → 导航到首页
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="rememberCredentials">是否记住凭证</param>
    /// <returns>登录结果</returns>
    Task<LoginResult> LoginAsync(string username, string password, bool rememberCredentials = false);

    /// <summary>
    /// 使用已存储的Token尝试自动登录
    /// </summary>
    /// <returns>是否自动登录成功</returns>
    Task<bool> TryAutoLoginAsync();

    /// <summary>
    /// 处理登录成功后的流程
    /// 从LoginViewModel或其他来源接收登录成功通知时调用
    /// </summary>
    /// <param name="user">登录用户信息</param>
    /// <param name="tokenExpiresAt">Token过期时间</param>
    Task HandleLoginSuccessAsync(UserDetailDto user, DateTime tokenExpiresAt);

    /// <summary>
    /// 执行登出流程
    /// 包括：清理会话 → 清理Token → 导航回登录页
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// 获取登录流程诊断信息
    /// </summary>
    LoginFlowDiagnostics GetDiagnostics();
}

/// <summary>
/// 登录流程状态
/// </summary>
public enum LoginFlowState
{
    /// <summary>未登录</summary>
    NotLoggedIn,
    /// <summary>正在认证</summary>
    Authenticating,
    /// <summary>正在启动会话</summary>
    StartingSession,
    /// <summary>正在加载模块</summary>
    LoadingModules,
    /// <summary>正在导航</summary>
    Navigating,
    /// <summary>已登录</summary>
    LoggedIn,
    /// <summary>正在登出</summary>
    LoggingOut
}

/// <summary>
/// 登录流程状态变更事件参数
/// </summary>
public class LoginFlowStateChangedEventArgs : EventArgs
{
    public LoginFlowState PreviousState { get; }
    public LoginFlowState CurrentState { get; }
    public string? StatusMessage { get; }

    public LoginFlowStateChangedEventArgs(
        LoginFlowState previousState,
        LoginFlowState currentState,
        string? statusMessage = null)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        StatusMessage = statusMessage;
    }
}

/// <summary>
/// 登录成功事件参数
/// </summary>
public class LoginSuccessEventArgs : EventArgs
{
    public UserDetailDto User { get; }
    public DateTime TokenExpiresAt { get; }
    public bool IsAutoLogin { get; }

    public LoginSuccessEventArgs(UserDetailDto user, DateTime tokenExpiresAt, bool isAutoLogin = false)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
        TokenExpiresAt = tokenExpiresAt;
        IsAutoLogin = isAutoLogin;
    }
}

/// <summary>
/// 登录结果
/// </summary>
public record LoginResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; init; }

    /// <summary>错误消息（失败时）</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>错误代码（失败时）</summary>
    public string? ErrorCode { get; init; }

    /// <summary>用户信息（成功时）</summary>
    public UserDetailDto? User { get; init; }

    public static LoginResult Succeeded(UserDetailDto user) => new()
    {
        Success = true,
        User = user
    };

    public static LoginResult Failed(string errorMessage, string? errorCode = null) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode
    };
}

/// <summary>
/// 登录流程诊断信息
/// </summary>
public record LoginFlowDiagnostics(
    LoginFlowState CurrentState,
    bool IsLoggedIn,
    string? UserName,
    string? UserRole,
    DateTime? LoginTime,
    DateTime? LastStateChangeTime,
    int LoginAttemptCount,
    int AutoLoginAttemptCount
);
