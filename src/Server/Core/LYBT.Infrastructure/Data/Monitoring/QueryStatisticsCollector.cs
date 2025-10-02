using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Data.Monitoring
{
    /// <summary>
    /// 查询统计信息
    /// </summary>
    public class QueryStatistics
    {
        public string QueryPattern { get; set; } = string.Empty;
        public int ExecutionCount { get; set; }
        public double TotalDurationMs { get; set; }
        public double AverageDurationMs => ExecutionCount > 0 ? TotalDurationMs / ExecutionCount : 0;
        public double MaxDurationMs { get; set; }
        public double MinDurationMs { get; set; } = double.MaxValue;
        public DateTime FirstExecutedAt { get; set; }
        public DateTime LastExecutedAt { get; set; }
        public List<string> SlowExecutions { get; set; } = new();
    }

    /// <summary>
    /// 查询统计收集器
    /// 用于收集和分析查询性能数据，识别性能瓶颈
    /// </summary>
    public class QueryStatisticsCollector : IQueryStatisticsCollector
    {
        private readonly ConcurrentDictionary<string, QueryStatistics> _statistics = new();
        private readonly ILogger<QueryStatisticsCollector> _logger;
        private readonly int _maxSlowExecutionsPerQuery = 10;
        private readonly object _lockObject = new();

        public QueryStatisticsCollector(ILogger<QueryStatisticsCollector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 记录查询执行
        /// </summary>
        public void RecordQueryExecution(string commandText, double durationMs, bool isSlowQuery)
        {
            var queryPattern = ExtractQueryPattern(commandText);

            _statistics.AddOrUpdate(queryPattern,
                // 新增
                key =>
                {
                    var stats = new QueryStatistics
                    {
                        QueryPattern = queryPattern,
                        ExecutionCount = 1,
                        TotalDurationMs = durationMs,
                        MaxDurationMs = durationMs,
                        MinDurationMs = durationMs,
                        FirstExecutedAt = DateTime.UtcNow,
                        LastExecutedAt = DateTime.UtcNow
                    };

                    if (isSlowQuery)
                    {
                        stats.SlowExecutions.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {durationMs:F2}ms");
                    }

                    return stats;
                },
                // 更新
                (key, existing) =>
                {
                    lock (_lockObject)
                    {
                        existing.ExecutionCount++;
                        existing.TotalDurationMs += durationMs;
                        existing.MaxDurationMs = Math.Max(existing.MaxDurationMs, durationMs);
                        existing.MinDurationMs = Math.Min(existing.MinDurationMs, durationMs);
                        existing.LastExecutedAt = DateTime.UtcNow;

                        if (isSlowQuery && existing.SlowExecutions.Count < _maxSlowExecutionsPerQuery)
                        {
                            existing.SlowExecutions.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {durationMs:F2}ms");
                        }
                    }
                    return existing;
                });

            // 检测N+1模式
            DetectN1Pattern(queryPattern);
        }

        /// <summary>
        /// 提取查询模式（移除参数值，保留结构）
        /// </summary>
        private string ExtractQueryPattern(string commandText)
        {
            // 简化SQL，移除参数值，保留结构
            var pattern = System.Text.RegularExpressions.Regex.Replace(
                commandText,
                @"@p\d+|@__[\w_]+_\d+|'[^']*'|\b\d+\b",
                "?");

            // 移除多余空格
            pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"\s+", " ");

            // 截断过长的模式
            if (pattern.Length > 500)
            {
                pattern = pattern.Substring(0, 497) + "...";
            }

            return pattern.Trim();
        }

        /// <summary>
        /// 检测N+1查询模式
        /// </summary>
        private void DetectN1Pattern(string queryPattern)
        {
            var stats = _statistics.GetValueOrDefault(queryPattern);
            if (stats == null) return;

            // 如果相同模式的查询在短时间内执行多次，可能是N+1
            var timeDiff = (stats.LastExecutedAt - stats.FirstExecutedAt).TotalSeconds;
            if (timeDiff > 0 && timeDiff < 60) // 1分钟内
            {
                var queryRate = stats.ExecutionCount / timeDiff; // 每秒查询数

                if (queryRate > 5) // 每秒超过5次相同查询
                {
                    _logger.LogWarning(
                        "检测到潜在N+1查询模式:\n" +
                        "模式: {QueryPattern}\n" +
                        "执行次数: {Count}\n" +
                        "时间窗口: {TimeWindow}秒\n" +
                        "查询频率: {Rate:F2}次/秒\n" +
                        "建议: 使用Include()预加载或批量查询优化",
                        queryPattern,
                        stats.ExecutionCount,
                        timeDiff,
                        queryRate);
                }
            }
        }

        /// <summary>
        /// 获取统计报告
        /// </summary>
        public string GetStatisticsReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== 查询性能统计报告 ===");
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            // 按执行次数排序（找出最频繁的查询）
            var topQueries = _statistics.Values
                .OrderByDescending(s => s.ExecutionCount)
                .Take(20);

            report.AppendLine("【最频繁查询 TOP 20】");
            foreach (var stat in topQueries)
            {
                report.AppendLine($"- 执行次数: {stat.ExecutionCount}");
                report.AppendLine($"  平均耗时: {stat.AverageDurationMs:F2}ms");
                report.AppendLine($"  最大/最小: {stat.MaxDurationMs:F2}ms / {stat.MinDurationMs:F2}ms");
                report.AppendLine($"  模式: {TruncateString(stat.QueryPattern, 100)}");
                report.AppendLine();
            }

            // 按平均执行时间排序（找出最慢的查询）
            var slowestQueries = _statistics.Values
                .Where(s => s.ExecutionCount >= 5) // 至少执行5次
                .OrderByDescending(s => s.AverageDurationMs)
                .Take(10);

            report.AppendLine("【最慢查询 TOP 10】（至少执行5次）");
            foreach (var stat in slowestQueries)
            {
                report.AppendLine($"- 平均耗时: {stat.AverageDurationMs:F2}ms");
                report.AppendLine($"  执行次数: {stat.ExecutionCount}");
                report.AppendLine($"  总耗时: {stat.TotalDurationMs:F2}ms");
                report.AppendLine($"  模式: {TruncateString(stat.QueryPattern, 100)}");
                if (stat.SlowExecutions.Any())
                {
                    report.AppendLine($"  慢查询记录: {string.Join(", ", stat.SlowExecutions.Take(3))}");
                }
                report.AppendLine();
            }

            // N+1嫌疑查询
            var n1Suspects = _statistics.Values
                .Where(s => s.ExecutionCount > 50 && s.AverageDurationMs < 10) // 执行多次且每次很快
                .OrderByDescending(s => s.ExecutionCount)
                .Take(10);

            if (n1Suspects.Any())
            {
                report.AppendLine("【N+1嫌疑查询】（执行>50次，平均<10ms）");
                foreach (var stat in n1Suspects)
                {
                    report.AppendLine($"- 执行次数: {stat.ExecutionCount}");
                    report.AppendLine($"  平均耗时: {stat.AverageDurationMs:F2}ms");
                    report.AppendLine($"  时间跨度: {(stat.LastExecutedAt - stat.FirstExecutedAt).TotalSeconds:F2}秒");
                    report.AppendLine($"  模式: {TruncateString(stat.QueryPattern, 100)}");
                    report.AppendLine();
                }
            }

            // 总体统计
            report.AppendLine("【总体统计】");
            report.AppendLine($"- 不同查询模式数: {_statistics.Count}");
            report.AppendLine($"- 总查询次数: {_statistics.Values.Sum(s => s.ExecutionCount)}");
            report.AppendLine($"- 总执行时间: {_statistics.Values.Sum(s => s.TotalDurationMs):F2}ms");
            report.AppendLine($"- 平均查询时间: {(_statistics.Values.Any() ? _statistics.Values.Average(s => s.AverageDurationMs) : 0):F2}ms");

            return report.ToString();
        }

        /// <summary>
        /// 清除统计数据
        /// </summary>
        public void ClearStatistics()
        {
            _statistics.Clear();
            _logger.LogInformation("查询统计数据已清除");
        }

        /// <summary>
        /// 导出统计数据为JSON
        /// </summary>
        public string ExportStatisticsAsJson()
        {
            var exportData = _statistics.Values
                .Select(s => new
                {
                    s.QueryPattern,
                    s.ExecutionCount,
                    s.TotalDurationMs,
                    s.AverageDurationMs,
                    s.MaxDurationMs,
                    s.MinDurationMs,
                    s.FirstExecutedAt,
                    s.LastExecutedAt,
                    SlowExecutionCount = s.SlowExecutions.Count
                })
                .OrderByDescending(s => s.ExecutionCount);

            return System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        private string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;
            return str.Substring(0, maxLength - 3) + "...";
        }
    }

    /// <summary>
    /// 查询统计收集器接口
    /// </summary>
    public interface IQueryStatisticsCollector
    {
        void RecordQueryExecution(string commandText, double durationMs, bool isSlowQuery);
        string GetStatisticsReport();
        void ClearStatistics();
        string ExportStatisticsAsJson();
    }
}
