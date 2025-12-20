using System.Net.Http;
using LYBT.Desktop.Foundation.Logging;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LYBT.Desktop.Infrastructure.Http;

/// <summary>
/// HTTP请求CorrelationId注入处理器
/// refactor-logging-system: 为每个HTTP请求注入X-Correlation-ID头，实现端到端追踪
/// </summary>
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<CorrelationIdDelegatingHandler>? _logger;

    /// <summary>
    /// HTTP头名称
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdDelegatingHandler(ILogger<CorrelationIdDelegatingHandler>? logger = null)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 获取或创建CorrelationId
        var correlationId = CorrelationIdContext.CurrentOrNew;

        // 注入到HTTP请求头
        if (!request.Headers.Contains(CorrelationIdHeader))
        {
            request.Headers.Add(CorrelationIdHeader, correlationId);
        }

        // 使用Serilog LogContext确保日志包含CorrelationId
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger?.LogDebug(
                "发送HTTP请求 - 方法: {HttpMethod}, URI: {RequestUri}, CorrelationId: {CorrelationId}",
                request.Method,
                request.RequestUri,
                correlationId);

            var response = await base.SendAsync(request, cancellationToken);

            // 记录响应状态
            _logger?.LogDebug(
                "收到HTTP响应 - 状态码: {StatusCode}, CorrelationId: {CorrelationId}",
                (int)response.StatusCode,
                correlationId);

            // 如果服务器返回了不同的CorrelationId，记录日志
            if (response.Headers.TryGetValues(CorrelationIdHeader, out var serverCorrelationIds))
            {
                var serverCorrelationId = serverCorrelationIds.FirstOrDefault();
                if (!string.IsNullOrEmpty(serverCorrelationId) && serverCorrelationId != correlationId)
                {
                    _logger?.LogDebug(
                        "服务器返回的CorrelationId与请求不同 - 请求: {RequestCorrelationId}, 响应: {ResponseCorrelationId}",
                        correlationId,
                        serverCorrelationId);
                }
            }

            return response;
        }
    }
}
