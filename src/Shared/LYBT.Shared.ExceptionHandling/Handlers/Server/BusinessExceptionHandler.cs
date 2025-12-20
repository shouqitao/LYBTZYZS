using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.ExceptionHandling.ProblemDetails;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.ExceptionHandling.Handlers;

/// <summary>
/// 业务异常处理器 - 处理AppException及其子类
/// consolidate-exception-handling: 从LYBT.WebAPI迁移
/// </summary>
public class BusinessExceptionHandler : IExceptionHandler
{
    private readonly ILogger<BusinessExceptionHandler> _logger;

    public BusinessExceptionHandler(ILogger<BusinessExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 只处理 AppException 及其子类
        if (exception is not AppException appException)
        {
            return false; // 交给下一个处理器
        }

        var correlationId = GetCorrelationId(httpContext);

        // 业务异常使用Warning级别日志
        _logger.LogWarning(
            exception,
            "业务异常 - 类型: {ExceptionType}, 错误码: {ErrorCode}, 消息: {Message}, CorrelationId: {CorrelationId}, 路径: {RequestPath}, 方法: {HttpMethod}, 用户: {UserId}",
            exception.GetType().Name,
            appException.ErrorCode ?? "N/A",
            exception.Message,
            correlationId,
            httpContext.Request.Path,
            httpContext.Request.Method,
            httpContext.User?.Identity?.Name ?? "匿名用户");

        var problemDetails = ProblemDetailsFactory.Create(
            appException,
            httpContext.Request.Path,
            correlationId,
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static string GetCorrelationId(HttpContext httpContext)
    {
        // 尝试从请求头获取CorrelationId
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId)
            && !string.IsNullOrEmpty(correlationId))
        {
            return correlationId!;
        }

        // 使用 TraceIdentifier 作为回退
        return httpContext.TraceIdentifier;
    }
}
