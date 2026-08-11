using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 通用 API 异常（B2 US-ERR-007: 显式指定 HTTP 状态码与错误类别的兜底异常——
/// 供外部集成/网关场景使用，业务规则优先使用具体子类）
/// </summary>
public class ApiException : AppException
{
    /// <summary>显式 HTTP 状态码（默认 500）</summary>
    public int StatusCode { get; set; } = 500;

    /// <summary>错误类别（默认 General）</summary>
    public ErrorCategory CategoryOverride { get; set; } = ErrorCategory.General;

    public override int GetHttpStatusCode() => StatusCode;

    public override ErrorCategory Category => CategoryOverride;

    public ApiException(string message) : base(message)
    {
    }

    public ApiException(string message, int statusCode, string? errorCode = null, string? userMessage = null)
        : base(message, errorCode, userMessage)
    {
        StatusCode = statusCode;
        UserMessage = userMessage ?? message;
    }

    public ApiException(string message, int statusCode, ErrorCategory category, string? errorCode = null)
        : base(message, errorCode)
    {
        StatusCode = statusCode;
        CategoryOverride = category;
        UserMessage = message;
    }
}
