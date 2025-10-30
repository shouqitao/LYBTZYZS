using LYBT.EventBus.Events;
using FluentAssertions;
using Xunit;

namespace LYBT.Core.EventBus.Tests.Events;

/// <summary>
/// IntegrationEventBase 测试类
/// </summary>
public class IntegrationEventBaseTests
{
    /// <summary>
    /// 测试事件实例
    /// </summary>
    private class TestEvent : IntegrationEventBase
    {
        public string TestData { get; set; } = string.Empty;

        public TestEvent() : base() { }
        
        public TestEvent(string source) : base(source) { }
    }

    [Fact]
    public void Constructor_ShouldInitializeBasicProperties()
    {
        // Arrange & Act
        var @event = new TestEvent();

        // Assert
        @event.Id.Should().NotBeEmpty();
        @event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        @event.EventType.Should().Be(nameof(TestEvent));
        @event.Source.Should().Be("Unknown");
        @event.Version.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithSource_ShouldSetSource()
    {
        // Arrange
        var source = "TestModule";

        // Act
        var @event = new TestEvent(source);

        // Assert
        @event.Source.Should().Be(source);
        @event.Id.Should().NotBeEmpty();
        @event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        @event.EventType.Should().Be(nameof(TestEvent));
        @event.Version.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new TestEvent(null!);
        action.Should().Throw<ArgumentNullException>()
              .WithParameterName("source");
    }

    [Fact]
    public void GetDescription_ShouldReturnFormattedString()
    {
        // Arrange
        var @event = new TestEvent("TestModule");

        // Act
        var description = @event.GetDescription();

        // Assert
        description.Should().Contain(@event.EventType);
        description.Should().Contain(@event.Source);
        description.Should().Contain(@event.OccurredOn.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var @event = new TestEvent("TestModule");

        // Act
        var result = @event.ToString();

        // Assert
        result.Should().Contain(@event.EventType);
        result.Should().Contain(@event.Id.ToString());
        result.Should().Contain(@event.Source);
        result.Should().Contain(@event.OccurredOn.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var event1 = new TestEvent();
        var event2 = new TestEvent();
        
        // 使用反射设置相同的ID
        var idProperty = typeof(IntegrationEventBase).GetProperty("Id")!;
        idProperty.SetValue(event2, event1.Id);

        // Act & Assert
        event1.Equals(event2).Should().BeTrue();
        event2.Equals(event1).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var event1 = new TestEvent();
        var event2 = new TestEvent();

        // Act & Assert
        event1.Equals(event2).Should().BeFalse();
        event2.Equals(event1).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNonEventObject_ShouldReturnFalse()
    {
        // Arrange
        var @event = new TestEvent();
        var nonEvent = new object();

        // Act & Assert
        @event.Equals(nonEvent).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var @event = new TestEvent();

        // Act & Assert
        @event.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeBasedOnId()
    {
        // Arrange
        var event1 = new TestEvent();
        var event2 = new TestEvent();
        
        // 使用反射设置相同的ID
        var idProperty = typeof(IntegrationEventBase).GetProperty("Id")!;
        idProperty.SetValue(event2, event1.Id);

        // Act & Assert
        event1.GetHashCode().Should().Be(event2.GetHashCode());
    }

    [Fact]
    public void ResetForTesting_ShouldUpdateIdAndTime()
    {
        // Arrange
        var @event = new TestEvent();
        var originalId = @event.Id;
        var originalTime = @event.OccurredOn;

        // 等待一毫秒确保时间不同
        Thread.Sleep(1);

        // Act
        var resetMethod = typeof(IntegrationEventBase).GetMethod("ResetForTesting", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        resetMethod.Invoke(@event, null);

        // Assert
        @event.Id.Should().NotBe(originalId);
        @event.OccurredOn.Should().BeAfter(originalTime);
    }
}