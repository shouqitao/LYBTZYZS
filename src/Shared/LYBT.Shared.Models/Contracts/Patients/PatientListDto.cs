using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Patients;

/// <summary>
/// 患者列表DTO - 扁平化设计
/// OpenSpec: refactor-dto-simplification - 用于列表视图展示
/// </summary>
public class PatientListDto
{
    /// <summary>患者ID</summary>
    [DisplayName("患者ID")]
    public Guid Id { get; set; }

    /// <summary>患者姓名</summary>
    [DisplayName("患者姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    [DisplayName("性别")]
    public Gender Gender { get; set; }

    /// <summary>年龄（由Service计算）</summary>
    [DisplayName("年龄")]
    public int? Age { get; set; }

    /// <summary>手机号码</summary>
    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    /// <summary>地址</summary>
    [DisplayName("地址")]
    public string? Address { get; set; }

    /// <summary>最后就诊时间</summary>
    [DisplayName("最后就诊时间")]
    public DateTime? LastVisitTime { get; set; }

    /// <summary>就诊次数</summary>
    [DisplayName("就诊次数")]
    public int VisitCount { get; set; }

    /// <summary>拼音码</summary>
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
}
