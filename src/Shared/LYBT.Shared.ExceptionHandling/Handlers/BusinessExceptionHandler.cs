using System.Linq;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.ExceptionHandling.Handlers;

/// <summary>
/// 业务异常处理器 - 处理AppException及其子类
/// A-31-C2: 从 LYBT.Infrastructure.ExceptionHandling 迁移
/// X-3: 异常路径统一写 ProblemDetails（RFC 7807）；ApiResponse 仅用于成功/已知业务失败响应
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
        // P2-6-3 并发转 409 统一：DbUpdateConcurrencyException 未经 AppException 包装时统一转 409（无 EF Core 直接引用，用全名匹配避免 Shared 层依赖 EF）
        var exTypeName = exception.GetType().FullName;
        if (exTypeName == "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException")
        {
            _logger.LogWarning(exception, "并发冲突 - CorrelationId: {CorrelationId}, 路径: {Path}", GetCorrelationId(httpContext), httpContext.Request.Path);
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                title: "并发冲突",
                detail: "数据已被其他用户修改，请刷新后重试",
                errorCode: ErrorCode.ConcurrencyConflict.ToFormattedString());
            return true;
        }

        // T1.4: 敏感数据解密失败转 422（AesGcmValueConverter 解密失败抛 CryptographicException）
        if (exTypeName == "System.Security.Cryptography.CryptographicException" || exception is System.Security.Cryptography.CryptographicException)
        {
            _logger.LogWarning(exception, "敏感数据解密失败 - CorrelationId: {CorrelationId}, 路径: {Path}", GetCorrelationId(httpContext), httpContext.Request.Path);
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status422UnprocessableEntity,
                title: "敏感数据错误",
                detail: ErrorMessages.Get(ErrorCode.SensitiveDecryptFailed),
                errorCode: ErrorCode.SensitiveDecryptFailed.ToFormattedString());
            return true;
        }

        // FluentValidation 校验异常转 400（与 ValidationBehavior 管道互补）
        if (exception is FluentValidation.ValidationException validationException)
        {
            var msg = validationException.Errors.FirstOrDefault()?.ErrorMessage ?? "参数校验失败";
            _logger.LogWarning(exception, "参数校验失败 - CorrelationId: {CorrelationId}", GetCorrelationId(httpContext));
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                title: "参数校验失败",
                detail: msg,
                errorCode: ErrorCode.ValidationFailed.ToFormattedString());
            return true;
        }

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

        var statusCode = appException.GetHttpStatusCode();
        await WriteProblemAsync(
            httpContext,
            statusCode,
            title: "Business Error",
            detail: appException.UserMessage ?? appException.Message,
            errorCode: appException.ErrorCode ?? appException.TypedErrorCode?.ToFormattedString());

        return true;
    }

    /// <summary>
    /// 写 RFC 7807 ProblemDetails。经 <see cref="Results.Problem"/> 走 IProblemDetailsService，
    /// 自动触发 ProblemDetailsConfiguration.CustomizeProblemDetails（注入 correlationId/timestamp/traceId/severity/type）。
    /// </summary>
    private static Task WriteProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string? errorCode)
    {
        var extensions = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(errorCode))
            extensions["errorCode"] = errorCode;

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            instance: httpContext.Request.Path,
            extensions: extensions.Count > 0 ? extensions : null)
            .ExecuteAsync(httpContext);
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
