using System.Net;

namespace LYBT.Shared.Models.Exceptions;

/// <summary>
/// API调用异常 - UltraThink统一异常体系
/// </summary>
public class ApiException : AppException
{
    /// <summary>
    /// HTTP状态码
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

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

    public ApiException() : base("API调用失败") { }

    public ApiException(string message) : base(message) { }

    public ApiException(string message, Exception innerException) : base(message, innerException) { }

    public ApiException(HttpStatusCode statusCode, string? responseContent = null, Exception? innerException = null)
        : base($"API调用失败: {statusCode}", innerException!)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public ApiException(HttpStatusCode statusCode, string? requestUrl, string? httpMethod, string? responseContent = null, Exception? innerException = null)
        : base($"API调用失败: {httpMethod} {requestUrl} 返回 {statusCode}", innerException!)
    {
        StatusCode = statusCode;
        RequestUrl = requestUrl;
        HttpMethod = httpMethod;
        ResponseContent = responseContent;
    }

    /// <summary>
    /// 是否为客户端错误 (4xx)
    /// </summary>
    public bool IsClientError => (int)StatusCode >= 400 && (int)StatusCode < 500;

    /// <summary>
    /// 是否为服务器错误 (5xx)
    /// </summary>
    public bool IsServerError => (int)StatusCode >= 500;

    /// <summary>
    /// 是否为认证相关错误 (401, 403)
    /// </summary>
    public bool IsAuthenticationError => StatusCode == HttpStatusCode.Unauthorized || StatusCode == HttpStatusCode.Forbidden;

    /// <summary>
    /// 创建未授权异常
    /// </summary>
    public static ApiException Unauthorized(string? message = null)
        => new(HttpStatusCode.Unauthorized, null, null, message ?? "身份验证失败，请重新登录");

    /// <summary>
    /// 创建禁止访问异常
    /// </summary>
    public static ApiException Forbidden(string? message = null)
        => new(HttpStatusCode.Forbidden, null, null, message ?? "没有权限访问此资源");

    /// <summary>
    /// 创建服务不可用异常
    /// </summary>
    public static ApiException ServiceUnavailable(string? message = null)
        => new(HttpStatusCode.ServiceUnavailable, null, null, message ?? "服务暂时不可用，请稍后重试");

    /// <summary>
    /// 创建请求超时异常
    /// </summary>
    public static ApiException Timeout(string? message = null)
        => new(HttpStatusCode.RequestTimeout, null, null, message ?? "请求超时，请检查网络连接");
}
