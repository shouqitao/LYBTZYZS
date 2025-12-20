using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Http;
using ProblemDetailsModel = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace LYBT.Shared.ExceptionHandling.ProblemDetails;

/// <summary>
/// ProblemDetails扩展方法
/// consolidate-exception-handling: 新增统一扩展方法
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// 从HttpContext获取CorrelationId
    /// </summary>
    public static string GetCorrelationId(this HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId)
            && !string.IsNullOrEmpty(correlationId))
        {
            return correlationId!;
        }
        return httpContext.TraceIdentifier;
    }

    /// <summary>
    /// 将AppException转换为ProblemDetails
    /// </summary>
    public static ProblemDetailsModel ToProblemDetails(
        this AppException exception,
        HttpContext httpContext)
    {
        return ProblemDetailsFactory.Create(
            exception,
            httpContext.Request.Path,
            httpContext.GetCorrelationId(),
            httpContext.TraceIdentifier);
    }

    /// <summary>
    /// 将ErrorCode转换为ProblemDetails
    /// </summary>
    public static ProblemDetailsModel ToProblemDetails(
        this ErrorCode errorCode,
        HttpContext httpContext,
        string? detail = null)
    {
        return ProblemDetailsFactory.Create(
            errorCode,
            httpContext.Request.Path,
            httpContext.GetCorrelationId(),
            httpContext.TraceIdentifier,
            detail);
    }

    /// <summary>
    /// 获取ProblemDetails中的ErrorCode
    /// </summary>
    public static string? GetErrorCode(this ProblemDetailsModel problemDetails)
    {
        if (problemDetails.Extensions.TryGetValue("errorCode", out var errorCode))
        {
            return errorCode?.ToString();
        }
        return null;
    }

    /// <summary>
    /// 获取ProblemDetails中的CorrelationId
    /// </summary>
    public static string? GetCorrelationId(this ProblemDetailsModel problemDetails)
    {
        if (problemDetails.Extensions.TryGetValue("correlationId", out var correlationId))
        {
            return correlationId?.ToString();
        }
        return null;
    }

    /// <summary>
    /// 获取ProblemDetails中的验证错误
    /// </summary>
    public static Dictionary<string, List<string>>? GetValidationErrors(this ProblemDetailsModel problemDetails)
    {
        if (problemDetails.Extensions.TryGetValue("errors", out var errors))
        {
            return errors as Dictionary<string, List<string>>;
        }
        return null;
    }
}
