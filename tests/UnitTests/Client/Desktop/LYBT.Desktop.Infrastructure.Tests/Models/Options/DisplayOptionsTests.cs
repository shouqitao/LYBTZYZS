using LYBT.Desktop.Infrastructure.Models.Options;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Models.Options;

/// <summary>
/// DisplayOptions 单元测试
/// OpenSpec: unify-control-data-binding
/// </summary>
public class DisplayOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new DisplayOptions();
        Assert.False(options.IsCompactMode);
        Assert.True(options.ShowHeader);
        Assert.True(options.ShowFooter);
    }

    [Fact]
    public void WithValues_SetsCorrectly()
    {
        var options = new DisplayOptions(IsCompactMode: true, ShowHeader: false, ShowFooter: false);
        Assert.True(options.IsCompactMode);
        Assert.False(options.ShowHeader);
        Assert.False(options.ShowFooter);
    }

    [Fact]
    public void Record_SupportsWithExpression()
    {
        var original = new DisplayOptions();
        var modified = original with { IsCompactMode = true };
        Assert.False(original.IsCompactMode);
        Assert.True(modified.IsCompactMode);
    }
}
