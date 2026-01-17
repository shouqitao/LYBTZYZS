using LYBT.Desktop.Infrastructure.Models.Options;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Models.Options;

/// <summary>
/// PaginationOptions 单元测试
/// OpenSpec: unify-control-data-binding
/// </summary>
public class PaginationOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new PaginationOptions();
        Assert.Equal(20, options.DefaultPageSize);
        Assert.Equal(new[] { 10, 20, 50, 100 }, options.PageSizeOptions);
        Assert.True(options.ShowPageSizeSelector);
    }

    [Fact]
    public void WithCustomValues_SetsCorrectly()
    {
        var options = new PaginationOptions(
            DefaultPageSize: 50,
            PageSizeOptions: new[] { 25, 50, 100 },
            ShowPageSizeSelector: false);
        Assert.Equal(50, options.DefaultPageSize);
        Assert.Equal(new[] { 25, 50, 100 }, options.PageSizeOptions);
        Assert.False(options.ShowPageSizeSelector);
    }

    [Fact]
    public void Record_IsImmutable()
    {
        var original = new PaginationOptions();
        var modified = original with { DefaultPageSize = 100 };
        Assert.Equal(20, original.DefaultPageSize);
        Assert.Equal(100, modified.DefaultPageSize);
    }
}
