namespace LYBT.Desktop.Infrastructure.Navigation;

using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;

/// <summary>
/// 导航服务聚合接口 — 减少 NavigationCoordinator 构造函数参数
/// </summary>
public interface INavigationServices
{
    Prism.Regions.IRegionManager RegionManager { get; }
    ISessionManager SessionManager { get; }
    IRoleRegistry RoleRegistry { get; }
    INavigationHistoryService HistoryService { get; }
    IModuleLazyLoader ModuleLazyLoader { get; }
    IRegionMonitor RegionMonitor { get; }
    IUserNotificationService? UserNotificationService { get; }
}
