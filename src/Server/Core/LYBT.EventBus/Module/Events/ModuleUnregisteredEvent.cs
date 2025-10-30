using LYBT.Core.EventBus.Events;

namespace LYBT.EventBus.Module.Events;

/// <summary>
/// 模块注销事件
/// 当模块从系统中注销时发布
/// </summary>
public class ModuleUnregisteredEvent : IntegrationEventBase
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
    /// 模块版本
    /// </summary>
    public new string Version { get; }

    /// <summary>
    /// 注销原因
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// 注销时间
    /// </summary>
    public DateTime UnregistrationTime { get; }

    /// <summary>
    /// 是否为强制注销
    /// </summary>
    public bool IsForced { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleDescriptor">模块描述符</param>
    /// <param name="reason">注销原因</param>
    /// <param name="isForced">是否为强制注销</param>
    /// <param name="source">事件来源</param>
    public ModuleUnregisteredEvent(
        ModuleDescriptor moduleDescriptor,
        string? reason = null,
        bool isForced = false,
        string source = "ModuleManager")
        : base(source)
    {
        if (moduleDescriptor == null)
            throw new ArgumentNullException(nameof(moduleDescriptor));

        ModuleId = moduleDescriptor.Id;
        ModuleName = moduleDescriptor.Name;
        Version = moduleDescriptor.Version.ToString();
        Reason = reason;
        IsForced = isForced;
        UnregistrationTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取事件描述
    /// </summary>
    /// <returns>事件描述</returns>
    public override string GetDescription()
    {
        var description = $"模块 '{ModuleName}' (ID: {ModuleId}) v{Version} 已从系统注销";

        if (!string.IsNullOrWhiteSpace(Reason))
        {
            description += $", 原因: {Reason}";
        }

        if (IsForced)
        {
            description += " (强制注销)";
        }

        return description;
    }
}
