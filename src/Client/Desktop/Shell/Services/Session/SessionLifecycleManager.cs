using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Shell.Services.Session;

/// <summary>
/// 会话生命周期管理实现
/// 集中管理用户会话状态和Token生命周期
/// </summary>
public class SessionLifecycleManager : ISessionLifecycleManager, IDisposable
{
    private readonly ILogger<SessionLifecycleManager> _logger;
    private readonly ITokenLifecycleService _tokenLifecycleService;
    private readonly IUserActivityTracker _userActivityTracker;
    private readonly IEventAggregator _eventAggregator;
    private readonly object _stateLock = new();

    private SessionState _currentState = SessionState.Unauthenticated;
    private string? _currentUserName;
    private string? _currentUserRole;
    private DateTime? _sessionStartTime;
    private DateTime? _tokenExpiresAt;
    private DateTime? _lastActivityTime;
    private int _tokenRefreshCount;
    private DateTime? _lastTokenRefreshTime;
    private SubscriptionToken? _tokenLifecycleSubscription;
    private bool _disposed;

    public SessionLifecycleManager(
        ILogger<SessionLifecycleManager> logger,
        ITokenLifecycleService tokenLifecycleService,
        IUserActivityTracker userActivityTracker,
        IEventAggregator eventAggregator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenLifecycleService = tokenLifecycleService ?? throw new ArgumentNullException(nameof(tokenLifecycleService));
        _userActivityTracker = userActivityTracker ?? throw new ArgumentNullException(nameof(userActivityTracker));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        // 订阅Token生命周期事件（通过EventAggregator）
        _tokenLifecycleSubscription = _eventAggregator
            .GetEvent<TokenLifecycleStateChangedEvent>()
            .Subscribe(OnTokenLifecycleStateChanged, ThreadOption.UIThread);

        // 订阅用户活动事件
        // OpenSpec: simplify-auth-architecture - 移除SessionExpiring订阅，不再显示过期警告
        _userActivityTracker.SessionExpired += OnUserActivitySessionExpired;
    }

    /// <inheritdoc />
    public SessionState CurrentState
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
    public bool IsAuthenticated
    {
        get
        {
            lock (_stateLock)
            {
                // OpenSpec: simplify-auth-architecture - 移除Expiring状态检查
                return _currentState == SessionState.Authenticated ||
                       _currentState == SessionState.Refreshing;
            }
        }
    }

    /// <inheritdoc />
    public string? CurrentUserName
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUserName;
            }
        }
    }

    /// <inheritdoc />
    public string? CurrentUserRole
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUserRole;
            }
        }
    }

    /// <inheritdoc />
    public TimeSpan? TokenRemainingTime => _tokenLifecycleService.RemainingTime;

    /// <inheritdoc />
    public event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    // OpenSpec: simplify-auth-architecture - SessionExpiring事件已移除

    /// <inheritdoc />
    public event EventHandler? SessionExpired;

    /// <inheritdoc />
    public Task StartSessionAsync(string userName, string userRole, DateTime tokenExpiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userRole);

        lock (_stateLock)
        {
            _currentUserName = userName;
            _currentUserRole = userRole;
            _tokenExpiresAt = tokenExpiresAt;
            _sessionStartTime = DateTime.UtcNow;
            _lastActivityTime = DateTime.UtcNow;
            _tokenRefreshCount = 0;
        }

        // 启动Token生命周期监控
        _tokenLifecycleService.StartMonitoring(tokenExpiresAt);

        // 启动用户活动追踪
        _userActivityTracker.StartTracking();

        // 转换到已认证状态
        TransitionTo(SessionState.Authenticated);

        _logger.LogInformation("会话已启动 [用户: {UserName}, 角色: {UserRole}, Token过期时间: {ExpiresAt}]",
            userName, userRole, tokenExpiresAt);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EndSessionAsync()
    {
        _logger.LogInformation("正在结束会话 [用户: {UserName}]", _currentUserName);

        // 停止Token生命周期监控
        _tokenLifecycleService.StopMonitoring();
        _tokenLifecycleService.Reset();

        // 停止用户活动追踪
        _userActivityTracker.StopTracking();

        lock (_stateLock)
        {
            _currentUserName = null;
            _currentUserRole = null;
            _tokenExpiresAt = null;
            _sessionStartTime = null;
            _lastActivityTime = null;
        }

        // 转换到未认证状态
        TransitionTo(SessionState.Unauthenticated);

        _logger.LogInformation("会话已结束");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> RefreshTokenAsync()
    {
        var previousState = CurrentState;

        // 转换到刷新中状态
        TransitionTo(SessionState.Refreshing);

        try
        {
            var success = await _tokenLifecycleService.TryRefreshTokenAsync();

            if (success)
            {
                lock (_stateLock)
                {
                    _tokenRefreshCount++;
                    _lastTokenRefreshTime = DateTime.UtcNow;
                }

                TransitionTo(SessionState.Authenticated);
                _logger.LogInformation("Token刷新成功 [刷新次数: {Count}]", _tokenRefreshCount);
                return true;
            }
            else
            {
                TransitionTo(SessionState.Expired);
                _logger.LogWarning("Token刷新失败");
                SessionExpired?.Invoke(this, EventArgs.Empty);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token刷新异常");
            TransitionTo(SessionState.Expired);
            SessionExpired?.Invoke(this, EventArgs.Empty);
            return false;
        }
    }

    /// <inheritdoc />
    public void UpdateTokenExpiration(DateTime newExpiresAt)
    {
        lock (_stateLock)
        {
            _tokenExpiresAt = newExpiresAt;
        }

        _tokenLifecycleService.UpdateExpiration(newExpiresAt);
        _logger.LogDebug("Token过期时间已更新: {ExpiresAt}", newExpiresAt);
    }

    /// <inheritdoc />
    public void RecordUserActivity()
    {
        lock (_stateLock)
        {
            _lastActivityTime = DateTime.UtcNow;
        }

        _userActivityTracker.ResetActivity();
    }

    /// <inheritdoc />
    public SessionDiagnostics GetDiagnostics()
    {
        lock (_stateLock)
        {
            return new SessionDiagnostics(
                CurrentState: _currentState,
                UserName: _currentUserName,
                UserRole: _currentUserRole,
                SessionStartTime: _sessionStartTime,
                TokenExpiresAt: _tokenExpiresAt,
                TokenRemainingTime: _tokenLifecycleService.RemainingTime,
                LastActivityTime: _lastActivityTime,
                TokenRefreshCount: _tokenRefreshCount,
                LastTokenRefreshTime: _lastTokenRefreshTime
            );
        }
    }

    /// <summary>
    /// 转换会话状态
    /// </summary>
    private void TransitionTo(SessionState newState)
    {
        SessionState previousState;

        lock (_stateLock)
        {
            if (_currentState == newState)
            {
                return;
            }

            previousState = _currentState;
            _currentState = newState;
        }

        _logger.LogDebug("会话状态转换: {From} -> {To}", previousState, newState);
        StateChanged?.Invoke(this, new SessionStateChangedEventArgs(previousState, newState));
    }

    /// <summary>
    /// Token生命周期状态变化处理
    /// </summary>
    private void OnTokenLifecycleStateChanged(TokenLifecycleStateChangedEventArgs args)
    {
        _logger.LogDebug("Token生命周期状态变更: {Previous} -> {Current}", args.PreviousState, args.CurrentState);

        switch (args.CurrentState)
        {
            case TokenLifecycleState.Warning:
                // OpenSpec: simplify-auth-architecture - Warning状态直接过期，不再显示警告
                // 让Token继续自然过期，或在后台静默刷新
                _logger.LogDebug("Token进入Warning状态，等待自动刷新或过期");
                break;

            case TokenLifecycleState.Expired:
                TransitionTo(SessionState.Expired);
                SessionExpired?.Invoke(this, EventArgs.Empty);
                break;

            case TokenLifecycleState.Active:
                // OpenSpec: simplify-auth-architecture - 移除Expiring状态检查
                if (CurrentState == SessionState.Refreshing)
                {
                    TransitionTo(SessionState.Authenticated);
                }
                break;
        }
    }

    // OpenSpec: simplify-auth-architecture - OnUserActivitySessionExpiring方法已移除

    /// <summary>
    /// 用户活动会话已过期处理
    /// </summary>
    private void OnUserActivitySessionExpired(object? sender, EventArgs e)
    {
        _logger.LogWarning("用户长时间不活跃，会话已过期");
        TransitionTo(SessionState.Expired);
        SessionExpired?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // 取消EventAggregator订阅
        _tokenLifecycleSubscription?.Dispose();

        // 取消用户活动事件订阅
        // OpenSpec: simplify-auth-architecture - SessionExpiring订阅已移除
        _userActivityTracker.SessionExpired -= OnUserActivitySessionExpired;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
