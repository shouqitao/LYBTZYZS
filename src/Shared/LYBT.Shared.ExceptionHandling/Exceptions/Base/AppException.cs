using LYBT.Shared.Models.Primitives.ErrorCodes;
using EC = LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 应用程序基础异常类 - 统一异常体系
/// consolidate-exception-handling: 从LYBT.Shared.Models迁移并优化
/// </summary>
public class AppException : Exception
{
    /// <summary>
    /// 错误代码（字符串格式，向后兼容）
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 类型化错误码（枚举格式）
    /// </summary>
    public EC? TypedErrorCode { get; set; }

    /// <summary>
    /// 用户友好的错误消息
    /// </summary>
    public string? UserMessage { get; set; }

    /// <summary>
    /// 是否向用户显示详细错误信息
    /// </summary>
    public bool ShowDetailToUser { get; set; }

    /// <summary>
    /// 获取HTTP状态码。
    /// 优先按 <see cref="TypedErrorCode"/> 映射；未带类型化错误码时按 <see cref="Category"/> 兜底，
    /// 使「只传 message」构造的子类（如 <c>NotFoundException</c>）也能映射到正确状态码。
    /// </summary>
    public virtual int GetHttpStatusCode()
    {
        if (TypedErrorCode is { } typed)
            return typed.ToHttpStatusCode();

        return CategoryToHttpStatusCode(Category);
    }

    /// <summary>
    /// 错误类别 → HTTP 状态码的单点映射（<see cref="GetHttpStatusCode"/> 的兜底分支）。
    /// </summary>
    /// <param name="category">错误类别</param>
    /// <returns>对应的 HTTP 状态码</returns>
    public static int CategoryToHttpStatusCode(ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => 400,
        ErrorCategory.Authentication => 401,
        ErrorCategory.Authorization => 403,
        ErrorCategory.Resource => 404,
        ErrorCategory.Concurrency => 409,
        ErrorCategory.Business => 422,
        ErrorCategory.Network => 503,
        ErrorCategory.External => 502,
        ErrorCategory.System => 500,
        ErrorCategory.Configuration => 500,
        _ => 500,
    };

    /// <summary>
    /// 获取错误类别
    /// </summary>
    public virtual ErrorCategory Category =>
        TypedErrorCode?.ToCategory() ?? ErrorCategory.General;

    public AppException() : base("应用程序异常")
    {
    }

    public AppException(string message) : base(message)
    {
    }

    public AppException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public AppException(string message, string? errorCode = null, string? userMessage = null, bool showDetailToUser = false)
        : base(message)
    {
        ErrorCode = errorCode;
        UserMessage = userMessage ?? message;
        ShowDetailToUser = showDetailToUser;
    }

    public AppException(string message, Exception innerException, string? errorCode = null, string? userMessage = null, bool showDetailToUser = false)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        UserMessage = userMessage ?? message;
        ShowDetailToUser = showDetailToUser;
    }

    /// <summary>
    /// 使用类型化错误码构造异常（推荐）
    /// </summary>
    public AppException(EC typedErrorCode, string message, string? userMessage = null, bool showDetailToUser = false)
        : base(message)
    {
        TypedErrorCode = typedErrorCode;
        ErrorCode = typedErrorCode.ToFormattedString();
        UserMessage = userMessage ?? message;
        ShowDetailToUser = showDetailToUser;
    }

    /// <summary>
    /// 使用类型化错误码构造异常（包含内部异常）
    /// </summary>
    public AppException(EC typedErrorCode, string message, Exception innerException, string? userMessage = null, bool showDetailToUser = false)
        : base(message, innerException)
    {
        TypedErrorCode = typedErrorCode;
        ErrorCode = typedErrorCode.ToFormattedString();
        UserMessage = userMessage ?? message;
        ShowDetailToUser = showDetailToUser;
    }
}
