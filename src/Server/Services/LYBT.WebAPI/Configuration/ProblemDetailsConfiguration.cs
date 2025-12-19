using LYBT.WebAPI.Middleware;

namespace LYBT.WebAPI.Configuration;

/// <summary>
/// RFC 7807 Problem Details配置
/// refactor-logging-system: 统一错误响应格式配置
/// </summary>
public static class ProblemDetailsConfiguration
{
    /// <summary>
    /// 配置ProblemDetails服务
    /// </summary>
    public static IServiceCollection AddProblemDetailsConfiguration(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                // 注入CorrelationId用于端到端追踪
                var correlationId = CorrelationIdMiddlewareExtensions.GetCorrelationId(context.HttpContext);
                context.ProblemDetails.Extensions["correlationId"] = correlationId;

                // 添加时间戳
                context.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

                // 添加TraceId
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                // 设置Instance为请求路径
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;

                // 根据状态码设置RFC 7807 type URI
                var statusCode = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                context.ProblemDetails.Type ??= GetProblemTypeUri(statusCode);
            };
        });

        return services;
    }

    /// <summary>
    /// 配置StatusCodePages中间件
    /// </summary>
    public static IApplicationBuilder UseStatusCodePagesWithProblemDetails(this IApplicationBuilder app)
    {
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var httpContext = statusCodeContext.HttpContext;
            var statusCode = httpContext.Response.StatusCode;

            // 只处理4xx和5xx状态码
            if (statusCode < 400) return;

            // 如果响应已经开始写入，跳过
            if (httpContext.Response.HasStarted) return;

            var correlationId = CorrelationIdMiddlewareExtensions.GetCorrelationId(httpContext);

            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = statusCode,
                Title = GetStatusCodeTitle(statusCode),
                Detail = GetStatusCodeDetail(statusCode),
                Instance = httpContext.Request.Path,
                Type = GetProblemTypeUri(statusCode)
            };

            problemDetails.Extensions["correlationId"] = correlationId;
            problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(problemDetails);
        });

        return app;
    }

    /// <summary>
    /// 获取RFC 7807标准问题类型URI
    /// </summary>
    private static string GetProblemTypeUri(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            405 => "https://tools.ietf.org/html/rfc7231#section-6.5.5",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            429 => "https://tools.ietf.org/html/rfc6585#section-4",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            502 => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
            503 => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            _ => $"https://httpstatuses.com/{statusCode}"
        };
    }

    /// <summary>
    /// 获取状态码标题
    /// </summary>
    private static string GetStatusCodeTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "请求错误",
            401 => "未授权",
            403 => "禁止访问",
            404 => "资源未找到",
            405 => "方法不允许",
            409 => "资源冲突",
            422 => "无法处理的实体",
            429 => "请求过于频繁",
            500 => "服务器内部错误",
            502 => "网关错误",
            503 => "服务不可用",
            _ => "请求处理失败"
        };
    }

    /// <summary>
    /// 获取状态码详细描述
    /// </summary>
    private static string GetStatusCodeDetail(int statusCode)
    {
        return statusCode switch
        {
            400 => "请求格式不正确，请检查请求参数",
            401 => "请先登录后再访问此资源",
            403 => "您没有权限访问此资源",
            404 => "请求的资源不存在",
            405 => "不支持当前请求方法",
            409 => "请求与当前资源状态冲突",
            422 => "请求数据验证失败",
            429 => "请求过于频繁，请稍后再试",
            500 => "服务器处理请求时发生错误，请稍后重试",
            502 => "网关错误，请稍后重试",
            503 => "服务暂时不可用，请稍后重试",
            _ => "处理请求时发生错误"
        };
    }
}
