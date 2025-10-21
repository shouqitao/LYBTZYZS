using System;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 诊断完成事件载荷
    /// </summary>
    public class ConsultationCompletedPayload
    {
        /// <summary>
        /// 诊断ID（后端创建后返回）
        /// </summary>
        public Guid ConsultationId { get; set; }

        /// <summary>
        /// 医案流程ID
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 主诉（简要，用于显示）
        /// </summary>
        public required string ChiefComplaint { get; set; }

        /// <summary>
        /// 诊断结果（简要，用于显示）
        /// </summary>
        public required string Diagnosis { get; set; }

        /// <summary>
        /// 是否保存为草稿（true=草稿，false=正式保存）
        /// </summary>
        public bool IsDraft { get; set; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
