using System;

namespace LYBT.Desktop.Contracts.Performance
{
    /// <summary>
    /// 性能指标数据模型
    /// </summary>
    public record PerformanceMetric
    {
        /// <summary>
        /// 操作名称
        /// </summary>
        public required string OperationName { get; init; }

        /// <summary>
        /// 操作耗时（毫秒）
        /// </summary>
        public required long DurationMs { get; init; }

        /// <summary>
        /// 操作开始前的内存使用量（字节）
        /// </summary>
        public long MemoryBeforeBytes { get; init; }

        /// <summary>
        /// 操作结束后的内存使用量（字节）
        /// </summary>
        public long MemoryAfterBytes { get; init; }

        /// <summary>
        /// 操作期间的内存增量（字节）
        /// </summary>
        public long MemoryDeltaBytes => MemoryAfterBytes - MemoryBeforeBytes;

        /// <summary>
        /// 指标记录时间戳
        /// </summary>
        public required DateTime Timestamp { get; init; }

        /// <summary>
        /// 性能等级评估
        /// </summary>
        public PerformanceLevel Level => DurationMs switch
        {
            <= PerformanceThresholds.ExcellentThreshold => PerformanceLevel.Excellent,
            <= PerformanceThresholds.GoodThreshold => PerformanceLevel.Good,
            <= PerformanceThresholds.AcceptableThreshold => PerformanceLevel.Acceptable,
            _ => PerformanceLevel.Poor
        };

        /// <summary>
        /// 格式化的耗时显示
        /// </summary>
        public string FormattedDuration => DurationMs switch
        {
            < 1000 => $"{DurationMs}ms",
            _ => $"{DurationMs / 1000.0:F2}s"
        };

        /// <summary>
        /// 格式化的内存显示
        /// </summary>
        public string FormattedMemoryDelta
        {
            get
            {
                var delta = MemoryDeltaBytes;
                return delta switch
                {
                    >= 1024 * 1024 => $"{delta / (1024.0 * 1024.0):F2}MB",
                    >= 1024 => $"{delta / 1024.0:F2}KB",
                    _ => $"{delta}B"
                };
            }
        }
    }

    /// <summary>
    /// 性能等级枚举
    /// </summary>
    public enum PerformanceLevel
    {
        /// <summary>优秀 - 响应极快</summary>
        Excellent,
        /// <summary>良好 - 响应快速</summary>
        Good,
        /// <summary>可接受 - 响应正常</summary>
        Acceptable,
        /// <summary>较差 - 需要优化</summary>
        Poor
    }

    /// <summary>
    /// 性能阈值常量
    /// </summary>
    public static class PerformanceThresholds
    {
        /// <summary>
        /// 优秀阈值（毫秒）
        /// </summary>
        public const int ExcellentThreshold = 500;

        /// <summary>
        /// 良好阈值（毫秒）
        /// </summary>
        public const int GoodThreshold = 1500;

        /// <summary>
        /// 可接受阈值（毫秒）
        /// </summary>
        public const int AcceptableThreshold = 3000;

        /// <summary>
        /// 获取性能等级的描述
        /// </summary>
        public static string GetLevelDescription(PerformanceLevel level) => level switch
        {
            PerformanceLevel.Excellent => "优秀",
            PerformanceLevel.Good => "良好",
            PerformanceLevel.Acceptable => "可接受",
            PerformanceLevel.Poor => "需要优化",
            _ => "未知"
        };
    }
}
