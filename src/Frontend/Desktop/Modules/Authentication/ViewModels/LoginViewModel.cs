using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Auth;
using LYBT.WPF.Client.Core.Events;
using LYBT.WPF.Client.Core.ViewModels;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using LYBT.WPF.Client.Services;
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
        private readonly ICredentialService _credentialService;

        private string _username = "";
        private string _password = "";
        private bool _rememberMe = false;
        private bool _hasSavedPassword = false;

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

        /// <summary>是否有保存的密码</summary>
        public bool HasSavedPassword
        {
            get => _hasSavedPassword;
            set => SetProperty(ref _hasSavedPassword, value);
        }

        public LoginViewModel(IEventAggregator eventAggregator, IAuthenticationService authService, ICredentialService credentialService)
            : base(eventAggregator)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

            LoginCommand = new DelegateCommand(ExecuteLoginAsync, CanExecuteLogin);
            
            // 监听登出事件以清除登录状态消息
            EventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout, ThreadOption.UIThread);
            
            // 立即加载保存的凭据
            LoadSavedCredentials();
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
                    // 保存凭据（如果选择了记住我）
                    _credentialService.SaveCredentials(Username, Password, RememberMe);
                    
                    // 检查是否为超级管理员
                    if (response.Data.User.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        StatusMessage = "超级管理员登录成功，正在跳转...";
                    }
                    else
                    {
                        // Role已经是枚举类型，直接使用GetDisplayName
                        var roleDisplayName = response.Data.User.Role.GetDisplayName();
                        StatusMessage = $"{roleDisplayName}登录成功，正在跳转...";
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
            
            // 登出时重新加载保存的凭据（如果有）
            LoadSavedCredentials();
        }
        
        /// <summary>
        /// 加载保存的凭据
        /// </summary>
        private void LoadSavedCredentials()
        {
            try
            {
                var savedCredentials = _credentialService.LoadCredentials();
                if (savedCredentials != null)
                {
                    Username = savedCredentials.Username;
                    Password = savedCredentials.Password;
                    RememberMe = savedCredentials.RememberMe;
                    HasSavedPassword = !string.IsNullOrEmpty(savedCredentials.Password);
                }
                else
                {
                    HasSavedPassword = false;
                }
            }
            catch (Exception ex)
            {
                // 静默处理错误，避免影响用户体验
                System.Diagnostics.Debug.WriteLine($"加载凭据时出错: {ex.Message}");
                HasSavedPassword = false;
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