using LYBT.Shared.Models.Constants;

namespace LYBT.Shared.Models.Exceptions;

/// <summary>
/// 应用程序基础异常类 - UltraThink统一异常体系
/// </summary>
public class AppException : Exception
{

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 用户友好的错误消息
    /// </summary>
    public string? UserMessage { get; set; }

    /// <summary>
    /// 是否向用户显示详细错误信息
    /// </summary>
    public bool ShowDetailToUser { get; set; }

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
}
