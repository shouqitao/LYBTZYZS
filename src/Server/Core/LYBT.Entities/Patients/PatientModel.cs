using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Entities.Patients
{
    /// <summary>
    /// 患者实体 - UltraThink v2.0架构简化版
    /// 合并了原BasePatient和PatientModel，包含完整患者档案信息
    /// 删除五笔码字段，保留拼音码用于快速搜索
    /// </summary>
    [Table("Patients")]
public class Patient
{
    /// <summary>患者唯一标识</summary>
    [Key]
    [DisplayName("患者ID")]
    public Guid Id { get; set; }

    /// <summary>患者姓名</summary>
    [Required]
    [StringLength(50)]
    [DisplayName("姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>拼音码（用于快速搜索）</summary>
    [StringLength(50)]
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    /// <summary>性别</summary>
    [DisplayName("性别")]
    public Gender Gender { get; set; } = Gender.Unknown;

    /// <summary>出生日期</summary>
    [DisplayName("出生日期")]
    public DateTime? BirthDate { get; set; }

    /// <summary>证件类型</summary>
    [StringLength(20)]
    [DisplayName("证件类型")]
    public string? IdType { get; set; }

    /// <summary>证件号码</summary>
    [StringLength(50)]
    [DisplayName("证件号码")]
    public string? IdNumber { get; set; }

    /// <summary>手机号码</summary>
    [StringLength(20)]
    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    /// <summary>地址</summary>
    [StringLength(200)]
    [DisplayName("地址")]
    public string? Address { get; set; }

    /// <summary>过敏史</summary>
    [StringLength(500)]
    [DisplayName("过敏史")]
    public string? AllergyHistory { get; set; }

    /// <summary>患者状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>最后就诊时间</summary>
    [DisplayName("最后就诊时间")]
    public DateTime? LastVisitTime { get; set; }

    /// <summary>就诊次数</summary>
    [DisplayName("就诊次数")]
    public int VisitCount { get; set; } = 0;

    /// <summary>年龄（计算属性）</summary>
    [NotMapped]
    [DisplayName("年龄")]
    public int? Age 
    { 
        get 
        {
            if (BirthDate.HasValue)
            {
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
            return null;
        }
    }

    /// <summary>禁用原因</summary>
    [StringLength(200)]
    [DisplayName("禁用原因")]
    public string? DisableReason { get; set; }

}

}