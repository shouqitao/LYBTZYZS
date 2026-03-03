namespace LYBT.Infrastructure.Constants;

/// <summary>
/// HTTP 头部和认证相关常量
/// </summary>
public static class HttpHeaderConstants
{
    public const string CorrelationId = "X-Correlation-ID";
    public const string Traceparent = "traceparent";
    public const string CorrelationIdItemKey = "CorrelationId";
    public const string TraceIdKey = "traceId";
    public const string BearerScheme = "Bearer";
}
