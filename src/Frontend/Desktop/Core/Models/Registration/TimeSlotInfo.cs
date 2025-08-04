namespace LYBT.WPF.Client.Core.Models.Registration
{
    /// <summary>
    /// 时间段信息
    /// </summary>
    public class TimeSlotInfo
    {
        /// <summary>
        /// 时间段ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// 时间段显示名称
        /// </summary>
        public string DisplayName => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

        /// <summary>
        /// 最大可预约数量
        /// </summary>
        public int MaxCount { get; set; }

        /// <summary>
        /// 已预约数量
        /// </summary>
        public int BookedCount { get; set; }

        /// <summary>
        /// 剩余可预约数量
        /// </summary>
        public int AvailableCount => MaxCount - BookedCount;

        /// <summary>
        /// 是否可预约
        /// </summary>
        public bool IsAvailable => AvailableCount > 0;

        /// <summary>
        /// 是否已满
        /// </summary>
        public bool IsFull => AvailableCount <= 0;
    }
}