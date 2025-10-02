namespace LYBT.Core.Infrastructure.Caching.Models
{
    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// 总键数量
        /// </summary>
        public long TotalKeys { get; set; }

        /// <summary>
        /// 命中次数
        /// </summary>
        public long HitCount { get; set; }

        /// <summary>
        /// 未命中次数
        /// </summary>
        public long MissCount { get; set; }

        /// <summary>
        /// 命中率
        /// </summary>
        public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;

        /// <summary>
        /// 总请求次数
        /// </summary>
        public long TotalRequests => HitCount + MissCount;

        /// <summary>
        /// 已用内存大小 (字节)
        /// </summary>
        public long UsedMemory { get; set; }

        /// <summary>
        /// 过期键数量
        /// </summary>
        public long ExpiredKeys { get; set; }

        /// <summary>
        /// 淘汰键数量
        /// </summary>
        public long EvictedKeys { get; set; }

        /// <summary>
        /// 逐出次数
        /// </summary>
        public long EvictionCount { get; set; }

        /// <summary>
        /// 当前缓存项数量
        /// </summary>
        public long CurrentItemCount { get; set; }

        /// <summary>
        /// 总内存使用量（字节）
        /// </summary>
        public long TotalMemoryUsage { get; set; }

        /// <summary>
        /// 最大容量
        /// </summary>
        public long? MaxCapacity { get; set; }

        /// <summary>
        /// 容量使用率
        /// </summary>
        public double CapacityUsageRatio => MaxCapacity.HasValue && MaxCapacity.Value > 0
            ? (double)CurrentItemCount / MaxCapacity.Value
            : 0;

        /// <summary>
        /// 逐出速率（每分钟）
        /// </summary>
        public double EvictionRate { get; set; }

        /// <summary>
        /// 统计时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 统计采样窗口（秒）
        /// </summary>
        public int SamplingWindowSeconds { get; set; } = 60;
    }
}
