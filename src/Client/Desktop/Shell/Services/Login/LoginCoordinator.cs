using System.Windows;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Login;

/// <summary>
/// 登录流程协调器实现
/// OpenSpec: refactor-auth-role-system (Phase 1.1) - 统一 AuthenticationStateMachine
/// </summary>
public class LoginCoordinator : ILoginCoordinator
{
    private readonly ILogger<LoginCoordinator> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly ITokenStorageService _tokenStorageService;
    private readonly ISessionLifecycleManager _sessionLifecycleManager;
    private readonly IModuleLoadingService _moduleLoadingService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ISessionManager _sessionManager;
    private readonly ICredentialVault? _credentialVault;
    private readonly IUsernameStorageService? _usernameStorage;
    private readonly IAuthenticationStateMachine _stateMachine;
    private readonly object _stateLock = new();

    private UserDetailDto? _currentUser;
    private DateTime? _loginTime;
    private DateTime? _lastStateChangeTime;
    private int _loginAttemptCount;

    public LoginCoordinator(
        ILogger<LoginCoordinator> logger,
        IAuthenticationService authenticationService,
        ITokenStorageService tokenStorageService,
        ISessionLifecycleManager sessionLifecycleManager,
        IModuleLoadingService moduleLoadingService,
        INavigationCoordinator navigationCoordinator,
        ISessionManager sessionManager,
        IAuthenticationStateMachine stateMachine,
        IConfiguration configuration,
        ICredentialVault? credentialVault = null,
        IUsernameStorageService? usernameStorage = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _tokenStorageService = tokenStorageService ?? throw new ArgumentNullException(nameof(tokenStorageService));
        _sessionLifecycleManager = sessionLifecycleManager ?? throw new ArgumentNullException(nameof(sessionLifecycleManager));
        _moduleLoadingService = moduleLoadingService ?? throw new ArgumentNullException(nameof(moduleLoadingService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _credentialVault = credentialVault;
        _usernameStorage = usernameStorage;

        _stateMachine.StateChanged += OnStateMachineStateChanged;
    }

    public AuthState CurrentState => _stateMachine.CurrentState;

    public bool IsLoggedIn => _stateMachine.IsAuthenticated;

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

    public event EventHandler<AuthStateChangedEventArgs>? StateChanged;

    public event EventHandler<LoginSuccessEventArgs>? LoginSucceeded;

    public event EventHandler? LogoutCompleted;

    /// <summary>
    /// 两种模式统一走 WebAPI 认证 (Remote→远程WebAPI, Local→localhost WebAPI)
    /// </summary>
    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        lock (_stateLock)
        {
            _loginAttemptCount++;
        }

        _logger.LogInformation("开始登录流程 [用户: {Username}, 尝试次数: {AttemptCount}]",
            username, _loginAttemptCount);

        _stateMachine.Fire(AuthEvent.StartLogin, "正在验证身份...");

        try
        {
            var loginRequest = new LoginRequest { UserName = username, Password = password, RememberMe = false };
            var result = await _authenticationService.LoginAsync(loginRequest);

            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("登录认证失败 [用户: {Username}]", username);
                _stateMachine.Fire(AuthEvent.LoginFailure, result.Message ?? "认证失败");
                return LoginResult.Failed(result.Message ?? "认证失败");
            }

            var loginResponse = result.Data;
            var user = loginResponse.User;

            _stateMachine.Fire(AuthEvent.CredentialsValidated, "正在启动会话...");

            await _tokenStorageService.SaveAuthenticationAsync(loginResponse, rememberMe: false);

            return await CompleteLoginFlowAsync(user, loginResponse.ExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录流程异常 [用户: {Username}]", username);
            _stateMachine.Fire(AuthEvent.LoginFailure, ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex));
            return LoginResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex));
        }
    }

    private async Task<LoginResult> CompleteLoginFlowAsync(UserDetailDto user, DateTime tokenExpiresAt)
    {
        await StartSessionAsync(user, tokenExpiresAt);
        _stateMachine.Fire(AuthEvent.ProfileLoaded, "正在加载模块...");

        await LoadModulesForUserAsync(user);
        _stateMachine.Fire(AuthEvent.ModulesLoaded, "正在跳转...");

        await NavigateToRoleHomeAsync(user);
        _stateMachine.Fire(AuthEvent.NavigationCompleted);

        LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs(user, tokenExpiresAt));

        _logger.LogInformation("登录流程完成 [用户: {Username}, 角色: {Role}]",
            user.UserName, user.Role);

        return LoginResult.Succeeded(user);
    }

    public async Task HandleLoginSuccessAsync(UserDetailDto user, DateTime tokenExpiresAt)
    {
        ArgumentNullException.ThrowIfNull(user);

        _logger.LogInformation("处理登录成功 [用户: {Username}, Token过期: {ExpiresAt}]",
            user.UserName, tokenExpiresAt);

        try
        {
            if (_stateMachine.CurrentState == AuthState.Idle)
            {
                _stateMachine.Fire(AuthEvent.StartLogin);
                _stateMachine.Fire(AuthEvent.CredentialsValidated, "正在启动会话...");
            }

            await StartSessionAsync(user, tokenExpiresAt);
            _stateMachine.Fire(AuthEvent.ProfileLoaded, "正在加载模块...");

            await LoadModulesForUserAsync(user);
            _stateMachine.Fire(AuthEvent.ModulesLoaded, "正在跳转...");

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

    public async Task LogoutAsync()
    {
        _logger.LogInformation("开始登出流程 [用户: {Username}]", _currentUser?.UserName);

        _stateMachine.Fire(AuthEvent.StartLogout, "正在登出...");

        try
        {
            await _sessionLifecycleManager.EndSessionAsync();

            _sessionManager.ClearSession();

            await _authenticationService.LogoutAsync();

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
            _stateMachine.Fire(AuthEvent.LogoutSuccess);
            throw;
        }
    }

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
                LoginAttemptCount: _loginAttemptCount
            );
        }
    }

    private void OnStateMachineStateChanged(object? sender, AuthStateChangedEventArgs e)
    {
        lock (_stateLock)
        {
            _lastStateChangeTime = e.Timestamp;
        }

        StateChanged?.Invoke(this, e);
    }

    private async Task StartSessionAsync(UserDetailDto user, DateTime tokenExpiresAt)
    {
        lock (_stateLock)
        {
            _currentUser = user;
            _loginTime = DateTime.UtcNow;
        }

        var userRole = user.Role.ToString();
        await _sessionLifecycleManager.StartSessionAsync(user.UserName!, userRole, tokenExpiresAt);

        _logger.LogDebug("会话已启动 [用户: {Username}]", user.UserName);
    }

    private async Task LoadModulesForUserAsync(UserDetailDto user)
    {
        bool isAdmin = user.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true ||
                       user.Role == UserRole.Admin;

        await _moduleLoadingService.LoadModulesAsync(new[] { "PatientsModule" });

        if (isAdmin)
        {
            _logger.LogDebug("管理员登录，加载管理工作台模块");
            await _moduleLoadingService.LoadModulesAsync(new[]
            {
                "UsersModule",
                "HerbsModule",
                "FormulaModule",
                "MedicalCaseModule"
            });
        }

        _logger.LogDebug("角色模块加载完成 [角色: {Role}]", user.Role);
    }

    private Task NavigateToRoleHomeAsync(UserDetailDto user)
    {
        var role = user.Role;

        if (Application.Current?.Dispatcher is null)
        {
            try
            {
                _navigationCoordinator.NavigateToHome(role);
                _logger.LogDebug("角色导航完成 [角色: {Role}]", role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "角色导航失败 [角色: {Role}]", role);
                throw;
            }
            return Task.CompletedTask;
        }

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                _navigationCoordinator.NavigateToHome(role);
                _logger.LogDebug("角色导航完成 [角色: {Role}]", role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "角色导航失败 [角色: {Role}]", role);
                throw;
            }
        }).Task;
    }

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
