using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AutoMapper;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Models.Auth;
using LYBT.Desktop.Auth.Services.Interfaces;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Auth.ViewModels
{
    /// <summary>
    /// 登录窗口视图模型 - UltraThink架构标准版本
    /// 完全使用IAuthModuleService实现模块自包含，符合四层架构规范
    /// Layer 4: Desktop层，使用LoginInfo模型，通过模块化服务与底层交互
    /// </summary>
    public class LoginViewModel : ServiceViewModel
    {
        #region 私有字段

        private readonly IAuthModuleService _authModuleService;
        private readonly IMapper _mapper;
        private LoginInfo _loginInfo = new();
        private string _apiStatus = "正在检测API连接...";

        #endregion

        #region 公共属性

        public DelegateCommand LoginCommand { get; }
        public DelegateCommand<PasswordBox>? PasswordChangedCommand { get; set; }

        /// <summary>登录信息模型</summary>
        public LoginInfo LoginInfo
        {
            get => _loginInfo;
            set
            {
                SetProperty(ref _loginInfo, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>用户名</summary>
        public string Username
        {
            get => LoginInfo.Username;
            set
            {
                LoginInfo.Username = value;
                OnPropertyChanged();
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>密码</summary>
        public string Password
        {
            get => LoginInfo.Password;
            set
            {
                LoginInfo.Password = value;
                OnPropertyChanged();
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>记住我</summary>
        public bool RememberMe
        {
            get => LoginInfo.RememberMe;
            set
            {
                LoginInfo.RememberMe = value;
                OnPropertyChanged();
            }
        }

        /// <summary>是否有保存的密码</summary>
        public bool HasSavedPassword
        {
            get => LoginInfo.HasSavedPassword;
            set
            {
                LoginInfo.HasSavedPassword = value;
                OnPropertyChanged();
            }
        }

        /// <summary>API是否在线</summary>
        public bool IsApiOnline
        {
            get => LoginInfo.IsApiOnline;
            set
            {
                LoginInfo.IsApiOnline = value;
                OnPropertyChanged();
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

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
            IAuthModuleService authModuleService,
            IMapper mapper)
            : base(eventAggregator)
        {
            _authModuleService = authModuleService ?? throw new ArgumentNullException(nameof(authModuleService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化登录信息
            LoginInfo.UserAgent = "LYBT.WPF.Client";
            LoginInfo.LoginType = "Password";
            LoginInfo.ClientIp = _authModuleService.GetClientIpAddress();

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);

            // 监听登出事件以清除登录状态消息
            EventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout, ThreadOption.UIThread);

            // 订阅模块服务事件
            _authModuleService.AuthStatusChanged += OnAuthStatusChanged;
            _authModuleService.ApiConnectionChanged += OnApiConnectionChanged;

            // 立即加载保存的凭据
            LoadSavedCredentials();

            // 模块服务会自动启动API连接监控
        }

        #endregion

        #region 命令处理

        protected override void OnLoadingStateChanged(bool isLoading)
        {
            base.OnLoadingStateChanged(isLoading);
            LoginCommand.RaiseCanExecuteChanged();
        }

        private bool CanExecuteLogin()
        {
            return !IsLoading && LoginInfo.CanLogin;
        }

        private async Task ExecuteLoginAsync()
        {
            try
            {
                IsLoading = true;
                LoginInfo.IsLoggingIn = true;
                ClearError();

                // UltraThink四层架构：使用模块化服务执行登录
                var result = await _authModuleService.LoginAsync(LoginInfo);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新登录信息
                    LoginInfo = result.Data;

                    // 设置状态消息
                    if (LoginInfo.User?.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        StatusMessage = "超级管理员登录成功，正在跳转...";
                    }
                    else
                    {
                        StatusMessage = $"{LoginInfo.RoleDisplay}登录成功，正在跳转...";
                    }

                    // 等待一下让用户看到成功消息
                    await Task.Delay(1000);

                    // 通过事件总线通知登录成功
                    EventAggregator.GetEvent<LoginSuccessEvent>().Publish();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "登录失败，请检查用户名和密码";
                }
            }
            catch (Exception ex)
            {
                HandleError("登录", ex);
            }
            finally
            {
                IsLoading = false;
                LoginInfo.IsLoggingIn = false;
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
            LoginInfo.ClearLoginState();

            // 登出时重新加载保存的凭据（如果有）
            LoadSavedCredentials();
        }

        /// <summary>
        /// 认证状态变更事件处理
        /// </summary>
        private void OnAuthStatusChanged(object? sender, AuthStatusChangedEventArgs e)
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
                HandleError("认证状态更新", ex);
            }
        }

        private void UpdateAuthStatus(AuthStatusChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.StatusMessage))
            {
                if (e.IsLoggedIn)
                {
                    StatusMessage = e.StatusMessage;
                }
                else
                {
                    ErrorMessage = e.StatusMessage;
                }
            }
        }

        /// <summary>
        /// API连接状态变更事件处理
        /// </summary>
        private void OnApiConnectionChanged(object? sender, ApiConnectionChangedEventArgs e)
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
                HandleError("API状态更新", ex);
            }
        }

        private void UpdateApiConnectionStatus(ApiConnectionChangedEventArgs e)
        {
            IsApiOnline = e.IsConnected;
            ApiStatus = e.StatusMessage;
            OnPropertyChanged(nameof(IsApiOnline));
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
                var result = _authModuleService.LoadSavedCredentials();
                if (result.IsSuccess && result.Data != null)
                {
                    var savedInfo = result.Data;
                    LoginInfo.Username = savedInfo.Username;
                    LoginInfo.Password = savedInfo.Password;
                    LoginInfo.RememberMe = savedInfo.RememberMe;
                    LoginInfo.HasSavedPassword = savedInfo.HasSavedPassword;
                }
                else
                {
                    LoginInfo.HasSavedPassword = false;
                }

                // 触发属性变更通知
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Password));
                OnPropertyChanged(nameof(RememberMe));
                OnPropertyChanged(nameof(HasSavedPassword));
            }
            catch (Exception ex)
            {
                // 静默处理错误，避免影响用户体验
                LoginInfo.HasSavedPassword = false;
                HandleError("加载凭据", ex);
            }
        }

        #endregion

        #region 清理资源

        /// <summary>
        /// 清理资源
        /// </summary>
        public new void Dispose()
        {
            // 取消事件订阅
            if (_authModuleService != null)
            {
                _authModuleService.AuthStatusChanged -= OnAuthStatusChanged;
                _authModuleService.ApiConnectionChanged -= OnApiConnectionChanged;
            }

            base.Dispose();
        }

        #endregion
    }
}