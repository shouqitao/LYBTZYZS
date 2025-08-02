using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Models.Queueing {

    /// <summary>
    /// 排队主表实体
    /// </summary>
    public class QueueingModel {

        /// <summary>
        /// 排队ID（主键）
        /// </summary>
        [DisplayName("排队ID（主键）")]
        public Guid Id { get; set; }

        /// <summary>
        /// 病人ID
        /// </summary>
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>
        /// 病人姓名（仅用于快速展示）
        /// </summary>
        [DisplayName("病人姓名（仅用于快速展示）")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 医生ID
        /// </summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 医生姓名
        /// </summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 排队类型（如“普通”、“急诊”）
        /// </summary>
        [DisplayName("排队类型（如“普通”、“急诊”）")]
        public string QueueType { get; set; } = "普通";

        /// <summary>
        /// 排队时间
        /// </summary>
        [DisplayName("排队时间")]
        public DateTime QueueTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 当前状态（如“排队中”、“已叫号”、“已就诊”、“已取消”）
        /// </summary>
        [DisplayName("当前状态（如“排队中”、“已叫号”、“已就诊”、“已取消”）")]
        public QueueStatus Status { get; set; } = QueueStatus.Waiting;

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}