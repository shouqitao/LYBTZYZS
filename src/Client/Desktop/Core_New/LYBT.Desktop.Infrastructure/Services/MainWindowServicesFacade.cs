using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Services.Auth;
using LYBT.Desktop.Services.Dialogs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 主窗口服务门面实现
    /// 聚合认证服务和对话框服务，简化MainWindowViewModel的依赖注入
    /// </summary>
    public class MainWindowServicesFacade : IMainWindowServicesFacade
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ICustomDialogService _customDialogService;
        private readonly ILogger<MainWindowServicesFacade> _logger;

        public MainWindowServicesFacade(
            IAuthenticationService authenticationService,
            ICustomDialogService customDialogService,
            ILogger<MainWindowServicesFacade> logger)
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _customDialogService = customDialogService ?? throw new ArgumentNullException(nameof(customDialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogDebug("主窗口服务门面初始化完成");
        }

        /// <summary>
        /// 认证服务
        /// </summary>
        public IAuthenticationService AuthenticationService => _authenticationService;

        /// <summary>
        /// 自定义对话框服务
        /// </summary>
        public ICustomDialogService CustomDialogService => _customDialogService;
    }
}
