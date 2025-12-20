using LYBT.Desktop.Foundation.Logging;
using Serilog.Core;
using Serilog.Events;

namespace LYBT.Desktop.Infrastructure.Logging;

/// <summary>
/// 客户端CorrelationId日志Enricher
/// 自动将当前CorrelationId添加到所有日志事件
/// </summary>
/// <remarks>
/// 已废弃：请使用 LYBT.Shared.Logging.Enrichers.CorrelationIdEnricher
/// 配合 FoundationCorrelationIdProvider 使用
/// 此类保留用于向后兼容
/// </remarks>
[Obsolete("使用 LYBT.Shared.Logging.Enrichers.CorrelationIdEnricher 配合 FoundationCorrelationIdProvider")]
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
