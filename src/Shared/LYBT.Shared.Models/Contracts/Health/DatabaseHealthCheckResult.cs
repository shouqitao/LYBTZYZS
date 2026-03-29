namespace LYBT.Shared.Models.Contracts.Health
{
    /// <summary>
    /// 数据库健康检查结果
    /// </summary>
    public class DatabaseHealthCheckResult
    {
        public DatabaseHealthCheckResult()
        {
            Name = "db";
            Description = "Database Connectivity";
            Status = HealthStatus.Unknown;
        }

        public DatabaseHealthCheckResult(string name, string description)
        {
            Name = name;
            Description = description;
            Status = HealthStatus.Unknown;
        }

        /// <summary>
        /// 检查项名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 检查项描述
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 健康状态
        /// </summary>
        public HealthStatus Status { get; set; }

        /// <summary>
        /// 检查耗时（毫秒）
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// 数据库提供程序名称
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// 待处理迁移数量
        /// </summary>
        public int PendingMigrationCount { get; set; }

        /// <summary>
        /// 数据库服务器版本
        /// </summary>
        public string? ServerVersion { get; set; }
    }

    /// <summary>
    /// 健康状态枚举
    /// </summary>
    public enum HealthStatus
    {
        Unknown,
        Healthy,
        Degraded,
        Unhealthy
    }
}
