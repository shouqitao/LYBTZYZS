using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Validators.Formula;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Validators.Formula;

/// <summary>
/// FormulaInputDtoValidator 单元测试
/// 验证规则：Name(必填,最长100) + Herbs(必填,至少一个) + 各种长度限制
/// </summary>
public class FormulaInputDtoValidatorTests
{
    private readonly FormulaInputDtoValidator _validator = new();

    #region Valid Input Tests

    [Fact]
    public void Validate_WithValidInput_ShouldPass()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMinimalValidInput_ShouldPass()
    {
        // Arrange - 仅必填字段
        var dto = new FormulaInputDto
        {
            Name = "四君子汤",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new()
                {
                    HerbName = "人参",
                    Dosage = 10,
                    Unit = "克"
                }
            }
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
        var dto = CreateValidFormulaInputDto();
        dto.Name = name!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("方剂名称不能为空");
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Name = new string('汤', 101); // > 100

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("方剂名称长度不能超过100个字符");
    }

    [Fact]
    public void Validate_WithNameAtMaxLength_ShouldPass()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Name = new string('汤', 100);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Optional Field Length Tests

    [Fact]
    public void Validate_WithEffectTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Effect = new string('补', 501); // > 500

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Effect)
            .WithErrorMessage("功效长度不能超过500个字符");
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Description = new string('描', 1001); // > 1000

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("描述长度不能超过1000个字符");
    }

    [Fact]
    public void Validate_WithUsageTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Usage = new string('用', 501); // > 500

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Usage)
            .WithErrorMessage("用法长度不能超过500个字符");
    }

    [Fact]
    public void Validate_WithIndicationsTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Indications = new string('主', 501); // > 500

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Indications)
            .WithErrorMessage("主治长度不能超过500个字符");
    }

    [Fact]
    public void Validate_WithRemarkTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Remark = new string('备', 1001); // > 1000

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Remark)
            .WithErrorMessage("备注长度不能超过1000个字符");
    }

    #endregion

    #region Herbs Validation Tests

    [Fact]
    public void Validate_WithEmptyHerbs_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Herbs)
            .WithErrorMessage("方剂必须包含至少一味药材");
    }

    [Fact]
    public void Validate_WithNullHerbs_ShouldFail()
    {
        // Arrange - Herbs为null时触发NotNull验证
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = null!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Herbs)
            .WithErrorMessage("药材列表不能为空");
    }

    [Fact]
    public void Validate_WithValidHerbs_ShouldPass()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = "人参", Dosage = 10, Unit = "克" },
            new() { HerbName = "白术", Dosage = 15, Unit = "克" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Herbs);
    }

    #endregion

    #region HerbItem Validation Tests

    [Fact]
    public void Validate_HerbItem_WithEmptyHerbName_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = "", Dosage = 10, Unit = "克" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Herbs[0].HerbName")
            .WithErrorMessage("药材名称不能为空");
    }

    [Fact]
    public void Validate_HerbItem_WithHerbNameTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = new string('参', 101), Dosage = 10, Unit = "克" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Herbs[0].HerbName")
            .WithErrorMessage("药材名称长度不能超过100个字符");
    }

    [Fact]
    public void Validate_HerbItem_WithZeroDosage_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = "人参", Dosage = 0, Unit = "克" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Herbs[0].Dosage")
            .WithErrorMessage("用量必须大于0");
    }

    [Fact]
    public void Validate_HerbItem_WithDosageOver1000_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = "人参", Dosage = 1001, Unit = "克" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Herbs[0].Dosage")
            .WithErrorMessage("用量不能超过1000克");
    }

    [Fact]
    public void Validate_HerbItem_WithEmptyUnit_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = "人参", Dosage = 10, Unit = "" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Herbs[0].Unit")
            .WithErrorMessage("单位不能为空");
    }

    [Fact]
    public void Validate_HerbItem_WithUnitTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = "人参", Dosage = 10, Unit = new string('克', 11) }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Herbs[0].Unit")
            .WithErrorMessage("单位长度不能超过10个字符");
    }

    [Fact]
    public void Validate_HerbItem_WithProcessingMethodTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = "人参", Dosage = 10, Unit = "克", ProcessingMethod = new string('炒', 101) }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Herbs[0].ProcessingMethod")
            .WithErrorMessage("加工方法长度不能超过100个字符");
    }

    [Fact]
    public void Validate_HerbItem_WithUsageTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidFormulaInputDto();
        dto.Herbs = new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = "人参", Dosage = 10, Unit = "克", Usage = new string('用', 201) }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Herbs[0].Usage")
            .WithErrorMessage("用法长度不能超过200个字符");
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Validate_ShouldReturnCorrectErrorMessages()
    {
        // Arrange
        var dto = new FormulaInputDto
        {
            Name = "",
            Herbs = new List<FormulaHerbItemInputDto>()
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
        result.Errors.Should().Contain(e => e.ErrorMessage == "方剂名称不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "方剂必须包含至少一味药材");
    }

    #endregion

    #region Helper Methods

    private static FormulaInputDto CreateValidFormulaInputDto()
    {
        return new FormulaInputDto
        {
            Name = "四君子汤",
            Effect = "补气健脾",
            Description = "补气健脾的基础方剂",
            Usage = "水煎服，日一剂",
            Indications = "脾胃气虚",
            Remark = "经典方剂",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbName = "人参", Dosage = 10, Unit = "克" },
                new() { HerbName = "白术", Dosage = 15, Unit = "克" },
                new() { HerbName = "茯苓", Dosage = 15, Unit = "克" },
                new() { HerbName = "甘草", Dosage = 6, Unit = "克" }
            }
        };
    }

    #endregion
}
