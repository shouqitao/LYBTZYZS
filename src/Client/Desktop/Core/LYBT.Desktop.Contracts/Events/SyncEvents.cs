using Prism.Events;

namespace LYBT.Desktop.Contracts.Events;

/// <summary>
/// 同步状态事件聚合类
/// 用于跨模块的同步状态通知（SyncViewModel -> MainWindowViewModel -> SidebarControl）
/// </summary>
public static class SyncEvents
{
    /// <summary>
    /// 同步状态变更事件 -- 通知侧边栏显示同步指示器
    /// </summary>
    public class StatusChangedEvent : PubSubEvent<SyncStatusPayload> { }
}

/// <summary>
/// 同步状态事件载荷
/// </summary>
public record SyncStatusPayload
{
    /// <summary>
    /// 是否正在同步
    /// </summary>
    public required bool IsSyncing { get; init; }

    /// <summary>
    /// 上次同步完成时间
    /// </summary>
    public DateTime? LastSyncTime { get; init; }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string? StatusMessage { get; init; }
}
