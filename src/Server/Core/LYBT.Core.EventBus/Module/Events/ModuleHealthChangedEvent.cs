using LYBT.Core.EventBus.Events;

namespace LYBT.Core.EventBus.Module.Events;

/// <summary>
/// 模块健康状态变更事件
/// 当模块健康状态发生变化时发布
/// </summary>
public class ModuleHealthChangedEvent : IntegrationEventBase
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
    /// 旧健康状态
    /// </summary>
    public HealthStatus OldStatus { get; }

    /// <summary>
    /// 新健康状态
    /// </summary>
    public HealthStatus NewStatus { get; }

    /// <summary>
    /// 健康检查描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 检查时间
    /// </summary>
    public DateTime CheckTime { get; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; }

    /// <summary>
    /// 额外数据
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// 异常信息（如果有）
    /// </summary>
    public string? ExceptionMessage { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="oldStatus">旧健康状态</param>
    /// <param name="newHealthStatus">新健康状态</param>
    /// <param name="source">事件来源</param>
    public ModuleHealthChangedEvent(
        string moduleId,
        string moduleName,
        HealthStatus oldStatus,
        ModuleHealthStatus newHealthStatus,
        string source = "ModuleManager")
        : base(source)
    {
        ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
        ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        OldStatus = oldStatus;
        NewStatus = newHealthStatus?.Status ?? HealthStatus.Unknown;
        Description = newHealthStatus?.Description ?? "未知状态";
        CheckTime = newHealthStatus?.CheckTime ?? DateTime.UtcNow;
        ResponseTimeMs = newHealthStatus?.ResponseTimeMs ?? 0;
        Data = newHealthStatus?.Data ?? new Dictionary<string, object>();
        ExceptionMessage = newHealthStatus?.Exception?.Message;
    }

    /// <summary>
    /// 检查是否为关键健康变更
    /// </summary>
    /// <returns>是否为关键变更</returns>
    public bool IsCriticalChange()
    {
        // 从健康状态变为不健康状态
        if (OldStatus == HealthStatus.Healthy && NewStatus == HealthStatus.Unhealthy)
            return true;

        // 变为不健康状态
        if (NewStatus == HealthStatus.Unhealthy)
            return true;

        return false;
    }

    /// <summary>
    /// 检查是否为恢复性变更
    /// </summary>
    /// <returns>是否为恢复性变更</returns>
    public bool IsRecoveryChange()
    {
        // 从不健康状态恢复到健康状态
        if (OldStatus == HealthStatus.Unhealthy && NewStatus == HealthStatus.Healthy)
            return true;

        // 从降级状态恢复到健康状态
        if (OldStatus == HealthStatus.Degraded && NewStatus == HealthStatus.Healthy)
            return true;

        return false;
    }

    /// <summary>
    /// 检查是否为降级变更
    /// </summary>
    /// <returns>是否为降级变更</returns>
    public bool IsDegradationChange()
    {
        // 从健康状态变为降级状态
        if (OldStatus == HealthStatus.Healthy && NewStatus == HealthStatus.Degraded)
            return true;

        return false;
    }

    /// <summary>
    /// 获取健康变更严重程度
    /// </summary>
    /// <returns>严重程度</returns>
    public HealthChangeSeverity GetSeverity()
    {
        if (IsCriticalChange())
            return HealthChangeSeverity.Critical;

        if (IsDegradationChange())
            return HealthChangeSeverity.Warning;

        if (IsRecoveryChange())
            return HealthChangeSeverity.Info;

        return HealthChangeSeverity.Normal;
    }

    /// <summary>
    /// 获取事件描述
    /// </summary>
    /// <returns>事件描述</returns>
    public override string GetDescription()
    {
        var description = $"模块 '{ModuleName}' (ID: {ModuleId}) 健康状态从 '{OldStatus.GetDisplayName()}' 变更为 '{NewStatus.GetDisplayName()}'";

        if (!string.IsNullOrWhiteSpace(Description) && Description != NewStatus.GetDefaultDescription())
        {
            description += $", 详情: {Description}";
        }

        if (ResponseTimeMs > 0)
        {
            description += $", 响应时间: {ResponseTimeMs}ms";
        }

        return description;
    }

    /// <summary>
    /// 获取健康变更摘要
    /// </summary>
    /// <returns>健康变更摘要</returns>
    public string GetHealthSummary()
    {
        var summary = $"{ModuleName}: {OldStatus.GetDisplayName()} → {NewStatus.GetDisplayName()}";

        var severity = GetSeverity();
        summary += severity switch
        {
            HealthChangeSeverity.Critical => " [严重]",
            HealthChangeSeverity.Warning => " [警告]",
            HealthChangeSeverity.Info => " [恢复]",
            _ => ""
        };

        return summary;
    }
}

/// <summary>
/// 健康变更严重程度
/// </summary>
public enum HealthChangeSeverity
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
