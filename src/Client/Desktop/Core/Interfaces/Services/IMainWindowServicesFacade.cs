using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;

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
        /// 对话框服务
        /// </summary>
        ICustomDialogService CustomDialogService { get; }

        /// <summary>
        /// 用户服务
        /// </summary>
        IUserService UserService { get; }

        /// <summary>
        /// 患者服务
        /// </summary>
        IPatientService PatientService { get; }
    }
}