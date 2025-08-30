using System;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
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
        private ICustomDialogService? _customDialogService;
        private IUserService? _userService;
        private IPatientService? _patientService;

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
        /// 对话框服务
        /// </summary>
        public ICustomDialogService CustomDialogService =>
            _customDialogService ??= _containerProvider.Resolve<ICustomDialogService>();

        /// <summary>
        /// 用户服务
        /// </summary>
        public IUserService UserService =>
            _userService ??= _containerProvider.Resolve<IUserService>();

        /// <summary>
        /// 患者服务
        /// </summary>
        public IPatientService PatientService =>
            _patientService ??= _containerProvider.Resolve<IPatientService>();
    }
}