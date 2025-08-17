using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AutoMapper;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Models.Auth;
using LYBT.Desktop.Services;
using Prism.Commands;
using Prism.Events;
using LYBT.Shared.Interfaces.Services;


namespace LYBT.Desktop.Auth.ViewModels {
    /// <summary>
    /// 登录窗口视图模型（UltraThink架构重构版）
    /// Layer 4: Desktop层，使用LoginInfo模型，通过AutoMapper与DTO交互
    /// </summary>
    public class LoginViewModel : ServiceViewModel {
        private readonly IAuthService _authService;
        private readonly ICredentialService _credentialService;
        private readonly IMapper _mapper;
        private System.Threading.Timer? _apiCheckTimer;

        private LoginInfo _loginInfo = new();
        private string _apiStatus = "正在检测API连接...";

        public DelegateCommand LoginCommand { get; }
        public DelegateCommand<PasswordBox>? PasswordChangedCommand { get; set; }

        /// <summary>登录信息模型</summary>
        public LoginInfo LoginInfo {
            get => _loginInfo;
            set {
                SetProperty(ref _loginInfo, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>用户名</summary>
        public string Username {
            get => LoginInfo.Username;
            set {
                LoginInfo.Username = value;
                OnPropertyChanged();
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>密码</summary>
        public string Password {
            get => LoginInfo.Password;
            set {
                LoginInfo.Password = value;
                OnPropertyChanged();
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>记住我</summary>
        public bool RememberMe {
            get => LoginInfo.RememberMe;
            set {
                LoginInfo.RememberMe = value;
                OnPropertyChanged();
            }
        }

        /// <summary>是否有保存的密码</summary>
        public bool HasSavedPassword {
            get => LoginInfo.HasSavedPassword;
            set {
                LoginInfo.HasSavedPassword = value;
                OnPropertyChanged();
            }
        }

        /// <summary>API是否在线</summary>
        public bool IsApiOnline {
            get => LoginInfo.IsApiOnline;
            set {
                LoginInfo.IsApiOnline = value;
                OnPropertyChanged();
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>API状态信息</summary>
        public string ApiStatus {
            get => _apiStatus;
            set => SetProperty(ref _apiStatus, value);
        }

        public LoginViewModel(IEventAggregator eventAggregator, IAuthenticationService authService, ICredentialService credentialService, IMapper mapper)
            : base(eventAggregator) {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化登录信息
            LoginInfo.UserAgent = "LYBT.WPF.Client";
            LoginInfo.LoginType = "Password";
            LoginInfo.ClientIp = GetLocalIPAddress();

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);

            // 监听登出事件以清除登录状态消息
            EventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout, ThreadOption.UIThread);

            // 立即加载保存的凭据
            LoadSavedCredentials();

            // 启动API连接检测
            StartApiConnectionCheck();
        }

        protected override void OnLoadingStateChanged(bool isLoading) {
            base.OnLoadingStateChanged(isLoading);
            LoginCommand.RaiseCanExecuteChanged();
        }

        private bool CanExecuteLogin() {
            return !IsLoading && LoginInfo.CanLogin;
        }

        private async Task ExecuteLoginAsync() {
            // 使用LoginInfo的验证方法
            var (isValid, errorMessage) = LoginInfo.Validate();
            if (!isValid) {
                ErrorMessage = errorMessage;
                return;
            }

            try {
                IsLoading = true;
                LoginInfo.IsLoggingIn = true;
                ClearError();

                // 使用AutoMapper将LoginInfo转换为LoginRequest
                var request = _mapper.Map<LoginRequest>(LoginInfo);
                var response = await _authService.LoginAsync(request);

                if (response.IsSuccess && response.Data != null) {
                    // 使用AutoMapper将LoginResponse合并到LoginInfo
                    var updatedLoginInfo = _mapper.Map<(LoginInfo, LoginResponse), LoginInfo>((LoginInfo, response.Data));
                    LoginInfo = updatedLoginInfo;

                    // 保存凭据（如果选择了记住我）
                    _credentialService.SaveCredentials(Username, Password, RememberMe);

                    // 设置状态消息
                    if (LoginInfo.User?.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true) {
                        StatusMessage = "超级管理员登录成功，正在跳转...";
                    } else {
                        StatusMessage = $"{LoginInfo.RoleDisplay}登录成功，正在跳转...";
                    }

                    // 等待一下让用户看到成功消息
                    await Task.Delay(1000);

                    // 通过事件总线通知登录成功
                    EventAggregator.GetEvent<LoginSuccessEvent>().Publish();
                } else {
                    LoginInfo.SetLoginFailure(response.ErrorMessage ?? "登录失败，请检查用户名和密码");
                    ErrorMessage = LoginInfo.ErrorMessage;
                }
            } catch (Exception ex) {
                LoginInfo.SetLoginFailure($"登录异常：{ex.Message}");
                HandleError("登录", ex);
            } finally {
                IsLoading = false;
                LoginInfo.IsLoggingIn = false;
            }
        }

        /// <summary>
        /// 登出事件处理
        /// </summary>
        private void OnLogout() {
            ClearError();
            ClearStatus();

            // 清除登录状态
            LoginInfo.ClearLoginState();

            // 登出时重新加载保存的凭据（如果有）
            LoadSavedCredentials();
        }

        /// <summary>
        /// 加载保存的凭据
        /// </summary>
        private void LoadSavedCredentials() {
            try {
                var savedCredentials = _credentialService.LoadCredentials();
                if (savedCredentials != null) {
                    LoginInfo.Username = savedCredentials.Username;
                    LoginInfo.Password = savedCredentials.Password;
                    LoginInfo.RememberMe = savedCredentials.RememberMe;
                    LoginInfo.HasSavedPassword = !string.IsNullOrEmpty(savedCredentials.Password);
                } else {
                    LoginInfo.HasSavedPassword = false;
                }

                // 触发属性变更通知
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Password));
                OnPropertyChanged(nameof(RememberMe));
                OnPropertyChanged(nameof(HasSavedPassword));
            } catch (Exception) {
                // 静默处理错误，避免影响用户体验
                LoginInfo.HasSavedPassword = false;
            }
        }

        private string GetLocalIPAddress() {
            try {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList) {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                        return ip.ToString();
                    }
                }
            } catch {
                // 忽略错误
            }
            return "127.0.0.1";
        }

        /// <summary>
        /// 启动API连接检测
        /// </summary>
        private void StartApiConnectionCheck() {
            // 立即执行一次检测
            _ = CheckApiConnection();

            // 设置定时器，每5秒检测一次
            _apiCheckTimer = new System.Threading.Timer(async _ => await CheckApiConnection(), null,
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// 检测API连接状态
        /// </summary>
        private async Task CheckApiConnection() {
            try {
                // 检查_authService是否为null
                if (_authService == null) {
                    UpdateApiStatus(false, "❌ 认证服务未初始化");
                    return;
                }

                // 调用认证服务的健康检查接口
                var isOnline = await _authService.CheckConnectionAsync();

                // 安全地更新UI
                UpdateApiStatus(isOnline, isOnline ? "✅ API连接正常" : "❌ API服务不可用");
            } catch (Exception ex) {
                UpdateApiStatus(false, $"❌ 连接失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全地更新API状态
        /// </summary>
        private void UpdateApiStatus(bool isOnline, string statusMessage) {
            // 检查是否在UI线程
            if (Application.Current?.Dispatcher != null) {
                if (Application.Current.Dispatcher.CheckAccess()) {
                    // 已在UI线程，直接更新
                    LoginInfo.IsApiOnline = isOnline;
                    ApiStatus = statusMessage;
                    OnPropertyChanged(nameof(IsApiOnline));
                } else {
                    // 不在UI线程，使用Dispatcher
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        LoginInfo.IsApiOnline = isOnline;
                        ApiStatus = statusMessage;
                        OnPropertyChanged(nameof(IsApiOnline));
                    }));
                }
            } else {
                // 如果Application.Current为null（设计时或单元测试），直接设置
                LoginInfo.IsApiOnline = isOnline;
                ApiStatus = statusMessage;
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public new void Dispose() {
            _apiCheckTimer?.Dispose();
            base.Dispose();
        }
    }
}