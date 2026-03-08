using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Registration;

/// <summary>
/// 挂号列表 DTO -- 队列展示用
/// PRD: registration.md US-REG-003
/// </summary>
public class RegistrationListDto
{
    /// <summary>挂号 ID</summary>
    [DisplayName("挂号ID")]
    public Guid Id { get; set; }

    /// <summary>患者 ID</summary>
    [DisplayName("患者")]
    public Guid PatientId { get; set; }

    /// <summary>患者姓名</summary>
    [DisplayName("患者姓名")]
    public string PatientName { get; set; } = string.Empty;

    /// <summary>指派医生 ID</summary>
    [DisplayName("医生")]
    public Guid DoctorId { get; set; }

    /// <summary>医生姓名</summary>
    [DisplayName("医生姓名")]
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>关联医案 ID</summary>
    [DisplayName("关联医案")]
    public Guid? MedicalCaseId { get; set; }

    /// <summary>挂号来源</summary>
    [DisplayName("挂号来源")]
    public RegistrationSource Source { get; set; }

    /// <summary>挂号状态</summary>
    [DisplayName("挂号状态")]
    public RegistrationStatus Status { get; set; }

    /// <summary>挂号时间</summary>
    [DisplayName("挂号时间")]
    public DateTime CreatedAt { get; set; }
}
