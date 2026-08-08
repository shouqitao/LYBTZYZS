using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.ExceptionHandling;
using LYBT.Shared.Models.Primitives.ErrorCodes;
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
}


