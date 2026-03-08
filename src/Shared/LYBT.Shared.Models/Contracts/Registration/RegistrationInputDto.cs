using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Registration;

/// <summary>
/// 挂号输入 DTO -- 前台创建挂号时使用
/// PRD: registration.md US-REG-001
/// </summary>
public class RegistrationInputDto
{
    /// <summary>患者 ID</summary>
    [Required(ErrorMessage = "患者不能为空")]
    [DisplayName("患者")]
    public Guid PatientId { get; set; }

    /// <summary>患者姓名 (冗余，列表展示用)</summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(100, ErrorMessage = "患者姓名长度不能超过{1}个字符")]
    [DisplayName("患者姓名")]
    public string PatientName { get; set; } = string.Empty;

    /// <summary>指派医生 ID</summary>
    [Required(ErrorMessage = "医生不能为空")]
    [DisplayName("医生")]
    public Guid DoctorId { get; set; }

    /// <summary>医生姓名 (冗余)</summary>
    [Required(ErrorMessage = "医生姓名不能为空")]
    [StringLength(100, ErrorMessage = "医生姓名长度不能超过{1}个字符")]
    [DisplayName("医生姓名")]
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>挂号来源</summary>
    [DisplayName("挂号来源")]
    public RegistrationSource Source { get; set; }

    /// <summary>备注</summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过{1}个字符")]
    [DisplayName("备注")]
    public string? Remark { get; set; }
}
