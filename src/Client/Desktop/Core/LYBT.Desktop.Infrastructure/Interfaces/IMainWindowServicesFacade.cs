using LYBT.Desktop.Foundation.Security;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 主窗口服务门面接口 - Phase 3.4: 移除对话框服务,仅保留认证服务
    /// </summary>
    public interface IMainWindowServicesFacade
    {
        /// <summary>
        /// 认证服务
        /// </summary>
        IAuthenticationService AuthenticationService { get; }
    }
}
