namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Shell.Services.Login;

public class ShellServices : IShellServices
{
    public ShellServices(
        IMenuManager menu,
        INavigationManager navigation,
        IStatusBarManager statusBar,
        ISidebarStateManager sidebar,
        ILoginStateManager loginState,
        ShellEventCoordinator events,
        ShellDialogHelper dialogs,
        IThemeService theme,
        IApplicationTickService tick,
        IActiveConsultationService activeConsultation,
        IShellLogoutService logout)
    {
        Menu = menu;
        Navigation = navigation;
        StatusBar = statusBar;
        Sidebar = sidebar;
        LoginState = loginState;
        Events = events;
        Dialogs = dialogs;
        Theme = theme;
        Tick = tick;
        ActiveConsultation = activeConsultation;
        Logout = logout;
    }

    public IMenuManager Menu { get; }
    public INavigationManager Navigation { get; }
    public IStatusBarManager StatusBar { get; }
    public ISidebarStateManager Sidebar { get; }
    public ILoginStateManager LoginState { get; }
    public ShellEventCoordinator Events { get; }
    public ShellDialogHelper Dialogs { get; }
    public IThemeService Theme { get; }
    public IApplicationTickService Tick { get; }
    public IActiveConsultationService ActiveConsultation { get; }
    public IShellLogoutService Logout { get; }
}
