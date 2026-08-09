using FluentAssertions;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Xunit;
using EC = LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Tests.Server;

/// <summary>
/// 业务异常类单元测试
/// consolidate-exception-handling: Phase 9
/// </summary>
public class BusinessExceptionTests
{
    #region BusinessException测试

    [Fact]
    public void BusinessException_DefaultConstructor_SetsDefaultMessage()
    {
        // Act
        var exception = new BusinessException();

        // Assert
        exception.Message.Should().Be("业务规则违反");
    }

    [Fact]
    public void BusinessException_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "自定义业务错误";

        // Act
        var exception = new BusinessException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void BusinessException_WithBusinessRule_SetsBusinessRule()
    {
        // Arrange
        var message = "业务错误";
        var businessRule = "BIZ001";

        // Act
        var exception = new BusinessException(message, businessRule);

        // Assert
        exception.BusinessRule.Should().Be(businessRule);
        exception.UserMessage.Should().Be(message);
    }

    [Fact]
    public void BusinessException_WithTypedErrorCode_SetsProperties()
    {
        // Arrange
        var errorCode = EC.MedicalCaseNotFound;
        var message = "医案不存在";

        // Act
        var exception = new BusinessException(errorCode, message);

        // Assert
        exception.TypedErrorCode.Should().Be(errorCode);
        // A-31-C2: 子类不再 override GetHttpStatusCode，状态码经 TypedErrorCode → ErrorCodeExtensions.ToHttpStatusCode 推导（SSOT）
        exception.GetHttpStatusCode().Should().Be(errorCode.ToHttpStatusCode());
        exception.Category.Should().Be(ErrorCategory.Business);
    }

    #endregion

    #region NotFoundException测试

    [Fact]
    public void NotFoundException_WithResourceInfo_SetsProperties()
    {
        // Arrange
        var resourceType = "Patient";
        var resourceId = "12345";

        // Act
        var exception = new NotFoundException(resourceType, resourceId);

        // Assert
        exception.ResourceType.Should().Be(resourceType);
        exception.ResourceId.Should().Be(resourceId);
        exception.Message.Should().Contain(resourceType);
        exception.GetHttpStatusCode().Should().Be(404);
    }

    [Fact]
    public void NotFoundException_WithTypedErrorCode_SetsCorrectStatus()
    {
        // Arrange
        var errorCode = EC.UserNotFound;

        // Act
        var exception = new NotFoundException(errorCode, "用户不存在", "User", "123");

        // Assert
        exception.TypedErrorCode.Should().Be(errorCode);
        exception.GetHttpStatusCode().Should().Be(404);
    }

    #endregion
}
