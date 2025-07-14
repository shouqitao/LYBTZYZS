namespace LYBT.Module.Registration.Dtos {

    /// <summary>
    /// 挂号详情 DTO
    /// </summary>
    public class RegistrationDetailDto {

        /// <summary>挂号ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>挂号类型</summary>
        public string RegistrationType { get; set; } = string.Empty;

        /// <summary>挂号时间</summary>
        public DateTime RegistrationTime { get; set; }

        /// <summary>状态（如“待看诊”、“已完成”、“已取消”）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}