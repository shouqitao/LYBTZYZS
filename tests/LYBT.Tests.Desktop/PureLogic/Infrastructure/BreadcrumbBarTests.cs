using System.Windows.Input;
using FluentAssertions;
using LYBT.Desktop.Infrastructure.Controls;
using LYBT.Tests.Desktop.Infrastructure;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure;

/// <summary>
/// Phase 2.1: BreadcrumbBar tests
/// Tests for breadcrumb navigation and parsing
/// </summary>
public class BreadcrumbBarTests : UserJourneyTestBase
{
    public BreadcrumbBarTests(UserJourneyFixture fixture) : base(fixture)
    {
        // Ensure WPF environment is initialized
        WpfTestHelper.InitializeWpf();
    }

    private BreadcrumbBar CreateSut() => new();

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var sut = CreateSut();

        sut.NavigationPath.Should().BeEmpty();
        sut.Breadcrumbs.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesBreadcrumbsCollection()
    {
        var sut = CreateSut();

        sut.Breadcrumbs.Should().NotBeNull();
    }

    [Fact]
    public void NavigationPath_WhenEmpty_DoesNotCreateBreadcrumbs()
    {
        var sut = CreateSut();
        sut.NavigationPath = "";

        sut.Breadcrumbs.Should().BeEmpty();
    }

    [Fact]
    public void NavigationPath_WhenWhitespace_DoesNotCreateBreadcrumbs()
    {
        var sut = CreateSut();
        sut.NavigationPath = "   ";

        sut.Breadcrumbs.Should().BeEmpty();
    }

    [Fact]
    public void NavigationPath_SingleItem_CreatesOneBreadcrumb()
    {
        var sut = CreateSut();
        sut.NavigationPath = "患者选择";

        sut.Breadcrumbs.Should().HaveCount(1);
        sut.Breadcrumbs[0].Label.Should().Be("患者选择");
        sut.Breadcrumbs[0].Level.Should().Be(1);
        sut.Breadcrumbs[0].IsCurrent.Should().BeTrue();
        sut.Breadcrumbs[0].IsLast.Should().BeTrue();
    }

    [Fact]
    public void NavigationPath_TwoItems_SeparatedByGreaterThan()
    {
        var sut = CreateSut();
        sut.NavigationPath = "患者选择 > 临床工作台";

        sut.Breadcrumbs.Should().HaveCount(2);

        sut.Breadcrumbs[0].Label.Should().Be("患者选择");
        sut.Breadcrumbs[0].Level.Should().Be(1);
        sut.Breadcrumbs[0].IsCurrent.Should().BeFalse();
        sut.Breadcrumbs[0].IsLast.Should().BeFalse();

        sut.Breadcrumbs[1].Label.Should().Be("临床工作台");
        sut.Breadcrumbs[1].Level.Should().Be(2);
        sut.Breadcrumbs[1].IsCurrent.Should().BeTrue();
        sut.Breadcrumbs[1].IsLast.Should().BeTrue();
    }

    [Fact]
    public void NavigationPath_ThreeItems_CreatesThreeBreadcrumbs()
    {
        var sut = CreateSut();
        sut.NavigationPath = "患者选择 > 临床工作台 > 医案编辑";

        sut.Breadcrumbs.Should().HaveCount(3);

        sut.Breadcrumbs[0].Label.Should().Be("患者选择");
        sut.Breadcrumbs[0].IsCurrent.Should().BeFalse();
        sut.Breadcrumbs[0].IsLast.Should().BeFalse();

        sut.Breadcrumbs[1].Label.Should().Be("临床工作台");
        sut.Breadcrumbs[1].IsCurrent.Should().BeFalse();
        sut.Breadcrumbs[1].IsLast.Should().BeFalse();

        sut.Breadcrumbs[2].Label.Should().Be("医案编辑");
        sut.Breadcrumbs[2].IsCurrent.Should().BeTrue();
        sut.Breadcrumbs[2].IsLast.Should().BeTrue();
    }

    [Fact]
    public void NavigationPath_TrimsWhitespaceFromParts()
    {
        var sut = CreateSut();
        sut.NavigationPath = "  患者选择  >  临床工作台  >  医案编辑  ";

        sut.Breadcrumbs.Should().HaveCount(3);
        sut.Breadcrumbs[0].Label.Should().Be("患者选择");
        sut.Breadcrumbs[1].Label.Should().Be("临床工作台");
        sut.Breadcrumbs[2].Label.Should().Be("医案编辑");
    }

    [Fact]
    public void NavigationPath_HandlesMultipleSeparators()
    {
        var sut = CreateSut();
        sut.NavigationPath = "患者 > 临床 > 医案 > 处方 > 完成";

        sut.Breadcrumbs.Should().HaveCount(5);
    }

    [Fact]
    public void NavigateCommand_CanBeSet()
    {
        var sut = CreateSut();
        var command = Substitute.For<ICommand>();

        sut.NavigateCommand = command;

        sut.NavigateCommand.Should().Be(command);
    }

    [Fact]
    public void NavigationPath_WhenChanged_UpdatesBreadcrumbs()
    {
        var sut = CreateSut();
        sut.NavigationPath = "患者选择";

        sut.Breadcrumbs.Should().HaveCount(1);

        sut.NavigationPath = "患者选择 > 临床工作台";

        sut.Breadcrumbs.Should().HaveCount(2);
    }

    [Fact]
    public void BreadcrumbItems_HaveCorrectLevels()
    {
        var sut = CreateSut();
        sut.NavigationPath = "A > B > C > D";

        sut.Breadcrumbs[0].Level.Should().Be(1);
        sut.Breadcrumbs[1].Level.Should().Be(2);
        sut.Breadcrumbs[2].Level.Should().Be(3);
        sut.Breadcrumbs[3].Level.Should().Be(4);
    }

    [Fact]
    public void BreadcrumbItems_NavigateCommand_IsSetFromParent()
    {
        var sut = CreateSut();
        var command = Substitute.For<ICommand>();
        sut.NavigateCommand = command;
        sut.NavigationPath = "A > B > C";

        foreach (var breadcrumb in sut.Breadcrumbs)
        {
            breadcrumb.NavigateCommand.Should().Be(command);
        }
    }

    [Fact]
    public void BreadcrumbItem_Model_HasAllProperties()
    {
        var item = new BreadcrumbItem
        {
            Label = "测试",
            Level = 1,
            IsCurrent = true,
            IsLast = true,
            NavigateCommand = null
        };

        item.Label.Should().Be("测试");
        item.Level.Should().Be(1);
        item.IsCurrent.Should().BeTrue();
        item.IsLast.Should().BeTrue();
        item.NavigateCommand.Should().BeNull();
    }

    [Fact]
    public void NavigationPath_EmptyString_ClearsBreadcrumbs()
    {
        var sut = CreateSut();
        sut.NavigationPath = "A > B > C";

        sut.Breadcrumbs.Should().HaveCount(3);

        sut.NavigationPath = "";

        sut.Breadcrumbs.Should().BeEmpty();
    }
}
