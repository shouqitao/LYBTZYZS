using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Configuration;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// UltraThink Phase 5.4: 性能分析和报告服务
    /// 整合所有性能数据，生成综合报告和趋势分析
    /// </summary>
    public interface IPerformanceAnalysisService
    {
        /// <summary>
        /// 生成综合性能报告
        /// </summary>
        Task<ComprehensivePerformanceReport> GenerateComprehensiveReportAsync(DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// 分析性能趋势
        /// </summary>
        Task<PerformanceTrendAnalysis> AnalyzeTrendsAsync(TimeSpan period);

        /// <summary>
        /// 检测性能异常
        /// </summary>
        Task<List<PerformanceAnomaly>> DetectAnomaliesAsync();

        /// <summary>
        /// 生成性能优化建议
        /// </summary>
        Task<List<PerformanceRecommendation>> GenerateOptimizationRecommendationsAsync();

        /// <summary>
        /// 获取性能健康评分
        /// </summary>
        PerformanceHealthScore GetHealthScore();

        /// <summary>
        /// 导出性能数据
        /// </summary>
        Task<string> ExportPerformanceDataAsync(PerformanceDataExportOptions options);

        /// <summary>
        /// 启动实时监控
        /// </summary>
        void StartRealTimeMonitoring();

        /// <summary>
        /// 停止实时监控
        /// </summary>
        void StopRealTimeMonitoring();

        /// <summary>
        /// 性能警报事件
        /// </summary>
        event EventHandler<PerformanceAlertEventArgs> PerformanceAlert;
    }

    /// <summary>
    /// 性能分析和报告服务实现
    /// </summary>
    public class PerformanceAnalysisService : IPerformanceAnalysisService, IDisposable
    {
        private readonly ILogger<PerformanceAnalysisService> _logger;
        private readonly IAppConfiguration _configuration;
        private readonly IPerformanceMonitorService _performanceMonitor;
        private readonly IMemoryManagerService _memoryManager;
        private readonly IUIPerformanceOptimizer _uiOptimizer;
        private readonly ISmartVirtualizationManager _virtualizationManager;
        private readonly ISmartLoadingStrategy _loadingStrategy;
        private readonly IDataBindingOptimizer _bindingOptimizer;

        private readonly Timer? _realTimeMonitoringTimer;
        private bool _realTimeMonitoringEnabled;
        private readonly List<PerformanceSnapshot> _performanceHistory = new();
        private readonly object _historyLock = new object();

        public event EventHandler<PerformanceAlertEventArgs>? PerformanceAlert;

        public PerformanceAnalysisService(
            ILogger<PerformanceAnalysisService> logger,
            IAppConfiguration configuration,
            IPerformanceMonitorService performanceMonitor,
            IMemoryManagerService memoryManager,
            IUIPerformanceOptimizer uiOptimizer,
            ISmartVirtualizationManager virtualizationManager,
            ISmartLoadingStrategy loadingStrategy,
            IDataBindingOptimizer bindingOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _uiOptimizer = uiOptimizer ?? throw new ArgumentNullException(nameof(uiOptimizer));
            _virtualizationManager = virtualizationManager ?? throw new ArgumentNullException(nameof(virtualizationManager));
            _loadingStrategy = loadingStrategy ?? throw new ArgumentNullException(nameof(loadingStrategy));
            _bindingOptimizer = bindingOptimizer ?? throw new ArgumentNullException(nameof(bindingOptimizer));

            // 实时监控定时器（每30秒采集一次性能快照）
            _realTimeMonitoringTimer = new Timer(CapturePerformanceSnapshot, null, Timeout.Infinite, Timeout.Infinite);

            _logger.LogInformation("性能分析服务已启动");
        }

        public async Task<ComprehensivePerformanceReport> GenerateComprehensiveReportAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            var start = startTime ?? DateTime.Now.AddHours(-24); // 默认24小时
            var end = endTime ?? DateTime.Now;

            _logger.LogInformation("生成综合性能报告: {StartTime} - {EndTime}", start, end);

            var report = new ComprehensivePerformanceReport
            {
                GeneratedAt = DateTime.Now,
                StartTime = start,
                EndTime = end,
                Duration = end - start
            };

            try
            {
                // 收集各个组件的统计数据
                report.PerformanceStatistics = _performanceMonitor.GetStatistics(end - start);
                report.MemoryUsage = _memoryManager.GetMemoryUsage();
                report.UIPerformanceStatistics = _uiOptimizer.GetPerformanceStatistics();
                report.VirtualizationStatistics = _virtualizationManager.GetStatistics();
                report.LoadingStatistics = _loadingStrategy.GetStatistics();
                report.BindingStatistics = _bindingOptimizer.GetStatistics();

                // 生成详细的性能报告
                var detailedReport = await _performanceMonitor.GenerateReportAsync(start, end);
                report.DetailedReport = detailedReport;

                // 计算性能指标
                report.HealthScore = CalculateHealthScore(report);
                report.PerformanceGrade = CalculatePerformanceGrade(report.HealthScore.OverallScore);

                // 生成问题和建议
                report.DetectedIssues = await AnalyzePerformanceIssues(report);
                report.Recommendations = await GenerateRecommendations(report);

                // 生成摘要
                report.Summary = GenerateReportSummary(report);

                _logger.LogInformation("综合性能报告生成完成，健康评分: {HealthScore}", report.HealthScore.OverallScore);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成综合性能报告失败");
                throw;
            }
        }

        public async Task<PerformanceTrendAnalysis> AnalyzeTrendsAsync(TimeSpan period)
        {
            _logger.LogInformation("分析性能趋势，周期: {Period}", period);

            var analysis = new PerformanceTrendAnalysis
            {
                AnalysisDate = DateTime.Now,
                Period = period
            };

            try
            {
                lock (_historyLock)
                {
                    var cutoffTime = DateTime.Now - period;
                    var relevantSnapshots = _performanceHistory
                        .Where(s => s.Timestamp >= cutoffTime)
                        .OrderBy(s => s.Timestamp)
                        .ToList();

                    if (relevantSnapshots.Count < 2)
                    {
                        analysis.HasSufficientData = false;
                        analysis.Message = "数据不足，无法进行趋势分析";
                        return analysis;
                    }

                    analysis.HasSufficientData = true;
                    analysis.DataPoints = relevantSnapshots.Count;

                    // 分析内存使用趋势
                    analysis.MemoryTrend = AnalyzeMemoryTrend(relevantSnapshots);

                    // 分析响应时间趋势
                    analysis.ResponseTimeTrend = AnalyzeResponseTimeTrend(relevantSnapshots);

                    // 分析缓存命中率趋势
                    analysis.CacheHitRateTrend = AnalyzeCacheHitRateTrend(relevantSnapshots);

                    // 分析错误率趋势
                    analysis.ErrorRateTrend = AnalyzeErrorRateTrend(relevantSnapshots);

                    // 生成趋势总结
                    analysis.TrendSummary = GenerateTrendSummary(analysis);
                }

                await Task.CompletedTask;
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "性能趋势分析失败");
                throw;
            }
        }

        public async Task<List<PerformanceAnomaly>> DetectAnomaliesAsync()
        {
            var anomalies = new List<PerformanceAnomaly>();

            try
            {
                // 检测内存异常
                var memoryUsage = _memoryManager.GetMemoryUsage();
                if (memoryUsage.MemoryUsagePercentage > 90)
                {
                    anomalies.Add(new PerformanceAnomaly
                    {
                        Type = "HighMemoryUsage",
                        Severity = "Critical",
                        Description = $"内存使用率过高: {memoryUsage.MemoryUsagePercentage:F1}%",
                        DetectedAt = DateTime.Now,
                        Value = memoryUsage.MemoryUsagePercentage,
                        Threshold = 90.0,
                        Component = "MemoryManager"
                    });
                }

                // 检测UI性能异常
                var uiStats = _uiOptimizer.GetPerformanceStatistics();
                if (uiStats.SlowOperationPercentage > 20)
                {
                    anomalies.Add(new PerformanceAnomaly
                    {
                        Type = "HighSlowUIOperations",
                        Severity = "High",
                        Description = $"UI慢操作比例过高: {uiStats.SlowOperationPercentage:F1}%",
                        DetectedAt = DateTime.Now,
                        Value = uiStats.SlowOperationPercentage,
                        Threshold = 20.0,
                        Component = "UIOptimizer"
                    });
                }

                // 检测绑定性能异常
                var bindingStats = _bindingOptimizer.GetStatistics();
                if (bindingStats.ThrottleRate > 50)
                {
                    anomalies.Add(new PerformanceAnomaly
                    {
                        Type = "HighBindingThrottleRate",
                        Severity = "Medium",
                        Description = $"绑定节流率过高: {bindingStats.ThrottleRate:F1}%",
                        DetectedAt = DateTime.Now,
                        Value = bindingStats.ThrottleRate,
                        Threshold = 50.0,
                        Component = "BindingOptimizer"
                    });
                }

                _logger.LogInformation("检测到 {Count} 个性能异常", anomalies.Count);
                await Task.CompletedTask;
                return anomalies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "性能异常检测失败");
                throw;
            }
        }

        public async Task<List<PerformanceRecommendation>> GenerateOptimizationRecommendationsAsync()
        {
            var recommendations = new List<PerformanceRecommendation>();

            try
            {
                var healthScore = GetHealthScore();

                // 基于内存使用情况的建议
                if (healthScore.MemoryScore < 70)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Title = "优化内存使用",
                        Description = "内存使用效率有待提升",
                        Priority = "High",
                        Category = "Memory",
                        ActionItems = new[]
                        {
                            "检查内存泄漏",
                            "优化大对象的生命周期",
                            "调整垃圾回收策略",
                            "实施对象池模式"
                        },
                        EstimatedImpact = "20-30%内存使用减少",
                        EstimatedEffort = "Medium"
                    });
                }

                // 基于UI性能的建议
                if (healthScore.UIPerformanceScore < 70)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Title = "优化UI响应性能",
                        Description = "UI响应速度需要改善",
                        Priority = "High",
                        Category = "UI",
                        ActionItems = new[]
                        {
                            "启用虚拟化",
                            "优化数据绑定",
                            "减少UI更新频率",
                            "使用异步操作"
                        },
                        EstimatedImpact = "30-50%响应时间改善",
                        EstimatedEffort = "Medium"
                    });
                }

                // 基于缓存效率的建议
                var loadingStats = _loadingStrategy.GetStatistics();
                if (loadingStats.CacheHitRate < 80)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Title = "优化缓存策略",
                        Description = "缓存命中率偏低，需要优化缓存策略",
                        Priority = "Medium",
                        Category = "Caching",
                        ActionItems = new[]
                        {
                            "调整缓存过期时间",
                            "增加预加载策略",
                            "优化缓存键设计",
                            "实施智能预取"
                        },
                        EstimatedImpact = "15-25%加载时间减少",
                        EstimatedEffort = "Low"
                    });
                }

                _logger.LogInformation("生成了 {Count} 个优化建议", recommendations.Count);
                await Task.CompletedTask;
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成优化建议失败");
                throw;
            }
        }

        public PerformanceHealthScore GetHealthScore()
        {
            try
            {
                var healthScore = new PerformanceHealthScore();

                // 计算内存健康评分
                var memoryUsage = _memoryManager.GetMemoryUsage();
                healthScore.MemoryScore = CalculateMemoryScore(memoryUsage);

                // 计算UI性能评分
                var uiStats = _uiOptimizer.GetPerformanceStatistics();
                healthScore.UIPerformanceScore = CalculateUIPerformanceScore(uiStats);

                // 计算缓存效率评分
                var loadingStats = _loadingStrategy.GetStatistics();
                healthScore.CacheEfficiencyScore = CalculateCacheEfficiencyScore(loadingStats);

                // 计算系统稳定性评分
                var performanceStats = _performanceMonitor.GetStatistics();
                healthScore.SystemStabilityScore = CalculateSystemStabilityScore(performanceStats);

                // 计算总体评分
                healthScore.OverallScore = (healthScore.MemoryScore + healthScore.UIPerformanceScore + 
                                          healthScore.CacheEfficiencyScore + healthScore.SystemStabilityScore) / 4;

                healthScore.LastUpdated = DateTime.Now;
                healthScore.Grade = CalculatePerformanceGrade(healthScore.OverallScore);

                return healthScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算健康评分失败");
                return new PerformanceHealthScore { OverallScore = 0, Grade = "F" };
            }
        }

        public async Task<string> ExportPerformanceDataAsync(PerformanceDataExportOptions options)
        {
            try
            {
                var report = await GenerateComprehensiveReportAsync(options.StartTime, options.EndTime);
                
                return options.Format.ToLowerInvariant() switch
                {
                    "json" => System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                    "csv" => ConvertToCsv(report),
                    _ => throw new ArgumentException($"不支持的导出格式: {options.Format}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出性能数据失败");
                throw;
            }
        }

        public void StartRealTimeMonitoring()
        {
            if (_realTimeMonitoringEnabled) return;

            _realTimeMonitoringEnabled = true;
            _realTimeMonitoringTimer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(30));
            
            _logger.LogInformation("实时性能监控已启动");
        }

        public void StopRealTimeMonitoring()
        {
            if (!_realTimeMonitoringEnabled) return;

            _realTimeMonitoringEnabled = false;
            _realTimeMonitoringTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            
            _logger.LogInformation("实时性能监控已停止");
        }

        #region 私有方法

        private void CapturePerformanceSnapshot(object? state)
        {
            try
            {
                if (!_realTimeMonitoringEnabled) return;

                var snapshot = new PerformanceSnapshot
                {
                    Timestamp = DateTime.Now,
                    MemoryUsage = _memoryManager.GetMemoryUsage(),
                    PerformanceStats = _performanceMonitor.GetStatistics(),
                    UIStats = _uiOptimizer.GetPerformanceStatistics(),
                    LoadingStats = _loadingStrategy.GetStatistics(),
                    BindingStats = _bindingOptimizer.GetStatistics()
                };

                lock (_historyLock)
                {
                    _performanceHistory.Add(snapshot);
                    
                    // 限制历史数据数量（保持最近1000个快照）
                    if (_performanceHistory.Count > 1000)
                    {
                        _performanceHistory.RemoveAt(0);
                    }
                }

                // 检查是否需要发出警报
                CheckForAlerts(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "采集性能快照失败");
            }
        }

        private void CheckForAlerts(PerformanceSnapshot snapshot)
        {
            // 内存使用警报
            if (snapshot.MemoryUsage.MemoryUsagePercentage > 85)
            {
                OnPerformanceAlert("HighMemoryUsage", 
                    $"内存使用率达到 {snapshot.MemoryUsage.MemoryUsagePercentage:F1}%", 
                    "Critical");
            }

            // UI性能警报
            if (snapshot.UIStats.SlowOperationPercentage > 30)
            {
                OnPerformanceAlert("SlowUIOperations", 
                    $"UI慢操作比例达到 {snapshot.UIStats.SlowOperationPercentage:F1}%", 
                    "High");
            }
        }

        private PerformanceHealthScore CalculateHealthScore(ComprehensivePerformanceReport report)
        {
            var healthScore = new PerformanceHealthScore();

            healthScore.MemoryScore = CalculateMemoryScore(report.MemoryUsage);
            healthScore.UIPerformanceScore = CalculateUIPerformanceScore(report.UIPerformanceStatistics);
            healthScore.CacheEfficiencyScore = CalculateCacheEfficiencyScore(report.LoadingStatistics);
            healthScore.SystemStabilityScore = CalculateSystemStabilityScore(report.PerformanceStatistics);

            healthScore.OverallScore = (healthScore.MemoryScore + healthScore.UIPerformanceScore + 
                                      healthScore.CacheEfficiencyScore + healthScore.SystemStabilityScore) / 4;

            return healthScore;
        }

        private double CalculateMemoryScore(MemoryUsageInfo memoryUsage)
        {
            // 基于内存使用百分比计算评分
            var score = 100 - memoryUsage.MemoryUsagePercentage;
            return Math.Max(0, Math.Min(100, score));
        }

        private double CalculateUIPerformanceScore(UIPerformanceStatistics uiStats)
        {
            // 基于慢操作比例计算评分
            var score = 100 - uiStats.SlowOperationPercentage;
            return Math.Max(0, Math.Min(100, score));
        }

        private double CalculateCacheEfficiencyScore(LoadingStatistics loadingStats)
        {
            // 基于缓存命中率计算评分
            return Math.Max(0, Math.Min(100, loadingStats.CacheHitRate));
        }

        private double CalculateSystemStabilityScore(PerformanceStatistics performanceStats)
        {
            // 基于成功率计算评分
            return Math.Max(0, Math.Min(100, performanceStats.SuccessRate));
        }

        private string CalculatePerformanceGrade(double score)
        {
            return score switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
        }

        private async Task<List<PerformanceIssue>> AnalyzePerformanceIssues(ComprehensivePerformanceReport report)
        {
            var issues = new List<PerformanceIssue>();

            // 分析详细报告中的问题
            if (report.DetailedReport?.Issues != null)
            {
                issues.AddRange(report.DetailedReport.Issues);
            }

            await Task.CompletedTask;
            return issues;
        }

        private async Task<List<PerformanceRecommendation>> GenerateRecommendations(ComprehensivePerformanceReport report)
        {
            return await GenerateOptimizationRecommendationsAsync();
        }

        private string GenerateReportSummary(ComprehensivePerformanceReport report)
        {
            return $"性能报告摘要:\n" +
                   $"- 健康评分: {report.HealthScore.OverallScore:F1} ({report.PerformanceGrade})\n" +
                   $"- 内存使用: {report.MemoryUsage.MemoryUsagePercentage:F1}%\n" +
                   $"- 平均响应时间: {report.PerformanceStatistics.AverageOperationTime.TotalMilliseconds:F2}ms\n" +
                   $"- 缓存命中率: {report.LoadingStatistics.CacheHitRate:F1}%\n" +
                   $"- 检测到的问题: {report.DetectedIssues.Count}\n" +
                   $"- 优化建议: {report.Recommendations.Count}";
        }

        private TrendDirection AnalyzeMemoryTrend(List<PerformanceSnapshot> snapshots)
        {
            if (snapshots.Count < 2) return TrendDirection.Stable;

            var first = snapshots.First().MemoryUsage.MemoryUsagePercentage;
            var last = snapshots.Last().MemoryUsage.MemoryUsagePercentage;
            var difference = last - first;

            return difference switch
            {
                > 5 => TrendDirection.Increasing,
                < -5 => TrendDirection.Decreasing,
                _ => TrendDirection.Stable
            };
        }

        private TrendDirection AnalyzeResponseTimeTrend(List<PerformanceSnapshot> snapshots)
        {
            if (snapshots.Count < 2) return TrendDirection.Stable;

            var first = snapshots.First().PerformanceStats.AverageOperationTime.TotalMilliseconds;
            var last = snapshots.Last().PerformanceStats.AverageOperationTime.TotalMilliseconds;
            var percentageChange = (last - first) / first * 100;

            return percentageChange switch
            {
                > 10 => TrendDirection.Increasing,
                < -10 => TrendDirection.Decreasing,
                _ => TrendDirection.Stable
            };
        }

        private TrendDirection AnalyzeCacheHitRateTrend(List<PerformanceSnapshot> snapshots)
        {
            if (snapshots.Count < 2) return TrendDirection.Stable;

            var first = snapshots.First().LoadingStats.CacheHitRate;
            var last = snapshots.Last().LoadingStats.CacheHitRate;
            var difference = last - first;

            return difference switch
            {
                > 5 => TrendDirection.Increasing,
                < -5 => TrendDirection.Decreasing,
                _ => TrendDirection.Stable
            };
        }

        private TrendDirection AnalyzeErrorRateTrend(List<PerformanceSnapshot> snapshots)
        {
            if (snapshots.Count < 2) return TrendDirection.Stable;

            var first = 100 - snapshots.First().PerformanceStats.SuccessRate;
            var last = 100 - snapshots.Last().PerformanceStats.SuccessRate;
            var difference = last - first;

            return difference switch
            {
                > 2 => TrendDirection.Increasing,
                < -2 => TrendDirection.Decreasing,
                _ => TrendDirection.Stable
            };
        }

        private string GenerateTrendSummary(PerformanceTrendAnalysis analysis)
        {
            return $"性能趋势分析 ({analysis.Period}):\n" +
                   $"- 内存使用趋势: {analysis.MemoryTrend}\n" +
                   $"- 响应时间趋势: {analysis.ResponseTimeTrend}\n" +
                   $"- 缓存命中率趋势: {analysis.CacheHitRateTrend}\n" +
                   $"- 错误率趋势: {analysis.ErrorRateTrend}";
        }

        private string ConvertToCsv(ComprehensivePerformanceReport report)
        {
            // 简化的CSV转换实现
            return $"Metric,Value\n" +
                   $"Overall Score,{report.HealthScore.OverallScore:F1}\n" +
                   $"Memory Usage %,{report.MemoryUsage.MemoryUsagePercentage:F1}\n" +
                   $"Average Response Time (ms),{report.PerformanceStatistics.AverageOperationTime.TotalMilliseconds:F2}\n" +
                   $"Cache Hit Rate %,{report.LoadingStatistics.CacheHitRate:F1}\n" +
                   $"Success Rate %,{report.PerformanceStatistics.SuccessRate:F1}";
        }

        private void OnPerformanceAlert(string alertType, string message, string severity)
        {
            var args = new PerformanceAlertEventArgs
            {
                AlertType = alertType,
                Message = message,
                Severity = severity,
                Timestamp = DateTime.Now
            };

            _logger.LogWarning("性能警报: {AlertType} - {Message}", alertType, message);
            PerformanceAlert?.Invoke(this, args);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopRealTimeMonitoring();
            _realTimeMonitoringTimer?.Dispose();
        }

        #endregion
    }

    #region 支持类型

    /// <summary>
    /// 综合性能报告
    /// </summary>
    public class ComprehensivePerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        
        public PerformanceStatistics PerformanceStatistics { get; set; } = new();
        public MemoryUsageInfo MemoryUsage { get; set; } = new();
        public UIPerformanceStatistics UIPerformanceStatistics { get; set; } = new();
        public VirtualizationStatistics VirtualizationStatistics { get; set; } = new();
        public LoadingStatistics LoadingStatistics { get; set; } = new();
        public BindingStatistics BindingStatistics { get; set; } = new();
        
        public PerformanceReport DetailedReport { get; set; } = new();
        public PerformanceHealthScore HealthScore { get; set; } = new();
        public string PerformanceGrade { get; set; } = string.Empty;
        public List<PerformanceIssue> DetectedIssues { get; set; } = new();
        public List<PerformanceRecommendation> Recommendations { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 性能健康评分
    /// </summary>
    public class PerformanceHealthScore
    {
        public double OverallScore { get; set; }
        public double MemoryScore { get; set; }
        public double UIPerformanceScore { get; set; }
        public double CacheEfficiencyScore { get; set; }
        public double SystemStabilityScore { get; set; }
        public string Grade { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// 性能趋势分析
    /// </summary>
    public class PerformanceTrendAnalysis
    {
        public DateTime AnalysisDate { get; set; }
        public TimeSpan Period { get; set; }
        public bool HasSufficientData { get; set; }
        public int DataPoints { get; set; }
        public string Message { get; set; } = string.Empty;
        
        public TrendDirection MemoryTrend { get; set; }
        public TrendDirection ResponseTimeTrend { get; set; }
        public TrendDirection CacheHitRateTrend { get; set; }
        public TrendDirection ErrorRateTrend { get; set; }
        
        public string TrendSummary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 趋势方向
    /// </summary>
    public enum TrendDirection
    {
        Decreasing,
        Stable,
        Increasing
    }

    /// <summary>
    /// 性能异常
    /// </summary>
    public class PerformanceAnomaly
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public double Value { get; set; }
        public double Threshold { get; set; }
        public string Component { get; set; } = string.Empty;
    }

    /// <summary>
    /// 性能快照
    /// </summary>
    internal class PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public MemoryUsageInfo MemoryUsage { get; set; } = new();
        public PerformanceStatistics PerformanceStats { get; set; } = new();
        public UIPerformanceStatistics UIStats { get; set; } = new();
        public LoadingStatistics LoadingStats { get; set; } = new();
        public BindingStatistics BindingStats { get; set; } = new();
    }

    /// <summary>
    /// 性能数据导出选项
    /// </summary>
    public class PerformanceDataExportOptions
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Format { get; set; } = "json"; // json, csv
        public bool IncludeDetailedData { get; set; } = true;
    }

    /// <summary>
    /// 性能警报事件参数
    /// </summary>
    public class PerformanceAlertEventArgs : EventArgs
    {
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    #endregion
}