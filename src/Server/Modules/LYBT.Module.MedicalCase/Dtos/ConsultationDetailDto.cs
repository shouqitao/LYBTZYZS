namespace LYBT.Module.MedicalCase.Dtos;

/// <summary>
/// 辨证信息响应DTO（完整四诊字段）
/// Epic #1612 Task 1.3
/// </summary>
public class ConsultationDetailDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// 主诉
    /// </summary>
    public string? ChiefComplaint { get; set; }

    /// <summary>
    /// 现病史
    /// </summary>
    public string? PresentIllness { get; set; }

    /// <summary>
    /// 望诊
    /// </summary>
    public string? Inspection { get; set; }

    /// <summary>
    /// 闻诊
    /// </summary>
    public string? AuscultationOlfaction { get; set; }

    /// <summary>
    /// 问诊
    /// </summary>
    public string? Inquiry { get; set; }

    /// <summary>
    /// 切诊
    /// </summary>
    public string? Palpation { get; set; }

    /// <summary>
    /// 中医诊断
    /// </summary>
    public string? TCMDiagnosis { get; set; }

    /// <summary>
    /// 治疗原则
    /// </summary>
    public string? TreatmentPrinciple { get; set; }

    /// <summary>
    /// 医嘱
    /// </summary>
    public string? MedicalAdvice { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// Step1完成时间（辩证）
    /// </summary>
    public DateTime? Step1CompletedAt { get; set; }

    /// <summary>
    /// Step2完成时间（施治）
    /// </summary>
    public DateTime? Step2CompletedAt { get; set; }

    /// <summary>
    /// Step3完成时间（总结）
    /// </summary>
    public DateTime? Step3CompletedAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
