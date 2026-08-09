using System.Windows;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Login;

/// <summary>
/// 登录流程协调器实现
/// </summary>
public class LoginCoordinator : ILoginCoordinator, IDisposable
{
    private readonly ILogger<LoginCoordinator> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly ITokenStorageService _tokenStorageService;
    private readonly ISessionLifecycleManager _sessionLifecycleManager;
    private readonly IApplicationBootstrapper _applicationBootstrapper;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ISessionManager _sessionManager;
    private readonly ICredentialVault? _credentialVault;
    private readonly IUsernameStorageService? _usernameStorage;
    private readonly IAuthenticationStateMachine _stateMachine;
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _loginLock = new(1, 1);

    private UserDetailDto? _currentUser;
    private DateTime? _loginTime;
    private DateTime? _lastStateChangeTime;
    private int _loginAttemptCount;

    public LoginCoordinator(
        ILogger<LoginCoordinator> logger,
        IAuthenticationService authenticationService,
        ITokenStorageService tokenStorageService,
        ISessionLifecycleManager sessionLifecycleManager,
        IApplicationBootstrapper applicationBootstrapper,
        INavigationCoordinator navigationCoordinator,
        ISessionManager sessionManager,
        IAuthenticationStateMachine stateMachine,
        ICredentialVault? credentialVault = null,
        IUsernameStorageService? usernameStorage = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _tokenStorageService = tokenStorageService ?? throw new ArgumentNullException(nameof(tokenStorageService));
        _sessionLifecycleManager = sessionLifecycleManager ?? throw new ArgumentNullException(nameof(sessionLifecycleManager));
        _applicationBootstrapper = applicationBootstrapper ?? throw new ArgumentNullException(nameof(applicationBootstrapper));
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
    public async Task<CommandResult<UserDetailDto>> LoginAsync(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        await _loginLock.WaitAsync(TimeSpan.FromSeconds(30));
        try
        {
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

                if (!result.Success || result.Data == null)
                {
                    _logger.LogWarning("登录认证失败 [用户: {Username}]", username);
                    _stateMachine.Fire(AuthEvent.LoginFailure, result.Error ?? "认证失败");
                    return CommandResult<UserDetailDto>.Failed(result.Error ?? "认证失败");
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
                return CommandResult<UserDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex));
            }
        }
        finally
        {
            _loginLock.Release();
        }
    }

    private async Task<CommandResult<UserDetailDto>> CompleteLoginFlowAsync(UserDetailDto user, DateTime tokenExpiresAt)
    {
        await StartSessionAsync(user, tokenExpiresAt);
        _stateMachine.Fire(AuthEvent.ProfileLoaded, "正在加载模块...");

        await LoadModulesForUserAsync(user);
        _stateMachine.Fire(AuthEvent.ModulesLoaded, "正在跳转...");

        await NavigateToRoleHomeAsync(user);
        _stateMachine.Fire(AuthEvent.NavigationCompleted);

        RaiseLoginSucceeded(user, tokenExpiresAt);

        _logger.LogInformation("登录流程完成 [用户: {Username}, 角色: {Role}]",
            user.UserName, user.Role);

        return CommandResult<UserDetailDto>.Succeeded(user);
    }

    /// <summary>
    /// 逐订阅者触发登录成功事件：单个订阅者异常不中断发布链，也不影响登录流程结果
    /// </summary>
    private void RaiseLoginSucceeded(UserDetailDto user, DateTime tokenExpiresAt)
    {
        var args = new LoginSuccessEventArgs(user, tokenExpiresAt);
        var subscribers = LoginSucceeded?.GetInvocationList();
        if (subscribers == null) return;

        foreach (var subscriber in subscribers)
        {
            try
            {
                ((EventHandler<LoginSuccessEventArgs>)subscriber).Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录成功事件订阅者处理异常");
            }
        }
    }

    public async Task LogoutAsync()
    {
        // 与会话变更入口（LoginAsync/LogoutAsync）串行化，避免与登录流程交错
        await _loginLock.WaitAsync(TimeSpan.FromSeconds(30));
        try
        {
            _logger.LogInformation("开始登出流程 [用户: {Username}]", _currentUser?.UserName);

            _stateMachine.Fire(AuthEvent.StartLogout, "正在登出...");

            try
            {
                await _sessionLifecycleManager.EndSessionAsync();

                _sessionManager.ClearSession();

                await _authenticationService.LogoutAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出流程异常");
                _stateMachine.Fire(AuthEvent.LogoutFailure, ClientErrorMessageMapper.GetSafeOperationFailureMessage("登出", ex));
                throw;
            }
            finally
            {
                // 无论登出成功与否都清理本地会话，避免 _currentUser 残留
                lock (_stateLock)
                {
                    _currentUser = null;
                    _loginTime = null;
                }
            }

            _stateMachine.Fire(AuthEvent.LogoutSuccess);
            LogoutCompleted?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation("登出流程完成");
        }
        finally
        {
            _loginLock.Release();
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
        await _applicationBootstrapper.LoadModulesForRoleAsync(user.Role);

        _logger.LogDebug("角色模块加载完成 [角色: {Role}]", user.Role);
    }

    private Task NavigateToRoleHomeAsync(UserDetailDto user)
    {
        var role = user.Role;

        if (Application.Current?.Dispatcher is null)
        {
            try
            {
                _ = _navigationCoordinator.NavigateToHome(role);
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
                _ = _navigationCoordinator.NavigateToHome(role);
                _logger.LogDebug("角色导航完成 [角色: {Role}]", role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "角色导航失败 [角色: {Role}]", role);
                throw;
            }
        }).Task;
    }

    public void Dispose()
    {
        _loginLock.Dispose();
    }
}
