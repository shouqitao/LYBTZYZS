using Serilog.Events;

namespace LYBT.Shared.Logging.Bootstrap;

/// <summary>
/// 统一日志配置项
/// 合并 DesktopSerilogConfiguration（文件路径/模板/级别）与 SerilogMSSqlServerExtensions（MSSQL 开关）配置项
/// </summary>
public sealed class LybtLoggingOptions
{
    /// <summary>
    /// 最低日志级别（Desktop 静态日志器默认 Information）
    /// </summary>
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// 应用名称（写入日志属性 Application）
    /// </summary>
    public string ApplicationName { get; set; } = "LYBT.Desktop";

    /// <summary>
    /// 日志文件基础路径 - %LOCALAPPDATA%/LYBTZYZS/logs
    /// </summary>
    public string LogBasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LYBTZYZS",
        "logs");

    /// <summary>
    /// 日志文件名模板
    /// </summary>
    public string LogFileName { get; set; } = "lybt-desktop-.log";

    /// <summary>
    /// 是否启用 MSSqlServer sink（Server-only 能力）
    /// </summary>
    public bool UseMssqlSink { get; set; }

    /// <summary>
    /// MSSqlServer sink 是否自动建表
    /// 代码优先：默认 false，由 EF Core 迁移管理表结构（解决 appsettings.Production.json autoCreateSqlTable:true 与代码 false 的冲突）
    /// </summary>
    public bool AutoCreateSqlTable { get; set; }
}
