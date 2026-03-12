using FluentAssertions;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Primitives.ErrorCodes;
using Xunit;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Tests.Server.Unit.Shared.ExceptionHandling;

/// <summary>
/// UnauthorizedException 单元测试
/// Sprint 5 - US-ERR-007 (CODE-25): TokenExpired 工厂方法补全
/// </summary>
public class UnauthorizedExceptionTests
{
    #region TokenExpired 工厂方法测试

    [Fact]
    public void TokenExpired_SetsCorrectErrorCode()
    {
        // Act
        var ex = UnauthorizedException.TokenExpired();

        // Assert
        ex.TypedErrorCode.Should().Be(EC.AuthAccessTokenExpired);
    }

    [Fact]
    public void TokenExpired_SetsCorrectUserMessage()
    {
        // Act
        var ex = UnauthorizedException.TokenExpired();

        // Assert
        ex.UserMessage.Should().Be("访问令牌已过期，请重新登录");
    }

    [Fact]
    public void TokenExpired_SetsCorrectFailureReason()
    {
        // Act
        var ex = UnauthorizedException.TokenExpired();

        // Assert
        ex.FailureReason.Should().Be("访问令牌已过期");
    }

    [Fact]
    public void TokenExpired_ReturnsHttpStatus401()
    {
        // Act
        var ex = UnauthorizedException.TokenExpired();

        // Assert
        ex.GetHttpStatusCode().Should().Be(401);
    }

    [Fact]
    public void TokenExpired_ErrorCodeFormattedString_ContainsCorrectValue()
    {
        // Act
        var ex = UnauthorizedException.TokenExpired();

        // Assert
        ex.ErrorCode.Should().Be("ERR-10206");
    }

    #endregion

    #region AuthAccessTokenExpired ErrorCode 测试

    [Fact]
    public void AuthAccessTokenExpired_HasCorrectValue()
    {
        // Assert
        ((int)EC.AuthAccessTokenExpired).Should().Be(10206);
    }

    [Fact]
    public void AuthAccessTokenExpired_ToHttpStatusCode_Returns401()
    {
        // Act
        var httpCode = EC.AuthAccessTokenExpired.ToHttpStatusCode();

        // Assert
        httpCode.Should().Be(401);
    }

    [Fact]
    public void AuthAccessTokenExpired_GetUserMessage_ReturnsMeaningfulMessage()
    {
        // Act
        var message = ErrorMessages.GetUserMessage(EC.AuthAccessTokenExpired);

        // Assert
        message.Should().NotBeNullOrEmpty();
        message.Should().Contain("令牌");
    }

    #endregion
}
