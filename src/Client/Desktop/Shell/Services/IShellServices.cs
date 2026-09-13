namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Shell.Services.Login;

/// <summary>
/// Shell 服务聚合接口 — 减少 MainWindowViewModel 构造函数参数
/// </summary>
public interface IShellServices
{
    IMenuManager Menu { get; }
    INavigationManager Navigation { get; }
    IStatusBarManager StatusBar { get; }
    ISidebarStateManager Sidebar { get; }
    ILoginStateManager LoginState { get; }
    ShellEventCoordinator Events { get; }
    ShellDialogHelper Dialogs { get; }
    IThemeService Theme { get; }
    IApplicationTickService Tick { get; }
    IActiveConsultationService ActiveConsultation { get; }

    /// <summary>Shell 唯一登出入口（含活跃医案离开守卫）</summary>
    IShellLogoutService Logout { get; }
}
