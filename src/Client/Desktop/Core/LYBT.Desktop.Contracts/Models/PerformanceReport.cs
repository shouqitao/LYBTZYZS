using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LYBT.Desktop.Contracts.Models
{
    /// <summary>
    /// 性能报告
    /// </summary>
    public record PerformanceReport
    {
        /// <summary>
        /// 报告生成时间
        /// </summary>
        public required DateTime GeneratedAt { get; init; }

        /// <summary>
        /// 报告包含的所有性能指标
        /// </summary>
        public required IReadOnlyCollection<PerformanceMetric> Metrics { get; init; }

        /// <summary>
        /// 总耗时（毫秒）
        /// </summary>
        public long TotalDurationMs => Metrics.Sum(m => m.DurationMs);

        /// <summary>
        /// 总内存增量（字节）
        /// </summary>
        public long TotalMemoryDeltaBytes => Metrics.Sum(m => m.MemoryDeltaBytes);

        /// <summary>
        /// 平均操作耗时（毫秒）
        /// </summary>
        public double AverageDurationMs => Metrics.Count > 0 ? Metrics.Average(m => m.DurationMs) : 0;

        /// <summary>
        /// 最慢的操作
        /// </summary>
        public PerformanceMetric? SlowestOperation => Metrics.OrderByDescending(m => m.DurationMs).FirstOrDefault();

        /// <summary>
        /// 按性能等级分组的统计
        /// </summary>
        public Dictionary<PerformanceLevel, int> LevelDistribution => Metrics
            .GroupBy(m => m.Level)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
