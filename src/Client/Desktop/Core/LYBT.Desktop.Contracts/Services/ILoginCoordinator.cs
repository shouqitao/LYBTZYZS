using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 登录流程协调器接口
/// 负责编排完整的登录流程，包括认证、会话启动、模块加载和导航
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
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>登录结果</returns>
    Task<CommandResult<UserDetailDto>> LoginAsync(string username, string password);

    /// <summary>
    /// 执行登出流程
    /// 包括：清理会话 → 清理Token → 导航回登录页
    /// </summary>
    Task LogoutAsync();
}

/// <summary>
/// 登录成功事件参数
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
