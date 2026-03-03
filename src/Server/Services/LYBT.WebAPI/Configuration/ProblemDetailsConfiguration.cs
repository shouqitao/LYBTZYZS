using LYBT.Infrastructure.Constants;
using LYBT.Shared.ExceptionHandling.ProblemDetails;
using LYBT.Shared.Primitives.ErrorCodes;
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
                context.ProblemDetails.Extensions[HttpHeaderConstants.TraceIdKey] = context.HttpContext.TraceIdentifier;

                // T5-P3-03: 非AppException路径添加默认severity（使用ErrorSeverity枚举统一）
                if (!context.ProblemDetails.Extensions.ContainsKey("severity"))
                {
                    var statusCode2 = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                    context.ProblemDetails.Extensions["severity"] = MapStatusCodeToSeverity(statusCode2);
                }

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
            problemDetails.Extensions[HttpHeaderConstants.TraceIdKey] = httpContext.TraceIdentifier;
            problemDetails.Extensions["severity"] = MapStatusCodeToSeverity(statusCode);

            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(problemDetails);
        });

        return app;
    }

    /// <summary>
    /// 将HTTP状态码映射到ErrorSeverity枚举的小写字符串
    /// DRY: 统一使用ErrorSeverity枚举，与AppException路径一致
    /// </summary>
    private static string MapStatusCodeToSeverity(int statusCode) => (statusCode switch
    {
        >= 500 => ErrorSeverity.Critical,
        >= 400 => ErrorSeverity.Warning,
        _ => ErrorSeverity.Info
    }).ToString().ToLowerInvariant();

    /// <summary>
    /// 获取RFC 7807标准问题类型URI
    /// DRY: 委托到共享常量类 ProblemTypeUris
    /// </summary>
    private static string GetProblemTypeUri(int statusCode) => ProblemTypeUris.GetByStatusCode(statusCode);

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
