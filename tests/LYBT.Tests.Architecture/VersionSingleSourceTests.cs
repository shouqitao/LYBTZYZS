using System.Reflection;
using System.Text.RegularExpressions;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Primitives;

namespace LYBT.Tests.Architecture;

/// <summary>
/// 版本单源守卫（2026-09-24 确立，防复发）。
/// <para>标准 = <c>docs/00-governance/05-versioning-standard.md</c>：§1 单一版本源（代码/配置/测试禁止产品版本字面量）、
/// §4.1 机器门禁（<c>InformationalVersion</c> 前缀 == <c>VersionPrefix</c>）；历史 <c>1.0.x/1.1.0/2.0.0</c> 全部作废。
/// 本组守卫防止发布号重新散落回代码/资源：凡能从程序集元数据推导的，一律推导。</para>
/// </summary>
public class VersionSingleSourceTests
{
    private static string RepoRoot
    {
        get
        {
            // 以解决方案文件为锚（tests/ 下也有 Directory.Build.props，不能只按该文件名向上找）
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LYBTZYZS.sln")))
                dir = dir.Parent;

            dir.Should().NotBeNull("测试必须能从输出目录向上找到解决方案根（LYBTZYZS.sln）");
            return dir!.FullName;
        }
    }

    /// <summary>版本单源：Directory.Build.props 的 VersionPrefix。</summary>
    private static string VersionPrefix
    {
        get
        {
            var props = File.ReadAllText(Path.Combine(RepoRoot, "Directory.Build.props"));
            var match = Regex.Match(props, @"<VersionPrefix>([^<]+)</VersionPrefix>");
            match.Success.Should().BeTrue("Directory.Build.props 必须定义 VersionPrefix——它是版本单源");
            return match.Groups[1].Value.Trim();
        }
    }

    [Fact]
    public void AppVersion_IsDerivedFromAssemblyMetadata_AndFollowsTheVersionPrefixLine()
    {
        var prefix = VersionPrefix;
        var current = AppVersion.Current;

        current.Should().NotBe(AppVersion.Unknown, "程序集必须携带版本元数据（VersionPrefix 由 SDK 写入）");
        current.Should().MatchRegex(@"^\d+\.\d+\.\d+", "版本号必须是 SemVer 三段式");

        // 与单源同版本线（major.minor）且不低于它——与打包脚本的两条校验同源
        var currentValue = new Version(current);
        var prefixValue = new Version(prefix);
        currentValue.Major.Should().Be(prefixValue.Major, "版本线由 VersionPrefix 决定，升线必须先改单源");
        currentValue.Minor.Should().Be(prefixValue.Minor, "版本线由 VersionPrefix 决定，升线必须先改单源");
        currentValue.Should().BeGreaterThanOrEqualTo(prefixValue);
    }

    [Fact]
    public void AppVersion_AssemblyMetadata_IsInformationalVersionOfTheSingleSource()
    {
        var assembly = typeof(AppVersion).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        informational.Should().NotBeNullOrWhiteSpace("SDK 会把 VersionPrefix 写进 InformationalVersion");
        // 允许 CI/打包注入构建元数据后缀（0.0.1+<sha>），但版本号主体必须就是单源
        // 标准 §4.1：InformationalVersion 前缀（+ 构建元数据之前的部分）== VersionPrefix
        informational!.Split('+')[0].Should().Be(VersionPrefix);
    }

    [Fact]
    public void VersionBearingSurfaces_AreMetadataDerived_NotHardcoded()
    {
        SystemConstants.ApplicationVersion.Should().Be(AppVersion.Current,
            "Desktop 关于页/登录页/日志/导出包的版本必须来自程序集元数据");
        new AppInfoOptions().Version.Should().Be(AppVersion.Current,
            "服务端 App:Version 默认值必须来自程序集元数据（配置未显式覆盖时）");
    }

    [Fact]
    public void ShellResources_CarryNoHardcodedProductVersion()
    {
        var resx = Path.Combine(RepoRoot, "src", "Client", "Desktop", "Shell", "Resources", "Strings", "StringResources.resx");
        File.Exists(resx).Should().BeTrue();

        // 只取 <data> 下的 <value>（resx 头部的 resheader 含 reader 程序集版本 4.0.0.0，不是产品版本）
        var doc = System.Xml.Linq.XDocument.Load(resx);
        var values = doc.Root!
            .Elements("data")
            .Select(d => d.Element("value")?.Value ?? string.Empty)
            .Where(v => Regex.IsMatch(v, @"\d+\.\d+\.\d+"))
            .ToList();

        values.Should().BeEmpty("资源文案不得硬编码发布号——版本一律由 AppVersion 注入");
    }

    [Fact]
    public void ClinicConfig_CarriesNoHardcodedProductVersion()
    {
        var config = Path.Combine(RepoRoot, "src", "Server", "Services", "LYBT.WebAPI", "config", "clinic.config.json");
        File.Exists(config).Should().BeTrue();

        var hardcoded = Regex.Matches(File.ReadAllText(config), @"""Version""\s*:\s*""[^""]*""")
            .Select(m => m.Value)
            .ToList();

        hardcoded.Should().BeEmpty("clinic.config.json 不得携带产品版本号（无消费者，且会与单源漂移）");
    }
}
