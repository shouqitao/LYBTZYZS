using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Auth;
using LYBT.WPF.Client.Core.Events;
using LYBT.WPF.Client.Core.ViewModels;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using Prism.Commands;
using Prism.Events;


namespace LYBT.WPF.Client.Modules.Authentication.ViewModels
{
    /// <summary>
    /// 登录窗口视图模型
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;

        private string _username = "sysadmin";
        private string _password = "Admin@123456";
        private bool _rememberMe = true;

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

        public LoginViewModel(IEventAggregator eventAggregator, IAuthenticationService authService)
            : base(eventAggregator)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            LoginCommand = new DelegateCommand(ExecuteLoginAsync, CanExecuteLogin);
            
            // 监听登出事件以清除登录状态消息
            EventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout, ThreadOption.UIThread);
        }

        protected override void OnLoadingStateChanged(bool isLoading)
        {
            base.OnLoadingStateChanged(isLoading);
            LoginCommand.RaiseCanExecuteChanged();
        }

        private bool CanExecuteLogin()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private async void ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "请输入用户名";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "请输入密码";
                return;
            }

            try
            {
                IsLoading = true;
                ClearError();

                var request = new LYBT.Shared.Models.Auth.LoginRequest
                {
                    Username = Username.Trim(),
                    Password = Password,
                    RememberMe = RememberMe,
                    ClientIp = GetLocalIPAddress(),
                    UserAgent = "LYBT.WPF.Client",
                    LoginType = "Password"
                };

                var response = await _authService.LoginAsync(request);

                if (response.IsSuccess && response.Data != null)
                {
                    // 检查是否为超级管理员
                    if (response.Data.User.UserName?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        StatusMessage = "超级管理员登录成功，正在跳转...";
                    }
                    else
                    {
                        // Role是字符串，需要先转换为枚举才能使用GetDisplayName
                        if (Enum.TryParse<UserRole>(response.Data.User.Role, out var userRole))
                        {
                            var roleDisplayName = userRole.GetDisplayName();
                            StatusMessage = $"{roleDisplayName}登录成功，正在跳转...";
                        }
                        else
                        {
                            StatusMessage = $"{response.Data.User.Role}登录成功，正在跳转...";
                        }
                    }
                    
                    // 等待一下让用户看到成功消息
                    await Task.Delay(1000);
                    
                    // 通过事件总线通知登录成功
                    EventAggregator.GetEvent<LoginSuccessEvent>().Publish();
                }
                else
                {
                    ErrorMessage = response.Message ?? "登录失败，请检查用户名和密码";
                }
            }
            catch (Exception ex)
            {
                HandleError("登录", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 登出事件处理
        /// </summary>
        private void OnLogout()
        {
            ClearError();
            ClearStatus();
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