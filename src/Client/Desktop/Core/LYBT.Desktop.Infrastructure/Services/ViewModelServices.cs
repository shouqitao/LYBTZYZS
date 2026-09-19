using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services.Toast;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// ViewModel服务聚合实现
    /// </summary>
    public sealed class ViewModelServices : IViewModelServices
    {
        public ILoggerFactory LoggerFactory { get; }
        public IEventAggregator EventAggregator { get; }
        public IRegionManager RegionManager { get; }
        public ISessionManager SessionManager { get; }
        public IUserNotificationService UserNotificationService { get; }
        public ICommonDialogService CommonDialogService { get; }
        public IToastService ToastService { get; }
        public IRoleRegistry RoleRegistry { get; }
        public IUiThreadDispatcher UiThreadDispatcher { get; }
        public INavigationCoordinator NavigationCoordinator { get; }

        public ViewModelServices(
            ILoggerFactory loggerFactory,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            ISessionManager sessionManager,
            IUserNotificationService userNotificationService,
            ICommonDialogService commonDialogService,
            IToastService toastService,
            IRoleRegistry roleRegistry,
            IUiThreadDispatcher uiThreadDispatcher,
            INavigationCoordinator navigationCoordinator)
        {
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            UserNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));
            CommonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));
            ToastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
            RoleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
            UiThreadDispatcher = uiThreadDispatcher ?? throw new ArgumentNullException(nameof(uiThreadDispatcher));
            NavigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));

            // N6：装配 Control code-behind 通知宿主（BaseDetailContainer / HerbItemControl 等无 DI 构造）
            UiNotificationHost.Set(UserNotificationService, ToastService, CommonDialogService);
        }
    }
}
