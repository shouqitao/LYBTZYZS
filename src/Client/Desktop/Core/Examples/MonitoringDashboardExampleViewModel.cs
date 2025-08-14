using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Core.Services.Monitoring;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Serilog.Events;

namespace LYBT.Desktop.Core.Examples
{
    /// <summary>
    /// 监控仪表板示例ViewModel - UltraThink Stage 5.3.1 完整演示
    /// 
    /// 展示功能：
    /// 1. 结构化日志记录
    /// 2. 性能监控指标
    /// 3. 业务指标追踪
    /// 4. 实时监控展示
    /// </summary>
    public class MonitoringDashboardExampleViewModel : BindableBase, IDisposable
    {
        #region 私有字段

        private readonly IStructuredLoggingService _loggingService;
        private readonly IPerformanceMonitoringService _performanceService;
        private readonly IBusinessMetricsService _businessService;
        private readonly ILogger<MonitoringDashboardExampleViewModel> _logger;
        
        private string _currentLogLevel = "Information";
        private string _performanceReport = string.Empty;
        private string _businessReport = string.Empty;
        private bool _isMonitoring = false;
        
        private IDisposable? _alertSubscription;
        private Random _random = new Random();

        #endregion

        #region 构造函数

        public MonitoringDashboardExampleViewModel(
            IStructuredLoggingService loggingService,
            IPerformanceMonitoringService performanceService,
            IBusinessMetricsService businessService,
            ILogger<MonitoringDashboardExampleViewModel> logger)
        {
            _loggingService = loggingService;
            _performanceService = performanceService;
            _businessService = businessService;
            _logger = logger;
            
            InitializeCommands();
            InitializeDemoData();
            
            // 订阅性能告警
            _alertSubscription = _performanceService.SubscribeToAlerts(OnPerformanceAlert);
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前日志级别
        /// </summary>
        public string CurrentLogLevel
        {
            get => _currentLogLevel;
            set => SetProperty(ref _currentLogLevel, value);
        }

        /// <summary>
        /// 性能报告
        /// </summary>
        public string PerformanceReport
        {
            get => _performanceReport;
            set => SetProperty(ref _performanceReport, value);
        }

        /// <summary>
        /// 业务报告
        /// </summary>
        public string BusinessReport
        {
            get => _businessReport;
            set => SetProperty(ref _businessReport, value);
        }

        /// <summary>
        /// 是否正在监控
        /// </summary>
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set => SetProperty(ref _isMonitoring, value);
        }

        /// <summary>
        /// 日志条目
        /// </summary>
        public ObservableCollection<LogEntry> LogEntries { get; } = new();

        /// <summary>
        /// 性能指标
        /// </summary>
        public ObservableCollection<MetricDisplay> PerformanceMetrics { get; } = new();

        /// <summary>
        /// 业务洞察
        /// </summary>
        public ObservableCollection<InsightDisplay> BusinessInsights { get; } = new();

        /// <summary>
        /// 性能告警
        /// </summary>
        public ObservableCollection<AlertDisplay> PerformanceAlerts { get; } = new();

        #endregion

        #region 命令

        public ICommand SimulateBusinessOperationCommand { get; private set; } = null!;
        public ICommand SimulateSlowOperationCommand { get; private set; } = null!;
        public ICommand SimulateErrorCommand { get; private set; } = null!;
        public ICommand GeneratePerformanceReportCommand { get; private set; } = null!;
        public ICommand GenerateBusinessReportCommand { get; private set; } = null!;
        public ICommand StartMonitoringCommand { get; private set; } = null!;
        public ICommand StopMonitoringCommand { get; private set; } = null!;
        public ICommand ChangeLogLevelCommand { get; private set; } = null!;
        public ICommand IdentifyBottlenecksCommand { get; private set; } = null!;
        public ICommand ClearLogsCommand { get; private set; } = null!;

        #endregion

        #region 初始化

        private void InitializeCommands()
        {
            SimulateBusinessOperationCommand = new DelegateCommand(async () => await SimulateBusinessOperationAsync());
            SimulateSlowOperationCommand = new DelegateCommand(async () => await SimulateSlowOperationAsync());
            SimulateErrorCommand = new DelegateCommand(SimulateError);
            GeneratePerformanceReportCommand = new DelegateCommand(GeneratePerformanceReport);
            GenerateBusinessReportCommand = new DelegateCommand(GenerateBusinessReport);
            StartMonitoringCommand = new DelegateCommand(async () => await StartMonitoringAsync());
            StopMonitoringCommand = new DelegateCommand(StopMonitoring);
            ChangeLogLevelCommand = new DelegateCommand<string>(ChangeLogLevel);
            IdentifyBottlenecksCommand = new DelegateCommand(IdentifyBottlenecks);
            ClearLogsCommand = new DelegateCommand(ClearLogs);
        }

        private void InitializeDemoData()
        {
            // 初始化一些演示数据
            SimulateHistoricalData();
            UpdateMetrics();
            AddLog("监控仪表板已初始化", LogLevel.Information);
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 模拟业务操作
        /// </summary>
        private async Task SimulateBusinessOperationAsync()
        {
            AddLog("开始模拟业务操作流程", LogLevel.Information);
            
            using (var flow = _businessService.StartBusinessFlow("患者诊疗流程"))
            {
                // 步骤1：患者接待
                var patientId = Guid.NewGuid();
                _businessService.RecordPatientOperation(
                    PatientOperationType.Register,
                    patientId,
                    new Dictionary<string, object> { { "PatientName", "张三" }, { "Age", 35 } });
                
                flow.RecordStep("患者注册", true);
                AddLog($"✓ 新患者注册: {patientId}", LogLevel.Information);
                await Task.Delay(500);
                
                // 步骤2：创建诊疗
                var consultationId = Guid.NewGuid();
                _businessService.RecordConsultationOperation(
                    ConsultationOperationType.Create,
                    consultationId);
                
                flow.RecordStep("创建诊疗", true);
                AddLog($"✓ 创建诊疗: {consultationId}", LogLevel.Information);
                await Task.Delay(800);
                
                // 步骤3：诊断
                _businessService.RecordConsultationOperation(
                    ConsultationOperationType.Diagnose,
                    consultationId,
                    TimeSpan.FromMinutes(_random.Next(10, 30)));
                
                flow.RecordStep("完成诊断", true);
                AddLog("✓ 完成四诊诊断", LogLevel.Information);
                await Task.Delay(500);
                
                // 步骤4：开具处方
                var prescriptionId = Guid.NewGuid();
                var amount = 150 + _random.Next(50, 200);
                _businessService.RecordPrescriptionOperation(
                    PrescriptionOperationType.Create,
                    prescriptionId,
                    amount);
                
                // 记录药材使用
                _businessService.RecordHerbUsage("黄芪", 30, "g", 2.5m);
                _businessService.RecordHerbUsage("当归", 15, "g", 4.0m);
                _businessService.RecordHerbUsage("白术", 20, "g", 3.0m);
                
                flow.RecordStep("开具处方", true);
                AddLog($"✓ 开具处方，金额: ¥{amount}", LogLevel.Information);
                
                // 步骤5：完成
                _businessService.RecordConsultationOperation(
                    ConsultationOperationType.Complete,
                    consultationId);
                
                flow.Complete(true);
            }
            
            AddLog("业务流程完成", LogLevel.Information);
            UpdateBusinessInsights();
        }

        /// <summary>
        /// 模拟慢操作
        /// </summary>
        private async Task SimulateSlowOperationAsync()
        {
            var operations = new[] { "数据库查询", "API调用", "文件处理", "报表生成" };
            var operation = operations[_random.Next(operations.Length)];
            
            AddLog($"开始执行: {operation}", LogLevel.Debug);
            
            using (var timer = _performanceService.StartTimer(operation, "Simulation"))
            {
                var duration = 1000 + _random.Next(2000); // 1-3秒
                await Task.Delay(duration);
                
                // 模拟数据库操作
                if (operation == "数据库查询")
                {
                    _performanceService.RecordDatabaseOperation(
                        "SELECT * FROM Patients WHERE ...",
                        TimeSpan.FromMilliseconds(duration),
                        _random.Next(10, 100));
                }
                // 模拟API调用
                else if (operation == "API调用")
                {
                    _performanceService.RecordApiCall(
                        "/api/patients",
                        "GET",
                        200,
                        TimeSpan.FromMilliseconds(duration));
                }
            }
            
            AddLog($"完成操作: {operation}", LogLevel.Debug);
            
            // 记录缓存操作
            var cacheKey = $"cache:{operation}:{Guid.NewGuid()}";
            _performanceService.RecordCacheOperation(cacheKey, _random.Next(100) > 30, _random.Next(100, 10000));
            
            UpdateMetrics();
        }

        /// <summary>
        /// 模拟错误
        /// </summary>
        private void SimulateError()
        {
            var errors = new Exception[]
            {
                new InvalidOperationException("无效的操作状态"),
                new TimeoutException("操作超时"),
                new UnauthorizedAccessException("访问被拒绝"),
                new ArgumentException("参数错误")
            };
            
            var error = errors[_random.Next(errors.Length)];
            var context = $"模拟错误场景 #{_random.Next(1000)}";
            
            _loggingService.LogError(error, context, new Dictionary<string, object>
            {
                { "Module", "Simulation" },
                { "Severity", "Medium" }
            });
            
            AddLog($"✗ 错误: {error.Message}", LogLevel.Error);
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        private void GeneratePerformanceReport()
        {
            var metrics = _performanceService.GetMetrics(TimeSpan.FromHours(1));
            var report = _performanceService.GenerateReport(
                DateTime.Now.AddHours(-1),
                DateTime.Now);
            
            PerformanceReport = $@"
══════════════════════════════════════════
            性能监控报告
    {DateTime.Now:yyyy-MM-dd HH:mm:ss}
══════════════════════════════════════════

【概览】
• 总操作数: {metrics.TotalOperations:N0}
• API调用: {metrics.TotalApiCalls:N0}
• 数据库操作: {metrics.TotalDatabaseOperations:N0}
• 缓存操作: {metrics.TotalCacheOperations:N0}

【性能指标】
• 平均响应时间: {metrics.AverageResponseTimeMs:F1}ms
• 缓存命中率: {metrics.CacheHitRate:F1}%
• API成功率: {metrics.ApiSuccessRate:F1}%
• CPU使用率: {metrics.AverageCpuUsage:F1}%
• 内存使用: {metrics.AverageMemoryUsageMB:F0}MB

【最慢操作 Top 5】
{string.Join("\n", metrics.TopSlowOperations.Select(op => 
    $"  • {op.Name}: 平均{op.AverageDurationMs:F0}ms, 最大{op.MaxDurationMs:F0}ms"))}

【最频繁操作 Top 5】
{string.Join("\n", metrics.MostFrequentOperations.Select(op => 
    $"  • {op.Name}: {op.Count}次, 平均{op.AverageDurationMs:F0}ms"))}

【分类统计】
{string.Join("\n", report.Categories.Select(cat => 
    $"  • {cat.Category}: {cat.Count}次, 平均{cat.AverageDurationMs:F0}ms"))}

══════════════════════════════════════════
";
            
            AddLog("性能报告已生成", LogLevel.Information);
        }

        /// <summary>
        /// 生成业务报告
        /// </summary>
        private void GenerateBusinessReport()
        {
            var summary = _businessService.GetMetricsSummary(
                DateTime.Today.AddDays(-7),
                DateTime.Today);
            
            var revenue = _businessService.GetRevenueStatistics(
                DateTime.Today.AddDays(-7),
                DateTime.Today);
            
            BusinessReport = $@"
══════════════════════════════════════════
           业务指标报告
    {DateTime.Now:yyyy-MM-dd HH:mm:ss}
══════════════════════════════════════════

【时间范围】
{summary.StartDate:yyyy-MM-dd} 至 {summary.EndDate:yyyy-MM-dd}

【患者统计】
• 新增患者: {summary.TotalPatients}人
• 总接诊量: {summary.TotalVisits}人次
• 日均接诊: {(summary.TotalVisits / 7.0):F1}人次

【诊疗统计】
• 总诊疗数: {summary.TotalConsultations}次
• 完成诊疗: {summary.CompletedConsultations}次
• 平均诊疗时长: {summary.AverageConsultationDuration.TotalMinutes:F0}分钟
• 完成率: {(summary.CompletedConsultations * 100.0 / Math.Max(1, summary.TotalConsultations)):F1}%

【处方统计】
• 总处方数: {summary.TotalPrescriptions}张
• 处方总额: ¥{summary.TotalRevenue:F2}
• 平均处方价值: ¥{summary.AveragePrescriptionValue:F2}

【营收分析】
• 总营收: ¥{revenue.TotalRevenue:F2}
• 日均营收: ¥{revenue.AverageDailyRevenue:F2}
• 峰值营收: ¥{revenue.PeakRevenue:F2}

【热门药材 Top 5】
{string.Join("\n", summary.TopHerbs.Select(herb => 
    $"  {herb.Rank}. {herb.HerbName}: {herb.UsageCount}次, {herb.TotalQuantity:F0}{herb.Unit}, ¥{herb.TotalValue:F2}"))}

【趋势分析】
{string.Join("\n", summary.DailyTrends.TakeLast(3).Select(trend => 
    $"  • {trend.Date:MM-dd}: 患者{trend.Patients} 诊疗{trend.Consultations} 处方{trend.Prescriptions} 营收¥{trend.Revenue:F2}"))}

══════════════════════════════════════════
";
            
            AddLog("业务报告已生成", LogLevel.Information);
        }

        /// <summary>
        /// 开始监控
        /// </summary>
        private async Task StartMonitoringAsync()
        {
            IsMonitoring = true;
            AddLog("实时监控已启动", LogLevel.Information);
            
            // 记录资源使用
            _performanceService.RecordResourceUsage();
            
            // 模拟持续监控
            _ = Task.Run(async () =>
            {
                while (IsMonitoring)
                {
                    await Task.Delay(5000);
                    UpdateMetrics();
                    
                    // 随机生成一些活动
                    if (_random.Next(100) > 70)
                    {
                        await SimulateBusinessOperationAsync();
                    }
                    if (_random.Next(100) > 80)
                    {
                        await SimulateSlowOperationAsync();
                    }
                }
            });
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        private void StopMonitoring()
        {
            IsMonitoring = false;
            AddLog("实时监控已停止", LogLevel.Information);
        }

        /// <summary>
        /// 更改日志级别
        /// </summary>
        private void ChangeLogLevel(string? level)
        {
            if (Enum.TryParse<LogEventLevel>(level, out var logLevel))
            {
                _loggingService.AdjustLogLevel(logLevel);
                CurrentLogLevel = level!;
                AddLog($"日志级别已调整为: {level}", LogLevel.Information);
            }
        }

        /// <summary>
        /// 识别瓶颈
        /// </summary>
        private void IdentifyBottlenecks()
        {
            var bottlenecks = _performanceService.IdentifyBottlenecks();
            
            AddLog($"发现 {bottlenecks.Count} 个性能瓶颈:", LogLevel.Warning);
            
            foreach (var bottleneck in bottlenecks.Take(5))
            {
                AddLog($"  • [{bottleneck.Type}] {bottleneck.Component}: {bottleneck.Description}", LogLevel.Warning);
                if (!string.IsNullOrEmpty(bottleneck.Recommendation))
                {
                    AddLog($"    建议: {bottleneck.Recommendation}", LogLevel.Information);
                }
            }
        }

        /// <summary>
        /// 清理日志
        /// </summary>
        private void ClearLogs()
        {
            LogEntries.Clear();
            PerformanceAlerts.Clear();
            AddLog("日志已清理", LogLevel.Information);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 添加日志
        /// </summary>
        private void AddLog(string message, LogLevel level)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level.ToString(),
                Message = message
            };
            
            LogEntries.Insert(0, entry);
            
            // 限制日志数量
            while (LogEntries.Count > 100)
            {
                LogEntries.RemoveAt(LogEntries.Count - 1);
            }
            
            // 同时记录到结构化日志
            _loggingService.LogBusinessOperation($"Dashboard.{level}", new { Message = message });
        }

        /// <summary>
        /// 更新指标
        /// </summary>
        private void UpdateMetrics()
        {
            var metrics = _performanceService.GetMetrics(TimeSpan.FromMinutes(5));
            var logStats = _loggingService.GetStatistics();
            
            PerformanceMetrics.Clear();
            PerformanceMetrics.Add(new MetricDisplay { Name = "总操作数", Value = metrics.TotalOperations.ToString("N0"), Category = "操作" });
            PerformanceMetrics.Add(new MetricDisplay { Name = "API调用", Value = metrics.TotalApiCalls.ToString("N0"), Category = "API" });
            PerformanceMetrics.Add(new MetricDisplay { Name = "缓存命中率", Value = $"{metrics.CacheHitRate:F1}%", Category = "缓存" });
            PerformanceMetrics.Add(new MetricDisplay { Name = "平均响应", Value = $"{metrics.AverageResponseTimeMs:F0}ms", Category = "性能" });
            PerformanceMetrics.Add(new MetricDisplay { Name = "CPU使用", Value = $"{metrics.AverageCpuUsage:F1}%", Category = "资源" });
            PerformanceMetrics.Add(new MetricDisplay { Name = "内存使用", Value = $"{metrics.AverageMemoryUsageMB:F0}MB", Category = "资源" });
            PerformanceMetrics.Add(new MetricDisplay { Name = "日志总数", Value = logStats.TotalLogCount.ToString("N0"), Category = "日志" });
            PerformanceMetrics.Add(new MetricDisplay { Name = "错误数", Value = logStats.ErrorCount.ToString("N0"), Category = "日志" });
        }

        /// <summary>
        /// 更新业务洞察
        /// </summary>
        private void UpdateBusinessInsights()
        {
            var insights = _businessService.GetBusinessInsights();
            
            BusinessInsights.Clear();
            foreach (var insight in insights.Take(5))
            {
                BusinessInsights.Add(new InsightDisplay
                {
                    Type = insight.Type.ToString(),
                    Category = insight.Category,
                    Title = insight.Title,
                    Description = insight.Description,
                    Impact = insight.Impact.ToString(),
                    Recommendation = insight.Recommendation
                });
            }
        }

        /// <summary>
        /// 处理性能告警
        /// </summary>
        private void OnPerformanceAlert(PerformanceAlert alert)
        {
            PerformanceAlerts.Insert(0, new AlertDisplay
            {
                Type = alert.Type.ToString(),
                Severity = alert.Severity.ToString(),
                Message = alert.Message,
                Details = alert.Details,
                Timestamp = alert.Timestamp
            });
            
            // 限制告警数量
            while (PerformanceAlerts.Count > 20)
            {
                PerformanceAlerts.RemoveAt(PerformanceAlerts.Count - 1);
            }
            
            AddLog($"⚠ 性能告警: {alert.Message}", LogLevel.Warning);
        }

        /// <summary>
        /// 模拟历史数据
        /// </summary>
        private void SimulateHistoricalData()
        {
            // 生成过去7天的模拟数据
            for (int day = 6; day >= 0; day--)
            {
                var date = DateTime.Today.AddDays(-day);
                
                // 模拟患者数据
                for (int i = 0; i < _random.Next(5, 15); i++)
                {
                    _businessService.RecordPatientOperation(
                        i % 3 == 0 ? PatientOperationType.Register : PatientOperationType.Return,
                        Guid.NewGuid(),
                        new Dictionary<string, object> { { "Date", date } });
                }
                
                // 模拟诊疗数据
                for (int i = 0; i < _random.Next(8, 20); i++)
                {
                    var consultationId = Guid.NewGuid();
                    _businessService.RecordConsultationOperation(
                        ConsultationOperationType.Complete,
                        consultationId,
                        TimeSpan.FromMinutes(_random.Next(15, 45)));
                }
                
                // 模拟处方数据
                for (int i = 0; i < _random.Next(5, 15); i++)
                {
                    _businessService.RecordPrescriptionOperation(
                        PrescriptionOperationType.Paid,
                        Guid.NewGuid(),
                        100 + _random.Next(50, 300));
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            IsMonitoring = false;
            _alertSubscription?.Dispose();
            
            var logStats = _loggingService.GetStatistics();
            _logger.LogInformation(
                "监控仪表板已关闭 - 日志: {Logs}, 错误: {Errors}, 业务操作: {Business}",
                logStats.TotalLogCount, logStats.ErrorCount, logStats.BusinessLogCount);
        }

        #endregion
    }

    #region 显示模型

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 指标显示
    /// </summary>
    public class MetricDisplay
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// 洞察显示
    /// </summary>
    public class InsightDisplay
    {
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string? Recommendation { get; set; }
    }

    /// <summary>
    /// 告警显示
    /// </summary>
    public class AlertDisplay
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error
    }

    #endregion
}