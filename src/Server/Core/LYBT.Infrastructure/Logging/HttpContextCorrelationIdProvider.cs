using LYBT.Shared.Logging.Abstractions;
using Microsoft.AspNetCore.Http;

namespace LYBT.Infrastructure.Logging;

/// <summary>
/// 基于HttpContext的CorrelationId提供者
/// 用于Server端从HttpContext获取和设置CorrelationId
/// </summary>
public class HttpContextCorrelationIdProvider : ICorrelationIdProvider
{
    /// <summary>
    /// HttpContext.Items中存储CorrelationId的键名
    /// </summary>
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public HttpContextCorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// 从HttpContext获取CorrelationId
    /// </summary>
    /// <returns>CorrelationId，如果不存在返回null</returns>
    public string? GetCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue(CorrelationIdItemKey, out var correlationIdObj)
            && correlationIdObj is string correlationId
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return null;
    }

    /// <summary>
    /// 设置CorrelationId到HttpContext
    /// </summary>
    /// <param name="correlationId">要设置的CorrelationId</param>
    public void SetCorrelationId(string correlationId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items[CorrelationIdItemKey] = correlationId;
        }
    }
}
