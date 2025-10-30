using LYBT.Core.EventBus.Events;

namespace LYBT.Core.EventBus.Module.Events;

/// <summary>
/// 模块注册事件
/// 当模块成功注册到系统时发布
/// </summary>
public class ModuleRegisteredEvent : IntegrationEventBase
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
    /// 模块类别
    /// </summary>
    public ModuleCategory Category { get; }

    /// <summary>
    /// 模块描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 是否为核心模块
    /// </summary>
    public bool IsCoreModule { get; }

    /// <summary>
    /// 模块依赖项
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime RegistrationTime { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="moduleDescriptor">模块描述符</param>
    /// <param name="source">事件来源</param>
    public ModuleRegisteredEvent(ModuleDescriptor moduleDescriptor, string source = "ModuleManager")
        : base(source)
    {
        if (moduleDescriptor == null)
            throw new ArgumentNullException(nameof(moduleDescriptor));

        ModuleId = moduleDescriptor.Id;
        ModuleName = moduleDescriptor.Name;
        Version = moduleDescriptor.Version.ToString();
        Category = moduleDescriptor.Category;
        Description = moduleDescriptor.Description;
        IsCoreModule = moduleDescriptor.IsCoreModule;
        Dependencies = moduleDescriptor.Dependencies;
        RegistrationTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取事件描述
    /// </summary>
    /// <returns>事件描述</returns>
    public override string GetDescription()
    {
        return $"模块 '{ModuleName}' (ID: {ModuleId}) v{Version} 已注册到系统";
    }
}
