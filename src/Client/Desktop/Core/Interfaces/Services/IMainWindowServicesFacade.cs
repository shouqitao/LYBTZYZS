using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Services.Performance;
using LYBT.Desktop.Workbenches.Core;
using LYBT.Shared.BusinessServices.Interfaces;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 主窗口服务门面，用于简化MainWindowViewModel的依赖注入
    /// </summary>
    public interface IMainWindowServicesFacade
    {
        /// <summary>
        /// 认证服务
        /// </summary>
        IAuthenticationService AuthenticationService { get; }

        /// <summary>
        /// 权限服务
        /// </summary>
        IPermissionService PermissionService { get; }

        /// <summary>
        /// 对话框服务
        /// </summary>
        ICustomDialogService CustomDialogService { get; }

        /// <summary>
        /// 工作台路由器
        /// </summary>
        IWorkbenchRouter WorkbenchRouter { get; }

        /// <summary>
        /// UI性能优化器
        /// </summary>
        IUIPerformanceOptimizer UIPerformanceOptimizer { get; }

        /// <summary>
        /// 模块加载协调器
        /// </summary>
        IModuleLoadingCoordinator ModuleLoadingCoordinator { get; }

        /// <summary>
        /// 用户服务（延迟加载）
        /// </summary>
        IUserService UserService { get; }

        /// <summary>
        /// 患者服务（延迟加载）
        /// </summary>
        IPatientService PatientService { get; }

        /// <summary>
        /// API测试服务（可选，延迟加载）
        /// </summary>
        ApiTestService? ApiTestService { get; }
    }
}