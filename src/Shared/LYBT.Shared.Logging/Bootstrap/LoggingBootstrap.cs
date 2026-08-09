using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Logging.Correlation;
using LYBT.Shared.Logging.Extensions;
using LYBT.Shared.Logging.Management;
using LYBT.Shared.Logging.Masking;
using LYBT.Shared.Logging.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace LYBT.Shared.Logging.Bootstrap;

/// <summary>
/// 日志引导入口——统一接管 Server + Desktop 日志初始化
/// 对外暴露单入口：<see cref="Initialize"/>（Desktop 静态日志器）、<see cref="CreateBootstrapLogger"/>（Server 两阶段 Phase 1）、
/// <see cref="AddLybtLogging(IHostBuilder, Action{LybtLoggingOptions})"/>（Server Phase 2）与 DI 注册
/// </summary>
public static class LoggingBootstrap
{
    private static readonly Lazy<ICorrelationIdProvider> _correlationIdProvider = new(() => new ActivityCorrelationIdProvider());

    private static readonly Lazy<LoggingLevelManager> _loggingLevelManager = new(() => new LoggingLevelManager(LogEventLevel.Information));

    /// <summary>
    /// CorrelationId提供者全局单例（静态 Logger 与 DI 注册共用同一实例）
    /// </summary>
    public static ICorrelationIdProvider CorrelationIdProvider => _correlationIdProvider.Value;

    /// <summary>
    /// 日志级别管理器全局单例（Server Final Logger 的 LevelSwitch 与 DI 注册共用同一实例）
    /// </summary>
    public static LoggingLevelManager LoggingLevelManager => _loggingLevelManager.Value;

    /// <summary>
    /// 初始化全局日志器（Desktop 静态入口，文件+控制台 sink + CorrelationId + 脱敏）
    /// </summary>
    /// <param name="configure">日志配置项（默认：Information 级别、LYBT.Desktop 应用名、%LOCALAPPDATA%/LYBTZYZS/logs）</param>
    public static void Initialize(Action<LybtLoggingOptions>? configure = null)
    {
        var options = new LybtLoggingOptions();
        configure?.Invoke(options);

        EnsureLogDirectoryExists(options.LogBasePath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(options.MinimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Prism", LogEventLevel.Warning)
            // 使用共享日志配置（Enrichers + CorrelationId + 脱敏策略）
            .UseSharedLogging(CorrelationIdProvider)
            .Enrich.WithProperty("Application", options.ApplicationName)
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(options.LogBasePath, options.LogFileName),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Serilog日志系统已初始化，日志路径: {LogPath}", options.LogBasePath);
    }

    /// <summary>
    /// 关闭Serilog日志器
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }

    /// <summary>
    /// 创建 ILoggerFactory（LoggerFactory + Serilog 桥接，dispose:false 复用静态 Log.Logger）
    /// </summary>
    public static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder => builder.AddSerilog(dispose: false));
    }

    /// <summary>
    /// Server 两阶段初始化 Phase 1：Bootstrap Logger（确保启动阶段异常能够被记录）
    /// </summary>
    /// <param name="isTestEnvironment">测试环境使用普通 Logger，避免 WebApplicationFactory "logger is already frozen" 错误</param>
    public static void CreateBootstrapLogger(bool isTestEnvironment)
    {
        if (!isTestEnvironment)
        {
            // 生产/开发环境使用Bootstrap Logger（支持两阶段初始化）
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/bootstrap-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateBootstrapLogger();
        }
        else
        {
            // 测试环境使用简单Logger，避免Bootstrap Logger冻结问题
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Console()
                .CreateLogger();
        }
    }

    /// <summary>
    /// Server 两阶段初始化 Phase 2：Final Logger 统一入口
    /// 从配置文件读取，添加所有Enrichers和敏感数据脱敏，受 LoggingLevelManager 运行时控制
    /// </summary>
    /// <param name="hostBuilder">主机构建器</param>
    /// <param name="configure">日志配置项（Server 侧需设置 ApplicationName/UseMssqlSink）</param>
    public static IHostBuilder AddLybtLogging(
        this IHostBuilder hostBuilder,
        Action<LybtLoggingOptions>? configure = null)
    {
        hostBuilder.UseSerilog((context, services, configuration) =>
            ConfigureFinalLogger(configuration, context, services, configure));
        return hostBuilder;
    }

    /// <summary>
    /// 配置 Final Logger（在 UseSerilog 回调中调用）
    /// </summary>
    private static void ConfigureFinalLogger(
        LoggerConfiguration configuration,
        HostBuilderContext context,
        IServiceProvider services,
        Action<LybtLoggingOptions>? configure)
    {
        var options = new LybtLoggingOptions();
        configure?.Invoke(options);

        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", options.ApplicationName)
            .WithSensitiveDataMasking()
            .MinimumLevel.ControlledBy(LoggingLevelManager.LevelSwitch); // 必须在 ReadFrom.Configuration 之后

        // MSSQL sink 为 Server-only 能力（options 开关控制）；测试环境跳过，避免 AutoCreateSqlTable 与 EF 迁移冲突
        if (options.UseMssqlSink && !context.HostingEnvironment.IsEnvironment("Test"))
        {
            var dbOptions = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            configuration.WriteToMssqlSink(
                dbOptions.ConnectionString,
                sinkOptions => sinkOptions.AutoCreateSqlTable = options.AutoCreateSqlTable);
        }
    }

    /// <summary>
    /// 确保日志目录存在
    /// </summary>
    private static void EnsureLogDirectoryExists(string logBasePath)
    {
        if (!Directory.Exists(logBasePath))
        {
            try
            {
                Directory.CreateDirectory(logBasePath);
            }
            catch (Exception ex)
            {
                // 如果无法创建目录，使用临时目录
                System.Diagnostics.Debug.WriteLine($"无法创建日志目录 {logBasePath}: {ex.Message}");
            }
        }
    }
}
