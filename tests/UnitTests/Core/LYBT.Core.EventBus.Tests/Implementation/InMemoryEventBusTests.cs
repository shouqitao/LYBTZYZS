using LYBT.Core.EventBus.Abstractions;
using LYBT.Core.EventBus.Events;
using LYBT.Core.EventBus.Implementation;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Core.EventBus.Tests.Implementation;

/// <summary>
/// InMemoryEventBus 测试类
/// </summary>
public class InMemoryEventBusTests : IDisposable
{
    private readonly ServiceCollection _services;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ILogger<InMemoryEventBus>> _mockLogger;
    private readonly InMemoryEventBus _eventBus;

    /// <summary>
    /// 测试事件
    /// </summary>
    public class TestEvent : IntegrationEventBase
    {
        public string Message { get; set; } = string.Empty;

        public TestEvent(string message, string source = "TestModule") : base(source)
        {
            Message = message;
        }
    }

    /// <summary>
    /// 测试事件处理器
    /// </summary>
    public class TestEventHandler : IIntegrationEventHandler<TestEvent>
    {
        public string HandlerName => "TestEventHandler";
        public Type EventType => typeof(TestEvent);

        public List<TestEvent> ProcessedEvents { get; } = new();
        public bool ShouldThrow { get; set; } = false;

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            if (ShouldThrow)
                throw new InvalidOperationException("Test exception");

            ProcessedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    public InMemoryEventBusTests()
    {
        _services = new ServiceCollection();
        _mockLogger = new Mock<ILogger<InMemoryEventBus>>();
        
        // 注册测试处理器
        _services.AddSingleton<TestEventHandler>();
        
        _serviceProvider = _services.BuildServiceProvider();
        _eventBus = new InMemoryEventBus(_serviceProvider, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new InMemoryEventBus(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new InMemoryEventBus(_serviceProvider, null!);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("logger");
    }

    [Fact]
    public void Subscribe_ValidEventAndHandler_ShouldReturnTrue()
    {
        // Arrange, Act
        var result = _eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Assert
        result.Should().BeTrue();
        _eventBus.GetSubscriptionCount<TestEvent>().Should().Be(1);
        _eventBus.GetRegisteredEventTypes().Should().Contain(typeof(TestEvent));
    }

    [Fact]
    public void Subscribe_SameHandlerTwice_ShouldNotDuplicate()
    {
        // Arrange, Act
        var result1 = _eventBus.Subscribe<TestEvent, TestEventHandler>();
        var result2 = _eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        _eventBus.GetSubscriptionCount<TestEvent>().Should().Be(1);
    }

    [Fact]
    public void Subscribe_WithTypes_ValidEventAndHandler_ShouldReturnTrue()
    {
        // Arrange, Act
        var result = _eventBus.Subscribe(typeof(TestEvent), typeof(TestEventHandler));

        // Assert
        result.Should().BeTrue();
        _eventBus.GetSubscriptionCount<TestEvent>().Should().Be(1);
    }

    [Fact]
    public void Subscribe_WithTypes_NullEventType_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => _eventBus.Subscribe(null!, typeof(TestEventHandler));
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("eventType");
    }

    [Fact]
    public void Subscribe_WithTypes_NullHandlerType_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => _eventBus.Subscribe(typeof(TestEvent), null!);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("handlerType");
    }

    [Fact]
    public void Subscribe_WithTypes_InvalidEventType_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        var action = () => _eventBus.Subscribe(typeof(string), typeof(TestEventHandler));
        action.Should().Throw<ArgumentException>()
              .WithParameterName("eventType");
    }

    [Fact]
    public void Subscribe_WithTypes_InvalidHandlerType_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        var action = () => _eventBus.Subscribe(typeof(TestEvent), typeof(string));
        action.Should().Throw<ArgumentException>()
              .WithParameterName("handlerType");
    }

    [Fact]
    public void Unsubscribe_ExistingSubscription_ShouldReturnTrue()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Act
        var result = _eventBus.Unsubscribe<TestEvent, TestEventHandler>();

        // Assert
        result.Should().BeTrue();
        _eventBus.GetSubscriptionCount<TestEvent>().Should().Be(0);
        _eventBus.GetRegisteredEventTypes().Should().NotContain(typeof(TestEvent));
    }

    [Fact]
    public void Unsubscribe_NonExistentSubscription_ShouldReturnFalse()
    {
        // Arrange, Act
        var result = _eventBus.Unsubscribe<TestEvent, TestEventHandler>();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = async () => await _eventBus.PublishAsync<TestEvent>(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
                   .WithParameterName("event");
    }

    [Fact]
    public async Task PublishAsync_WithSubscribedHandler_ShouldProcessEvent()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent, TestEventHandler>();
        var testEvent = new TestEvent("Test message");
        var handler = _serviceProvider.GetRequiredService<TestEventHandler>();

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        handler.ProcessedEvents.Should().ContainSingle();
        handler.ProcessedEvents[0].Message.Should().Be("Test message");
    }

    [Fact]
    public async Task PublishAsync_WithoutSubscription_ShouldCompleteWithoutError()
    {
        // Arrange
        var testEvent = new TestEvent("Test message");

        // Act & Assert
        var action = async () => await _eventBus.PublishAsync(testEvent);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_HandlerThrowsException_ShouldContinueWithOtherHandlers()
    {
        // Arrange
        var handler1 = new TestEventHandler { ShouldThrow = true };
        var handler2 = new TestEventHandler { ShouldThrow = false };
        
        var services = new ServiceCollection();
        services.AddSingleton(handler1);
        services.AddSingleton(handler2);
        
        using var provider = services.BuildServiceProvider();
        var eventBus = new InMemoryEventBus(provider, _mockLogger.Object);
        
        eventBus.Subscribe(typeof(TestEvent), typeof(TestEventHandler));
        
        var testEvent = new TestEvent("Test message");

        // Act & Assert
        var action = async () => await eventBus.PublishAsync(testEvent);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void GetSubscriptionCount_WithSubscriptions_ShouldReturnCorrectCount()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Act
        var count = _eventBus.GetSubscriptionCount<TestEvent>();

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public void GetSubscriptionCount_WithoutSubscriptions_ShouldReturnZero()
    {
        // Act
        var count = _eventBus.GetSubscriptionCount<TestEvent>();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void GetRegisteredEventTypes_WithSubscriptions_ShouldReturnEventTypes()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Act
        var eventTypes = _eventBus.GetRegisteredEventTypes();

        // Assert
        eventTypes.Should().ContainSingle();
        eventTypes.Should().Contain(typeof(TestEvent));
    }

    [Fact]
    public void GetRegisteredEventTypes_WithoutSubscriptions_ShouldReturnEmptyCollection()
    {
        // Act
        var eventTypes = _eventBus.GetRegisteredEventTypes();

        // Assert
        eventTypes.Should().BeEmpty();
    }

    [Fact]
    public void ClearSubscriptions_WithSubscriptions_ShouldRemoveAll()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Act
        _eventBus.ClearSubscriptions();

        // Assert
        _eventBus.GetSubscriptionCount<TestEvent>().Should().Be(0);
        _eventBus.GetRegisteredEventTypes().Should().BeEmpty();
    }

    [Fact]
    public void GetStatistics_ShouldReturnCurrentStatistics()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent, TestEventHandler>();

        // Act
        var statistics = _eventBus.GetStatistics();

        // Assert
        statistics.Should().NotBeNull();
        statistics.RegisteredEventTypes.Should().Be(1);
        statistics.RegisteredHandlers.Should().Be(1);
        statistics.LastActivityTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PublishAsync_ShouldUpdateStatistics()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent, TestEventHandler>();
        var testEvent = new TestEvent("Test message");

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        var statistics = _eventBus.GetStatistics();
        statistics.TotalPublishedEvents.Should().Be(1);
        statistics.TotalProcessedEvents.Should().Be(1);
        statistics.FailedEvents.Should().Be(0);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}