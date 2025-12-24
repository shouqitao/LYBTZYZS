using System.Diagnostics;
using System.Net.Http;
using LYBT.Shared.Logging.Masking;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Http;

/// <summary>
/// HTTP请求/响应日志处理器
/// LOG-012: 记录所有API调用的请求和响应信息
/// LOG-013: 添加traceparent header用于分布式追踪
/// </summary>
public class LoggingHttpHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHttpHandler> _logger;

    public LoggingHttpHandler(ILogger<LoggingHttpHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 获取或创建CorrelationId
        var activity = Activity.Current;
        var correlationId = activity?.Id ?? Guid.NewGuid().ToString("N");

        // LOG-013: 添加traceparent header用于分布式追踪
        if (activity != null && !request.Headers.Contains("traceparent"))
        {
            request.Headers.TryAddWithoutValidation("traceparent", activity.Id);
        }

        var sw = Stopwatch.StartNew();
        var method = request.Method;
        var uri = SensitiveDataMasker.MaskUri(request.RequestUri?.ToString() ?? "");

        // LOG-012: 记录请求
        _logger.LogInformation(
            "[HTTP] >>> {Method} {Uri} CorrelationId={CorrelationId}",
            method,
            uri,
            correlationId);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            // LOG-012: 记录响应
            var logLevel = response.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;
            _logger.Log(logLevel,
                "[HTTP] <<< {StatusCode} {Uri} Duration={Duration}ms CorrelationId={CorrelationId}",
                (int)response.StatusCode,
                uri,
                sw.ElapsedMilliseconds,
                correlationId);

            // 非成功响应记录Body（脱敏后）
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!string.IsNullOrEmpty(body))
                    {
                        _logger.LogWarning(
                            "[HTTP] Error Response Body: {Body} CorrelationId={CorrelationId}",
                            SensitiveDataMasker.SanitizeText(body),
                            correlationId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[HTTP] Failed to read error response body");
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[HTTP] !!! {Method} {Uri} failed after {Duration}ms CorrelationId={CorrelationId}",
                method,
                uri,
                sw.ElapsedMilliseconds,
                correlationId);
            throw;
        }
    }
}
