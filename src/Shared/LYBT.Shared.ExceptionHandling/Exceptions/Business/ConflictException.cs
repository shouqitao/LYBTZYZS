using LYBT.Shared.Primitives.ErrorCodes;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 资源冲突异常 - 用于并发冲突、数据重复等场景
/// consolidate-exception-handling: 从LYBT.Shared.Models迁移
/// </summary>
public class ConflictException : AppException
{
    /// <summary>
    /// 资源类型（别名EntityType）
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// 实体类型（别名ResourceType）
    /// </summary>
    public string? EntityType
    {
        get => ResourceType;
        set => ResourceType = value;
    }

    /// <summary>
    /// 资源ID（别名EntityId）
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// 实体ID（别名ResourceId）
    /// </summary>
    public string? EntityId
    {
        get => ResourceId;
        set => ResourceId = value;
    }

    /// <summary>
    /// 当前版本号
    /// </summary>
    public int? CurrentVersion { get; set; }

    /// <summary>
    /// 期望版本号
    /// </summary>
    public int? ExpectedVersion { get; set; }

    public override int GetHttpStatusCode() => 409;

    public override ErrorCategory Category => ErrorCategory.Concurrency;

    public ConflictException() : base("资源冲突")
    {
        TypedErrorCode = EC.ConcurrencyConflict;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
    }

    public ConflictException(string message) : base(message)
    {
        TypedErrorCode = EC.ConcurrencyConflict;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        UserMessage = message;
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
        TypedErrorCode = EC.ConcurrencyConflict;
        ErrorCode = TypedErrorCode.Value.ToFormattedString();
        UserMessage = message;
    }

    public ConflictException(EC errorCode, string message, string? userMessage = null)
        : base(errorCode, message, userMessage)
    {
    }

    // 静态工厂方法
    public static ConflictException MedicalCaseVersion(Guid caseId, int expectedVersion, int currentVersion)
    {
        return new ConflictException(
            EC.MedicalCaseVersionConflict,
            $"病历 (ID: {caseId}) 版本冲突，期望版本: {expectedVersion}，当前版本: {currentVersion}",
            "病历数据已被其他用户修改，请刷新页面后重试")
        {
            ResourceType = "病历",
            ResourceId = caseId.ToString(),
            ExpectedVersion = expectedVersion,
            CurrentVersion = currentVersion
        };
    }

    public static ConflictException MedicalCaseLocked(Guid caseId, string? lockedBy = null)
    {
        var message = lockedBy != null
            ? $"病历正在被用户 {lockedBy} 编辑"
            : "病历正在被其他用户编辑";
        return new ConflictException(EC.MedicalCaseLocked, message, message)
        {
            ResourceType = "病历",
            ResourceId = caseId.ToString()
        };
    }

    public static ConflictException Duplicate(string resourceType, string fieldName, string value)
    {
        return new ConflictException(
            EC.ConcurrencyConflict,
            $"{resourceType}的{fieldName}已存在: {value}",
            $"{fieldName}已被使用");
    }
}
