using LYBT.Desktop.Services.Auth;
using LYBT.Desktop.Services.Dialogs;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 主窗口服务门面接口 - 聚合认证和对话框服务
    /// </summary>
    public interface IMainWindowServicesFacade
    {
        /// <summary>
        /// 认证服务
        /// </summary>
        IAuthenticationService AuthenticationService { get; }

        /// <summary>
        /// 自定义对话框服务
        /// </summary>
        ICustomDialogService CustomDialogService { get; }
    }
}
