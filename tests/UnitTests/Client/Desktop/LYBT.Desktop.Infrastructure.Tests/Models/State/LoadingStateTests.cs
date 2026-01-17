using LYBT.Desktop.Infrastructure.Models.State;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Models.State;

/// <summary>
/// LoadingState 单元测试
/// OpenSpec: unify-control-data-binding
/// </summary>
public class LoadingStateTests
{
    [Fact]
    public void IsLoading_DefaultsFalse()
    {
        var state = new LoadingState();
        Assert.False(state.IsLoading);
    }

    [Fact]
    public void LoadingMessage_DefaultsNull()
    {
        var state = new LoadingState();
        Assert.Null(state.LoadingMessage);
    }

    [Fact]
    public void StartLoading_SetsIsLoadingTrue()
    {
        var state = new LoadingState();
        state.StartLoading();
        Assert.True(state.IsLoading);
    }

    [Fact]
    public void StartLoading_WithMessage_SetsMessage()
    {
        var state = new LoadingState();
        state.StartLoading("加载中...");
        Assert.True(state.IsLoading);
        Assert.Equal("加载中...", state.LoadingMessage);
    }

    [Fact]
    public void StopLoading_SetsIsLoadingFalse()
    {
        var state = new LoadingState();
        state.StartLoading("加载中...");
        state.StopLoading();
        Assert.False(state.IsLoading);
        Assert.Null(state.LoadingMessage);
    }
}
