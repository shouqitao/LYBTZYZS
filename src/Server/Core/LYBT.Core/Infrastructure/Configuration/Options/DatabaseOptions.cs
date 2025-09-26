using System.ComponentModel.DataAnnotations;

namespace LYBT.Core.Infrastructure.Configuration.Options
{

    /// <summary>
    /// 数据库配置选项
    /// </summary>
    public class DatabaseOptions
    {
        public const string SectionName = "DatabaseOptions";

        /// <summary>
        /// 是否启用自动迁移
        /// </summary>
        public bool EnableAutoMigration { get; set; } = true;

        /// <summary>
        /// 是否启用敏感数据日志记录
        /// </summary>
        public bool EnableSensitiveDataLogging { get; set; } = false;

        /// <summary>
        /// 是否启用详细错误
        /// </summary>
        public bool EnableDetailedErrors { get; set; } = false;

        /// <summary>
        /// 命令超时时间（秒）
        /// </summary>
        [Range(1, 300, ErrorMessage = "命令超时时间必须在1-300秒之间")]
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// 连接池配置
        /// </summary>
        public ConnectionPoolOptions ConnectionPool { get; set; } = new();

        /// <summary>
        /// 性能监控配置
        /// </summary>
        public DatabaseMonitoringOptions Monitoring { get; set; } = new();

        /// <summary>
        /// 备份配置
        /// </summary>
        public DatabaseBackupOptions Backup { get; set; } = new();
    }

    /// <summary>
    /// 连接池配置
    /// </summary>
    public class ConnectionPoolOptions
    {

        /// <summary>
        /// 最大池大小
        /// </summary>
        [Range(1, 1000, ErrorMessage = "最大连接池大小必须在1-1000之间")]
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// 最小池大小
        /// </summary>
        [Range(0, 100, ErrorMessage = "最小连接池大小必须在0-100之间")]
        public int MinPoolSize { get; set; } = 0;

        /// <summary>
        /// 连接生存时间（秒）
        /// </summary>
        [Range(0, 3600, ErrorMessage = "连接生存时间必须在0-3600秒之间")]
        public int ConnectionLifetime { get; set; } = 0;

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        [Range(1, 300, ErrorMessage = "连接超时时间必须在1-300秒之间")]
        public int ConnectionTimeout { get; set; } = 30;
    }

    /// <summary>
    /// 数据库监控配置
    /// </summary>
    public class DatabaseMonitoringOptions
    {

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// 慢查询阈值（毫秒）
        /// </summary>
        [Range(100, 60000, ErrorMessage = "慢查询阈值必须在100-60000毫秒之间")]
        public int SlowQueryThreshold { get; set; } = 1000;

        /// <summary>
        /// 是否记录查询统计
        /// </summary>
        public bool LogQueryStatistics { get; set; } = true;

        /// <summary>
        /// 是否启用死锁检测
        /// </summary>
        public bool EnableDeadlockDetection { get; set; } = true;
    }

    /// <summary>
    /// 数据库备份配置
    /// </summary>
    public class DatabaseBackupOptions
    {

        /// <summary>
        /// 是否启用自动备份
        /// </summary>
        public bool EnableAutoBackup { get; set; } = false;

        /// <summary>
        /// 备份间隔（小时）
        /// </summary>
        [Range(1, 168, ErrorMessage = "备份间隔必须在1-168小时之间")]
        public int BackupInterval { get; set; } = 24;

        /// <summary>
        /// 备份保留天数
        /// </summary>
        [Range(1, 365, ErrorMessage = "备份保留天数必须在1-365天之间")]
        public int BackupRetentionDays { get; set; } = 30;

        /// <summary>
        /// 备份路径
        /// </summary>
        public string BackupPath { get; set; } = "Backups";

        /// <summary>
        /// 是否压缩备份
        /// </summary>
        public bool CompressBackup { get; set; } = true;
    }
}
