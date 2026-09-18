using System.Reflection;
using FluentAssertions;
using Xunit;

namespace LYBT.Tests.Architecture;

/// <summary>
/// Shell 视图 → ViewModel 绑定契约守护（P1 回归防护）。
/// <para>背景：<c>HeaderControl/SideNavControl/FooterControl</c> 声明了
/// <c>prism:ViewModelLocator.AutoWireViewModel="True"</c>，但 Prism 约定名（<c>HeaderControlViewModel</c> 等）
/// 与实际 VM 类名（<c>HeaderViewModel</c> 等）不匹配且未显式登记 → AW 静默跳过赋值、控件继承宿主 DataContext，
/// 无异常无日志，绑定失效仅表现为 UI 空白。</para>
/// <para>本测试以 <c>ShellViewMappings.Mappings</c>（应用实际使用的同一映射表）为真相源，
/// 扫描 Shell XAML 中开启 AW 的视图，断言其 VM 可通过「约定」或「显式映射」解析——新增 AW 控件漏登记即失败。</para>
/// </summary>
public class ShellViewViewModelBindingTests
{
    private const string ShellAssemblyName = "LYBT.Desktop.Shell";

    [Fact]
    public void Shell_AutoWireViews_Must_Resolve_ViewModel_By_Convention_Or_Explicit_Mapping()
    {
        var shellAssembly = Assembly.Load(ShellAssemblyName);
        var explicitMappings = GetShellMappings();

        var viewsRoot = TryFindShellViewsDirectory();
        if (viewsRoot is null)
            return; // 非源码环境（无仓库根）时跳过文件扫描——映射表断言仍由下方事实覆盖

        var autoWireViews = FindAutoWireViews(viewsRoot);
        autoWireViews.Should().NotBeEmpty("Shell 至少应存在开启 AutoWireViewModel 的视图");

        var unresolved = new List<string>();
        foreach (var (viewTypeName, file) in autoWireViews)
        {
            var viewType = shellAssembly.GetType(viewTypeName, throwOnError: false);
            viewType.Should().NotBeNull($"{viewTypeName} 应存在于 {ShellAssemblyName}（文件 {file}）");

            var hasExplicitMapping = explicitMappings.ContainsKey(viewTypeName);
            var conventionType = ResolveConventionViewModel(viewType!);
            if (!hasExplicitMapping && (conventionType is null || conventionType.Assembly != shellAssembly))
                unresolved.Add($"{viewTypeName}（{file}）→ 约定名 {GetConventionName(viewType!)} 不存在且未在 ShellViewMappings 登记");
        }

        unresolved.Should().BeEmpty(
            "开启 AutoWireViewModel 的 Shell 视图必须在 ShellViewMappings.Mappings 显式登记（约定名不匹配时 AW 会静默失败）");
    }

    [Fact]
    public void Shell_Mappings_Must_Be_Concrete_ViewModels_In_Shell_Assembly()
    {
        var shellAssembly = Assembly.Load(ShellAssemblyName);
        var mappings = GetShellMappings();

        mappings.Should().NotBeEmpty();
        foreach (var (viewFullName, viewModelType) in mappings)
        {
            viewModelType.Namespace.Should().StartWith("LYBT.Desktop.Shell", $"{viewFullName} 的 VM 应位于 Shell 包内");
            viewModelType.IsAbstract.Should().BeFalse();
            viewModelType.Assembly.Should().BeSameAs(shellAssembly);
            shellAssembly.GetType(viewFullName, throwOnError: false)
                .Should().NotBeNull($"{viewFullName} 应存在于 {ShellAssemblyName}");
        }

        // 三控件的绑定契约：VM 必须暴露 XAML 顶层绑定的成员（防止改名只改一侧）
        AssertVmSurface("HeaderViewModel", "CurrentUserDisplayName", "CurrentUserRoleDisplay", "EditProfileCommand",
            "NavigateBackCommand");
        AssertVmSurface("SideNavViewModel", "IsSidebarExpanded", "SidebarWidth", "IsNavTextVisible",
            "IsDarkMode", "SelectedNavItem", "GroupedNavigationItems", "LogoutCommand");
        AssertVmSurface("FooterViewModel", "ApiStatusText", "ApiStatusIcon", "ApiStatusColor",
            "ConnectionModeDisplay", "CurrentTimeDisplay");
    }

    private static void AssertVmSurface(string vmTypeName, params string[] memberNames)
    {
        var type = Assembly.Load(ShellAssemblyName).GetType($"LYBT.Desktop.Shell.ViewModels.{vmTypeName}", throwOnError: false);
        type.Should().NotBeNull($"{vmTypeName} 应存在");
        foreach (var member in memberNames)
        {
            type!.GetProperty(member, BindingFlags.Public | BindingFlags.Instance)
                .Should().NotBeNull($"{vmTypeName}.{member} 是 XAML 绑定契约的一部分，改名需同步 XAML");
        }
    }

    /// <summary>读取应用实际使用的映射表（ShellViewMappings.Mappings）</summary>
    private static Dictionary<string, Type> GetShellMappings()
    {
        var mappingsType = Assembly.Load(ShellAssemblyName).GetType("LYBT.Desktop.Shell.ShellViewMappings", throwOnError: false);
        mappingsType.Should().NotBeNull("ShellViewMappings 应存在（App.ConfigureViewModelLocator 的映射真相源）");

        var result = new Dictionary<string, Type>(StringComparer.Ordinal);
        var mappings = (System.Collections.IEnumerable)mappingsType!
            .GetProperty("Mappings", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;
        foreach (var item in mappings)
        {
            var itemType = item.GetType();
            var view = (Type)itemType.GetField("Item1")!.GetValue(item)!;
            var viewModel = (Type)itemType.GetField("Item2")!.GetValue(item)!;
            result[view.ToString()] = viewModel;
        }

        return result;
    }

    private static Type? ResolveConventionViewModel(Type viewType)
    {
        var name = GetConventionName(viewType);
        return viewType.Assembly.GetType(name, throwOnError: false);
    }

    /// <summary>复刻 Prism 8.1.97 约定：.Views.→.ViewModels. +（以 View 结尾则 +Model，否则 +ViewModel）</summary>
    private static string GetConventionName(Type viewType)
    {
        var viewName = viewType.FullName!.Replace(".Views.", ".ViewModels.");
        var suffix = viewName.EndsWith("View", StringComparison.Ordinal) ? "Model" : "ViewModel";
        return viewName + suffix;
    }

    /// <summary>扫描 Shell XAML，返回 (视图类型全名, 相对路径) 列表（仅 AutoWireViewModel="True"）</summary>
    private static List<(string ViewTypeName, string File)> FindAutoWireViews(string viewsRoot)
    {
        var result = new List<(string, string)>();
        foreach (var file in Directory.EnumerateFiles(viewsRoot, "*.xaml", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("AutoWireViewModel=\"True\"", StringComparison.Ordinal))
                continue;

            var match = System.Text.RegularExpressions.Regex.Match(text, "x:Class=\"([^\"]+)\"");
            if (!match.Success)
                continue;

            result.Add((match.Groups[1].Value, Path.GetRelativePath(viewsRoot, file)));
        }

        return result;
    }

    /// <summary>从测试程序集位置向上查找仓库根（含 LYBTZYZS.sln）→ Shell/Views 与 Shell/Controls</summary>
    private static string? TryFindShellViewsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Client", "Desktop", "Shell");
            if (File.Exists(Path.Combine(dir.FullName, "LYBTZYZS.sln")) && Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }
}
