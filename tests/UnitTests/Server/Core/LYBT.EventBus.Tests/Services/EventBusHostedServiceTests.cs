using LYBT.EventBus.Abstractions;
using LYBT.EventBus.Extensions;
using LYBT.EventBus.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Core.EventBus.Tests.Services;

/// <summary>
/// EventBusHostedService 测试类
/// </summary>
public class EventBusHostedServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<EventBusHostedService>> _mockLogger;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly EventBusSubscriptionOptions _subscriptionOptions;
    private readonly IOptions<EventBusSubscriptionOptions> _options;

    public EventBusHostedServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<EventBusHostedService>>();
        _mockEventBus = new Mock<IEventBus>();
        _subscriptionOptions = new EventBusSubscriptionOptions();
        _options = Options.Create(_subscriptionOptions);

        // 设置 ServiceProvider 返回 EventBus
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEventBus)))
                           .Returns(_mockEventBus.Object);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new EventBusHostedService(null!, _mockLogger.Object, _options);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new EventBusHostedService(_mockServiceProvider.Object, null!, _options);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new EventBusHostedService(_mockServiceProvider.Object, _mockLogger.Object, null!);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("subscriptionOptions");
    }

    [Fact]
    public async Task StartAsync_WithNoSubscriptions_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new EventBusHostedService(_mockServiceProvider.Object, _mockLogger.Object, _options);

        // Act & Assert
        var action = async () => await service.StartAsync(CancellationToken.None);
        await action.Should().NotThrowAsync();

        // 验证没有调用订阅方法
        _mockEventBus.Verify(eb => eb.Subscribe(It.IsAny<Type>(), It.IsAny<Type>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WithSubscriptions_ShouldSubscribeAll()
    {
        // Arrange
        _subscriptionOptions.AddSubscription<TestEvent, TestEventHandler>();
        _subscriptionOptions.AddSubscription<TestEvent2, TestEventHandler2>();

        _mockEventBus.Setup(eb => eb.Subscribe(It.IsAny<Type>(), It.IsAny<Type>()))
                     .Returns(true);

        var service = new EventBusHostedService(_mockServiceProvider.Object, _mockLogger.Object, _options);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _mockEventBus.Verify(eb => eb.Subscribe(typeof(TestEvent), typeof(TestEventHandler)), Times.Once);
        _mockEventBus.Verify(eb => eb.Subscribe(typeof(TestEvent2), typeof(TestEventHandler2)), Times.Once);
    }

    [Fact]
    public async Task StartAsync_SubscriptionFails_ShouldContinueWithOthers()
    {
        // Arrange
        _subscriptionOptions.AddSubscription<TestEvent, TestEventHandler>();
        _subscriptionOptions.AddSubscription<TestEvent2, TestEventHandler2>();

        _mockEventBus.Setup(eb => eb.Subscribe(typeof(TestEvent), typeof(TestEventHandler)))
                     .Returns(false);
        _mockEventBus.Setup(eb => eb.Subscribe(typeof(TestEvent2), typeof(TestEventHandler2)))
                     .Returns(true);

        var service = new EventBusHostedService(_mockServiceProvider.Object, _mockLogger.Object, _options);

        // Act & Assert
        var action = async () => await service.StartAsync(CancellationToken.None);
        await action.Should().NotThrowAsync();

        _mockEventBus.Verify(eb => eb.Subscribe(typeof(TestEvent), typeof(TestEventHandler)), Times.Once);
        _mockEventBus.Verify(eb => eb.Subscribe(typeof(TestEvent2), typeof(TestEventHandler2)), Times.Once);
    }

    [Fact]
    public async Task StartAsync_SubscriptionThrowsException_ShouldContinueWithOthers()
    {
        // Arrange
        _subscriptionOptions.AddSubscription<TestEvent, TestEventHandler>();
        _subscriptionOptions.AddSubscription<TestEvent2, TestEventHandler2>();

        _mockEventBus.Setup(eb => eb.Subscribe(typeof(TestEvent), typeof(TestEventHandler)))
                     .Throws(new InvalidOperationException("Test exception"));
        _mockEventBus.Setup(eb => eb.Subscribe(typeof(TestEvent2), typeof(TestEventHandler2)))
                     .Returns(true);

        var service = new EventBusHostedService(_mockServiceProvider.Object, _mockLogger.Object, _options);

        // Act & Assert
        var action = async () => await service.StartAsync(CancellationToken.None);
        await action.Should().NotThrowAsync();

        _mockEventBus.Verify(eb => eb.Subscribe(typeof(TestEvent), typeof(TestEventHandler)), Times.Once);
        _mockEventBus.Verify(eb => eb.Subscribe(typeof(TestEvent2), typeof(TestEventHandler2)), Times.Once);
    }

    [Fact]
    public async Task StartAsync_EventBusNotAvailable_ShouldThrowException()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEventBus)))
                           .Throws(new InvalidOperationException("EventBus not registered"));

        var service = new EventBusHostedService(_mockServiceProvider.Object, _mockLogger.Object, _options);

        // Act & Assert
        var action = async () => await service.StartAsync(CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
                   .WithMessage("EventBus not registered");
    }

    [Fact]
    public async Task StopAsync_WithEventBus_ShouldClearSubscriptions()
    {
        // Arrange
        var service = new EventBusHostedService(_mockServiceProvider.Object, _mockLogger.Object, _options);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockEventBus.Verify(eb => eb.ClearSubscriptions(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_WithoutEventBus_ShouldCompleteSuccessfully()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEventBus)))
                           .Returns((IEventBus?)null);

        var service = new EventBusHostedService(_mockServiceProvider.Object, _mockLogger.Object, _options);

        // Act & Assert
        var action = async () => await service.StopAsync(CancellationToken.None);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_ClearSubscriptionsThrows_ShouldNotThrow()
    {
        // Arrange
        _mockEventBus.Setup(eb => eb.ClearSubscriptions())
                     .Throws(new InvalidOperationException("Test exception"));

        var service = new EventBusHostedService(_mockServiceProvider.Object, _mockLogger.Object, _options);

        // Act & Assert
        var action = async () => await service.StopAsync(CancellationToken.None);
        await action.Should().NotThrowAsync();
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