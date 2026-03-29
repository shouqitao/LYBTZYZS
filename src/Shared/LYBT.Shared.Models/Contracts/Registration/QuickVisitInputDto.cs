using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Registration;

/// <summary>
/// 医生快速看诊输入 DTO
/// PRD: registration.md US-REG-002
/// 医生选择患者后，系统自动创建 Registration + MedicalCase
/// </summary>
public class QuickVisitInputDto
{
    /// <summary>患者 ID</summary>
    [Required(ErrorMessage = "患者不能为空")]
    [DisplayName("患者")]
    public Guid PatientId { get; set; }

    /// <summary>患者姓名 (冗余，用于日志和返回)</summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(100, ErrorMessage = "患者姓名长度不能超过{1}个字符")]
    [DisplayName("患者姓名")]
    public string PatientName { get; set; } = string.Empty;

    /// <summary>备注</summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过{1}个字符")]
    [DisplayName("备注")]
    public string? Remark { get; set; }
}

/// <summary>
/// 医生快速看诊返回 DTO
/// 包含自动创建的 RegistrationId 和 MedicalCaseId
/// </summary>
public class QuickVisitResultDto
{
    /// <summary>挂号记录 ID</summary>
    public Guid RegistrationId { get; set; }

    /// <summary>医案 ID</summary>
    public Guid MedicalCaseId { get; set; }

    /// <summary>患者 ID</summary>
    public Guid PatientId { get; set; }

    /// <summary>患者姓名</summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>医生 ID</summary>
    public Guid DoctorId { get; set; }

    /// <summary>医生姓名</summary>
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }
}
