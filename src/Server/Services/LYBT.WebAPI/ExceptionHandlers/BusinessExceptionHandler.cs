using LYBT.Infrastructure.Logging;
using LYBT.Shared.Models.Errors;
using LYBT.Shared.Models.Exceptions;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.ExceptionHandlers;

/// <summary>
/// 业务异常处理器 - 处理AppException及其子类
/// refactor-logging-system: IExceptionHandler链模式，专门处理业务异常
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

        var correlationId = httpContext.GetCorrelationId();
        var sanitizedMessage = SensitiveDataMasker.SanitizeText(exception.Message);

        // 业务异常使用Warning级别日志
        _logger.LogWarning(
            exception,
            "业务异常 - 类型: {ExceptionType}, 错误码: {ErrorCode}, 消息: {Message}, CorrelationId: {CorrelationId}, 路径: {RequestPath}, 方法: {HttpMethod}, 用户: {UserId}",
            exception.GetType().Name,
            appException.ErrorCode ?? "N/A",
            sanitizedMessage,
            correlationId,
            httpContext.Request.Path,
            httpContext.Request.Method,
            httpContext.User?.Identity?.Name ?? "匿名用户");

        var problemDetails = CreateProblemDetails(httpContext, appException, correlationId);

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        AppException exception,
        string correlationId)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path,
            Type = GetProblemTypeUri(exception.GetHttpStatusCode())
        };

        // 添加通用扩展属性
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(exception.ErrorCode))
        {
            problemDetails.Extensions["errorCode"] = exception.ErrorCode;
        }

        if (exception.TypedErrorCode.HasValue)
        {
            problemDetails.Extensions["errorCodeInt"] = (int)exception.TypedErrorCode.Value;
            problemDetails.Extensions["errorCategory"] = exception.Category.ToString();
        }

        // 根据具体异常类型设置响应
        switch (exception)
        {
            case ConflictException conflictException:
                problemDetails.Status = 409;
                problemDetails.Title = "资源冲突";
                problemDetails.Detail = conflictException.UserMessage ?? conflictException.Message;
                if (!string.IsNullOrEmpty(conflictException.ResourceType))
                {
                    problemDetails.Extensions["resourceType"] = conflictException.ResourceType;
                }
                if (!string.IsNullOrEmpty(conflictException.ResourceId))
                {
                    problemDetails.Extensions["resourceId"] = conflictException.ResourceId;
                }
                break;

            case UnauthorizedException unauthorizedException:
                problemDetails.Status = 401;
                problemDetails.Title = "未授权";
                problemDetails.Detail = unauthorizedException.UserMessage ?? unauthorizedException.Message;
                if (!string.IsNullOrEmpty(unauthorizedException.FailureReason))
                {
                    problemDetails.Extensions["failureReason"] = unauthorizedException.FailureReason;
                }
                break;

            case ValidationException validationException:
                problemDetails.Status = 400;
                problemDetails.Title = "验证失败";
                problemDetails.Detail = validationException.UserMessage ?? validationException.Message;
                if (validationException.HasErrors)
                {
                    problemDetails.Extensions["errors"] = validationException.Errors;
                }
                if (!string.IsNullOrEmpty(validationException.FieldName))
                {
                    problemDetails.Extensions["fieldName"] = validationException.FieldName;
                }
                break;

            case NotFoundException notFoundException:
                problemDetails.Status = 404;
                problemDetails.Title = "资源未找到";
                problemDetails.Detail = notFoundException.UserMessage ?? notFoundException.Message;
                if (!string.IsNullOrEmpty(notFoundException.ResourceType))
                {
                    problemDetails.Extensions["resourceType"] = notFoundException.ResourceType;
                }
                if (!string.IsNullOrEmpty(notFoundException.ResourceId))
                {
                    problemDetails.Extensions["resourceId"] = notFoundException.ResourceId;
                }
                break;

            case BusinessException businessException:
                problemDetails.Status = 400;
                problemDetails.Title = "业务错误";
                problemDetails.Detail = businessException.UserMessage ?? businessException.Message;
                if (!string.IsNullOrEmpty(businessException.BusinessRule))
                {
                    problemDetails.Extensions["businessRule"] = businessException.BusinessRule;
                }
                break;

            case ApiException apiException:
                problemDetails.Status = (int)apiException.StatusCode;
                problemDetails.Title = "API调用异常";
                problemDetails.Detail = apiException.ShowDetailToUser
                    ? apiException.UserMessage ?? apiException.Message
                    : "API调用失败";
                if (!string.IsNullOrEmpty(apiException.ResponseContent))
                {
                    problemDetails.Extensions["responseContent"] = apiException.ResponseContent;
                }
                break;

            default:
                // 通用 AppException
                problemDetails.Status = exception.GetHttpStatusCode();
                problemDetails.Title = "应用程序异常";
                problemDetails.Detail = exception.ShowDetailToUser
                    ? exception.UserMessage ?? exception.Message
                    : "应用程序处理异常";
                break;
        }

        return problemDetails;
    }

    private static string GetProblemTypeUri(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            _ => $"https://httpstatuses.com/{statusCode}"
        };
    }
}
