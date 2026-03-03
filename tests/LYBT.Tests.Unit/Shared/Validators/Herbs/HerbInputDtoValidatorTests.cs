using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Primitives.Validation;
using LYBT.Shared.Validators.Herbs;
using Xunit;

namespace LYBT.Tests.Unit.Shared.Validators.Herbs;

/// <summary>
/// HerbInputDtoValidator 单元测试
/// 验证规则：Name(必填,1-100) + Unit(必填,最长10) + Price(>0,<=100000) + 各种长度限制
/// </summary>
public class HerbInputDtoValidatorTests
{
    private readonly HerbInputDtoValidator _validator = new();

    #region Valid Input Tests

    [Fact]
    public void Validate_WithValidInput_ShouldPass()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMinimalValidInput_ShouldPass()
    {
        // Arrange - 仅必填字段
        var dto = new HerbInputDto
        {
            Name = "黄芪",
            Unit = "克",
            Price = 0.5m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Name Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ShouldFail(string? name)
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Name = name!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("药材名称不能为空");
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Name = new string('黄', ValidationConstants.NameMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameAtMaxLength_ShouldPass()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Name = new string('黄', ValidationConstants.NameMaxLength);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Unit Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyUnit_ShouldFail(string? unit)
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Unit = unit!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Unit)
            .WithErrorMessage("单位不能为空");
    }

    [Fact]
    public void Validate_WithUnitTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Unit = new string('克', 11); // > 10

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Unit)
            .WithErrorMessage("单位长度不能超过10个字符");
    }

    [Theory]
    [InlineData("克")]
    [InlineData("g")]
    [InlineData("毫升")]
    [InlineData("ml")]
    public void Validate_WithValidUnit_ShouldPass(string unit)
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Unit = unit;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Unit);
    }

    #endregion

    #region Price Validation Tests

    [Fact]
    public void Validate_WithZeroPrice_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Price = 0;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("单价必须大于0");
    }

    [Fact]
    public void Validate_WithNegativePrice_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Price = -1;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("单价必须大于0");
    }

    [Fact]
    public void Validate_WithPriceOverMax_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Price = ValidationConstants.PriceMaxValue + 1;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage($"单价不能超过{ValidationConstants.PriceMaxValue}");
    }

    [Theory]
    [InlineData(0.02)]  // 大于 PriceMinValue (0.01)
    [InlineData(0.5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Validate_WithValidPrice_ShouldPass(decimal price)
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Price = price;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_WithPriceAtMaxValue_ShouldPass()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Price = ValidationConstants.PriceMaxValue;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    #endregion

    #region Optional Field Length Tests

    [Fact]
    public void Validate_WithPinYinCodeTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.PinYinCode = new string('a', ValidationConstants.CodeMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PinYinCode)
            .WithErrorMessage($"拼音码长度不能超过{ValidationConstants.CodeMaxLength}个字符");
    }

    [Fact]
    public void Validate_WithCategoryTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Category = new string('补', ValidationConstants.CodeMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Category)
            .WithErrorMessage($"分类长度不能超过{ValidationConstants.CodeMaxLength}个字符");
    }

    [Fact]
    public void Validate_WithOriginTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Origin = new string('甘', 101); // > 100

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Origin)
            .WithErrorMessage("产地长度不能超过100个字符");
    }

    [Fact]
    public void Validate_WithSpecTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Spec = new string('特', 101); // > 100

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Spec)
            .WithErrorMessage("规格长度不能超过100个字符");
    }

    [Fact]
    public void Validate_WithEffectTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Effect = new string('补', 501); // > 500

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Effect)
            .WithErrorMessage("功效长度不能超过500个字符");
    }

    [Fact]
    public void Validate_WithUsageTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        dto.Usage = new string('用', ValidationConstants.UsageMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Usage)
            .WithErrorMessage($"用法用量长度不能超过{ValidationConstants.UsageMaxLength}个字符");
    }

    [Fact]
    public void Validate_WithRemarkTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
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
        var dto = new HerbInputDto
        {
            Name = "",
            Unit = "",
            Price = -1
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.Errors.Should().HaveCountGreaterOrEqualTo(3);
        result.Errors.Should().Contain(e => e.ErrorMessage == "药材名称不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "单位不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "单价必须大于0");
    }

    #endregion

    #region Helper Methods

    private static HerbInputDto CreateValidHerbInputDto()
    {
        return new HerbInputDto
        {
            Name = "黄芪",
            PinYinCode = "HQ",
            Category = "补气药",
            Origin = "甘肃",
            Spec = "特级",
            Unit = "克",
            Price = 0.5m,
            Effect = "补气升阳，固表止汗",
            Usage = "煎服，10-30克",
            Remark = "无"
        };
    }

    #endregion
}
