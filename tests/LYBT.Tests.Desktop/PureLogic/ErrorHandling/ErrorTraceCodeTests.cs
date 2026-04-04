using FluentAssertions;
using LYBT.Shared.ExceptionHandling.Mappers;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.ErrorHandling;

public class ErrorTraceCodeTests : IDisposable
{
    public void Dispose()
    {
        ClientErrorMessageMapper.TraceIdProvider = null;
    }

    #region US-ERR-007: Error trace code generation

    [Fact]
    public void US_ERR_007_GetShortTrackingCode_WithProvider_Returns8UppercaseChars()
    {
        ClientErrorMessageMapper.TraceIdProvider = () => "abcdef1234567890abcdef1234567890";

        var code = ClientErrorMessageMapper.GetShortTrackingCode();

        code.Should().Be("ABCDEF12");
        code.Length.Should().Be(8);
    }

    [Fact]
    public void US_ERR_007_GetShortTrackingCode_WithProvider_IsUppercase()
    {
        ClientErrorMessageMapper.TraceIdProvider = () => "abcdef1234567890";

        var code = ClientErrorMessageMapper.GetShortTrackingCode();

        code.Should().MatchRegex("^[A-Z0-9]+$");
    }

    [Fact]
    public void US_ERR_007_GetFullTrackingCode_WithProvider_ReturnsFullId()
    {
        var fullId = "abcdef1234567890abcdef1234567890";
        ClientErrorMessageMapper.TraceIdProvider = () => fullId;

        var code = ClientErrorMessageMapper.GetFullTrackingCode();

        code.Should().Be(fullId);
    }

    [Fact]
    public void US_ERR_007_GetShortTrackingCode_WithoutProvider_Returns8Chars()
    {
        ClientErrorMessageMapper.TraceIdProvider = null;

        var code = ClientErrorMessageMapper.GetShortTrackingCode();

        code.Length.Should().Be(8);
    }

    [Fact]
    public void US_ERR_007_GetShortTrackingCode_WithoutProvider_IsUppercase()
    {
        ClientErrorMessageMapper.TraceIdProvider = null;

        var code = ClientErrorMessageMapper.GetShortTrackingCode();

        code.Should().MatchRegex("^[A-Z0-9]+$");
    }

    #endregion
}
