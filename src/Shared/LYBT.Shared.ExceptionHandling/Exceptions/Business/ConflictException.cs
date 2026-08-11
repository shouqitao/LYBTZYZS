using LYBT.Shared.Models.Primitives.ErrorCodes;
using EC = LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 资源冲突异常（B2 US-ERR-007: 409——并发/唯一性冲突，如版本冲突、重复数据）
/// </summary>
public class ConflictException : AppException
{
    public override ErrorCategory Category => ErrorCategory.Business;

    public override int GetHttpStatusCode() => 409;

    public ConflictException(string message) : base(message)
    {
        TypedErrorCode = EC.ConcurrencyConflict;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        UserMessage = message;
    }

    public ConflictException(string message, string? errorCode = null) : base(message, errorCode)
    {
        UserMessage = message;
    }

    public ConflictException(EC errorCode, string message) : base(errorCode, message)
    {
        UserMessage = message;
    }
}
