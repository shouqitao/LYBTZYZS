using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Patients
{

    /// <summary>
    /// 患者详情DTO - 简化版
    /// 只包含核心的患者信息字段
    /// </summary>
    public class PatientDetailDto
    {

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid Id { get; set; }

        /// <summary>姓名</summary>
        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
        [DisplayName("姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [Required(ErrorMessage = "性别不能为空")]
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>年龄</summary>
        [Range(0, 150, ErrorMessage = "年龄必须在0-150之间")]
        [DisplayName("年龄")]
        public int Age { get; set; }

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public DateTime? BirthDate { get; set; }

        /// <summary>手机号</summary>
        [Required(ErrorMessage = "手机号不能为空")]
        [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
        [DisplayName("手机号")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>身份证号</summary>
        [StringLength(30, ErrorMessage = "身份证号长度不能超过30个字符")]
        [DisplayName("身份证号")]
        public string? IDNumber { get; set; }

        /// <summary>地址</summary>
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>拼音码（用于快速搜索）</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}