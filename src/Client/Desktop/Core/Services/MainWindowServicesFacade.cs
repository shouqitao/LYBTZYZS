using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services.Performance;
using LYBT.Desktop.Workbenches.Core;
using LYBT.Shared.BusinessServices.Interfaces;
using Prism.Ioc;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 主窗口服务门面实现，用于简化MainWindowViewModel的依赖注入
    /// </summary>
    public class MainWindowServicesFacade : IMainWindowServicesFacade
    {
        private readonly IContainerProvider _containerProvider;
        
        // 缓存已解析的服务以提高性能
        private IAuthenticationService? _authenticationService;
        private IPermissionService? _permissionService;
        private ICustomDialogService? _customDialogService;
        private IWorkbenchRouter? _workbenchRouter;
        private IUIPerformanceOptimizer? _uiPerformanceOptimizer;
        private IModuleLoadingCoordinator? _moduleLoadingCoordinator;
        private IUserService? _userService;
        private IPatientService? _patientService;
        private ApiTestService? _apiTestService;

        public MainWindowServicesFacade(IContainerProvider containerProvider)
        {
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
        }

        /// <summary>
        /// 认证服务
        /// </summary>
        public IAuthenticationService AuthenticationService =>
            _authenticationService ??= _containerProvider.Resolve<IAuthenticationService>();

        /// <summary>
        /// 权限服务
        /// </summary>
        public IPermissionService PermissionService =>
            _permissionService ??= _containerProvider.Resolve<IPermissionService>();

        /// <summary>
        /// 对话框服务
        /// </summary>
        public ICustomDialogService CustomDialogService =>
            _customDialogService ??= _containerProvider.Resolve<ICustomDialogService>();

        /// <summary>
        /// 工作台路由器
        /// </summary>
        public IWorkbenchRouter WorkbenchRouter =>
            _workbenchRouter ??= _containerProvider.Resolve<IWorkbenchRouter>();

        /// <summary>
        /// UI性能优化器
        /// </summary>
        public IUIPerformanceOptimizer UIPerformanceOptimizer =>
            _uiPerformanceOptimizer ??= _containerProvider.Resolve<IUIPerformanceOptimizer>();

        /// <summary>
        /// 模块加载协调器
        /// </summary>
        public IModuleLoadingCoordinator ModuleLoadingCoordinator =>
            _moduleLoadingCoordinator ??= _containerProvider.Resolve<IModuleLoadingCoordinator>();

        /// <summary>
        /// 用户服务（延迟加载）
        /// </summary>
        public IUserService UserService =>
            _userService ??= _containerProvider.Resolve<IUserService>();

        /// <summary>
        /// 患者服务（延迟加载）
        /// </summary>
        public IPatientService PatientService =>
            _patientService ??= _containerProvider.Resolve<IPatientService>();

        /// <summary>
        /// API测试服务（可选，延迟加载）
        /// </summary>
        public ApiTestService? ApiTestService
        {
            get
            {
                if (_apiTestService == null)
                {
                    try
                    {
                        _apiTestService = _containerProvider.Resolve<ApiTestService>();
                    }
                    catch
                    {
                        // API测试服务是可选的，如果无法解析则返回null
                        _apiTestService = null;
                    }
                }
                return _apiTestService;
            }
        }
    }
}