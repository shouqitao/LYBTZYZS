using LYBT.Shared.Models.Constants;
using LYBT.Shared.Models.Errors;

namespace LYBT.Shared.Models.Exceptions;

/// <summary>
/// 应用程序基础异常类 - UltraThink统一异常体系
/// refactor-logging-system: 扩展支持类型化ErrorCode枚举
/// </summary>
public class AppException : Exception
{
    /// <summary>
    /// 错误代码（字符串格式，向后兼容）
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 类型化错误码（枚举格式）
    /// refactor-logging-system: 新增枚举类型错误码，便于前端处理和日志分析
    /// </summary>
    public Errors.ErrorCode? TypedErrorCode { get; set; }

    /// <summary>
    /// 用户友好的错误消息
    /// </summary>
    public string? UserMessage { get; set; }

    /// <summary>
    /// 是否向用户显示详细错误信息
    /// </summary>
    public bool ShowDetailToUser { get; set; }

    /// <summary>
    /// 获取HTTP状态码（基于TypedErrorCode，默认500）
    /// </summary>
    public virtual int GetHttpStatusCode() =>
        TypedErrorCode?.ToHttpStatusCode() ?? 500;

    /// <summary>
    /// 获取错误类别
    /// </summary>
    public virtual ErrorCategory Category =>
        TypedErrorCode?.ToCategory() ?? ErrorCategory.General;

    public AppException() : base(ErrorMessageKeys.APP_EXCEPTION)
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
    /// 使用类型化错误码构造异常
    /// refactor-logging-system: 新构造函数，推荐使用
    /// </summary>
    public AppException(Errors.ErrorCode typedErrorCode, string message, string? userMessage = null, bool showDetailToUser = false)
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
    public AppException(Errors.ErrorCode typedErrorCode, string message, Exception innerException, string? userMessage = null, bool showDetailToUser = false)
        : base(message, innerException)
    {
        TypedErrorCode = typedErrorCode;
        ErrorCode = typedErrorCode.ToFormattedString();
        UserMessage = userMessage ?? message;
        ShowDetailToUser = showDetailToUser;
    }
}
