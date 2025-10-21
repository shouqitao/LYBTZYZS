using System;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 流程取消事件载荷
    /// </summary>
    public class MedicalCaseFlowCancelledPayload
    {
        /// <summary>
        /// 医案流程ID
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 取消原因
        /// </summary>
        public required string CancelReason { get; set; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
