using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Registration;

/// <summary>
/// 挂号详情 DTO -- 扁平化设计
/// PRD: registration.md
/// </summary>
public class RegistrationDetailDto : ICreatorTrackable
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

    /// <summary>关联医案 ID (Waiting 时为 null)</summary>
    [DisplayName("关联医案")]
    public Guid? MedicalCaseId { get; set; }

    /// <summary>挂号来源</summary>
    [DisplayName("挂号来源")]
    public RegistrationSource Source { get; set; }

    /// <summary>挂号状态</summary>
    [DisplayName("挂号状态")]
    public RegistrationStatus Status { get; set; }

    /// <summary>备注</summary>
    [DisplayName("备注")]
    public string? Remark { get; set; }

    /// <summary>创建时间 (挂号时间)</summary>
    [DisplayName("挂号时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>创建者 ID</summary>
    [DisplayName("创建者")]
    public Guid? CreatedBy { get; set; }
}
