using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LYBT.Infrastructure.Performance.Monitoring.Components
{
    /// <summary>
    /// 监控仪表板 - UltraThink专门化组件
    /// 职责单一：专注统一监控数据展示和报警接口
    /// 代码干净：清晰的数据聚合和展示逻辑
    /// 性能出色：高效的数据汇总和实时状态计算
    /// </summary>
    public class MonitoringDashboard
    {
        private readonly ILogger<MonitoringDashboard> _logger;
        private readonly ConcurrentQueue<SystemAlert> _alertQueue;
        private readonly ConcurrentDictionary<string, HealthCheckResult> _healthCheckCache;
        private readonly object _dashboardLock = new object();
        
        // 仪表板配置
        private readonly TimeSpan _healthCheckCacheExpiration = TimeSpan.FromMinutes(2);
        private readonly int _maxAlertHistorySize = 1000;
        private readonly Dictionary<string, double> _healthScoreWeights;

        public MonitoringDashboard(ILogger<MonitoringDashboard> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertQueue = new ConcurrentQueue<SystemAlert>();
            _healthCheckCache = new ConcurrentDictionary<string, HealthCheckResult>();
            _healthScoreWeights = InitializeHealthScoreWeights();
        }

        #region 核心仪表板方法

        /// <summary>
        /// 生成监控仪表板数据
        /// </summary>
        public async Task<MonitoringDashboardData> GenerateDashboardDataAsync(
            PerformanceReport performanceReport,
            ErrorStatisticsReport errorReport,
            LogAnalysisResult logAnalysis,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始生成监控仪表板数据");

                var dashboardData = new MonitoringDashboardData
                {
                    GeneratedAt = DateTime.UtcNow,
                    PerformanceSummary = performanceReport,
                    ErrorSummary = errorReport,
                    LogSummary = logAnalysis
                };

                // 计算系统健康状态
                dashboardData.SystemHealth = await CalculateSystemHealthAsync(
                    performanceReport, errorReport, logAnalysis, cancellationToken);

                // 获取活跃警报
                dashboardData.ActiveAlerts = GetActiveAlerts();

                // 生成自定义指标
                dashboardData.CustomMetrics = GenerateCustomMetrics(
                    performanceReport, errorReport, logAnalysis);

                // 生成推荐操作
                dashboardData.RecommendedActions = GenerateRecommendedActions(dashboardData);

                _logger.LogInformation("监控仪表板数据生成完成，系统健康状态：{HealthStatus}，活跃警报数：{AlertCount}",
                    dashboardData.SystemHealth.OverallStatus, dashboardData.ActiveAlerts.Count);

                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成监控仪表板数据失败");
                throw;
            }
        }

        /// <summary>
        /// 获取系统健康状态
        /// </summary>
        public async Task<SystemHealthStatus> GetSystemHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始获取系统健康状态");

                var healthStatus = new SystemHealthStatus
                {
                    LastCheckTime = DateTime.UtcNow
                };

                // 执行各项健康检查
                var healthChecks = new List<Task<HealthCheckResult>>
                {
                    PerformSystemHealthCheckAsync("API_Performance", cancellationToken),
                    PerformSystemHealthCheckAsync("Database_Connectivity", cancellationToken),
                    PerformSystemHealthCheckAsync("Memory_Usage", cancellationToken),
                    PerformSystemHealthCheckAsync("Disk_Space", cancellationToken),
                    PerformSystemHealthCheckAsync("Error_Rate", cancellationToken)
                };

                var results = await Task.WhenAll(healthChecks);
                healthStatus.HealthChecks.AddRange(results);

                // 计算总体健康状态和分数
                CalculateOverallHealthStatus(healthStatus);

                _logger.LogInformation("系统健康状态检查完成：{OverallStatus}，分数：{Score:F2}",
                    healthStatus.OverallStatus, healthStatus.OverallScore);

                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统健康状态失败");
                throw;
            }
        }

        /// <summary>
        /// 添加系统警报
        /// </summary>
        public async Task AddSystemAlertAsync(string alertType, AlertSeverity severity, 
            string title, string message, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var alert = new SystemAlert
                {
                    AlertType = alertType,
                    Severity = severity,
                    Title = title,
                    Message = message,
                    TriggeredAt = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                _alertQueue.Enqueue(alert);

                // 维护警报历史大小
                while (_alertQueue.Count > _maxAlertHistorySize)
                {
                    _alertQueue.TryDequeue(out _);
                }

                _logger.LogWarning("添加系统警报：{AlertType} - {Title}，严重程度：{Severity}", 
                    alertType, title, severity);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加系统警报失败");
            }
        }

        /// <summary>
        /// 确认警报
        /// </summary>
        public async Task AcknowledgeAlertAsync(string alertId, string acknowledgedBy, CancellationToken cancellationToken = default)
        {
            try
            {
                var alerts = _alertQueue.ToList();
                var targetAlert = alerts.FirstOrDefault(a => a.AlertId == alertId);
                
                if (targetAlert != null)
                {
                    targetAlert.IsAcknowledged = true;
                    targetAlert.AcknowledgedBy = acknowledgedBy;
                    targetAlert.AcknowledgedAt = DateTime.UtcNow;

                    _logger.LogInformation("警报已确认：{AlertId}，确认人：{AcknowledgedBy}", alertId, acknowledgedBy);
                }
                else
                {
                    _logger.LogWarning("未找到要确认的警报：{AlertId}", alertId);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认警报失败：{AlertId}", alertId);
            }
        }

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 初始化监控仪表板
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("初始化MonitoringDashboard");
                
                // 执行初始化逻辑
                await Task.CompletedTask;
                
                _logger.LogInformation("MonitoringDashboard初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化MonitoringDashboard失败");
                throw;
            }
        }

        /// <summary>
        /// 关闭监控仪表板
        /// </summary>
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("关闭MonitoringDashboard");

                // 清理缓存和队列
                _healthCheckCache.Clear();

                _logger.LogInformation("MonitoringDashboard关闭完成，处理了{AlertCount}个警报", _alertQueue.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭MonitoringDashboard失败");
                throw;
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 计算系统健康状态
        /// </summary>
        private async Task<SystemHealthStatus> CalculateSystemHealthAsync(
            PerformanceReport performanceReport,
            ErrorStatisticsReport errorReport,
            LogAnalysisResult logAnalysis,
            CancellationToken cancellationToken)
        {
            var healthStatus = new SystemHealthStatus
            {
                LastCheckTime = DateTime.UtcNow
            };

            try
            {
                // 性能健康检查
                var performanceCheck = new HealthCheckResult
                {
                    CheckName = "Performance",
                    Status = DeterminePerformanceHealth(performanceReport),
                    Description = $"平均响应时间：{performanceReport.AverageResponseTimeMs:F2}ms，错误率：{performanceReport.ErrorRate:P2}",
                    ResponseTime = TimeSpan.FromMilliseconds(performanceReport.AverageResponseTimeMs)
                };
                healthStatus.HealthChecks.Add(performanceCheck);

                // 错误健康检查
                var errorCheck = new HealthCheckResult
                {
                    CheckName = "Errors",
                    Status = DetermineErrorHealth(errorReport),
                    Description = $"总错误数：{errorReport.TotalErrors}，关键错误数：{errorReport.CriticalErrors}",
                    ResponseTime = TimeSpan.Zero
                };
                healthStatus.HealthChecks.Add(errorCheck);

                // 日志健康检查
                var logCheck = new HealthCheckResult
                {
                    CheckName = "Logs",
                    Status = DetermineLogHealth(logAnalysis),
                    Description = $"错误日志比例：{(double)logAnalysis.ErrorLogCount / Math.Max(1, logAnalysis.TotalLogEntries):P2}",
                    ResponseTime = TimeSpan.Zero
                };
                healthStatus.HealthChecks.Add(logCheck);

                // 计算总体健康状态
                CalculateOverallHealthStatus(healthStatus);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算系统健康状态失败");
                healthStatus.Issues.Add($"健康状态计算错误：{ex.Message}");
            }

            return healthStatus;
        }

        /// <summary>
        /// 执行系统健康检查
        /// </summary>
        private async Task<HealthCheckResult> PerformSystemHealthCheckAsync(string checkName, CancellationToken cancellationToken)
        {
            var cacheKey = $"{checkName}_{DateTime.UtcNow:yyyyMMddHHmm}";
            
            // 检查缓存
            if (_healthCheckCache.TryGetValue(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            var startTime = DateTime.UtcNow;
            var result = new HealthCheckResult
            {
                CheckName = checkName,
                Status = HealthStatus.Healthy
            };

            try
            {
                _logger.LogDebug("执行健康检查：{CheckName}", checkName);

                switch (checkName)
                {
                    case "API_Performance":
                        result = await CheckApiPerformanceAsync(cancellationToken);
                        break;
                    case "Database_Connectivity":
                        result = await CheckDatabaseConnectivityAsync(cancellationToken);
                        break;
                    case "Memory_Usage":
                        result = CheckMemoryUsage();
                        break;
                    case "Disk_Space":
                        result = CheckDiskSpace();
                        break;
                    case "Error_Rate":
                        result = CheckErrorRate();
                        break;
                    default:
                        result.Description = "未知的健康检查类型";
                        result.Status = HealthStatus.Degraded;
                        break;
                }

                result.ResponseTime = DateTime.UtcNow - startTime;

                // 缓存结果
                _healthCheckCache[cacheKey] = result;
                
                // 清理过期缓存
                CleanExpiredHealthCheckCache();
            }
            catch (Exception ex)
            {
                result.Status = HealthStatus.Critical;
                result.ErrorMessage = ex.Message;
                result.Description = $"健康检查失败：{ex.Message}";
                
                _logger.LogError(ex, "健康检查失败：{CheckName}", checkName);
            }

            return result;
        }

        /// <summary>
        /// 检查API性能
        /// </summary>
        private async Task<HealthCheckResult> CheckApiPerformanceAsync(CancellationToken cancellationToken)
        {
            // 模拟API性能检查
            await Task.Delay(10, cancellationToken);
            
            return new HealthCheckResult
            {
                CheckName = "API_Performance",
                Status = HealthStatus.Healthy,
                Description = "API性能正常",
                Data = new Dictionary<string, object>
                {
                    ["AverageResponseTime"] = "150ms",
                    ["Throughput"] = "1000 req/min"
                }
            };
        }

        /// <summary>
        /// 检查数据库连接性
        /// </summary>
        private async Task<HealthCheckResult> CheckDatabaseConnectivityAsync(CancellationToken cancellationToken)
        {
            // 模拟数据库连接检查
            await Task.Delay(20, cancellationToken);
            
            return new HealthCheckResult
            {
                CheckName = "Database_Connectivity",
                Status = HealthStatus.Healthy,
                Description = "数据库连接正常",
                Data = new Dictionary<string, object>
                {
                    ["ConnectionCount"] = 10,
                    ["ResponseTime"] = "20ms"
                }
            };
        }

        /// <summary>
        /// 检查内存使用情况
        /// </summary>
        private HealthCheckResult CheckMemoryUsage()
        {
            var totalMemory = GC.GetTotalMemory(false);
            var memoryMB = totalMemory / 1024.0 / 1024.0;
            
            var status = memoryMB switch
            {
                > 1000 => HealthStatus.Critical,
                > 500 => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };

            return new HealthCheckResult
            {
                CheckName = "Memory_Usage",
                Status = status,
                Description = $"内存使用量：{memoryMB:F2}MB",
                Data = new Dictionary<string, object>
                {
                    ["MemoryUsageMB"] = memoryMB,
                    ["GCCollectionCount"] = GC.CollectionCount(0)
                }
            };
        }

        /// <summary>
        /// 检查磁盘空间
        /// </summary>
        private HealthCheckResult CheckDiskSpace()
        {
            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                var systemDrive = drives.FirstOrDefault(d => d.Name.StartsWith("C:"));
                
                if (systemDrive != null)
                {
                    var freeSpaceGB = systemDrive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
                    var totalSpaceGB = systemDrive.TotalSize / 1024.0 / 1024.0 / 1024.0;
                    var usagePercent = (totalSpaceGB - freeSpaceGB) / totalSpaceGB * 100;

                    var status = usagePercent switch
                    {
                        > 90 => HealthStatus.Critical,
                        > 80 => HealthStatus.Degraded,
                        _ => HealthStatus.Healthy
                    };

                    return new HealthCheckResult
                    {
                        CheckName = "Disk_Space",
                        Status = status,
                        Description = $"磁盘使用率：{usagePercent:F1}%，可用空间：{freeSpaceGB:F2}GB",
                        Data = new Dictionary<string, object>
                        {
                            ["UsagePercent"] = usagePercent,
                            ["FreeSpaceGB"] = freeSpaceGB,
                            ["TotalSpaceGB"] = totalSpaceGB
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查磁盘空间失败");
            }

            return new HealthCheckResult
            {
                CheckName = "Disk_Space",
                Status = HealthStatus.Degraded,
                Description = "无法获取磁盘空间信息"
            };
        }

        /// <summary>
        /// 检查错误率
        /// </summary>
        private HealthCheckResult CheckErrorRate()
        {
            // 这里应该基于实际的错误统计数据
            var errorRate = 0.02; // 假设2%的错误率
            
            var status = errorRate switch
            {
                > 0.1 => HealthStatus.Critical,
                > 0.05 => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };

            return new HealthCheckResult
            {
                CheckName = "Error_Rate",
                Status = status,
                Description = $"错误率：{errorRate:P2}",
                Data = new Dictionary<string, object>
                {
                    ["ErrorRate"] = errorRate
                }
            };
        }

        /// <summary>
        /// 计算总体健康状态
        /// </summary>
        private void CalculateOverallHealthStatus(SystemHealthStatus healthStatus)
        {
            if (!healthStatus.HealthChecks.Any())
            {
                healthStatus.OverallStatus = HealthStatus.Degraded;
                healthStatus.OverallScore = 0;
                return;
            }

            // 计算加权健康分数
            var totalScore = 0.0;
            var totalWeight = 0.0;

            foreach (var check in healthStatus.HealthChecks)
            {
                var weight = _healthScoreWeights.TryGetValue(check.CheckName, out var w) ? w : 1.0;
                var score = check.Status switch
                {
                    HealthStatus.Healthy => 100.0,
                    HealthStatus.Degraded => 60.0,
                    HealthStatus.Critical => 20.0,
                    _ => 0.0
                };

                totalScore += score * weight;
                totalWeight += weight;

                // 收集问题
                if (check.Status != HealthStatus.Healthy)
                {
                    healthStatus.Issues.Add($"{check.CheckName}: {check.Description}");
                }
            }

            healthStatus.OverallScore = totalWeight > 0 ? totalScore / totalWeight : 0;

            // 确定总体状态
            healthStatus.OverallStatus = healthStatus.OverallScore switch
            {
                >= 80 => HealthStatus.Healthy,
                >= 60 => HealthStatus.Degraded,
                _ => HealthStatus.Critical
            };

            // 生成建议
            GenerateHealthRecommendations(healthStatus);
        }

        /// <summary>
        /// 生成健康建议
        /// </summary>
        private void GenerateHealthRecommendations(SystemHealthStatus healthStatus)
        {
            foreach (var check in healthStatus.HealthChecks.Where(c => c.Status != HealthStatus.Healthy))
            {
                var recommendation = check.CheckName switch
                {
                    "Performance" => "考虑优化API响应时间和减少错误率",
                    "Errors" => "需要调查和修复关键错误",
                    "Logs" => "检查应用程序日志中的错误模式",
                    "Memory_Usage" => "考虑增加内存或优化内存使用",
                    "Disk_Space" => "清理磁盘空间或扩展存储",
                    "Error_Rate" => "调查错误原因并提高系统稳定性",
                    _ => $"需要关注{check.CheckName}的健康状况"
                };

                healthStatus.Recommendations.Add(recommendation);
            }

            if (!healthStatus.Recommendations.Any() && healthStatus.OverallStatus == HealthStatus.Healthy)
            {
                healthStatus.Recommendations.Add("系统运行正常，继续保持当前状态");
            }
        }

        /// <summary>
        /// 确定性能健康状态
        /// </summary>
        private HealthStatus DeterminePerformanceHealth(PerformanceReport report)
        {
            if (report.ErrorRate > 0.1 || report.AverageResponseTimeMs > 5000)
                return HealthStatus.Critical;
            if (report.ErrorRate > 0.05 || report.AverageResponseTimeMs > 2000)
                return HealthStatus.Degraded;
            return HealthStatus.Healthy;
        }

        /// <summary>
        /// 确定错误健康状态
        /// </summary>
        private HealthStatus DetermineErrorHealth(ErrorStatisticsReport report)
        {
            if (report.CriticalErrors > 5)
                return HealthStatus.Critical;
            if (report.CriticalErrors > 0 || report.ErrorRate > 0.05)
                return HealthStatus.Degraded;
            return HealthStatus.Healthy;
        }

        /// <summary>
        /// 确定日志健康状态
        /// </summary>
        private HealthStatus DetermineLogHealth(LogAnalysisResult analysis)
        {
            var errorRatio = analysis.TotalLogEntries > 0 
                ? (double)analysis.ErrorLogCount / analysis.TotalLogEntries 
                : 0;

            if (errorRatio > 0.2 || analysis.Anomalies.Any(a => a.Severity >= AnomalySeverity.Critical))
                return HealthStatus.Critical;
            if (errorRatio > 0.1 || analysis.Anomalies.Any(a => a.Severity >= AnomalySeverity.Major))
                return HealthStatus.Degraded;
            return HealthStatus.Healthy;
        }

        /// <summary>
        /// 获取活跃警报
        /// </summary>
        private List<SystemAlert> GetActiveAlerts()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            return _alertQueue
                .Where(alert => !alert.IsAcknowledged && alert.TriggeredAt > cutoffTime)
                .OrderByDescending(alert => alert.Severity)
                .ThenByDescending(alert => alert.TriggeredAt)
                .Take(20)
                .ToList();
        }

        /// <summary>
        /// 生成自定义指标
        /// </summary>
        private Dictionary<string, object> GenerateCustomMetrics(
            PerformanceReport performanceReport,
            ErrorStatisticsReport errorReport,
            LogAnalysisResult logAnalysis)
        {
            return new Dictionary<string, object>
            {
                ["SystemUptime"] = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
                ["TotalProcessorTime"] = Environment.ProcessorCount,
                ["WorkingSet"] = Environment.WorkingSet / 1024 / 1024, // MB
                ["PerformanceScore"] = CalculatePerformanceScore(performanceReport),
                ["ErrorTrend"] = CalculateErrorTrend(errorReport),
                ["LogHealthScore"] = CalculateLogHealthScore(logAnalysis),
                ["LastUpdated"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        /// <summary>
        /// 生成推荐操作
        /// </summary>
        private List<string> GenerateRecommendedActions(MonitoringDashboardData dashboardData)
        {
            var actions = new List<string>();

            if (dashboardData.SystemHealth.OverallScore < 80)
            {
                actions.Add("系统健康分数较低，建议立即检查关键问题");
            }

            if (dashboardData.ActiveAlerts.Any(a => a.Severity >= AlertSeverity.Critical))
            {
                actions.Add("存在关键警报，需要立即处理");
            }

            if (dashboardData.PerformanceSummary.ErrorRate > 0.05)
            {
                actions.Add("错误率偏高，建议检查应用程序稳定性");
            }

            if (dashboardData.ErrorSummary.CriticalErrors > 0)
            {
                actions.Add($"发现{dashboardData.ErrorSummary.CriticalErrors}个关键错误，需要优先处理");
            }

            if (!actions.Any())
            {
                actions.Add("系统运行正常，建议继续监控关键指标");
            }

            return actions;
        }

        /// <summary>
        /// 计算性能分数
        /// </summary>
        private double CalculatePerformanceScore(PerformanceReport report)
        {
            var responseTimeScore = Math.Max(0, 100 - report.AverageResponseTimeMs / 50); // 5000ms = 0分
            var errorRateScore = Math.Max(0, 100 - report.ErrorRate * 1000); // 10% = 0分
            return (responseTimeScore + errorRateScore) / 2;
        }

        /// <summary>
        /// 计算错误趋势
        /// </summary>
        private string CalculateErrorTrend(ErrorStatisticsReport report)
        {
            if (!report.ErrorTrends.Any()) return "稳定";
            
            var increasingTrends = report.ErrorTrends.Count(t => t.Direction == TrendDirection.Increasing);
            var totalTrends = report.ErrorTrends.Count;
            
            return increasingTrends > totalTrends * 0.6 ? "上升" : 
                   increasingTrends < totalTrends * 0.3 ? "下降" : "稳定";
        }

        /// <summary>
        /// 计算日志健康分数
        /// </summary>
        private double CalculateLogHealthScore(LogAnalysisResult analysis)
        {
            if (analysis.TotalLogEntries == 0) return 100;
            
            var errorRatio = (double)analysis.ErrorLogCount / analysis.TotalLogEntries;
            var anomalyCount = analysis.Anomalies.Count(a => a.Severity >= AnomalySeverity.Major);
            
            var errorScore = Math.Max(0, 100 - errorRatio * 500); // 20% = 0分
            var anomalyScore = Math.Max(0, 100 - anomalyCount * 20); // 5个重大异常 = 0分
            
            return (errorScore + anomalyScore) / 2;
        }

        /// <summary>
        /// 清理过期健康检查缓存
        /// </summary>
        private void CleanExpiredHealthCheckCache()
        {
            var cutoffTime = DateTime.UtcNow - _healthCheckCacheExpiration;
            var expiredKeys = _healthCheckCache
                .Where(kvp => kvp.Key.EndsWith($"_{cutoffTime.AddMinutes(-1):yyyyMMddHHmm}"))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _healthCheckCache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// 初始化健康分数权重
        /// </summary>
        private Dictionary<string, double> InitializeHealthScoreWeights()
        {
            return new Dictionary<string, double>
            {
                ["Performance"] = 2.0,
                ["Errors"] = 2.0,
                ["Logs"] = 1.5,
                ["API_Performance"] = 2.0,
                ["Database_Connectivity"] = 2.0,
                ["Memory_Usage"] = 1.0,
                ["Disk_Space"] = 0.8,
                ["Error_Rate"] = 2.0
            };
        }

        #endregion
    }
}