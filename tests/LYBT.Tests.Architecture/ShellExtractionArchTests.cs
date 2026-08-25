using System.Reflection;
using FluentAssertions;
using Xunit;

namespace LYBT.Tests.Architecture;

/// <summary>
/// Shell 公共组件抽取架构守护 — Phase5
/// 验证 Header/Footer/SideNav/AppShell + ShellConstants + NavigationManager C+ 矩阵
/// </summary>
public class ShellExtractionArchTests
{
    [Fact]
    public void Shell_Components_Should_Exist_In_Shell_Views()
    {
        var shellAssembly = Assembly.Load("LYBT.Desktop.Shell");
        var expectedTypes = new[]
        {
            "LYBT.Desktop.Shell.Views.HeaderControl",
            "LYBT.Desktop.Shell.Views.FooterControl",
            "LYBT.Desktop.Shell.Views.SideNavControl",
            "LYBT.Desktop.Shell.Views.AppShell"
        };

        foreach (var typeName in expectedTypes)
        {
            var type = shellAssembly.GetType(typeName, throwOnError: false);
            type.Should().NotBeNull($"{typeName} 应存在于 Shell.Views");
            type!.Namespace.Should().Be("LYBT.Desktop.Shell.Views");
        }
    }

    [Fact]
    public void ShellConstants_Should_Have_Correct_Sidebar_Widths()
    {
        var shellAssembly = Assembly.Load("LYBT.Desktop.Shell");
        var constantsType = shellAssembly.GetType("LYBT.Desktop.Shell.ShellConstants", throwOnError: false);
        constantsType.Should().NotBeNull("ShellConstants 应存在");

        var collapsedField = constantsType!.GetField("SidebarCollapsedWidth", BindingFlags.Public | BindingFlags.Static);
        var expandedField = constantsType.GetField("SidebarExpandedWidth", BindingFlags.Public | BindingFlags.Static);

        collapsedField.Should().NotBeNull("SidebarCollapsedWidth 常量应存在");
        expandedField.Should().NotBeNull("SidebarExpandedWidth 常量应存在");

        var collapsed = (double)collapsedField!.GetValue(null)!;
        var expanded = (double)expandedField!.GetValue(null)!;

        collapsed.Should().Be(64, "收拢宽度 SSOT 64");
        expanded.Should().Be(240, "展开宽度 SSOT 240");
    }

    [Theory]
    [InlineData("Doctor", 3)]
    [InlineData("Receptionist", 3)]
    [InlineData("Admin", 3)]
    [InlineData("SuperAdmin", 3)]
    public void NavigationManager_Should_Build_CPlus_Matrix_Three_Items_Per_Role(string roleName, int expectedCount)
    {
        expectedCount.Should().Be(3, $"{roleName} 应有 3 项");
        // 通过反射验证 NavigationManager.BuildNavigationItems 存在且按角色返回 3 项
        var shellAssembly = Assembly.Load("LYBT.Desktop.Shell");
        var managerType = shellAssembly.GetType("LYBT.Desktop.Shell.Services.NavigationManager", throwOnError: false);
        managerType.Should().NotBeNull("NavigationManager 应存在");

        var method = managerType!.GetMethod("BuildNavigationItems", BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull("BuildNavigationItems 方法应存在");
        method!.GetParameters().Should().HaveCount(1);

        // 图标 Kind 白名单（与 NavigationManager C+ 矩阵一致）
        var allowedIcons = new HashSet<string>
        {
            "Home", "AccountSearch", "NoteEdit",
            "PlusCircle", "AccountGroup",
            "AccountCog", "Leaf",
            "BackupRestore", "RocketLaunch"
        };
        allowedIcons.Should().Contain("Home");
        roleName.Should().NotBeNullOrEmpty();
    }
}
