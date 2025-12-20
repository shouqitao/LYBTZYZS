using LYBT.Shared.Logging.Abstractions;
using LYBT.Shared.Logging.Enrichers;
using LYBT.Shared.Logging.Masking;
using Serilog;
using Serilog.Events;

namespace LYBT.Shared.Logging.Extensions;

/// <summary>
/// Serilog LoggerConfiguration扩展方法
/// 提供统一的日志配置入口
/// </summary>
public static class LoggerConfigurationExtensions
{
    /// <summary>
    /// 默认日志输出模板
    /// </summary>
    public const string DefaultOutputTemplate =
        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// 详细日志输出模板(包含更多上下文信息)
    /// </summary>
    public const string DetailedOutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{CorrelationId}] [{MachineName}] [{ThreadId}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// 应用共享日志配置
    /// </summary>
    /// <param name="loggerConfiguration">LoggerConfiguration实例</param>
    /// <param name="correlationIdProvider">CorrelationId提供者</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static LoggerConfiguration UseSharedLogging(
        this LoggerConfiguration loggerConfiguration,
        ICorrelationIdProvider correlationIdProvider)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);
        ArgumentNullException.ThrowIfNull(correlationIdProvider);

        return loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithCorrelationId(correlationIdProvider)
            .Destructure.With<SensitiveDataDestructuringPolicy>();
    }

    /// <summary>
    /// 启用敏感数据脱敏
    /// </summary>
    /// <param name="loggerConfiguration">LoggerConfiguration实例</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static LoggerConfiguration WithSensitiveDataMasking(
        this LoggerConfiguration loggerConfiguration)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);

        return loggerConfiguration
            .Destructure.With<SensitiveDataDestructuringPolicy>();
    }

    /// <summary>
    /// 配置控制台输出
    /// </summary>
    /// <param name="loggerConfiguration">LoggerConfiguration实例</param>
    /// <param name="minimumLevel">最低日志级别</param>
    /// <param name="outputTemplate">输出模板</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static LoggerConfiguration WriteToConsoleWithTemplate(
        this LoggerConfiguration loggerConfiguration,
        LogEventLevel minimumLevel = LogEventLevel.Debug,
        string? outputTemplate = null)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);

        return loggerConfiguration
            .WriteTo.Console(
                restrictedToMinimumLevel: minimumLevel,
                outputTemplate: outputTemplate ?? DefaultOutputTemplate);
    }

    /// <summary>
    /// 配置文件输出
    /// </summary>
    /// <param name="loggerConfiguration">LoggerConfiguration实例</param>
    /// <param name="logFilePath">日志文件路径</param>
    /// <param name="minimumLevel">最低日志级别</param>
    /// <param name="outputTemplate">输出模板</param>
    /// <param name="rollingInterval">滚动间隔</param>
    /// <param name="retainedFileCountLimit">保留文件数量限制</param>
    /// <returns>配置后的LoggerConfiguration</returns>
    public static LoggerConfiguration WriteToFileWithTemplate(
        this LoggerConfiguration loggerConfiguration,
        string logFilePath,
        LogEventLevel minimumLevel = LogEventLevel.Information,
        string? outputTemplate = null,
        RollingInterval rollingInterval = RollingInterval.Day,
        int? retainedFileCountLimit = 31)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);
        ArgumentNullException.ThrowIfNull(logFilePath);

        return loggerConfiguration
            .WriteTo.File(
                path: logFilePath,
                restrictedToMinimumLevel: minimumLevel,
                outputTemplate: outputTemplate ?? DetailedOutputTemplate,
                rollingInterval: rollingInterval,
                retainedFileCountLimit: retainedFileCountLimit);
    }
}
