using System.Windows;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Login;

/// <summary>
/// 登录流程协调器实现
/// 编排完整的登录流程，包括认证、会话启动、模块加载和导航
/// OpenSpec: refactor-login-authentication (Phase 2.2)
/// 使用LoginStateMachine管理高层认证状态，LoginFlowState用于UI进度反馈
/// </summary>
public class LoginCoordinator : ILoginCoordinator
{
    private readonly ILogger<LoginCoordinator> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly ITokenStorageService _tokenStorageService;
    private readonly ISessionLifecycleManager _sessionLifecycleManager;
    private readonly IModuleLoadingService _moduleLoadingService;
    private readonly IRoleNavigationService _roleNavigationService;
    private readonly ICredentialVault? _credentialVault;
    private readonly IUsernameStorageService? _usernameStorage;
    private readonly ILoginStateMachine _loginStateMachine;
    private readonly object _stateLock = new();

    private LoginFlowState _currentState = LoginFlowState.NotLoggedIn;
    private UserDetailDto? _currentUser;
    private DateTime? _loginTime;
    private DateTime? _lastStateChangeTime;
    private int _loginAttemptCount;
    private int _autoLoginAttemptCount;

    public LoginCoordinator(
        ILogger<LoginCoordinator> logger,
        IAuthenticationService authenticationService,
        ITokenStorageService tokenStorageService,
        ISessionLifecycleManager sessionLifecycleManager,
        IModuleLoadingService moduleLoadingService,
        IRoleNavigationService roleNavigationService,
        ILoginStateMachine loginStateMachine,
        ICredentialVault? credentialVault = null,
        IUsernameStorageService? usernameStorage = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _tokenStorageService = tokenStorageService ?? throw new ArgumentNullException(nameof(tokenStorageService));
        _sessionLifecycleManager = sessionLifecycleManager ?? throw new ArgumentNullException(nameof(sessionLifecycleManager));
        _moduleLoadingService = moduleLoadingService ?? throw new ArgumentNullException(nameof(moduleLoadingService));
        _roleNavigationService = roleNavigationService ?? throw new ArgumentNullException(nameof(roleNavigationService));
        _loginStateMachine = loginStateMachine ?? throw new ArgumentNullException(nameof(loginStateMachine));
        _credentialVault = credentialVault;
        _usernameStorage = usernameStorage;
    }

    /// <inheritdoc />
    public LoginFlowState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    /// <inheritdoc />
    public bool IsLoggedIn
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState == LoginFlowState.LoggedIn;
            }
        }
    }

    /// <inheritdoc />
    public UserDetailDto? CurrentUser
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUser;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<LoginFlowStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<LoginSuccessEventArgs>? LoginSucceeded;

    /// <inheritdoc />
    public event EventHandler? LogoutCompleted;

    /// <inheritdoc />
    public async Task<LoginResult> LoginAsync(string username, string password, bool rememberCredentials = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        lock (_stateLock)
        {
            _loginAttemptCount++;
        }

        _logger.LogInformation("开始登录流程 [用户: {Username}, 尝试次数: {AttemptCount}]",
            username, _loginAttemptCount);

        // OpenSpec: refactor-login-authentication (Phase 2.2) - 触发状态机开始登录
        _loginStateMachine.Fire(LoginTrigger.StartLogin);

        try
        {
            // Step 1: 认证
            TransitionTo(LoginFlowState.Authenticating, "正在验证身份...");

            var loginRequest = new LoginRequest { UserName = username, Password = password };
            var result = await _authenticationService.LoginAsync(loginRequest);

            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("登录认证失败 [用户: {Username}]", username);
                // OpenSpec: refactor-login-authentication (Phase 2.2) - 触发登录失败
                _loginStateMachine.Fire(LoginTrigger.LoginFailure);
                TransitionTo(LoginFlowState.NotLoggedIn);
                return LoginResult.Failed(result.Message ?? "认证失败");
            }

            var loginResponse = result.Data;
            var user = loginResponse.User;

            // Step 2: 保存认证信息
            await _tokenStorageService.SaveAuthenticationAsync(loginResponse, rememberCredentials);

            // OpenSpec: refactor-login-authentication (CVT-001)
            // 当rememberCredentials=true且有AutoLoginToken时，保存到CredentialVault
            if (rememberCredentials && !string.IsNullOrEmpty(loginResponse.AutoLoginToken) && _credentialVault != null)
            {
                var saved = await _credentialVault.SaveAutoLoginTokenAsync(user.UserName!, loginResponse.AutoLoginToken);
                if (saved)
                {
                    _logger.LogInformation("AutoLoginToken已保存到CredentialVault - UserName: {UserName}", user.UserName);
                }
                else
                {
                    _logger.LogWarning("保存AutoLoginToken失败 - UserName: {UserName}", user.UserName);
                }
            }

            // Step 3: 启动会话
            TransitionTo(LoginFlowState.StartingSession, "正在启动会话...");
            await StartSessionAsync(user, loginResponse.ExpiresAt);

            // Step 4: 加载模块
            TransitionTo(LoginFlowState.LoadingModules, "正在加载模块...");
            await LoadModulesForUserAsync(user);

            // Step 5: 导航到首页
            TransitionTo(LoginFlowState.Navigating, "正在跳转...");
            await NavigateToRoleHomeAsync(user);

            // 完成登录
            // OpenSpec: refactor-login-authentication (Phase 2.2) - 触发登录成功
            _loginStateMachine.Fire(LoginTrigger.LoginSuccess);
            TransitionTo(LoginFlowState.LoggedIn);
            LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs(user, loginResponse.ExpiresAt, isAutoLogin: false));

            _logger.LogInformation("登录流程完成 [用户: {Username}, 角色: {Role}]",
                user.UserName, user.Role);

            return LoginResult.Succeeded(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录流程异常 [用户: {Username}]", username);
            // OpenSpec: refactor-login-authentication (Phase 2.2) - 异常时触发登录失败
            _loginStateMachine.Fire(LoginTrigger.LoginFailure);
            TransitionTo(LoginFlowState.NotLoggedIn);
            return LoginResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex));
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryAutoLoginAsync()
    {
        lock (_stateLock)
        {
            _autoLoginAttemptCount++;
        }

        _logger.LogDebug("尝试自动登录 [尝试次数: {AttemptCount}]", _autoLoginAttemptCount);

        // OpenSpec: refactor-login-authentication (Phase 2.2) - 触发自动登录开始
        _loginStateMachine.Fire(LoginTrigger.StartAutoLogin);

        try
        {
            // 策略1: 尝试使用存储的JWT Token进行验证
            var token = await _tokenStorageService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                TransitionTo(LoginFlowState.Authenticating, "正在验证Token...");
                var validationResult = await _authenticationService.ValidateTokenAsync(token);

                if (validationResult.IsSuccess && validationResult.Data?.IsValid == true)
                {
                    var loginResponse = await _tokenStorageService.GetLoginResponseAsync();
                    if (loginResponse?.User != null)
                    {
                        var user = loginResponse.User;
                        await HandleLoginSuccessAsync(user, loginResponse.ExpiresAt);
                        // OpenSpec: Phase 2.2 - LoginSuccess已在HandleLoginSuccessAsync中触发
                        LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs(user, loginResponse.ExpiresAt, isAutoLogin: true));
                        _logger.LogInformation("JWT Token验证成功，自动登录完成 [用户: {Username}]", user.UserName);
                        return true;
                    }
                }

                _logger.LogDebug("JWT Token无效，尝试使用AutoLoginToken");
            }

            // 策略2: 尝试使用CredentialVault中的AutoLoginToken
            // OpenSpec: refactor-login-authentication (CVT-001)
            if (_credentialVault != null && _usernameStorage != null)
            {
                var savedUsername = await _usernameStorage.GetSavedUsernameAsync();
                if (!string.IsNullOrEmpty(savedUsername))
                {
                    var autoLoginToken = await _credentialVault.GetAutoLoginTokenAsync(savedUsername);
                    if (!string.IsNullOrEmpty(autoLoginToken))
                    {
                        _logger.LogDebug("发现AutoLoginToken，尝试自动登录 [用户: {Username}]", savedUsername);
                        TransitionTo(LoginFlowState.Authenticating, "正在自动登录...");

                        var autoLoginRequest = new AutoLoginRequest
                        {
                            UserName = savedUsername,
                            AutoLoginToken = autoLoginToken
                        };

                        var result = await _authenticationService.LoginWithAutoTokenAsync(autoLoginRequest);
                        if (result.IsSuccess && result.Data != null)
                        {
                            var loginResponse = result.Data;
                            var user = loginResponse.User;

                            // 保存新的认证信息
                            await _tokenStorageService.SaveAuthenticationAsync(loginResponse, rememberMe: true);

                            // 保存新的AutoLoginToken（Token轮换）
                            if (!string.IsNullOrEmpty(loginResponse.AutoLoginToken))
                            {
                                await _credentialVault.SaveAutoLoginTokenAsync(user.UserName!, loginResponse.AutoLoginToken);
                                _logger.LogDebug("AutoLoginToken已轮换 [用户: {Username}]", user.UserName);
                            }

                            // 执行登录后流程
                            await HandleLoginSuccessAsync(user, loginResponse.ExpiresAt);
                            // OpenSpec: Phase 2.2 - LoginSuccess已在HandleLoginSuccessAsync中触发
                            LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs(user, loginResponse.ExpiresAt, isAutoLogin: true));
                            _logger.LogInformation("AutoLoginToken自动登录成功 [用户: {Username}]", user.UserName);
                            return true;
                        }
                        else
                        {
                            _logger.LogWarning("AutoLoginToken自动登录失败: {Message}", result.Message);
                            // AutoLoginToken无效，清除本地存储
                            await _credentialVault.ClearCredentialsAsync(savedUsername);
                        }
                    }
                }
            }

            // 所有自动登录策略都失败
            _logger.LogDebug("无可用的自动登录凭据");
            // OpenSpec: refactor-login-authentication (Phase 2.2) - 自动登录失败回到NotLoggedIn
            _loginStateMachine.Fire(LoginTrigger.LoginFailure);
            await ClearInvalidTokenAsync();
            TransitionTo(LoginFlowState.NotLoggedIn);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动登录异常");
            // OpenSpec: refactor-login-authentication (Phase 2.2) - 异常时触发登录失败
            _loginStateMachine.Fire(LoginTrigger.LoginFailure);
            TransitionTo(LoginFlowState.NotLoggedIn);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task HandleLoginSuccessAsync(UserDetailDto user, DateTime tokenExpiresAt)
    {
        ArgumentNullException.ThrowIfNull(user);

        _logger.LogInformation("处理登录成功 [用户: {Username}, Token过期: {ExpiresAt}]",
            user.UserName, tokenExpiresAt);

        try
        {
            // Step 1: 启动会话
            TransitionTo(LoginFlowState.StartingSession, "正在启动会话...");
            await StartSessionAsync(user, tokenExpiresAt);

            // Step 2: 加载模块
            TransitionTo(LoginFlowState.LoadingModules, "正在加载模块...");
            await LoadModulesForUserAsync(user);

            // Step 3: 导航到首页
            TransitionTo(LoginFlowState.Navigating, "正在跳转...");
            await NavigateToRoleHomeAsync(user);

            // 完成
            // OpenSpec: refactor-login-authentication (Phase 2.2) - 触发登录成功
            _loginStateMachine.Fire(LoginTrigger.LoginSuccess);
            TransitionTo(LoginFlowState.LoggedIn);

            _logger.LogInformation("登录成功处理完成 [用户: {Username}]", user.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理登录成功时发生异常");
            // OpenSpec: refactor-login-authentication (Phase 2.2) - 异常时触发登录失败
            _loginStateMachine.Fire(LoginTrigger.LoginFailure);
            TransitionTo(LoginFlowState.NotLoggedIn);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        _logger.LogInformation("开始登出流程 [用户: {Username}]", _currentUser?.UserName);

        // OpenSpec: refactor-login-authentication (Phase 2.2) - 触发开始登出
        _loginStateMachine.Fire(LoginTrigger.StartLogout);
        TransitionTo(LoginFlowState.LoggingOut, "正在登出...");

        try
        {
            // 结束会话
            await _sessionLifecycleManager.EndSessionAsync();

            // 调用认证服务登出
            await _authenticationService.LogoutAsync();

            // 清理状态
            lock (_stateLock)
            {
                _currentUser = null;
                _loginTime = null;
            }

            // OpenSpec: refactor-login-authentication (Phase 2.2) - 触发登出成功
            _loginStateMachine.Fire(LoginTrigger.LogoutSuccess);
            TransitionTo(LoginFlowState.NotLoggedIn);
            LogoutCompleted?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation("登出流程完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出流程异常");
            // OpenSpec: refactor-login-authentication (Phase 2.2) - 登出失败（回滚到LoggedIn或强制NotLoggedIn）
            // 即使异常也强制转换到未登录状态（用户体验优先，本地登出应该始终成功）
            _loginStateMachine.Fire(LoginTrigger.LogoutSuccess); // 本地登出成功
            TransitionTo(LoginFlowState.NotLoggedIn);
            throw;
        }
    }

    /// <inheritdoc />
    public LoginFlowDiagnostics GetDiagnostics()
    {
        lock (_stateLock)
        {
            return new LoginFlowDiagnostics(
                CurrentState: _currentState,
                IsLoggedIn: _currentState == LoginFlowState.LoggedIn,
                UserName: _currentUser?.UserName,
                UserRole: _currentUser?.Role.ToString(),
                LoginTime: _loginTime,
                LastStateChangeTime: _lastStateChangeTime,
                LoginAttemptCount: _loginAttemptCount,
                AutoLoginAttemptCount: _autoLoginAttemptCount
            );
        }
    }

    /// <summary>
    /// 启动会话
    /// </summary>
    private async Task StartSessionAsync(UserDetailDto user, DateTime tokenExpiresAt)
    {
        lock (_stateLock)
        {
            _currentUser = user;
            _loginTime = DateTime.Now;
        }

        var userRole = user.Role.ToString();
        await _sessionLifecycleManager.StartSessionAsync(user.UserName!, userRole, tokenExpiresAt);

        _logger.LogDebug("会话已启动 [用户: {Username}]", user.UserName);
    }

    /// <summary>
    /// 为用户加载所需模块
    /// </summary>
    private async Task LoadModulesForUserAsync(UserDetailDto user)
    {
        bool isAdmin = user.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true ||
                       user.Role == UserRole.Admin;

        // 加载基础模块
        await _moduleLoadingService.LoadModulesAsync(new[] { "PatientsModule" });

        // 根据角色加载额外模块
        if (isAdmin)
        {
            _logger.LogDebug("管理员登录，加载管理工作台模块");
            await _moduleLoadingService.LoadModulesAsync(new[]
            {
                "UsersModule",
                "HerbsModule",
                "FormulaModule",
                // [已删除] ConsultationModule - 功能已迁移到MedicalCase模块
                "MedicalCaseModule",
                "PrescriptionsModule"
            });
        }

        _logger.LogDebug("角色模块加载完成 [角色: {Role}]", user.Role);
    }

    /// <summary>
    /// 导航到角色首页
    /// </summary>
    private Task NavigateToRoleHomeAsync(UserDetailDto user)
    {
        var roleName = user.Role.ToString();

        // 在单元测试中，Application.Current 为 null，直接调用导航服务
        if (Application.Current?.Dispatcher is null)
        {
            try
            {
                _roleNavigationService.NavigateToRoleHome(roleName);
                _logger.LogDebug("角色导航完成 [角色: {Role}]", roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "角色导航失败 [角色: {Role}]", roleName);
                throw;
            }
            return Task.CompletedTask;
        }

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                _roleNavigationService.NavigateToRoleHome(roleName);
                _logger.LogDebug("角色导航完成 [角色: {Role}]", roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "角色导航失败 [角色: {Role}]", roleName);
                throw;
            }
        }).Task;
    }

    /// <summary>
    /// 清除无效Token
    /// </summary>
    private async Task ClearInvalidTokenAsync()
    {
        try
        {
            _authenticationService.ClearAuthInfo();
            _logger.LogDebug("已清除无效Token");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清除无效Token时发生异常");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 转换流程状态
    /// </summary>
    private void TransitionTo(LoginFlowState newState, string? statusMessage = null)
    {
        LoginFlowState previousState;

        lock (_stateLock)
        {
            if (_currentState == newState)
            {
                return;
            }

            previousState = _currentState;
            _currentState = newState;
            _lastStateChangeTime = DateTime.Now;
        }

        _logger.LogDebug("登录流程状态转换: {From} -> {To} [{Message}]",
            previousState, newState, statusMessage ?? "");

        StateChanged?.Invoke(this, new LoginFlowStateChangedEventArgs(previousState, newState, statusMessage));
    }
}
