using LYBT.Desktop.Controls.Models;

namespace LYBT.Tests.Desktop.Models;

public class NavigationItemTests
{
    [Fact]
    public void New_Instance_DefaultGroup_IsBusiness()
    {
        var item = new NavigationItem();
        Assert.Equal("业务", item.Group);
    }

    [Theory]
    [InlineData("主页")]
    [InlineData("业务")]
    [InlineData("管理")]
    public void Group_CanBeSet_ToKnownValues(string group)
    {
        var item = new NavigationItem { Group = group };
        Assert.Equal(group, item.Group);
    }
}
