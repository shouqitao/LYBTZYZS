using LYBT.Shared.Primitives.ErrorCodes;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 验证异常 - 用于数据验证失败场景
/// consolidate-exception-handling: 从LYBT.Shared.Models迁移
/// </summary>
public class ValidationException : AppException
{
    /// <summary>
    /// 验证错误集合（字段名 -> 错误消息列表）
    /// </summary>
    public Dictionary<string, string[]> Errors { get; } = new();

    /// <summary>
    /// 单字段验证失败时的字段名
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// 是否包含验证错误
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    public override int GetHttpStatusCode() => 400;

    public override ErrorCategory Category => ErrorCategory.Validation;

    public ValidationException() : base("验证失败")
    {
        TypedErrorCode = EC.ValidationFailed;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
    }

    public ValidationException(string message) : base(message)
    {
        TypedErrorCode = EC.ValidationFailed;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        UserMessage = message;
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        TypedErrorCode = EC.ValidationFailed;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        UserMessage = message;
    }

    public ValidationException(string fieldName, string errorMessage)
        : base($"字段 '{fieldName}' 验证失败: {errorMessage}")
    {
        TypedErrorCode = EC.ValidationFailed;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        FieldName = fieldName;
        UserMessage = errorMessage;
        Errors[fieldName] = new[] { errorMessage };
    }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("验证失败")
    {
        TypedErrorCode = EC.ValidationFailed;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        foreach (var error in errors)
        {
            Errors[error.Key] = error.Value;
        }
        UserMessage = "验证失败，请检查输入数据";
    }

    /// <summary>
    /// 添加验证错误
    /// </summary>
    public ValidationException AddError(string fieldName, string errorMessage)
    {
        if (Errors.TryGetValue(fieldName, out var existingErrors))
        {
            var newErrors = new string[existingErrors.Length + 1];
            existingErrors.CopyTo(newErrors, 0);
            newErrors[existingErrors.Length] = errorMessage;
            Errors[fieldName] = newErrors;
        }
        else
        {
            Errors[fieldName] = new[] { errorMessage };
        }
        return this;
    }
}
