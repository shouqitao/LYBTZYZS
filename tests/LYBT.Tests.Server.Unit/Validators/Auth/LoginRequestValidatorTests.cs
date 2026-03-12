using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Validators.Auth;
using Xunit;

namespace LYBT.Tests.Server.Unit.Validators.Auth;

/// <summary>
/// LoginRequestValidator 单元测试
/// 验证规则：UserName(必填,最长32) + Password(必填,最短6)
/// </summary>
public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    #region Valid Input Tests

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("a", "123456")]
    [InlineData("user123", "password")]
    [InlineData("admin", "P@ssw0rd!")]
    public void Validate_WithVariousValidInputs_ShouldPass(string username, string password)
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = username,
            Password = password
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region UserName Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyUsername_ShouldFail(string? username)
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = username!,
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserName)
            .WithErrorMessage("用户名不能为空");
    }

    [Fact]
    public void Validate_WithUsernameTooLong_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = new string('a', 33), // 33 > 32
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserName)
            .WithErrorMessage("用户名长度不能超过32个字符");
    }

    [Fact]
    public void Validate_WithUsernameAtMaxLength_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = new string('a', 32), // exactly 32
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserName);
    }

    #endregion

    #region Password Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPassword_ShouldFail(string? password)
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = password!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("密码不能为空");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    public void Validate_WithPasswordTooShort_ShouldFail(string password)
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = password
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("密码长度不能少于6个字符");
    }

    [Fact]
    public void Validate_WithPasswordAtMinLength_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "123456" // exactly 6
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Validate_ShouldReturnCorrectErrorMessages()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "",
            Password = "123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.ErrorMessage == "用户名不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "密码长度不能少于6个字符");
    }

    #endregion
}
