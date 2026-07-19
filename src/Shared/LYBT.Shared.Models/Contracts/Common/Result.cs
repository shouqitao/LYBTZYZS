using LYBT.Shared.Primitives.ErrorCodes;

namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 操作结果封装。用于命令和查询的统一返回类型。
/// 统一 Result 模式：合并 Shared.Models.Result 和 ServiceResult 的有用功能。
/// </summary>
public class Result<T>
{
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; }

    /// <summary>返回数据</summary>
    public T? Value { get; }

    /// <summary>数据别名（兼容 Shared.Models.Result）</summary>
    public T? Data => Value;

    /// <summary>错误信息</summary>
    public string? Error { get; }

    /// <summary>错误信息别名（兼容 Shared.Models.Result）</summary>
    public string? ErrorMessage => Error;

    /// <summary>消息别名（兼容 ServiceResult）</summary>
    public string? Message => Error;

    /// <summary>错误列表（支持多个验证错误）</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>错误码</summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>模块错误码（用于 HTTP 状态码映射）</summary>
    public ErrorCode? ModuleErrorCode { get; }

    /// <summary>关联异常（可选，用于调试）</summary>
    public Exception? Exception { get; }

    private Result(bool isSuccess, T? value, string? error, IReadOnlyList<string>? errors, ErrorCode errorCode, Exception? exception = null, ErrorCode? moduleErrorCode = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors ?? Array.Empty<string>();
        ErrorCode = errorCode;
        Exception = exception;
        ModuleErrorCode = moduleErrorCode;
    }

    /// <summary>创建成功结果</summary>
    public static Result<T> Success(T value) => new(true, value, null, null, default);

    /// <summary>创建成功结果（带消息）</summary>
    public static Result<T> Success(T value, string message) => new(true, value, message, null, default);

    /// <summary>创建失败结果</summary>
    public static Result<T> Failure(ErrorCode code, string error) => new(false, default, error, null, code);

    /// <summary>创建失败结果（仅错误信息，兼容 Shared.Models.Result）</summary>
    public static Result<T> Failure(string error) => new(false, default, error, null, ErrorCode.InternalError);

    /// <summary>创建失败结果（带异常，兼容 ServiceResult）</summary>
    public static Result<T> Failure(string error, Exception exception) => new(false, default, error, null, ErrorCode.InternalError, exception);

    /// <summary>创建失败结果（带多个错误）</summary>
    public static Result<T> Failure(ErrorCode code, List<string> errors) => new(false, default, string.Join("; ", errors), errors, code);

    /// <summary>创建失败结果（多个错误信息，兼容 Shared.Models.Result）</summary>
    public static Result<T> Failure(List<string> errors) => new(false, default, string.Join("; ", errors), errors, ErrorCode.InternalError);

    /// <summary>创建验证失败结果</summary>
    public static Result<T> ValidationFailure(string error) => new(false, default, error, null, ErrorCode.ValidationFailed);

    /// <summary>从异常创建失败结果</summary>
    public static Result<T> FromException(Exception ex, string? operationName = null)
    {
        var message = string.IsNullOrEmpty(operationName)
            ? ex.Message
            : $"{operationName}失败: {ex.Message}";
        return new(false, default, message, new List<string> { message }, ErrorCode.InternalError, ex);
    }

    /// <summary>隐式转换：值 → Result&lt;T&gt;</summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// 无数据的操作结果。
/// </summary>
public class Result
{
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; }

    /// <summary>错误信息</summary>
    public string? Error { get; }

    /// <summary>错误信息别名（兼容 Shared.Models.Result）</summary>
    public string? ErrorMessage => Error;

    /// <summary>消息别名（兼容 ServiceResult）</summary>
    public string? Message => Error;

    /// <summary>错误列表（支持多个验证错误）</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>错误码</summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>模块错误码（用于 HTTP 状态码映射）</summary>
    public ErrorCode? ModuleErrorCode { get; }

    /// <summary>关联异常（可选，用于调试）</summary>
    public Exception? Exception { get; }

    private Result(bool isSuccess, string? error, IReadOnlyList<string>? errors, ErrorCode errorCode, Exception? exception = null, ErrorCode? moduleErrorCode = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? Array.Empty<string>();
        ErrorCode = errorCode;
        Exception = exception;
        ModuleErrorCode = moduleErrorCode;
    }

    /// <summary>创建成功结果</summary>
    public static Result Success() => new(true, null, null, default);

    /// <summary>创建成功结果（带消息）</summary>
    public static Result Success(string message) => new(true, message, null, default);

    /// <summary>创建失败结果</summary>
    public static Result Failure(ErrorCode code, string error) => new(false, error, null, code);

    /// <summary>创建失败结果（仅错误信息，兼容 Shared.Models.Result）</summary>
    public static Result Failure(string error) => new(false, error, null, ErrorCode.InternalError);

    /// <summary>创建失败结果（带异常，兼容 ServiceResult）</summary>
    public static Result Failure(string error, Exception exception) => new(false, error, null, ErrorCode.InternalError, exception);

    /// <summary>创建失败结果（带多个错误）</summary>
    public static Result Failure(ErrorCode code, List<string> errors) => new(false, string.Join("; ", errors), errors, code);

    /// <summary>创建失败结果（多个错误信息，兼容 Shared.Models.Result）</summary>
    public static Result Failure(List<string> errors) => new(false, string.Join("; ", errors), errors, ErrorCode.InternalError);

    /// <summary>从异常创建失败结果</summary>
    public static Result FromException(Exception ex, string? operationName = null)
    {
        var message = string.IsNullOrEmpty(operationName)
            ? ex.Message
            : $"{operationName}失败: {ex.Message}";
        return new(false, message, new List<string> { message }, ErrorCode.InternalError, ex);
    }
}
