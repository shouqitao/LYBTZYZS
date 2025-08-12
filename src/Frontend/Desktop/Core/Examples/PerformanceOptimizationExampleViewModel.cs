using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Services.Performance;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Prism.Mvvm;
using Prism.Commands;

namespace LYBT.Desktop.Core.Examples
{
    /// <summary>
    /// 性能优化示例ViewModel - UltraThink Stage 5.2.3 完整演示
    /// 
    /// 展示核心功能：
    /// 1. 用户行为分析和预测
    /// 2. 智能预测性预加载
    /// 3. 并发任务管理
    /// 4. 性能监控和优化
    /// </summary>
    public class PerformanceOptimizationExampleViewModel : BindableBase, IDisposable
    {
        #region 私有字段

        private readonly IUserBehaviorAnalyzer _behaviorAnalyzer;
        private readonly IPredictivePreloadService _preloadService;
        private readonly ISmartConcurrencyManager _concurrencyManager;
        private readonly ISmartLoadingManager _loadingManager;
        private readonly ILogger<PerformanceOptimizationExampleViewModel> _logger;

        private string _currentModule = "Patients";
        private string _currentAction = "View";
        private string _performanceReport = string.Empty;
        private bool _isPreloadingEnabled = true;
        private int _concurrencyLevel = 4;

        #endregion

        #region 构造函数

        public PerformanceOptimizationExampleViewModel(
            IUserBehaviorAnalyzer behaviorAnalyzer,
            IPredictivePreloadService preloadService,
            ISmartConcurrencyManager concurrencyManager,
            ISmartLoadingManager loadingManager,
            ILogger<PerformanceOptimizationExampleViewModel> logger)
        {
            _behaviorAnalyzer = behaviorAnalyzer ?? throw new ArgumentNullException(nameof(behaviorAnalyzer));
            _preloadService = preloadService ?? throw new ArgumentNullException(nameof(preloadService));
            _concurrencyManager = concurrencyManager ?? throw new ArgumentNullException(nameof(concurrencyManager));
            _loadingManager = loadingManager ?? throw new ArgumentNullException(nameof(loadingManager));
            _logger = logger;

            InitializeCommands();
            InitializeDemoData();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前模块
        /// </summary>
        public string CurrentModule
        {
            get => _currentModule;
            set => SetProperty(ref _currentModule, value);
        }

        /// <summary>
        /// 当前操作
        /// </summary>
        public string CurrentAction
        {
            get => _currentAction;
            set => SetProperty(ref _currentAction, value);
        }

        /// <summary>
        /// 性能报告
        /// </summary>
        public string PerformanceReport
        {
            get => _performanceReport;
            private set => SetProperty(ref _performanceReport, value);
        }

        /// <summary>
        /// 是否启用预加载
        /// </summary>
        public bool IsPreloadingEnabled
        {
            get => _isPreloadingEnabled;
            set
            {
                if (SetProperty(ref _isPreloadingEnabled, value))
                {
                    if (value)
                        _preloadService.ResumePreloading();
                    else
                        _preloadService.PausePreloading();
                }
            }
        }

        /// <summary>
        /// 并发度
        /// </summary>
        public int ConcurrencyLevel
        {
            get => _concurrencyLevel;
            set
            {
                if (SetProperty(ref _concurrencyLevel, value))
                {
                    _concurrencyManager.AdjustConcurrencyLevel(value);
                }
            }
        }

        /// <summary>
        /// 预测结果
        /// </summary>
        public ObservableCollection<PredictionDisplayItem> Predictions { get; } = new();

        /// <summary>
        /// 性能指标
        /// </summary>
        public ObservableCollection<MetricDisplayItem> Metrics { get; } = new();

        /// <summary>
        /// 任务执行日志
        /// </summary>
        public ObservableCollection<string> ExecutionLog { get; } = new();

        #endregion

        #region 命令

        public ICommand SimulateUserActionCommand { get; private set; } = null!;
        public ICommand PredictNextActionCommand { get; private set; } = null!;
        public ICommand ExecuteBatchTasksCommand { get; private set; } = null!;
        public ICommand ExecuteTaskGroupCommand { get; private set; } = null!;
        public ICommand ShowBehaviorHeatmapCommand { get; private set; } = null!;
        public ICommand OptimizePerformanceCommand { get; private set; } = null!;
        public ICommand GenerateReportCommand { get; private set; } = null!;
        public ICommand ClearLogsCommand { get; private set; } = null!;

        #endregion

        #region 初始化

        private void InitializeCommands()
        {
            SimulateUserActionCommand = new DelegateCommand(async () => await SimulateUserActionAsync());
            PredictNextActionCommand = new DelegateCommand(async () => await PredictNextActionAsync());
            ExecuteBatchTasksCommand = new DelegateCommand(async () => await ExecuteBatchTasksAsync());
            ExecuteTaskGroupCommand = new DelegateCommand(async () => await ExecuteTaskGroupAsync());
            ShowBehaviorHeatmapCommand = new DelegateCommand(ShowBehaviorHeatmap);
            OptimizePerformanceCommand = new DelegateCommand(async () => await OptimizePerformanceAsync());
            GenerateReportCommand = new DelegateCommand(GeneratePerformanceReport);
            ClearLogsCommand = new DelegateCommand(() => ExecutionLog.Clear());
        }

        private void InitializeDemoData()
        {
            // 初始化一些示例用户行为数据
            var demoActions = new[]
            {
                new UserAction { ModuleName = "Patients", ActionName = "View", Timestamp = DateTime.Now.AddMinutes(-30) },
                new UserAction { ModuleName = "Patients", ActionName = "Search", Timestamp = DateTime.Now.AddMinutes(-28) },
                new UserAction { ModuleName = "Consultation", ActionName = "Create", Timestamp = DateTime.Now.AddMinutes(-25) },
                new UserAction { ModuleName = "Consultation", ActionName = "Diagnose", Timestamp = DateTime.Now.AddMinutes(-20) },
                new UserAction { ModuleName = "Prescriptions", ActionName = "Create", Timestamp = DateTime.Now.AddMinutes(-15) },
                new UserAction { ModuleName = "Prescriptions", ActionName = "Print", Timestamp = DateTime.Now.AddMinutes(-10) },
                new UserAction { ModuleName = "Patients", ActionName = "View", Timestamp = DateTime.Now.AddMinutes(-5) }
            };

            foreach (var action in demoActions)
            {
                _behaviorAnalyzer.RecordAction(action);
            }

            AddLog("演示数据已初始化 - 记录了7个历史操作");
        }

        #endregion

        #region 演示方法

        /// <summary>
        /// 模拟用户操作
        /// </summary>
        private async Task SimulateUserActionAsync()
        {
            using var operation = _loadingManager.StartLoading("simulate_action", "模拟用户操作...", layer: 1);

            try
            {
                // 记录用户操作
                var action = new UserAction
                {
                    ModuleName = CurrentModule,
                    ActionName = CurrentAction,
                    Timestamp = DateTime.Now,
                    Duration = TimeSpan.FromSeconds(new Random().Next(1, 10))
                };

                _behaviorAnalyzer.RecordAction(action);
                AddLog($"✓ 记录操作: {action.ModuleName}.{action.ActionName}");

                // 触发预测性预加载
                if (IsPreloadingEnabled)
                {
                    await _preloadService.StartPredictivePreloadingAsync(CurrentModule, CurrentAction);
                    AddLog($"⚡ 启动预测性预加载");
                }

                // 更新性能指标
                UpdateMetrics();

                await Task.Delay(500); // 模拟操作执行
            }
            catch (Exception ex)
            {
                AddLog($"✗ 模拟操作失败: {ex.Message}");
                _logger.LogError(ex, "模拟用户操作失败");
            }
            finally
            {
                operation?.Complete();
            }
        }

        /// <summary>
        /// 预测下一步操作
        /// </summary>
        private async Task PredictNextActionAsync()
        {
            using var operation = _loadingManager.StartLoading("predict", "分析用户行为模式...", layer: 1);

            try
            {
                var result = await _behaviorAnalyzer.PredictNextActionAsync(CurrentModule, CurrentAction);
                
                Predictions.Clear();
                foreach (var prediction in result.PredictedActions.Take(5))
                {
                    Predictions.Add(new PredictionDisplayItem
                    {
                        Module = prediction.ModuleName,
                        Action = prediction.ActionName,
                        Probability = $"{prediction.Probability * 100:F1}%",
                        DataToPreload = string.Join(", ", prediction.DataToPreload),
                        EstimatedTime = $"{prediction.EstimatedTimeToAction.TotalSeconds:F0}秒"
                    });
                }

                AddLog($"📊 预测完成 - 置信度: {result.Confidence * 100:F1}%");
                AddLog($"   最可能: {result.PredictedActions.FirstOrDefault()?.ModuleName}.{result.PredictedActions.FirstOrDefault()?.ActionName}");
            }
            catch (Exception ex)
            {
                AddLog($"✗ 预测失败: {ex.Message}");
                _logger.LogError(ex, "预测下一步操作失败");
            }
            finally
            {
                operation?.Complete();
            }
        }

        /// <summary>
        /// 执行批量任务
        /// </summary>
        private async Task ExecuteBatchTasksAsync()
        {
            using var operation = _loadingManager.StartLoading("batch_tasks", "执行批量任务...", layer: 1, supportProgress: true);

            try
            {
                AddLog($"🚀 开始批量执行 10 个任务");

                // 创建批量任务
                var tasks = Enumerable.Range(1, 10).Select(i => new Func<CancellationToken, Task<string>>(
                    async ct =>
                    {
                        await Task.Delay(Random.Shared.Next(100, 1000), ct);
                        return $"任务{i}完成";
                    }
                )).ToList();

                // 使用智能并发管理器执行
                var results = await _concurrencyManager.ExecuteBatchAsync(
                    tasks,
                    new BatchConcurrencyOptions
                    {
                        MaxParallelism = ConcurrencyLevel,
                        Priority = TaskPriority.Normal,
                        FailFast = false
                    });

                AddLog($"✓ 批量执行完成 - 成功: {results.Count()}/10");
                
                // 更新进度
                operation?.UpdateProgress(100, "批量任务完成");
            }
            catch (Exception ex)
            {
                AddLog($"✗ 批量执行失败: {ex.Message}");
                _logger.LogError(ex, "批量任务执行失败");
            }
            finally
            {
                operation?.Complete();
            }
        }

        /// <summary>
        /// 执行任务组（依赖任务）
        /// </summary>
        private async Task ExecuteTaskGroupAsync()
        {
            using var operation = _loadingManager.StartLoading("task_group", "执行任务组...", layer: 1);

            try
            {
                AddLog($"🔗 开始执行依赖任务组");

                // 创建任务组 - 模拟诊疗流程
                var taskGroup = new TaskGroup
                {
                    Id = "diagnosis_workflow",
                    FailFast = false,
                    Tasks = new List<GroupTask>
                    {
                        new GroupTask
                        {
                            Id = "load_patient",
                            Name = "加载患者信息",
                            Dependencies = new List<string>(),
                            Execute = async (inputs, ct) =>
                            {
                                await Task.Delay(500, ct);
                                AddLog("  ➤ 患者信息加载完成");
                                return new { PatientId = Guid.NewGuid(), Name = "张三" };
                            }
                        },
                        new GroupTask
                        {
                            Id = "load_history",
                            Name = "加载病历",
                            Dependencies = new List<string> { "load_patient" },
                            Execute = async (inputs, ct) =>
                            {
                                await Task.Delay(800, ct);
                                AddLog("  ➤ 病历加载完成");
                                return new { CaseCount = 5 };
                            }
                        },
                        new GroupTask
                        {
                            Id = "analyze_symptoms",
                            Name = "分析症状",
                            Dependencies = new List<string> { "load_history" },
                            Execute = async (inputs, ct) =>
                            {
                                await Task.Delay(1000, ct);
                                AddLog("  ➤ 症状分析完成");
                                return new { Diagnosis = "风寒感冒" };
                            }
                        },
                        new GroupTask
                        {
                            Id = "generate_prescription",
                            Name = "生成处方",
                            Dependencies = new List<string> { "analyze_symptoms" },
                            Execute = async (inputs, ct) =>
                            {
                                await Task.Delay(600, ct);
                                AddLog("  ➤ 处方生成完成");
                                return new { PrescriptionId = Guid.NewGuid() };
                            }
                        }
                    }
                };

                var result = await _concurrencyManager.ExecuteTaskGroupAsync(taskGroup);

                AddLog($"✓ 任务组执行完成 - 成功: {result.Success}, 耗时: {result.ExecutionTime.TotalSeconds:F1}秒");
            }
            catch (Exception ex)
            {
                AddLog($"✗ 任务组执行失败: {ex.Message}");
                _logger.LogError(ex, "任务组执行失败");
            }
            finally
            {
                operation?.Complete();
            }
        }

        /// <summary>
        /// 显示行为热图
        /// </summary>
        private void ShowBehaviorHeatmap()
        {
            try
            {
                var heatmap = _behaviorAnalyzer.GetBehaviorHeatmap(TimeSpan.FromHours(24));
                
                AddLog($"📈 用户行为热图 (过去24小时):");
                AddLog($"   总操作数: {heatmap.TotalActions}");
                
                foreach (var (module, hourData) in heatmap.HeatmapData)
                {
                    var peakHour = hourData.OrderByDescending(kv => kv.Value).FirstOrDefault();
                    AddLog($"   {module}: 高峰时段 {peakHour.Key}:00 ({peakHour.Value}次)");
                }

                // 获取模块访问模式
                var pattern = _behaviorAnalyzer.GetAccessPattern(CurrentModule);
                AddLog($"\n📊 {CurrentModule}模块访问模式:");
                AddLog($"   访问频率: {pattern.AccessFrequency}次");
                AddLog($"   平均会话时长: {pattern.AverageSessionDuration.TotalMinutes:F1}分钟");
                
                if (pattern.CommonActions.Any())
                {
                    AddLog($"   常用操作:");
                    foreach (var action in pattern.CommonActions.Take(3))
                    {
                        AddLog($"     - {action.ActionName} ({action.Percentage:F1}%)");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"✗ 生成热图失败: {ex.Message}");
                _logger.LogError(ex, "显示行为热图失败");
            }
        }

        /// <summary>
        /// 优化性能
        /// </summary>
        private async Task OptimizePerformanceAsync()
        {
            using var operation = _loadingManager.StartLoading("optimize", "执行性能优化...", layer: 1);

            try
            {
                AddLog($"🔧 开始性能优化");

                // 优化预测模型
                await _behaviorAnalyzer.OptimizePredictionModelAsync();
                AddLog("  ✓ 预测模型已优化");

                // 获取并发状态
                var concurrencyStatus = _concurrencyManager.GetStatus();
                
                // 根据系统负载调整并发度
                if (concurrencyStatus.AverageCpuUsage < 40 && concurrencyStatus.QueuedTasks > 5)
                {
                    ConcurrencyLevel = Math.Min(ConcurrencyLevel + 1, 8);
                    AddLog($"  ↑ 并发度提升至 {ConcurrencyLevel}");
                }
                else if (concurrencyStatus.AverageCpuUsage > 70)
                {
                    ConcurrencyLevel = Math.Max(ConcurrencyLevel - 1, 2);
                    AddLog($"  ↓ 并发度降低至 {ConcurrencyLevel}");
                }

                // 清理缓存
                var preloadStatus = _preloadService.GetPreloadStatus();
                if (preloadStatus.TotalMemoryUsageMB > 100)
                {
                    AddLog($"  ♻ 缓存使用 {preloadStatus.TotalMemoryUsageMB:F1}MB，建议清理");
                }

                AddLog($"✓ 性能优化完成");
                
                // 更新指标
                UpdateMetrics();
            }
            catch (Exception ex)
            {
                AddLog($"✗ 性能优化失败: {ex.Message}");
                _logger.LogError(ex, "性能优化失败");
            }
            finally
            {
                operation?.Complete();
            }
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        private void GeneratePerformanceReport()
        {
            try
            {
                var preloadStats = _preloadService.GetStatistics();
                var preloadStatus = _preloadService.GetPreloadStatus();
                var concurrencyMetrics = _concurrencyManager.GetMetrics();
                var concurrencyStatus = _concurrencyManager.GetStatus();

                var report = $@"
═══════════════════════════════════════
        性能优化报告
        {DateTime.Now:yyyy-MM-dd HH:mm:ss}
═══════════════════════════════════════

【预加载服务】
• 总预加载次数: {preloadStats.TotalPreloads:N0}
• 成功率: {(preloadStats.TotalPreloads > 0 ? (double)preloadStats.SuccessfulPreloads / preloadStats.TotalPreloads * 100 : 0):F1}%
• 缓存命中率: {preloadStats.CacheHitRate:F1}%
• 数据预加载量: {preloadStats.TotalBytesPreloaded / (1024.0 * 1024.0):F2} MB
• 平均预加载大小: {preloadStats.AveragePreloadSizeMB:F2} MB
• 当前状态: {(preloadStatus.IsActive ? (preloadStatus.IsPaused ? "已暂停" : "运行中") : "已停止")}
• 队列任务: {preloadStatus.QueuedTasks}
• 活动任务: {preloadStatus.ActiveTasks}

【并发管理】
• 总执行任务: {concurrencyMetrics.TotalTasksExecuted:N0}
• 成功率: {concurrencyMetrics.SuccessRate:F1}%
• 平均执行时间: {concurrencyMetrics.AverageExecutionTimeMs:F1} ms
• 当前吞吐量: {concurrencyMetrics.CurrentThroughput:F1} 任务/秒
• 峰值并发度: {concurrencyMetrics.PeakConcurrency}
• 资源利用率: {concurrencyMetrics.ResourceUtilization * 100:F1}%
• 当前并发度: {concurrencyStatus.CurrentConcurrency}
• 活动任务数: {concurrencyStatus.ActiveTasks}
• 队列深度: 高:{concurrencyStatus.HighPriorityQueued} 中:{concurrencyStatus.NormalPriorityQueued} 低:{concurrencyStatus.LowPriorityQueued}

【系统资源】
• CPU使用率: {concurrencyStatus.AverageCpuUsage:F1}%
• 内存使用: {concurrencyStatus.AverageMemoryUsageMB:F1} MB
• 缓存内存: {preloadStatus.TotalMemoryUsageMB:F1} MB

【优化建议】
";
                // 生成优化建议
                if (preloadStats.CacheHitRate < 50)
                {
                    report += "• ⚠ 缓存命中率较低，建议增加缓存时长\n";
                }
                
                if (concurrencyMetrics.SuccessRate < 90)
                {
                    report += "• ⚠ 任务成功率偏低，建议检查错误处理\n";
                }
                
                if (concurrencyStatus.AverageCpuUsage > 80)
                {
                    report += "• ⚠ CPU使用率过高，建议降低并发度\n";
                }
                else if (concurrencyStatus.AverageCpuUsage < 30 && concurrencyStatus.QueuedTasks > 0)
                {
                    report += "• ⚠ CPU空闲但有排队任务，建议提高并发度\n";
                }
                
                if (preloadStatus.TotalMemoryUsageMB > 150)
                {
                    report += "• ⚠ 缓存占用内存过多，建议清理或减少缓存大小\n";
                }

                report += "\n═══════════════════════════════════════";

                PerformanceReport = report;
                AddLog("📄 性能报告已生成");
            }
            catch (Exception ex)
            {
                AddLog($"✗ 生成报告失败: {ex.Message}");
                _logger.LogError(ex, "生成性能报告失败");
            }
        }

        #endregion

        #region 辅助方法

        private void UpdateMetrics()
        {
            try
            {
                var preloadStatus = _preloadService.GetPreloadStatus();
                var concurrencyStatus = _concurrencyManager.GetStatus();
                var concurrencyMetrics = _concurrencyManager.GetMetrics();

                Metrics.Clear();
                Metrics.Add(new MetricDisplayItem { Name = "缓存命中率", Value = $"{preloadStatus.CacheHitRate:F1}%", Category = "预加载" });
                Metrics.Add(new MetricDisplayItem { Name = "队列任务", Value = $"{preloadStatus.QueuedTasks}", Category = "预加载" });
                Metrics.Add(new MetricDisplayItem { Name = "活动任务", Value = $"{concurrencyStatus.ActiveTasks}", Category = "并发" });
                Metrics.Add(new MetricDisplayItem { Name = "成功率", Value = $"{concurrencyMetrics.SuccessRate:F1}%", Category = "并发" });
                Metrics.Add(new MetricDisplayItem { Name = "CPU使用", Value = $"{concurrencyStatus.AverageCpuUsage:F1}%", Category = "系统" });
                Metrics.Add(new MetricDisplayItem { Name = "内存使用", Value = $"{concurrencyStatus.AverageMemoryUsageMB:F0}MB", Category = "系统" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新指标失败");
            }
        }

        private void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            ExecutionLog.Insert(0, $"[{timestamp}] {message}");
            
            // 限制日志数量
            while (ExecutionLog.Count > 100)
            {
                ExecutionLog.RemoveAt(ExecutionLog.Count - 1);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // 清理资源
            (_behaviorAnalyzer as IDisposable)?.Dispose();
            (_preloadService as IDisposable)?.Dispose();
            (_concurrencyManager as IDisposable)?.Dispose();
        }

        #endregion
    }

    #region 显示模型

    /// <summary>
    /// 预测显示项
    /// </summary>
    public class PredictionDisplayItem
    {
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Probability { get; set; } = string.Empty;
        public string DataToPreload { get; set; } = string.Empty;
        public string EstimatedTime { get; set; } = string.Empty;
    }

    /// <summary>
    /// 指标显示项
    /// </summary>
    public class MetricDisplayItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    #endregion
}