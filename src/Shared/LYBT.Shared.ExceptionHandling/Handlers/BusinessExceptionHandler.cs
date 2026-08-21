using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.ExceptionHandling.Handlers;

/// <summary>
/// 业务异常处理器 - 处理AppException及其子类
/// A-31-C2: 从 LYBT.Infrastructure.ExceptionHandling 迁移
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
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(new ApiResponse
            {
                Success = false,
                Message = "数据已被其他用户修改，请刷新后重试",
                Errors = new { code = ErrorCode.ConcurrencyConflict.ToFormattedString(), correlationId = GetCorrelationId(httpContext), traceId = httpContext.TraceIdentifier },
                RequestId = GetCorrelationId(httpContext)
            }, cancellationToken);
            return true;
        }

        // T1.4: 敏感数据解密失败转 422（AesGcmValueConverter 解密失败抛 CryptographicException）
        if (exTypeName == "System.Security.Cryptography.CryptographicException" || exception is System.Security.Cryptography.CryptographicException)
        {
            _logger.LogWarning(exception, "敏感数据解密失败 - CorrelationId: {CorrelationId}, 路径: {Path}", GetCorrelationId(httpContext), httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(new ApiResponse
            {
                Success = false,
                Message = ErrorMessages.Get(ErrorCode.SensitiveDecryptFailed),
                Errors = new { code = ErrorCode.SensitiveDecryptFailed.ToFormattedString(), correlationId = GetCorrelationId(httpContext), traceId = httpContext.TraceIdentifier },
                RequestId = GetCorrelationId(httpContext)
            }, cancellationToken);
            return true;
        }

        // FluentValidation 校验异常转 400（与 ValidationBehavior 管道互补，无直接引用用全名匹配）
        if (exTypeName == "FluentValidation.ValidationException")
        {
            dynamic dynEx = exception;
            string msg = "参数校验失败";
            try { msg = dynEx.Errors[0].ErrorMessage ?? msg; } catch { }
            _logger.LogWarning(exception, "参数校验失败 - CorrelationId: {CorrelationId}", GetCorrelationId(httpContext));
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(new ApiResponse
            {
                Success = false,
                Message = msg,
                Errors = new { code = ErrorCode.ValidationFailed.ToFormattedString(), correlationId = GetCorrelationId(httpContext), traceId = httpContext.TraceIdentifier },
                RequestId = GetCorrelationId(httpContext)
            }, cancellationToken);
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
        var response = new ApiResponse
        {
            Success = false,
            Message = appException.UserMessage ?? appException.Message,
            Errors = new
            {
                code = appException.ErrorCode ?? appException.TypedErrorCode?.ToFormattedString(),
                correlationId,
                traceId = httpContext.TraceIdentifier
            },
            RequestId = correlationId
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

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
