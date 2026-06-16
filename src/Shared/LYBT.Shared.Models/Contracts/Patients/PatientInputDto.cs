using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.Validation;

namespace LYBT.Shared.Models.Contracts.Patients
{
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

        // OpenSpec: refactor-dto-simplification - Status字段已移除
        // InputDto不应包含Status字段，状态变更应通过专用API进行

        /// <summary>患者ID（更新时必填，创建时为null）</summary>
        [DisplayName("患者ID")]
        public Guid? Id { get; set; }
    }
}
