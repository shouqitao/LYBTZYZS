using System.IO;
using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
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
        // 说明（viewmodel-layer-design §4.2）：这些属性是子 VM（Credentials/ConnectionStatus）的**代理**，
        // 状态唯一真相源在子 VM，本 VM 不另存字段——故不迁 [ObservableProperty]（迁则会引入第二份状态并与子 VM 失同步）。
        // 子 VM 属性变更经下方“子 VM 属性转发”区域直接转发 PropertyChanged。

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

        /// <summary>
        /// 自动登录开关（designs/login.pen：记住密码 / 自动登录 两端对齐）
        /// 当前仅保存 UI 状态，自动登录执行链路未接入（预留）
        /// </summary>
        public bool IsAutoLogin { get; set; }

        /// <summary>
        /// 品牌区大标题 - 诊所名可绑定配置（clinic-settings.json，IClinicSettingsService）
        /// </summary>
        public string ClinicName { get; }

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

        /// <summary>连通性重试（代理子 VM 命令——保持命令实例与其 CanExecute 同源）</summary>
        public ICommand RetryApiCheckCommand => ConnectionStatus.RetryApiCheckCommand;

        /// <summary>切换到本地模式（代理子 VM 命令）</summary>
        public ICommand SwitchToLocalCommand => ConnectionStatus.SwitchToLocalCommand;

        /// <summary>切换到远程模式（代理子 VM 命令）</summary>
        public ICommand SwitchToRemoteCommand => ConnectionStatus.SwitchToRemoteCommand;

        // LoginCommand / CloseApplicationCommand / OpenSettingsCommand / ForgotPasswordCommand
        // 由 [RelayCommand] 源生成（见下方命令方法），不再手写 ICommand 字段

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
            IConnectionSettingsService? connectionSettingsService = null,
            LoginCredentialsViewModel? credentials = null,
            ConnectionStatusViewModel? connectionStatus = null,
            IClinicSettingsService? clinicSettingsService = null)
            : base(services)
        {
            _loginCoordinator = loginCoordinator ?? throw new ArgumentNullException(nameof(loginCoordinator));
            _dialogService = dialogService;

            // 品牌区大标题：诊所名可绑定配置，空值回退默认
            ClinicName = clinicSettingsService is null
                ? "凌隐宝堂中医诊所"
                : string.IsNullOrWhiteSpace(clinicSettingsService.ClinicName) ? "凌隐宝堂中医诊所" : clinicSettingsService.ClinicName;

            // 创建子 VM（D3: DI 注入优先，手动 new 为测试/可选依赖回退）
            Credentials = credentials ?? new LoginCredentialsViewModel(services, usernameStorage, credentialVault);
            ConnectionStatus = connectionStatus ?? new ConnectionStatusViewModel(services, applicationStateService, connectionModeService, connectionSettingsService);

            // 命令由 [RelayCommand] 源生成（LoginAsync/CloseApplicationAsync/OpenSettings/ForgotPassword）；
            // RetryApiCheck/SwitchToLocal/SwitchToRemote 为子 VM 命令代理，保持同一命令实例

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
            finally
            {
                LoginCommand.NotifyCanExecuteChanged(); // 初始化完成（可能已加载保存的凭证）后重新评估
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

        /// <summary>
        /// 登录 - P2-14-1 防重入：CanExecute 含 !IsLoading，且 AsyncRelayCommand 默认拒绝并发
        /// （allowConcurrentExecutions:false），双击仅首次生效
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true; ErrorMessage = string.Empty; StatusMessage = "正在登录...";
                LoginCommand.NotifyCanExecuteChanged(); // 立即禁用登录按钮

                var result = await _loginCoordinator.LoginAsync(Username, Password);

                if (result.Success)
                {
                    await Credentials.SaveCredentialsAsync(RememberPassword);
                }
                else
                {
                    ErrorMessage = result.Error ?? "登录失败，请检查用户名和密码";
                    Password = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[VM] Login.Execute failed - Username={Username}", Username);
                ErrorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex);
                Password = string.Empty;
            }
            finally
            {
                // 所有路径统一复位加载状态（成功路径此前遗漏，登录后返回登录页会残留遮罩并禁用按钮）
                IsLoading = false;
                StatusMessage = string.Empty;
                LoginCommand.NotifyCanExecuteChanged(); // IsLoading 恢复后重新评估
            }
        }

        /// <summary>登录按钮可用性：用户名/密码非空且未在登录中</summary>
        private bool CanLogin() =>
            !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) && !IsLoading;

        [RelayCommand]
        private void OpenSettings()
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

        [RelayCommand]
        private async Task CloseApplicationAsync()
        {
            var confirmed = await CommonDialogService.ShowConfirmAsync("确定要退出程序吗？", "退出确认");

            if (confirmed)
            {
                Application.Current.Shutdown();
            }
        }

        /// <summary>忘记密码链接（设计稿对齐；找回流程待业务确认，空实现占位）</summary>
        [RelayCommand]
        private void ForgotPassword()
        {
            // 设计稿对齐：忘记密码流程待业务确认
        }

        #region 子 VM 属性转发

        private void OnCredentialsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
            // Username/Password 变化时重新评估 CanExecute（修复登录按钮灰色不可用）
            if (e.PropertyName is nameof(Credentials.Username) or nameof(Credentials.Password))
            {
                LoginCommand.NotifyCanExecuteChanged();
            }
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
