using LYBT.Shared.Primitives.ErrorCodes;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 业务规则异常 - 用于违反业务规则的场景
/// consolidate-exception-handling: 从LYBT.Shared.Models迁移
/// </summary>
public class BusinessException : AppException
{
    /// <summary>
    /// 违反的业务规则描述
    /// </summary>
    public string? BusinessRule { get; set; }

    public override int GetHttpStatusCode() => 400;

    public override ErrorCategory Category => ErrorCategory.Business;

    public BusinessException() : base("业务规则违反")
    {
        TypedErrorCode = EC.Unknown;
    }

    public BusinessException(string message) : base(message)
    {
        UserMessage = message;
    }

    public BusinessException(string message, Exception innerException) : base(message, innerException)
    {
        UserMessage = message;
    }

    public BusinessException(string message, string businessRule)
        : base(message)
    {
        BusinessRule = businessRule;
        UserMessage = message;
    }

    public BusinessException(EC errorCode, string message, string? businessRule = null)
        : base(errorCode, message)
    {
        BusinessRule = businessRule;
    }
}
