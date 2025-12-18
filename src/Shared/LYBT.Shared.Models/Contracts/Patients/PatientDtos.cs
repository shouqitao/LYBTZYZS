using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Constants;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Patients
{

    /// <summary>
    /// 患者信息DTO - UltraThink v2.0简化版
    /// 与Patient实体对齐，统一字段名BirthDate、IdNumber
    /// </summary>
    public class PatientDto : StatusDto
    {

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; }

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public DateTime? BirthDate { get; set; }

        /// <summary>年龄（由Service层计算）</summary>
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
        public int MaritalStatus { get; set; } = 0;

        /// <summary>证件类型</summary>
        [DisplayName("证件类型")]
        public int IdType { get; set; } = 0;

        /// <summary>血型</summary>
        [DisplayName("血型")]
        public int BloodType { get; set; } = 0;

        /// <summary>过敏史</summary>
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>既往病史（Epic #1934批量导入功能新增）</summary>
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
    }

    /// <summary>
    /// 患者输入DTO - 统一创建和更新
    /// Phase 3: 合并PatientCreateDto和PatientUpdateDto
    /// Issue #2240 Fix: 移除Age字段（输入DTO只接收BirthDate，Age由Service计算）
    /// </summary>
    public class PatientInputDto
    {

        /// <summary>患者姓名</summary>
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "患者姓名长度不能超过{1}个字符")]
        [DisplayName("患者姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码（可手动修正多音字错误）</summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public DateTime? BirthDate { get; set; }

        /// <summary>身份证号</summary>
        [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
        [RegularExpression(ValidationConstants.IdCardRegex, ErrorMessage = "身份证号格式不正确")]
        [DisplayName("身份证号")]
        public string? IdNumber { get; set; }

        /// <summary>手机号</summary>
        [StringLength(ValidationConstants.PhoneMaxLength, ErrorMessage = "手机号长度不能超过{1}个字符")]
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        /// <summary>地址</summary>
        [StringLength(ValidationConstants.AddressMaxLength, ErrorMessage = "地址长度不能超过{1}个字符")]
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "过敏史长度不能超过{1}个字符")]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>既往病史（Epic #1934批量导入功能新增）</summary>
        [StringLength(1000, ErrorMessage = "既往病史长度不能超过1000个字符")]
        [DisplayName("既往病史")]
        public string? MedicalHistory { get; set; }

        /// <summary>婚姻状态</summary>
        [DisplayName("婚姻状态")]
        public int MaritalStatus { get; set; } = 0;

        /// <summary>证件类型</summary>
        [DisplayName("证件类型")]
        public int IdType { get; set; } = 0;

        /// <summary>血型</summary>
        [DisplayName("血型")]
        public int BloodType { get; set; } = 0;

        /// <summary>紧急联系人姓名</summary>
        [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "紧急联系人姓名长度不能超过{1}个字符")]
        [DisplayName("紧急联系人姓名")]
        public string? EmergencyContactName { get; set; }

        /// <summary>紧急联系人电话</summary>
        [StringLength(ValidationConstants.PhoneMaxLength, ErrorMessage = "紧急联系人电话长度不能超过{1}个字符")]
        [DisplayName("紧急联系人电话")]
        public string? EmergencyContactPhone { get; set; }

        /// <summary>紧急联系人关系</summary>
        [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "紧急联系人关系长度不能超过{1}个字符")]
        [DisplayName("紧急联系人关系")]
        public string? EmergencyContactRelation { get; set; }

        // OpenSpec: refactor-dto-simplification - Status字段已移除
        // InputDto不应包含Status字段，状态变更应通过专用API进行

        /// <summary>患者ID（更新时必填，创建时为null）</summary>
        [DisplayName("患者ID")]
        public Guid? Id { get; set; }
    }

    /// <summary>
    /// 快速创建患者DTO - 用于快速创建患者档案（仅包含必要字段）
    /// Issue #2240 Fix: 移除Age字段，改为必须提供BirthDate
    /// </summary>
    public class QuickPatientCreateDto
    {

        /// <summary>患者姓名</summary>
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "患者姓名长度不能超过{1}个字符")]
        [DisplayName("患者姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>出生日期（必填）</summary>
        [Required(ErrorMessage = "出生日期不能为空")]
        [DisplayName("出生日期")]
        public DateTime BirthDate { get; set; }

        /// <summary>手机号码</summary>
        [StringLength(ValidationConstants.PhoneMaxLength, ErrorMessage = "手机号码长度不能超过{1}个字符")]
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>快速就诊原因</summary>
        [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "就诊原因长度不能超过{1}个字符")]
        [DisplayName("就诊原因")]
        public string? ChiefComplaint { get; set; }
    }
}
