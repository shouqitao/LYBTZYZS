namespace LYBT.EventBus.Abstractions;

/// <summary>
/// 事件总线接口
/// 负责事件的发布、订阅和路由
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布集成事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布任务</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;

    /// <summary>
    /// 订阅事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <returns>订阅是否成功</returns>
    bool Subscribe<TEvent, THandler>()
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>;

    /// <summary>
    /// 订阅事件处理器（通过类型）
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handlerType">处理器类型</param>
    /// <returns>订阅是否成功</returns>
    bool Subscribe(Type eventType, Type handlerType);

    /// <summary>
    /// 取消订阅
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <returns>取消订阅是否成功</returns>
    bool Unsubscribe<TEvent, THandler>()
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>;

    /// <summary>
    /// 获取事件的订阅数量
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <returns>订阅数量</returns>
    int GetSubscriptionCount<TEvent>() where TEvent : class, IIntegrationEvent;

    /// <summary>
    /// 获取所有已注册的事件类型
    /// </summary>
    /// <returns>事件类型集合</returns>
    IReadOnlyCollection<Type> GetRegisteredEventTypes();

    /// <summary>
    /// 清空所有订阅
    /// </summary>
    void ClearSubscriptions();
}

/// <summary>
/// 事件总线统计信息
/// </summary>
public class EventBusStatistics
{
    /// <summary>
    /// 总发布事件数
    /// </summary>
    public long TotalPublishedEvents { get; set; }

    /// <summary>
    /// 总处理事件数
    /// </summary>
    public long TotalProcessedEvents { get; set; }

    /// <summary>
    /// 处理失败事件数
    /// </summary>
    public long FailedEvents { get; set; }

    /// <summary>
    /// 注册的事件类型数
    /// </summary>
    public int RegisteredEventTypes { get; set; }

    /// <summary>
    /// 注册的处理器数
    /// </summary>
    public int RegisteredHandlers { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime? LastActivityTime { get; set; }
}
