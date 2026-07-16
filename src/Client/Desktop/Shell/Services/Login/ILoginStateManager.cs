using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Shell.Services.Login;

/// <summary>
/// 登录状态管理器接口
/// 管理用户登录状态、Token生命周期事件处理、会话过期自动登出、密码变更事件处理
/// 从 MainWindowViewModel 提取的登录状态管理职责
/// </summary>
public interface ILoginStateManager : IDisposable
{
    /// <summary>是否已登录</summary>
    bool IsLoggedIn { get; }

    /// <summary>是否未登录</summary>
    bool IsNotLoggedIn { get; }

    /// <summary>当前登录用户</summary>
    UserDetailDto? CurrentUser { get; }

    /// <summary>窗口标题（含登录用户信息）</summary>
    string Title { get; }

    /// <summary>当前用户显示名称</summary>
    string CurrentUserDisplayName { get; }

    /// <summary>当前用户首字母（头像占位）</summary>
    string CurrentUserInitial { get; }

    /// <summary>当前用户角色显示文本</summary>
    string CurrentUserRoleDisplay { get; }

    /// <summary>
    /// 登录状态变更事件
    /// 当 IsLoggedIn、CurrentUser、Title 等属性变更后触发，供 UI 层刷新绑定
    /// </summary>
    event EventHandler? LoginStateChanged;

    /// <summary>
    /// 登出请求事件
    /// PerformLogoutAsync 完成服务端登出后触发，通知 UI 层清理导航状态
    /// </summary>
    event EventHandler? LogoutRequested;

    /// <summary>应用登录成功状态（设置用户信息、更新标题）</summary>
    void ApplyLoginSuccess(UserDetailDto user);

    /// <summary>应用密码变更状态（重置为未登录）</summary>
    void ApplyPasswordChanged();

    /// <summary>应用用户资料更新</summary>
    void ApplyProfileUpdate(UserDetailDto updatedUser);

    /// <summary>执行完整登出流程（停止追踪、重置Token、服务端登出、发布事件）</summary>
    Task PerformLogoutAsync();

    /// <summary>处理Token过期（显示提示 + 自动登出）</summary>
    Task HandleTokenExpiredAsync();

    /// <summary>处理会话过期（显示提示 + 自动登出）</summary>
    Task HandleSessionExpiredAsync();
}
