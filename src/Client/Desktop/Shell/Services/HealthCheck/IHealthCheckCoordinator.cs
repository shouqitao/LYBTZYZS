using LYBT.Desktop.Foundation.HealthCheck;

namespace LYBT.Desktop.Shell.Services.HealthCheck;

/// <summary>
/// 健康检查协调器接口
/// 负责管理API健康检查的调度和状态
/// </summary>
public interface IHealthCheckCoordinator : IDisposable
{
    /// <summary>获取当前API健康状态</summary>
    ApiHealthStatus CurrentStatus { get; }

    /// <summary>健康检查间隔（秒）</summary>
    int CheckIntervalSeconds { get; }

    /// <summary>状态变更事件</summary>
    event EventHandler<HealthStatusChangedEventArgs>? StatusChanged;

    /// <summary>启动定时健康检查</summary>
    void Start();

    /// <summary>停止定时健康检查</summary>
    void Stop();

    /// <summary>立即执行一次健康检查</summary>
    Task CheckNowAsync();
}

/// <summary>健康状态变更事件参数</summary>
public class HealthStatusChangedEventArgs : EventArgs
{
    /// <summary>之前的状态</summary>
    public ApiHealthStatus PreviousStatus { get; init; }

    /// <summary>当前状态</summary>
    public ApiHealthStatus CurrentStatus { get; init; }
}
