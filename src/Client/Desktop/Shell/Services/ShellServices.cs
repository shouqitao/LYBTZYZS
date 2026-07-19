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
        ILoginStateManager loginState,
        ShellEventCoordinator events,
        ShellDialogHelper dialogs,
        IThemeService theme,
        IApplicationTickService tick,
        IActiveConsultationService activeConsultation)
    {
        Menu = menu;
        Navigation = navigation;
        StatusBar = statusBar;
        LoginState = loginState;
        Events = events;
        Dialogs = dialogs;
        Theme = theme;
        Tick = tick;
        ActiveConsultation = activeConsultation;
    }

    public IMenuManager Menu { get; }
    public INavigationManager Navigation { get; }
    public IStatusBarManager StatusBar { get; }
    public ILoginStateManager LoginState { get; }
    public ShellEventCoordinator Events { get; }
    public ShellDialogHelper Dialogs { get; }
    public IThemeService Theme { get; }
    public IApplicationTickService Tick { get; }
    public IActiveConsultationService ActiveConsultation { get; }
}
