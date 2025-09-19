using LYBT.Shared.Models.Constants;

namespace LYBT.Shared.Models.Exceptions;

/// <summary>
/// 业务逻辑异常 - UltraThink统一异常体系
/// </summary>
public class BusinessException : AppException
{

    /// <summary>
    /// 业务规则名称
    /// </summary>
    public string? BusinessRule { get; set; }

    public BusinessException() : base(ErrorMessageKeys.BUSINESS_FAILURE)
    {
    }

    public BusinessException(string message) : base(message)
    {
        ShowDetailToUser = true; // 业务异常通常需要显示给用户
    }

    public BusinessException(string message, Exception innerException) : base(message, innerException)
    {
        ShowDetailToUser = true;
    }

    public BusinessException(string message, string businessRule) : base(message)
    {
        BusinessRule = businessRule;
        ShowDetailToUser = true;
    }

    public BusinessException(string message, string errorCode, string businessRule) : base(message, errorCode)
    {
        BusinessRule = businessRule;
        ShowDetailToUser = true;
    }
}
