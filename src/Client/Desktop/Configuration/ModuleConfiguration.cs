using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Configuration;

/// <summary>
/// 模块特定配置类
/// 支持条件注册和模块特定设置
/// </summary>
public class ModuleConfiguration
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用该模块
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 服务生命周期类型
    /// </summary>
    public ServiceLifetimeType LifetimeType { get; set; } = ServiceLifetimeType.Singleton;

    /// <summary>
    /// 模块特定的配置属性
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// 延迟加载配置
    /// </summary>
    public bool LazyLoading { get; set; } = false;

    /// <summary>
    /// 条件注册表达式（基于配置的条件）
    /// </summary>
    public string? ConditionalExpression { get; set; }

    /// <summary>
    /// 模块依赖列表
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// 会话管理集成设置
    /// </summary>
    public SessionIntegrationSettings SessionIntegration { get; set; } = new();
}

/// <summary>
/// 服务生命周期类型枚举
/// </summary>
public enum ServiceLifetimeType
{
    /// <summary>
    /// 瞬态 - 每次请求创建新实例
    /// </summary>
    Transient,
    
    /// <summary>
    /// 范围 - 在同一作用域内单例
    /// </summary>
    Scoped,
    
    /// <summary>
    /// 单例 - 应用程序生命周期内单例
    /// </summary>
    Singleton
}

/// <summary>
/// 会话管理集成设置
/// </summary>
public class SessionIntegrationSettings
{
    /// <summary>
    /// 是否需要会话管理器
    /// </summary>
    public bool RequiresSessionManager { get; set; } = false;

    /// <summary>
    /// 会话超时配置（分钟）
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// 是否自动续期会话
    /// </summary>
    public bool AutoRenewSession { get; set; } = true;
}

/// <summary>
/// 模块配置管理器
/// </summary>
public class ModuleConfigurationManager
{
    private readonly Dictionary<string, ModuleConfiguration> _configurations = new();

    /// <summary>
    /// 添加或更新模块配置
    /// </summary>
    /// <param name="configuration">模块配置</param>
    public void AddOrUpdateConfiguration(ModuleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(configuration.ModuleName);

        _configurations[configuration.ModuleName] = configuration;
    }

    /// <summary>
    /// 获取模块配置
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <returns>模块配置，如果不存在则返回默认配置</returns>
    public ModuleConfiguration GetConfiguration(string moduleName)
    {
        return _configurations.TryGetValue(moduleName, out var config) 
            ? config 
            : new ModuleConfiguration { ModuleName = moduleName };
    }

    /// <summary>
    /// 获取所有已启用的模块配置
    /// </summary>
    /// <returns>已启用的模块配置列表</returns>
    public IEnumerable<ModuleConfiguration> GetEnabledConfigurations()
    {
        return _configurations.Values.Where(c => c.IsEnabled);
    }

    /// <summary>
    /// 检查模块是否应该被注册（基于条件表达式）
    /// </summary>
    /// <param name="configuration">模块配置</param>
    /// <param name="context">评估上下文</param>
    /// <returns>是否应该注册</returns>
    public bool ShouldRegisterModule(ModuleConfiguration configuration, Dictionary<string, object>? context = null)
    {
        if (!configuration.IsEnabled)
            return false;

        // 如果没有条件表达式，默认注册
        if (string.IsNullOrEmpty(configuration.ConditionalExpression))
            return true;

        // 简单的条件表达式评估（可以后续扩展为更复杂的表达式引擎）
        return EvaluateSimpleCondition(configuration.ConditionalExpression, context ?? new());
    }

    /// <summary>
    /// 验证模块依赖关系
    /// </summary>
    /// <param name="moduleName">模块名称</param>
    /// <returns>验证结果和错误消息</returns>
    public (bool IsValid, string? ErrorMessage) ValidateDependencies(string moduleName)
    {
        if (!_configurations.TryGetValue(moduleName, out var config))
            return (true, null); // 没有配置视为有效

        foreach (var dependency in config.Dependencies)
        {
            if (!_configurations.ContainsKey(dependency))
                return (false, $"模块 {moduleName} 依赖的模块 {dependency} 未找到");

            var depConfig = _configurations[dependency];
            if (!depConfig.IsEnabled)
                return (false, $"模块 {moduleName} 依赖的模块 {dependency} 已禁用");
        }

        return (true, null);
    }

    /// <summary>
    /// 获取模块依赖排序后的配置列表
    /// </summary>
    /// <returns>按依赖顺序排列的配置列表</returns>
    public List<ModuleConfiguration> GetDependencyOrderedConfigurations()
    {
        var result = new List<ModuleConfiguration>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var config in GetEnabledConfigurations())
        {
            VisitModule(config, result, visited, visiting);
        }

        return result;
    }

    private bool EvaluateSimpleCondition(string condition, Dictionary<string, object> context)
    {
        // 简化的条件评估，支持基本的键值匹配
        // 格式: "key=value" 或 "key!=value"
        if (condition.Contains('='))
        {
            var parts = condition.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var expectedValue = parts[1].Trim();
                
                if (context.TryGetValue(key, out var actualValue))
                {
                    return string.Equals(actualValue?.ToString(), expectedValue, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        
        return true; // 默认为true，避免阻断注册
    }

    private void VisitModule(
        ModuleConfiguration config, 
        List<ModuleConfiguration> result, 
        HashSet<string> visited, 
        HashSet<string> visiting)
    {
        if (visited.Contains(config.ModuleName))
            return;

        if (visiting.Contains(config.ModuleName))
            throw new InvalidOperationException($"检测到循环依赖: {config.ModuleName}");

        visiting.Add(config.ModuleName);

        // 先处理依赖
        foreach (var dependencyName in config.Dependencies)
        {
            if (_configurations.TryGetValue(dependencyName, out var dependency) && dependency.IsEnabled)
            {
                VisitModule(dependency, result, visited, visiting);
            }
        }

        visiting.Remove(config.ModuleName);
        visited.Add(config.ModuleName);
        result.Add(config);
    }
}