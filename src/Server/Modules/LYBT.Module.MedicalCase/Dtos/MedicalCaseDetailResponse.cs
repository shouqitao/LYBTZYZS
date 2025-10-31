using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Dtos;

/// <summary>
/// 病案详情响应DTO
/// Epic #1612 Task 1.3
/// 包含Patient、Consultation、Prescription完整信息
/// </summary>
public class MedicalCaseDetailResponse
{
    public Guid Id { get; set; }

    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// 医生ID
    /// </summary>
    public Guid DoctorId { get; set; }

    /// <summary>
    /// 医生姓名
    /// </summary>
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>
    /// 辨证信息
    /// </summary>
    public ConsultationDetailDto? Consultation { get; set; }

    /// <summary>
    /// 处方信息（可选）
    /// </summary>
    public MedicalCasePrescriptionDto? Prescription { get; set; }

    /// <summary>
    /// 病案状态
    /// </summary>
    public MedicalCaseStatus Status { get; set; }

    /// <summary>
    /// 是否需要开处方
    /// </summary>
    public bool NeedsPrescription { get; set; }

    /// <summary>
    /// 诊疗时间
    /// </summary>
    public DateTime ConsultationDate { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
