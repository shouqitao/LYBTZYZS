using LYBT.Shared.Models.Primitives.ErrorCodes;
using EC = LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 资源未找到异常
/// consolidate-exception-handling: 从LYBT.Shared.Models迁移
/// </summary>
public class NotFoundException : AppException
{
    /// <summary>
    /// 资源类型
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// 资源ID
    /// </summary>
    public string? ResourceId { get; set; }

    public override ErrorCategory Category => ErrorCategory.Resource;

    public NotFoundException() : base("请求的资源不存在")
    {
        TypedErrorCode = EC.NotFound;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
    }

    public NotFoundException(string message) : base(message)
    {
        TypedErrorCode = EC.NotFound;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        UserMessage = message;
    }

    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} (ID: {resourceId}) 不存在")
    {
        TypedErrorCode = EC.NotFound;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        ResourceType = resourceType;
        ResourceId = resourceId;
        UserMessage = $"{resourceType}不存在";
    }

    public NotFoundException(EC errorCode, string message, string? resourceType = null, string? resourceId = null)
        : base(errorCode, message)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
