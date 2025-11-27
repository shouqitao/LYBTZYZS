using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Auth.Models; // Issue #1825: ConnectionMode
using LYBT.Desktop.Auth.Services; // Issue #1825: IConnectionSettingsService
using LYBT.Desktop.Foundation.Application; // Issue #1823: IApplicationStateService
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
        private readonly IApplicationStateService _applicationStateService; // Issue #1823: API健康检查前置
        private readonly IUsernameStorageService? _usernameStorage;
        private readonly ISecureCredentialStorage? _credentialStorage; // Issue #1246: 密码加密存储
        private readonly IConnectionSettingsService? _connectionSettingsService; // Issue #1825: 连接模式设置

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private bool _rememberPassword; // Issue #1246: 记住密码
        private bool _hasSavedPassword;
        private string? _savedUsername; // 记录加载的保存用户名，用于检测用户名变更
        private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
        private string _apiStatusMessage = "正在检查连接...";
        private ConnectionMode _connectionMode; // Issue #1825: 连接模式

        public LoginViewModel(
            IAuthenticationService authService,
            ITokenStorageService tokenStorage,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IApplicationStateService applicationStateService, // Issue #1823: API健康检查前置
            IUsernameStorageService? usernameStorage = null,
            ISecureCredentialStorage? credentialStorage = null, // Issue #1246: 密码加密存储服务
            IConnectionSettingsService? connectionSettingsService = null) // Issue #1825: 连接模式设置
            : base(eventAggregator, loggerFactory, regionManager, null, null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService)); // Issue #1823
            _usernameStorage = usernameStorage;
            _credentialStorage = credentialStorage; // Issue #1246
            _connectionSettingsService = connectionSettingsService; // Issue #1825

            LoginCommand = new DelegateCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);

            // Issue #1825: 初始化连接模式（默认Remote）
            _connectionMode = _connectionSettingsService?.GetConnectionMode() ?? ConnectionMode.Remote;

            // Issue #861 & #1246: 在后台线程加载保存的凭据（用户名 + 密码）
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // 短暂延迟,让 UI 先完成初始化
                await LoadSavedCredentialsAsync();
                // Issue #1823: API健康检查已前置到应用启动Phase 3，这里直接从IApplicationStateService读取状态
                await LoadApiStatusFromStateServiceAsync();
            });
        }

        /// <summary>
        /// 从IApplicationStateService加载API状态
        /// Issue #1823: API健康检查已前置到应用启动Phase 3
        /// </summary>
        private async Task LoadApiStatusFromStateServiceAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (_applicationStateService.IsApiHealthy)
                    {
                        ApiStatus = ApiHealthStatus.Healthy;
                        ApiStatusMessage = "WebAPI 已连接";
                        Logger.LogInformation("从IApplicationStateService读取API状态：健康");
                    }
                    else
                    {
                        ApiStatus = ApiHealthStatus.Unhealthy;
                        ApiStatusMessage = $"WebAPI 连接失败: {_applicationStateService.ConnectionStatus}";
                        Logger.LogWarning("从IApplicationStateService读取API状态：不健康 - {Status}", _applicationStateService.ConnectionStatus);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载API状态时发生错误");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ApiStatus = ApiHealthStatus.Unhealthy;
                    ApiStatusMessage = $"加载API状态失败: {ex.Message}";
                });
            }
        }

        #region INavigationAware - Token 自动验证（Issue #1824）

        /// <summary>
        /// 导航到登录页面时触发 - Issue #1824 Token 自动验证
        /// </summary>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // 异步执行 Token 验证，避免阻塞 UI 线程
            _ = Task.Run(async () =>
            {
                await TryAutoLoginWithTokenAsync();
            });
        }

        /// <summary>
        /// 尝试使用保存的 Token 自动登录 - Issue #1824
        /// </summary>
        private async Task TryAutoLoginWithTokenAsync()
        {
            try
            {
                // 步骤 1：获取保存的 Token
                var token = await _tokenStorage.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    Logger.LogInformation("未找到保存的 Token，显示登录表单");
                    return; // 无 Token，显示登录表单
                }

                Logger.LogInformation("发现保存的 Token，开始验证...");

                // 步骤 2：验证 Token
                var validationResult = await _authService.ValidateTokenAsync(token);

                if (!validationResult.IsSuccess || validationResult.Data == null)
                {
                    // API 调用失败（可能 API 不可用）
                    Logger.LogWarning("Token 验证 API 调用失败: {Message}，降级到密码登录", validationResult.Message);
                    await ClearInvalidTokenAsync();
                    return;
                }

                var validation = validationResult.Data;

                // 步骤 3：检查 Token 是否有效
                if (!validation.IsValid)
                {
                    // Token 无效或过期
                    Logger.LogWarning("Token 无效或已过期: {Message}，降级到密码登录", validation.ErrorMessage);
                    await ClearInvalidTokenAsync();
                    return;
                }

                // 步骤 4：Token 有效，执行自动登录
                Logger.LogInformation("Token 验证成功，用户 {Username} 自动登录", validation.Username);

                // 获取完整的用户信息
                var loginResponse = await _tokenStorage.GetLoginResponseAsync();
                if (loginResponse?.User == null)
                {
                    Logger.LogWarning("无法获取完整的用户信息，降级到密码登录");
                    await ClearInvalidTokenAsync();
                    return;
                }

                // 触发登录成功事件（与密码登录相同的流程）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Logger.LogInformation(" Token 自动登录成功，发布 LoginSuccessEvent");
                    EventAggregator.GetEvent<LoginSuccessEvent>().Publish(loginResponse.User);
                });
            }
            catch (Exception ex)
            {
                // 捕获所有异常（包括 API 不可用）
                Logger.LogError(ex, "Token 自动验证失败，降级到密码登录");
                await ClearInvalidTokenAsync();
            }
        }

        /// <summary>
        /// 清除无效的 Token
        /// </summary>
        private async Task ClearInvalidTokenAsync()
        {
            try
            {
                await _tokenStorage.ClearAuthenticationAsync();
                Logger.LogInformation("已清除无效的 Token");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "清除无效 Token 时发生错误");
            }
        }

        #endregion

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
                            // 先记录保存的用户名，再设置属性（避免触发清空逻辑）
                            _savedUsername = credentials.Value.Username;
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
                            // 先记录保存的用户名，再设置属性
                            _savedUsername = savedUsername;
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
                // 检测是否与保存的用户名不同（用于清空密码）
                var shouldClearPassword = _savedUsername != null &&
                                          !string.IsNullOrEmpty(_savedUsername) &&
                                          !string.IsNullOrEmpty(value) &&
                                          value != _savedUsername &&
                                          !string.IsNullOrEmpty(_password);

                if (SetProperty(ref _username, value))
                {
                    // 如果用户名改变了（且不是初始加载），清空密码
                    if (shouldClearPassword)
                    {
                        Password = string.Empty;
                        Logger.LogInformation("用户名已变更（从 {SavedUsername} 到 {NewUsername}），密码字段已清空", _savedUsername, value);
                    }

                    (LoginCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
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
            set
            {
                if (SetProperty(ref _apiStatus, value))
                {
                    RaisePropertyChanged(nameof(IsApiUnhealthy));
                }
            }
        }

        public string ApiStatusMessage
        {
            get => _apiStatusMessage;
            set => SetProperty(ref _apiStatusMessage, value);
        }

        /// <summary>
        /// API 是否不健康（用于显示警告横幅）
        /// Issue #1823: API 不可用警告横幅
        /// </summary>
        public bool IsApiUnhealthy => ApiStatus == ApiHealthStatus.Unhealthy;

        /// <summary>
        /// 连接模式 - Issue #1825
        /// </summary>
        public ConnectionMode ConnectionMode
        {
            get => _connectionMode;
            set
            {
                if (SetProperty(ref _connectionMode, value))
                {
                    // 更新UI相关属性
                    RaisePropertyChanged(nameof(IsRemoteModeSelected));
                    RaisePropertyChanged(nameof(IsLocalModeSelected));
                    RaisePropertyChanged(nameof(ConnectionModeDisplay));

                    // 保存连接模式
                    _connectionSettingsService?.SaveConnectionMode(value);
                    Logger.LogInformation("连接模式已切换: {Mode}", value);

                    // 更新API状态显示
                    UpdateConnectionStatus();
                }
            }
        }

        /// <summary>
        /// 是否选择远程模式（用于RadioButton双向绑定）- Issue #1825
        /// </summary>
        public bool IsRemoteModeSelected
        {
            get => ConnectionMode == ConnectionMode.Remote;
            set
            {
                if (value && ConnectionMode != ConnectionMode.Remote)
                {
                    ConnectionMode = ConnectionMode.Remote;
                }
            }
        }

        /// <summary>
        /// 是否选择本地模式（用于RadioButton双向绑定）- Issue #1825
        /// </summary>
        public bool IsLocalModeSelected
        {
            get => ConnectionMode == ConnectionMode.Local;
            set
            {
                if (value && ConnectionMode != ConnectionMode.Local)
                {
                    ConnectionMode = ConnectionMode.Local;
                }
            }
        }

        /// <summary>
        /// 连接模式显示文本（用于状态栏）- Issue #1825
        /// </summary>
        public string ConnectionModeDisplay
        {
            get
            {
                return ConnectionMode switch
                {
                    ConnectionMode.Remote => "远程模式 - 连接到WebAPI服务",
                    ConnectionMode.Local => "本地模式 - 使用本地数据库（v2.0）",
                    _ => "未知模式"
                };
            }
        }

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 更新连接状态显示 - Issue #1825
        /// </summary>
        private void UpdateConnectionStatus()
        {
            if (ConnectionMode == ConnectionMode.Local)
            {
                ApiStatusMessage = "本地模式 - 无需连接API（v2.0功能）";
                ApiStatus = ApiHealthStatus.Healthy;
                Logger.LogInformation("切换到本地模式，API状态已设置为Healthy");
            }
            else
            {
                // 远程模式：保持当前API状态或重新检查
                if (ApiStatus == ApiHealthStatus.Checking)
                {
                    ApiStatusMessage = "正在检查远程API连接...";
                }
                Logger.LogInformation("切换到远程模式，保持当前API状态");
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

                // Issue #1825: 检查连接模式
                if (ConnectionMode == ConnectionMode.Local)
                {
                    ErrorMessage = "本地模式暂未实现，该功能计划在 v2.0 版本中提供。\n请切换到\"远程模式\"以使用 WebAPI 服务。";
                    Logger.LogWarning("用户尝试使用本地模式登录，但该功能尚未实现");
                    return;
                }

                // 构造登录请求
                var loginRequest = new LoginRequest
                {
                    UserName = Username,
                    Password = Password,
                    RememberMe = RememberMe
                };

                // 调用认证服务（远程模式）
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
                Logger.LogInformation(" 发布 LoginSuccessEvent，触发 MainWindowViewModel 处理后续导航");
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
