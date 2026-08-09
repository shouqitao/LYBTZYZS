using System.Diagnostics;

namespace LYBT.Shared.Logging.Correlation;

/// <summary>
/// 基于System.Diagnostics.Activity的CorrelationId提供者
/// 使用W3C TraceId作为CorrelationId，实现与分布式追踪的统一
/// </summary>
/// <remarks>
/// Activity API优势：
/// - .NET原生支持，自动AsyncLocal传播
/// - HttpClient自动添加W3C traceparent头
/// - 兼容OpenTelemetry标准
/// </remarks>
public class ActivityCorrelationIdProvider : ICorrelationIdProvider
{
    /// <inheritdoc/>
    public string? GetCorrelationId()
    {
        return Activity.Current?.TraceId.ToString();
    }
}
