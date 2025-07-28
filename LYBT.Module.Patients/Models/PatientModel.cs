using LYBT.Common.Enums;
using LYBT.Common.Enums.Patients;
using LYBT.Common.Enums.System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Patients.Models {

    /// <summary>
    /// 患者信息实体
    /// </summary>
    public class PatientModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DisplayName("Gender")]
        public Gender Gender { get; set; }

        [DisplayName("Age")]
        public int? Age { get; set; }

        [Required, StringLength(20)]
        [DisplayName("PhoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, StringLength(32)]
        [DisplayName("IDNumber")]
        public string IDNumber { get; set; } = string.Empty;

        [StringLength(256)]
        [DisplayName("Address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 过敏史
        /// </summary>
        [StringLength(256)]
        [DisplayName("过敏史")]
        public string AllergyHistory { get; set; } = string.Empty;

        /// <summary>
        /// 民族
        /// </summary>
        [StringLength(32)]
        [DisplayName("民族")]
        public string Ethnicity { get; set; } = string.Empty;

        /// <summary>
        /// 学历
        /// </summary>
        [StringLength(32)]
        [DisplayName("学历")]
        public string Education { get; set; } = string.Empty;

        /// <summary>
        /// 职业
        /// </summary>
        [StringLength(64)]
        [DisplayName("职业")]
        public string Profession { get; set; } = string.Empty;

        /// <summary>
        /// 证件类型
        /// </summary>
        [StringLength(16)]
        [DisplayName("证件类型")]
        public string IDType { get; set; } = "身份证";

        /// <summary>
        /// 婚姻状况
        /// </summary>
        [StringLength(16)]
        [DisplayName("婚姻状况")]
        public string MaritalStatus { get; set; } = string.Empty;

        /// <summary>
        /// 出生日期（从身份证解析或手动录入）
        /// </summary>
        [DisplayName("出生日期")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [DisplayName("Status")]
        public PatientStatus Status { get; set; } = PatientStatus.Normal;

        [StringLength(128)]
        [DisplayName("DisableReason")]
        public string DisableReason { get; set; } = string.Empty;

        /// <summary>
        /// 是否为特殊病人（前台不可见，仅特定医生可见）
        /// </summary>
        [DisplayName("是否为特殊病人（前台不可见，仅特定医生可见）")]
        public bool IsSpecial { get; set; } = false;

        [StringLength(256)]
        [DisplayName("Remark")]
        public string Remark { get; set; } = string.Empty;

        [Required]
        [DisplayName("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [DisplayName("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [StringLength(32)]
        [DisplayName("PinyinCode")]
        public string PinyinCode { get; set; } = string.Empty;

        [StringLength(32)]
        [DisplayName("WuBiCode")]
        public string WuBiCode { get; set; } = string.Empty;

        /// <summary>
        /// 允许查看该特殊病人的医生列表（仅IsSpecial为true时有效）
        /// </summary>
        [DisplayName("允许查看该特殊病人的医生列表（仅IsSpecial为true时有效）")]
        public virtual ICollection<SpecialPatientDoctor> SpecialPatientDoctors { get; set; } = new List<SpecialPatientDoctor>();
    }
}