using System.IO;
using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Foundation.ExceptionHandling;
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
    /// 组合 LoginCredentialsViewModel + ConnectionStatusViewModel 子 VM
    /// </summary>
    public partial class LoginViewModel : NavigableViewModelBase
    {
        private readonly ILoginCoordinator _loginCoordinator;
        private readonly IDialogService? _dialogService;
        private CancellationTokenSource? _cts;

        /// <summary>
        /// 首次运行标记文件路径 (%LOCALAPPDATA%\LYBT\Desktop\first_run_done.flag)
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
                // 标记失败不阻塞使用
            }
        }

        #region 子 ViewModel

        /// <summary>凭证输入子 VM</summary>
        public LoginCredentialsViewModel Credentials { get; }

        /// <summary>连接状态子 VM</summary>
        public ConnectionStatusViewModel ConnectionStatus { get; }

        #endregion

        #region 代理属性（XAML 向后兼容）

        public string Username
        {
            get => Credentials.Username;
            set => Credentials.Username = value;
        }

        public string Password
        {
            get => Credentials.Password;
            set => Credentials.Password = value;
        }

        public bool RememberUsername
        {
            get => Credentials.RememberUsername;
            set => Credentials.RememberUsername = value;
        }

        public bool RememberPassword
        {
            get => Credentials.RememberPassword;
            set => Credentials.RememberPassword = value;
        }

        public bool HasSavedPassword
        {
            get => Credentials.HasSavedPassword;
            set => Credentials.HasSavedPassword = value;
        }

        public ApiHealthStatus ApiStatus
        {
            get => ConnectionStatus.ApiStatus;
            set => ConnectionStatus.ApiStatus = value;
        }

        public string ApiStatusMessage
        {
            get => ConnectionStatus.ApiStatusMessage;
            set => ConnectionStatus.ApiStatusMessage = value;
        }

        public bool IsApiUnhealthy => ConnectionStatus.IsApiUnhealthy;

        public string CurrentModeDisplay
        {
            get => ConnectionStatus.CurrentModeDisplay;
            set => ConnectionStatus.CurrentModeDisplay = value;
        }

        public bool IsRemoteMode
        {
            get => ConnectionStatus.IsRemoteMode;
            set => ConnectionStatus.IsRemoteMode = value;
        }

        public bool IsRemoteAvailable
        {
            get => ConnectionStatus.IsRemoteAvailable;
            set => ConnectionStatus.IsRemoteAvailable = value;
        }

        public bool HasMessage => !string.IsNullOrWhiteSpace(StatusMessage) || !string.IsNullOrWhiteSpace(ErrorMessage);

        #endregion

        #region 命令

        public ICommand LoginCommand { get; }
        public ICommand CloseApplicationCommand { get; }
        public ICommand RetryApiCheckCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand SwitchToLocalCommand { get; }
        public ICommand SwitchToRemoteCommand { get; }

        #endregion

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
            _dialogService = dialogService;

            // 创建子 VM
            Credentials = new LoginCredentialsViewModel(services, usernameStorage, credentialVault);
            ConnectionStatus = new ConnectionStatusViewModel(services, applicationStateService, connectionModeService);

            // 命令
            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), () => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && !IsLoading);
            CloseApplicationCommand = new DelegateCommand(async () => await ExecuteCloseApplicationAsync());
            RetryApiCheckCommand = ConnectionStatus.RetryApiCheckCommand;
            OpenSettingsCommand = new DelegateCommand(ExecuteOpenSettings);
            SwitchToLocalCommand = ConnectionStatus.SwitchToLocalCommand;
            SwitchToRemoteCommand = ConnectionStatus.SwitchToRemoteCommand;

            // 订阅子 VM 属性变更以转发到本 VM
            Credentials.PropertyChanged += OnCredentialsPropertyChanged;
            ConnectionStatus.PropertyChanged += OnConnectionStatusPropertyChanged;

            // ErrorMessage/StatusMessage 变更时通知 HasMessage
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
                await Credentials.LoadSavedCredentialsAsync();
                await ConnectionStatus.LoadApiStatusAsync();
                await ConnectionStatus.DetectConnectionModeAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when ViewModel is disposed during initialization
            }
        }

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

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
        }

        private async Task ExecuteLoginAsync()
        {
            try
            {
                IsLoading = true; ErrorMessage = string.Empty; StatusMessage = "正在登录...";

                var passwordToSave = RememberPassword ? Password : null;
                var result = await _loginCoordinator.LoginAsync(Username, Password);

                if (result.Success)
                {
                    await Credentials.SaveCredentialsAsync(RememberPassword);
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

        private async Task ExecuteCloseApplicationAsync()
        {
            var confirmed = await CommonDialogService.ShowConfirmAsync("确定要退出程序吗？", "退出确认");

            if (confirmed)
            {
                Application.Current.Shutdown();
            }
        }

        #region 子 VM 属性转发

        private void OnCredentialsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        private void OnConnectionStatusPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        #endregion

        protected override void OnDisposing()
        {
            Credentials.PropertyChanged -= OnCredentialsPropertyChanged;
            ConnectionStatus.PropertyChanged -= OnConnectionStatusPropertyChanged;

            ConnectionStatus.Dispose();

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
