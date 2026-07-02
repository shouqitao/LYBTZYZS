using LYBT.Shared.Primitives.ErrorCodes;

namespace LYBT.SharedKernel.Common;

/// <summary>
/// 操作结果封装。用于命令和查询的统一返回类型。
/// </summary>
public class Result<T>
{
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; }

    /// <summary>返回数据</summary>
    public T? Value { get; }

    /// <summary>错误信息</summary>
    public string? Error { get; }

    /// <summary>错误码</summary>
    public ErrorCode ErrorCode { get; }

    private Result(bool isSuccess, T? value, string? error, ErrorCode errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    /// <summary>创建成功结果</summary>
    public static Result<T> Success(T value) => new(true, value, null, default);

    /// <summary>创建失败结果</summary>
    public static Result<T> Failure(ErrorCode code, string error) => new(false, default, error, code);

    /// <summary>创建验证失败结果</summary>
    public static Result<T> ValidationFailure(string error) => new(false, default, error, ErrorCode.ValidationFailed);

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

    /// <summary>错误码</summary>
    public ErrorCode ErrorCode { get; }

    private Result(bool isSuccess, string? error, ErrorCode errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    /// <summary>创建成功结果</summary>
    public static Result Success() => new(true, null, default);

    /// <summary>创建失败结果</summary>
    public static Result Failure(ErrorCode code, string error) => new(false, error, code);
}


