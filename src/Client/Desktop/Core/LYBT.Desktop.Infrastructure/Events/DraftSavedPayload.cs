namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 草稿保存事件载荷
    /// </summary>
    public class DraftSavedPayload
    {
        /// <summary>
        /// 医案流程ID
        /// </summary>
        public Guid MedicalCaseFlowId { get; set; }

        /// <summary>
        /// 当前步骤（1-4）
        /// </summary>
        public int CurrentStep { get; set; }

        /// <summary>
        /// 草稿数据快照（JSON序列化）
        /// </summary>
        public required string DraftDataSnapshot { get; set; }

        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
