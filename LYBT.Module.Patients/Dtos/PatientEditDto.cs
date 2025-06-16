using LYBT.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Patients.Dtos {
    /// <summary>
    /// 编辑病人信息Dto
    /// </summary>
    public class PatientEditDto {
        /// <summary>病人ID（必填）</summary>
        [Required(ErrorMessage = "ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>姓名（必填）</summary>
        [Required(ErrorMessage = "姓名不能为空")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别（必填）</summary>
        [Required(ErrorMessage = "性别不能为空")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>年龄</summary>
        public int? Age { get; set; }

        /// <summary>过敏史</summary>
        public string AllergyHistory { get; set; } = string.Empty;

        /// <summary>民族</summary>
        public string Ethnicity { get; set; } = string.Empty;

        /// <summary>地址（必填）</summary>
        [Required(ErrorMessage = "地址不能为空")]
        public string Address { get; set; } = string.Empty;

        /// <summary>手机号（必填）</summary>
        [Required(ErrorMessage = "手机号不能为空")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>学历</summary>
        public string Education { get; set; } = string.Empty;

        /// <summary>职业</summary>
        public string Profession { get; set; } = string.Empty;

        /// <summary>证件类型</summary>
        public string IDType { get; set; } = string.Empty;

        /// <summary>证件号（必填）</summary>
        [Required(ErrorMessage = "证件号不能为空")]
        public string IDNumber { get; set; } = string.Empty;

        /// <summary>婚姻状况</summary>
        public string MaritalStatus { get; set; } = string.Empty;
    }
}
