using LYBT.Shared.Models.Errors;

namespace LYBT.Shared.Models.Exceptions;

/// <summary>
/// 授权失败异常 - HTTP 401 Unauthorized
/// refactor-logging-system: 用于认证失败场景
/// </summary>
public class UnauthorizedException : AppException
{
    /// <summary>
    /// 认证方案（如 Bearer, Basic）
    /// </summary>
    public string? AuthenticationScheme { get; set; }

    /// <summary>
    /// 认证失败原因
    /// </summary>
    public string? FailureReason { get; set; }

    public override int GetHttpStatusCode() => 401;

    public override ErrorCategory Category => ErrorCategory.Authentication;

    public UnauthorizedException() : base("未授权访问")
    {
        TypedErrorCode = Errors.ErrorCode.Unauthorized;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        ShowDetailToUser = true;
        UserMessage = "请先登录后再访问此资源";
    }

    public UnauthorizedException(string message) : base(message)
    {
        TypedErrorCode = Errors.ErrorCode.Unauthorized;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        ShowDetailToUser = true;
        UserMessage = message;
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
        TypedErrorCode = Errors.ErrorCode.Unauthorized;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        ShowDetailToUser = true;
        UserMessage = message;
    }

    public UnauthorizedException(Errors.ErrorCode typedErrorCode, string message, string? userMessage = null)
        : base(message)
    {
        TypedErrorCode = typedErrorCode;
        ErrorCode = typedErrorCode.ToFormattedString();
        ShowDetailToUser = true;
        UserMessage = userMessage ?? message;
    }

    /// <summary>
    /// 创建密码错误异常
    /// </summary>
    public static UnauthorizedException InvalidPassword()
    {
        return new UnauthorizedException(Errors.ErrorCode.InvalidPassword, "用户名或密码错误", "用户名或密码错误，请重试");
    }

    /// <summary>
    /// 创建凭证过期异常
    /// </summary>
    public static UnauthorizedException CredentialsExpired()
    {
        return new UnauthorizedException(Errors.ErrorCode.CredentialsExpired, "登录凭证已过期", "您的登录已过期，请重新登录");
    }

    /// <summary>
    /// 创建刷新令牌无效异常
    /// </summary>
    public static UnauthorizedException InvalidRefreshToken()
    {
        return new UnauthorizedException(Errors.ErrorCode.InvalidRefreshToken, "刷新令牌无效或已过期", "登录状态异常，请重新登录");
    }

    /// <summary>
    /// 创建用户被禁用异常
    /// </summary>
    public static UnauthorizedException UserDisabled()
    {
        return new UnauthorizedException(Errors.ErrorCode.UserDisabled, "用户账号已被禁用", "您的账号已被禁用，请联系管理员")
        {
            FailureReason = "UserDisabled"
        };
    }

    /// <summary>
    /// 创建用户被锁定异常
    /// </summary>
    public static UnauthorizedException UserLocked(int? remainingMinutes = null)
    {
        var userMessage = remainingMinutes.HasValue
            ? $"账号已被锁定，请 {remainingMinutes} 分钟后重试"
            : "账号已被锁定，请稍后重试";

        return new UnauthorizedException(Errors.ErrorCode.UserLocked, "用户账号已被锁定", userMessage)
        {
            FailureReason = "UserLocked"
        };
    }

    /// <summary>
    /// 创建需要修改密码异常
    /// </summary>
    public static UnauthorizedException PasswordChangeRequired()
    {
        return new UnauthorizedException(Errors.ErrorCode.PasswordChangeRequired, "需要修改密码", "首次登录需要修改密码")
        {
            FailureReason = "PasswordChangeRequired"
        };
    }
}
