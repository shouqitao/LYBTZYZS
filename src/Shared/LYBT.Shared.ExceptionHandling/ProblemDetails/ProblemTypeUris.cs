namespace LYBT.Shared.ExceptionHandling.ProblemDetails;

/// <summary>
/// RFC 7807 标准问题类型 URI 常量
/// DRY: 统一 ProblemDetailsConfiguration 和 ProblemDetailsFactory 的 URI 映射
/// </summary>
public static class ProblemTypeUris
{
    // RFC 7231 - HTTP/1.1 Semantics and Content
    public const string BadRequest = "https://tools.ietf.org/html/rfc7231#section-6.5.1";         // 400
    public const string Forbidden = "https://tools.ietf.org/html/rfc7231#section-6.5.3";          // 403
    public const string NotFound = "https://tools.ietf.org/html/rfc7231#section-6.5.4";           // 404
    public const string MethodNotAllowed = "https://tools.ietf.org/html/rfc7231#section-6.5.5";   // 405
    public const string Conflict = "https://tools.ietf.org/html/rfc7231#section-6.5.8";           // 409
    public const string InternalServerError = "https://tools.ietf.org/html/rfc7231#section-6.6.1"; // 500
    public const string BadGateway = "https://tools.ietf.org/html/rfc7231#section-6.6.3";          // 502
    public const string ServiceUnavailable = "https://tools.ietf.org/html/rfc7231#section-6.6.4";  // 503
    public const string GatewayTimeout = "https://tools.ietf.org/html/rfc7231#section-6.6.5";      // 504

    // RFC 7235 - HTTP/1.1 Authentication
    public const string Unauthorized = "https://tools.ietf.org/html/rfc7235#section-3.1";         // 401

    // RFC 4918 - WebDAV
    public const string UnprocessableEntity = "https://tools.ietf.org/html/rfc4918#section-11.2";  // 422

    // RFC 6585 - Additional HTTP Status Codes
    public const string TooManyRequests = "https://tools.ietf.org/html/rfc6585#section-4";         // 429

    /// <summary>
    /// 根据 HTTP 状态码获取 RFC 7807 问题类型 URI
    /// </summary>
    public static string GetByStatusCode(int statusCode)
    {
        return statusCode switch
        {
            400 => BadRequest,
            401 => Unauthorized,
            403 => Forbidden,
            404 => NotFound,
            405 => MethodNotAllowed,
            409 => Conflict,
            422 => UnprocessableEntity,
            429 => TooManyRequests,
            499 => $"https://httpstatuses.com/499",
            500 => InternalServerError,
            502 => BadGateway,
            503 => ServiceUnavailable,
            504 => GatewayTimeout,
            _ => $"https://httpstatuses.com/{statusCode}"
        };
    }
}
