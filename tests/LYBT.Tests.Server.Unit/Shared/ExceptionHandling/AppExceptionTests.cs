using FluentAssertions;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Primitives.ErrorCodes;
using Xunit;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Tests.Server.Unit.Shared.ExceptionHandling;

/// <summary>
/// AppException单元测试
/// consolidate-exception-handling: Phase 9
/// </summary>
public class AppExceptionTests
{
    #region 构造函数测试

    [Fact]
    public void Constructor_Default_SetsDefaultMessage()
    {
        // Act
        var exception = new AppException();

        // Assert
        exception.Message.Should().Be("应用程序异常");
        exception.ErrorCode.Should().BeNull();
        exception.TypedErrorCode.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "测试错误消息";

        // Act
        var exception = new AppException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "外层错误";
        var innerException = new InvalidOperationException("内层错误");

        // Act
        var exception = new AppException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithTypedErrorCode_SetsAllProperties()
    {
        // Arrange
        var errorCode = EC.UserNotFound;
        var message = "用户不存在";
        var userMessage = "无法找到指定的用户";

        // Act
        var exception = new AppException(errorCode, message, userMessage, showDetailToUser: true);

        // Assert
        exception.TypedErrorCode.Should().Be(errorCode);
        exception.ErrorCode.Should().Be("ERR-10001");
        exception.Message.Should().Be(message);
        exception.UserMessage.Should().Be(userMessage);
        exception.ShowDetailToUser.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithTypedErrorCode_UserMessageDefaultsToMessage()
    {
        // Arrange
        var errorCode = EC.PatientNotFound;
        var message = "患者不存在";

        // Act
        var exception = new AppException(errorCode, message);

        // Assert
        exception.UserMessage.Should().Be(message);
    }

    #endregion

    #region HTTP状态码测试

    [Theory]
    [InlineData(EC.UserNotFound, 404)]
    [InlineData(EC.ValidationFailed, 400)]
    [InlineData(EC.Unauthorized, 401)]
    [InlineData(EC.Forbidden, 403)]
    [InlineData(EC.ConcurrencyConflict, 409)]
    public void GetHttpStatusCode_WithTypedErrorCode_ReturnsCorrectStatus(EC errorCode, int expectedStatus)
    {
        // Arrange
        var exception = new AppException(errorCode, "测试");

        // Act
        var result = exception.GetHttpStatusCode();

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Fact]
    public void GetHttpStatusCode_WithoutTypedErrorCode_Returns500()
    {
        // Arrange
        var exception = new AppException("测试错误");

        // Act
        var result = exception.GetHttpStatusCode();

        // Assert
        result.Should().Be(500);
    }

    #endregion

    #region 错误类别测试

    [Theory]
    [InlineData(EC.ValidationFailed, ErrorCategory.Validation)]
    [InlineData(EC.Unauthorized, ErrorCategory.Authentication)]
    [InlineData(EC.NotFound, ErrorCategory.Resource)]
    [InlineData(EC.InternalError, ErrorCategory.System)]
    public void Category_WithTypedErrorCode_ReturnsCorrectCategory(EC errorCode, ErrorCategory expectedCategory)
    {
        // Arrange
        var exception = new AppException(errorCode, "测试");

        // Act
        var result = exception.Category;

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Fact]
    public void Category_WithoutTypedErrorCode_ReturnsGeneral()
    {
        // Arrange
        var exception = new AppException("测试错误");

        // Act
        var result = exception.Category;

        // Assert
        result.Should().Be(ErrorCategory.General);
    }

    #endregion
}
