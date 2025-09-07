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
        /// 默认过期时间（分钟）
        /// </summary>
        [Range(1, 1440, ErrorMessage = "默认过期时间必须在1-1440分钟之间")]
        public int DefaultExpiryMinutes { get; set; } = 30;

        /// <summary>
        /// 缓存类型
        /// </summary>
        [Required(ErrorMessage = "缓存类型不能为空")]
        [RegularExpression("^(Memory|Redis|Hybrid)$", ErrorMessage = "缓存类型必须是Memory、Redis或Hybrid")]
        public string CacheType { get; set; } = "Memory";

        /// <summary>
        /// 内存缓存配置
        /// </summary>
        public MemoryCacheOptions MemoryCache { get; set; } = new();

        /// <summary>
        /// Redis缓存配置
        /// </summary>
        public RedisCacheOptions RedisCache { get; set; } = new();

        /// <summary>
        /// 缓存统计配置
        /// </summary>
        public CacheStatisticsOptions Statistics { get; set; } = new();

        /// <summary>
        /// 性能优化配置
        /// </summary>
        public CachePerformanceOptions Performance { get; set; } = new();
    }

    /// <summary>
    /// 内存缓存配置
    /// </summary>
    public class MemoryCacheOptions
    {

        /// <summary>
        /// 大小限制
        /// </summary>
        [Range(1, 10000, ErrorMessage = "缓存大小限制必须在1-10000之间")]
        public int SizeLimit { get; set; } = 200;

        /// <summary>
        /// 压缩百分比
        /// </summary>
        [Range(0.01, 0.5, ErrorMessage = "压缩百分比必须在0.01-0.5之间")]
        public double CompactionPercentage { get; set; } = 0.10;

        /// <summary>
        /// 过期扫描频率（秒）
        /// </summary>
        [Range(10, 3600, ErrorMessage = "过期扫描频率必须在10-3600秒之间")]
        public int ExpirationScanFrequency { get; set; } = 30;
    }

    /// <summary>
    /// Redis缓存配置
    /// </summary>
    public class RedisCacheOptions
    {

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 实例名称
        /// </summary>
        public string InstanceName { get; set; } = "LYBT";

        /// <summary>
        /// 数据库索引
        /// </summary>
        [Range(0, 15, ErrorMessage = "Redis数据库索引必须在0-15之间")]
        public int Database { get; set; } = 0;

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        [Range(1000, 30000, ErrorMessage = "连接超时时间必须在1000-30000毫秒之间")]
        public int ConnectTimeout { get; set; } = 5000;

        /// <summary>
        /// 同步超时时间（毫秒）
        /// </summary>
        [Range(1000, 30000, ErrorMessage = "同步超时时间必须在1000-30000毫秒之间")]
        public int SyncTimeout { get; set; } = 5000;

        /// <summary>
        /// 重试次数
        /// </summary>
        [Range(0, 10, ErrorMessage = "重试次数必须在0-10之间")]
        public int RetryTimes { get; set; } = 3;
    }

    /// <summary>
    /// 缓存统计配置
    /// </summary>
    public class CacheStatisticsOptions
    {

        /// <summary>
        /// 是否启用统计
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 是否跟踪键
        /// </summary>
        public bool TrackKeys { get; set; } = true;

        /// <summary>
        /// 是否跟踪性能
        /// </summary>
        public bool TrackPerformance { get; set; } = true;

        /// <summary>
        /// 统计数据保留天数
        /// </summary>
        [Range(1, 365, ErrorMessage = "统计数据保留天数必须在1-365之间")]
        public int RetentionDays { get; set; } = 7;
    }

    /// <summary>
    /// 缓存性能优化配置
    /// </summary>
    public class CachePerformanceOptions
    {

        /// <summary>
        /// 是否启用压缩
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// 压缩阈值（字节）
        /// </summary>
        [Range(100, 1048576, ErrorMessage = "压缩阈值必须在100-1048576字节之间")]
        public int CompressionThreshold { get; set; } = 1024;

        /// <summary>
        /// 是否启用预加载
        /// </summary>
        public bool EnablePreloading { get; set; } = false;

        /// <summary>
        /// 预加载键模式列表
        /// </summary>
        public List<string> PreloadPatterns { get; set; } = new();

        /// <summary>
        /// 是否启用批量操作优化
        /// </summary>
        public bool EnableBatchOptimization { get; set; } = true;

        /// <summary>
        /// 批量操作大小
        /// </summary>
        [Range(10, 1000, ErrorMessage = "批量操作大小必须在10-1000之间")]
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// 缓存预热时间（分钟）
        /// </summary>
        [Range(0, 60, ErrorMessage = "缓存预热时间必须在0-60分钟之间")]
        public int WarmupTimeMinutes { get; set; } = 5;
    }
}
