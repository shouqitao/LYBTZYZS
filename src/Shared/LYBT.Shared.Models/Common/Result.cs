using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Common;

/// <summary>
/// 统一返回值模式 - 封装成功/失败状态和错误信息
/// Phase 1 Task 1.5: 为Service层提供统一的返回值模式，替代直接抛出异常
/// Issue #1864: 新增ErrorCode支持结构化错误处理
/// </summary>
/// <typeparam name="T">返回数据类型</typeparam>
/// <remarks>
/// 设计原则：
/// - 成功场景：使用Success静态方法创建，返回数据存储在Data属性
/// - 失败场景：使用Failure静态方法创建，错误信息存储在ErrorMessage或Errors属性
/// - 避免直接new Result对象，统一使用静态工厂方法
///
/// 使用示例：
/// <code>
/// // 成功场景
/// var result = Result&lt;UserDto&gt;.Success(userDto);
/// if (result.IsSuccess)
/// {
///     var data = result.Data; // 获取返回数据
/// }
///
/// // 失败场景 - 单个错误
/// var result = Result&lt;UserDto&gt;.Failure("用户名已存在");
/// if (!result.IsSuccess)
/// {
///     var error = result.ErrorMessage; // 获取错误信息
/// }
///
/// // 失败场景 - 多个错误（FluentValidation）
/// var validationResult = await validator.ValidateAsync(input);
/// if (!validationResult.IsValid)
/// {
///     var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
///     return Result&lt;UserDto&gt;.Failure(errors);
/// }
/// </code>
/// </remarks>
public class Result<T>
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
    /// 错误信息（失败时有值，单个错误或多个错误的拼接）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误列表（失败时有值，用于返回多个验证错误）
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// 认证错误码（用于认证相关操作的结构化错误处理）
    /// Issue #1864: 支持客户端统一处理和国际化
    /// </summary>
    public AuthErrorCode? ErrorCode { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="data">返回的数据对象</param>
    /// <returns>成功的Result对象</returns>
    public static Result<T> Success(T data)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败结果（单个错误信息）
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    /// <returns>失败的Result对象</returns>
    public static Result<T> Failure(string errorMessage)
    {
        return new Result<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Errors = new List<string> { errorMessage }
        };
    }

    /// <summary>
    /// 创建失败结果（多个错误信息）
    /// </summary>
    /// <param name="errors">错误信息列表（如FluentValidation验证错误）</param>
    /// <returns>失败的Result对象</returns>
    public static Result<T> Failure(List<string> errors)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Errors = errors,
            ErrorMessage = string.Join("; ", errors)
        };
    }

    /// <summary>
    /// 创建失败结果（带错误码）
    /// Issue #1864: 支持结构化错误处理
    /// </summary>
    /// <param name="errorCode">认证错误码</param>
    /// <param name="errorMessage">可选的错误消息，默认使用错误码对应的消息</param>
    /// <returns>失败的Result对象</returns>
    public static Result<T> Failure(AuthErrorCode errorCode, string? errorMessage = null)
    {
        var message = errorMessage ?? GetAuthErrorMessage(errorCode);
        return new Result<T>
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = message,
            Errors = new List<string> { message }
        };
    }

    /// <summary>
    /// 获取认证错误码对应的默认消息
    /// </summary>
    private static string GetAuthErrorMessage(AuthErrorCode errorCode)
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

    /// <summary>
    /// 从异常创建失败结果
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <param name="operationName">操作名称（可选，用于日志）</param>
    /// <returns>失败的Result对象</returns>
    public static Result<T> FromException(Exception ex, string? operationName = null)
    {
        var message = string.IsNullOrEmpty(operationName)
            ? ex.Message
            : $"{operationName}失败: {ex.Message}";
        return new Result<T>
        {
            IsSuccess = false,
            ErrorMessage = message,
            Errors = new List<string> { message }
        };
    }
}

/// <summary>
/// 无数据返回的统一结果模式
/// Phase 1 Task 1.6: 用于Delete、Enable等无需返回数据的操作
/// </summary>
/// <remarks>
/// 使用场景：Delete、Enable、Disable等操作，只需要返回成功/失败状态
/// 使用示例：
/// <code>
/// // 成功
/// return Result.Success();
///
/// // 失败
/// return Result.Failure("删除失败");
///
/// // 多个错误
/// return Result.Failure(new List&lt;string&gt; { "错误1", "错误2" });
/// </code>
/// </remarks>
public class Result
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息（失败时有值）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误列表（失败时有值，用于返回多个验证错误）
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// 消息属性（兼容性）
    /// </summary>
    public string? Message => ErrorMessage;

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static Result Success()
    {
        return new Result
        {
            IsSuccess = true
        };
    }

    /// <summary>
    /// 创建失败结果（单个错误信息）
    /// </summary>
    public static Result Failure(string errorMessage)
    {
        return new Result
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Errors = new List<string> { errorMessage }
        };
    }

    /// <summary>
    /// 创建失败结果（多个错误信息）
    /// </summary>
    public static Result Failure(List<string> errors)
    {
        return new Result
        {
            IsSuccess = false,
            Errors = errors,
            ErrorMessage = string.Join("; ", errors)
        };
    }

    /// <summary>
    /// 从异常创建失败结果
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <param name="operationName">操作名称（可选，用于日志）</param>
    /// <returns>失败的Result对象</returns>
    public static Result FromException(Exception ex, string? operationName = null)
    {
        var message = string.IsNullOrEmpty(operationName)
            ? ex.Message
            : $"{operationName}失败: {ex.Message}";
        return new Result
        {
            IsSuccess = false,
            ErrorMessage = message,
            Errors = new List<string> { message }
        };
    }
}
