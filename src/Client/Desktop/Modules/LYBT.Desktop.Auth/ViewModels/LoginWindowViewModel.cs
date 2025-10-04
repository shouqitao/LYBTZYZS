using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Auth.ViewModels
{
    /// <summary>
    /// 登录窗口视图模型 - Phase 4B 骨架实现
    /// </summary>
    public class LoginWindowViewModel : BindableBase
    {
        private readonly ILogger<LoginWindowViewModel> _logger;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isLoading;

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 登录命令
        /// </summary>
        public DelegateCommand LoginCommand { get; }

        public LoginWindowViewModel(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<LoginWindowViewModel>()
                ?? throw new ArgumentNullException(nameof(loggerFactory));

            LoginCommand = new DelegateCommand(ExecuteLogin, CanExecuteLogin);
        }

        private void ExecuteLogin()
        {
            _logger.LogInformation("LoginWindow - 登录命令执行（骨架实现）");
            _logger.LogDebug("用户名: {Username}", Username);

            // TODO: Phase 4C - 实现实际登录逻辑
        }

        private bool CanExecuteLogin()
        {
            return !string.IsNullOrWhiteSpace(Username)
                && !string.IsNullOrWhiteSpace(Password)
                && !IsLoading;
        }
    }
}
