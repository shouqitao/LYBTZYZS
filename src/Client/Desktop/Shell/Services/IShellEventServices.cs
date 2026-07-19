namespace LYBT.Desktop.Shell.Services;

using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.Shell.Services.Login;

/// <summary>
/// Shell 事件协调器服务聚合接口 — 减少 ShellEventCoordinator 构造函数参数
/// </summary>
public interface IShellEventServices
{
    ILoginStateManager LoginState { get; }
    IUserActivityTracker ActivityTracker { get; }
    ILoginCoordinator LoginCoordinator { get; }
    ITokenLifecycleService TokenLifecycle { get; }
    INavigationCoordinator Navigation { get; }
    INavigationManager NavigationManager { get; }
    IModuleLazyLoader ModuleLoader { get; }
    IUiThreadDispatcher UiDispatcher { get; }
}
