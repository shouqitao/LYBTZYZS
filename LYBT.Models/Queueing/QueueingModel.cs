using LYBT.Common.Enums;

namespace LYBT.Models.Queueing {

    /// <summary>
    /// 排队主表实体
    /// </summary>
    public class QueueingModel {

        /// <summary>
        /// 排队ID（主键）
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 病人ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 病人姓名（仅用于快速展示）
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 排队类型（如“普通”、“急诊”）
        /// </summary>
        public string QueueType { get; set; } = "普通";

        /// <summary>
        /// 排队时间
        /// </summary>
        public DateTime QueueTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 当前状态（如“排队中”、“已叫号”、“已就诊”、“已取消”）
        /// </summary>
        public QueueStatus Status { get; set; } = QueueStatus.Waiting;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}