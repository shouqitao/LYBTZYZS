using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AutoMapper;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Auth.ViewModels
{
    /// <summary>
    /// 登录窗口视图模型 - UltraThink架构标准版本
    /// 完全使用AuthModule实现模块自包含，符合四层架构规范
    /// Layer 4: Desktop层，使用DTO模型，通过模块化服务与底层交互
    /// </summary>
    public class LoginViewModel : ModernViewModelBase
    {
        #region 私有字段

        private readonly AuthModule _authModule;
        private readonly IMapper _mapper;
        private LoginRequest _loginRequest = new();
        private string _apiStatus = "正在检测API连接...";

        #endregion

        #region 公共属性

        /// <summary>
        /// 登录命令 - 零警告初始化
        /// </summary>
        public DelegateCommand LoginCommand { get; }

        /// <summary>
        /// 密码变更命令 - 零警告初始化
        /// </summary>
        public DelegateCommand<PasswordBox> PasswordChangedCommand { get; }

        /// <summary>登录请求模型</summary>
        public LoginRequest LoginRequest
        {
            get => _loginRequest;
            set
            {
                if (SetProperty(ref _loginRequest, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>用户名</summary>
        public string Username
        {
            get => LoginRequest.Username;
            set
            {
                if (LoginRequest.Username != value)
                {
                    LoginRequest.Username = value;
                    RaisePropertyChanged(nameof(Username));
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>密码</summary>
        public string Password
        {
            get => LoginRequest.Password;
            set
            {
                if (LoginRequest.Password != value)
                {
                    LoginRequest.Password = value;
                    RaisePropertyChanged(nameof(Password));
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>记住我</summary>
        public bool RememberMe
        {
            get => LoginRequest.RememberMe;
            set
            {
                if (LoginRequest.RememberMe != value)
                {
                    LoginRequest.RememberMe = value;
                    RaisePropertyChanged(nameof(RememberMe));
                }
            }
        }

        /// <summary>是否有保存的密码</summary>
        public bool HasSavedPassword { get; set; } = false;

        /// <summary>API是否在线</summary>
        public bool IsApiOnline { get; set; } = true;

        /// <summary>API状态信息</summary>
        public string ApiStatus
        {
            get => _apiStatus;
            set => SetProperty(ref _apiStatus, value);
        }

        #endregion

        #region 构造函数

        public LoginViewModel(
            IEventAggregator eventAggregator,
            AuthModule authModule,
            IMapper mapper,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, errorHandlingService)
        {
            _authModule = authModule ?? throw new ArgumentNullException(nameof(authModule));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 零警告命令初始化
            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);
            PasswordChangedCommand = new DelegateCommand<PasswordBox>(OnPasswordChanged);

            // 初始化登录信息
            LoginRequest.UserAgent = "LYBT.WPF.Client";
            LoginRequest.LoginType = "Password";

            // 监听登出事件以清除登录状态消息
            EventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout, ThreadOption.UIThread);

            // 订阅模块服务事件
            _authModule.AuthStatusChanged += OnAuthStatusChanged;
            _authModule.ApiConnectionChanged += OnApiConnectionChanged;

            // 立即加载保存的凭据
            LoadSavedCredentials();

            // 模块服务会自动启动API连接监控
        }

        #endregion

        #region Command 重写

        /// <summary>
        /// 重写Command状态更新
        /// </summary>
        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            LoginCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令处理

        private bool CanExecuteLogin()
        {
            return !IsLoading && IsApiOnline && 
                   !string.IsNullOrWhiteSpace(LoginRequest.Username) && 
                   !string.IsNullOrWhiteSpace(LoginRequest.Password);
        }

        private async Task ExecuteLoginAsync()
        {
            var success = await ExecuteAsync(async () =>
            {
                // UltraThink四层架构：使用模块化服务执行登录
                var result = await _authModule.LoginAsync(LoginRequest);

                if (result.IsSuccess && result.Data != null)
                {
                    // 设置状态消息
                    if (result.Data.User?.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        SetStatus("超级管理员登录成功，正在跳转...");
                    }
                    else
                    {
                        SetStatus("用户登录成功，正在跳转...");
                    }

                    // 等待一下让用户看到成功消息
                    await Task.Delay(1000);

                    // 通过事件总线通知登录成功
                    EventAggregator.GetEvent<LoginSuccessEvent>().Publish();
                }
                else
                {
                    SetError(result.ErrorMessage ?? "登录失败，请检查用户名和密码");
                }
            }, "登录");
        }

        private void OnPasswordChanged(PasswordBox? passwordBox)
        {
            if (passwordBox != null)
            {
                Password = passwordBox.Password;
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 登出事件处理
        /// </summary>
        private void OnLogout()
        {
            ClearError();
            ClearStatus();

            // 清除登录状态
            LoginRequest = new LoginRequest
            {
                UserAgent = "LYBT.WPF.Client",
                LoginType = "Password"
            };

            // 登出时重新加载保存的凭据（如果有）
            LoadSavedCredentials();
        }

        /// <summary>
        /// 认证状态变更事件处理
        /// </summary>
        private void OnAuthStatusChanged(object? sender, (bool IsLoggedIn, string? Username, string? Message) e)
        {
            try
            {
                // 在UI线程上更新状态
                if (Application.Current?.Dispatcher != null)
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        UpdateAuthStatus(e);
                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateAuthStatus(e)));
                    }
                }
                else
                {
                    UpdateAuthStatus(e);
                }
            }
            catch (Exception ex)
            {
                _ = HandleErrorAsync("认证状态更新", ex, false);
            }
        }

        private void UpdateAuthStatus((bool IsLoggedIn, string? Username, string? Message) e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                if (e.IsLoggedIn)
                {
                    SetStatus(e.Message);
                }
                else
                {
                    SetError(e.Message);
                }
            }
        }

        /// <summary>
        /// API连接状态变更事件处理
        /// </summary>
        private void OnApiConnectionChanged(object? sender, (bool IsConnected, string Message) e)
        {
            try
            {
                // 在UI线程上更新API状态
                if (Application.Current?.Dispatcher != null)
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        UpdateApiConnectionStatus(e);
                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateApiConnectionStatus(e)));
                    }
                }
                else
                {
                    UpdateApiConnectionStatus(e);
                }
            }
            catch (Exception ex)
            {
                _ = HandleErrorAsync("API状态更新", ex, false);
            }
        }

        private void UpdateApiConnectionStatus((bool IsConnected, string Message) e)
        {
            IsApiOnline = e.IsConnected;
            ApiStatus = e.Message;
            RaisePropertyChanged(nameof(IsApiOnline));
            RaiseCanExecuteChanged();
        }

        #endregion

        #region 凭据管理

        /// <summary>
        /// 加载保存的凭据
        /// </summary>
        private void LoadSavedCredentials()
        {
            try
            {
                // UltraThink四层架构：使用模块化服务加载凭据
                var result = _authModule.LoadSavedCredentials();
                if (result.IsSuccess && result.Data != null)
                {
                    var savedRequest = result.Data;
                    LoginRequest.Username = savedRequest.Username;
                    LoginRequest.Password = savedRequest.Password;
                    LoginRequest.RememberMe = savedRequest.RememberMe;
                    HasSavedPassword = !string.IsNullOrEmpty(savedRequest.Password);
                }
                else
                {
                    HasSavedPassword = false;
                }

                // 触发属性变更通知
                RaisePropertyChanged(nameof(Username));
                RaisePropertyChanged(nameof(Password));
                RaisePropertyChanged(nameof(RememberMe));
                RaisePropertyChanged(nameof(HasSavedPassword));
            }
            catch (Exception ex)
            {
                // 静默处理错误，避免影响用户体验
                HasSavedPassword = false;
                _ = HandleErrorAsync("加载凭据", ex, false);
            }
        }

        #endregion

        #region 清理资源

        /// <summary>
        /// 清理资源
        /// </summary>
        protected override void OnDisposing()
        {
            // 取消事件订阅
            if (_authModule != null)
            {
                _authModule.AuthStatusChanged -= OnAuthStatusChanged;
                _authModule.ApiConnectionChanged -= OnApiConnectionChanged;
            }

            base.OnDisposing();
        }

        #endregion
    }
}