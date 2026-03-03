using FluentAssertions;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Validation;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Tests.Unit.Shared.Configuration.Validation;

/// <summary>
/// JwtOptionsValidator 单元测试
/// </summary>
public class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidBase64SecretKey_ReturnsSuccess()
    {
        // Arrange - 64字节的Base64编码密钥
        var options = new JwtOptions
        {
            SecretKey = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidBase64SecretKey_ReturnsFailure()
    {
        // Arrange - 非Base64字符串
        var options = new JwtOptions
        {
            SecretKey = "not-valid-base64!!!",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Base64");
    }

    [Fact]
    public void Validate_ShortBase64SecretKey_ReturnsFailure()
    {
        // Arrange - 有效的Base64但解码后小于32字节
        var options = new JwtOptions
        {
            SecretKey = "c2hvcnQ=", // "short" in Base64
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("32 字节");
    }

    [Fact]
    public void Validate_EmptySecretKey_ReturnsSuccess()
    {
        // Arrange - 空字符串会跳过Base64验证（由DataAnnotation Required处理）
        var options = new JwtOptions
        {
            SecretKey = "",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_AccessTokenLongerThanRefreshToken_ReturnsFailure()
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            AccessTokenExpirationMinutes = 7 * 24 * 60 + 1, // 超过7天
            RefreshTokenExpirationDays = 7
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("AccessToken");
    }

    [Fact]
    public void Validate_AccessTokenEqualToRefreshToken_ReturnsFailure()
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            AccessTokenExpirationMinutes = 7 * 24 * 60, // 刚好7天
            RefreshTokenExpirationDays = 7
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Validate_AccessTokenShorterThanRefreshToken_ReturnsSuccess()
    {
        // Arrange
        var options = new JwtOptions
        {
            SecretKey = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            AccessTokenExpirationMinutes = 30, // 30分钟
            RefreshTokenExpirationDays = 7 // 7天
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}
