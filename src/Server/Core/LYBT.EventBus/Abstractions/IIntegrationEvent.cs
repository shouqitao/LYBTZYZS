namespace LYBT.Core.EventBus.Abstractions;

/// <summary>
/// 集成事件基础接口
/// 用于模块间异步通信的事件标记
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// 事件唯一标识
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// 事件创建时间
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// 事件类型名称
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// 事件来源模块
    /// </summary>
    string Source { get; }

    /// <summary>
    /// 事件版本（用于向后兼容）
    /// </summary>
    int Version { get; }
}
