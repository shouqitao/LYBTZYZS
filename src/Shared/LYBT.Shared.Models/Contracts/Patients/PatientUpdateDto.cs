using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Patients {

    /// <summary>
    /// 患者更新DTO - 前后端共享API契约
    /// 用于更新患者档案的请求模型
    /// </summary>
    public class PatientUpdateDto {

        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
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
        [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
        [DisplayName("手机号")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>证件类型</summary>
        [StringLength(20, ErrorMessage = "证件类型长度不能超过20个字符")]
        [DisplayName("证件类型")]
        public string IDType { get; set; } = "身份证";

        /// <summary>证件号</summary>
        [StringLength(30, ErrorMessage = "证件号长度不能超过30个字符")]
        [DisplayName("证件号")]
        public string IDNumber { get; set; } = string.Empty;

        /// <summary>地址</summary>
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        [DisplayName("地址")]
        public string Address { get; set; } = string.Empty;

        /// <summary>职业</summary>
        [StringLength(50, ErrorMessage = "职业长度不能超过50个字符")]
        [DisplayName("职业")]
        public string Profession { get; set; } = string.Empty;

        /// <summary>婚姻状况</summary>
        [StringLength(20, ErrorMessage = "婚姻状况长度不能超过20个字符")]
        [DisplayName("婚姻状况")]
        public string MaritalStatus { get; set; } = string.Empty;

        /// <summary>民族</summary>
        [StringLength(20, ErrorMessage = "民族长度不能超过20个字符")]
        [DisplayName("民族")]
        public string Ethnicity { get; set; } = "汉族";

        /// <summary>学历</summary>
        [StringLength(30, ErrorMessage = "学历长度不能超过30个字符")]
        [DisplayName("学历")]
        public string Education { get; set; } = string.Empty;

        /// <summary>过敏史</summary>
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        [DisplayName("过敏史")]
        public string AllergyHistory { get; set; } = string.Empty;

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; } = true;

        /// <summary>备注</summary>
        [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}