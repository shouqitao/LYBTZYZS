using LYBT.Desktop.Foundation.Logging;
using Serilog.Core;
using Serilog.Events;

namespace LYBT.Desktop.Infrastructure.Logging;

/// <summary>
/// 客户端CorrelationId日志Enricher
/// refactor-logging-system: 自动将当前CorrelationId添加到所有日志事件
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    /// <summary>
    /// 属性名称
    /// </summary>
    public const string PropertyName = "CorrelationId";

    /// <summary>
    /// 将CorrelationId添加到日志事件
    /// </summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = CorrelationIdContext.Current ?? string.Empty;
        var property = propertyFactory.CreateProperty(PropertyName, correlationId);
        logEvent.AddPropertyIfAbsent(property);
    }
}
