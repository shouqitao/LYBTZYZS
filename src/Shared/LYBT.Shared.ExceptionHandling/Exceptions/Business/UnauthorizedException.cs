using LYBT.Shared.Models.Primitives.ErrorCodes;
using EC = LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 未授权异常（B2 US-ERR-007: 401——未登录/凭据无效）
/// </summary>
public class UnauthorizedException : AppException
{
    public override ErrorCategory Category => ErrorCategory.Authentication;

    public override int GetHttpStatusCode() => 401;

    public UnauthorizedException(string message) : base(message)
    {
        TypedErrorCode = EC.Unauthorized;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        UserMessage = message;
    }

    public UnauthorizedException(string message, string? errorCode = null) : base(message, errorCode)
    {
        UserMessage = message;
    }
}
