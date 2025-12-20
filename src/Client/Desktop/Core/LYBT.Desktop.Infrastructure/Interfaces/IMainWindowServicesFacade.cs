using LYBT.Desktop.Foundation.Security;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 主窗口服务门面接口
    /// </summary>
    public interface IMainWindowServicesFacade
    {
        /// <summary>
        /// 认证服务
        /// </summary>
        IAuthenticationService AuthenticationService { get; }
    }
}
