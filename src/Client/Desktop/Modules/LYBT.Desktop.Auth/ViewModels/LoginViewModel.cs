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
    /// 登录视图模型 - 实现基于角色的导航
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IRegionManager _regionManager;
        private readonly IApiHealthCheckService? _apiHealthCheckService;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasSavedPassword;
        private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
        private string _apiStatusMessage = "正在检查连接...";

        public LoginViewModel(
            IAuthService authService,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IApiHealthCheckService? apiHealthCheckService = null)
            : base(eventAggregator, loggerFactory)
        {
            _authService = authService;
            _regionManager = regionManager;
            _apiHealthCheckService = apiHealthCheckService;

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);

            // 异步启动健康检查（不阻塞 UI）
            _ = Task.Run(async () => await CheckApiHealthAsync());
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

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public new string StatusMessage
        {
            get => _statusMessage;
            set
            {
                SetProperty(ref _statusMessage, value);
                RaisePropertyChanged(nameof(HasMessage));
            }
        }

        public new string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                RaisePropertyChanged(nameof(HasMessage));
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
                ApiStatus = ApiHealthStatus.Unhealthy;
                ApiStatusMessage = "健康检查服务未配置";
                return;
            }

            try
            {
                var status = await _apiHealthCheckService.CheckHealthAsync();
                ApiStatus = status;

                ApiStatusMessage = status switch
                {
                    ApiHealthStatus.Healthy => "WebAPI 已连接",
                    ApiHealthStatus.Unhealthy => $"WebAPI 连接失败: {_apiHealthCheckService.LastErrorMessage}",
                    _ => "正在检查连接..."
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "健康检查失败");
                ApiStatus = ApiHealthStatus.Unhealthy;
                ApiStatusMessage = $"健康检查异常: {ex.Message}";
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

                // 导航到主窗口并设置内容区域（添加回调验证）
                _regionManager.RequestNavigate("ContentRegion", targetView, navigationResult =>
                {
                    if (navigationResult.Result != true)
                    {
                        Logger.LogError("导航失败: {Error}", navigationResult.Error?.Message);
                        ErrorMessage = $"导航失败：{navigationResult.Error?.Message}";
                    }
                    else
                    {
                        Logger.LogInformation("导航成功到 {TargetView}", targetView);
                    }
                });

                // 发布登录成功事件 - 修复 Issue #848：使用 LoginSuccessEvent 触发主窗口 UI 更新
                EventAggregator.GetEvent<LoginSuccessEvent>().Publish(user);
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
