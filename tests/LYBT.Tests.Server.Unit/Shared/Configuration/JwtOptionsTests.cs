using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using LYBT.Shared.Configuration.Options.Common;
using Xunit;

namespace LYBT.Tests.Server.Unit.Shared.Configuration;

/// <summary>
/// JwtOptions 单元测试
/// </summary>
public class JwtOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeJwt()
    {
        // Assert
        JwtOptions.SectionName.Should().Be("Jwt");
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var options = new JwtOptions();

        // Assert
        options.SecretKey.Should().BeEmpty();
        options.Issuer.Should().Be("LYBT.WebAPI");
        options.Audience.Should().Be("LYBT.Client");
        options.AccessTokenExpirationMinutes.Should().Be(30);
        options.RefreshTokenExpirationDays.Should().Be(7);
        options.ClockSkewSeconds.Should().Be(300);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validation_SecretKey_Required(string? secretKey)
    {
        // Arrange
        var options = new JwtOptions { SecretKey = secretKey! };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(JwtOptions.SecretKey)));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("12345678901234567890123456789")]
    public void Validation_SecretKey_MinLength32(string secretKey)
    {
        // Arrange
        var options = new JwtOptions { SecretKey = secretKey };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(JwtOptions.SecretKey)));
    }

    [Fact]
    public void Validation_SecretKey_ValidWhen32OrMore()
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = "12345678901234567890123456789012" // 32 chars
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(1441)]
    public void Validation_AccessTokenExpirationMinutes_OutOfRange(int minutes)
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = "12345678901234567890123456789012",
            AccessTokenExpirationMinutes = minutes
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(JwtOptions.AccessTokenExpirationMinutes)));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(60)]
    [InlineData(1440)]
    public void Validation_AccessTokenExpirationMinutes_ValidRange(int minutes)
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = "12345678901234567890123456789012",
            AccessTokenExpirationMinutes = minutes
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Validation_RefreshTokenExpirationDays_OutOfRange(int days)
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = "12345678901234567890123456789012",
            RefreshTokenExpirationDays = days
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    public void Validation_RefreshTokenExpirationDays_ValidRange(int days)
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = "12345678901234567890123456789012",
            RefreshTokenExpirationDays = days
        };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeTrue();
    }
}
