using LYBT.Shared.Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace LYBT.Infrastructure.Logging;

/// <summary>
/// Serilog配置扩展方法
/// 提供Server端日志配置的便捷方法
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// 添加敏感数据脱敏策略（使用共享组件）
    /// </summary>
    /// <param name="configuration">Serilog配置</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static LoggerConfiguration WithSensitiveDataMasking(this LoggerConfiguration configuration)
    {
        return configuration.Destructure.With<Shared.Logging.Masking.SensitiveDataDestructuringPolicy>();
    }

    /// <summary>
    /// 添加HttpContext的CorrelationId Enricher
    /// </summary>
    /// <param name="enrichmentConfiguration">富集配置</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static LoggerConfiguration WithHttpContextCorrelationId(
        this Serilog.Configuration.LoggerEnrichmentConfiguration enrichmentConfiguration,
        IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(enrichmentConfiguration);
        var provider = new HttpContextCorrelationIdProvider(httpContextAccessor);
        return enrichmentConfiguration.With(new Shared.Logging.Enrichers.CorrelationIdEnricher(provider));
    }

    /// <summary>
    /// 添加CorrelationId Enricher（使用ICorrelationIdProvider）
    /// </summary>
    /// <param name="enrichmentConfiguration">富集配置</param>
    /// <param name="correlationIdProvider">CorrelationId提供者</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static LoggerConfiguration WithCorrelationIdProvider(
        this Serilog.Configuration.LoggerEnrichmentConfiguration enrichmentConfiguration,
        ICorrelationIdProvider correlationIdProvider)
    {
        ArgumentNullException.ThrowIfNull(enrichmentConfiguration);
        return enrichmentConfiguration.With(new Shared.Logging.Enrichers.CorrelationIdEnricher(correlationIdProvider));
    }
}
