#nullable enable

using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Caching.Configuration
{
    /// <summary>
    /// 统一缓存配置选项 - 整合前后端缓存配置
    /// </summary>
    /// <remarks>
    /// <para>整合目标: 统一服务端Infrastructure/CacheOptions和客户端Desktop/CacheOptions</para>
    /// <para>配置支持: Memory/Redis/Hybrid多种缓存类型</para>
    /// <para>兼容性: 保持与现有配置的API兼容</para>
    /// <para>扩展性: 支持统计、性能优化、分区等高级功能</para>
    /// </remarks>
    public class UnifiedCacheOptions
    {
        public const string SectionName = "UnifiedCache";

        #region 基础配置

        /// <summary>
        /// 缓存类型
        /// </summary>
        [Required(ErrorMessage = "缓存类型不能为空")]
        [RegularExpression("^(Memory|Redis|Hybrid)$", ErrorMessage = "缓存类型必须是Memory、Redis或Hybrid")]
        public string CacheType { get; set; } = "Memory";

        /// <summary>
        /// 默认过期时间（分钟）
        /// </summary>
        [Range(1, 1440, ErrorMessage = "默认过期时间必须在1-1440分钟之间")]
        public int DefaultExpiryMinutes { get; set; } = 30;

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 环境特定配置
        /// </summary>
        public string Environment { get; set; } = "Production";

        #endregion

        #region 内存缓存配置

        /// <summary>
        /// 内存缓存配置
        /// </summary>
        public UnifiedMemoryCacheOptions Memory { get; set; } = new();

        #endregion

        #region Redis缓存配置

        /// <summary>
        /// Redis缓存配置
        /// </summary>
        public UnifiedRedisCacheOptions Redis { get; set; } = new();

        #endregion

        #region 统计与监控

        /// <summary>
        /// 缓存统计配置
        /// </summary>
        public UnifiedCacheStatisticsOptions Statistics { get; set; } = new();

        #endregion

        #region 性能优化

        /// <summary>
        /// 性能优化配置
        /// </summary>
        public UnifiedCachePerformanceOptions Performance { get; set; } = new();

        #endregion

        #region 分区配置

        /// <summary>
        /// 分区配置
        /// </summary>
        public UnifiedCachePartitioningOptions Partitioning { get; set; } = new();

        #endregion

        #region 配置验证

        /// <summary>
        /// 验证配置有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult Validate()
        {
            var errors = new List<string>();

            if (DefaultExpiryMinutes <= 0)
            {
                errors.Add("默认过期时间必须大于零");
            }

            if (!new[] { "Memory", "Redis", "Hybrid" }.Contains(CacheType))
            {
                errors.Add("缓存类型必须是Memory、Redis或Hybrid之一");
            }

            // 验证子配置
            var memoryValidation = Memory.Validate();
            if (!memoryValidation.IsValid)
            {
                errors.AddRange(memoryValidation.Errors.Select(e => $"Memory配置: {e}"));
            }

            var redisValidation = Redis.Validate();
            if (!redisValidation.IsValid)
            {
                errors.AddRange(redisValidation.Errors.Select(e => $"Redis配置: {e}"));
            }

            var statsValidation = Statistics.Validate();
            if (!statsValidation.IsValid)
            {
                errors.AddRange(statsValidation.Errors.Select(e => $"Statistics配置: {e}"));
            }

            var performanceValidation = Performance.Validate();
            if (!performanceValidation.IsValid)
            {
                errors.AddRange(performanceValidation.Errors.Select(e => $"Performance配置: {e}"));
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        #endregion

        #region 预设配置

        /// <summary>
        /// 开发环境配置
        /// </summary>
        public static UnifiedCacheOptions Development()
        {
            return new UnifiedCacheOptions
            {
                CacheType = "Memory",
                DefaultExpiryMinutes = 5,
                Environment = "Development",
                Memory = UnifiedMemoryCacheOptions.Development(),
                Statistics = new UnifiedCacheStatisticsOptions { Enabled = true, TrackKeys = true },
                Performance = new UnifiedCachePerformanceOptions { EnableDetailedLogging = true }
            };
        }

        /// <summary>
        /// 生产环境配置
        /// </summary>
        public static UnifiedCacheOptions Production()
        {
            return new UnifiedCacheOptions
            {
                CacheType = "Memory",
                DefaultExpiryMinutes = 60,
                Environment = "Production",
                Memory = UnifiedMemoryCacheOptions.Production(),
                Statistics = new UnifiedCacheStatisticsOptions { Enabled = true, TrackPerformance = true },
                Performance = new UnifiedCachePerformanceOptions { EnableCompression = true }
            };
        }

        /// <summary>
        /// 高性能配置
        /// </summary>
        public static UnifiedCacheOptions HighPerformance()
        {
            return new UnifiedCacheOptions
            {
                CacheType = "Memory",
                DefaultExpiryMinutes = 30,
                Environment = "Production",
                Memory = UnifiedMemoryCacheOptions.HighPerformance(),
                Statistics = new UnifiedCacheStatisticsOptions { Enabled = false },
                Performance = new UnifiedCachePerformanceOptions 
                { 
                    EnableDetailedLogging = false,
                    EnableBatchOptimization = true 
                }
            };
        }

        #endregion
    }

    /// <summary>
    /// 统一内存缓存配置
    /// </summary>
    public class UnifiedMemoryCacheOptions
    {
        /// <summary>
        /// 大小限制（缓存项数量）
        /// </summary>
        [Range(1, 50000, ErrorMessage = "缓存大小限制必须在1-50000之间")]
        public int SizeLimit { get; set; } = 1000;

        /// <summary>
        /// 最大内存占用（字节）
        /// </summary>
        public long MaxMemorySize { get; set; } = 100 * 1024 * 1024; // 100MB

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

        /// <summary>
        /// LRU淘汰阈值
        /// </summary>
        [Range(0.1, 1.0, ErrorMessage = "LRU淘汰阈值必须在0.1-1.0之间")]
        public double LruEvictionThreshold { get; set; } = 0.9;

        /// <summary>
        /// 内存压力淘汰阈值
        /// </summary>
        [Range(0.1, 1.0, ErrorMessage = "内存淘汰阈值必须在0.1-1.0之间")]
        public double MemoryEvictionThreshold { get; set; } = 0.8;

        public ValidationResult Validate()
        {
            var errors = new List<string>();

            if (SizeLimit <= 0)
                errors.Add("缓存大小限制必须大于零");

            if (MaxMemorySize <= 0)
                errors.Add("最大内存占用必须大于零");

            if (CompactionPercentage <= 0 || CompactionPercentage > 0.5)
                errors.Add("压缩百分比必须在0.01-0.5之间");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static UnifiedMemoryCacheOptions Development()
        {
            return new UnifiedMemoryCacheOptions
            {
                SizeLimit = 500,
                MaxMemorySize = 50 * 1024 * 1024, // 50MB
                ExpirationScanFrequency = 10
            };
        }

        public static UnifiedMemoryCacheOptions Production()
        {
            return new UnifiedMemoryCacheOptions
            {
                SizeLimit = 5000,
                MaxMemorySize = 500 * 1024 * 1024, // 500MB
                ExpirationScanFrequency = 60
            };
        }

        public static UnifiedMemoryCacheOptions HighPerformance()
        {
            return new UnifiedMemoryCacheOptions
            {
                SizeLimit = 2000,
                MaxMemorySize = 200 * 1024 * 1024, // 200MB
                ExpirationScanFrequency = 120,
                CompactionPercentage = 0.05
            };
        }
    }

    /// <summary>
    /// 统一Redis缓存配置
    /// </summary>
    public class UnifiedRedisCacheOptions
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

        public ValidationResult Validate()
        {
            var errors = new List<string>();

            if (ConnectTimeout <= 0)
                errors.Add("连接超时时间必须大于零");

            if (SyncTimeout <= 0)
                errors.Add("同步超时时间必须大于零");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }
    }

    /// <summary>
    /// 统一缓存统计配置
    /// </summary>
    public class UnifiedCacheStatisticsOptions
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

        public ValidationResult Validate()
        {
            var errors = new List<string>();

            if (RetentionDays <= 0)
                errors.Add("统计数据保留天数必须大于零");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }
    }

    /// <summary>
    /// 统一缓存性能配置
    /// </summary>
    public class UnifiedCachePerformanceOptions
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
        /// 是否记录详细日志
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        public ValidationResult Validate()
        {
            var errors = new List<string>();

            if (CompressionThreshold <= 0)
                errors.Add("压缩阈值必须大于零");

            if (BatchSize <= 0)
                errors.Add("批量操作大小必须大于零");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }
    }

    /// <summary>
    /// 统一缓存分区配置
    /// </summary>
    public class UnifiedCachePartitioningOptions
    {
        /// <summary>
        /// 是否启用分区
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 默认分区名称
        /// </summary>
        public string DefaultPartition { get; set; } = "default";

        /// <summary>
        /// 分区配置列表
        /// </summary>
        public List<PartitionConfig> Partitions { get; set; } = new();
    }

    /// <summary>
    /// 分区配置
    /// </summary>
    public class PartitionConfig
    {
        /// <summary>
        /// 分区名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 最大项数
        /// </summary>
        public int MaxItems { get; set; } = 1000;

        /// <summary>
        /// 最大内存占用
        /// </summary>
        public long MaxMemory { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// 默认过期时间
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 获取错误摘要
        /// </summary>
        /// <returns>错误摘要</returns>
        public string GetErrorSummary()
        {
            return string.Join("; ", Errors);
        }
    }
}