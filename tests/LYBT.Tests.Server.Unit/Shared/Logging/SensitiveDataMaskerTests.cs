using FluentAssertions;
using Xunit;
using LYBT.Shared.Logging.Masking;

namespace LYBT.Tests.Server.Unit.Shared.Logging;

/// <summary>
/// SensitiveDataMasker 单元测试
/// Sprint3-A3-09: Shared.Logging 零覆盖测试
/// </summary>
public class SensitiveDataMaskerTests
{
    #region Mask 方法测试

    [Fact]
    public void Mask_WithNullValue_ShouldReturnEmpty()
    {
        var result = SensitiveDataMasker.Mask(null, MaskingMode.Default);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Mask_WithEmptyValue_ShouldReturnEmpty()
    {
        var result = SensitiveDataMasker.Mask("", MaskingMode.Full);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Mask_WithFullMode_ShouldReturnHiddenText()
    {
        var result = SensitiveDataMasker.Mask("sensitive_data", MaskingMode.Full);
        result.Should().Be("[已隐藏]");
    }

    [Fact]
    public void Mask_WithHashMode_ShouldReturnRedactedWithHash()
    {
        var result = SensitiveDataMasker.Mask("test_password", MaskingMode.Hash);
        result.Should().StartWith("[REDACTED:");
        result.Should().EndWith("]");
        result.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void Mask_WithPartialMode_PhoneNumber_ShouldMaskMiddle()
    {
        var result = SensitiveDataMasker.Mask("13812345678", MaskingMode.Partial, SensitiveDataType.ContactInfo);
        result.Should().StartWith("138");
        result.Should().EndWith("5678");
        result.Should().Contain("****");
    }

    [Fact]
    public void Mask_WithDefaultMode_LongString_ShouldShowFirstAndLast()
    {
        var result = SensitiveDataMasker.Mask("abcdefghij", MaskingMode.Default);
        result.Should().StartWith("abc");
        result.Should().EndWith("hij");
        result.Should().Contain("*");
    }

    [Fact]
    public void Mask_WithDefaultMode_ShortString_ShouldReturnStars()
    {
        var result = SensitiveDataMasker.Mask("ab", MaskingMode.Default);
        result.Should().Be("**");
    }

    #endregion

    #region IsSensitiveFieldName 测试

    [Theory]
    [InlineData("Password", true)]
    [InlineData("AccessToken", true)]
    [InlineData("SecretKey", true)]
    [InlineData("ConnectionString", true)]
    [InlineData("UserName", false)]
    [InlineData("Email", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsSensitiveFieldName_ShouldDetectCorrectly(string? fieldName, bool expected)
    {
        SensitiveDataMasker.IsSensitiveFieldName(fieldName).Should().Be(expected);
    }

    #endregion

    #region SanitizeText 测试

    [Fact]
    public void SanitizeText_WithPassword_ShouldRedact()
    {
        var result = SensitiveDataMasker.SanitizeText("password=mysecret123");
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("mysecret123");
    }

    [Fact]
    public void SanitizeText_WithBearerToken_ShouldRedact()
    {
        // PasswordPattern 优先匹配 "Authorization:" 字段
        var result = SensitiveDataMasker.SanitizeText("token=eyJhbGciOiJIUzI1NiJ9");
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("eyJhbGci");
    }

    [Fact]
    public void SanitizeText_WithNullInput_ShouldReturnEmpty()
    {
        SensitiveDataMasker.SanitizeText(null).Should().BeEmpty();
    }

    #endregion

    #region MaskUri 测试

    [Fact]
    public void MaskUri_WithSensitiveParams_ShouldRedact()
    {
        var result = SensitiveDataMasker.MaskUri("https://api.example.com?token=abc123&name=test");
        result.Should().Contain("token=***");
        result.Should().Contain("name=test");
    }

    [Fact]
    public void MaskUri_WithNull_ShouldReturnEmpty()
    {
        SensitiveDataMasker.MaskUri(null).Should().BeEmpty();
    }

    #endregion

    #region SanitizeException 测试

    [Fact]
    public void SanitizeException_WithNull_ShouldReturnEmpty()
    {
        SensitiveDataMasker.SanitizeException(null).Should().BeEmpty();
    }

    [Fact]
    public void SanitizeException_WithException_ShouldContainTypeName()
    {
        var ex = new InvalidOperationException("password=secret123");
        var result = SensitiveDataMasker.SanitizeException(ex);
        result.Should().Contain("InvalidOperationException");
        result.Should().NotContain("secret123");
    }

    #endregion

    #region MaskObject 测试

    [Fact]
    public void MaskObject_WithSensitiveProperties_ShouldMask()
    {
        var obj = new TestObjectWithSensitive
        {
            UserName = "test_user",
            Password = "my_secret"
        };

        var result = SensitiveDataMasker.MaskObject(obj);
        result["UserName"].Should().Be("test_user");
        result["Password"].Should().NotBe("my_secret");
    }

    private class TestObjectWithSensitive
    {
        public string UserName { get; set; } = "";

        [SensitiveData(SensitiveDataType.PersonalInfo, MaskingMode = MaskingMode.Full)]
        public string Password { get; set; } = "";
    }

    #endregion
}
