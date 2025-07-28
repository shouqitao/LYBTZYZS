using LYBT.Common.Enums.Doctors;
using LYBT.Common.Enums.System;
using LYBT.Models.Patients;
using LYBT.Models.Users;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Doctors {

    /// <summary>
    /// 医生领域实体
    /// </summary>
    public class DoctorModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required]
        [DisplayName("Gender")]
        public Gender Gender { get; set; } = Gender.Unknown;

        [DisplayName("Age")]
        public int Age { get; set; } = 0;

        [Required]
        [DisplayName("Title")]
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        [StringLength(64)]
        [DisplayName("Specialty")]
        public string Specialty { get; set; } = string.Empty;

        [Required]
        [DisplayName("Status")]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        [Required]
        [DisplayName("WorkStatus")]
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        [Required]
        [DisplayName("CreatedTime")]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        [StringLength(256)]
        [DisplayName("Remark")]
        public string? Remark { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        [DisplayName("出生日期")]
        public DateTime? Birthday { get; set; }

        [StringLength(32)]
        [DisplayName("LicenseNumber")]
        public string? LicenseNumber { get; set; }

        [StringLength(32)]
        [DisplayName("PinyinCode")]
        public string PinyinCode { get; set; } = string.Empty;

        [StringLength(32)]
        [DisplayName("ContactNumber")]
        public string? ContactNumber { get; set; } // 医生对外联系方式

        [Required]
        [DisplayName("UserId")]
        public Guid UserId { get; set; }

        [Required]
        [DisplayName("User")]
        public virtual UserModel User { get; set; } = null!;

        /// <summary>
        /// 授权可查看的特殊病人关系集合
        /// </summary>
        [DisplayName("授权可查看的特殊病人关系集合")]
        public virtual ICollection<SpecialPatientDoctor> SpecialPatientPatients { get; set; } = new List<SpecialPatientDoctor>();
    }
}