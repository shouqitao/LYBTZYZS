using FluentValidation.TestHelper;
using LYBT.Module.Users.Validators;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Module.Users.Tests.Validators;

/// <summary>
/// UserUpdateDtoValidator 单元测试
/// Issue #866 - Phase 2.2: Users 模块测试 - Validator 测试
/// 测试 UserUpdateDto 的所有验证规则
/// </summary>
public class UserUpdateDtoValidatorTests
{
    private readonly UserUpdateDtoValidator _validator;

    public UserUpdateDtoValidatorTests()
    {
        _validator = new UserUpdateDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new UserUpdateDto
        {
            Id = Guid.NewGuid(),
            RealName = "更新用户",
            PhoneNumber = "13812345678",
            Email = "update@example.com",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyId_FailsValidation()
    {
        // Arrange
        var dto = new UserUpdateDto
        {
            Id = Guid.Empty,
            RealName = "更新用户",
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WithNullEmail_PassesValidation()
    {
        // Arrange - 没有 Email 验证规则
        var dto = new UserUpdateDto
        {
            Id = Guid.NewGuid(),
            RealName = "更新用户",
            Email = null,
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithInvalidEmail_PassesValidation()
    {
        // Arrange - 没有 Email 格式验证
        var dto = new UserUpdateDto
        {
            Id = Guid.NewGuid(),
            RealName = "更新用户",
            Email = "invalid-email",
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithNullPhoneNumber_PassesValidation()
    {
        // Arrange - 没有 PhoneNumber 验证规则
        var dto = new UserUpdateDto
        {
            Id = Guid.NewGuid(),
            RealName = "更新用户",
            PhoneNumber = null,
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithInvalidPhoneNumber_PassesValidation()
    {
        // Arrange - 没有 PhoneNumber 格式验证
        var dto = new UserUpdateDto
        {
            Id = Guid.NewGuid(),
            RealName = "更新用户",
            PhoneNumber = "invalid-phone",
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithNullRealName_PassesValidation()
    {
        // Arrange - 没有 RealName 验证规则
        var dto = new UserUpdateDto
        {
            Id = Guid.NewGuid(),
            RealName = null,
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RealName);
    }

    [Fact]
    public void Validate_WithLongRealName_PassesValidation()
    {
        // Arrange - 没有长度验证
        var dto = new UserUpdateDto
        {
            Id = Guid.NewGuid(),
            RealName = new string('名', 100),
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RealName);
    }

    [Fact]
    public void Validate_WithAllOptionalFieldsNull_PassesValidation()
    {
        // Arrange - 只验证 Id，其他都可空
        var dto = new UserUpdateDto
        {
            Id = Guid.NewGuid(),
            RealName = null,
            PhoneNumber = null,
            Email = null,
            Role = null,
            Status = CommonStatus.Enabled
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
