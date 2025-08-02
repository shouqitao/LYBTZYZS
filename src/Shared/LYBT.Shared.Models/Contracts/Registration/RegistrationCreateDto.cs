using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Registration {

    /// <summary>
    /// 新增挂号 DTO
    /// </summary>
    public class RegistrationCreateDto {

        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        [DisplayName("病人ID")]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>挂号类型（如"普通"、"急诊"）</summary>
        [Required(ErrorMessage = "挂号类型不能为空")]
        [DisplayName("挂号类型（如\"普通\"、\"急诊\"）")]
        public string RegistrationType { get; set; } = "普通";

        /// <summary>挂号时间</summary>
        [DisplayName("挂号时间")]
        public DateTime RegistrationTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}