using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LYBT.Desktop.Contracts.Performance
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

        /// <summary>
        /// 获取格式化的报告文本
        /// </summary>
        public string GetFormattedReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═".PadRight(60, '═'));
            sb.AppendLine("  性能监控报告");
            sb.AppendLine("═".PadRight(60, '═'));
            sb.AppendLine($"  生成时间: {GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  监控操作数: {Metrics.Count}");
            sb.AppendLine($"  总耗时: {TotalDurationMs / 1000.0:F2}s");
            sb.AppendLine($"  平均耗时: {AverageDurationMs:F0}ms");
            sb.AppendLine($"  内存增量: {FormatBytes(TotalMemoryDeltaBytes)}");
            sb.AppendLine("─".PadRight(60, '─'));

            if (SlowestOperation != null)
            {
                sb.AppendLine($"  最慢操作: {SlowestOperation.OperationName} ({SlowestOperation.FormattedDuration})");
                sb.AppendLine("─".PadRight(60, '─'));
            }

            // 按性能等级显示分布
            sb.AppendLine("  性能分布:");
            foreach (var level in Enum.GetValues<PerformanceLevel>())
            {
                var count = LevelDistribution.GetValueOrDefault(level, 0);
                var percentage = Metrics.Count > 0 ? (count * 100.0 / Metrics.Count) : 0;
                var levelDesc = PerformanceThresholds.GetLevelDescription(level);
                var indicator = GetLevelIndicator(level);
                sb.AppendLine($"    {indicator} {levelDesc,-8}: {count,3} ({percentage,5:F1}%)");
            }

            sb.AppendLine("─".PadRight(60, '─'));
            sb.AppendLine("  详细指标:");
            sb.AppendLine("─".PadRight(60, '─'));

            foreach (var metric in Metrics.OrderBy(m => m.Timestamp))
            {
                var indicator = GetLevelIndicator(metric.Level);
                sb.AppendLine($"  {indicator} {metric.OperationName,-30} {metric.FormattedDuration,10} {metric.FormattedMemoryDelta,12}");
            }

            sb.AppendLine("═".PadRight(60, '═'));

            return sb.ToString();
        }

        /// <summary>
        /// 获取JSON格式的报告（便于日志分析）
        /// </summary>
        public string GetJsonReport()
        {
            var entries = Metrics.Select(m => new
            {
                m.OperationName,
                m.DurationMs,
                m.MemoryBeforeBytes,
                m.MemoryAfterBytes,
                m.MemoryDeltaBytes,
                Timestamp = m.Timestamp.ToString("O"),
                Level = m.Level.ToString()
            });

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                GeneratedAt,
                Summary = new
                {
                    TotalOperations = Metrics.Count,
                    TotalDurationMs,
                    TotalMemoryDeltaBytes,
                    AverageDurationMs
                },
                LevelDistribution,
                Metrics = entries
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        private static string GetLevelIndicator(PerformanceLevel level) => level switch
        {
            PerformanceLevel.Excellent => "✓",
            PerformanceLevel.Good => "○",
            PerformanceLevel.Acceptable => "△",
            PerformanceLevel.Poor => "✗",
            _ => "?"
        };

        private static string FormatBytes(long bytes)
        {
            return bytes switch
            {
                >= 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F2}GB",
                >= 1024L * 1024 => $"{bytes / (1024.0 * 1024):F2}MB",
                >= 1024L => $"{bytes / 1024.0:F2}KB",
                _ => $"{bytes}B"
            };
        }
    }
}
