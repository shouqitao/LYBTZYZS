namespace LYBT.Module.Queueing.Dtos {

    /// <summary>
    /// 排队列表 DTO
    /// </summary>
    public class QueueingDto {

        /// <summary>排队ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>排队类型</summary>
        public string QueueType { get; set; } = string.Empty;

        /// <summary>排队时间</summary>
        public DateTime QueueTime { get; set; }

        /// <summary>状态</summary>
        public string Status { get; set; } = string.Empty;
    }
}