using LYBT.Core.EventBus.Abstractions;
using LYBT.Core.EventBus.Extensions;
using LYBT.Core.EventBus.Implementation;
using LYBT.Core.EventBus.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Core.EventBus.Tests.Extensions;

/// <summary>
/// ServiceCollectionExtensions 测试类
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInMemoryEventBus_ShouldRegisterEventBusServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // 添加日志服务

        // Act
        services.AddInMemoryEventBus();

        // Assert
        using var provider = services.BuildServiceProvider();
        
        // 验证事件总线注册
        var eventBus = provider.GetService<IEventBus>();
        eventBus.Should().NotBeNull();
        eventBus.Should().BeOfType<InMemoryEventBus>();
        
        // 验证托管服务注册
        var hostedServices = provider.GetServices<IHostedService>();
        hostedServices.Should().ContainSingle(s => s.GetType() == typeof(EventBusHostedService));
        
        // 验证配置选项注册
        var options = provider.GetService<IOptions<EventBusSubscriptionOptions>>();
        options.Should().NotBeNull();
    }

    [Fact]
    public void AddInMemoryEventBus_MultipleCalls_ShouldRegisterSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // 添加日志服务

        // Act
        services.AddInMemoryEventBus();
        services.AddInMemoryEventBus();

        // Assert
        using var provider = services.BuildServiceProvider();
        
        var eventBus1 = provider.GetService<IEventBus>();
        var eventBus2 = provider.GetService<IEventBus>();
        
        eventBus1.Should().BeSameAs(eventBus2);
    }

    [Fact]
    public void AddEventHandler_ShouldRegisterHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventHandler<TestEvent, TestEventHandler>();

        // Assert
        using var provider = services.BuildServiceProvider();
        
        var handler = provider.GetService<TestEventHandler>();
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AddEventHandler_WithLifetime_ShouldRegisterWithSpecifiedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventHandler<TestEvent, TestEventHandler>(ServiceLifetime.Singleton);

        // Assert
        using var provider = services.BuildServiceProvider();
        
        var handler1 = provider.GetService<TestEventHandler>();
        var handler2 = provider.GetService<TestEventHandler>();
        
        handler1.Should().BeSameAs(handler2);
    }

    [Fact]
    public void AddEventHandlerWithSubscription_ShouldRegisterHandlerAndSubscription()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventHandlerWithSubscription<TestEvent, TestEventHandler>();

        // Assert
        using var provider = services.BuildServiceProvider();
        
        // 验证处理器注册
        var handler = provider.GetService<TestEventHandler>();
        handler.Should().NotBeNull();
        
        // 验证订阅配置
        var options = provider.GetService<IOptions<EventBusSubscriptionOptions>>();
        options.Should().NotBeNull();

        var subscriptions = options!.Value.Subscriptions;
        subscriptions.Should().ContainSingle();
        subscriptions[0].EventType.Should().Be(typeof(TestEvent));
        subscriptions[0].HandlerType.Should().Be(typeof(TestEventHandler));
    }

    /// <summary>
    /// 测试事件
    /// </summary>
    private class TestEvent : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EventType { get; } = nameof(TestEvent);
        public string Source { get; } = "Test";
        public int Version { get; } = 1;
    }

    /// <summary>
    /// 测试事件处理器
    /// </summary>
    private class TestEventHandler : IIntegrationEventHandler<TestEvent>
    {
        public string HandlerName => "TestEventHandler";
        public Type EventType => typeof(TestEvent);

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// EventBusSubscriptionOptions 测试类
/// </summary>
public class EventBusSubscriptionOptionsTests
{
    [Fact]
    public void AddSubscription_WithGenericTypes_ShouldAddSubscription()
    {
        // Arrange
        var options = new EventBusSubscriptionOptions();

        // Act
        options.AddSubscription<TestEvent, TestEventHandler>();

        // Assert
        options.Subscriptions.Should().ContainSingle();
        options.Subscriptions[0].EventType.Should().Be(typeof(TestEvent));
        options.Subscriptions[0].HandlerType.Should().Be(typeof(TestEventHandler));
    }

    [Fact]
    public void AddSubscription_WithTypes_ShouldAddSubscription()
    {
        // Arrange
        var options = new EventBusSubscriptionOptions();

        // Act
        options.AddSubscription(typeof(TestEvent), typeof(TestEventHandler));

        // Assert
        options.Subscriptions.Should().ContainSingle();
        options.Subscriptions[0].EventType.Should().Be(typeof(TestEvent));
        options.Subscriptions[0].HandlerType.Should().Be(typeof(TestEventHandler));
    }

    [Fact]
    public void AddSubscription_WithNullEventType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new EventBusSubscriptionOptions();

        // Act & Assert
        var action = () => options.AddSubscription(null!, typeof(TestEventHandler));
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("eventType");
    }

    [Fact]
    public void AddSubscription_WithNullHandlerType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new EventBusSubscriptionOptions();

        // Act & Assert
        var action = () => options.AddSubscription(typeof(TestEvent), null!);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("handlerType");
    }

    [Fact]
    public void AddSubscription_MultipleTimes_ShouldAddMultipleSubscriptions()
    {
        // Arrange
        var options = new EventBusSubscriptionOptions();

        // Act
        options.AddSubscription<TestEvent, TestEventHandler>();
        options.AddSubscription<TestEvent2, TestEventHandler2>();

        // Assert
        options.Subscriptions.Should().HaveCount(2);
    }

    /// <summary>
    /// 测试事件
    /// </summary>
    private class TestEvent : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EventType { get; } = nameof(TestEvent);
        public string Source { get; } = "Test";
        public int Version { get; } = 1;
    }

    /// <summary>
    /// 测试事件2
    /// </summary>
    private class TestEvent2 : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EventType { get; } = nameof(TestEvent2);
        public string Source { get; } = "Test";
        public int Version { get; } = 1;
    }

    /// <summary>
    /// 测试事件处理器
    /// </summary>
    private class TestEventHandler : IIntegrationEventHandler<TestEvent>
    {
        public string HandlerName => "TestEventHandler";
        public Type EventType => typeof(TestEvent);

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 测试事件处理器2
    /// </summary>
    private class TestEventHandler2 : IIntegrationEventHandler<TestEvent2>
    {
        public string HandlerName => "TestEventHandler2";
        public Type EventType => typeof(TestEvent2);

        public Task HandleAsync(TestEvent2 @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}