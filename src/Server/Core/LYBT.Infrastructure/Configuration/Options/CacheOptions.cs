using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Options
{
    /// <summary>
    /// 缓存配置选项
    /// </summary>
    public class CacheOptions
    {
        public const string SectionName = "CacheOptions";

        /// <summary>
        /// 内存缓存配置
        /// </summary>
        public MemoryCacheConfig Memory { get; set; } = new();

        /// <summary>
        /// 监控配置
        /// </summary>
        public MonitoringConfig Monitoring { get; set; } = new();

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 全局缓存key前缀
        /// </summary>
        public string GlobalKeyPrefix { get; set; } = "LYBT:";
    }

    /// <summary>
    /// 内存缓存配置
    /// </summary>
    public class MemoryCacheConfig
    {
        /// <summary>
        /// 缓存大小限制（项目数量）
        /// </summary>
        [Range(100, 1000000, ErrorMessage = "缓存大小限制必须在100-1000000之间")]
        public long? SizeLimit { get; set; } = 10000;

        /// <summary>
        /// 压缩百分比，当达到SizeLimit时压缩的百分比
        /// </summary>
        [Range(0.05, 0.5, ErrorMessage = "压缩百分比必须在0.05-0.5之间")]
        public double CompactionPercentage { get; set; } = 0.05;

        /// <summary>
        /// 过期扫描频率（秒）
        /// </summary>
        [Range(30, 3600, ErrorMessage = "过期扫描频率必须在30-3600秒之间")]
        public int ExpirationScanFrequencySeconds { get; set; } = 60;

        /// <summary>
        /// 默认缓存持续时间（分钟）
        /// </summary>
        [Range(1, 1440, ErrorMessage = "默认缓存时间必须在1-1440分钟之间")]
        public int DefaultCacheDurationMinutes { get; set; } = 5;

        /// <summary>
        /// 空值缓存持续时间（分钟）
        /// </summary>
        [Range(1, 60, ErrorMessage = "空值缓存时间必须在1-60分钟之间")]
        public int NullCacheDurationMinutes { get; set; } = 1;

        /// <summary>
        /// 是否使用滑动过期
        /// </summary>
        public bool UseSlidingExpiration { get; set; } = true;

        /// <summary>
        /// 默认缓存项大小
        /// </summary>
        [Range(1, 1000, ErrorMessage = "默认缓存项大小必须在1-1000之间")]
        public int DefaultItemSize { get; set; } = 1;

        /// <summary>
        /// 缓存项优先级策略
        /// </summary>
        public PriorityStrategy PriorityStrategy { get; set; } = PriorityStrategy.Default;

        /// <summary>
        /// 是否启用统计
        /// </summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>
        /// 是否记录逐出日志
        /// </summary>
        public bool LogEvictions { get; set; } = true;
    }

    /// <summary>
    /// 监控配置
    /// </summary>
    public class MonitoringConfig
    {
        /// <summary>
        /// 是否启用监控
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 采样间隔（秒）
        /// </summary>
        [Range(10, 600, ErrorMessage = "采样间隔必须在10-600秒之间")]
        public int SamplingIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 缓存命中率告警阈值
        /// </summary>
        [Range(0.1, 1.0, ErrorMessage = "命中率阈值必须在0.1-1.0之间")]
        public double HitRateThreshold { get; set; } = 0.8;

        /// <summary>
        /// 缓存容量占用告警阈值
        /// </summary>
        [Range(0.5, 1.0, ErrorMessage = "容量占用阈值必须在0.5-1.0之间")]
        public double CapacityThreshold { get; set; } = 0.85;

        /// <summary>
        /// 逐出速率告警阈值（每分钟）
        /// </summary>
        [Range(0, 1000, ErrorMessage = "逐出速率阈值必须在0-1000之间")]
        public int EvictionRateThreshold { get; set; } = 100;

        /// <summary>
        /// 保留历史快照数量
        /// </summary>
        [Range(1, 100, ErrorMessage = "历史快照数量必须在1-100之间")]
        public int HistorySnapshotCount { get; set; } = 10;

        /// <summary>
        /// 是否启用性能计数器
        /// </summary>
        public bool EnablePerformanceCounters { get; set; } = false;

        /// <summary>
        /// 告警事件ID
        /// </summary>
        public AlertEventIds EventIds { get; set; } = new();
    }

    /// <summary>
    /// 告警事件ID配置
    /// </summary>
    public class AlertEventIds
    {
        /// <summary>
        /// 低命中率事件ID
        /// </summary>
        public int LowHitRate { get; set; } = 5001;

        /// <summary>
        /// 高容量占用事件ID
        /// </summary>
        public int HighCapacity { get; set; } = 5002;

        /// <summary>
        /// 高逐出率事件ID
        /// </summary>
        public int HighEvictionRate { get; set; } = 5003;

        /// <summary>
        /// 配置缺失事件ID
        /// </summary>
        public int ConfigMissing { get; set; } = 5004;
    }

    /// <summary>
    /// 缓存项优先级策略
    /// </summary>
    public enum PriorityStrategy
    {
        /// <summary>
        /// 默认策略（Normal优先级）
        /// </summary>
        Default,

        /// <summary>
        /// 基于访问频率的LRU策略
        /// </summary>
        LRU,

        /// <summary>
        /// 基于生存时间的策略
        /// </summary>
        TTL,

        /// <summary>
        /// 自定义策略
        /// </summary>
        Custom
    }
}
