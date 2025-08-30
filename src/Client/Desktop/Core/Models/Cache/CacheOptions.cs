using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Core.Models.Cache
{
    /// <summary>
    /// 缓存选项配置
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// 默认缓存过期时间
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// 最大缓存项数量
        /// </summary>
        public int MaxItemCount { get; set; } = 1000;

        /// <summary>
        /// 最大内存占用（字节）
        /// </summary>
        public long MaxMemorySize { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// 缓存清理间隔
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 是否启用统计
        /// </summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>
        /// 是否启用后台清理
        /// </summary>
        public bool EnableBackgroundCleanup { get; set; } = true;

        /// <summary>
        /// LRU淘汰阈值（当达到最大项数的这个比例时开始淘汰）
        /// </summary>
        public double LruEvictionThreshold { get; set; } = 0.9;

        /// <summary>
        /// 内存压力淘汰阈值（当达到最大内存的这个比例时开始淘汰）
        /// </summary>
        public double MemoryEvictionThreshold { get; set; } = 0.8;

        /// <summary>
        /// 单次淘汰的项数比例
        /// </summary>
        public double EvictionPercentage { get; set; } = 0.1;

        /// <summary>
        /// 是否在访问时检查过期
        /// </summary>
        public bool CheckExpirationOnAccess { get; set; } = true;

        /// <summary>
        /// 是否启用分区隔离
        /// </summary>
        public bool EnablePartitioning { get; set; } = true;

        /// <summary>
        /// 默认分区名称
        /// </summary>
        public string DefaultPartition { get; set; } = "default";

        /// <summary>
        /// 是否启用依赖失效
        /// </summary>
        public bool EnableDependencyInvalidation { get; set; } = false;

        /// <summary>
        /// 是否记录详细日志
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// 创建默认选项
        /// </summary>
        /// <returns>默认缓存选项</returns>
        public static CacheOptions Default()
        {
            return new CacheOptions();
        }

        /// <summary>
        /// 创建开发环境选项（更短的过期时间，更详细的日志）
        /// </summary>
        /// <returns>开发环境缓存选项</returns>
        public static CacheOptions Development()
        {
            return new CacheOptions
            {
                DefaultExpiration = TimeSpan.FromMinutes(5),
                MaxItemCount = 500,
                MaxMemorySize = 50 * 1024 * 1024, // 50MB
                CleanupInterval = TimeSpan.FromMinutes(1),
                EnableDetailedLogging = true,
                CheckExpirationOnAccess = true
            };
        }

        /// <summary>
        /// 创建生产环境选项（更长的过期时间，更大的容量）
        /// </summary>
        /// <returns>生产环境缓存选项</returns>
        public static CacheOptions Production()
        {
            return new CacheOptions
            {
                DefaultExpiration = TimeSpan.FromHours(1),
                MaxItemCount = 5000,
                MaxMemorySize = 500 * 1024 * 1024, // 500MB
                CleanupInterval = TimeSpan.FromMinutes(10),
                EnableDetailedLogging = false,
                EnableBackgroundCleanup = true
            };
        }

        /// <summary>
        /// 创建高性能选项（禁用一些功能以提升性能）
        /// </summary>
        /// <returns>高性能缓存选项</returns>
        public static CacheOptions HighPerformance()
        {
            return new CacheOptions
            {
                DefaultExpiration = TimeSpan.FromMinutes(30),
                MaxItemCount = 2000,
                MaxMemorySize = 200 * 1024 * 1024, // 200MB
                CleanupInterval = TimeSpan.FromMinutes(15),
                EnableStatistics = false,
                EnableDetailedLogging = false,
                EnableDependencyInvalidation = false,
                CheckExpirationOnAccess = false
            };
        }

        /// <summary>
        /// 验证配置有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult Validate()
        {
            var errors = new List<string>();

            if (DefaultExpiration <= TimeSpan.Zero)
                errors.Add("默认过期时间必须大于零");

            if (MaxItemCount <= 0)
                errors.Add("最大缓存项数量必须大于零");

            if (MaxMemorySize <= 0)
                errors.Add("最大内存占用必须大于零");

            if (CleanupInterval <= TimeSpan.Zero)
                errors.Add("清理间隔必须大于零");

            if (LruEvictionThreshold <= 0 || LruEvictionThreshold > 1)
                errors.Add("LRU淘汰阈值必须在0-1之间");

            if (MemoryEvictionThreshold <= 0 || MemoryEvictionThreshold > 1)
                errors.Add("内存淘汰阈值必须在0-1之间");

            if (EvictionPercentage <= 0 || EvictionPercentage > 1)
                errors.Add("淘汰比例必须在0-1之间");

            if (string.IsNullOrWhiteSpace(DefaultPartition))
                errors.Add("默认分区名称不能为空");

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }
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
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 获取错误摘要
        /// </summary>
        /// <returns>错误摘要</returns>
        public string GetErrorSummary()
        {
            return string.Join("; ", Errors);
        }
    }

    /// <summary>
    /// 分区配置
    /// </summary>
    public class PartitionOptions
    {
        /// <summary>
        /// 分区名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 最大项数
        /// </summary>
        public int MaxItems { get; set; }

        /// <summary>
        /// 最大内存占用
        /// </summary>
        public long MaxMemory { get; set; }

        /// <summary>
        /// 默认过期时间
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; }
    }
}