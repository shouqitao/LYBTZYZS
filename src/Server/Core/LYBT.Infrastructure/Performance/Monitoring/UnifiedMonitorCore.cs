using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Performance.Monitoring.Components;

namespace LYBT.Infrastructure.Performance.Monitoring
{
    /// <summary>
    /// 统一监控核心引擎 - UltraThink架构实现
    /// 职责单一：作为监控系统的协调器和统一接口
    /// 代码干净：简洁的组件组合和清晰的职责分离
    /// 性能出色：优化的监控数据收集和异步处理
    /// 
    /// 监控系统架构：
    /// - PerformanceMonitor: 性能监控器（API性能、数据库查询性能）
    /// - LogAnalyzer: 日志分析器（智能分析应用日志，识别异常模式）
    /// - ErrorTracker: 错误追踪器（统一错误收集、分类、预警机制）
    /// - MonitoringDashboard: 监控仪表板（统一数据展示和报警接口）
    /// </summary>
    public class UnifiedMonitorCore : IUnifiedMonitor, IDisposable
    {
        #region UltraThink专门化组件

        private readonly PerformanceMonitor _performanceMonitor;
        private readonly LogAnalyzer _logAnalyzer;
        private readonly ErrorTracker _errorTracker;
        private readonly MonitoringDashboard _monitoringDashboard;
        private readonly ILogger<UnifiedMonitorCore> _logger;

        private readonly Timer _monitoringTimer;
        private readonly CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region 构造函数

        public UnifiedMonitorCore(
            ILogger<UnifiedMonitorCore> logger,
            ILogger<PerformanceMonitor> performanceLogger,
            ILogger<LogAnalyzer> logAnalyzerLogger,
            ILogger<ErrorTracker> errorTrackerLogger,
            ILogger<MonitoringDashboard> dashboardLogger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _logger.LogDebug("开始初始化UnifiedMonitor统一监控系统");

                // 创建专门化监控组件
                _performanceMonitor = new PerformanceMonitor(performanceLogger);
                _logAnalyzer = new LogAnalyzer(logAnalyzerLogger);
                _errorTracker = new ErrorTracker(errorTrackerLogger);
                _monitoringDashboard = new MonitoringDashboard(dashboardLogger);

                // 启动定时监控任务
                _monitoringTimer = new Timer(ExecuteMonitoringCycle, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

                _logger.LogInformation("UnifiedMonitor统一监控系统初始化完成，已启动周期监控");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化UnifiedMonitor失败");
                throw;
            }
        }

        #endregion

        #region 性能监控接口（委托给PerformanceMonitor）

        /// <summary>
        /// 开始API性能监控
        /// </summary>
        public async Task<string> StartApiPerformanceMonitoringAsync(string apiEndpoint, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托API性能监控启动：{Endpoint}", apiEndpoint);
                return await _performanceMonitor.StartApiMonitoringAsync(apiEndpoint, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动API性能监控失败：{Endpoint}", apiEndpoint);
                throw;
            }
        }

        /// <summary>
        /// 结束API性能监控
        /// </summary>
        public async Task<ApiPerformanceResult> StopApiPerformanceMonitoringAsync(string monitoringId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托API性能监控结束：{MonitoringId}", monitoringId);
                var result = await _performanceMonitor.StopApiMonitoringAsync(monitoringId, cancellationToken);
                
                // 如果性能异常，自动记录到错误追踪器
                if (result.ResponseTimeMs > 5000) // 超过5秒视为性能异常
                {
                    await _errorTracker.TrackPerformanceIssueAsync(
                        $"API性能异常：{result.ApiEndpoint}，响应时间：{result.ResponseTimeMs}ms", 
                        cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "结束API性能监控失败：{MonitoringId}", monitoringId);
                throw;
            }
        }

        /// <summary>
        /// 获取性能监控报告
        /// </summary>
        public async Task<PerformanceReport> GetPerformanceReportAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托性能报告生成：{StartTime} - {EndTime}", startTime, endTime);
                return await _performanceMonitor.GeneratePerformanceReportAsync(startTime, endTime, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成性能报告失败");
                throw;
            }
        }

        #endregion

        #region 日志分析接口（委托给LogAnalyzer）

        /// <summary>
        /// 分析应用日志
        /// </summary>
        public async Task<LogAnalysisResult> AnalyzeApplicationLogsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托日志分析：{StartTime} - {EndTime}", startTime, endTime);
                return await _logAnalyzer.AnalyzeLogsAsync(startTime, endTime, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析应用日志失败");
                throw;
            }
        }

        /// <summary>
        /// 检测异常日志模式
        /// </summary>
        public async Task<List<LogPattern>> DetectAnomalousLogPatternsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托异常日志模式检测");
                return await _logAnalyzer.DetectAnomalousPatterns(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测异常日志模式失败");
                throw;
            }
        }

        #endregion

        #region 错误追踪接口（委托给ErrorTracker）

        /// <summary>
        /// 记录应用错误
        /// </summary>
        public async Task TrackErrorAsync(Exception exception, string context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托错误追踪：{ExceptionType} in {Context}", exception.GetType().Name, context);
                await _errorTracker.TrackErrorAsync(exception, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录应用错误失败");
                // 错误追踪本身失败时不抛出异常，避免影响业务流程
            }
        }

        /// <summary>
        /// 获取错误统计报告
        /// </summary>
        public async Task<ErrorStatisticsReport> GetErrorStatisticsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托错误统计报告：{StartTime} - {EndTime}", startTime, endTime);
                return await _errorTracker.GetErrorStatisticsAsync(startTime, endTime, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取错误统计报告失败");
                throw;
            }
        }

        /// <summary>
        /// 获取关键错误列表
        /// </summary>
        public async Task<List<CriticalError>> GetCriticalErrorsAsync(int topCount = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托关键错误获取：Top {Count}", topCount);
                return await _errorTracker.GetCriticalErrorsAsync(topCount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取关键错误列表失败");
                throw;
            }
        }

        #endregion

        #region 监控仪表板接口（委托给MonitoringDashboard）

        /// <summary>
        /// 获取监控仪表板数据
        /// </summary>
        public async Task<MonitoringDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托监控仪表板数据获取");

                // 并行收集各种监控数据
                var tasks = new List<Task>
                {
                    Task.Run(async () =>
                    {
                        var endTime = DateTime.UtcNow;
                        var startTime = endTime.AddHours(-1);
                        var performanceData = await _performanceMonitor.GeneratePerformanceReportAsync(startTime, endTime, cancellationToken);
                        return performanceData;
                    }, cancellationToken),

                    Task.Run(async () =>
                    {
                        var endTime = DateTime.UtcNow;
                        var startTime = endTime.AddHours(-1);
                        var errorData = await _errorTracker.GetErrorStatisticsAsync(startTime, endTime, cancellationToken);
                        return errorData;
                    }, cancellationToken),

                    Task.Run(async () =>
                    {
                        var endTime = DateTime.UtcNow;
                        var startTime = endTime.AddMinutes(-30);
                        var logData = await _logAnalyzer.AnalyzeLogsAsync(startTime, endTime, cancellationToken);
                        return logData;
                    }, cancellationToken)
                };

                await Task.WhenAll(tasks);

                return await _monitoringDashboard.GenerateDashboardDataAsync(
                    (PerformanceReport)((Task<PerformanceReport>)tasks[0]).Result,
                    (ErrorStatisticsReport)((Task<ErrorStatisticsReport>)tasks[1]).Result,
                    (LogAnalysisResult)((Task<LogAnalysisResult>)tasks[2]).Result,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取监控仪表板数据失败");
                throw;
            }
        }

        /// <summary>
        /// 获取实时系统健康状态
        /// </summary>
        public async Task<SystemHealthStatus> GetSystemHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托系统健康状态检查");
                return await _monitoringDashboard.GetSystemHealthStatusAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统健康状态失败");
                throw;
            }
        }

        #endregion

        #region 周期性监控任务

        /// <summary>
        /// 执行周期性监控任务
        /// </summary>
        private void ExecuteMonitoringCycle(object? state)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            // Fire-and-forget pattern with proper exception handling
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogDebug("开始执行周期性监控任务");

                    // 并行执行各种监控任务
                    var tasks = new List<Task>
                    {
                        _performanceMonitor.CollectPerformanceMetricsAsync(_cancellationTokenSource.Token),
                        _logAnalyzer.PeriodicLogAnalysisAsync(_cancellationTokenSource.Token),
                        _errorTracker.ProcessPendingErrorsAsync(_cancellationTokenSource.Token)
                    };

                    await Task.WhenAll(tasks);

                    // 检查系统健康状况
                    var healthStatus = await GetSystemHealthStatusAsync(_cancellationTokenSource.Token);
                    
                    if (healthStatus.OverallStatus != HealthStatus.Healthy)
                    {
                        _logger.LogWarning("系统健康状况异常：{Status}，问题数：{Issues}", 
                            healthStatus.OverallStatus, healthStatus.Issues.Count);
                    }

                    _logger.LogDebug("周期性监控任务执行完成");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "周期性监控任务执行失败");
                }
            }, _cancellationTokenSource.Token);
        }

        #endregion

        #region 监控系统管理

        /// <summary>
        /// 启动监控系统
        /// </summary>
        public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("启动统一监控系统");
                
                await _performanceMonitor.InitializeAsync(cancellationToken);
                await _logAnalyzer.InitializeAsync(cancellationToken);
                await _errorTracker.InitializeAsync(cancellationToken);
                await _monitoringDashboard.InitializeAsync(cancellationToken);

                _logger.LogInformation("统一监控系统启动完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动监控系统失败");
                throw;
            }
        }

        /// <summary>
        /// 停止监控系统
        /// </summary>
        public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("停止统一监控系统");

                _cancellationTokenSource.Cancel();
                _monitoringTimer?.Dispose();

                await _performanceMonitor.ShutdownAsync(cancellationToken);
                await _logAnalyzer.ShutdownAsync(cancellationToken);
                await _errorTracker.ShutdownAsync(cancellationToken);
                await _monitoringDashboard.ShutdownAsync(cancellationToken);

                _logger.LogInformation("统一监控系统停止完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止监控系统失败");
                throw;
            }
        }

        #endregion

        #region IDisposable实现

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _cancellationTokenSource?.Cancel();
                        _monitoringTimer?.Dispose();
                        _cancellationTokenSource?.Dispose();

                        _logger.LogDebug("UnifiedMonitor资源清理完成");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "清理UnifiedMonitor资源失败");
                    }
                }

                _disposed = true;
            }
        }

        #endregion
    }
}