using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Patients;

/// <summary>
/// 患者详情DTO - 扁平化设计
/// OpenSpec: dto-architecture-specification - 统一使用PatientDetailDto
/// </summary>
public class PatientDetailDto : ICreatorTrackable
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

    /// <summary>出生日期</summary>
    [DisplayName("出生日期")]
    public DateTime? BirthDate { get; set; }

    /// <summary>年龄（由Service计算）</summary>
    [DisplayName("年龄")]
    public int? Age { get; set; }

    /// <summary>身份证号</summary>
    [DisplayName("身份证号")]
    public string? IdNumber { get; set; }

    /// <summary>手机号码</summary>
    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    /// <summary>地址</summary>
    [DisplayName("地址")]
    public string? Address { get; set; }

    /// <summary>婚姻状态</summary>
    [DisplayName("婚姻状态")]
    public int MaritalStatus { get; set; }

    /// <summary>证件类型</summary>
    [DisplayName("证件类型")]
    public int IdType { get; set; }

    /// <summary>血型</summary>
    [DisplayName("血型")]
    public int BloodType { get; set; }

    /// <summary>过敏史</summary>
    [DisplayName("过敏史")]
    public string? AllergyHistory { get; set; }

    /// <summary>既往病史</summary>
    [DisplayName("既往病史")]
    public string? MedicalHistory { get; set; }

    /// <summary>紧急联系人姓名</summary>
    [DisplayName("紧急联系人姓名")]
    public string? EmergencyContactName { get; set; }

    /// <summary>紧急联系人电话</summary>
    [DisplayName("紧急联系人电话")]
    public string? EmergencyContactPhone { get; set; }

    /// <summary>紧急联系人关系</summary>
    [DisplayName("紧急联系人关系")]
    public string? EmergencyContactRelation { get; set; }

    /// <summary>最后就诊时间</summary>
    [DisplayName("最后就诊时间")]
    public DateTime? LastVisitTime { get; set; }

    /// <summary>就诊次数</summary>
    [DisplayName("就诊次数")]
    public int VisitCount { get; set; }

    /// <summary>禁用原因</summary>
    [DisplayName("禁用原因")]
    public string? DisableReason { get; set; }

    /// <summary>拼音码</summary>
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>创建者ID - 用于所有权检查</summary>
    [DisplayName("创建者")]
    public Guid? CreatedBy { get; set; }
}
