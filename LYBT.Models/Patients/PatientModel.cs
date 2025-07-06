using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums;
using LYBT.Common.Enums.Patient;

namespace LYBT.Models.Patients {
    /// <summary>
    /// 患者信息实体
    /// </summary>
    public class PatientModel {
        [Key]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Gender Gender { get; set; }

        public int? Age { get; set; }

        [Required, StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, StringLength(32)]
        public string IDNumber { get; set; } = string.Empty;

        [StringLength(256)]
        public string Address { get; set; } = string.Empty;

        [Required]
        public PatientStatus Status { get; set; } = PatientStatus.Active;

        [StringLength(128)]
        public string DisableReason { get; set; } = string.Empty;

        /// <summary>
        /// 是否为特殊病人（前台不可见，仅特定医生可见）
        /// </summary>
        public bool IsSpecial { get; set; } = false;

        [StringLength(256)]
        public string Remark { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [StringLength(32)]
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>
        /// 允许查看该特殊病人的医生列表（仅IsSpecial为true时有效）
        /// </summary>
        public virtual ICollection<SpecialPatientDoctor> SpecialPatientDoctors { get; set; } = new List<SpecialPatientDoctor>();
    }
}