using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Configuration.Constants;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 内存缓存配置
/// </summary>
public sealed class MemoryCacheOptions
{
    public const string SectionName = ConfigurationSections.MemoryCache;

    /// <summary>
    /// 是否启用缓存
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 缓存大小限制 (字节)
    /// </summary>
    [Range(1048576, 1073741824)] // 1MB - 1GB
    public long SizeLimit { get; set; } = 104857600; // 100MB

    /// <summary>
    /// 压缩百分比
    /// </summary>
    [Range(0.01, 0.5)]
    public double CompactionPercentage { get; set; } = 0.05;

    /// <summary>
    /// 过期扫描频率 (秒)
    /// </summary>
    [Range(10, 300)]
    public int ExpirationScanFrequencySeconds { get; set; } = 60;

    /// <summary>
    /// 默认过期时间 (分钟)
    /// </summary>
    [Range(1, 1440)]
    public int DefaultExpirationMinutes { get; set; } = 5;
}
