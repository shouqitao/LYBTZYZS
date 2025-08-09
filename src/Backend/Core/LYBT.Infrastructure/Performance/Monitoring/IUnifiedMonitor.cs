namespace LYBT.Infrastructure.Performance.Monitoring
{
    /// <summary>
    /// 统一监控系统接口
    /// UltraThink设计：为监控系统提供清晰、统一的接口契约
    /// </summary>
    public interface IUnifiedMonitor
    {
        #region 性能监控

        /// <summary>
        /// 开始API性能监控
        /// </summary>
        Task<string> StartApiPerformanceMonitoringAsync(string apiEndpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// 结束API性能监控
        /// </summary>
        Task<ApiPerformanceResult> StopApiPerformanceMonitoringAsync(string monitoringId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取性能监控报告
        /// </summary>
        Task<PerformanceReport> GetPerformanceReportAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

        #endregion

        #region 日志分析

        /// <summary>
        /// 分析应用日志
        /// </summary>
        Task<LogAnalysisResult> AnalyzeApplicationLogsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检测异常日志模式
        /// </summary>
        Task<List<LogPattern>> DetectAnomalousLogPatternsAsync(CancellationToken cancellationToken = default);

        #endregion

        #region 错误追踪

        /// <summary>
        /// 记录应用错误
        /// </summary>
        Task TrackErrorAsync(Exception exception, string context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取错误统计报告
        /// </summary>
        Task<ErrorStatisticsReport> GetErrorStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取关键错误列表
        /// </summary>
        Task<List<CriticalError>> GetCriticalErrorsAsync(int topCount = 10, CancellationToken cancellationToken = default);

        #endregion

        #region 监控仪表板

        /// <summary>
        /// 获取监控仪表板数据
        /// </summary>
        Task<MonitoringDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取实时系统健康状态
        /// </summary>
        Task<SystemHealthStatus> GetSystemHealthStatusAsync(CancellationToken cancellationToken = default);

        #endregion

        #region 监控系统管理

        /// <summary>
        /// 启动监控系统
        /// </summary>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 停止监控系统
        /// </summary>
        Task StopMonitoringAsync(CancellationToken cancellationToken = default);

        #endregion
    }
}