using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Interfaces.Services;
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
    /// 登录视图模型 - 实现基于角色的导航（已统一架构）
    /// </summary>
    public class LoginViewModel : UnifiedViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IApiHealthCheckService? _apiHealthCheckService;
        private readonly LYBT.Desktop.Services.Business.IUsernameStorageService? _usernameStorage;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private bool _hasSavedPassword;
        private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
        private string _apiStatusMessage = "正在检查连接...";

        public LoginViewModel(
            IAuthService authService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IApiHealthCheckService? apiHealthCheckService = null,
            LYBT.Desktop.Services.Business.IUsernameStorageService? usernameStorage = null)
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
            _authService = authService;
            _apiHealthCheckService = apiHealthCheckService;
            _usernameStorage = usernameStorage;

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);

            // Issue #861: 在后台线程加载保存的用户名
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // 短暂延迟,让 UI 先完成初始化
                await LoadSavedUsernameAsync();
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
        /// 加载保存的用户名 - Issue #861
        /// </summary>
        private async Task LoadSavedUsernameAsync()
        {
            try
            {
                if (_usernameStorage == null)
                {
                    return;
                }

                var savedUsername = await _usernameStorage.GetSavedUsernameAsync();
                var isRememberMeEnabled = await _usernameStorage.IsRememberMeEnabledAsync();

                if (!string.IsNullOrEmpty(savedUsername))
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Username = savedUsername;
                        RememberMe = isRememberMeEnabled;
                        Logger.LogInformation("已自动填充用户名: {Username}", savedUsername);
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载保存的用户名失败");
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
                    Username = Username,
                    Password = Password,
                    RememberMe = RememberMe
                };

                // 调用认证服务
                var response = await _authService.LoginAsync(loginRequest);

                if (response.IsSuccess && response.Data != null)
                {
                    StatusMessage = "登录成功，正在跳转...";

                    // 保存Token和用户信息
                    await _authService.SaveAuthenticationAsync(response.Data);

                    // Issue #861: 保存用户名（如果勾选了"记住用户名"）
                    if (_usernameStorage != null)
                    {
                        await _usernameStorage.SaveUsernameAsync(Username, RememberMe);
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
                string targetView = role switch
                {
                    UserRole.Admin => "AdminWorkstationView",
                    UserRole.Doctor => "ClinicalWorkstationView",
                    _ => "ClinicalWorkstationView" // 默认导航到诊疗工作台
                };

                Logger.LogInformation($"根据角色 {role} 导航到 {targetView}");

                // Issue #877 修复步骤2: 先发布登录成功事件，让 Shell 更新 UI 状态
                Logger.LogInformation("📢 发布 LoginSuccessEvent，触发 Shell UI 更新");
                EventAggregator.GetEvent<LoginSuccessEvent>().Publish(user);

                // Issue #877 修复步骤3: 延迟导航，等待 UI 绑定生效
                // 延迟 100ms 确保 MainWindow.IsLoggedIn 更新后，ContentRegion 已变为可见
                _ = Task.Delay(100).ContinueWith(_ =>
                {
                    Logger.LogInformation("⏰ 延迟完成，开始导航到 {TargetView}", targetView);

                    // 在 UI 线程上执行导航
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        RegionManager.RequestNavigate("ContentRegion", targetView, navigationResult =>
                        {
                            if (navigationResult.Result != true)
                            {
                                Logger.LogError("❌ 导航失败: {Error}", navigationResult.Error?.Message);
                                ErrorMessage = $"导航失败：{navigationResult.Error?.Message}";
                            }
                            else
                            {
                                Logger.LogInformation("✅ 导航成功到 {TargetView}", targetView);
                            }
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到工作台时发生错误");
                ErrorMessage = "导航失败：" + ex.Message;
            }
        }

        #endregion
    }
}
