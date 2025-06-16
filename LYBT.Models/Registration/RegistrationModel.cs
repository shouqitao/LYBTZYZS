using LYBT.Common.Enums;

namespace LYBT.Models.Registration;

/// <summary>
/// 挂号核心数据模型，用于在模块内部处理完整数据记录
/// </summary>
public class RegistrationModel {
    /// <summary>
    /// 唯一标识符（主键，GUID 格式）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 病人 ID（外键，关联 Patients 表）
    /// </summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// 病人姓名（用于列表显示）
    /// </summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// 医生 ID（外键，关联 Doctors 表）
    /// </summary>
    public string DoctorId { get; set; } = string.Empty;

    /// <summary>
    /// 医生姓名（用于列表显示）
    /// </summary>
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>
    /// 挂号类型（如普通、复诊、急诊），使用 RegistrationType 枚举
    /// </summary>
    public RegistrationType RegistrationType { get; set; } = RegistrationType.General;

    /// <summary>
    /// 是否为医生直接挂号（如医生自己操作挂号流程，true 表示医生直接挂号）
    /// </summary>
    public bool IsFromDoctor { get; set; } = false;

    /// <summary>
    /// 挂号状态（如“待看诊”、“已完成”、“取消”），使用 RegistrationStatus 枚举
    /// </summary>
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

    /// <summary>
    /// 挂号时间
    /// </summary>
    public DateTime RegistrationTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 备注信息（可选）
    /// </summary>
    public string Remark { get; set; } = string.Empty;
}
