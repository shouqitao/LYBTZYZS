namespace LYBT.Module.Registration.Dtos {

    /// <summary>
    /// 挂号列表 DTO
    /// </summary>
    public class RegistrationDto {

        /// <summary>挂号ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>挂号类型</summary>
        public string RegistrationType { get; set; } = string.Empty;

        /// <summary>挂号时间</summary>
        public DateTime RegistrationTime { get; set; }

        /// <summary>状态</summary>
        public string Status { get; set; } = string.Empty;
    }
}