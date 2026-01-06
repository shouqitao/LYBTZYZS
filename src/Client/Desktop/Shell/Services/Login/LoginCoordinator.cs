using System.Windows;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Desktop.Contracts.Security;
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
/// OpenSpec: refactor-auth-role-system (Phase 1.1)
/// 使用统一的 AuthenticationStateMachine 替代原有的双状态机架构
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
    private readonly IAuthenticationStateMachine _stateMachine;
    private readonly object _stateLock = new();

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
        IAuthenticationStateMachine stateMachine,
        ICredentialVault? credentialVault = null,
        IUsernameStorageService? usernameStorage = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _tokenStorageService = tokenStorageService ?? throw new ArgumentNullException(nameof(tokenStorageService));
        _sessionLifecycleManager = sessionLifecycleManager ?? throw new ArgumentNullException(nameof(sessionLifecycleManager));
        _moduleLoadingService = moduleLoadingService ?? throw new ArgumentNullException(nameof(moduleLoadingService));
        _roleNavigationService = roleNavigationService ?? throw new ArgumentNullException(nameof(roleNavigationService));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _credentialVault = credentialVault;
        _usernameStorage = usernameStorage;

        // 订阅状态机事件，转发给外部订阅者
        _stateMachine.StateChanged += OnStateMachineStateChanged;
    }

    /// <inheritdoc />
    public AuthState CurrentState => _stateMachine.CurrentState;

    /// <inheritdoc />
    public bool IsLoggedIn => _stateMachine.IsAuthenticated;

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
    public event EventHandler<AuthStateChangedEventArgs>? StateChanged;

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

        // OpenSpec: refactor-auth-role-system - 使用统一状态机
        _stateMachine.Fire(AuthEvent.StartLogin, "正在验证身份...");

        try
        {
            // Step 1: 认证
            var loginRequest = new LoginRequest { UserName = username, Password = password };
            var result = await _authenticationService.LoginAsync(loginRequest);

            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("登录认证失败 [用户: {Username}]", username);
                _stateMachine.Fire(AuthEvent.LoginFailure, result.Message ?? "认证失败");
                return LoginResult.Failed(result.Message ?? "认证失败");
            }

            var loginResponse = result.Data;
            var user = loginResponse.User;

            // 凭证验证成功，进入LoadingProfile阶段
            _stateMachine.Fire(AuthEvent.CredentialsValidated, "正在启动会话...");

            // Step 2: 保存认证信息
            await _tokenStorageService.SaveAuthenticationAsync(loginResponse, rememberCredentials);

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
            await StartSessionAsync(user, loginResponse.ExpiresAt);
            _stateMachine.Fire(AuthEvent.ProfileLoaded, "正在加载模块...");

            // Step 4: 加载模块
            await LoadModulesForUserAsync(user);
            _stateMachine.Fire(AuthEvent.ModulesLoaded, "正在跳转...");

            // Step 5: 导航到首页
            await NavigateToRoleHomeAsync(user);
            _stateMachine.Fire(AuthEvent.NavigationCompleted);

            // 完成登录
            LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs(user, loginResponse.ExpiresAt, isAutoLogin: false));

            _logger.LogInformation("登录流程完成 [用户: {Username}, 角色: {Role}]",
                user.UserName, user.Role);

            return LoginResult.Succeeded(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录流程异常 [用户: {Username}]", username);
            _stateMachine.Fire(AuthEvent.LoginFailure, ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex));
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

        // OpenSpec: refactor-auth-role-system - 使用统一状态机
        _stateMachine.Fire(AuthEvent.StartAutoLogin, "正在验证Token...");

        try
        {
            // 策略1: 尝试使用存储的JWT Token进行验证
            var token = await _tokenStorageService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                var validationResult = await _authenticationService.ValidateTokenAsync(token);

                if (validationResult.IsSuccess && validationResult.Data?.IsValid == true)
                {
                    var loginResponse = await _tokenStorageService.GetLoginResponseAsync();
                    if (loginResponse?.User != null)
                    {
                        var user = loginResponse.User;

                        // Token验证成功，进入LoadingProfile
                        _stateMachine.Fire(AuthEvent.TokenValidated, "正在启动会话...");

                        await StartSessionAsync(user, loginResponse.ExpiresAt);
                        _stateMachine.Fire(AuthEvent.ProfileLoaded, "正在加载模块...");

                        await LoadModulesForUserAsync(user);
                        _stateMachine.Fire(AuthEvent.ModulesLoaded, "正在跳转...");

                        await NavigateToRoleHomeAsync(user);
                        _stateMachine.Fire(AuthEvent.NavigationCompleted);

                        LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs(user, loginResponse.ExpiresAt, isAutoLogin: true));
                        _logger.LogInformation("JWT Token验证成功，自动登录完成 [用户: {Username}]", user.UserName);
                        return true;
                    }
                }

                _logger.LogDebug("JWT Token无效，尝试使用AutoLoginToken");
            }

            // 策略2: 尝试使用CredentialVault中的AutoLoginToken
            if (_credentialVault != null && _usernameStorage != null)
            {
                var savedUsername = await _usernameStorage.GetSavedUsernameAsync();
                if (!string.IsNullOrEmpty(savedUsername))
                {
                    var autoLoginToken = await _credentialVault.GetAutoLoginTokenAsync(savedUsername);
                    if (!string.IsNullOrEmpty(autoLoginToken))
                    {
                        _logger.LogDebug("发现AutoLoginToken，尝试自动登录 [用户: {Username}]", savedUsername);

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

                            // Token验证成功
                            _stateMachine.Fire(AuthEvent.TokenValidated, "正在启动会话...");

                            // 保存新的认证信息
                            await _tokenStorageService.SaveAuthenticationAsync(loginResponse, rememberMe: true);

                            // 保存新的AutoLoginToken（Token轮换）
                            if (!string.IsNullOrEmpty(loginResponse.AutoLoginToken))
                            {
                                await _credentialVault.SaveAutoLoginTokenAsync(user.UserName!, loginResponse.AutoLoginToken);
                                _logger.LogDebug("AutoLoginToken已轮换 [用户: {Username}]", user.UserName);
                            }

                            // 执行登录后流程
                            await StartSessionAsync(user, loginResponse.ExpiresAt);
                            _stateMachine.Fire(AuthEvent.ProfileLoaded, "正在加载模块...");

                            await LoadModulesForUserAsync(user);
                            _stateMachine.Fire(AuthEvent.ModulesLoaded, "正在跳转...");

                            await NavigateToRoleHomeAsync(user);
                            _stateMachine.Fire(AuthEvent.NavigationCompleted);

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
            _stateMachine.Fire(AuthEvent.LoginFailure);
            await ClearInvalidTokenAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动登录异常");
            _stateMachine.Fire(AuthEvent.LoginFailure);
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
            // 假设已经通过认证，从LoadingProfile开始
            // 如果当前状态不是ValidatingToken或Authenticating，先重置
            if (_stateMachine.CurrentState == AuthState.Idle)
            {
                _stateMachine.Fire(AuthEvent.StartLogin);
                _stateMachine.Fire(AuthEvent.CredentialsValidated, "正在启动会话...");
            }

            // Step 1: 启动会话
            await StartSessionAsync(user, tokenExpiresAt);
            _stateMachine.Fire(AuthEvent.ProfileLoaded, "正在加载模块...");

            // Step 2: 加载模块
            await LoadModulesForUserAsync(user);
            _stateMachine.Fire(AuthEvent.ModulesLoaded, "正在跳转...");

            // Step 3: 导航到首页
            await NavigateToRoleHomeAsync(user);
            _stateMachine.Fire(AuthEvent.NavigationCompleted);

            _logger.LogInformation("登录成功处理完成 [用户: {Username}]", user.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理登录成功时发生异常");
            _stateMachine.Fire(AuthEvent.LoginFailure);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task LogoutAsync()
    {
        _logger.LogInformation("开始登出流程 [用户: {Username}]", _currentUser?.UserName);

        // OpenSpec: refactor-auth-role-system - 使用统一状态机
        _stateMachine.Fire(AuthEvent.StartLogout, "正在登出...");

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

            _stateMachine.Fire(AuthEvent.LogoutSuccess);
            LogoutCompleted?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation("登出流程完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出流程异常");
            // 即使异常也强制转换到未登录状态（用户体验优先，本地登出应该始终成功）
            _stateMachine.Fire(AuthEvent.LogoutSuccess);
            throw;
        }
    }

    /// <inheritdoc />
    public LoginFlowDiagnostics GetDiagnostics()
    {
        lock (_stateLock)
        {
            return new LoginFlowDiagnostics(
                CurrentState: _stateMachine.CurrentState,
                IsLoggedIn: _stateMachine.IsAuthenticated,
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
    /// 状态机状态变更事件处理
    /// </summary>
    private void OnStateMachineStateChanged(object? sender, AuthStateChangedEventArgs e)
    {
        lock (_stateLock)
        {
            _lastStateChangeTime = e.Timestamp;
        }

        // 转发事件给外部订阅者
        StateChanged?.Invoke(this, e);
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
                "MedicalCaseModule"
                // [已删除] "PrescriptionsModule" - 空壳模块已移除
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
}
