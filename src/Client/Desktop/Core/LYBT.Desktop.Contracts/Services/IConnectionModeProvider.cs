namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 连接模式提供者 - 支持运行时模式查询和切换 (SYNC-D02/D03)
/// Singleton 服务应注入此接口而非直接注入 ConnectionMode 枚举，
/// 以确保模式切换后获取最新值。
/// </summary>
public interface IConnectionModeProvider
{
    /// <summary>
    /// 当前连接模式
    /// </summary>
    ConnectionMode CurrentMode { get; }

    /// <summary>
    /// 是否为远程模式
    /// </summary>
    bool IsRemote => CurrentMode == ConnectionMode.Remote;

    /// <summary>
    /// 是否为本地模式
    /// </summary>
    bool IsLocal => CurrentMode == ConnectionMode.Local;

    /// <summary>
    /// 是否正在切换模式
    /// </summary>
    bool IsSwitching { get; }

    /// <summary>
    /// 切换到指定模式 (SYNC-D03)
    /// 执行验证 -> 清理 UI -> 切换模式 -> 触发事件
    /// </summary>
    /// <returns>切换结果: 成功/失败原因</returns>
    Task<ModeSwitchResult> SwitchModeAsync(ConnectionMode targetMode, CancellationToken ct = default);

    /// <summary>
    /// 模式变更事件 - 切换完成后触发
    /// </summary>
    event EventHandler<ConnectionModeChangedEventArgs>? ModeChanged;
}

/// <summary>
/// 模式切换结果
/// </summary>
public sealed record ModeSwitchResult(bool Success, string? ErrorMessage = null)
{
    public static ModeSwitchResult Succeeded() => new(true);
    public static ModeSwitchResult Failed(string error) => new(false, error);
}

/// <summary>
/// 模式变更事件参数
/// </summary>
public sealed class ConnectionModeChangedEventArgs : EventArgs
{
    public ConnectionMode PreviousMode { get; }
    public ConnectionMode CurrentMode { get; }

    public ConnectionModeChangedEventArgs(ConnectionMode previousMode, ConnectionMode currentMode)
    {
        PreviousMode = previousMode;
        CurrentMode = currentMode;
    }
}
