using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Validators.Auth;
using Xunit;

namespace LYBT.Shared.Validators.Tests.Auth;

/// <summary>
/// ChangePasswordRequestValidator 单元测试
/// 验证规则：OldPassword(必填) + NewPassword(必填,6-50字符,不能与旧密码相同)
/// </summary>
public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    #region Valid Input Tests

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPass123",
            NewPassword = "NewPass456"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("old123", "new456")]
    [InlineData("Password1", "Password2")]
    [InlineData("short1", "123456")]
    public void Validate_WithValidDifferentPasswords_ShouldPass(string oldPassword, string newPassword)
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = oldPassword,
            NewPassword = newPassword
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region OldPassword Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOldPassword_ShouldFail(string? oldPassword)
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = oldPassword!,
            NewPassword = "NewPass456"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OldPassword)
            .WithErrorMessage("原密码不能为空");
    }

    #endregion

    #region NewPassword Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyNewPassword_ShouldFail(string? newPassword)
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPass123",
            NewPassword = newPassword!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("新密码不能为空");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    public void Validate_WithNewPasswordTooShort_ShouldFail(string newPassword)
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPass123",
            NewPassword = newPassword
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("新密码长度不能少于6个字符");
    }

    [Fact]
    public void Validate_WithNewPasswordTooLong_ShouldFail()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPass123",
            NewPassword = new string('a', 51) // 51 > 50
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("新密码长度不能超过50个字符");
    }

    [Fact]
    public void Validate_WithNewPasswordAtMinLength_ShouldPass()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPass123",
            NewPassword = "123456" // exactly 6
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_WithNewPasswordAtMaxLength_ShouldPass()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "OldPass123",
            NewPassword = new string('a', 50) // exactly 50
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_WithSameOldAndNewPassword_ShouldFail()
    {
        // Arrange
        var samePassword = "SamePass123";
        var request = new ChangePasswordRequest
        {
            OldPassword = samePassword,
            NewPassword = samePassword
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("新密码不能与原密码相同");
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Validate_ShouldReturnCorrectErrorMessages()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "",
            NewPassword = "123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.ErrorMessage == "原密码不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "新密码长度不能少于6个字符");
    }

    #endregion
}
