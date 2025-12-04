using LYBT.Entities.Attributes;
using LYBT.Infrastructure.Logging;
using Xunit;

namespace LYBT.Infrastructure.Tests.Logging;

/// <summary>
/// 敏感数据脱敏器单元测试
/// Issue #2254: 验证各种脱敏模式正确工作
/// </summary>
public class SensitiveDataMaskerTests
{
    #region Partial Masking Tests

    [Theory]
    [InlineData("13812345678", SensitiveDataType.ContactInfo, "138****5678")]
    [InlineData("1234567", SensitiveDataType.ContactInfo, "123****4567")]
    public void Mask_ContactInfo_Partial_ShouldMaskMiddle(string input, SensitiveDataType dataType, string expected)
    {
        // Act
        var result = SensitiveDataMasker.Mask(input, MaskingMode.Partial, dataType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("110101199001011234", SensitiveDataType.IdentityInfo)]
    [InlineData("12345678901234", SensitiveDataType.IdentityInfo)]
    public void Mask_IdentityInfo_Partial_ShouldShowFirstThreeAndLastFour(string input, SensitiveDataType dataType)
    {
        // Act
        var result = SensitiveDataMasker.Mask(input, MaskingMode.Partial, dataType);

        // Assert
        Assert.StartsWith(input[..3], result);
        Assert.EndsWith(input[^4..], result);
        Assert.Contains("*", result);
    }

    [Theory]
    [InlineData("北京市海淀区中关村大街1号", SensitiveDataType.PersonalInfo)]
    [InlineData("测试地址信息", SensitiveDataType.PersonalInfo)]
    public void Mask_PersonalInfo_Partial_ShouldShowFirstTwoAndLastTwo(string input, SensitiveDataType dataType)
    {
        // Act
        var result = SensitiveDataMasker.Mask(input, MaskingMode.Partial, dataType);

        // Assert
        Assert.StartsWith(input[..2], result);
        Assert.EndsWith(input[^2..], result);
        Assert.Contains("*", result);
    }

    [Fact]
    public void Mask_ShortString_Partial_ShouldReturnStars()
    {
        // Arrange
        var shortInput = "AB";

        // Act
        var result = SensitiveDataMasker.Mask(shortInput, MaskingMode.Partial, SensitiveDataType.PersonalInfo);

        // Assert
        Assert.Equal("****", result);
    }

    #endregion

    #region Full Masking Tests

    [Theory]
    [InlineData("任何敏感信息")]
    [InlineData("13812345678")]
    [InlineData("110101199001011234")]
    public void Mask_Full_ShouldReturnHiddenMarker(string input)
    {
        // Act
        var result = SensitiveDataMasker.Mask(input, MaskingMode.Full);

        // Assert
        Assert.Equal("[已隐藏]", result);
    }

    #endregion

    #region Hash Masking Tests

    [Fact]
    public void Mask_Hash_ShouldReturnConsistentHash()
    {
        // Arrange
        var input = "这是一段很长的病史记录，包含详细的诊断信息";

        // Act
        var result1 = SensitiveDataMasker.Mask(input, MaskingMode.Hash);
        var result2 = SensitiveDataMasker.Mask(input, MaskingMode.Hash);

        // Assert
        Assert.Equal(result1, result2); // 相同输入应产生相同哈希
        Assert.StartsWith("[REDACTED:", result1);
        Assert.EndsWith("]", result1);
    }

    [Fact]
    public void Mask_Hash_DifferentInputs_ShouldReturnDifferentHashes()
    {
        // Arrange
        var input1 = "病史记录1";
        var input2 = "病史记录2";

        // Act
        var result1 = SensitiveDataMasker.Mask(input1, MaskingMode.Hash);
        var result2 = SensitiveDataMasker.Mask(input2, MaskingMode.Hash);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    #endregion

    #region Default Masking Tests

    [Theory]
    [InlineData("A", "**")]
    [InlineData("AB", "**")]
    public void Mask_Default_VeryShortString_ShouldReturnTwoStars(string input, string expected)
    {
        // Act
        var result = SensitiveDataMasker.Mask(input, MaskingMode.Default);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Mask_Default_MediumString_ShouldShowFirstAndLast()
    {
        // Arrange
        var input = "测试中";

        // Act
        var result = SensitiveDataMasker.Mask(input, MaskingMode.Default);

        // Assert
        Assert.StartsWith("测", result);
        Assert.EndsWith("中", result);
    }

    [Fact]
    public void Mask_Default_LongString_ShouldShowFirstThreeAndLastThree()
    {
        // Arrange
        var input = "这是一个比较长的测试字符串";

        // Act
        var result = SensitiveDataMasker.Mask(input, MaskingMode.Default);

        // Assert
        Assert.StartsWith(input[..3], result);
        Assert.EndsWith(input[^3..], result);
    }

    #endregion

    #region Null and Empty Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Mask_NullOrEmpty_ShouldReturnEmpty(string? input)
    {
        // Act
        var result = SensitiveDataMasker.Mask(input, MaskingMode.Default);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region MaskObject Tests

    [Fact]
    public void MaskObject_WithSensitiveProperties_ShouldMaskCorrectly()
    {
        // Arrange
        var testObj = new TestSensitiveClass
        {
            Name = "张三",
            PhoneNumber = "13812345678",
            IdNumber = "110101199001011234",
            NormalProperty = "普通属性"
        };

        // Act
        var result = SensitiveDataMasker.MaskObject(testObj);

        // Assert
        Assert.Equal("张三", result["Name"]); // 非敏感属性不脱敏
        Assert.NotEqual("13812345678", result["PhoneNumber"]); // 敏感属性已脱敏
        Assert.NotEqual("110101199001011234", result["IdNumber"]); // 敏感属性已脱敏
        Assert.Equal("普通属性", result["NormalProperty"]); // 非敏感属性不脱敏
    }

    /// <summary>
    /// 测试用敏感数据类
    /// </summary>
    private class TestSensitiveClass
    {
        public string Name { get; set; } = string.Empty;

        [SensitiveData(SensitiveDataType.ContactInfo, MaskingMode = MaskingMode.Partial)]
        public string PhoneNumber { get; set; } = string.Empty;

        [SensitiveData(SensitiveDataType.IdentityInfo, MaskingMode = MaskingMode.Partial)]
        public string IdNumber { get; set; } = string.Empty;

        public string NormalProperty { get; set; } = string.Empty;
    }

    #endregion
}
