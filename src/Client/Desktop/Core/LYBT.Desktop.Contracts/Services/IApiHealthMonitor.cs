namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 全局 API 健康监控器接口
/// 统一管理远程 API 可用性检测，支持订阅模式和断路器保护
/// </summary>
public interface IApiHealthMonitor : IDisposable
{
    // ====== 状态属性 ======

    /// <summary>当前健康状态</summary>
    ApiMonitorHealthStatus Status { get; }

    /// <summary>连接状态（更细粒度）</summary>
    ApiConnectionState ConnectionState { get; }

    /// <summary>上次检查时间</summary>
    DateTime? LastCheckTime { get; }

    /// <summary>下次检查时间</summary>
    DateTime? NextCheckTime { get; }

    /// <summary>连续失败次数</summary>
    int ConsecutiveFailures { get; }

    /// <summary>最后错误信息</summary>
    string? LastError { get; }

    /// <summary>是否正在检查中</summary>
    bool IsChecking { get; }

    // ====== 配置属性 ======

    /// <summary>检查间隔（默认10秒）</summary>
    TimeSpan CheckInterval { get; set; }

    /// <summary>检查超时（默认5秒）</summary>
    TimeSpan CheckTimeout { get; set; }

    /// <summary>断路器阈值（连续失败次数触发，默认3次）</summary>
    int CircuitBreakerThreshold { get; set; }

    /// <summary>断路器恢复时间（默认30秒）</summary>
    TimeSpan CircuitBreakerRecoveryTime { get; set; }

    // ====== 事件 ======

    /// <summary>状态变更事件</summary>
    event EventHandler<ApiHealthMonitorChangedEventArgs>? StatusChanged;

    /// <summary>检查完成事件</summary>
    event EventHandler<HealthCheckCompletedEventArgs>? CheckCompleted;

    // ====== 方法 ======

    /// <summary>启动监控</summary>
    Task StartMonitoringAsync(CancellationToken ct = default);

    /// <summary>停止监控</summary>
    Task StopMonitoringAsync();

    /// <summary>立即检查（强制）</summary>
    Task<ApiMonitorHealthStatus> ForceCheckAsync();

    /// <summary>重置断路器</summary>
    void ResetCircuitBreaker();
}

/// <summary>
/// API 健康状态枚举（监控器专用）
/// </summary>
public enum ApiMonitorHealthStatus
{
    /// <summary>检查中</summary>
    Checking,

    /// <summary>健康</summary>
    Healthy,

    /// <summary>不健康</summary>
    Unhealthy
}

/// <summary>
/// API 连接状态枚举
/// </summary>
public enum ApiConnectionState
{
    /// <summary>初始状态</summary>
    Unknown,

    /// <summary>正在检查</summary>
    Checking,

    /// <summary>已连接</summary>
    Connected,

    /// <summary>降级（延迟高或部分功能不可用）</summary>
    Degraded,

    /// <summary>已断开</summary>
    Disconnected,

    /// <summary>重连中</summary>
    Reconnecting
}

/// <summary>
/// 断路器状态枚举
/// </summary>
public enum CircuitState
{
    /// <summary>关闭（正常通行）</summary>
    Closed,

    /// <summary>开启（快速失败）</summary>
    Open,

    /// <summary>半开（尝试恢复）</summary>
    HalfOpen
}

/// <summary>
/// API 健康状态变更事件参数
/// </summary>
public class ApiHealthMonitorChangedEventArgs : EventArgs
{
    public ApiMonitorHealthStatus OldStatus { get; init; }
    public ApiMonitorHealthStatus NewStatus { get; init; }
    public ApiConnectionState OldConnectionState { get; init; }
    public ApiConnectionState NewConnectionState { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 健康检查完成事件参数
/// </summary>
public class HealthCheckCompletedEventArgs : EventArgs
{
    public ApiMonitorHealthStatus Status { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
