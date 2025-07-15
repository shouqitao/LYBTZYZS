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

        [Required]
        [DisplayName("Status")]
        public PatientStatus Status { get; set; } = PatientStatus.Active;

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

        /// <summary>
        /// 允许查看该特殊病人的医生列表（仅IsSpecial为true时有效）
        /// </summary>
        [DisplayName("允许查看该特殊病人的医生列表（仅IsSpecial为true时有效）")]
        public virtual ICollection<SpecialPatientDoctor> SpecialPatientDoctors { get; set; } = new List<SpecialPatientDoctor>();
    }
}