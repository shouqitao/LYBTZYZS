using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using LYBT.Infrastructure.Security;
using LYBT.Infrastructure.Configuration.Options;

namespace Infrastructure.UnitTests.Security;

public class KeyManagementServiceTests
{
    private readonly Mock<ILogger<KeyManagementService>> _mockLogger;
    private readonly Mock<IOptions<SecurityOptions>> _mockSecurityOptions;
    private readonly SecurityOptions _securityOptions;
    private readonly KeyManagementService _service;

    public KeyManagementServiceTests()
    {
        _mockLogger = new Mock<ILogger<KeyManagementService>>();
        _mockSecurityOptions = new Mock<IOptions<SecurityOptions>>();
        
        _securityOptions = new SecurityOptions
        {
            JwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-that-is-long-enough-for-jwt",
                Issuer = "test-issuer",
                Audience = "test-audience"
            },
            KeyRotation = new KeyRotationSettings
            {
                EnableRotation = true,
                RotationIntervalHours = 24
            }
        };
        
        _mockSecurityOptions.Setup(x => x.Value).Returns(_securityOptions);
        _service = new KeyManagementService(_mockLogger.Object, _mockSecurityOptions.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => new KeyManagementService(_mockLogger.Object, _mockSecurityOptions.Object);
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new KeyManagementService(null!, _mockSecurityOptions.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullSecurityOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new KeyManagementService(_mockLogger.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("securityOptions");
    }

    [Fact]
    public async Task ShouldRotateKeyAsync_WithRotationDisabled_ShouldReturnFalse()
    {
        // Arrange
        _securityOptions.KeyRotation.EnableRotation = false;

        // Act
        var result = await _service.ShouldRotateKeyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldRotateKeyAsync_WithRotationEnabled_ShouldCheckRotationInterval()
    {
        // Arrange
        _securityOptions.KeyRotation.EnableRotation = true;
        _securityOptions.KeyRotation.RotationIntervalHours = 1; // 1小时间隔

        // Act
        var result = await _service.ShouldRotateKeyAsync();

        // Assert
        // 由于这是首次检查，应该需要轮换
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RotateJwtSecretAsync_WithValidConfiguration_ShouldGenerateNewSecret()
    {
        // Arrange
        var originalSecret = _securityOptions.JwtSettings.Secret;

        // Act
        var newSecret = await _service.RotateJwtSecretAsync();

        // Assert
        newSecret.Should().NotBeNull();
        newSecret.Should().NotBeEmpty();
        newSecret.Should().NotBe(originalSecret);
        newSecret.Length.Should().BeGreaterOrEqualTo(32); // JWT密钥应该足够长
        
        // 验证新密钥是否为有效的Base64字符串（如果使用Base64编码）
        var isValidBase64 = IsValidBase64String(newSecret);
        if (isValidBase64)
        {
            var decodedBytes = Convert.FromBase64String(newSecret);
            decodedBytes.Length.Should().BeGreaterOrEqualTo(32);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-24)]
    public async Task ShouldRotateKeyAsync_WithInvalidRotationInterval_ShouldReturnFalse(int invalidHours)
    {
        // Arrange
        _securityOptions.KeyRotation.EnableRotation = true;
        _securityOptions.KeyRotation.RotationIntervalHours = invalidHours;

        // Act
        var result = await _service.ShouldRotateKeyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RotateJwtSecretAsync_CalledMultipleTimes_ShouldGenerateDifferentSecrets()
    {
        // Act
        var secret1 = await _service.RotateJwtSecretAsync();
        var secret2 = await _service.RotateJwtSecretAsync();
        var secret3 = await _service.RotateJwtSecretAsync();

        // Assert
        secret1.Should().NotBe(secret2);
        secret2.Should().NotBe(secret3);
        secret1.Should().NotBe(secret3);
        
        // 所有密钥都应该有合适的长度
        secret1.Length.Should().BeGreaterOrEqualTo(32);
        secret2.Length.Should().BeGreaterOrEqualTo(32);
        secret3.Length.Should().BeGreaterOrEqualTo(32);
    }

    [Fact]
    public async Task ShouldRotateKeyAsync_AfterRotation_ShouldUpdateLastRotationTime()
    {
        // Arrange
        _securityOptions.KeyRotation.EnableRotation = true;
        _securityOptions.KeyRotation.RotationIntervalHours = 24;

        // Act
        var shouldRotateBefore = await _service.ShouldRotateKeyAsync();
        await _service.RotateJwtSecretAsync();
        var shouldRotateAfter = await _service.ShouldRotateKeyAsync();

        // Assert
        shouldRotateBefore.Should().BeTrue();
        // 刚刚轮换后，在间隔时间内不应该再次轮换
        shouldRotateAfter.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullJwtSettings_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidOptions = new SecurityOptions
        {
            JwtSettings = null!,
            KeyRotation = new KeyRotationSettings()
        };
        var mockOptions = new Mock<IOptions<SecurityOptions>>();
        mockOptions.Setup(x => x.Value).Returns(invalidOptions);

        // Act & Assert
        var action = () => new KeyManagementService(_mockLogger.Object, mockOptions.Object);
        action.Should().Throw<ArgumentException>()
            .WithMessage("JWT设置不能为空 (Parameter 'JwtSettings')");
    }

    [Fact]
    public void Constructor_WithNullKeyRotationSettings_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidOptions = new SecurityOptions
        {
            JwtSettings = new JwtSettings { Secret = "test-secret" },
            KeyRotation = null!
        };
        var mockOptions = new Mock<IOptions<SecurityOptions>>();
        mockOptions.Setup(x => x.Value).Returns(invalidOptions);

        // Act & Assert
        var action = () => new KeyManagementService(_mockLogger.Object, mockOptions.Object);
        action.Should().Throw<ArgumentException>()
            .WithMessage("密钥轮换设置不能为空 (Parameter 'KeyRotation')");
    }

    private static bool IsValidBase64String(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}