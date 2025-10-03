using LYBT.Core.EventBus.Abstractions;
using LYBT.Core.EventBus.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LYBT.Core.EventBus.Extensions;

/// <summary>
/// 事件总线服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加内存事件总线服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        // 注册事件总线为单例
        services.TryAddSingleton<IEventBus, InMemoryEventBus>();

        // 注册订阅配置选项
        services.Configure<EventBusSubscriptionOptions>(_ => { });

        // 注册托管服务
        services.AddHostedService<Services.EventBusHostedService>();

        return services;
    }

    /// <summary>
    /// 添加事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="lifetime">服务生命周期，默认为Scoped</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEventHandler<TEvent, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        // 注册处理器
        services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));

        return services;
    }

    /// <summary>
    /// 添加事件处理器并自动订阅
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="lifetime">服务生命周期，默认为Scoped</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEventHandlerWithSubscription<TEvent, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        // 注册处理器
        services.AddEventHandler<TEvent, THandler>(lifetime);

        // 添加配置回调，在服务构建完成后自动订阅
        services.Configure<EventBusSubscriptionOptions>(options =>
        {
            options.AddSubscription<TEvent, THandler>();
        });

        return services;
    }
}

/// <summary>
/// 事件总线订阅配置选项
/// </summary>
public class EventBusSubscriptionOptions
{
    private readonly List<(Type EventType, Type HandlerType)> _subscriptions = new();

    /// <summary>
    /// 获取所有订阅配置
    /// </summary>
    public IReadOnlyList<(Type EventType, Type HandlerType)> Subscriptions => _subscriptions.AsReadOnly();

    /// <summary>
    /// 添加订阅配置
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">处理器类型</typeparam>
    public void AddSubscription<TEvent, THandler>()
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        _subscriptions.Add((typeof(TEvent), typeof(THandler)));
    }

    /// <summary>
    /// 添加订阅配置
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handlerType">处理器类型</param>
    public void AddSubscription(Type eventType, Type handlerType)
    {
        if (eventType == null)
            throw new ArgumentNullException(nameof(eventType));
        if (handlerType == null)
            throw new ArgumentNullException(nameof(handlerType));

        _subscriptions.Add((eventType, handlerType));
    }
}
