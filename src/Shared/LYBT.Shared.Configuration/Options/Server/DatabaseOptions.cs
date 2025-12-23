using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Configuration.Constants;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 数据库配置
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = ConfigurationSections.Database;

    /// <summary>
    /// 连接字符串（可选，代码有fallback链：此处 → ConnectionStrings:DefaultConnection → 环境变量）
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 自动迁移
    /// </summary>
    public bool AutoMigrate { get; set; } = false;

    /// <summary>
    /// 开发环境自动创建数据库
    /// </summary>
    public bool EnsureCreatedInDevelopment { get; set; } = true;

    /// <summary>
    /// 迁移超时时间（秒）
    /// </summary>
    [Range(30, 7200)]
    public int MigrationTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 连接池配置
    /// </summary>
    public ConnectionPoolOptions ConnectionPool { get; set; } = new();

    /// <summary>
    /// 监控配置
    /// </summary>
    public MonitoringOptions Monitoring { get; set; } = new();

    /// <summary>
    /// 重试策略配置
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();
}

/// <summary>
/// 连接池配置
/// </summary>
public sealed class ConnectionPoolOptions
{
    [Range(1, 100)]
    public int MaxConnections { get; set; } = 20;

    [Range(0, 50)]
    public int MinConnections { get; set; } = 2;

    [Range(5, 120)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    [Range(5, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// 数据库监控配置
/// </summary>
public sealed class MonitoringOptions
{
    public bool Enabled { get; set; } = true;
    public bool LogAllQueries { get; set; } = false;

    [Range(100, 60000)]
    public int SlowQueryThresholdMs { get; set; } = 1000;
}

/// <summary>
/// 重试策略配置
/// </summary>
public sealed class RetryPolicyOptions
{
    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;

    [Range(100, 10000)]
    public int BaseDelayMs { get; set; } = 1000;

    [Range(1000, 60000)]
    public int MaxDelayMs { get; set; } = 10000;
}
