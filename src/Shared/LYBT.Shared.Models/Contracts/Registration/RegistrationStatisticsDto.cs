namespace LYBT.Shared.Models.Contracts.Registration
{
    /// <summary>
    /// 挂号统计DTO
    /// </summary>
    public class RegistrationStatisticsDto
    {
        /// <summary>
        /// 总挂号数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 已完成数
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// 等待中数
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// 已取消数
        /// </summary>
        public int CancelledCount { get; set; }

        /// <summary>
        /// 爽约数
        /// </summary>
        public int NoShowCount { get; set; }

        /// <summary>
        /// 普通号数
        /// </summary>
        public int NormalCount { get; set; }

        /// <summary>
        /// 专家号数
        /// </summary>
        public int ExpertCount { get; set; }

        /// <summary>
        /// 急诊号数
        /// </summary>
        public int EmergencyCount { get; set; }

        /// <summary>
        /// VIP号数
        /// </summary>
        public int VIPCount { get; set; }

        /// <summary>
        /// 总收入
        /// </summary>
        public decimal TotalIncome { get; set; }
    }
}