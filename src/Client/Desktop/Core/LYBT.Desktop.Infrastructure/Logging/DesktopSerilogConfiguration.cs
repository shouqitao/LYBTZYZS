using System.IO;
using LYBT.Shared.Logging.Abstractions;
using LYBT.Shared.Logging.Extensions;
using LYBT.Shared.Logging.Masking;
using Serilog;
using Serilog.Events;

namespace LYBT.Desktop.Infrastructure.Logging;

/// <summary>
/// 客户端Serilog配置
/// 为WPF客户端配置结构化日志，使用共享日志组件
/// </summary>
public static class DesktopSerilogConfiguration
{
    /// <summary>
    /// 日志文件基础路径 - %LOCALAPPDATA%/LYBTZYZS/logs
    /// </summary>
    public static string LogBasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LYBTZYZS",
        "logs");

    /// <summary>
    /// 日志文件完整路径模板
    /// </summary>
    public static string LogFilePath => Path.Combine(LogBasePath, "lybt-desktop-.log");

    /// <summary>
    /// CorrelationId提供者（单例）
    /// 使用基于Activity API的实现，支持W3C TraceContext标准
    /// </summary>
    private static readonly Lazy<ICorrelationIdProvider> _correlationIdProvider =
        new(() => new ActivityCorrelationIdProvider());

    /// <summary>
    /// 获取CorrelationId提供者
    /// </summary>
    public static ICorrelationIdProvider CorrelationIdProvider => _correlationIdProvider.Value;

    /// <summary>
    /// 配置Serilog日志
    /// </summary>
    /// <param name="minimumLevel">最低日志级别，默认Information</param>
    /// <returns>配置好的LoggerConfiguration</returns>
    public static LoggerConfiguration CreateLoggerConfiguration(LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        // 确保日志目录存在
        EnsureLogDirectoryExists();

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Prism", LogEventLevel.Warning)
            // 使用共享日志配置
            .UseSharedLogging(CorrelationIdProvider)
            .Enrich.WithProperty("Application", "LYBT.Desktop")
            // 敏感数据脱敏
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: LogFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
    }

    /// <summary>
    /// 初始化Serilog全局日志器
    /// </summary>
    /// <param name="minimumLevel">最低日志级别</param>
    public static void Initialize(LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        Log.Logger = CreateLoggerConfiguration(minimumLevel).CreateLogger();
        Log.Information("Serilog日志系统已初始化，日志路径: {LogPath}", LogBasePath);
    }

    /// <summary>
    /// 关闭Serilog日志器
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }

    /// <summary>
    /// 确保日志目录存在
    /// </summary>
    private static void EnsureLogDirectoryExists()
    {
        if (!Directory.Exists(LogBasePath))
        {
            try
            {
                Directory.CreateDirectory(LogBasePath);
            }
            catch (Exception ex)
            {
                // 如果无法创建目录，使用临时目录
                System.Diagnostics.Debug.WriteLine($"无法创建日志目录 {LogBasePath}: {ex.Message}");
            }
        }
    }
}
