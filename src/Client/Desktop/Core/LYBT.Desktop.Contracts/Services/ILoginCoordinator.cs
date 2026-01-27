using LYBT.Desktop.Contracts.Security;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 登录流程协调器接口
/// 负责编排完整的登录流程，包括认证、会话启动、模块加载和导航
/// OpenSpec: refactor-auth-role-system (Phase 1.1)
/// 已重构为使用统一的 AuthState 替代原有的 LoginFlowState
/// </summary>
public interface ILoginCoordinator
{
    /// <summary>
    /// 当前认证状态
    /// </summary>
    AuthState CurrentState { get; }

    /// <summary>
    /// 是否已登录
    /// </summary>
    bool IsLoggedIn { get; }

    /// <summary>
    /// 当前登录用户信息
    /// </summary>
    UserDetailDto? CurrentUser { get; }

    /// <summary>
    /// 认证状态变更事件
    /// </summary>
    event EventHandler<AuthStateChangedEventArgs>? StateChanged;

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
    /// OpenSpec: simplify-login-options - 移除rememberCredentials参数，凭证保存由ViewModel处理
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>登录结果</returns>
    Task<LoginResult> LoginAsync(string username, string password);

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
/// 登录成功事件参数
/// OpenSpec: simplify-login-options - 移除IsAutoLogin属性
/// </summary>
public class LoginSuccessEventArgs : EventArgs
{
    public UserDetailDto User { get; }
    public DateTime TokenExpiresAt { get; }

    public LoginSuccessEventArgs(UserDetailDto user, DateTime tokenExpiresAt)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
        TokenExpiresAt = tokenExpiresAt;
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
/// OpenSpec: refactor-auth-role-system - 使用AuthState替代LoginFlowState
/// OpenSpec: simplify-login-options - 移除AutoLoginAttemptCount
/// </summary>
public record LoginFlowDiagnostics(
    AuthState CurrentState,
    bool IsLoggedIn,
    string? UserName,
    string? UserRole,
    DateTime? LoginTime,
    DateTime? LastStateChangeTime,
    int LoginAttemptCount
);
