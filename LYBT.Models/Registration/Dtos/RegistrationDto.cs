using System.ComponentModel;
namespace LYBT.Module.Registration.Dtos {

    /// <summary>
    /// 挂号列表 DTO
    /// </summary>
    public class RegistrationDto {

        /// <summary>挂号ID</summary>
        [DisplayName("挂号ID")]
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>挂号类型</summary>
        [DisplayName("挂号类型")]
        public string RegistrationType { get; set; } = string.Empty;

        /// <summary>挂号时间</summary>
        [DisplayName("挂号时间")]
        public DateTime RegistrationTime { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public string Status { get; set; } = string.Empty;
    }
}