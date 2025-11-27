using System.Windows.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 用户活动追踪服务实现
/// OpenSpec: refactor-token-sliding-expiration (AUTH-001, AUTH-002, AUTH-003)
/// 监听用户UI交互,检测不活跃状态,触发会话过期事件
/// </summary>
public class UserActivityTracker : IUserActivityTracker, IUserActivityState, IDisposable
{
    private readonly ILogger<UserActivityTracker> _logger;
    private readonly IApplicationTickService _tickService;
    private readonly object _lock = new();

    // 配置参数
    private readonly int _inactivityTimeoutMinutes;
    private readonly int _warningBeforeTimeoutMinutes;
    private readonly int _activityCheckIntervalSeconds;

    // 状态
    private DateTime _lastActivityTime;
    private bool _isTracking;
    private bool _warningShown;
    private bool _disposed;
    private long _lastCheckTickCount;

    /// <inheritdoc />
    public event EventHandler<SessionExpiringEventArgs>? SessionExpiring;

    /// <inheritdoc />
    public event EventHandler? SessionExpired;

    /// <inheritdoc />
    public DateTime LastActivityTime
    {
        get
        {
            lock (_lock)
            {
                return _lastActivityTime;
            }
        }
    }

    /// <inheritdoc />
    public bool IsUserActive
    {
        get
        {
            lock (_lock)
            {
                var elapsed = DateTime.Now - _lastActivityTime;
                return elapsed.TotalMinutes < _inactivityTimeoutMinutes;
            }
        }
    }

    /// <inheritdoc />
    public TimeSpan TimeUntilInactive
    {
        get
        {
            lock (_lock)
            {
                var elapsed = DateTime.Now - _lastActivityTime;
                var remaining = TimeSpan.FromMinutes(_inactivityTimeoutMinutes) - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }

    /// <inheritdoc />
    public bool IsTracking
    {
        get
        {
            lock (_lock)
            {
                return _isTracking;
            }
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    /// <param name="tickService">统一定时服务</param>
    /// <param name="inactivityTimeoutMinutes">不活跃超时时间(分钟),默认15</param>
    /// <param name="warningBeforeTimeoutMinutes">警告提前时间(分钟),默认2</param>
    /// <param name="activityCheckIntervalSeconds">检查间隔(秒),默认60</param>
    public UserActivityTracker(
        ILogger<UserActivityTracker> logger,
        IApplicationTickService tickService,
        int inactivityTimeoutMinutes = 15,
        int warningBeforeTimeoutMinutes = 2,
        int activityCheckIntervalSeconds = 60)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));

        _inactivityTimeoutMinutes = inactivityTimeoutMinutes;
        _warningBeforeTimeoutMinutes = warningBeforeTimeoutMinutes;
        _activityCheckIntervalSeconds = activityCheckIntervalSeconds;

        _lastActivityTime = DateTime.Now;

        _logger.LogDebug("UserActivityTracker已创建 (超时={Timeout}分钟, 警告={Warning}分钟, 检查间隔={Interval}秒)",
            _inactivityTimeoutMinutes, _warningBeforeTimeoutMinutes, _activityCheckIntervalSeconds);
    }

    /// <inheritdoc />
    public void StartTracking()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                _logger.LogWarning("尝试启动已释放的UserActivityTracker");
                return;
            }

            if (_isTracking)
            {
                _logger.LogDebug("UserActivityTracker已在追踪中");
                return;
            }

            _lastActivityTime = DateTime.Now;
            _warningShown = false;
            _lastCheckTickCount = _tickService.TickCount;

            // 订阅输入事件
            InputManager.Current.PreProcessInput += OnPreProcessInput;

            // 订阅Tick服务
            _tickService.Tick += OnTick;

            _isTracking = true;
            _logger.LogInformation("UserActivityTracker已启动追踪 (超时={Timeout}分钟, 警告={Warning}分钟, 检查间隔={Interval}秒)",
                _inactivityTimeoutMinutes, _warningBeforeTimeoutMinutes, _activityCheckIntervalSeconds);
        }
    }

    /// <inheritdoc />
    public void StopTracking()
    {
        lock (_lock)
        {
            if (!_isTracking)
            {
                return;
            }

            // 取消订阅
            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            _tickService.Tick -= OnTick;

            _isTracking = false;
            _warningShown = false;
            _logger.LogInformation("UserActivityTracker已停止追踪");
        }
    }

    /// <inheritdoc />
    public void ResetActivity()
    {
        lock (_lock)
        {
            _lastActivityTime = DateTime.Now;
            _warningShown = false;
        }

        _logger.LogDebug("用户活动计时器已重置");
    }

    private void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
    {
        // 只监听有意义的用户操作，忽略鼠标移动等被动事件
        var inputEvent = e.StagingItem.Input;

        // 过滤：只接受键盘事件、鼠标点击、鼠标滚轮
        bool isValidActivity = inputEvent is KeyboardEventArgs ||
                               inputEvent is MouseButtonEventArgs ||
                               inputEvent is MouseWheelEventArgs;

        if (!isValidActivity)
        {
            return;
        }

        lock (_lock)
        {
            _lastActivityTime = DateTime.Now;

            // 如果之前显示过警告,现在用户有活动,重置警告状态
            if (_warningShown)
            {
                _warningShown = false;
                _logger.LogDebug("用户活动检测到,警告状态已重置");
            }
        }
    }

    private void OnTick(object? sender, ApplicationTickEventArgs e)
    {
        // 根据配置的检查间隔决定是否执行检查
        var ticksSinceLastCheck = e.TickCount - _lastCheckTickCount;
        if (ticksSinceLastCheck < _activityCheckIntervalSeconds)
        {
            return;
        }

        _lastCheckTickCount = e.TickCount;
        CheckInactivity();
    }

    private void CheckInactivity()
    {
        DateTime lastActivity;
        bool warningShown;
        bool isTracking;

        lock (_lock)
        {
            lastActivity = _lastActivityTime;
            warningShown = _warningShown;
            isTracking = _isTracking;
        }

        if (!isTracking)
        {
            return;
        }

        var elapsed = DateTime.Now - lastActivity;
        var totalTimeoutMinutes = _inactivityTimeoutMinutes;
        var warningThresholdMinutes = totalTimeoutMinutes - _warningBeforeTimeoutMinutes;

        // 检查是否已超时
        if (elapsed.TotalMinutes >= totalTimeoutMinutes)
        {
            _logger.LogWarning("用户不活跃时间已超过{Timeout}分钟,触发会话过期", totalTimeoutMinutes);
            OnSessionExpired();
            return;
        }

        // 检查是否需要显示警告
        if (!warningShown && elapsed.TotalMinutes >= warningThresholdMinutes)
        {
            lock (_lock)
            {
                _warningShown = true;
            }

            var remainingTime = TimeSpan.FromMinutes(totalTimeoutMinutes) - elapsed;
            _logger.LogInformation("用户不活跃时间已达{Elapsed:F1}分钟,显示过期警告 (剩余{Remaining:F1}分钟)",
                elapsed.TotalMinutes, remainingTime.TotalMinutes);

            OnSessionExpiring(remainingTime);
        }
    }

    private void OnSessionExpiring(TimeSpan remainingTime)
    {
        var args = new SessionExpiringEventArgs
        {
            RemainingTime = remainingTime
        };

        try
        {
            SessionExpiring?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionExpiring事件处理器执行出错");
        }
    }

    private void OnSessionExpired()
    {
        try
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SessionExpired事件处理器执行出错");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopTracking();
        _disposed = true;

        _logger.LogDebug("UserActivityTracker已释放");
    }
}
