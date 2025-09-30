using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Models.ViewModels.Base;
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

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _hasSavedPassword;

        public LoginViewModel(
            IAuthService authService,
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory)
        {
            _authService = authService;
            _regionManager = regionManager;

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);
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

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }

        #endregion

        #region Methods

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

                // 导航到主窗口并设置内容区域
                _regionManager.RequestNavigate("ContentRegion", targetView);

                // 发布登录成功事件
                EventAggregator.GetEvent<UserLoggedInEvent>().Publish(new UserLoggedInEventArgs(user, token));
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
