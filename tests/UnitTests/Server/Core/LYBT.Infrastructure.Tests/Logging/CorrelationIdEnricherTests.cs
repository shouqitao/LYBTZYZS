using FluentAssertions;
using LYBT.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using Moq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace LYBT.Infrastructure.Tests.Logging;

/// <summary>
/// CorrelationIdEnricher单元测试
/// refactor-logging-system: Task 4.1
/// </summary>
public class CorrelationIdEnricherTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly CorrelationIdEnricher _sut;

    public CorrelationIdEnricherTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _sut = new CorrelationIdEnricher(_httpContextAccessorMock.Object);
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new CorrelationIdEnricher(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpContextAccessor");
    }

    [Fact]
    public void Constructor_WithValidHttpContextAccessor_ShouldNotThrow()
    {
        // Act
        var act = () => new CorrelationIdEnricher(_httpContextAccessorMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Enrich方法测试

    [Fact]
    public void Enrich_WhenLogEventIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var propertyFactoryMock = new Mock<ILogEventPropertyFactory>();

        // Act
        var act = () => _sut.Enrich(null!, propertyFactoryMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enrich_WhenPropertyFactoryIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logEvent = CreateLogEvent();

        // Act
        var act = () => _sut.Enrich(logEvent, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enrich_WhenCorrelationIdAlreadyExists_ShouldNotOverwrite()
    {
        // Arrange
        var existingCorrelationId = "existing-correlation-id";
        var logEvent = CreateLogEventWithCorrelationId(existingCorrelationId);
        var propertyFactory = CreatePropertyFactory();

        // Act
        _sut.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey(CorrelationIdEnricher.CorrelationIdPropertyName);
        var property = logEvent.Properties[CorrelationIdEnricher.CorrelationIdPropertyName];
        property.ToString().Should().Contain(existingCorrelationId);
    }

    [Fact]
    public void Enrich_WhenHttpContextIsNull_ShouldUseDefaultCorrelationId()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var logEvent = CreateLogEvent();
        var propertyFactory = CreatePropertyFactory();

        // Act
        _sut.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey(CorrelationIdEnricher.CorrelationIdPropertyName);
        var property = logEvent.Properties[CorrelationIdEnricher.CorrelationIdPropertyName];
        property.ToString().Should().Contain(CorrelationIdEnricher.DefaultCorrelationId);
    }

    [Fact]
    public void Enrich_WhenCorrelationIdInHttpContext_ShouldUseIt()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-123";
        var httpContext = new DefaultHttpContext();
        httpContext.Items[CorrelationIdEnricher.CorrelationIdItemKey] = expectedCorrelationId;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var logEvent = CreateLogEvent();
        var propertyFactory = CreatePropertyFactory();

        // Act
        _sut.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey(CorrelationIdEnricher.CorrelationIdPropertyName);
        var property = logEvent.Properties[CorrelationIdEnricher.CorrelationIdPropertyName];
        property.ToString().Should().Contain(expectedCorrelationId);
    }

    [Fact]
    public void Enrich_WhenCorrelationIdNotInHttpContextItems_ShouldUseDefault()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var logEvent = CreateLogEvent();
        var propertyFactory = CreatePropertyFactory();

        // Act
        _sut.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey(CorrelationIdEnricher.CorrelationIdPropertyName);
        var property = logEvent.Properties[CorrelationIdEnricher.CorrelationIdPropertyName];
        property.ToString().Should().Contain(CorrelationIdEnricher.DefaultCorrelationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Enrich_WhenCorrelationIdIsNullOrWhitespace_ShouldUseDefault(string? correlationId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items[CorrelationIdEnricher.CorrelationIdItemKey] = correlationId!;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var logEvent = CreateLogEvent();
        var propertyFactory = CreatePropertyFactory();

        // Act
        _sut.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey(CorrelationIdEnricher.CorrelationIdPropertyName);
        var property = logEvent.Properties[CorrelationIdEnricher.CorrelationIdPropertyName];
        property.ToString().Should().Contain(CorrelationIdEnricher.DefaultCorrelationId);
    }

    [Fact]
    public void Enrich_WhenCorrelationIdItemIsNotString_ShouldUseDefault()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items[CorrelationIdEnricher.CorrelationIdItemKey] = 12345; // int instead of string
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var logEvent = CreateLogEvent();
        var propertyFactory = CreatePropertyFactory();

        // Act
        _sut.Enrich(logEvent, propertyFactory);

        // Assert
        logEvent.Properties.Should().ContainKey(CorrelationIdEnricher.CorrelationIdPropertyName);
        var property = logEvent.Properties[CorrelationIdEnricher.CorrelationIdPropertyName];
        property.ToString().Should().Contain(CorrelationIdEnricher.DefaultCorrelationId);
    }

    #endregion

    #region 常量测试

    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        // Assert
        CorrelationIdEnricher.CorrelationIdPropertyName.Should().Be("CorrelationId");
        CorrelationIdEnricher.CorrelationIdItemKey.Should().Be("CorrelationId");
        CorrelationIdEnricher.DefaultCorrelationId.Should().Be("N/A");
    }

    #endregion

    #region 辅助方法

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test message", Array.Empty<MessageTemplateToken>()),
            Array.Empty<LogEventProperty>());
    }

    private static LogEvent CreateLogEventWithCorrelationId(string correlationId)
    {
        var property = new LogEventProperty(
            CorrelationIdEnricher.CorrelationIdPropertyName,
            new ScalarValue(correlationId));

        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test message", Array.Empty<MessageTemplateToken>()),
            new[] { property });
    }

    private static ILogEventPropertyFactory CreatePropertyFactory()
    {
        var mockFactory = new Mock<ILogEventPropertyFactory>();
        mockFactory.Setup(f => f.CreateProperty(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<bool>()))
            .Returns((string name, object? value, bool destructureObjects) =>
                new LogEventProperty(name, new ScalarValue(value)));
        return mockFactory.Object;
    }

    #endregion
}
