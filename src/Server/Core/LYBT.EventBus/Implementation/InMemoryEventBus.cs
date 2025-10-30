using System.Collections.Concurrent;
using LYBT.Core.EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace LYBT.EventBus.Implementation;

/// <summary>
/// 内存事件总线实现
/// 提供基于内存的事件发布订阅功能
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Type>> _subscriptions;
    private readonly EventBusStatistics _statistics;
    private long _totalPublishedEvents;
    private long _totalProcessedEvents;
    private long _failedEvents;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供程序</param>
    /// <param name="logger">日志记录器</param>
    public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subscriptions = new ConcurrentDictionary<Type, ConcurrentBag<Type>>();
        _statistics = new EventBusStatistics
        {
            LastActivityTime = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);
        _logger.LogInformation("发布事件: {EventType}, ID: {EventId}, 来源: {Source}",
            eventType.Name, @event.Id, @event.Source);

        try
        {
            // 更新统计信息
            Interlocked.Increment(ref _totalPublishedEvents);
            _statistics.LastActivityTime = DateTime.UtcNow;

            // 获取订阅的处理器
            if (!_subscriptions.TryGetValue(eventType, out var handlerTypes))
            {
                _logger.LogWarning("没有找到事件 {EventType} 的处理器", eventType.Name);
                return;
            }

            // 并行处理所有订阅的处理器
            var tasks = new List<Task>();
            foreach (var handlerType in handlerTypes)
            {
                tasks.Add(ProcessEventAsync(@event, handlerType, cancellationToken));
            }

            await Task.WhenAll(tasks);
            _logger.LogInformation("事件 {EventType} 处理完成，共处理 {HandlerCount} 个处理器",
                eventType.Name, tasks.Count);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedEvents);
            _logger.LogError(ex, "发布事件 {EventType} 时发生异常", eventType.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public bool Subscribe<TEvent, THandler>()
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        return Subscribe(typeof(TEvent), typeof(THandler));
    }

    /// <inheritdoc />
    public bool Subscribe(Type eventType, Type handlerType)
    {
        if (eventType == null)
            throw new ArgumentNullException(nameof(eventType));
        if (handlerType == null)
            throw new ArgumentNullException(nameof(handlerType));

        // 验证事件类型
        if (!typeof(IIntegrationEvent).IsAssignableFrom(eventType))
            throw new ArgumentException($"事件类型 {eventType.Name} 必须实现 IIntegrationEvent 接口", nameof(eventType));

        // 验证处理器类型
        var expectedHandlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        if (!expectedHandlerInterface.IsAssignableFrom(handlerType))
            throw new ArgumentException($"处理器类型 {handlerType.Name} 必须实现 {expectedHandlerInterface.Name} 接口", nameof(handlerType));

        try
        {
            _subscriptions.AddOrUpdate(
                eventType,
                new ConcurrentBag<Type> { handlerType },
                (key, existing) =>
                {
                    if (!existing.Contains(handlerType))
                        existing.Add(handlerType);
                    return existing;
                });

            _statistics.RegisteredHandlers = _subscriptions.Values.SelectMany(h => h).Count();
            _statistics.RegisteredEventTypes = _subscriptions.Keys.Count;

            _logger.LogInformation("成功订阅事件处理器: {EventType} -> {HandlerType}",
                eventType.Name, handlerType.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅事件处理器失败: {EventType} -> {HandlerType}",
                eventType.Name, handlerType.Name);
            return false;
        }
    }

    /// <inheritdoc />
    public bool Unsubscribe<TEvent, THandler>()
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        var eventType = typeof(TEvent);
        var handlerType = typeof(THandler);

        try
        {
            if (_subscriptions.TryGetValue(eventType, out var handlers))
            {
                var updatedHandlers = new ConcurrentBag<Type>(handlers.Where(h => h != handlerType));

                if (updatedHandlers.IsEmpty)
                {
                    _subscriptions.TryRemove(eventType, out _);
                }
                else
                {
                    _subscriptions.TryUpdate(eventType, updatedHandlers, handlers);
                }

                _statistics.RegisteredHandlers = _subscriptions.Values.SelectMany(h => h).Count();
                _statistics.RegisteredEventTypes = _subscriptions.Keys.Count;

                _logger.LogInformation("成功取消订阅事件处理器: {EventType} -> {HandlerType}",
                    eventType.Name, handlerType.Name);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订阅事件处理器失败: {EventType} -> {HandlerType}",
                eventType.Name, handlerType.Name);
            return false;
        }
    }

    /// <inheritdoc />
    public int GetSubscriptionCount<TEvent>() where TEvent : class, IIntegrationEvent
    {
        var eventType = typeof(TEvent);
        return _subscriptions.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Type> GetRegisteredEventTypes()
    {
        return _subscriptions.Keys.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public void ClearSubscriptions()
    {
        _subscriptions.Clear();
        _statistics.RegisteredHandlers = 0;
        _statistics.RegisteredEventTypes = 0;
        _logger.LogInformation("已清空所有事件订阅");
    }

    /// <summary>
    /// 获取事件总线统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public EventBusStatistics GetStatistics()
    {
        return new EventBusStatistics
        {
            TotalPublishedEvents = _totalPublishedEvents,
            TotalProcessedEvents = _totalProcessedEvents,
            FailedEvents = _failedEvents,
            RegisteredEventTypes = _statistics.RegisteredEventTypes,
            RegisteredHandlers = _statistics.RegisteredHandlers,
            LastActivityTime = _statistics.LastActivityTime
        };
    }

    /// <summary>
    /// 处理单个事件的异步方法
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="handlerType">处理器类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    private async Task ProcessEventAsync(IIntegrationEvent @event, Type handlerType, CancellationToken cancellationToken)
    {
        try
        {
            // 从服务容器获取处理器实例
            var handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
            {
                _logger.LogWarning("无法从服务容器获取处理器实例: {HandlerType}", handlerType.Name);
                return;
            }

            // 获取HandleAsync方法
            var method = handlerType.GetMethod("HandleAsync");
            if (method == null)
            {
                _logger.LogWarning("处理器 {HandlerType} 没有HandleAsync方法", handlerType.Name);
                return;
            }

            // 调用处理器的HandleAsync方法
            var result = method.Invoke(handler, new object[] { @event, cancellationToken });
            if (result is Task task)
            {
                await task;
            }
            else
            {
                _logger.LogWarning("处理器 {HandlerType} 的HandleAsync方法没有返回Task", handlerType.Name);
                return;
            }

            Interlocked.Increment(ref _totalProcessedEvents);
            _logger.LogDebug("事件处理器 {HandlerType} 成功处理事件 {EventType}",
                handlerType.Name, @event.EventType);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedEvents);
            _logger.LogError(ex, "事件处理器 {HandlerType} 处理事件 {EventType} 时发生异常",
                handlerType.Name, @event.EventType);
            // 不重新抛出异常，避免影响其他处理器
        }
    }
}
