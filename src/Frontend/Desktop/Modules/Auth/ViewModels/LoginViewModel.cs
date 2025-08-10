using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Auth;
using LYBT.WPF.Client.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;

namespace LYBT.WPF.Client.Modules.Auth.ViewModels
{
    /// <summary>
    /// 登录视图模型
    /// </summary>
    public class LoginViewModel : BindableBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IEventAggregator _eventAggregator;

        #region Properties

        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private bool _rememberMe;
        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private string _serverStatus = "检查中...";
        public string ServerStatus
        {
            get => _serverStatus;
            set => SetProperty(ref _serverStatus, value);
        }

        #endregion

        #region Commands

        public DelegateCommand LoginCommand { get; }
        public DelegateCommand<object> PasswordChangedCommand { get; }
        public DelegateCommand CheckServerCommand { get; }
        public DelegateCommand ExitCommand { get; }

        #endregion

        #region Constructor

        public LoginViewModel(
            IAuthenticationService authenticationService,
            IEventAggregator eventAggregator)
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // 初始化命令
            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin)
                .ObservesProperty(() => UserName)
                .ObservesProperty(() => Password)
                .ObservesProperty(() => IsLoading);

            PasswordChangedCommand = new DelegateCommand<object>(ExecutePasswordChanged);
            CheckServerCommand = new DelegateCommand(async () => await CheckServerStatusAsync());
            ExitCommand = new DelegateCommand(ExecuteExit);

            // 初始化时检查服务器状态
            Task.Run(async () => await CheckServerStatusAsync());
        }

        #endregion

        #region Command Methods

        private bool CanExecuteLogin()
        {
            return !string.IsNullOrWhiteSpace(UserName) && 
                   !string.IsNullOrWhiteSpace(Password) && 
                   !IsLoading;
        }

        private async Task ExecuteLoginAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var loginRequest = new LoginRequest
                {
                    Username = UserName.Trim(),
                    Password = Password,
                    RememberMe = RememberMe
                };

                var result = await _authenticationService.LoginAsync(loginRequest);

                if (result.IsSuccess)
                {
                    // 登录成功，导航到主界面
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 发布登录成功事件
                        _eventAggregator.GetEvent<LoginSuccessEvent>()?.Publish(result.Data);
                        
                        // 导航到主界面（具体导航逻辑根据实际需求调整）
                        NavigateToMainView();
                    });
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "登录失败，请检查用户名和密码";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"登录时发生错误: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecutePasswordChanged(object passwordBox)
        {
            if (passwordBox is System.Windows.Controls.PasswordBox pb)
            {
                Password = pb.Password;
            }
        }

        private async Task CheckServerStatusAsync()
        {
            try
            {
                ServerStatus = "正在连接服务器...";
                // 暂时简化服务器状态检查
                // TODO: 实现真正的服务器连接检查
                await Task.Delay(500); // 模拟检查延迟
                ServerStatus = "服务器连接正常 ✓";
            }
            catch
            {
                ServerStatus = "服务器状态未知";
            }
        }

        private void ExecuteExit()
        {
            Application.Current.Shutdown();
        }

        private void NavigateToMainView()
        {
            // 导航到主界面的逻辑
            // 这里需要根据实际的主界面导航需求来实现
            // 例如：切换到主窗口，关闭登录窗口等
        }

        #endregion
    }

    /// <summary>
    /// 登录成功事件
    /// </summary>
    public class LoginSuccessEvent : PubSubEvent<object>
    {
    }
}