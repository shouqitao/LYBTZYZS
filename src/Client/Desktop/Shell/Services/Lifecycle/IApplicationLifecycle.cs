namespace LYBT.Desktop.Shell.Services.Lifecycle;

/// <summary>
/// 应用程序生命周期管理接口
/// 提供状态机模式管理应用启动流程
/// </summary>
public interface IApplicationLifecycle
{
    /// <summary>
    /// 当前应用状态
    /// </summary>
    ApplicationState CurrentState { get; }

    /// <summary>
    /// 状态变化事件
    /// </summary>
    event EventHandler<ApplicationStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// 尝试转换到目标状态
    /// </summary>
    /// <param name="targetState">目标状态</param>
    /// <returns>转换是否成功</returns>
    Task<bool> TransitionToAsync(ApplicationState targetState);

    /// <summary>
    /// 注册状态处理器
    /// </summary>
    /// <param name="state">要处理的状态</param>
    /// <param name="handler">处理器</param>
    void RegisterStateHandler(ApplicationState state, Func<Task> handler);

    /// <summary>
    /// 移除状态处理器
    /// </summary>
    /// <param name="state">状态</param>
    void RemoveStateHandler(ApplicationState state);

    /// <summary>
    /// 获取状态转换历史（用于诊断）
    /// </summary>
    IReadOnlyList<StateTransitionRecord> GetTransitionHistory();
}

/// <summary>
/// 状态变化事件参数
/// </summary>
public class ApplicationStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 前一个状态
    /// </summary>
    public ApplicationState PreviousState { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public ApplicationState CurrentState { get; }

    /// <summary>
    /// 转换时间戳
    /// </summary>
    public DateTime Timestamp { get; }

    public ApplicationStateChangedEventArgs(ApplicationState previousState, ApplicationState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        Timestamp = DateTime.Now;
    }
}

/// <summary>
/// 状态转换记录（用于诊断）
/// </summary>
public record StateTransitionRecord(
    ApplicationState FromState,
    ApplicationState ToState,
    DateTime Timestamp,
    TimeSpan Duration,
    bool Success,
    string? ErrorMessage = null
);
