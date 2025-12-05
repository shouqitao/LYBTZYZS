using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Auth;

/// <summary>
/// 认证操作结果 - 带结构化错误码
/// Issue #1864: 统一认证错误处理，支持客户端统一处理和国际化
/// </summary>
/// <typeparam name="T">返回数据类型</typeparam>
/// <remarks>
/// 设计原则：
/// - 继承Result模式，增加AuthErrorCode支持
/// - 错误码用于客户端统一处理和国际化
/// - 保持与现有Result兼容，可无缝替换
/// </remarks>
public class AuthResult<T>
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 返回数据（成功时有值）
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 错误码（失败时有值）
    /// </summary>
    public AuthErrorCode ErrorCode { get; set; } = AuthErrorCode.None;

    /// <summary>
    /// 错误信息（失败时有值）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static AuthResult<T> Success(T data)
    {
        return new AuthResult<T>
        {
            IsSuccess = true,
            Data = data,
            ErrorCode = AuthErrorCode.None
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static AuthResult<T> Failure(AuthErrorCode errorCode, string? message = null)
    {
        return new AuthResult<T>
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = message ?? GetDefaultMessage(errorCode)
        };
    }

    /// <summary>
    /// 创建失败结果（仅消息，向后兼容）
    /// </summary>
    public static AuthResult<T> Failure(string message)
    {
        return new AuthResult<T>
        {
            IsSuccess = false,
            ErrorCode = AuthErrorCode.InternalError,
            ErrorMessage = message
        };
    }

    /// <summary>
    /// 获取错误码的默认消息
    /// </summary>
    private static string GetDefaultMessage(AuthErrorCode errorCode)
    {
        return errorCode switch
        {
            AuthErrorCode.None => "操作成功",
            AuthErrorCode.InvalidCredentials => "用户名或密码错误",
            AuthErrorCode.UserNotFound => "用户不存在",
            AuthErrorCode.UserDisabled => "用户账号已被禁用",
            AuthErrorCode.PasswordExpired => "密码已过期，请修改密码",
            AuthErrorCode.WeakPassword => "密码不符合安全要求",
            AuthErrorCode.TokenExpired => "登录已过期，请重新登录",
            AuthErrorCode.TokenInvalid => "登录凭据无效",
            AuthErrorCode.TokenRevoked => "登录已失效，请重新登录",
            AuthErrorCode.RefreshTokenExpired => "会话已过期，请重新登录",
            AuthErrorCode.RefreshTokenInvalid => "刷新凭据无效",
            AuthErrorCode.SessionNotFound => "会话不存在",
            AuthErrorCode.SessionExpired => "会话已到期，请重新登录",
            AuthErrorCode.ConcurrentSessionLimit => "登录设备数超过限制",
            AuthErrorCode.InternalError => "服务器内部错误",
            AuthErrorCode.ServiceUnavailable => "服务暂时不可用",
            _ => "未知错误"
        };
    }

    // ========== 便捷工厂方法 ==========

    /// <summary>
    /// 凭据无效
    /// </summary>
    public static AuthResult<T> InvalidCredentials(string? message = null)
        => Failure(AuthErrorCode.InvalidCredentials, message);

    /// <summary>
    /// 用户不存在
    /// </summary>
    public static AuthResult<T> UserNotFound(string? message = null)
        => Failure(AuthErrorCode.UserNotFound, message);

    /// <summary>
    /// 用户已禁用
    /// </summary>
    public static AuthResult<T> UserDisabled(string? message = null)
        => Failure(AuthErrorCode.UserDisabled, message);

    /// <summary>
    /// Token已过期
    /// </summary>
    public static AuthResult<T> TokenExpired(string? message = null)
        => Failure(AuthErrorCode.TokenExpired, message);

    /// <summary>
    /// Token已撤销
    /// </summary>
    public static AuthResult<T> TokenRevoked(string? message = null)
        => Failure(AuthErrorCode.TokenRevoked, message);

    /// <summary>
    /// RefreshToken已过期
    /// </summary>
    public static AuthResult<T> RefreshTokenExpired(string? message = null)
        => Failure(AuthErrorCode.RefreshTokenExpired, message);

    /// <summary>
    /// RefreshToken无效
    /// </summary>
    public static AuthResult<T> RefreshTokenInvalid(string? message = null)
        => Failure(AuthErrorCode.RefreshTokenInvalid, message);

    /// <summary>
    /// 会话已过期
    /// </summary>
    public static AuthResult<T> SessionExpired(string? message = null)
        => Failure(AuthErrorCode.SessionExpired, message);
}

/// <summary>
/// 无数据返回的认证操作结果
/// </summary>
public class AuthResult
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误码（失败时有值）
    /// </summary>
    public AuthErrorCode ErrorCode { get; set; } = AuthErrorCode.None;

    /// <summary>
    /// 错误信息（失败时有值）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static AuthResult Success()
    {
        return new AuthResult
        {
            IsSuccess = true,
            ErrorCode = AuthErrorCode.None
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static AuthResult Failure(AuthErrorCode errorCode, string? message = null)
    {
        return new AuthResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = message ?? GetDefaultMessage(errorCode)
        };
    }

    /// <summary>
    /// 获取错误码的默认消息
    /// </summary>
    private static string GetDefaultMessage(AuthErrorCode errorCode)
    {
        return errorCode switch
        {
            AuthErrorCode.None => "操作成功",
            AuthErrorCode.InvalidCredentials => "用户名或密码错误",
            AuthErrorCode.UserNotFound => "用户不存在",
            AuthErrorCode.UserDisabled => "用户账号已被禁用",
            AuthErrorCode.TokenRevoked => "登录已失效，请重新登录",
            AuthErrorCode.InternalError => "服务器内部错误",
            _ => "未知错误"
        };
    }
}
