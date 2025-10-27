using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.MedicalCase.Dtos;

/// <summary>
/// 更新辨证信息请求DTO（完整四诊字段）
/// Epic #1612 Task 1.3
/// </summary>
public class UpdateConsultationRequest
{
    /// <summary>
    /// 主诉
    /// </summary>
    [Required(ErrorMessage = "主诉不能为空")]
    [MaxLength(500, ErrorMessage = "主诉长度不能超过500字符")]
    public string ChiefComplaint { get; set; } = string.Empty;

    /// <summary>
    /// 现病史
    /// </summary>
    [MaxLength(1000)]
    public string? PresentIllness { get; set; }

    /// <summary>
    /// 望诊（四诊之一）
    /// </summary>
    [MaxLength(500)]
    public string? Inspection { get; set; }

    /// <summary>
    /// 闻诊（四诊之二）
    /// </summary>
    [MaxLength(500)]
    public string? AuscultationOlfaction { get; set; }

    /// <summary>
    /// 问诊（四诊之三）
    /// </summary>
    [MaxLength(500)]
    public string? Inquiry { get; set; }

    /// <summary>
    /// 切诊（四诊之四）
    /// </summary>
    [MaxLength(500)]
    public string? Palpation { get; set; }

    /// <summary>
    /// 中医诊断
    /// </summary>
    [MaxLength(500)]
    public string? TCMDiagnosis { get; set; }

    /// <summary>
    /// 治疗原则
    /// </summary>
    [MaxLength(500)]
    public string? TreatmentPrinciple { get; set; }

    /// <summary>
    /// 医嘱
    /// </summary>
    [MaxLength(1000)]
    public string? MedicalAdvice { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }
}
