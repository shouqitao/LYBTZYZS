using LYBT.Infrastructure.Logging;

namespace LYBT.Infrastructure.Monitoring
{
    /// <summary>
    /// 统一监控管理器接口 - UltraThink监控优化
    /// </summary>
    public interface IUnifiedMonitor
    {
        /// <summary>
        /// 开始监控
        /// </summary>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 停止监控
        /// </summary>
        Task StopMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加告警规则
        /// </summary>
        Task AddAlertRuleAsync(AlertRule rule, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除告警规则
        /// </summary>
        Task RemoveAlertRuleAsync(string ruleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取系统健康状态
        /// </summary>
        Task<SystemHealth> GetSystemHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取性能指标
        /// </summary>
        Task<PerformanceReport> GetPerformanceReportAsync(TimeSpan timeRange, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取活跃的告警
        /// </summary>
        Task<List<ActiveAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 确认告警
        /// </summary>
        Task AcknowledgeAlertAsync(string alertId, string acknowledgedBy, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取监控统计
        /// </summary>
        Task<MonitoringStatistics> GetMonitoringStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 记录自定义指标
        /// </summary>
        Task RecordCustomMetricAsync(string name, double value, Dictionary<string, object>? tags = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置监控阈值
        /// </summary>
        Task SetThresholdAsync(string metricName, ThresholdDefinition threshold, CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成监控报告
        /// </summary>
        Task<Stream> GenerateReportAsync(ReportOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// 订阅告警事件
        /// </summary>
        void SubscribeToAlerts(IAlertSubscriber subscriber);

        /// <summary>
        /// 取消订阅告警事件
        /// </summary>
        void UnsubscribeFromAlerts(IAlertSubscriber subscriber);
    }

    /// <summary>
    /// 告警规则
    /// </summary>
    public class AlertRule
    {
        /// <summary>
        /// 规则ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 规则名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 指标名称
        /// </summary>
        public string MetricName { get; set; } = string.Empty;

        /// <summary>
        /// 条件类型
        /// </summary>
        public AlertConditionType ConditionType { get; set; }

        /// <summary>
        /// 阈值
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// 持续时间（满足条件的持续时间才触发告警）
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 严重程度
        /// </summary>
        public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 告警操作
        /// </summary>
        public List<AlertAction> Actions { get; set; } = new();

        /// <summary>
        /// 标签过滤器
        /// </summary>
        public Dictionary<string, string> TagFilters { get; set; } = new();

        /// <summary>
        /// 静默期（告警触发后的静默时间）
        /// </summary>
        public TimeSpan SilencePeriod { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建者
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// 告警条件类型
    /// </summary>
    public enum AlertConditionType
    {
        GreaterThan,        // 大于
        LessThan,           // 小于
        Equals,             // 等于
        NotEquals,          // 不等于
        Contains,           // 包含
        NotContains,        // 不包含
        PercentageChange,   // 百分比变化
        AbsoluteChange      // 绝对值变化
    }

    /// <summary>
    /// 告警严重程度
    /// </summary>
    public enum AlertSeverity
    {
        Info,       // 信息
        Warning,    // 警告
        Critical,   // 严重
        Emergency   // 紧急
    }

    /// <summary>
    /// 告警操作
    /// </summary>
    public class AlertAction
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public AlertActionType Type { get; set; }

        /// <summary>
        /// 配置参数
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    /// <summary>
    /// 告警操作类型
    /// </summary>
    public enum AlertActionType
    {
        Email,          // 邮件通知
        Sms,            // 短信通知
        Webhook,        // Webhook调用
        Log,            // 记录日志
        ExecuteCommand, // 执行命令
        RestartService  // 重启服务
    }

    /// <summary>
    /// 系统健康状态
    /// </summary>
    public class SystemHealth
    {
        /// <summary>
        /// 整体状态
        /// </summary>
        public HealthStatus OverallStatus { get; set; }

        /// <summary>
        /// 检查时间
        /// </summary>
        public DateTime CheckTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 组件健康状态
        /// </summary>
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();

        /// <summary>
        /// 系统指标
        /// </summary>
        public SystemMetrics Metrics { get; set; } = new();

        /// <summary>
        /// 健康评分（0-100）
        /// </summary>
        public double HealthScore { get; set; }

        /// <summary>
        /// 健康总结
        /// </summary>
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 健康状态
    /// </summary>
    public enum HealthStatus
    {
        Healthy,    // 健康
        Degraded,   // 降级
        Unhealthy,  // 不健康
        Critical    // 严重
    }

    /// <summary>
    /// 组件健康状态
    /// </summary>
    public class ComponentHealth
    {
        /// <summary>
        /// 组件名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        public HealthStatus Status { get; set; }

        /// <summary>
        /// 状态描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 响应时间（毫秒）
        /// </summary>
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// 详细信息
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// 系统指标
    /// </summary>
    public class SystemMetrics
    {
        /// <summary>
        /// CPU使用率（百分比）
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// 内存使用率（百分比）
        /// </summary>
        public double MemoryUsagePercent { get; set; }

        /// <summary>
        /// 磁盘使用率（百分比）
        /// </summary>
        public double DiskUsagePercent { get; set; }

        /// <summary>
        /// 网络接收速率（MB/s）
        /// </summary>
        public double NetworkReceiveMBps { get; set; }

        /// <summary>
        /// 网络发送速率（MB/s）
        /// </summary>
        public double NetworkTransmitMBps { get; set; }

        /// <summary>
        /// 活动连接数
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// 请求速率（请求/分钟）
        /// </summary>
        public double RequestsPerMinute { get; set; }

        /// <summary>
        /// 错误率（百分比）
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// 平均响应时间（毫秒）
        /// </summary>
        public double AverageResponseTimeMs { get; set; }
    }

    /// <summary>
    /// 性能报告
    /// </summary>
    public class PerformanceReport
    {
        /// <summary>
        /// 报告时间范围
        /// </summary>
        public TimeSpan TimeRange { get; set; }

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 性能趋势
        /// </summary>
        public List<PerformanceTrend> Trends { get; set; } = new();

        /// <summary>
        /// 性能基准对比
        /// </summary>
        public PerformanceBenchmark Benchmark { get; set; } = new();

        /// <summary>
        /// 异常检测结果
        /// </summary>
        public List<AnomalyDetection> Anomalies { get; set; } = new();

        /// <summary>
        /// 性能建议
        /// </summary>
        public List<PerformanceRecommendation> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// 性能趋势
    /// </summary>
    public class PerformanceTrend
    {
        /// <summary>
        /// 指标名称
        /// </summary>
        public string MetricName { get; set; } = string.Empty;

        /// <summary>
        /// 数据点
        /// </summary>
        public List<DataPoint> DataPoints { get; set; } = new();

        /// <summary>
        /// 趋势方向
        /// </summary>
        public TrendDirection Direction { get; set; }

        /// <summary>
        /// 变化百分比
        /// </summary>
        public double ChangePercent { get; set; }
    }

    /// <summary>
    /// 数据点
    /// </summary>
    public class DataPoint
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 趋势方向
    /// </summary>
    public enum TrendDirection
    {
        Up,         // 上升
        Down,       // 下降
        Stable,     // 稳定
        Volatile    // 波动
    }

    /// <summary>
    /// 性能基准
    /// </summary>
    public class PerformanceBenchmark
    {
        /// <summary>
        /// 基准指标
        /// </summary>
        public Dictionary<string, double> BaselineMetrics { get; set; } = new();

        /// <summary>
        /// 当前指标
        /// </summary>
        public Dictionary<string, double> CurrentMetrics { get; set; } = new();

        /// <summary>
        /// 性能评分（0-100）
        /// </summary>
        public double PerformanceScore { get; set; }

        /// <summary>
        /// 改进建议
        /// </summary>
        public List<string> Improvements { get; set; } = new();
    }

    /// <summary>
    /// 异常检测
    /// </summary>
    public class AnomalyDetection
    {
        /// <summary>
        /// 指标名称
        /// </summary>
        public string MetricName { get; set; } = string.Empty;

        /// <summary>
        /// 异常类型
        /// </summary>
        public AnomalyType Type { get; set; }

        /// <summary>
        /// 异常值
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// 预期值
        /// </summary>
        public double ExpectedValue { get; set; }

        /// <summary>
        /// 偏差程度
        /// </summary>
        public double DeviationPercent { get; set; }

        /// <summary>
        /// 检测时间
        /// </summary>
        public DateTime DetectedAt { get; set; }

        /// <summary>
        /// 严重程度
        /// </summary>
        public AlertSeverity Severity { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 异常类型
    /// </summary>
    public enum AnomalyType
    {
        Spike,          // 尖峰
        Drop,           // 骤降
        Trend,          // 趋势异常
        Seasonal,       // 季节性异常
        Pattern         // 模式异常
    }

    /// <summary>
    /// 性能建议
    /// </summary>
    public class PerformanceRecommendation
    {
        /// <summary>
        /// 建议类型
        /// </summary>
        public RecommendationType Type { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public RecommendationPriority Priority { get; set; }

        /// <summary>
        /// 建议内容
        /// </summary>
        public string Recommendation { get; set; } = string.Empty;

        /// <summary>
        /// 预期影响
        /// </summary>
        public string ExpectedImpact { get; set; } = string.Empty;

        /// <summary>
        /// 实施难度
        /// </summary>
        public ImplementationDifficulty Difficulty { get; set; }

        /// <summary>
        /// 相关指标
        /// </summary>
        public List<string> RelatedMetrics { get; set; } = new();
    }

    /// <summary>
    /// 建议类型
    /// </summary>
    public enum RecommendationType
    {
        Performance,    // 性能优化
        Resource,       // 资源优化
        Configuration,  // 配置优化
        Architecture,   // 架构优化
        Maintenance     // 维护建议
    }

    /// <summary>
    /// 建议优先级
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// 实施难度
    /// </summary>
    public enum ImplementationDifficulty
    {
        Easy,       // 容易
        Medium,     // 中等
        Hard,       // 困难
        Complex     // 复杂
    }

    /// <summary>
    /// 活跃告警
    /// </summary>
    public class ActiveAlert
    {
        /// <summary>
        /// 告警ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 规则ID
        /// </summary>
        public string RuleId { get; set; } = string.Empty;

        /// <summary>
        /// 规则名称
        /// </summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>
        /// 严重程度
        /// </summary>
        public AlertSeverity Severity { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public AlertStatus Status { get; set; }

        /// <summary>
        /// 触发时间
        /// </summary>
        public DateTime TriggeredAt { get; set; }

        /// <summary>
        /// 确认时间
        /// </summary>
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>
        /// 确认人
        /// </summary>
        public string? AcknowledgedBy { get; set; }

        /// <summary>
        /// 解决时间
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// 告警消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 当前值
        /// </summary>
        public double CurrentValue { get; set; }

        /// <summary>
        /// 阈值
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 告警状态
    /// </summary>
    public enum AlertStatus
    {
        Active,         // 活跃
        Acknowledged,   // 已确认
        Resolved,       // 已解决
        Suppressed      // 已抑制
    }

    /// <summary>
    /// 监控统计
    /// </summary>
    public class MonitoringStatistics
    {
        /// <summary>
        /// 总告警规则数
        /// </summary>
        public int TotalRules { get; set; }

        /// <summary>
        /// 活跃告警数
        /// </summary>
        public int ActiveAlerts { get; set; }

        /// <summary>
        /// 已确认告警数
        /// </summary>
        public int AcknowledgedAlerts { get; set; }

        /// <summary>
        /// 系统健康评分
        /// </summary>
        public double HealthScore { get; set; }

        /// <summary>
        /// 监控的指标数量
        /// </summary>
        public int MonitoredMetrics { get; set; }

        /// <summary>
        /// 数据点数量
        /// </summary>
        public long DataPoints { get; set; }

        /// <summary>
        /// 平均响应时间
        /// </summary>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// 系统正常运行时间
        /// </summary>
        public TimeSpan Uptime { get; set; }
    }

    /// <summary>
    /// 阈值定义
    /// </summary>
    public class ThresholdDefinition
    {
        /// <summary>
        /// 警告阈值
        /// </summary>
        public double? WarningThreshold { get; set; }

        /// <summary>
        /// 严重阈值
        /// </summary>
        public double? CriticalThreshold { get; set; }

        /// <summary>
        /// 紧急阈值
        /// </summary>
        public double? EmergencyThreshold { get; set; }

        /// <summary>
        /// 阈值类型
        /// </summary>
        public ThresholdType Type { get; set; } = ThresholdType.Absolute;
    }

    /// <summary>
    /// 阈值类型
    /// </summary>
    public enum ThresholdType
    {
        Absolute,       // 绝对值
        Percentage,     // 百分比
        Rate,           // 变化率
        Moving          // 移动平均
    }

    /// <summary>
    /// 报告选项
    /// </summary>
    public class ReportOptions
    {
        /// <summary>
        /// 报告类型
        /// </summary>
        public ReportType Type { get; set; }

        /// <summary>
        /// 时间范围
        /// </summary>
        public TimeSpan TimeRange { get; set; }

        /// <summary>
        /// 包含的指标
        /// </summary>
        public List<string> IncludeMetrics { get; set; } = new();

        /// <summary>
        /// 输出格式
        /// </summary>
        public ReportFormat Format { get; set; } = ReportFormat.Html;

        /// <summary>
        /// 详细程度
        /// </summary>
        public ReportDetailLevel DetailLevel { get; set; } = ReportDetailLevel.Summary;
    }

    /// <summary>
    /// 报告类型
    /// </summary>
    public enum ReportType
    {
        HealthReport,       // 健康报告
        PerformanceReport,  // 性能报告
        AlertReport,        // 告警报告
        TrendAnalysis,      // 趋势分析
        ComprehensiveReport // 综合报告
    }

    /// <summary>
    /// 报告格式
    /// </summary>
    public enum ReportFormat
    {
        Html,
        Pdf,
        Json,
        Csv
    }

    /// <summary>
    /// 报告详细程度
    /// </summary>
    public enum ReportDetailLevel
    {
        Summary,    // 摘要
        Standard,   // 标准
        Detailed,   // 详细
        Verbose     // 详尽
    }

    /// <summary>
    /// 告警订阅者接口
    /// </summary>
    public interface IAlertSubscriber
    {
        /// <summary>
        /// 处理告警
        /// </summary>
        Task HandleAlertAsync(ActiveAlert alert, CancellationToken cancellationToken = default);

        /// <summary>
        /// 订阅者ID
        /// </summary>
        string SubscriberId { get; }

        /// <summary>
        /// 支持的告警严重程度
        /// </summary>
        List<AlertSeverity> SupportedSeverities { get; }
    }
}