using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Consultation;

/// <summary>
/// 诊疗列表DTO - 列表视图最小字段集
/// OpenSpec: refactor-dto-simplification - 扁平化设计
/// </summary>
/// <remarks>
/// 设计原则：
/// - 仅包含列表显示必需的字段
/// - 排除详情字段（四诊详细内容）
/// - 包含必要的展示字段（PatientName/DoctorName）
/// </remarks>
public class ConsultationListDto
{
    /// <summary>诊疗ID（等于MedicalCaseId，共享主键）</summary>
    [DisplayName("诊疗ID")]
    public Guid Id { get; set; }

    /// <summary>医疗案例ID</summary>
    [DisplayName("医疗案例ID")]
    public Guid MedicalCaseId { get; set; }

    /// <summary>患者姓名</summary>
    [DisplayName("患者姓名")]
    public string? PatientName { get; set; }

    /// <summary>医生姓名</summary>
    [DisplayName("医生姓名")]
    public string? DoctorName { get; set; }

    /// <summary>中医诊断（列表摘要显示）</summary>
    [DisplayName("中医诊断")]
    public string? TCMDiagnosis { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }
}
