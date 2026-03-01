using LYBT.Shared.Primitives.ErrorCodes;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.Models.Contracts.Auth;

/// <summary>
/// 认证操作结果 - 带结构化错误码
/// Sprint3-Batch3: 统一使用 GenericErrorCode
/// </summary>
/// <typeparam name="T">返回数据类型</typeparam>
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
    /// 统一错误码（失败时有值）
    /// </summary>
    public GenericErrorCode? ModuleErrorCode { get; set; }

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
            Data = data
        };
    }

    /// <summary>
    /// 创建失败结果（带错误码）
    /// </summary>
    public static AuthResult<T> Failure(GenericErrorCode errorCode, string? message = null)
    {
        return new AuthResult<T>
        {
            IsSuccess = false,
            ModuleErrorCode = errorCode,
            ErrorMessage = message ?? ErrorMessages.GetUserMessage(errorCode)
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
            ModuleErrorCode = GenericErrorCode.InternalError,
            ErrorMessage = message
        };
    }

    // ========== 便捷工厂方法 ==========

    /// <summary>
    /// 凭据无效
    /// </summary>
    public static AuthResult<T> InvalidCredentials(string? message = null)
        => Failure(GenericErrorCode.AuthInvalidCredentials, message);

    /// <summary>
    /// 用户不存在
    /// </summary>
    public static AuthResult<T> UserNotFound(string? message = null)
        => Failure(GenericErrorCode.UserNotFound, message);

    /// <summary>
    /// 用户已禁用
    /// </summary>
    public static AuthResult<T> UserDisabled(string? message = null)
        => Failure(GenericErrorCode.UserDisabled, message);

    /// <summary>
    /// Token已撤销
    /// </summary>
    public static AuthResult<T> TokenRevoked(string? message = null)
        => Failure(GenericErrorCode.AuthTokenRevoked, message);

    /// <summary>
    /// RefreshToken已过期
    /// </summary>
    public static AuthResult<T> RefreshTokenExpired(string? message = null)
        => Failure(GenericErrorCode.AuthRefreshTokenExpired, message);

    /// <summary>
    /// RefreshToken无效
    /// </summary>
    public static AuthResult<T> RefreshTokenInvalid(string? message = null)
        => Failure(GenericErrorCode.AuthRefreshTokenInvalid, message);

    /// <summary>
    /// 会话已过期
    /// </summary>
    public static AuthResult<T> SessionExpired(string? message = null)
        => Failure(GenericErrorCode.SessionExpired, message);
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
    /// 统一错误码（失败时有值）
    /// </summary>
    public GenericErrorCode? ModuleErrorCode { get; set; }

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
            IsSuccess = true
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static AuthResult Failure(GenericErrorCode errorCode, string? message = null)
    {
        return new AuthResult
        {
            IsSuccess = false,
            ModuleErrorCode = errorCode,
            ErrorMessage = message ?? ErrorMessages.GetUserMessage(errorCode)
        };
    }
}
