using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Primitives.ErrorCodes;
using ProblemDetailsModel = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace LYBT.Shared.ExceptionHandling.ProblemDetails;

/// <summary>
/// ProblemDetails工厂类 - 创建RFC 7807标准的错误响应
/// consolidate-exception-handling: 新增统一ProblemDetails创建
/// </summary>
public static class ProblemDetailsFactory
{
    /// <summary>
    /// 从AppException创建ProblemDetails
    /// </summary>
    public static ProblemDetailsModel Create(
        AppException exception,
        string instance,
        string correlationId,
        string traceId)
    {
        var statusCode = exception.GetHttpStatusCode();
        var errorCode = exception.TypedErrorCode ?? ErrorCode.Unknown;

        var problemDetails = new ProblemDetailsModel
        {
            Status = statusCode,
            Title = GetTitle(errorCode),
            Detail = exception.UserMessage ?? exception.Message,
            Instance = instance,
            Type = GetProblemTypeUri(statusCode)
        };

        // 添加扩展属性
        problemDetails.Extensions["errorCode"] = errorCode.ToFormattedString();
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        // ValidationException特殊处理
        if (exception is ValidationException validationException && validationException.Errors.Count > 0)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        // ConflictException特殊处理
        if (exception is ConflictException conflictException)
        {
            if (!string.IsNullOrEmpty(conflictException.EntityType))
            {
                problemDetails.Extensions["entityType"] = conflictException.EntityType;
            }
            if (!string.IsNullOrEmpty(conflictException.EntityId))
            {
                problemDetails.Extensions["entityId"] = conflictException.EntityId;
            }
        }

        return problemDetails;
    }

    /// <summary>
    /// 从ErrorCode创建ProblemDetails
    /// </summary>
    public static ProblemDetailsModel Create(
        ErrorCode errorCode,
        string instance,
        string correlationId,
        string traceId,
        string? detail = null)
    {
        var statusCode = errorCode.ToHttpStatusCode();

        var problemDetails = new ProblemDetailsModel
        {
            Status = statusCode,
            Title = GetTitle(errorCode),
            Detail = detail ?? ErrorMessages.Get(errorCode),
            Instance = instance,
            Type = GetProblemTypeUri(statusCode)
        };

        problemDetails.Extensions["errorCode"] = errorCode.ToFormattedString();
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        return problemDetails;
    }

    /// <summary>
    /// 创建验证错误的ProblemDetails
    /// </summary>
    public static ProblemDetailsModel CreateValidationProblem(
        Dictionary<string, List<string>> errors,
        string instance,
        string correlationId,
        string traceId)
    {
        var problemDetails = new ProblemDetailsModel
        {
            Status = 400,
            Title = "验证失败",
            Detail = "请求数据验证失败，请检查输入",
            Instance = instance,
            Type = GetProblemTypeUri(400)
        };

        problemDetails.Extensions["errorCode"] = ErrorCode.ValidationFailed.ToFormattedString();
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        problemDetails.Extensions["errors"] = errors;

        return problemDetails;
    }

    /// <summary>
    /// 获取错误码对应的标题
    /// </summary>
    private static string GetTitle(ErrorCode errorCode)
    {
        var category = errorCode.ToCategory();
        return category switch
        {
            ErrorCategory.Validation => "验证失败",
            ErrorCategory.Authentication => "身份认证失败",
            ErrorCategory.Authorization => "权限不足",
            ErrorCategory.Resource => "资源未找到",
            ErrorCategory.Business => "业务规则错误",
            ErrorCategory.Concurrency => "并发冲突",
            ErrorCategory.System => "系统错误",
            ErrorCategory.External => "外部服务错误",
            ErrorCategory.Configuration => "配置错误",
            _ => "操作失败"
        };
    }

    /// <summary>
    /// 获取HTTP状态码对应的RFC问题类型URI
    /// </summary>
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
            499 => "https://httpstatuses.com/499",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            502 => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
            503 => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            504 => "https://tools.ietf.org/html/rfc7231#section-6.6.5",
            _ => $"https://httpstatuses.com/{statusCode}"
        };
    }
}
