using FluentAssertions;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Primitives.ErrorCodes;
using Xunit;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Tests.Exceptions;

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
        var errorCode = EC.MedicalCaseHasPrescriptions;
        var message = "病例存在处方无法删除";

        // Act
        var exception = new BusinessException(errorCode, message);

        // Assert
        exception.TypedErrorCode.Should().Be(errorCode);
        // BusinessException overrides GetHttpStatusCode to always return 400
        exception.GetHttpStatusCode().Should().Be(400);
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

    [Fact]
    public void NotFoundException_StaticFactory_User_CreatesCorrectException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var exception = NotFoundException.User(userId);

        // Assert
        exception.TypedErrorCode.Should().Be(EC.UserNotFound);
        exception.ResourceType.Should().Be("用户");
        exception.ResourceId.Should().Be(userId.ToString());
    }

    #endregion

    #region ValidationException测试

    [Fact]
    public void ValidationException_WithFieldAndMessage_SetsProperties()
    {
        // Arrange
        var fieldName = "UserName";
        var errorMessage = "用户名不能为空";

        // Act
        var exception = new ValidationException(fieldName, errorMessage);

        // Assert
        exception.FieldName.Should().Be(fieldName);
        exception.Errors.Should().ContainKey(fieldName);
        exception.GetHttpStatusCode().Should().Be(400);
    }

    [Fact]
    public void ValidationException_WithMultipleErrors_SetsAllErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Name", new[] { "不能为空", "长度不能超过50" } },
            { "Email", new[] { "格式不正确" } }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().HaveCount(2);
        exception.Errors["Name"].Should().HaveCount(2);
        exception.Errors["Email"].Should().HaveCount(1);
    }

    [Fact]
    public void ValidationException_AddError_AppendsToExisting()
    {
        // Arrange
        var exception = new ValidationException("Name", "不能为空");

        // Act
        exception.AddError("Name", "长度过短");
        exception.AddError("Email", "格式错误");

        // Assert
        exception.Errors["Name"].Should().HaveCount(2);
        exception.Errors["Email"].Should().HaveCount(1);
    }

    #endregion

    #region ConflictException测试

    [Fact]
    public void ConflictException_WithMessage_SetsProperties()
    {
        // Arrange
        var message = "用户名已存在";

        // Act
        var exception = new ConflictException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.GetHttpStatusCode().Should().Be(409);
    }

    [Fact]
    public void ConflictException_WithTypedErrorCode_SetsCorrectProperties()
    {
        // Arrange
        var errorCode = EC.UserNameExists;

        // Act
        var exception = new ConflictException(errorCode, "用户名已被占用", "请更换其他用户名");

        // Assert
        exception.TypedErrorCode.Should().Be(errorCode);
        exception.GetHttpStatusCode().Should().Be(409);
    }

    [Fact]
    public void ConflictException_StaticFactory_MedicalCaseVersion_CreatesCorrectException()
    {
        // Arrange
        var caseId = Guid.NewGuid();
        var expectedVersion = 1;
        var currentVersion = 2;

        // Act
        var exception = ConflictException.MedicalCaseVersion(caseId, expectedVersion, currentVersion);

        // Assert
        exception.TypedErrorCode.Should().Be(EC.MedicalCaseVersionConflict);
        exception.ExpectedVersion.Should().Be(expectedVersion);
        exception.CurrentVersion.Should().Be(currentVersion);
    }

    #endregion

    #region UnauthorizedException测试

    [Fact]
    public void UnauthorizedException_Default_Returns401()
    {
        // Act
        var exception = new UnauthorizedException();

        // Assert
        exception.GetHttpStatusCode().Should().Be(401);
    }

    [Fact]
    public void UnauthorizedException_WithTypedErrorCode_SetsProperties()
    {
        // Arrange
        var errorCode = EC.InvalidRefreshToken;

        // Act
        var exception = new UnauthorizedException(errorCode, "刷新令牌无效", "令牌已过期");

        // Assert
        exception.TypedErrorCode.Should().Be(errorCode);
        exception.FailureReason.Should().Be("令牌已过期");
        exception.GetHttpStatusCode().Should().Be(401);
    }

    [Fact]
    public void UnauthorizedException_StaticFactory_InvalidPassword_CreatesCorrectException()
    {
        // Act
        var exception = UnauthorizedException.InvalidPassword();

        // Assert
        exception.TypedErrorCode.Should().Be(EC.InvalidPassword);
        exception.FailureReason.Should().Be("密码验证失败");
    }

    #endregion
}
