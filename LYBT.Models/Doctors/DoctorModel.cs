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
        [DisplayName("医生ID")]
        public Guid Id { get; set; }

        [Required]
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        [Required]
        [DisplayName("职称")]
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        [StringLength(64)]
        [DisplayName("专科")]
        public string Specialty { get; set; } = string.Empty;

        [Required]
        [DisplayName("在职状态")]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        [Required]
        [DisplayName("工作状态")]
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        [Required]
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        [StringLength(256)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        [Required]
        [DisplayName("出生日期")]
        public DateTime Birthday { get; set; }

        [StringLength(32)]
        [DisplayName("执业证号")]
        public string? LicenseNumber { get; set; }

        [StringLength(32)]
        [DisplayName("拼音码")]
        public string PinyinCode { get; set; } = string.Empty;

        [StringLength(32)]
        [DisplayName("联系电话")]
        public string? ContactNumber { get; set; }

        [Required]
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        [Required]
        [DisplayName("关联用户")]
        public virtual UserModel User { get; set; } = null!;

        /// <summary>
        /// 计算年龄
        /// </summary>
        public int Age {
            get {
                var today = DateTime.Today;
                var age = today.Year - Birthday.Year;
                if (Birthday.Date > today.AddYears(-age))
                    age--;
                return age;
            }
        }

        /// <summary>
        /// 特殊患者关系集合
        /// </summary>
        [DisplayName("特殊患者关系")]
        public virtual ICollection<SpecialPatientDoctor> SpecialPatients { get; set; } = new List<SpecialPatientDoctor>();
    }
}