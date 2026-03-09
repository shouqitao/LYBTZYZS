using FluentAssertions;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Controls;
using LYBT.Tests.Desktop.Infrastructure;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure.Controls;

/// <summary>
/// GlobalStatusBar 控件 DependencyProperty 测试
/// Sprint 5 - US-SHELL-007 (CODE-21): 状态栏用户名/版本号
/// </summary>
[Trait("Category", "WPF")]
public class GlobalStatusBarTests
{
    static GlobalStatusBarTests()
    {
        WpfTestHelper.InitializeWpf();
    }

    #region CurrentUserName DP 测试

    [StaFact]
    public void CurrentUserName_DefaultValue_IsEmptyString()
    {
        // Arrange & Act
        var bar = new GlobalStatusBar();

        // Assert
        bar.CurrentUserName.Should().Be(string.Empty);
    }

    [StaFact]
    public void CurrentUserName_SetValue_ReflectsCorrectly()
    {
        // Arrange
        var bar = new GlobalStatusBar();

        // Act
        bar.CurrentUserName = "张医生";

        // Assert
        bar.CurrentUserName.Should().Be("张医生");
    }

    [StaFact]
    public void CurrentUserName_SetNull_ReturnsEmptyString()
    {
        // Arrange
        var bar = new GlobalStatusBar();

        // Act
        bar.CurrentUserName = null!;

        // Assert
        bar.CurrentUserName.Should().BeEmpty();
    }

    #endregion

    #region AppVersion DP 测试

    [StaFact]
    public void AppVersion_DefaultValue_IsSystemConstantsVersion()
    {
        // Arrange & Act
        var bar = new GlobalStatusBar();

        // Assert
        bar.AppVersion.Should().Be(SystemConstants.ApplicationVersion);
    }

    [StaFact]
    public void AppVersion_SetValue_ReflectsCorrectly()
    {
        // Arrange
        var bar = new GlobalStatusBar();

        // Act
        bar.AppVersion = "3.0.0";

        // Assert
        bar.AppVersion.Should().Be("3.0.0");
    }

    #endregion
}
