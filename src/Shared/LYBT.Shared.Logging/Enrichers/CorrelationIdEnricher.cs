using LYBT.Shared.Logging.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace LYBT.Shared.Logging.Enrichers;

/// <summary>
/// CorrelationId日志富集器
/// 通过ICorrelationIdProvider接口获取CorrelationId，支持Server和Desktop两端
/// </summary>
/// <remarks>
/// 此Enricher作为LogContext.PushProperty的补充机制:
/// - Server端使用HttpContextCorrelationIdProvider从HttpContext获取
/// - Desktop端使用AsyncLocalCorrelationIdProvider从AsyncLocal获取
/// - 优先从LogContext获取(由中间件注入),其次从Provider获取
/// </remarks>
public class CorrelationIdEnricher : ILogEventEnricher
{
    /// <summary>
    /// CorrelationId属性名称
    /// </summary>
    public const string CorrelationIdPropertyName = "CorrelationId";

    /// <summary>
    /// 默认CorrelationId值(当无法获取时使用)
    /// </summary>
    public const string DefaultCorrelationId = "N/A";

    private readonly ICorrelationIdProvider _correlationIdProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="correlationIdProvider">CorrelationId提供者</param>
    public CorrelationIdEnricher(ICorrelationIdProvider correlationIdProvider)
    {
        _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
    }

    /// <summary>
    /// 富集日志事件,添加CorrelationId属性
    /// </summary>
    /// <param name="logEvent">日志事件</param>
    /// <param name="propertyFactory">属性工厂</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        // 如果LogContext中已有CorrelationId(由中间件注入),则不覆盖
        if (logEvent.Properties.ContainsKey(CorrelationIdPropertyName))
        {
            return;
        }

        var correlationId = _correlationIdProvider.GetCorrelationId() ?? DefaultCorrelationId;
        var property = propertyFactory.CreateProperty(CorrelationIdPropertyName, correlationId);
        logEvent.AddPropertyIfAbsent(property);
    }
}

/// <summary>
/// CorrelationId Enricher扩展方法
/// </summary>
public static class CorrelationIdEnricherExtensions
{
    /// <summary>
    /// 添加CorrelationId Enricher
    /// </summary>
    /// <param name="enrichmentConfiguration">富集配置</param>
    /// <param name="correlationIdProvider">CorrelationId提供者</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static Serilog.LoggerConfiguration WithCorrelationId(
        this Serilog.Configuration.LoggerEnrichmentConfiguration enrichmentConfiguration,
        ICorrelationIdProvider correlationIdProvider)
    {
        ArgumentNullException.ThrowIfNull(enrichmentConfiguration);
        return enrichmentConfiguration.With(new CorrelationIdEnricher(correlationIdProvider));
    }
}
