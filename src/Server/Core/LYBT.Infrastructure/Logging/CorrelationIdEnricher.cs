using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace LYBT.Infrastructure.Logging;

/// <summary>
/// CorrelationId日志富集器
/// refactor-logging-system: 从HttpContext获取CorrelationId并添加到日志上下文
/// </summary>
/// <remarks>
/// 此Enricher作为LogContext.PushProperty的补充机制:
/// - 中间件使用LogContext.PushProperty在请求作用域内注入CorrelationId
/// - 此Enricher用于LogContext未生效的场景(如后台任务、非HTTP上下文)
/// - 优先从LogContext获取(由中间件注入),其次从HttpContext.Items获取
/// </remarks>
public class CorrelationIdEnricher : ILogEventEnricher
{
    /// <summary>
    /// CorrelationId属性名称
    /// </summary>
    public const string CorrelationIdPropertyName = "CorrelationId";

    /// <summary>
    /// HttpContext.Items中存储CorrelationId的键名
    /// </summary>
    public const string CorrelationIdItemKey = "CorrelationId";

    /// <summary>
    /// 默认CorrelationId值(当无法获取时使用)
    /// </summary>
    public const string DefaultCorrelationId = "N/A";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
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

        var correlationId = GetCorrelationId();
        var property = propertyFactory.CreateProperty(CorrelationIdPropertyName, correlationId);
        logEvent.AddPropertyIfAbsent(property);
    }

    /// <summary>
    /// 从HttpContext获取CorrelationId
    /// </summary>
    /// <returns>CorrelationId,如果不存在返回默认值</returns>
    private string GetCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return DefaultCorrelationId;
        }

        if (httpContext.Items.TryGetValue(CorrelationIdItemKey, out var correlationIdObj)
            && correlationIdObj is string correlationId
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return DefaultCorrelationId;
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
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static Serilog.LoggerConfiguration WithCorrelationId(
        this Serilog.Configuration.LoggerEnrichmentConfiguration enrichmentConfiguration,
        IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(enrichmentConfiguration);
        return enrichmentConfiguration.With(new CorrelationIdEnricher(httpContextAccessor));
    }
}
