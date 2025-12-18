using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase;

/// <summary>
/// 医案列表DTO - 扁平化设计
/// OpenSpec: refactor-dto-simplification - 用于列表视图展示
/// </summary>
public class MedicalCaseListDto
{
    /// <summary>医案ID</summary>
    [DisplayName("医案ID")]
    public Guid Id { get; set; }

    /// <summary>案例编号</summary>
    [DisplayName("案例编号")]
    public string? CaseNumber { get; set; }

    /// <summary>患者ID</summary>
    [DisplayName("患者ID")]
    public Guid PatientId { get; set; }

    /// <summary>患者姓名</summary>
    [DisplayName("患者姓名")]
    public string PatientName { get; set; } = string.Empty;

    /// <summary>患者性别</summary>
    [DisplayName("患者性别")]
    public string? PatientGender { get; set; }

    /// <summary>患者年龄</summary>
    [DisplayName("患者年龄")]
    public int? PatientAge { get; set; }

    /// <summary>医生ID</summary>
    [DisplayName("医生ID")]
    public Guid DoctorId { get; set; }

    /// <summary>医生姓名</summary>
    [DisplayName("医生姓名")]
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>诊疗时间</summary>
    [DisplayName("诊疗时间")]
    public DateTime ConsultationDate { get; set; }

    /// <summary>案例状态</summary>
    [DisplayName("案例状态")]
    public MedicalCaseStatus CaseStatus { get; set; }

    /// <summary>诊断</summary>
    [DisplayName("诊断")]
    public string? Diagnosis { get; set; }

    /// <summary>是否有诊疗记录</summary>
    [DisplayName("有诊疗")]
    public bool HasConsultation { get; set; }

    /// <summary>是否有处方</summary>
    [DisplayName("有处方")]
    public bool HasPrescription { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
}
