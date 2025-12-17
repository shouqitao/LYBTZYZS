using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Errors;

namespace LYBT.Shared.Models.Exceptions;

/// <summary>
/// 并发冲突异常 - HTTP 409 Conflict
/// refactor-logging-system: 用于乐观并发控制场景
/// </summary>
public class ConflictException : AppException
{
    /// <summary>
    /// 资源类型
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// 资源标识符
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// 当前版本号
    /// </summary>
    public byte[]? CurrentVersion { get; set; }

    /// <summary>
    /// 预期版本号
    /// </summary>
    public byte[]? ExpectedVersion { get; set; }

    public override int GetHttpStatusCode() => 409;

    public override ErrorCategory Category => ErrorCategory.Concurrency;

    public ConflictException() : base("资源冲突")
    {
        TypedErrorCode = Errors.ErrorCode.ConcurrencyConflict;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        ShowDetailToUser = true;
    }

    public ConflictException(string message) : base(message)
    {
        TypedErrorCode = Errors.ErrorCode.ConcurrencyConflict;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        ShowDetailToUser = true;
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
        TypedErrorCode = Errors.ErrorCode.ConcurrencyConflict;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        ShowDetailToUser = true;
    }

    public ConflictException(string resourceType, string resourceId, string? message = null)
        : base(message ?? $"资源 {resourceType}[{resourceId}] 已被其他用户修改，请刷新后重试")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
        TypedErrorCode = Errors.ErrorCode.ConcurrencyConflict;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        ShowDetailToUser = true;
    }

    public ConflictException(string resourceType, Guid resourceId, string? message = null)
        : this(resourceType, resourceId.ToString(), message)
    {
    }

    /// <summary>
    /// 创建包含版本信息的冲突异常
    /// </summary>
    public ConflictException(string resourceType, string resourceId, byte[]? currentVersion, byte[]? expectedVersion)
        : this(resourceType, resourceId)
    {
        CurrentVersion = currentVersion;
        ExpectedVersion = expectedVersion;
    }

    /// <summary>
    /// 创建病例版本冲突异常
    /// </summary>
    public static ConflictException MedicalCaseVersion(Guid medicalCaseId, byte[]? currentVersion = null, byte[]? expectedVersion = null)
    {
        return new ConflictException("MedicalCase", medicalCaseId.ToString(), currentVersion, expectedVersion)
        {
            TypedErrorCode = Errors.ErrorCode.MedicalCaseVersionConflict,
            ErrorCode = Errors.ErrorCode.MedicalCaseVersionConflict.ToFormattedString(),
            UserMessage = "病例数据已被其他用户修改，请刷新页面后重试"
        };
    }

    /// <summary>
    /// 创建病例锁定冲突异常
    /// </summary>
    public static ConflictException MedicalCaseLocked(Guid medicalCaseId, string? lockedByUser = null)
    {
        var message = string.IsNullOrEmpty(lockedByUser)
            ? "病例正在被其他用户编辑"
            : $"病例正在被用户 {lockedByUser} 编辑";

        return new ConflictException("MedicalCase", medicalCaseId.ToString(), message)
        {
            TypedErrorCode = Errors.ErrorCode.MedicalCaseLocked,
            ErrorCode = Errors.ErrorCode.MedicalCaseLocked.ToFormattedString(),
            UserMessage = message
        };
    }

    /// <summary>
    /// 创建重复资源冲突异常
    /// </summary>
    public static ConflictException Duplicate(string resourceType, string fieldName, string fieldValue)
    {
        return new ConflictException(resourceType, fieldValue, $"{resourceType} 的 {fieldName} '{fieldValue}' 已存在")
        {
            UserMessage = $"{fieldName} '{fieldValue}' 已被使用"
        };
    }
}
