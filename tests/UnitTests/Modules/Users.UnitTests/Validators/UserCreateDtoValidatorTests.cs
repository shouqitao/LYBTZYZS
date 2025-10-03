using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Module.Users.Validators;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Module.Users.Tests.Validators;

/// <summary>
/// UserCreateDtoValidator 单元测试
/// Issue #866 - Phase 2.2: Users 模块测试 - Validator 测试
/// 测试 UserCreateDto 的所有验证规则
/// </summary>
public class UserCreateDtoValidatorTests
{
    private readonly UserCreateDtoValidator _validator;

    public UserCreateDtoValidatorTests()
    {
        _validator = new UserCreateDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            Username = "validuser",
            Password = "Password123",
            ConfirmPassword = "Password123",
            RealName = "测试用户",
            PhoneNumber = "13812345678",
            Email = "test@example.com",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUsername_FailsValidation()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            Username = "",
            Password = "Password123",
            ConfirmPassword = "Password123",
            RealName = "测试用户",
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WithNullUsername_FailsValidation()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            Username = null,
            Password = "Password123",
            ConfirmPassword = "Password123",
            RealName = "测试用户",
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WithMinimumValidUsername_PassesValidation()
    {
        // Arrange - 只验证 NotEmpty，单字符也通过
        var dto = new UserCreateDto
        {
            Username = "a",
            Password = "Password123",
            ConfirmPassword = "Password123",
            RealName = "测试用户",
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WithSpecialCharactersInUsername_PassesValidation()
    {
        // Arrange - 没有格式验证，特殊字符也通过
        var dto = new UserCreateDto
        {
            Username = "user@#$%",
            Password = "Password123",
            ConfirmPassword = "Password123",
            RealName = "测试用户",
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WithAllOptionalFields_PassesValidation()
    {
        // Arrange - 其他字段没有验证规则
        var dto = new UserCreateDto
        {
            Username = "validuser",
            Password = null,
            ConfirmPassword = null,
            RealName = null,
            PhoneNumber = null,
            Email = null,
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
