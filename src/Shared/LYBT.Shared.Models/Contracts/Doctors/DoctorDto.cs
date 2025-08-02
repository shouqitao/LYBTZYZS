using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Doctors {

    /// <summary>
    /// 医生列表 DTO（仅包含医生专属字段和UserId，姓名等通过User获取）
    /// </summary>
    public class DoctorDto {

        [DisplayName("Id")]
        public Guid Id { get; set; }

        [DisplayName("UserId")]
        public Guid UserId { get; set; }

        [DisplayName("Birthday")]
        public DateTime? Birthday { get; set; }

        [DisplayName("Title")]
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        [DisplayName("LicenseNumber")]
        public string? LicenseNumber { get; set; }

        /// <summary>
        /// 身份证号码
        /// </summary>
        [DisplayName("身份证号码")]
        public string? IdNumber { get; set; }

        [DisplayName("Specialty")]
        public string Specialty { get; set; } = string.Empty;

        [DisplayName("Status")]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        [DisplayName("WorkStatus")]
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        [DisplayName("PinyinCode")]
        public string PinyinCode { get; set; } = string.Empty;

        [DisplayName("Remark")]
        public string? Remark { get; set; }

        [DisplayName("ContactNumber")]
        public string? ContactNumber { get; set; } // 医生对外联系方式

        // 用户信息（只读）
        [DisplayName("UserName")]
        public string? UserName { get; set; }

        [DisplayName("RealName")]
        public string? RealName { get; set; }

        [DisplayName("PhoneNumber")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [DisplayName("年龄")]
        public int? Age { get; set; }
    }
}