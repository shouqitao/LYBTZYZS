using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Configuration.Constants;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 日志配置
/// </summary>
public sealed class LoggingOptions
{
    public const string SectionName = ConfigurationSections.Logging;

    /// <summary>
    /// 日志清理配置
    /// </summary>
    public LogCleanupOptions Cleanup { get; set; } = new();
}

/// <summary>
/// 日志清理配置
/// </summary>
public sealed class LogCleanupOptions
{
    public bool Enabled { get; set; } = true;

    [Range(1, 365)]
    public int RetentionDays { get; set; } = 90;

    [Range(1, 168)]
    public int CleanupIntervalHours { get; set; } = 24;

    [Range(1, 60)]
    public int InitialDelayMinutes { get; set; } = 5;

    [Range(100, 10000)]
    public int BatchSize { get; set; } = 1000;
}
