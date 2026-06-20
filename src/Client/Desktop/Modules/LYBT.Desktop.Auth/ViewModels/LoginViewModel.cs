using System.IO;
using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Auth.ViewModels
{
    /// <summary>
    /// 登录视图模型 - 使用LoginCoordinator编排登录流程
    /// </summary>
    public partial class LoginViewModel : NavigableViewModelBase
    {
        private readonly ILoginCoordinator _loginCoordinator;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IUsernameStorageService? _usernameStorage;
        private readonly ICredentialVault? _credentialVault;
        private readonly IDialogService? _dialogService;
        private readonly IConnectionModeService? _connectionModeService;
        private readonly IConnectionSettingsService? _connectionSettingsService;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// 首次运行标记文件路径 (%LOCALAPPDATA%\LYBT\Desktop\first_run_done.flag)
        /// 与 UsernameStorageService 共用目录约定
        /// </summary>
        private static readonly string FirstRunMarkerPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LYBT", "Desktop", "first_run_done.flag");

        private static bool IsFirstRun => !File.Exists(FirstRunMarkerPath);

        private static void MarkFirstRunCompleted()
        {
            try
            {
                var dir = Path.GetDirectoryName(FirstRunMarkerPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(FirstRunMarkerPath, DateTime.UtcNow.ToString("O"));
            }
            catch
            {
                // 标记失败不阻塞使用 - 下次启动仍会弹出向导
            }
        }

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberUsername;
        private bool _rememberPassword;
        private bool _hasSavedPassword;
        private string? _savedUsername;
        private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
        private string _apiStatusMessage = "正在检查连接...";

        // 连接模式显示 (远程模式/本地模式)
        private string _currentModeDisplay = "检测中...";
        private bool _isRemoteMode;
        private bool _isRemoteAvailable;

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

        #region 记住账号+记住密码

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

        public bool HasMessage => !string.IsNullOrWhiteSpace(StatusMessage) || !string.IsNullOrWhiteSpace(ErrorMessage);

        public ApiHealthStatus ApiStatus { get => _apiStatus; set { if (SetProperty(ref _apiStatus, value)) { OnPropertyChanged(nameof(IsApiUnhealthy)); (RetryApiCheckCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } } }
        public string ApiStatusMessage { get => _apiStatusMessage; set => SetProperty(ref _apiStatusMessage, value); }
        public bool IsApiUnhealthy => ApiStatus == ApiHealthStatus.Unhealthy;

        /// <summary>
        /// 当前连接模式显示文本 (远程模式/本地模式/检测中...)
        /// </summary>
        public string CurrentModeDisplay
        {
            get => _currentModeDisplay;
            set => SetProperty(ref _currentModeDisplay, value);
        }

        /// <summary>
        /// 是否为远程模式 - 用于颜色编码 (true=绿色, false=橙色)
        /// </summary>
        public bool IsRemoteMode
        {
            get => _isRemoteMode;
            set => SetProperty(ref _isRemoteMode, value);
        }

        /// <summary>
        /// 远程服务是否可用 - 控制"切换到远程"按钮的 CanExecute
        /// </summary>
        public bool IsRemoteAvailable
        {
            get => _isRemoteAvailable;
            set
            {
                if (SetProperty(ref _isRemoteAvailable, value))
                {
                    (SwitchToRemoteCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

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
        /// 打开服务器配置对话框命令
        /// </summary>
        public ICommand OpenSettingsCommand { get; }

        public ICommand SwitchToLocalCommand { get; }

        public ICommand SwitchToRemoteCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public LoginViewModel(
            IViewModelServices services,
            ILoginCoordinator loginCoordinator,
            IApplicationStateService applicationStateService,
            IUsernameStorageService? usernameStorage = null,
            ICredentialVault? credentialVault = null,
            IDialogService? dialogService = null,
            IConnectionModeService? connectionModeService = null,
            IConnectionSettingsService? connectionSettingsService = null)
            : base(services)
        {
            _loginCoordinator = loginCoordinator ?? throw new ArgumentNullException(nameof(loginCoordinator));
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
            _usernameStorage = usernameStorage;
            _credentialVault = credentialVault;
            _dialogService = dialogService;
            _connectionModeService = connectionModeService;
            _connectionSettingsService = connectionSettingsService;

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), () => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && !IsLoading);
            CloseApplicationCommand = new DelegateCommand(async () => await ExecuteCloseApplicationAsync());
            RetryApiCheckCommand = new DelegateCommand(async () => await ExecuteRetryApiCheckAsync(), () => ApiStatus == ApiHealthStatus.Unhealthy);
            OpenSettingsCommand = new DelegateCommand(ExecuteOpenSettings);
            SwitchToLocalCommand = new DelegateCommand(ExecuteSwitchToLocal);
            SwitchToRemoteCommand = new DelegateCommand(ExecuteSwitchToRemote, () => IsRemoteAvailable);

            _applicationStateService.StatusChanged += OnApiStatusChanged;

            // 订阅连接模式变更事件
            if (_connectionModeService != null)
            {
                _connectionModeService.ModeChanged += OnConnectionModeChanged;
                // 初始化当前模式显示
                CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                IsRemoteMode = _connectionModeService.IsRemote;
                IsRemoteAvailable = _connectionModeService.IsRemoteAvailable;
            }

            // P0-FIX: ErrorMessage/StatusMessage 变更时通知 HasMessage 属性
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ErrorMessage) || e.PropertyName == nameof(StatusMessage))
                    OnPropertyChanged(nameof(HasMessage));
            };

            _cts = new CancellationTokenSource();
            BackgroundInitAsync().SafeFireAndForget(ex => Logger.LogError(ex, "[VM] Login.BackgroundInit failed"));
        }

        private async Task BackgroundInitAsync()
        {
            try
            {
                await Task.Delay(100, _cts?.Token ?? CancellationToken.None);
                await MaybeShowFirstRunSetupAsync();
                await LoadSavedCredentialsAsync();
                await LoadApiStatusFromStateServiceAsync();
                await DetectConnectionModeAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when ViewModel is disposed during initialization
            }
        }

        /// <summary>
        /// 首次运行检测 - 若标记文件不存在则弹出配置向导
        /// </summary>
        private async Task MaybeShowFirstRunSetupAsync()
        {
            if (!IsFirstRun)
            {
                Logger.LogDebug("[VM] Login.FirstRun - 标记文件已存在，跳过向导");
                return;
            }

            if (_dialogService is null)
            {
                Logger.LogWarning("[VM] Login.FirstRun - IDialogService 未注入，无法显示首次运行向导");
                return;
            }

            try
            {
                await Services.UiThreadDispatcher.InvokeAsync(() =>
                {
                    Logger.LogInformation("[VM] Login.FirstRun - 显示首次运行配置向导");
                    _dialogService.ShowDialog(nameof(Views.FirstRunSetupView), null, result =>
                    {
                        Logger.LogInformation("[VM] Login.FirstRun - 向导已关闭: {Result}", result.Result);
                        MarkFirstRunCompleted();
                    });
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.FirstRun - 显示向导失败");
            }
        }

        /// <summary>
        /// 检测最佳连接模式 (远程优先，本地回退) 并更新 UI 显示
        /// </summary>
        private async Task DetectConnectionModeAsync()
        {
            if (_connectionModeService is null) return;

            try
            {
                await Services.UiThreadDispatcher.InvokeAsync(() =>
                {
                    CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                    IsRemoteMode = _connectionModeService.IsRemote;
                });

                var mode = await _connectionModeService.DetectBestModeAsync();
                var remoteAvailable = await _connectionModeService.CheckRemoteAvailableAsync();

                await Services.UiThreadDispatcher.InvokeAsync(() =>
                {
                    IsRemoteAvailable = remoteAvailable;
                    CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                    IsRemoteMode = _connectionModeService.IsRemote;
                    Logger.LogInformation("[VM] Login.DetectMode - 连接模式: {Mode} ({Display}), 远程可用: {RemoteAvailable}",
                        mode, _connectionModeService.CurrentModeDisplay, remoteAvailable);
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.DetectMode failed");
            }
        }

        /// <summary>
        /// 连接模式变更事件处理器
        /// </summary>
        private void OnConnectionModeChanged(object? sender, ConnectionMode e)
        {
            try
            {
                if (_connectionModeService is null) return;
                Services.UiThreadDispatcher.InvokeAsync(() =>
                {
                    CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                    IsRemoteMode = _connectionModeService.IsRemote;
                    IsRemoteAvailable = _connectionModeService.IsRemoteAvailable;
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.OnConnectionModeChanged failed");
            }
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
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
                var result = await _loginCoordinator.LoginAsync(Username, Password);

                if (result.Success)
                {
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
                    IsLoading = false;
                    ErrorMessage = result.ErrorMessage ?? "登录失败，请检查用户名和密码";
                    Password = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.Execute failed - Username={Username}", Username);
                IsLoading = false;
                ErrorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex);
                Password = string.Empty;
            }
            finally { StatusMessage = string.Empty; }
        }

        /// <summary>
        /// 打开服务器配置对话框
        /// </summary>
        private void ExecuteOpenSettings()
        {
            if (_dialogService is null)
            {
                Logger.LogWarning("[VM] Login.OpenSettings - IDialogService 未注入，无法打开服务器配置");
                return;
            }

            _dialogService.ShowDialog(nameof(Views.ServerConfigView), null, result =>
            {
                Logger.LogInformation("[VM] Login.OpenSettings - 配置对话框已关闭: {Result}", result.Result);
            });
        }

        /// <summary>切换到本地模式 - 直接切换并持久化</summary>
        private void ExecuteSwitchToLocal()
        {
            if (_connectionModeService is null)
            {
                Logger.LogWarning("[VM] Login.SwitchToLocal - IConnectionModeService 未注入");
                return;
            }

            try
            {
                Logger.LogInformation("[VM] Login.SwitchToLocal → 本地模式");
                _connectionModeService.SetMode(ConnectionMode.Local);

                CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                IsRemoteMode = _connectionModeService.IsRemote;
                ApiStatusMessage = _connectionModeService.ApiStatusDisplay;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.SwitchToLocal failed");
            }
        }

        /// <summary>
        /// 切换到远程模式 - 直接切换（远程可用性已由 CanExecute 校验）
        /// 远程 URL 由"服务器配置"对话框维护，此处不再弹窗
        /// </summary>
        private void ExecuteSwitchToRemote()
        {
            if (_connectionModeService is null)
            {
                Logger.LogWarning("[VM] Login.SwitchToRemote - IConnectionModeService 未注入");
                return;
            }

            try
            {
                Logger.LogInformation("[VM] Login.SwitchToRemote → 远程模式");
                _connectionModeService.SetMode(ConnectionMode.Remote);

                CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                IsRemoteMode = _connectionModeService.IsRemote;
                ApiStatusMessage = _connectionModeService.ApiStatusDisplay;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.SwitchToRemote failed");
            }
        }

        /// <summary>
        /// 关闭应用程序
        /// remove-titlebar-add-close-button: 使用ICommonDialogService显示确认框后退出程序
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

            if (_connectionModeService != null)
            {
                _connectionModeService.ModeChanged -= OnConnectionModeChanged;
            }

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
