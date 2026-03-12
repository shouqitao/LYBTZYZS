using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Validators.Prescriptions;
using Xunit;

namespace LYBT.Tests.Server.Unit.Validators.Prescriptions;

/// <summary>
/// PrescriptionInputDtoValidator 单元测试
/// 验证规则：MedicalCaseId(创建时必填) + Items(必填,至少一项) + DosageCount(1-100) + Discount(0-1) + 各种长度限制
/// </summary>
public class PrescriptionInputDtoValidatorTests
{
    private readonly PrescriptionInputDtoValidator _validator = new();

    #region Valid Input Tests

    [Fact]
    public void Validate_WithValidInput_ShouldPass()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMinimalValidInput_ShouldPass()
    {
        // Arrange - 仅必填字段
        var dto = new PrescriptionInputDto
        {
            MedicalCaseId = Guid.NewGuid(),
            DosageCount = 3,
            Discount = 1,
            Items = new List<PrescriptionItemInputDto>
            {
                new() { HerbId = Guid.NewGuid(), Dosage = 10 }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region MedicalCaseId Validation Tests

    [Fact]
    public void Validate_WithEmptyMedicalCaseId_OnCreate_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Id = null; // 创建模式
        dto.MedicalCaseId = Guid.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MedicalCaseId)
            .WithErrorMessage("医疗案例ID不能为空");
    }

    [Fact]
    public void Validate_WithEmptyMedicalCaseId_OnUpdate_ShouldPass()
    {
        // Arrange - 更新时不需要 MedicalCaseId
        var dto = CreateValidPrescriptionInputDto();
        dto.Id = Guid.NewGuid(); // 更新模式
        dto.MedicalCaseId = Guid.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MedicalCaseId);
    }

    #endregion

    #region Optional Field Length Tests

    [Fact]
    public void Validate_WithReferencedFormulasTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.ReferencedFormulas = new string('方', 501); // > 500

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReferencedFormulas)
            .WithErrorMessage("引用验方长度不能超过500个字符");
    }

    [Fact]
    public void Validate_WithAdviceTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Advice = new string('嘱', 501); // > 500

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Advice)
            .WithErrorMessage("医嘱长度不能超过500个字符");
    }

    [Fact]
    public void Validate_WithRemarkTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Remark = new string('备', 501); // > 500

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Remark)
            .WithErrorMessage("备注长度不能超过500个字符");
    }

    #endregion

    #region Discount Validation Tests

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2)]
    public void Validate_WithInvalidDiscount_ShouldFail(decimal discount)
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Discount = discount;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Discount)
            .WithErrorMessage("折扣必须在0到1之间");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(0.8)]
    [InlineData(1)]
    public void Validate_WithValidDiscount_ShouldPass(decimal discount)
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Discount = discount;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Discount);
    }

    #endregion

    #region DosageCount Validation Tests

    [Fact]
    public void Validate_WithZeroDosageCount_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.DosageCount = 0;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DosageCount)
            .WithErrorMessage("剂数必须大于0");
    }

    [Fact]
    public void Validate_WithNegativeDosageCount_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.DosageCount = -1;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DosageCount)
            .WithErrorMessage("剂数必须大于0");
    }

    [Fact]
    public void Validate_WithDosageCountOver100_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.DosageCount = 101;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DosageCount)
            .WithErrorMessage("剂数不能超过100");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(14)]
    [InlineData(100)]
    public void Validate_WithValidDosageCount_ShouldPass(int dosageCount)
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.DosageCount = dosageCount;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DosageCount);
    }

    #endregion

    #region Items Validation Tests

    [Fact]
    public void Validate_WithEmptyItems_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Items = new List<PrescriptionItemInputDto>();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WithNullItems_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Items = null!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("处方明细不能为空");
    }

    [Fact]
    public void Validate_WithValidItems_ShouldPass()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    #endregion

    #region Item Validation Tests

    [Fact]
    public void Validate_Item_WithEmptyHerbId_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Items = new List<PrescriptionItemInputDto>
        {
            new() { HerbId = Guid.Empty, Dosage = 10 }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].HerbId")
            .WithErrorMessage("药材ID不能为空");
    }

    [Fact]
    public void Validate_Item_WithZeroDosage_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Items = new List<PrescriptionItemInputDto>
        {
            new() { HerbId = Guid.NewGuid(), Dosage = 0 }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Dosage")
            .WithErrorMessage("用量必须大于0");
    }

    [Fact]
    public void Validate_Item_WithDosageOver1000_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Items = new List<PrescriptionItemInputDto>
        {
            new() { HerbId = Guid.NewGuid(), Dosage = 1001 }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Dosage")
            .WithErrorMessage("用量不能超过1000克");
    }

    [Fact]
    public void Validate_Item_WithUsageTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Items = new List<PrescriptionItemInputDto>
        {
            new() { HerbId = Guid.NewGuid(), Dosage = 10, Usage = new string('用', 201) }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Usage")
            .WithErrorMessage("用法长度不能超过200个字符");
    }

    [Fact]
    public void Validate_Item_WithRemarkTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPrescriptionInputDto();
        dto.Items = new List<PrescriptionItemInputDto>
        {
            new() { HerbId = Guid.NewGuid(), Dosage = 10, Remark = new string('备', 501) }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Remark")
            .WithErrorMessage("备注长度不能超过500个字符");
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Validate_ShouldReturnCorrectErrorMessages()
    {
        // Arrange
        var dto = new PrescriptionInputDto
        {
            Id = null,
            MedicalCaseId = Guid.Empty,
            DosageCount = 0,
            Discount = 2,
            Items = new List<PrescriptionItemInputDto>()
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
        result.Errors.Should().Contain(e => e.ErrorMessage == "医疗案例ID不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "剂数必须大于0");
        result.Errors.Should().Contain(e => e.ErrorMessage == "折扣必须在0到1之间");
    }

    #endregion

    #region Helper Methods

    private static PrescriptionInputDto CreateValidPrescriptionInputDto()
    {
        return new PrescriptionInputDto
        {
            MedicalCaseId = Guid.NewGuid(),
            DosageCount = 3,
            Discount = 1,
            ReferencedFormulas = "四君子汤",
            Advice = "忌辛辣，清淡饮食",
            Remark = "初诊处方",
            Items = new List<PrescriptionItemInputDto>
            {
                new() { HerbId = Guid.NewGuid(), Dosage = 10, Usage = "先煎" },
                new() { HerbId = Guid.NewGuid(), Dosage = 15 },
                new() { HerbId = Guid.NewGuid(), Dosage = 15 }
            }
        };
    }

    #endregion
}
