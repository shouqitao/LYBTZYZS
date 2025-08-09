namespace LYBT.Infrastructure.Performance.Monitoring
{
    #region 性能监控数据模型

    /// <summary>
    /// API性能监控结果
    /// </summary>
    public class ApiPerformanceResult
    {
        public string MonitoringId { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long ResponseTimeMs { get; set; }
        public int StatusCode { get; set; }
        public long RequestSizeBytes { get; set; }
        public long ResponseSizeBytes { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }

    /// <summary>
    /// 性能报告
    /// </summary>
    public class PerformanceReport
    {
        public DateTime ReportStartTime { get; set; }
        public DateTime ReportEndTime { get; set; }
        public int TotalApiCalls { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double MedianResponseTimeMs { get; set; }
        public long MaxResponseTimeMs { get; set; }
        public long MinResponseTimeMs { get; set; }
        public int ErrorCount { get; set; }
        public double ErrorRate { get; set; }
        public List<ApiPerformanceResult> SlowestApis { get; set; } = new();
        public List<ApiPerformanceResult> MostFrequentApis { get; set; } = new();
        public Dictionary<string, int> StatusCodeDistribution { get; set; } = new();
        public List<string> PerformanceInsights { get; set; } = new();
    }

    #endregion

    #region 日志分析数据模型

    /// <summary>
    /// 日志分析结果
    /// </summary>
    public class LogAnalysisResult
    {
        public DateTime AnalysisStartTime { get; set; }
        public DateTime AnalysisEndTime { get; set; }
        public int TotalLogEntries { get; set; }
        public int ErrorLogCount { get; set; }
        public int WarningLogCount { get; set; }
        public int InfoLogCount { get; set; }
        public List<LogPattern> DetectedPatterns { get; set; } = new();
        public List<LogAnomaly> Anomalies { get; set; } = new();
        public Dictionary<string, int> LogLevelDistribution { get; set; } = new();
        public List<string> TopErrorMessages { get; set; } = new();
        public List<string> AnalysisInsights { get; set; } = new();
    }

    /// <summary>
    /// 日志模式
    /// </summary>
    public class LogPattern
    {
        public string PatternId { get; set; } = Guid.NewGuid().ToString();
        public string PatternDescription { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public int Frequency { get; set; }
        public PatternSeverity Severity { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public List<string> ExampleMessages { get; set; } = new();
    }

    /// <summary>
    /// 日志异常
    /// </summary>
    public class LogAnomaly
    {
        public string AnomalyId { get; set; } = Guid.NewGuid().ToString();
        public string AnomalyType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AnomalySeverity Severity { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Evidence { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
    }

    #endregion

    #region 错误追踪数据模型

    /// <summary>
    /// 错误统计报告
    /// </summary>
    public class ErrorStatisticsReport
    {
        public DateTime ReportStartTime { get; set; }
        public DateTime ReportEndTime { get; set; }
        public int TotalErrors { get; set; }
        public int CriticalErrors { get; set; }
        public int UnhandledExceptions { get; set; }
        public Dictionary<string, int> ErrorTypeDistribution { get; set; } = new();
        public Dictionary<string, int> ErrorSourceDistribution { get; set; } = new();
        public List<CriticalError> TopCriticalErrors { get; set; } = new();
        public List<ErrorTrend> ErrorTrends { get; set; } = new();
        public double ErrorRate { get; set; }
        public string MostCommonErrorType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 关键错误
    /// </summary>
    public class CriticalError
    {
        public string ErrorId { get; set; } = Guid.NewGuid().ToString();
        public string ErrorType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public int OccurrenceCount { get; set; }
        public ErrorSeverity Severity { get; set; }
        public string Context { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    /// <summary>
    /// 错误趋势
    /// </summary>
    public class ErrorTrend
    {
        public DateTime TimeSlot { get; set; }
        public int ErrorCount { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public TrendDirection Direction { get; set; }
    }

    #endregion

    #region 监控仪表板数据模型

    /// <summary>
    /// 监控仪表板数据
    /// </summary>
    public class MonitoringDashboardData
    {
        public DateTime GeneratedAt { get; set; }
        public SystemHealthStatus SystemHealth { get; set; } = new();
        public PerformanceReport PerformanceSummary { get; set; } = new();
        public ErrorStatisticsReport ErrorSummary { get; set; } = new();
        public LogAnalysisResult LogSummary { get; set; } = new();
        public List<SystemAlert> ActiveAlerts { get; set; } = new();
        public Dictionary<string, object> CustomMetrics { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
    }

    /// <summary>
    /// 系统健康状态
    /// </summary>
    public class SystemHealthStatus
    {
        public HealthStatus OverallStatus { get; set; }
        public double OverallScore { get; set; }
        public DateTime LastCheckTime { get; set; }
        public List<HealthCheckResult> HealthChecks { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// 健康检查结果
    /// </summary>
    public class HealthCheckResult
    {
        public string CheckName { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public string Description { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// 系统警报
    /// </summary>
    public class SystemAlert
    {
        public string AlertId { get; set; } = Guid.NewGuid().ToString();
        public string AlertType { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }
        public bool IsAcknowledged { get; set; }
        public string? AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion

    #region 枚举定义

    /// <summary>
    /// 模式严重程度
    /// </summary>
    public enum PatternSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// 异常严重程度
    /// </summary>
    public enum AnomalySeverity
    {
        Minor = 1,
        Moderate = 2,
        Major = 3,
        Critical = 4
    }

    /// <summary>
    /// 错误严重程度
    /// </summary>
    public enum ErrorSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// 趋势方向
    /// </summary>
    public enum TrendDirection
    {
        Decreasing = -1,
        Stable = 0,
        Increasing = 1
    }

    /// <summary>
    /// 健康状态
    /// </summary>
    public enum HealthStatus
    {
        Critical = 0,
        Degraded = 1,
        Healthy = 2
    }

    /// <summary>
    /// 警报严重程度
    /// </summary>
    public enum AlertSeverity
    {
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    #endregion
}