using LYBT.Shared.Primitives.ErrorCodes;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

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

    public override int GetHttpStatusCode() => 404;

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

    // 静态工厂方法
    public static NotFoundException User(Guid userId) =>
        new(EC.UserNotFound, "用户不存在", "用户", userId.ToString());

    public static NotFoundException Patient(Guid patientId) =>
        new(EC.PatientNotFound, "患者不存在", "患者", patientId.ToString());

    public static NotFoundException Herb(Guid herbId) =>
        new(EC.HerbNotFound, "药材不存在", "药材", herbId.ToString());

    public static NotFoundException Prescription(Guid prescriptionId) =>
        new(EC.PrescriptionNotFound, "处方不存在", "处方", prescriptionId.ToString());

    public static NotFoundException MedicalCase(Guid caseId) =>
        new(EC.MedicalCaseNotFound, "病历不存在", "病历", caseId.ToString());

    public static NotFoundException Consultation(Guid consultationId) =>
        new(EC.ConsultationNotFound, "诊断记录不存在", "诊断记录", consultationId.ToString());

    public static NotFoundException Formula(Guid formulaId) =>
        new(EC.FormulaNotFound, "方剂不存在", "方剂", formulaId.ToString());
}
