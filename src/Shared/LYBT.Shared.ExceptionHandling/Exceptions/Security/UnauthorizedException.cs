using LYBT.Shared.Primitives.ErrorCodes;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 未授权异常 - 用于身份认证失败场景
/// consolidate-exception-handling: 从LYBT.Shared.Models迁移
/// </summary>
public class UnauthorizedException : AppException
{
    /// <summary>
    /// 认证方案
    /// </summary>
    public string? AuthenticationScheme { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    public string? FailureReason { get; set; }

    public override int GetHttpStatusCode() => 401;

    public override ErrorCategory Category => ErrorCategory.Authentication;

    public UnauthorizedException() : base("未授权访问")
    {
        TypedErrorCode = EC.Unauthorized;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
    }

    public UnauthorizedException(string message) : base(message)
    {
        TypedErrorCode = EC.Unauthorized;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        UserMessage = message;
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
        TypedErrorCode = EC.Unauthorized;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        UserMessage = message;
    }

    public UnauthorizedException(EC errorCode, string message, string? failureReason = null)
        : base(errorCode, message)
    {
        FailureReason = failureReason;
    }

    // 静态工厂方法
    public static UnauthorizedException InvalidPassword() =>
        new(EC.InvalidPassword, "用户名或密码错误", "密码验证失败");

    public static UnauthorizedException CredentialsExpired() =>
        new(EC.CredentialsExpired, "登录已过期，请重新登录", "凭据已过期");

    public static UnauthorizedException InvalidRefreshToken() =>
        new(EC.InvalidRefreshToken, "登录状态异常，请重新登录", "刷新令牌无效");

    public static UnauthorizedException UserDisabled() =>
        new(EC.UserDisabled, "用户账号已被禁用，请联系管理员", "用户已禁用");

    public static UnauthorizedException UserLocked() =>
        new(EC.UserLocked, "账号已被锁定，请稍后重试", "用户已锁定");

    public static UnauthorizedException PasswordChangeRequired() =>
        new(EC.PasswordChangeRequired, "首次登录需要修改密码", "需要修改密码");

    /// <summary>
    /// 设备指纹不匹配
    /// refactor-auth-role-system Phase 1.3
    /// </summary>
    public static UnauthorizedException DeviceMismatch() =>
        new(EC.DeviceMismatch, "登录设备异常，请重新登录", "设备指纹不匹配");

    /// <summary>
    /// 会话已过期
    /// refactor-auth-role-system Phase 1.3
    /// </summary>
    public static UnauthorizedException SessionExpired() =>
        new(EC.SessionExpired, "会话已过期，请重新登录", "会话已过期");
}
