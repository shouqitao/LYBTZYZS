namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.Shell.Services.Login;

public class ShellEventServices : IShellEventServices
{
    public ShellEventServices(
        ILoginStateManager loginState,
        IUserActivityTracker activityTracker,
        ILoginCoordinator loginCoordinator,
        ITokenLifecycleService tokenLifecycle,
        INavigationCoordinator navigation,
        INavigationManager navigationManager,
        IModuleLazyLoader moduleLoader,
        IUiThreadDispatcher uiDispatcher)
    {
        LoginState = loginState;
        ActivityTracker = activityTracker;
        LoginCoordinator = loginCoordinator;
        TokenLifecycle = tokenLifecycle;
        Navigation = navigation;
        NavigationManager = navigationManager;
        ModuleLoader = moduleLoader;
        UiDispatcher = uiDispatcher;
    }

    public ILoginStateManager LoginState { get; }
    public IUserActivityTracker ActivityTracker { get; }
    public ILoginCoordinator LoginCoordinator { get; }
    public ITokenLifecycleService TokenLifecycle { get; }
    public INavigationCoordinator Navigation { get; }
    public INavigationManager NavigationManager { get; }
    public IModuleLazyLoader ModuleLoader { get; }
    public IUiThreadDispatcher UiDispatcher { get; }
}
