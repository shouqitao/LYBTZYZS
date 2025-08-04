using System;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 看诊统计DTO
    /// </summary>
    public class ConsultationStatisticsDto
    {
        /// <summary>今日看诊数</summary>
        public int TodayCount { get; set; }

        /// <summary>待诊人数</summary>
        public int WaitingCount { get; set; }

        /// <summary>已完成人数</summary>
        public int CompletedCount { get; set; }

        /// <summary>取消人数</summary>
        public int CancelledCount { get; set; }

        /// <summary>今日收入</summary>
        public decimal TodayIncome { get; set; }

        /// <summary>本周看诊数</summary>
        public int WeekCount { get; set; }

        /// <summary>本月看诊数</summary>
        public int MonthCount { get; set; }

        /// <summary>平均看诊时长（分钟）</summary>
        public double AverageConsultationTime { get; set; }
    }
}