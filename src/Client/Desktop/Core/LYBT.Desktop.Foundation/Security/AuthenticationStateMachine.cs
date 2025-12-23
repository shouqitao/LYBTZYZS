using LYBT.Desktop.Contracts.Security;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// 统一认证状态机实现
/// OpenSpec: refactor-auth-role-system (Phase 1.1)
/// 合并原有 LoginStateMachine 和 LoginFlowState 双状态机架构
/// 采用转换表驱动的状态机设计，线程安全
/// </summary>
public class AuthenticationStateMachine : IAuthenticationStateMachine
{
    private readonly ILogger<AuthenticationStateMachine> _logger;
    private readonly IEventAggregator? _eventAggregator;
    private readonly object _stateLock = new();
    private AuthState _currentState = AuthState.Idle;
    private string? _statusMessage;

    /// <summary>
    /// 状态转换表: (当前状态, 触发事件) -> 目标状态
    /// </summary>
    private static readonly Dictionary<(AuthState, AuthEvent), AuthState> Transitions = new()
    {
        // Idle 状态的转换
        { (AuthState.Idle, AuthEvent.StartLogin), AuthState.Authenticating },
        { (AuthState.Idle, AuthEvent.StartAutoLogin), AuthState.ValidatingToken },

        // Authenticating 状态的转换（手动登录 - 验证凭证）
        { (AuthState.Authenticating, AuthEvent.CredentialsValidated), AuthState.LoadingProfile },
        { (AuthState.Authenticating, AuthEvent.LoginFailure), AuthState.Failed },
        { (AuthState.Authenticating, AuthEvent.Reset), AuthState.Idle },

        // ValidatingToken 状态的转换（自动登录 - 验证Token）
        { (AuthState.ValidatingToken, AuthEvent.TokenValidated), AuthState.LoadingProfile },
        { (AuthState.ValidatingToken, AuthEvent.LoginFailure), AuthState.Idle },
        { (AuthState.ValidatingToken, AuthEvent.Reset), AuthState.Idle },

        // LoadingProfile 状态的转换
        { (AuthState.LoadingProfile, AuthEvent.ProfileLoaded), AuthState.LoadingModules },
        { (AuthState.LoadingProfile, AuthEvent.LoginFailure), AuthState.Failed },
        { (AuthState.LoadingProfile, AuthEvent.Reset), AuthState.Idle },

        // LoadingModules 状态的转换
        { (AuthState.LoadingModules, AuthEvent.ModulesLoaded), AuthState.Navigating },
        { (AuthState.LoadingModules, AuthEvent.LoginFailure), AuthState.Failed },
        { (AuthState.LoadingModules, AuthEvent.Reset), AuthState.Idle },

        // Navigating 状态的转换
        { (AuthState.Navigating, AuthEvent.NavigationCompleted), AuthState.Authenticated },
        { (AuthState.Navigating, AuthEvent.LoginFailure), AuthState.Failed },
        { (AuthState.Navigating, AuthEvent.Reset), AuthState.Idle },

        // Authenticated 状态的转换
        { (AuthState.Authenticated, AuthEvent.StartLogout), AuthState.LoggingOut },
        { (AuthState.Authenticated, AuthEvent.SessionExpire), AuthState.SessionExpired },
        { (AuthState.Authenticated, AuthEvent.StartTokenRefresh), AuthState.RefreshingToken },

        // Failed 状态的转换
        { (AuthState.Failed, AuthEvent.StartLogin), AuthState.Authenticating },
        { (AuthState.Failed, AuthEvent.StartAutoLogin), AuthState.ValidatingToken },
        { (AuthState.Failed, AuthEvent.Reset), AuthState.Idle },

        // LoggingOut 状态的转换
        { (AuthState.LoggingOut, AuthEvent.LogoutSuccess), AuthState.Idle },
        { (AuthState.LoggingOut, AuthEvent.LogoutFailure), AuthState.Authenticated },
        { (AuthState.LoggingOut, AuthEvent.Reset), AuthState.Idle },

        // SessionExpired 状态的转换
        { (AuthState.SessionExpired, AuthEvent.StartLogin), AuthState.Authenticating },
        { (AuthState.SessionExpired, AuthEvent.StartAutoLogin), AuthState.ValidatingToken },
        { (AuthState.SessionExpired, AuthEvent.Reset), AuthState.Idle },

        // RefreshingToken 状态的转换
        { (AuthState.RefreshingToken, AuthEvent.TokenRefreshSuccess), AuthState.Authenticated },
        { (AuthState.RefreshingToken, AuthEvent.TokenRefreshFailure), AuthState.SessionExpired },
        { (AuthState.RefreshingToken, AuthEvent.Reset), AuthState.Idle },
    };

    /// <inheritdoc />
    public AuthState CurrentState
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
    public bool IsAuthenticated => CurrentState == AuthState.Authenticated;

    /// <inheritdoc />
    public bool IsTransitioning => CurrentState is AuthState.Authenticating
        or AuthState.ValidatingToken
        or AuthState.LoadingProfile
        or AuthState.LoadingModules
        or AuthState.Navigating
        or AuthState.LoggingOut
        or AuthState.RefreshingToken;

    /// <inheritdoc />
    public string? StatusMessage
    {
        get
        {
            lock (_stateLock)
            {
                return _statusMessage;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<AuthStateChangedEventArgs>? StateChanged;

    public AuthenticationStateMachine(
        ILogger<AuthenticationStateMachine> logger,
        IEventAggregator? eventAggregator = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventAggregator = eventAggregator;
    }

    /// <summary>
    /// 用于测试的构造函数，允许设置初始状态
    /// </summary>
    internal AuthenticationStateMachine(
        ILogger<AuthenticationStateMachine> logger,
        AuthState initialState)
        : this(logger, null)
    {
        _currentState = initialState;
    }

    /// <inheritdoc />
    public bool CanFire(AuthEvent evt)
    {
        lock (_stateLock)
        {
            return Transitions.ContainsKey((_currentState, evt));
        }
    }

    /// <inheritdoc />
    public bool Fire(AuthEvent evt, string? statusMessage = null)
    {
        AuthState previousState;
        AuthState newState;

        lock (_stateLock)
        {
            var key = (_currentState, evt);
            if (!Transitions.TryGetValue(key, out newState))
            {
                _logger.LogWarning("无效的状态转换 [当前状态: {CurrentState}] [事件: {Event}]",
                    _currentState, evt);
                return false;
            }

            previousState = _currentState;
            _currentState = newState;
            _statusMessage = statusMessage ?? GetDefaultStatusMessage(newState);
        }

        _logger.LogInformation("认证状态转换 [{PreviousState}] --({Event})--> [{NewState}] [{Message}]",
            previousState, evt, newState, _statusMessage);

        // 在锁外触发事件，避免死锁
        var args = new AuthStateChangedEventArgs(previousState, newState, evt, _statusMessage);
        RaiseStateChanged(args);

        return true;
    }

    /// <inheritdoc />
    public Task<bool> FireAsync(AuthEvent evt, string? statusMessage = null)
    {
        return Task.FromResult(Fire(evt, statusMessage));
    }

    /// <inheritdoc />
    public void Reset()
    {
        Fire(AuthEvent.Reset);
    }

    /// <inheritdoc />
    public IEnumerable<AuthEvent> GetPermittedEvents()
    {
        lock (_stateLock)
        {
            return Transitions.Keys
                .Where(k => k.Item1 == _currentState)
                .Select(k => k.Item2)
                .ToList();
        }
    }

    /// <summary>
    /// 强制设置状态（仅用于恢复场景，跳过转换验证）
    /// </summary>
    internal void ForceState(AuthState state, string? statusMessage = null)
    {
        lock (_stateLock)
        {
            var previousState = _currentState;
            _currentState = state;
            _statusMessage = statusMessage ?? GetDefaultStatusMessage(state);
            _logger.LogWarning("强制状态设置 [{PreviousState}] -> [{NewState}]",
                previousState, state);
        }
    }

    /// <summary>
    /// 获取状态的默认显示消息
    /// </summary>
    private static string? GetDefaultStatusMessage(AuthState state)
    {
        return state switch
        {
            AuthState.Idle => null,
            AuthState.Authenticating => "正在验证身份...",
            AuthState.ValidatingToken => "正在验证Token...",
            AuthState.LoadingProfile => "正在启动会话...",
            AuthState.LoadingModules => "正在加载模块...",
            AuthState.Navigating => "正在跳转...",
            AuthState.Authenticated => null,
            AuthState.Failed => "登录失败",
            AuthState.LoggingOut => "正在登出...",
            AuthState.SessionExpired => "会话已过期",
            AuthState.RefreshingToken => "正在刷新Token...",
            _ => null
        };
    }

    /// <summary>
    /// 触发状态变更事件
    /// </summary>
    private void RaiseStateChanged(AuthStateChangedEventArgs args)
    {
        try
        {
            // 触发本地事件
            StateChanged?.Invoke(this, args);

            // 发布Prism PubSubEvent（如果EventAggregator可用）
            _eventAggregator?.GetEvent<AuthStateChangedPubSubEvent>().Publish(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状态变更事件处理异常 [{PreviousState}] -> [{CurrentState}]",
                args.PreviousState, args.CurrentState);
        }
    }
}

/// <summary>
/// Prism PubSubEvent用于跨模块状态变更通知
/// </summary>
public class AuthStateChangedPubSubEvent : PubSubEvent<AuthStateChangedEventArgs>
{
}
