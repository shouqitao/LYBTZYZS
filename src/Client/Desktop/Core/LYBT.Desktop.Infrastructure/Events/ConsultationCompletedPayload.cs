namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 诊断完成事件载荷
    /// Epic #2210 Phase 4: 用于4:6统一工作区的诊断面板与处方面板通信
    /// </summary>
    public class ConsultationCompletedPayload
    {
        /// <summary>
        /// 医案ID
        /// </summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 是否需要开处方
        /// </summary>
        public bool NeedsPrescription { get; set; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
