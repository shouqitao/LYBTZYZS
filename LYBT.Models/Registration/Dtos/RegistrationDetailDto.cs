using System.ComponentModel;
namespace LYBT.Module.Registration.Dtos {

    /// <summary>
    /// 挂号详情 DTO
    /// </summary>
    public class RegistrationDetailDto {

        /// <summary>挂号ID</summary>
        [DisplayName("挂号ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
/// <summary>
/// PatientName 属性。
/// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>挂号类型</summary>
        [DisplayName("挂号类型")]
/// <summary>
/// RegistrationType 属性。
/// </summary>
        public string RegistrationType { get; set; } = string.Empty;

        /// <summary>挂号时间</summary>
        [DisplayName("挂号时间")]
/// <summary>
/// RegistrationTime 属性。
/// </summary>
        public DateTime RegistrationTime { get; set; }

        /// <summary>状态（如“待看诊”、“已完成”、“已取消”）</summary>
        [DisplayName("状态（如“待看诊”、“已完成”、“已取消”）")]
/// <summary>
/// Status 属性。
/// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
