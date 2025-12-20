using System.Net;
using LYBT.Shared.Primitives.ErrorCodes;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// API调用异常 - 用于外部API调用失败场景
/// consolidate-exception-handling: 从LYBT.Shared.Models迁移
/// </summary>
public class ApiException : AppException
{
    /// <summary>
    /// HTTP状态码
    /// </summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.InternalServerError;

    /// <summary>
    /// 响应内容
    /// </summary>
    public string? ResponseContent { get; set; }

    /// <summary>
    /// 请求URL
    /// </summary>
    public string? RequestUrl { get; set; }

    /// <summary>
    /// HTTP方法
    /// </summary>
    public string? HttpMethod { get; set; }

    public override int GetHttpStatusCode() => (int)StatusCode;

    public override ErrorCategory Category => ErrorCategory.External;

    public ApiException() : base("API调用异常")
    {
        TypedErrorCode = EC.ServiceUnavailable;
    }

    public ApiException(string message) : base(message)
    {
        UserMessage = message;
    }

    public ApiException(string message, Exception innerException) : base(message, innerException)
    {
        UserMessage = message;
    }

    public ApiException(HttpStatusCode statusCode, string message, string? responseContent = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
        UserMessage = GetDefaultUserMessage(statusCode);
    }

    public ApiException(HttpStatusCode statusCode, string message, string requestUrl, string httpMethod, string? responseContent = null)
        : base(message)
    {
        StatusCode = statusCode;
        RequestUrl = requestUrl;
        HttpMethod = httpMethod;
        ResponseContent = responseContent;
        UserMessage = GetDefaultUserMessage(statusCode);
    }

    private static string GetDefaultUserMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "身份验证失败，请重新登录",
        HttpStatusCode.Forbidden => "没有权限执行此操作",
        HttpStatusCode.NotFound => "请求的资源不存在",
        HttpStatusCode.BadRequest => "请求参数无效",
        HttpStatusCode.Conflict => "数据冲突，请刷新后重试",
        HttpStatusCode.InternalServerError => "服务器内部错误",
        HttpStatusCode.ServiceUnavailable => "服务暂时不可用，请稍后重试",
        HttpStatusCode.RequestTimeout => "请求超时，请检查网络连接",
        _ => "API调用失败，请稍后重试"
    };

    // 静态工厂方法
    public static ApiException Unauthorized(string? message = null) =>
        new(HttpStatusCode.Unauthorized, message ?? "API认证失败");

    public static ApiException Forbidden(string? message = null) =>
        new(HttpStatusCode.Forbidden, message ?? "API访问被拒绝");

    public static ApiException ServiceUnavailable(string? message = null) =>
        new(HttpStatusCode.ServiceUnavailable, message ?? "API服务不可用");

    public static ApiException Timeout(string? message = null) =>
        new(HttpStatusCode.RequestTimeout, message ?? "API请求超时");
}
