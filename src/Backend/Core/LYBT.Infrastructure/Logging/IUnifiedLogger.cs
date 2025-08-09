using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LYBT.Infrastructure.Logging
{
    /// <summary>
    /// 统一日志管理器接口 - UltraThink监控优化
    /// </summary>
    public interface IUnifiedLogger
    {
        /// <summary>
        /// 记录信息日志
        /// </summary>
        Task LogInfoAsync(string message, object? data = null, 
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        Task LogWarningAsync(string message, object? data = null,
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        Task LogErrorAsync(Exception exception, string message, object? data = null,
            [CallerMemberName] string? memberName = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0);

        /// <summary>
        /// 记录业务操作日志
        /// </summary>
        Task LogOperationAsync(string operation, string result, object? context = null,
            TimeSpan? duration = null, string? userId = null);

        /// <summary>
        /// 记录性能日志
        /// </summary>
        Task LogPerformanceAsync(string operation, TimeSpan duration, 
            PerformanceMetrics? metrics = null, object? context = null);

        /// <summary>
        /// 记录安全日志
        /// </summary>
        Task LogSecurityEventAsync(SecurityEventType eventType, string description,
            string? userId = null, string? ipAddress = null, object? additionalData = null);

        /// <summary>
        /// 记录审计日志
        /// </summary>
        Task LogAuditAsync(string action, string resource, string? oldValue, string? newValue,
            string? userId = null, object? metadata = null);

        /// <summary>
        /// 开始性能跟踪
        /// </summary>
        IPerformanceTracker StartPerformanceTracking(string operation, object? context = null);

        /// <summary>
        /// 批量日志记录
        /// </summary>
        Task LogBatchAsync(IEnumerable<LogEntry> entries);

        /// <summary>
        /// 结构化查询日志
        /// </summary>
        Task<List<LogEntry>> QueryLogsAsync(LogQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取日志统计
        /// </summary>
        Task<LogStatistics> GetStatisticsAsync(TimeSpan timeRange, CancellationToken cancellationToken = default);

        /// <summary>
        /// 导出日志
        /// </summary>
        Task<Stream> ExportLogsAsync(LogExportOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理旧日志
        /// </summary>
        Task<int> CleanupLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 性能跟踪器接口
    /// </summary>
    public interface IPerformanceTracker : IDisposable
    {
        /// <summary>
        /// 操作名称
        /// </summary>
        string Operation { get; }

        /// <summary>
        /// 开始时间
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// 添加上下文数据
        /// </summary>
        void AddContext(string key, object value);

        /// <summary>
        /// 添加性能指标
        /// </summary>
        void AddMetric(string name, double value);

        /// <summary>
        /// 标记检查点
        /// </summary>
        void Checkpoint(string name);

        /// <summary>
        /// 完成跟踪
        /// </summary>
        Task CompleteAsync(string? result = null);
    }

    /// <summary>
    /// 安全事件类型
    /// </summary>
    public enum SecurityEventType
    {
        Login,              // 登录
        Logout,             // 登出
        LoginFailed,        // 登录失败
        PasswordChange,     // 密码修改
        AccountLocked,      // 账户锁定
        UnauthorizedAccess, // 未授权访问
        PermissionDenied,   // 权限拒绝
        DataBreach,         // 数据泄露
        SuspiciousActivity, // 可疑活动
        ConfigurationChange // 配置更改
    }

    /// <summary>
    /// 性能指标
    /// </summary>
    public class PerformanceMetrics
    {
        /// <summary>
        /// CPU使用率（百分比）
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// 内存使用量（MB）
        /// </summary>
        public long MemoryUsageMB { get; set; }

        /// <summary>
        /// 数据库查询次数
        /// </summary>
        public int DatabaseQueries { get; set; }

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public int CacheHits { get; set; }

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        public int CacheMisses { get; set; }

        /// <summary>
        /// HTTP请求数量
        /// </summary>
        public int HttpRequests { get; set; }

        /// <summary>
        /// 异常数量
        /// </summary>
        public int ExceptionCount { get; set; }

        /// <summary>
        /// 自定义指标
        /// </summary>
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// 日志类别
        /// </summary>
        public LogCategory Category { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 操作ID（用于关联）
        /// </summary>
        public string? OperationId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 调用者信息
        /// </summary>
        public CallerInfo? CallerInfo { get; set; }

        /// <summary>
        /// 上下文数据
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();

        /// <summary>
        /// 性能指标
        /// </summary>
        public PerformanceMetrics? Metrics { get; set; }

        /// <summary>
        /// 持续时间
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// 机器名
        /// </summary>
        public string MachineName { get; set; } = System.Environment.MachineName;

        /// <summary>
        /// 应用程序名
        /// </summary>
        public string ApplicationName { get; set; } = "LYBT";

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 环境名
        /// </summary>
        public string Environment { get; set; } = "Unknown";
    }

    /// <summary>
    /// 日志类别
    /// </summary>
    public enum LogCategory
    {
        General,        // 一般
        Performance,    // 性能
        Security,       // 安全
        Audit,          // 审计
        Operation,      // 业务操作
        Error,          // 错误
        Debug,          // 调试
        System          // 系统
    }

    /// <summary>
    /// 调用者信息
    /// </summary>
    public class CallerInfo
    {
        /// <summary>
        /// 方法名
        /// </summary>
        public string? MemberName { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 类名
        /// </summary>
        public string? ClassName => ExtractClassName(FilePath);

        private static string? ExtractClassName(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }
    }

    /// <summary>
    /// 日志查询
    /// </summary>
    public class LogQuery
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel? Level { get; set; }

        /// <summary>
        /// 日志类别
        /// </summary>
        public LogCategory? Category { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 操作ID
        /// </summary>
        public string? OperationId { get; set; }

        /// <summary>
        /// 消息关键词
        /// </summary>
        public string? MessageKeyword { get; set; }

        /// <summary>
        /// 异常类型
        /// </summary>
        public string? ExceptionType { get; set; }

        /// <summary>
        /// 机器名
        /// </summary>
        public string? MachineName { get; set; }

        /// <summary>
        /// 分页参数
        /// </summary>
        public PaginationQuery Pagination { get; set; } = new();

        /// <summary>
        /// 排序字段
        /// </summary>
        public string SortBy { get; set; } = "Timestamp";

        /// <summary>
        /// 排序方向
        /// </summary>
        public SortDirection SortDirection { get; set; } = SortDirection.Descending;
    }

    /// <summary>
    /// 分页查询
    /// </summary>
    public class PaginationQuery
    {
        /// <summary>
        /// 页码（从1开始）
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// 跳过的记录数
        /// </summary>
        public int Skip => (PageIndex - 1) * PageSize;
    }

    /// <summary>
    /// 排序方向
    /// </summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// 日志统计
    /// </summary>
    public class LogStatistics
    {
        /// <summary>
        /// 时间范围
        /// </summary>
        public TimeSpan TimeRange { get; set; }

        /// <summary>
        /// 总日志数
        /// </summary>
        public long TotalLogs { get; set; }

        /// <summary>
        /// 按级别统计
        /// </summary>
        public Dictionary<LogLevel, long> LogsByLevel { get; set; } = new();

        /// <summary>
        /// 按类别统计
        /// </summary>
        public Dictionary<LogCategory, long> LogsByCategory { get; set; } = new();

        /// <summary>
        /// 错误率
        /// </summary>
        public double ErrorRate => TotalLogs > 0 ? (double)(LogsByLevel.GetValueOrDefault(LogLevel.Error) + LogsByLevel.GetValueOrDefault(LogLevel.Critical)) / TotalLogs : 0;

        /// <summary>
        /// 平均性能指标
        /// </summary>
        public PerformanceMetrics? AverageMetrics { get; set; }

        /// <summary>
        /// 热点操作（出现频率最高的操作）
        /// </summary>
        public List<HotOperation> HotOperations { get; set; } = new();

        /// <summary>
        /// 活跃用户数
        /// </summary>
        public int ActiveUsers { get; set; }

        /// <summary>
        /// 异常类型统计
        /// </summary>
        public Dictionary<string, int> ExceptionTypes { get; set; } = new();
    }

    /// <summary>
    /// 热点操作
    /// </summary>
    public class HotOperation
    {
        /// <summary>
        /// 操作名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 调用次数
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// 平均耗时（毫秒）
        /// </summary>
        public double AverageTimeMs { get; set; }

        /// <summary>
        /// 错误次数
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// 错误率
        /// </summary>
        public double ErrorRate => Count > 0 ? (double)ErrorCount / Count : 0;
    }

    /// <summary>
    /// 日志导出选项
    /// </summary>
    public class LogExportOptions
    {
        /// <summary>
        /// 查询条件
        /// </summary>
        public LogQuery Query { get; set; } = new();

        /// <summary>
        /// 导出格式
        /// </summary>
        public ExportFormat Format { get; set; } = ExportFormat.Json;

        /// <summary>
        /// 是否压缩
        /// </summary>
        public bool Compress { get; set; } = true;

        /// <summary>
        /// 包含的字段
        /// </summary>
        public List<string> IncludeFields { get; set; } = new();

        /// <summary>
        /// 排除的字段
        /// </summary>
        public List<string> ExcludeFields { get; set; } = new();

        /// <summary>
        /// 最大导出记录数
        /// </summary>
        public int MaxRecords { get; set; } = 100000;
    }

    /// <summary>
    /// 导出格式
    /// </summary>
    public enum ExportFormat
    {
        Json,
        Csv,
        Excel,
        Xml
    }
}