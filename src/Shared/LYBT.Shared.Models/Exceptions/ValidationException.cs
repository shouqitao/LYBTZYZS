using LYBT.Shared.Models.Constants;

namespace LYBT.Shared.Models.Exceptions;

/// <summary>
/// 数据验证异常 - UltraThink统一异常体系
/// </summary>
public class ValidationException : AppException
{

    /// <summary>
    /// 验证错误集合 (字段名 -> 错误消息数组)
    /// </summary>
    public Dictionary<string, string[]> Errors { get; set; }

    /// <summary>
    /// 失败的字段名（单个字段验证失败时）
    /// </summary>
    public string? FieldName { get; set; }

    public ValidationException() : base(ErrorMessageKeys.VALIDATION_FAILURE)
    {
        Errors = new Dictionary<string, string[]>();
        ShowDetailToUser = true; // 验证异常需要显示给用户
    }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
        ShowDetailToUser = true;
    }

    public ValidationException(string message, Dictionary<string, string[]> errors) : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
        ShowDetailToUser = true;
    }

    public ValidationException(string fieldName, string errorMessage) : base(string.Format(ErrorMessageKeys.FIELD_VALIDATION_FAILED, fieldName, errorMessage))
    {
        FieldName = fieldName;
        Errors = new Dictionary<string, string[]>
        {
            [fieldName] = new[] { errorMessage }
        };
        ShowDetailToUser = true;
    }

    public ValidationException(string fieldName, string[] errorMessages) : base(string.Format(ErrorMessageKeys.FIELD_VALIDATION_ERROR, fieldName))
    {
        FieldName = fieldName;
        Errors = new Dictionary<string, string[]>
        {
            [fieldName] = errorMessages
        };
        ShowDetailToUser = true;
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
        ShowDetailToUser = true;
    }

    /// <summary>
    /// 添加验证错误
    /// </summary>
    public void AddError(string fieldName, string errorMessage)
    {
        if (Errors.ContainsKey(fieldName))
        {
            var existingErrors = Errors[fieldName].ToList();
            existingErrors.Add(errorMessage);
            Errors[fieldName] = existingErrors.ToArray();
        }
        else
        {
            Errors[fieldName] = new[] { errorMessage };
        }
    }

    /// <summary>
    /// 是否包含验证错误
    /// </summary>
    public bool HasErrors => Errors.Any();
}
