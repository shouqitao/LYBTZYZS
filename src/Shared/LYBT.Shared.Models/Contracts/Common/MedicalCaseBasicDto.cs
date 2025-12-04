using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 医案基本信息DTO - 用于跨模块查询
/// 包含关联的诊断信息，避免额外查询Consultation
/// </summary>
public class MedicalCaseBasicDto
{
    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// 医案状态
    /// </summary>
    public MedicalCaseStatus Status { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 中医诊断 - 来自关联的Consultation
    /// </summary>
    public string? TCMDiagnosis { get; set; }
}
