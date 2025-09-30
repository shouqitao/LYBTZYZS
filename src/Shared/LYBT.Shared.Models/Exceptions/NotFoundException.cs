using LYBT.Shared.Models.Constants;

namespace LYBT.Shared.Models.Exceptions;

/// <summary>
/// 资源未找到异常 - UltraThink统一异常体系
/// </summary>
public class NotFoundException : AppException
{

    /// <summary>
    /// 资源类型
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// 资源标识符
    /// </summary>
    public string? ResourceId { get; set; }

    public NotFoundException() : base(ErrorMessageKeys.RESOURCE_NOT_FOUND)
    {
        ShowDetailToUser = true; // 资源不存在需要告诉用户
    }

    public NotFoundException(string message) : base(message)
    {
        ShowDetailToUser = true;
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
        ShowDetailToUser = true;
    }

    public NotFoundException(string resourceType, string resourceId)
        : base(string.Format(ErrorMessageKeys.RESOURCE_WITH_ID_NOT_FOUND, resourceType, resourceId))
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
        ShowDetailToUser = true;
    }

    public NotFoundException(string resourceType, Guid resourceId)
        : this(resourceType, resourceId.ToString())
    {
    }

    /// <summary>
    /// 创建用户不存在异常
    /// </summary>
    public static NotFoundException User(Guid userId) => new(ErrorMessageKeys.USER_NOT_FOUND.Replace("用户不存在", "用户"), userId);

    /// <summary>
    /// 创建患者不存在异常
    /// </summary>
    public static NotFoundException Patient(Guid patientId) => new(ErrorMessageKeys.PATIENT_NOT_FOUND.Replace("患者不存在", "患者"), patientId);

    /// <summary>
    /// 创建药材不存在异常
    /// </summary>
    public static NotFoundException Herb(Guid herbId) => new(ErrorMessageKeys.HERB_NOT_FOUND.Replace("药材不存在", "药材"), herbId);

    /// <summary>
    /// 创建处方不存在异常
    /// </summary>
    public static NotFoundException Prescription(Guid prescriptionId) => new(ErrorMessageKeys.PRESCRIPTION_NOT_FOUND.Replace("处方不存在", "处方"), prescriptionId);

    /// <summary>
    /// 创建医案不存在异常
    /// </summary>
    public static NotFoundException MedicalCase(Guid medicalCaseId) => new(ErrorMessageKeys.MEDICAL_CASE_NOT_FOUND.Replace("医案不存在", "医案"), medicalCaseId);

    /// <summary>
    /// 创建诊断不存在异常
    /// </summary>
    public static NotFoundException Consultation(Guid consultationId) => new(ErrorMessageKeys.CONSULTATION_NOT_FOUND.Replace("诊断不存在", "诊断"), consultationId);
}
