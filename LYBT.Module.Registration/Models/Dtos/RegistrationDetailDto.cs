using System.ComponentModel;

namespace LYBT.Module.Registration.Models.Dtos {

    /// <summary>
    /// 挂号详情 DTO
    /// </summary>
    public class RegistrationDetailDto {

        /// <summary>挂号ID</summary>
        [DisplayName("挂号ID")]
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>挂号类型</summary>
        [DisplayName("挂号类型")]
        public string RegistrationType { get; set; } = string.Empty;

        /// <summary>挂号时间</summary>
        [DisplayName("挂号时间")]
        public DateTime RegistrationTime { get; set; }

        /// <summary>状态（如“待看诊”、“已完成”、“已取消”）</summary>
        [DisplayName("状态（如“待看诊”、“已完成”、“已取消”）")]
        public string Status { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}