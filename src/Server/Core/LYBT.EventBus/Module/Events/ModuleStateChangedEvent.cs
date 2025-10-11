using LYBT.EventBus.Events;

namespace LYBT.EventBus.Module.Events;

/// <summary>
/// 模块状态变更事件
/// 当模块状态发生变化时发布
/// </summary>
public class ModuleStateChangedEvent : IntegrationEventBase
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
    /// 旧状态
    /// </summary>
    public ModuleState OldState { get; }

    /// <summary>
    /// 新状态
    /// </summary>
    public ModuleState NewState { get; }

    /// <summary>
    /// 状态变更时间
    /// </summary>
    public DateTime StateChangeTime { get; }

    /// <summary>
    /// 状态变更原因
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// 额外数据
    /// </summary>
    public IReadOnlyDictionary<string, object> AdditionalData { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleName">模块名称</param>
    /// <param name="oldState">旧状态</param>
    /// <param name="newState">新状态</param>
    /// <param name="reason">变更原因</param>
    /// <param name="additionalData">额外数据</param>
    /// <param name="source">事件来源</param>
    public ModuleStateChangedEvent(
        string moduleId,
        string moduleName,
        ModuleState oldState,
        ModuleState newState,
        string? reason = null,
        IReadOnlyDictionary<string, object>? additionalData = null,
        string source = "ModuleManager")
        : base(source)
    {
        ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
        ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        OldState = oldState;
        NewState = newState;
        Reason = reason;
        AdditionalData = additionalData ?? new Dictionary<string, object>();
        StateChangeTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 从模块状态变更事件参数创建
    /// </summary>
    /// <param name="args">状态变更事件参数</param>
    /// <param name="reason">变更原因</param>
    /// <param name="additionalData">额外数据</param>
    /// <param name="source">事件来源</param>
    /// <returns>模块状态变更事件</returns>
    public static ModuleStateChangedEvent FromEventArgs(
        ModuleStateChangedEventArgs args,
        string? reason = null,
        IReadOnlyDictionary<string, object>? additionalData = null,
        string source = "ModuleManager")
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));

        return new ModuleStateChangedEvent(
            args.Module.Descriptor.Id,
            args.Module.Descriptor.Name,
            args.OldState,
            args.NewState,
            reason,
            additionalData,
            source);
    }

    /// <summary>
    /// 检查是否为关键状态变更
    /// </summary>
    /// <returns>是否为关键变更</returns>
    public bool IsCriticalChange()
    {
        // 进入错误状态被视为关键变更
        if (NewState == ModuleState.Error)
            return true;

        // 从运行状态变为非运行状态被视为关键变更
        if (OldState == ModuleState.Running && NewState != ModuleState.Running)
            return true;

        // 核心模块的状态变更被视为关键变更
        return false; // 这里需要额外的上下文信息来判断是否为核心模块
    }

    /// <summary>
    /// 检查是否为正向变更
    /// </summary>
    /// <returns>是否为正向变更</returns>
    public bool IsPositiveChange()
    {
        return NewState switch
        {
            ModuleState.Running => OldState != ModuleState.Running,
            ModuleState.Initialized => OldState is ModuleState.Uninitialized or ModuleState.Error,
            _ => false
        };
    }

    /// <summary>
    /// 检查是否为负向变更
    /// </summary>
    /// <returns>是否为负向变更</returns>
    public bool IsNegativeChange()
    {
        return NewState switch
        {
            ModuleState.Error => true,
            ModuleState.Stopped => OldState == ModuleState.Running,
            ModuleState.Disabled => OldState is ModuleState.Running or ModuleState.Initialized,
            _ => false
        };
    }

    /// <summary>
    /// 获取事件描述
    /// </summary>
    /// <returns>事件描述</returns>
    public override string GetDescription()
    {
        var description = $"模块 '{ModuleName}' (ID: {ModuleId}) 状态从 '{OldState.GetDisplayName()}' 变更为 '{NewState.GetDisplayName()}'";

        if (!string.IsNullOrWhiteSpace(Reason))
        {
            description += $", 原因: {Reason}";
        }

        return description;
    }

    /// <summary>
    /// 获取状态变更摘要
    /// </summary>
    /// <returns>状态变更摘要</returns>
    public string GetChangeSummary()
    {
        var summary = $"{ModuleName}: {OldState.GetDisplayName()} → {NewState.GetDisplayName()}";

        if (IsCriticalChange())
        {
            summary += " [关键]";
        }
        else if (IsPositiveChange())
        {
            summary += " [正向]";
        }
        else if (IsNegativeChange())
        {
            summary += " [负向]";
        }

        return summary;
    }
}
