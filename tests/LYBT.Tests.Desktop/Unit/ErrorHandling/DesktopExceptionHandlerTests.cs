using FluentAssertions;
using LYBT.Desktop.Infrastructure.ExceptionHandling;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop;

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
