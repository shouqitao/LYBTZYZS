using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace LYBT.Tests.Desktop._Infrastructure;

#region 测试分类特性

/// <summary>
/// 标记测试为单元测试 (Unit Test)
/// 特点：无外部依赖，执行速度快 (< 100ms)，确定性高
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class UnitTestAttribute : Attribute, ITraitAttribute
{
    public string Name => "Category";
    public string Value => "Unit";
}

/// <summary>
/// 标记测试为集成测试 (Integration Test)
/// 特点：可能涉及数据库、文件系统、网络等外部依赖
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class IntegrationTestAttribute : Attribute, ITraitAttribute
{
    public string Name => "Category";
    public string Value => "Integration";
}

/// <summary>
/// 标记测试为端到端测试 (E2E Test)
/// 特点：完整用户流程，涉及多个子系统
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class E2ETestAttribute : Attribute, ITraitAttribute
{
    public string Name => "Category";
    public string Value => "E2E";
}

/// <summary>
/// 标记测试为纯逻辑测试 (Pure Logic)
/// 特点：完全不依赖外部服务，适合 TDD 快速反馈
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class PureLogicAttribute : Attribute, ITraitAttribute
{
    public string Name => "Category";
    public string Value => "PureLogic";
}

/// <summary>
/// 标记测试为用户旅程测试 (User Journey)
/// 特点：验证完整用户场景，高业务价值
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class UserJourneyAttribute : Attribute, ITraitAttribute
{
    public string Name => "Category";
    public string Value => "UserJourney";
}

#endregion

#region 性能分类特性

/// <summary>
/// 标记测试为慢速测试 (执行时间 > 1秒)
/// 可用于 CI 中选择性排除
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class SlowTestAttribute : Attribute, ITraitAttribute
{
    public string Name => "Speed";
    public string Value => "Slow";
}

/// <summary>
/// 标记测试需要 UI 线程 (WPF/WinForms)
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class UIThreadAttribute : Attribute, ITraitAttribute
{
    public string Name => "Threading";
    public string Value => "UIThread";
}

#endregion

#region 平台/环境特性

/// <summary>
/// 标记测试需要数据库连接
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class DatabaseAttribute : Attribute, ITraitAttribute
{
    public string Name => "Dependency";
    public string Value => "Database";
}

/// <summary>
/// 标记测试需要 WebAPI 服务
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class WebApiDependencyAttribute : Attribute, ITraitAttribute
{
    public string Name => "Dependency";
    public string Value => "WebApi";
}

/// <summary>
/// 标记测试仅在 Windows 平台运行
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class WindowsOnlyAttribute : Attribute, ITraitAttribute
{
    public string Name => "Platform";
    public string Value => "Windows";
}

#endregion

#region 发现器实现 (XUnit 需要)

/// <summary>
/// 单元测试特性发现器
/// </summary>
public class UnitTestAttributeDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>("Category", "Unit");
    }
}

/// <summary>
/// 集成测试特性发现器
/// </summary>
public class IntegrationTestAttributeDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>("Category", "Integration");
    }
}

/// <summary>
/// E2E测试特性发现器
/// </summary>
public class E2ETestAttributeDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>("Category", "E2E");
    }
}

#endregion

/// <summary>
/// Trait 名称常量 - 用于命令行筛选
/// 使用示例: dotnet test --filter "Category=Unit"
///           dotnet test --filter "Speed!=Slow"
///           dotnet test --filter "Dependency!=Database"
/// </summary>
public static class TestTraits
{
    public static class Categories
    {
        public const string Unit = "Unit";
        public const string Integration = "Integration";
        public const string E2E = "E2E";
        public const string PureLogic = "PureLogic";
        public const string UserJourney = "UserJourney";
    }

    public static class Speed
    {
        public const string Slow = "Slow";
        public const string Fast = "Fast";
    }

    public static class Dependencies
    {
        public const string Database = "Database";
        public const string WebApi = "WebApi";
        public const string FileSystem = "FileSystem";
        public const string Network = "Network";
    }

    public static class Platforms
    {
        public const string Windows = "Windows";
        public const string Linux = "Linux";
        public const string MacOS = "MacOS";
    }
}
