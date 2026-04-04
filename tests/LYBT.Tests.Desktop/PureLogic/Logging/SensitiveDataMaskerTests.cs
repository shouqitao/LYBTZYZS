using FluentAssertions;
using LYBT.Shared.Logging.Masking;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Logging;

/// <summary>
/// US-LOG-003: 系统应对日志中的敏感数据进行脱敏处理，防止敏感信息泄露到日志文件。
/// 验证 SensitiveDataMasker 的核心脱敏行为。
/// </summary>
public class SensitiveDataMaskerTests
{
    [Fact]
    public void US_LOG_003_Mask_FullMode_ReturnsHiddenText()
    {
        // Arrange / Act
        var result = SensitiveDataMasker.Mask("secretvalue", MaskingMode.Full);

        // Assert
        result.Should().Be("[已隐藏]");
    }

    [Fact]
    public void US_LOG_003_Mask_FullMode_EmptyInput_ReturnsEmpty()
    {
        // Arrange / Act
        var result = SensitiveDataMasker.Mask("", MaskingMode.Full);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void US_LOG_003_Mask_HashMode_StartsWithRedactedPrefix()
    {
        // Arrange / Act
        var result = SensitiveDataMasker.Mask("secretvalue", MaskingMode.Hash);

        // Assert
        result.Should().StartWith("[REDACTED:");
        result.Should().EndWith("]");
    }

    [Fact]
    public void US_LOG_003_Mask_HashMode_Contains8HexCharacters()
    {
        // Arrange / Act
        var result = SensitiveDataMasker.Mask("secretvalue", MaskingMode.Hash);

        // Assert — format is [REDACTED:XXXXXXXX]
        var inner = result.Replace("[REDACTED:", "").Replace("]", "");
        inner.Length.Should().Be(8);
        inner.Should().MatchRegex("^[0-9A-F]{8}$");
    }

    [Fact]
    public void US_LOG_003_Mask_HashMode_SameInputProducesSameHash()
    {
        // Arrange / Act
        var result1 = SensitiveDataMasker.Mask("secretvalue", MaskingMode.Hash);
        var result2 = SensitiveDataMasker.Mask("secretvalue", MaskingMode.Hash);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void US_LOG_003_IsSensitiveFieldName_Password_ReturnsTrue()
    {
        // Arrange / Act / Assert
        SensitiveDataMasker.IsSensitiveFieldName("Password").Should().BeTrue();
    }

    [Fact]
    public void US_LOG_003_IsSensitiveFieldName_Token_ReturnsTrue()
    {
        // Arrange / Act / Assert
        SensitiveDataMasker.IsSensitiveFieldName("Token").Should().BeTrue();
    }

    [Fact]
    public void US_LOG_003_IsSensitiveFieldName_AccessToken_ReturnsTrue()
    {
        // Arrange / Act / Assert
        SensitiveDataMasker.IsSensitiveFieldName("AccessToken").Should().BeTrue();
    }

    [Fact]
    public void US_LOG_003_IsSensitiveFieldName_UserName_ReturnsFalse()
    {
        // Arrange / Act / Assert
        SensitiveDataMasker.IsSensitiveFieldName("UserName").Should().BeFalse();
    }

    [Fact]
    public void US_LOG_003_IsSensitiveFieldName_NullOrEmpty_ReturnsFalse()
    {
        // Arrange / Act / Assert
        SensitiveDataMasker.IsSensitiveFieldName(null).Should().BeFalse();
        SensitiveDataMasker.IsSensitiveFieldName("").Should().BeFalse();
    }

    [Fact]
    public void US_LOG_003_SanitizeText_MasksBearerToken()
    {
        // Arrange
        var input = "Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.token";

        // Act
        var result = SensitiveDataMasker.SanitizeText(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiJ9");
    }

    [Fact]
    public void US_LOG_003_SanitizeText_MasksPasswordField()
    {
        // Arrange
        var input = "password=mysecretpassword";

        // Act
        var result = SensitiveDataMasker.SanitizeText(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("mysecretpassword");
    }

    [Fact]
    public void US_LOG_003_MaskUri_MasksPasswordQueryParameter()
    {
        // Arrange
        var uri = "http://api.example.com/login?password=secret123&user=admin";

        // Act
        var result = SensitiveDataMasker.MaskUri(uri);

        // Assert
        result.Should().Contain("password=***");
        result.Should().NotContain("secret123");
        result.Should().Contain("user=admin");
    }

    [Fact]
    public void US_LOG_003_MaskUri_MasksTokenQueryParameter()
    {
        // Arrange
        var uri = "http://api.example.com/refresh?token=abc123def456";

        // Act
        var result = SensitiveDataMasker.MaskUri(uri);

        // Assert
        result.Should().Contain("token=***");
        result.Should().NotContain("abc123def456");
    }

    [Fact]
    public void US_LOG_003_MaskUri_NullInput_ReturnsEmpty()
    {
        // Arrange / Act
        var result = SensitiveDataMasker.MaskUri(null);

        // Assert
        result.Should().BeEmpty();
    }
}
