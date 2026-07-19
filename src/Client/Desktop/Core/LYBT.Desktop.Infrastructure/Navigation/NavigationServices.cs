namespace LYBT.Desktop.Infrastructure.Navigation;

using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;

public class NavigationServices : INavigationServices
{
    public NavigationServices(
        Prism.Regions.IRegionManager regionManager,
        ISessionManager sessionManager,
        IRoleRegistry roleRegistry,
        INavigationHistoryService historyService,
        IModuleLazyLoader moduleLazyLoader,
        IRegionMonitor regionMonitor,
        IUserNotificationService? userNotificationService = null)
    {
        RegionManager = regionManager;
        SessionManager = sessionManager;
        RoleRegistry = roleRegistry;
        HistoryService = historyService;
        ModuleLazyLoader = moduleLazyLoader;
        RegionMonitor = regionMonitor;
        UserNotificationService = userNotificationService;
    }

    public Prism.Regions.IRegionManager RegionManager { get; }
    public ISessionManager SessionManager { get; }
    public IRoleRegistry RoleRegistry { get; }
    public INavigationHistoryService HistoryService { get; }
    public IModuleLazyLoader ModuleLazyLoader { get; }
    public IRegionMonitor RegionMonitor { get; }
    public IUserNotificationService? UserNotificationService { get; }
}
