namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 应用级别的统一定时任务调度服务
/// OpenSpec: refactor-token-sliding-expiration (AUTH-000)
/// 使用单一DispatcherTimer,每秒触发Tick事件,所有周期性任务订阅此事件
/// </summary>
public interface IApplicationTickService
{
    /// <summary>
    /// 每秒触发的Tick事件
    /// 订阅者应在回调中根据自身需求决定是否执行(如每10次Tick执行一次)
    /// </summary>
    event EventHandler<ApplicationTickEventArgs>? Tick;

    /// <summary>
    /// 当前Tick计数(从启动开始累计)
    /// </summary>
    long TickCount { get; }

    /// <summary>
    /// 定时器是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 启动定时器
    /// </summary>
    void Start();

    /// <summary>
    /// 停止定时器
    /// </summary>
    void Stop();
}

/// <summary>
/// Tick事件参数
/// </summary>
public class ApplicationTickEventArgs : EventArgs
{
    /// <summary>
    /// 当前Tick计数
    /// </summary>
    public long TickCount { get; init; }

    /// <summary>
    /// Tick时间戳
    /// </summary>
    public DateTime Timestamp { get; init; }
}
