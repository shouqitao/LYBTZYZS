namespace LYBT.Module.Queueing.Dtos {

    /// <summary>
    /// 排队详情 DTO
    /// </summary>
    public class QueueingDetailDto {

        /// <summary>排队ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>排队类型</summary>
        public string QueueType { get; set; } = string.Empty;

        /// <summary>排队时间</summary>
        public DateTime QueueTime { get; set; }

        /// <summary>当前状态（如“排队中”、“已叫号”、“已就诊”、“已取消”）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}