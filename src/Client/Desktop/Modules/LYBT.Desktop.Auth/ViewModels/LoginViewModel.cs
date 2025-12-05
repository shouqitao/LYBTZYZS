using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Auth.Models;
using LYBT.Desktop.Auth.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Interfaces;
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
    /// <summary>登录视图模型 - 实现基于角色的导航（ADR-002合规）</summary>
    public class LoginViewModel : UnifiedViewModelBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ITokenStorageService _tokenStorage;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IUsernameStorageService? _usernameStorage;
        private readonly ISecureCredentialStorage? _credentialStorage;
        private readonly IConnectionSettingsService? _connectionSettingsService;
        private readonly ICommonDialogService? _dialogService;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private bool _rememberPassword;
        private bool _hasSavedPassword;
        private string? _savedUsername;
        private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
        private string _apiStatusMessage = "正在检查连接...";
        private ConnectionMode _connectionMode;

        public string Username
        {
            get => _username;
            set
            {
                var shouldClearPassword = _savedUsername != null && !string.IsNullOrEmpty(_savedUsername) && !string.IsNullOrEmpty(value) && value != _savedUsername && !string.IsNullOrEmpty(_password);
                if (SetProperty(ref _username, value)) { if (shouldClearPassword) Password = string.Empty; (LoginCommand as DelegateCommand)?.RaiseCanExecuteChanged(); }
            }
        }

        public string Password { get => _password; set { SetProperty(ref _password, value); (LoginCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } }
        public bool RememberMe { get => _rememberMe; set => SetProperty(ref _rememberMe, value); }
        public bool RememberPassword { get => _rememberPassword; set { if (SetProperty(ref _rememberPassword, value) && value && !RememberMe) RememberMe = true; } }
        public bool HasMessage => !string.IsNullOrWhiteSpace(StatusMessage) || !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool HasSavedPassword { get => _hasSavedPassword; set => SetProperty(ref _hasSavedPassword, value); }
        public ApiHealthStatus ApiStatus { get => _apiStatus; set { if (SetProperty(ref _apiStatus, value)) RaisePropertyChanged(nameof(IsApiUnhealthy)); } }
        public string ApiStatusMessage { get => _apiStatusMessage; set => SetProperty(ref _apiStatusMessage, value); }
        public bool IsApiUnhealthy => ApiStatus == ApiHealthStatus.Unhealthy;

        public ConnectionMode ConnectionMode
        {
            get => _connectionMode;
            set
            {
                if (SetProperty(ref _connectionMode, value))
                {
                    RaisePropertyChanged(nameof(IsRemoteModeSelected)); RaisePropertyChanged(nameof(IsLocalModeSelected)); RaisePropertyChanged(nameof(ConnectionModeDisplay));
                    _connectionSettingsService?.SaveConnectionMode(value); UpdateConnectionStatus();
                }
            }
        }

        public bool IsRemoteModeSelected { get => ConnectionMode == ConnectionMode.Remote; set { if (value && ConnectionMode != ConnectionMode.Remote) ConnectionMode = ConnectionMode.Remote; } }
        public bool IsLocalModeSelected { get => ConnectionMode == ConnectionMode.Local; set { if (value && ConnectionMode != ConnectionMode.Local) ConnectionMode = ConnectionMode.Local; } }
        public string ConnectionModeDisplay => ConnectionMode switch { ConnectionMode.Remote => "远程模式 - 连接到WebAPI服务", ConnectionMode.Local => "本地模式 - 使用本地数据库（v2.0）", _ => "未知模式" };
        public ICommand LoginCommand { get; }

        /// <summary>
        /// 关闭应用程序命令
        /// remove-titlebar-add-close-button: 仅在登录界面可用的关闭按钮
        /// </summary>
        public ICommand CloseApplicationCommand { get; }

        public LoginViewModel(
            IAuthenticationService authService,
            ITokenStorageService tokenStorage,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IApplicationStateService applicationStateService,
            IUsernameStorageService? usernameStorage = null,
            ISecureCredentialStorage? credentialStorage = null,
            IConnectionSettingsService? connectionSettingsService = null,
            ICommonDialogService? dialogService = null)
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
            _usernameStorage = usernameStorage; _credentialStorage = credentialStorage; _connectionSettingsService = connectionSettingsService;
            _dialogService = dialogService;

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), () => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && !IsLoading);
            CloseApplicationCommand = new DelegateCommand(async () => await ExecuteCloseApplicationAsync());
            _connectionMode = _connectionSettingsService?.GetConnectionMode() ?? ConnectionMode.Remote;

            _ = Task.Run(async () => { await Task.Delay(100); await LoadSavedCredentialsAsync(); await LoadApiStatusFromStateServiceAsync(); });
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _ = Task.Run(async () => await TryAutoLoginWithTokenAsync());
        }

        private async Task LoadApiStatusFromStateServiceAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (_applicationStateService.IsApiHealthy) { ApiStatus = ApiHealthStatus.Healthy; ApiStatusMessage = "WebAPI 已连接"; }
                    else { ApiStatus = ApiHealthStatus.Unhealthy; ApiStatusMessage = $"WebAPI 连接失败: {_applicationStateService.ConnectionStatus}"; }
                });
            }
            catch (Exception ex) { await Application.Current.Dispatcher.InvokeAsync(() => { ApiStatus = ApiHealthStatus.Unhealthy; ApiStatusMessage = $"加载API状态失败: {ex.Message}"; }); }
        }

        private async Task TryAutoLoginWithTokenAsync()
        {
            try
            {
                var token = await _tokenStorage.GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return;
                var validationResult = await _authService.ValidateTokenAsync(token);
                if (!validationResult.IsSuccess || validationResult.Data == null || !validationResult.Data.IsValid) { await ClearInvalidTokenAsync(); return; }
                var loginResponse = await _tokenStorage.GetLoginResponseAsync();
                if (loginResponse?.User == null) { await ClearInvalidTokenAsync(); return; }
                await Application.Current.Dispatcher.InvokeAsync(() => EventAggregator.GetEvent<LoginSuccessEvent>().Publish(loginResponse.User));
            }
            catch { await ClearInvalidTokenAsync(); }
        }

        private async Task ClearInvalidTokenAsync()
        {
            try { await _tokenStorage.ClearAuthenticationAsync(); }
            catch (Exception ex) { Logger.LogError(ex, "清除无效Token时发生错误"); }
        }

        private async Task LoadSavedCredentialsAsync()
        {
            try
            {
                if (_credentialStorage != null)
                {
                    var credentials = await _credentialStorage.LoadCredentialsAsync();
                    var isRememberPasswordEnabled = await _credentialStorage.IsRememberPasswordEnabledAsync();
                    if (credentials.HasValue && !string.IsNullOrEmpty(credentials.Value.Username))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() => { _savedUsername = credentials.Value.Username; Username = credentials.Value.Username; Password = credentials.Value.Password; RememberMe = true; RememberPassword = isRememberPasswordEnabled; });
                        return;
                    }
                }
                if (_usernameStorage != null)
                {
                    var savedUsername = await _usernameStorage.GetSavedUsernameAsync();
                    var isRememberMeEnabled = await _usernameStorage.IsRememberMeEnabledAsync();
                    if (!string.IsNullOrEmpty(savedUsername)) await Application.Current.Dispatcher.InvokeAsync(() => { _savedUsername = savedUsername; Username = savedUsername; RememberMe = isRememberMeEnabled; });
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "加载保存的凭据失败"); }
        }

        private void UpdateConnectionStatus()
        {
            if (ConnectionMode == ConnectionMode.Local) { ApiStatusMessage = "本地模式 - 无需连接API（v2.0功能）"; ApiStatus = ApiHealthStatus.Healthy; }
            else if (ApiStatus == ApiHealthStatus.Checking) ApiStatusMessage = "正在检查远程API连接...";
        }

        private async Task ExecuteLoginAsync()
        {
            try
            {
                IsLoading = true; ErrorMessage = string.Empty; StatusMessage = "正在登录...";
                if (ConnectionMode == ConnectionMode.Local) { ErrorMessage = "本地模式暂未实现，请切换到\"远程模式\""; return; }
                var loginRequest = new LoginRequest { UserName = Username, Password = Password, RememberMe = RememberMe };
                var response = await _authService.LoginAsync(loginRequest);
                if (response.IsSuccess && response.Data != null)
                {
                    StatusMessage = "登录成功，正在跳转...";
                    await _tokenStorage.SaveAuthenticationAsync(response.Data, RememberMe);
                    if (_credentialStorage != null && RememberPassword) await _credentialStorage.SaveCredentialsAsync(Username, Password, RememberPassword);
                    else
                    {
                        if (_usernameStorage != null && RememberMe && !RememberPassword) await _usernameStorage.SaveUsernameAsync(Username, RememberMe);
                        if (_credentialStorage != null && !RememberPassword) await _credentialStorage.ClearCredentialsAsync();
                    }
                    EventAggregator.GetEvent<LoginSuccessEvent>().Publish(response.Data.User);
                }
                else { ErrorMessage = response.Message ?? "登录失败，请检查用户名和密码"; Password = string.Empty; }
            }
            catch (Exception ex) { Logger.LogError(ex, "登录过程中发生错误"); ErrorMessage = "登录失败：" + ex.Message; Password = string.Empty; }
            finally { IsLoading = false; StatusMessage = string.Empty; }
        }

        /// <summary>
        /// 关闭应用程序
        /// remove-titlebar-add-close-button: 使用ICommonDialogService显示确认框后退出程序
        /// </summary>
        private async Task ExecuteCloseApplicationAsync()
        {
            var confirmed = _dialogService != null
                ? await _dialogService.ShowConfirmAsync("确定要退出程序吗？", "退出确认")
                : MessageBox.Show("确定要退出程序吗？", "退出确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (confirmed)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
