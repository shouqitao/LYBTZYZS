using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 主窗口服务门面实现
    /// </summary>
    public class MainWindowServicesFacade : IMainWindowServicesFacade
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<MainWindowServicesFacade> _logger;

        public MainWindowServicesFacade(
            IAuthenticationService authenticationService,
            ILogger<MainWindowServicesFacade> logger)
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogDebug("主窗口服务门面初始化完成");
        }

        /// <summary>
        /// 认证服务
        /// </summary>
        public IAuthenticationService AuthenticationService => _authenticationService;
    }
}
