using LYBT.Core.EventBus.Abstractions;
using LYBT.Core.EventBus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Core.EventBus.Services;

/// <summary>
/// 事件总线托管服务
/// 负责在应用程序启动时自动订阅配置的事件处理器
/// </summary>
public class EventBusHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusHostedService> _logger;
    private readonly EventBusSubscriptionOptions _subscriptionOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供程序</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="subscriptionOptions">订阅配置选项</param>
    public EventBusHostedService(
        IServiceProvider serviceProvider,
        ILogger<EventBusHostedService> logger,
        IOptions<EventBusSubscriptionOptions> subscriptionOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subscriptionOptions = subscriptionOptions?.Value ?? throw new ArgumentNullException(nameof(subscriptionOptions));
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动任务</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始启动事件总线托管服务");

        try
        {
            var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
            var subscriptionCount = 0;

            // 订阅所有配置的事件处理器
            foreach (var (eventType, handlerType) in _subscriptionOptions.Subscriptions)
            {
                try
                {
                    var success = eventBus.Subscribe(eventType, handlerType);
                    if (success)
                    {
                        subscriptionCount++;
                        _logger.LogDebug("成功订阅事件处理器: {EventType} -> {HandlerType}",
                            eventType.Name, handlerType.Name);
                    }
                    else
                    {
                        _logger.LogWarning("订阅事件处理器失败: {EventType} -> {HandlerType}",
                            eventType.Name, handlerType.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "订阅事件处理器时发生异常: {EventType} -> {HandlerType}",
                        eventType.Name, handlerType.Name);
                }
            }

            _logger.LogInformation("事件总线托管服务启动完成，共订阅 {SubscriptionCount} 个事件处理器", subscriptionCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动事件总线托管服务时发生异常");
            throw;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始停止事件总线托管服务");

        try
        {
            var eventBus = _serviceProvider.GetService<IEventBus>();
            if (eventBus != null)
            {
                // 清空所有订阅
                eventBus.ClearSubscriptions();
                _logger.LogInformation("已清空所有事件订阅");
            }

            _logger.LogInformation("事件总线托管服务停止完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止事件总线托管服务时发生异常");
        }

        await Task.CompletedTask;
    }
}