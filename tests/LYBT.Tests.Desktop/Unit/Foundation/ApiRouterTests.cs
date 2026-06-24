using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using NSubstitute;

namespace LYBT.Tests.Desktop;

/// <summary>
/// Tests for simplified ApiRouter — delegates to IConnectionSettingsService.
/// </summary>
public class ApiRouterTests
{
    #region CurrentUrl

    [Fact]
    public void CurrentUrl_ShouldDelegateToConnectionSettingsService()
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://192.168.1.100:5000");
        var router = new ApiRouter(cs);

        router.CurrentUrl.Should().Be("http://192.168.1.100:5000");
    }

    [Fact]
    public void CurrentUrl_ShouldReflectUrlChanges()
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns("http://127.0.0.1:5300", "http://remote:5000");
        var router = new ApiRouter(cs);

        router.CurrentUrl.Should().Be("http://127.0.0.1:5300");
        router.CurrentUrl.Should().Be("http://remote:5000");
    }

    #endregion

    #region IsLocal

    [Theory]
    [InlineData("http://127.0.0.1:5300", true)]
    [InlineData("http://localhost:5300", true)]
    [InlineData("http://192.168.1.100:5000", false)]
    public void IsLocal_ShouldDelegateToConnectionSettingsService(string url, bool expected)
    {
        var cs = Substitute.For<IConnectionSettingsService>();
        cs.CurrentUrl.Returns(url);
        cs.IsLocal.Returns(expected);
        var router = new ApiRouter(cs);

        router.IsLocal.Should().Be(expected);
    }

    #endregion
}
