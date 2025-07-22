using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums;
using LYBT.Module.Users.Models;
using LYBT.Models.Patients;
using System.ComponentModel;

namespace LYBT.Models.Doctors {
    /// <summary>
    /// 医生领域实体
    /// </summary>
    public class DoctorModel {
        [Key]
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        [Required]
        [DisplayName("Gender")]
/// <summary>
/// Gender 属性。
/// </summary>
        public Gender Gender { get; set; } = Gender.Unknown;

        [DisplayName("Age")]
/// <summary>
/// Age 属性。
/// </summary>
        public int Age { get; set; } = 0;

        [Required]
        [DisplayName("Title")]
/// <summary>
/// Title 属性。
/// </summary>
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        [StringLength(64)]
        [DisplayName("Specialty")]
/// <summary>
/// Specialty 属性。
/// </summary>
        public string Specialty { get; set; } = string.Empty;

        [Required]
        [DisplayName("Status")]
/// <summary>
/// Status 属性。
/// </summary>
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        [Required]
        [DisplayName("WorkStatus")]
/// <summary>
/// WorkStatus 属性。
/// </summary>
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        [Required]
        [DisplayName("CreatedTime")]
/// <summary>
/// CreatedTime 属性。
/// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        [StringLength(256)]
        [DisplayName("Remark")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }

        [DisplayName("Birthday")]
/// <summary>
/// Birthday 属性。
/// </summary>
        public DateTime Birthday { get; set; }

        [StringLength(32)]
        [DisplayName("LicenseNumber")]
/// <summary>
/// LicenseNumber 属性。
/// </summary>
        public string? LicenseNumber { get; set; }

        [StringLength(32)]
        [DisplayName("PinyinCode")]
/// <summary>
/// PinyinCode 属性。
/// </summary>
        public string PinyinCode { get; set; } = string.Empty;

        [StringLength(32)]
        [DisplayName("ContactNumber")]
/// <summary>
/// ContactNumber 属性。
/// </summary>
        public string? ContactNumber { get; set; } // 医生对外联系方式

        [Required]
        [DisplayName("UserId")]
/// <summary>
/// UserId 属性。
/// </summary>
        public Guid UserId { get; set; }

        [Required]
        [DisplayName("User")]
/// <summary>
/// User 属性。
/// </summary>
        public virtual UserModel User { get; set; } = null!;

        /// <summary>
        /// 授权可查看的特殊病人关系集合
        /// </summary>
        [DisplayName("授权可查看的特殊病人关系集合")]
/// <summary>
/// SpecialPatientPatients 属性。
/// </summary>
        public virtual ICollection<SpecialPatientDoctor> SpecialPatientPatients { get; set; } = new List<SpecialPatientDoctor>();
    }
}
