using FluentAssertions;
using LYBT.Shared.ExceptionHandling.Handlers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.ErrorHandling;

public class DesktopExceptionHandlerTests
{
    private readonly DesktopExceptionHandler _sut;

    public DesktopExceptionHandlerTests()
    {
        var logger = Substitute.For<ILogger<DesktopExceptionHandler>>();
        _sut = new DesktopExceptionHandler(logger);
    }

    #region US-ERR-003: Retryable exception classification

    [Fact]
    public void US_ERR_003_CanRetry_TimeoutException_ReturnsTrue()
    {
        _sut.CanRetry(new TimeoutException()).Should().BeTrue();
    }

    [Fact]
    public void US_ERR_003_CanRetry_HttpRequestException_ReturnsTrue()
    {
        _sut.CanRetry(new System.Net.Http.HttpRequestException()).Should().BeTrue();
    }

    [Fact]
    public void US_ERR_003_CanRetry_TaskCanceledException_ReturnsTrue()
    {
        _sut.CanRetry(new TaskCanceledException()).Should().BeTrue();
    }

    [Fact]
    public void US_ERR_003_CanRetry_SocketException_ReturnsTrue()
    {
        _sut.CanRetry(new System.Net.Sockets.SocketException()).Should().BeTrue();
    }

    [Fact]
    public void US_ERR_003_CanRetry_ArgumentException_ReturnsFalse()
    {
        _sut.CanRetry(new ArgumentException("invalid")).Should().BeFalse();
    }

    [Fact]
    public void US_ERR_003_CanRetry_InvalidOperationException_ReturnsFalse()
    {
        _sut.CanRetry(new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public void US_ERR_003_HandleException_Generic_ReturnsFailureResult()
    {
        var result = _sut.HandleException<string>(new InvalidOperationException("test"), "TestMethod");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void US_ERR_003_HandleException_DoesNotThrow()
    {
        var act = () => _sut.HandleException(new Exception("test"));
        act.Should().NotThrow();
    }

    #endregion
}
