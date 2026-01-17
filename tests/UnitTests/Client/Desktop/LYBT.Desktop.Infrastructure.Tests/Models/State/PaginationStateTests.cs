using LYBT.Desktop.Infrastructure.Models.State;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Models.State;

public class PaginationStateTests
{
    [Fact]
    public void TotalPages_CalculatesCorrectly()
    {
        var state = new PaginationState { TotalCount = 55, PageSize = 20 };
        Assert.Equal(3, state.TotalPages);
    }

    [Fact]
    public void HasPrevious_FalseOnFirstPage()
    {
        var state = new PaginationState { CurrentPage = 1 };
        Assert.False(state.HasPrevious);
    }

    [Fact]
    public void HasNext_TrueWhenNotOnLastPage()
    {
        var state = new PaginationState { CurrentPage = 1, TotalCount = 50, PageSize = 20 };
        Assert.True(state.HasNext);
    }

    [Fact]
    public void GoToPage_ClampsToValidRange()
    {
        var state = new PaginationState { TotalCount = 50, PageSize = 20 };
        state.GoToPage(10);
        Assert.Equal(3, state.CurrentPage);
    }

    [Fact]
    public void Reset_SetsDefaultValues()
    {
        var state = new PaginationState { CurrentPage = 5, TotalCount = 100 };
        state.Reset();
        Assert.Equal(1, state.CurrentPage);
        Assert.Equal(0, state.TotalCount);
    }
}
