using LYBT.Shared.Logging.Abstractions;
using LYBT.Shared.Logging.Enrichers;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace LYBT.Tests.Unit.Shared.Logging;

/// <summary>
/// CorrelationIdEnricher 单元测试
/// Sprint3-A3-09: Shared.Logging 零覆盖测试
/// </summary>
public class CorrelationIdEnricherTests
{
    [Fact]
    public void Constructor_WithNullProvider_ShouldThrow()
    {
        var act = () => new CorrelationIdEnricher(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enrich_ShouldAddCorrelationId()
    {
        var provider = Substitute.For<ICorrelationIdProvider>();
        provider.GetCorrelationId().Returns("test-correlation-id");

        var enricher = new CorrelationIdEnricher(provider);
        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();

        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().ContainKey(CorrelationIdEnricher.CorrelationIdPropertyName);
    }

    [Fact]
    public void Enrich_WhenPropertyExists_ShouldNotOverwrite()
    {
        var provider = Substitute.For<ICorrelationIdProvider>();
        provider.GetCorrelationId().Returns("new-id");

        var enricher = new CorrelationIdEnricher(provider);
        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();

        // Pre-add CorrelationId
        logEvent.AddPropertyIfAbsent(
            factory.CreateProperty(CorrelationIdEnricher.CorrelationIdPropertyName, "existing-id"));

        enricher.Enrich(logEvent, factory);

        var propValue = logEvent.Properties[CorrelationIdEnricher.CorrelationIdPropertyName].ToString();
        propValue.Should().Contain("existing-id");
    }

    [Fact]
    public void Enrich_WhenProviderReturnsNull_ShouldUseDefault()
    {
        var provider = Substitute.For<ICorrelationIdProvider>();
        provider.GetCorrelationId().Returns((string?)null);

        var enricher = new CorrelationIdEnricher(provider);
        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();

        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().ContainKey(CorrelationIdEnricher.CorrelationIdPropertyName);
    }

    [Fact]
    public void CorrelationIdPropertyName_ShouldBeExpectedValue()
    {
        CorrelationIdEnricher.CorrelationIdPropertyName.Should().Be("CorrelationId");
    }

    [Fact]
    public void DefaultCorrelationId_ShouldBeNA()
    {
        CorrelationIdEnricher.DefaultCorrelationId.Should().Be("N/A");
    }

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", []),
            []);
    }

    /// <summary>
    /// 简单的 ILogEventPropertyFactory 实现用于测试
    /// </summary>
    private class LogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
