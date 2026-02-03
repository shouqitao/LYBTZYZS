using System.Windows;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
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
/// 编排完整的登录流程，包括认证、会话启动、模块加载和导航
/// OpenSpec: refactor-auth-role-system (Phase 1.1)
/// 使用统一的 AuthenticationStateMachine 替代原有的双状态机架构
/// OpenSpec: implement-local-mode - 支持本地模式认证
/// </summary>
public class LoginCoordinator : ILoginCoordinator
{
    private readonly ILogger<LoginCoordinator> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly ITokenStorageService _tokenStorageService;
    private readonly ISessionLifecycleManager _sessionLifecycleManager;
    private readonly IModuleLoadingService _moduleLoadingService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ICredentialVault? _credentialVault;
    private readonly IUsernameStorageService? _usernameStorage;
    private readonly IAuthenticationStateMachine _stateMachine;
    private readonly ILocalAuthService? _localAuthService;
    private readonly ConnectionMode _connectionMode;
    private readonly object _stateLock = new();

    private UserDetailDto? _currentUser;
    private DateTime? _loginTime;
    private DateTime? _lastStateChangeTime;
    private int _loginAttemptCount;
    // OpenSpec: simplify-login-options - 移除 _autoLoginAttemptCount 和 _suppressAutoLogin

    public LoginCoordinator(
        ILogger<LoginCoordinator> logger,
        IAuthenticationService authenticationService,
        ITokenStorageService tokenStorageService,
        ISessionLifecycleManager sessionLifecycleManager,
        IModuleLoadingService moduleLoadingService,
        INavigationCoordinator navigationCoordinator,
        IAuthenticationStateMachine stateMachine,
        IConfiguration configuration,
        ICredentialVault? credentialVault = null,
        IUsernameStorageService? usernameStorage = null,
        ILocalAuthService? localAuthService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _tokenStorageService = tokenStorageService ?? throw new ArgumentNullException(nameof(tokenStorageService));
        _sessionLifecycleManager = sessionLifecycleManager ?? throw new ArgumentNullException(nameof(sessionLifecycleManager));
        _moduleLoadingService = moduleLoadingService ?? throw new ArgumentNullException(nameof(moduleLoadingService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _credentialVault = credentialVault;
        _usernameStorage = usernameStorage;
        _localAuthService = localAuthService;

        // OpenSpec: implement-local-mode - 读取连接模式
        var modeString = configuration?["ConnectionMode"];
        _connectionMode = Enum.TryParse<ConnectionMode>(modeString, ignoreCase: true, out var mode)
            ? mode
            : ConnectionMode.Remote;

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
    /// OpenSpec: simplify-login-options - 移除rememberCredentials参数，凭证保存由ViewModel处理
    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        lock (_stateLock)
        {
            _loginAttemptCount++;
        }

        _logger.LogInformation("开始登录流程 [用户: {Username}, 尝试次数: {AttemptCount}, 模式: {Mode}]",
            username, _loginAttemptCount, _connectionMode);

        // OpenSpec: refactor-auth-role-system - 使用统一状态机
        _stateMachine.Fire(AuthEvent.StartLogin, "正在验证身份...");

        try
        {
            // OpenSpec: implement-local-mode - 根据连接模式选择认证方式
            if (_connectionMode == ConnectionMode.Local)
            {
                return await LoginLocalAsync(username, password);
            }
            else
            {
                return await LoginRemoteAsync(username, password);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录流程异常 [用户: {Username}]", username);
            _stateMachine.Fire(AuthEvent.LoginFailure, ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex));
            return LoginResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex));
        }
    }


    /// <summary>
    /// 远程模式登录（通过 WebAPI）
    /// </summary>
    private async Task<LoginResult> LoginRemoteAsync(string username, string password)
    {
        // Step 1: 认证（不再传递RememberMe，由ViewModel决定是否保存密码）
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

        // 凭证验证成功，进入LoadingProfile阶段
        _stateMachine.Fire(AuthEvent.CredentialsValidated, "正在启动会话...");

        // Step 2: 保存JWT Token认证信息（始终保存用于当前会话）
        await _tokenStorageService.SaveAuthenticationAsync(loginResponse, rememberMe: false);

        // Step 3-5: 完成登录流程
        return await CompleteLoginFlowAsync(user, loginResponse.ExpiresAt);
    }

    /// <summary>
    /// 本地模式登录（SQLite 认证）
    /// OpenSpec: implement-local-mode
    /// </summary>
    private async Task<LoginResult> LoginLocalAsync(string username, string password)
    {
        if (_localAuthService == null)
        {
            _logger.LogError("本地模式登录失败：LocalAuthService 未注册");
            _stateMachine.Fire(AuthEvent.LoginFailure, "本地认证服务未配置");
            return LoginResult.Failed("本地认证服务未配置");
        }

        // Step 1: 本地认证
        var userEntity = await _localAuthService.ValidateAsync(username, password);

        if (userEntity == null)
        {
            _logger.LogWarning("本地登录认证失败 [用户: {Username}]", username);
            _stateMachine.Fire(AuthEvent.LoginFailure, "用户名或密码错误");
            return LoginResult.Failed("用户名或密码错误");
        }

        // 将 Entity 转换为 DTO
        var user = new UserDetailDto
        {
            Id = userEntity.Id,
            UserName = userEntity.UserName,
            RealName = userEntity.RealName,
            Role = userEntity.Role,
            Status = userEntity.Status,
            PhoneNumber = userEntity.PhoneNumber,
            Email = userEntity.Email
        };

        // 凭证验证成功，进入LoadingProfile阶段
        _stateMachine.Fire(AuthEvent.CredentialsValidated, "正在启动会话...");

        // 本地模式不需要保存 JWT Token，设置一个长期有效的过期时间
        var expiresAt = DateTime.Now.AddYears(1);

        // Step 3-5: 完成登录流程
        return await CompleteLoginFlowAsync(user, expiresAt);
    }

    /// <summary>
    /// 完成登录流程的公共步骤（会话启动、模块加载、导航）
    /// </summary>
    private async Task<LoginResult> CompleteLoginFlowAsync(UserDetailDto user, DateTime tokenExpiresAt)
    {
        // Step 3: 启动会话
        await StartSessionAsync(user, tokenExpiresAt);
        _stateMachine.Fire(AuthEvent.ProfileLoaded, "正在加载模块...");

        // Step 4: 加载模块
        await LoadModulesForUserAsync(user);
        _stateMachine.Fire(AuthEvent.ModulesLoaded, "正在跳转...");

        // Step 5: 导航到首页
        await NavigateToRoleHomeAsync(user);
        _stateMachine.Fire(AuthEvent.NavigationCompleted);

        // 完成登录
        LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs(user, tokenExpiresAt));

        _logger.LogInformation("登录流程完成 [用户: {Username}, 角色: {Role}]",
            user.UserName, user.Role);

        return LoginResult.Succeeded(user);
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
                LoginAttemptCount: _loginAttemptCount
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
    /// OpenSpec: unify-navigation-architecture (ADR-7) - 使用统一NavigationCoordinator
    /// </summary>
    private Task NavigateToRoleHomeAsync(UserDetailDto user)
    {
        var role = user.Role;

        // 在单元测试中，Application.Current 为 null，直接调用导航服务
        if (Application.Current?.Dispatcher is null)
        {
            try
            {
                // 直接使用用户角色导航，避免依赖SessionManager延迟加载
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
                // 直接使用用户角色导航，避免依赖SessionManager延迟加载
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
