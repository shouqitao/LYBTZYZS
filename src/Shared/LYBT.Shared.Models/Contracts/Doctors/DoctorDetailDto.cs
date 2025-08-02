using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Doctors {

    /// <summary>
    /// 医生详情 DTO（仅包含医生专属字段和UserId，姓名等通过User获取）
    /// </summary>
    public class DoctorDetailDto {

        [DisplayName("Id")]
        public Guid Id { get; set; }

        [DisplayName("UserId")]
        public Guid UserId { get; set; }

        [DisplayName("Gender")]
        public Gender Gender { get; set; } = Gender.Unknown;

        [DisplayName("Birthday")]
        public DateTime? Birthday { get; set; }

        [DisplayName("Title")]
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        [DisplayName("LicenseNumber")]
        public string? LicenseNumber { get; set; }

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

        // 关联的用户信息（只读）
        [DisplayName("UserName")]
        public string? UserName { get; set; }

        [DisplayName("RealName")]
        public string? RealName { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [DisplayName("联系电话")]
        public string? PhoneNumber { get; set; }

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