using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.Validation;
using LYBT.Shared.Validators.Users;
using Xunit;

namespace LYBT.Tests.Unit.Shared.Validators.Users;

/// <summary>
/// UserInputDtoValidator 单元测试
/// 验证规则：创建时 UserName/RealName/Role 必填，Password 有长度限制，ConfirmPassword 需匹配
/// </summary>
public class UserInputDtoValidatorTests
{
    private readonly UserInputDtoValidator _validator = new();

    #region Valid Input Tests

    [Fact]
    public void Validate_WithValidNewUser_ShouldPass()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OnUpdate_ShouldNotRequireUsername()
    {
        // Arrange - 更新时有 Id，不需要 UserName
        var dto = new UserInputDto
        {
            Id = Guid.NewGuid(),
            RealName = "李四",
            Role = UserRole.Doctor
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserName);
    }

    [Fact]
    public void Validate_WithEmptyOptionalFields_ShouldPass()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Email = null;
        dto.PhoneNumber = null;
        dto.Remark = null;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region UserName Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyUsername_OnCreate_ShouldFail(string? username)
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Id = null; // 创建模式
        dto.UserName = username!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserName)
            .WithErrorMessage("用户名不能为空");
    }

    #endregion

    #region RealName Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyRealName_OnCreate_ShouldFail(string? realName)
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Id = null;
        dto.RealName = realName!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RealName)
            .WithErrorMessage("真实姓名不能为空");
    }

    [Fact]
    public void Validate_WithRealNameTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.RealName = new string('李', ValidationConstants.NameMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RealName)
            .WithErrorMessage($"真实姓名长度不能超过{ValidationConstants.NameMaxLength}个字符");
    }

    #endregion

    #region Role Validation Tests

    [Fact]
    public void Validate_WithInvalidRole_OnCreate_ShouldFail()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Id = null;
        dto.Role = (UserRole)999;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Role)
            .WithErrorMessage("用户角色无效");
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Doctor)]
    public void Validate_WithValidRole_ShouldPass(UserRole role)
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Role = role;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    #endregion

    #region Password Validation Tests

    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    public void Validate_WithPasswordTooShort_ShouldFail(string password)
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Password = password;
        dto.ConfirmPassword = password;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("密码长度不能少于8个字符");
    }

    [Fact]
    public void Validate_WithPasswordTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        var longPassword = new string('a', ValidationConstants.PasswordMaxLength + 1);
        dto.Password = longPassword;
        dto.ConfirmPassword = longPassword;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage($"密码长度不能超过{ValidationConstants.PasswordMaxLength}个字符");
    }

    [Fact]
    public void Validate_WithMismatchedPasswords_ShouldFail()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Password = "Password123";
        dto.ConfirmPassword = "DifferentPassword";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("两次输入的密码不一致");
    }

    [Fact]
    public void Validate_WithMatchingPasswords_ShouldPass()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Password = "Password123";
        dto.ConfirmPassword = "Password123";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void Validate_WithNoPassword_ShouldPass()
    {
        // Arrange - 更新时可以不修改密码
        var dto = new UserInputDto
        {
            Id = Guid.NewGuid(),
            Password = null,
            ConfirmPassword = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
        result.ShouldNotHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    #endregion

    #region Email Validation Tests

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Email = "notanemail";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(ValidationConstants.EmailFormatErrorMessage);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("admin@hospital.cn")]
    public void Validate_WithValidEmail_ShouldPass(string email)
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Email = email;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithEmailTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Email = new string('a', 92) + "@test.com"; // 92+9=101 > 100

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("邮箱长度不能超过100个字符");
    }

    #endregion

    #region PhoneNumber Validation Tests

    [Fact]
    public void Validate_WithInvalidPhoneNumber_ShouldFail()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.PhoneNumber = "12345";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage(ValidationConstants.PhoneFormatErrorMessage);
    }

    [Theory]
    [InlineData("13800138000")]
    [InlineData("15912345678")]
    [InlineData("18888888888")]
    public void Validate_WithValidPhoneNumber_ShouldPass(string phoneNumber)
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.PhoneNumber = phoneNumber;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    #endregion

    #region Remark Validation Tests

    [Fact]
    public void Validate_WithRemarkTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidNewUserInputDto();
        dto.Remark = new string('备', ValidationConstants.RemarkMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Remark)
            .WithErrorMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符");
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Validate_ShouldReturnCorrectErrorMessages()
    {
        // Arrange
        var dto = new UserInputDto
        {
            Id = null, // 创建模式
            UserName = "",
            RealName = "",
            Password = "123",
            ConfirmPassword = "456"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
        result.Errors.Should().Contain(e => e.ErrorMessage == "用户名不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "真实姓名不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "密码长度不能少于8个字符");
        result.Errors.Should().Contain(e => e.ErrorMessage == "两次输入的密码不一致");
    }

    #endregion

    #region Helper Methods

    private static UserInputDto CreateValidNewUserInputDto()
    {
        return new UserInputDto
        {
            Id = null, // 创建模式
            UserName = "doctor001",
            RealName = "张医生",
            Role = UserRole.Doctor,
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            Email = "doctor@example.com",
            PhoneNumber = "13800138000"
        };
    }

    #endregion
}
