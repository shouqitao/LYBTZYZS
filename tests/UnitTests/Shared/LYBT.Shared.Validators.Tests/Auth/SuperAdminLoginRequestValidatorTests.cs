using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Validators.Auth;
using Xunit;

namespace LYBT.Shared.Validators.Tests.Auth;

/// <summary>
/// SuperAdminLoginRequestValidator 单元测试
/// 验证规则：Password(必填)
/// </summary>
public class SuperAdminLoginRequestValidatorTests
{
    private readonly SuperAdminLoginRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidPassword_ShouldPass()
    {
        // Arrange
        var request = new SuperAdminLoginRequest
        {
            Password = "SuperSecretPassword"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPassword_ShouldFail(string? password)
    {
        // Arrange
        var request = new SuperAdminLoginRequest
        {
            Password = password!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("密码不能为空");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("shortpwd")]
    [InlineData("VeryLongPasswordThatShouldStillBeValid")]
    public void Validate_WithVariousValidPasswords_ShouldPass(string password)
    {
        // Arrange - SuperAdmin只验证非空，不验证长度
        var request = new SuperAdminLoginRequest
        {
            Password = password
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
