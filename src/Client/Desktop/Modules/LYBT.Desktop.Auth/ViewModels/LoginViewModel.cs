using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Contracts;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;

namespace LYBT.Desktop.Auth.ViewModels
{
    /// <summary>
    /// 登录视图模型 - 使用LoginCoordinator编排登录流程
    /// OpenSpec: refactor-viewmodel-base-classes - 从UnifiedViewModelBase迁移到NavigableViewModelBase
    /// OpenSpec: remove-secure-credential-storage - 移除废弃的SecureCredentialStorage依赖
    /// </summary>
    public class LoginViewModel : NavigableViewModelBase
    {
        private readonly ILoginCoordinator _loginCoordinator;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IUsernameStorageService? _usernameStorage;
        private readonly ICredentialVault? _credentialVault;
        private CancellationTokenSource? _cts;

        private string _username = string.Empty;
        private string _password = string.Empty;

        // OpenSpec: simplify-login-options - 记住账号+记住密码
        private bool _rememberUsername;
        private bool _rememberPassword;
        private bool _hasSavedPassword;
        private string? _savedUsername;
        private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
        private string _apiStatusMessage = "正在检查连接...";

        // US-SYNC-008: 连接模式选择 + 切换前置检查
        private ConnectionMode _selectedConnectionMode = ConnectionMode.Remote;
        private readonly IModeSwitchValidator? _modeSwitchValidator;

        public string Username
        {
            get => _username;
            set
            {
                var shouldClearPassword = _savedUsername != null && !string.IsNullOrEmpty(_savedUsername) && !string.IsNullOrEmpty(value) && value != _savedUsername && !string.IsNullOrEmpty(_password);
                if (SetProperty(ref _username, value))
                {
                    if (shouldClearPassword)
                    {
                        Password = string.Empty;
                        HasSavedPassword = false;
                    }
                    (LoginCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string Password { get => _password; set { SetProperty(ref _password, value); (LoginCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } }

        #region 记住账号+记住密码 (OpenSpec: simplify-login-options)

        /// <summary>
        /// 记住账号 - 勾选后保存用户名，下次启动自动填充
        /// </summary>
        public bool RememberUsername
        {
            get => _rememberUsername;
            set
            {
                var oldValue = _rememberUsername;
                if (SetProperty(ref _rememberUsername, value))
                {
                    // 取消勾选时清除已保存的用户名
                    if (oldValue && !value)
                    {
                        _ = ClearSavedUsernameAsync();
                    }
                }
            }
        }

        /// <summary>
        /// 记住密码 - 勾选后保存密码（DPAPI加密），下次启动自动填充
        /// </summary>
        public bool RememberPassword
        {
            get => _rememberPassword;
            set
            {
                var oldValue = _rememberPassword;
                if (SetProperty(ref _rememberPassword, value))
                {
                    // T5-P2-07: 勾选"记住密码"时自动勾选"记住用户名"
                    if (value && !RememberUsername)
                    {
                        RememberUsername = true;
                    }

                    // 取消勾选时清除已保存的密码
                    if (oldValue && !value)
                    {
                        _ = ClearSavedPasswordAsync();
                    }
                }
            }
        }

        /// <summary>
        /// 是否有已保存的密码 - 用于显示"已保存"提示
        /// </summary>
        public bool HasSavedPassword
        {
            get => _hasSavedPassword;
            set => SetProperty(ref _hasSavedPassword, value);
        }

        /// <summary>
        /// 清除已保存的用户名
        /// </summary>
        private async Task ClearSavedUsernameAsync()
        {
            try
            {
                if (_usernameStorage != null)
                {
                    await _usernameStorage.ClearUsernameAsync();
                    Logger.LogInformation("[VM] Login.ClearSavedUsername - 已清除保存的用户名");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.ClearSavedUsername failed");
            }
        }

        /// <summary>
        /// 清除已保存的密码
        /// </summary>
        private async Task ClearSavedPasswordAsync()
        {
            try
            {
                if (_credentialVault != null && !string.IsNullOrEmpty(Username))
                {
                    await _credentialVault.ClearPasswordAsync(Username);
                    Logger.LogInformation("[VM] Login.ClearSavedPassword - 已清除用户 {Username} 的保存密码", Username);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.ClearSavedPassword failed");
            }
        }

        #endregion

        #region 连接模式 (US-SYNC-008: 模式切换 + 切换前置检查)

        /// <summary>
        /// 当前选择的连接模式
        /// US-SYNC-008: 支持远程/本地模式切换，切换前执行前置检查
        /// </summary>
        public ConnectionMode SelectedConnectionMode
        {
            get => _selectedConnectionMode;
            set
            {
                if (_selectedConnectionMode == value) return;
                _ = ValidateAndSwitchModeAsync(value);
            }
        }

        /// <summary>
        /// 是否选择远程模式（用于RadioButton绑定）
        /// </summary>
        public bool IsRemoteMode
        {
            get => _selectedConnectionMode == ConnectionMode.Remote;
            set { if (value) SelectedConnectionMode = ConnectionMode.Remote; }
        }

        /// <summary>
        /// 是否选择本地模式（用于RadioButton绑定）
        /// </summary>
        public bool IsLocalMode
        {
            get => _selectedConnectionMode == ConnectionMode.Local;
            set { if (value) SelectedConnectionMode = ConnectionMode.Local; }
        }

        /// <summary>
        /// US-SYNC-008: 模式切换前置验证
        /// 验证通过后切换模式，失败则恢复原模式并提示用户
        /// </summary>
        private async Task ValidateAndSwitchModeAsync(ConnectionMode targetMode)
        {
            if (_modeSwitchValidator == null)
            {
                // 无验证器时直接切换 (向后兼容)
                ApplyModeSwitch(targetMode);
                return;
            }

            var result = targetMode switch
            {
                ConnectionMode.Remote => await _modeSwitchValidator.ValidateLocalToRemoteSwitchAsync(),
                ConnectionMode.Local => await _modeSwitchValidator.ValidateRemoteToLocalSwitchAsync(),
                _ => ModeSwitchValidationResult.Valid
            };

            if (result.IsValid)
            {
                ApplyModeSwitch(targetMode);
            }
            else
            {
                // 验证失败: 恢复 RadioButton 状态并提示用户
                OnPropertyChanged(nameof(IsRemoteMode));
                OnPropertyChanged(nameof(IsLocalMode));
                await CommonDialogService.ShowWarningAsync(result.ErrorMessage!, "模式切换");
            }
        }

        /// <summary>
        /// 应用模式切换并通知 UI 更新
        /// </summary>
        private void ApplyModeSwitch(ConnectionMode targetMode)
        {
            SetProperty(ref _selectedConnectionMode, targetMode);
            OnPropertyChanged(nameof(IsRemoteMode));
            OnPropertyChanged(nameof(IsLocalMode));
            Logger.LogInformation("[VM] Login.ModeSwitch - switched to {Mode}", targetMode);
        }

        #endregion

        public bool HasMessage => !string.IsNullOrWhiteSpace(StatusMessage) || !string.IsNullOrWhiteSpace(ErrorMessage);
        public ApiHealthStatus ApiStatus { get => _apiStatus; set { if (SetProperty(ref _apiStatus, value)) { OnPropertyChanged(nameof(IsApiUnhealthy)); (RetryApiCheckCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } } }
        public string ApiStatusMessage { get => _apiStatusMessage; set => SetProperty(ref _apiStatusMessage, value); }
        public bool IsApiUnhealthy => ApiStatus == ApiHealthStatus.Unhealthy;

        public ICommand LoginCommand { get; }

        /// <summary>
        /// 关闭应用程序命令
        /// remove-titlebar-add-close-button: 仅在登录界面可用的关闭按钮
        /// </summary>
        public ICommand CloseApplicationCommand { get; }

        /// <summary>
        /// 重试API连接命令
        /// remove-statusbar-relocate-status: 登录界面API状态指示器重试功能
        /// </summary>
        public ICommand RetryApiCheckCommand { get; }

        /// <summary>
        /// 构造函数
        /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
        /// OpenSpec: refactor-startup-connection-resilience - 移除ConnectionMode，事件驱动状态更新
        /// </summary>
        public LoginViewModel(
            IViewModelServices services,
            ILoginCoordinator loginCoordinator,
            IApplicationStateService applicationStateService,
            IUsernameStorageService? usernameStorage = null,
            ICredentialVault? credentialVault = null,
            IModeSwitchValidator? modeSwitchValidator = null)
            : base(services)
        {
            _loginCoordinator = loginCoordinator ?? throw new ArgumentNullException(nameof(loginCoordinator));
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
            _usernameStorage = usernameStorage;
            _credentialVault = credentialVault;
            _modeSwitchValidator = modeSwitchValidator;

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), () => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && !IsLoading);
            CloseApplicationCommand = new DelegateCommand(async () => await ExecuteCloseApplicationAsync());
            RetryApiCheckCommand = new DelegateCommand(async () => await ExecuteRetryApiCheckAsync(), () => ApiStatus == ApiHealthStatus.Unhealthy);

            _applicationStateService.StatusChanged += OnApiStatusChanged;

            _cts = new CancellationTokenSource();
            BackgroundInitAsync().SafeFireAndForget(ex => Logger.LogError(ex, "[VM] Login.BackgroundInit failed"));
        }

        private async Task BackgroundInitAsync()
        {
            try
            {
                await Task.Delay(100, _cts?.Token ?? CancellationToken.None);
                await LoadSavedCredentialsAsync();
                await LoadApiStatusFromStateServiceAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when ViewModel is disposed during initialization
            }
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            // OpenSpec: simplify-login-options - 移除自动登录，用户始终看到登录界面
        }

        private async Task LoadApiStatusFromStateServiceAsync()
        {
            try
            {
                await Services.UiThreadDispatcher.InvokeAsync(() =>
                {
                    if (_applicationStateService.IsApiHealthy) { ApiStatus = ApiHealthStatus.Healthy; ApiStatusMessage = "WebAPI 已连接"; }
                    else { ApiStatus = ApiHealthStatus.Unhealthy; ApiStatusMessage = $"WebAPI 连接失败: {_applicationStateService.ConnectionStatus}"; }
                });
            }
            catch (Exception ex) { Logger.LogError(ex, "[VM] Login.LoadApiStatus failed"); await Services.UiThreadDispatcher.InvokeAsync(() => { ApiStatus = ApiHealthStatus.Unhealthy; ApiStatusMessage = "加载API状态失败，请稍后重试"; }); }
        }

        /// <summary>
        /// 加载已保存的用户名
        /// OpenSpec: remove-secure-credential-storage - 简化为仅从UsernameStorageService加载
        /// 自动登录功能由LoginCoordinator通过CredentialVault处理
        /// </summary>
        private async Task LoadSavedCredentialsAsync()
        {
            try
            {
                if (_usernameStorage != null)
                {
                    var savedUsername = await _usernameStorage.GetSavedUsernameAsync();
                    var isRememberMeEnabled = await _usernameStorage.IsRememberMeEnabledAsync();
                    if (!string.IsNullOrEmpty(savedUsername))
                    {
                        // OpenSpec: redesign-login-remember-password - 加载已保存的密码
                        string? savedPassword = null;
                        bool hasSavedPassword = false;
                        if (_credentialVault != null)
                        {
                            hasSavedPassword = await _credentialVault.HasSavedPasswordAsync(savedUsername);
                            if (hasSavedPassword)
                            {
                                savedPassword = await _credentialVault.GetPasswordAsync(savedUsername);
                            }
                        }

                        await Services.UiThreadDispatcher.InvokeAsync(() =>
                        {
                            _savedUsername = savedUsername;
                            Username = savedUsername;
                            RememberUsername = isRememberMeEnabled;
                            HasSavedPassword = hasSavedPassword;

                            // OpenSpec: redesign-login-remember-password - 填充密码
                            if (!string.IsNullOrEmpty(savedPassword))
                            {
                                Password = savedPassword;
                                RememberPassword = true;
                                // 密码已加载，勾选"记住密码"
                                Logger.LogInformation("[VM] Login.LoadCredentials - 已加载用户 {Username} 的保存密码", savedUsername);
                            }
                            else
                            {
                                // 没有保存密码，不勾选"记住密码"
                                RememberPassword = false;
                            }
                        });
                    }
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "[VM] Login.LoadCredentials failed"); }
        }

        /// <summary>
        /// API状态变更事件处理器
        /// OpenSpec: refactor-startup-connection-resilience - 事件驱动UI更新
        /// </summary>
        private void OnApiStatusChanged(object? sender, ApiStatusChangedEventArgs e)
        {
            try
            {
                Services.UiThreadDispatcher.InvokeAsync(() =>
                {
                    if (e.IsHealthy)
                    {
                        ApiStatus = ApiHealthStatus.Healthy;
                        ApiStatusMessage = "WebAPI 已连接";
                    }
                    else
                    {
                        ApiStatus = ApiHealthStatus.Unhealthy;
                        ApiStatusMessage = $"WebAPI 连接失败: {e.LastError ?? e.ConnectionStatus}";
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.OnApiStatusChanged failed");
            }
        }

        private async Task ExecuteLoginAsync()
        {
            try
            {
                IsLoading = true; ErrorMessage = string.Empty; StatusMessage = "正在登录...";

                // 保存密码用于后续存储（登录成功后才保存）
                var passwordToSave = RememberPassword ? Password : null;

                // OpenSpec: simplify-login-options - 使用LoginCoordinator执行登录（不再传递RememberMe）
                var result = await _loginCoordinator.LoginAsync(Username, Password);

                if (result.Success)
                {
                    // OpenSpec: simplify-login-options - 根据勾选状态保存用户名
                    if (_usernameStorage != null)
                    {
                        if (RememberUsername)
                        {
                            await _usernameStorage.SaveUsernameAsync(Username, rememberMe: true);
                            Logger.LogInformation("[VM] Login.Execute - 已保存用户名 {Username}", Username);
                        }
                        else
                        {
                            await _usernameStorage.ClearUsernameAsync();
                        }
                    }

                    // OpenSpec: redesign-login-remember-password - 保存密码
                    if (_credentialVault != null)
                    {
                        if (!string.IsNullOrEmpty(passwordToSave))
                        {
                            // 勾选"记住密码"，保存密码
                            var saveResult = await _credentialVault.SavePasswordAsync(Username, passwordToSave);
                            if (saveResult)
                            {
                                Logger.LogInformation("[VM] Login.Execute - 已保存用户 {Username} 的密码", Username);
                            }
                        }
                        else
                        {
                            // 未勾选"记住密码"，清除已保存的密码
                            await _credentialVault.ClearPasswordAsync(Username);
                        }
                    }

                    // LoginCoordinator已处理会话启动、模块加载和导航
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "登录失败，请检查用户名和密码";
                    Password = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.Execute failed - Username={Username}", Username);
                ErrorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex);
                Password = string.Empty;
            }
            finally { IsLoading = false; StatusMessage = string.Empty; }
        }

        /// <summary>
        /// 关闭应用程序
        /// remove-titlebar-add-close-button: 使用ICommonDialogService显示确认框后退出程序
        /// OpenSpec: enhance-viewmodel-architecture - 使用基类CommonDialogService
        /// </summary>
        private async Task ExecuteCloseApplicationAsync()
        {
            var confirmed = await CommonDialogService.ShowConfirmAsync("确定要退出程序吗？", "退出确认");

            if (confirmed)
            {
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// 重试API连接检查
        /// remove-statusbar-relocate-status: 登录界面API状态指示器重试功能
        /// </summary>
        private async Task ExecuteRetryApiCheckAsync()
        {
            try
            {
                ApiStatus = ApiHealthStatus.Checking;
                ApiStatusMessage = "正在检查连接...";

                // 触发ApplicationStateService重新检查API健康状态
                await _applicationStateService.CheckApiHealthAsync();

                await Services.UiThreadDispatcher.InvokeAsync(() =>
                {
                    if (_applicationStateService.IsApiHealthy)
                    {
                        ApiStatus = ApiHealthStatus.Healthy;
                        ApiStatusMessage = "WebAPI 已连接";
                    }
                    else
                    {
                        ApiStatus = ApiHealthStatus.Unhealthy;
                        ApiStatusMessage = $"WebAPI 连接失败: {_applicationStateService.ConnectionStatus}";
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.RetryApiCheck failed");
                await Services.UiThreadDispatcher.InvokeAsync(() =>
                {
                    ApiStatus = ApiHealthStatus.Unhealthy;
                    ApiStatusMessage = "连接检查失败，请稍后重试";
                });
            }
        }

        protected override void OnDisposing()
        {
            _applicationStateService.StatusChanged -= OnApiStatusChanged;

            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            base.OnDisposing();
        }
    }
}
