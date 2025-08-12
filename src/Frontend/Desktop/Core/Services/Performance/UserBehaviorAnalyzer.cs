using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// 用户行为分析器 - UltraThink Stage 5.2.3 创新组件
    /// 
    /// 核心功能：
    /// 1. 收集和分析用户操作模式
    /// 2. 预测下一步可能的操作
    /// 3. 生成预加载建议
    /// 4. 学习和优化预测模型
    /// </summary>
    public interface IUserBehaviorAnalyzer
    {
        /// <summary>
        /// 记录用户操作
        /// </summary>
        void RecordAction(UserAction action);

        /// <summary>
        /// 预测下一步操作
        /// </summary>
        Task<PredictionResult> PredictNextActionAsync(string currentModule, string currentAction);

        /// <summary>
        /// 获取模块访问模式
        /// </summary>
        ModuleAccessPattern GetAccessPattern(string moduleName);

        /// <summary>
        /// 获取用户操作热图
        /// </summary>
        UserBehaviorHeatmap GetBehaviorHeatmap(TimeSpan period);

        /// <summary>
        /// 优化预测模型
        /// </summary>
        Task OptimizePredictionModelAsync();
    }

    /// <summary>
    /// 用户行为分析器实现
    /// </summary>
    public class UserBehaviorAnalyzer : IUserBehaviorAnalyzer, IDisposable
    {
        #region 私有字段

        private readonly ILogger<UserBehaviorAnalyzer> _logger;
        
        // 操作历史记录
        private readonly ConcurrentQueue<UserAction> _actionHistory = new();
        private readonly int _maxHistorySize = 1000;
        
        // 操作序列模式识别
        private readonly ConcurrentDictionary<string, ActionSequencePattern> _sequencePatterns = new();
        
        // 模块转换概率矩阵
        private readonly ConcurrentDictionary<string, ModuleTransitionProbability> _transitionMatrix = new();
        
        // 时间模式分析
        private readonly ConcurrentDictionary<int, TimeBasedPattern> _timePatterns = new();
        
        // 预测模型优化定时器
        private readonly Timer _optimizationTimer;
        
        // 性能计数器
        private long _totalPredictions = 0;
        private long _correctPredictions = 0;

        #endregion

        #region 构造函数

        public UserBehaviorAnalyzer(ILogger<UserBehaviorAnalyzer> logger)
        {
            _logger = logger;
            
            InitializeDefaultPatterns();
            
            // 每30分钟优化一次预测模型
            _optimizationTimer = new Timer(
                async _ => await OptimizePredictionModelAsync(),
                null,
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(30));
            
            _logger.LogInformation("用户行为分析器已初始化");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 记录用户操作
        /// </summary>
        public void RecordAction(UserAction action)
        {
            try
            {
                // 添加到历史记录
                _actionHistory.Enqueue(action);
                
                // 限制历史记录大小
                while (_actionHistory.Count > _maxHistorySize)
                {
                    _actionHistory.TryDequeue(out _);
                }

                // 更新操作序列模式
                UpdateSequencePatterns(action);
                
                // 更新模块转换概率
                UpdateTransitionProbabilities(action);
                
                // 更新时间模式
                UpdateTimePatterns(action);
                
                _logger.LogDebug("记录用户操作: {Module}.{Action} at {Time}",
                    action.ModuleName, action.ActionName, action.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录用户操作时发生错误");
            }
        }

        /// <summary>
        /// 预测下一步操作
        /// </summary>
        public async Task<PredictionResult> PredictNextActionAsync(string currentModule, string currentAction)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var predictions = new List<PredictedAction>();
                    
                    // 基于序列模式预测
                    var sequencePredictions = PredictFromSequencePatterns(currentModule, currentAction);
                    predictions.AddRange(sequencePredictions);
                    
                    // 基于转换概率预测
                    var transitionPredictions = PredictFromTransitionMatrix(currentModule);
                    predictions.AddRange(transitionPredictions);
                    
                    // 基于时间模式预测
                    var timePredictions = PredictFromTimePatterns();
                    predictions.AddRange(timePredictions);
                    
                    // 合并和排序预测结果
                    var topPredictions = predictions
                        .GroupBy(p => new { p.ModuleName, p.ActionName })
                        .Select(g => new PredictedAction
                        {
                            ModuleName = g.Key.ModuleName,
                            ActionName = g.Key.ActionName,
                            Probability = g.Max(p => p.Probability),
                            DataToPreload = g.SelectMany(p => p.DataToPreload).Distinct().ToList(),
                            EstimatedTimeToAction = g.Min(p => p.EstimatedTimeToAction)
                        })
                        .OrderByDescending(p => p.Probability)
                        .Take(5)
                        .ToList();

                    Interlocked.Increment(ref _totalPredictions);
                    
                    return new PredictionResult
                    {
                        PredictedActions = topPredictions,
                        Confidence = CalculateConfidence(topPredictions),
                        PredictionTime = DateTime.Now
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "预测下一步操作时发生错误");
                    return new PredictionResult
                    {
                        PredictedActions = new List<PredictedAction>(),
                        Confidence = 0,
                        PredictionTime = DateTime.Now
                    };
                }
            });
        }

        /// <summary>
        /// 获取模块访问模式
        /// </summary>
        public ModuleAccessPattern GetAccessPattern(string moduleName)
        {
            var moduleActions = _actionHistory
                .Where(a => a.ModuleName == moduleName)
                .ToList();

            if (!moduleActions.Any())
            {
                return new ModuleAccessPattern
                {
                    ModuleName = moduleName,
                    AccessFrequency = 0,
                    AverageSessionDuration = TimeSpan.Zero
                };
            }

            // 分析访问模式
            var accessTimes = moduleActions.Select(a => a.Timestamp).ToList();
            var peakHours = accessTimes
                .GroupBy(t => t.Hour)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            var commonActions = moduleActions
                .GroupBy(a => a.ActionName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new ActionFrequency
                {
                    ActionName = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / moduleActions.Count * 100
                })
                .ToList();

            return new ModuleAccessPattern
            {
                ModuleName = moduleName,
                AccessFrequency = moduleActions.Count,
                PeakAccessHours = peakHours,
                CommonActions = commonActions,
                AverageSessionDuration = CalculateAverageSessionDuration(moduleActions),
                LastAccessTime = moduleActions.Max(a => a.Timestamp)
            };
        }

        /// <summary>
        /// 获取用户操作热图
        /// </summary>
        public UserBehaviorHeatmap GetBehaviorHeatmap(TimeSpan period)
        {
            var cutoffTime = DateTime.Now - period;
            var relevantActions = _actionHistory
                .Where(a => a.Timestamp >= cutoffTime)
                .ToList();

            var heatmapData = new Dictionary<string, Dictionary<int, int>>();

            foreach (var action in relevantActions)
            {
                var module = action.ModuleName;
                var hour = action.Timestamp.Hour;

                if (!heatmapData.ContainsKey(module))
                {
                    heatmapData[module] = new Dictionary<int, int>();
                }

                if (!heatmapData[module].ContainsKey(hour))
                {
                    heatmapData[module][hour] = 0;
                }

                heatmapData[module][hour]++;
            }

            return new UserBehaviorHeatmap
            {
                Period = period,
                HeatmapData = heatmapData,
                TotalActions = relevantActions.Count,
                GeneratedTime = DateTime.Now
            };
        }

        /// <summary>
        /// 优化预测模型
        /// </summary>
        public async Task OptimizePredictionModelAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("开始优化预测模型");
                    
                    // 清理过期的模式
                    CleanupOldPatterns();
                    
                    // 重新计算转换概率
                    RecalculateTransitionProbabilities();
                    
                    // 更新时间模式权重
                    UpdateTimePatternWeights();
                    
                    // 计算预测准确率
                    var accuracy = _totalPredictions > 0 
                        ? (double)_correctPredictions / _totalPredictions * 100 
                        : 0;
                    
                    _logger.LogInformation("预测模型优化完成 - 准确率: {Accuracy:F2}%", accuracy);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "优化预测模型时发生错误");
                }
            });
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化默认模式（基于中医诊所业务流程）
        /// </summary>
        private void InitializeDefaultPatterns()
        {
            // 初始化常见的操作序列模式
            _sequencePatterns["Patients.View->Consultation.Create"] = new ActionSequencePattern
            {
                Sequence = new[] { "Patients.View", "Consultation.Create" },
                Frequency = 100,
                AverageTimeInterval = TimeSpan.FromSeconds(5)
            };

            _sequencePatterns["Consultation.Create->Prescriptions.Create"] = new ActionSequencePattern
            {
                Sequence = new[] { "Consultation.Create", "Prescriptions.Create" },
                Frequency = 90,
                AverageTimeInterval = TimeSpan.FromMinutes(10)
            };

            _sequencePatterns["Prescriptions.Create->Prescriptions.Print"] = new ActionSequencePattern
            {
                Sequence = new[] { "Prescriptions.Create", "Prescriptions.Print" },
                Frequency = 85,
                AverageTimeInterval = TimeSpan.FromSeconds(30)
            };

            // 初始化模块转换概率
            _transitionMatrix["Patients"] = new ModuleTransitionProbability
            {
                FromModule = "Patients",
                Transitions = new Dictionary<string, double>
                {
                    { "Consultation", 0.7 },
                    { "MedicalCase", 0.2 },
                    { "Prescriptions", 0.1 }
                }
            };

            _transitionMatrix["Consultation"] = new ModuleTransitionProbability
            {
                FromModule = "Consultation",
                Transitions = new Dictionary<string, double>
                {
                    { "Prescriptions", 0.8 },
                    { "MedicalCase", 0.15 },
                    { "Patients", 0.05 }
                }
            };
        }

        /// <summary>
        /// 更新操作序列模式
        /// </summary>
        private void UpdateSequencePatterns(UserAction action)
        {
            var recentActions = _actionHistory
                .TakeLast(5)
                .Select(a => $"{a.ModuleName}.{a.ActionName}")
                .ToList();

            if (recentActions.Count >= 2)
            {
                for (int i = 0; i < recentActions.Count - 1; i++)
                {
                    var sequence = $"{recentActions[i]}->{recentActions[i + 1]}";
                    
                    _sequencePatterns.AddOrUpdate(sequence,
                        new ActionSequencePattern
                        {
                            Sequence = new[] { recentActions[i], recentActions[i + 1] },
                            Frequency = 1,
                            LastOccurrence = DateTime.Now
                        },
                        (key, existing) =>
                        {
                            existing.Frequency++;
                            existing.LastOccurrence = DateTime.Now;
                            return existing;
                        });
                }
            }
        }

        /// <summary>
        /// 更新模块转换概率
        /// </summary>
        private void UpdateTransitionProbabilities(UserAction action)
        {
            var previousAction = _actionHistory
                .TakeLast(2)
                .FirstOrDefault();

            if (previousAction != null && previousAction.ModuleName != action.ModuleName)
            {
                _transitionMatrix.AddOrUpdate(previousAction.ModuleName,
                    new ModuleTransitionProbability
                    {
                        FromModule = previousAction.ModuleName,
                        Transitions = new Dictionary<string, double> { { action.ModuleName, 1.0 } }
                    },
                    (key, existing) =>
                    {
                        if (!existing.Transitions.ContainsKey(action.ModuleName))
                        {
                            existing.Transitions[action.ModuleName] = 0;
                        }
                        existing.Transitions[action.ModuleName]++;
                        
                        // 归一化概率
                        var total = existing.Transitions.Values.Sum();
                        foreach (var k in existing.Transitions.Keys.ToList())
                        {
                            existing.Transitions[k] /= total;
                        }
                        
                        return existing;
                    });
            }
        }

        /// <summary>
        /// 更新时间模式
        /// </summary>
        private void UpdateTimePatterns(UserAction action)
        {
            var hour = action.Timestamp.Hour;
            
            _timePatterns.AddOrUpdate(hour,
                new TimeBasedPattern
                {
                    Hour = hour,
                    CommonModules = new Dictionary<string, int> { { action.ModuleName, 1 } },
                    CommonActions = new Dictionary<string, int> { { action.ActionName, 1 } }
                },
                (key, existing) =>
                {
                    if (!existing.CommonModules.ContainsKey(action.ModuleName))
                        existing.CommonModules[action.ModuleName] = 0;
                    existing.CommonModules[action.ModuleName]++;

                    if (!existing.CommonActions.ContainsKey(action.ActionName))
                        existing.CommonActions[action.ActionName] = 0;
                    existing.CommonActions[action.ActionName]++;

                    return existing;
                });
        }

        /// <summary>
        /// 基于序列模式预测
        /// </summary>
        private List<PredictedAction> PredictFromSequencePatterns(string currentModule, string currentAction)
        {
            var currentKey = $"{currentModule}.{currentAction}";
            var predictions = new List<PredictedAction>();

            foreach (var pattern in _sequencePatterns.Where(p => p.Key.StartsWith(currentKey + "->")))
            {
                var nextAction = pattern.Key.Split("->")[1];
                var parts = nextAction.Split('.');
                
                if (parts.Length == 2)
                {
                    predictions.Add(new PredictedAction
                    {
                        ModuleName = parts[0],
                        ActionName = parts[1],
                        Probability = Math.Min(0.9, pattern.Value.Frequency / 100.0),
                        DataToPreload = GetPreloadDataForAction(parts[0], parts[1]),
                        EstimatedTimeToAction = pattern.Value.AverageTimeInterval ?? TimeSpan.FromSeconds(5)
                    });
                }
            }

            return predictions;
        }

        /// <summary>
        /// 基于转换概率预测
        /// </summary>
        private List<PredictedAction> PredictFromTransitionMatrix(string currentModule)
        {
            var predictions = new List<PredictedAction>();

            if (_transitionMatrix.TryGetValue(currentModule, out var transitions))
            {
                foreach (var transition in transitions.Transitions.OrderByDescending(t => t.Value).Take(3))
                {
                    predictions.Add(new PredictedAction
                    {
                        ModuleName = transition.Key,
                        ActionName = "View", // 默认动作
                        Probability = transition.Value * 0.8, // 略低于序列模式
                        DataToPreload = GetPreloadDataForModule(transition.Key),
                        EstimatedTimeToAction = TimeSpan.FromSeconds(10)
                    });
                }
            }

            return predictions;
        }

        /// <summary>
        /// 基于时间模式预测
        /// </summary>
        private List<PredictedAction> PredictFromTimePatterns()
        {
            var predictions = new List<PredictedAction>();
            var currentHour = DateTime.Now.Hour;

            if (_timePatterns.TryGetValue(currentHour, out var pattern))
            {
                var topModule = pattern.CommonModules
                    .OrderByDescending(m => m.Value)
                    .FirstOrDefault();

                if (topModule.Key != null)
                {
                    predictions.Add(new PredictedAction
                    {
                        ModuleName = topModule.Key,
                        ActionName = "View",
                        Probability = 0.6, // 时间模式权重较低
                        DataToPreload = GetPreloadDataForModule(topModule.Key),
                        EstimatedTimeToAction = TimeSpan.FromMinutes(5)
                    });
                }
            }

            return predictions;
        }

        /// <summary>
        /// 获取操作的预加载数据建议
        /// </summary>
        private List<string> GetPreloadDataForAction(string module, string action)
        {
            return (module, action) switch
            {
                ("Prescriptions", "Create") => new List<string> { "Herbs", "FormulaTemplates", "CurrentPatient" },
                ("Consultation", "Create") => new List<string> { "PatientHistory", "CommonDiagnosis", "Symptoms" },
                ("Patients", "View") => new List<string> { "PatientList", "RecentPatients" },
                _ => new List<string>()
            };
        }

        /// <summary>
        /// 获取模块的预加载数据建议
        /// </summary>
        private List<string> GetPreloadDataForModule(string module)
        {
            return module switch
            {
                "Prescriptions" => new List<string> { "Herbs", "FormulaTemplates" },
                "Consultation" => new List<string> { "Symptoms", "Diagnoses" },
                "Patients" => new List<string> { "PatientList" },
                "MedicalCase" => new List<string> { "CaseHistory", "Templates" },
                _ => new List<string>()
            };
        }

        /// <summary>
        /// 计算预测置信度
        /// </summary>
        private double CalculateConfidence(List<PredictedAction> predictions)
        {
            if (!predictions.Any()) return 0;

            var topProbability = predictions.First().Probability;
            var totalProbability = predictions.Sum(p => p.Probability);

            // 置信度基于最高概率和概率分布
            return topProbability * (topProbability / Math.Max(0.01, totalProbability));
        }

        /// <summary>
        /// 计算平均会话时长
        /// </summary>
        private TimeSpan CalculateAverageSessionDuration(List<UserAction> actions)
        {
            if (actions.Count < 2) return TimeSpan.Zero;

            var sessions = new List<TimeSpan>();
            DateTime? sessionStart = null;

            foreach (var action in actions.OrderBy(a => a.Timestamp))
            {
                if (sessionStart == null)
                {
                    sessionStart = action.Timestamp;
                }
                else if (action.Timestamp - sessionStart.Value > TimeSpan.FromMinutes(30))
                {
                    // 超过30分钟认为是新会话
                    sessions.Add(action.Timestamp - sessionStart.Value);
                    sessionStart = action.Timestamp;
                }
            }

            return sessions.Any() 
                ? TimeSpan.FromSeconds(sessions.Average(s => s.TotalSeconds))
                : TimeSpan.Zero;
        }

        /// <summary>
        /// 清理过期模式
        /// </summary>
        private void CleanupOldPatterns()
        {
            var cutoff = DateTime.Now - TimeSpan.FromDays(7);

            // 清理过期的序列模式
            var oldPatterns = _sequencePatterns
                .Where(p => p.Value.LastOccurrence < cutoff)
                .Select(p => p.Key)
                .ToList();

            foreach (var key in oldPatterns)
            {
                _sequencePatterns.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// 重新计算转换概率
        /// </summary>
        private void RecalculateTransitionProbabilities()
        {
            // 基于最近的历史记录重新计算
            var recentActions = _actionHistory.TakeLast(500).ToList();
            
            // 重置转换矩阵
            foreach (var key in _transitionMatrix.Keys.ToList())
            {
                _transitionMatrix[key].Transitions.Clear();
            }

            // 重新计算
            for (int i = 0; i < recentActions.Count - 1; i++)
            {
                if (recentActions[i].ModuleName != recentActions[i + 1].ModuleName)
                {
                    UpdateTransitionProbabilities(recentActions[i + 1]);
                }
            }
        }

        /// <summary>
        /// 更新时间模式权重
        /// </summary>
        private void UpdateTimePatternWeights()
        {
            // 基于最近7天的数据调整权重
            var recentActions = _actionHistory
                .Where(a => a.Timestamp >= DateTime.Now - TimeSpan.FromDays(7))
                .ToList();

            foreach (var pattern in _timePatterns.Values)
            {
                var hourActions = recentActions
                    .Where(a => a.Timestamp.Hour == pattern.Hour)
                    .ToList();

                if (hourActions.Any())
                {
                    // 重新计算该时段的常见模块和操作
                    pattern.CommonModules = hourActions
                        .GroupBy(a => a.ModuleName)
                        .ToDictionary(g => g.Key, g => g.Count());

                    pattern.CommonActions = hourActions
                        .GroupBy(a => a.ActionName)
                        .ToDictionary(g => g.Key, g => g.Count());
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _optimizationTimer?.Dispose();
            _logger.LogInformation("用户行为分析器已释放");
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 用户操作
    /// </summary>
    public class UserAction
    {
        public string ModuleName { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string? EntityType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 预测结果
    /// </summary>
    public class PredictionResult
    {
        public List<PredictedAction> PredictedActions { get; set; } = new();
        public double Confidence { get; set; }
        public DateTime PredictionTime { get; set; }
    }

    /// <summary>
    /// 预测的操作
    /// </summary>
    public class PredictedAction
    {
        public string ModuleName { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public double Probability { get; set; }
        public List<string> DataToPreload { get; set; } = new();
        public TimeSpan EstimatedTimeToAction { get; set; }
    }

    /// <summary>
    /// 模块访问模式
    /// </summary>
    public class ModuleAccessPattern
    {
        public string ModuleName { get; set; } = string.Empty;
        public int AccessFrequency { get; set; }
        public List<int> PeakAccessHours { get; set; } = new();
        public List<ActionFrequency> CommonActions { get; set; } = new();
        public TimeSpan AverageSessionDuration { get; set; }
        public DateTime LastAccessTime { get; set; }
    }

    /// <summary>
    /// 操作频率
    /// </summary>
    public class ActionFrequency
    {
        public string ActionName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 用户行为热图
    /// </summary>
    public class UserBehaviorHeatmap
    {
        public TimeSpan Period { get; set; }
        public Dictionary<string, Dictionary<int, int>> HeatmapData { get; set; } = new();
        public int TotalActions { get; set; }
        public DateTime GeneratedTime { get; set; }
    }

    /// <summary>
    /// 操作序列模式
    /// </summary>
    internal class ActionSequencePattern
    {
        public string[] Sequence { get; set; } = Array.Empty<string>();
        public int Frequency { get; set; }
        public TimeSpan? AverageTimeInterval { get; set; }
        public DateTime LastOccurrence { get; set; }
    }

    /// <summary>
    /// 模块转换概率
    /// </summary>
    internal class ModuleTransitionProbability
    {
        public string FromModule { get; set; } = string.Empty;
        public Dictionary<string, double> Transitions { get; set; } = new();
    }

    /// <summary>
    /// 时间模式
    /// </summary>
    internal class TimeBasedPattern
    {
        public int Hour { get; set; }
        public Dictionary<string, int> CommonModules { get; set; } = new();
        public Dictionary<string, int> CommonActions { get; set; } = new();
    }

    #endregion
}