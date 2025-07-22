using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Registration.Dtos {

    /// <summary>
    /// 新增挂号 DTO
    /// </summary>
    public class RegistrationCreateDto {

        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        [DisplayName("病人ID")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>挂号类型（如“普通”、“急诊”）</summary>
        [Required(ErrorMessage = "挂号类型不能为空")]
        [DisplayName("挂号类型（如“普通”、“急诊”）")]
/// <summary>
/// RegistrationType 属性。
/// </summary>
        public string RegistrationType { get; set; } = "普通";

        /// <summary>挂号时间</summary>
        [DisplayName("挂号时间")]
/// <summary>
/// RegistrationTime 属性。
/// </summary>
        public DateTime RegistrationTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
