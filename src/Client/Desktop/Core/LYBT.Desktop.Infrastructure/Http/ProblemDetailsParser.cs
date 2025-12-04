using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Http;

/// <summary>
/// ProblemDetails响应解析器
/// refactor-logging-system: 解析RFC 7807格式的错误响应
/// </summary>
public class ProblemDetailsParser
{
    private readonly ILogger<ProblemDetailsParser>? _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProblemDetailsParser(ILogger<ProblemDetailsParser>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 从HTTP响应解析ProblemDetails
    /// </summary>
    /// <param name="response">HTTP响应</param>
    /// <returns>解析结果，如果不是ProblemDetails格式则返回null</returns>
    public async Task<ProblemDetailsResponse?> ParseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return null;

        try
        {
            var content = await response.Content.ReadAsStringAsync();
            return Parse(content, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "解析ProblemDetails响应失败");
            return null;
        }
    }

    /// <summary>
    /// 从JSON字符串解析ProblemDetails
    /// </summary>
    /// <param name="json">JSON内容</param>
    /// <param name="statusCode">HTTP状态码</param>
    /// <returns>解析结果</returns>
    public ProblemDetailsResponse? Parse(string? json, int? statusCode = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(json, JsonOptions);
            if (problemDetails != null && statusCode.HasValue && !problemDetails.Status.HasValue)
            {
                problemDetails.Status = statusCode;
            }

            return problemDetails;
        }
        catch (JsonException ex)
        {
            _logger?.LogDebug(ex, "JSON内容不是有效的ProblemDetails格式");
            return null;
        }
    }

    /// <summary>
    /// 尝试从HTTP响应解析ProblemDetails
    /// </summary>
    /// <param name="response">HTTP响应</param>
    /// <param name="problemDetails">解析结果</param>
    /// <returns>是否成功解析</returns>
    public async Task<(bool Success, ProblemDetailsResponse? ProblemDetails)> TryParseAsync(HttpResponseMessage response)
    {
        var result = await ParseAsync(response);
        return (result != null, result);
    }

    /// <summary>
    /// 从异常创建ProblemDetails（用于本地错误）
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="correlationId">CorrelationId</param>
    /// <returns>ProblemDetails响应</returns>
    public static ProblemDetailsResponse FromException(Exception exception, string? correlationId = null)
    {
        return new ProblemDetailsResponse
        {
            Status = 500,
            Title = "客户端错误",
            Detail = exception.Message,
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.Now
        };
    }

    /// <summary>
    /// 创建网络错误的ProblemDetails
    /// </summary>
    /// <param name="correlationId">CorrelationId</param>
    /// <returns>ProblemDetails响应</returns>
    public static ProblemDetailsResponse CreateNetworkError(string? correlationId = null)
    {
        return new ProblemDetailsResponse
        {
            Status = 0,
            Title = "网络错误",
            Detail = "无法连接到服务器，请检查网络连接后重试",
            ErrorCode = "NETWORK_ERROR",
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.Now
        };
    }

    /// <summary>
    /// 创建超时错误的ProblemDetails
    /// </summary>
    /// <param name="correlationId">CorrelationId</param>
    /// <returns>ProblemDetails响应</returns>
    public static ProblemDetailsResponse CreateTimeoutError(string? correlationId = null)
    {
        return new ProblemDetailsResponse
        {
            Status = 408,
            Title = "请求超时",
            Detail = "服务器响应超时，请稍后重试",
            ErrorCode = "TIMEOUT",
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.Now
        };
    }
}
