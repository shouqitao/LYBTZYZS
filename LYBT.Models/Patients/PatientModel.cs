using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums;
using LYBT.Common.Enums.Patient;
using System.ComponentModel;

namespace LYBT.Models.Patients {
    /// <summary>
    /// 患者信息实体
    /// </summary>
    public class PatientModel {
        [Key]
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("Name")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;

        [Required]
        [DisplayName("Gender")]
/// <summary>
/// Gender 属性。
/// </summary>
        public Gender Gender { get; set; }

        [DisplayName("Age")]
/// <summary>
/// Age 属性。
/// </summary>
        public int? Age { get; set; }

        [Required, StringLength(20)]
        [DisplayName("PhoneNumber")]
/// <summary>
/// PhoneNumber 属性。
/// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, StringLength(32)]
        [DisplayName("IDNumber")]
/// <summary>
/// IDNumber 属性。
/// </summary>
        public string IDNumber { get; set; } = string.Empty;

        [StringLength(256)]
        [DisplayName("Address")]
/// <summary>
/// Address 属性。
/// </summary>
        public string Address { get; set; } = string.Empty;

        [Required]
        [DisplayName("Status")]
/// <summary>
/// Status 属性。
/// </summary>
        public PatientStatus Status { get; set; } = PatientStatus.Active;

        [StringLength(128)]
        [DisplayName("DisableReason")]
/// <summary>
/// DisableReason 属性。
/// </summary>
        public string DisableReason { get; set; } = string.Empty;

        /// <summary>
        /// 是否为特殊病人（前台不可见，仅特定医生可见）
        /// </summary>
        [DisplayName("是否为特殊病人（前台不可见，仅特定医生可见）")]
/// <summary>
/// IsSpecial 属性。
/// </summary>
        public bool IsSpecial { get; set; } = false;

        [StringLength(256)]
        [DisplayName("Remark")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string Remark { get; set; } = string.Empty;

        [Required]
        [DisplayName("CreatedAt")]
/// <summary>
/// CreatedAt 属性。
/// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [DisplayName("UpdatedAt")]
/// <summary>
/// UpdatedAt 属性。
/// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [StringLength(32)]
        [DisplayName("PinyinCode")]
/// <summary>
/// PinyinCode 属性。
/// </summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>
        /// 允许查看该特殊病人的医生列表（仅IsSpecial为true时有效）
        /// </summary>
        [DisplayName("允许查看该特殊病人的医生列表（仅IsSpecial为true时有效）")]
/// <summary>
/// SpecialPatientDoctors 属性。
/// </summary>
        public virtual ICollection<SpecialPatientDoctor> SpecialPatientDoctors { get; set; } = new List<SpecialPatientDoctor>();
    }
}
