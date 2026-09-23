using System.Reflection;

namespace LYBT.Shared.Models.Primitives;

/// <summary>
/// 应用版本（运行时单一来源）。
/// <para><b>版本单源 = <c>Directory.Build.props</c> 的 <c>&lt;VersionPrefix&gt;</c></b>：SDK 会把它写进程序集元数据
/// （<see cref="AssemblyInformationalVersionAttribute"/> = VersionPrefix[+后缀]，<c>AssemblyVersion</c> = VersionPrefix.0）。
/// 本类只**读取元数据**，不出现任何硬编码版本号——发布号散落各处必然与单源漂移。</para>
/// <para>版本策略见 <c>docs/06-operations/12-desktop-release.md §0</c>（当前阶段强制 0.0.x）。</para>
/// </summary>
public static class AppVersion
{
    /// <summary>无版本元数据时的占位值（表示「未取到版本」，绝不冒充真实发布号）。</summary>
    public const string Unknown = "0.0.0";

    /// <summary>当前应用版本：优先程序集 <c>InformationalVersion</c>，退化到 <c>AssemblyVersion</c> 三段。</summary>
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        // 读**本程序集**（LYBT.Shared.Models）的元数据，而不是 Assembly.GetEntryAssembly()：
        // 版本单源 Directory.Build.props 对全解决方案生效，各程序集版本必然一致；
        // 而入口程序集在测试宿主（testhost）下不是产品程序集，会读出测试宿主的版本。
        var assembly = typeof(AppVersion).Assembly;

        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            // 去掉构建元数据后缀（如 "0.0.1+<source-revision>"）——只保留版本号本身
            var plusIndex = informational.IndexOf('+');
            return plusIndex > 0 ? informational[..plusIndex] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? Unknown;
    }
}
