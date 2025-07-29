using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Authentication;
using LYBT.WPF.Client.Core.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;
using Prism.Events;


namespace LYBT.WPF.Client.Modules.Authentication.ViewModels
{
    /// <summary>
    /// 登录窗口视图模型
    /// </summary>
    public class LoginViewModel : BindableBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IEventAggregator _eventAggregator;

        private string _username = "sysadmin";
        private string _password = string.Empty;
        private bool _rememberMe = true;
        private bool _isLoading = false;
        private string _loginStatusMessage = string.Empty;

        public DelegateCommand LoginCommand { get; }
        public DelegateCommand<PasswordBox>? PasswordChangedCommand { get; set; }

        /// <summary>用户名</summary>
        public string Username
        {
            get => _username;
            set 
            { 
                SetProperty(ref _username, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>密码</summary>
        public string Password
        {
            get => _password;
            set 
            { 
                SetProperty(ref _password, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>记住我</summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set 
            { 
                SetProperty(ref _isLoading, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>登录状态消息</summary>
        public string LoginStatusMessage
        {
            get => _loginStatusMessage;
            set => SetProperty(ref _loginStatusMessage, value);
        }

        public LoginViewModel(IEventAggregator eventAggregator, IAuthenticationService authService)
        {
            _eventAggregator = eventAggregator;
            _authService = authService;

            LoginCommand = new DelegateCommand(ExecuteLogin, CanExecuteLogin);
        }

        private bool CanExecuteLogin()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private async void ExecuteLogin()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                LoginStatusMessage = "请输入用户名";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                LoginStatusMessage = "请输入密码";
                return;
            }

            IsLoading = true;
            LoginStatusMessage = string.Empty;

            try
            {
                var request = new LoginRequest
                {
                    Username = Username.Trim(),
                    Password = Password,
                    RememberMe = RememberMe,
                    ClientIp = GetLocalIPAddress(),
                    UserAgent = "LYBT.WPF.Client",
                    LoginType = "Password"
                };

                var response = await _authService.LoginAsync(request);
                
                if (response.Success)
                {
                    LoginStatusMessage = "登录成功，正在跳转...";
                    
                    // 等待一下让用户看到成功消息
                    await Task.Delay(1000);
                    
                    // 通过事件总线通知登录成功
                    _eventAggregator.GetEvent<LoginSuccessEvent>().Publish();
                }
                else
                {
                    LoginStatusMessage = response.Message ?? "登录失败，请检查用户名和密码";
                }
            }
            catch (Exception ex)
            {
                LoginStatusMessage = $"登录出错：{ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // 忽略错误
            }
            return "127.0.0.1";
        }
    }
}