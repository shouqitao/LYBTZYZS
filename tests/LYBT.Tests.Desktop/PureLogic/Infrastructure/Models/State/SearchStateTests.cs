using LYBT.Desktop.Infrastructure.Models.State;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure.Models.State;

/// <summary>
/// SearchState 单元测试
/// OpenSpec: unify-control-data-binding
/// </summary>
public class SearchStateTests
{
    [Fact]
    public void SearchText_DefaultsEmpty()
    {
        var state = new SearchState();
        Assert.Equal(string.Empty, state.SearchText);
    }

    [Fact]
    public void IsSearching_DefaultsFalse()
    {
        var state = new SearchState();
        Assert.False(state.IsSearching);
    }

    [Fact]
    public void HasSearchText_FalseWhenEmpty()
    {
        var state = new SearchState { SearchText = "" };
        Assert.False(state.HasSearchText);
    }

    [Fact]
    public void HasSearchText_TrueWhenNotEmpty()
    {
        var state = new SearchState { SearchText = "test" };
        Assert.True(state.HasSearchText);
    }

    [Fact]
    public void Clear_ResetsSearchText()
    {
        var state = new SearchState { SearchText = "test", IsSearching = true };
        state.Clear();
        Assert.Equal(string.Empty, state.SearchText);
        Assert.False(state.IsSearching);
    }
}
