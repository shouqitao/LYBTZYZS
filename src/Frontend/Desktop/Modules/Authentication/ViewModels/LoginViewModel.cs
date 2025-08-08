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
        private bool _isApiOnline = false;
        private string _apiStatus = "正在检测API连接...";
        private System.Threading.Timer? _apiCheckTimer;

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

        /// <summary>API是否在线</summary>
        public bool IsApiOnline
        {
            get => _isApiOnline;
            set
            {
                SetProperty(ref _isApiOnline, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>API状态信息</summary>
        public string ApiStatus
        {
            get => _apiStatus;
            set => SetProperty(ref _apiStatus, value);
        }

        public LoginViewModel(IEventAggregator eventAggregator, IAuthenticationService authService, ICredentialService credentialService)
            : base(eventAggregator)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);

            // 监听登出事件以清除登录状态消息
            EventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout, ThreadOption.UIThread);

            // 立即加载保存的凭据
            LoadSavedCredentials();

            // 启动API连接检测
            StartApiConnectionCheck();
        }

        protected override void OnLoadingStateChanged(bool isLoading)
        {
            base.OnLoadingStateChanged(isLoading);
            LoginCommand.RaiseCanExecuteChanged();
        }

        private bool CanExecuteLogin()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && IsApiOnline;
        }

        private async Task ExecuteLoginAsync()
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
                        // 根据用户名判断角色
                        var roleDisplayName = "用户";
                        StatusMessage = $"{roleDisplayName}登录成功，正在跳转...";
                    }

                    // 等待一下让用户看到成功消息
                    await Task.Delay(1000);

                    // 通过事件总线通知登录成功
                    EventAggregator.GetEvent<LoginSuccessEvent>().Publish();
                }
                else
                {
                    ErrorMessage = response.ErrorMessage ?? "登录失败，请检查用户名和密码";
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
            catch (Exception)
            {
                // 静默处理错误，避免影响用户体验
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

        /// <summary>
        /// 启动API连接检测
        /// </summary>
        private void StartApiConnectionCheck()
        {
            // 立即执行一次检测
            _ = CheckApiConnection();

            // 设置定时器，每5秒检测一次
            _apiCheckTimer = new System.Threading.Timer(async _ => await CheckApiConnection(), null,
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// 检测API连接状态
        /// </summary>
        private async Task CheckApiConnection()
        {
            try
            {
                // 调用认证服务的健康检查接口
                var isOnline = await _authService.CheckConnectionAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsApiOnline = isOnline;
                    ApiStatus = isOnline ? "✅ API连接正常" : "❌ API服务不可用";
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsApiOnline = false;
                    ApiStatus = $"❌ 连接失败: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public new void Dispose()
        {
            _apiCheckTimer?.Dispose();
            base.Dispose();
        }
    }
}