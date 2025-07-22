using System;
using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Module.Doctors.Dtos {
    /// <summary>
    /// 医生列表 DTO（仅包含医生专属字段和UserId，姓名等通过User获取）
    /// </summary>
    public class DoctorDto {
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }
        [DisplayName("UserId")]
/// <summary>
/// UserId 属性。
/// </summary>
        public Guid UserId { get; set; }
        [DisplayName("Birthday")]
/// <summary>
/// Birthday 属性。
/// </summary>
        public DateTime? Birthday { get; set; }
        [DisplayName("Title")]
/// <summary>
/// Title 属性。
/// </summary>
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;
        [DisplayName("LicenseNumber")]
/// <summary>
/// LicenseNumber 属性。
/// </summary>
        public string? LicenseNumber { get; set; }
        [DisplayName("Specialty")]
/// <summary>
/// Specialty 属性。
/// </summary>
        public string Specialty { get; set; } = string.Empty;
        [DisplayName("Status")]
/// <summary>
/// Status 属性。
/// </summary>
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;
        [DisplayName("WorkStatus")]
/// <summary>
/// WorkStatus 属性。
/// </summary>
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;
        [DisplayName("PinyinCode")]
/// <summary>
/// PinyinCode 属性。
/// </summary>
        public string PinyinCode { get; set; } = string.Empty;
        [DisplayName("Remark")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
        [DisplayName("ContactNumber")]
/// <summary>
/// ContactNumber 属性。
/// </summary>
        public string? ContactNumber { get; set; } // 医生对外联系方式
        // 用户信息（只读）
        [DisplayName("UserName")]
/// <summary>
/// UserName 属性。
/// </summary>
        public string? UserName { get; set; }
        [DisplayName("RealName")]
/// <summary>
/// RealName 属性。
/// </summary>
        public string? RealName { get; set; }
        [DisplayName("PhoneNumber")]
/// <summary>
/// PhoneNumber 属性。
/// </summary>
        public string? PhoneNumber { get; set; }
        [DisplayName("Gender")]
/// <summary>
/// Gender 属性。
/// </summary>
        public Gender Gender { get; set; } // 新增性别字段
    }
}
