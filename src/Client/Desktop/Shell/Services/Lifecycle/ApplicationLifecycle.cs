using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Lifecycle;

/// <summary>
/// 应用程序生命周期管理实现
/// 使用状态机模式管理应用启动流程
/// </summary>
public class ApplicationLifecycle : IApplicationLifecycle
{
    private readonly ILogger<ApplicationLifecycle> _logger;
    private readonly object _stateLock = new();
    private readonly Dictionary<ApplicationState, Func<Task>> _stateHandlers = new();
    private readonly List<StateTransitionRecord> _transitionHistory = new();

    private ApplicationState _currentState = ApplicationState.NotStarted;

    /// <summary>
    /// 有效的状态转换映射
    /// </summary>
    private static readonly Dictionary<ApplicationState, ApplicationState[]> ValidTransitions = new()
    {
        { ApplicationState.NotStarted, [ApplicationState.Initializing] },
        { ApplicationState.Initializing, [ApplicationState.Authenticating, ApplicationState.ShuttingDown] },
        { ApplicationState.Authenticating, [ApplicationState.Ready, ApplicationState.ShuttingDown] },
        { ApplicationState.Ready, [ApplicationState.Running, ApplicationState.ShuttingDown] },
        { ApplicationState.Running, [ApplicationState.Authenticating, ApplicationState.ShuttingDown] },
        { ApplicationState.ShuttingDown, [] }
    };

    public ApplicationLifecycle(ILogger<ApplicationLifecycle> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ApplicationState CurrentState
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
    public event EventHandler<ApplicationStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public async Task<bool> TransitionToAsync(ApplicationState targetState)
    {
        var startTime = DateTime.Now;
        ApplicationState previousState;

        lock (_stateLock)
        {
            previousState = _currentState;

            // 验证转换是否有效
            if (!IsValidTransition(previousState, targetState))
            {
                _logger.LogWarning("无效的状态转换: {From} -> {To}", previousState, targetState);
                RecordTransition(previousState, targetState, startTime, false, "无效的状态转换");
                return false;
            }

            _currentState = targetState;
        }

        _logger.LogInformation("状态转换: {From} -> {To}", previousState, targetState);

        try
        {
            // 执行状态处理器
            if (_stateHandlers.TryGetValue(targetState, out var handler))
            {
                await handler();
            }

            // 触发状态变化事件
            StateChanged?.Invoke(this, new ApplicationStateChangedEventArgs(previousState, targetState));

            RecordTransition(previousState, targetState, startTime, true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状态处理器执行失败: {State}", targetState);

            // 回滚状态
            lock (_stateLock)
            {
                _currentState = previousState;
            }

            RecordTransition(previousState, targetState, startTime, false, ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public void RegisterStateHandler(ApplicationState state, Func<Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_stateLock)
        {
            _stateHandlers[state] = handler;
        }

        _logger.LogDebug("注册状态处理器: {State}", state);
    }

    /// <inheritdoc />
    public void RemoveStateHandler(ApplicationState state)
    {
        lock (_stateLock)
        {
            _stateHandlers.Remove(state);
        }

        _logger.LogDebug("移除状态处理器: {State}", state);
    }

    /// <inheritdoc />
    public IReadOnlyList<StateTransitionRecord> GetTransitionHistory()
    {
        lock (_stateLock)
        {
            return _transitionHistory.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// 验证状态转换是否有效
    /// </summary>
    private static bool IsValidTransition(ApplicationState from, ApplicationState to)
    {
        if (ValidTransitions.TryGetValue(from, out var validTargets))
        {
            return validTargets.Contains(to);
        }
        return false;
    }

    /// <summary>
    /// 记录状态转换
    /// </summary>
    private void RecordTransition(ApplicationState from, ApplicationState to, DateTime startTime, bool success, string? errorMessage = null)
    {
        var duration = DateTime.Now - startTime;
        var record = new StateTransitionRecord(from, to, startTime, duration, success, errorMessage);

        lock (_stateLock)
        {
            _transitionHistory.Add(record);

            // 限制历史记录数量
            if (_transitionHistory.Count > 100)
            {
                _transitionHistory.RemoveAt(0);
            }
        }

        if (duration.TotalSeconds > 3)
        {
            _logger.LogWarning("状态转换耗时过长: {From} -> {To}, 耗时: {Duration}ms",
                from, to, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug("状态转换完成: {From} -> {To}, 耗时: {Duration}ms",
                from, to, duration.TotalMilliseconds);
        }
    }
}
