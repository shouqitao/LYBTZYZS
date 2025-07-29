namespace LYBT.Infrastructure.Options {

    /// <summary>
    /// 缓存配置选项
    /// </summary>
    public class CacheOptions {

        /// <summary>
        /// 默认过期时间（分钟）
        /// </summary>
        public int DefaultExpiryMinutes { get; set; } = 60;

        /// <summary>
        /// 缓存类型：Memory, Distributed, Redis, Hybrid
        /// </summary>
        public string CacheType { get; set; } = "Memory";

        /// <summary>
        /// Redis连接字符串（当使用Redis时）
        /// </summary>
        public string? RedisConnectionString { get; set; }

        /// <summary>
        /// SQL Server连接字符串（当使用SQL Server分布式缓存时）
        /// </summary>
        public string? SqlServerConnectionString { get; set; }

        /// <summary>
        /// 内存缓存配置
        /// </summary>
        public MemoryCacheConfig MemoryCache { get; set; } = new();

        /// <summary>
        /// 分布式缓存配置
        /// </summary>
        public DistributedCacheConfig DistributedCache { get; set; } = new();

        /// <summary>
        /// 缓存压缩配置
        /// </summary>
        public CompressionConfig Compression { get; set; } = new();

        /// <summary>
        /// 缓存统计配置
        /// </summary>
        public CacheStatsConfig Statistics { get; set; } = new();
    }

    /// <summary>
    /// 内存缓存配置
    /// </summary>
    public class MemoryCacheConfig {

        /// <summary>
        /// 最大内存大小（MB）
        /// </summary>
        public int SizeLimit { get; set; } = 100;

        /// <summary>
        /// 压缩比例阈值
        /// </summary>
        public double CompactionPercentage { get; set; } = 0.05;

        /// <summary>
        /// 扫描频率（秒）
        /// </summary>
        public int ExpirationScanFrequency { get; set; } = 60;
    }

    /// <summary>
    /// 分布式缓存配置
    /// </summary>
    public class DistributedCacheConfig {

        /// <summary>
        /// 实例名称
        /// </summary>
        public string InstanceName { get; set; } = "LYBT";

        /// <summary>
        /// 默认滑动过期时间（分钟）
        /// </summary>
        public int DefaultSlidingExpirationMinutes { get; set; } = 20;

        /// <summary>
        /// 默认绝对过期时间（分钟）
        /// </summary>
        public int DefaultAbsoluteExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// 是否启用连接重试
        /// </summary>
        public bool EnableConnectionResilience { get; set; } = true;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 3;
    }

    /// <summary>
    /// 压缩配置
    /// </summary>
    public class CompressionConfig {

        /// <summary>
        /// 是否启用压缩
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 压缩阈值（字节），超过此大小才压缩
        /// </summary>
        public int CompressionThreshold { get; set; } = 1024;

        /// <summary>
        /// 压缩算法：GZip, Deflate, Brotli
        /// </summary>
        public string Algorithm { get; set; } = "GZip";
    }

    /// <summary>
    /// 缓存统计配置
    /// </summary>
    public class CacheStatsConfig {

        /// <summary>
        /// 是否启用统计
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 统计数据保留天数
        /// </summary>
        public int RetentionDays { get; set; } = 7;

        /// <summary>
        /// 是否记录键的统计信息
        /// </summary>
        public bool TrackKeys { get; set; } = false;

        /// <summary>
        /// 是否记录性能指标
        /// </summary>
        public bool TrackPerformance { get; set; } = true;
    }
}