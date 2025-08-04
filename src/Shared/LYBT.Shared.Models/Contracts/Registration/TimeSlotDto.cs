namespace LYBT.Shared.Models.Contracts.Registration
{
    /// <summary>
    /// 时间段DTO
    /// </summary>
    public class TimeSlotDto
    {
        /// <summary>
        /// 时间段ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 结束时间
        /// </summary>
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 最大预约数
        /// </summary>
        public int MaxCount { get; set; }

        /// <summary>
        /// 已预约数
        /// </summary>
        public int BookedCount { get; set; }

        /// <summary>
        /// 是否可预约
        /// </summary>
        public bool IsAvailable => BookedCount < MaxCount;
    }
}