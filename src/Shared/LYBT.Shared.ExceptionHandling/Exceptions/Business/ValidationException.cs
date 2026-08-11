using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 验证异常（B2 US-ERR-006: 422 + 字段级 errors 字典——服务层主动抛出的验证失败，
/// 与 FluentValidation 管道（400）并存；本类统一映射 422）
/// </summary>
public class ValidationException : AppException
{
    public override int GetHttpStatusCode() => 422;

    public override ErrorCategory Category => ErrorCategory.Validation;

    /// <summary>字段级错误字典（字段名 → 错误消息列表），客户端用于表单高亮</summary>
    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();

    public ValidationException(string message) : base(message)
    {
        UserMessage = message;
    }

    public ValidationException(string field, string message) : base(message)
    {
        Errors[field] = new[] { message };
        UserMessage = message;
    }

    public ValidationException(IDictionary<string, string[]> errors, string message = "请求参数验证失败")
        : base(message)
    {
        Errors = errors;
        UserMessage = message;
    }
}
