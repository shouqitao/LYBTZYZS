using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 患者导入DTO
    /// </summary>
    public class PatientImportDto
    {

        /// <summary>姓名</summary>
        [Required(ErrorMessage = "姓名不能为空")]
        [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
        [DisplayName("姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public string GenderText { get; set; } = string.Empty;

        /// <summary>年龄</summary>
        [DisplayName("年龄")]
        public int? Age { get; set; }

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public string? BirthDateText { get; set; }

        /// <summary>身份证号</summary>
        [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
        [DisplayName("身份证号")]
        public string? IdCardNumber { get; set; }

        /// <summary>身份证号（兼容别名）</summary>
        public string? IdNumber => IdCardNumber;

        /// <summary>手机号码</summary>
        [StringLength(11, ErrorMessage = "手机号码长度不能超过11个字符")]
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>地址</summary>
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>紧急联系人姓名</summary>
        [StringLength(50, ErrorMessage = "紧急联系人姓名长度不能超过50个字符")]
        [DisplayName("紧急联系人姓名")]
        public string? EmergencyContactName { get; set; }

        /// <summary>紧急联系人电话</summary>
        [StringLength(20, ErrorMessage = "紧急联系人电话长度不能超过20个字符")]
        [DisplayName("紧急联系人电话")]
        public string? EmergencyContactPhone { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}
