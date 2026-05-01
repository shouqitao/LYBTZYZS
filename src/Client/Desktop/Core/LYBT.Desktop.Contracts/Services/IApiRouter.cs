namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// API 路由器接口 — 根据连接状态自动选择远程或本地 API
/// </summary>
public interface IApiRouter
{
    /// <summary>当前使用的 API 模式</summary>
    ApiMode CurrentMode { get; }

    /// <summary>是否使用本地 API</summary>
    bool IsOffline { get; }

    /// <summary>手动覆盖（null = 自动判断）</summary>
    ApiMode? ManualOverride { get; set; }

    /// <summary>模式变更事件</summary>
    event EventHandler<ApiModeChangedEventArgs>? ModeChanged;

    /// <summary>手动切换到指定模式（设置 ManualOverride）</summary>
    void SwitchTo(ApiMode mode);

    /// <summary>清除手动覆盖，恢复自动判断</summary>
    void ClearManualOverride();
}

/// <summary>
/// API 模式枚举
/// </summary>
public enum ApiMode
{
    /// <summary>使用远程 WebAPI</summary>
    Remote,

    /// <summary>使用本地 LocalWebAPI</summary>
    Local
}

/// <summary>
/// API 模式变更事件参数
/// </summary>
public class ApiModeChangedEventArgs : EventArgs
{
    /// <summary>旧模式</summary>
    public ApiMode OldMode { get; init; }

    /// <summary>新模式</summary>
    public ApiMode NewMode { get; init; }

    /// <summary>是否为手动切换</summary>
    public bool IsManual { get; init; }

    /// <summary>变更时间</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
