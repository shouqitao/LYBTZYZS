using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Contracts.Roles;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// ViewModel服务聚合接口
    /// OpenSpec: enhance-viewmodel-architecture
    ///
    /// 设计原则:
    /// - 聚合ViewModel基类所需的通用服务
    /// - 简化子类构造函数参数 (7个 -> 1个)
    /// - 所有服务非空(DI保证)
    /// </summary>
    public interface IViewModelServices
    {
        /// <summary>
        /// 日志工厂
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Prism事件聚合器
        /// </summary>
        IEventAggregator EventAggregator { get; }

        /// <summary>
        /// Prism区域管理器
        /// </summary>
        IRegionManager RegionManager { get; }

        /// <summary>
        /// 会话管理器
        /// </summary>
        ISessionManager SessionManager { get; }

        /// <summary>
        /// 用户通知服务
        /// </summary>
        IUserNotificationService UserNotificationService { get; }

        /// <summary>
        /// 通用对话框服务
        /// </summary>
        ICommonDialogService CommonDialogService { get; }

        /// <summary>
        /// 角色注册表
        /// </summary>
        IRoleRegistry RoleRegistry { get; }
    }
}
