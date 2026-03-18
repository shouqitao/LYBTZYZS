namespace LYBT.Desktop.Foundation.Application;

/// <summary>
/// API状态变更事件参数
/// OpenSpec: refactor-startup-connection-resilience - 事件驱动状态更新
/// </summary>
public class ApiStatusChangedEventArgs : EventArgs
{
    /// <summary>API是否健康</summary>
    public bool IsHealthy { get; }

    /// <summary>连接状态描述</summary>
    public string ConnectionStatus { get; }

    /// <summary>最后一次错误信息（无错误时为null）</summary>
    public string? LastError { get; }

    /// <summary>检查时间</summary>
    public DateTime CheckTime { get; }

    public ApiStatusChangedEventArgs(bool isHealthy, string connectionStatus, string? lastError = null)
    {
        IsHealthy = isHealthy;
        ConnectionStatus = connectionStatus;
        LastError = lastError;
        CheckTime = DateTime.UtcNow;
    }
}
