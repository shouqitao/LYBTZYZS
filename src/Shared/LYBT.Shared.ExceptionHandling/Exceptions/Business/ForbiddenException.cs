using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 权限不足异常 - 已认证但无访问权限（HTTP 403）。
/// 与 <see cref="UnauthorizedException"/>（未认证，HTTP 401）区分：
/// 401 表示「你是谁未知」，403 表示「知道你是谁，但你无权访问」。
/// </summary>
public class ForbiddenException : AppException
{
    /// <inheritdoc/>
    public override ErrorCategory Category => ErrorCategory.Authorization;

    /// <inheritdoc/>
    public override int GetHttpStatusCode() => 403;

    /// <summary>
    /// 使用消息构造异常。
    /// </summary>
    /// <param name="message">错误消息</param>
    public ForbiddenException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用消息与错误码构造异常。
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="errorCode">错误码（字符串格式）</param>
    public ForbiddenException(string message, string? errorCode = null)
        : base(message, errorCode)
    {
    }

    /// <summary>
    /// 使用类型化错误码构造异常。
    /// </summary>
    /// <param name="errorCode">类型化错误码</param>
    /// <param name="message">错误消息</param>
    public ForbiddenException(ErrorCode errorCode, string message)
        : base(errorCode, message)
    {
    }
}
