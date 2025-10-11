namespace LYBT.EventBus.Abstractions;

/// <summary>
/// 集成事件处理器基础接口
/// </summary>
public interface IIntegrationEventHandler
{
    /// <summary>
    /// 处理器名称
    /// </summary>
    string HandlerName { get; }

    /// <summary>
    /// 支持的事件类型
    /// </summary>
    Type EventType { get; }
}

/// <summary>
/// 泛型事件处理器接口
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
    where TEvent : class, IIntegrationEvent
{
    /// <summary>
    /// 异步处理事件
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
