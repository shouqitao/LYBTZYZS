using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Doctors {

    /// <summary>
    /// 医生详情 DTO（简化版）
    /// </summary>
    public class DoctorDetailDto {

        [DisplayName("医生ID")]
        public Guid Id { get; set; }

        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        [DisplayName("医生姓名")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("专长")]
        public string Specialty { get; set; } = string.Empty;

        [DisplayName("挂号费")]
        public decimal RegistrationFee { get; set; }

        [DisplayName("执业证书号")]
        public string LicenseNumber { get; set; } = string.Empty;

        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        [DisplayName("联系电话")]
        public string? ContactNumber { get; set; }

        [DisplayName("简介")]
        public string? Introduction { get; set; }

        [DisplayName("状态")]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        // 关联的用户信息（如果需要）
        [DisplayName("用户名")]
        public string? Username { get; set; }

        // 统计信息（只读）
        [DisplayName("今日接诊数")]
        public int TodayPatientCount { get; set; }

        [DisplayName("总接诊数")]
        public int TotalPatientCount { get; set; }
    }
}