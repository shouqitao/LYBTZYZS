using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Auth.ViewModels
{
    /// <summary>
    /// 登录视图模型 - 实现基于角色的导航（ADR-002合规）
    /// 使用Foundation层的IAuthenticationService（Infrastructure Service）
    /// </summary>
    public class LoginViewModel : UnifiedViewModelBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ITokenStorageService _tokenStorage;
        private readonly IApiHealthCheckService? _apiHealthCheckService;
        private readonly IUsernameStorageService? _usernameStorage;
        private readonly ISecureCredentialStorage? _credentialStorage; // Issue #1246: 密码加密存储

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private bool _rememberPassword; // Issue #1246: 记住密码
        private bool _hasSavedPassword;
        private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
        private string _apiStatusMessage = "正在检查连接...";

        public LoginViewModel(
            IAuthenticationService authService,
            ITokenStorageService tokenStorage,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IApiHealthCheckService? apiHealthCheckService = null,
            IUsernameStorageService? usernameStorage = null,
            ISecureCredentialStorage? credentialStorage = null) // Issue #1246: 密码加密存储服务
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
            _authService = authService;
            _tokenStorage = tokenStorage;
            _apiHealthCheckService = apiHealthCheckService;
            _usernameStorage = usernameStorage;
            _credentialStorage = credentialStorage; // Issue #1246

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);

            // Issue #861 & #1246: 在后台线程加载保存的凭据（用户名 + 密码）
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // 短暂延迟,让 UI 先完成初始化
                await LoadSavedCredentialsAsync();
                await CheckApiHealthAsyncSafe();
            });
        }

        /// <summary>
        /// 安全启动健康检查(fire-and-forget)
        /// </summary>
        private async Task CheckApiHealthAsyncSafe()
        {
            try
            {
                await CheckApiHealthAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "健康检查过程中发生错误");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ApiStatus = ApiHealthStatus.Unhealthy;
                    ApiStatusMessage = $"健康检查失败: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// 加载保存的凭据 - Issue #861 & #1246
        /// 优先加载"记住密码"的凭据（含用户名+密码），否则仅加载用户名
        /// </summary>
        private async Task LoadSavedCredentialsAsync()
        {
            try
            {
                // 1. 优先尝试加载"记住密码"的完整凭据（Issue #1246）
                if (_credentialStorage != null)
                {
                    var credentials = await _credentialStorage.LoadCredentialsAsync();
                    var isRememberPasswordEnabled = await _credentialStorage.IsRememberPasswordEnabledAsync();

                    if (credentials.HasValue && !string.IsNullOrEmpty(credentials.Value.Username))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Username = credentials.Value.Username;
                            Password = credentials.Value.Password;
                            RememberMe = true; // 记住密码时必然记住用户名
                            RememberPassword = isRememberPasswordEnabled;
                            Logger.LogInformation("已自动填充用户名和密码（DPAPI解密）: {UserName}", credentials.Value.Username);
                        });
                        return; // 成功加载密码后直接返回
                    }
                }

                // 2. 降级：仅加载"记住用户名"（Issue #861）
                if (_usernameStorage != null)
                {
                    var savedUsername = await _usernameStorage.GetSavedUsernameAsync();
                    var isRememberMeEnabled = await _usernameStorage.IsRememberMeEnabledAsync();

                    if (!string.IsNullOrEmpty(savedUsername))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Username = savedUsername;
                            RememberMe = isRememberMeEnabled;
                            Logger.LogInformation("已自动填充用户名: {UserName}", savedUsername);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载保存的凭据失败");
            }
        }

        #region Properties

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                (LoginCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                (LoginCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        /// <summary>
        /// 记住密码 - Issue #1246
        /// 勾选时自动勾选"记住用户名"
        /// </summary>
        public bool RememberPassword
        {
            get => _rememberPassword;
            set
            {
                if (SetProperty(ref _rememberPassword, value))
                {
                    // 勾选"记住密码"时，自动勾选"记住用户名"
                    if (value && !RememberMe)
                    {
                        RememberMe = true;
                    }
                }
            }
        }

        public bool HasMessage => !string.IsNullOrWhiteSpace(StatusMessage) || !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool HasSavedPassword
        {
            get => _hasSavedPassword;
            set => SetProperty(ref _hasSavedPassword, value);
        }

        public ApiHealthStatus ApiStatus
        {
            get => _apiStatus;
            set => SetProperty(ref _apiStatus, value);
        }

        public string ApiStatusMessage
        {
            get => _apiStatusMessage;
            set => SetProperty(ref _apiStatusMessage, value);
        }

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }

        #endregion

        #region Methods

        private async Task CheckApiHealthAsync()
        {
            if (_apiHealthCheckService == null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ApiStatus = ApiHealthStatus.Unhealthy;
                    ApiStatusMessage = "健康检查服务未配置";
                });
                return;
            }

            try
            {
                var status = await _apiHealthCheckService.CheckHealthAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ApiStatus = status;
                    ApiStatusMessage = status switch
                    {
                        ApiHealthStatus.Healthy => "WebAPI 已连接",
                        ApiHealthStatus.Unhealthy => $"WebAPI 连接失败: {_apiHealthCheckService.LastErrorMessage}",
                        _ => "正在检查连接..."
                    };
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "健康检查失败");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ApiStatus = ApiHealthStatus.Unhealthy;
                    ApiStatusMessage = $"健康检查异常: {ex.Message}";
                });
            }
        }

        private bool CanExecuteLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !IsLoading;
        }

        private async Task ExecuteLoginAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                StatusMessage = "正在登录...";

                // 构造登录请求
                var loginRequest = new LoginRequest
                {
                    UserName = Username,
                    Password = Password,
                    RememberMe = RememberMe
                };

                // 调用认证服务
                var response = await _authService.LoginAsync(loginRequest);

                if (response.IsSuccess && response.Data != null)
                {
                    StatusMessage = "登录成功，正在跳转...";

                    // 保存Token和用户信息
                    await _tokenStorage.SaveAuthenticationAsync(response.Data, RememberMe);

                    // Issue #1246: 保存凭据（用户名 + 密码）如果勾选了"记住密码"
                    if (_credentialStorage != null && RememberPassword)
                    {
                        await _credentialStorage.SaveCredentialsAsync(Username, Password, RememberPassword);
                        Logger.LogInformation("凭据已保存（DPAPI加密）");
                    }
                    else
                    {
                        // Issue #861: 仅保存用户名（如果勾选了"记住用户名"但未勾选"记住密码"）
                        if (_usernameStorage != null && RememberMe && !RememberPassword)
                        {
                            await _usernameStorage.SaveUsernameAsync(Username, RememberMe);
                        }

                        // 如果取消勾选"记住密码"，清除已保存的密码
                        if (_credentialStorage != null && !RememberPassword)
                        {
                            await _credentialStorage.ClearCredentialsAsync();
                        }
                    }

                    // 根据角色导航到对应的工作台
                    NavigateBasedOnRole(response.Data.User.Role, response.Data.User, response.Data.Token);
                }
                else
                {
                    ErrorMessage = response.Message ?? "登录失败，请检查用户名和密码";
                    Password = string.Empty; // 清空密码
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "登录过程中发生错误");
                ErrorMessage = "登录失败：" + ex.Message;
                Password = string.Empty;
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        private void NavigateBasedOnRole(UserRole role, UserDto user, string token)
        {
            try
            {
                // Bug #1524修复：移除LoginViewModel中的导航逻辑
                // 原因：与MainWindowViewModel.LoadMainContent()导航冲突
                // - LoginViewModel在100ms后导航到ClinicalWorkstationView（旧界面）
                // - MainWindowViewModel在LoginSuccessEvent触发时导航到HomeView（新界面）
                // - 导致最终显示旧界面（ClinicalWorkstationView覆盖HomeView）
                //
                // 解决方案：登录后的导航完全交给MainWindowViewModel.LoadMainContent()处理
                // - Epic #1494设计要求：登录后始终显示HomeView（统一医生主页）
                // - MainWindowViewModel会根据角色加载对应模块（ClinicalWorkstationModule等）
                // - 用户点击"开始看诊"按钮进入医案流程

                Logger.LogInformation($"用户 {user.UserName}（角色: {role}）登录成功");

                // Issue #877: 发布登录成功事件，触发 Shell UI 更新和导航
                Logger.LogInformation("📢 发布 LoginSuccessEvent，触发 MainWindowViewModel 处理后续导航");
                EventAggregator.GetEvent<LoginSuccessEvent>().Publish(user);

                // 导航逻辑由 MainWindowViewModel.OnLoginSuccess() 和 LoadMainContent() 处理
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "发布登录成功事件时发生错误");
                ErrorMessage = "登录后处理失败：" + ex.Message;
            }
        }

        #endregion
    }
}
