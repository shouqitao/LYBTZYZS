namespace LYBT.Core.EventBus.Module;

/// <summary>
/// 模块描述符
/// 包含模块的元数据信息
/// </summary>
public class ModuleDescriptor
{
    /// <summary>
    /// 模块唯一标识符
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 模块显示名称
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 模块描述
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 模块版本
    /// </summary>
    public Version Version { get; init; } = new Version(1, 0, 0);

    /// <summary>
    /// 模块作者
    /// </summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>
    /// 模块类别
    /// </summary>
    public ModuleCategory Category { get; init; } = ModuleCategory.Business;

    /// <summary>
    /// 模块优先级
    /// 数值越小优先级越高，用于控制模块的加载和启动顺序
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// 模块依赖项
    /// 指定此模块依赖的其他模块ID列表
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 可选依赖项
    /// 指定此模块的可选依赖模块ID列表
    /// </summary>
    public IReadOnlyList<string> OptionalDependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 模块标签
    /// 用于分类和搜索模块
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 是否为核心模块
    /// 核心模块不能被禁用
    /// </summary>
    public bool IsCoreModule { get; init; } = false;

    /// <summary>
    /// 是否默认启用
    /// </summary>
    public bool IsEnabledByDefault { get; init; } = true;

    /// <summary>
    /// 最小支持的框架版本
    /// </summary>
    public Version? MinimumFrameworkVersion { get; init; }

    /// <summary>
    /// 模块配置架构
    /// 用于验证模块配置的JSON架构
    /// </summary>
    public string? ConfigurationSchema { get; init; }

    /// <summary>
    /// 创建模块描述符
    /// </summary>
    /// <param name="id">模块ID</param>
    /// <param name="name">模块名称</param>
    /// <param name="version">模块版本</param>
    /// <returns>模块描述符</returns>
    public static ModuleDescriptor Create(string id, string name, Version? version = null)
    {
        return new ModuleDescriptor
        {
            Id = id ?? throw new ArgumentNullException(nameof(id)),
            Name = name ?? throw new ArgumentNullException(nameof(name)),
            DisplayName = name,
            Version = version ?? new Version(1, 0, 0)
        };
    }

    /// <summary>
    /// 验证模块描述符
    /// </summary>
    /// <returns>验证结果</returns>
    public ModuleValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("模块ID不能为空");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("模块名称不能为空");

        if (Priority < 0)
            errors.Add("模块优先级不能为负数");

        // 检查循环依赖（简单检查）
        if (Dependencies.Contains(Id))
            errors.Add("模块不能依赖自身");

        if (Dependencies.Any(d => string.IsNullOrWhiteSpace(d)))
            errors.Add("模块依赖项不能包含空值");

        return new ModuleValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 获取模块的完整名称
    /// </summary>
    /// <returns>完整名称</returns>
    public string GetFullName()
    {
        return $"{Name} v{Version}";
    }

    /// <summary>
    /// 检查是否依赖指定模块
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="includeOptional">是否包含可选依赖</param>
    /// <returns>是否依赖</returns>
    public bool DependsOn(string moduleId, bool includeOptional = false)
    {
        if (Dependencies.Contains(moduleId))
            return true;

        if (includeOptional && OptionalDependencies.Contains(moduleId))
            return true;

        return false;
    }

    /// <summary>
    /// 检查是否兼容指定的框架版本
    /// </summary>
    /// <param name="frameworkVersion">框架版本</param>
    /// <returns>是否兼容</returns>
    public bool IsCompatibleWith(Version frameworkVersion)
    {
        if (MinimumFrameworkVersion == null)
            return true;

        return frameworkVersion >= MinimumFrameworkVersion;
    }
}
