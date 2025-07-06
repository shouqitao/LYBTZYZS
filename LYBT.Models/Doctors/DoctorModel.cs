using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums;
using LYBT.Module.Users.Models;
using LYBT.Models.Patients;

namespace LYBT.Models.Doctors {
    /// <summary>
    /// 医生领域实体
    /// </summary>
    public class DoctorModel {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Gender Gender { get; set; } = Gender.Unknown;

        public int Age { get; set; } = 0;

        [Required]
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        [StringLength(64)]
        public string Specialty { get; set; } = string.Empty;

        [Required]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        [Required]
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        [Required]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        [StringLength(256)]
        public string Remark { get; set; } = string.Empty;

        public DateTime Birthday { get; set; }

        [StringLength(32)]
        public string? LicenseNumber { get; set; }

        [StringLength(32)]
        public string PinyinCode { get; set; } = string.Empty;

        [StringLength(32)]
        public string? ContactNumber { get; set; } // 医生对外联系方式

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public virtual UserModel User { get; set; } = null!;

        /// <summary>
        /// 授权可查看的特殊病人关系集合
        /// </summary>
        public virtual ICollection<SpecialPatientDoctor> SpecialPatientPatients { get; set; } = new List<SpecialPatientDoctor>();
    }
}