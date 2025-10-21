using System;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 处方完成事件载荷
    /// </summary>
    public class PrescriptionCompletedPayload
    {
        /// <summary>
        /// 处方ID（后端创建后返回）
        /// </summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>
        /// 医案流程ID
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 处方药品总数
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// 处方总金额
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 是否保存为草稿
        /// </summary>
        public bool IsDraft { get; set; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
