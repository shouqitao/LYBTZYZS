using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Registration.Dtos {

    /// <summary>
    /// 新增挂号 DTO
    /// </summary>
    public class RegistrationCreateDto {

        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>挂号类型（如“普通”、“急诊”）</summary>
        [Required(ErrorMessage = "挂号类型不能为空")]
        public string RegistrationType { get; set; } = "普通";

        /// <summary>挂号时间</summary>
        public DateTime RegistrationTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}