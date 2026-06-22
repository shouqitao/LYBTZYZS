using FluentAssertions;
using LYBT.Desktop.Controls.Models;

namespace LYBT.Tests.Desktop;

public class NavigationItemTests
{
    [Fact]
    public void New_Instance_DefaultGroup_IsBusiness()
    {
        var item = new NavigationItem();
        item.Group.Should().Be("业务");
    }

    [Theory]
    [InlineData("主页")]
    [InlineData("业务")]
    [InlineData("管理")]
    public void Group_CanBeSet_ToKnownValues(string group)
    {
        var item = new NavigationItem { Group = group };
        item.Group.Should().Be(group);
    }
}
