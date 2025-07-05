using System;
using LYBT.Common.Enums;

namespace LYBT.Module.Doctors.Dtos {
    /// <summary>
    /// 医生列表 DTO（仅包含医生专属字段和UserId，姓名等通过User获取）
    /// </summary>
    public class DoctorDto {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime? Birthday { get; set; }
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;
        public string? LicenseNumber { get; set; }
        public string Specialty { get; set; } = string.Empty;
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;
        public string PinyinCode { get; set; } = string.Empty;
        public string? Remark { get; set; }
    }
}