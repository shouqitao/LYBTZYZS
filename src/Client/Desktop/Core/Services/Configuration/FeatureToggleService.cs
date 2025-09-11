using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Configuration
{

    /// <summary>
    /// 特性开关服务接口 - UltraThink Stage 5.3.2
    /// 提供功能开关、A/B测试、灰度发布等能力
    /// </summary>
    public interface IFeatureToggleService
    {

        /// <summary>
        /// 检查特性是否启用
        /// </summary>
        bool IsEnabled(string featureName);

        /// <summary>
        /// 检查特性是否对特定用户启用
        /// </summary>
        bool IsEnabledForUser(string featureName, string userId);

        /// <summary>
        /// 获取特性变体（用于A/B测试）
        /// </summary>
        string GetVariant(string featureName, string userId);

        /// <summary>
        /// 设置特性状态
        /// </summary>
        Task SetFeatureStateAsync(string featureName, FeatureState state);

        /// <summary>
        /// 获取特性配置
        /// </summary>
        FeatureConfiguration? GetFeatureConfiguration(string featureName);

        /// <summary>
        /// 获取所有特性
        /// </summary>
        List<FeatureInfo> GetAllFeatures();

        /// <summary>
        /// 注册特性
        /// </summary>
        Task RegisterFeatureAsync(FeatureDefinition definition);

        /// <summary>
        /// 评估特性（带上下文）
        /// </summary>
        FeatureEvaluationResult Evaluate(string featureName, EvaluationContext context);

        /// <summary>
        /// 注册特性变更监听
        /// </summary>
        IDisposable OnFeatureChanged(string featureName, Action<FeatureChangeEventArgs> callback);

        /// <summary>
        /// 获取特性使用统计
        /// </summary>
        FeatureUsageStatistics GetUsageStatistics(string featureName);
    }

    /// <summary>
    /// 特性开关服务实现
    /// </summary>
    public class FeatureToggleService : IFeatureToggleService
    {
        private readonly ILogger<FeatureToggleService> _logger;
        private readonly IConfigurationManagerService _configService;
        private readonly Dictionary<string, FeatureDefinition> _features = new();
        private readonly Dictionary<string, FeatureUsageData> _usageData = new();
        private readonly Dictionary<string, List<FeatureChangeCallback>> _callbacks = new();
        private readonly object _lock = new object();

        public FeatureToggleService(
            ILogger<FeatureToggleService> logger,
            IConfigurationManagerService configService)
        {
            _logger = logger;
            _configService = configService;

            InitializeDefaultFeatures();
            LoadFeatureConfigurations();
        }

        #region 初始化

        private void InitializeDefaultFeatures()
        {
            // 注册默认特性
            var defaultFeatures = new[]
            {
                new FeatureDefinition
                {
                    Name = "NewPrescriptionUI",
                    DisplayName = "新版处方界面",
                    Description = "使用优化后的处方编辑界面",
                    Category = "UI",
                    DefaultState = FeatureState.Disabled,
                    ReleaseStage = ReleaseStage.Beta,
                    Tags = new[] { "ui", "prescription", "beta" }
                },
                new FeatureDefinition
                {
                    Name = "SmartDiagnosis",
                    DisplayName = "[已移除] 智能诊断辅助",
                    Description = "Record-Only模式：AI辅助的中医诊断功能已移除",
                    Category = "Deprecated",
                    DefaultState = FeatureState.Disabled,
                    CurrentState = FeatureState.Disabled,
                    ReleaseStage = ReleaseStage.Alpha,
                    Tags = new[] { "deprecated", "removed", "record-only" }
                },
                new FeatureDefinition
                {
                    Name = "AdvancedCaching",
                    DisplayName = "高级缓存策略",
                    Description = "使用预测性缓存优化性能",
                    Category = "Performance",
                    DefaultState = FeatureState.Enabled,
                    ReleaseStage = ReleaseStage.GA,
                    Tags = new[] { "performance", "cache" }
                },
                new FeatureDefinition
                {
                    Name = "DetailedLogging",
                    DisplayName = "详细日志记录",
                    Description = "记录详细的操作和性能日志",
                    Category = "Monitoring",
                    DefaultState = FeatureState.EnabledForAdmins,
                    ReleaseStage = ReleaseStage.GA,
                    Tags = new[] { "logging", "monitoring" }
                },
                new FeatureDefinition
                {
                    Name = "BatchOperations",
                    DisplayName = "批量操作优化",
                    Description = "启用批量API调用和数据处理",
                    Category = "Performance",
                    DefaultState = FeatureState.Enabled,
                    ReleaseStage = ReleaseStage.GA,
                    RolloutPercentage = 100,
                    Tags = new[] { "performance", "api" }
                }
            };

            foreach (var feature in defaultFeatures)
            {
                _features[feature.Name] = feature;
                _usageData[feature.Name] = new FeatureUsageData();
            }

            _logger.LogInformation("初始化了 {Count} 个默认特性开关", defaultFeatures.Length);
        }

        private void LoadFeatureConfigurations()
        {
            try
            {
                // 从配置加载特性状态覆盖
                var featureSection = _configService.GetSection("Features");

                foreach (var child in featureSection.GetChildren())
                {
                    var featureName = child.Key;
                    if (_features.ContainsKey(featureName))
                    {
                        var state = child["State"];
                        if (Enum.TryParse<FeatureState>(state, out var featureState))
                        {
                            _features[featureName].CurrentState = featureState;
                        }

                        var rollout = child["RolloutPercentage"];
                        if (int.TryParse(rollout, out var percentage))
                        {
                            _features[featureName].RolloutPercentage = percentage;
                        }
                    }
                }

                _logger.LogDebug("特性配置加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载特性配置失败");
            }
        }

        #endregion 初始化

        #region 核心功能

        /// <inheritdoc/>
        public bool IsEnabled(string featureName)
        {
            return IsEnabledForUser(featureName, "default");
        }

        /// <inheritdoc/>
        public bool IsEnabledForUser(string featureName, string userId)
        {
            try
            {
                lock (_lock)
                {
                    if (!_features.TryGetValue(featureName, out var feature))
                    {
                        _logger.LogWarning("未知特性: {FeatureName}", featureName);
                        return false;
                    }

                    // 记录使用
                    RecordUsage(featureName);

                    // 评估特性状态
                    var context = new EvaluationContext
                    {
                        UserId = userId,
                        UserRole = GetUserRole(userId),
                        Environment = GetCurrentEnvironment(),
                        Random = GetUserHash(userId, featureName)
                    };

                    var result = EvaluateFeature(feature, context);

                    _logger.LogDebug(
                        "特性 {Feature} 对用户 {User} 的状态: {Enabled}",
                        featureName, userId, result);

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "评估特性 {Feature} 失败", featureName);
                return false;
            }
        }

        /// <inheritdoc/>
        public string GetVariant(string featureName, string userId)
        {
            lock (_lock)
            {
                if (!_features.TryGetValue(featureName, out var feature))
                {
                    return "control";
                }

                if (feature.Variants == null || feature.Variants.Count == 0)
                {
                    return IsEnabledForUser(featureName, userId) ? "enabled" : "disabled";
                }

                // 基于用户哈希选择变体
                var hash = GetUserHash(userId, featureName);
                var totalWeight = feature.Variants.Sum(v => v.Weight);
                var target = hash % totalWeight;

                var current = 0;
                foreach (var variant in feature.Variants)
                {
                    current += variant.Weight;
                    if (target < current)
                    {
                        RecordVariantUsage(featureName, variant.Name);
                        return variant.Name;
                    }
                }

                return feature.Variants.First().Name;
            }
        }

        /// <inheritdoc/>
        public async Task SetFeatureStateAsync(string featureName, FeatureState state)
        {
            try
            {
                lock (_lock)
                {
                    if (!_features.ContainsKey(featureName))
                    {
                        throw new ArgumentException($"特性 {featureName} 不存在");
                    }

                    var oldState = _features[featureName].CurrentState;
                    _features[featureName].CurrentState = state;

                    // 触发变更事件
                    NotifyFeatureChanged(featureName, oldState, state);
                }

                // 保存到配置
                await _configService.SetValueAsync($"Features:{featureName}:State", state.ToString());

                _logger.LogInformation("特性 {Feature} 状态已更新为 {State}", featureName, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置特性状态失败: {Feature}", featureName);
                throw;
            }
        }

        /// <inheritdoc/>
        public FeatureConfiguration? GetFeatureConfiguration(string featureName)
        {
            lock (_lock)
            {
                if (!_features.TryGetValue(featureName, out var feature))
                {
                    return null;
                }

                return new FeatureConfiguration
                {
                    Name = feature.Name,
                    DisplayName = feature.DisplayName,
                    Description = feature.Description,
                    State = feature.CurrentState,
                    RolloutPercentage = feature.RolloutPercentage,
                    Variants = feature.Variants?.ToList(),
                    Rules = feature.Rules?.ToList(),
                    Metadata = feature.Metadata
                };
            }
        }

        /// <inheritdoc/>
        public List<FeatureInfo> GetAllFeatures()
        {
            lock (_lock)
            {
                return _features.Values.Select(f => new FeatureInfo
                {
                    Name = f.Name,
                    DisplayName = f.DisplayName,
                    Category = f.Category,
                    State = f.CurrentState,
                    ReleaseStage = f.ReleaseStage,
                    IsEnabled = IsEnabled(f.Name),
                    Usage = _usageData.ContainsKey(f.Name) ?
                        _usageData[f.Name].TotalChecks : 0
                }).ToList();
            }
        }

        /// <inheritdoc/>
        public async Task RegisterFeatureAsync(FeatureDefinition definition)
        {
            try
            {
                lock (_lock)
                {
                    _features[definition.Name] = definition;

                    if (!_usageData.ContainsKey(definition.Name))
                    {
                        _usageData[definition.Name] = new FeatureUsageData();
                    }
                }

                // 保存到配置
                await _configService.SetValueAsync($"Features:{definition.Name}:Registered", true);

                _logger.LogInformation("特性 {Feature} 已注册", definition.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册特性失败: {Feature}", definition.Name);
                throw;
            }
        }

        /// <inheritdoc/>
        public FeatureEvaluationResult Evaluate(string featureName, EvaluationContext context)
        {
            lock (_lock)
            {
                if (!_features.TryGetValue(featureName, out var feature))
                {
                    return new FeatureEvaluationResult
                    {
                        FeatureName = featureName,
                        IsEnabled = false,
                        Reason = "特性不存在"
                    };
                }

                var enabled = EvaluateFeature(feature, context);
                var variant = GetVariant(featureName, context.UserId);

                return new FeatureEvaluationResult
                {
                    FeatureName = featureName,
                    IsEnabled = enabled,
                    Variant = variant,
                    Reason = GetEvaluationReason(feature, context, enabled),
                    EvaluationTime = DateTime.Now
                };
            }
        }

        /// <inheritdoc/>
        public IDisposable OnFeatureChanged(string featureName, Action<FeatureChangeEventArgs> callback)
        {
            lock (_callbacks)
            {
                if (!_callbacks.ContainsKey(featureName))
                {
                    _callbacks[featureName] = new List<FeatureChangeCallback>();
                }

                var registration = new FeatureChangeCallback(callback);
                _callbacks[featureName].Add(registration);

                return new CallbackDisposable(() =>
                {
                    lock (_callbacks)
                    {
                        _callbacks[featureName].Remove(registration);
                    }
                });
            }
        }

        /// <inheritdoc/>
        public FeatureUsageStatistics GetUsageStatistics(string featureName)
        {
            lock (_lock)
            {
                if (!_usageData.TryGetValue(featureName, out var data))
                {
                    return new FeatureUsageStatistics { FeatureName = featureName };
                }

                return new FeatureUsageStatistics
                {
                    FeatureName = featureName,
                    TotalChecks = data.TotalChecks,
                    EnabledCount = data.EnabledCount,
                    DisabledCount = data.DisabledCount,
                    VariantUsage = data.VariantUsage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    LastCheckedTime = data.LastCheckedTime,
                    UniqueUsers = data.UniqueUsers.Count
                };
            }
        }

        #endregion 核心功能

        #region 私有方法

        private bool EvaluateFeature(FeatureDefinition feature, EvaluationContext context)
        {
            // 检查全局状态
            switch (feature.CurrentState)
            {
                case FeatureState.Disabled:
                    return false;

                case FeatureState.Enabled:
                    return true;

                case FeatureState.EnabledForAdmins:
                    return context.UserRole == "Admin";
            }

            // 检查规则
            if (feature.Rules != null)
            {
                foreach (var rule in feature.Rules.OrderBy(r => r.Priority))
                {
                    if (EvaluateRule(rule, context))
                    {
                        return rule.Enable;
                    }
                }
            }

            // 检查灰度发布
            if (feature.RolloutPercentage.HasValue)
            {
                return context.Random < feature.RolloutPercentage.Value;
            }

            return feature.DefaultState == FeatureState.Enabled;
        }

        private bool EvaluateRule(FeatureRule rule, EvaluationContext context)
        {
            switch (rule.Type)
            {
                case RuleType.User:
                    return rule.Values?.Contains(context.UserId) ?? false;

                case RuleType.Role:
                    return rule.Values?.Contains(context.UserRole) ?? false;

                case RuleType.Environment:
                    return rule.Values?.Contains(context.Environment) ?? false;

                case RuleType.Percentage:
                    if (int.TryParse(rule.Values?.FirstOrDefault(), out var percentage))
                    {
                        return context.Random < percentage;
                    }

                    break;

                case RuleType.Time:
                    if (DateTime.TryParse(rule.Values?.FirstOrDefault(), out var startTime) &&
                        DateTime.TryParse(rule.Values?.Skip(1).FirstOrDefault(), out var endTime))
                    {
                        var now = DateTime.Now;
                        return now >= startTime && now <= endTime;
                    }

                    break;
            }

            return false;
        }

        private int GetUserHash(string userId, string featureName)
        {
            var combined = $"{userId}:{featureName}";
            return Math.Abs(combined.GetHashCode()) % 100;
        }

        private string GetUserRole(string userId)
        {
            // 从用户服务获取角色
            // 这里简化处理
            return userId == "admin" ? "Admin" : "User";
        }

        private string GetCurrentEnvironment()
        {
            return Environment.GetEnvironmentVariable("LYBT_ENVIRONMENT") ?? "Development";
        }

        private string GetEvaluationReason(FeatureDefinition feature, EvaluationContext context, bool enabled)
        {
            if (feature.CurrentState == FeatureState.Disabled)
            {
                return "特性已全局禁用";
            }

            if (feature.CurrentState == FeatureState.Enabled)
            {
                return "特性已全局启用";
            }

            if (feature.CurrentState == FeatureState.EnabledForAdmins && context.UserRole == "Admin")
            {
                return "特性对管理员启用";
            }

            if (feature.RolloutPercentage.HasValue)
            {
                return $"灰度发布 {feature.RolloutPercentage}%";
            }

            return enabled ? "符合启用规则" : "不符合启用条件";
        }

        private void RecordUsage(string featureName)
        {
            if (_usageData.TryGetValue(featureName, out var data))
            {
                data.TotalChecks++;
                data.LastCheckedTime = DateTime.Now;
            }
        }

        private void RecordVariantUsage(string featureName, string variant)
        {
            if (_usageData.TryGetValue(featureName, out var data))
            {
                if (!data.VariantUsage.ContainsKey(variant))
                {
                    data.VariantUsage[variant] = 0;
                }

                data.VariantUsage[variant]++;
            }
        }

        private void NotifyFeatureChanged(string featureName, FeatureState oldState, FeatureState newState)
        {
            List<FeatureChangeCallback>? callbacks = null;

            lock (_callbacks)
            {
                if (_callbacks.TryGetValue(featureName, out var list))
                {
                    callbacks = list.ToList();
                }
            }

            if (callbacks != null)
            {
                var args = new FeatureChangeEventArgs
                {
                    FeatureName = featureName,
                    OldState = oldState,
                    NewState = newState,
                    Timestamp = DateTime.Now
                };

                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback.Invoke(args);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "特性变更回调执行失败");
                    }
                }
            }
        }

        #endregion 私有方法
    }

    #region 数据模型

    /// <summary>
    /// 特性定义
    /// </summary>
    public class FeatureDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "General";
        public FeatureState DefaultState { get; set; } = FeatureState.Disabled;
        public FeatureState CurrentState { get; set; } = FeatureState.Disabled;
        public ReleaseStage ReleaseStage { get; set; } = ReleaseStage.Alpha;
        public int? RolloutPercentage { get; set; }
        public List<FeatureVariant>? Variants { get; set; }
        public List<FeatureRule>? Rules { get; set; }
        public string[]? Tags { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 特性状态
    /// </summary>
    public enum FeatureState
    {
        Disabled,
        Enabled,
        EnabledForAdmins,
        RollingOut
    }

    /// <summary>
    /// 发布阶段
    /// </summary>
    public enum ReleaseStage
    {
        Alpha,
        Beta,
        RC,
        GA
    }

    /// <summary>
    /// 特性变体（A/B测试）
    /// </summary>
    public class FeatureVariant
    {
        public string Name { get; set; } = string.Empty;
        public int Weight { get; set; } = 50;
        public Dictionary<string, object>? Configuration { get; set; }
    }

    /// <summary>
    /// 特性规则
    /// </summary>
    public class FeatureRule
    {
        public string Name { get; set; } = string.Empty;
        public RuleType Type { get; set; }
        public List<string>? Values { get; set; }
        public bool Enable { get; set; } = true;
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// 规则类型
    /// </summary>
    public enum RuleType
    {
        User,
        Role,
        Environment,
        Percentage,
        Time,
        Custom
    }

    /// <summary>
    /// 评估上下文
    /// </summary>
    public class EvaluationContext
    {
        public string UserId { get; set; } = string.Empty;
        public string UserRole { get; set; } = "User";
        public string Environment { get; set; } = "Development";
        public int Random { get; set; }
        public Dictionary<string, object>? CustomData { get; set; }
    }

    /// <summary>
    /// 评估结果
    /// </summary>
    public class FeatureEvaluationResult
    {
        public string FeatureName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string? Variant { get; set; }
        public string? Reason { get; set; }
        public DateTime EvaluationTime { get; set; }
    }

    /// <summary>
    /// 特性配置
    /// </summary>
    public class FeatureConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public FeatureState State { get; set; }
        public int? RolloutPercentage { get; set; }
        public List<FeatureVariant>? Variants { get; set; }
        public List<FeatureRule>? Rules { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 特性信息
    /// </summary>
    public class FeatureInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public FeatureState State { get; set; }
        public ReleaseStage ReleaseStage { get; set; }
        public bool IsEnabled { get; set; }
        public long Usage { get; set; }
    }

    /// <summary>
    /// 特性使用统计
    /// </summary>
    public class FeatureUsageStatistics
    {
        public string FeatureName { get; set; } = string.Empty;
        public long TotalChecks { get; set; }
        public long EnabledCount { get; set; }
        public long DisabledCount { get; set; }
        public Dictionary<string, long> VariantUsage { get; set; } = new();
        public DateTime? LastCheckedTime { get; set; }
        public int UniqueUsers { get; set; }
    }

    /// <summary>
    /// 特性使用数据
    /// </summary>
    internal class FeatureUsageData
    {
        public long TotalChecks { get; set; }
        public long EnabledCount { get; set; }
        public long DisabledCount { get; set; }
        public Dictionary<string, long> VariantUsage { get; set; } = new();
        public DateTime? LastCheckedTime { get; set; }
        public HashSet<string> UniqueUsers { get; set; } = new();
    }

    /// <summary>
    /// 特性变更事件参数
    /// </summary>
    public class FeatureChangeEventArgs
    {
        public string FeatureName { get; set; } = string.Empty;
        public FeatureState OldState { get; set; }
        public FeatureState NewState { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 特性变更回调
    /// </summary>
    internal class FeatureChangeCallback
    {
        private readonly Action<FeatureChangeEventArgs> _callback;

        public FeatureChangeCallback(Action<FeatureChangeEventArgs> callback)
        {
            _callback = callback;
        }

        public void Invoke(FeatureChangeEventArgs args)
        {
            _callback(args);
        }
    }

    #endregion 数据模型
}
