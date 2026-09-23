using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 日志配置（节名 <c>Logging</c>）
/// </summary>
public sealed class LoggingOptions
{
    public const string SectionName = "Logging";

    /// <summary>
    /// 数据库日志清理配置（SystemLog 表）
    /// </summary>
    public LogCleanupOptions Cleanup { get; set; } = new();

    /// <summary>
    /// 文件日志归档配置（F-08）
    /// </summary>
    public LogArchiveOptions Archive { get; set; } = new();
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

/// <summary>
/// 文件日志归档配置（F-08）
/// <para>
/// 把「已结束月份」且超过 <see cref="ArchiveAfterDays"/> 天的日志文件按月打包到
/// <c>{LogDirectory}/{ArchiveDirectory}/{filePrefix}-YYYY-MM.zip</c>，归档成功后删除源文件。
/// 与 Serilog File sink 的 <c>retainedFileCountLimit</c>（30 天）互补：归档先于滚动清理发生，
/// 归档后的 zip 不受滚动删除影响。
/// </para>
/// </summary>
public sealed class LogArchiveOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>仅归档该天数之前的文件（当天/近期文件仍在写入，不归档）</summary>
    [Range(1, 365)]
    public int ArchiveAfterDays { get; set; } = 7;

    /// <summary>归档子目录（相对 <see cref="LogDirectory"/>）</summary>
    [Required]
    public string ArchiveDirectory { get; set; } = "archive";

    /// <summary>归档成功后删除源日志文件</summary>
    public bool DeleteSourceAfterArchive { get; set; } = true;

    /// <summary>归档作业间隔（小时）</summary>
    [Range(1, 168)]
    public int IntervalHours { get; set; } = 24;

    /// <summary>首次执行前的延迟（分钟）——避免启动时与数据库初始化/首次写入争抢磁盘</summary>
    [Range(0, 60)]
    public int InitialDelayMinutes { get; set; } = 10;

    /// <summary>参与归档的日志文件前缀（Serilog File sink 的 <c>{prefix}-YYYYMMDD.log</c>）</summary>
    [MinLength(1)]
    public string[] FilePrefixes { get; set; } = ["lybt-web-api", "bootstrap"];

    /// <summary>
    /// 日志目录。默认 <c>logs</c>（相对路径，与 Serilog File sink 的 <c>logs/lybt-web-api-.log</c> 同基准——
    /// 均相对进程工作目录解析）；可配绝对路径覆盖。
    /// </summary>
    [Required]
    public string LogDirectory { get; set; } = "logs";
}
