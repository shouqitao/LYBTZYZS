using LYBT.EventBus.Events;

namespace LYBT.EventBus.Module.Events;

/// <summary>
/// 模块依赖事件
/// 当模块依赖关系发生变化时发布
/// </summary>
public class ModuleDependencyEvent : IntegrationEventBase
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public string ModuleId { get; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// 依赖类型
    /// </summary>
    public new DependencyEventType EventType { get; }

    /// <summary>
    /// 依赖模块ID
    /// </summary>
    public string DependencyModuleId { get; }

    /// <summary>
    /// 依赖模块名称
    /// </summary>
    public string DependencyModuleName { get; }

    /// <summary>
    /// 是否为可选依赖
    /// </summary>
    public bool IsOptional { get; }

    /// <summary>
    /// 依赖检查结果
    /// </summary>
    public bool IsSatisfied { get; }

    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTime EventTime { get; }

    /// <summary>
    /// 额外信息
    /// </summary>
    public string? AdditionalInfo { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="eventType">依赖事件类型</param>
    /// <param name="dependencyModuleId">依赖模块ID</param>
    /// <param name="dependencyModuleName">依赖模块名称</param>
    /// <param name="isOptional">是否为可选依赖</param>
    /// <param name="isSatisfied">依赖是否满足</param>
    /// <param name="additionalInfo">额外信息</param>
    /// <param name="source">事件来源</param>
    public ModuleDependencyEvent(
        string moduleId,
        string moduleName,
        DependencyEventType eventType,
        string dependencyModuleId,
        string dependencyModuleName,
        bool isOptional = false,
        bool isSatisfied = true,
        string? additionalInfo = null,
        string source = "ModuleManager")
        : base(source)
    {
        ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
        ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        EventType = eventType;
        DependencyModuleId = dependencyModuleId ?? throw new ArgumentNullException(nameof(dependencyModuleId));
        DependencyModuleName = dependencyModuleName ?? throw new ArgumentNullException(nameof(dependencyModuleName));
        IsOptional = isOptional;
        IsSatisfied = isSatisfied;
        AdditionalInfo = additionalInfo;
        EventTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 检查是否为关键依赖事件
    /// </summary>
    /// <returns>是否为关键事件</returns>
    public bool IsCritical()
    {
        // 必需依赖未满足是关键事件
        if (!IsOptional && !IsSatisfied)
            return true;

        // 依赖模块不可用是关键事件
        if (EventType == DependencyEventType.DependencyUnavailable && !IsOptional)
            return true;

        // 循环依赖是关键事件
        if (EventType == DependencyEventType.CircularDependencyDetected)
            return true;

        return false;
    }

    /// <summary>
    /// 获取事件严重程度
    /// </summary>
    /// <returns>严重程度</returns>
    public DependencyEventSeverity GetSeverity()
    {
        if (IsCritical())
            return DependencyEventSeverity.Critical;

        return EventType switch
        {
            DependencyEventType.DependencyUnavailable when IsOptional => DependencyEventSeverity.Warning,
            DependencyEventType.DependencyVersionMismatch => DependencyEventSeverity.Warning,
            DependencyEventType.DependencyResolved => DependencyEventSeverity.Info,
            DependencyEventType.DependencyAvailable => DependencyEventSeverity.Info,
            _ => DependencyEventSeverity.Normal
        };
    }

    /// <summary>
    /// 获取事件描述
    /// </summary>
    /// <returns>事件描述</returns>
    public override string GetDescription()
    {
        var dependencyType = IsOptional ? "可选依赖" : "必需依赖";
        var satisfiedStatus = IsSatisfied ? "已满足" : "未满足";

        var description = EventType switch
        {
            DependencyEventType.DependencyResolved =>
                $"模块 '{ModuleName}' 的{dependencyType} '{DependencyModuleName}' 已解析",
            DependencyEventType.DependencyUnavailable =>
                $"模块 '{ModuleName}' 的{dependencyType} '{DependencyModuleName}' 不可用",
            DependencyEventType.DependencyAvailable =>
                $"模块 '{ModuleName}' 的{dependencyType} '{DependencyModuleName}' 现已可用",
            DependencyEventType.DependencyVersionMismatch =>
                $"模块 '{ModuleName}' 的{dependencyType} '{DependencyModuleName}' 版本不匹配",
            DependencyEventType.CircularDependencyDetected =>
                $"检测到模块 '{ModuleName}' 与 '{DependencyModuleName}' 之间的循环依赖",
            _ => $"模块 '{ModuleName}' 的依赖 '{DependencyModuleName}' 发生了变化"
        };

        if (!string.IsNullOrWhiteSpace(AdditionalInfo))
        {
            description += $", 详情: {AdditionalInfo}";
        }

        return description;
    }

    /// <summary>
    /// 获取依赖摘要
    /// </summary>
    /// <returns>依赖摘要</returns>
    public string GetDependencySummary()
    {
        var summary = $"{ModuleName} → {DependencyModuleName}";

        if (IsOptional)
        {
            summary += " [可选]";
        }

        var severity = GetSeverity();
        summary += severity switch
        {
            DependencyEventSeverity.Critical => " [严重]",
            DependencyEventSeverity.Warning => " [警告]",
            DependencyEventSeverity.Info => " [信息]",
            _ => ""
        };

        return summary;
    }
}

/// <summary>
/// 依赖事件类型
/// </summary>
public enum DependencyEventType
{
    /// <summary>
    /// 依赖已解析
    /// </summary>
    DependencyResolved = 0,

    /// <summary>
    /// 依赖不可用
    /// </summary>
    DependencyUnavailable = 1,

    /// <summary>
    /// 依赖现已可用
    /// </summary>
    DependencyAvailable = 2,

    /// <summary>
    /// 依赖版本不匹配
    /// </summary>
    DependencyVersionMismatch = 3,

    /// <summary>
    /// 检测到循环依赖
    /// </summary>
    CircularDependencyDetected = 4
}

/// <summary>
/// 依赖事件严重程度
/// </summary>
public enum DependencyEventSeverity
{
    /// <summary>
    /// 正常
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 信息
    /// </summary>
    Info = 1,

    /// <summary>
    /// 警告
    /// </summary>
    Warning = 2,

    /// <summary>
    /// 严重
    /// </summary>
    Critical = 3
}
